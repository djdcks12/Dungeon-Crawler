using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 인챈트 매니저 - 아이템에 인챈트 적용 및 효과 관리
    /// 하드코어 던전 크롤러의 인챈트 시스템 총괄
    /// </summary>
    public class EnchantManager : NetworkBehaviour
    {
        [Header("인챈트 설정")]
        [SerializeField] private int maxEnchantsPerItem = 3; // 아이템당 최대 인챈트 수
        [SerializeField] private float enchantSuccessRate = 0.8f; // 인챈트 성공률 (80%)
        [SerializeField] private bool allowEnchantOverwrite = false; // 동일 인챈트 덮어쓰기 허용
        
        // 컴포넌트 참조
        private PlayerStatsManager statsManager;
        private InventoryManager inventoryManager;
        private EquipmentManager equipmentManager;
        
        // 현재 적용된 인챈트 효과들
        private Dictionary<EnchantType, float> activeEnchantEffects = new Dictionary<EnchantType, float>();
        
        // 이벤트
        public System.Action<ItemInstance, EnchantData> OnEnchantApplied;
        public System.Action<ItemInstance, EnchantData> OnEnchantRemoved;
        public System.Action OnEnchantEffectsUpdated;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // 컴포넌트 참조
            statsManager = GetComponent<PlayerStatsManager>();
            inventoryManager = GetComponent<InventoryManager>();
            equipmentManager = GetComponent<EquipmentManager>();
            
            // 장비 변경 이벤트 구독
            if (equipmentManager != null)
            {
                equipmentManager.OnEquipmentChanged += OnEquipmentChangedHandler;
            }
            
            Debug.Log($"EnchantManager initialized for {name}");
        }
        
        public override void OnNetworkDespawn()
        {
            // 이벤트 구독 해제
            if (equipmentManager != null)
            {
                equipmentManager.OnEquipmentChanged -= OnEquipmentChangedHandler;
            }
            
            base.OnNetworkDespawn();
        }
        
        /// <summary>
        /// 인챈트 북을 사용하여 아이템에 인챈트 적용
        /// </summary>
        public bool ApplyEnchantToItem(ItemInstance enchantBook, ItemInstance targetItem)
        {
            if (!IsOwner) return false;
            if (enchantBook == null || targetItem == null) return false;
            
            // 인챈트 북에서 인챈트 데이터 추출
            var enchantData = ExtractEnchantFromBook(enchantBook);
            if (enchantData.enchantType == EnchantType.None) return false;
            
            // 인챈트 적용 가능 여부 검사
            if (!CanApplyEnchant(targetItem, enchantData))
            {
                Debug.LogWarning($"Cannot apply enchant {enchantData.GetEnchantName()} to {targetItem.ItemData.ItemName}");
                return false;
            }
            
            // 인챈트 성공률 체크
            if (Random.value > enchantSuccessRate)
            {
                Debug.Log($"❌ Enchant failed! Success rate: {enchantSuccessRate:P0}");
                // 인챈트 북은 소모되지만 인챈트는 실패
                return true; // 인챈트 북 소모는 성공
            }
            
            // 인챈트 적용
            bool success = AddEnchantToItem(targetItem, enchantData);
            
            if (success)
            {
                Debug.Log($"✨ Successfully enchanted {targetItem.ItemData.ItemName} with {enchantData.GetEnchantName()}");
                OnEnchantApplied?.Invoke(targetItem, enchantData);
                
                // 착용 중인 아이템이면 즉시 효과 적용
                if (equipmentManager != null && equipmentManager.IsItemEquipped(targetItem))
                {
                    RecalculateEnchantEffects();
                }
            }
            
            return success;
        }
        
        /// <summary>
        /// 인챈트 북에서 인챈트 데이터 추출
        /// </summary>
        private EnchantData ExtractEnchantFromBook(ItemInstance enchantBook)
        {
            if (enchantBook.ItemId != "enchant_book" || enchantBook.CustomData == null)
            {
                return default(EnchantData);
            }
            
            if (enchantBook.CustomData.TryGetValue("EnchantData", out string enchantJson))
            {
                try
                {
                    return JsonUtility.FromJson<EnchantData>(enchantJson);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to parse enchant data: {e.Message}");
                    return default(EnchantData);
                }
            }
            
            return default(EnchantData);
        }
        
        /// <summary>
        /// 아이템에 인챈트 적용 가능 여부 검사
        /// </summary>
        private bool CanApplyEnchant(ItemInstance item, EnchantData enchant)
        {
            // 장비 아이템만 인챈트 가능
            if (item.ItemData.ItemType != ItemType.Equipment)
            {
                return false;
            }
            
            // 현재 인챈트 목록 가져오기
            var currentEnchants = GetItemEnchants(item);
            
            // 최대 인챈트 수 체크
            if (currentEnchants.Count >= maxEnchantsPerItem)
            {
                Debug.LogWarning($"Item already has maximum enchants ({maxEnchantsPerItem})");
                return false;
            }
            
            // 동일한 인챈트 타입 중복 체크
            if (!allowEnchantOverwrite && currentEnchants.Any(e => e.enchantType == enchant.enchantType))
            {
                Debug.LogWarning($"Item already has {enchant.enchantType} enchant");
                return false;
            }
            
            // 인챈트 타입별 아이템 호환성 체크
            return IsEnchantCompatible(item, enchant);
        }
        
        /// <summary>
        /// 인챈트와 아이템의 호환성 검사
        /// </summary>
        private bool IsEnchantCompatible(ItemInstance item, EnchantData enchant)
        {
            var weaponCategory = item.ItemData.WeaponCategory;
            var equipSlot = item.ItemData.EquipmentSlot;
            
            switch (enchant.enchantType)
            {
                // 무기 전용 인챈트
                case EnchantType.Sharpness:
                case EnchantType.CriticalHit:
                case EnchantType.LifeSteal:
                    return weaponCategory != WeaponCategory.None;
                
                // 방어구 전용 인챈트
                case EnchantType.Protection:
                case EnchantType.Thorns:
                    return equipSlot == EquipmentSlot.Head || 
                           equipSlot == EquipmentSlot.Chest || 
                           equipSlot == EquipmentSlot.Legs || 
                           equipSlot == EquipmentSlot.Feet;
                
                // 범용 인챈트
                case EnchantType.Regeneration:
                case EnchantType.Fortune:
                case EnchantType.Speed:
                case EnchantType.Durability:
                    return true;
                
                // 마법 무기/장비 전용
                case EnchantType.MagicBoost:
                    return weaponCategory == WeaponCategory.Staff || 
                           equipSlot == EquipmentSlot.Ring1 || 
                           equipSlot == EquipmentSlot.Ring2 || 
                           equipSlot == EquipmentSlot.Necklace;
                
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// 아이템에 인챈트 추가
        /// </summary>
        private bool AddEnchantToItem(ItemInstance item, EnchantData enchant)
        {
            var enchantments = item.Enchantments?.ToList() ?? new List<string>();
            
            // 동일한 인챈트 타입이 있으면 교체 (덮어쓰기 허용 시)
            if (allowEnchantOverwrite)
            {
                var existingIndex = enchantments.FindIndex(e => 
                {
                    try
                    {
                        var existing = JsonUtility.FromJson<EnchantData>(e);
                        return existing.enchantType == enchant.enchantType;
                    }
                    catch
                    {
                        return false;
                    }
                });
                
                if (existingIndex >= 0)
                {
                    enchantments[existingIndex] = JsonUtility.ToJson(enchant);
                    // ItemInstance의 enchantments 배열 업데이트
                    UpdateItemEnchantments(item, enchantments);
                    return true;
                }
            }
            
            // 새 인챈트 추가
            string enchantJson = JsonUtility.ToJson(enchant);
            enchantments.Add(enchantJson);
            
            // ItemInstance의 enchantments 배열 업데이트
            UpdateItemEnchantments(item, enchantments);
            
            return true;
        }
        
        /// <summary>
        /// ItemInstance의 인챈트 배열 업데이트 (리플렉션 사용)
        /// </summary>
        private void UpdateItemEnchantments(ItemInstance item, List<string> enchantments)
        {
            var itemType = typeof(ItemInstance);
            var enchantField = itemType.GetField("enchantments", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (enchantField != null)
            {
                enchantField.SetValue(item, enchantments.ToArray());
            }
        }
        
        /// <summary>
        /// 아이템의 인챈트 목록 가져오기
        /// </summary>
        public List<EnchantData> GetItemEnchants(ItemInstance item)
        {
            var enchants = new List<EnchantData>();
            
            if (item?.Enchantments != null)
            {
                foreach (string enchantJson in item.Enchantments)
                {
                    try
                    {
                        var enchant = JsonUtility.FromJson<EnchantData>(enchantJson);
                        enchants.Add(enchant);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to parse enchant: {e.Message}");
                    }
                }
            }
            
            return enchants;
        }
        
        /// <summary>
        /// 아이템에서 인챈트 제거
        /// </summary>
        public bool RemoveEnchantFromItem(ItemInstance item, EnchantType enchantType)
        {
            if (!IsOwner || item?.Enchantments == null) return false;
            
            var enchantments = item.Enchantments.ToList();
            
            for (int i = enchantments.Count - 1; i >= 0; i--)
            {
                try
                {
                    var enchant = JsonUtility.FromJson<EnchantData>(enchantments[i]);
                    if (enchant.enchantType == enchantType)
                    {
                        enchantments.RemoveAt(i);
                        UpdateItemEnchantments(item, enchantments);
                        OnEnchantRemoved?.Invoke(item, enchant);
                        
                        // 착용 중인 아이템이면 효과 재계산
                        if (equipmentManager != null && equipmentManager.IsItemEquipped(item))
                        {
                            RecalculateEnchantEffects();
                        }
                        
                        return true;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to parse enchant for removal: {e.Message}");
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 장비 변경 시 인챈트 효과 재계산
        /// </summary>
        private void OnEquipmentChangedHandler(EquipmentSlot slot, ItemInstance item)
        {
            RecalculateEnchantEffects();
        }
        
        /// <summary>
        /// 착용 중인 모든 장비의 인챈트 효과 재계산
        /// </summary>
        public void RecalculateEnchantEffects()
        {
            if (!IsOwner || equipmentManager == null) return;
            
            // 기존 효과 초기화
            activeEnchantEffects.Clear();
            
            // 착용 중인 모든 아이템의 인챈트 수집
            var equippedItems = equipmentManager.GetAllEquippedItems();
            
            foreach (var item in equippedItems)
            {
                if (item == null) continue;
                
                var enchants = GetItemEnchants(item);
                foreach (var enchant in enchants)
                {
                    // 동일한 인챈트 타입의 효과는 누적
                    if (activeEnchantEffects.ContainsKey(enchant.enchantType))
                    {
                        activeEnchantEffects[enchant.enchantType] += enchant.power;
                    }
                    else
                    {
                        activeEnchantEffects[enchant.enchantType] = enchant.power;
                    }
                }
            }
            
            // 계산된 효과를 스탯 시스템에 적용
            ApplyEnchantEffectsToStats();
            
            OnEnchantEffectsUpdated?.Invoke();
            
            Debug.Log($"🔮 Recalculated enchant effects: {activeEnchantEffects.Count} active enchants");
        }
        
        /// <summary>
        /// 인챈트 효과를 스탯 시스템에 적용
        /// </summary>
        private void ApplyEnchantEffectsToStats()
        {
            if (statsManager == null) return;
            
            // 인챈트 보너스 스탯 계산
            var enchantStats = CalculateEnchantStatBonuses();
            
            // 플레이어 스탯에 인챈트 보너스 적용
            if (statsManager.CurrentStats != null)
            {
                statsManager.CurrentStats.SetEnchantBonusStats(enchantStats);
                statsManager.UpdateEquipmentStats(statsManager.CurrentStats.GetTotalEquipmentStats());
            }
        }
        
        /// <summary>
        /// 인챈트로부터 스탯 보너스 계산
        /// </summary>
        private StatBlock CalculateEnchantStatBonuses()
        {
            var enchantStats = new StatBlock();
            
            foreach (var effect in activeEnchantEffects)
            {
                switch (effect.Key)
                {
                    case EnchantType.Sharpness:
                        // 공격력 증가 - STR 보너스로 변환
                        enchantStats.strength += effect.Value * 0.5f; // 5% 공격력 = 2.5 STR
                        break;
                        
                    case EnchantType.Protection:
                        // 방어력 증가 - DEF 보너스로 변환
                        enchantStats.defense += effect.Value * 0.25f; // 4% 방어력 = 1 DEF
                        break;
                        
                    case EnchantType.Speed:
                        // 이동속도 증가 - AGI 보너스로 변환
                        enchantStats.agility += effect.Value * 0.3f; // 6% 속도 = 1.8 AGI
                        break;
                        
                    case EnchantType.MagicBoost:
                        // 마법 공격력 증가 - INT 보너스로 변환
                        enchantStats.intelligence += effect.Value * 0.35f; // 7% 마법력 = 2.45 INT
                        break;
                        
                    case EnchantType.Fortune:
                        // 행운 증가 - LUK 보너스
                        enchantStats.luck += effect.Value * 0.125f; // 8% 행운 = 1 LUK
                        break;
                        
                    // 기타 인챈트들은 특수 효과로 처리 (스탯 보너스 외)
                    case EnchantType.CriticalHit:
                    case EnchantType.LifeSteal:
                    case EnchantType.Thorns:
                    case EnchantType.Regeneration:
                    case EnchantType.Durability:
                        // 이들은 전투/기타 시스템에서 직접 참조
                        break;
                }
            }
            
            return enchantStats;
        }
        
        /// <summary>
        /// 특정 인챈트 효과 값 가져오기
        /// </summary>
        public float GetEnchantEffect(EnchantType enchantType)
        {
            return activeEnchantEffects.GetValueOrDefault(enchantType, 0f);
        }
        
        /// <summary>
        /// 모든 활성 인챈트 효과 가져오기
        /// </summary>
        public Dictionary<EnchantType, float> GetAllActiveEffects()
        {
            return new Dictionary<EnchantType, float>(activeEnchantEffects);
        }
        
        /// <summary>
        /// 인챈트 효과가 있는지 확인
        /// </summary>
        public bool HasEnchantEffect(EnchantType enchantType)
        {
            return activeEnchantEffects.ContainsKey(enchantType) && activeEnchantEffects[enchantType] > 0f;
        }
        
        /// <summary>
        /// 디버그: 현재 인챈트 효과 로그 출력
        /// </summary>
        [ContextMenu("Log Active Enchant Effects")]
        public void LogActiveEnchantEffects()
        {
            Debug.Log($"🔮 Active Enchant Effects for {name}:");
            
            if (activeEnchantEffects.Count == 0)
            {
                Debug.Log("  No active enchant effects");
                return;
            }
            
            foreach (var effect in activeEnchantEffects)
            {
                Debug.Log($"  {effect.Key}: +{effect.Value}");
            }
        }
        
        /// <summary>
        /// 인챈트 통계 정보
        /// </summary>
        public (int totalEnchants, int uniqueTypes, float totalPower) GetEnchantStatistics()
        {
            if (equipmentManager == null) return (0, 0, 0f);
            
            int totalEnchants = 0;
            var uniqueTypes = new HashSet<EnchantType>();
            float totalPower = 0f;
            
            var equippedItems = equipmentManager.GetAllEquippedItems();
            foreach (var item in equippedItems)
            {
                if (item == null) continue;
                
                var enchants = GetItemEnchants(item);
                totalEnchants += enchants.Count;
                
                foreach (var enchant in enchants)
                {
                    uniqueTypes.Add(enchant.enchantType);
                    totalPower += enchant.power;
                }
            }
            
            return (totalEnchants, uniqueTypes.Count, totalPower);
        }
    }
}