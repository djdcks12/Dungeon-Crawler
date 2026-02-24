using UnityEngine;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 패시브 스킬 트리 노드 데이터 (ScriptableObject)
    /// 각 종족별 패시브 트리 (20노드), 레벨업 시 포인트 획득하여 투자
    /// </summary>
    [CreateAssetMenu(fileName = "New Passive Node", menuName = "Dungeon Crawler/Passive Skill Node")]
    public class PassiveSkillTreeData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string nodeId;
        [SerializeField] private string nodeName;
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private Sprite nodeIcon;

        [Header("분류")]
        [SerializeField] private Race requiredRace;
        [SerializeField] private PassiveNodeType nodeType;
        [SerializeField] private PassiveNodeTier tier;          // 1~4티어 (거리 = 연결 순서)

        [Header("비용")]
        [SerializeField] private int pointCost = 1;             // 패시브 포인트 비용
        [SerializeField] private int requiredLevel = 1;         // 필요 캐릭터 레벨

        [Header("선행 노드")]
        [SerializeField] private string[] prerequisiteNodeIds;  // 선행 노드 ID

        [Header("스탯 보너스")]
        [SerializeField] private StatBlock statBonus;
        [SerializeField] private float hpBonus;
        [SerializeField] private float mpBonus;
        [SerializeField] private float hpPercentBonus;           // HP% 증가
        [SerializeField] private float mpPercentBonus;           // MP% 증가

        [Header("특수 효과")]
        [SerializeField] private float critChanceBonus;          // 크리티컬 확률 보너스
        [SerializeField] private float critDamageBonus;          // 크리티컬 데미지 보너스
        [SerializeField] private float moveSpeedBonus;           // 이동속도 보너스%
        [SerializeField] private float attackSpeedBonus;         // 공격속도 보너스%
        [SerializeField] private float cooldownReduction;        // 쿨다운 감소%
        [SerializeField] private float lifestealPercent;         // 생명력 흡수%
        [SerializeField] private float manaRegenBonus;           // 마나 재생 보너스

        [Header("키스톤 효과 (양면)")]
        [SerializeField] private bool isKeystone;                // 키스톤 노드 여부
        [SerializeField] private string keystonePositiveDesc;    // 긍정 효과 설명
        [SerializeField] private string keystoneNegativeDesc;    // 부정 효과 설명
        [SerializeField] private float keystonePositiveValue;    // 긍정 수치
        [SerializeField] private float keystoneNegativeValue;    // 부정 수치
        [SerializeField] private PassiveKeystoneType keystoneType;

        [Header("트리 시각화")]
        [SerializeField] private Vector2 treePosition;           // 트리에서의 위치 (UI용)

        // Properties
        public string NodeId => nodeId;
        public string NodeName => nodeName;
        public string Description => description;
        public Sprite NodeIcon => nodeIcon;
        public Race RequiredRace => requiredRace;
        public PassiveNodeType NodeType => nodeType;
        public PassiveNodeTier Tier => tier;
        public int PointCost => pointCost;
        public int RequiredLevel => requiredLevel;
        public string[] PrerequisiteNodeIds => prerequisiteNodeIds;
        public StatBlock StatBonus => statBonus;
        public float HPBonus => hpBonus;
        public float MPBonus => mpBonus;
        public float HPPercentBonus => hpPercentBonus;
        public float MPPercentBonus => mpPercentBonus;
        public float CritChanceBonus => critChanceBonus;
        public float CritDamageBonus => critDamageBonus;
        public float MoveSpeedBonus => moveSpeedBonus;
        public float AttackSpeedBonus => attackSpeedBonus;
        public float CooldownReduction => cooldownReduction;
        public float LifestealPercent => lifestealPercent;
        public float ManaRegenBonus => manaRegenBonus;
        public bool IsKeystone => isKeystone;
        public string KeystonePositiveDesc => keystonePositiveDesc;
        public string KeystoneNegativeDesc => keystoneNegativeDesc;
        public float KeystonePositiveValue => keystonePositiveValue;
        public float KeystoneNegativeValue => keystoneNegativeValue;
        public PassiveKeystoneType KeystoneType => keystoneType;
        public Vector2 TreePosition => treePosition;
    }

    /// <summary>
    /// 패시브 노드 타입
    /// </summary>
    public enum PassiveNodeType
    {
        Minor,      // 소형 (작은 보너스)
        Major,      // 대형 (큰 보너스)
        Keystone    // 키스톤 (강력한 양면 효과)
    }

    /// <summary>
    /// 패시브 노드 티어 (깊이)
    /// </summary>
    public enum PassiveNodeTier
    {
        Tier1 = 1,  // 시작 근처
        Tier2 = 2,  // 중간
        Tier3 = 3,  // 외곽
        Tier4 = 4   // 키스톤 (최외곽)
    }

    /// <summary>
    /// 키스톤 타입 (강력한 양면 효과)
    /// </summary>
    public enum PassiveKeystoneType
    {
        None,
        GlassCannon,        // 데미지 크게 증가, HP 크게 감소
        IronFortress,       // DEF 크게 증가, 이동속도 크게 감소
        BloodMagic,         // 마나 없이 HP로 스킬 사용
        ElementalOverload,  // 원소 데미지 크게 증가, 물리 데미지 감소
        Berserker,          // 낮은 HP에서 데미지 증가
        PerfectBalance,     // 모든 스탯 약간 증가, 크리티컬 불가
        SoulAbsorption,     // 영혼 획득률 크게 증가, 경험치 감소
        LifeLink            // 데미지의 일부를 HP로 회복, 최대HP 감소
    }
}
