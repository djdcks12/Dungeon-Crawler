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
                var nearbyColliders = Physics2D.OverlapCircleAll(spawnPoint.position, playerDetectionRange);
                
                foreach (var collider in nearbyColliders)
                {
                    var player = collider.GetComponent<PlayerController>();
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
            
            // 스폰 데이터 선택
            var spawnData = SelectSpawnData();
            if (spawnData.raceData == null || spawnData.variantData == null)
            {
                Debug.LogWarning("No valid spawn data found");
                isSpawning = false;
                yield break;
            }
            
            // 스폰 위치 선택
            Vector3 spawnPosition = GetRandomSpawnPosition();
            
            // 등급 결정
            float grade = DetermineMonsterGrade();
            
            // 몬스터 엔티티 생성
            var monsterObject = Instantiate(spawnData.basePrefab, spawnPosition, Quaternion.identity);
            var networkObject = monsterObject.GetComponent<NetworkObject>();
            
            if (networkObject != null)
            {
                networkObject.Spawn();
                
                // 컴포넌트 설정
                yield return null; // 한 프레임 대기
                
                SetupMonsterEntity(monsterObject, spawnData, grade);
            }
            else
            {
                Debug.LogError($"Monster prefab {spawnData.basePrefab.name} is missing NetworkObject component");
                Destroy(monsterObject);
            }
            
            lastSpawnTime = Time.time;
            isSpawning = false;
        }
        
        /// <summary>
        /// 스폰 데이터 선택 (가중치 기반)
        /// </summary>
        private MonsterEntitySpawnData SelectSpawnData()
        {
            if (spawnDataSets == null || spawnDataSets.Length == 0) return new MonsterEntitySpawnData();
            
            // 현재 층에 적합한 스폰 데이터 필터링
            var validSpawnData = new List<MonsterEntitySpawnData>();
            foreach (var data in spawnDataSets)
            {
                if (data.variantData.CanSpawnOnFloor(currentFloor))
                {
                    validSpawnData.Add(data);
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
        private void SetupMonsterEntity(GameObject monsterObject, MonsterEntitySpawnData spawnData, float grade)
        {
            Debug.Log($"🔧 SetupMonsterEntity: IsServer={IsServer}, NetworkObjectId={NetworkObjectId}");
            
            if (!IsServer)
            {
                Debug.LogError($"❌ SetupMonsterEntity called on client! This should only run on server.");
                return;
            }
            // MonsterEntity 설정
            var monsterEntity = monsterObject.GetComponent<MonsterEntity>();
            if (monsterEntity == null)
            {
                monsterEntity = monsterObject.AddComponent<MonsterEntity>();
            }
            
            // MonsterSkillSystem 확인/추가
            if (monsterObject.GetComponent<MonsterSkillSystem>() == null)
            {
                monsterObject.AddComponent<MonsterSkillSystem>();
            }
            
            // MonsterSoulDropSystem 확인/추가
            var soulDropSystem = monsterObject.GetComponent<MonsterSoulDropSystem>();
            if (soulDropSystem == null)
            {
                soulDropSystem = monsterObject.AddComponent<MonsterSoulDropSystem>();
            }
            
            // 몬스터 생성 (종족 + 개체 + 등급)
            Debug.Log($"🔧 SetupMonsterEntity: Calling GenerateMonster with race={spawnData.raceData?.raceName}, variant={spawnData.variantData?.variantName}, grade={grade}");
            monsterEntity.GenerateMonster(spawnData.raceData, spawnData.variantData, grade);
            
            // 사망 이벤트 구독
            monsterEntity.OnDeath += () => OnMonsterEntityDeath(monsterEntity);
            
            // 층별 난이도 적용
            ApplyFloorDifficulty(monsterEntity);
            
            // 활성 몬스터 목록에 추가
            activeMonsters.Add(monsterEntity);
            
            Debug.Log($"✨ Spawned {spawnData.variantData.variantName} ({grade}) on floor {currentFloor}");
        }
        
        /// <summary>
        /// 층별 난이도 적용
        /// </summary>
        private void ApplyFloorDifficulty(MonsterEntity monsterEntity)
        {
            if (currentFloor <= 1) return;
            
            // 층수에 따른 배율 계산
            float difficultyMultiplier = Mathf.Pow(floorDifficultyMultiplier, currentFloor - 1);
            
            // 스탯 강화는 MonsterEntity 내부에서 처리됨 (이미 적용됨)
            Debug.Log($"🏔️ Applied floor {currentFloor} difficulty (x{difficultyMultiplier:F2}) to {monsterEntity.VariantData.variantName}");
        }
        
        /// <summary>
        /// 몬스터 엔티티 사망 처리
        /// </summary>
        private void OnMonsterEntityDeath(MonsterEntity monsterEntity)
        {
            if (monsterEntity == null) return;
            
            // 몬스터 영혼 드롭 체크
            var soulDropSystem = monsterEntity.GetComponent<MonsterSoulDropSystem>();
            if (soulDropSystem != null)
            {
                soulDropSystem.CheckMonsterSoulDrop(monsterEntity);
            }
            
            // 활성 몬스터 목록에서 제거 (CleanupDeadMonsters에서 처리됨)
            Debug.Log($"💀 MonsterEntity died: {monsterEntity.VariantData?.variantName ?? "Unknown"} ({monsterEntity.Grade})");
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
        /// 특정 위치에 특정 몬스터 엔티티 스폰
        /// </summary>
        public MonsterEntity SpawnSpecificMonsterEntity(MonsterRaceData raceData, MonsterVariantData variantData, 
                                                        Vector3 position, float? forceGrade = null)
        {
            if (!IsServer) return null;
            
            if (raceData == null || variantData == null)
            {
                Debug.LogError("Cannot spawn monster entity: missing race or variant data");
                return null;
            }
            
            float grade = forceGrade ?? DetermineMonsterGrade();
            
            var monsterObject = Instantiate(variantData.prefab, position, Quaternion.identity);
            var networkObject = monsterObject.GetComponent<NetworkObject>();
            
            if (networkObject != null)
            {
                networkObject.Spawn();
                
                var spawnData = new MonsterEntitySpawnData
                {
                    raceData = raceData,
                    variantData = variantData,
                    basePrefab = variantData.prefab,
                    spawnWeight = 1f
                };
                
                SetupMonsterEntity(monsterObject, spawnData, grade);
                
                return monsterObject.GetComponent<MonsterEntity>();
            }
            else
            {
                Destroy(monsterObject);
                return null;
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
                if (monster != null && monster.NetworkObject != null)
                {
                    monster.NetworkObject.Despawn();
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