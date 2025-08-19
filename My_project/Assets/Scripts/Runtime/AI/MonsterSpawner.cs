using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 몬스터 스폰 관리 시스템
    /// 지정된 영역에 몬스터를 동적으로 생성하고 관리
    /// </summary>
    public class MonsterSpawner : NetworkBehaviour
    {
        [Header("스폰 설정")]
        [SerializeField] private GameObject[] monsterPrefabs;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private int maxMonsters = 10;
        [SerializeField] private float spawnRadius = 5f;
        [SerializeField] private float spawnInterval = 10f;
        [SerializeField] private bool autoSpawn = true;
        
        [Header("몬스터 레벨 설정")]
        [SerializeField] private int minLevel = 1;
        [SerializeField] private int maxLevel = 5;
        [SerializeField] private float levelVariance = 0.2f;
        
        [Header("스폰 조건")]
        [SerializeField] private float playerDetectionRange = 20f;
        [SerializeField] private int minPlayersNearby = 1;
        [SerializeField] private bool spawnOnlyWhenPlayersNear = true;
        
        [Header("몬스터 타입별 확률")]
        [SerializeField] private MonsterSpawnData[] spawnData;
        
        // 현재 생성된 몬스터들
        private List<MonsterAI> activeMonsters = new List<MonsterAI>();
        private float lastSpawnTime = 0f;
        private bool isSpawning = false;
        
        // 네트워크 동기화
        private NetworkVariable<int> currentMonsterCount = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        public int CurrentMonsterCount => currentMonsterCount.Value;
        public int MaxMonsters => maxMonsters;
        public bool CanSpawn => currentMonsterCount.Value < maxMonsters;
        
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
            
            Debug.Log($"MonsterSpawner initialized: {name} with {spawnPoints.Length} spawn points");
        }
        
        private void Update()
        {
            if (!IsServer) return;
            
            // 죽은 몬스터 제거
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
                    SpawnRandomMonster();
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
                    if (player != null && player.IsOwner)
                    {
                        var statsManager = player.GetComponent<PlayerStatsManager>();
                        if (statsManager != null && !statsManager.IsDead)
                        {
                            nearbyPlayers++;
                            break; // 이 스폰 포인트에서는 플레이어를 찾았으므로 다음 포인트로
                        }
                    }
                }
            }
            
            return nearbyPlayers >= minPlayersNearby;
        }
        
        /// <summary>
        /// 랜덤 몬스터 스폰
        /// </summary>
        public void SpawnRandomMonster()
        {
            if (!IsServer || isSpawning) return;
            
            StartCoroutine(SpawnMonsterCoroutine());
        }
        
        /// <summary>
        /// 몬스터 스폰 코루틴
        /// </summary>
        private IEnumerator SpawnMonsterCoroutine()
        {
            isSpawning = true;
            
            // 스폰 위치 선택
            Vector3 spawnPosition = GetRandomSpawnPosition();
            
            // 몬스터 타입 선택
            GameObject monsterPrefab = SelectMonsterPrefab();
            if (monsterPrefab == null)
            {
                Debug.LogWarning("No monster prefab available for spawning");
                isSpawning = false;
                yield break;
            }
            
            // 몬스터 레벨 결정
            int monsterLevel = CalculateMonsterLevel();
            
            // 몬스터 생성
            var monsterObject = Instantiate(monsterPrefab, spawnPosition, Quaternion.identity);
            var networkObject = monsterObject.GetComponent<NetworkObject>();
            
            if (networkObject != null)
            {
                networkObject.Spawn();
                
                // 컴포넌트 설정
                yield return null; // 한 프레임 대기
                
                SetupMonster(monsterObject, monsterLevel);
            }
            else
            {
                Debug.LogError($"Monster prefab {monsterPrefab.name} is missing NetworkObject component");
                Destroy(monsterObject);
            }
            
            lastSpawnTime = Time.time;
            isSpawning = false;
        }
        
        /// <summary>
        /// 랜덤 스폰 위치 계산
        /// </summary>
        private Vector3 GetRandomSpawnPosition()
        {
            // 랜덤 스폰 포인트 선택
            Transform selectedSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            
            // 스폰 포인트 주변 랜덤 위치
            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = selectedSpawnPoint.position + new Vector3(randomOffset.x, randomOffset.y, 0);
            
            // 지형 충돌 체크 (추후 구현)
            // spawnPosition = ValidateSpawnPosition(spawnPosition);
            
            return spawnPosition;
        }
        
        /// <summary>
        /// 몬스터 프리팹 선택
        /// </summary>
        private GameObject SelectMonsterPrefab()
        {
            if (monsterPrefabs == null || monsterPrefabs.Length == 0) return null;
            
            // 스폰 데이터가 있으면 확률 기반 선택
            if (spawnData != null && spawnData.Length > 0)
            {
                float totalWeight = 0f;
                foreach (var data in spawnData)
                {
                    totalWeight += data.spawnWeight;
                }
                
                float randomValue = Random.Range(0f, totalWeight);
                float currentWeight = 0f;
                
                foreach (var data in spawnData)
                {
                    currentWeight += data.spawnWeight;
                    if (randomValue <= currentWeight)
                    {
                        return data.monsterPrefab;
                    }
                }
            }
            
            // 기본 랜덤 선택
            return monsterPrefabs[Random.Range(0, monsterPrefabs.Length)];
        }
        
        /// <summary>
        /// 몬스터 레벨 계산
        /// </summary>
        private int CalculateMonsterLevel()
        {
            // 기본 레벨 범위에서 선택
            int baseLevel = Random.Range(minLevel, maxLevel + 1);
            
            // 근처 플레이어 평균 레벨 기반 조정 (추후 구현)
            int adjustedLevel = AdjustLevelBasedOnPlayers(baseLevel);
            
            return Mathf.Clamp(adjustedLevel, 1, 15);
        }
        
        /// <summary>
        /// 플레이어 레벨 기반 몬스터 레벨 조정
        /// </summary>
        private int AdjustLevelBasedOnPlayers(int baseLevel)
        {
            // 근처 플레이어들의 평균 레벨 계산
            List<int> playerLevels = new List<int>();
            
            foreach (var spawnPoint in spawnPoints)
            {
                var nearbyColliders = Physics2D.OverlapCircleAll(spawnPoint.position, playerDetectionRange);
                
                foreach (var collider in nearbyColliders)
                {
                    var player = collider.GetComponent<PlayerController>();
                    if (player != null && player.IsOwner)
                    {
                        var statsManager = player.GetComponent<PlayerStatsManager>();
                        if (statsManager != null && !statsManager.IsDead)
                        {
                            playerLevels.Add(statsManager.CurrentStats.CurrentLevel);
                        }
                    }
                }
            }
            
            if (playerLevels.Count == 0) return baseLevel;
            
            // 평균 레벨 계산
            float averageLevel = 0f;
            foreach (int level in playerLevels)
            {
                averageLevel += level;
            }
            averageLevel /= playerLevels.Count;
            
            // 레벨 조정 (평균 레벨 ± variance)
            float adjustment = Random.Range(-levelVariance, levelVariance);
            int adjustedLevel = Mathf.RoundToInt(averageLevel + adjustment);
            
            return adjustedLevel;
        }
        
        /// <summary>
        /// 몬스터 설정
        /// </summary>
        private void SetupMonster(GameObject monsterObject, int level)
        {
            // MonsterAI 설정
            var monsterAI = monsterObject.GetComponent<MonsterAI>();
            if (monsterAI != null)
            {
                // AI 타입 랜덤 선택
                var aiTypes = System.Enum.GetValues(typeof(MonsterAIType));
                var randomAIType = (MonsterAIType)aiTypes.GetValue(Random.Range(0, aiTypes.Length));
                monsterAI.SetAIType(randomAIType);
                
                activeMonsters.Add(monsterAI);
            }
            
            // MonsterHealth 설정
            var monsterHealth = monsterObject.GetComponent<MonsterHealth>();
            if (monsterHealth != null)
            {
                // 레벨 기반 스탯 계산
                float healthMultiplier = 1f + (level - 1) * 0.5f;
                float maxHealth = 100f * healthMultiplier;
                long expReward = 50 + (level * 25);
                
                string monsterName = monsterObject.name.Replace("(Clone)", "");
                monsterHealth.SetMonsterInfo(monsterName, level, "Spawned", maxHealth, expReward);
            }
            
            Debug.Log($"✨ Spawned {monsterObject.name} (Level {level}) at {monsterObject.transform.position}");
        }
        
        /// <summary>
        /// 죽은 몬스터 정리
        /// </summary>
        private void CleanupDeadMonsters()
        {
            for (int i = activeMonsters.Count - 1; i >= 0; i--)
            {
                if (activeMonsters[i] == null)
                {
                    activeMonsters.RemoveAt(i);
                }
                else
                {
                    var monsterHealth = activeMonsters[i].GetComponent<MonsterHealth>();
                    if (monsterHealth != null && monsterHealth.IsDead)
                    {
                        activeMonsters.RemoveAt(i);
                    }
                }
            }
        }
        
        /// <summary>
        /// 특정 위치에 특정 몬스터 스폰
        /// </summary>
        public void SpawnSpecificMonster(GameObject prefab, Vector3 position, int level = -1)
        {
            if (!IsServer) return;
            
            if (level == -1)
            {
                level = CalculateMonsterLevel();
            }
            
            var monsterObject = Instantiate(prefab, position, Quaternion.identity);
            var networkObject = monsterObject.GetComponent<NetworkObject>();
            
            if (networkObject != null)
            {
                networkObject.Spawn();
                SetupMonster(monsterObject, level);
            }
            else
            {
                Destroy(monsterObject);
            }
        }
        
        /// <summary>
        /// 모든 몬스터 제거
        /// </summary>
        public void ClearAllMonsters()
        {
            if (!IsServer) return;
            
            foreach (var monster in activeMonsters)
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
                if (monster != null)
                {
                    var monsterHealth = monster.GetComponent<MonsterHealth>();
                    if (monsterHealth == null || !monsterHealth.IsDead)
                    {
                        return true; // 살아있는 몬스터 발견
                    }
                }
            }
            return false;
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
        /// 디버그 정보
        /// </summary>
        public void LogSpawnerInfo()
        {
            Debug.Log($"=== MonsterSpawner {name} Info ===");
            Debug.Log($"Active Monsters: {activeMonsters.Count}/{maxMonsters}");
            Debug.Log($"Spawn Points: {spawnPoints.Length}");
            Debug.Log($"Auto Spawn: {autoSpawn}");
            Debug.Log($"Can Spawn: {CanSpawn}");
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
                        Gizmos.color = Color.blue;
                        Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
                        
                        // 스폰 반경
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawWireSphere(spawnPoint.position, spawnRadius);
                        
                        // 플레이어 감지 범위
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireSphere(spawnPoint.position, playerDetectionRange);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 몬스터 스폰 데이터
    /// </summary>
    [System.Serializable]
    public struct MonsterSpawnData
    {
        public GameObject monsterPrefab;
        public float spawnWeight;       // 스폰 확률 가중치
        public int minLevel;           // 최소 레벨
        public int maxLevel;           // 최대 레벨
        public string description;      // 설명
    }
}