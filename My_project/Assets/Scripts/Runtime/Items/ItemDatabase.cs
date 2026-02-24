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
            BuildIndexes();
            
            isInitialized = true;
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
                }
                else
                {
                    Debug.LogWarning($"⚠️ Invalid ItemData: {(item != null ? item.name : "null")}");
                }
            }
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
                results = results.Where(item => item.ItemName.IndexOf(searchTerm, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                              item.Description.IndexOf(searchTerm, System.StringComparison.OrdinalIgnoreCase) >= 0);
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