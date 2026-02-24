using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 파티 보상 분배 시스템
    /// 경험치/골드 균등 분배, 아이템 순차 분배
    /// </summary>
    public class PartyRewardDistributor : NetworkBehaviour
    {
        public static PartyRewardDistributor Instance { get; private set; }

        [Header("분배 설정")]
        [SerializeField] private float expShareRadius = 15f;
        [SerializeField] private float levelDiffPenaltyPerLevel = 0.05f; // 레벨 차이당 5% 패널티
        [SerializeField] private float maxLevelPenalty = 0.5f;           // 최대 50% 패널티

        [Header("파티 보너스")]
        [SerializeField] private float partyBonusPerMember = 0.1f; // 인원당 10% 보너스

        // 아이템 순차 분배용 인덱스
        private Dictionary<int, int> partyLootIndex = new Dictionary<int, int>();

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
        /// 몬스터 처치 보상 분배 (서버에서 호출)
        /// </summary>
        public void DistributeKillReward(ulong killerClientId, long baseExp, long baseGold, Vector3 killPosition)
        {
            if (!IsServer) return;

            var partyManager = FindFirstObjectByType<PartyManager>();
            if (partyManager == null || !partyManager.HasParty(killerClientId))
            {
                // 솔로 - 보상 전액 지급
                GiveRewardToPlayer(killerClientId, baseExp, baseGold);
                return;
            }

            // 파티 보상 분배
            var members = partyManager.GetPlayerPartyMembers(killerClientId);
            var nearbyMembers = GetNearbyMembers(members, killPosition);

            if (nearbyMembers.Count == 0)
            {
                GiveRewardToPlayer(killerClientId, baseExp, baseGold);
                return;
            }

            // 파티 보너스 계산
            float partyBonus = 1f + (nearbyMembers.Count - 1) * partyBonusPerMember;

            // 총 보상 (파티 보너스 적용)
            long totalExp = (long)(baseExp * partyBonus);
            long totalGold = (long)(baseGold * partyBonus);

            // 균등 분배
            long expPerMember = totalExp / nearbyMembers.Count;
            long goldPerMember = totalGold / nearbyMembers.Count;

            // 킬러 평균 레벨 계산
            float avgLevel = GetAverageLevel(nearbyMembers);

            foreach (var memberId in nearbyMembers)
            {
                // 레벨 차이 보정
                int memberLevel = GetPlayerLevel(memberId);
                float levelDiff = Mathf.Abs(memberLevel - avgLevel);
                float levelPenalty = Mathf.Min(levelDiff * levelDiffPenaltyPerLevel, maxLevelPenalty);
                float levelMultiplier = 1f - levelPenalty;

                long adjustedExp = (long)(expPerMember * levelMultiplier);
                long adjustedGold = goldPerMember; // 골드는 레벨 보정 없음

                GiveRewardToPlayer(memberId, adjustedExp, adjustedGold);
            }

            // 퀘스트 진행도 업데이트 (킬러만)
            if (QuestManager.Instance != null)
            {
                // 파티원 모두에게 퀘스트 처치 카운트 (주변 범위 내)
                foreach (var memberId in nearbyMembers)
                {
                    QuestManager.Instance.OnMonsterKilled(memberId, "", false);
                }
            }
        }

        /// <summary>
        /// 아이템 드롭 분배 (순차 분배)
        /// </summary>
        public ulong GetItemRecipient(ulong killerClientId, Vector3 dropPosition)
        {
            if (!IsServer) return killerClientId;

            var partyManager = FindFirstObjectByType<PartyManager>();
            if (partyManager == null || !partyManager.HasParty(killerClientId))
                return killerClientId;

            var party = partyManager.GetPlayerParty(killerClientId);
            if (!party.HasValue) return killerClientId;

            int partyId = party.Value.partyId;
            var members = partyManager.GetPlayerPartyMembers(killerClientId);
            var nearbyMembers = GetNearbyMembers(members, dropPosition);

            if (nearbyMembers.Count <= 1) return killerClientId;

            // 순차 분배
            if (!partyLootIndex.ContainsKey(partyId))
                partyLootIndex[partyId] = 0;

            int index = partyLootIndex[partyId] % nearbyMembers.Count;
            partyLootIndex[partyId]++;

            return nearbyMembers[index];
        }

        /// <summary>
        /// 보상 지급
        /// </summary>
        private void GiveRewardToPlayer(ulong clientId, long exp, long gold)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return;
            if (client.PlayerObject == null) return;

            var statsManager = client.PlayerObject.GetComponent<PlayerStatsManager>();
            if (statsManager == null) return;

            if (exp > 0) statsManager.AddExperience(exp);
            if (gold > 0) statsManager.ChangeGold(gold);
        }

        /// <summary>
        /// 범위 내 파티원 가져오기
        /// </summary>
        private List<ulong> GetNearbyMembers(List<PartyMember> members, Vector3 position)
        {
            var nearby = new List<ulong>();

            foreach (var member in members)
            {
                if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(member.clientId, out var client))
                    continue;
                if (client.PlayerObject == null) continue;

                float dist = Vector3.Distance(client.PlayerObject.transform.position, position);
                if (dist <= expShareRadius)
                {
                    nearby.Add(member.clientId);
                }
            }

            return nearby;
        }

        /// <summary>
        /// 플레이어 레벨 가져오기
        /// </summary>
        private int GetPlayerLevel(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return 1;
            if (client.PlayerObject == null) return 1;

            var stats = client.PlayerObject.GetComponent<PlayerStatsManager>();
            return stats?.CurrentStats?.CurrentLevel ?? 1;
        }

        /// <summary>
        /// 평균 레벨 계산
        /// </summary>
        private float GetAverageLevel(List<ulong> memberIds)
        {
            if (memberIds.Count == 0) return 1f;

            float total = 0;
            foreach (var id in memberIds)
                total += GetPlayerLevel(id);

            return total / memberIds.Count;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this)
                Instance = null;
        }
    }
}
