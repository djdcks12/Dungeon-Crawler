using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class SeasonMechanicSystem : NetworkBehaviour
    {
        public static SeasonMechanicSystem Instance { get; private set; }

        private NetworkVariable<int> mistDensity = new NetworkVariable<int>(0);
        private NetworkVariable<bool> mistBossActive = new NetworkVariable<bool>(false);

        private Dictionary<ulong, int> playerMistOrbs = new Dictionary<ulong, int>();
        private Dictionary<ulong, int> playerMistTokens = new Dictionary<ulong, int>();

        public System.Action OnMistUpdated;

        private static readonly MistReward[] mistShopItems = new MistReward[]
        {
            new MistReward("Mist Weapon Box", 50, "Random Mist weapon (Rare+)"),
            new MistReward("Mist Armor Box", 40, "Random Mist armor (Rare+)"),
            new MistReward("Mist Gem Pouch", 30, "3 random gems"),
            new MistReward("Mist Mutation Stone", 25, "Random mutation stone"),
            new MistReward("Mist Burn Amulet", 20, "Infernal Hordes entry"),
            new MistReward("Mist EXP Tome", 15, "5000 EXP"),
            new MistReward("Mist Gold Purse", 10, "3000 Gold"),
            new MistReward("Mist Rift Key", 60, "Rift challenge key"),
            new MistReward("Mist Waystone", 80, "Rare waystone"),
            new MistReward("Mist Crown", 200, "Season-exclusive cosmetic title"),
        };

        private static readonly int[] densityThresholds = { 0, 20, 40, 60, 80, 100 };
        private static readonly float[] monsterHPScale = { 1.0f, 1.2f, 1.5f, 2.0f, 2.5f, 3.0f };
        private static readonly float[] rewardScale = { 1.0f, 1.3f, 1.6f, 2.0f, 2.5f, 3.5f };

        public override void OnNetworkSpawn()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (IsClient)
            {
                mistDensity.OnValueChanged += OnMistDensityChanged;
                mistBossActive.OnValueChanged += OnMistBossActiveChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsClient)
            {
                mistDensity.OnValueChanged -= OnMistDensityChanged;
                mistBossActive.OnValueChanged -= OnMistBossActiveChanged;
            }
            if (Instance == this)
            {
                OnMistUpdated = null;
                Instance = null;
            }
            base.OnNetworkDespawn();
        }

        private void OnMistDensityChanged(int prev, int next) => OnMistUpdated?.Invoke();
        private void OnMistBossActiveChanged(bool prev, bool next) => OnMistUpdated?.Invoke();

        [ServerRpc(RequireOwnership = false)]
        public void DepositMistOrbsServerRpc(int amount, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!playerMistOrbs.ContainsKey(clientId)) playerMistOrbs[clientId] = 0;
            if (playerMistOrbs[clientId] < amount)
            {
                NotifyClientRpc("Not enough Mist Orbs", clientId);
                return;
            }

            playerMistOrbs[clientId] -= amount;
            mistDensity.Value = Mathf.Min(100, mistDensity.Value + amount);

            OrbsDepositedClientRpc(amount, mistDensity.Value, clientId);

            if (mistDensity.Value >= 100 && !mistBossActive.Value)
            {
                SpawnMistBoss();
            }
        }

        private void SpawnMistBoss()
        {
            mistBossActive.Value = true;
            MistBossSpawnedClientRpc();
        }

        public void OnMistBossDefeated()
        {
            if (!IsServer) return;
            mistBossActive.Value = false;
            mistDensity.Value = 0;

            foreach (var kvp in playerMistOrbs)
            {
                if (!playerMistTokens.ContainsKey(kvp.Key))
                    playerMistTokens[kvp.Key] = 0;
                playerMistTokens[kvp.Key] += 50;
                MistBossRewardClientRpc(50, kvp.Key);
            }
        }

        public void AddMistOrbs(ulong clientId, int amount)
        {
            if (!IsServer) return;
            if (!playerMistOrbs.ContainsKey(clientId)) playerMistOrbs[clientId] = 0;
            playerMistOrbs[clientId] += amount;
            OrbsReceivedClientRpc(amount, playerMistOrbs[clientId], clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PurchaseMistRewardServerRpc(int shopIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (shopIndex < 0 || shopIndex >= mistShopItems.Length) return;

            var item = mistShopItems[shopIndex];
            if (!playerMistTokens.ContainsKey(clientId)) playerMistTokens[clientId] = 0;

            if (playerMistTokens[clientId] < item.tokenCost)
            {
                NotifyClientRpc("Not enough Mist Tokens (" + playerMistTokens[clientId] + "/" + item.tokenCost + ")", clientId);
                return;
            }

            playerMistTokens[clientId] -= item.tokenCost;

            if (item.name == "Mist EXP Tome")
            {
                var stats = GetPlayerStatsData(clientId);
                if (stats != null) stats.AddExperience(5000);
            }
            else if (item.name == "Mist Gold Purse")
            {
                var stats = GetPlayerStatsData(clientId);
                if (stats != null) stats.ChangeGold(3000);
            }
            else if (item.name == "Mist Burn Amulet" && InfernalHordesSystem.Instance != null)
            {
                InfernalHordesSystem.Instance.AddBurnAmulet(clientId, 1);
            }

            PurchaseCompleteClientRpc(shopIndex, item.name, playerMistTokens[clientId], clientId);
        }

        public int GetDensityLevel()
        {
            for (int i = densityThresholds.Length - 1; i >= 0; i--)
            {
                if (mistDensity.Value >= densityThresholds[i])
                    return i;
            }
            return 0;
        }

        public float GetCurrentMonsterHPScale() => monsterHPScale[GetDensityLevel()];
        public float GetCurrentRewardScale() => rewardScale[GetDensityLevel()];
        public int GetMistDensity() => mistDensity.Value;
        public bool IsMistBossActive() => mistBossActive.Value;

        public static MistReward[] GetShopItems() => mistShopItems;

        #region ClientRPCs

        [ClientRpc]
        private void OrbsDepositedClientRpc(int amount, int newDensity, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification("Deposited " + amount + " Mist Orbs (Density: " + newDensity + "%)", NotificationType.System);
            OnMistUpdated?.Invoke();
        }

        [ClientRpc]
        private void OrbsReceivedClientRpc(int amount, int total, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification("Mist Orb +" + amount + " (Total: " + total + ")", NotificationType.System);
        }

        [ClientRpc]
        private void MistBossSpawnedClientRpc()
        {
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification("The Mist Boss has appeared! Density at 100%!", NotificationType.Warning);
            OnMistUpdated?.Invoke();
        }

        [ClientRpc]
        private void MistBossRewardClientRpc(int tokens, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification("Mist Boss defeated! +" + tokens + " Mist Tokens", NotificationType.System);
        }

        [ClientRpc]
        private void PurchaseCompleteClientRpc(int idx, string itemName, int remaining, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification("Purchased: " + itemName + " (Tokens: " + remaining + ")", NotificationType.System);
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
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }

    public class MistReward
    {
        public string name;
        public int tokenCost;
        public string description;

        public MistReward(string name, int cost, string desc)
        {
            this.name = name; this.tokenCost = cost; this.description = desc;
        }
    }
}
