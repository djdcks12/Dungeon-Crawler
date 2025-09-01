using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 종족별 기본 스탯 및 성장 데이터
    /// 하드코어 던전 크롤러 종족 시스템
    /// </summary>
    [CreateAssetMenu(fileName = "New Race Data", menuName = "Dungeon Crawler/Race Data")]
    public class RaceData : ScriptableObject
    {
        [Header("Race Information")]
        public Race raceType;
        public string raceName;
        [TextArea(3, 5)]
        public string description;
        public Sprite raceIcon;
        
        [Header("Base Stats (Level 1)")]
        [SerializeField] private StatBlock baseStats;
        
        [Header("Stat Growth Per Level")]
        [SerializeField] private StatGrowth statGrowth;
        
        [Header("Elemental Affinity")]
        [SerializeField] private ElementalStats elementalAffinity;
        
        [Header("Race Specialties")]
        [SerializeField] private RaceSpecialty[] specialties;
        
        [Header("Default Effects")]
        [SerializeField] private EffectData defaultHitEffect; // 맨손 공격 시 기본 이펙트
        
        [Header("Player Sprite Animations")]
        [SerializeField] private Sprite[] idleSprites;
        [SerializeField] private float idleFrameRate = 6f;
        [SerializeField] private Sprite[] walkSprites;
        [SerializeField] private float walkFrameRate = 8f;
        [SerializeField] private Sprite[] attackSprites;
        [SerializeField] private float attackFrameRate = 12f;
        [SerializeField] private Sprite[] castingSprites;
        [SerializeField] private float castingFrameRate = 10f;
        [SerializeField] private Sprite[] deathSprites;
        [SerializeField] private float deathFrameRate = 8f;
        
        // 프로퍼티들
        public StatBlock BaseStats => baseStats;
        public StatGrowth StatGrowth => statGrowth;
        public ElementalStats ElementalAffinity => elementalAffinity;
        public RaceSpecialty[] Specialties => specialties;
        public EffectData DefaultHitEffect => defaultHitEffect;
        
        // 스프라이트 애니메이션 프로퍼티들
        public Sprite[] IdleSprites => idleSprites;
        public float IdleFrameRate => idleFrameRate;
        public Sprite[] WalkSprites => walkSprites;
        public float WalkFrameRate => walkFrameRate;
        public Sprite[] AttackSprites => attackSprites;
        public float AttackFrameRate => attackFrameRate;
        public Sprite[] CastingSprites => castingSprites;
        public float CastingFrameRate => castingFrameRate;
        public Sprite[] DeathSprites => deathSprites;
        public float DeathFrameRate => deathFrameRate;
        
        /// <summary>
        /// 애니메이션 스프라이트 유효성 검사
        /// </summary>
        public bool HasValidIdleAnimation => idleSprites != null && idleSprites.Length > 0;
        public bool HasValidWalkAnimation => walkSprites != null && walkSprites.Length > 0;
        public bool HasValidAttackAnimation => attackSprites != null && attackSprites.Length > 0;
        public bool HasValidCastingAnimation => castingSprites != null && castingSprites.Length > 0;
        public bool HasValidDeathAnimation => deathSprites != null && deathSprites.Length > 0;
        
        /// <summary>
        /// 기본 스프라이트 가져오기 (첫 번째 Idle 스프라이트)
        /// </summary>
        public Sprite GetDefaultSprite()
        {
            if (HasValidIdleAnimation)
            {
                return idleSprites[0];
            }
            return null;
        }
        
        /// <summary>
        /// 특정 레벨에서의 스탯 계산
        /// </summary>
        public StatBlock CalculateStatsAtLevel(int level)
        {
            if (level < 1) level = 1;
            if (level > 15) level = 15;
            
            float growthMultiplier = level - 1; // 1레벨 기준이므로 -1
            
            return new StatBlock
            {
                strength = baseStats.strength + (statGrowth.strengthGrowth * growthMultiplier),
                agility = baseStats.agility + (statGrowth.agilityGrowth * growthMultiplier),
                vitality = baseStats.vitality + (statGrowth.vitalityGrowth * growthMultiplier),
                intelligence = baseStats.intelligence + (statGrowth.intelligenceGrowth * growthMultiplier),
                defense = baseStats.defense + (statGrowth.defenseGrowth * growthMultiplier),
                magicDefense = baseStats.magicDefense + (statGrowth.magicDefenseGrowth * growthMultiplier),
                luck = baseStats.luck + (statGrowth.luckGrowth * growthMultiplier),
                stability = baseStats.stability + (statGrowth.stabilityGrowth * growthMultiplier)
            };
        }
        
        /// <summary>
        /// 종족별 특성 확인
        /// </summary>
        public bool HasSpecialty(RaceSpecialtyType specialtyType)
        {
            foreach (var specialty in specialties)
            {
                if (specialty.specialtyType == specialtyType)
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// 종족별 특성 값 가져오기
        /// </summary>
        public float GetSpecialtyValue(RaceSpecialtyType specialtyType)
        {
            foreach (var specialty in specialties)
            {
                if (specialty.specialtyType == specialtyType)
                    return specialty.value;
            }
            return 0f;
        }
        
        /// <summary>
        /// 종족별 추천 플레이 스타일 정보
        /// </summary>
        public string GetPlayStyleInfo()
        {
            switch (raceType)
            {
                case Race.Human:
                    return "균형잡힌 만능형 - 모든 계열 스킬 학습 가능, 안정적인 성장";
                case Race.Elf:
                    return "마법 특화형 - 높은 마력과 마법방어, 원소마법에 특화";
                case Race.Beast:
                    return "물리 특화형 - 강력한 근접전투, 높은 공격력과 기동력";
                case Race.Machina:
                    return "방어 특화형 - 높은 체력과 방어력, 기술 계열 스킬 특화";
                default:
                    return "알 수 없는 종족";
            }
        }
        
        /// <summary>
        /// 디버그용 종족 정보 출력
        /// </summary>
        public void LogRaceInfo()
        {
            Debug.Log($"=== {raceName} Race Info ===");
            Debug.Log($"Base Stats: STR={baseStats.strength}, AGI={baseStats.agility}, VIT={baseStats.vitality}, INT={baseStats.intelligence}");
            Debug.Log($"Base Stats: DEF={baseStats.defense}, MDEF={baseStats.magicDefense}, LUK={baseStats.luck}");
            Debug.Log($"Growth: STR+{statGrowth.strengthGrowth}, AGI+{statGrowth.agilityGrowth}, VIT+{statGrowth.vitalityGrowth}, INT+{statGrowth.intelligenceGrowth}");
            Debug.Log($"Growth: DEF+{statGrowth.defenseGrowth}, MDEF+{statGrowth.magicDefenseGrowth}, LUK+{statGrowth.luckGrowth}");
        }
    }
    
    /// <summary>
    /// 종족 열거형
    /// </summary>
    public enum Race
    {
        Human,      // 인간 - 균형형
        Elf,        // 엘프 - 마법 특화
        Beast,      // 수인 - 물리 특화
        Machina     // 기계족 - 방어 특화
    }
    
    /// <summary>
    /// 데미지 범위 구조체 (민댐/맥댐 시스템)
    /// </summary>
    [System.Serializable]
    public struct DamageRange
    {
        public float minDamage;    // 최소 데미지
        public float maxDamage;    // 최대 데미지
        public float stability;    // 안정성 (편차 조절)
        
        public DamageRange(float min, float max, float stab = 0f)
        {
            minDamage = min;
            maxDamage = max;
            stability = stab;
        }
        
        /// <summary>
        /// 안정성을 적용한 실제 데미지 범위 계산
        /// </summary>
        public DamageRange GetStabilizedRange(float stabilityBonus)
        {
            float totalStability = stability + stabilityBonus;
            float adjustedMin = minDamage + (totalStability * 0.5f);
            float adjustedMax = maxDamage - (totalStability * 0.3f);
            
            // 맥댐이 민댐보다 작아지지 않도록 보정
            adjustedMax = Mathf.Max(adjustedMax, adjustedMin + 1f);
            
            return new DamageRange(adjustedMin, adjustedMax, totalStability);
        }
        
        /// <summary>
        /// 범위 내에서 랜덤 데미지 계산
        /// </summary>
        public float GetRandomDamage()
        {
            return Random.Range(minDamage, maxDamage);
        }
    }
    
    /// <summary>
    /// 속성 데미지 범위 구조체
    /// </summary>
    [System.Serializable]
    public struct ElementalDamageRange
    {
        public DamageRange fire;
        public DamageRange ice;
        public DamageRange lightning;
        public DamageRange poison;
        public DamageRange dark;
        public DamageRange holy;
    }
    
    /// <summary>
    /// 전투 스탯 구조체 (민댐/맥댐 시스템 포함)
    /// </summary>
    [System.Serializable]
    public struct CombatStats
    {
        public DamageRange physicalDamage;
        public DamageRange magicalDamage;
        public ElementalDamageRange elementalDamage;
        public float criticalChance;
        public float criticalMultiplier;
        public float stability;        // 안정성 스탯
        
        public CombatStats(DamageRange physical, DamageRange magical, float critChance = 0.05f, float critMultiplier = 2.0f, float stab = 0f)
        {
            physicalDamage = physical;
            magicalDamage = magical;
            elementalDamage = new ElementalDamageRange();
            criticalChance = critChance;
            criticalMultiplier = critMultiplier;
            stability = stab;
        }
    }

    /// <summary>
    /// 기본 스탯 구조체
    /// </summary>
    [System.Serializable]
    public struct StatBlock : INetworkSerializable
    {
        public float strength;      // 힘 - 물리 공격력
        public float agility;       // 민첩 - 공격속도, 이동속도, 회피율
        public float vitality;      // 체력 - 최대 HP, HP 재생
        public float intelligence;  // 지능 - 마법 공격력, 최대 MP
        public float defense;       // 물리 방어력
        public float magicDefense;  // 마법 방어력
        public float luck;          // 운 - 드롭률, 치명타 확률
        public float stability;     // 안정성 - 데미지 편차 조절
        
        public StatBlock(float str, float agi, float vit, float inte, float def, float mdef, float luk, float stab = 0f)
        {
            strength = str;
            agility = agi;
            vitality = vit;
            intelligence = inte;
            defense = def;
            magicDefense = mdef;
            luck = luk;
            stability = stab;
        }
        
        public static StatBlock operator +(StatBlock a, StatBlock b)
        {
            return new StatBlock(
                a.strength + b.strength,
                a.agility + b.agility,
                a.vitality + b.vitality,
                a.intelligence + b.intelligence,
                a.defense + b.defense,
                a.magicDefense + b.magicDefense,
                a.luck + b.luck,
                a.stability + b.stability
            );
        }
        
        public static StatBlock operator *(StatBlock stats, float multiplier)
        {
            return new StatBlock(
                stats.strength * multiplier,
                stats.agility * multiplier,
                stats.vitality * multiplier,
                stats.intelligence * multiplier,
                stats.defense * multiplier,
                stats.magicDefense * multiplier,
                stats.luck * multiplier,
                stats.stability * multiplier
            );
        }
        
        /// <summary>
        /// 스탯 중 하나라도 0이 아닌지 확인
        /// </summary>
        public bool HasAnyStats()
        {
            return strength != 0 || agility != 0 || vitality != 0 || intelligence != 0 ||
                   defense != 0 || magicDefense != 0 || luck != 0 || stability != 0;
        }
        
        /// <summary>
        /// 스탯 정보를 텍스트로 반환
        /// </summary>
        public string GetStatsText()
        {
            var statTexts = new System.Collections.Generic.List<string>();
            
            if (strength != 0) statTexts.Add($"STR: {strength:+0;-0}");
            if (agility != 0) statTexts.Add($"AGI: {agility:+0;-0}");
            if (vitality != 0) statTexts.Add($"VIT: {vitality:+0;-0}");
            if (intelligence != 0) statTexts.Add($"INT: {intelligence:+0;-0}");
            if (defense != 0) statTexts.Add($"DEF: {defense:+0;-0}");
            if (magicDefense != 0) statTexts.Add($"MDEF: {magicDefense:+0;-0}");
            if (luck != 0) statTexts.Add($"LUK: {luck:+0;-0}");
            if (stability != 0) statTexts.Add($"STAB: {stability:+0;-0}");
            
            return string.Join("\n", statTexts);
        }
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref strength);
            serializer.SerializeValue(ref agility);
            serializer.SerializeValue(ref vitality);
            serializer.SerializeValue(ref intelligence);
            serializer.SerializeValue(ref defense);
            serializer.SerializeValue(ref magicDefense);
            serializer.SerializeValue(ref luck);
            serializer.SerializeValue(ref stability);
        }
    }
    
    /// <summary>
    /// 레벨당 스탯 성장 구조체
    /// </summary>
    [System.Serializable]
    public struct StatGrowth
    {
        public float strengthGrowth;
        public float agilityGrowth;
        public float vitalityGrowth;
        public float intelligenceGrowth;
        public float defenseGrowth;
        public float magicDefenseGrowth;
        public float luckGrowth;
        public float stabilityGrowth;
        
        public StatGrowth(float str, float agi, float vit, float inte, float def, float mdef, float luk, float stab = 0f)
        {
            strengthGrowth = str;
            agilityGrowth = agi;
            vitalityGrowth = vit;
            intelligenceGrowth = inte;
            defenseGrowth = def;
            magicDefenseGrowth = mdef;
            luckGrowth = luk;
            stabilityGrowth = stab;
        }
    }
    
    /// <summary>
    /// 6대 속성 스탯 구조체
    /// </summary>
    [System.Serializable]
    public struct ElementalStats
    {
        [Header("Fire")]
        public float fireAttack;
        public float fireResist;
        
        [Header("Ice")]
        public float iceAttack;
        public float iceResist;
        
        [Header("Lightning")]
        public float lightningAttack;
        public float lightningResist;
        
        [Header("Poison")]
        public float poisonAttack;
        public float poisonResist;
        
        [Header("Dark")]
        public float darkAttack;
        public float darkResist;
        
        [Header("Holy")]
        public float holyAttack;
        public float holyResist;
    }
    
    /// <summary>
    /// 종족별 특성 구조체
    /// </summary>
    [System.Serializable]
    public struct RaceSpecialty
    {
        public RaceSpecialtyType specialtyType;
        public float value;
        public string description;
    }
    
    /// <summary>
    /// 종족 특성 타입
    /// </summary>
    public enum RaceSpecialtyType
    {
        MagicMastery,           // 마법 숙련도
        PhysicalMastery,        // 물리 숙련도
        TechnicalMastery,       // 기술 숙련도
        DropRateBonus,          // 드롭률 보너스
        ExpBonus,               // 경험치 보너스
        ElementalResistance,    // 속성 저항
        CriticalBonus,          // 치명타 보너스
        MovementBonus           // 이동 보너스
    }
}