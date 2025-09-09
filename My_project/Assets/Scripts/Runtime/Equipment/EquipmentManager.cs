using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 완전한 장비 시스템 관리자 - 하드코어 던전 크롤러
    /// 11개 장비 슬롯 지원, 인벤토리 연동, 스탯 적용
    /// </summary>
    public class EquipmentManager : NetworkBehaviour
    {
        [Header("Equipment Slots")]
        [SerializeField] private EquipmentData equipmentData;
        
        // 컴포넌트 참조
        private InventoryManager inventoryManager;
        private PlayerStatsManager statsManager;
        
        // 이벤트
        public System.Action<EquipmentSlot, ItemInstance> OnEquipmentChanged;
        public System.Action<StatBlock> OnEquipmentStatsChanged;
        
        public EquipmentData Equipment => equipmentData;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // 컴포넌트 참조 설정
            inventoryManager = GetComponent<InventoryManager>();
            statsManager = GetComponent<PlayerStatsManager>();
            
            // 장비 데이터 초기화
            if (equipmentData == null)
            {
                equipmentData = new EquipmentData();
                equipmentData.Initialize();
            }
            
            // 장비 변경 이벤트 구독
            OnEquipmentChanged += OnEquipmentChangedHandler;
            
            Debug.Log("⚔️ Equipment Manager initialized with 11 equipment slots");
        }
        
        public override void OnNetworkDespawn()
        {
            // 이벤트 구독 해제
            OnEquipmentChanged -= OnEquipmentChangedHandler;
            
            base.OnNetworkDespawn();
        }
        
        /// <summary>
        /// 아이템 착용 시도 (완전한 버전)
        /// </summary>
        public bool TryEquipItem(ItemInstance item, bool fromInventory = true)
        {
            if (!IsOwner || item?.ItemData == null) return false;
            
            if (!item.ItemData.IsEquippable)
            {
                Debug.LogWarning($"Cannot equip {item.ItemData.ItemName}: Not an equippable item");
                return false;
            }
            
            // 종족 제한 체크
            if (statsManager?.CurrentStats != null)
            {
                if (!item.ItemData.CanPlayerEquip(statsManager.CurrentStats.CharacterRace))
                {
                    Debug.LogWarning($"Cannot equip {item.ItemData.ItemName}: Race restriction");
                    return false;
                }
            }
            
            // 적절한 장비 슬롯 찾기
            EquipmentSlot targetSlot = GetOptimalEquipmentSlot(item);
            if (targetSlot == EquipmentSlot.None)
            {
                Debug.LogWarning($"Cannot equip {item.ItemData.ItemName}: No suitable equipment slot");
                return false;
            }
            
            // 기존 착용 아이템 처리
            ItemInstance currentItem = equipmentData.GetEquippedItem(targetSlot);
            if (currentItem != null)
            {
                // 인벤토리에 공간이 있는지 확인
                if (fromInventory && inventoryManager != null)
                {
                    int availableSpace = inventoryManager.GetAvailableSlots();
                    if (availableSpace <= 0)
                    {
                        Debug.LogWarning("Cannot equip: Inventory is full");
                        return false;
                    }
                }
            }
            
            // 서버에서 장비 변경 처리
            if (IsServer)
            {
                PerformEquipChange(targetSlot, item, currentItem, fromInventory);
            }
            else
            {
                // 클라이언트에서 서버로 요청
                EquipItemServerRpc(item, targetSlot, fromInventory);
            }
            
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
        
        /// <summary>
        /// 장비 착용 서버 RPC
        /// </summary>
        [ServerRpc]
        private void EquipItemServerRpc(ItemInstance item, EquipmentSlot targetSlot, bool fromInventory)
        {
            if (!IsServer) return;
            
            ItemInstance currentItem = equipmentData.GetEquippedItem(targetSlot);
            PerformEquipChange(targetSlot, item, currentItem, fromInventory);
        }
        
        /// <summary>
        /// 실제 장비 변경 수행
        /// </summary>
        private void PerformEquipChange(EquipmentSlot targetSlot, ItemInstance newItem, ItemInstance oldItem, bool fromInventory)
        {
            // 기존 아이템을 인벤토리로 이동
            if (oldItem != null && fromInventory && inventoryManager != null)
            {
                inventoryManager.AddItemToInventory(oldItem);
            }
            
            // 새 아이템을 인벤토리에서 제거
            if (fromInventory && inventoryManager != null)
            {
                inventoryManager.RemoveItemFromInventory(newItem);
            }
            
            // 장비 착용
            equipmentData.SetEquippedItem(targetSlot, newItem);
            
            // 이벤트 발생
            OnEquipmentChanged?.Invoke(targetSlot, newItem);
            
            Debug.Log($"⚔️ Equipment changed: {newItem.ItemData.ItemName} equipped to {targetSlot}");
        }
        
        /// <summary>
        /// 최적의 장비 슬롯 찾기 (개선된 버전)
        /// </summary>
        private EquipmentSlot GetOptimalEquipmentSlot(ItemInstance item)
        {
            if (item?.ItemData == null) return EquipmentSlot.None;
            
            // ItemData에 명시적 슬롯 정보가 있으면 우선 사용
            if (item.ItemData.EquipmentSlot != EquipmentSlot.None)
            {
                return item.ItemData.EquipmentSlot;
            }
            
            // WeaponGroup에 따른 슬롯 결정
            if (item.ItemData.IsWeapon)
            {
                WeaponGroup weaponGroup = item.ItemData.WeaponGroup;
                return WeaponTypeMapper.GetEquipmentSlot(weaponGroup);
            }
            
            // 무기가 아닌 경우 방어구나 악세서리 판단
            return DetermineArmorSlot(item);
        }
        
        /// <summary>
        /// 방어구/악세서리 슬롯 결정
        /// </summary>
        private EquipmentSlot DetermineArmorSlot(ItemInstance item)
        {
            string itemName = item.ItemData.ItemName.ToLower();
            
            // 이름으로 슬롯 추정
            if (itemName.Contains("helmet") || itemName.Contains("hat") || itemName.Contains("crown"))
                return EquipmentSlot.Head;
            
            if (itemName.Contains("armor") || itemName.Contains("chestplate") || itemName.Contains("shirt"))
                return EquipmentSlot.Chest;
            
            if (itemName.Contains("pants") || itemName.Contains("leggings") || itemName.Contains("trousers"))
                return EquipmentSlot.Legs;
            
            if (itemName.Contains("boots") || itemName.Contains("shoes") || itemName.Contains("sandals"))
                return EquipmentSlot.Feet;
            
            if (itemName.Contains("gloves") || itemName.Contains("gauntlets"))
                return EquipmentSlot.Hands;
            
            if (itemName.Contains("ring"))
            {
                // 비어있는 반지 슬롯 찾기
                if (equipmentData.IsSlotEmpty(EquipmentSlot.Ring1))
                    return EquipmentSlot.Ring1;
                else
                    return EquipmentSlot.Ring2;
            }
            
            if (itemName.Contains("necklace") || itemName.Contains("amulet") || itemName.Contains("pendant"))
                return EquipmentSlot.Necklace;
            
            // 기본값: 가슴 슬롯 (일반 방어구로 가정)
            return EquipmentSlot.Chest;
        }
        
        /// <summary>
        /// 장비 변경 이벤트 핸들러
        /// </summary>
        private void OnEquipmentChangedHandler(EquipmentSlot slot, ItemInstance item)
        {
            // 장비 스탯 재계산
            RecalculateEquipmentStats();
            
            // 클라이언트에 장비 변경 알림
            if (IsServer)
            {
                NotifyEquipmentChangedClientRpc(slot, item);
            }
        }
        
        /// <summary>
        /// 장비 스탯 재계산
        /// </summary>
        private void RecalculateEquipmentStats()
        {
            StatBlock totalEquipmentStats = equipmentData.CalculateTotalStatBonus();
            
            // PlayerStatsManager에 장비 스탯 적용
            if (statsManager != null)
            {
                statsManager.UpdateEquipmentStats(totalEquipmentStats);
                
                // 무기 장착 처리 - PlayerStatsData의 EquipWeapon 호출
                HandleWeaponEquip();
            }
            
            // 스탯 변경 이벤트 발생
            OnEquipmentStatsChanged?.Invoke(totalEquipmentStats);
            
            Debug.Log($"📊 Equipment stats recalculated: {GetStatSummary(totalEquipmentStats)}");
        }
        
        /// <summary>
        /// 무기 장착 처리
        /// </summary>
        private void HandleWeaponEquip()
        {
            // 주무기나 양손무기 확인
            var mainHandItem = equipmentData.GetEquippedItem(EquipmentSlot.MainHand);
            var twoHandItem = equipmentData.GetEquippedItem(EquipmentSlot.TwoHand);
            
            ItemInstance weaponItem = twoHandItem ?? mainHandItem; // 양손무기 우선
            
            if (weaponItem != null && weaponItem.ItemData != null && weaponItem.ItemData.IsWeapon)
            {
                // 새로운 시스템: ItemInstance를 직접 전달
                statsManager.CurrentStats.EquipWeapon(weaponItem);
            }
            else
            {
                // 무기가 없으면 해제
                statsManager.CurrentStats.UnequipWeapon();
            }
        }
        
        /// <summary>
        /// 스탯 요약 텍스트
        /// </summary>
        private string GetStatSummary(StatBlock stats)
        {
            var summary = new List<string>();
            
            if (stats.strength > 0) summary.Add($"STR+{stats.strength}");
            if (stats.agility > 0) summary.Add($"AGI+{stats.agility}");
            if (stats.vitality > 0) summary.Add($"VIT+{stats.vitality}");
            if (stats.intelligence > 0) summary.Add($"INT+{stats.intelligence}");
            if (stats.defense > 0) summary.Add($"DEF+{stats.defense}");
            if (stats.magicDefense > 0) summary.Add($"MDEF+{stats.magicDefense}");
            if (stats.luck > 0) summary.Add($"LUK+{stats.luck}");
            if (stats.stability > 0) summary.Add($"STAB+{stats.stability}");
            
            return summary.Count > 0 ? string.Join(", ", summary) : "No bonuses";
        }
        
        /// <summary>
        /// 클라이언트에 장비 변경 알림
        /// </summary>
        [ClientRpc]
        private void NotifyEquipmentChangedClientRpc(EquipmentSlot slot, ItemInstance item)
        {
            // UI 업데이트나 시각적 효과 처리
            Debug.Log($"🔄 Equipment UI update: {slot} -> {item?.ItemData?.ItemName ?? "Empty"}");
        }
        
        /// <summary>
        /// 특정 아이템이 착용되어 있는지 확인
        /// </summary>
        public bool IsItemEquipped(ItemInstance item)
        {
            if (item == null || equipmentData == null) return false;
            
            var equippedItems = equipmentData.GetAllEquippedItems();
            
            foreach (var equippedItem in equippedItems)
            {
                if (equippedItem != null && equippedItem.InstanceId == item.InstanceId)
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}