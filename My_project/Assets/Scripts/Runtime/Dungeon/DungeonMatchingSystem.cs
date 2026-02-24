using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public enum MatchRole
    {
        Tank = 0,
        DPS = 1,
        Healer = 2
    }

    public class QueueEntry
    {
        public ulong clientId;
        public string playerName;
        public int level;
        public int gearScore;
        public MatchRole role;
        public string dungeonId;
        public float queueTime;
    }

    public class MatchResult
    {
        public string matchId;
        public string dungeonId;
        public List<ulong> members = new List<ulong>();
        public Dictionary<ulong, MatchRole> roles = new Dictionary<ulong, MatchRole>();
    }

    public class PendingMatch
    {
        public MatchResult result;
        public HashSet<ulong> acceptedClients = new HashSet<ulong>();
        public float createdTime;
        public const float ACCEPT_TIMEOUT = 30f;
    }

    public class DungeonMatchingSystem : NetworkBehaviour
    {
        public static DungeonMatchingSystem Instance { get; private set; }

        private const float MATCH_CHECK_INTERVAL = 5f;
        private const float MAX_QUEUE_TIME = 300f;
        private const float FLEXIBLE_ROLE_TIME = 60f;
        private const float EXPANDED_LEVEL_TIME = 120f;
        private const int DEFAULT_LEVEL_RANGE = 3;
        private const int EXPANDED_LEVEL_RANGE = 5;
        private const int REQUIRED_PARTY_SIZE = 4;

        private Dictionary<string, List<QueueEntry>> dungeonQueues = new Dictionary<string, List<QueueEntry>>();
        private Dictionary<string, PendingMatch> pendingMatches = new Dictionary<string, PendingMatch>();
        private float lastMatchCheckTime;

        // Local client state
        public bool localInQueue { get; private set; }
        public float localQueueStartTime { get; private set; }
        public string localQueuedDungeon { get; private set; }
        public bool localMatchPending { get; private set; }

        // Events
        public System.Action OnQueueStateChanged;
        public System.Action<string> OnMatchFound;
        public System.Action OnMatchStarted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            base.OnDestroy();
        }

        private void Update()
        {
            if (!IsServer) return;

            if (Time.time - lastMatchCheckTime >= MATCH_CHECK_INTERVAL)
            {
                lastMatchCheckTime = Time.time;
                ProcessQueues();
                ProcessPendingMatches();
            }
        }

        #region ServerRpcs

        [ServerRpc(RequireOwnership = false)]
        public void JoinQueueServerRpc(string dungeonId, int roleInt, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            MatchRole role = (MatchRole)roleInt;

            // Remove from any existing queue
            RemoveClientFromAllQueues(clientId);

            if (!dungeonQueues.ContainsKey(dungeonId))
            {
                dungeonQueues[dungeonId] = new List<QueueEntry>();
            }

            PlayerStatsData stats = GetPlayerStatsData(clientId);
            int playerLevel = stats != null ? stats.CurrentLevel : 1;
            string playerName = stats != null ? stats.CharacterName : $"Player_{clientId}";

            QueueEntry entry = new QueueEntry
            {
                clientId = clientId,
                playerName = playerName,
                level = playerLevel,
                gearScore = playerLevel * 10,
                role = role,
                dungeonId = dungeonId,
                queueTime = Time.time
            };

            dungeonQueues[dungeonId].Add(entry);

            int estimatedWait = EstimateWaitTime(dungeonId);
            NotifyQueueJoinedClientRpc(dungeonId, estimatedWait, clientId);

            Debug.Log($"[DungeonMatching] {playerName} joined queue for {dungeonId} as {role}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void LeaveQueueServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            RemoveClientFromAllQueues(clientId);
            NotifyMatchCancelledClientRpc("You left the queue.", clientId);
            Debug.Log($"[DungeonMatching] Client {clientId} left queue.");
        }

        [ServerRpc(RequireOwnership = false)]
        public void AcceptMatchServerRpc(string matchId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!pendingMatches.ContainsKey(matchId)) return;

            PendingMatch pending = pendingMatches[matchId];
            pending.acceptedClients.Add(clientId);

            if (pending.acceptedClients.Count >= pending.result.members.Count)
            {
                StartMatch(pending.result);
                pendingMatches.Remove(matchId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void DeclineMatchServerRpc(string matchId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!pendingMatches.ContainsKey(matchId)) return;

            PendingMatch pending = pendingMatches[matchId];
            pendingMatches.Remove(matchId);

            // Re-queue others who accepted
            foreach (ulong memberId in pending.result.members)
            {
                if (memberId == clientId) continue;

                MatchRole memberRole = pending.result.roles[memberId];
                ReQueueClient(memberId, pending.result.dungeonId, memberRole);
                NotifyMatchCancelledClientRpc("A player declined the match. Re-queuing...", memberId);
            }

            NotifyMatchCancelledClientRpc("You declined the match.", clientId);
            Debug.Log($"[DungeonMatching] Client {clientId} declined match {matchId}.");
        }

        #endregion

        #region ClientRpcs

        [ClientRpc]
        private void NotifyQueueJoinedClientRpc(string dungeonName, int estimatedWait, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            localInQueue = true;
            localQueueStartTime = Time.time;
            localQueuedDungeon = dungeonName;
            localMatchPending = false;

            OnQueueStateChanged?.Invoke();
            string msg = $"Joined queue for {dungeonName}. Estimated wait: {estimatedWait}s";
            NotificationManager.Instance?.ShowNotification(msg, NotificationType.System);
        }

        [ClientRpc]
        private void NotifyMatchFoundClientRpc(string matchId, string dungeonName, int playerCount, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            localMatchPending = true;
            OnMatchFound?.Invoke(matchId);
            string msg = $"Match found for {dungeonName}! ({playerCount} players) Accept?";
            NotificationManager.Instance?.ShowNotification(msg, NotificationType.System);
        }

        [ClientRpc]
        private void NotifyMatchStartClientRpc(string dungeonName, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            localInQueue = false;
            localMatchPending = false;
            localQueuedDungeon = null;

            OnMatchStarted?.Invoke();
            OnQueueStateChanged?.Invoke();
            string msg = $"Entering {dungeonName}!";
            NotificationManager.Instance?.ShowNotification(msg, NotificationType.System);
        }

        [ClientRpc]
        private void NotifyMatchCancelledClientRpc(string reason, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            localInQueue = false;
            localMatchPending = false;
            localQueuedDungeon = null;

            OnQueueStateChanged?.Invoke();
            NotificationManager.Instance?.ShowNotification(reason, NotificationType.Warning);
        }

        #endregion

        #region Server Logic

        private void ProcessQueues()
        {
            foreach (var kvp in dungeonQueues)
            {
                string dungeonId = kvp.Key;
                List<QueueEntry> queue = kvp.Value;

                // Remove timed-out entries
                for (int i = queue.Count - 1; i >= 0; i--)
                {
                    if (Time.time - queue[i].queueTime > MAX_QUEUE_TIME)
                    {
                        NotifyMatchCancelledClientRpc("Queue timed out.", queue[i].clientId);
                        queue.RemoveAt(i);
                    }
                }

                if (queue.Count < REQUIRED_PARTY_SIZE) continue;

                TryFormMatch(dungeonId, queue);
            }
        }

        private void TryFormMatch(string dungeonId, List<QueueEntry> queue)
        {
            // Sort by queue time (oldest first)
            queue.Sort((a, b) => a.queueTime.CompareTo(b.queueTime));

            for (int i = 0; i < queue.Count; i++)
            {
                QueueEntry anchor = queue[i];
                float waitTime = Time.time - anchor.queueTime;
                int levelRange = waitTime >= EXPANDED_LEVEL_TIME ? EXPANDED_LEVEL_RANGE : DEFAULT_LEVEL_RANGE;
                bool flexibleRoles = waitTime >= FLEXIBLE_ROLE_TIME;

                List<QueueEntry> candidates = queue
                    .Where(e => Mathf.Abs(e.level - anchor.level) <= levelRange)
                    .ToList();

                if (candidates.Count < REQUIRED_PARTY_SIZE) continue;

                List<QueueEntry> party = TryBuildParty(candidates, flexibleRoles);
                if (party != null && party.Count == REQUIRED_PARTY_SIZE)
                {
                    CreatePendingMatch(dungeonId, party);
                    foreach (var member in party)
                    {
                        queue.Remove(member);
                    }
                    return;
                }
            }
        }

        private List<QueueEntry> TryBuildParty(List<QueueEntry> candidates, bool flexibleRoles)
        {
            if (flexibleRoles)
            {
                // Any 4 players regardless of role
                return candidates.Take(REQUIRED_PARTY_SIZE).ToList();
            }

            // Strict composition: 1 Tank + 2 DPS + 1 Healer
            var tanks = candidates.Where(e => e.role == MatchRole.Tank).ToList();
            var dps = candidates.Where(e => e.role == MatchRole.DPS).ToList();
            var healers = candidates.Where(e => e.role == MatchRole.Healer).ToList();

            if (tanks.Count < 1 || dps.Count < 2 || healers.Count < 1)
            {
                return null;
            }

            List<QueueEntry> party = new List<QueueEntry>
            {
                tanks[0],
                dps[0],
                dps[1],
                healers[0]
            };

            return party;
        }

        private void CreatePendingMatch(string dungeonId, List<QueueEntry> party)
        {
            string matchId = $"match_{dungeonId}_{System.DateTime.UtcNow.Ticks}";

            MatchResult result = new MatchResult
            {
                matchId = matchId,
                dungeonId = dungeonId
            };

            foreach (var entry in party)
            {
                result.members.Add(entry.clientId);
                result.roles[entry.clientId] = entry.role;
            }

            PendingMatch pending = new PendingMatch
            {
                result = result,
                createdTime = Time.time
            };

            pendingMatches[matchId] = pending;

            foreach (ulong memberId in result.members)
            {
                NotifyMatchFoundClientRpc(matchId, dungeonId, party.Count, memberId);
            }

            Debug.Log($"[DungeonMatching] Pending match {matchId} created with {party.Count} players.");
        }

        private void ProcessPendingMatches()
        {
            List<string> expiredMatches = new List<string>();

            foreach (var kvp in pendingMatches)
            {
                PendingMatch pending = kvp.Value;
                if (Time.time - pending.createdTime > PendingMatch.ACCEPT_TIMEOUT)
                {
                    expiredMatches.Add(kvp.Key);
                }
            }

            foreach (string matchId in expiredMatches)
            {
                PendingMatch pending = pendingMatches[matchId];

                foreach (ulong memberId in pending.result.members)
                {
                    if (pending.acceptedClients.Contains(memberId))
                    {
                        MatchRole memberRole = pending.result.roles[memberId];
                        ReQueueClient(memberId, pending.result.dungeonId, memberRole);
                        NotifyMatchCancelledClientRpc("Match timed out. Re-queuing...", memberId);
                    }
                    else
                    {
                        NotifyMatchCancelledClientRpc("Match timed out.", memberId);
                    }
                }

                pendingMatches.Remove(matchId);
            }
        }

        private void StartMatch(MatchResult result)
        {
            foreach (ulong memberId in result.members)
            {
                NotifyMatchStartClientRpc(result.dungeonId, memberId);
            }

            Debug.Log($"[DungeonMatching] Match {result.matchId} started for dungeon {result.dungeonId}.");
        }

        #endregion

        #region Helpers

        private void RemoveClientFromAllQueues(ulong clientId)
        {
            foreach (var kvp in dungeonQueues)
            {
                kvp.Value.RemoveAll(e => e.clientId == clientId);
            }
        }

        private void ReQueueClient(ulong clientId, string dungeonId, MatchRole role)
        {
            if (!dungeonQueues.ContainsKey(dungeonId))
            {
                dungeonQueues[dungeonId] = new List<QueueEntry>();
            }

            PlayerStatsData stats = GetPlayerStatsData(clientId);
            int playerLevel = stats != null ? stats.CurrentLevel : 1;
            string playerName = stats != null ? stats.CharacterName : $"Player_{clientId}";

            QueueEntry entry = new QueueEntry
            {
                clientId = clientId,
                playerName = playerName,
                level = playerLevel,
                gearScore = playerLevel * 10,
                role = role,
                dungeonId = dungeonId,
                queueTime = Time.time
            };

            dungeonQueues[dungeonId].Add(entry);
        }

        private int EstimateWaitTime(string dungeonId)
        {
            if (!dungeonQueues.ContainsKey(dungeonId)) return 120;

            int queueSize = dungeonQueues[dungeonId].Count;
            if (queueSize >= REQUIRED_PARTY_SIZE) return 10;
            if (queueSize >= 2) return 60;
            return 120;
        }

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (NetworkManager.Singleton == null) return null;
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) return null;

            var client = NetworkManager.Singleton.ConnectedClients[clientId];
            if (client.PlayerObject == null) return null;

            return client.PlayerObject.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        #endregion
    }
}
