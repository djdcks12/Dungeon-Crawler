using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class PinnacleBossSystem : NetworkBehaviour
    {
        public static PinnacleBossSystem Instance { get; private set; }

        private NetworkVariable<int> activeBossIndex = new NetworkVariable<int>(-1);
        private NetworkVariable<int> currentPhase = new NetworkVariable<int>(0);
        private NetworkVariable<float> bossHP = new NetworkVariable<float>(0f);
        private NetworkVariable<float> bossMaxHP = new NetworkVariable<float>(0f);
        private NetworkVariable<bool> isBossActive = new NetworkVariable<bool>(false);

        private Dictionary<ulong, PinnacleProgress> playerProgress = new Dictionary<ulong, PinnacleProgress>();
        private Dictionary<ulong, float> damageContributions = new Dictionary<ulong, float>();

        public System.Action OnBossStateChanged;
        public System.Action<int> OnPhaseChanged;

        private static readonly PinnacleBossData[] pinnacleBosses = new PinnacleBossData[]
        {
            new PinnacleBossData("The Shaper", 500000f, 3, 50,
                new string[] { "Beam Barrage", "Slam Nova", "Clone Summon" },
                new float[] { 0.75f, 0.50f, 0.25f },
                5000, 50000, "shaper_aspect"),
            new PinnacleBossData("The Elder", 600000f, 3, 55,
                new string[] { "Tentacle Slam", "Portal Storm", "Decay Pulse" },
                new float[] { 0.70f, 0.45f, 0.20f },
                6000, 60000, "elder_aspect"),
            new PinnacleBossData("Sirus, Awakener", 750000f, 3, 60,
                new string[] { "Die Beam", "Meteor Maze", "Clone Assault" },
                new float[] { 0.65f, 0.40f, 0.15f },
                8000, 80000, "sirus_aspect"),
            new PinnacleBossData("The Maven", 900000f, 3, 65,
                new string[] { "Memory Game", "Cascade Beam", "Gravity Well" },
                new float[] { 0.60f, 0.35f, 0.10f },
                10000, 100000, "maven_aspect"),
            new PinnacleBossData("Uber Lilith", 1200000f, 3, 70,
                new string[] { "Blood Wave", "Shadow Clone", "World Ender" },
                new float[] { 0.55f, 0.30f, 0.05f },
                15000, 150000, "lilith_aspect"),
        };

        private float phaseTimer = 0f;
        private float enrageTimer = 0f;
        private float enrageTimeLimit = 600f;

        public override void OnNetworkSpawn()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (IsClient)
            {
                activeBossIndex.OnValueChanged += OnActiveBossIndexChanged;
                currentPhase.OnValueChanged += OnCurrentPhaseChanged;
                isBossActive.OnValueChanged += OnIsBossActiveChanged;
                bossHP.OnValueChanged += OnBossHPChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsClient)
            {
                activeBossIndex.OnValueChanged -= OnActiveBossIndexChanged;
                currentPhase.OnValueChanged -= OnCurrentPhaseChanged;
                isBossActive.OnValueChanged -= OnIsBossActiveChanged;
                bossHP.OnValueChanged -= OnBossHPChanged;
            }
            if (Instance == this)
            {
                OnBossStateChanged = null;
                OnPhaseChanged = null;
                Instance = null;
            }
            base.OnNetworkDespawn();
        }

        private void OnActiveBossIndexChanged(int prev, int next) => OnBossStateChanged?.Invoke();
        private void OnCurrentPhaseChanged(int prev, int next) => OnPhaseChanged?.Invoke(next);
        private void OnIsBossActiveChanged(bool prev, bool next) => OnBossStateChanged?.Invoke();
        private void OnBossHPChanged(float prev, float next) => OnBossStateChanged?.Invoke();

        private void Update()
        {
            if (!IsServer || !isBossActive.Value) return;

            enrageTimer += Time.deltaTime;
            if (enrageTimer >= enrageTimeLimit)
            {
                WipePlayers();
                return;
            }

            float hpPercent = bossMaxHP.Value > 0f ? bossHP.Value / bossMaxHP.Value : 1f;
            int bossIdx = activeBossIndex.Value;
            if (bossIdx < 0 || bossIdx >= pinnacleBosses.Length) return;

            var boss = pinnacleBosses[bossIdx];
            int expectedPhase = 0;
            for (int i = 0; i < boss.phaseThresholds.Length; i++)
            {
                if (hpPercent <= boss.phaseThresholds[i])
                    expectedPhase = i + 1;
            }

            if (expectedPhase != currentPhase.Value)
            {
                currentPhase.Value = expectedPhase;
                PhaseTransitionClientRpc(expectedPhase, boss.phaseAbilities[Mathf.Min(expectedPhase, boss.phaseAbilities.Length - 1)]);
            }

            if (bossHP.Value <= 0f)
            {
                CompleteBoss();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void StartPinnacleBossServerRpc(int bossIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (isBossActive.Value) return;
            if (bossIndex < 0 || bossIndex >= pinnacleBosses.Length) return;

            var boss = pinnacleBosses[bossIndex];

            var progress = GetOrCreateProgress(clientId);
            if (progress.weeklyClears.ContainsKey(bossIndex) && progress.weeklyClears[bossIndex] >= 1)
            {
                NotifyClientRpc("Weekly clear limit reached for this boss", clientId);
                return;
            }

            if (progress.highestRiftCleared < boss.requiredRiftLevel)
            {
                NotifyClientRpc("Rift level " + boss.requiredRiftLevel + " required", clientId);
                return;
            }

            activeBossIndex.Value = bossIndex;
            bossMaxHP.Value = boss.maxHP;
            bossHP.Value = boss.maxHP;
            currentPhase.Value = 0;
            isBossActive.Value = true;
            enrageTimer = 0f;
            damageContributions.Clear();

            BossStartedClientRpc(bossIndex, boss.name);
        }

        public void DealDamageToBoss(ulong clientId, float damage)
        {
            if (!IsServer || !isBossActive.Value) return;

            if (!damageContributions.ContainsKey(clientId))
                damageContributions[clientId] = 0f;
            damageContributions[clientId] += damage;

            bossHP.Value = Mathf.Max(0f, bossHP.Value - damage);
        }

        private void CompleteBoss()
        {
            if (!IsServer) return;

            int bossIdx = activeBossIndex.Value;
            var boss = pinnacleBosses[bossIdx];

            foreach (var kvp in damageContributions)
            {
                var progress = GetOrCreateProgress(kvp.Key);
                if (!progress.weeklyClears.ContainsKey(bossIdx))
                    progress.weeklyClears[bossIdx] = 0;
                progress.weeklyClears[bossIdx]++;

                float contribution = bossMaxHP.Value > 0f ? kvp.Value / bossMaxHP.Value : 0f;
                long expReward = (long)(boss.expReward * Mathf.Max(0.1f, contribution));
                long goldReward = (long)(boss.goldReward * Mathf.Max(0.1f, contribution));

                var stats = GetPlayerStatsData(kvp.Key);
                if (stats != null)
                {
                    stats.AddExperience(expReward);
                    stats.ChangeGold(goldReward);
                }

                if (contribution >= 0.1f && LegendaryAspectSystem.Instance != null)
                {
                    BossRewardClientRpc(bossIdx, expReward, goldReward, boss.uniqueAspectId, kvp.Key);
                }
                else
                {
                    BossRewardClientRpc(bossIdx, expReward, goldReward, "", kvp.Key);
                }
            }

            isBossActive.Value = false;
            activeBossIndex.Value = -1;
            BossDefeatedClientRpc(bossIdx, boss.name);
        }

        private void WipePlayers()
        {
            isBossActive.Value = false;
            int bossIdx = activeBossIndex.Value;
            activeBossIndex.Value = -1;
            damageContributions.Clear();

            string bossName = bossIdx >= 0 && bossIdx < pinnacleBosses.Length ? pinnacleBosses[bossIdx].name : "Unknown";
            BossWipeClientRpc(bossName);
        }

        public void SetPlayerRiftLevel(ulong clientId, int riftLevel)
        {
            if (!IsServer) return;
            var progress = GetOrCreateProgress(clientId);
            if (riftLevel > progress.highestRiftCleared)
                progress.highestRiftCleared = riftLevel;
        }

        public float GetBossHPPercent()
        {
            if (bossMaxHP.Value <= 0f) return 0f;
            return bossHP.Value / bossMaxHP.Value;
        }

        public int GetCurrentPhase() => currentPhase.Value;
        public bool IsBossActive() => isBossActive.Value;
        public int GetActiveBossIndex() => activeBossIndex.Value;
        public float GetEnragePercent() => enrageTimeLimit > 0f ? enrageTimer / enrageTimeLimit : 0f;

        public static PinnacleBossData GetBossData(int index)
        {
            if (index < 0 || index >= pinnacleBosses.Length) return null;
            return pinnacleBosses[index];
        }

        public static int GetBossCount() => pinnacleBosses.Length;

        #region ClientRPCs

        [ClientRpc]
        private void BossStartedClientRpc(int bossIndex, string bossName)
        {
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification("Pinnacle Boss: " + bossName + " engaged!", NotificationType.Warning);
        }

        [ClientRpc]
        private void PhaseTransitionClientRpc(int phase, string abilityName)
        {
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification("Phase " + (phase + 1) + " - " + abilityName, NotificationType.Warning);
        }

        [ClientRpc]
        private void BossDefeatedClientRpc(int bossIndex, string bossName)
        {
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification(bossName + " defeated!", NotificationType.System);
            OnBossStateChanged?.Invoke();
        }

        [ClientRpc]
        private void BossWipeClientRpc(string bossName)
        {
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification(bossName + " - Enrage! Party wiped.", NotificationType.Warning);
            OnBossStateChanged?.Invoke();
        }

        [ClientRpc]
        private void BossRewardClientRpc(int bossIndex, long exp, long gold, string aspectId, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
            {
                notif.ShowNotification("Reward: " + exp + " EXP, " + gold + " Gold", NotificationType.System);
                if (aspectId != "")
                    notif.ShowNotification("Unique Aspect unlocked: " + aspectId, NotificationType.System);
            }
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

        private PinnacleProgress GetOrCreateProgress(ulong clientId)
        {
            if (!playerProgress.ContainsKey(clientId))
                playerProgress[clientId] = new PinnacleProgress();
            return playerProgress[clientId];
        }

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

    public class PinnacleBossData
    {
        public string name;
        public float maxHP;
        public int phaseCount;
        public int requiredRiftLevel;
        public string[] phaseAbilities;
        public float[] phaseThresholds;
        public long expReward;
        public long goldReward;
        public string uniqueAspectId;

        public PinnacleBossData(string name, float maxHP, int phaseCount, int riftReq,
            string[] abilities, float[] thresholds, long exp, long gold, string aspectId)
        {
            this.name = name; this.maxHP = maxHP; this.phaseCount = phaseCount;
            this.requiredRiftLevel = riftReq; this.phaseAbilities = abilities;
            this.phaseThresholds = thresholds; this.expReward = exp;
            this.goldReward = gold; this.uniqueAspectId = aspectId;
        }
    }

    public class PinnacleProgress
    {
        public int highestRiftCleared = 0;
        public Dictionary<int, int> weeklyClears = new Dictionary<int, int>();
        public Dictionary<int, bool> firstClears = new Dictionary<int, bool>();
    }
}
