using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 도감/컬렉션 시스템 - 몬스터/아이템/스킬 수집 도감
    /// 카테고리별 완성률, 완성 보상
    /// PlayerPrefs 기반 로컬 저장
    /// </summary>
    public class CollectionSystem : MonoBehaviour
    {
        public static CollectionSystem Instance { get; private set; }

        // 수집된 항목들
        private HashSet<string> collectedMonsters = new HashSet<string>();
        private HashSet<string> collectedItems = new HashSet<string>();
        private HashSet<string> collectedSkills = new HashSet<string>();
        private Dictionary<string, int> monsterKillCounts = new Dictionary<string, int>();

        // 전체 항목 수 (Resources에서 로드)
        private int totalMonsters;
        private int totalItems;
        private int totalSkills;

        // 보상 달성 기록
        private HashSet<string> claimedRewards = new HashSet<string>();

        // 캐시된 전체 항목 수 (Resources.LoadAll 반복 호출 방지)
        private static int cachedTotalMonsters = -1;
        private static int cachedTotalItems = -1;
        private static int cachedTotalSkills = -1;

        // 이벤트
        public System.Action OnCollectionUpdated;
        public System.Action<string, CollectionCategory> OnNewEntryRegistered;

        // 접근자
        public int CollectedMonsterCount => collectedMonsters.Count;
        public int CollectedItemCount => collectedItems.Count;
        public int CollectedSkillCount => collectedSkills.Count;
        public int TotalMonsterCount => totalMonsters;
        public int TotalItemCount => totalItems;
        public int TotalSkillCount => totalSkills;

        private const string SAVE_KEY_MONSTERS = "Collection_Monsters";
        private const string SAVE_KEY_ITEMS = "Collection_Items";
        private const string SAVE_KEY_SKILLS = "Collection_Skills";
        private const string SAVE_KEY_KILLS = "Collection_Kills";
        private const string SAVE_KEY_REWARDS = "Collection_Rewards";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            CountTotals();
            Load();
        }

        /// <summary>
        /// 몬스터 처치 시 등록
        /// </summary>
        public void RegisterMonsterKill(string monsterId)
        {
            if (string.IsNullOrEmpty(monsterId)) return;

            // 처치 카운트 증가
            if (!monsterKillCounts.ContainsKey(monsterId))
                monsterKillCounts[monsterId] = 0;
            monsterKillCounts[monsterId]++;

            // 새 등록
            if (collectedMonsters.Add(monsterId))
            {
                OnNewEntryRegistered?.Invoke(monsterId, CollectionCategory.Monster);
                CheckMilestones(CollectionCategory.Monster);
                Save();

                var notif = NotificationManager.Instance;
                if (notif != null)
                    notif.ShowNotification("몬스터 도감에 새 항목이 등록되었습니다!", NotificationType.System);
            }

            OnCollectionUpdated?.Invoke();
        }

        /// <summary>
        /// 아이템 획득 시 등록
        /// </summary>
        public void RegisterItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;

            if (collectedItems.Add(itemId))
            {
                OnNewEntryRegistered?.Invoke(itemId, CollectionCategory.Item);
                CheckMilestones(CollectionCategory.Item);
                Save();

                // 트랜스모그 시스템 연동
                // (TransmogSystem은 서버에서 처리하므로 여기서는 도감만)

                OnCollectionUpdated?.Invoke();
            }
        }

        /// <summary>
        /// 스킬 학습 시 등록
        /// </summary>
        public void RegisterSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return;

            if (collectedSkills.Add(skillId))
            {
                OnNewEntryRegistered?.Invoke(skillId, CollectionCategory.Skill);
                CheckMilestones(CollectionCategory.Skill);
                Save();
                OnCollectionUpdated?.Invoke();
            }
        }

        /// <summary>
        /// 수집 여부 확인
        /// </summary>
        public bool HasCollectedMonster(string id) => collectedMonsters.Contains(id);
        public bool HasCollectedItem(string id) => collectedItems.Contains(id);
        public bool HasCollectedSkill(string id) => collectedSkills.Contains(id);

        /// <summary>
        /// 몬스터 처치 수
        /// </summary>
        public int GetMonsterKillCount(string monsterId)
        {
            return monsterKillCounts.TryGetValue(monsterId, out int count) ? count : 0;
        }

        /// <summary>
        /// 완성률 계산
        /// </summary>
        public float GetCompletionRate(CollectionCategory category)
        {
            return category switch
            {
                CollectionCategory.Monster => totalMonsters > 0 ? (float)collectedMonsters.Count / totalMonsters : 0,
                CollectionCategory.Item => totalItems > 0 ? (float)collectedItems.Count / totalItems : 0,
                CollectionCategory.Skill => totalSkills > 0 ? (float)collectedSkills.Count / totalSkills : 0,
                _ => 0f
            };
        }

        /// <summary>
        /// 전체 완성률
        /// </summary>
        public float GetTotalCompletionRate()
        {
            int total = totalMonsters + totalItems + totalSkills;
            int collected = collectedMonsters.Count + collectedItems.Count + collectedSkills.Count;
            return total > 0 ? (float)collected / total : 0f;
        }

        /// <summary>
        /// 마일스톤 체크 및 보상
        /// </summary>
        private void CheckMilestones(CollectionCategory category)
        {
            float rate = GetCompletionRate(category);
            string prefix = category.ToString().ToLower();

            CheckAndGrantMilestone($"{prefix}_25", rate >= 0.25f, $"{GetCategoryName(category)} 도감 25% 완성", 1000, 500);
            CheckAndGrantMilestone($"{prefix}_50", rate >= 0.50f, $"{GetCategoryName(category)} 도감 50% 완성", 3000, 1500);
            CheckAndGrantMilestone($"{prefix}_75", rate >= 0.75f, $"{GetCategoryName(category)} 도감 75% 완성", 5000, 3000);
            CheckAndGrantMilestone($"{prefix}_100", rate >= 1.00f, $"{GetCategoryName(category)} 도감 100% 완성!", 10000, 5000);

            // 전체 완성률 마일스톤
            float totalRate = GetTotalCompletionRate();
            CheckAndGrantMilestone("total_50", totalRate >= 0.50f, "전체 도감 50% 완성", 10000, 5000);
            CheckAndGrantMilestone("total_100", totalRate >= 1.00f, "전체 도감 100% 완성!", 50000, 20000);
        }

        private void CheckAndGrantMilestone(string rewardId, bool condition, string message, int gold, int exp)
        {
            if (!condition || claimedRewards.Contains(rewardId)) return;

            claimedRewards.Add(rewardId);

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"도감 달성: {message} (골드 +{gold}, 경험치 +{exp})", NotificationType.Achievement);

            // 업적 연동
            if (AchievementSystem.Instance != null)
            {
                if (rewardId.Contains("50")) AchievementSystem.Instance.IncrementProgress("collector_50");
                if (rewardId.Contains("100") && rewardId.StartsWith("item"))
                    AchievementSystem.Instance.IncrementProgress("collector_200");
            }

            // 칭호 해금
            if (TitleSystem.Instance != null)
            {
                if (rewardId == "total_50") TitleSystem.Instance.UnlockTitle(0, "collector_50");
                if (rewardId == "total_100") TitleSystem.Instance.UnlockTitle(0, "collector_200");
            }

            Save();
        }

        private string GetCategoryName(CollectionCategory cat)
        {
            return cat switch
            {
                CollectionCategory.Monster => "몬스터",
                CollectionCategory.Item => "아이템",
                CollectionCategory.Skill => "스킬",
                _ => "기타"
            };
        }

        /// <summary>
        /// 전체 항목 수 카운트 (Resources에서)
        /// </summary>
        private void CountTotals()
        {
            if (cachedTotalMonsters < 0)
            {
                var monsterRaces = Resources.LoadAll<MonsterRaceData>("");
                var monsterVariants = Resources.LoadAll<MonsterVariantData>("");
                cachedTotalMonsters = monsterRaces.Length + monsterVariants.Length;
            }
            totalMonsters = cachedTotalMonsters;

            if (cachedTotalItems < 0)
            {
                var items = Resources.LoadAll<ItemData>("");
                cachedTotalItems = items.Length;
            }
            totalItems = cachedTotalItems;

            if (cachedTotalSkills < 0)
            {
                var skills = Resources.LoadAll<SkillData>("");
                cachedTotalSkills = skills.Length;
            }
            totalSkills = cachedTotalSkills;
        }

        #region 저장/로드

        private void Save()
        {
            PlayerPrefs.SetString(SAVE_KEY_MONSTERS, string.Join(",", collectedMonsters));
            PlayerPrefs.SetString(SAVE_KEY_ITEMS, string.Join(",", collectedItems));
            PlayerPrefs.SetString(SAVE_KEY_SKILLS, string.Join(",", collectedSkills));
            PlayerPrefs.SetString(SAVE_KEY_REWARDS, string.Join(",", claimedRewards));

            // 처치 수 저장
            var killPairs = new List<string>();
            foreach (var kvp in monsterKillCounts)
                killPairs.Add($"{kvp.Key}:{kvp.Value}");
            PlayerPrefs.SetString(SAVE_KEY_KILLS, string.Join(",", killPairs));

            PlayerPrefs.Save();
        }

        private void Load()
        {
            LoadSet(SAVE_KEY_MONSTERS, collectedMonsters);
            LoadSet(SAVE_KEY_ITEMS, collectedItems);
            LoadSet(SAVE_KEY_SKILLS, collectedSkills);
            LoadSet(SAVE_KEY_REWARDS, claimedRewards);

            // 처치 수 로드
            string killsStr = PlayerPrefs.GetString(SAVE_KEY_KILLS, "");
            if (!string.IsNullOrEmpty(killsStr))
            {
                foreach (var pair in killsStr.Split(','))
                {
                    var parts = pair.Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int count))
                        monsterKillCounts[parts[0]] = count;
                }
            }
        }

        private void LoadSet(string key, HashSet<string> set)
        {
            string data = PlayerPrefs.GetString(key, "");
            if (!string.IsNullOrEmpty(data))
            {
                foreach (var id in data.Split(','))
                {
                    if (!string.IsNullOrEmpty(id))
                        set.Add(id);
                }
            }
        }

        #endregion

        private void OnDestroy()
        {
            if (Instance == this)
            {
                OnCollectionUpdated = null;
                OnNewEntryRegistered = null;
                Instance = null;
            }
        }
    }

    /// <summary>
    /// 도감 카테고리
    /// </summary>
    public enum CollectionCategory
    {
        Monster,
        Item,
        Skill
    }
}
