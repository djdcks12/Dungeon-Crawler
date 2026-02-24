using UnityEngine;
using Unity.Netcode;

using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 평판/진영 시스템 - 서버 권위적
    /// 6개 진영, 7단계 평판 등급, 등급별 해금 보상
    /// </summary>
    public class ReputationSystem : NetworkBehaviour
    {
        public static ReputationSystem Instance { get; private set; }

        // 서버: 플레이어별 진영 평판
        private Dictionary<ulong, Dictionary<Faction, int>> playerReputations = new Dictionary<ulong, Dictionary<Faction, int>>();

        // 로컬
        private Dictionary<Faction, int> localReputations = new Dictionary<Faction, int>();

        // 이벤트
        public System.Action OnReputationUpdated;
        public System.Action<Faction, ReputationTier> OnReputationTierUp;

        // 진영별 정보
        private static readonly Dictionary<Faction, FactionInfo> factionInfos = new Dictionary<Faction, FactionInfo>
        {
            { Faction.TownGuard, new FactionInfo("마을 수비대", "마을과 시민을 수호하는 군인 조직", "#4488FF",
                new[] { "상점 할인 5%", "수비대 전용 퀘스트", "수비대 무기 구매", "수비대 방어구 구매", "수비대 칭호", "수비대 전용 버프", "수비대 최고등급 장비" }) },
            { Faction.MerchantGuild, new FactionInfo("상인 길드", "대륙 전역의 상인 연합", "#FFD700",
                new[] { "상점 할인 10%", "희귀 아이템 입고", "경매장 수수료 감소", "특수 재료 판매", "상인 칭호", "VIP 상점 해금", "전설 소모품 구매" }) },
            { Faction.AdventurerGuild, new FactionInfo("모험가 조합", "모험가들의 자치 조직", "#44FF44",
                new[] { "경험치 +5%", "던전 정보 공개", "파티 모집 우선", "특수 던전 입장", "모험가 칭호", "경험치 +15%", "전설 던전 입장" }) },
            { Faction.MageAcademy, new FactionInfo("마법 학회", "마법을 연구하는 학자 집단", "#AA44FF",
                new[] { "마법 스킬 강화 +5%", "마법서 구매", "인챈트 성공률 +5%", "고급 인챈트 해금", "학자 칭호", "마법 스킬 강화 +15%", "전설 인챈트" }) },
            { Faction.ShadowHunter, new FactionInfo("어둠 사냥꾼", "어둠의 위협을 제거하는 비밀 조직", "#FF4444",
                new[] { "크리티컬 +3%", "암살 스킬 해금", "독 저항 +10%", "야간 시야 확장", "사냥꾼 칭호", "크리티컬 +10%", "전설 암살 기술" }) },
            { Faction.AncientGuardian, new FactionInfo("고대 수호자", "고대 유적과 지식을 수호하는 수도사 집단", "#FF8844",
                new[] { "방어력 +3%", "고대 유물 감정", "유적 던전 입장", "고대 마법 학습", "수호자 칭호", "방어력 +10%", "전설 고대 장비" }) },
        };

        // 평판 등급 임계값
        private static readonly int[] tierThresholds = { -6000, -3000, 0, 3000, 8000, 15000, 30000 };
        // 등급: 적대(-6000이하), 냉담(-3000), 중립(0), 우호(3000), 존경(8000), 숭경(15000), 숭배(30000)

        // 접근자
        public int GetLocalReputation(Faction faction)
        {
            return localReputations.TryGetValue(faction, out int rep) ? rep : 0;
        }

        public ReputationTier GetLocalTier(Faction faction)
        {
            return GetTierFromRep(GetLocalReputation(faction));
        }

        public FactionInfo GetFactionInfo(Faction faction)
        {
            return factionInfos.TryGetValue(faction, out var info) ? info : null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// 평판 변경 (서버에서 호출)
        /// </summary>
        public void ChangeReputation(ulong clientId, Faction faction, int amount, string reason = "")
        {
            if (!IsServer) return;

            var reps = GetOrCreateReps(clientId);
            if (!reps.ContainsKey(faction)) reps[faction] = 0;

            ReputationTier oldTier = GetTierFromRep(reps[faction]);
            reps[faction] += amount;
            reps[faction] = Mathf.Clamp(reps[faction], -10000, 50000);
            ReputationTier newTier = GetTierFromRep(reps[faction]);

            bool tierChanged = newTier != oldTier && (int)newTier > (int)oldTier;

            ReputationChangedClientRpc((int)faction, reps[faction], amount, (int)newTier,
                tierChanged, reason, clientId);

            // 칭호 해금 (숭경 등급 이상)
            if (tierChanged && newTier >= ReputationTier.Revered)
            {
                if (TitleSystem.Instance != null)
                {
                    string titleId = $"faction_{faction.ToString().ToLower()}";
                    TitleSystem.Instance.UnlockTitle(clientId, titleId);
                }
            }
        }

        /// <summary>
        /// 상점 할인율 계산
        /// </summary>
        public float GetShopDiscount(ulong clientId, Faction shopFaction)
        {
            if (!IsServer) return 0f;
            var reps = GetOrCreateReps(clientId);
            if (!reps.ContainsKey(shopFaction)) return 0f;

            var tier = GetTierFromRep(reps[shopFaction]);
            return tier switch
            {
                ReputationTier.Friendly => 0.05f,
                ReputationTier.Honored => 0.10f,
                ReputationTier.Revered => 0.15f,
                ReputationTier.Exalted => 0.20f,
                _ => 0f
            };
        }

        /// <summary>
        /// 평판 보너스 스탯 계산 (로컬)
        /// </summary>
        public float GetLocalStatBonus(string statType)
        {
            float bonus = 0f;

            // 어둠 사냥꾼 크리티컬 보너스
            var hunterTier = GetLocalTier(Faction.ShadowHunter);
            if (statType == "CritRate")
            {
                if (hunterTier >= ReputationTier.Friendly) bonus += 0.03f;
                if (hunterTier >= ReputationTier.Revered) bonus += 0.07f;
            }

            // 고대 수호자 방어 보너스
            var guardianTier = GetLocalTier(Faction.AncientGuardian);
            if (statType == "Defense")
            {
                if (guardianTier >= ReputationTier.Friendly) bonus += 0.03f;
                if (guardianTier >= ReputationTier.Revered) bonus += 0.07f;
            }

            // 모험가 조합 경험치 보너스
            var adventurerTier = GetLocalTier(Faction.AdventurerGuild);
            if (statType == "ExpBonus")
            {
                if (adventurerTier >= ReputationTier.Friendly) bonus += 0.05f;
                if (adventurerTier >= ReputationTier.Revered) bonus += 0.10f;
            }

            // 마법 학회 마법 데미지 보너스
            var mageTier = GetLocalTier(Faction.MageAcademy);
            if (statType == "MagicDamage")
            {
                if (mageTier >= ReputationTier.Friendly) bonus += 0.05f;
                if (mageTier >= ReputationTier.Revered) bonus += 0.10f;
            }

            return bonus;
        }

        /// <summary>
        /// 동기화 요청
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestSyncServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var reps = GetOrCreateReps(clientId);

            ClearLocalRepsClientRpc(clientId);
            foreach (var kvp in reps)
            {
                SyncReputationClientRpc((int)kvp.Key, kvp.Value, clientId);
            }
            SyncCompleteClientRpc(clientId);
        }

        #region ClientRPCs

        [ClientRpc]
        private void ReputationChangedClientRpc(int factionInt, int newRep, int change, int newTierInt,
            bool tierChanged, string reason, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            var faction = (Faction)factionInt;
            localReputations[faction] = newRep;
            var newTier = (ReputationTier)newTierInt;

            OnReputationUpdated?.Invoke();

            var notif = NotificationManager.Instance;
            if (notif != null)
            {
                var info = GetFactionInfo(faction);
                string name = info?.name ?? faction.ToString();
                string sign = change > 0 ? "+" : "";
                notif.ShowNotification($"<color={info?.colorHex ?? "#FFFFFF"}>{name}</color> 평판 {sign}{change}", NotificationType.System);

                if (tierChanged)
                {
                    OnReputationTierUp?.Invoke(faction, newTier);
                    string tierName = GetTierName(newTier);
                    notif.ShowNotification($"<color={info?.colorHex ?? "#FFFFFF"}>{name}</color> 등급 상승: <color=#FFD700>{tierName}</color>", NotificationType.Achievement);
                }
            }
        }

        [ClientRpc]
        private void ClearLocalRepsClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localReputations.Clear();
        }

        [ClientRpc]
        private void SyncReputationClientRpc(int factionInt, int rep, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localReputations[(Faction)factionInt] = rep;
        }

        [ClientRpc]
        private void SyncCompleteClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            OnReputationUpdated?.Invoke();
        }

        #endregion

        #region Utility

        private Dictionary<Faction, int> GetOrCreateReps(ulong clientId)
        {
            if (!playerReputations.ContainsKey(clientId))
                playerReputations[clientId] = new Dictionary<Faction, int>();
            return playerReputations[clientId];
        }

        public static ReputationTier GetTierFromRep(int rep)
        {
            if (rep >= tierThresholds[6]) return ReputationTier.Exalted;
            if (rep >= tierThresholds[5]) return ReputationTier.Revered;
            if (rep >= tierThresholds[4]) return ReputationTier.Honored;
            if (rep >= tierThresholds[3]) return ReputationTier.Friendly;
            if (rep >= tierThresholds[2]) return ReputationTier.Neutral;
            if (rep >= tierThresholds[1]) return ReputationTier.Unfriendly;
            return ReputationTier.Hostile;
        }

        public static string GetTierName(ReputationTier tier)
        {
            return tier switch
            {
                ReputationTier.Hostile => "적대",
                ReputationTier.Unfriendly => "냉담",
                ReputationTier.Neutral => "중립",
                ReputationTier.Friendly => "우호",
                ReputationTier.Honored => "존경",
                ReputationTier.Revered => "숭경",
                ReputationTier.Exalted => "숭배",
                _ => "???"
            };
        }

        public static string GetTierColor(ReputationTier tier)
        {
            return tier switch
            {
                ReputationTier.Hostile => "#FF0000",
                ReputationTier.Unfriendly => "#FF6644",
                ReputationTier.Neutral => "#CCCCCC",
                ReputationTier.Friendly => "#44FF44",
                ReputationTier.Honored => "#4488FF",
                ReputationTier.Revered => "#AA44FF",
                ReputationTier.Exalted => "#FFD700",
                _ => "#FFFFFF"
            };
        }

        /// <summary>
        /// 다음 등급까지 필요 평판
        /// </summary>
        public static int GetRepToNextTier(int currentRep)
        {
            for (int i = 0; i < tierThresholds.Length; i++)
            {
                if (currentRep < tierThresholds[i])
                    return tierThresholds[i] - currentRep;
            }
            return 0; // 최대 등급
        }

        /// <summary>
        /// 현재 등급 내 진행률 (0~1)
        /// </summary>
        public static float GetTierProgress(int currentRep)
        {
            int tierIndex = 0;
            for (int i = tierThresholds.Length - 1; i >= 0; i--)
            {
                if (currentRep >= tierThresholds[i])
                {
                    tierIndex = i;
                    break;
                }
            }

            if (tierIndex >= tierThresholds.Length - 1) return 1f;

            int tierStart = tierThresholds[tierIndex];
            int tierEnd = tierThresholds[tierIndex + 1];
            return (float)(currentRep - tierStart) / (tierEnd - tierStart);
        }

        #endregion

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }

    /// <summary>
    /// 진영 enum
    /// </summary>
    public enum Faction
    {
        TownGuard = 0,       // 마을 수비대
        MerchantGuild = 1,   // 상인 길드
        AdventurerGuild = 2, // 모험가 조합
        MageAcademy = 3,     // 마법 학회
        ShadowHunter = 4,    // 어둠 사냥꾼
        AncientGuardian = 5  // 고대 수호자
    }

    /// <summary>
    /// 평판 등급
    /// </summary>
    public enum ReputationTier
    {
        Hostile = 0,     // 적대
        Unfriendly = 1,  // 냉담
        Neutral = 2,     // 중립
        Friendly = 3,    // 우호
        Honored = 4,     // 존경
        Revered = 5,     // 숭경
        Exalted = 6      // 숭배
    }

    /// <summary>
    /// 진영 정보
    /// </summary>
    public class FactionInfo
    {
        public string name;
        public string description;
        public string colorHex;
        public string[] tierRewards; // 등급별 해금 보상 설명 (7개)

        public FactionInfo(string name, string description, string colorHex, string[] tierRewards)
        {
            this.name = name;
            this.description = description;
            this.colorHex = colorHex;
            this.tierRewards = tierRewards;
        }
    }
}
