using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 인벤토리 데이터 관리 시스템
    /// 아이템 슬롯, 스택, 정렬 등을 처리
    /// </summary>
    [System.Serializable]
    public class InventoryData : INetworkSerializable, IEquatable<InventoryData>
    {
        [Header("인벤토리 설정")]
        [SerializeField] private int maxSlots = 30;
        [SerializeField] private bool allowStacking = true;
        [SerializeField] private bool autoSort = false;
        
        // 인벤토리 슬롯들
        private List<InventorySlot> slots = new List<InventorySlot>();
        
        // 이벤트
        public System.Action<int, ItemInstance> OnItemAdded;
        public System.Action<int, ItemInstance> OnItemRemoved;
        public System.Action<int, int> OnItemMoved;
        public System.Action OnInventoryChanged;
        
        // 프로퍼티
        public int MaxSlots => maxSlots;
        public int UsedSlots => slots.Count(slot => !slot.IsEmpty);
        public int EmptySlots => maxSlots - UsedSlots;
        public bool IsFull => UsedSlots >= maxSlots;
        public List<InventorySlot> Slots => slots;
        
        /// <summary>
        /// 인벤토리 초기화
        /// </summary>
        public void Initialize(int slotCount = 30)
        {
            maxSlots = slotCount;
            slots.Clear();
            
            // 빈 슬롯들로 초기화
            for (int i = 0; i < maxSlots; i++)
            {
                slots.Add(new InventorySlot(i));
            }
            
            Debug.Log($"Inventory initialized with {maxSlots} slots");
        }
        
        /// <summary>
        /// 아이템 추가 시도
        /// </summary>
        public bool TryAddItem(ItemInstance item, out int slotIndex)
        {
            slotIndex = -1;
            
            if (item == null || item.ItemData == null)
            {
                Debug.LogWarning("Cannot add null item to inventory");
                return false;
            }
            
            // 스택 가능한 아이템인지 확인
            if (allowStacking && item.ItemData.CanStack)
            {
                // 기존 스택에 추가 시도
                for (int i = 0; i < slots.Count; i++)
                {
                    if (!slots[i].IsEmpty && slots[i].Item.CanStackWith(item))
                    {
                        int spaceInStack = slots[i].Item.ItemData.MaxStackSize - slots[i].Item.Quantity;
                        if (spaceInStack > 0)
                        {
                            int addAmount = Mathf.Min(spaceInStack, item.Quantity);
                            slots[i].Item.AddQuantity(addAmount);
                            item.AddQuantity(-addAmount);
                            
                            slotIndex = i;
                            OnItemAdded?.Invoke(i, slots[i].Item);
                            
                            if (item.Quantity <= 0)
                            {
                                OnInventoryChanged?.Invoke();
                                return true;
                            }
                        }
                    }
                }
            }
            
            // 새 슬롯에 추가
            int emptySlot = FindEmptySlot();
            if (emptySlot != -1)
            {
                slots[emptySlot].SetItem(item);
                slotIndex = emptySlot;
                OnItemAdded?.Invoke(emptySlot, item);
                OnInventoryChanged?.Invoke();
                return true;
            }
            
            Debug.LogWarning("Inventory is full, cannot add item");
            return false;
        }
        
        /// <summary>
        /// 아이템 제거
        /// </summary>
        public bool RemoveItem(int slotIndex, int quantity = 1)
        {
            if (!IsValidSlotIndex(slotIndex) || slots[slotIndex].IsEmpty)
            {
                return false;
            }
            
            var slot = slots[slotIndex];
            var removedItem = slot.Item.Clone();
            
            if (slot.Item.Quantity <= quantity)
            {
                // 전체 제거
                removedItem.SetQuantity(slot.Item.Quantity);
                slot.Clear();
            }
            else
            {
                // 일부 제거
                slot.Item.AddQuantity(-quantity);
                removedItem.SetQuantity(quantity);
            }
            
            OnItemRemoved?.Invoke(slotIndex, removedItem);
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        /// <summary>
        /// 아이템 이동
        /// </summary>
        public bool MoveItem(int fromSlot, int toSlot)
        {
            if (!IsValidSlotIndex(fromSlot) || !IsValidSlotIndex(toSlot))
            {
                return false;
            }
            
            if (fromSlot == toSlot) return true;
            
            var fromItem = slots[fromSlot];
            var toItem = slots[toSlot];
            
            // 목적지가 비어있으면 단순 이동
            if (toItem.IsEmpty)
            {
                slots[toSlot].SetItem(fromItem.Item);
                slots[fromSlot].Clear();
                
                OnItemMoved?.Invoke(fromSlot, toSlot);
                OnInventoryChanged?.Invoke();
                return true;
            }
            
            // 같은 아이템이면 스택 시도
            if (fromItem.Item.CanStackWith(toItem.Item))
            {
                int spaceInTarget = toItem.Item.ItemData.MaxStackSize - toItem.Item.Quantity;
                int transferAmount = Mathf.Min(spaceInTarget, fromItem.Item.Quantity);
                
                if (transferAmount > 0)
                {
                    toItem.Item.AddQuantity(transferAmount);
                    fromItem.Item.AddQuantity(-transferAmount);
                    
                    if (fromItem.Item.Quantity <= 0)
                    {
                        slots[fromSlot].Clear();
                    }
                    
                    OnItemMoved?.Invoke(fromSlot, toSlot);
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
            
            // 서로 교환
            var tempItem = fromItem.Item;
            slots[fromSlot].SetItem(toItem.Item);
            slots[toSlot].SetItem(tempItem);
            
            OnItemMoved?.Invoke(fromSlot, toSlot);
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        /// <summary>
        /// 특정 아이템 개수 계산
        /// </summary>
        public int GetItemCount(string itemId)
        {
            int totalCount = 0;
            
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty && slot.Item.ItemId == itemId)
                {
                    totalCount += slot.Item.Quantity;
                }
            }
            
            return totalCount;
        }
        
        /// <summary>
        /// 특정 아이템 보유 여부 확인
        /// </summary>
        public bool HasItem(string itemId, int requiredQuantity = 1)
        {
            return GetItemCount(itemId) >= requiredQuantity;
        }
        
        /// <summary>
        /// 특정 아이템 모두 제거
        /// </summary>
        public int RemoveAllItems(string itemId, int maxRemove = int.MaxValue)
        {
            int removedCount = 0;
            
            for (int i = 0; i < slots.Count && removedCount < maxRemove; i++)
            {
                if (!slots[i].IsEmpty && slots[i].Item.ItemId == itemId)
                {
                    int removeFromSlot = Mathf.Min(slots[i].Item.Quantity, maxRemove - removedCount);
                    RemoveItem(i, removeFromSlot);
                    removedCount += removeFromSlot;
                }
            }
            
            return removedCount;
        }
        
        /// <summary>
        /// 인벤토리 정렬
        /// </summary>
        public void SortInventory()
        {
            // 비어있지 않은 슬롯들만 추출
            var itemsToSort = slots.Where(slot => !slot.IsEmpty)
                                  .Select(slot => slot.Item)
                                  .OrderBy(item => item.ItemData.ItemType)
                                  .ThenBy(item => item.ItemData.Grade)
                                  .ThenBy(item => item.ItemData.ItemName)
                                  .ToList();
            
            // 모든 슬롯 비우기
            foreach (var slot in slots)
            {
                slot.Clear();
            }
            
            // 정렬된 순서로 다시 배치
            for (int i = 0; i < itemsToSort.Count && i < maxSlots; i++)
            {
                slots[i].SetItem(itemsToSort[i]);
            }
            
            OnInventoryChanged?.Invoke();
        }
        
        /// <summary>
        /// 빈 슬롯 찾기
        /// </summary>
        private int FindEmptySlot()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsEmpty)
                {
                    return i;
                }
            }
            return -1;
        }
        
        /// <summary>
        /// 유효한 슬롯 인덱스인지 확인
        /// </summary>
        private bool IsValidSlotIndex(int index)
        {
            return index >= 0 && index < slots.Count;
        }
        
        /// <summary>
        /// 슬롯 가져오기
        /// </summary>
        public InventorySlot GetSlot(int index)
        {
            if (IsValidSlotIndex(index))
            {
                return slots[index];
            }
            return null;
        }
        
        /// <summary>
        /// 아이템 가져오기
        /// </summary>
        public ItemInstance GetItem(int slotIndex)
        {
            var slot = GetSlot(slotIndex);
            return slot?.Item;
        }
        
        /// <summary>
        /// 인벤토리 정보 출력 (디버그용)
        /// </summary>
        public void LogInventoryInfo()
        {
            Debug.Log($"=== Inventory Info ===");
            Debug.Log($"Used Slots: {UsedSlots}/{MaxSlots}");
            
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty)
                {
                    var item = slots[i].Item;
                    Debug.Log($"Slot {i}: {item.ItemData.ItemName} x{item.Quantity}");
                }
            }
        }
        
        /// <summary>
        /// 네트워크 직렬화
        /// </summary>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref maxSlots);
            
            if (serializer.IsReader)
            {
                Initialize(maxSlots);
            }
            
            // 사용 중인 슬롯만 직렬화
            int usedSlotsCount = UsedSlots;
            serializer.SerializeValue(ref usedSlotsCount);
            
            if (serializer.IsWriter)
            {
                foreach (var slot in slots.Where(s => !s.IsEmpty))
                {
                    int slotIndex = slot.SlotIndex;
                    serializer.SerializeValue(ref slotIndex);
                    slot.Item.NetworkSerialize(serializer);
                }
            }
            else
            {
                for (int i = 0; i < usedSlotsCount; i++)
                {
                    int slotIndex = 0;
                    serializer.SerializeValue(ref slotIndex);
                    
                    var item = new ItemInstance();
                    item.NetworkSerialize(serializer);
                    
                    if (IsValidSlotIndex(slotIndex))
                    {
                        slots[slotIndex].SetItem(item);
                    }
                }
            }
        }
        
        /// <summary>
        /// 사용 가능한 빈 슬롯 개수 반환
        /// </summary>
        public int GetAvailableSlots()
        {
            return EmptySlots;
        }
        
        /// <summary>
        /// IEquatable<InventoryData> 구현
        /// </summary>
        public bool Equals(InventoryData other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            
            // 기본 설정 비교
            if (maxSlots != other.maxSlots || 
                allowStacking != other.allowStacking || 
                autoSort != other.autoSort)
                return false;
            
            // 슬롯 개수 비교
            if (slots.Count != other.slots.Count)
                return false;
            
            // 각 슬롯의 아이템 비교
            for (int i = 0; i < slots.Count; i++)
            {
                var thisSlot = slots[i];
                var otherSlot = other.slots[i];
                
                // 둘 다 비어있으면 통과
                if (thisSlot.IsEmpty && otherSlot.IsEmpty)
                    continue;
                
                // 하나만 비어있으면 다름
                if (thisSlot.IsEmpty != otherSlot.IsEmpty)
                    return false;
                
                // 아이템 비교 (InstanceId 기준)
                if (thisSlot.Item?.InstanceId != otherSlot.Item?.InstanceId)
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Object.Equals 오버라이드
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as InventoryData);
        }
        
        /// <summary>
        /// GetHashCode 오버라이드
        /// </summary>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + maxSlots.GetHashCode();
            hash = hash * 23 + allowStacking.GetHashCode();
            hash = hash * 23 + autoSort.GetHashCode();
            
            // 비어있지 않은 슬롯들의 해시코드 포함
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty && slot.Item != null)
                {
                    hash = hash * 23 + slot.Item.InstanceId.GetHashCode();
                }
            }
            
            return hash;
        }
    }
    
    /// <summary>
    /// 인벤토리 슬롯
    /// </summary>
    [System.Serializable]
    public class InventorySlot
    {
        [SerializeField] private int slotIndex;
        [SerializeField] private ItemInstance item;
        
        public int SlotIndex => slotIndex;
        public ItemInstance Item => item;
        public bool IsEmpty => item == null || item.ItemData == null;
        
        public InventorySlot(int index)
        {
            slotIndex = index;
            item = null;
        }
        
        public void SetItem(ItemInstance newItem)
        {
            item = newItem;
        }
        
        public void Clear()
        {
            item = null;
        }
    }
}