using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 펫 데이터 ScriptableObject
    /// 전투/수집/버프 3종 펫, 레벨업 및 진화 지원
    /// </summary>
    [CreateAssetMenu(fileName = "New Pet", menuName = "Dungeon Crawler/Pet Data")]
    public class PetData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string petId;
        [SerializeField] private string petName;
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private Sprite petIcon;

        [Header("분류")]
        [SerializeField] private PetType petType;
        [SerializeField] private PetRarity rarity;
        [SerializeField] private PetElement element;

        [Header("기본 스탯")]
        [SerializeField] private int baseHP = 100;
        [SerializeField] private int baseATK = 10;
        [SerializeField] private int baseDEF = 5;
        [SerializeField] private float baseMoveSpeed = 3f;

        [Header("레벨링")]
        [SerializeField] private float hpGrowth = 10f;       // 레벨당 HP 증가
        [SerializeField] private float atkGrowth = 2f;       // 레벨당 ATK 증가
        [SerializeField] private float defGrowth = 1f;       // 레벨당 DEF 증가
        [SerializeField] private int maxLevel = 30;
        [SerializeField] private int baseExpRequired = 50;    // 레벨 1→2 필요 경험치

        [Header("특수 능력")]
        [SerializeField] private PetAbility[] abilities;      // 레벨별 해금 능력

        [Header("버프 (버프형 펫)")]
        [SerializeField] private StatBlock ownerStatBonus;    // 주인에게 부여하는 스탯
        [SerializeField] private float bonusGoldRate;          // 골드 획득 보너스 %
        [SerializeField] private float bonusExpRate;           // 경험치 보너스 %
        [SerializeField] private float bonusDropRate;          // 드롭률 보너스 %

        [Header("수집 (수집형 펫)")]
        [SerializeField] private float autoPickupRadius = 3f;  // 자동 줍기 반경
        [SerializeField] private bool canPickupGold = true;
        [SerializeField] private bool canPickupItems = false;
        [SerializeField] private float pickupCooldown = 0.5f;

        [Header("전투 (전투형 펫)")]
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private DamageType damageType = DamageType.Physical;

        [Header("진화")]
        [SerializeField] private string evolveToPetId;         // 진화 대상 펫 ID
        [SerializeField] private int evolveLevel = 0;          // 진화 필요 레벨 (0=진화 없음)
        [SerializeField] private string evolveMaterialId;      // 진화 필요 아이템 ID
        [SerializeField] private int evolveMaterialCount = 1;

        [Header("획득")]
        [SerializeField] private long purchasePrice;           // 구매 가격
        [SerializeField] private string dropSource;            // 드롭 출처 설명

        // Properties
        public string PetId => petId;
        public string PetName => petName;
        public string Description => description;
        public Sprite PetIcon => petIcon;
        public PetType PetType => petType;
        public PetRarity Rarity => rarity;
        public PetElement Element => element;
        public int BaseHP => baseHP;
        public int BaseATK => baseATK;
        public int BaseDEF => baseDEF;
        public float BaseMoveSpeed => baseMoveSpeed;
        public float HPGrowth => hpGrowth;
        public float ATKGrowth => atkGrowth;
        public float DEFGrowth => defGrowth;
        public int MaxLevel => maxLevel;
        public int BaseExpRequired => baseExpRequired;
        public PetAbility[] Abilities => abilities;
        public StatBlock OwnerStatBonus => ownerStatBonus;
        public float BonusGoldRate => bonusGoldRate;
        public float BonusExpRate => bonusExpRate;
        public float BonusDropRate => bonusDropRate;
        public float AutoPickupRadius => autoPickupRadius;
        public bool CanPickupGold => canPickupGold;
        public bool CanPickupItems => canPickupItems;
        public float PickupCooldown => pickupCooldown;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
        public DamageType DamageType => damageType;
        public string EvolveToPetId => evolveToPetId;
        public int EvolveLevel => evolveLevel;
        public string EvolveMaterialId => evolveMaterialId;
        public int EvolveMaterialCount => evolveMaterialCount;
        public long PurchasePrice => purchasePrice;
        public string DropSource => dropSource;

        /// <summary>
        /// 레벨별 HP 계산
        /// </summary>
        public int GetHP(int level) => Mathf.RoundToInt(baseHP + hpGrowth * (level - 1));

        /// <summary>
        /// 레벨별 ATK 계산
        /// </summary>
        public int GetATK(int level) => Mathf.RoundToInt(baseATK + atkGrowth * (level - 1));

        /// <summary>
        /// 레벨별 DEF 계산
        /// </summary>
        public int GetDEF(int level) => Mathf.RoundToInt(baseDEF + defGrowth * (level - 1));

        /// <summary>
        /// 레벨업 필요 경험치 계산
        /// </summary>
        public int GetExpRequired(int level) => Mathf.RoundToInt(baseExpRequired * (1 + (level - 1) * 0.5f));

        /// <summary>
        /// 진화 가능 여부
        /// </summary>
        public bool CanEvolve => evolveLevel > 0 && !string.IsNullOrEmpty(evolveToPetId);
    }

    /// <summary>
    /// 펫 타입
    /// </summary>
    public enum PetType
    {
        Combat,     // 전투형 (적 공격)
        Collector,  // 수집형 (아이템/골드 자동 줍기)
        Buffer      // 버프형 (주인 스탯 증가)
    }

    /// <summary>
    /// 펫 희귀도
    /// </summary>
    public enum PetRarity
    {
        Common,     // 일반
        Uncommon,   // 비일반
        Rare,       // 희귀
        Epic,       // 서사
        Legendary   // 전설
    }

    /// <summary>
    /// 펫 원소
    /// </summary>
    public enum PetElement
    {
        None,       // 무속성
        Fire,       // 화염
        Ice,        // 냉기
        Lightning,  // 번개
        Poison,     // 독
        Holy,       // 신성
        Dark        // 암흑
    }

    /// <summary>
    /// 펫 능력 (레벨별 해금)
    /// </summary>
    [System.Serializable]
    public struct PetAbility
    {
        public string abilityName;        // 능력 이름
        [TextArea(1, 3)]
        public string abilityDescription; // 능력 설명
        public int unlockLevel;           // 해금 레벨
        public PetAbilityType abilityType;
        public float value;               // 능력 수치
        public float cooldown;            // 쿨다운 (초)
    }

    /// <summary>
    /// 펫 능력 타입
    /// </summary>
    public enum PetAbilityType
    {
        AttackBoost,    // 공격력 증가
        DefenseBoost,   // 방어력 증가
        HealOwner,      // 주인 회복
        Taunt,          // 도발 (적 어그로)
        PickupRange,    // 줍기 범위 증가
        ExpBoost,       // 경험치 보너스
        GoldBoost,      // 골드 보너스
        DropBoost,      // 드롭률 보너스
        ElementalAttack,// 원소 공격
        Shield,         // 보호막 생성
        Revive,         // 주인 부활 (1회)
        Sprint          // 이동속도 증가
    }
}
