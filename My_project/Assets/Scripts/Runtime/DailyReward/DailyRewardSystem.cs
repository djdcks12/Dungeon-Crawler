using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 일일 보상 & 출석 시스템
    /// 30일 주기 출석 보상, 연속 출석 보너스, 일일/주간 미션
    /// </summary>
    public class DailyRewardSystem : NetworkBehaviour
    {
        public static DailyRewardSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int cycleDays = 30;          // 출석 주기 (일)
        [SerializeField] private int streakBonusThreshold = 7; // 연속 출석 보너스 기준

        // 일별 보상 (30일)
        private readonly DailyReward[] dailyRewards = new DailyReward[]
        {
            // 1주차: 기본 보상
            new DailyReward(500, 0, null, "골드 500"),
            new DailyReward(0, 100, null, "경험치 100"),
            new DailyReward(800, 0, null, "골드 800"),
            new DailyReward(0, 200, null, "경험치 200"),
            new DailyReward(1000, 100, null, "골드 1000 + 경험치 100"),
            new DailyReward(0, 0, "consumable_healthpotion_medium", "중급 체력 포션"),
            new DailyReward(2000, 500, null, "7일 보상: 골드 2000 + 경험치 500"),

            // 2주차: 중급 보상
            new DailyReward(1000, 0, null, "골드 1000"),
            new DailyReward(0, 300, null, "경험치 300"),
            new DailyReward(1500, 0, null, "골드 1500"),
            new DailyReward(0, 400, null, "경험치 400"),
            new DailyReward(1500, 200, null, "골드 1500 + 경험치 200"),
            new DailyReward(0, 0, "consumable_manapotion_medium", "중급 마나 포션"),
            new DailyReward(3000, 800, null, "14일 보상: 골드 3000 + 경험치 800"),

            // 3주차: 고급 보상
            new DailyReward(2000, 0, null, "골드 2000"),
            new DailyReward(0, 500, null, "경험치 500"),
            new DailyReward(2500, 0, null, "골드 2500"),
            new DailyReward(0, 600, null, "경험치 600"),
            new DailyReward(2500, 300, null, "골드 2500 + 경험치 300"),
            new DailyReward(0, 0, "consumable_healthpotion_large", "고급 체력 포션"),
            new DailyReward(5000, 1200, null, "21일 보상: 골드 5000 + 경험치 1200"),

            // 4주차: 특별 보상
            new DailyReward(3000, 0, null, "골드 3000"),
            new DailyReward(0, 700, null, "경험치 700"),
            new DailyReward(3500, 0, null, "골드 3500"),
            new DailyReward(0, 800, null, "경험치 800"),
            new DailyReward(3500, 400, null, "골드 3500 + 경험치 400"),
            new DailyReward(0, 0, "consumable_resurrectionscroll", "부활 스크롤"),
            new DailyReward(0, 1000, "consumable_strengthscroll", "힘의 스크롤 + 경험치 1000"),
            new DailyReward(5000, 500, null, "골드 5000 + 경험치 500"),
            new DailyReward(0, 0, "consumable_speedscroll", "속도의 스크롤"),
            new DailyReward(10000, 2000, null, "30일 보상: 골드 10000 + 경험치 2000"),
        };

        // 일일 미션
        private readonly DailyMission[] dailyMissions = new DailyMission[]
        {
            new DailyMission("daily_kill_10", "몬스터 10마리 처치", DailyMissionType.KillMonster, 10, 500, 200),
            new DailyMission("daily_dungeon_1", "던전 1회 클리어", DailyMissionType.ClearDungeon, 1, 1000, 500),
            new DailyMission("daily_quest_2", "퀘스트 2개 완료", DailyMissionType.CompleteQuest, 2, 800, 300),
        };

        // 주간 미션
        private readonly DailyMission[] weeklyMissions = new DailyMission[]
        {
            new DailyMission("weekly_kill_100", "몬스터 100마리 처치", DailyMissionType.KillMonster, 100, 5000, 2000),
            new DailyMission("weekly_boss_3", "보스 3마리 처치", DailyMissionType.KillBoss, 3, 8000, 3000),
        };

        // 서버 데이터
        private Dictionary<ulong, PlayerDailyData> playerData = new Dictionary<ulong, PlayerDailyData>();

        // 로컬 데이터
        private PlayerDailyData localData;

        // 이벤트
        public System.Action<int, DailyReward> OnRewardClaimed;      // day, reward
        public System.Action<string> OnMissionCompleted;              // missionId
        public System.Action<int> OnStreakUpdated;                    // streakDays

        // 접근자
        public PlayerDailyData LocalData => localData;
        public DailyReward[] DailyRewards => dailyRewards;
        public DailyMission[] DailyMissions => dailyMissions;
        public DailyMission[] WeeklyMissions => weeklyMissions;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// 로그인 시 출석 체크 (서버 호출)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CheckInServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var data = GetOrCreatePlayerData(clientId);

            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            // 이미 오늘 출석했으면 무시
            if (data.lastCheckInDate == today)
            {
                SyncDailyDataClientRpc(data, clientId);
                return;
            }

            // 어제 출석했으면 연속 출석
            string yesterday = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
            if (data.lastCheckInDate == yesterday)
            {
                data.streakDays++;
            }
            else
            {
                data.streakDays = 1;
            }

            data.lastCheckInDate = today;
            data.currentDay = (data.currentDay % cycleDays) + 1;

            // 일일/주간 미션 초기화 (날짜 바뀌면)
            if (data.lastMissionResetDate != today)
            {
                ResetDailyMissions(data);
                data.lastMissionResetDate = today;
            }

            // 주간 미션 초기화 (월요일)
            if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Monday && data.lastWeeklyResetDate != today)
            {
                ResetWeeklyMissions(data);
                data.lastWeeklyResetDate = today;
            }

            playerData[clientId] = data;
            SyncDailyDataClientRpc(data, clientId);
        }

        /// <summary>
        /// 출석 보상 수령
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ClaimDailyRewardServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var data = GetOrCreatePlayerData(clientId);

            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (data.lastRewardClaimedDate == today)
            {
                SendMessageClientRpc("오늘 보상은 이미 수령했습니다.", clientId);
                return;
            }

            int rewardIndex = (data.currentDay - 1) % dailyRewards.Length;
            var reward = dailyRewards[rewardIndex];

            // 보상 지급
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            if (reward.gold > 0)
                statsData.ChangeGold(reward.gold);
            if (reward.exp > 0)
                statsData.AddExperience(reward.exp);

            // 아이템 보상
            if (!string.IsNullOrEmpty(reward.itemId))
            {
                var inventoryMgr = GetInventoryManager(clientId);
                var itemData = ItemDatabase.GetItem(reward.itemId);
                if (inventoryMgr != null && itemData != null)
                    inventoryMgr.AddItem(new ItemInstance(itemData, 1));
            }

            // 연속 출석 보너스 (7일마다)
            int streakBonus = 0;
            if (data.streakDays > 0 && streakBonusThreshold > 0 && data.streakDays % streakBonusThreshold == 0)
            {
                streakBonus = data.streakDays / streakBonusThreshold * 1000;
                statsData.ChangeGold(streakBonus);
            }

            data.lastRewardClaimedDate = today;
            playerData[clientId] = data;

            NotifyRewardClaimedClientRpc(data.currentDay, reward.gold, reward.exp,
                reward.itemId ?? "", streakBonus, data.streakDays, clientId);
            SyncDailyDataClientRpc(data, clientId);
        }

        /// <summary>
        /// 미션 진행도 업데이트 (서버에서 호출)
        /// </summary>
        public void UpdateMissionProgress(ulong clientId, DailyMissionType type, int amount = 1)
        {
            if (!IsServer) return;
            if (!playerData.TryGetValue(clientId, out var data)) return;

            bool anyCompleted = false;

            // 일일 미션 체크
            for (int i = 0; i < dailyMissions.Length; i++)
            {
                if (dailyMissions[i].type != type) continue;
                string key = dailyMissions[i].missionId;

                if (!data.missionProgress.ContainsKey(key))
                    data.missionProgress[key] = 0;
                if (data.completedMissions.Contains(key)) continue;

                data.missionProgress[key] += amount;
                if (data.missionProgress[key] >= dailyMissions[i].targetCount)
                {
                    CompleteMission(clientId, dailyMissions[i], ref data);
                    anyCompleted = true;
                }
            }

            // 주간 미션 체크
            for (int i = 0; i < weeklyMissions.Length; i++)
            {
                if (weeklyMissions[i].type != type) continue;
                string key = weeklyMissions[i].missionId;

                if (!data.missionProgress.ContainsKey(key))
                    data.missionProgress[key] = 0;
                if (data.completedMissions.Contains(key)) continue;

                data.missionProgress[key] += amount;
                if (data.missionProgress[key] >= weeklyMissions[i].targetCount)
                {
                    CompleteMission(clientId, weeklyMissions[i], ref data);
                    anyCompleted = true;
                }
            }

            playerData[clientId] = data;
            if (anyCompleted)
                SyncDailyDataClientRpc(data, clientId);
        }

        private void CompleteMission(ulong clientId, DailyMission mission, ref PlayerDailyData data)
        {
            data.completedMissions.Add(mission.missionId);

            var statsData = GetPlayerStatsData(clientId);
            if (statsData != null)
            {
                if (mission.goldReward > 0) statsData.ChangeGold(mission.goldReward);
                if (mission.expReward > 0) statsData.AddExperience(mission.expReward);
            }

            NotifyMissionCompletedClientRpc(mission.missionId, mission.missionName,
                mission.goldReward, mission.expReward, clientId);
        }

        private void ResetDailyMissions(PlayerDailyData data)
        {
            foreach (var m in dailyMissions)
            {
                data.missionProgress.Remove(m.missionId);
                data.completedMissions.Remove(m.missionId);
            }
        }

        private void ResetWeeklyMissions(PlayerDailyData data)
        {
            foreach (var m in weeklyMissions)
            {
                data.missionProgress.Remove(m.missionId);
                data.completedMissions.Remove(m.missionId);
            }
        }

        #region ClientRPCs

        [ClientRpc]
        private void SyncDailyDataClientRpc(PlayerDailyData data, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localData = data;
            OnStreakUpdated?.Invoke(data.streakDays);
        }

        [ClientRpc]
        private void NotifyRewardClaimedClientRpc(int day, int gold, int exp,
            string itemId, int streakBonus, int streak, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            string msg = $"출석 보상 수령 (Day {day})!";
            if (gold > 0) msg += $" 골드+{gold}";
            if (exp > 0) msg += $" 경험치+{exp}";
            if (!string.IsNullOrEmpty(itemId)) msg += $" 아이템 획득!";
            if (streakBonus > 0) msg += $"\n연속 출석 {streak}일 보너스: 골드+{streakBonus}";

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification(msg, NotificationType.System);

            OnRewardClaimed?.Invoke(day, dailyRewards[(day - 1) % dailyRewards.Length]);
        }

        [ClientRpc]
        private void NotifyMissionCompletedClientRpc(string missionId, string missionName,
            int gold, int exp, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"미션 완료: {missionName} (골드+{gold}, 경험치+{exp})", NotificationType.QuestComplete);

            OnMissionCompleted?.Invoke(missionId);
        }

        [ClientRpc]
        private void SendMessageClientRpc(string message, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification(message, NotificationType.Warning);
        }

        #endregion

        #region Utility

        private PlayerDailyData GetOrCreatePlayerData(ulong clientId)
        {
            if (!playerData.ContainsKey(clientId))
            {
                playerData[clientId] = new PlayerDailyData
                {
                    currentDay = 0,
                    streakDays = 0,
                    lastCheckInDate = "",
                    lastRewardClaimedDate = "",
                    lastMissionResetDate = "",
                    lastWeeklyResetDate = "",
                    missionProgress = new Dictionary<string, int>(),
                    completedMissions = new HashSet<string>()
                };
            }
            return playerData[clientId];
        }

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        private InventoryManager GetInventoryManager(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject?.GetComponent<InventoryManager>();
        }

        /// <summary>
        /// 미션 진행도 조회 (UI용)
        /// </summary>
        public int GetMissionProgress(string missionId)
        {
            if (localData.missionProgress != null && localData.missionProgress.TryGetValue(missionId, out int val))
                return val;
            return 0;
        }

        public bool IsMissionCompleted(string missionId)
        {
            return localData.completedMissions != null && localData.completedMissions.Contains(missionId);
        }

        #endregion

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }

    #region Data Types

    [System.Serializable]
    public struct DailyReward
    {
        public int gold;
        public int exp;
        public string itemId;
        public string description;

        public DailyReward(int gold, int exp, string itemId, string desc)
        {
            this.gold = gold;
            this.exp = exp;
            this.itemId = itemId;
            this.description = desc;
        }
    }

    public enum DailyMissionType
    {
        KillMonster,
        KillBoss,
        ClearDungeon,
        CompleteQuest,
        CraftItem,
        EnhanceItem,
        SpendGold,
        GainLevel
    }

    [System.Serializable]
    public struct DailyMission
    {
        public string missionId;
        public string missionName;
        public DailyMissionType type;
        public int targetCount;
        public int goldReward;
        public int expReward;

        public DailyMission(string id, string name, DailyMissionType type, int target, int gold, int exp)
        {
            missionId = id;
            missionName = name;
            this.type = type;
            targetCount = target;
            goldReward = gold;
            expReward = exp;
        }
    }

    [System.Serializable]
    public struct PlayerDailyData : INetworkSerializable
    {
        public int currentDay;
        public int streakDays;
        public string lastCheckInDate;
        public string lastRewardClaimedDate;
        public string lastMissionResetDate;
        public string lastWeeklyResetDate;

        // 비네트워크 필드 (서버 전용)
        [System.NonSerialized] public Dictionary<string, int> missionProgress;
        [System.NonSerialized] public HashSet<string> completedMissions;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref currentDay);
            serializer.SerializeValue(ref streakDays);
            serializer.SerializeValue(ref lastCheckInDate);
            serializer.SerializeValue(ref lastRewardClaimedDate);
            serializer.SerializeValue(ref lastMissionResetDate);
            serializer.SerializeValue(ref lastWeeklyResetDate);
        }
    }

    #endregion
}
