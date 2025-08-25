using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 게임 내 모든 아이템 데이터를 관리하는 데이터베이스
    /// Resources 폴더에서 자동으로 아이템들을 로드하고 관리
    /// </summary>
    public static class ItemDatabase
    {
        private static Dictionary<string, ItemData> itemDatabase = new Dictionary<string, ItemData>();
        private static Dictionary<ItemGrade, List<ItemData>> itemsByGrade = new Dictionary<ItemGrade, List<ItemData>>();
        private static Dictionary<ItemType, List<ItemData>> itemsByType = new Dictionary<ItemType, List<ItemData>>();
        private static Dictionary<EquipmentSlot, List<ItemData>> itemsBySlot = new Dictionary<EquipmentSlot, List<ItemData>>();
        
        private static bool isInitialized = false;
        
        /// <summary>
        /// 데이터베이스 초기화
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized) return;
            
            LoadAllItems();
            CreateDefaultItems();
            BuildIndexes();
            
            isInitialized = true;
            Debug.Log($"ItemDatabase initialized with {itemDatabase.Count} items");
        }
        
        /// <summary>
        /// Resources에서 모든 ItemData 로드
        /// </summary>
        private static void LoadAllItems()
        {
            ItemData[] allItems = Resources.LoadAll<ItemData>("Items");
            
            Debug.Log($"🔍 Loading {allItems.Length} ItemData assets from Resources/Items/");
            
            foreach (var item in allItems)
            {
                if (item != null && !string.IsNullOrEmpty(item.ItemId))
                {
                    itemDatabase[item.ItemId] = item;
                    Debug.Log($"📦 Loaded: {item.ItemName} (ID: {item.ItemId}) - Icon: {(item.ItemIcon != null ? "✅" : "❌")}");
                    
                    if (item.ItemIcon != null)
                    {
                        Debug.Log($"   🖼️ Icon: {item.ItemIcon.name} ({item.ItemIcon.texture.width}x{item.ItemIcon.texture.height})");
                    }
                }
                else
                {
                    Debug.LogWarning($"⚠️ Invalid ItemData: {(item != null ? item.name : "null")}");
                }
            }
        }
        
        /// <summary>
        /// 기본 아이템들 생성 (하드코딩된 기본 아이템들)
        /// </summary>
        private static void CreateDefaultItems()
        {
            // ScriptableObject 에셋들을 우선 사용하고, 없는 아이템들만 하드코딩으로 생성
            Debug.Log("🔧 Creating fallback items (only if not loaded from assets)");
            
            // 기본 무기들 (에셋에 없으면 생성)
            if (!itemDatabase.ContainsKey("weapon_sword_basic"))
                CreateBasicWeapons();
            
            // 기본 방어구들 (에셋에 없으면 생성)  
            if (!itemDatabase.ContainsKey("armor_helmet_basic"))
                CreateBasicArmors();
            
            // 기본 소모품들 (에셋에 없으면 생성)
            if (!itemDatabase.ContainsKey("consumable_health_potion"))
                CreateBasicConsumables();
            
            // 기본 재료들은 스킵 (ScriptableObject 에셋 사용)
            // CreateBasicMaterials(); // 주석 처리
        }
        
        /// <summary>
        /// 기본 무기 생성
        /// </summary>
        private static void CreateBasicWeapons()
        {
            // 1등급 검
            var basicSword = CreateItem("weapon_sword_basic", "낡은 검", "초보자를 위한 기본적인 검이다.", 
                ItemType.Equipment, ItemGrade.Common, EquipmentSlot.MainHand, WeaponCategory.Sword,
                new StatBlock { strength = 2 }, new DamageRange(8, 12, 0), 100);
                
            // 1등급 활
            var basicBow = CreateItem("weapon_bow_basic", "낡은 활", "초보자를 위한 기본적인 활이다.", 
                ItemType.Equipment, ItemGrade.Common, EquipmentSlot.TwoHand, WeaponCategory.Bow,
                new StatBlock { agility = 2 }, new DamageRange(6, 10, 0), 80);
                
            // 1등급 지팡이
            var basicStaff = CreateItem("weapon_staff_basic", "낡은 지팡이", "초보자를 위한 기본적인 지팡이다.", 
                ItemType.Equipment, ItemGrade.Common, EquipmentSlot.TwoHand, WeaponCategory.Staff,
                new StatBlock { intelligence = 2 }, new DamageRange(5, 8, 0), 90);
                
            // 2등급 검
            var uncommonSword = CreateItem("weapon_sword_uncommon", "강철 검", "잘 단련된 강철로 만든 검이다.", 
                ItemType.Equipment, ItemGrade.Uncommon, EquipmentSlot.MainHand, WeaponCategory.Sword,
                new StatBlock { strength = 5, defense = 1 }, new DamageRange(15, 20, 0), 300);
                
            // 3등급 검
            var rareSword = CreateItem("weapon_sword_rare", "마법 검", "마법의 힘이 깃든 희귀한 검이다.", 
                ItemType.Equipment, ItemGrade.Rare, EquipmentSlot.MainHand, WeaponCategory.Sword,
                new StatBlock { strength = 8, intelligence = 3 }, new DamageRange(25, 35, 10), 1000);
        }
        
        /// <summary>
        /// 기본 방어구 생성
        /// </summary>
        private static void CreateBasicArmors()
        {
            // 1등급 헬멧
            var basicHelmet = CreateItem("armor_helmet_basic", "가죽 모자", "기본적인 가죽으로 만든 모자이다.", 
                ItemType.Equipment, ItemGrade.Common, EquipmentSlot.Head, WeaponCategory.None,
                new StatBlock { defense = 2, vitality = 1 }, new DamageRange(0, 0, 0), 50);
                
            // 1등급 갑옷
            var basicChest = CreateItem("armor_chest_basic", "가죽 갑옷", "기본적인 가죽으로 만든 갑옷이다.", 
                ItemType.Equipment, ItemGrade.Common, EquipmentSlot.Chest, WeaponCategory.None,
                new StatBlock { defense = 5, vitality = 2 }, new DamageRange(0, 0, 0), 100);
                
            // 2등급 갑옷
            var uncommonChest = CreateItem("armor_chest_uncommon", "강철 갑옷", "단단한 강철로 만든 갑옷이다.", 
                ItemType.Equipment, ItemGrade.Uncommon, EquipmentSlot.Chest, WeaponCategory.None,
                new StatBlock { defense = 10, vitality = 5, strength = 2 }, new DamageRange(0, 0, 0), 500);
        }
        
        /// <summary>
        /// 기본 소모품 생성
        /// </summary>
        private static void CreateBasicConsumables()
        {
            // 소형 체력 포션
            var smallHpPotion = CreateItem("consumable_hp_small", "소형 체력 포션", "체력을 50 회복시켜준다.", 
                ItemType.Consumable, ItemGrade.Common, EquipmentSlot.None, WeaponCategory.None,
                new StatBlock(), new DamageRange(0, 0, 0), 20);
            smallHpPotion.GetType().GetField("healAmount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(smallHpPotion, 50f);
                
            // 중형 체력 포션
            var mediumHpPotion = CreateItem("consumable_hp_medium", "중형 체력 포션", "체력을 150 회복시켜준다.", 
                ItemType.Consumable, ItemGrade.Uncommon, EquipmentSlot.None, WeaponCategory.None,
                new StatBlock(), new DamageRange(0, 0, 0), 100);
            mediumHpPotion.GetType().GetField("healAmount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(mediumHpPotion, 150f);
                
            // 소형 마나 포션
            var smallMpPotion = CreateItem("consumable_mp_small", "소형 마나 포션", "마나를 30 회복시켜준다.", 
                ItemType.Consumable, ItemGrade.Common, EquipmentSlot.None, WeaponCategory.None,
                new StatBlock(), new DamageRange(0, 0, 0), 25);
            smallMpPotion.GetType().GetField("manaAmount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(smallMpPotion, 30f);
        }
        
        /// <summary>
        /// 기본 재료 생성 (ScriptableObject 에셋 사용으로 비활성화)
        /// </summary>
        private static void CreateBasicMaterials()
        {
            // ScriptableObject 에셋들을 사용하므로 하드코딩된 재료 생성 불필요
            Debug.Log("📦 Skipping hardcoded material creation - using ScriptableObject assets instead");
            
            // 필요시 여기서 에셋이 제대로 로드되었는지 확인
            if (itemDatabase.ContainsKey("material_iron_ore"))
            {
                Debug.Log("✅ 철광석 에셋 로드 확인됨");
            }
            
            if (itemDatabase.ContainsKey("material_magic_stone"))
            {
                Debug.Log("✅ 마법석 에셋 로드 확인됨");  
            }
        }
        
        /// <summary>
        /// 아이템 생성 헬퍼 메서드
        /// </summary>
        private static ItemData CreateItem(string id, string name, string description, 
            ItemType type, ItemGrade grade, EquipmentSlot slot, WeaponCategory weaponCategory,
            StatBlock stats, DamageRange damageRange, long price)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            
            // 리플렉션을 사용하여 private 필드 설정
            var itemType = typeof(ItemData);
            itemType.GetField("itemId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(item, id);
            itemType.GetField("itemName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(item, name);
            itemType.GetField("description", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(item, description);
            itemType.GetField("itemType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(item, type);
            itemType.GetField("grade", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(item, grade);
            itemType.GetField("equipmentSlot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(item, slot);
            itemType.GetField("weaponCategory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(item, weaponCategory);
            itemType.GetField("statBonuses", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(item, stats);
            itemType.GetField("weaponDamageRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(item, damageRange);
            itemType.GetField("sellPrice", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(item, price);
            itemType.GetField("maxDurability", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(item, type == ItemType.Equipment ? 100 : 0);
            itemType.GetField("durability", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(item, type == ItemType.Equipment ? 100 : 0);
            
            // 스택 사이즈 설정
            int stackSize = type switch
            {
                ItemType.Equipment => 1,
                ItemType.Consumable => 20,
                ItemType.Material => 99,
                _ => 1
            };
            itemType.GetField("stackSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(item, stackSize);
            
            return item;
        }
        
        /// <summary>
        /// 인덱스 구축
        /// </summary>
        private static void BuildIndexes()
        {
            // 등급별 인덱스
            itemsByGrade.Clear();
            foreach (ItemGrade grade in (ItemGrade[])System.Enum.GetValues(typeof(ItemGrade)))
            {
                itemsByGrade[grade] = new List<ItemData>();
            }
            
            // 타입별 인덱스
            itemsByType.Clear();
            foreach (ItemType type in (ItemType[])System.Enum.GetValues(typeof(ItemType)))
            {
                itemsByType[type] = new List<ItemData>();
            }
            
            // 슬롯별 인덱스
            itemsBySlot.Clear();
            foreach (EquipmentSlot slot in (EquipmentSlot[])System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                itemsBySlot[slot] = new List<ItemData>();
            }
            
            // 모든 아이템을 인덱스에 추가
            foreach (var item in itemDatabase.Values)
            {
                itemsByGrade[item.Grade].Add(item);
                itemsByType[item.ItemType].Add(item);
                itemsBySlot[item.EquipmentSlot].Add(item);
            }
        }
        
        /// <summary>
        /// 아이템 ID로 아이템 데이터 가져오기
        /// </summary>
        public static ItemData GetItem(string itemId)
        {
            if (!isInitialized) Initialize();
            
            return itemDatabase.TryGetValue(itemId, out ItemData item) ? item : null;
        }
        
        /// <summary>
        /// 새 아이템 인스턴스 생성
        /// </summary>
        public static ItemInstance CreateItemInstance(string itemId, int quantity = 1)
        {
            var itemData = GetItem(itemId);
            if (itemData == null) return null;
            
            return new ItemInstance(itemData, quantity);
        }
        
        /// <summary>
        /// 등급별 아이템 목록 가져오기
        /// </summary>
        public static List<ItemData> GetItemsByGrade(ItemGrade grade)
        {
            if (!isInitialized) Initialize();
            
            return itemsByGrade.TryGetValue(grade, out List<ItemData> items) ? items : new List<ItemData>();
        }
        
        /// <summary>
        /// 타입별 아이템 목록 가져오기
        /// </summary>
        public static List<ItemData> GetItemsByType(ItemType type)
        {
            if (!isInitialized) Initialize();
            
            return itemsByType.TryGetValue(type, out List<ItemData> items) ? items : new List<ItemData>();
        }
        
        /// <summary>
        /// 슬롯별 아이템 목록 가져오기
        /// </summary>
        public static List<ItemData> GetItemsBySlot(EquipmentSlot slot)
        {
            if (!isInitialized) Initialize();
            
            return itemsBySlot.TryGetValue(slot, out List<ItemData> items) ? items : new List<ItemData>();
        }
        
        /// <summary>
        /// 모든 아이템 목록 가져오기
        /// </summary>
        public static List<ItemData> GetAllItems()
        {
            if (!isInitialized) Initialize();
            
            return itemDatabase.Values.ToList();
        }
        
        /// <summary>
        /// 랜덤 아이템 드롭 (등급 기반)
        /// </summary>
        public static ItemInstance GetRandomItemDrop(ItemGrade maxGrade = ItemGrade.Legendary)
        {
            if (!isInitialized) Initialize();
            
            // 등급별 확률로 등급 결정
            ItemGrade selectedGrade = ItemGrade.Common;
            float random = Random.Range(0f, 1f);
            float cumulativeProbability = 0f;
            
            for (int grade = 1; grade <= (int)maxGrade; grade++)
            {
                ItemGrade currentGrade = (ItemGrade)grade;
                float probability = ItemData.GetGradeDropRate(currentGrade);
                cumulativeProbability += probability;
                
                if (random <= cumulativeProbability)
                {
                    selectedGrade = currentGrade;
                    break;
                }
            }
            
            // 선택된 등급의 아이템 중 랜덤 선택
            var itemsOfGrade = GetItemsByGrade(selectedGrade);
            if (itemsOfGrade.Count == 0) return null;
            
            var randomItem = itemsOfGrade[Random.Range(0, itemsOfGrade.Count)];
            return new ItemInstance(randomItem);
        }
        
        /// <summary>
        /// 특정 타입의 랜덤 아이템 드롭
        /// </summary>
        public static ItemInstance GetRandomItemDropByType(ItemType type, ItemGrade maxGrade = ItemGrade.Legendary)
        {
            if (!isInitialized) Initialize();
            
            var itemsOfType = GetItemsByType(type);
            if (itemsOfType.Count == 0) return null;
            
            // 등급 필터링
            var filteredItems = itemsOfType.Where(item => item.Grade <= maxGrade).ToList();
            if (filteredItems.Count == 0) return null;
            
            var randomItem = filteredItems[Random.Range(0, filteredItems.Count)];
            return new ItemInstance(randomItem);
        }
        
        /// <summary>
        /// 데이터베이스에 새 아이템 추가 (런타임)
        /// </summary>
        public static void AddItem(ItemData item)
        {
            if (item == null || string.IsNullOrEmpty(item.ItemId)) return;
            
            if (!isInitialized) Initialize();
            
            itemDatabase[item.ItemId] = item;
            
            // 인덱스 업데이트
            itemsByGrade[item.Grade].Add(item);
            itemsByType[item.ItemType].Add(item);
            itemsBySlot[item.EquipmentSlot].Add(item);
        }
        
        /// <summary>
        /// 아이템 검색
        /// </summary>
        public static List<ItemData> SearchItems(string searchTerm, ItemType? type = null, ItemGrade? grade = null)
        {
            if (!isInitialized) Initialize();
            
            var results = itemDatabase.Values.AsEnumerable();
            
            // 이름으로 검색
            if (!string.IsNullOrEmpty(searchTerm))
            {
                results = results.Where(item => item.ItemName.Contains(searchTerm) || 
                                              item.Description.Contains(searchTerm));
            }
            
            // 타입 필터
            if (type.HasValue)
            {
                results = results.Where(item => item.ItemType == type.Value);
            }
            
            // 등급 필터
            if (grade.HasValue)
            {
                results = results.Where(item => item.Grade == grade.Value);
            }
            
            return results.ToList();
        }
        
        /// <summary>
        /// 디버깅용 통계 정보
        /// </summary>
        public static void LogStatistics()
        {
            if (!isInitialized) Initialize();
            
            Debug.Log($"=== Item Database Statistics ===");
            Debug.Log($"Total Items: {itemDatabase.Count}");
            
            foreach (ItemGrade grade in (ItemGrade[])System.Enum.GetValues(typeof(ItemGrade)))
            {
                int count = itemsByGrade[grade].Count;
                Debug.Log($"{grade} Items: {count}");
            }
            
            foreach (ItemType type in (ItemType[])System.Enum.GetValues(typeof(ItemType)))
            {
                int count = itemsByType[type].Count;
                Debug.Log($"{type} Items: {count}");
            }
        }
    }
}