using UnityEngine;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 던전 데이터 생성 유틸리티
    /// 기본적인 던전들을 프로그래매틱하게 생성
    /// </summary>
    public static class DungeonCreator
    {
        /// <summary>
        /// 기본 초보자 던전 생성
        /// </summary>
        public static DungeonData CreateBeginnerDungeon()
        {
            var dungeon = ScriptableObject.CreateInstance<DungeonData>();
            
            // 기본 정보 설정
            SetDungeonField(dungeon, "dungeonName", "초보자의 동굴");
            SetDungeonField(dungeon, "description", "새로운 모험가를 위한 안전한 던전입니다.");
            SetDungeonField(dungeon, "dungeonType", DungeonType.Normal);
            SetDungeonField(dungeon, "difficulty", DungeonDifficulty.Easy);
            SetDungeonField(dungeon, "maxFloors", 3);
            SetDungeonField(dungeon, "maxPlayers", 4);
            SetDungeonField(dungeon, "recommendedLevel", 1);
            SetDungeonField(dungeon, "timeLimit", 1800f); // 30분
            SetDungeonField(dungeon, "allowPvP", false);
            SetDungeonField(dungeon, "allowRevive", true);
            
            // 보상 설정
            SetDungeonField(dungeon, "baseExpReward", 500L);
            SetDungeonField(dungeon, "baseGoldReward", 250L);
            SetDungeonField(dungeon, "expMultiplierPerFloor", 1.3f);
            SetDungeonField(dungeon, "goldMultiplierPerFloor", 1.2f);
            SetDungeonField(dungeon, "completionBonusMultiplier", 1.5f);
            
            // 시간 관리 설정 - 초보자 던전은 여유롭게
            SetDungeonField(dungeon, "timeMode", DungeonTimeMode.PerFloor);
            SetDungeonField(dungeon, "baseFloorTime", 600f); // 10분
            SetDungeonField(dungeon, "timeIncreasePerFloor", 120f); // 층당 2분 추가
            
            // 층별 구성 설정
            var floorConfigs = new List<FloorConfiguration>();
            
            // 1층 - 튜토리얼 층 (시간은 자동 계산)
            floorConfigs.Add(new FloorConfiguration
            {
                floorNumber = 1,
                floorName = "입구",
                floorSize = new Vector2(30, 30),
                monsterCount = 3,
                eliteCount = 0,
                hasBoss = false,
                hasExit = false,
                completionBonus = 1.0f
            });
            
            // 2층 - 일반 층 (시간은 자동 계산)
            floorConfigs.Add(new FloorConfiguration
            {
                floorNumber = 2,
                floorName = "동굴 깊숙이",
                floorSize = new Vector2(40, 40),
                monsterCount = 5,
                eliteCount = 1,
                hasBoss = false,
                hasExit = false,
                completionBonus = 1.2f
            });
            
            // 3층 - 보스 층 (시간은 자동 계산)
            floorConfigs.Add(new FloorConfiguration
            {
                floorNumber = 3,
                floorName = "보스의 방",
                floorSize = new Vector2(50, 50),
                monsterCount = 2,
                eliteCount = 0,
                hasBoss = true,
                hasExit = true,
                completionBonus = 2.0f
            });
            
            SetDungeonField(dungeon, "floorConfigs", floorConfigs);
            
            // 몬스터 풀 설정 (기본 몬스터들)
            var monsters = new List<MonsterSpawnInfo>();
            
            monsters.Add(new MonsterSpawnInfo
            {
                monsterId = "goblin",
                monsterName = "고블린",
                minFloor = 1,
                maxFloor = 3,
                spawnWeight = 1.0f,
                minLevel = 1,
                maxLevel = 3,
                isElite = false
            });
            
            monsters.Add(new MonsterSpawnInfo
            {
                monsterId = "goblin_warrior",
                monsterName = "고블린 전사",
                minFloor = 2,
                maxFloor = 3,
                spawnWeight = 0.5f,
                minLevel = 2,
                maxLevel = 4,
                isElite = true
            });
            
            SetDungeonField(dungeon, "availableMonsters", monsters);
            
            // 보스 몬스터 설정
            var bosses = new List<BossSpawnInfo>();
            
            bosses.Add(new BossSpawnInfo
            {
                bossId = "goblin_king",
                bossName = "고블린 왕",
                minFloor = 3,
                maxFloor = 3,
                level = 5,
                healthMultiplier = 3.0f,
                damageMultiplier = 1.5f,
                guaranteedDrops = new List<ItemInstance>()
            });
            
            SetDungeonField(dungeon, "bossMonsters", bosses);
            
            return dungeon;
        }
        
        /// <summary>
        /// 중급 던전 생성
        /// </summary>
        public static DungeonData CreateIntermediateDungeon()
        {
            var dungeon = ScriptableObject.CreateInstance<DungeonData>();
            
            SetDungeonField(dungeon, "dungeonName", "어둠의 숲");
            SetDungeonField(dungeon, "description", "위험한 몬스터들이 도사리는 어두운 숲입니다.");
            SetDungeonField(dungeon, "dungeonType", DungeonType.Elite);
            SetDungeonField(dungeon, "difficulty", DungeonDifficulty.Normal);
            SetDungeonField(dungeon, "maxFloors", 5);
            SetDungeonField(dungeon, "maxPlayers", 8);
            SetDungeonField(dungeon, "recommendedLevel", 5);
            SetDungeonField(dungeon, "timeLimit", 2700f); // 45분
            SetDungeonField(dungeon, "allowPvP", true);
            SetDungeonField(dungeon, "allowRevive", false);
            
            SetDungeonField(dungeon, "baseExpReward", 1000L);
            SetDungeonField(dungeon, "baseGoldReward", 500L);
            SetDungeonField(dungeon, "expMultiplierPerFloor", 1.4f);
            SetDungeonField(dungeon, "goldMultiplierPerFloor", 1.3f);
            SetDungeonField(dungeon, "completionBonusMultiplier", 2.0f);
            
            // 시간 관리 설정 - 중급 던전은 조금 더 타이트하게
            SetDungeonField(dungeon, "timeMode", DungeonTimeMode.PerFloor);
            SetDungeonField(dungeon, "baseFloorTime", 480f); // 8분
            SetDungeonField(dungeon, "timeIncreasePerFloor", 180f); // 층당 3분 추가
            
            // 5층 구성 생성 (시간은 자동 계산)
            var floorConfigs = new List<FloorConfiguration>();
            for (int i = 1; i <= 5; i++)
            {
                floorConfigs.Add(new FloorConfiguration
                {
                    floorNumber = i,
                    floorName = $"숲 {i}구역",
                    floorSize = new Vector2(60 + i * 10, 60 + i * 10),
                    monsterCount = 8 + i * 2,
                    eliteCount = 1 + (i / 2),
                    hasBoss = i == 5,
                    hasExit = i == 5,
                    completionBonus = 1.0f + (i * 0.2f)
                });
            }
            SetDungeonField(dungeon, "floorConfigs", floorConfigs);
            
            return dungeon;
        }
        
        /// <summary>
        /// 고급 던전 생성
        /// </summary>
        public static DungeonData CreateAdvancedDungeon()
        {
            var dungeon = ScriptableObject.CreateInstance<DungeonData>();
            
            SetDungeonField(dungeon, "dungeonName", "고대의 유적");
            SetDungeonField(dungeon, "description", "고대 문명의 비밀이 숨겨진 위험한 유적입니다.");
            SetDungeonField(dungeon, "dungeonType", DungeonType.Boss);
            SetDungeonField(dungeon, "difficulty", DungeonDifficulty.Hard);
            SetDungeonField(dungeon, "maxFloors", 7);
            SetDungeonField(dungeon, "maxPlayers", 12);
            SetDungeonField(dungeon, "recommendedLevel", 8);
            SetDungeonField(dungeon, "timeLimit", 3600f); // 1시간
            SetDungeonField(dungeon, "allowPvP", true);
            SetDungeonField(dungeon, "allowRevive", false);
            
            SetDungeonField(dungeon, "baseExpReward", 2000L);
            SetDungeonField(dungeon, "baseGoldReward", 1000L);
            SetDungeonField(dungeon, "expMultiplierPerFloor", 1.5f);
            SetDungeonField(dungeon, "goldMultiplierPerFloor", 1.4f);
            SetDungeonField(dungeon, "completionBonusMultiplier", 3.0f);
            
            // 시간 관리 설정 - 고급 던전은 더 엄격하게 (총 시간 제한)
            SetDungeonField(dungeon, "timeMode", DungeonTimeMode.Total);
            
            return dungeon;
        }
        
        /// <summary>
        /// 최고급 나이트메어 던전 생성
        /// </summary>
        public static DungeonData CreateNightmareDungeon()
        {
            var dungeon = ScriptableObject.CreateInstance<DungeonData>();
            
            SetDungeonField(dungeon, "dungeonName", "지옥의 문");
            SetDungeonField(dungeon, "description", "최강의 모험가만이 도전할 수 있는 지옥같은 던전입니다.");
            SetDungeonField(dungeon, "dungeonType", DungeonType.Challenge);
            SetDungeonField(dungeon, "difficulty", DungeonDifficulty.Nightmare);
            SetDungeonField(dungeon, "maxFloors", 10);
            SetDungeonField(dungeon, "maxPlayers", 16);
            SetDungeonField(dungeon, "recommendedLevel", 12);
            SetDungeonField(dungeon, "timeLimit", 5400f); // 1.5시간
            SetDungeonField(dungeon, "allowPvP", true);
            SetDungeonField(dungeon, "allowRevive", false);
            
            SetDungeonField(dungeon, "baseExpReward", 5000L);
            SetDungeonField(dungeon, "baseGoldReward", 2500L);
            SetDungeonField(dungeon, "expMultiplierPerFloor", 1.8f);
            SetDungeonField(dungeon, "goldMultiplierPerFloor", 1.6f);
            SetDungeonField(dungeon, "completionBonusMultiplier", 5.0f);
            
            // 시간 관리 설정 - 나이트메어 던전은 극한의 시간 압박
            SetDungeonField(dungeon, "timeMode", DungeonTimeMode.Total);
            // timeLimit는 이미 5400f (1.5시간)로 설정됨
            
            return dungeon;
        }
        
        /// <summary>
        /// PvP 던전 생성
        /// </summary>
        public static DungeonData CreatePvPDungeon()
        {
            var dungeon = ScriptableObject.CreateInstance<DungeonData>();
            
            SetDungeonField(dungeon, "dungeonName", "투기장");
            SetDungeonField(dungeon, "description", "플레이어 간 전투가 허용된 위험한 던전입니다.");
            SetDungeonField(dungeon, "dungeonType", DungeonType.PvP);
            SetDungeonField(dungeon, "difficulty", DungeonDifficulty.Normal);
            SetDungeonField(dungeon, "maxFloors", 5);
            SetDungeonField(dungeon, "maxPlayers", 16);
            SetDungeonField(dungeon, "recommendedLevel", 6);
            SetDungeonField(dungeon, "timeLimit", 2400f); // 40분
            SetDungeonField(dungeon, "allowPvP", true);
            SetDungeonField(dungeon, "allowRevive", false);
            
            SetDungeonField(dungeon, "baseExpReward", 1500L);
            SetDungeonField(dungeon, "baseGoldReward", 750L);
            
            // 시간 관리 설정 - PvP 던전은 짧고 빠르게
            SetDungeonField(dungeon, "timeMode", DungeonTimeMode.PerFloor);
            SetDungeonField(dungeon, "baseFloorTime", 360f); // 6분
            SetDungeonField(dungeon, "timeIncreasePerFloor", 60f); // 층당 1분 추가
            
            return dungeon;
        }
        
        /// <summary>
        /// 모든 기본 던전 생성
        /// </summary>
        public static List<DungeonData> CreateAllBasicDungeons()
        {
            var dungeons = new List<DungeonData>();
            
            dungeons.Add(CreateBeginnerDungeon());
            dungeons.Add(CreateIntermediateDungeon());
            dungeons.Add(CreateAdvancedDungeon());
            dungeons.Add(CreateNightmareDungeon());
            dungeons.Add(CreatePvPDungeon());
            
            return dungeons;
        }
        
        /// <summary>
        /// Reflection을 사용한 필드 설정 (private 필드 접근)
        /// </summary>
        private static void SetDungeonField(DungeonData dungeon, string fieldName, object value)
        {
            var field = typeof(DungeonData).GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(dungeon, value);
            }
            else
            {
                Debug.LogWarning($"Field '{fieldName}' not found in DungeonData");
            }
        }
        
        /// <summary>
        /// 던전 데이터 유효성 검사
        /// </summary>
        public static bool ValidateDungeonData(DungeonData dungeon)
        {
            if (dungeon == null) return false;
            
            bool isValid = true;
            
            if (string.IsNullOrEmpty(dungeon.DungeonName))
            {
                Debug.LogError("Dungeon name is empty!");
                isValid = false;
            }
            
            if (dungeon.MaxFloors <= 0)
            {
                Debug.LogError("Max floors must be greater than 0!");
                isValid = false;
            }
            
            if (dungeon.TimeLimit <= 0)
            {
                Debug.LogError("Time limit must be greater than 0!");
                isValid = false;
            }
            
            if (dungeon.FloorConfigs.Count > dungeon.MaxFloors)
            {
                Debug.LogWarning($"More floor configurations than max floors!");
            }
            
            return isValid;
        }
    }
}