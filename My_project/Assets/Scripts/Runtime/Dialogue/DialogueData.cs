using UnityEngine;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 대화 데이터 (ScriptableObject)
    /// 분기 대화, 조건부 대화, 보상 지원
    /// </summary>
    [CreateAssetMenu(fileName = "New Dialogue", menuName = "Dungeon Crawler/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string dialogueId;
        [SerializeField] private string npcName;
        [SerializeField] private Sprite npcPortrait;

        [Header("대화 노드")]
        [SerializeField] private List<DialogueNode> nodes = new List<DialogueNode>();

        [Header("조건")]
        [SerializeField] private DialogueCondition condition;

        [Header("우선순위 (높을수록 먼저 매칭)")]
        [SerializeField] private int priority;

        // Properties
        public string DialogueId => dialogueId;
        public string NPCName => npcName;
        public Sprite NPCPortrait => npcPortrait;
        public List<DialogueNode> Nodes => nodes;
        public DialogueCondition Condition => condition;
        public int Priority => priority;
    }

    [System.Serializable]
    public class DialogueNode
    {
        [Header("노드 기본")]
        public string nodeId;
        public string speakerName;
        [TextArea(2, 4)]
        public string text;

        [Header("선택지 (없으면 자동 진행)")]
        public List<DialogueChoice> choices;

        [Header("다음 노드 (선택지 없을 때)")]
        public string nextNodeId; // 비어있으면 대화 종료

        [Header("노드 효과")]
        public DialogueNodeEffect effect;
    }

    [System.Serializable]
    public class DialogueChoice
    {
        public string choiceText;
        public string nextNodeId;

        [Header("선택지 조건 (선택적)")]
        public DialogueCondition condition;

        [Header("선택 효과")]
        public DialogueNodeEffect effect;
    }

    [System.Serializable]
    public class DialogueCondition
    {
        public DialogueConditionType conditionType = DialogueConditionType.None;
        public int requiredValue;
        public string requiredStringValue;
        public Race requiredRace = Race.None;
        public JobType requiredJob;
    }

    public enum DialogueConditionType
    {
        None,               // 조건 없음
        MinLevel,           // 최소 레벨
        MaxLevel,           // 최대 레벨
        HasQuest,           // 퀘스트 보유
        QuestComplete,      // 퀘스트 완료
        RaceIs,             // 종족 확인
        JobIs,              // 직업 확인
        HasItem,            // 아이템 보유
        GoldMin,            // 최소 골드
        HasSpecialization,  // 특성화 선택 여부
        DungeonCleared      // 던전 클리어
    }

    [System.Serializable]
    public class DialogueNodeEffect
    {
        public DialogueEffectType effectType = DialogueEffectType.None;
        public int intValue;
        public string stringValue;
    }

    public enum DialogueEffectType
    {
        None,
        GiveGold,
        GiveExp,
        GiveItem,
        AcceptQuest,
        CompleteQuest,
        OpenShop,
        OpenCrafting,
        TeleportToDungeon,
        HealPlayer,
        SetFlag        // 대화 플래그 설정 (진행도)
    }
}
