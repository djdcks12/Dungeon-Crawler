using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// ItemDatabase 초기화 헬퍼
    /// 테스트 환경에서 아이템 데이터베이스 초기화
    /// </summary>
    public class ItemDatabaseInitializer : MonoBehaviour
    {
        [Header("Initialization Settings")]
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private bool logInitialization = true;
        
        private void Start()
        {
            if (initializeOnStart)
            {
                InitializeDatabase();
            }
        }
        
        /// <summary>
        /// 아이템 데이터베이스 초기화
        /// </summary>
        [ContextMenu("Initialize Item Database")]
        public void InitializeDatabase()
        {
            try
            {
                ItemDatabase.Initialize();
                
                if (logInitialization)
                {
                    Debug.Log("✅ ItemDatabase initialized successfully");
                    
                    // 기본 아이템들이 로드되었는지 확인
                    var commonItems = ItemDatabase.GetItemsByGrade(ItemGrade.Common);
                    var rareItems = ItemDatabase.GetItemsByGrade(ItemGrade.Rare);
                    var epicItems = ItemDatabase.GetItemsByGrade(ItemGrade.Epic);
                    var legendaryItems = ItemDatabase.GetItemsByGrade(ItemGrade.Legendary);
                    
                    Debug.Log($"📦 Items loaded - Common: {commonItems.Count}, Rare: {rareItems.Count}, Epic: {epicItems.Count}, Legendary: {legendaryItems.Count}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Failed to initialize ItemDatabase: {e.Message}");
            }
        }
        
        /// <summary>
        /// 테스트용 아이템 생성
        /// </summary>
        [ContextMenu("Create Test Items")]
        public void CreateTestItems()
        {
            try
            {
                var testItems = new[]
                {
                    ItemDatabase.CreateItemInstance("potion_hp_small", 5),
                    ItemDatabase.CreateItemInstance("potion_mp_small", 3),
                    ItemDatabase.GetRandomItemDrop(ItemGrade.Rare),
                    ItemDatabase.GetRandomItemDrop(ItemGrade.Epic)
                };
                
                Debug.Log($"🧪 Created {testItems.Length} test items");
                
                foreach (var item in testItems)
                {
                    if (item != null)
                    {
                        Debug.Log($"  • {item.ItemData.ItemName} x{item.Quantity}");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Failed to create test items: {e.Message}");
            }
        }
    }
}