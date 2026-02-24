using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 업적 시스템 - 조건 달성 시 보상 지급
    /// PlayerPrefs 기반 로컬 저장
    /// </summary>
    public class AchievementSystem : MonoBehaviour
    {
        public static AchievementSystem Instance { get; private set; }

        // 업적 정의
        private List<AchievementData> allAchievements = new List<AchievementData>();

        // 달성된 업적 ID
        private HashSet<string> unlockedIds = new HashSet<string>();

        // 진행 상태 (카운트 기반 업적)
        private Dictionary<string, int> progressCounters = new Dictionary<string, int>();

        // 이벤트
        public System.Action<AchievementData> OnAchievementUnlocked;

        private const string SAVE_KEY_UNLOCKED = "Achievements_Unlocked";
        private const string SAVE_KEY_PROGRESS = "Achievements_Progress";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeAchievements();
            Load();
        }

        /// <summary>
        /// 업적 진행도 증가 (카운트 기반)
        /// </summary>
        public void IncrementProgress(string achievementId, int amount = 1)
        {
            if (unlockedIds.Contains(achievementId)) return;

            if (!progressCounters.ContainsKey(achievementId))
                progressCounters[achievementId] = 0;

            progressCounters[achievementId] += amount;

            var ach = allAchievements.Find(a => a.id == achievementId);
            if (ach != null && progressCounters[achievementId] >= ach.requiredCount)
            {
                UnlockAchievement(ach);
            }

            Save();
        }

        /// <summary>
        /// 조건 체크로 업적 달성 시도
        /// </summary>
        public void TryUnlock(string achievementId)
        {
            if (unlockedIds.Contains(achievementId)) return;

            var ach = allAchievements.Find(a => a.id == achievementId);
            if (ach != null)
            {
                UnlockAchievement(ach);
            }
        }

        /// <summary>
        /// 카테고리별 업적 알림 (시스템 자동 체크)
        /// </summary>
        public void NotifyEvent(AchievementEvent eventType, int value = 1)
        {
            foreach (var ach in allAchievements)
            {
                if (unlockedIds.Contains(ach.id)) continue;
                if (ach.eventType != eventType) continue;

                if (ach.requiredCount <= 1)
                {
                    // 단일 조건
                    if (value >= ach.requiredValue)
                        UnlockAchievement(ach);
                }
                else
                {
                    // 카운트 기반
                    IncrementProgress(ach.id, value);
                }
            }
        }

        private void UnlockAchievement(AchievementData ach)
        {
            if (unlockedIds.Contains(ach.id)) return;

            unlockedIds.Add(ach.id);

            // 보상 지급
            GrantReward(ach);

            // 알림
            var notifMgr = NotificationManager.Instance;
            if (notifMgr != null)
            {
                notifMgr.ShowNotification($"업적 달성: {ach.title}", NotificationType.Achievement);
            }

            OnAchievementUnlocked?.Invoke(ach);
            Save();

            Debug.Log($"[Achievement] Unlocked: {ach.title} ({ach.id})");
        }

        private void GrantReward(AchievementData ach)
        {
            if (ach.rewardExp <= 0 && ach.rewardGold <= 0) return;

            var players = FindObjectsByType<PlayerStatsManager>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p.IsOwner && p.CurrentStats != null)
                {
                    if (ach.rewardExp > 0)
                        p.CurrentStats.AddExperience(ach.rewardExp);
                    if (ach.rewardGold > 0)
                        p.CurrentStats.ChangeGold(ach.rewardGold);
                    break;
                }
            }
        }

        /// <summary>
        /// 전체 업적 목록 가져오기
        /// </summary>
        public List<AchievementData> GetAllAchievements()
        {
            return allAchievements;
        }

        /// <summary>
        /// 달성 여부 확인
        /// </summary>
        public bool IsUnlocked(string achievementId)
        {
            return unlockedIds.Contains(achievementId);
        }

        /// <summary>
        /// 진행도 가져오기
        /// </summary>
        public int GetProgress(string achievementId)
        {
            return progressCounters.ContainsKey(achievementId) ? progressCounters[achievementId] : 0;
        }

        /// <summary>
        /// 달성 통계
        /// </summary>
        public (int unlocked, int total) GetStats()
        {
            return (unlockedIds.Count, allAchievements.Count);
        }

        // === 업적 정의 ===

        private void InitializeAchievements()
        {
            allAchievements.Clear();

            // --- 전투 업적 ---
            Add("first_blood", "첫 전투", "첫 번째 몬스터를 처치하라", AchievementEvent.MonsterKill, 1, 0, 50, 10);
            Add("monster_slayer_10", "사냥꾼", "몬스터 10마리 처치", AchievementEvent.MonsterKill, 10, 0, 100, 30);
            Add("monster_slayer_50", "전사", "몬스터 50마리 처치", AchievementEvent.MonsterKill, 50, 0, 300, 100);
            Add("monster_slayer_100", "학살자", "몬스터 100마리 처치", AchievementEvent.MonsterKill, 100, 0, 500, 200);
            Add("monster_slayer_500", "전설의 사냥꾼", "몬스터 500마리 처치", AchievementEvent.MonsterKill, 500, 0, 2000, 1000);
            Add("boss_slayer", "보스 킬러", "보스 몬스터 처치", AchievementEvent.BossKill, 1, 0, 200, 50);
            Add("boss_slayer_5", "보스 헌터", "보스 5마리 처치", AchievementEvent.BossKill, 5, 0, 500, 200);
            Add("boss_slayer_20", "용사", "보스 20마리 처치", AchievementEvent.BossKill, 20, 0, 2000, 500);

            // --- 레벨 업적 ---
            Add("level_5", "성장", "레벨 5 달성", AchievementEvent.LevelUp, 1, 5, 200, 50);
            Add("level_10", "숙련자", "레벨 10 달성", AchievementEvent.LevelUp, 1, 10, 500, 200);
            Add("level_15", "마스터", "최대 레벨 달성", AchievementEvent.LevelUp, 1, 15, 2000, 1000);

            // --- 던전 업적 ---
            Add("dungeon_first", "탐험의 시작", "첫 던전 클리어", AchievementEvent.DungeonClear, 1, 0, 100, 50);
            Add("dungeon_5", "던전 탐험가", "5개 던전 클리어", AchievementEvent.DungeonClear, 5, 0, 500, 200);
            Add("dungeon_goblin", "고블린 토벌", "고블린 동굴 완주", AchievementEvent.DungeonSpecific, 1, 0, 200, 100);
            Add("dungeon_demon", "마왕 토벌", "마왕의 영역 완주", AchievementEvent.DungeonSpecific, 1, 0, 5000, 3000);

            // --- 경제 업적 ---
            Add("gold_1000", "부자의 길", "골드 1,000 보유", AchievementEvent.GoldReach, 1, 1000, 100, 0);
            Add("gold_10000", "재벌", "골드 10,000 보유", AchievementEvent.GoldReach, 1, 10000, 500, 0);
            Add("gold_100000", "재벌왕", "골드 100,000 보유", AchievementEvent.GoldReach, 1, 100000, 2000, 0);

            // --- 장비 업적 ---
            Add("enhance_5", "강화 입문", "+5 강화 성공", AchievementEvent.EnhanceSuccess, 1, 5, 200, 100);
            Add("enhance_10", "강화 마스터", "+10 강화 성공", AchievementEvent.EnhanceSuccess, 1, 10, 2000, 1000);
            Add("equip_rare", "희귀 장비", "레어 등급 장비 착용", AchievementEvent.EquipItem, 1, 3, 100, 50);
            Add("equip_legendary", "전설의 장비", "전설 등급 장비 착용", AchievementEvent.EquipItem, 1, 5, 1000, 500);

            // --- 스킬 업적 ---
            Add("skill_first", "첫 스킬", "첫 번째 스킬 습득", AchievementEvent.SkillLearn, 1, 0, 50, 20);
            Add("skill_5", "스킬 수집가", "스킬 5개 습득", AchievementEvent.SkillLearn, 5, 0, 200, 100);
            Add("skill_ultimate", "궁극기 해방", "궁극기 습득", AchievementEvent.SkillLearn, 1, 7, 500, 200);

            // --- PvP 업적 ---
            Add("pvp_first", "첫 승리", "PvP 첫 킬", AchievementEvent.PvPKill, 1, 0, 100, 50);
            Add("pvp_10", "결투사", "PvP 10킬", AchievementEvent.PvPKill, 10, 0, 500, 200);
            Add("pvp_revenge", "복수의 칼날", "복수 성공", AchievementEvent.PvPRevenge, 1, 0, 200, 100);

            // --- 기타 업적 ---
            Add("trade_first", "첫 거래", "플레이어 간 거래 완료", AchievementEvent.TradeComplete, 1, 0, 50, 30);
            Add("party_first", "동료", "파티 참가", AchievementEvent.PartyJoin, 1, 0, 50, 20);
            Add("death_survive", "부활", "사망 후 부활", AchievementEvent.Death, 1, 0, 30, 0);
        }

        private void Add(string id, string title, string desc, AchievementEvent eventType,
            int requiredCount, int requiredValue, long rewardExp, long rewardGold)
        {
            allAchievements.Add(new AchievementData
            {
                id = id,
                title = title,
                description = desc,
                eventType = eventType,
                requiredCount = requiredCount,
                requiredValue = requiredValue,
                rewardExp = rewardExp,
                rewardGold = rewardGold
            });
        }

        // === 저장/로드 ===

        private void Save()
        {
            // 달성 ID
            PlayerPrefs.SetString(SAVE_KEY_UNLOCKED, string.Join(",", unlockedIds));

            // 진행도
            var progressPairs = progressCounters.Select(kvp => $"{kvp.Key}:{kvp.Value}");
            PlayerPrefs.SetString(SAVE_KEY_PROGRESS, string.Join(",", progressPairs));

            PlayerPrefs.Save();
        }

        private void Load()
        {
            // 달성 ID
            if (PlayerPrefs.HasKey(SAVE_KEY_UNLOCKED))
            {
                string data = PlayerPrefs.GetString(SAVE_KEY_UNLOCKED);
                if (!string.IsNullOrEmpty(data))
                {
                    foreach (var id in data.Split(','))
                    {
                        if (!string.IsNullOrEmpty(id))
                            unlockedIds.Add(id);
                    }
                }
            }

            // 진행도
            if (PlayerPrefs.HasKey(SAVE_KEY_PROGRESS))
            {
                string data = PlayerPrefs.GetString(SAVE_KEY_PROGRESS);
                if (!string.IsNullOrEmpty(data))
                {
                    foreach (var pair in data.Split(','))
                    {
                        var parts = pair.Split(':');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int count))
                        {
                            progressCounters[parts[0]] = count;
                        }
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                OnAchievementUnlocked = null;
                Instance = null;
            }
        }
    }

    public enum AchievementEvent
    {
        MonsterKill,
        BossKill,
        LevelUp,
        DungeonClear,
        DungeonSpecific,
        GoldReach,
        EnhanceSuccess,
        EquipItem,
        SkillLearn,
        PvPKill,
        PvPRevenge,
        TradeComplete,
        PartyJoin,
        Death
    }

    [System.Serializable]
    public class AchievementData
    {
        public string id;
        public string title;
        public string description;
        public AchievementEvent eventType;
        public int requiredCount;
        public int requiredValue;
        public long rewardExp;
        public long rewardGold;
    }
}
