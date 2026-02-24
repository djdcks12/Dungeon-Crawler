using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    [System.Serializable]
    public class ChallengeRiftConfig
    {
        public string riftId;
        public string riftName;
        public string description;
        public string presetJobName;
        public string[] presetSkillIds;
        public string[] presetEquipmentIds;
        public int presetLevel;
        public float timeLimit = 300f;
        public int targetWave;
    }

    [System.Serializable]
    public class ChallengeProgress
    {
        public string riftId;
        public int currentWave;
        public float startTime;
        public float bestTime;
        public int monstersKilled;
        public bool isActive;
    }

    public class ChallengeRiftSystem : NetworkBehaviour
    {
        public static ChallengeRiftSystem Instance { get; private set; }

        public event Action OnChallengeStarted;
        public event Action<int> OnWaveCleared;
        public event Action<int, bool> OnChallengeEnded;

        // Local client state
        public bool localIsInChallenge { get; private set; }
        public int localCurrentWave { get; private set; }
        public float localTimeRemaining { get; private set; }
        public int localBestScore { get; private set; }

        // Server state
        private Dictionary<ulong, ChallengeProgress> activeChallengers = new Dictionary<ulong, ChallengeProgress>();
        private Dictionary<ulong, bool> weeklyCompletionMap = new Dictionary<ulong, bool>();

        private static readonly ChallengeRiftConfig[] PresetConfigs = new ChallengeRiftConfig[]
        {
            new ChallengeRiftConfig
            {
                riftId = "BerserkerTrial",
                riftName = "Berserker Trial",
                description = "Aggressive melee combat challenge. Slash through waves of enemies before time runs out.",
                presetJobName = "Berserker",
                presetSkillIds = new[] { "skill_cleave", "skill_frenzy", "skill_bloodlust", "skill_whirlwind" },
                presetEquipmentIds = new[] { "equip_greatsword_trial", "equip_plate_trial", "equip_ring_fury" },
                presetLevel = 10,
                timeLimit = 300f,
                targetWave = 15
            },
            new ChallengeRiftConfig
            {
                riftId = "MageTrial",
                riftName = "Mage Trial",
                description = "AoE magic build challenge. Decimate hordes with devastating spells.",
                presetJobName = "Mage",
                presetSkillIds = new[] { "skill_fireball", "skill_blizzard", "skill_chain_lightning", "skill_mana_shield" },
                presetEquipmentIds = new[] { "equip_staff_trial", "equip_robe_trial", "equip_amulet_arcane" },
                presetLevel = 10,
                timeLimit = 300f,
                targetWave = 12
            },
            new ChallengeRiftConfig
            {
                riftId = "SniperTrial",
                riftName = "Sniper Trial",
                description = "Precision range build challenge. Pick off targets with lethal accuracy.",
                presetJobName = "Sniper",
                presetSkillIds = new[] { "skill_aimed_shot", "skill_piercing_arrow", "skill_trap", "skill_eagle_eye" },
                presetEquipmentIds = new[] { "equip_longbow_trial", "equip_leather_trial", "equip_quiver_precision" },
                presetLevel = 10,
                timeLimit = 300f,
                targetWave = 10
            },
            new ChallengeRiftConfig
            {
                riftId = "GuardianTrial",
                riftName = "Guardian Trial",
                description = "Survival tank build challenge. Endure relentless waves and stand your ground.",
                presetJobName = "Guardian",
                presetSkillIds = new[] { "skill_shield_wall", "skill_taunt", "skill_holy_guard", "skill_iron_skin" },
                presetEquipmentIds = new[] { "equip_tower_shield_trial", "equip_heavy_plate_trial", "equip_ring_vitality" },
                presetLevel = 10,
                timeLimit = 300f,
                targetWave = 20
            }
        };

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
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        private void Update()
        {
            if (!IsServer) return;

            var expiredClients = new List<ulong>();

            foreach (var kvp in activeChallengers)
            {
                if (!kvp.Value.isActive) continue;

                var config = GetConfigById(kvp.Value.riftId);
                if (config == null) continue;

                float elapsed = Time.time - kvp.Value.startTime;
                if (elapsed >= config.timeLimit)
                {
                    expiredClients.Add(kvp.Key);
                }
            }

            foreach (ulong clientId in expiredClients)
            {
                FailChallenge(clientId, "Time limit exceeded!");
            }
        }

        public int GetCurrentWeekRiftIndex()
        {
            DateTime now = DateTime.UtcNow;
            int dayOfYear = now.DayOfYear;
            int weekNumber = dayOfYear / 7;
            return weekNumber % PresetConfigs.Length;
        }

        public ChallengeRiftConfig GetCurrentRiftConfig()
        {
            return PresetConfigs[GetCurrentWeekRiftIndex()];
        }

        public ChallengeRiftConfig GetRiftConfigByIndex(int index)
        {
            if (index < 0 || index >= PresetConfigs.Length) return null;
            return PresetConfigs[index];
        }

        private ChallengeRiftConfig GetConfigById(string riftId)
        {
            foreach (var config in PresetConfigs)
            {
                if (config.riftId == riftId) return config;
            }
            return null;
        }

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (NetworkManager.Singleton == null) return null;
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) return null;
            var client = NetworkManager.Singleton.ConnectedClients[clientId];
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        // ======================== Server RPCs ========================

        [ServerRpc(RequireOwnership = false)]
        public void StartChallengeRiftServerRpc(int riftIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (activeChallengers.ContainsKey(clientId) && activeChallengers[clientId].isActive)
            {
                NotifyChallengeFailedClientRpc("You are already in a challenge rift.", clientId);
                return;
            }

            if (weeklyCompletionMap.ContainsKey(clientId) && weeklyCompletionMap[clientId])
            {
                NotifyChallengeFailedClientRpc("You have already completed this week's challenge rift.", clientId);
                return;
            }

            var config = GetRiftConfigByIndex(riftIndex);
            if (config == null)
            {
                NotifyChallengeFailedClientRpc("Invalid rift configuration.", clientId);
                return;
            }

            var progress = new ChallengeProgress
            {
                riftId = config.riftId,
                currentWave = 0,
                startTime = Time.time,
                bestTime = 0f,
                monstersKilled = 0,
                isActive = true
            };

            activeChallengers[clientId] = progress;
            Debug.Log($"[ChallengeRift] Client {clientId} started rift: {config.riftName}");
            NotifyChallengeStartedClientRpc(config.riftName, config.timeLimit, clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ReportWaveClearServerRpc(int waveNum, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!activeChallengers.ContainsKey(clientId) || !activeChallengers[clientId].isActive)
            {
                return;
            }

            var progress = activeChallengers[clientId];

            if (waveNum != progress.currentWave + 1)
            {
                Debug.LogWarning($"[ChallengeRift] Client {clientId} reported out-of-order wave {waveNum}, expected {progress.currentWave + 1}");
                return;
            }

            progress.currentWave = waveNum;
            progress.monstersKilled += UnityEngine.Random.Range(5, 15);

            var config = GetConfigById(progress.riftId);
            NotifyWaveClearedClientRpc(waveNum, progress.monstersKilled, clientId);

            if (config != null && waveNum >= config.targetWave)
            {
                CompleteChallenge(clientId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void AbandonChallengeServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!activeChallengers.ContainsKey(clientId) || !activeChallengers[clientId].isActive)
            {
                return;
            }

            activeChallengers[clientId].isActive = false;
            Debug.Log($"[ChallengeRift] Client {clientId} abandoned the challenge.");
            NotifyChallengeFailedClientRpc("Challenge abandoned.", clientId);
        }

        // ======================== Server Logic ========================

        private void CompleteChallenge(ulong clientId)
        {
            if (!activeChallengers.ContainsKey(clientId)) return;

            var progress = activeChallengers[clientId];
            progress.isActive = false;

            var config = GetConfigById(progress.riftId);
            if (config == null) return;

            float elapsed = Time.time - progress.startTime;
            float timeRemaining = Mathf.Max(0f, config.timeLimit - elapsed);
            progress.bestTime = elapsed;

            int score = progress.currentWave * 1000 + Mathf.RoundToInt(timeRemaining * 10f);

            // Base rewards
            int baseGold = 500;
            int baseExp = 300;
            int waveBonus = progress.currentWave * 50;
            int timeBonus = Mathf.RoundToInt(timeRemaining * 2f);
            int totalGold = baseGold + waveBonus + timeBonus;
            int totalExp = baseExp + waveBonus + timeBonus;

            var statsData = GetPlayerStatsData(clientId);
            if (statsData != null)
            {
                statsData.ChangeGold((long)totalGold);
                statsData.AddExperience((long)totalExp);
            }

            // Weekly completion reward - guaranteed rare+ item via mail
            bool isWeeklyFirst = !weeklyCompletionMap.ContainsKey(clientId) || !weeklyCompletionMap[clientId];
            if (isWeeklyFirst)
            {
                weeklyCompletionMap[clientId] = true;
                SendWeeklyRewardMail(clientId, config, score);
            }

            Debug.Log($"[ChallengeRift] Client {clientId} completed {config.riftName}. Score: {score}, Gold: {totalGold}, Exp: {totalExp}");
            NotifyChallengeCompleteClientRpc(progress.currentWave, score, totalGold, totalExp, clientId);
        }

        private void FailChallenge(ulong clientId, string reason)
        {
            if (!activeChallengers.ContainsKey(clientId)) return;

            activeChallengers[clientId].isActive = false;
            Debug.Log($"[ChallengeRift] Client {clientId} failed: {reason}");
            NotifyChallengeFailedClientRpc(reason, clientId);
        }

        private void SendWeeklyRewardMail(ulong clientId, ChallengeRiftConfig config, int score)
        {
            string rewardItemId = "item_rare_rift_chest";
            int bonusGold = 1000;

            var attachment = new MailAttachment
            {
                gold = bonusGold,
                itemId = rewardItemId,
                quantity = 1,
                enhanceLevel = 0
            };

            string subject = $"[Challenge Rift] {config.riftName} Cleared!";
            string body = $"Congratulations! You cleared {config.riftName} with a score of {score}. Here is your weekly reward.";

            MailSystem.Instance?.SendSystemMail(clientId, subject, body, MailType.SystemReward, attachment, true);
        }

        public void ResetWeeklyCompletions()
        {
            weeklyCompletionMap.Clear();
            Debug.Log("[ChallengeRift] Weekly completions have been reset.");
        }

        // ======================== Client RPCs ========================

        [ClientRpc]
        private void NotifyChallengeStartedClientRpc(string riftName, float timeLimit, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            localIsInChallenge = true;
            localCurrentWave = 0;
            localTimeRemaining = timeLimit;

            NotificationManager.Instance?.ShowNotification(
                $"Challenge Rift {riftName} has begun! Time: {timeLimit}s",
                NotificationType.System);

            OnChallengeStarted?.Invoke();
        }

        [ClientRpc]
        private void NotifyWaveClearedClientRpc(int wave, int monstersKilled, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            localCurrentWave = wave;

            NotificationManager.Instance?.ShowNotification(
                $"Wave {wave} cleared! Monsters slain: {monstersKilled}",
                NotificationType.System);

            OnWaveCleared?.Invoke(wave);
        }

        [ClientRpc]
        private void NotifyChallengeCompleteClientRpc(int totalWaves, int score, int goldReward, int expReward, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            localIsInChallenge = false;
            if (score > localBestScore) localBestScore = score;

            NotificationManager.Instance?.ShowNotification(
                $"Challenge Complete! Waves: {totalWaves}, Score: {score}, Gold: +{goldReward}, Exp: +{expReward}",
                NotificationType.System);

            OnChallengeEnded?.Invoke(score, true);
        }

        [ClientRpc]
        private void NotifyChallengeFailedClientRpc(string reason, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            localIsInChallenge = false;

            NotificationManager.Instance?.ShowNotification(
                $"Challenge Failed: {reason}",
                NotificationType.Warning);

            OnChallengeEnded?.Invoke(0, false);
        }
    }
}
