using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 몬스터 개체 데이터 (종족 내의 세부 분류)
    /// 예: 고블린족 → 고블린 워리어, 고블린 샤먼, 일반 고블린
    /// </summary>
    [CreateAssetMenu(fileName = "New Monster Variant Data", menuName = "Dungeon Crawler/Monster/Variant Data")]
    public class MonsterVariantData : ScriptableObject
    {
        [Header("Variant Information")]
        [SerializeField] private string variantId;
        public string variantName;
        [TextArea(2, 4)]
        public string description;
        public Sprite variantIcon;
        public GameObject prefab;
        
        [Header("Race Reference")]
        public MonsterRaceData baseRace;
        
        [Header("Stat Variations")]
        [SerializeField] private StatBlock statMinVariance; // 최소 편차
        [SerializeField] private StatBlock statMaxVariance; // 최대 편차
        
        [Header("Variant Mandatory Skills")]
        [SerializeField] private MonsterSkillReference[] variantMandatorySkills;
        
        [Header("Variant Available Skills")]
        [SerializeField] private MonsterSkillReference[] variantAvailableSkills;
        
        [Header("Spawn Settings")]
        [SerializeField] private float spawnWeight = 1f;
        [SerializeField] private int minFloor = 1;
        [SerializeField] private int maxFloor = 10;
        
        [Header("AI Behavior")]
        [SerializeField] private MonsterAIType preferredAIType = MonsterAIType.Aggressive;
        [SerializeField] private float aggressionMultiplier = 1f;
        
        [Header("Variant Drops")]
        [SerializeField] private MonsterDropItem[] variantDrops; // 개체별 특별 드롭
        
        [Header("Animation Sprites")]
        [SerializeField] private Sprite[] idleSprites; // Idle 상태 스프라이트 배열
        [SerializeField] private Sprite[] moveSprites; // 이동 상태 스프라이트 배열
        [SerializeField] private Sprite[] attackSprites; // 공격 상태 스프라이트 배열
        [SerializeField] private int attackDamageFrame = 2; // 공격 애니메이션 중 몇 번째 프레임에서 데미지 적용 (0-based)
        [SerializeField] private Sprite[] hitSprites; // 피격 상태 스프라이트 배열
        [SerializeField] private Sprite[] castingSprites; // 스킬 시전 상태 스프라이트 배열
        [SerializeField] private Sprite[] deathSprites; // 사망 상태 스프라이트 배열
        
        [Header("Animation Settings")]
        [SerializeField] private float idleFrameRate = 6f;
        [SerializeField] private float moveFrameRate = 8f;
        [SerializeField] private float attackFrameRate = 12f;
        [SerializeField] private float castingFrameRate = 10f;
        [SerializeField] private float hitFrameRate = 10f;
        [SerializeField] private float deathFrameRate = 6f;
        
        [Header("Effects")]
        [SerializeField] private EffectData attackEffect; // 몬스터 공격 이펙트
        
        // 프로퍼티들
        public string VariantId => !string.IsNullOrEmpty(variantId) ? variantId : name; // variantId가 없으면 ScriptableObject name 사용
        public StatBlock StatMinVariance => statMinVariance;
        public StatBlock StatMaxVariance => statMaxVariance;
        public MonsterSkillReference[] VariantMandatorySkills => variantMandatorySkills;
        public MonsterSkillReference[] VariantAvailableSkills => variantAvailableSkills;
        public float SpawnWeight => spawnWeight;
        public int MinFloor => minFloor;
        public int MaxFloor => maxFloor;
        public MonsterAIType PreferredAIType => preferredAIType;
        public float AggressionMultiplier => aggressionMultiplier;
        public MonsterDropItem[] VariantDrops => variantDrops;
        public EffectData AttackEffect => attackEffect;
        
        // Animation 프로퍼티들
        public Sprite[] IdleSprites => idleSprites;
        public Sprite[] MoveSprites => moveSprites;
        public Sprite[] AttackSprites => attackSprites;
        public Sprite[] CastingSprites => castingSprites;
        public Sprite[] HitSprites => hitSprites;
        public Sprite[] DeathSprites => deathSprites;
        public int AttackDamageFrame => attackDamageFrame;
        public float IdleFrameRate => idleFrameRate;
        public float MoveFrameRate => moveFrameRate;
        public float AttackFrameRate => attackFrameRate;
        public float CastingFrameRate => castingFrameRate;
        public float HitFrameRate => hitFrameRate;
        public float DeathFrameRate => deathFrameRate;

        /// <summary>
        /// 개체별 스탯 편차 적용
        /// </summary>
        public StatBlock ApplyVarianceToStats(StatBlock baseStats)
        {
            return new StatBlock
            {
                strength = baseStats.strength + Random.Range(statMinVariance.strength, statMaxVariance.strength),
                agility = baseStats.agility + Random.Range(statMinVariance.agility, statMaxVariance.agility),
                vitality = baseStats.vitality + Random.Range(statMinVariance.vitality, statMaxVariance.vitality),
                intelligence = baseStats.intelligence + Random.Range(statMinVariance.intelligence, statMaxVariance.intelligence),
                defense = baseStats.defense + Random.Range(statMinVariance.defense, statMaxVariance.defense),
                magicDefense = baseStats.magicDefense + Random.Range(statMinVariance.magicDefense, statMaxVariance.magicDefense),
                luck = baseStats.luck + Random.Range(statMinVariance.luck, statMaxVariance.luck),
                stability = baseStats.stability + Random.Range(statMinVariance.stability, statMaxVariance.stability)
            };
        }
        
        /// <summary>
        /// 이 개체가 해당 층에 스폰 가능한지 확인
        /// </summary>
        public bool CanSpawnOnFloor(int floor)
        {
            return floor >= minFloor && floor <= maxFloor;
        }
        
        /// <summary>
        /// 개체의 모든 필수 스킬 가져오기 (종족 + 개체)
        /// </summary>
        public List<MonsterSkillReference> GetAllMandatorySkills(float grade)
        {
            var allSkills = new List<MonsterSkillReference>();
            
            // 종족 필수 스킬
            if (baseRace != null && baseRace.MandatorySkills != null)
            {
                foreach (var skill in baseRace.MandatorySkills)
                {
                    if (skill.IsAvailableForGrade(grade))
                    {
                        allSkills.Add(skill);
                    }
                }
            }
            
            // 개체 필수 스킬
            if (variantMandatorySkills != null)
            {
                foreach (var skill in variantMandatorySkills)
                {
                    if (skill.IsAvailableForGrade(grade))
                    {
                        allSkills.Add(skill);
                    }
                }
            }
            
            return allSkills;
        }
        
        /// <summary>
        /// 개체의 모든 선택 가능한 스킬 가져오기 (종족 + 개체)
        /// </summary>
        public List<MonsterSkillReference> GetAllAvailableSkills(float grade)
        {
            var allSkills = new List<MonsterSkillReference>();
            
            // 종족 선택 스킬
            if (baseRace != null && baseRace.AvailableSkills != null)
            {
                foreach (var skill in baseRace.AvailableSkills)
                {
                    if (skill.IsAvailableForGrade(grade))
                    {
                        allSkills.Add(skill);
                    }
                }
            }
            
            // 개체 선택 스킬
            if (variantAvailableSkills != null)
            {
                foreach (var skill in variantAvailableSkills)
                {
                    if (skill.IsAvailableForGrade(grade))
                    {
                        allSkills.Add(skill);
                    }
                }
            }
            
            return allSkills;
        }
        
        /// <summary>
        /// 등급에 따른 선택 스킬 개수 계산
        /// </summary>
        public int CalculateOptionalSkillCount(float grade)
        {
            // 80~120 등급을 기반으로 스킬 개수 결정
            // 80 = 1~2개, 100 = 2~3개, 120 = 3~4개
            int baseSkillCount = Mathf.RoundToInt((grade - 80f) / 20f) + 1; // 80=1, 100=2, 120=3
            
            return Random.Range(baseSkillCount, baseSkillCount + 2); // +0~1개 추가
        }
        
        /// <summary>
        /// 전체 아이템 드롭 계산 (종족 + 개체)
        /// </summary>
        public List<ItemData> CalculateAllItemDrops(float grade)
        {
            var droppedItems = new List<ItemData>();
            
            Debug.Log($"🎲 CalculateAllItemDrops: variant={variantName}, grade={grade}");
            
            // 종족 기본 드롭
            if (baseRace != null)
            {
                Debug.Log($"🎲 Checking base race drops: {baseRace.raceName}");
                var raceDrops = baseRace.CalculateItemDrops(grade);
                droppedItems.AddRange(raceDrops);
                Debug.Log($"🎲 Base race dropped {raceDrops.Count} items");
            }
            else
            {
                Debug.LogWarning($"🎲 baseRace is null for {variantName}!");
            }
            
            // 개체별 특별 드롭
            if (variantDrops != null && variantDrops.Length > 0)
            {
                Debug.Log($"🎲 Checking variant drops: {variantDrops.Length} drop entries");
                float gradeMultiplier = grade / 100f;
                Debug.Log($"🎲 Grade multiplier: {gradeMultiplier}");
                
                foreach (var dropItem in variantDrops)
                {
                    Debug.Log($"🎲 Checking drop: {dropItem.item?.ItemName ?? "NULL"}, dropRate={dropItem.dropRate}");
                    
                    if (dropItem.CanDropAtLevel(grade))
                    {
                        float adjustedDropRate = dropItem.dropRate * gradeMultiplier;
                        float randomValue = Random.value;
                        Debug.Log($"🎲 Drop check: adjustedRate={adjustedDropRate}, random={randomValue}, success={randomValue < adjustedDropRate}");
                        
                        if (randomValue < adjustedDropRate)
                        {
                            droppedItems.Add(dropItem.item);
                            Debug.Log($"🎲 ✅ Dropped: {dropItem.item.ItemName}");
                        }
                        else
                        {
                            Debug.Log($"🎲 ❌ Failed to drop: {dropItem.item?.ItemName ?? "NULL"}");
                        }
                    }
                    else
                    {
                        Debug.Log($"🎲 ❌ Item cannot drop at grade {grade}");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"🎲 variantDrops is null or empty for {variantName}!");
            }
            
            Debug.Log($"🎲 Total items calculated: {droppedItems.Count}");
            return droppedItems;
        }
        
        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        [ContextMenu("Show Variant Info")]
        public void ShowVariantInfo()
        {
            Debug.Log($"=== {variantName} ({baseRace?.raceName}) ===");
            Debug.Log($"Stat Variance - STR: {statMinVariance.strength}~{statMaxVariance.strength}");
            Debug.Log($"Spawn Weight: {spawnWeight}, Floor: {minFloor}-{maxFloor}");
            Debug.Log($"Mandatory Skills: {variantMandatorySkills?.Length ?? 0}");
            Debug.Log($"Available Skills: {variantAvailableSkills?.Length ?? 0}");
        }
    }
}