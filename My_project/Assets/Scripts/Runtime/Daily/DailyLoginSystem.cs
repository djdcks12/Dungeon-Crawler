using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public enum DailyRewardType { Gold, Exp, Item, MutationStone, BloodShard }

    [System.Serializable]
    public struct DailyLoginReward
    {
        public int day;
        public string rewardName;
        public DailyRewardType type;
        public long amount;
        public string itemId;
    }

    public class PlayerLoginData
    {
        public int totalLoginDays;
        public int consecutiveDays;
        public float lastLoginTime;
        public int currentCycleDay;
        public HashSet<int> claimedDays = new HashSet<int>();
    }

    public class DailyLoginSystem : NetworkBehaviour
    {
        public static DailyLoginSystem Instance { get; private set; }

        public event Action OnLoginDataUpdated;
        public event Action<int> OnDayRewardClaimed;

        // Local client state
        public int localTotalDays { get; private set; }
        public int localConsecutiveDays { get; private set; }
        public int localCycleDay { get; private set; }
        public HashSet<int> localClaimedDays { get; private set; } = new HashSet<int>();

        private Dictionary<ulong, PlayerLoginData> _playerLoginMap = new Dictionary<ulong, PlayerLoginData>();
        private DailyLoginReward[] _rewards;
        private Dictionary<int, float> _consecutiveBonusMultipliers;
        private const int CycleDays = 28;
        private const float SecondsPerDay = 86400f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeRewards();
            InitializeConsecutiveBonuses();
        }

        public override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            }
            base.OnNetworkDespawn();
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!IsServer) return;
            float serverTime = Time.realtimeSinceStartup;
            if (!_playerLoginMap.ContainsKey(clientId))
            {
                _playerLoginMap[clientId] = new PlayerLoginData
                {
                    totalLoginDays = 1,
                    consecutiveDays = 1,
                    lastLoginTime = serverTime,
                    currentCycleDay = 1
                };
            }
            else
            {
                ProcessNewLoginDay(clientId, serverTime);
            }
            var data = _playerLoginMap[clientId];
            SyncLoginDataClientRpc(data.totalLoginDays, data.consecutiveDays, data.currentCycleDay, clientId);
        }

        private void ProcessNewLoginDay(ulong clientId, float serverTime)
        {
            var data = _playerLoginMap[clientId];
            float elapsed = serverTime - data.lastLoginTime;
            int daysPassed = Mathf.FloorToInt(elapsed / SecondsPerDay);
            if (daysPassed >= 1)
            {
                data.totalLoginDays++;
                data.consecutiveDays = (daysPassed == 1) ? data.consecutiveDays + 1 : 1;
                data.currentCycleDay = (data.currentCycleDay % CycleDays) + 1;
                data.lastLoginTime = serverTime;
                data.claimedDays.Clear();
            }
        }

        public DailyLoginReward GetRewardForDay(int day)
        {
            int index = Mathf.Clamp(day - 1, 0, _rewards.Length - 1);
            return _rewards[index];
        }

        public DailyLoginReward[] GetAllRewards() => _rewards;

        public float GetConsecutiveBonus(int consecutiveDays)
        {
            float best = 1f;
            foreach (var kvp in _consecutiveBonusMultipliers)
            {
                if (consecutiveDays >= kvp.Key && kvp.Value > best) best = kvp.Value;
            }
            return best;
        }

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (NetworkManager.Singleton == null) return null;
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
            }
            return null;
        }

        [ServerRpc(RequireOwnership = false)]
        public void ClaimDailyLoginRewardServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!_playerLoginMap.TryGetValue(clientId, out var data)) return;

            int day = data.currentCycleDay;
            if (data.claimedDays.Contains(day)) return;

            DailyLoginReward reward = GetRewardForDay(day);
            float bonus = GetConsecutiveBonus(data.consecutiveDays);
            long finalAmount = reward.amount;

            var statsData = GetPlayerStatsData(clientId);

            switch (reward.type)
            {
                case DailyRewardType.Gold:
                    finalAmount = (long)(reward.amount * bonus);
                    if (statsData != null) statsData.ChangeGold(finalAmount);
                    break;
                case DailyRewardType.Exp:
                    finalAmount = (long)(reward.amount * bonus);
                    if (statsData != null) statsData.AddExperience(finalAmount);
                    break;
                case DailyRewardType.Item:
                case DailyRewardType.MutationStone:
                case DailyRewardType.BloodShard:
                    SendItemRewardViaMail(clientId, reward, day);
                    break;
            }

            data.claimedDays.Add(day);
            NotifyRewardClaimedClientRpc(reward.rewardName, finalAmount, day, clientId);
            SyncLoginDataClientRpc(data.totalLoginDays, data.consecutiveDays, data.currentCycleDay, clientId);
        }

        private void SendItemRewardViaMail(ulong clientId, DailyLoginReward reward, int day)
        {
            string title = $"Day {day} Attendance Reward";
            string body = $"{reward.rewardName} x{reward.amount}";
            var attachment = new MailAttachment
            {
                gold = 0,
                itemId = reward.itemId,
                quantity = (int)reward.amount,
                enhanceLevel = 0
            };
            MailSystem.Instance?.SendSystemMail(clientId, title, body, MailType.SystemReward, attachment, true);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestLoginDataServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!_playerLoginMap.TryGetValue(clientId, out var data)) return;
            SyncLoginDataClientRpc(data.totalLoginDays, data.consecutiveDays, data.currentCycleDay, clientId);
        }

        [ClientRpc]
        private void SyncLoginDataClientRpc(int totalDays, int consecutiveDays, int cycleDay, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localTotalDays = totalDays;
            localConsecutiveDays = consecutiveDays;
            localCycleDay = cycleDay;
            OnLoginDataUpdated?.Invoke();
        }

        [ClientRpc]
        private void NotifyRewardClaimedClientRpc(string rewardName, long amount, int day, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localClaimedDays.Add(day);
            string msg = $"Day {day} reward claimed: {rewardName} x{amount}";
            NotificationManager.Instance?.ShowNotification(msg, NotificationType.System);
            OnDayRewardClaimed?.Invoke(day);
        }

        private void InitializeConsecutiveBonuses()
        {
            _consecutiveBonusMultipliers = new Dictionary<int, float>
            {
                { 3, 1.5f },
                { 7, 2.0f },
                { 14, 3.0f },
                { 21, 4.0f },
                { 28, 5.0f }
            };
        }

        private void InitializeRewards()
        {
            _rewards = new DailyLoginReward[]
            {
                new DailyLoginReward { day =  1, rewardName = "Gold Pouch            ", type = DailyRewardType.Gold         , amount =  500, itemId = "" },
                new DailyLoginReward { day =  2, rewardName = "Gold Pouch            ", type = DailyRewardType.Gold         , amount = 1000, itemId = "" },
                new DailyLoginReward { day =  3, rewardName = "Gold Pouch            ", type = DailyRewardType.Gold         , amount = 1500, itemId = "" },
                new DailyLoginReward { day =  4, rewardName = "Gold Pouch            ", type = DailyRewardType.Gold         , amount = 2000, itemId = "" },
                new DailyLoginReward { day =  5, rewardName = "Gold Pouch            ", type = DailyRewardType.Gold         , amount = 2500, itemId = "" },
                new DailyLoginReward { day =  6, rewardName = "Gold Pouch            ", type = DailyRewardType.Gold         , amount = 3000, itemId = "" },
                new DailyLoginReward { day =  7, rewardName = "Rare Equipment Box    ", type = DailyRewardType.Item         , amount =    1, itemId = "item_rare_box_01" },
                new DailyLoginReward { day =  8, rewardName = "Exp Scroll            ", type = DailyRewardType.Exp          , amount = 1000, itemId = "" },
                new DailyLoginReward { day =  9, rewardName = "Exp Scroll            ", type = DailyRewardType.Exp          , amount = 2000, itemId = "" },
                new DailyLoginReward { day = 10, rewardName = "Exp Scroll            ", type = DailyRewardType.Exp          , amount = 3000, itemId = "" },
                new DailyLoginReward { day = 11, rewardName = "Exp Scroll            ", type = DailyRewardType.Exp          , amount = 4000, itemId = "" },
                new DailyLoginReward { day = 12, rewardName = "Exp Scroll            ", type = DailyRewardType.Exp          , amount = 5000, itemId = "" },
                new DailyLoginReward { day = 13, rewardName = "Exp Scroll            ", type = DailyRewardType.Exp          , amount = 6000, itemId = "" },
                new DailyLoginReward { day = 14, rewardName = "Epic Equipment Box    ", type = DailyRewardType.Item         , amount =    1, itemId = "item_epic_box_01" },
                new DailyLoginReward { day = 15, rewardName = "Mutation Stone        ", type = DailyRewardType.MutationStone, amount =    1, itemId = "item_mutation_stone" },
                new DailyLoginReward { day = 16, rewardName = "Mutation Stone        ", type = DailyRewardType.MutationStone, amount =    2, itemId = "item_mutation_stone" },
                new DailyLoginReward { day = 17, rewardName = "Mutation Stone        ", type = DailyRewardType.MutationStone, amount =    3, itemId = "item_mutation_stone" },
                new DailyLoginReward { day = 18, rewardName = "Mutation Stone        ", type = DailyRewardType.MutationStone, amount =    4, itemId = "item_mutation_stone" },
                new DailyLoginReward { day = 19, rewardName = "Mutation Stone        ", type = DailyRewardType.MutationStone, amount =    5, itemId = "item_mutation_stone" },
                new DailyLoginReward { day = 20, rewardName = "Mutation Stone        ", type = DailyRewardType.MutationStone, amount =   10, itemId = "item_mutation_stone" },
                new DailyLoginReward { day = 21, rewardName = "Epic Equipment Box    ", type = DailyRewardType.Item         , amount =    1, itemId = "item_epic_box_02" },
                new DailyLoginReward { day = 22, rewardName = "Blood Shard           ", type = DailyRewardType.BloodShard   , amount =   50, itemId = "item_blood_shard" },
                new DailyLoginReward { day = 23, rewardName = "Blood Shard           ", type = DailyRewardType.BloodShard   , amount =  100, itemId = "item_blood_shard" },
                new DailyLoginReward { day = 24, rewardName = "Blood Shard           ", type = DailyRewardType.BloodShard   , amount =  150, itemId = "item_blood_shard" },
                new DailyLoginReward { day = 25, rewardName = "Blood Shard           ", type = DailyRewardType.BloodShard   , amount =  200, itemId = "item_blood_shard" },
                new DailyLoginReward { day = 26, rewardName = "Blood Shard           ", type = DailyRewardType.BloodShard   , amount =  250, itemId = "item_blood_shard" },
                new DailyLoginReward { day = 27, rewardName = "Blood Shard           ", type = DailyRewardType.BloodShard   , amount =  500, itemId = "item_blood_shard" },
                new DailyLoginReward { day = 28, rewardName = "Legendary Chest       ", type = DailyRewardType.Item         , amount =    1, itemId = "item_legendary_box_01" }
            };
        }
    }
}
