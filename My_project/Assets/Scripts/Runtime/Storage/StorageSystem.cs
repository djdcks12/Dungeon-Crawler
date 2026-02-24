using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 창고 시스템 - 서버 권위적
    /// 개인 창고, 길드 창고, 확장 가능
    /// </summary>
    public class StorageSystem : NetworkBehaviour
    {
        public static StorageSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int defaultStorageSlots = 30;
        [SerializeField] private int maxStorageSlots = 100;
        [SerializeField] private int expandAmount = 10;
        [SerializeField] private int expandBaseCost = 5000; // 첫 확장 비용
        [SerializeField] private float expandCostMultiplier = 1.5f; // 확장당 비용 증가 배율

        // 서버: 플레이어별 창고
        private Dictionary<ulong, List<ItemInstance>> playerStorages = new Dictionary<ulong, List<ItemInstance>>();
        private Dictionary<ulong, int> playerStorageSlots = new Dictionary<ulong, int>(); // 현재 최대 슬롯

        // 서버: 길드 창고
        private Dictionary<int, List<ItemInstance>> guildStorages = new Dictionary<int, List<ItemInstance>>();

        // 로컬
        private List<ItemInstance> localStorage = new List<ItemInstance>();
        private int localMaxSlots;

        // 이벤트
        public System.Action OnStorageUpdated;
        public System.Action<int> OnStorageExpanded; // 새 최대 슬롯 수

        // 접근자
        public IReadOnlyList<ItemInstance> LocalStorage => localStorage;
        public int LocalMaxSlots => localMaxSlots;
        public int LocalUsedSlots => localStorage.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        #region 개인 창고

        /// <summary>
        /// 창고 열기 (동기화 요청)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void OpenStorageServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var storage = GetOrCreateStorage(clientId);
            int maxSlots = GetMaxSlots(clientId);

            // 저장된 아이템 전송
            ClearStorageClientRpc(maxSlots, clientId);
            for (int i = 0; i < storage.Count; i++)
            {
                SyncStorageItemClientRpc(i, storage[i], clientId);
            }
            StorageSyncCompleteClientRpc(storage.Count, clientId);
        }

        /// <summary>
        /// 인벤토리 → 창고로 아이템 이동
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void DepositItemServerRpc(int inventorySlot, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var storage = GetOrCreateStorage(clientId);
            int maxSlots = GetMaxSlots(clientId);

            if (storage.Count >= maxSlots)
            {
                SendMessageClientRpc("창고가 가득 찼습니다.", clientId);
                return;
            }

            var inventory = GetInventoryManager(clientId);
            if (inventory == null) return;

            var item = inventory.GetItemAtSlot(inventorySlot);
            if (item == null)
            {
                SendMessageClientRpc("해당 슬롯에 아이템이 없습니다.", clientId);
                return;
            }

            // 인벤토리에서 제거
            inventory.RemoveItem(inventorySlot, item.Quantity);

            // 창고에 추가
            storage.Add(item);

            DepositSuccessClientRpc(inventorySlot, item, storage.Count - 1, clientId);
        }

        /// <summary>
        /// 창고 → 인벤토리로 아이템 이동
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void WithdrawItemServerRpc(int storageSlot, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var storage = GetOrCreateStorage(clientId);

            if (storageSlot < 0 || storageSlot >= storage.Count)
            {
                SendMessageClientRpc("잘못된 창고 슬롯입니다.", clientId);
                return;
            }

            var inventory = GetInventoryManager(clientId);
            if (inventory == null) return;

            var item = storage[storageSlot];

            // 인벤토리에 추가 시도
            if (!inventory.AddItem(item))
            {
                SendMessageClientRpc("인벤토리가 가득 찼습니다.", clientId);
                return;
            }

            // 창고에서 제거
            storage.RemoveAt(storageSlot);

            WithdrawSuccessClientRpc(storageSlot, clientId);
        }

        /// <summary>
        /// 창고 확장 (골드 소모)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ExpandStorageServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            int currentMaxSlots = GetMaxSlots(clientId);

            if (currentMaxSlots >= maxStorageSlots)
            {
                SendMessageClientRpc("창고가 이미 최대 크기입니다.", clientId);
                return;
            }

            // 확장 비용 계산
            int timesExpanded = (currentMaxSlots - defaultStorageSlots) / expandAmount;
            int cost = Mathf.RoundToInt(expandBaseCost * Mathf.Pow(expandCostMultiplier, timesExpanded));

            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            if (statsData.Gold < cost)
            {
                SendMessageClientRpc($"확장 비용 {cost}G가 부족합니다.", clientId);
                return;
            }

            statsData.ChangeGold(-cost);
            int newMaxSlots = Mathf.Min(currentMaxSlots + expandAmount, maxStorageSlots);
            playerStorageSlots[clientId] = newMaxSlots;

            StorageExpandedClientRpc(newMaxSlots, cost, clientId);
        }

        #endregion

        #region ClientRPCs

        [ClientRpc]
        private void ClearStorageClientRpc(int maxSlots, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localStorage.Clear();
            localMaxSlots = maxSlots;
        }

        [ClientRpc]
        private void SyncStorageItemClientRpc(int slot, ItemInstance item, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            while (localStorage.Count <= slot) localStorage.Add(null);
            localStorage[slot] = item;
        }

        [ClientRpc]
        private void StorageSyncCompleteClientRpc(int totalItems, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            OnStorageUpdated?.Invoke();
        }

        [ClientRpc]
        private void DepositSuccessClientRpc(int inventorySlot, ItemInstance item, int storageSlot, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            while (localStorage.Count <= storageSlot) localStorage.Add(null);
            localStorage[storageSlot] = item;
            OnStorageUpdated?.Invoke();

            var notif = NotificationManager.Instance;
            if (notif != null)
            {
                string name = item.ItemData != null ? item.ItemData.ItemName : item.ItemId;
                notif.ShowNotification($"창고에 보관: {name}", NotificationType.System);
            }
        }

        [ClientRpc]
        private void WithdrawSuccessClientRpc(int storageSlot, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            if (storageSlot >= 0 && storageSlot < localStorage.Count)
                localStorage.RemoveAt(storageSlot);
            OnStorageUpdated?.Invoke();

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification("아이템을 인벤토리로 꺼냈습니다.", NotificationType.System);
        }

        [ClientRpc]
        private void StorageExpandedClientRpc(int newMaxSlots, int cost, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localMaxSlots = newMaxSlots;
            OnStorageExpanded?.Invoke(newMaxSlots);

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"창고 확장! ({newMaxSlots}칸) -{cost}G", NotificationType.System);
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

        private List<ItemInstance> GetOrCreateStorage(ulong clientId)
        {
            if (!playerStorages.ContainsKey(clientId))
                playerStorages[clientId] = new List<ItemInstance>();
            return playerStorages[clientId];
        }

        private int GetMaxSlots(ulong clientId)
        {
            if (!playerStorageSlots.ContainsKey(clientId))
                playerStorageSlots[clientId] = defaultStorageSlots;
            return playerStorageSlots[clientId];
        }

        private InventoryManager GetInventoryManager(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject?.GetComponent<InventoryManager>();
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
