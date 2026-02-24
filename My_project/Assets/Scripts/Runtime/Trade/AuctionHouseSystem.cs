using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// Server-authoritative auction house system for item trading.
    /// Supports bidding, buyout, and timed listings with mail-based delivery.
    /// </summary>
    public class AuctionHouseSystem : NetworkBehaviour
    {
        public static AuctionHouseSystem Instance { get; private set; }

        #region Constants

        public const int MAX_LISTINGS_PER_PLAYER = 10;
        public const float COMMISSION_RATE = 0.05f;
        private static readonly int[] DURATION_OPTIONS = { 43200, 86400, 172800 }; // 12h, 24h, 48h
        private const float EXPIRY_CHECK_INTERVAL = 30f;

        #endregion

        #region Data Classes

        [Serializable]
        public class AuctionListing
        {
            public string listingId;
            public ulong sellerClientId;
            public string itemId;
            public string itemName;
            public int itemGrade;
            public long startPrice;
            public long buyoutPrice;
            public float endTime;
            public ulong highestBidderId;
            public long highestBid;
            public bool isActive;
        }

        [Serializable]
        public struct AuctionListingInfo
        {
            public string listingId;
            public string itemName;
            public int itemGrade;
            public long startPrice;
            public long buyoutPrice;
            public float remainingTime;
            public long highestBid;
            public bool hasBids;
        }

        #endregion

        #region Events

        public event Action OnListingsUpdated;
        public event Action<string> OnAuctionWon;

        #endregion

        #region Server State

        private List<AuctionListing> activeListings = new List<AuctionListing>();
        private Dictionary<ulong, int> playerListingCounts = new Dictionary<ulong, int>();
        private float nextExpiryCheck;

        #endregion

        #region Client State

        public List<AuctionListingInfo> localListings = new List<AuctionListingInfo>();

        #endregion

        #region Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        private void Update()
        {
            if (!IsServer) return;
            if (Time.time < nextExpiryCheck) return;

            nextExpiryCheck = Time.time + EXPIRY_CHECK_INTERVAL;
            ProcessExpiredListings();
        }

        #endregion

        #region Server RPCs

        [ServerRpc(RequireOwnership = false)]
        public void CreateListingServerRpc(string itemId, string itemName, int grade,
            long startPrice, long buyoutPrice, int durationIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (durationIndex < 0 || durationIndex >= DURATION_OPTIONS.Length)
            {
                Debug.LogWarning($"[AuctionHouse] Invalid duration index {durationIndex} from client {clientId}");
                return;
            }

            if (startPrice <= 0 || buyoutPrice < startPrice)
            {
                Debug.LogWarning($"[AuctionHouse] Invalid prices from client {clientId}");
                return;
            }

            int currentCount = playerListingCounts.GetValueOrDefault(clientId, 0);
            if (currentCount >= MAX_LISTINGS_PER_PLAYER)
            {
                Debug.LogWarning($"[AuctionHouse] Client {clientId} exceeded max listings");
                return;
            }

            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            long commissionDeposit = (long)(startPrice * COMMISSION_RATE);
            if (commissionDeposit < 1) commissionDeposit = 1;

            if (statsData.Gold < commissionDeposit)
            {
                Debug.LogWarning($"[AuctionHouse] Client {clientId} insufficient gold for commission");
                return;
            }

            statsData.ChangeGold(-commissionDeposit);

            var listing = new AuctionListing
            {
                listingId = Guid.NewGuid().ToString().Substring(0, 8),
                sellerClientId = clientId,
                itemId = itemId,
                itemName = itemName,
                itemGrade = grade,
                startPrice = startPrice,
                buyoutPrice = buyoutPrice,
                endTime = Time.time + DURATION_OPTIONS[durationIndex],
                highestBidderId = ulong.MaxValue,
                highestBid = 0,
                isActive = true
            };

            activeListings.Add(listing);
            playerListingCounts[clientId] = currentCount + 1;

            NotifyListingCreatedClientRpc(listing.listingId, itemName, startPrice, clientId);
            BroadcastListings();

            Debug.Log($"[AuctionHouse] Listing {listing.listingId} created by client {clientId}: {itemName}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlaceBidServerRpc(string listingId, long bidAmount, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var listing = activeListings.FirstOrDefault(l => l.listingId == listingId && l.isActive);

            if (listing == null)
            {
                Debug.LogWarning($"[AuctionHouse] Listing {listingId} not found or inactive");
                return;
            }

            if (listing.sellerClientId == clientId)
            {
                Debug.LogWarning($"[AuctionHouse] Client {clientId} cannot bid on own listing");
                return;
            }

            long minimumBid = listing.highestBid > 0 ? listing.highestBid + 1 : listing.startPrice;
            if (bidAmount < minimumBid)
            {
                Debug.LogWarning($"[AuctionHouse] Bid {bidAmount} below minimum {minimumBid}");
                return;
            }

            if (Time.time >= listing.endTime)
            {
                Debug.LogWarning($"[AuctionHouse] Listing {listingId} has expired");
                return;
            }

            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null || statsData.Gold < bidAmount) return;

            // Refund previous highest bidder
            if (listing.highestBidderId != ulong.MaxValue && listing.highestBid > 0)
            {
                RefundBidder(listing.highestBidderId, listing.highestBid, listing.itemName);
            }

            // Hold new bidder's gold
            statsData.ChangeGold(-bidAmount);
            listing.highestBidderId = clientId;
            listing.highestBid = bidAmount;

            NotifyBidPlacedClientRpc(listingId, bidAmount, clientId);
            BroadcastListings();

            Debug.Log($"[AuctionHouse] Bid {bidAmount} placed on {listingId} by client {clientId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void BuyoutServerRpc(string listingId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var listing = activeListings.FirstOrDefault(l => l.listingId == listingId && l.isActive);

            if (listing == null || listing.buyoutPrice <= 0)
            {
                Debug.LogWarning($"[AuctionHouse] Buyout failed for listing {listingId}");
                return;
            }

            if (listing.sellerClientId == clientId)
            {
                Debug.LogWarning($"[AuctionHouse] Client {clientId} cannot buyout own listing");
                return;
            }

            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null || statsData.Gold < listing.buyoutPrice) return;

            // Refund previous highest bidder
            if (listing.highestBidderId != ulong.MaxValue && listing.highestBid > 0)
            {
                RefundBidder(listing.highestBidderId, listing.highestBid, listing.itemName);
            }

            // Deduct buyout price from buyer
            statsData.ChangeGold(-listing.buyoutPrice);

            // Complete the auction
            CompleteAuction(listing, clientId, listing.buyoutPrice);

            Debug.Log($"[AuctionHouse] Buyout of {listingId} by client {clientId} for {listing.buyoutPrice}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void CancelListingServerRpc(string listingId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var listing = activeListings.FirstOrDefault(l => l.listingId == listingId && l.isActive);

            if (listing == null || listing.sellerClientId != clientId)
            {
                Debug.LogWarning($"[AuctionHouse] Cancel failed for listing {listingId}");
                return;
            }

            if (listing.highestBidderId != ulong.MaxValue)
            {
                Debug.LogWarning($"[AuctionHouse] Cannot cancel listing {listingId} with active bids");
                return;
            }

            listing.isActive = false;
            DecrementListingCount(clientId);

            // Return item to seller via mail
            if (MailSystem.Instance != null)
            {
                var attachment = new MailAttachment { gold = 0, itemId = listing.itemId, quantity = 1 };
                MailSystem.Instance.SendSystemMail(clientId, "경매 취소",
                    $"{listing.itemName} 경매가 취소되었습니다.", MailType.SystemReward, attachment, true);
            }

            activeListings.Remove(listing);
            BroadcastListings();

            Debug.Log($"[AuctionHouse] Listing {listingId} cancelled by client {clientId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestListingsServerRpc(ServerRpcParams rpcParams = default)
        {
            BroadcastListings();
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        private void NotifyListingCreatedClientRpc(string listingId, string itemName, long startPrice, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager.Instance?.ShowNotification(
                $"{itemName} 경매 등록 완료 (시작가: {startPrice}G)", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyBidPlacedClientRpc(string listingId, long bidAmount, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager.Instance?.ShowNotification(
                $"입찰 완료: {bidAmount}G", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyAuctionWonClientRpc(string listingId, string itemName, long finalPrice, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager.Instance?.ShowNotification(
                $"{itemName} 낙찰! ({finalPrice}G)", NotificationType.System);
            OnAuctionWon?.Invoke(listingId);
        }

        [ClientRpc]
        private void NotifyAuctionSoldClientRpc(string listingId, string itemName, long salePrice, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager.Instance?.ShowNotification(
                $"{itemName} 판매 완료! ({salePrice}G)", NotificationType.System);
        }

        [ClientRpc]
        private void SyncListingsClientRpc(string serializedListings)
        {
            localListings = JsonUtility.FromJson<ListingWrapper>(serializedListings).listings;
            OnListingsUpdated?.Invoke();
        }

        #endregion

        #region Server Logic

        private void ProcessExpiredListings()
        {
            float currentTime = Time.time;
            var expiredListings = activeListings
                .Where(l => l.isActive && currentTime >= l.endTime)
                .ToList();

            foreach (var listing in expiredListings)
            {
                if (listing.highestBidderId != ulong.MaxValue && listing.highestBid > 0)
                {
                    CompleteAuction(listing, listing.highestBidderId, listing.highestBid);
                }
                else
                {
                    ExpireListingNoSale(listing);
                }
            }

            if (expiredListings.Count > 0)
            {
                BroadcastListings();
            }
        }

        private void CompleteAuction(AuctionListing listing, ulong buyerClientId, long salePrice)
        {
            listing.isActive = false;
            DecrementListingCount(listing.sellerClientId);

            // Calculate seller proceeds after commission
            long commission = (long)(salePrice * COMMISSION_RATE);
            if (commission < 1) commission = 1;
            long sellerProceeds = salePrice - commission;

            // Send gold to seller via mail
            if (MailSystem.Instance != null)
            {
                var sellerAttachment = new MailAttachment { gold = (int)sellerProceeds, itemId = string.Empty, quantity = 0 };
                MailSystem.Instance.SendSystemMail(listing.sellerClientId, "경매 판매 완료",
                    $"{listing.itemName} 판매 대금 {sellerProceeds}G (수수료 {commission}G)",
                    MailType.SystemReward, sellerAttachment, true);

                // Send item to buyer via mail
                var buyerAttachment = new MailAttachment { gold = 0, itemId = listing.itemId, quantity = 1 };
                MailSystem.Instance.SendSystemMail(buyerClientId, "경매 낙찰",
                    $"{listing.itemName} 낙찰 ({salePrice}G)", MailType.SystemReward, buyerAttachment, true);
            }

            // Notify both parties
            NotifyAuctionWonClientRpc(listing.listingId, listing.itemName, salePrice, buyerClientId);
            NotifyAuctionSoldClientRpc(listing.listingId, listing.itemName, salePrice, listing.sellerClientId);

            activeListings.Remove(listing);

            Debug.Log($"[AuctionHouse] Auction {listing.listingId} completed: {listing.itemName} sold for {salePrice}G");
        }

        private void ExpireListingNoSale(AuctionListing listing)
        {
            listing.isActive = false;
            DecrementListingCount(listing.sellerClientId);

            // Return item to seller via mail
            if (MailSystem.Instance != null)
            {
                var attachment = new MailAttachment { gold = 0, itemId = listing.itemId, quantity = 1 };
                MailSystem.Instance.SendSystemMail(listing.sellerClientId, "경매 만료",
                    $"{listing.itemName} 경매가 유찰되었습니다.", MailType.SystemReward, attachment, true);
            }

            activeListings.Remove(listing);

            Debug.Log($"[AuctionHouse] Listing {listing.listingId} expired with no bids");
        }

        private void RefundBidder(ulong bidderId, long amount, string itemName)
        {
            var bidderStats = GetPlayerStatsData(bidderId);
            if (bidderStats != null)
            {
                bidderStats.ChangeGold(amount);
            }
            else if (MailSystem.Instance != null)
            {
                // Player offline - send refund via mail
                var attachment = new MailAttachment { gold = (int)amount, itemId = string.Empty, quantity = 0 };
                MailSystem.Instance.SendSystemMail(bidderId, "입찰 환불",
                    $"{itemName} 입찰금 {amount}G가 환불되었습니다.", MailType.SystemReward, attachment, true);
            }
        }

        private void BroadcastListings()
        {
            float currentTime = Time.time;
            var infoList = activeListings
                .Where(l => l.isActive)
                .Select(l => new AuctionListingInfo
                {
                    listingId = l.listingId,
                    itemName = l.itemName,
                    itemGrade = l.itemGrade,
                    startPrice = l.startPrice,
                    buyoutPrice = l.buyoutPrice,
                    remainingTime = l.endTime - currentTime,
                    highestBid = l.highestBid,
                    hasBids = l.highestBidderId != ulong.MaxValue
                }).ToList();

            var wrapper = new ListingWrapper { listings = infoList };
            string serialized = JsonUtility.ToJson(wrapper);
            SyncListingsClientRpc(serialized);
        }

        #endregion

        #region Helpers

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;

            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        private void DecrementListingCount(ulong clientId)
        {
            if (playerListingCounts.ContainsKey(clientId))
            {
                playerListingCounts[clientId] = Mathf.Max(0, playerListingCounts[clientId] - 1);
                if (playerListingCounts[clientId] == 0)
                    playerListingCounts.Remove(clientId);
            }
        }

        [Serializable]
        private class ListingWrapper
        {
            public List<AuctionListingInfo> listings;
        }

        #endregion
    }
}
