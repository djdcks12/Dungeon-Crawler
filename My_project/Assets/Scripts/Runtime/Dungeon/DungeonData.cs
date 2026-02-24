using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 던전 데이터 ScriptableObject
    /// 던전의 기본 정보, 층별 구성, 몬스터 배치 등을 정의
    /// </summary>
    [CreateAssetMenu(fileName = "New Dungeon Data", menuName = "Dungeon Crawler/Dungeon Data")]
    public class DungeonData : ScriptableObject
    {
        [Header("기본 던전 정보")]
        [SerializeField] private string dungeonName = "새로운 던전";
        [SerializeField] private string description = "";
        [SerializeField] private DungeonType dungeonType = DungeonType.Normal;
        [SerializeField] private DungeonDifficulty difficulty = DungeonDifficulty.Normal;
        [SerializeField] private Sprite dungeonIcon;
        
        [Header("던전 설정")]
        [SerializeField] private int maxFloors = 10;
        [SerializeField] private int maxPlayers = 16;
        [SerializeField] private int recommendedLevel = 1;
        [SerializeField] private float timeLimit = 3600f; // 1시간
        [SerializeField] private bool allowPvP = true;
        [SerializeField] private bool allowRevive = false;
        
        [Header("보상 설정")]
        [SerializeField] private long baseExpReward = 1000;
        [SerializeField] private long baseGoldReward = 500;
        [SerializeField] private float expMultiplierPerFloor = 1.2f;
        [SerializeField] private float goldMultiplierPerFloor = 1.1f;
        [SerializeField] private float completionBonusMultiplier = 2.0f;
        
        [Header("시간 관리 설정")]
        [SerializeField] private DungeonTimeMode timeMode = DungeonTimeMode.PerFloor;
        [SerializeField] private float baseFloorTime = 600f; // 기본 층별 시간 (10분)
        [SerializeField] private float timeIncreasePerFloor = 120f; // 층당 시간 증가량 (2분)
        [SerializeField] private AnimationCurve floorTimeCurve = AnimationCurve.Linear(1, 1, 10, 2); // 층별 시간 배율 곡선
        
        [Header("층별 구성")]
        [SerializeField] private List<FloorConfiguration> floorConfigs = new List<FloorConfiguration>();
        
        [Header("몬스터 풀")]
        [SerializeField] private List<MonsterSpawnInfo> availableMonsters = new List<MonsterSpawnInfo>();
        
        [Header("보스 몬스터")]
        [SerializeField] private List<BossSpawnInfo> bossMonsters = new List<BossSpawnInfo>();
        
        // 프로퍼티들
        public string DungeonName => dungeonName;
        public string Description => description;
        public DungeonType DungeonType => dungeonType;
        public DungeonDifficulty Difficulty => difficulty;
        public Sprite DungeonIcon => dungeonIcon;
        public int MaxFloors => maxFloors;
        public int MaxPlayers => maxPlayers;
        public int RecommendedLevel => recommendedLevel;
        public float TimeLimit => timeLimit;
        public bool AllowPvP => allowPvP;
        public bool AllowRevive => allowRevive;
        public long BaseExpReward => baseExpReward;
        public long BaseGoldReward => baseGoldReward;
        public List<FloorConfiguration> FloorConfigs => floorConfigs;
        public List<MonsterSpawnInfo> AvailableMonsters => availableMonsters;
        public List<BossSpawnInfo> BossMonsters => bossMonsters;
        public float ExpMultiplierPerFloor => expMultiplierPerFloor;
        public float GoldMultiplierPerFloor => goldMultiplierPerFloor;
        public float CompletionBonusMultiplier => completionBonusMultiplier;
        public DungeonTimeMode TimeMode => timeMode;
        public float BaseFloorTime => baseFloorTime;
        public float TimeIncreasePerFloor => timeIncreasePerFloor;
        public AnimationCurve FloorTimeCurve => floorTimeCurve;
        
        /// <summary>
        /// 던전 정보 구조체로 변환
        /// </summary>
        public DungeonInfo ToDungeonInfo()
        {
            return new DungeonInfo
            {
                dungeonId = GetInstanceID(),
                dungeonNameHash = DungeonNameRegistry.RegisterName(dungeonName),
                dungeonType = dungeonType,
                difficulty = difficulty,
                currentFloor = 1,
                maxFloors = maxFloors,
                recommendedLevel = recommendedLevel,
                maxPlayers = maxPlayers,
                timeLimit = timeLimit,
                baseExpReward = baseExpReward,
                baseGoldReward = baseGoldReward
            };
        }
        
        /// <summary>
        /// 특정 층의 구성 정보 가져오기
        /// </summary>
        public FloorConfiguration GetFloorConfig(int floorNumber)
        {
            if (floorConfigs == null || floorConfigs.Count == 0)
            {
                return CreateDefaultFloorConfig(floorNumber);
            }
            
            // 지정된 층 구성이 있으면 사용
            var config = floorConfigs.FirstOrDefault(f => f.floorNumber == floorNumber);
            if (config != null)
            {
                return config;
            }
            
            // 없으면 가장 가까운 층 구성을 기반으로 생성
            var closestConfig = floorConfigs
                .Where(f => f.floorNumber <= floorNumber)
                .OrderByDescending(f => f.floorNumber)
                .FirstOrDefault();
                
            if (closestConfig != null)
            {
                return CreateScaledFloorConfig(closestConfig, floorNumber);
            }
            
            return CreateDefaultFloorConfig(floorNumber);
        }
        
        /// <summary>
        /// 층별 경험치 보상 계산
        /// </summary>
        public long CalculateExpReward(int floorNumber)
        {
            float multiplier = Mathf.Pow(expMultiplierPerFloor, floorNumber - 1);
            return (long)(baseExpReward * multiplier);
        }
        
        /// <summary>
        /// 층별 골드 보상 계산
        /// </summary>
        public long CalculateGoldReward(int floorNumber)
        {
            float multiplier = Mathf.Pow(goldMultiplierPerFloor, floorNumber - 1);
            return (long)(baseGoldReward * multiplier);
        }
        
        /// <summary>
        /// 완주 보너스 계산
        /// </summary>
        public DungeonReward CalculateCompletionReward(float completionTime, int monstersKilled)
        {
            long totalExp = 0;
            long totalGold = 0;
            
            // 각 층별 보상 합계
            for (int floor = 1; floor <= maxFloors; floor++)
            {
                totalExp += CalculateExpReward(floor);
                totalGold += CalculateGoldReward(floor);
            }
            
            // 완주 보너스 적용
            totalExp = (long)(totalExp * completionBonusMultiplier);
            totalGold = (long)(totalGold * completionBonusMultiplier);
            
            return new DungeonReward
            {
                expReward = totalExp,
                goldReward = totalGold,
                completionTime = completionTime,
                floorReached = maxFloors,
                monstersKilled = monstersKilled,
                survivalRate = 1.0f,
                itemRewards = GenerateCompletionItems()
            };
        }
        
        /// <summary>
        /// 해당 층에 스폰 가능한 몬스터 목록 가져오기
        /// </summary>
        public List<MonsterSpawnInfo> GetSpawnableMonsters(int floorNumber)
        {
            return availableMonsters
                .Where(m => m.minFloor <= floorNumber && m.maxFloor >= floorNumber)
                .ToList();
        }
        
        /// <summary>
        /// 보스 층에 스폰할 보스 선택
        /// </summary>
        public BossSpawnInfo GetBossForFloor(int floorNumber)
        {
            var availableBosses = bossMonsters
                .Where(b => b.minFloor <= floorNumber && b.maxFloor >= floorNumber)
                .ToList();
                
            if (availableBosses.Count == 0)
                return null;
                
            // 랜덤하게 보스 선택
            int randomIndex = Random.Range(0, availableBosses.Count);
            return availableBosses[randomIndex];
        }
        
        /// <summary>
        /// 기본 층 구성 생성
        /// </summary>
        private FloorConfiguration CreateDefaultFloorConfig(int floorNumber)
        {
            return new FloorConfiguration
            {
                floorNumber = floorNumber,
                floorName = $"{dungeonName} {floorNumber}층",
                floorSize = new Vector2(50, 50),
                monsterCount = 10 + (floorNumber * 2),
                eliteCount = Mathf.Max(1, floorNumber / 3),
                hasBoss = floorNumber % 5 == 0 || floorNumber == maxFloors,
                hasExit = floorNumber == maxFloors,
                completionBonus = 1.0f + (floorNumber * 0.1f)
            };
        }
        
        /// <summary>
        /// FloorConfiguration을 DungeonFloor로 변환
        /// </summary>
        public DungeonFloor ToNetworkDungeonFloor(FloorConfiguration config)
        {
            return new DungeonFloor
            {
                floorNumber = config.floorNumber,
                floorNameHash = DungeonNameRegistry.RegisterName(config.floorName),
                floorSize = config.floorSize,
                monsterCount = config.monsterCount,
                eliteCount = config.eliteCount,
                hasBoss = config.hasBoss,
                hasExit = config.hasExit,
                completionBonus = config.completionBonus,
                playerSpawnPoint = config.playerSpawnPoints.Count > 0 ? (Vector3)config.playerSpawnPoints[0] : Vector3.zero,
                exitPoint = (Vector3)config.exitPoint
            };
        }
        
        /// <summary>
        /// 기존 구성을 기반으로 확대된 층 구성 생성
        /// </summary>
        private FloorConfiguration CreateScaledFloorConfig(FloorConfiguration baseConfig, int targetFloor)
        {
            float scale = (float)targetFloor / baseConfig.floorNumber;
            
            return new FloorConfiguration
            {
                floorNumber = targetFloor,
                floorName = $"{dungeonName} {targetFloor}층",
                floorSize = baseConfig.floorSize * Mathf.Sqrt(scale),
                monsterCount = Mathf.RoundToInt(baseConfig.monsterCount * scale),
                eliteCount = Mathf.RoundToInt(baseConfig.eliteCount * scale),
                hasBoss = targetFloor % 5 == 0 || targetFloor == maxFloors,
                hasExit = targetFloor == maxFloors,
                completionBonus = baseConfig.completionBonus * scale
            };
        }
        
        /// <summary>
        /// 완주 보상 아이템 생성
        /// </summary>
        private List<ItemInstance> GenerateCompletionItems()
        {
            var items = new List<ItemInstance>();
            
            // 던전 완주 시 특별 아이템 드롭
            // 난이도와 던전 타입에 따라 다른 보상
            
            ItemDatabase.Initialize();
            
            switch (difficulty)
            {
                case DungeonDifficulty.Easy:
                    // 기본 아이템들
                    break;
                    
                case DungeonDifficulty.Normal:
                    // 중급 아이템들
                    break;
                    
                case DungeonDifficulty.Hard:
                    // 고급 아이템들
                    break;
                    
                case DungeonDifficulty.Nightmare:
                    // 전설 아이템 확률
                    if (Random.value < 0.1f) // 10% 확률
                    {
                        var legendaryItems = ItemDatabase.GetItemsByGrade(ItemGrade.Legendary);
                        if (legendaryItems.Count > 0)
                        {
                            var randomLegendary = legendaryItems[Random.Range(0, legendaryItems.Count)];
                            items.Add(new ItemInstance(randomLegendary, 1));
                        }
                    }
                    break;
            }
            
            return items;
        }
        
        /// <summary>
        /// 던전 유효성 검사
        /// </summary>
        [ContextMenu("Validate Dungeon Data")]
        public void ValidateDungeonData()
        {
            if (string.IsNullOrEmpty(dungeonName))
            {
                Debug.LogWarning($"Dungeon name is empty!");
            }
            
            if (maxFloors <= 0)
            {
                Debug.LogError($"Max floors must be greater than 0!");
            }
            
            if (floorConfigs.Count > maxFloors)
            {
                Debug.LogWarning($"More floor configs ({floorConfigs.Count}) than max floors ({maxFloors})!");
            }
            
            if (availableMonsters.Count == 0)
            {
                Debug.LogWarning($"No monsters configured for dungeon!");
            }
            
            Debug.Log($"Dungeon '{dungeonName}' validation complete.");
        }
        
        /// <summary>
        /// 특정 층의 제한시간 계산
        /// </summary>
        public float CalculateFloorTimeLimit(int floorNumber)
        {
            switch (timeMode)
            {
                case DungeonTimeMode.PerFloor:
                    // 층별 시간 = 기본시간 + (층수-1) * 증가량 * 곡선 배율
                    float curveMultiplier = floorTimeCurve.Evaluate(floorNumber);
                    return baseFloorTime + ((floorNumber - 1) * timeIncreasePerFloor * curveMultiplier);
                    
                case DungeonTimeMode.Total:
                    // 총 제한시간을 층수로 나눈 시간
                    return maxFloors > 0 ? timeLimit / maxFloors : timeLimit;
                    
                case DungeonTimeMode.Custom:
                    // FloorConfiguration에서 개별 설정된 시간
                    var floorConfig = GetFloorConfig(floorNumber);
                    return floorConfig.floorTimeLimit > 0 ? floorConfig.floorTimeLimit : baseFloorTime;
                    
                default:
                    return baseFloorTime;
            }
        }
        
        /// <summary>
        /// 모든 층의 총 예상 시간 계산
        /// </summary>
        public float CalculateTotalEstimatedTime()
        {
            float totalTime = 0f;
            
            for (int floor = 1; floor <= maxFloors; floor++)
            {
                totalTime += CalculateFloorTimeLimit(floor);
            }
            
            return totalTime;
        }
        
        /// <summary>
        /// 시간 관리 정보 텍스트 생성
        /// </summary>
        public string GetTimeManagementInfo()
        {
            string info = $"시간 모드: {GetTimeModeDisplayName(timeMode)}\n";
            
            switch (timeMode)
            {
                case DungeonTimeMode.PerFloor:
                    info += $"기본 층별 시간: {baseFloorTime / 60:F1}분\n";
                    info += $"층당 증가시간: {timeIncreasePerFloor / 60:F1}분\n";
                    info += $"예상 총 시간: {CalculateTotalEstimatedTime() / 60:F1}분";
                    break;
                    
                case DungeonTimeMode.Total:
                    info += $"총 제한시간: {timeLimit / 60:F1}분\n";
                    info += maxFloors > 0 ? $"층당 평균시간: {(timeLimit / maxFloors) / 60:F1}분" : "층당 평균시간: N/A";
                    break;
                    
                case DungeonTimeMode.Custom:
                    info += "층별 개별 설정\n";
                    for (int i = 1; i <= maxFloors; i++)
                    {
                        float floorTime = CalculateFloorTimeLimit(i);
                        info += $"{i}층: {floorTime / 60:F1}분\n";
                    }
                    break;
            }
            
            return info;
        }
        
        /// <summary>
        /// 시간 모드 표시명 반환
        /// </summary>
        private string GetTimeModeDisplayName(DungeonTimeMode mode)
        {
            switch (mode)
            {
                case DungeonTimeMode.PerFloor: return "층별 시간 (이월 가능)";
                case DungeonTimeMode.Total: return "총 제한시간 (고정)";
                case DungeonTimeMode.Custom: return "커스텀 설정";
                default: return "알 수 없음";
            }
        }
    }
    
    /// <summary>
    /// 층별 구성 정보
    /// </summary>
    [System.Serializable]
    public class FloorConfiguration
    {
        public int floorNumber = 1;
        public string floorName = "";
        public Vector2 floorSize = new Vector2(50, 50);
        public int monsterCount = 10;
        public int eliteCount = 1;
        public bool hasBoss = false;
        public bool hasExit = false;
        public float completionBonus = 1.0f;
        public float floorTimeLimit = 600f; // 층별 제한시간 (초) - 기본 10분
        
        // 스폰 포인트들 (상대 좌표)
        public List<Vector2> monsterSpawnPoints = new List<Vector2>();
        public List<Vector2> playerSpawnPoints = new List<Vector2>();
        public Vector2 exitPoint = Vector2.zero;
    }
    
    /// <summary>
    /// 몬스터 스폰 정보
    /// </summary>
    [System.Serializable]
    public class MonsterSpawnInfo
    {
        public string monsterId = "";
        public string monsterName = "";
        public GameObject monsterPrefab;
        public int minFloor = 1;
        public int maxFloor = 10;
        public float spawnWeight = 1.0f; // 스폰 가중치
        public int minLevel = 1;
        public int maxLevel = 15;
        public bool isElite = false;
    }
    
    /// <summary>
    /// 보스 몬스터 스폰 정보
    /// </summary>
    [System.Serializable]
    public class BossSpawnInfo
    {
        public string bossId = "";
        public string bossName = "";
        public GameObject bossPrefab;
        public int minFloor = 5;
        public int maxFloor = 10;
        public int level = 10;
        public float healthMultiplier = 5.0f;
        public float damageMultiplier = 2.0f;
        public List<ItemInstance> guaranteedDrops = new List<ItemInstance>();
    }
}