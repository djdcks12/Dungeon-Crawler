using UnityEngine;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 퀘스트 타입
    /// </summary>
    public enum QuestType
    {
        Kill,       // 몬스터 처치
        Collect,    // 아이템 수집
        Explore,    // 던전 탐험 (특정 층 도달)
        BossKill,   // 보스 처치
        Enhance,    // 장비 강화
        LevelUp     // 레벨 달성
    }

    /// <summary>
    /// 퀘스트 상태
    /// </summary>
    public enum QuestStatus
    {
        Available,  // 수락 가능
        Active,     // 진행중
        Completed,  // 조건 달성 (보상 미수령)
        Rewarded,   // 보상 수령 완료
        Failed      // 실패
    }

    /// <summary>
    /// 퀘스트 난이도
    /// </summary>
    public enum QuestDifficulty
    {
        Easy,
        Normal,
        Hard,
        Epic
    }

    /// <summary>
    /// 퀘스트 목표 데이터
    /// </summary>
    [System.Serializable]
    public struct QuestObjective
    {
        public QuestType objectiveType;
        public string targetId;         // 몬스터 종족명, 아이템ID, 던전명 등
        public string targetName;       // 표시용 이름
        public int requiredCount;       // 필요 수량
    }

    /// <summary>
    /// 퀘스트 보상 데이터
    /// </summary>
    [System.Serializable]
    public struct QuestReward
    {
        public long experienceReward;
        public long goldReward;
        public string itemRewardId;     // 보상 아이템 ID (없으면 "")
        public int itemRewardCount;
    }

    /// <summary>
    /// 퀘스트 ScriptableObject 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuest", menuName = "Game/Quest Data")]
    public class QuestData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string questId = "";
        [SerializeField] private string questName = "";
        [SerializeField, TextArea(2, 4)] private string description = "";
        [SerializeField] private QuestDifficulty difficulty = QuestDifficulty.Normal;
        [SerializeField] private int requiredLevel = 1;
        [SerializeField] private bool isRepeatable = false;
        [SerializeField] private bool isDaily = false;

        [Header("목표")]
        [SerializeField] private QuestObjective[] objectives;

        [Header("보상")]
        [SerializeField] private QuestReward reward;

        [Header("선행 퀘스트")]
        [SerializeField] private string prerequisiteQuestId = "";

        // Properties
        public string QuestId => questId;
        public string QuestName => questName;
        public string Description => description;
        public QuestDifficulty Difficulty => difficulty;
        public int RequiredLevel => requiredLevel;
        public bool IsRepeatable => isRepeatable;
        public bool IsDaily => isDaily;
        public QuestObjective[] Objectives => objectives;
        public QuestReward Reward => reward;
        public string PrerequisiteQuestId => prerequisiteQuestId;

        /// <summary>
        /// 난이도 색상
        /// </summary>
        public Color GetDifficultyColor()
        {
            switch (difficulty)
            {
                case QuestDifficulty.Easy: return Color.green;
                case QuestDifficulty.Normal: return Color.white;
                case QuestDifficulty.Hard: return new Color(1f, 0.6f, 0f);
                case QuestDifficulty.Epic: return new Color(0.7f, 0.3f, 1f);
                default: return Color.white;
            }
        }

        /// <summary>
        /// 난이도 이름
        /// </summary>
        public string GetDifficultyName()
        {
            switch (difficulty)
            {
                case QuestDifficulty.Easy: return "쉬움";
                case QuestDifficulty.Normal: return "보통";
                case QuestDifficulty.Hard: return "어려움";
                case QuestDifficulty.Epic: return "영웅";
                default: return "???";
            }
        }
    }

    /// <summary>
    /// 퀘스트 진행 상태 (런타임)
    /// </summary>
    [System.Serializable]
    public class QuestProgress
    {
        public string questId;
        public QuestStatus status;
        public int[] currentCounts;     // 각 목표별 현재 진행도
        public long acceptedTime;
        public long completedTime;

        public QuestProgress(QuestData data)
        {
            questId = data.QuestId;
            status = QuestStatus.Active;
            currentCounts = new int[data.Objectives.Length];
            acceptedTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            completedTime = 0;
        }

        /// <summary>
        /// 모든 목표 달성 여부
        /// </summary>
        public bool IsAllObjectivesComplete(QuestData data)
        {
            if (data.Objectives == null) return true;
            for (int i = 0; i < data.Objectives.Length; i++)
            {
                if (i >= currentCounts.Length) return false;
                if (currentCounts[i] < data.Objectives[i].requiredCount)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 진행률 (0~1)
        /// </summary>
        public float GetProgressRatio(QuestData data)
        {
            if (data.Objectives == null || data.Objectives.Length == 0) return 1f;

            float total = 0f;
            for (int i = 0; i < data.Objectives.Length; i++)
            {
                int required = data.Objectives[i].requiredCount;
                int current = i < currentCounts.Length ? currentCounts[i] : 0;
                total += Mathf.Clamp01((float)current / Mathf.Max(1, required));
            }
            return data.Objectives.Length > 0 ? total / data.Objectives.Length : 0f;
        }
    }
}
