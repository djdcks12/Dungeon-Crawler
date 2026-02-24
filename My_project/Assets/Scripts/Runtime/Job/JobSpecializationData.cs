using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 직업 특성화 데이터 (ScriptableObject)
    /// 각 직업당 2가지 특성화 경로
    /// </summary>
    [CreateAssetMenu(fileName = "New Specialization", menuName = "Dungeon Crawler/Job Specialization Data")]
    public class JobSpecializationData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string specId;
        [SerializeField] private string specName;
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private Sprite specIcon;

        [Header("직업 연결")]
        [SerializeField] private JobType parentJob;
        [SerializeField] private int specIndex; // 0 = A경로, 1 = B경로

        [Header("요구사항")]
        [SerializeField] private int requiredLevel = 10;
        [SerializeField] private int resetGoldCost = 50000; // 리셋 비용

        [Header("스탯 보너스")]
        [SerializeField] private StatBlock statBonus;
        [SerializeField] private float hpBonusPercent;
        [SerializeField] private float mpBonusPercent;

        [Header("전투 보너스")]
        [SerializeField] private float critRateBonusPercent;
        [SerializeField] private float critDamageBonusPercent;
        [SerializeField] private float attackSpeedBonusPercent;
        [SerializeField] private float cooldownReductionPercent;
        [SerializeField] private float lifestealPercent;

        [Header("패시브 효과 3개")]
        [SerializeField] private SpecPassive passive1;
        [SerializeField] private SpecPassive passive2;
        [SerializeField] private SpecPassive passive3;

        [Header("특성 스킬")]
        [SerializeField] private string specSkillName;
        [TextArea(1, 2)]
        [SerializeField] private string specSkillDescription;
        [SerializeField] private float specSkillDamageMultiplier = 1.5f;
        [SerializeField] private float specSkillCooldown = 30f;
        [SerializeField] private int specSkillManaCost = 50;

        // Properties
        public string SpecId => specId;
        public string SpecName => specName;
        public string Description => description;
        public Sprite SpecIcon => specIcon;
        public JobType ParentJob => parentJob;
        public int SpecIndex => specIndex;
        public int RequiredLevel => requiredLevel;
        public int ResetGoldCost => resetGoldCost;
        public StatBlock StatBonus => statBonus;
        public float HPBonusPercent => hpBonusPercent;
        public float MPBonusPercent => mpBonusPercent;
        public float CritRateBonusPercent => critRateBonusPercent;
        public float CritDamageBonusPercent => critDamageBonusPercent;
        public float AttackSpeedBonusPercent => attackSpeedBonusPercent;
        public float CooldownReductionPercent => cooldownReductionPercent;
        public float LifestealPercent => lifestealPercent;
        public SpecPassive Passive1 => passive1;
        public SpecPassive Passive2 => passive2;
        public SpecPassive Passive3 => passive3;
        public string SpecSkillName => specSkillName;
        public string SpecSkillDescription => specSkillDescription;
        public float SpecSkillDamageMultiplier => specSkillDamageMultiplier;
        public float SpecSkillCooldown => specSkillCooldown;
        public int SpecSkillManaCost => specSkillManaCost;
    }

    [System.Serializable]
    public class SpecPassive
    {
        public string passiveName;
        [TextArea(1, 2)]
        public string passiveDescription;
        public SpecPassiveType passiveType;
        public float value;
    }

    public enum SpecPassiveType
    {
        DamageIncrease,         // 데미지 증가%
        DamageReduction,        // 데미지 감소%
        SkillDamageIncrease,    // 스킬 데미지 증가%
        HealingIncrease,        // 치유량 증가%
        GoldBonusPercent,       // 골드 획득 증가%
        ExpBonusPercent,        // 경험치 증가%
        DropRateIncrease,       // 드롭률 증가%
        CriticalImprove,        // 크리티컬 강화%
        ElementalDamage,        // 원소 데미지 증가%
        MovementSpeed,          // 이동속도 증가%
        ManaReduction,          // 마나 소모 감소%
        CooldownReduction,      // 쿨다운 감소%
        DotDamageIncrease,      // DoT 데미지 증가%
        AoEDamageIncrease,      // 광역 데미지 증가%
        SummonStrength,         // 소환수 강화%
        CounterAttackChance,    // 반격 확률%
        BlockChance,            // 방어 확률%
        DodgeChance             // 회피 확률%
    }
}
