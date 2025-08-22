using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 테스트 환경용 게임 매니저
    /// 개발자 치트, 테스트 시나리오 실행을 위한 매니저
    /// </summary>
    public class TestGameManager : NetworkBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool enableCheatCodes = true;
        [SerializeField] private KeyCode cheatMenuKey = KeyCode.F1;
        
        [Header("Test References")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private DebugUI debugUI;
        
        private bool isCheatMenuVisible = false;
        
        public static TestGameManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsServer)
            {
                Debug.Log("🎮 Test Game Manager spawned on Server");
                InitializeTestEnvironment();
            }
            else
            {
                Debug.Log("🎮 Test Game Manager spawned on Client");
            }
        }
        
        private void Update()
        {
            if (enableCheatCodes && Input.GetKeyDown(cheatMenuKey))
            {
                ToggleCheatMenu();
            }
            
            HandleCheatInputs();
        }
        
        /// <summary>
        /// 테스트 환경 초기화
        /// </summary>
        private void InitializeTestEnvironment()
        {
            Debug.Log("🔧 Initializing Test Environment...");
            
            // 기본 테스트 아이템들 생성
            InitializeTestItems();
            
            // 테스트용 몬스터 스폰 포인트 설정
            SetupTestSpawnPoints();
        }
        
        /// <summary>
        /// 테스트용 아이템들 초기화
        /// </summary>
        private void InitializeTestItems()
        {
            // ItemDatabase 초기화 확인
            ItemDatabase.Initialize();
            Debug.Log("📦 ItemDatabase initialized for testing");
        }
        
        /// <summary>
        /// 테스트 스폰 포인트 설정
        /// </summary>
        private void SetupTestSpawnPoints()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("⚠️ No spawn points configured for testing");
                return;
            }
            
            Debug.Log($"📍 {spawnPoints.Length} spawn points ready for testing");
        }
        
        /// <summary>
        /// 치트 메뉴 토글
        /// </summary>
        private void ToggleCheatMenu()
        {
            isCheatMenuVisible = !isCheatMenuVisible;
            
            if (debugUI != null)
            {
                debugUI.SetCheatMenuVisible(isCheatMenuVisible);
            }
        }
        
        /// <summary>
        /// 치트 입력 처리
        /// </summary>
        private void HandleCheatInputs()
        {
            if (!enableCheatCodes) return;
            
            // 레벨업 치트
            if (Input.GetKeyDown(KeyCode.L))
            {
                CheatLevelUp();
            }
            
            // 골드 추가 치트
            if (Input.GetKeyDown(KeyCode.G))
            {
                CheatAddGold(1000);
            }
            
            // 테스트 아이템 생성
            if (Input.GetKeyDown(KeyCode.I))
            {
                CheatCreateTestItem();
            }
            
            // 몬스터 스폰
            if (Input.GetKeyDown(KeyCode.M))
            {
                CheatSpawnMonster();
            }
            
            // 체력 회복
            if (Input.GetKeyDown(KeyCode.H))
            {
                CheatHealPlayer();
            }
        }
        
        /// <summary>
        /// 레벨업 치트
        /// </summary>
        [ContextMenu("Cheat: Level Up")]
        public void CheatLevelUp()
        {
            var localPlayer = GetLocalPlayer();
            if (localPlayer != null)
            {
                var statsManager = localPlayer.GetComponent<PlayerStatsManager>();
                if (statsManager != null)
                {
                    statsManager.AddExperience(10000); // 충분한 경험치 지급
                    Debug.Log("⬆️ Level Up cheat activated");
                }
            }
        }
        
        /// <summary>
        /// 골드 추가 치트
        /// </summary>
        public void CheatAddGold(long amount)
        {
            var localPlayer = GetLocalPlayer();
            if (localPlayer != null)
            {
                var statsManager = localPlayer.GetComponent<PlayerStatsManager>();
                if (statsManager != null)
                {
                    statsManager.AddGold(amount);
                    Debug.Log($"💰 Added {amount} gold via cheat");
                }
            }
        }
        
        /// <summary>
        /// 테스트 아이템 생성 치트
        /// </summary>
        [ContextMenu("Cheat: Create Test Item")]
        public void CheatCreateTestItem()
        {
            var localPlayer = GetLocalPlayer();
            if (localPlayer == null)
            {
                Debug.LogError("❌ Local player not found for item creation");
                return;
            }

            Debug.Log("🔍 Searching for InventoryManager...");
            
            // InventoryManager 찾기 또는 자동 추가
            var inventoryManager = localPlayer.GetComponent<InventoryManager>();
            if (inventoryManager == null)
            {
                Debug.LogWarning("⚠️ InventoryManager not found, adding automatically...");
                inventoryManager = localPlayer.gameObject.AddComponent<InventoryManager>();
                Debug.Log("✅ InventoryManager added to player");
                
                // InventoryManager가 NetworkBehaviour이므로 OnNetworkSpawn을 기다려야 함
                // 임시로 다음 프레임까지 기다림
                StartCoroutine(DelayedItemCreation(inventoryManager));
                return;
            }
            
            // InventoryManager가 이미 있는 경우 바로 진행
            CreateTestItemsWithInventory(inventoryManager);
        }
        
        /// <summary>
        /// 지연된 아이템 생성 (새로 추가된 InventoryManager용)
        /// </summary>
        private System.Collections.IEnumerator DelayedItemCreation(InventoryManager inventoryManager)
        {
            // 다음 프레임까지 대기 (NetworkBehaviour 초기화 시간 확보)
            yield return null;
            
            Debug.Log("🔄 Delayed item creation starting...");
            CreateTestItemsWithInventory(inventoryManager);
        }
        
        /// <summary>
        /// InventoryManager를 사용하여 테스트 아이템 생성
        /// </summary>
        private void CreateTestItemsWithInventory(InventoryManager inventoryManager)
        {
            // ItemDatabase 초기화 (isInitialized는 private이므로 Initialize 직접 호출)
            Debug.Log("🔄 Initializing ItemDatabase...");
            ItemDatabase.Initialize();
            
            // ItemDatabase 통계 먼저 출력
            ItemDatabase.LogStatistics();
            
            // 모든 아이템 목록 확인
            var allItems = ItemDatabase.GetAllItems();
            Debug.Log($"📋 Found {allItems.Count} items in database:");
            foreach (var item in allItems.Take(3))  // 처음 3개만 표시
            {
                Debug.Log($"  - {item.ItemId}: {item.ItemName} (Grade: {item.Grade})");
            }
            
            // 랜덤 아이템 생성 시도 - 더 자세한 디버깅
            Debug.Log("🎲 Attempting to create random item...");
            
            // 등급별 아이템 수 확인
            Debug.Log("🔍 Items by grade:");
            for (int grade = 1; grade <= 5; grade++)
            {
                var gradeEnum = (ItemGrade)grade;
                var itemsOfGrade = ItemDatabase.GetItemsByGrade(gradeEnum);
                Debug.Log($"  {gradeEnum}: {itemsOfGrade.Count} items");
            }
            
            var testItem = ItemDatabase.GetRandomItemDrop();
            
            Debug.Log($"🔍 Random drop result: testItem = {(testItem != null ? "NOT NULL" : "NULL")}");
            
            if (testItem != null)
            {
                Debug.Log($"🔍 Item details: ID={testItem.ItemId}, IsValid={testItem.IsValid()}");
                
                if (testItem.ItemData != null)
                {
                    Debug.Log($"🔍 ItemData: {testItem.ItemData.ItemName} (Grade: {testItem.ItemData.Grade})");
                }
                else
                {
                    Debug.LogError("❌ testItem.ItemData is NULL!");
                }
                
                if (testItem.IsValid())
                {
                    Debug.Log("🔍 Item is valid, adding to inventory...");
                    bool success = inventoryManager.AddItem(testItem);
                    if (success)
                    {
                        Debug.Log($"🎁 SUCCESS: Created test item: {testItem.ItemData.ItemName} (Grade: {testItem.ItemData.Grade})");
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Failed to add item to inventory: {testItem.ItemData.ItemName}");
                    }
                }
                else
                {
                    Debug.LogError("❌ testItem.IsValid() returned false!");
                }
            }
            else
            {
                Debug.LogError("❌ GetRandomItemDrop() returned null - Creating specific test items...");
                
                // 대체 방안: ItemDatabase에서 직접 아이템 생성
                CreateSpecificTestItems(inventoryManager);
            }
        }
        
        /// <summary>
        /// 특정 테스트 아이템들 생성 (더 안정적인 방법)
        /// </summary>
        private void CreateSpecificTestItems(InventoryManager inventoryManager)
        {
            try
            {
                Debug.Log("🔧 Creating specific test items...");
                
                // ItemDatabase에서 특정 아이템들을 직접 생성
                string[] testItemIds = { 
                    "weapon_sword_basic",    // 낡은 검
                    "armor_helmet_basic",    // 가죽 모자  
                    "consumable_hp_small",   // 소형 체력 포션
                    "material_iron_ore"      // 철광석
                };
                
                bool anySuccess = false;
                
                foreach (string itemId in testItemIds)
                {
                    Debug.Log($"🔍 Trying to create item: {itemId}");
                    
                    // ItemData 먼저 확인
                    var itemData = ItemDatabase.GetItem(itemId);
                    if (itemData == null)
                    {
                        Debug.LogError($"❌ ItemData not found for {itemId}");
                        continue;
                    }
                    
                    Debug.Log($"✅ Found ItemData: {itemData.ItemName}");
                    
                    var testItem = ItemDatabase.CreateItemInstance(itemId);
                    if (testItem == null)
                    {
                        Debug.LogError($"❌ CreateItemInstance returned null for {itemId}");
                        continue;
                    }
                    
                    Debug.Log($"✅ ItemInstance created for {itemId}");
                    
                    if (!testItem.IsValid())
                    {
                        Debug.LogError($"❌ ItemInstance.IsValid() failed for {itemId}");
                        continue;
                    }
                    
                    Debug.Log($"✅ ItemInstance is valid for {itemId}");
                    
                    bool success = inventoryManager.AddItem(testItem);
                    if (success)
                    {
                        Debug.Log($"🎁 SUCCESS: Created {testItem.ItemData.ItemName}");
                        anySuccess = true;
                    }
                    else
                    {
                        Debug.LogError($"❌ InventoryManager.AddItem failed for {itemId}");
                    }
                }
                
                if (!anySuccess)
                {
                    Debug.LogError("❌ All specific item creation failed - ItemDatabase might have issues");
                    // ItemDatabase 통계 출력으로 디버깅
                    ItemDatabase.LogStatistics();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Exception during specific item creation: {e.Message}");
            }
        }
        
        /// <summary>
        /// 몬스터 스폰 치트
        /// </summary>
        [ContextMenu("Cheat: Spawn Monster")]
        public void CheatSpawnMonster()
        {
            var spawner = FindFirstObjectByType<MonsterSpawner>();
            if (spawner != null)
            {
                // 테스트용 몬스터 스폰 시도
                Debug.Log("👹 Attempting to spawn test monster...");
                // MonsterSpawner의 공개 메서드가 있다면 호출
            }
            else
            {
                Debug.LogWarning("⚠️ No MonsterSpawner found for cheat");
            }
        }
        
        /// <summary>
        /// 플레이어 체력 회복 치트
        /// </summary>
        [ContextMenu("Cheat: Heal Player")]
        public void CheatHealPlayer()
        {
            var localPlayer = GetLocalPlayer();
            if (localPlayer != null)
            {
                var statsManager = localPlayer.GetComponent<PlayerStatsManager>();
                if (statsManager != null)
                {
                    statsManager.Heal(9999f); // 최대 힐
                    statsManager.RestoreMP(9999f); // 최대 마나 회복
                    Debug.Log("💚 Player fully healed via cheat");
                }
            }
        }
        
        /// <summary>
        /// 로컬 플레이어 가져오기
        /// </summary>
        private PlayerController GetLocalPlayer()
        {
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                var networkBehaviour = player.GetComponent<NetworkBehaviour>();
                if (networkBehaviour != null && networkBehaviour.IsLocalPlayer)
                {
                    return player;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 네트워크 상태 정보 가져오기
        /// </summary>
        public string GetNetworkStatusInfo()
        {
            if (!NetworkManager.Singleton.IsListening)
                return "❌ Network Not Started";
                
            if (NetworkManager.Singleton.IsHost)
                return $"🔸 Host (Players: {NetworkManager.Singleton.ConnectedClients.Count})";
                
            if (NetworkManager.Singleton.IsServer)
                return $"🔹 Server (Players: {NetworkManager.Singleton.ConnectedClients.Count})";
                
            if (NetworkManager.Singleton.IsClient)
                return "🔸 Client Connected";
                
            return "❓ Unknown State";
        }
        
        /// <summary>
        /// 모든 플레이어에게 테스트 메시지 전송
        /// </summary>
        [ContextMenu("Test: Broadcast Message")]
        public void TestBroadcastMessage()
        {
            if (IsServer)
            {
                TestMessageClientRpc("🧪 Test message from server!");
            }
        }
        
        [ClientRpc]
        private void TestMessageClientRpc(string message)
        {
            Debug.Log($"📢 Received: {message}");
        }
        
        private void OnGUI()
        {
            if (!enableCheatCodes || !isCheatMenuVisible) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("🧪 Test Cheat Menu", new GUIStyle(GUI.skin.label) { fontSize = 16 });
            GUILayout.Space(10);
            
            if (GUILayout.Button("Level Up (L)"))
                CheatLevelUp();
                
            if (GUILayout.Button("Add 1000 Gold (G)"))
                CheatAddGold(1000);
                
            if (GUILayout.Button("Create Test Item (I)"))
                CheatCreateTestItem();
                
            if (GUILayout.Button("Spawn Monster (M)"))
                CheatSpawnMonster();
                
            if (GUILayout.Button("Heal Player (H)"))
                CheatHealPlayer();
                
            GUILayout.Space(10);
            
            if (GUILayout.Button("Test Broadcast"))
                TestBroadcastMessage();
            
            GUILayout.Label($"Network: {GetNetworkStatusInfo()}");
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}