using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class RiftChallengeSystem : NetworkBehaviour
    {
        public static RiftChallengeSystem Instance { get; private set; }

        [Header("Rift Settings")]
        [SerializeField] private float riftDuration = 900f;
        [SerializeField] private float hpScalePerLevel = 0.08f;
        [SerializeField] private float dmgScalePerLevel = 0.08f;
        [SerializeField] private int killsForBoss = 150;
        [SerializeField] private float bossHPMultiplier = 5f;

        private NetworkVariable<int> riftLevel = new NetworkVariable<int>(0);
        private NetworkVariable<float> timeRemaining = new NetworkVariable<float>(0f);
        private NetworkVariable<int> killCount = new NetworkVariable<int>(0);
        private NetworkVariable<bool> isRiftActive = new NetworkVariable<bool>(false);
        private NetworkVariable<bool> bossSpawned = new NetworkVariable<bool>(false);

        private Dictionary<ulong, int> highestRiftCleared = new Dictionary<ulong, int>();
        private Dictionary<ulong, int> riftKeys = new Dictionary<ulong, int>();

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

        private void Update()
        {
            if (!IsServer || !isRiftActive.Value) return;

            timeRemaining.Value -= Time.deltaTime;
            if (timeRemaining.Value <= 0f)
            {
                FailRift();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void StartRiftServerRpc(int level, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (isRiftActive.Value) return;
            if (level < 1) level = 1;

            if (!riftKeys.ContainsKey(clientId)) riftKeys[clientId] = 1;

            if (level > 1)
            {
                if (!highestRiftCleared.ContainsKey(clientId)) highestRiftCleared[clientId] = 0;
                if (level > highestRiftCleared[clientId] + 1) return;
            }

            if (riftKeys[clientId] <= 0)
            {
                NotifyRiftFailClientRpc("No rift keys available!", clientId);
                return;
            }

            riftKeys[clientId]--;
            riftLevel.Value = level;
            timeRemaining.Value = riftDuration;
            killCount.Value = 0;
            bossSpawned.Value = false;
            isRiftActive.Value = true;

            NotifyRiftStartClientRpc(level);
        }

        public void OnMonsterKilled()
        {
            if (!IsServer || !isRiftActive.Value) return;

            killCount.Value++;

            if (!bossSpawned.Value && killCount.Value >= killsForBoss)
            {
                bossSpawned.Value = true;
                NotifyBossSpawnedClientRpc();
            }
        }

        public void OnBossKilled()
        {
            if (!IsServer || !isRiftActive.Value || !bossSpawned.Value) return;
            CompleteRift();
        }

        private void CompleteRift()
        {
            isRiftActive.Value = false;
            int level = riftLevel.Value;
            float timeUsed = riftDuration - timeRemaining.Value;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                ulong cid = client.ClientId;
                if (!highestRiftCleared.ContainsKey(cid)) highestRiftCleared[cid] = 0;
                if (level > highestRiftCleared[cid]) highestRiftCleared[cid] = level;

                if (!riftKeys.ContainsKey(cid)) riftKeys[cid] = 0;
                riftKeys[cid]++;

                var playerObj = client.PlayerObject;
                if (playerObj != null)
                {
                    var stats = playerObj.GetComponent<PlayerStatsManager>()?.CurrentStats;
                    if (stats != null)
                    {
                        long goldReward = 500L * level * level;
                        long expReward = 200L * level * level;
                        stats.ChangeGold(goldReward);
                        stats.AddExperience(expReward);
                    }
                }
            }

            NotifyRiftCompleteClientRpc(level, timeUsed);
        }

        private void FailRift()
        {
            isRiftActive.Value = false;
            NotifyRiftFailedClientRpc(riftLevel.Value);
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddRiftKeyServerRpc(int count, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!riftKeys.ContainsKey(clientId)) riftKeys[clientId] = 0;
            riftKeys[clientId] += count;
        }

        public float GetMonsterHPScale()
        {
            return 1f + (riftLevel.Value * hpScalePerLevel);
        }

        public float GetMonsterDamageScale()
        {
            return 1f + (riftLevel.Value * dmgScalePerLevel);
        }

        public float GetBossHPScale()
        {
            return GetMonsterHPScale() * bossHPMultiplier;
        }

        public int GetHighestCleared(ulong clientId)
        {
            if (!highestRiftCleared.ContainsKey(clientId)) return 0;
            return highestRiftCleared[clientId];
        }

        public int GetKeyCount(ulong clientId)
        {
            if (!riftKeys.ContainsKey(clientId)) return 0;
            return riftKeys[clientId];
        }

        public bool IsActive => isRiftActive.Value;
        public int CurrentLevel => riftLevel.Value;
        public float TimeRemaining => timeRemaining.Value;
        public int KillCount => killCount.Value;
        public int KillsRequired => killsForBoss;
        public bool BossSpawned => bossSpawned.Value;
        public float Progress => killsForBoss > 0 ? (float)killCount.Value / killsForBoss : 0f;

        [ClientRpc]
        private void NotifyRiftStartClientRpc(int level)
        {
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(
                    "Rift Level " + level + " opened!", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyBossSpawnedClientRpc()
        {
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(
                    "Rift Guardian has appeared!", NotificationType.Warning);
        }

        [ClientRpc]
        private void NotifyRiftCompleteClientRpc(int level, float timeUsed)
        {
            if (NotificationManager.Instance != null)
            {
                int minutes = (int)(timeUsed / 60f);
                int seconds = (int)(timeUsed % 60f);
                NotificationManager.Instance.ShowNotification(
                    "Rift Level " + level + " cleared in " + minutes + "m " + seconds + "s!", NotificationType.System);
            }
        }

        [ClientRpc]
        private void NotifyRiftFailedClientRpc(int level)
        {
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(
                    "Rift Level " + level + " failed! Time expired.", NotificationType.Warning);
        }

        [ClientRpc]
        private void NotifyRiftFailClientRpc(string message, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(message, NotificationType.Warning);
        }
    }
}
