using UnityEngine;
using Unity.Netcode;

using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 트랜스모그(외형 변경) 시스템 - 서버 권위적
    /// 장비 외형 수집 도감, 외형 적용/해제
    /// </summary>
    public class TransmogSystem : NetworkBehaviour
    {
        public static TransmogSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int transmogCost = 500; // 외형 변경 비용
        [SerializeField] private int removeCost = 100; // 외형 해제 비용

        // 서버: 플레이어별 수집된 외형 ID
        private Dictionary<ulong, HashSet<string>> playerCollections = new Dictionary<ulong, HashSet<string>>();

        // 서버: 플레이어별 현재 적용된 외형 (슬롯→아이템ID)
        private Dictionary<ulong, Dictionary<int, string>> playerTransmogs = new Dictionary<ulong, Dictionary<int, string>>();

        // 로컬: 내 수집 도감
        private HashSet<string> localCollection = new HashSet<string>();

        // 로컬: 내 적용된 외형
        private Dictionary<int, string> localTransmogs = new Dictionary<int, string>();

        // 이벤트
        public System.Action OnCollectionUpdated;
        public System.Action OnTransmogChanged;

        // 접근자
        public IReadOnlyCollection<string> LocalCollection => localCollection;
        public IReadOnlyDictionary<int, string> LocalTransmogs => localTransmogs;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// 아이템 획득/장착 시 외형 도감에 등록 (서버에서 호출)
        /// </summary>
        public void RegisterAppearance(ulong clientId, string itemId)
        {
            if (!IsServer) return;
            if (string.IsNullOrEmpty(itemId)) return;

            var collection = GetOrCreateCollection(clientId);
            if (collection.Add(itemId))
            {
                AppearanceRegisteredClientRpc(itemId, clientId);
            }
        }

        /// <summary>
        /// 외형 적용 요청
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ApplyTransmogServerRpc(int equipSlot, string appearanceItemId,
            ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            string itemId = appearanceItemId;

            var collection = GetOrCreateCollection(clientId);
            if (!collection.Contains(itemId))
            {
                SendMessageClientRpc("수집하지 않은 외형입니다.", clientId);
                return;
            }

            // 골드 차감
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            if (statsData.Gold < transmogCost)
            {
                SendMessageClientRpc($"외형 변경 비용 {transmogCost}G가 부족합니다.", clientId);
                return;
            }

            statsData.ChangeGold(-transmogCost);

            var transmogs = GetOrCreateTransmogs(clientId);
            transmogs[equipSlot] = itemId;

            TransmogAppliedClientRpc(equipSlot, appearanceItemId, transmogCost, clientId);
        }

        /// <summary>
        /// 외형 해제 요청
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RemoveTransmogServerRpc(int equipSlot, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            var transmogs = GetOrCreateTransmogs(clientId);
            if (!transmogs.ContainsKey(equipSlot))
            {
                SendMessageClientRpc("해당 슬롯에 적용된 외형이 없습니다.", clientId);
                return;
            }

            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            if (statsData.Gold < removeCost)
            {
                SendMessageClientRpc($"외형 해제 비용 {removeCost}G가 부족합니다.", clientId);
                return;
            }

            statsData.ChangeGold(-removeCost);
            transmogs.Remove(equipSlot);

            TransmogRemovedClientRpc(equipSlot, removeCost, clientId);
        }

        /// <summary>
        /// 도감 동기화 요청
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestSyncServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var collection = GetOrCreateCollection(clientId);
            var transmogs = GetOrCreateTransmogs(clientId);

            // 컬렉션 동기화
            ClearLocalDataClientRpc(clientId);
            foreach (var itemId in collection)
            {
                SyncCollectionItemClientRpc(itemId, clientId);
            }

            // 트랜스모그 동기화
            foreach (var kvp in transmogs)
            {
                SyncTransmogClientRpc(kvp.Key, kvp.Value, clientId);
            }

            SyncCompleteClientRpc(collection.Count, transmogs.Count, clientId);
        }

        /// <summary>
        /// 특정 슬롯의 표시용 아이템 ID 반환 (트랜스모그 적용됨)
        /// </summary>
        public string GetDisplayItemId(int equipSlot, string originalItemId)
        {
            if (localTransmogs.TryGetValue(equipSlot, out string transmogId))
                return transmogId;
            return originalItemId;
        }

        /// <summary>
        /// 수집 여부 확인
        /// </summary>
        public bool HasCollected(string itemId)
        {
            return localCollection.Contains(itemId);
        }

        /// <summary>
        /// 수집 개수
        /// </summary>
        public int CollectionCount => localCollection.Count;

        #region ClientRPCs

        [ClientRpc]
        private void AppearanceRegisteredClientRpc(string itemId, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localCollection.Add(itemId);
            OnCollectionUpdated?.Invoke();

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification("새 외형이 도감에 등록되었습니다!", NotificationType.System);
        }

        [ClientRpc]
        private void TransmogAppliedClientRpc(int equipSlot, string itemId, int cost, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localTransmogs[equipSlot] = itemId;
            OnTransmogChanged?.Invoke();

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"외형 변경 완료! -{cost}G", NotificationType.System);
        }

        [ClientRpc]
        private void TransmogRemovedClientRpc(int equipSlot, int cost, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localTransmogs.Remove(equipSlot);
            OnTransmogChanged?.Invoke();

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"외형 해제 완료! -{cost}G", NotificationType.System);
        }

        [ClientRpc]
        private void ClearLocalDataClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localCollection.Clear();
            localTransmogs.Clear();
        }

        [ClientRpc]
        private void SyncCollectionItemClientRpc(string itemId, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localCollection.Add(itemId);
        }

        [ClientRpc]
        private void SyncTransmogClientRpc(int equipSlot, string itemId, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localTransmogs[equipSlot] = itemId;
        }

        [ClientRpc]
        private void SyncCompleteClientRpc(int collectionCount, int transmogCount, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            OnCollectionUpdated?.Invoke();
            OnTransmogChanged?.Invoke();
        }

        [ClientRpc]
        private void SendMessageClientRpc(string message, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification(message, NotificationType.Warning);
        }

        #endregion

        #region Utility

        private HashSet<string> GetOrCreateCollection(ulong clientId)
        {
            if (!playerCollections.ContainsKey(clientId))
                playerCollections[clientId] = new HashSet<string>();
            return playerCollections[clientId];
        }

        private Dictionary<int, string> GetOrCreateTransmogs(ulong clientId)
        {
            if (!playerTransmogs.ContainsKey(clientId))
                playerTransmogs[clientId] = new Dictionary<int, string>();
            return playerTransmogs[clientId];
        }

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        #endregion

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }
}
