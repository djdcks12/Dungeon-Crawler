using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public enum MountType
    {
        Ground,     // 지상 마운트
        Flying      // 비행 마운트
    }

    public enum MountRarity
    {
        Common = 1,
        Uncommon = 2,
        Rare = 3,
        Epic = 4,
        Legendary = 5
    }

    /// <summary>
    /// 마운트 데이터 (ScriptableObject)
    /// </summary>
    [CreateAssetMenu(fileName = "New Mount", menuName = "Dungeon Crawler/Mount Data")]
    public class MountData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string mountId;
        [SerializeField] private string mountName;
        [TextArea(2, 3)]
        [SerializeField] private string description;
        [SerializeField] private Sprite mountIcon;

        [Header("분류")]
        [SerializeField] private MountType mountType;
        [SerializeField] private MountRarity rarity;

        [Header("이동 속도")]
        [SerializeField] private float speedBonus = 50f;           // 이동속도 보너스%
        [SerializeField] private float flySpeedBonus = 30f;        // 비행속도 보너스% (비행 마운트만)

        [Header("획득")]
        [SerializeField] private int purchasePrice;                 // 구매 가격 (0이면 드롭/퀘스트)
        [SerializeField] private int requiredLevel = 1;
        [SerializeField] private string unlockCondition;            // 해금 조건 설명

        [Header("탑승 보너스")]
        [SerializeField] private float hpRegenBonus;                // 탑승 중 HP 재생 보너스
        [SerializeField] private float expBonusPercent;             // 탑승 중 경험치 보너스%
        [SerializeField] private float gatherSpeedBonus;            // 채집 속도 보너스%

        [Header("시각")]
        [SerializeField] private RuntimeAnimatorController mountAnimator;
        [SerializeField] private Vector2 riderOffset;               // 라이더 위치 오프셋

        // Properties
        public string MountId => mountId;
        public string MountName => mountName;
        public string Description => description;
        public Sprite MountIcon => mountIcon;
        public MountType MountType => mountType;
        public MountRarity Rarity => rarity;
        public float SpeedBonus => speedBonus;
        public float FlySpeedBonus => flySpeedBonus;
        public int PurchasePrice => purchasePrice;
        public int RequiredLevel => requiredLevel;
        public string UnlockCondition => unlockCondition;
        public float HPRegenBonus => hpRegenBonus;
        public float ExpBonusPercent => expBonusPercent;
        public float GatherSpeedBonus => gatherSpeedBonus;
        public RuntimeAnimatorController MountAnimator => mountAnimator;
        public Vector2 RiderOffset => riderOffset;
    }
}
