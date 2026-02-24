using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 보스 몬스터 스폰 시스템
    /// 던전 층별로 적절한 보스를 스폰하고 관리
    /// </summary>
    public class BossSpawner : NetworkBehaviour
    {
        [Header("보스 프리팹 설정")]
        [SerializeField] private GameObject[] floorGuardianPrefabs; // 층 수호자
        [SerializeField] private GameObject[] eliteBossPrefabs;     // 엘리트 보스
        [SerializeField] private GameObject finalBossPrefab;        // 최종 보스 (10층)
        [SerializeField] private GameObject hiddenBossPrefab;       // 히든 보스 (11층)
        
        [Header("스폰 설정")]
        [SerializeField] private Transform[] bossSpawnPoints;
        [SerializeField] private bool spawnBossOnFloorStart = true;
        [SerializeField] private float bossSpawnDelay = 30f; // 30초 후 보스 스폰
        
        [Header("보스 강화 설정")]
        [SerializeField] private float bossHealthMultiplier = 3f;   // 보스 체력 배율
        [SerializeField] private float bossDamageMultiplier = 1.5f; // 보스 데미지 배율
        [SerializeField] private float floorDifficultyMultiplier = 0.2f; // 층당 난이도 증가
        
        // 보스 관리
        private Dictionary<int, NetworkObject> spawnedBosses = new Dictionary<int, NetworkObject>();
        private bool bossSpawned = false;
        
        // 보스 스폰 규칙
        private readonly Dictionary<int, BossType> floorBossTypes = new Dictionary<int, BossType>
        {
            { 3, BossType.FloorGuardian },  // 3층 수호자
            { 5, BossType.EliteBoss },      // 5층 엘리트
            { 7, BossType.FloorGuardian },  // 7층 수호자
            { 9, BossType.EliteBoss },      // 9층 엘리트
            { 10, BossType.FinalBoss },     // 10층 최종 보스
            { 11, BossType.HiddenBoss }     // 11층 히든 보스
        };
        
        // 싱글톤 패턴
        private static BossSpawner instance;
        public static BossSpawner Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<BossSpawner>();
                }
                return instance;
            }
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

        public override void OnDestroy()
        {
            StopAllCoroutines();
            base.OnDestroy();
            if (instance == this)
                instance = null;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
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
        
        /// <summary>
        /// 층 변경 이벤트 처리
        /// </summary>
        private void OnFloorChanged(int newFloor)
        {
            if (!IsServer) return;
            
            // 이전 층의 보스 정리
            CleanupPreviousBoss();
            
            // 보스 층인지 확인
            if (floorBossTypes.ContainsKey(newFloor))
            {
                if (spawnBossOnFloorStart)
                {
                    // 즉시 스폰
                    SpawnBossForFloor(newFloor);
                }
                else
                {
                    // 딜레이 후 스폰
                    StartCoroutine(SpawnBossAfterDelay(newFloor, bossSpawnDelay));
                }
            }
        }
        
        /// <summary>
        /// 특정 층에 맞는 보스 스폰
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SpawnBossForFloorServerRpc(int floor)
        {
            SpawnBossForFloor(floor);
        }
        
        /// <summary>
        /// 층별 보스 스폰 실행
        /// </summary>
        private void SpawnBossForFloor(int floor)
        {
            if (!IsServer) return;
            
            if (!floorBossTypes.ContainsKey(floor))
            {
                Debug.LogWarning($"No boss defined for floor {floor}");
                return;
            }
            
            BossType bossType = floorBossTypes[floor];
            GameObject bossPrefab = GetBossPrefab(bossType, floor);
            
            if (bossPrefab == null)
            {
                Debug.LogError($"Boss prefab not found for {bossType} on floor {floor}");
                return;
            }
            
            // 스폰 위치 결정
            Vector3 spawnPosition = GetBossSpawnPosition(floor);
            
            // 보스 스폰
            GameObject bossInstance = Instantiate(bossPrefab, spawnPosition, Quaternion.identity);
            NetworkObject bossNetworkObject = bossInstance.GetComponent<NetworkObject>();
            
            if (bossNetworkObject != null)
            {
                bossNetworkObject.Spawn();
                
                // 보스 설정
                ConfigureBoss(bossInstance, bossType, floor);
                
                // 스폰된 보스 기록
                spawnedBosses[floor] = bossNetworkObject;
                bossSpawned = true;
                
                // 클라이언트에게 보스 스폰 알림
                NotifyBossSpawnClientRpc(floor, bossType, spawnPosition);
                
                Debug.Log($"🐉 Boss spawned: {bossType} on Floor {floor} at {spawnPosition}");
            }
        }
        
        /// <summary>
        /// 보스 타입에 맞는 프리팹 가져오기
        /// </summary>
        private GameObject GetBossPrefab(BossType bossType, int floor)
        {
            switch (bossType)
            {
                case BossType.FloorGuardian:
                    if (floorGuardianPrefabs != null && floorGuardianPrefabs.Length > 0)
                    {
                        int index = (floor / 2) % floorGuardianPrefabs.Length; // 층에 따라 다른 수호자
                        return floorGuardianPrefabs[index];
                    }
                    break;
                    
                case BossType.EliteBoss:
                    if (eliteBossPrefabs != null && eliteBossPrefabs.Length > 0)
                    {
                        int index = ((floor - 5) / 4) % eliteBossPrefabs.Length; // 엘리트 보스 종류
                        return eliteBossPrefabs[index];
                    }
                    break;
                    
                case BossType.FinalBoss:
                    return finalBossPrefab;
                    
                case BossType.HiddenBoss:
                    return hiddenBossPrefab;
            }
            
            return null;
        }
        
        /// <summary>
        /// 보스 스폰 위치 계산
        /// </summary>
        private Vector3 GetBossSpawnPosition(int floor)
        {
            if (bossSpawnPoints != null && bossSpawnPoints.Length > 0)
            {
                // 지정된 스폰 포인트 사용
                int spawnIndex = floor % bossSpawnPoints.Length;
                return bossSpawnPoints[spawnIndex].position;
            }
            else
            {
                // 기본 위치: 던전 중앙에서 약간 떨어진 곳
                Vector3 dungeonCenter = Vector3.zero;
                Vector3 offset = Random.insideUnitCircle * 5f;
                return dungeonCenter + new Vector3(offset.x, offset.y, 0f);
            }
        }
        
        /// <summary>
        /// 보스 설정 (난이도, 스탯 등)
        /// </summary>
        private void ConfigureBoss(GameObject bossInstance, BossType bossType, int floor)
        {
            var bossAI = bossInstance.GetComponent<BossMonsterAI>();
            var monsterAI = bossInstance.GetComponent<MonsterAI>();
            
            if (bossAI != null)
            {
                // 보스 타입과 층 설정
                bossAI.SetBossType(bossType);
                bossAI.SetTargetFloor(floor);
            }
            
            if (monsterAI != null)
            {
                // 층별 데미지 강화
                float damageMultiplier = bossDamageMultiplier + (floor * floorDifficultyMultiplier * 0.5f);
                float enhancedDamage = monsterAI.AttackDamage * damageMultiplier;
                monsterAI.SetAttackDamage(enhancedDamage);
                
                // 보스는 더 공격적으로 설정
                monsterAI.SetAIType(MonsterAIType.Aggressive);
                
                Debug.Log($"Boss damage set to {enhancedDamage:F1} (x{damageMultiplier:F1})");
            }
        }
        
        /// <summary>
        /// 이전 층의 보스 정리
        /// </summary>
        private void CleanupPreviousBoss()
        {
            var bossesToRemove = new List<int>();
            
            foreach (var bossPair in spawnedBosses)
            {
                if (bossPair.Value != null && bossPair.Value.IsSpawned)
                {
                    // 보스가 살아있으면 삭제
                    bossPair.Value.Despawn();
                }
                bossesToRemove.Add(bossPair.Key);
            }
            
            foreach (int floor in bossesToRemove)
            {
                spawnedBosses.Remove(floor);
            }
            
            bossSpawned = false;
        }
        
        /// <summary>
        /// 딜레이 후 보스 스폰
        /// </summary>
        private System.Collections.IEnumerator SpawnBossAfterDelay(int floor, float delay)
        {
            // 플레이어들에게 보스 스폰 예고 알림
            NotifyBossSpawnWarningClientRpc(floor, delay);
            
            yield return new WaitForSeconds(delay);
            
            // 여전히 해당 층에 있을 때만 스폰
            if (DungeonManager.Instance != null && DungeonManager.Instance.CurrentFloor == floor)
            {
                SpawnBossForFloor(floor);
            }
        }
        
        /// <summary>
        /// 보스 처치 확인
        /// </summary>
        public void OnBossDefeated(int floor, BossType bossType)
        {
            if (!IsServer) return;
            
            if (spawnedBosses.ContainsKey(floor))
            {
                spawnedBosses.Remove(floor);
            }
            
            // 보스 처치 보상 지급
            GrantBossRewards(floor, bossType);
            
            // 클라이언트에게 보스 처치 알림
            NotifyBossDefeatedClientRpc(floor, bossType);
            
            Debug.Log($"🏆 Boss defeated: {bossType} on Floor {floor}");
        }
        
        /// <summary>
        /// 보스 처치 보상 지급
        /// </summary>
        private void GrantBossRewards(int floor, BossType bossType)
        {
            // 던전에 있는 모든 플레이어에게 보상 지급
            var connectedClients = NetworkManager.Singleton.ConnectedClients;
            
            foreach (var client in connectedClients.Values)
            {
                if (client.PlayerObject != null)
                {
                    var statsManager = client.PlayerObject.GetComponent<PlayerStatsManager>();
                    if (statsManager != null)
                    {
                        // 층별 보상 계산
                        long expReward = CalculateBossExpReward(floor, bossType);
                        long goldReward = CalculateBossGoldReward(floor, bossType);
                        
                        // 보상 지급
                        statsManager.AddExperience(expReward);
                        statsManager.ChangeGold(goldReward);
                        
                        Debug.Log($"Boss rewards: Player {client.ClientId} received {expReward} EXP, {goldReward} Gold");
                    }
                }
            }
        }
        
        /// <summary>
        /// 보스 경험치 보상 계산
        /// </summary>
        private long CalculateBossExpReward(int floor, BossType bossType)
        {
            long baseReward = 1000; // 기본 보상
            
            float typeMultiplier = bossType switch
            {
                BossType.FloorGuardian => 1.0f,
                BossType.EliteBoss => 1.5f,
                BossType.FinalBoss => 3.0f,
                BossType.HiddenBoss => 5.0f,
                _ => 1.0f
            };
            
            return (long)(baseReward * floor * typeMultiplier);
        }
        
        /// <summary>
        /// 보스 골드 보상 계산
        /// </summary>
        private long CalculateBossGoldReward(int floor, BossType bossType)
        {
            long baseReward = 500; // 기본 보상
            
            float typeMultiplier = bossType switch
            {
                BossType.FloorGuardian => 1.0f,
                BossType.EliteBoss => 2.0f,
                BossType.FinalBoss => 5.0f,
                BossType.HiddenBoss => 10.0f,
                _ => 1.0f
            };
            
            return (long)(baseReward * floor * typeMultiplier);
        }
        
        /// <summary>
        /// 현재 층에 보스가 있는지 확인
        /// </summary>
        public bool HasBossOnFloor(int floor)
        {
            return spawnedBosses.ContainsKey(floor) && 
                   spawnedBosses[floor] != null && 
                   spawnedBosses[floor].IsSpawned;
        }
        
        /// <summary>
        /// 현재 층이 보스 층인지 확인
        /// </summary>
        public bool IsBossFloor(int floor)
        {
            return floorBossTypes.ContainsKey(floor);
        }
        
        // ClientRpc 메서드들
        [ClientRpc]
        private void NotifyBossSpawnClientRpc(int floor, BossType bossType, Vector3 position)
        {
            Debug.Log($"🐉 보스 등장! {bossType}이(가) {floor}층에 나타났습니다!");
            
            // UI 알림이나 사운드 효과 재생
            if (UIManager.Instance != null)
            {
                // 보스 등장 UI 표시 (추후 구현)
            }
        }
        
        [ClientRpc]
        private void NotifyBossSpawnWarningClientRpc(int floor, float delay)
        {
            Debug.Log($"⚠️ {delay}초 후 {floor}층에 보스가 등장합니다!");
        }
        
        [ClientRpc]
        private void NotifyBossDefeatedClientRpc(int floor, BossType bossType)
        {
            Debug.Log($"🏆 보스 처치! {bossType}을(를) 물리쳤습니다!");
            
            // 승리 효과 표시
            if (UIManager.Instance != null)
            {
                // 보스 처치 UI 표시 (추후 구현)
            }
        }
        
        /// <summary>
        /// 디버그 정보
        /// </summary>
        [ContextMenu("Show Boss Spawner Debug Info")]
        private void ShowDebugInfo()
        {
            Debug.Log("=== Boss Spawner Debug ===");
            Debug.Log($"Boss Spawned: {bossSpawned}");
            Debug.Log($"Active Bosses: {spawnedBosses.Count}");
            
            foreach (var boss in spawnedBosses)
            {
                Debug.Log($"- Floor {boss.Key}: {(boss.Value != null ? "Active" : "Null")}");
            }
        }
    }
}