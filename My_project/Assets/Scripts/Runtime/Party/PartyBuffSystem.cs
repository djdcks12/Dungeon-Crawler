using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 파티 버프 시스템
    /// 파티원 수에 따른 경험치/골드 보너스 + 직업 시너지
    /// </summary>
    public class PartyBuffSystem : NetworkBehaviour
    {
        public static PartyBuffSystem Instance { get; private set; }

        [Header("인원 보너스")]
        [SerializeField] private float expBonusPerMember = 0.10f;  // 인원당 10%
        [SerializeField] private float goldBonusPerMember = 0.05f; // 인원당 5%

        [Header("직업 시너지")]
        [SerializeField] private float synergyBonusPerUniqueJob = 0.05f; // 고유 직업당 5%

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 파티 경험치 보너스 배율 계산
        /// </summary>
        public float GetExpMultiplier(ulong clientId)
        {
            var partyManager = FindFirstObjectByType<PartyManager>();
            if (partyManager == null || !partyManager.HasParty(clientId))
                return 1f;

            var members = partyManager.GetPlayerPartyMembers(clientId);
            int memberCount = members.Count;

            if (memberCount <= 1) return 1f;

            // 인원 보너스
            float bonus = 1f + (memberCount - 1) * expBonusPerMember;

            // 직업 시너지
            bonus += GetJobSynergyBonus(members);

            return bonus;
        }

        /// <summary>
        /// 파티 골드 보너스 배율 계산
        /// </summary>
        public float GetGoldMultiplier(ulong clientId)
        {
            var partyManager = FindFirstObjectByType<PartyManager>();
            if (partyManager == null || !partyManager.HasParty(clientId))
                return 1f;

            var members = partyManager.GetPlayerPartyMembers(clientId);
            int memberCount = members.Count;

            if (memberCount <= 1) return 1f;

            return 1f + (memberCount - 1) * goldBonusPerMember;
        }

        /// <summary>
        /// 직업 시너지 보너스 계산
        /// </summary>
        private float GetJobSynergyBonus(List<PartyMember> members)
        {
            var uniqueJobs = new HashSet<JobType>();

            foreach (var member in members)
            {
                if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(member.clientId, out var client))
                    continue;
                if (client.PlayerObject == null) continue;

                var statsManager = client.PlayerObject.GetComponent<PlayerStatsManager>();
                if (statsManager != null)
                {
                    uniqueJobs.Add(statsManager.CurrentJobType);
                }
            }

            // 2개 이상의 고유 직업이 있으면 보너스
            int uniqueCount = uniqueJobs.Count;
            if (uniqueCount <= 1) return 0f;

            return (uniqueCount - 1) * synergyBonusPerUniqueJob;
        }

        /// <summary>
        /// 파티 버프 정보 텍스트
        /// </summary>
        public string GetBuffInfoText(ulong clientId)
        {
            float expMult = GetExpMultiplier(clientId);
            float goldMult = GetGoldMultiplier(clientId);

            if (expMult <= 1f && goldMult <= 1f)
                return "";

            string info = "<color=#00FFFF>파티 버프</color>\n";
            if (expMult > 1f)
                info += $"  EXP +{(expMult - 1f) * 100:F0}%\n";
            if (goldMult > 1f)
                info += $"  Gold +{(goldMult - 1f) * 100:F0}%\n";

            return info;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this)
                Instance = null;
        }
    }
}
