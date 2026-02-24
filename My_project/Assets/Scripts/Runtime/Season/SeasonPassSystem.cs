using UnityEngine;
using Unity.Netcode;

using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 시즌패스 시스템 - 50단계 무료/프리미엄 보상 트랙
    /// 시즌 경험치로 단계 해금, 보상 수령
    /// </summary>
    public class SeasonPassSystem : NetworkBehaviour
    {
        public static SeasonPassSystem Instance { get; private set; }

        [Header("Season Settings")]
        [SerializeField] private int maxTier = 50;
        [SerializeField] private int baseExpPerTier = 1000; // 1단계 필요 경험치
        [SerializeField] private float expScaling = 1.05f; // 단계당 경험치 증가 배율
        [SerializeField] private int seasonDurationDays = 90; // 시즌 기간 (일)

        // 서버: 플레이어별 시즌 데이터
        private Dictionary<ulong, SeasonPlayerData> playerSeasonData = new Dictionary<ulong, SeasonPlayerData>();

        // 보상 테이블
        private SeasonReward[] freeRewards;
        private SeasonReward[] premiumRewards;

        // 로컬
        private int localTier;
        private int localExp;
        private int localExpToNext;
        private bool localIsPremium;
        private HashSet<int> localClaimedFree = new HashSet<int>();
        private HashSet<int> localClaimedPremium = new HashSet<int>();
        private int localSeasonNumber = 1;

        // 이벤트
        public System.Action OnSeasonDataUpdated;
        public System.Action<int> OnTierUp; // newTier

        // 접근자
        public int LocalTier => localTier;
        public int LocalExp => localExp;
        public int LocalExpToNext => localExpToNext;
        public bool LocalIsPremium => localIsPremium;
        public int MaxTier => maxTier;
        public int SeasonNumber => localSeasonNumber;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeRewards();
        }

        /// <summary>
        /// 시즌 경험치 획득 (서버에서 호출)
        /// </summary>
        public void AddSeasonExp(ulong clientId, int amount, string source = "")
        {
            if (!IsServer) return;
            if (amount <= 0) return;

            var data = GetOrCreateData(clientId);
            data.currentExp += amount;

            // 단계 업 체크
            bool tieredUp = false;
            while (data.currentTier < maxTier)
            {
                int needed = GetExpForTier(data.currentTier + 1);
                if (data.currentExp >= needed)
                {
                    data.currentExp -= needed;
                    data.currentTier++;
                    tieredUp = true;
                }
                else break;
            }

            // 최대 단계에서 경험치 캡
            if (data.currentTier >= maxTier)
                data.currentExp = 0;

            int expToNext = data.currentTier < maxTier ? GetExpForTier(data.currentTier + 1) : 0;

            SeasonExpGainedClientRpc(amount, data.currentTier, data.currentExp, expToNext,
                tieredUp, source, clientId);
        }

        /// <summary>
        /// 무료 보상 수령
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ClaimFreeRewardServerRpc(int tier, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var data = GetOrCreateData(clientId);

            if (tier < 1 || tier > maxTier)
            {
                SendMessageClientRpc("잘못된 단계입니다.", clientId);
                return;
            }

            if (data.currentTier < tier)
            {
                SendMessageClientRpc("아직 해당 단계에 도달하지 않았습니다.", clientId);
                return;
            }

            if (data.claimedFreeTiers.Contains(tier))
            {
                SendMessageClientRpc("이미 수령한 보상입니다.", clientId);
                return;
            }

            data.claimedFreeTiers.Add(tier);
            var reward = freeRewards[tier - 1];
            GrantReward(clientId, reward);

            RewardClaimedClientRpc(tier, false, reward.description, clientId);
        }

        /// <summary>
        /// 프리미엄 보상 수령
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ClaimPremiumRewardServerRpc(int tier, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var data = GetOrCreateData(clientId);

            if (!data.isPremium)
            {
                SendMessageClientRpc("프리미엄 패스가 필요합니다.", clientId);
                return;
            }

            if (tier < 1 || tier > maxTier)
            {
                SendMessageClientRpc("잘못된 단계입니다.", clientId);
                return;
            }

            if (data.currentTier < tier)
            {
                SendMessageClientRpc("아직 해당 단계에 도달하지 않았습니다.", clientId);
                return;
            }

            if (data.claimedPremiumTiers.Contains(tier))
            {
                SendMessageClientRpc("이미 수령한 보상입니다.", clientId);
                return;
            }

            data.claimedPremiumTiers.Add(tier);
            var reward = premiumRewards[tier - 1];
            GrantReward(clientId, reward);

            RewardClaimedClientRpc(tier, true, reward.description, clientId);
        }

        /// <summary>
        /// 프리미엄 활성화 (골드 구매)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ActivatePremiumServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var data = GetOrCreateData(clientId);

            if (data.isPremium)
            {
                SendMessageClientRpc("이미 프리미엄 패스를 보유중입니다.", clientId);
                return;
            }

            int cost = 50000; // 프리미엄 패스 비용
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            if (statsData.Gold < cost)
            {
                SendMessageClientRpc($"프리미엄 패스 비용 {cost}G가 부족합니다.", clientId);
                return;
            }

            statsData.ChangeGold(-cost);
            data.isPremium = true;

            PremiumActivatedClientRpc(cost, clientId);
        }

        /// <summary>
        /// 미수령 보상 일괄 수령
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ClaimAllAvailableServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var data = GetOrCreateData(clientId);
            int claimed = 0;

            for (int t = 1; t <= data.currentTier; t++)
            {
                if (!data.claimedFreeTiers.Contains(t))
                {
                    data.claimedFreeTiers.Add(t);
                    GrantReward(clientId, freeRewards[t - 1]);
                    claimed++;
                }

                if (data.isPremium && !data.claimedPremiumTiers.Contains(t))
                {
                    data.claimedPremiumTiers.Add(t);
                    GrantReward(clientId, premiumRewards[t - 1]);
                    claimed++;
                }
            }

            if (claimed > 0)
                BulkClaimCompleteClientRpc(claimed, clientId);
            else
                SendMessageClientRpc("수령할 보상이 없습니다.", clientId);
        }

        /// <summary>
        /// 시즌 데이터 동기화 요청
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestSyncServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var data = GetOrCreateData(clientId);
            int expToNext = data.currentTier < maxTier ? GetExpForTier(data.currentTier + 1) : 0;

            SyncSeasonDataClientRpc(data.currentTier, data.currentExp, expToNext,
                data.isPremium, localSeasonNumber, clientId);

            // 수령 상태 동기화
            foreach (int t in data.claimedFreeTiers)
                SyncClaimedClientRpc(t, false, clientId);
            foreach (int t in data.claimedPremiumTiers)
                SyncClaimedClientRpc(t, true, clientId);

            SyncCompleteClientRpc(clientId);
        }

        /// <summary>
        /// 보상 정보 조회
        /// </summary>
        public SeasonReward GetFreeReward(int tier)
        {
            if (tier < 1 || tier > maxTier) return default;
            return freeRewards[tier - 1];
        }

        public SeasonReward GetPremiumReward(int tier)
        {
            if (tier < 1 || tier > maxTier) return default;
            return premiumRewards[tier - 1];
        }

        public bool IsFreeClaimed(int tier) => localClaimedFree.Contains(tier);
        public bool IsPremiumClaimed(int tier) => localClaimedPremium.Contains(tier);

        /// <summary>
        /// 단계별 필요 경험치 계산
        /// </summary>
        public int GetExpForTier(int tier)
        {
            return Mathf.RoundToInt(baseExpPerTier * Mathf.Pow(expScaling, tier - 1));
        }

        #region ClientRPCs

        [ClientRpc]
        private void SeasonExpGainedClientRpc(int amount, int newTier, int newExp, int expToNext,
            bool tieredUp, string source, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localTier = newTier;
            localExp = newExp;
            localExpToNext = expToNext;

            if (tieredUp)
            {
                OnTierUp?.Invoke(newTier);
                var notif = NotificationManager.Instance;
                if (notif != null)
                    notif.ShowNotification($"시즌패스 {newTier}단계 달성!", NotificationType.Achievement);
            }

            OnSeasonDataUpdated?.Invoke();
        }

        [ClientRpc]
        private void RewardClaimedClientRpc(int tier, bool isPremium, string desc, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            if (isPremium) localClaimedPremium.Add(tier);
            else localClaimedFree.Add(tier);

            OnSeasonDataUpdated?.Invoke();

            var notif = NotificationManager.Instance;
            if (notif != null)
            {
                string type = isPremium ? "<color=#FFD700>프리미엄</color>" : "무료";
                notif.ShowNotification($"시즌 보상 수령 ({type}): {desc}", NotificationType.System);
            }
        }

        [ClientRpc]
        private void PremiumActivatedClientRpc(int cost, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localIsPremium = true;
            OnSeasonDataUpdated?.Invoke();

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"프리미엄 패스 활성화! -{cost}G", NotificationType.Achievement);
        }

        [ClientRpc]
        private void BulkClaimCompleteClientRpc(int claimedCount, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            OnSeasonDataUpdated?.Invoke();

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"보상 {claimedCount}개 일괄 수령 완료!", NotificationType.System);
        }

        [ClientRpc]
        private void SyncSeasonDataClientRpc(int tier, int exp, int expToNext,
            bool isPremium, int seasonNum, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localTier = tier;
            localExp = exp;
            localExpToNext = expToNext;
            localIsPremium = isPremium;
            localSeasonNumber = seasonNum;
            localClaimedFree.Clear();
            localClaimedPremium.Clear();
        }

        [ClientRpc]
        private void SyncClaimedClientRpc(int tier, bool isPremium, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            if (isPremium) localClaimedPremium.Add(tier);
            else localClaimedFree.Add(tier);
        }

        [ClientRpc]
        private void SyncCompleteClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            OnSeasonDataUpdated?.Invoke();
        }

        [ClientRpc]
        private void SendMessageClientRpc(string message, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification(message, NotificationType.Warning);
        }

        #endregion

        #region Server Utility

        private void GrantReward(ulong clientId, SeasonReward reward)
        {
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            if (reward.gold > 0)
                statsData.ChangeGold(reward.gold);

            if (reward.exp > 0)
                statsData.AddExperience(reward.exp);

            if (!string.IsNullOrEmpty(reward.itemId))
            {
                var inventory = GetInventoryManager(clientId);
                if (inventory != null)
                {
                    var item = new ItemInstance();
                    item.Initialize(reward.itemId, reward.itemQuantity > 0 ? reward.itemQuantity : 1);
                    if (!inventory.AddItem(item))
                    {
                        // 인벤토리 가득 → 우편 발송
                        if (MailSystem.Instance != null)
                        {
                            var mailAttachment = new MailAttachment { itemId = reward.itemId, quantity = reward.itemQuantity > 0 ? reward.itemQuantity : 1 };
                            MailSystem.Instance.SendSystemMail(clientId, "시즌패스 보상",
                                $"인벤토리 부족으로 우편 발송: {reward.description}",
                                MailType.SystemReward, mailAttachment, true);
                        }
                    }
                }
            }

            // 칭호 보상
            if (!string.IsNullOrEmpty(reward.titleId))
            {
                if (TitleSystem.Instance != null)
                    TitleSystem.Instance.UnlockTitle(clientId, reward.titleId);
            }
        }

        private SeasonPlayerData GetOrCreateData(ulong clientId)
        {
            if (!playerSeasonData.ContainsKey(clientId))
                playerSeasonData[clientId] = new SeasonPlayerData();
            return playerSeasonData[clientId];
        }

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        private InventoryManager GetInventoryManager(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject?.GetComponent<InventoryManager>();
        }

        #endregion

        #region 보상 테이블 초기화

        private void InitializeRewards()
        {
            freeRewards = new SeasonReward[maxTier];
            premiumRewards = new SeasonReward[maxTier];

            for (int i = 0; i < maxTier; i++)
            {
                int tier = i + 1;
                // 무료 트랙
                freeRewards[i] = GenerateFreeReward(tier);
                // 프리미엄 트랙
                premiumRewards[i] = GeneratePremiumReward(tier);
            }
        }

        private SeasonReward GenerateFreeReward(int tier)
        {
            // 5단계마다 특별 보상, 나머지 골드/경험치
            if (tier == 50) return new SeasonReward(20000, 5000, null, 0, "season_champion", "시즌 챔피언 보상");
            if (tier % 10 == 0) return new SeasonReward(5000 * (tier / 10), 2000 * (tier / 10), null, 0, null, $"마일스톤 {tier}단계");
            if (tier % 5 == 0) return new SeasonReward(2000 * (tier / 5), 800 * (tier / 5), "consumable_healthpotion_large", 3, null, $"보급품 {tier}단계");

            int goldBase = 300 + tier * 50;
            int expBase = 100 + tier * 30;
            return new SeasonReward(goldBase, expBase, null, 0, null, $"골드 {goldBase} + 경험치 {expBase}");
        }

        private SeasonReward GeneratePremiumReward(int tier)
        {
            if (tier == 50) return new SeasonReward(50000, 10000, "consumable_resurrectionscroll", 5, "season_elite", "시즌 엘리트 보상");
            if (tier == 40) return new SeasonReward(30000, 8000, "consumable_protectionscroll", 5, null, "프리미엄 40단계");
            if (tier == 30) return new SeasonReward(20000, 6000, "consumable_speedscroll", 5, null, "프리미엄 30단계");
            if (tier == 20) return new SeasonReward(15000, 4000, "consumable_strengthscroll", 5, null, "프리미엄 20단계");
            if (tier == 10) return new SeasonReward(10000, 3000, "consumable_healthpotion_max", 3, null, "프리미엄 10단계");

            if (tier % 5 == 0)
                return new SeasonReward(3000 * (tier / 5), 1200 * (tier / 5), "consumable_manapotion_large", 2, null, $"프리미엄 보급품 {tier}단계");

            int goldBase = 500 + tier * 80;
            int expBase = 200 + tier * 50;
            return new SeasonReward(goldBase, expBase, null, 0, null, $"골드 {goldBase} + 경험치 {expBase}");
        }

        #endregion

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }

    /// <summary>
    /// 시즌 보상 데이터
    /// </summary>
    [System.Serializable]
    public struct SeasonReward
    {
        public int gold;
        public int exp;
        public string itemId;
        public int itemQuantity;
        public string titleId;
        public string description;

        public SeasonReward(int gold, int exp, string itemId, int itemQuantity, string titleId, string description)
        {
            this.gold = gold;
            this.exp = exp;
            this.itemId = itemId;
            this.itemQuantity = itemQuantity;
            this.titleId = titleId;
            this.description = description;
        }
    }

    /// <summary>
    /// 플레이어 시즌 데이터 (서버)
    /// </summary>
    public class SeasonPlayerData
    {
        public int currentTier;
        public int currentExp;
        public bool isPremium;
        public HashSet<int> claimedFreeTiers = new HashSet<int>();
        public HashSet<int> claimedPremiumTiers = new HashSet<int>();
    }
}
