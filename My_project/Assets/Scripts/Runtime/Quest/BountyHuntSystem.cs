using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class BountyHuntSystem : NetworkBehaviour
    {
        public static BountyHuntSystem Instance { get; private set; }

        private Dictionary<ulong, BountyBoard> playerBounties = new Dictionary<ulong, BountyBoard>();

        public System.Action OnBountiesUpdated;

        private static readonly BountyTemplate[] bountyPool = new BountyTemplate[]
        {
            new BountyTemplate("Goblin Slayer", BountyType.KillMonster, "Goblin", 20, 500, 100, 1),
            new BountyTemplate("Orc Crusher", BountyType.KillMonster, "Orc", 15, 800, 150, 2),
            new BountyTemplate("Undead Purifier", BountyType.KillMonster, "Undead", 15, 1000, 200, 3),
            new BountyTemplate("Beast Tamer", BountyType.KillMonster, "Beast", 10, 700, 120, 2),
            new BountyTemplate("Elemental Banisher", BountyType.KillMonster, "Elemental", 10, 1200, 250, 4),
            new BountyTemplate("Demon Hunter", BountyType.KillMonster, "Demon", 8, 1500, 300, 5),
            new BountyTemplate("Dragon Slayer", BountyType.KillMonster, "Dragon", 3, 3000, 500, 7),
            new BountyTemplate("Construct Breaker", BountyType.KillMonster, "Construct", 12, 900, 180, 3),
            new BountyTemplate("Elite Hunter", BountyType.KillElite, "", 5, 2000, 400, 5),
            new BountyTemplate("Boss Vanquisher", BountyType.KillBoss, "", 1, 5000, 1000, 8),
            new BountyTemplate("Dungeon Clearer", BountyType.ClearZone, "any", 1, 3000, 600, 6),
            new BountyTemplate("Deep Explorer", BountyType.ClearZone, "deep", 1, 4000, 800, 7),
            new BountyTemplate("Mass Slaughter", BountyType.KillMonster, "any", 50, 1500, 300, 3),
            new BountyTemplate("Quick Strike", BountyType.KillElite, "", 3, 1000, 200, 4),
            new BountyTemplate("Champion Seeker", BountyType.KillBoss, "", 2, 8000, 1500, 9),
        };

        public override void OnNetworkSpawn()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (IsClient) RequestSyncServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestDailyBountiesServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var board = GetOrCreateBoard(clientId);

            if (board.lastRefreshDay == GetCurrentDay() && board.activeBounties.Count > 0)
            {
                SyncBountiesToClient(clientId, board);
                return;
            }

            board.activeBounties.Clear();
            board.lastRefreshDay = GetCurrentDay();
            board.completedToday = 0;

            List<int> used = new List<int>();
            for (int i = 0; i < 5; i++)
            {
                int idx;
                int tries = 0;
                do { idx = Random.Range(0, bountyPool.Length); tries++; }
                while (used.Contains(idx) && tries < 50);
                used.Add(idx);

                var tmpl = bountyPool[idx];
                board.activeBounties.Add(new ActiveBounty
                {
                    templateIndex = idx,
                    currentCount = 0,
                    targetCount = tmpl.targetCount,
                    completed = false,
                    rewarded = false
                });
            }

            SyncBountiesToClient(clientId, board);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ClaimBountyRewardServerRpc(int bountyIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var board = GetOrCreateBoard(clientId);

            if (bountyIndex < 0 || bountyIndex >= board.activeBounties.Count) return;
            var bounty = board.activeBounties[bountyIndex];
            if (!bounty.completed || bounty.rewarded) return;

            var tmpl = bountyPool[bounty.templateIndex];
            bounty.rewarded = true;
            board.completedToday++;

            var stats = GetPlayerStatsData(clientId);
            if (stats != null)
            {
                stats.ChangeGold(tmpl.goldReward);
                stats.AddExperience(tmpl.expReward);
            }

            if (SkillMutationSystem.Instance != null)
            {
                int stoneType = Random.Range(1, 11);
                SkillMutationSystem.Instance.AddMutationStone(clientId, (MutationType)stoneType, 1);
            }

            BountyRewardClientRpc(bountyIndex, tmpl.goldReward, tmpl.expReward, clientId);

            if (board.completedToday >= 5)
            {
                long bonusGold = 5000;
                long bonusExp = 2000;
                if (stats != null)
                {
                    stats.ChangeGold(bonusGold);
                    stats.AddExperience(bonusExp);
                }
                AllBountiesCompleteClientRpc(bonusGold, bonusExp, clientId);
            }
        }

        public void OnMonsterKilled(ulong clientId, string monsterRace, bool isElite, bool isBoss)
        {
            if (!IsServer) return;
            var board = GetOrCreateBoard(clientId);

            bool anyUpdated = false;
            for (int i = 0; i < board.activeBounties.Count; i++)
            {
                var bounty = board.activeBounties[i];
                if (bounty.completed || bounty.rewarded) continue;

                var tmpl = bountyPool[bounty.templateIndex];
                bool matches = false;

                switch (tmpl.type)
                {
                    case BountyType.KillMonster:
                        matches = tmpl.targetId == "any" || tmpl.targetId == monsterRace;
                        break;
                    case BountyType.KillElite:
                        matches = isElite || isBoss;
                        break;
                    case BountyType.KillBoss:
                        matches = isBoss;
                        break;
                }

                if (matches)
                {
                    bounty.currentCount++;
                    if (bounty.currentCount >= bounty.targetCount)
                        bounty.completed = true;
                    anyUpdated = true;
                }
            }

            if (anyUpdated)
                SyncBountiesToClient(clientId, board);
        }

        public void OnZoneCleared(ulong clientId, string zoneType)
        {
            if (!IsServer) return;
            var board = GetOrCreateBoard(clientId);

            bool anyUpdated = false;
            for (int i = 0; i < board.activeBounties.Count; i++)
            {
                var bounty = board.activeBounties[i];
                if (bounty.completed || bounty.rewarded) continue;

                var tmpl = bountyPool[bounty.templateIndex];
                if (tmpl.type == BountyType.ClearZone &&
                    (tmpl.targetId == "any" || tmpl.targetId == zoneType))
                {
                    bounty.currentCount++;
                    if (bounty.currentCount >= bounty.targetCount)
                        bounty.completed = true;
                    anyUpdated = true;
                }
            }

            if (anyUpdated)
                SyncBountiesToClient(clientId, board);
        }

        public static BountyTemplate GetBountyTemplate(int index)
        {
            if (index < 0 || index >= bountyPool.Length) return null;
            return bountyPool[index];
        }

        #region Sync

        [ServerRpc(RequireOwnership = false)]
        private void RequestSyncServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var board = GetOrCreateBoard(clientId);
            SyncBountiesToClient(clientId, board);
        }

        private void SyncBountiesToClient(ulong clientId, BountyBoard board)
        {
            ClearBountiesClientRpc(clientId);
            for (int i = 0; i < board.activeBounties.Count; i++)
            {
                var b = board.activeBounties[i];
                SyncBountyClientRpc(i, b.templateIndex, b.currentCount, b.targetCount, b.completed, b.rewarded, clientId);
            }
            SyncDoneClientRpc(board.completedToday, clientId);
        }

        [ClientRpc]
        private void ClearBountiesClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
        }

        [ClientRpc]
        private void SyncBountyClientRpc(int idx, int tmplIdx, int current, int target, bool done, bool rewarded, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
        }

        [ClientRpc]
        private void SyncDoneClientRpc(int completedCount, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            OnBountiesUpdated?.Invoke();
        }

        [ClientRpc]
        private void BountyRewardClientRpc(int idx, long gold, long exp, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification("Bounty complete! +" + gold + "G, +" + exp + " EXP", NotificationType.System);
            OnBountiesUpdated?.Invoke();
        }

        [ClientRpc]
        private void AllBountiesCompleteClientRpc(long gold, long exp, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification("All bounties complete! Bonus: +" + gold + "G, +" + exp + " EXP", NotificationType.System);
        }

        #endregion

        #region Utility

        private BountyBoard GetOrCreateBoard(ulong clientId)
        {
            if (!playerBounties.ContainsKey(clientId))
                playerBounties[clientId] = new BountyBoard();
            return playerBounties[clientId];
        }

        private int GetCurrentDay()
        {
            return (int)(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 86400);
        }

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        #endregion

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }

    public enum BountyType { KillMonster, KillElite, KillBoss, ClearZone }

    public class BountyTemplate
    {
        public string name;
        public BountyType type;
        public string targetId;
        public int targetCount;
        public long goldReward;
        public long expReward;
        public int difficulty;

        public BountyTemplate(string name, BountyType type, string targetId, int count, long gold, long exp, int diff)
        {
            this.name = name; this.type = type; this.targetId = targetId;
            this.targetCount = count; this.goldReward = gold; this.expReward = exp; this.difficulty = diff;
        }
    }

    public class ActiveBounty
    {
        public int templateIndex;
        public int currentCount;
        public int targetCount;
        public bool completed;
        public bool rewarded;
    }

    public class BountyBoard
    {
        public List<ActiveBounty> activeBounties = new List<ActiveBounty>();
        public int lastRefreshDay = 0;
        public int completedToday = 0;
    }
}
