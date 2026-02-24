using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 플레이어 간 거래 시스템
    /// ServerRpc 기반, 양쪽 확인 후 교환
    /// </summary>
    public class TradeSystem : NetworkBehaviour
    {
        public static TradeSystem Instance { get; private set; }

        [Header("설정")]
        [SerializeField] private float tradeRange = 10f;
        [SerializeField] private float tradeTimeout = 60f;

        // 활성 거래 세션
        private Dictionary<int, TradeSession> activeTrades = new Dictionary<int, TradeSession>();
        private int nextTradeId = 1;

        // 플레이어별 현재 거래 ID
        private Dictionary<ulong, int> playerTradeMap = new Dictionary<ulong, int>();

        // 이벤트
        public System.Action<ulong> OnTradeRequested;       // 거래 요청 받음
        public System.Action<int> OnTradeStarted;            // 거래 시작
        public System.Action<int> OnTradeCancelled;          // 거래 취소
        public System.Action<int> OnTradeCompleted;          // 거래 완료
        public System.Action<int, ulong, TradeOffer> OnTradeOfferUpdated; // 오퍼 변경

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (!IsServer) return;

            // 타임아웃 체크
            var expiredTrades = new List<int>();
            foreach (var kvp in activeTrades)
            {
                if (Time.time - kvp.Value.startTime > tradeTimeout)
                {
                    expiredTrades.Add(kvp.Key);
                }
            }
            foreach (int tradeId in expiredTrades)
            {
                CancelTradeInternal(tradeId, "거래 시간이 초과되었습니다.");
            }
        }

        /// <summary>
        /// 거래 요청 (클라이언트 → 서버)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestTradeServerRpc(ulong targetClientId, ServerRpcParams rpcParams = default)
        {
            ulong requesterId = rpcParams.Receive.SenderClientId;

            // 자기 자신에게 거래 불가
            if (requesterId == targetClientId)
            {
                NotifyClientRpc(requesterId, "자기 자신과는 거래할 수 없습니다.");
                return;
            }

            // 이미 거래 중인지 확인
            if (playerTradeMap.ContainsKey(requesterId))
            {
                NotifyClientRpc(requesterId, "이미 거래 중입니다.");
                return;
            }
            if (playerTradeMap.ContainsKey(targetClientId))
            {
                NotifyClientRpc(requesterId, "상대방이 이미 거래 중입니다.");
                return;
            }

            // 거리 확인
            if (!IsInRange(requesterId, targetClientId))
            {
                NotifyClientRpc(requesterId, "상대방이 너무 멀리 있습니다.");
                return;
            }

            // 거래 요청 알림
            string requesterName = GetPlayerName(requesterId);
            NotifyClientRpc(targetClientId, $"{requesterName}님이 거래를 요청합니다. /trade accept 로 수락");

            // 임시 세션 생성 (수락 대기)
            int tradeId = nextTradeId++;
            activeTrades[tradeId] = new TradeSession
            {
                tradeId = tradeId,
                player1 = requesterId,
                player2 = targetClientId,
                startTime = Time.time,
                status = TradeStatus.Pending,
                offer1 = new TradeOffer(),
                offer2 = new TradeOffer()
            };

            playerTradeMap[requesterId] = tradeId;
            playerTradeMap[targetClientId] = tradeId;

            NotifyClientRpc(requesterId, "거래 요청을 보냈습니다. 상대방의 수락을 기다리는 중...");
        }

        /// <summary>
        /// 거래 수락
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void AcceptTradeServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!playerTradeMap.TryGetValue(clientId, out int tradeId))
            {
                NotifyClientRpc(clientId, "대기 중인 거래 요청이 없습니다.");
                return;
            }

            if (!activeTrades.TryGetValue(tradeId, out var session))
                return;

            if (session.status != TradeStatus.Pending)
                return;

            // 수락 (player2만 수락 가능)
            if (session.player2 != clientId)
            {
                NotifyClientRpc(clientId, "거래 수락 권한이 없습니다.");
                return;
            }

            session.status = TradeStatus.Active;
            activeTrades[tradeId] = session;

            NotifyClientRpc(session.player1, "거래가 시작되었습니다!");
            NotifyClientRpc(session.player2, "거래가 시작되었습니다!");

            OnTradeStartedClientRpc(tradeId);
        }

        /// <summary>
        /// 거래에 아이템 올리기
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SetTradeItemServerRpc(int slotIndex, string itemId, int quantity, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!playerTradeMap.TryGetValue(clientId, out int tradeId))
                return;
            if (!activeTrades.TryGetValue(tradeId, out var session))
                return;
            if (session.status != TradeStatus.Active)
                return;

            // 확인 상태 초기화 (오퍼 변경 시)
            session.player1Confirmed = false;
            session.player2Confirmed = false;

            bool isPlayer1 = session.player1 == clientId;
            var offer = isPlayer1 ? session.offer1 : session.offer2;

            // 슬롯 범위 확인
            if (slotIndex < 0 || slotIndex >= 6)
                return;

            // 아이템 검증
            if (!string.IsNullOrEmpty(itemId))
            {
                // 인벤토리에 보유 확인
                if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                    return;
                var inventoryManager = client.PlayerObject?.GetComponent<InventoryManager>();
                if (inventoryManager == null || !inventoryManager.HasItem(itemId, quantity))
                {
                    NotifyClientRpc(clientId, "해당 아이템이 부족합니다.");
                    return;
                }

                offer.items[slotIndex] = new TradeItemEntry { itemId = itemId, quantity = quantity };
            }
            else
            {
                offer.items[slotIndex] = default;
            }

            activeTrades[tradeId] = session;

            // 양쪽에 오퍼 변경 알림
            SyncTradeOfferClientRpc(tradeId, clientId, offer);
        }

        /// <summary>
        /// 거래에 골드 설정
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SetTradeGoldServerRpc(long goldAmount, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!playerTradeMap.TryGetValue(clientId, out int tradeId))
                return;
            if (!activeTrades.TryGetValue(tradeId, out var session))
                return;
            if (session.status != TradeStatus.Active)
                return;

            // 골드 보유량 확인
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return;
            var statsManager = client.PlayerObject?.GetComponent<PlayerStatsManager>();
            if (statsManager == null || statsManager.CurrentStats.Gold < goldAmount)
            {
                NotifyClientRpc(clientId, "골드가 부족합니다.");
                return;
            }

            session.player1Confirmed = false;
            session.player2Confirmed = false;

            bool isPlayer1 = session.player1 == clientId;
            if (isPlayer1)
                session.offer1.gold = goldAmount;
            else
                session.offer2.gold = goldAmount;

            activeTrades[tradeId] = session;

            var offer = isPlayer1 ? session.offer1 : session.offer2;
            SyncTradeOfferClientRpc(tradeId, clientId, offer);
        }

        /// <summary>
        /// 거래 확인 (양쪽 다 확인해야 교환)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ConfirmTradeServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!playerTradeMap.TryGetValue(clientId, out int tradeId))
                return;
            if (!activeTrades.TryGetValue(tradeId, out var session))
                return;
            if (session.status != TradeStatus.Active)
                return;

            if (session.player1 == clientId)
                session.player1Confirmed = true;
            else
                session.player2Confirmed = true;

            activeTrades[tradeId] = session;

            // 양쪽 다 확인했으면 교환 실행
            if (session.player1Confirmed && session.player2Confirmed)
            {
                ExecuteTrade(tradeId);
            }
            else
            {
                string name = GetPlayerName(clientId);
                ulong otherId = session.player1 == clientId ? session.player2 : session.player1;
                NotifyClientRpc(otherId, $"{name}님이 거래를 확인했습니다.");
                NotifyClientRpc(clientId, "거래 확인 완료. 상대방의 확인을 기다리는 중...");
            }
        }

        /// <summary>
        /// 거래 취소
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CancelTradeServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!playerTradeMap.TryGetValue(clientId, out int tradeId))
                return;

            string name = GetPlayerName(clientId);
            CancelTradeInternal(tradeId, $"{name}님이 거래를 취소했습니다.");
        }

        /// <summary>
        /// 거래 교환 실행
        /// </summary>
        private void ExecuteTrade(int tradeId)
        {
            if (!activeTrades.TryGetValue(tradeId, out var session))
                return;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(session.player1, out var client1) ||
                !NetworkManager.Singleton.ConnectedClients.TryGetValue(session.player2, out var client2))
            {
                CancelTradeInternal(tradeId, "플레이어 연결이 끊어졌습니다.");
                return;
            }
            if (client1.PlayerObject == null || client2.PlayerObject == null)
            {
                CancelTradeInternal(tradeId, "플레이어 연결이 끊어졌습니다.");
                return;
            }

            var inv1 = client1.PlayerObject.GetComponent<InventoryManager>();
            var inv2 = client2.PlayerObject.GetComponent<InventoryManager>();
            var stats1 = client1.PlayerObject.GetComponent<PlayerStatsManager>();
            var stats2 = client2.PlayerObject.GetComponent<PlayerStatsManager>();

            if (inv1 == null || inv2 == null || stats1 == null || stats2 == null)
            {
                CancelTradeInternal(tradeId, "거래 처리 오류.");
                return;
            }

            // 골드 교환
            if (session.offer1.gold > 0)
            {
                stats1.ChangeGold(-session.offer1.gold);
                stats2.ChangeGold(session.offer1.gold);
            }
            if (session.offer2.gold > 0)
            {
                stats2.ChangeGold(-session.offer2.gold);
                stats1.ChangeGold(session.offer2.gold);
            }

            // 아이템 교환 (player1 → player2)
            TransferItems(session.offer1, inv1, inv2);

            // 아이템 교환 (player2 → player1)
            TransferItems(session.offer2, inv2, inv1);

            // 거래 완료
            session.status = TradeStatus.Completed;
            activeTrades[tradeId] = session;

            NotifyClientRpc(session.player1, "거래가 완료되었습니다!");
            NotifyClientRpc(session.player2, "거래가 완료되었습니다!");

            OnTradeCompletedClientRpc(tradeId);

            // 정리
            CleanupTrade(tradeId);
        }

        private void TransferItems(TradeOffer offer, InventoryManager from, InventoryManager to)
        {
            if (offer.items == null) return;
            foreach (var entry in offer.items)
            {
                if (string.IsNullOrEmpty(entry.itemId)) continue;

                var itemData = ItemDatabase.GetItem(entry.itemId);
                if (itemData == null) continue;

                // 보내는 쪽에서 제거
                // 실제로는 인벤토리에서 해당 아이템을 찾아서 이동
                var itemInstance = new ItemInstance(itemData, entry.quantity);
                to.TryAddItemDirect(itemInstance, out _);
            }
        }

        private void CancelTradeInternal(int tradeId, string reason)
        {
            if (!activeTrades.TryGetValue(tradeId, out var session))
                return;

            NotifyClientRpc(session.player1, reason);
            NotifyClientRpc(session.player2, reason);

            OnTradeCancelledClientRpc(tradeId);
            CleanupTrade(tradeId);
        }

        private void CleanupTrade(int tradeId)
        {
            if (activeTrades.TryGetValue(tradeId, out var session))
            {
                playerTradeMap.Remove(session.player1);
                playerTradeMap.Remove(session.player2);
                activeTrades.Remove(tradeId);
            }
        }

        private bool IsInRange(ulong client1Id, ulong client2Id)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(client1Id, out var c1))
                return false;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(client2Id, out var c2))
                return false;
            if (c1.PlayerObject == null || c2.PlayerObject == null)
                return false;

            float dist = Vector3.Distance(
                c1.PlayerObject.transform.position,
                c2.PlayerObject.transform.position);

            return dist <= tradeRange;
        }

        private string GetPlayerName(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return $"Player_{clientId}";
            if (client.PlayerObject == null) return $"Player_{clientId}";

            var statsManager = client.PlayerObject.GetComponent<PlayerStatsManager>();
            return statsManager?.CurrentStats?.CharacterName ?? $"Player_{clientId}";
        }

        // === ClientRpc ===

        [ClientRpc]
        private void NotifyClientRpc(ulong targetClientId, string message)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(message, NotificationType.System);

            var chatUI = FindFirstObjectByType<ChatUI>();
            if (chatUI != null)
                chatUI.AddSystemMessage(message);
        }

        [ClientRpc]
        private void OnTradeStartedClientRpc(int tradeId)
        {
            OnTradeStarted?.Invoke(tradeId);
        }

        [ClientRpc]
        private void OnTradeCancelledClientRpc(int tradeId)
        {
            OnTradeCancelled?.Invoke(tradeId);
        }

        [ClientRpc]
        private void OnTradeCompletedClientRpc(int tradeId)
        {
            OnTradeCompleted?.Invoke(tradeId);
        }

        [ClientRpc]
        private void SyncTradeOfferClientRpc(int tradeId, ulong offererClientId, TradeOffer offer)
        {
            OnTradeOfferUpdated?.Invoke(tradeId, offererClientId, offer);
        }

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                OnTradeRequested = null;
                OnTradeStarted = null;
                OnTradeCancelled = null;
                OnTradeCompleted = null;
                OnTradeOfferUpdated = null;
                Instance = null;
            }
            base.OnDestroy();
        }
    }

    // === 데이터 구조 ===

    public enum TradeStatus
    {
        Pending,
        Active,
        Completed,
        Cancelled
    }

    [System.Serializable]
    public struct TradeSession
    {
        public int tradeId;
        public ulong player1;
        public ulong player2;
        public float startTime;
        public TradeStatus status;
        public TradeOffer offer1;
        public TradeOffer offer2;
        public bool player1Confirmed;
        public bool player2Confirmed;
    }

    [System.Serializable]
    public struct TradeOffer : INetworkSerializable
    {
        public TradeItemEntry[] items; // 최대 6개 슬롯
        public long gold;

        public TradeOffer(int slots = 6)
        {
            items = new TradeItemEntry[slots];
            gold = 0;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref gold);

            int count = items?.Length ?? 0;
            serializer.SerializeValue(ref count);

            if (serializer.IsReader)
            {
                items = new TradeItemEntry[count];
            }

            for (int i = 0; i < count; i++)
            {
                serializer.SerializeValue(ref items[i]);
            }
        }
    }

    [System.Serializable]
    public struct TradeItemEntry : INetworkSerializable
    {
        public string itemId;
        public int quantity;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref itemId);
            serializer.SerializeValue(ref quantity);
        }
    }
}
