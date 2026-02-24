using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 플레이어 인벤토리 관리 시스템
    /// 네트워크 동기화 및 인벤토리 로직 처리
    /// </summary>
    public class InventoryManager : NetworkBehaviour
    {
        [Header("인벤토리 설정")]
        [SerializeField] private int inventorySize = 30;
        
        // 인벤토리 데이터
        private InventoryData inventory;
        
        // 네트워크 동기화
        private NetworkVariable<InventoryData> networkInventory = new NetworkVariable<InventoryData>(
            default, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);
        
        // 컴포넌트 참조
        private PlayerController playerController;
        private PlayerStatsManager statsManager;
        
        // 이벤트
        public System.Action<ItemInstance, int> OnItemAdded;
        public System.Action<ItemInstance, int> OnItemRemoved;
        public System.Action OnInventoryUpdated;
        
        // 프로퍼티
        public InventoryData Inventory => inventory;
        public bool IsFull => inventory?.IsFull ?? true;
        public int UsedSlots => inventory?.UsedSlots ?? 0;
        public int MaxSlots => inventorySize;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // 컴포넌트 참조
            playerController = GetComponent<PlayerController>();
            statsManager = GetComponent<PlayerStatsManager>();
            
            // 인벤토리 초기화
            if (IsOwner)
            {
                inventory = new InventoryData();
                inventory.Initialize(inventorySize);
                
                // 이벤트 구독
                inventory.OnItemAdded += OnInventoryItemAdded;
                inventory.OnItemRemoved += OnInventoryItemRemoved;
                inventory.OnInventoryChanged += OnInventoryDataChanged;
                
                // 네트워크 동기화
                networkInventory.Value = inventory;
            }
            else
            {
                // 다른 플레이어의 인벤토리 데이터 수신
                networkInventory.OnValueChanged += OnNetworkInventoryChanged;
                if (networkInventory.Value != null)
                {
                    inventory = networkInventory.Value;
                }
            }
            
            Debug.Log($"InventoryManager initialized for {name} (IsOwner: {IsOwner})");
        }
        
        public override void OnNetworkDespawn()
        {
            if (inventory != null)
            {
                inventory.OnItemAdded -= OnInventoryItemAdded;
                inventory.OnItemRemoved -= OnInventoryItemRemoved;
                inventory.OnInventoryChanged -= OnInventoryDataChanged;
            }

            if (!IsOwner)
            {
                networkInventory.OnValueChanged -= OnNetworkInventoryChanged;
            }

            OnItemAdded = null;
            OnItemRemoved = null;
            OnInventoryUpdated = null;

            base.OnNetworkDespawn();
        }
        
        /// <summary>
        /// 아이템 추가 (서버 RPC)
        /// </summary>
        [ServerRpc]
        public void AddItemServerRpc(string itemId, int quantity = 1, ServerRpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            
            // 아이템 데이터 가져오기
            var itemData = ItemDatabase.GetItem(itemId);
            if (itemData == null)
            {
                Debug.LogWarning($"Item not found: {itemId}");
                return;
            }
            
            // 아이템 인스턴스 생성
            var itemInstance = new ItemInstance();
            itemInstance.Initialize(itemId, quantity);
            
            // 인벤토리에 추가
            AddItemToInventoryClientRpc(itemInstance, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            });
        }
        
        /// <summary>
        /// 아이템을 인벤토리에 추가 (클라이언트 RPC)
        /// </summary>
        [ClientRpc]
        private void AddItemToInventoryClientRpc(ItemInstance item, ClientRpcParams rpcParams = default)
        {
            if (!IsOwner) return;
            
            if (inventory.TryAddItem(item, out int slotIndex))
            {
                Debug.Log($"Added {item.ItemData.ItemName} x{item.Quantity} to inventory slot {slotIndex}");
                
                // 골드 아이템 특별 처리
                if (item.ItemId == "gold_coin")
                {
                    if (statsManager != null)
                    {
                        statsManager.ChangeGold(item.ItemData.SellPrice * item.Quantity);
                        // 골드는 인벤토리에서 즉시 제거
                        inventory.RemoveItem(slotIndex, item.Quantity);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Failed to add {item.ItemData.ItemName} to inventory (full)");
                
                // 인벤토리가 꽉 찬 경우 바닥에 드롭
                DropItemOnGround(item);
            }
        }
        
        /// <summary>
        /// 아이템 직접 추가 (서버에서 호출, DroppedItem 픽업용)
        /// </summary>
        public bool TryAddItemDirect(ItemInstance item, out int slotIndex)
        {
            slotIndex = -1;
            
            Debug.Log($"🎯 TryAddItemDirect called - IsServer: {IsServer}, IsOwner: {IsOwner}, item: {item?.ItemData?.ItemName}");
            
            if (!IsServer) 
            {
                Debug.LogError("❌ TryAddItemDirect called on client");
                return false;
            }
            if (inventory == null) 
            {
                Debug.LogError("❌ inventory is null");
                return false;
            }
            
            if (inventory.TryAddItem(item, out slotIndex))
            {
                Debug.Log($"✅ Added {item.ItemData.ItemName} x{item.Quantity} to inventory slot {slotIndex}");
                
                // 골드 아이템 특별 처리
                if (item.ItemId == "gold_coin")
                {
                    if (statsManager != null)
                    {
                        statsManager.ChangeGold(item.ItemData.SellPrice * item.Quantity);
                        // 골드는 인벤토리에서 즉시 제거
                        inventory.RemoveItem(slotIndex, item.Quantity);
                    }
                }
                
                // 네트워크 동기화
                networkInventory.Value = inventory;
                
                // 이벤트 호출
                OnItemAdded?.Invoke(item, slotIndex);
                OnInventoryUpdated?.Invoke();
                
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 아이템 제거
        /// </summary>
        public bool RemoveItem(int slotIndex, int quantity = 1)
        {
            if (!IsOwner) return false;
            
            bool success = inventory.RemoveItem(slotIndex, quantity);
            if (success)
            {
                // 네트워크 동기화
                networkInventory.Value = inventory;
            }
            
            return success;
        }
        
        /// <summary>
        /// 아이템 이동
        /// </summary>
        public bool MoveItem(int fromSlot, int toSlot)
        {
            if (!IsOwner) return false;
            
            bool success = inventory.MoveItem(fromSlot, toSlot);
            if (success)
            {
                // 네트워크 동기화
                networkInventory.Value = inventory;
            }
            
            return success;
        }
        
        /// <summary>
        /// 아이템 사용
        /// </summary>
        public bool UseItem(int slotIndex)
        {
            if (!IsOwner) return false;
            
            var item = inventory.GetItem(slotIndex);
            if (item?.ItemData == null) return false;
            
            // 아이템 타입별 사용 처리
            bool consumed = ProcessItemUse(item);
            
            if (consumed)
            {
                RemoveItem(slotIndex, 1);
                Debug.Log($"Used {item.ItemData.ItemName}");
            }
            
            return consumed;
        }
        
        /// <summary>
        /// 아이템 사용 처리
        /// </summary>
        private bool ProcessItemUse(ItemInstance item)
        {
            if (statsManager == null) return false;
            
            switch (item.ItemData.ItemType)
            {
                case ItemType.Consumable:
                    return ProcessConsumableUse(item);
                    
                case ItemType.Equipment:
                    return ProcessEquipmentUse(item);
                    
                default:
                    Debug.Log($"Cannot use item: {item.ItemData.ItemName}");
                    return false;
            }
        }
        
        /// <summary>
        /// 소비 아이템 사용
        /// </summary>
        private bool ProcessConsumableUse(ItemInstance item)
        {
            // 체력 포션 등의 로직
            if (item.ItemId.Contains("health_potion"))
            {
                float healAmount = item.ItemData.SellPrice * 0.1f; // 임시 공식
                statsManager.Heal(healAmount);
                return true;
            }
            
            if (item.ItemId.Contains("mana_potion"))
            {
                float manaAmount = item.ItemData.SellPrice * 0.1f; // 임시 공식
                statsManager.RestoreMP(manaAmount);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 장비 아이템 사용 (착용)
        /// </summary>
        private bool ProcessEquipmentUse(ItemInstance item)
        {
            // 장비 착용 시스템 (추후 EquipmentManager와 연동)
            Debug.Log($"Equipment use not implemented: {item.ItemData.ItemName}");
            return false;
        }
        
        /// <summary>
        /// 아이템을 바닥에 드롭
        /// </summary>
        public void DropItem(int slotIndex, int quantity = 1)
        {
            if (!IsOwner) return;
            
            var item = inventory.GetItem(slotIndex);
            if (item?.ItemData == null) return;
            
            // 드롭할 아이템 생성
            var dropItem = item.Clone();
            dropItem.SetQuantity(Mathf.Min(quantity, item.Quantity));
            
            // 인벤토리에서 제거
            RemoveItem(slotIndex, quantity);
            
            // 바닥에 드롭
            DropItemOnGround(dropItem);
        }
        
        /// <summary>
        /// 바닥에 아이템 드롭
        /// </summary>
        private void DropItemOnGround(ItemInstance item)
        {
            DropItemServerRpc(item, transform.position);
        }
        
        /// <summary>
        /// 서버에서 아이템 드롭 처리
        /// </summary>
        [ServerRpc]
        private void DropItemServerRpc(ItemInstance item, Vector3 position)
        {
            var itemDropSystem = FindFirstObjectByType<ItemDropSystem>();
            if (itemDropSystem != null)
            {
                // 플레이어 주변에 랜덤하게 드롭
                Vector2 randomOffset = Random.insideUnitCircle * 1.5f;
                Vector3 dropPosition = position + new Vector3(randomOffset.x, randomOffset.y, 0);
                
                // ItemDropSystem의 DropItemAtPosition 메서드 사용
                itemDropSystem.DropItemAtPosition(dropPosition, item, GetComponent<PlayerController>());
                Debug.Log($"🎒 Dropped {item.ItemData.ItemName} x{item.Quantity} at {dropPosition} via ItemDropSystem");
            }
            else
            {
                Debug.LogError($"❌ ItemDropSystem not found! Cannot drop {item.ItemData.ItemName}");
            }
        }
        
        /// <summary>
        /// 인벤토리 정렬
        /// </summary>
        public void SortInventory()
        {
            if (!IsOwner) return;
            
            inventory.SortInventory();
            networkInventory.Value = inventory;
        }
        
        /// <summary>
        /// 특정 슬롯의 아이템 가져오기
        /// </summary>
        public ItemInstance GetItemAtSlot(int slotIndex)
        {
            return inventory?.GetItem(slotIndex);
        }

        /// <summary>
        /// 특정 아이템 개수 확인
        /// </summary>
        public int GetItemCount(string itemId)
        {
            return inventory?.GetItemCount(itemId) ?? 0;
        }
        
        /// <summary>
        /// 특정 아이템 보유 여부
        /// </summary>
        public bool HasItem(string itemId, int requiredQuantity = 1)
        {
            return inventory?.HasItem(itemId, requiredQuantity) ?? false;
        }
        
        /// <summary>
        /// 인벤토리 이벤트 핸들러들
        /// </summary>
        private void OnInventoryItemAdded(int slotIndex, ItemInstance item)
        {
            OnItemAdded?.Invoke(item, slotIndex);
        }
        
        private void OnInventoryItemRemoved(int slotIndex, ItemInstance item)
        {
            OnItemRemoved?.Invoke(item, slotIndex);
        }
        
        private void OnInventoryDataChanged()
        {
            if (IsOwner)
            {
                networkInventory.Value = inventory;
            }
            OnInventoryUpdated?.Invoke();
        }
        
        private void OnNetworkInventoryChanged(InventoryData previousValue, InventoryData newValue)
        {
            inventory = newValue;
            OnInventoryUpdated?.Invoke();
        }
        
        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        [ContextMenu("Log Inventory Info")]
        public void LogInventoryInfo()
        {
            inventory?.LogInventoryInfo();
        }
        
        /// <summary>
        /// 사용 가능한 인벤토리 슬롯 개수
        /// </summary>
        public int GetAvailableSlots()
        {
            return inventory?.GetAvailableSlots() ?? 0;
        }
        
        /// <summary>
        /// 인벤토리에 공간이 있는지 확인
        /// </summary>
        public bool HasSpace()
        {
            if (inventory == null) return false;
            return inventory.HasEmptySlot();
        }
        
        /// <summary>
        /// 아이템을 인벤토리에 추가 (간단한 인터페이스)
        /// </summary>
        public bool AddItem(ItemInstance item)
        {
            return AddItemToInventory(item);
        }
        
        /// <summary>
        /// 아이템을 인벤토리에 추가 (Equipment System 연동용)
        /// </summary>
        public bool AddItemToInventory(ItemInstance item)
        {
            if (item == null || inventory == null) 
            {
                Debug.LogError("❌ Cannot add item: item or inventory is null");
                return false;
            }
            
            if (item.ItemData == null)
            {
                Debug.LogError("❌ Cannot add item: ItemData is null");
                return false;
            }
            
            int slotIndex;
            bool success = inventory.TryAddItem(item, out slotIndex);
            
            if (success)
            {
                OnItemAdded?.Invoke(item, slotIndex);
                OnInventoryUpdated?.Invoke();
                
                // 네트워크 동기화
                if (IsOwner)
                {
                    networkInventory.Value = inventory;
                }
                
                Debug.Log($"📦 Added {item.ItemData.ItemName} x{item.Quantity} to inventory slot {slotIndex}");
            }
            else
            {
                Debug.LogError($"❌ Failed to add {item.ItemData.ItemName} to inventory - inventory full or other error");
            }
            
            return success;
        }
        
        /// <summary>
        /// 인벤토리에서 아이템 제거 (Equipment System 연동용)
        /// </summary>
        public bool RemoveItemFromInventory(ItemInstance item)
        {
            if (item == null || inventory == null) return false;
            
            // 인벤토리에서 해당 아이템 찾아서 제거
            for (int i = 0; i < inventory.MaxSlots; i++)
            {
                var slotItem = inventory.GetItem(i);
                if (slotItem != null && slotItem.InstanceId == item.InstanceId)
                {
                    inventory.RemoveItem(i, item.Quantity);
                    
                    OnItemRemoved?.Invoke(item, item.Quantity);
                    OnInventoryUpdated?.Invoke();
                    
                    // 네트워크 동기화
                    if (IsOwner)
                    {
                        networkInventory.Value = inventory;
                    }
                    
                    Debug.Log($"📦 Removed {item.ItemData.ItemName} from inventory");
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 기즈모 그리기 (자동 픽업 제거됨)
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // 자동 픽업 기능 제거로 기즈모 표시 안함
        }
    }
}