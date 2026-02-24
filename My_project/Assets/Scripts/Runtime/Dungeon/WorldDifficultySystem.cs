using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class WorldDifficultySystem : NetworkBehaviour
    {
        public static WorldDifficultySystem Instance { get; private set; }

        private NetworkVariable<int> currentTier = new NetworkVariable<int>(0);
        private Dictionary<ulong, int> playerUnlockedTiers = new Dictionary<ulong, int>();

        private static readonly DifficultyTier[] tiers = new DifficultyTier[]
        {
            new DifficultyTier("Normal", 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0),
            new DifficultyTier("Enhanced", 1.5f, 1.3f, 1.4f, 1.3f, 1.5f, 5),
            new DifficultyTier("Elite", 2.2f, 1.8f, 2.0f, 1.7f, 2.5f, 8),
            new DifficultyTier("Inferno", 3.5f, 2.5f, 3.0f, 2.2f, 4.0f, 11),
            new DifficultyTier("Doom", 5.0f, 4.0f, 5.0f, 3.0f, 7.0f, 14)
        };

        public override void OnNetworkSpawn()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this) Instance = null;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this)
                Instance = null;
        }

        public DifficultyTier GetCurrentTier()
        {
            int idx = Mathf.Clamp(currentTier.Value, 0, tiers.Length - 1);
            return tiers[idx];
        }

        public int CurrentTierIndex => currentTier.Value;
        public string CurrentTierName => GetCurrentTier().tierName;
        public float MonsterHPMultiplier => GetCurrentTier().monsterHPMult;
        public float MonsterDamageMultiplier => GetCurrentTier().monsterDmgMult;
        public float DropRateMultiplier => GetCurrentTier().dropRateMult;
        public float ExpMultiplier => GetCurrentTier().expMult;
        public float GoldMultiplier => GetCurrentTier().goldMult;

        [ServerRpc(RequireOwnership = false)]
        public void SetDifficultyServerRpc(int tierIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!IsServer) return;
            if (tierIndex < 0 || tierIndex >= tiers.Length) return;

            if (!playerUnlockedTiers.ContainsKey(clientId))
                playerUnlockedTiers[clientId] = 0;

            if (tierIndex > playerUnlockedTiers[clientId])
            {
                NotifyDifficultyFailClientRpc(clientId);
                return;
            }

            currentTier.Value = tierIndex;
            NotifyDifficultyChangedClientRpc(tierIndex);
        }

        [ServerRpc(RequireOwnership = false)]
        public void UnlockNextTierServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!playerUnlockedTiers.ContainsKey(clientId))
                playerUnlockedTiers[clientId] = 0;

            int current = playerUnlockedTiers[clientId];
            if (current >= tiers.Length - 1) return;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var diffClient)) return;
            var playerObj = diffClient.PlayerObject;
            if (playerObj == null) return;
            var stats = playerObj.GetComponent<PlayerStatsManager>()?.CurrentStats;
            if (stats == null) return;

            int requiredLevel = tiers[current + 1].requiredLevel;
            if (stats.CurrentLevel < requiredLevel) return;

            playerUnlockedTiers[clientId] = current + 1;
            NotifyTierUnlockedClientRpc(current + 1, clientId);
        }

        public int GetUnlockedTier(ulong clientId)
        {
            if (!playerUnlockedTiers.ContainsKey(clientId)) return 0;
            return playerUnlockedTiers[clientId];
        }

        public DifficultyTier[] GetAllTiers() => tiers;
        public int TierCount => tiers.Length;

        [ClientRpc]
        private void NotifyDifficultyChangedClientRpc(int tierIndex)
        {
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(
                    "Difficulty changed to: " + tiers[tierIndex].tierName, NotificationType.System);
        }

        [ClientRpc]
        private void NotifyDifficultyFailClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(
                    "Cannot set this difficulty! Unlock it first by reaching the required level.", NotificationType.Warning);
        }

        [ClientRpc]
        private void NotifyTierUnlockedClientRpc(int tierIndex, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(
                    "Difficulty tier unlocked: " + tiers[tierIndex].tierName + "!", NotificationType.System);
        }
    }

    [System.Serializable]
    public class DifficultyTier
    {
        public string tierName;
        public float monsterHPMult;
        public float monsterDmgMult;
        public float dropRateMult;
        public float expMult;
        public float goldMult;
        public int requiredLevel;

        public DifficultyTier(string name, float hp, float dmg, float drop, float exp, float gold, int reqLvl)
        {
            tierName = name;
            monsterHPMult = hp;
            monsterDmgMult = dmg;
            dropRateMult = drop;
            expMult = exp;
            goldMult = gold;
            requiredLevel = reqLvl;
        }
    }
}
