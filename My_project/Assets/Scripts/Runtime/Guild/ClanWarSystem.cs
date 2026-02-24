using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class ClanWarSystem : NetworkBehaviour
    {
        public static ClanWarSystem Instance { get; private set; }

        private NetworkVariable<int> currentSeason = new NetworkVariable<int>(1);
        private NetworkVariable<int> warPhase = new NetworkVariable<int>(0);

        private Dictionary<string, ClanWarData> clanWarStats = new Dictionary<string, ClanWarData>();
        private Dictionary<int, TerritoryData> territories = new Dictionary<int, TerritoryData>();

        public System.Action OnWarStateChanged;
        public System.Action<int> OnTerritoryChanged;

        private static readonly TerritoryTemplate[] territoryTemplates = new TerritoryTemplate[]
        {
            new TerritoryTemplate(0, "Dragon Peak", TerritoryBonus.ExpBonus, 0.05f, "EXP +5% for guild members"),
            new TerritoryTemplate(1, "Crystal Mine", TerritoryBonus.GoldBonus, 0.10f, "Gold +10% for guild members"),
            new TerritoryTemplate(2, "Ancient Grove", TerritoryBonus.DropBonus, 0.05f, "Drop rate +5% for guild members"),
        };

        private float warCycleTimer = 0f;
        private float preparationDuration = 432000f;
        private float battleDuration = 172800f;

        public override void OnNetworkSpawn()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (IsServer)
            {
                foreach (var tmpl in territoryTemplates)
                {
                    territories[tmpl.id] = new TerritoryData
                    {
                        templateId = tmpl.id,
                        ownerGuildId = "",
                        controlPoints = 0
                    };
                }
            }

            if (IsClient)
            {
                warPhase.OnValueChanged += OnWarPhaseChanged;
                currentSeason.OnValueChanged += OnCurrentSeasonChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsClient)
            {
                warPhase.OnValueChanged -= OnWarPhaseChanged;
                currentSeason.OnValueChanged -= OnCurrentSeasonChanged;
            }
            if (Instance == this)
            {
                OnWarStateChanged = null;
                OnTerritoryChanged = null;
                Instance = null;
            }
            base.OnNetworkDespawn();
        }

        private void OnWarPhaseChanged(int prev, int next) => OnWarStateChanged?.Invoke();
        private void OnCurrentSeasonChanged(int prev, int next) => OnWarStateChanged?.Invoke();

        private void Update()
        {
            if (!IsServer) return;

            warCycleTimer += Time.deltaTime;

            if (warPhase.Value == 0 && warCycleTimer >= preparationDuration)
            {
                warPhase.Value = 1;
                warCycleTimer = 0f;
                BattleStartedClientRpc();
            }
            else if (warPhase.Value == 1 && warCycleTimer >= battleDuration)
            {
                EndBattle();
                warPhase.Value = 0;
                warCycleTimer = 0f;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ContributeToTerritoryServerRpc(int territoryId, int points, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (warPhase.Value != 1) return;
            if (!territories.ContainsKey(territoryId)) return;

            var guildSystem = GuildSystem.Instance;
            if (guildSystem == null) return;

            string guildId = guildSystem.GetPlayerGuildId(clientId);
            if (string.IsNullOrEmpty(guildId))
            {
                NotifyClientRpc("You must be in a guild to participate", clientId);
                return;
            }

            var territory = territories[territoryId];

            if (!clanWarStats.ContainsKey(guildId))
                clanWarStats[guildId] = new ClanWarData();

            var warData = clanWarStats[guildId];
            if (!warData.territoryContributions.ContainsKey(territoryId))
                warData.territoryContributions[territoryId] = 0;
            warData.territoryContributions[territoryId] += points;
            warData.totalContribution += points;

            if (!warData.memberContributions.ContainsKey(clientId))
                warData.memberContributions[clientId] = 0;
            warData.memberContributions[clientId] += points;

            TerritoryUpdateClientRpc(territoryId, guildId, warData.territoryContributions[territoryId]);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RegisterClanForWarServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var guildSystem = GuildSystem.Instance;
            if (guildSystem == null) return;

            string guildId = guildSystem.GetPlayerGuildId(clientId);
            if (string.IsNullOrEmpty(guildId))
            {
                NotifyClientRpc("You must be in a guild", clientId);
                return;
            }

            if (!clanWarStats.ContainsKey(guildId))
            {
                clanWarStats[guildId] = new ClanWarData();
                NotifyClientRpc("Guild registered for clan war!", clientId);
            }
        }

        private void EndBattle()
        {
            if (!IsServer) return;

            foreach (var tmpl in territoryTemplates)
            {
                int territoryId = tmpl.id;
                string winnerGuildId = "";
                int highestPoints = 0;

                foreach (var kvp in clanWarStats)
                {
                    if (kvp.Value.territoryContributions.TryGetValue(territoryId, out int pts))
                    {
                        if (pts > highestPoints)
                        {
                            highestPoints = pts;
                            winnerGuildId = kvp.Key;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(winnerGuildId))
                {
                    territories[territoryId].ownerGuildId = winnerGuildId;
                    territories[territoryId].controlPoints = highestPoints;
                    TerritoryConqueredClientRpc(territoryId, winnerGuildId, tmpl.name);
                }
            }

            DistributeRewards();

            foreach (var kvp in clanWarStats)
            {
                kvp.Value.territoryContributions.Clear();
                kvp.Value.memberContributions.Clear();
                kvp.Value.totalContribution = 0;
            }

            currentSeason.Value++;
        }

        private void DistributeRewards()
        {
            foreach (var kvp in clanWarStats)
            {
                string guildId = kvp.Key;
                var warData = kvp.Value;

                int territoriesOwned = 0;
                foreach (var territory in territories.Values)
                {
                    if (territory.ownerGuildId == guildId)
                        territoriesOwned++;
                }

                long baseGold = 10000L * territoriesOwned;
                long baseExp = 5000L * territoriesOwned;

                foreach (var memberKvp in warData.memberContributions)
                {
                    float contributionRatio = warData.totalContribution > 0
                        ? (float)memberKvp.Value / warData.totalContribution
                        : 0f;

                    long memberGold = (long)(baseGold * Mathf.Max(0.1f, contributionRatio));
                    long memberExp = (long)(baseExp * Mathf.Max(0.1f, contributionRatio));

                    var stats = GetPlayerStatsData(memberKvp.Key);
                    if (stats != null)
                    {
                        stats.ChangeGold(memberGold);
                        stats.AddExperience(memberExp);
                    }

                    WarRewardClientRpc(memberGold, memberExp, territoriesOwned, memberKvp.Key);
                }
            }
        }

        public float GetTerritoryBonus(ulong clientId, TerritoryBonus bonusType)
        {
            if (!IsServer) return 0f;
            var guildSystem = GuildSystem.Instance;
            if (guildSystem == null) return 0f;

            string guildId = guildSystem.GetPlayerGuildId(clientId);
            if (string.IsNullOrEmpty(guildId)) return 0f;

            float totalBonus = 0f;
            foreach (var tmpl in territoryTemplates)
            {
                if (tmpl.bonusType == bonusType && territories.TryGetValue(tmpl.id, out var territory))
                {
                    if (territory.ownerGuildId == guildId)
                        totalBonus += tmpl.bonusValue;
                }
            }
            return totalBonus;
        }

        public int GetWarPhase() => warPhase.Value;
        public int GetCurrentSeason() => currentSeason.Value;

        public string GetTerritoryOwner(int territoryId)
        {
            return territories.TryGetValue(territoryId, out var t) ? t.ownerGuildId : "";
        }

        public static TerritoryTemplate[] GetTerritoryTemplates() => territoryTemplates;

        #region ClientRPCs

        [ClientRpc]
        private void BattleStartedClientRpc()
        {
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification("Clan War battle phase has begun!", NotificationType.Warning);
            OnWarStateChanged?.Invoke();
        }

        [ClientRpc]
        private void TerritoryUpdateClientRpc(int territoryId, string guildId, int points)
        {
            OnTerritoryChanged?.Invoke(territoryId);
        }

        [ClientRpc]
        private void TerritoryConqueredClientRpc(int territoryId, string guildId, string territoryName)
        {
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification(guildId + " conquered " + territoryName + "!", NotificationType.System);
            OnTerritoryChanged?.Invoke(territoryId);
        }

        [ClientRpc]
        private void WarRewardClientRpc(long gold, long exp, int territories, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification("War Reward: " + gold + "G, " + exp + " EXP (" + territories + " territories)", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyClientRpc(string message, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification(message, NotificationType.Warning);
        }

        #endregion

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }

    public enum TerritoryBonus
    {
        ExpBonus,
        GoldBonus,
        DropBonus
    }

    public class TerritoryTemplate
    {
        public int id;
        public string name;
        public TerritoryBonus bonusType;
        public float bonusValue;
        public string description;

        public TerritoryTemplate(int id, string name, TerritoryBonus bonusType, float bonusValue, string desc)
        {
            this.id = id; this.name = name; this.bonusType = bonusType;
            this.bonusValue = bonusValue; this.description = desc;
        }
    }

    public class TerritoryData
    {
        public int templateId;
        public string ownerGuildId;
        public int controlPoints;
    }

    public class ClanWarData
    {
        public Dictionary<int, int> territoryContributions = new Dictionary<int, int>();
        public Dictionary<ulong, int> memberContributions = new Dictionary<ulong, int>();
        public int totalContribution = 0;
    }
}
