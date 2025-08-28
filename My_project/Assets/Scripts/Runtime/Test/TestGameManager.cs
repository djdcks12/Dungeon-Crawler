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
        [SerializeField] private MonsterEntitySpawner monsterSpawner;
        
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
                InitializeMonsterSpawner();
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
            
            // 테스트 아이템 생성 (I키 대신 T키 사용)
            if (Input.GetKeyDown(KeyCode.T))
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
            
            var inventoryManager = localPlayer.GetComponent<InventoryManager>();
            if (inventoryManager == null)
            {
                Debug.LogError("❌ InventoryManager not found on player. Make sure DungeonPlayer prefab has InventoryManager component.");
                return;
            }
            
            // 랜덤 아이템 생성 및 인벤토리 추가
            var testItem = ItemDatabase.GetRandomItemDrop();
            if (testItem != null && testItem.IsValid())
            {
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
                Debug.LogError("❌ Failed to create random test item");
            }
        }
        
        /// <summary>
        /// MonsterSpawner를 서버 권한으로 초기화
        /// </summary>
        private void InitializeMonsterSpawner()
        {
            if (!IsServer) return;
            
            Debug.Log("🏭 Initializing MonsterEntitySpawner with server authority...");
            
            if (monsterSpawner != null)
            {
                // MonsterEntitySpawner가 이미 있으면 서버에서 직접 제어
                Debug.Log($"🏭 Found existing MonsterEntitySpawner: {monsterSpawner.name}");
                bool spawnerIsServer = NetworkManager.Singleton != null ? NetworkManager.Singleton.IsServer : true;
                Debug.Log($"🏭 MonsterSpawner IsServer check: {spawnerIsServer}");
                
                // 서버에서 스폰 활성화
                monsterSpawner.SetSpawningEnabled(true);
            }
            else
            {
                Debug.LogWarning("🏭 MonsterEntitySpawner reference not set in TestGameManager");
            }
        }
        
        /// <summary>
        /// 서버에서 몬스터 수동 스폰 (치트 기능)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SpawnTestMonsterServerRpc()
        {
            if (!IsServer || monsterSpawner == null) return;
            
            Debug.Log("🧪 Spawning test monster via ServerRpc...");
            monsterSpawner.SpawnRandomMonsterEntity();
        }
        
        /// <summary>
        /// 몬스터 스폰 치트 (MonsterEntitySpawner 사용)
        /// </summary>
        [ContextMenu("Cheat: Spawn Monster")]
        public void CheatSpawnMonster()
        {
            if (!IsServer)
            {
                Debug.LogWarning("⚠️ Monster spawning can only be done on Server/Host");
                return;
            }
            
            // MonsterEntitySpawner 우선 시도
            if (monsterSpawner != null)
            {
                Debug.Log("👹 Attempting to spawn test monster using MonsterEntitySpawner...");
                monsterSpawner.SpawnRandomMonsterEntity();
                Debug.Log($"✅ Monster spawn requested via MonsterEntitySpawner. Active: {monsterSpawner.CurrentMonsterCount}/{monsterSpawner.MaxActiveMonsters}");
                return;
            }
            
            // 구형 MonsterSpawner 폴백
            var spawner = FindFirstObjectByType<MonsterSpawner>();
            if (spawner != null)
            {
                Debug.Log("👹 Attempting to spawn test monster using legacy MonsterSpawner...");
                
                // MonsterSpawner의 공개 메서드로 랜덤 몬스터 스폰
                spawner.SpawnRandomMonster();
                
                Debug.Log($"✅ Monster spawn requested. Active: {spawner.CurrentMonsterCount}/{spawner.MaxMonsters}");
            }
            else
            {
                Debug.LogWarning("⚠️ No MonsterEntitySpawner or MonsterSpawner found in scene.");
            }
        }
        
        /// <summary>
        /// 스폰된 몬스터의 서버 권한 상태 체크
        /// </summary>
        [ContextMenu("Test: Check Monster Server Authority")]
        public void TestMonsterServerAuthority()
        {
            var monsters = FindObjectsByType<MonsterEntity>(FindObjectsSortMode.None);
            if (monsters.Length == 0)
            {
                Debug.LogWarning("🔍 No MonsterEntity found in scene");
                return;
            }
            
            bool isServer = NetworkManager.Singleton != null ? NetworkManager.Singleton.IsServer : true;
            
            Debug.Log($"🔍 Found {monsters.Length} MonsterEntity objects:");
            for (int i = 0; i < monsters.Length; i++)
            {
                var monster = monsters[i];
                var networkObject = monster.GetComponent<NetworkObject>();
                
                Debug.Log($"🔍 Monster {i}: {monster.name}");
                Debug.Log($"🔍   - IsServer: {isServer}");
                Debug.Log($"🔍   - NetworkObject: {networkObject != null}");
                if (networkObject != null)
                {
                    Debug.Log($"🔍   - NetworkObjectId: {networkObject.NetworkObjectId}");
                    Debug.Log($"🔍   - OwnerClientId: {networkObject.OwnerClientId}");
                }
                Debug.Log($"🔍   - VariantName: {monster.VariantData?.variantName ?? "NULL"}");
                Debug.Log($"🔍   - RaceName: {monster.RaceData?.raceName ?? "NULL"}");
                Debug.Log($"🔍   - Current HP: {monster.CurrentHP}/{monster.MaxHP}");
                Debug.Log($"🔍   - IsDead: {monster.IsDead}");
            }
        }
        
        /// <summary>
        /// 몬스터 데미지 테스트 (서버 권한 검증용)
        /// </summary>
        [ContextMenu("Test: Damage Monster")]
        public void TestDamageMonster()
        {
            if (!IsServer)
            {
                Debug.LogWarning("⚠️ Damage test can only be done on Server/Host");
                return;
            }
            
            var monsters = FindObjectsByType<MonsterEntity>(FindObjectsSortMode.None);
            if (monsters.Length == 0)
            {
                Debug.LogWarning("🔍 No MonsterEntity found for damage test");
                return;
            }
            
            var monster = monsters[0];
            var localPlayer = GetLocalPlayer();
            
            Debug.Log($"🗡️ Testing damage on monster: {monster.name}");
            bool isServer = NetworkManager.Singleton != null ? NetworkManager.Singleton.IsServer : true;
            Debug.Log($"🗡️ Monster IsServer: {isServer}, HP: {monster.CurrentHP}/{monster.MaxHP}");
            
            float testDamage = 50f;
            float actualDamage = monster.TakeDamage(testDamage, DamageType.Physical, localPlayer);
            
            Debug.Log($"🗡️ Damage test result: {testDamage} requested → {actualDamage} actual");
            Debug.Log($"🗡️ Monster HP after damage: {monster.CurrentHP}/{monster.MaxHP}");
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