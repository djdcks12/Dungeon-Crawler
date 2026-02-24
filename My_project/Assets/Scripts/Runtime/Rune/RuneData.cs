using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 룬 타입: 공격/방어/유틸리티
    /// </summary>
    public enum RuneType
    {
        Attack,     // 공격 룬 (데미지, 크리, 원소)
        Defense,    // 방어 룬 (방어력, HP, 저항)
        Utility     // 유틸리티 룬 (속도, 쿨감, 경험치)
    }

    /// <summary>
    /// 룬 등급
    /// </summary>
    public enum RuneGrade
    {
        Chipped = 1,    // 깨진 (최하급)
        Flawed = 2,     // 결함 있는
        Normal = 3,     // 보통
        Flawless = 4,   // 완벽한
        Perfect = 5     // 완전한 (최상급)
    }

    /// <summary>
    /// 장비 소켓 색상 (소켓-룬 매칭)
    /// </summary>
    public enum SocketColor
    {
        Red,    // 공격 룬용
        Blue,   // 방어 룬용
        Green,  // 유틸리티 룬용
        White   // 모든 룬 장착 가능
    }

    /// <summary>
    /// 룬 ScriptableObject 데이터
    /// 보석/룬을 장비 소켓에 장착하여 추가 능력 부여
    /// </summary>
    [CreateAssetMenu(fileName = "New Rune", menuName = "Dungeon Crawler/Rune Data")]
    public class RuneData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string runeId;
        [SerializeField] private string runeName;
        [TextArea(2, 3)]
        [SerializeField] private string description;
        [SerializeField] private Sprite runeIcon;

        [Header("분류")]
        [SerializeField] private RuneType runeType;
        [SerializeField] private RuneGrade runeGrade;
        [SerializeField] private SocketColor socketColor;

        [Header("가격")]
        [SerializeField] private int buyPrice;
        [SerializeField] private int sellPrice;

        [Header("스탯 보너스")]
        [SerializeField] private StatBlock statBonus;
        [SerializeField] private float hpBonus;
        [SerializeField] private float mpBonus;

        [Header("특수 효과")]
        [SerializeField] private float critChanceBonus;
        [SerializeField] private float critDamageBonus;
        [SerializeField] private float attackSpeedBonus;
        [SerializeField] private float moveSpeedBonus;
        [SerializeField] private float cooldownReduction;
        [SerializeField] private float lifestealPercent;
        [SerializeField] private float expBonusPercent;
        [SerializeField] private float goldBonusPercent;

        [Header("원소 데미지")]
        [SerializeField] private DamageType elementalType;
        [SerializeField] private float elementalDamageBonus;

        [Header("조합 효과")]
        [SerializeField] private string comboRuneId;        // 이 룬과 조합되는 룬 ID
        [SerializeField] private string comboEffectDesc;    // 조합 효과 설명
        [SerializeField] private float comboBonusMultiplier; // 조합 보너스 배율

        // Properties
        public string RuneId => runeId;
        public string RuneName => runeName;
        public string Description => description;
        public Sprite RuneIcon => runeIcon;
        public RuneType RuneType => runeType;
        public RuneGrade RuneGrade => runeGrade;
        public SocketColor SocketColor => socketColor;
        public int BuyPrice => buyPrice;
        public int SellPrice => sellPrice;
        public StatBlock StatBonus => statBonus;
        public float HPBonus => hpBonus;
        public float MPBonus => mpBonus;
        public float CritChanceBonus => critChanceBonus;
        public float CritDamageBonus => critDamageBonus;
        public float AttackSpeedBonus => attackSpeedBonus;
        public float MoveSpeedBonus => moveSpeedBonus;
        public float CooldownReduction => cooldownReduction;
        public float LifestealPercent => lifestealPercent;
        public float ExpBonusPercent => expBonusPercent;
        public float GoldBonusPercent => goldBonusPercent;
        public DamageType ElementalType => elementalType;
        public float ElementalDamageBonus => elementalDamageBonus;
        public string ComboRuneId => comboRuneId;
        public string ComboEffectDesc => comboEffectDesc;
        public float ComboBonusMultiplier => comboBonusMultiplier;
    }
}
