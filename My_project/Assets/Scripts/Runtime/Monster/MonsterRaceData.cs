using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 몬스터 종족 데이터 (플레이어와 동일한 구조)
    /// 고블린족, 오크족, 언데드족 등의 기본 정보
    /// </summary>
    [CreateAssetMenu(fileName = "New Monster Race Data", menuName = "Dungeon Crawler/Monster/Race Data")]
    public class MonsterRaceData : ScriptableObject
    {
        [Header("Race Information")]
        public MonsterRace raceType;
        public string raceName;
        [TextArea(3, 5)]
        public string description;
        public Sprite raceIcon;
        
        [Header("Base Stats")]
        [SerializeField] private StatBlock baseStats;
        
        [Header("Stat Growth Per Grade")]
        [SerializeField] private StatGrowth gradeGrowth;
        
        [Header("Elemental Affinity")]
        [SerializeField] private ElementalStats elementalAffinity;
        
        [Header("Race Mandatory Skills")]
        [SerializeField] private MonsterSkillReference[] mandatorySkills;
        
        [Header("Available Optional Skills")]
        [SerializeField] private MonsterSkillReference[] availableSkills;
        
        [Header("Experience & Drops")]
        [SerializeField] private long baseExperience = 50;
        [SerializeField] private long baseGold = 10;
        [SerializeField] private float soulDropRate = 0.001f; // 0.1%
        
        [Header("Item Drops")]
        [SerializeField] private MonsterDropItem[] commonDrops; // 공통 드롭 (종족별)
        [SerializeField] private MonsterDropItem[] rareDrops;   // 희귀 드롭 (종족별)
        
        // 프로퍼티들
        public StatBlock BaseStats => baseStats;
        public StatGrowth GradeGrowth => gradeGrowth;
        public ElementalStats ElementalAffinity => elementalAffinity;
        public MonsterSkillReference[] MandatorySkills => mandatorySkills;
        public MonsterSkillReference[] AvailableSkills => availableSkills;
        public long BaseExperience => baseExperience;
        public long BaseGold => baseGold;
        public float SoulDropRate => soulDropRate;
        public MonsterDropItem[] CommonDrops => commonDrops;
        public MonsterDropItem[] RareDrops => rareDrops;
        
        /// <summary>
        /// 등급에 따른 스탯 계산 (플레이어 레벨과 유사)
        /// </summary>
        public StatBlock CalculateStatsForGrade(float grade)
        {
            // 80-120 등급을 80-120% 스탯 배율로 변환
            float multiplier = grade / 100f; // 80 = 0.8배, 100 = 1.0배, 120 = 1.2배
            
            return new StatBlock
            {
                strength = baseStats.strength * multiplier,
                agility = baseStats.agility * multiplier,
                vitality = baseStats.vitality * multiplier,
                intelligence = baseStats.intelligence * multiplier,
                defense = baseStats.defense * multiplier,
                magicDefense = baseStats.magicDefense * multiplier,
                luck = baseStats.luck * multiplier,
                stability = baseStats.stability * multiplier
            };
        }
        
        /// <summary>
        /// 등급별 배율 계산 (80-120 등급 시스템에서는 직접 사용)
        /// </summary>
        private float GetGradeMultiplier(float grade)
        {
            return grade / 100f; // 80 = 0.8배, 100 = 1.0배, 120 = 1.2배
        }
        
        /// <summary>
        /// 등급에 따른 경험치 계산
        /// </summary>
        public long CalculateExperienceForGrade(float grade)
        {
            float multiplier = GetGradeMultiplier(grade);
            return (long)(baseExperience * multiplier);
        }
        
        /// <summary>
        /// 등급에 따른 골드 계산
        /// </summary>
        public long CalculateGoldForGrade(float grade)
        {
            float multiplier = GetGradeMultiplier(grade);
            return (long)(baseGold * multiplier);
        }
        
        /// <summary>
        /// 등급에 따른 영혼 드롭률 계산
        /// </summary>
        public float CalculateSoulDropRateForGrade(float grade)
        {
            float multiplier = GetGradeMultiplier(grade);
            return Mathf.Min(soulDropRate * multiplier, 1f); // 최대 100%
        }
        
        /// <summary>
        /// 등급에 따른 아이템 드롭 계산
        /// </summary>
        public List<ItemData> CalculateItemDrops(float grade)
        {
            var droppedItems = new List<ItemData>();
            
            // 등급에 따른 드롭 보너스 (80=0.8배, 100=1.0배, 120=1.2배)
            float gradeMultiplier = grade / 100f;
            
            // 공통 드롭 체크
            if (commonDrops != null)
            {
                foreach (var dropItem in commonDrops)
                {
                    float adjustedDropRate = dropItem.dropRate * gradeMultiplier;
                    if (Random.value < adjustedDropRate)
                    {
                        droppedItems.Add(dropItem.item);
                    }
                }
            }
            
            // 희귀 드롭 체크 (등급이 높을수록 확률 증가)
            if (rareDrops != null)
            {
                float rareBonusMultiplier = Mathf.Pow(gradeMultiplier, 1.5f); // 희귀 아이템은 등급 영향 더 크게
                foreach (var dropItem in rareDrops)
                {
                    float adjustedDropRate = dropItem.dropRate * rareBonusMultiplier;
                    if (Random.value < adjustedDropRate)
                    {
                        droppedItems.Add(dropItem.item);
                    }
                }
            }
            
            return droppedItems;
        }
    }
    
    /// <summary>
    /// 몬스터 종족 타입
    /// </summary>
    public enum MonsterRace
    {
        Goblin,     // 고블린족 - 민첩형
        Orc,        // 오크족 - 힘형
        Undead,     // 언데드족 - 마법형
        Beast,      // 야수족 - 물리형
        Elemental,  // 정령족 - 원소형
        Demon,      // 악마족 - 혼합형
        Dragon,     // 드래곤족 - 최상급
        Construct,  // 기계족 - 방어형
        Spider,     // 거미족 - 속박/독
        Serpent,    // 뱀족 - 독/민첩
        Fungal,     // 균류족 - 포자/DoT
        Bandit,     // 산적족 - 인간형/강탈
        Cultist,    // 광신도족 - 암흑마법/소환
        Drowned,    // 수몰족 - 물/저주
        Insect,     // 곤충족 - 떼/빠름
        Golem       // 골렘족 - 돌/방어
    }
    
    /// <summary>
    /// 몬스터 드롭 아이템 정의
    /// </summary>
    [System.Serializable]
    public struct MonsterDropItem
    {
        [Header("Drop Item")]
        public ItemData item;           // 드롭될 아이템
        public float dropRate;          // 드롭 확률 (0.0 ~ 1.0)
        public int minQuantity;         // 최소 개수
        public int maxQuantity;         // 최대 개수
        
        [Header("Level Requirements")]
        public int minLevel;            // 최소 등급 (80~120)
        public int maxLevel;            // 최대 등급 (80~120)
        
        /// <summary>
        /// 해당 등급에서 드롭 가능한지 확인
        /// </summary>
        public bool CanDropAtLevel(float level)
        {
            return level >= minLevel && level <= maxLevel;
        }
        
        /// <summary>
        /// 실제 드롭 개수 계산
        /// </summary>
        public int GetRandomQuantity()
        {
            return Random.Range(minQuantity, maxQuantity + 1);
        }
    }
    
    /// <summary>
    /// 몬스터 스킬 참조 (스킬 + 등급 범위)
    /// </summary>
    [System.Serializable]
    public struct MonsterSkillReference
    {
        public MonsterSkillData skillData;
        public float minGrade; // 80~120 범위
        public float maxGrade; // 80~120 범위
        public bool isMandatory; // 필수 스킬 여부
        
        public bool IsAvailableForGrade(float grade)
        {
            return grade >= minGrade && grade <= maxGrade;
        }
        
        public bool IsAvailableForLevel(int level)
        {
            return IsAvailableForGrade(level);
        }
    }
}