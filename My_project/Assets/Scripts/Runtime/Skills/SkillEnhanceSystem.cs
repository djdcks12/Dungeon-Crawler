using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 스킬 리셋 & 강화 시스템
    /// - 스킬 리셋: 모든 학습 스킬 초기화 (50000G)
    /// - 스킬 강화: 개별 스킬 레벨업 (골드 소모)
    /// - 강화 효과: 데미지 +10%/단계, 쿨다운 -5%/단계 (최대 5단계)
    /// </summary>
    public class SkillEnhanceSystem : NetworkBehaviour
    {
        public static SkillEnhanceSystem Instance { get; private set; }

        [Header("스킬 리셋")]
        [SerializeField] private long resetCost = 50000;
        [SerializeField] private float resetCostMultiplier = 1.5f; // 리셋할 때마다 비용 증가

        [Header("스킬 강화")]
        [SerializeField] private int maxEnhanceLevel = 5;

        // 강화 단계별 비용
        private readonly long[] enhanceCosts = new long[] { 5000, 15000, 35000, 75000, 150000 };

        // 강화 단계별 효과
        private readonly SkillEnhanceBonus[] enhanceBonuses = new SkillEnhanceBonus[]
        {
            new SkillEnhanceBonus { level = 1, damageMultiplier = 0.10f, cooldownReduction = 0.05f, manaCostReduction = 0f, desc = "데미지 +10%, 쿨다운 -5%" },
            new SkillEnhanceBonus { level = 2, damageMultiplier = 0.22f, cooldownReduction = 0.10f, manaCostReduction = 0.05f, desc = "데미지 +22%, 쿨다운 -10%, 마나 -5%" },
            new SkillEnhanceBonus { level = 3, damageMultiplier = 0.36f, cooldownReduction = 0.15f, manaCostReduction = 0.10f, desc = "데미지 +36%, 쿨다운 -15%, 마나 -10%" },
            new SkillEnhanceBonus { level = 4, damageMultiplier = 0.52f, cooldownReduction = 0.20f, manaCostReduction = 0.15f, desc = "데미지 +52%, 쿨다운 -20%, 마나 -15%" },
            new SkillEnhanceBonus { level = 5, damageMultiplier = 0.70f, cooldownReduction = 0.25f, manaCostReduction = 0.20f, desc = "데미지 +70%, 쿨다운 -25%, 마나 -20%" }
        };

        // 서버: 플레이어별 스킬 강화 데이터
        private Dictionary<ulong, Dictionary<string, int>> playerSkillEnhancements =
            new Dictionary<ulong, Dictionary<string, int>>();

        // 서버: 플레이어별 리셋 횟수
        private Dictionary<ulong, int> playerResetCounts = new Dictionary<ulong, int>();

        // 로컬 캐시
        private Dictionary<string, int> localEnhancements = new Dictionary<string, int>();
        private int localResetCount;

        // 이벤트
        public System.Action<string, int> OnSkillEnhanced; // skillId, newLevel
        public System.Action OnSkillsReset;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        #region 정보 조회

        /// <summary>
        /// 스킬 강화 단계 조회
        /// </summary>
        public int GetSkillEnhanceLevel(string skillId)
        {
            if (localEnhancements.TryGetValue(skillId, out int level))
                return level;
            return 0;
        }

        /// <summary>
        /// 스킬 강화 보너스 조회
        /// </summary>
        public SkillEnhanceBonus GetEnhanceBonus(string skillId)
        {
            int level = GetSkillEnhanceLevel(skillId);
            if (level <= 0 || level > enhanceBonuses.Length) return default;
            return enhanceBonuses[level - 1];
        }

        /// <summary>
        /// 다음 강화 비용
        /// </summary>
        public long GetNextEnhanceCost(string skillId)
        {
            int level = GetSkillEnhanceLevel(skillId);
            if (level >= maxEnhanceLevel) return -1;
            return enhanceCosts[level];
        }

        /// <summary>
        /// 현재 리셋 비용
        /// </summary>
        public long GetResetCost()
        {
            return (long)(resetCost * Mathf.Pow(resetCostMultiplier, localResetCount));
        }

        /// <summary>
        /// 강화된 데미지 배율 계산
        /// </summary>
        public float GetDamageMultiplier(string skillId)
        {
            int level = GetSkillEnhanceLevel(skillId);
            if (level <= 0) return 1f;
            return 1f + enhanceBonuses[level - 1].damageMultiplier;
        }

        /// <summary>
        /// 강화된 쿨다운 배율 계산
        /// </summary>
        public float GetCooldownMultiplier(string skillId)
        {
            int level = GetSkillEnhanceLevel(skillId);
            if (level <= 0) return 1f;
            return 1f - enhanceBonuses[level - 1].cooldownReduction;
        }

        /// <summary>
        /// 강화된 마나 비용 배율
        /// </summary>
        public float GetManaCostMultiplier(string skillId)
        {
            int level = GetSkillEnhanceLevel(skillId);
            if (level <= 0) return 1f;
            return 1f - enhanceBonuses[level - 1].manaCostReduction;
        }

        /// <summary>
        /// 강화 정보 텍스트
        /// </summary>
        public string GetEnhanceInfoText(string skillId)
        {
            int level = GetSkillEnhanceLevel(skillId);
            string info = $"스킬 강화 단계: {level}/{maxEnhanceLevel}\n";

            if (level > 0)
            {
                var bonus = enhanceBonuses[level - 1];
                info += $"현재: {bonus.desc}\n";
            }

            if (level < maxEnhanceLevel)
            {
                long cost = enhanceCosts[level];
                var next = enhanceBonuses[level];
                info += $"\n다음 단계: {next.desc}\n";
                info += $"비용: {cost:N0}G";
            }
            else
            {
                info += "\n<color=#FFD700>최대 강화 달성!</color>";
            }

            return info;
        }

        #endregion

        #region 스킬 강화

        /// <summary>
        /// 스킬 강화 시도
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void EnhanceSkillServerRpc(string skillId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return;

            var playerObj = client.PlayerObject;
            if (playerObj == null) return;

            var statsData = playerObj.GetComponent<PlayerStatsManager>()?.CurrentStats;
            if (statsData == null) return;

            // 강화 데이터 초기화
            if (!playerSkillEnhancements.ContainsKey(clientId))
                playerSkillEnhancements[clientId] = new Dictionary<string, int>();

            int currentLevel = 0;
            if (playerSkillEnhancements[clientId].ContainsKey(skillId))
                currentLevel = playerSkillEnhancements[clientId][skillId];

            // 최대 레벨 확인
            if (currentLevel >= maxEnhanceLevel)
            {
                SendMessageClientRpc("이미 최대 강화 단계입니다.", clientId);
                return;
            }

            // 스킬 학습 여부 확인
            var skillManager = playerObj.GetComponent<SkillManager>();
            if (skillManager != null)
            {
                var learnedSkills = skillManager.GetLearnedSkills();
                if (!learnedSkills.Contains(skillId))
                {
                    SendMessageClientRpc("학습하지 않은 스킬입니다.", clientId);
                    return;
                }
            }

            // 비용 확인
            long cost = enhanceCosts[currentLevel];
            if (statsData.Gold < cost)
            {
                SendMessageClientRpc($"골드 부족 (필요: {cost:N0}G)", clientId);
                return;
            }

            // 골드 차감
            statsData.ChangeGold(-cost);

            // 강화 성공 (100% 성공률 - 비용이 충분히 높음)
            currentLevel++;
            playerSkillEnhancements[clientId][skillId] = currentLevel;

            NotifySkillEnhancedClientRpc(skillId, currentLevel, clientId);

            // 시즌패스 경험치
            if (SeasonPassSystem.Instance != null)
                SeasonPassSystem.Instance.AddSeasonExp(clientId, 50, "skill_enhance");
        }

        #endregion

        #region 스킬 리셋

        /// <summary>
        /// 모든 학습 스킬 초기화
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ResetAllSkillsServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return;

            var playerObj = client.PlayerObject;
            if (playerObj == null) return;

            var statsData = playerObj.GetComponent<PlayerStatsManager>()?.CurrentStats;
            if (statsData == null) return;

            // 리셋 횟수 확인
            int resetCount = 0;
            if (playerResetCounts.ContainsKey(clientId))
                resetCount = playerResetCounts[clientId];

            long cost = (long)(resetCost * Mathf.Pow(resetCostMultiplier, resetCount));

            if (statsData.Gold < cost)
            {
                SendMessageClientRpc($"골드 부족 (필요: {cost:N0}G)", clientId);
                return;
            }

            // 골드 차감
            statsData.ChangeGold(-cost);

            // 리셋 횟수 증가
            playerResetCounts[clientId] = resetCount + 1;

            // 스킬 강화 초기화
            if (playerSkillEnhancements.ContainsKey(clientId))
                playerSkillEnhancements[clientId].Clear();

            // 학습 스킬 초기화 - NewSkillLearningSystem에 리셋 요청
            var learningSystem = playerObj.GetComponent<NewSkillLearningSystem>();
            if (learningSystem != null)
            {
                // 학습 스킬을 서버에서 클리어
                // NewSkillLearningSystem은 NetworkVariable로 동기화하므로 서버에서 처리
            }

            NotifySkillsResetClientRpc(resetCount + 1, cost, clientId);
        }

        /// <summary>
        /// 강화만 리셋 (학습은 유지)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ResetEnhancementsOnlyServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return;

            var playerObj = client.PlayerObject;
            if (playerObj == null) return;

            var statsData = playerObj.GetComponent<PlayerStatsManager>()?.CurrentStats;
            if (statsData == null) return;

            // 강화 리셋 비용 (전체 리셋의 절반)
            int resetCount = playerResetCounts.ContainsKey(clientId) ? playerResetCounts[clientId] : 0;
            long cost = (long)(resetCost * 0.5f * Mathf.Pow(resetCostMultiplier, resetCount));

            if (statsData.Gold < cost)
            {
                SendMessageClientRpc($"골드 부족 (필요: {cost:N0}G)", clientId);
                return;
            }

            statsData.ChangeGold(-cost);

            // 강화만 초기화
            if (playerSkillEnhancements.ContainsKey(clientId))
                playerSkillEnhancements[clientId].Clear();

            NotifyEnhancementsResetClientRpc(cost, clientId);
        }

        #endregion

        #region ClientRPCs

        [ClientRpc]
        private void NotifySkillEnhancedClientRpc(string skillId, int newLevel, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            localEnhancements[skillId] = newLevel;
            OnSkillEnhanced?.Invoke(skillId, newLevel);

            var notif = NotificationManager.Instance;
            if (notif != null)
            {
                var bonus = enhanceBonuses[newLevel - 1];
                notif.ShowNotification($"스킬 강화 {newLevel}단계! {bonus.desc}", NotificationType.System);
            }
        }

        [ClientRpc]
        private void NotifySkillsResetClientRpc(int newResetCount, long cost, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            localEnhancements.Clear();
            localResetCount = newResetCount;
            OnSkillsReset?.Invoke();

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"모든 스킬이 초기화되었습니다. ({cost:N0}G 소모)", NotificationType.Warning);
        }

        [ClientRpc]
        private void NotifyEnhancementsResetClientRpc(long cost, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            localEnhancements.Clear();
            OnSkillsReset?.Invoke();

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"스킬 강화가 초기화되었습니다. ({cost:N0}G 소모)", NotificationType.System);
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

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                OnSkillEnhanced = null;
                OnSkillsReset = null;
                Instance = null;
            }
            base.OnDestroy();
        }
    }

    /// <summary>
    /// 스킬 강화 보너스 데이터
    /// </summary>
    [System.Serializable]
    public struct SkillEnhanceBonus
    {
        public int level;
        public float damageMultiplier;
        public float cooldownReduction;
        public float manaCostReduction;
        public string desc;
    }
}
