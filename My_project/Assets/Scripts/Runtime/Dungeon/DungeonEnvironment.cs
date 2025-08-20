using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 던전 환경 요소 관리 시스템
    /// 함정, 보물상자, 비밀 통로 등 인터랙티브 요소들을 관리
    /// </summary>
    public class DungeonEnvironment : NetworkBehaviour
    {
        [Header("환경 요소 설정")]
        [SerializeField] private bool enableTraps = true;
        [SerializeField] private bool enableTreasureChests = true;
        [SerializeField] private bool enableSecretPassages = true;
        [SerializeField] private bool enableDestructibleObjects = true;
        
        [Header("스폰 확률")]
        [SerializeField] private float trapSpawnChance = 0.3f;          // 30% 확률
        [SerializeField] private float chestSpawnChance = 0.2f;         // 20% 확률
        [SerializeField] private float secretPassageChance = 0.1f;      // 10% 확률
        [SerializeField] private float destructibleSpawnChance = 0.4f;  // 40% 확률
        
        [Header("환경 요소 프리팹")]
        [SerializeField] private GameObject[] trapPrefabs;
        [SerializeField] private GameObject[] chestPrefabs;
        [SerializeField] private GameObject[] secretPassagePrefabs;
        [SerializeField] private GameObject[] destructiblePrefabs;
        
        [Header("스폰 영역 설정")]
        [SerializeField] private Transform[] environmentSpawnPoints;
        [SerializeField] private float spawnRadius = 20f;
        [SerializeField] private int maxEnvironmentObjects = 50;
        
        // 환경 요소 관리
        private Dictionary<int, List<NetworkObject>> floorEnvironmentObjects = new Dictionary<int, List<NetworkObject>>();
        private List<DungeonTrap> activeTraps = new List<DungeonTrap>();
        private List<TreasureChest> activeChests = new List<TreasureChest>();
        private List<SecretPassage> activePassages = new List<SecretPassage>();
        
        // 네트워크 변수
        private NetworkVariable<int> totalEnvironmentObjects = new NetworkVariable<int>(0);
        
        // 이벤트
        public System.Action<DungeonTrap, PlayerController> OnTrapTriggered;
        public System.Action<TreasureChest, PlayerController> OnChestOpened;
        public System.Action<SecretPassage, PlayerController> OnSecretFound;
        
        // 싱글톤 패턴
        private static DungeonEnvironment instance;
        public static DungeonEnvironment Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<DungeonEnvironment>();
                }
                return instance;
            }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (instance == null)
            {
                instance = this;
            }
            
            if (IsServer)
            {
                // DungeonManager 이벤트 구독
                if (DungeonManager.Instance != null)
                {
                    DungeonManager.Instance.OnFloorChanged += OnFloorChanged;
                }
            }
        }
        
        public override void OnNetworkDespawn()
        {
            if (IsServer && DungeonManager.Instance != null)
            {
                DungeonManager.Instance.OnFloorChanged -= OnFloorChanged;
            }
            
            base.OnNetworkDespawn();
        }
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 층 변경 이벤트 처리
        /// </summary>
        private void OnFloorChanged(int newFloor)
        {
            if (!IsServer) return;
            
            // 이전 층 환경 요소 정리
            ClearPreviousFloorEnvironment();
            
            // 새 층에 환경 요소 생성
            StartCoroutine(GenerateFloorEnvironmentCoroutine(newFloor));
        }
        
        /// <summary>
        /// 이전 층 환경 요소 정리
        /// </summary>
        private void ClearPreviousFloorEnvironment()
        {
            // 활성화된 환경 요소들 정리
            activeTraps.Clear();
            activeChests.Clear();
            activePassages.Clear();
            
            // 네트워크 오브젝트들 정리
            foreach (var floorObjects in floorEnvironmentObjects.Values)
            {
                foreach (var obj in floorObjects)
                {
                    if (obj != null && obj.IsSpawned)
                    {
                        obj.Despawn();
                    }
                }
            }
            floorEnvironmentObjects.Clear();
        }
        
        /// <summary>
        /// 층별 환경 요소 생성 코루틴
        /// </summary>
        private IEnumerator GenerateFloorEnvironmentCoroutine(int floor)
        {
            yield return new WaitForSeconds(1f); // 층 로드 완료 대기
            
            if (!floorEnvironmentObjects.ContainsKey(floor))
            {
                floorEnvironmentObjects[floor] = new List<NetworkObject>();
            }
            
            // 층별 환경 요소 개수 계산
            int environmentCount = CalculateEnvironmentCount(floor);
            
            for (int i = 0; i < environmentCount; i++)
            {
                GenerateRandomEnvironmentObject(floor);
                yield return new WaitForSeconds(0.1f); // 프레임 분산
            }
            
            totalEnvironmentObjects.Value = GetTotalActiveObjects();
            
            Debug.Log($"🏛️ Floor {floor} environment generated: {environmentCount} objects");
        }
        
        /// <summary>
        /// 층별 환경 요소 개수 계산
        /// </summary>
        private int CalculateEnvironmentCount(int floor)
        {
            // 기본 개수 + 층수별 증가
            int baseCount = 10;
            int floorBonus = floor * 2;
            int maxForFloor = Mathf.Min(baseCount + floorBonus, maxEnvironmentObjects);
            
            return Random.Range(maxForFloor / 2, maxForFloor + 1);
        }
        
        /// <summary>
        /// 랜덤 환경 요소 생성
        /// </summary>
        private void GenerateRandomEnvironmentObject(int floor)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition();
            
            // 환경 요소 타입 결정
            EnvironmentType environmentType = DetermineEnvironmentType(floor);
            
            GameObject prefab = GetEnvironmentPrefab(environmentType);
            if (prefab == null) return;
            
            // 오브젝트 생성
            GameObject environmentObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
            
            // 네트워크 스폰
            NetworkObject networkObject = environmentObject.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
                floorEnvironmentObjects[floor].Add(networkObject);
                
                // 환경 요소별 설정
                ConfigureEnvironmentObject(environmentObject, environmentType, floor);
            }
        }
        
        /// <summary>
        /// 환경 요소 타입 결정
        /// </summary>
        private EnvironmentType DetermineEnvironmentType(int floor)
        {
            float random = Random.value;
            float cumulativeChance = 0f;
            
            if (enableTraps)
            {
                cumulativeChance += trapSpawnChance;
                if (random <= cumulativeChance)
                    return EnvironmentType.Trap;
            }
            
            if (enableTreasureChests)
            {
                cumulativeChance += chestSpawnChance;
                if (random <= cumulativeChance)
                    return EnvironmentType.TreasureChest;
            }
            
            if (enableSecretPassages && floor > 3) // 3층 이후부터 비밀 통로
            {
                cumulativeChance += secretPassageChance;
                if (random <= cumulativeChance)
                    return EnvironmentType.SecretPassage;
            }
            
            if (enableDestructibleObjects)
            {
                return EnvironmentType.DestructibleObject;
            }
            
            return EnvironmentType.Trap; // 기본값
        }
        
        /// <summary>
        /// 환경 요소 프리팹 가져오기
        /// </summary>
        private GameObject GetEnvironmentPrefab(EnvironmentType environmentType)
        {
            return environmentType switch
            {
                EnvironmentType.Trap => GetRandomPrefab(trapPrefabs),
                EnvironmentType.TreasureChest => GetRandomPrefab(chestPrefabs),
                EnvironmentType.SecretPassage => GetRandomPrefab(secretPassagePrefabs),
                EnvironmentType.DestructibleObject => GetRandomPrefab(destructiblePrefabs),
                _ => null
            };
        }
        
        /// <summary>
        /// 랜덤 프리팹 선택
        /// </summary>
        private GameObject GetRandomPrefab(GameObject[] prefabs)
        {
            if (prefabs == null || prefabs.Length == 0) return null;
            return prefabs[Random.Range(0, prefabs.Length)];
        }
        
        /// <summary>
        /// 환경 요소 설정
        /// </summary>
        private void ConfigureEnvironmentObject(GameObject environmentObject, EnvironmentType environmentType, int floor)
        {
            switch (environmentType)
            {
                case EnvironmentType.Trap:
                    ConfigureTrap(environmentObject, floor);
                    break;
                case EnvironmentType.TreasureChest:
                    ConfigureTreasureChest(environmentObject, floor);
                    break;
                case EnvironmentType.SecretPassage:
                    ConfigureSecretPassage(environmentObject, floor);
                    break;
                case EnvironmentType.DestructibleObject:
                    ConfigureDestructibleObject(environmentObject, floor);
                    break;
            }
        }
        
        /// <summary>
        /// 함정 설정
        /// </summary>
        private void ConfigureTrap(GameObject trapObject, int floor)
        {
            var trap = trapObject.GetComponent<DungeonTrap>();
            if (trap == null)
            {
                trap = trapObject.AddComponent<DungeonTrap>();
            }
            
            // 층별 함정 강화
            float floorMultiplier = 1f + (floor * 0.2f);
            TrapType trapType = (TrapType)Random.Range(0, System.Enum.GetValues(typeof(TrapType)).Length);
            
            trap.Initialize(trapType, floorMultiplier, this);
            activeTraps.Add(trap);
        }
        
        /// <summary>
        /// 보물상자 설정
        /// </summary>
        private void ConfigureTreasureChest(GameObject chestObject, int floor)
        {
            var chest = chestObject.GetComponent<TreasureChest>();
            if (chest == null)
            {
                chest = chestObject.AddComponent<TreasureChest>();
            }
            
            // 층별 보상 강화
            ChestType chestType = DetermineChestType(floor);
            chest.Initialize(chestType, floor, this);
            activeChests.Add(chest);
        }
        
        /// <summary>
        /// 비밀 통로 설정
        /// </summary>
        private void ConfigureSecretPassage(GameObject passageObject, int floor)
        {
            var passage = passageObject.GetComponent<SecretPassage>();
            if (passage == null)
            {
                passage = passageObject.AddComponent<SecretPassage>();
            }
            
            passage.Initialize(floor, this);
            activePassages.Add(passage);
        }
        
        /// <summary>
        /// 파괴 가능한 오브젝트 설정
        /// </summary>
        private void ConfigureDestructibleObject(GameObject destructibleObject, int floor)
        {
            var destructible = destructibleObject.GetComponent<DestructibleObject>();
            if (destructible == null)
            {
                destructible = destructibleObject.AddComponent<DestructibleObject>();
            }
            
            float health = 50f + (floor * 25f); // 층별 체력 증가
            destructible.Initialize(health, floor);
        }
        
        /// <summary>
        /// 보물상자 타입 결정
        /// </summary>
        private ChestType DetermineChestType(int floor)
        {
            float random = Random.value;
            
            if (floor >= 8 && random < 0.05f) // 8층+ 5% 전설 상자
                return ChestType.Legendary;
            if (floor >= 5 && random < 0.15f) // 5층+ 15% 영웅 상자
                return ChestType.Epic;
            if (floor >= 3 && random < 0.30f) // 3층+ 30% 희귀 상자
                return ChestType.Rare;
            if (random < 0.60f) // 60% 일반 상자
                return ChestType.Common;
            
            return ChestType.Common;
        }
        
        /// <summary>
        /// 랜덤 스폰 위치 계산
        /// </summary>
        private Vector3 GetRandomSpawnPosition()
        {
            if (environmentSpawnPoints != null && environmentSpawnPoints.Length > 0)
            {
                Transform randomPoint = environmentSpawnPoints[Random.Range(0, environmentSpawnPoints.Length)];
                Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
                return randomPoint.position + new Vector3(randomOffset.x, randomOffset.y, 0);
            }
            else
            {
                // 기본 스폰 영역
                Vector2 randomPosition = Random.insideUnitCircle * spawnRadius;
                return new Vector3(randomPosition.x, randomPosition.y, 0);
            }
        }
        
        /// <summary>
        /// 총 활성 오브젝트 수 계산
        /// </summary>
        private int GetTotalActiveObjects()
        {
            int total = 0;
            foreach (var floorObjects in floorEnvironmentObjects.Values)
            {
                total += floorObjects.Count;
            }
            return total;
        }
        
        /// <summary>
        /// 함정 발동 처리
        /// </summary>
        public void OnTrapTriggeredByPlayer(DungeonTrap trap, PlayerController player)
        {
            OnTrapTriggered?.Invoke(trap, player);
            
            // 통계 업데이트 등
            Debug.Log($"⚠️ Player {player.name} triggered trap: {trap.TrapType}");
        }
        
        /// <summary>
        /// 보물상자 열림 처리
        /// </summary>
        public void OnChestOpenedByPlayer(TreasureChest chest, PlayerController player)
        {
            OnChestOpened?.Invoke(chest, player);
            
            // 보상 지급
            chest.GiveRewards(player);
            
            Debug.Log($"💰 Player {player.name} opened chest: {chest.ChestType}");
        }
        
        /// <summary>
        /// 비밀 통로 발견 처리
        /// </summary>
        public void OnSecretFoundByPlayer(SecretPassage passage, PlayerController player)
        {
            OnSecretFound?.Invoke(passage, player);
            
            // 특별 보상이나 효과
            passage.ActivateSecret(player);
            
            Debug.Log($"🔍 Player {player.name} found secret passage!");
        }
        
        /// <summary>
        /// 특정 위치 주변 함정 제거 (안전 지역 생성용)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ClearTrapsInAreaServerRpc(Vector3 center, float radius)
        {
            if (!IsServer) return;
            
            for (int i = activeTraps.Count - 1; i >= 0; i--)
            {
                if (activeTraps[i] != null)
                {
                    float distance = Vector3.Distance(activeTraps[i].transform.position, center);
                    if (distance <= radius)
                    {
                        var networkObj = activeTraps[i].GetComponent<NetworkObject>();
                        if (networkObj != null && networkObj.IsSpawned)
                        {
                            networkObj.Despawn();
                        }
                        activeTraps.RemoveAt(i);
                    }
                }
            }
        }
        
        /// <summary>
        /// 환경 요소 통계
        /// </summary>
        public EnvironmentStats GetEnvironmentStats()
        {
            return new EnvironmentStats
            {
                totalTraps = activeTraps.Count,
                totalChests = activeChests.Count,
                totalSecrets = activePassages.Count,
                totalObjects = totalEnvironmentObjects.Value
            };
        }
        
        /// <summary>
        /// 디버그 정보
        /// </summary>
        [ContextMenu("Show Environment Debug Info")]
        private void ShowDebugInfo()
        {
            var stats = GetEnvironmentStats();
            Debug.Log("=== Dungeon Environment Debug ===");
            Debug.Log($"Total Objects: {stats.totalObjects}");
            Debug.Log($"Active Traps: {stats.totalTraps}");
            Debug.Log($"Active Chests: {stats.totalChests}");
            Debug.Log($"Active Secrets: {stats.totalSecrets}");
            Debug.Log($"Floor Objects: {floorEnvironmentObjects.Count} floors");
        }
    }
    
    /// <summary>
    /// 환경 요소 타입
    /// </summary>
    public enum EnvironmentType
    {
        Trap,
        TreasureChest,
        SecretPassage,
        DestructibleObject
    }
    
    /// <summary>
    /// 환경 요소 통계
    /// </summary>
    [System.Serializable]
    public struct EnvironmentStats
    {
        public int totalObjects;
        public int totalTraps;
        public int totalChests;
        public int totalSecrets;
    }
}