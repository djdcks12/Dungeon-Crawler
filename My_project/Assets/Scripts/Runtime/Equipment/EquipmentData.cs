using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 장비 데이터 클래스 - 착용 중인 장비들을 관리
    /// </summary>
    [System.Serializable]
    public class EquipmentData : INetworkSerializable
    {
        // 장비 슬롯별 착용 아이템
        private Dictionary<EquipmentSlot, ItemInstance> equippedItems;
        
        // 네트워크 직렬화를 위한 배열
        [SerializeField] private EquipmentSlotData[] equipmentSlots;
        
        public EquipmentData()
        {
            Initialize();
        }
        
        /// <summary>
        /// 장비 데이터 초기화
        /// </summary>
        public void Initialize()
        {
            equippedItems = new Dictionary<EquipmentSlot, ItemInstance>();
            
            // 모든 장비 슬롯을 null로 초기화
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                if (slot != EquipmentSlot.None)
                {
                    equippedItems[slot] = null;
                }
            }
            
            UpdateSerializationArray();
        }
        
        /// <summary>
        /// 특정 슬롯에 아이템 착용
        /// </summary>
        public void SetEquippedItem(EquipmentSlot slot, ItemInstance item)
        {
            if (slot == EquipmentSlot.None) return;
            
            equippedItems[slot] = item;
            UpdateSerializationArray();
        }
        
        /// <summary>
        /// 특정 슬롯의 착용 아이템 가져오기
        /// </summary>
        public ItemInstance GetEquippedItem(EquipmentSlot slot)
        {
            if (slot == EquipmentSlot.None) return null;
            
            equippedItems.TryGetValue(slot, out ItemInstance item);
            return item;
        }
        
        /// <summary>
        /// 모든 착용 아이템 가져오기
        /// </summary>
        public List<ItemInstance> GetAllEquippedItems()
        {
            return equippedItems.Values.Where(item => item != null).ToList();
        }
        
        /// <summary>
        /// 저장용 장비 딕셔너리 가져오기
        /// </summary>
        public Dictionary<EquipmentSlot, ItemInstance> GetAllEquippedItemsForSave()
        {
            return new Dictionary<EquipmentSlot, ItemInstance>(equippedItems);
        }
        
        /// <summary>
        /// 저장 데이터에서 장비 로드
        /// </summary>
        public void LoadFromSaveData(Dictionary<EquipmentSlot, ItemInstance> savedEquipment)
        {
            if (savedEquipment == null) return;
            
            Initialize();
            
            foreach (var kvp in savedEquipment)
            {
                if (kvp.Value != null)
                {
                    equippedItems[kvp.Key] = kvp.Value;
                }
            }
            
            UpdateSerializationArray();
        }
        
        /// <summary>
        /// 특정 슬롯이 비어있는지 확인
        /// </summary>
        public bool IsSlotEmpty(EquipmentSlot slot)
        {
            return GetEquippedItem(slot) == null;
        }
        
        /// <summary>
        /// 아이템이 착용되어 있는지 확인
        /// </summary>
        public bool IsItemEquipped(ItemInstance item)
        {
            if (item == null) return false;
            
            return equippedItems.Values.Any(equippedItem => 
                equippedItem != null && equippedItem.InstanceId == item.InstanceId);
        }
        
        /// <summary>
        /// 아이템이 착용된 슬롯 찾기
        /// </summary>
        public EquipmentSlot FindItemSlot(ItemInstance item)
        {
            if (item == null) return EquipmentSlot.None;
            
            foreach (var kvp in equippedItems)
            {
                if (kvp.Value != null && kvp.Value.InstanceId == item.InstanceId)
                {
                    return kvp.Key;
                }
            }
            
            return EquipmentSlot.None;
        }
        
        /// <summary>
        /// 모든 장비 해제
        /// </summary>
        public void ClearAllEquipment()
        {
            foreach (var slot in equippedItems.Keys.ToList())
            {
                equippedItems[slot] = null;
            }
            
            UpdateSerializationArray();
        }
        
        /// <summary>
        /// 착용 장비 개수
        /// </summary>
        public int GetEquippedItemCount()
        {
            return equippedItems.Values.Count(item => item != null);
        }
        
        /// <summary>
        /// 특정 카테고리의 무기가 착용되어 있는지 확인
        /// </summary>
        public bool HasWeaponOfCategory(WeaponCategory category)
        {
            var mainHandItem = GetEquippedItem(EquipmentSlot.MainHand);
            var offHandItem = GetEquippedItem(EquipmentSlot.OffHand);
            var twoHandItem = GetEquippedItem(EquipmentSlot.TwoHand);
            
            return (mainHandItem?.ItemData?.WeaponCategory == category) ||
                   (offHandItem?.ItemData?.WeaponCategory == category) ||
                   (twoHandItem?.ItemData?.WeaponCategory == category);
        }
        
        /// <summary>
        /// 장비로 인한 총 스탯 보너스 계산
        /// </summary>
        public StatBlock CalculateTotalStatBonus()
        {
            var totalStats = new StatBlock();
            
            foreach (var item in equippedItems.Values)
            {
                if (item?.ItemData != null)
                {
                    totalStats = totalStats.Add(item.ItemData.statBonuses);
                }
            }
            
            return totalStats;
        }
        
        /// <summary>
        /// 네트워크 직렬화용 배열 업데이트
        /// </summary>
        private void UpdateSerializationArray()
        {
            var slots = new List<EquipmentSlotData>();
            
            foreach (var kvp in equippedItems)
            {
                if (kvp.Value != null)
                {
                    slots.Add(new EquipmentSlotData
                    {
                        slot = kvp.Key,
                        item = kvp.Value
                    });
                }
            }
            
            equipmentSlots = slots.ToArray();
        }
        
        /// <summary>
        /// 네트워크 직렬화에서 딕셔너리 복원
        /// </summary>
        private void UpdateFromSerializationArray()
        {
            if (equippedItems == null)
            {
                Initialize();
                return;
            }
            
            // 모든 슬롯을 null로 초기화
            foreach (var slot in equippedItems.Keys.ToList())
            {
                equippedItems[slot] = null;
            }
            
            // 직렬화 배열에서 데이터 복원
            if (equipmentSlots != null)
            {
                foreach (var slotData in equipmentSlots)
                {
                    if (slotData.item != null)
                    {
                        equippedItems[slotData.slot] = slotData.item;
                    }
                }
            }
        }
        
        /// <summary>
        /// 네트워크 직렬화
        /// </summary>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsWriter)
            {
                UpdateSerializationArray();
            }
            
            serializer.SerializeValue(ref equipmentSlots);
            
            if (serializer.IsReader)
            {
                UpdateFromSerializationArray();
            }
        }
        
        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        public void LogEquipmentInfo()
        {
            Debug.Log("=== Equipment Info ===");
            foreach (var kvp in equippedItems)
            {
                if (kvp.Value != null)
                {
                    Debug.Log($"{kvp.Key}: {kvp.Value.ItemData.itemName} (Grade: {kvp.Value.ItemData.Grade})");
                }
                else
                {
                    Debug.Log($"{kvp.Key}: Empty");
                }
            }
            Debug.Log("=====================");
        }
    }
}