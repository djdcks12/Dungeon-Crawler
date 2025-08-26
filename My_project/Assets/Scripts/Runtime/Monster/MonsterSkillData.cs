using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 몬스터 스킬 데이터 (패시브 + 액티브)
    /// 영혼에 포함되어 플레이어가 획득할 수 있는 스킬
    /// </summary>
    [CreateAssetMenu(fileName = "New Monster Skill Data", menuName = "Dungeon Crawler/Monster/Skill Data")]
    public class MonsterSkillData : ScriptableObject
    {
        [Header("Skill Information")]
        public string skillName;
        [TextArea(3, 5)]
        public string description;
        public Sprite skillIcon;
        
        [Header("Skill Type")]
        public MonsterSkillType skillType;
        public MonsterSkillCategory category;
        
        [Header("Skill Effects (Range-based)")]
        [SerializeField] private MonsterSkillEffect skillEffect;
        
        [Header("Activation Conditions")]
        [SerializeField] private float cooldown = 5f;
        [SerializeField] private float manaCost = 0f;
        [SerializeField] private float range = 0f;
        [SerializeField] private MonsterSkillTrigger trigger = MonsterSkillTrigger.Manual;
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private Color skillColor = Color.white;
        [SerializeField] private float effectDuration = 1f;
        
        // 프로퍼티들
        public MonsterSkillType SkillType => skillType;
        public MonsterSkillCategory Category => category;
        public float Cooldown => cooldown;
        public float ManaCost => manaCost;
        public float Range => range;
        public MonsterSkillTrigger Trigger => trigger;
        public GameObject EffectPrefab => effectPrefab;
        public Color SkillColor => skillColor;
        public float EffectDuration => effectDuration;
        
        /// <summary>
        /// 등급에 따른 스킬 효과 가져오기
        /// </summary>
        public MonsterSkillEffect GetSkillEffect()
        {
            return skillEffect;
        }
        
        /// <summary>
        /// 스킬이 패시브인지 확인
        /// </summary>
        public bool IsPassive => skillType == MonsterSkillType.Passive;
        
        /// <summary>
        /// 스킬이 액티브인지 확인
        /// </summary>
        public bool IsActive => skillType == MonsterSkillType.Active;
        
        /// <summary>
        /// 스킬 설명 (범위 표시)
        /// </summary>
        public string GetDescriptionRange()
        {
            return $"{description}\n\n{skillEffect.GetEffectDescriptionRange()}";
        }
        
        /// <summary>
        /// 스킬 설명 (특정 등급)
        /// </summary>
        public string GetDescriptionForGrade(float grade)
        {
            return $"{description}\n\n[Grade {grade:F0}]\n{skillEffect.GetEffectDescriptionForGrade(grade)}";
        }
    }
    
    /// <summary>
    /// 몬스터 스킬 타입
    /// </summary>
    public enum MonsterSkillType
    {
        Passive,    // 패시브 - 항상 적용
        Active      // 액티브 - 발동 조건 필요
    }
    
    /// <summary>
    /// 몬스터 스킬 카테고리
    /// </summary>
    public enum MonsterSkillCategory
    {
        // 공격계
        PhysicalAttack, // 물리 공격
        MagicalAttack,  // 마법 공격
        DamageBonus,    // 데미지 증가
        
        // 방어계
        PhysicalDefense, // 물리 방어
        MagicalDefense,  // 마법 방어
        HealthBonus,     // 체력 증가
        
        // 보조계
        MovementSpeed,   // 이동속도
        AttackSpeed,     // 공격속도
        Regeneration,    // 재생
        
        // 특수계
        ElementalMastery, // 속성 숙련
        StatusResistance, // 상태이상 저항
        SpecialAbility,   // 고유 능력
        
        // 지원계
        AllyBuff,         // 아군 버프
        AuraEffect,       // 오라 효과 (주변 지속 버프)
        Summoning         // 소환
    }
    
    /// <summary>
    /// 몬스터 스킬 트리거
    /// </summary>
    public enum MonsterSkillTrigger
    {
        Manual,         // 수동 발동
        OnCombatStart,  // 전투 시작 시
        OnTakeDamage,   // 피해를 받을 때
        OnDealDamage,   // 피해를 줄 때
        OnLowHealth,    // 체력이 낮을 때
        OnTargetDeath,  // 적 사망 시
        OnCooldown      // 쿨다운마다
    }
    
    /// <summary>
    /// 스킬 효과 범위 (최소~최대값)
    /// </summary>
    [System.Serializable]
    public struct SkillEffectRange : INetworkSerializable
    {
        public float minValue;
        public float maxValue;
        
        public SkillEffectRange(float min, float max)
        {
            minValue = min;
            maxValue = max;
        }
        
        /// <summary>
        /// 등급에 따른 값 계산 (80~120 등급)
        /// </summary>
        public float GetValueForGrade(float grade)
        {
            // 80~120을 0~1로 정규화
            float normalizedGrade = (grade - 80f) / 40f;
            normalizedGrade = Mathf.Clamp01(normalizedGrade);
            
            return Mathf.Lerp(minValue, maxValue, normalizedGrade);
        }
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref minValue);
            serializer.SerializeValue(ref maxValue);
        }
    }
    
    /// <summary>
    /// 복합 스킬 효과 (여러 스탯과 효과 포함)
    /// </summary>
    [System.Serializable]
    public struct MonsterSkillEffect
    {
        [Header("Stat Bonus Ranges")]
        public SkillEffectRange strengthBonus;     // 힘 +범위
        public SkillEffectRange agilityBonus;      // 민첩 +범위
        public SkillEffectRange vitalityBonus;     // 체력 +범위
        public SkillEffectRange intelligenceBonus; // 지능 +범위
        public SkillEffectRange defenseBonus;      // 방어력 +범위
        public SkillEffectRange magicDefenseBonus; // 마법방어력 +범위
        public SkillEffectRange luckBonus;         // 운 +범위
        public SkillEffectRange stabilityBonus;    // 안정성 +범위
        
        [Header("Special Effect Ranges")]
        public SkillEffectRange damageMultiplierRange;  // 데미지 배율 범위
        public SkillEffectRange defenseMultiplierRange; // 방어 배율 범위
        public SkillEffectRange speedMultiplierRange;   // 속도 배율 범위
        public SkillEffectRange healingAmountRange;     // 치유량 범위
        public SkillEffectRange durationRange;          // 지속시간 범위
        
        [Header("Status Effects")]
        public StatusType inflictStatus;    // 부여할 상태이상
        public SkillEffectRange statusDurationRange; // 상태이상 지속시간 범위
        public SkillEffectRange statusChanceRange;   // 상태이상 확률 범위
        
        /// <summary>
        /// 범위 기반 효과 설명 생성
        /// </summary>
        public string GetEffectDescriptionRange()
        {
            var desc = "";
            
            // 스탯 보너스 범위
            if (strengthBonus.maxValue > 0) desc += $"STR +{strengthBonus.minValue:F0}~{strengthBonus.maxValue:F0} ";
            if (agilityBonus.maxValue > 0) desc += $"AGI +{agilityBonus.minValue:F0}~{agilityBonus.maxValue:F0} ";
            if (vitalityBonus.maxValue > 0) desc += $"VIT +{vitalityBonus.minValue:F0}~{vitalityBonus.maxValue:F0} ";
            if (intelligenceBonus.maxValue > 0) desc += $"INT +{intelligenceBonus.minValue:F0}~{intelligenceBonus.maxValue:F0} ";
            if (defenseBonus.maxValue > 0) desc += $"DEF +{defenseBonus.minValue:F0}~{defenseBonus.maxValue:F0} ";
            if (magicDefenseBonus.maxValue > 0) desc += $"MDEF +{magicDefenseBonus.minValue:F0}~{magicDefenseBonus.maxValue:F0} ";
            if (luckBonus.maxValue > 0) desc += $"LUK +{luckBonus.minValue:F0}~{luckBonus.maxValue:F0} ";
            if (stabilityBonus.maxValue > 0) desc += $"STAB +{stabilityBonus.minValue:F0}~{stabilityBonus.maxValue:F0} ";
            
            // 배율 효과 범위
            if (damageMultiplierRange.maxValue > 1f) desc += $"Damage x{damageMultiplierRange.minValue:F1}~{damageMultiplierRange.maxValue:F1} ";
            if (speedMultiplierRange.maxValue > 1f) desc += $"Speed x{speedMultiplierRange.minValue:F1}~{speedMultiplierRange.maxValue:F1} ";
            
            // 치유 범위
            if (healingAmountRange.maxValue > 0) desc += $"Heal {healingAmountRange.minValue:F0}~{healingAmountRange.maxValue:F0} ";
            
            // 상태이상 범위
            if (inflictStatus != StatusType.None && statusChanceRange.maxValue > 0)
            {
                desc += $"{inflictStatus} ({statusChanceRange.minValue:P0}~{statusChanceRange.maxValue:P0}, {statusDurationRange.minValue:F1}~{statusDurationRange.maxValue:F1}s) ";
            }
            
            return desc.Trim();
        }
        
        /// <summary>
        /// 특정 등급에 대한 실제 효과 설명 생성
        /// </summary>
        public string GetEffectDescriptionForGrade(float grade)
        {
            var desc = "";
            
            // 스탯 보너스 (실제 값)
            if (strengthBonus.maxValue > 0) desc += $"STR +{strengthBonus.GetValueForGrade(grade):F0} ";
            if (agilityBonus.maxValue > 0) desc += $"AGI +{agilityBonus.GetValueForGrade(grade):F0} ";
            if (vitalityBonus.maxValue > 0) desc += $"VIT +{vitalityBonus.GetValueForGrade(grade):F0} ";
            if (intelligenceBonus.maxValue > 0) desc += $"INT +{intelligenceBonus.GetValueForGrade(grade):F0} ";
            if (defenseBonus.maxValue > 0) desc += $"DEF +{defenseBonus.GetValueForGrade(grade):F0} ";
            if (magicDefenseBonus.maxValue > 0) desc += $"MDEF +{magicDefenseBonus.GetValueForGrade(grade):F0} ";
            if (luckBonus.maxValue > 0) desc += $"LUK +{luckBonus.GetValueForGrade(grade):F0} ";
            if (stabilityBonus.maxValue > 0) desc += $"STAB +{stabilityBonus.GetValueForGrade(grade):F0} ";
            
            return desc.Trim();
        }
        
        /// <summary>
        /// 특정 등급에 대한 실제 StatBlock 생성
        /// </summary>
        public StatBlock GetStatBlockForGrade(float grade)
        {
            return new StatBlock
            {
                strength = strengthBonus.GetValueForGrade(grade),
                agility = agilityBonus.GetValueForGrade(grade),
                vitality = vitalityBonus.GetValueForGrade(grade),
                intelligence = intelligenceBonus.GetValueForGrade(grade),
                defense = defenseBonus.GetValueForGrade(grade),
                magicDefense = magicDefenseBonus.GetValueForGrade(grade),
                luck = luckBonus.GetValueForGrade(grade),
                stability = stabilityBonus.GetValueForGrade(grade)
            };
        }
    }
}