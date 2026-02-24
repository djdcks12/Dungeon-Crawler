using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 새로운 몬스터 엔티티 스포너
    /// MonsterEntity 기반의 고급 몬스터들을 생성
    /// </summary>
    public class MonsterEntitySpawner : NetworkBehaviour
    {
        [Header("Spawn Configuration")]
        [SerializeField] private MonsterEntitySpawnData[] spawnDataSets;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private int maxActiveMonsters = 15;
        [SerializeField] private float spawnInterval = 12f;
        [SerializeField] private bool autoSpawn = true;
        
        // GC 최적화: 재사용 버퍼
        private static readonly Collider2D[] s_OverlapBuffer = new Collider2D[8];

        [Header("Spawn Conditions")]
        [SerializeField] private float playerDetectionRange = 25f;
        [SerializeField] private int minPlayersNearby = 1;
        [SerializeField] private bool spawnOnlyWhenPlayersNear = true;
        
        [Header("Grade Range")]
        [SerializeField] private float minGrade = 80f;
        [SerializeField] private float maxGrade = 120f;
        [SerializeField] private float averageGrade = 100f;
        [SerializeField] private float gradeStandardDeviation = 7f;
        
        [Header("Floor-based Scaling")]
        [SerializeField] private int currentFloor = 1;
        [SerializeField] private float floorDifficultyMultiplier = 1.2f;
        
        // 활성 몬스터 추적
        private List<MonsterEntity> activeMonsters = new List<MonsterEntity>();
        private float lastSpawnTime = 0f;
        private bool isSpawning = false;
        
        // 네트워크 동기화
        private NetworkVariable<int> currentMonsterCount = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        public int CurrentMonsterCount => currentMonsterCount.Value;
        public int MaxActiveMonsters => maxActiveMonsters;
        public bool CanSpawn => currentMonsterCount.Value < maxActiveMonsters;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // 스폰 포인트가 없으면 자신의 위치를 사용
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                spawnPoints = new Transform[] { transform };
            }
            
            // 서버에서만 스폰 관리
            if (IsServer && autoSpawn)
            {
                StartCoroutine(SpawnCoroutine());
            }
            else
            {
                Debug.LogWarning($"🏭 Spawn coroutine NOT started: IsServer={IsServer}, autoSpawn={autoSpawn}");
            }
            
            Debug.Log($"MonsterEntitySpawner initialized: {name} with {spawnDataSets?.Length ?? 0} spawn sets");
        }
        
        private void Update()
        {
            if (!IsServer) return;
            
            // 죽은 몬스터 정리
            CleanupDeadMonsters();
            
            // 네트워크 변수 업데이트
            currentMonsterCount.Value = activeMonsters.Count;
        }
        
        /// <summary>
        /// 자동 스폰 코루틴
        /// </summary>
        private IEnumerator SpawnCoroutine()
        {
            while (autoSpawn)
            {
                yield return new WaitForSeconds(spawnInterval);
                
                if (CanSpawn && ShouldSpawn())
                {
                    SpawnRandomMonsterEntity();
                }
            }
        }
        
        /// <summary>
        /// 스폰 조건 확인
        /// </summary>
        private bool ShouldSpawn()
        {
            if (!spawnOnlyWhenPlayersNear) return true;
            
            // 근처 플레이어 수 확인
            int nearbyPlayers = 0;
            foreach (var spawnPoint in spawnPoints)
            {
                int nearbyCount = Physics2D.OverlapCircleNonAlloc(spawnPoint.position, playerDetectionRange, s_OverlapBuffer);

                for (int j = 0; j < nearbyCount; j++)
                {
                    var player = s_OverlapBuffer[j].GetComponent<PlayerController>();
                    if (player != null)
                    {
                        var statsManager = player.GetComponent<PlayerStatsManager>();
                        if (statsManager != null && !statsManager.IsDead)
                        {
                            nearbyPlayers++;
                            break;
                        }
                    }
                }
            }
            
            return nearbyPlayers >= minPlayersNearby;
        }
        
        /// <summary>
        /// 랜덤 몬스터 엔티티 스폰
        /// </summary>
        public void SpawnRandomMonsterEntity()
        {
            Debug.Log($"🔧 SpawnRandomMonsterEntity called: IsServer={IsServer}, isSpawning={isSpawning}");
            
            if (!IsServer || isSpawning) 
            {
                Debug.LogWarning($"❌ SpawnRandomMonsterEntity skipped: IsServer={IsServer}, isSpawning={isSpawning}");
                return;
            }
            
            StartCoroutine(SpawnMonsterEntityCoroutine());
        }
        
        /// <summary>
        /// 몬스터 엔티티 스폰 코루틴
        /// </summary>
        private IEnumerator SpawnMonsterEntityCoroutine()
        {
            isSpawning = true;
            
            try 
            {
                // 스폰 데이터 선택
                var spawnData = SelectSpawnData();
                if (spawnData.raceData == null || spawnData.variantData == null)
                {
                    Debug.LogWarning("No valid spawn data found");
                    yield break;
                }
                
                // 스폰 위치 선택
                Vector3 spawnPosition = GetRandomSpawnPosition();
                
                // 등급 결정
                float grade = DetermineMonsterGrade();
                
                // 몬스터 오브젝트 풀에서 가져오기
                var monsterObject = MonsterObjectPool.Instance?.GetPooledMonster(spawnPosition, spawnData.variantData);
                
                if (monsterObject == null)
                {
                    Debug.LogError("MonsterObjectPool.Instance is null or failed to get pooled monster");
                    yield break;
                }

                // 추가 컴포넌트들 확인/추가
                var monsterEntity = monsterObject.GetComponent<MonsterEntity>();
                
                // 모든 컴포넌트 설정 완료 후 몬스터 엔티티 설정
                SetupMonsterEntity(monsterObject, spawnData, grade, monsterEntity);
                
                // 컴포넌트 설정이 완료된 후 NetworkObject 추가 및 스폰
                var networkObject = monsterObject.GetComponent<NetworkObject>();
                if (networkObject == null)
                {
                    networkObject = monsterObject.AddComponent<NetworkObject>();
                }
                
                // 서버에서 스폰
                try 
                {
                    networkObject.Spawn(true);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ Failed to spawn {monsterObject.name}: {e.Message}");
                    // NetworkObject 제거 후 풀로 반환
                    if (networkObject != null)
                    {
                        Destroy(networkObject);
                    }
                    MonsterObjectPool.Instance?.ReturnMonster(monsterObject);
                    yield break;
                }
                
                lastSpawnTime = Time.time;
            }
            finally
            {
                // 항상 isSpawning을 false로 설정 (오류가 발생해도)
                isSpawning = false;
                Debug.Log($"🔧 SpawnCoroutine completed: isSpawning reset to false");
            }
        }
        
        /// <summary>
        /// 스폰 데이터 선택 (가중치 기반)
        /// </summary>
        private MonsterEntitySpawnData SelectSpawnData()
        {
            Debug.Log($"🎲 SelectSpawnData: spawnDataSets.Length={spawnDataSets?.Length ?? 0}");
            
            if (spawnDataSets == null || spawnDataSets.Length == 0) 
            {
                Debug.LogError($"🎲 No spawn data sets configured!");
                return new MonsterEntitySpawnData();
            }
            
            // 현재 층에 적합한 스폰 데이터 필터링
            var validSpawnData = new List<MonsterEntitySpawnData>();
            foreach (var data in spawnDataSets)
            {
                Debug.Log($"🎲 Checking spawn data: race={data.raceData?.raceName ?? "NULL"}, variant={data.variantData?.variantName ?? "NULL"}");
                
                if (data.variantData?.CanSpawnOnFloor(currentFloor) == true)
                {
                    validSpawnData.Add(data);
                    Debug.Log($"🎲 ✅ Added valid spawn data: {data.variantData.variantName}");
                }
                else
                {
                    Debug.LogWarning($"🎲 ❌ Spawn data rejected: variantData is null or can't spawn on floor {currentFloor}");
                }
            }
            
            if (validSpawnData.Count == 0) return new MonsterEntitySpawnData();
            
            // 가중치 기반 선택
            float totalWeight = 0f;
            foreach (var data in validSpawnData)
            {
                totalWeight += data.spawnWeight;
            }
            
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;
            
            foreach (var data in validSpawnData)
            {
                currentWeight += data.spawnWeight;
                if (randomValue <= currentWeight)
                {
                    return data;
                }
            }
            
            return validSpawnData[0]; // 폴백
        }
        
        /// <summary>
        /// 몬스터 등급 결정 (80-120 범위 정규분포)
        /// </summary>
        private float DetermineMonsterGrade()
        {
            // Box-Muller 변환으로 정규분포 생성
            float roll1 = Random.Range(0.001f, 0.999f); // 0 방지
            float roll2 = Random.Range(0f, 1f);
            
            float gaussianRandom = Mathf.Sqrt(-2.0f * Mathf.Log(roll1)) * Mathf.Cos(2.0f * Mathf.PI * roll2);
            float grade = averageGrade + gaussianRandom * gradeStandardDeviation;
            
            return Mathf.Clamp(grade, minGrade, maxGrade);
        }
        
        /// <summary>
        /// 랜덤 스폰 위치 계산
        /// </summary>
        private Vector3 GetRandomSpawnPosition()
        {
            Transform selectedSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            
            // 스폰 포인트 주변 랜덤 위치
            Vector2 randomOffset = Random.insideUnitCircle * 3f;
            Vector3 spawnPosition = selectedSpawnPoint.position + new Vector3(randomOffset.x, randomOffset.y, 0);
            
            // 장애물 체크 (추후 구현)
            return spawnPosition;
        }
        
        /// <summary>
        /// 몬스터 엔티티 설정
        /// </summary>
        private void SetupMonsterEntity(GameObject monsterObject, MonsterEntitySpawnData spawnData, float grade, MonsterEntity monsterEntity)
        {            
            // 몬스터를 Layer 3으로 설정
            monsterObject.layer = 3;
            
            // 몬스터 생성 (종족 + 개체 + 등급)
            Debug.Log($"🔧 SetupMonsterEntity: Calling GenerateMonster with race={spawnData.raceData?.raceName}, variant={spawnData.variantData?.variantName}, grade={grade}");
            monsterEntity.GenerateMonster(spawnData.raceData, spawnData.variantData, grade);
            
            // 사망 이벤트 구독 (기존 핸들러 제거 후 재등록 - 풀링 시 누적 방지)
            monsterEntity.OnDeath = null;
            monsterEntity.OnDeath += () => OnMonsterEntityDeath(monsterEntity);
            
            // 활성 몬스터 목록에 추가
            activeMonsters.Add(monsterEntity);
            
            Debug.Log($"✨ Spawned {spawnData.variantData.variantName} ({grade}) on floor {currentFloor} - Layer: {monsterObject.layer}");
        }
        
        /// <summary>
        /// 몬스터 엔티티 사망 처리
        /// </summary>
        private void OnMonsterEntityDeath(MonsterEntity monsterEntity)
        {
            if (monsterEntity == null) return;
            
            Debug.Log($"💀 MonsterEntity died: {monsterEntity.VariantData?.variantName ?? "Unknown"} ({monsterEntity.Grade})");
            
            // 몬스터 영혼 드롭 체크
            var soulDropSystem = monsterEntity.GetComponent<MonsterSoulDropSystem>();
            if (soulDropSystem != null)
            {
                soulDropSystem.CheckMonsterSoulDrop(monsterEntity);
            }
            
            // 풀로 반환 (NetworkObject 처리는 ReturnMonster에서)
            var poolable = monsterEntity.GetComponent<PoolableMonster>();
            if (poolable != null)
            {
                poolable.OnMonsterDeath(); // 2초 후 풀로 반환
            }
            else
            {
                // 풀링 컴포넌트가 없으면 직접 풀로 반환
                MonsterObjectPool.Instance?.ReturnMonster(monsterEntity.gameObject);
            }
            
            // 활성 몬스터 목록에서 즉시 제거
            activeMonsters.Remove(monsterEntity);
            
            Debug.Log($"💀 Monster cleanup completed. Active monsters: {activeMonsters.Count}");
        }
        
        /// <summary>
        /// 죽은 몬스터 정리
        /// </summary>
        private void CleanupDeadMonsters()
        {
            for (int i = activeMonsters.Count - 1; i >= 0; i--)
            {
                if (activeMonsters[i] == null || activeMonsters[i].IsDead)
                {
                    activeMonsters.RemoveAt(i);
                }
            }
        }
        
        /// <summary>
        /// 현재 층 설정
        /// </summary>
        public void SetCurrentFloor(int floor)
        {
            currentFloor = Mathf.Max(1, floor);
            Debug.Log($"MonsterEntitySpawner floor set to: {currentFloor}");
        }
        
        /// <summary>
        /// 스폰 활성화/비활성화
        /// </summary>
        public void SetSpawningEnabled(bool enabled)
        {
            autoSpawn = enabled;
            
            if (enabled && IsServer)
            {
                StartCoroutine(SpawnCoroutine());
            }
        }
        
        /// <summary>
        /// 모든 활성 몬스터 제거
        /// </summary>
        public void ClearAllMonsters()
        {
            if (!IsServer) return;
            
            foreach (var monster in activeMonsters.ToArray())
            {
                if (monster != null)
                {
                    var networkObject = monster.GetComponent<NetworkObject>();
                    if (networkObject != null)
                    {
                        networkObject.Despawn();
                    }
                }
            }
            
            activeMonsters.Clear();
        }
        
        /// <summary>
        /// 살아있는 몬스터가 있는지 확인
        /// </summary>
        public bool HasActiveMonsters()
        {
            foreach (var monster in activeMonsters)
            {
                if (monster != null && !monster.IsDead)
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 등급 분포 조정
        /// </summary>
        public void AdjustGradeDistribution(float newAverage, float newStandardDeviation)
        {
            averageGrade = Mathf.Clamp(newAverage, minGrade, maxGrade);
            gradeStandardDeviation = Mathf.Max(1f, newStandardDeviation);
            Debug.Log($"Grade distribution adjusted: Average = {averageGrade}, StdDev = {gradeStandardDeviation}");
        }
        
        /// <summary>
        /// 디버그 정보
        /// </summary>
        [ContextMenu("Show Spawner Info")]
        public void ShowSpawnerInfo()
        {
            Debug.Log($"=== MonsterEntitySpawner {name} Info ===");
            Debug.Log($"Active Monsters: {activeMonsters.Count}/{maxActiveMonsters}");
            Debug.Log($"Current Floor: {currentFloor}");
            Debug.Log($"Spawn Data Sets: {spawnDataSets?.Length ?? 0}");
            Debug.Log($"Auto Spawn: {autoSpawn}");
            Debug.Log($"Can Spawn: {CanSpawn}");
            
            if (activeMonsters.Count > 0)
            {
                Debug.Log("Active Monsters:");
                foreach (var monster in activeMonsters)
                {
                    if (monster != null)
                    {
                        Debug.Log($"- {monster.VariantData?.variantName ?? "Unknown"} ({monster.Grade})");
                    }
                }
            }
        }
        
        public override void OnDestroy()
        {
            StopAllCoroutines();
            base.OnDestroy();
        }

        /// <summary>
        /// 디버그 기즈모
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (spawnPoints != null)
            {
                foreach (var spawnPoint in spawnPoints)
                {
                    if (spawnPoint != null)
                    {
                        // 스폰 포인트
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
                        
                        // 스폰 반경
                        Gizmos.color = Color.orange;
                        Gizmos.DrawWireSphere(spawnPoint.position, 3f);
                        
                        // 플레이어 감지 범위
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireSphere(spawnPoint.position, playerDetectionRange);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 몬스터 엔티티 스폰 데이터
    /// </summary>
    [System.Serializable]
    public struct MonsterEntitySpawnData
    {
        [Header("Monster Definition")]
        public MonsterRaceData raceData;
        public MonsterVariantData variantData;
        public GameObject basePrefab;
        
        [Header("Spawn Settings")]
        public float spawnWeight;
        public string description;
    }
    
}