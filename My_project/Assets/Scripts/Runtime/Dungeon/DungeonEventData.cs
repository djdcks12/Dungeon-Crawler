using UnityEngine;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 던전 이벤트 데이터 - ScriptableObject 기반
    /// 제단, 샘물, 큐리오, 보물방, 함정, 아레나, 시련, 상인, 포탈, 휴식지 등
    /// </summary>
    [CreateAssetMenu(fileName = "NewDungeonEvent", menuName = "Dungeon Crawler/Dungeon Event Data")]
    public class DungeonEventData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string eventId;
        [SerializeField] private string eventName;
        [SerializeField] private string description;
        [SerializeField] private Sprite eventIcon;
        [SerializeField] private DungeonEventType eventType;
        [SerializeField] private DungeonEventRarity rarity;

        [Header("트리거 설정")]
        [SerializeField] private DungeonEventTrigger triggerType;
        [SerializeField] private float spawnChance = 0.1f;     // 발생 확률 (0~1)
        [SerializeField] private int minFloor = 1;
        [SerializeField] private int maxFloor = 10;
        [SerializeField] private DungeonDifficulty minDifficulty = DungeonDifficulty.Easy;
        [SerializeField] private bool oncePerDungeon = false;   // 던전당 1회만

        [Header("상호작용")]
        [SerializeField] private string interactionText;        // "조사한다", "기도한다" 등
        [SerializeField] private float interactionTime = 1f;    // 상호작용 소요 시간
        [SerializeField] private bool requiresChoice = false;   // 선택지 분기 여부

        [Header("기본 효과 (맨손 상호작용)")]
        [SerializeField] private List<EventOutcome> bareHandOutcomes = new List<EventOutcome>();

        [Header("아이템 상호작용 (안전한 선택)")]
        [SerializeField] private List<ItemInteraction> itemInteractions = new List<ItemInteraction>();

        [Header("선택지 (requiresChoice = true일 때)")]
        [SerializeField] private List<EventChoice> choices = new List<EventChoice>();

        [Header("전투 이벤트 (아레나/매복)")]
        [SerializeField] private List<CombatWave> combatWaves = new List<CombatWave>();
        [SerializeField] private float combatTimeLimit = 0f;    // 0 = 제한 없음

        [Header("상점 이벤트")]
        [SerializeField] private List<EventShopItem> shopItems = new List<EventShopItem>();

        [Header("시각 효과")]
        [SerializeField] private Color glowColor = Color.white;
        [SerializeField] private string activationSFX;

        // === Public Properties ===
        public string EventId => eventId;
        public string EventName => eventName;
        public string Description => description;
        public Sprite EventIcon => eventIcon;
        public DungeonEventType EventType => eventType;
        public DungeonEventRarity Rarity => rarity;
        public DungeonEventTrigger TriggerType => triggerType;
        public float SpawnChance => spawnChance;
        public int MinFloor => minFloor;
        public int MaxFloor => maxFloor;
        public DungeonDifficulty MinDifficulty => minDifficulty;
        public bool OncePerDungeon => oncePerDungeon;
        public string InteractionText => interactionText;
        public float InteractionTime => interactionTime;
        public bool RequiresChoice => requiresChoice;
        public List<EventOutcome> BareHandOutcomes => bareHandOutcomes;
        public List<ItemInteraction> ItemInteractions => itemInteractions;
        public List<EventChoice> Choices => choices;
        public List<CombatWave> CombatWaves => combatWaves;
        public float CombatTimeLimit => combatTimeLimit;
        public List<EventShopItem> EventShopItems => shopItems;
        public Color GlowColor => glowColor;
        public string ActivationSFX => activationSFX;
    }

    // === Enums ===

    public enum DungeonEventType
    {
        Shrine,         // 제단 - 임시 버프
        Fountain,       // 샘물 - HP/MP 회복
        Curio,          // 큐리오 - 위험+보상 상호작용
        TreasureRoom,   // 보물방 - 잠긴 보물
        AmbushTrap,     // 매복 - 적 기습
        Arena,          // 아레나 - 웨이브 전투
        Trial,          // 시련 - 특수 조건 전투
        Shop,           // 상인 - 아이템 구매
        Portal,         // 포탈 - 비밀 구역 이동
        RestSite,       // 휴식지 - 안전 회복
        Altar,          // 제물 - 희생으로 강화
        Gamble,         // 도박 - 골드로 랜덤 아이템
        Blessing,       // 축복 - 영구 버프 (해당 던전 내)
        Curse,          // 저주 - 디버프 + 보상 증가
        MysteryBox      // 미스터리 - 완전 랜덤
    }

    public enum DungeonEventRarity
    {
        Common,         // 흔함 (30% 이상)
        Uncommon,       // 비범 (15~30%)
        Rare,           // 희귀 (5~15%)
        Epic,           // 영웅 (1~5%)
        Legendary       // 전설 (1% 미만)
    }

    public enum DungeonEventTrigger
    {
        RoomBased,      // 특정 방에 배치
        ChanceBased,    // 탐색 시 확률 발동
        FloorGuaranteed,// 특정 층 보장
        ConditionBased, // 조건 충족 시 (HP 낮음, 골드 많음 등)
        TimeBased       // 시간 경과 후 발동
    }

    public enum EventConditionType
    {
        None,
        LowHP,          // HP 30% 이하
        HighGold,       // 골드 1000 이상
        HasItem,        // 특정 아이템 보유
        NightTime,      // 밤 시간
        PartySize,      // 파티원 수
        BossDefeated,   // 보스 처치 후
        NoDeaths        // 사망 없음
    }

    // === Data Structures ===

    [System.Serializable]
    public class EventOutcome
    {
        public string description;              // "축복을 받았다!"
        public float chance = 1f;               // 이 결과 발생 확률
        public EventEffectType effectType;
        public float effectValue;               // HP 회복량, 버프 수치 등
        public float duration;                  // 버프 지속시간 (초)
        public StatusType statusEffect;         // 적용할 상태이상
        public bool isNegative;                 // 부정적 결과인지
    }

    public enum EventEffectType
    {
        HealHP,             // HP 회복
        HealMP,             // MP 회복
        HealPercent,        // HP% 회복
        DamageHP,           // HP 피해
        GainGold,           // 골드 획득
        LoseGold,           // 골드 손실
        GainExp,            // 경험치 획득
        BuffStat,           // 스탯 버프
        DebuffStat,         // 스탯 디버프
        GainItem,           // 아이템 획득
        ApplyStatus,        // 상태이상 적용
        RemoveStatus,       // 상태이상 해제
        RevealMap,          // 지도 공개
        Teleport,           // 텔레포트
        SpawnMonster,       // 몬스터 소환
        IncreaseDamage,     // 데미지 증가 (%)
        ReduceDamage,       // 데미지 감소 (%)
        IncreaseSpeed,      // 이동속도 증가 (%)
        CooldownReset,      // 쿨다운 초기화
        InvincibilityShort, // 짧은 무적
        FullRestore,        // 전체 회복
        RandomBuff,         // 랜덤 버프
        CurseAndReward      // 저주 + 보상 증가
    }

    [System.Serializable]
    public class ItemInteraction
    {
        public string requiredItemName;         // 필요 아이템 이름
        public string resultDescription;        // "성수를 사용하여 정화했다"
        public EventOutcome guaranteedOutcome;   // 보장된 결과
    }

    [System.Serializable]
    public class EventChoice
    {
        public string choiceText;               // "기도한다" / "무시한다"
        public string resultText;               // 결과 설명
        public List<EventOutcome> outcomes;     // 해당 선택의 결과
        public EventConditionType condition;    // 선택 조건
        public float conditionValue;            // 조건 수치
    }

    [System.Serializable]
    public class CombatWave
    {
        public int waveNumber;
        public string monsterRace;              // 소환할 몬스터 종족
        public string monsterVariant;           // 변종 이름
        public int count;                       // 마리 수
        public float delayBeforeWave;           // 웨이브 시작 딜레이
        public bool isElite;                    // 엘리트 여부
    }

    [System.Serializable]
    public class EventShopItem
    {
        public string itemName;                 // 판매 아이템
        public int price;                       // 가격
        public int stock;                       // 재고
        public float discountChance;            // 할인 확률
        public float discountPercent;           // 할인율
    }
}
