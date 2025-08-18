using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 간소화된 장비 시스템 관리자 - 컴파일 문제 해결용
    /// </summary>
    public class EquipmentManager : NetworkBehaviour
    {
        [Header("Equipment Slots")]
        [SerializeField] private EquipmentData equipmentData;
        
        // 이벤트
        public System.Action<EquipmentSlot, ItemInstance> OnEquipmentChanged;
        
        public EquipmentData Equipment => equipmentData;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // 장비 데이터 초기화
            if (equipmentData == null)
            {
                equipmentData = new EquipmentData();
                equipmentData.Initialize();
            }
        }
        
        /// <summary>
        /// 아이템 착용 시도 (간소화 버전)
        /// </summary>
        public bool TryEquipItem(ItemInstance item, bool fromInventory = true)
        {
            if (!IsOwner || item?.ItemData == null) return false;
            
            if (!item.ItemData.IsEquippable)
            {
                Debug.LogWarning($"Cannot equip {item.ItemData.ItemName}: Not an equippable item");
                return false;
            }
            
            // 기본적인 슬롯 매핑
            EquipmentSlot targetSlot = GetBasicEquipmentSlot(item);
            if (targetSlot == EquipmentSlot.None)
            {
                Debug.LogWarning($"Cannot equip {item.ItemData.ItemName}: No suitable equipment slot");
                return false;
            }
            
            // 기존 아이템 교체 없이 간단히 착용
            equipmentData.SetEquippedItem(targetSlot, item);
            OnEquipmentChanged?.Invoke(targetSlot, item);
            
            Debug.Log($"⚔️ Equipped {item.ItemData.ItemName} to {targetSlot}");
            return true;
        }
        
        /// <summary>
        /// 아이템 해제 (간소화 버전)
        /// </summary>
        public bool UnequipItem(EquipmentSlot slot, bool addToInventory = true)
        {
            if (!IsOwner) return false;
            
            ItemInstance equippedItem = equipmentData.GetEquippedItem(slot);
            if (equippedItem == null) return false;
            
            equipmentData.SetEquippedItem(slot, null);
            OnEquipmentChanged?.Invoke(slot, null);
            
            Debug.Log($"🛡️ Unequipped {equippedItem.ItemData.ItemName} from {slot}");
            return true;
        }
        
        /// <summary>
        /// 기본적인 장비 슬롯 매핑
        /// </summary>
        private EquipmentSlot GetBasicEquipmentSlot(ItemInstance item)
        {
            if (item?.ItemData == null) return EquipmentSlot.None;
            
            if (item.ItemData.ItemType == ItemType.Equipment)
            {
                // 장비 슬롯 정보가 있으면 사용
                if (item.ItemData.EquipmentSlot != EquipmentSlot.None)
                {
                    return item.ItemData.EquipmentSlot;
                }
                
                // WeaponCategory로 구분해서 기본 슬롯 결정
                switch (item.ItemData.WeaponCategory)
                {
                    case WeaponCategory.Sword:
                    case WeaponCategory.Dagger:
                    case WeaponCategory.Axe:
                    case WeaponCategory.Mace:
                        return EquipmentSlot.MainHand;
                    case WeaponCategory.Bow:
                    case WeaponCategory.Staff:
                        return EquipmentSlot.TwoHand;
                    case WeaponCategory.Shield:
                        return EquipmentSlot.OffHand;
                    default:
                        return EquipmentSlot.Chest; // 방어구로 가정
                }
            }
            
            return EquipmentSlot.None;
        }
        
        /// <summary>
        /// 모든 착용 장비 가져오기 (ItemScatter 연동용)
        /// </summary>
        public List<ItemInstance> GetAllEquippedItems()
        {
            var equippedItems = new List<ItemInstance>();
            
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                if (slot == EquipmentSlot.None) continue;
                
                var item = equipmentData.GetEquippedItem(slot);
                if (item != null)
                {
                    equippedItems.Add(item);
                }
            }
            
            return equippedItems;
        }
        
        /// <summary>
        /// 특정 슬롯의 착용 아이템 가져오기
        /// </summary>
        public ItemInstance GetEquippedItem(EquipmentSlot slot)
        {
            return equipmentData?.GetEquippedItem(slot);
        }
        
        /// <summary>
        /// 모든 장비 해제 (사망 시 사용)
        /// </summary>
        public void UnequipAllItems(bool addToInventory = false)
        {
            if (!IsOwner) return;
            
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                if (slot == EquipmentSlot.None) continue;
                
                UnequipItem(slot, addToInventory);
            }
            
            Debug.Log("🛡️ All equipment unequipped");
        }
    }
}