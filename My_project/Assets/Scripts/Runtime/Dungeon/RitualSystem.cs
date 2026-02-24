using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public enum RitualRewardType
    {
        RareWeapon, EpicArmor, LegendaryMaterial, SkillBook, ParagonPoint,
        ResetScroll, TemperingMaterial, CorruptionOrb, InfusionStone,
        ExpPotion, GoldBundle, Title, PetEgg, RelicFragment, MutationStone
    }

    [Serializable]
    public class RitualReward
    {
        public RitualRewardType type;
        public string name;
        public string description;
        public int pointCost;

        public RitualReward(RitualRewardType type, string name, string desc, int pointCost)
        {
            this.type = type;
            this.name = name;
            this.description = desc;
            this.pointCost = pointCost;
        }
    }

    [Serializable]
    public class RitualState
    {
        public ulong ownerId;
        public int totalPoints;
        public int monstersToDefeat;
        public int monstersDefeated;
        public bool waveActive;
        public bool rewardPhase;
        public List<RitualReward> availableRewards = new List<RitualReward>();
    }

    /// <summary>
    /// Ritual System - Sacrifice items at ritual altars in dungeons for powerful rewards.
    /// Sacrifice items → earn points → defend monster waves → spend points on rewards.
    /// </summary>
    public class RitualSystem : NetworkBehaviour
    {
        public static RitualSystem Instance { get; private set; }

        // Point values by item grade
        private static readonly int[] PointsByGrade = { 10, 25, 60, 150, 400 }; // Common, Uncommon, Rare, Epic, Legendary
        private const int MaterialBasePoints = 5;
        private const float AltarSpawnChance = 0.30f;
        private const int MaxRewardsShown = 5;
        private const int WaveMonstersPerPoint = 1; // per 50 points

        // All possible rewards
        private static readonly RitualReward[] AllRewards = new RitualReward[]
        {
            new RitualReward(RitualRewardType.RareWeapon, "희귀 무기", "랜덤 희귀 등급 무기", 80),
            new RitualReward(RitualRewardType.EpicArmor, "에픽 방어구", "랜덤 에픽 등급 방어구", 200),
            new RitualReward(RitualRewardType.LegendaryMaterial, "전설 재료", "전설 제작용 특수 재료", 350),
            new RitualReward(RitualRewardType.SkillBook, "스킬 서적", "랜덤 스킬 1레벨 상승", 250),
            new RitualReward(RitualRewardType.ParagonPoint, "파라곤 포인트", "파라곤 포인트 +1", 300),
            new RitualReward(RitualRewardType.ResetScroll, "리셋 스크롤", "스탯/스킬 초기화", 150),
            new RitualReward(RitualRewardType.TemperingMaterial, "템퍼링 재료", "템퍼링 시도 재료", 100),
            new RitualReward(RitualRewardType.CorruptionOrb, "타락 오브", "아이템 타락 시도 재료", 400),
            new RitualReward(RitualRewardType.InfusionStone, "주입석", "주입 시스템 재료", 180),
            new RitualReward(RitualRewardType.ExpPotion, "경험치 물약", "경험치 대량 획득", 120),
            new RitualReward(RitualRewardType.GoldBundle, "골드 뭉치", "골드 5000G", 60),
            new RitualReward(RitualRewardType.Title, "칭호", "특별 칭호 획득", 500),
            new RitualReward(RitualRewardType.PetEgg, "펫 알", "랜덤 펫 부화 알", 350),
            new RitualReward(RitualRewardType.RelicFragment, "유물 파편", "유물 합성 재료", 220),
            new RitualReward(RitualRewardType.MutationStone, "변이석", "몬스터 변이 유도 아이템", 160)
        };

        // Active ritual states per dungeon floor
        private Dictionary<string, RitualState> activeRituals = new Dictionary<string, RitualState>();

        public Action<string, int> OnSacrificeAdded;      // altarKey, totalPoints
        public Action<string> OnWaveStarted;               // altarKey
        public Action<string> OnWaveComplete;              // altarKey
        public Action<string, RitualRewardType> OnRewardClaimed; // altarKey, rewardType

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

        #region Altar Spawning

        public bool ShouldSpawnAltar()
        {
            return UnityEngine.Random.value <= AltarSpawnChance;
        }

        public string InitializeAltar(string floorKey, ulong ownerId)
        {
            if (!IsServer) return null;

            string altarKey = $"ritual_{floorKey}_{Time.time:F0}";
            activeRituals[altarKey] = new RitualState
            {
                ownerId = ownerId,
                totalPoints = 0,
                monstersToDefeat = 0,
                monstersDefeated = 0,
                waveActive = false,
                rewardPhase = false
            };

            Debug.Log($"[RitualSystem] Altar initialized: {altarKey}");
            return altarKey;
        }

        #endregion

        #region ServerRpc

        [ServerRpc(RequireOwnership = false)]
        public void SacrificeItemServerRpc(string altarKey, int itemGrade, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!activeRituals.TryGetValue(altarKey, out var ritual))
            {
                NotifyRitualMessageClientRpc(clientId, "의식 제단을 찾을 수 없습니다.");
                return;
            }

            if (ritual.waveActive || ritual.rewardPhase)
            {
                NotifyRitualMessageClientRpc(clientId, "현재 의식이 진행 중입니다.");
                return;
            }

            int points = (itemGrade >= 0 && itemGrade < PointsByGrade.Length)
                ? PointsByGrade[itemGrade]
                : MaterialBasePoints;

            ritual.totalPoints += points;

            NotifyRitualMessageClientRpc(clientId,
                $"제물 투입! +{points} 포인트 (총: {ritual.totalPoints})");
            OnSacrificeAdded?.Invoke(altarKey, ritual.totalPoints);

            Debug.Log($"[RitualSystem] Sacrifice: +{points} points at {altarKey}. Total: {ritual.totalPoints}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void StartRitualWaveServerRpc(string altarKey, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!activeRituals.TryGetValue(altarKey, out var ritual))
            {
                NotifyRitualMessageClientRpc(clientId, "의식 제단을 찾을 수 없습니다.");
                return;
            }

            if (ritual.totalPoints <= 0)
            {
                NotifyRitualMessageClientRpc(clientId, "제물을 먼저 투입하세요.");
                return;
            }

            if (ritual.waveActive)
            {
                NotifyRitualMessageClientRpc(clientId, "웨이브가 이미 진행 중입니다.");
                return;
            }

            // Calculate monsters based on points
            ritual.monstersToDefeat = Mathf.Max(3, ritual.totalPoints / 50 * WaveMonstersPerPoint + 3);
            ritual.monstersDefeated = 0;
            ritual.waveActive = true;

            NotifyWaveStartClientRpc(clientId, altarKey, ritual.monstersToDefeat);
            OnWaveStarted?.Invoke(altarKey);

            Debug.Log($"[RitualSystem] Wave started at {altarKey}. Monsters: {ritual.monstersToDefeat}");
        }

        /// <summary>
        /// Called when a monster in the ritual wave is defeated.
        /// </summary>
        public void ReportRitualKill(string altarKey)
        {
            if (!IsServer) return;
            if (!activeRituals.TryGetValue(altarKey, out var ritual)) return;
            if (!ritual.waveActive) return;

            ritual.monstersDefeated++;

            if (ritual.monstersDefeated >= ritual.monstersToDefeat)
            {
                ritual.waveActive = false;
                ritual.rewardPhase = true;

                // Generate random rewards
                ritual.availableRewards.Clear();
                var shuffled = AllRewards.OrderBy(_ => UnityEngine.Random.value).ToList();
                int rewardCount = Mathf.Min(MaxRewardsShown, shuffled.Count);
                for (int i = 0; i < rewardCount; i++)
                {
                    ritual.availableRewards.Add(shuffled[i]);
                }

                NotifyWaveCompleteClientRpc(ritual.ownerId, altarKey, ritual.totalPoints);
                OnWaveComplete?.Invoke(altarKey);

                // Send reward options
                string rewardList = string.Join("|",
                    ritual.availableRewards.Select(r => $"{(int)r.type},{r.name},{r.pointCost}"));
                NotifyRewardOptionsClientRpc(ritual.ownerId, altarKey, rewardList, ritual.totalPoints);

                Debug.Log($"[RitualSystem] Wave complete at {altarKey}. Entering reward phase.");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ClaimRitualRewardServerRpc(string altarKey, int rewardTypeInt, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!activeRituals.TryGetValue(altarKey, out var ritual))
            {
                NotifyRitualMessageClientRpc(clientId, "의식 제단을 찾을 수 없습니다.");
                return;
            }

            if (!ritual.rewardPhase)
            {
                NotifyRitualMessageClientRpc(clientId, "보상 단계가 아닙니다.");
                return;
            }

            RitualRewardType rewardType = (RitualRewardType)rewardTypeInt;
            var reward = ritual.availableRewards.FirstOrDefault(r => r.type == rewardType);
            if (reward == null)
            {
                NotifyRitualMessageClientRpc(clientId, "해당 보상을 찾을 수 없습니다.");
                return;
            }

            if (ritual.totalPoints < reward.pointCost)
            {
                NotifyRitualMessageClientRpc(clientId,
                    $"포인트 부족 ({ritual.totalPoints}/{reward.pointCost})");
                return;
            }

            ritual.totalPoints -= reward.pointCost;
            ritual.availableRewards.Remove(reward);

            // Apply reward
            ApplyReward(clientId, reward);

            NotifyRewardClaimedClientRpc(clientId, reward.name, reward.pointCost, ritual.totalPoints);
            OnRewardClaimed?.Invoke(altarKey, rewardType);

            // If no points left or no rewards, end ritual
            if (ritual.totalPoints <= 0 || ritual.availableRewards.Count == 0)
            {
                activeRituals.Remove(altarKey);
                NotifyRitualMessageClientRpc(clientId, "의식이 종료되었습니다.");
            }

            Debug.Log($"[RitualSystem] Reward claimed: {reward.name} for {reward.pointCost} points");
        }

        #endregion

        #region Reward Application

        private void ApplyReward(ulong clientId, RitualReward reward)
        {
            var statsData = GetPlayerStatsData(clientId);

            switch (reward.type)
            {
                case RitualRewardType.GoldBundle:
                    statsData?.ChangeGold(5000);
                    break;
                case RitualRewardType.ExpPotion:
                    statsData?.AddExperience(3000);
                    break;
                default:
                    // Other rewards: grant via notification (actual item/effect systems handle separately)
                    Debug.Log($"[RitualSystem] Granted reward: {reward.name} to client {clientId}");
                    break;
            }
        }

        #endregion

        #region ClientRpc

        [ClientRpc]
        private void NotifyRitualMessageClientRpc(ulong targetClientId, string message)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager.Instance?.ShowNotification(message, NotificationType.Warning);
        }

        [ClientRpc]
        private void NotifyWaveStartClientRpc(ulong targetClientId, string altarKey, int monsterCount)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager.Instance?.ShowNotification(
                $"<color=#FF4444>의식 웨이브 시작!</color> {monsterCount}마리 처치하세요!",
                NotificationType.System);
        }

        [ClientRpc]
        private void NotifyWaveCompleteClientRpc(ulong targetClientId, string altarKey, int totalPoints)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager.Instance?.ShowNotification(
                $"<color=#00FF00>웨이브 방어 성공!</color> {totalPoints} 포인트로 보상을 선택하세요!",
                NotificationType.System);
        }

        [ClientRpc]
        private void NotifyRewardOptionsClientRpc(ulong targetClientId, string altarKey,
            string rewardListCsv, int points)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            // UI should parse rewardListCsv to display reward options
            Debug.Log($"[RitualSystem] Rewards available: {rewardListCsv} (Points: {points})");
        }

        [ClientRpc]
        private void NotifyRewardClaimedClientRpc(ulong targetClientId, string rewardName,
            int cost, int remainingPoints)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager.Instance?.ShowNotification(
                $"<color=#FFD700>보상 획득:</color> {rewardName} (-{cost}pt, 잔여: {remainingPoints}pt)",
                NotificationType.System);
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
