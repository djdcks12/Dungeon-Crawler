using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public enum LeaderboardCategory
    {
        Level,
        DungeonClear,
        TotalDamage,
        PvPKills,
        GoldEarned,
        CollectionRate,
        BossKills,
        CraftingLevel
    }

    [System.Serializable]
    public class LeaderboardEntry
    {
        public ulong clientId;
        public string playerName;
        public long score;
        public int rank;

        public LeaderboardEntry(ulong clientId, string playerName, long score, int rank = 0)
        {
            this.clientId = clientId;
            this.playerName = playerName;
            this.score = score;
            this.rank = rank;
        }
    }

    public class LeaderboardSystem : NetworkBehaviour
    {
        public static LeaderboardSystem Instance { get; private set; }

        private const int MAX_ENTRIES = 100;
        private const float UPDATE_INTERVAL = 60f;
        private const int SYNC_TOP_COUNT = 10;

        // Server state
        private Dictionary<LeaderboardCategory, List<LeaderboardEntry>> leaderboards
            = new Dictionary<LeaderboardCategory, List<LeaderboardEntry>>();

        // Client local state
        private Dictionary<LeaderboardCategory, List<LeaderboardEntry>> localLeaderboards
            = new Dictionary<LeaderboardCategory, List<LeaderboardEntry>>();
        private Dictionary<LeaderboardCategory, int> localPlayerRank
            = new Dictionary<LeaderboardCategory, int>();

        // Events
        public Action<LeaderboardCategory> OnLeaderboardUpdated;
        public Action<int, int> OnRankChanged;

        private float updateTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeBoards(leaderboards);
            InitializeBoards(localLeaderboards);
            foreach (LeaderboardCategory cat in Enum.GetValues(typeof(LeaderboardCategory)))
                localPlayerRank[cat] = 0;
        }

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                OnLeaderboardUpdated = null;
                OnRankChanged = null;
                Instance = null;
            }
            base.OnDestroy();
        }

        private void InitializeBoards(Dictionary<LeaderboardCategory, List<LeaderboardEntry>> boards)
        {
            foreach (LeaderboardCategory cat in Enum.GetValues(typeof(LeaderboardCategory)))
                boards[cat] = new List<LeaderboardEntry>();
        }

        private void Update()
        {
            if (!IsServer) return;

            updateTimer += Time.deltaTime;
            if (updateTimer >= UPDATE_INTERVAL)
            {
                updateTimer = 0f;
                SortAndRankAllBoards();
            }
        }

        private void SortAndRankAllBoards()
        {
            foreach (LeaderboardCategory cat in Enum.GetValues(typeof(LeaderboardCategory)))
                SortAndRankBoard(cat);
        }

        private void SortAndRankBoard(LeaderboardCategory category)
        {
            if (!leaderboards.ContainsKey(category)) return;

            var board = leaderboards[category];
            board.Sort((a, b) => b.score.CompareTo(a.score));

            while (board.Count > MAX_ENTRIES)
                board.RemoveAt(board.Count - 1);

            for (int i = 0; i < board.Count; i++)
                board[i].rank = i + 1;
        }

        // --- Server Public API ---

        public void UpdateScore(ulong clientId, string playerName, LeaderboardCategory category, long score)
        {
            if (!IsServer) return;

            var board = leaderboards[category];
            var entry = board.Find(e => e.clientId == clientId);
            int oldRank = entry?.rank ?? 0;

            if (entry != null)
            {
                entry.score = score;
                entry.playerName = playerName;
            }
            else
            {
                board.Add(new LeaderboardEntry(clientId, playerName, score));
            }

            SortAndRankBoard(category);

            var updated = board.Find(e => e.clientId == clientId);
            if (updated != null && oldRank != 0 && updated.rank != oldRank)
            {
                NotifyRankChangedClientRpc((int)category, updated.rank, oldRank, clientId);
            }
        }

        public void IncrementScore(ulong clientId, string playerName, LeaderboardCategory category, long delta)
        {
            if (!IsServer) return;

            var board = leaderboards[category];
            var entry = board.Find(e => e.clientId == clientId);
            long currentScore = entry?.score ?? 0;
            UpdateScore(clientId, playerName, category, currentScore + delta);
        }

        public int GetPlayerRank(ulong clientId, LeaderboardCategory category)
        {
            if (!leaderboards.ContainsKey(category)) return 0;

            var entry = leaderboards[category].Find(e => e.clientId == clientId);
            return entry?.rank ?? 0;
        }

        public List<LeaderboardEntry> GetTopEntries(LeaderboardCategory category, int count)
        {
            if (!leaderboards.ContainsKey(category))
                return new List<LeaderboardEntry>();

            return leaderboards[category].Take(Mathf.Min(count, MAX_ENTRIES)).ToList();
        }

        // --- Season Reset ---

        public void ResetAllLeaderboards()
        {
            if (!IsServer) return;

            foreach (LeaderboardCategory cat in Enum.GetValues(typeof(LeaderboardCategory)))
                leaderboards[cat].Clear();

            Debug.Log("[LeaderboardSystem] All leaderboards have been reset (season reset).");
        }

        public void ResetLeaderboard(LeaderboardCategory category)
        {
            if (!IsServer) return;

            if (leaderboards.ContainsKey(category))
            {
                leaderboards[category].Clear();
                Debug.Log($"[LeaderboardSystem] Leaderboard {category} has been reset.");
            }
        }

        // --- Network RPCs ---

        [ServerRpc(RequireOwnership = false)]
        public void RequestLeaderboardServerRpc(int categoryInt, ServerRpcParams rpcParams = default)
        {
            if (!Enum.IsDefined(typeof(LeaderboardCategory), categoryInt)) return;

            var category = (LeaderboardCategory)categoryInt;
            var topEntries = GetTopEntries(category, SYNC_TOP_COUNT);
            string serialized = SerializeEntries(topEntries);

            ulong senderClientId = rpcParams.Receive.SenderClientId;
            ClientRpcParams clientParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { senderClientId }
                }
            };

            SyncLeaderboardClientRpc(categoryInt, serialized, clientParams);
        }

        [ClientRpc]
        public void SyncLeaderboardClientRpc(int categoryInt, string serializedData, ClientRpcParams clientRpcParams = default)
        {
            var category = (LeaderboardCategory)categoryInt;
            var entries = DeserializeEntries(serializedData);

            localLeaderboards[category] = entries;

            var localEntry = entries.Find(e => e.clientId == NetworkManager.Singleton.LocalClientId);
            localPlayerRank[category] = localEntry?.rank ?? 0;

            OnLeaderboardUpdated?.Invoke(category);
        }

        [ClientRpc]
        public void NotifyRankChangedClientRpc(int category, int newRank, int oldRank, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            var cat = (LeaderboardCategory)category;
            localPlayerRank[cat] = newRank;

            OnRankChanged?.Invoke(newRank, oldRank);

            string direction = newRank < oldRank ? "UP" : "DOWN";
            string msg = $"{cat} ranking changed: #{oldRank} -> #{newRank} ({direction})";
            Debug.Log($"[LeaderboardSystem] {msg}");

            NotificationManager.Instance?.ShowNotification(msg, NotificationType.System);
        }

        // --- Serialization (comma-separated) ---

        private string SerializeEntries(List<LeaderboardEntry> entries)
        {
            if (entries == null || entries.Count == 0) return string.Empty;

            var parts = new List<string>();
            foreach (var e in entries)
                parts.Add($"{e.clientId}|{e.playerName}|{e.score}|{e.rank}");

            return string.Join(",", parts);
        }

        private List<LeaderboardEntry> DeserializeEntries(string data)
        {
            var result = new List<LeaderboardEntry>();
            if (string.IsNullOrEmpty(data)) return result;

            string[] entries = data.Split(',');
            foreach (string entryStr in entries)
            {
                string[] fields = entryStr.Split('|');
                if (fields.Length < 4) continue;

                if (ulong.TryParse(fields[0], out ulong clientId) &&
                    long.TryParse(fields[2], out long score) &&
                    int.TryParse(fields[3], out int rank))
                {
                    result.Add(new LeaderboardEntry(clientId, fields[1], score, rank));
                }
            }

            return result;
        }

        // --- Client Convenience ---

        public List<LeaderboardEntry> GetLocalTopEntries(LeaderboardCategory category, int count)
        {
            if (!localLeaderboards.ContainsKey(category))
                return new List<LeaderboardEntry>();

            return localLeaderboards[category].Take(count).ToList();
        }

        public int GetLocalPlayerRank(LeaderboardCategory category)
        {
            return localPlayerRank.ContainsKey(category) ? localPlayerRank[category] : 0;
        }

        // --- Integration Hooks ---

        public void OnMonsterKilled(ulong clientId, string playerName, long damage, bool isBoss)
        {
            if (!IsServer) return;

            IncrementScore(clientId, playerName, LeaderboardCategory.TotalDamage, damage);

            if (isBoss)
                IncrementScore(clientId, playerName, LeaderboardCategory.BossKills, 1);
        }

        public void OnLevelUp(ulong clientId, string playerName, int newLevel)
        {
            if (!IsServer) return;

            UpdateScore(clientId, playerName, LeaderboardCategory.Level, newLevel);
        }

        public void OnDungeonCleared(ulong clientId, string playerName)
        {
            if (!IsServer) return;

            IncrementScore(clientId, playerName, LeaderboardCategory.DungeonClear, 1);
        }

        public void OnPvPKill(ulong clientId, string playerName)
        {
            if (!IsServer) return;

            IncrementScore(clientId, playerName, LeaderboardCategory.PvPKills, 1);
        }

        public void OnGoldEarned(ulong clientId, string playerName, long amount)
        {
            if (!IsServer) return;

            IncrementScore(clientId, playerName, LeaderboardCategory.GoldEarned, amount);
        }
    }
}
