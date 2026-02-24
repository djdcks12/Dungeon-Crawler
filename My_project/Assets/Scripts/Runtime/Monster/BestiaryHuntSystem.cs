using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public enum CaptureRewardType
    {
        MonsterEssence,   // Crafting material
        SoulStone,        // Pet conversion material
        RaceTrait,        // Temporary passive from monster's race
        EliteToken,       // Special token from elite captures
        BossTrophy        // Trophy from boss captures
    }

    [Serializable]
    public class CapturedMonster
    {
        public string monsterRace;
        public string monsterVariant;
        public int monsterLevel;
        public float captureTime;
        public bool isProcessed;
    }

    [Serializable]
    public class BestiaryEntry
    {
        public string monsterRace;
        public int captureCount;
        public int uniqueVariants;
        public bool eliteCaptured;
        public bool bossCaptured;
    }

    /// <summary>
    /// Bestiary Hunt System - Capture weakened monsters for special materials.
    /// Monsters under 20% HP can be captured. Captured monsters are processed at the lab.
    /// </summary>
    public class BestiaryHuntSystem : NetworkBehaviour
    {
        public static BestiaryHuntSystem Instance { get; private set; }

        public const int DailyCaptureLimit = 10;
        public const float CaptureHpThreshold = 0.20f;

        // Capture success rate based on remaining HP percentage
        // 20% HP → 50%, 10% → 75%, 5% → 90%
        private static float GetCaptureRate(float hpPercent)
        {
            if (hpPercent <= 0.05f) return 0.90f;
            if (hpPercent <= 0.10f) return 0.75f;
            if (hpPercent <= 0.15f) return 0.60f;
            return 0.50f;
        }

        // Bestiary milestones
        private static readonly int[] MilestoneThresholds = { 5, 15, 30, 50, 100 };
        private static readonly string[] MilestoneRewards =
        {
            "포획 효율 +10%", "함정 제작비 -20%", "희귀 재료 확률 +15%",
            "엘리트 포획률 +10%", "보스 포획 가능"
        };

        // Monster races for bestiary
        private static readonly string[] MonsterRaces =
        {
            "Goblin", "Orc", "Undead", "Beast", "Elemental", "Demon", "Dragon", "Construct"
        };

        // Per-player data
        private Dictionary<ulong, List<CapturedMonster>> playerCaptures = new Dictionary<ulong, List<CapturedMonster>>();
        private Dictionary<ulong, Dictionary<string, BestiaryEntry>> playerBestiary = new Dictionary<ulong, Dictionary<string, BestiaryEntry>>();
        private Dictionary<ulong, int> dailyCaptureCount = new Dictionary<ulong, int>();

        public Action<ulong, string> OnMonsterCaptured;
        public Action<ulong, string, int> OnBestiaryMilestone; // clientId, race, milestone

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        #region Public API

        public int GetDailyRemaining(ulong clientId)
        {
            if (!dailyCaptureCount.TryGetValue(clientId, out int count)) return DailyCaptureLimit;
            return Mathf.Max(0, DailyCaptureLimit - count);
        }

        public List<CapturedMonster> GetUnprocessedCaptures(ulong clientId)
        {
            if (!playerCaptures.TryGetValue(clientId, out var captures))
                return new List<CapturedMonster>();
            return captures.Where(c => !c.isProcessed).ToList();
        }

        public Dictionary<string, BestiaryEntry> GetBestiary(ulong clientId)
        {
            if (!playerBestiary.TryGetValue(clientId, out var bestiary))
                return new Dictionary<string, BestiaryEntry>();
            return bestiary;
        }

        public int GetTotalCaptures(ulong clientId)
        {
            if (!playerBestiary.TryGetValue(clientId, out var bestiary)) return 0;
            return bestiary.Values.Sum(e => e.captureCount);
        }

        #endregion

        #region ServerRpc

        [ServerRpc(RequireOwnership = false)]
        public void AttemptCaptureServerRpc(string monsterRace, string monsterVariant, int monsterLevel,
            float hpPercent, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            // Check daily limit
            if (!dailyCaptureCount.ContainsKey(clientId))
                dailyCaptureCount[clientId] = 0;

            if (dailyCaptureCount[clientId] >= DailyCaptureLimit)
            {
                NotifyCaptureResultClientRpc(clientId, false, "일일 포획 한도 도달 (10/10)");
                return;
            }

            // Check HP threshold
            if (hpPercent > CaptureHpThreshold)
            {
                NotifyCaptureResultClientRpc(clientId, false,
                    $"몬스터 HP가 너무 높습니다 ({hpPercent:P0}). {CaptureHpThreshold:P0} 이하에서 포획 가능");
                return;
            }

            // Boss capture requires milestone
            bool isBoss = monsterVariant.Contains("Boss");
            if (isBoss)
            {
                int totalCaptures = GetTotalCaptures(clientId);
                if (totalCaptures < MilestoneThresholds[4])
                {
                    NotifyCaptureResultClientRpc(clientId, false,
                        $"보스 포획은 총 {MilestoneThresholds[4]}회 포획 달성 후 가능합니다.");
                    return;
                }
            }

            // Calculate success rate
            float captureRate = GetCaptureRate(hpPercent);

            // Apply milestone bonus
            int total = GetTotalCaptures(clientId);
            if (total >= MilestoneThresholds[0]) captureRate += 0.10f; // +10% from milestone

            bool isElite = monsterVariant.Contains("Elite") || monsterVariant.Contains("Leader");
            if (isElite && total >= MilestoneThresholds[3])
                captureRate += 0.10f; // +10% for elites from milestone

            captureRate = Mathf.Min(captureRate, 0.95f);

            bool success = UnityEngine.Random.value <= captureRate;
            dailyCaptureCount[clientId]++;

            if (!success)
            {
                NotifyCaptureResultClientRpc(clientId, false,
                    $"포획 실패! (성공률: {captureRate:P0}) 남은 횟수: {GetDailyRemaining(clientId)}");
                return;
            }

            // Capture success
            if (!playerCaptures.ContainsKey(clientId))
                playerCaptures[clientId] = new List<CapturedMonster>();

            var captured = new CapturedMonster
            {
                monsterRace = monsterRace,
                monsterVariant = monsterVariant,
                monsterLevel = monsterLevel,
                captureTime = Time.time,
                isProcessed = false
            };
            playerCaptures[clientId].Add(captured);

            // Update bestiary
            UpdateBestiary(clientId, monsterRace, monsterVariant, isElite, isBoss);

            NotifyCaptureResultClientRpc(clientId, true,
                $"<color=#00FF00>포획 성공!</color> {monsterRace} {monsterVariant} Lv.{monsterLevel}. 남은 횟수: {GetDailyRemaining(clientId)}");

            OnMonsterCaptured?.Invoke(clientId, monsterRace);
            Debug.Log($"[BestiaryHunt] Client {clientId} captured {monsterRace} {monsterVariant} Lv.{monsterLevel}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void ProcessCaptureServerRpc(int captureIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!playerCaptures.TryGetValue(clientId, out var captures))
            {
                NotifyProcessResultClientRpc(clientId, "포획된 몬스터가 없습니다.");
                return;
            }

            var unprocessed = captures.Where(c => !c.isProcessed).ToList();
            if (captureIndex < 0 || captureIndex >= unprocessed.Count)
            {
                NotifyProcessResultClientRpc(clientId, "잘못된 인덱스입니다.");
                return;
            }

            var monster = unprocessed[captureIndex];
            monster.isProcessed = true;

            // Determine rewards
            string rewardText = ProcessMonsterRewards(clientId, monster);

            NotifyProcessResultClientRpc(clientId,
                $"<color=#AA00FF>분해 완료:</color> {monster.monsterRace} {monster.monsterVariant} → {rewardText}");

            Debug.Log($"[BestiaryHunt] Client {clientId} processed {monster.monsterRace} {monster.monsterVariant}");
        }

        #endregion

        #region Processing

        private string ProcessMonsterRewards(ulong clientId, CapturedMonster monster)
        {
            var rewards = new List<string>();
            var statsData = GetPlayerStatsData(clientId);

            // Base: Monster Essence (always)
            int essenceCount = 1 + monster.monsterLevel / 5;
            rewards.Add($"몬스터 에센스 x{essenceCount}");

            // 30% chance: Soul Stone
            if (UnityEngine.Random.value < 0.30f)
            {
                rewards.Add("영혼석 x1");
            }

            // 20% chance: Race Trait scroll
            if (UnityEngine.Random.value < 0.20f)
            {
                rewards.Add($"{monster.monsterRace} 종족 특성서");
            }

            // Elite bonus
            bool isElite = monster.monsterVariant.Contains("Elite") || monster.monsterVariant.Contains("Leader");
            if (isElite)
            {
                rewards.Add("엘리트 토큰 x1");
                if (statsData != null) statsData.ChangeGold(500);
                rewards.Add("골드 +500");
            }

            // Boss bonus
            if (monster.monsterVariant.Contains("Boss"))
            {
                rewards.Add("보스 트로피 x1");
                if (statsData != null)
                {
                    statsData.ChangeGold(2000);
                    statsData.AddExperience(1000);
                }
                rewards.Add("골드 +2000, EXP +1000");
            }

            return string.Join(", ", rewards);
        }

        #endregion

        #region Bestiary

        private void UpdateBestiary(ulong clientId, string race, string variant, bool isElite, bool isBoss)
        {
            if (!playerBestiary.ContainsKey(clientId))
                playerBestiary[clientId] = new Dictionary<string, BestiaryEntry>();

            var bestiary = playerBestiary[clientId];

            if (!bestiary.TryGetValue(race, out var entry))
            {
                entry = new BestiaryEntry
                {
                    monsterRace = race,
                    captureCount = 0,
                    uniqueVariants = 0,
                    eliteCaptured = false,
                    bossCaptured = false
                };
                bestiary[race] = entry;
            }

            entry.captureCount++;
            if (isElite) entry.eliteCaptured = true;
            if (isBoss) entry.bossCaptured = true;

            // Check milestones
            int totalCaptures = GetTotalCaptures(clientId);
            for (int i = 0; i < MilestoneThresholds.Length; i++)
            {
                if (totalCaptures == MilestoneThresholds[i])
                {
                    NotifyMilestoneClientRpc(clientId, MilestoneThresholds[i], MilestoneRewards[i]);
                    OnBestiaryMilestone?.Invoke(clientId, race, MilestoneThresholds[i]);
                    break;
                }
            }
        }

        #endregion

        #region ClientRpc

        [ClientRpc]
        private void NotifyCaptureResultClientRpc(ulong targetClientId, bool success, string message)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            var type = success ? NotificationType.System : NotificationType.Warning;
            NotificationManager.Instance?.ShowNotification(message, type);
        }

        [ClientRpc]
        private void NotifyProcessResultClientRpc(ulong targetClientId, string message)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager.Instance?.ShowNotification(message, NotificationType.System);
        }

        [ClientRpc]
        private void NotifyMilestoneClientRpc(ulong targetClientId, int milestone, string reward)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager.Instance?.ShowNotification(
                $"<color=#FFD700>포획 마일스톤 {milestone}회 달성!</color> 보상: {reward}",
                NotificationType.Achievement);
        }

        #endregion

        #region Helpers

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (NetworkManager.Singleton == null) return null;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        #endregion
    }
}
