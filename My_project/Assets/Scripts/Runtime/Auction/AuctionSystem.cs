using UnityEngine;
using Unity.Netcode;

using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    #region Auction Types

    public enum AuctionStatus
    {
        Active,
        Sold,
        Expired,
        Cancelled
    }

    [System.Serializable]
    public struct AuctionListing : INetworkSerializable, System.IEquatable<AuctionListing>
    {
        public int listingId;
        public string itemId;
        public string itemName;
        public int quantity;
        public int enhanceLevel;
        public long startPrice;
        public long buyoutPrice;
        public long currentBid;
        public ulong sellerClientId;
        public string sellerName;
        public ulong highestBidder;
        public float expirationTime;
        public AuctionStatus status;
        public int itemGrade;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref listingId);
            serializer.SerializeValue(ref itemId);
            serializer.SerializeValue(ref itemName);
            serializer.SerializeValue(ref quantity);
            serializer.SerializeValue(ref enhanceLevel);
            serializer.SerializeValue(ref startPrice);
            serializer.SerializeValue(ref buyoutPrice);
            serializer.SerializeValue(ref currentBid);
            serializer.SerializeValue(ref sellerClientId);
            serializer.SerializeValue(ref sellerName);
            serializer.SerializeValue(ref highestBidder);
            serializer.SerializeValue(ref expirationTime);
            serializer.SerializeValue(ref status);
            serializer.SerializeValue(ref itemGrade);
        }

        public bool Equals(AuctionListing other) => listingId == other.listingId;
    }

    #endregion

    /// <summary>
    /// 경매장 시스템 - 서버 권위적
    /// 아이템 등록/입찰/즉시구매, 수수료, 검색/필터, 만료 처리
    /// </summary>
    public class AuctionSystem : NetworkBehaviour
    {
        public static AuctionSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float listingDuration = 3600f;     // 경매 시간 (1시간)
        [SerializeField] private float commissionRate = 0.05f;      // 수수료 5%
        [SerializeField] private int maxListingsPerPlayer = 10;     // 플레이어당 최대 등록 수
        [SerializeField] private long minStartPrice = 10;           // 최소 시작가
        [SerializeField] private float bidIncrementRate = 0.1f;     // 최소 입찰 증가율 10%

        // 서버 데이터
        private Dictionary<int, AuctionListing> listings = new Dictionary<int, AuctionListing>();
        private Dictionary<ulong, List<int>> playerListings = new Dictionary<ulong, List<int>>();
        private int nextListingId = 1;

        // 로컬 데이터
        private List<AuctionListing> localListings = new List<AuctionListing>();

        // 이벤트
        public System.Action<List<AuctionListing>> OnListingsUpdated;
        public System.Action<AuctionListing> OnItemSold;
        public System.Action<AuctionListing> OnOutbid;
        public System.Action<string> OnAuctionMessage;

        public IReadOnlyList<AuctionListing> LocalListings => localListings;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (!IsServer) return;
            ProcessExpirations();
        }

        private float lastExpirationCheck;
        private void ProcessExpirations()
        {
            if (Time.time - lastExpirationCheck < 10f) return;
            lastExpirationCheck = Time.time;

            var expired = new List<int>();
            foreach (var kvp in listings)
            {
                if (kvp.Value.status == AuctionStatus.Active && Time.time >= kvp.Value.expirationTime)
                    expired.Add(kvp.Key);
            }

            foreach (var id in expired)
            {
                var listing = listings[id];

                if (listing.currentBid > 0 && listing.highestBidder != 0)
                {
                    // 낙찰
                    CompleteSale(id);
                }
                else
                {
                    // 유찰 - 아이템 반환
                    listing.status = AuctionStatus.Expired;
                    listings[id] = listing;
                    ReturnItemToSeller(listing);

                    if (playerListings.TryGetValue(listing.sellerClientId, out var pList))
                        pList.Remove(id);

                    SendMessageClientRpc("경매가 유찰되었습니다. 아이템이 반환됩니다.", listing.sellerClientId);
                }
            }
        }

        #region 등록

        [ServerRpc(RequireOwnership = false)]
        public void CreateListingServerRpc(int inventorySlot, int quantity, long startPrice, long buyoutPrice,
            ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            // 등록 수 제한
            if (playerListings.TryGetValue(clientId, out var pList) && pList.Count >= maxListingsPerPlayer)
            {
                SendMessageClientRpc($"최대 {maxListingsPerPlayer}개까지 등록 가능합니다.", clientId);
                return;
            }

            if (startPrice < minStartPrice)
            {
                SendMessageClientRpc($"최소 시작가는 {minStartPrice}G입니다.", clientId);
                return;
            }

            if (buyoutPrice > 0 && buyoutPrice < startPrice)
            {
                SendMessageClientRpc("즉시구매가는 시작가 이상이어야 합니다.", clientId);
                return;
            }

            var inventoryMgr = GetInventoryManager(clientId);
            if (inventoryMgr == null) return;

            var item = inventoryMgr.GetItemAtSlot(inventorySlot);
            if (item == null || item.ItemData == null)
            {
                SendMessageClientRpc("유효하지 않은 아이템입니다.", clientId);
                return;
            }

            if (item.Quantity < quantity || quantity <= 0)
            {
                SendMessageClientRpc("수량이 부족합니다.", clientId);
                return;
            }

            // 아이템 제거
            if (item.Quantity == quantity)
            {
                inventoryMgr.Inventory.RemoveAllItems(item.ItemId, quantity);
            }
            else
            {
                item.ChangeQuantity(item.Quantity - quantity);
            }

            // 등록 생성
            var statsData = GetPlayerStatsData(clientId);
            string sellerName = statsData != null ? (statsData.CharacterName ?? "Unknown") : "Unknown";

            var listing = new AuctionListing
            {
                listingId = nextListingId++,
                itemId = item.ItemId,
                itemName = item.ItemData.ItemName,
                quantity = quantity,
                enhanceLevel = item.EnhanceLevel,
                startPrice = startPrice,
                buyoutPrice = buyoutPrice,
                currentBid = 0,
                sellerClientId = clientId,
                sellerName = sellerName,
                highestBidder = 0,
                expirationTime = Time.time + listingDuration,
                status = AuctionStatus.Active,
                itemGrade = (int)(item.ItemData.Grade)
            };

            listings[listing.listingId] = listing;
            if (!playerListings.ContainsKey(clientId))
                playerListings[clientId] = new List<int>();
            playerListings[clientId].Add(listing.listingId);

            SendMessageClientRpc($"'{item.ItemData.ItemName}' 경매 등록 완료!", clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void CancelListingServerRpc(int listingId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!listings.TryGetValue(listingId, out var listing))
            {
                SendMessageClientRpc("존재하지 않는 경매입니다.", clientId);
                return;
            }

            if (listing.sellerClientId != clientId)
            {
                SendMessageClientRpc("본인의 경매만 취소할 수 있습니다.", clientId);
                return;
            }

            if (listing.currentBid > 0)
            {
                SendMessageClientRpc("입찰이 있는 경매는 취소할 수 없습니다.", clientId);
                return;
            }

            listing.status = AuctionStatus.Cancelled;
            listings[listingId] = listing;

            ReturnItemToSeller(listing);

            if (playerListings.TryGetValue(clientId, out var pList))
                pList.Remove(listingId);

            SendMessageClientRpc("경매가 취소되었습니다.", clientId);
        }

        #endregion

        #region 입찰 / 즉시구매

        [ServerRpc(RequireOwnership = false)]
        public void PlaceBidServerRpc(int listingId, long bidAmount, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!listings.TryGetValue(listingId, out var listing) || listing.status != AuctionStatus.Active)
            {
                SendMessageClientRpc("유효하지 않은 경매입니다.", clientId);
                return;
            }

            if (listing.sellerClientId == clientId)
            {
                SendMessageClientRpc("본인의 경매에 입찰할 수 없습니다.", clientId);
                return;
            }

            // 최소 입찰가 체크
            long minBid = listing.currentBid > 0
                ? (long)(listing.currentBid * (1 + bidIncrementRate))
                : listing.startPrice;

            if (bidAmount < minBid)
            {
                SendMessageClientRpc($"최소 입찰가는 {minBid}G입니다.", clientId);
                return;
            }

            // 골드 체크
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null || statsData.Gold < bidAmount)
            {
                SendMessageClientRpc("골드가 부족합니다.", clientId);
                return;
            }

            // 이전 최고 입찰자에게 환불
            if (listing.highestBidder != 0 && listing.currentBid > 0)
            {
                RefundBidder(listing.highestBidder, listing.currentBid);
                SendMessageClientRpc("더 높은 입찰이 있어 골드가 환불되었습니다.", listing.highestBidder);
            }

            // 골드 차감 & 입찰 등록
            statsData.ChangeGold(-bidAmount);
            listing.currentBid = bidAmount;
            listing.highestBidder = clientId;
            listings[listingId] = listing;

            SendMessageClientRpc($"입찰 완료: {bidAmount}G", clientId);

            // 즉시구매가 이상이면 즉시 거래
            if (listing.buyoutPrice > 0 && bidAmount >= listing.buyoutPrice)
            {
                CompleteSale(listingId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void BuyoutServerRpc(int listingId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!listings.TryGetValue(listingId, out var listing) || listing.status != AuctionStatus.Active)
            {
                SendMessageClientRpc("유효하지 않은 경매입니다.", clientId);
                return;
            }

            if (listing.buyoutPrice <= 0)
            {
                SendMessageClientRpc("즉시구매가 없는 경매입니다.", clientId);
                return;
            }

            if (listing.sellerClientId == clientId)
            {
                SendMessageClientRpc("본인의 경매를 구매할 수 없습니다.", clientId);
                return;
            }

            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null || statsData.Gold < listing.buyoutPrice)
            {
                SendMessageClientRpc("골드가 부족합니다.", clientId);
                return;
            }

            // 이전 입찰자 환불
            if (listing.highestBidder != 0 && listing.highestBidder != clientId && listing.currentBid > 0)
            {
                RefundBidder(listing.highestBidder, listing.currentBid);
            }

            // 즉시구매
            statsData.ChangeGold(-listing.buyoutPrice);
            listing.currentBid = listing.buyoutPrice;
            listing.highestBidder = clientId;
            listings[listingId] = listing;

            CompleteSale(listingId);
        }

        #endregion

        #region 검색

        [ServerRpc(RequireOwnership = false)]
        public void SearchListingsServerRpc(string searchTerm, int gradeFilter, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            var results = new List<AuctionListing>();
            foreach (var listing in listings.Values)
            {
                if (listing.status != AuctionStatus.Active) continue;

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    if (string.IsNullOrEmpty(listing.itemName)) continue;
                    string name = listing.itemName.ToLower();
                    if (!name.Contains(searchTerm.ToLower())) continue;
                }

                if (gradeFilter > 0 && listing.itemGrade != gradeFilter) continue;

                results.Add(listing);
            }

            // 가격순 정렬
            results.Sort((a, b) => a.startPrice.CompareTo(b.startPrice));

            // 최대 50개
            if (results.Count > 50)
                results = results.GetRange(0, 50);

            SyncListingsClientRpc(results.ToArray(), clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void GetMyListingsServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            var results = new List<AuctionListing>();
            if (playerListings.TryGetValue(clientId, out var pList))
            {
                foreach (var id in pList)
                {
                    if (listings.TryGetValue(id, out var listing))
                        results.Add(listing);
                }
            }

            SyncListingsClientRpc(results.ToArray(), clientId);
        }

        #endregion

        #region Internal

        private void CompleteSale(int listingId)
        {
            if (!listings.TryGetValue(listingId, out var listing)) return;

            listing.status = AuctionStatus.Sold;
            listings[listingId] = listing;

            // 판매자에게 골드 지급 (수수료 제외)
            long commission = (long)(listing.currentBid * commissionRate);
            long sellerReceives = listing.currentBid - commission;

            var sellerStats = GetPlayerStatsData(listing.sellerClientId);
            if (sellerStats != null)
                sellerStats.ChangeGold(sellerReceives);

            // 구매자에게 아이템 지급
            var buyerInventory = GetInventoryManager(listing.highestBidder);
            if (buyerInventory != null)
            {
                var itemData = ItemDatabase.GetItem(listing.itemId);
                if (itemData != null)
                {
                    var item = new ItemInstance(itemData, listing.quantity);
                    item.EnhanceLevel = listing.enhanceLevel;
                    buyerInventory.AddItem(item);
                }
            }

            if (playerListings.TryGetValue(listing.sellerClientId, out var pList))
                pList.Remove(listingId);

            SendMessageClientRpc($"아이템 판매 완료! {sellerReceives}G 획득 (수수료 {commission}G)", listing.sellerClientId);
            SendMessageClientRpc($"'{listing.itemName}' 구매 완료!", listing.highestBidder);
        }

        private void ReturnItemToSeller(AuctionListing listing)
        {
            var inventory = GetInventoryManager(listing.sellerClientId);
            if (inventory == null) return;

            var itemData = ItemDatabase.GetItem(listing.itemId);
            if (itemData != null)
            {
                var item = new ItemInstance(itemData, listing.quantity);
                item.EnhanceLevel = listing.enhanceLevel;
                inventory.AddItem(item);
            }
        }

        private void RefundBidder(ulong clientId, long amount)
        {
            var stats = GetPlayerStatsData(clientId);
            if (stats != null)
                stats.ChangeGold(amount);
        }

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (NetworkManager.Singleton == null) return null;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        private InventoryManager GetInventoryManager(ulong clientId)
        {
            if (NetworkManager.Singleton == null) return null;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject?.GetComponent<InventoryManager>();
        }

        #endregion

        #region ClientRPCs

        [ClientRpc]
        private void SyncListingsClientRpc(AuctionListing[] results, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localListings.Clear();
            localListings.AddRange(results);
            OnListingsUpdated?.Invoke(localListings);
        }

        [ClientRpc]
        private void SendMessageClientRpc(string message, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification(message, NotificationType.System);

            OnAuctionMessage?.Invoke(message);
        }

        #endregion

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }
}
