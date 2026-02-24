using UnityEngine;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 월드 랜덤 이벤트 데이터
    /// 던전 밖 필드에서 발생하는 다양한 이벤트
    /// </summary>
    [CreateAssetMenu(fileName = "New World Event", menuName = "Dungeon Crawler/World Event Data")]
    public class WorldEventData : ScriptableObject
    {
        [Header("Event Info")]
        [SerializeField] private string eventId;
        [SerializeField] private string eventName;
        [TextArea(3, 5)]
        [SerializeField] private string description;
        [SerializeField] private Sprite eventIcon;

        [Header("Event Type")]
        [SerializeField] private WorldEventType eventType;
        [SerializeField] private WorldEventRarity rarity;

        [Header("Spawn Conditions")]
        [SerializeField] private float spawnChance = 0.1f;     // 기본 발생 확률 (0~1)
        [SerializeField] private float checkInterval = 30f;     // 확인 간격 (초)
        [SerializeField] private int minPlayerLevel = 1;
        [SerializeField] private int maxPlayerLevel = 99;
        [SerializeField] private bool requiresNight;            // 밤에만 발생
        [SerializeField] private bool requiresDay;              // 낮에만 발생
        [SerializeField] private WeatherCondition requiredWeather = WeatherCondition.Any;

        [Header("Duration")]
        [SerializeField] private float duration = 60f;          // 이벤트 지속시간 (초)
        [SerializeField] private bool isPermanentUntilCompleted; // 완료할 때까지 유지

        [Header("Event Content")]
        [SerializeField] private WorldEventMonster[] eventMonsters;      // 출현 몬스터
        [SerializeField] private WorldEventReward[] rewards;             // 보상
        [SerializeField] private WorldEventDialogue[] dialogues;        // 대사
        [SerializeField] private WorldEventChoice[] choices;            // 선택지

        [Header("Special Settings")]
        [SerializeField] private float spawnRadius = 10f;       // 이벤트 발생 반경
        [SerializeField] private bool announceToAll;            // 전체 공지 여부
        [SerializeField] private string announceMessage;        // 공지 메시지

        // Properties
        public string EventId => eventId;
        public string EventName => eventName;
        public string Description => description;
        public Sprite EventIcon => eventIcon;
        public WorldEventType EventType => eventType;
        public WorldEventRarity Rarity => rarity;
        public float SpawnChance => spawnChance;
        public float CheckInterval => checkInterval;
        public int MinPlayerLevel => minPlayerLevel;
        public int MaxPlayerLevel => maxPlayerLevel;
        public bool RequiresNight => requiresNight;
        public bool RequiresDay => requiresDay;
        public WeatherCondition RequiredWeather => requiredWeather;
        public float Duration => duration;
        public bool IsPermanentUntilCompleted => isPermanentUntilCompleted;
        public WorldEventMonster[] EventMonsters => eventMonsters;
        public WorldEventReward[] Rewards => rewards;
        public WorldEventDialogue[] Dialogues => dialogues;
        public WorldEventChoice[] Choices => choices;
        public float SpawnRadius => spawnRadius;
        public bool AnnounceToAll => announceToAll;
        public string AnnounceMessage => announceMessage;
    }

    /// <summary>
    /// 월드 이벤트 타입
    /// </summary>
    public enum WorldEventType
    {
        TreasureGoblin,     // 트레저 고블린 (쫓으면 보물 드롭)
        WanderingMerchant,  // 방랑 상인 (특별 아이템 판매)
        MiniBoss,           // 미니보스 스폰
        MonsterRaid,        // 몬스터 습격 (대규모 웨이브)
        TreasureDiscovery,  // 보물 발견 (숨겨진 상자)
        WeatherAnomaly,     // 기상 이변 (폭풍 시 특수 몬스터)
        NightHaunt,         // 야간 출몰 (밤에만 언데드 습격)
        MysteriousNPC,      // 수수께끼 NPC (퀴즈/도전)
        ElementalRift,      // 원소 균열 (원소 몬스터 대량)
        BountyTarget,       // 현상금 대상 (강력한 단일 몬스터)
        FallenStar,         // 떨어진 별 (레어 자원)
        AncientAltar,       // 고대 제단 (버프/저주 선택)
        MerchantCaravan,    // 상인 캐러밴 (호위 미니퀘스트)
        GhostEncounter,     // 유령 조우 (과거의 기억)
        DragonSighting      // 드래곤 목격 (초강력 이벤트)
    }

    /// <summary>
    /// 월드 이벤트 희귀도
    /// </summary>
    public enum WorldEventRarity
    {
        Common,         // 일반 (50%)
        Uncommon,       // 비일반 (30%)
        Rare,           // 희귀 (15%)
        Epic,           // 서사 (4%)
        Legendary       // 전설 (1%)
    }

    /// <summary>
    /// 날씨 조건
    /// </summary>
    public enum WeatherCondition
    {
        Any,            // 아무 날씨
        Clear,          // 맑음
        Rain,           // 비
        Storm,          // 폭풍
        Snow            // 눈
    }

    /// <summary>
    /// 이벤트 몬스터 정의
    /// </summary>
    [System.Serializable]
    public struct WorldEventMonster
    {
        public string monsterVariantId;     // MonsterVariantData 참조
        public int count;                   // 출현 수
        public float gradeMin;              // 최소 등급
        public float gradeMax;              // 최대 등급
        public bool isBoss;                 // 보스 여부
        public float spawnDelay;            // 스폰 딜레이 (초)
    }

    /// <summary>
    /// 이벤트 보상 정의
    /// </summary>
    [System.Serializable]
    public struct WorldEventReward
    {
        public WorldRewardType rewardType;
        public int amount;                  // 수량 또는 수치
        public string itemId;               // 아이템 ID (아이템 보상 시)
        public float dropChance;            // 드롭 확률 (0~1)
    }

    /// <summary>
    /// 보상 타입
    /// </summary>
    public enum WorldRewardType
    {
        Gold,
        Experience,
        Item,
        Buff,
        SkillPoint
    }

    /// <summary>
    /// 이벤트 대사
    /// </summary>
    [System.Serializable]
    public struct WorldEventDialogue
    {
        public string speakerName;
        [TextArea(2, 4)]
        public string dialogue;
        public int order;
    }

    /// <summary>
    /// 이벤트 선택지
    /// </summary>
    [System.Serializable]
    public struct WorldEventChoice
    {
        public string choiceText;           // 선택지 텍스트
        [TextArea(1, 3)]
        public string resultDescription;    // 결과 설명
        public WorldRewardType rewardType;  // 보상 타입
        public int rewardAmount;            // 보상 수량
        public float riskChance;            // 위험 확률 (0~1)
        public int riskDamage;              // 위험 시 피해
    }
}
