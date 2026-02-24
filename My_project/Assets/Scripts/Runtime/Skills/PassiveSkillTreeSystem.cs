using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 패시브 스킬 트리 시스템
    /// 종족별 패시브 트리, 레벨업 시 포인트 획득, 노드 투자
    /// 서버 권위적
    /// </summary>
    public class PassiveSkillTreeSystem : NetworkBehaviour
    {
        public static PassiveSkillTreeSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int pointsPerLevel = 1;          // 레벨당 패시브 포인트
        [SerializeField] private int bonusPointLevelInterval = 5;  // 보너스 포인트 레벨 간격
        [SerializeField] private int resetCostBase = 500;          // 초기화 비용 (골드)
        [SerializeField] private float resetCostMultiplier = 1.5f; // 초기화 비용 배율 (횟수당)

        // 모든 패시브 노드
        private PassiveSkillTreeData[] allNodes;

        // 종족별 노드 캐시
        private Dictionary<Race, List<PassiveSkillTreeData>> nodesByRace = new Dictionary<Race, List<PassiveSkillTreeData>>();

        // 플레이어별 활성화된 노드
        private Dictionary<ulong, HashSet<string>> activatedNodes = new Dictionary<ulong, HashSet<string>>();

        // 플레이어별 사용 가능한 포인트
        private Dictionary<ulong, int> availablePoints = new Dictionary<ulong, int>();

        // 플레이어별 초기화 횟수
        private Dictionary<ulong, int> resetCounts = new Dictionary<ulong, int>();

        // 로컬 데이터
        private HashSet<string> localActivatedNodes = new HashSet<string>();
        private int localAvailablePoints;

        // 이벤트
        public System.Action<string> OnNodeActivated; // nodeId
        public System.Action<int> OnPointsChanged; // availablePoints
        public System.Action OnTreeReset;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            LoadNodeData();
        }

        private void LoadNodeData()
        {
            allNodes = Resources.LoadAll<PassiveSkillTreeData>("ScriptableObjects/PassiveNodes");
            Debug.Log($"[PassiveTree] {allNodes.Length}개 패시브 노드 로드됨");

            // 종족별 분류
            nodesByRace.Clear();
            foreach (var node in allNodes)
            {
                if (node == null) continue;
                if (!nodesByRace.ContainsKey(node.RequiredRace))
                    nodesByRace[node.RequiredRace] = new List<PassiveSkillTreeData>();
                nodesByRace[node.RequiredRace].Add(node);
            }
        }

        /// <summary>
        /// 레벨업 시 패시브 포인트 부여
        /// </summary>
        public void GrantPointsForLevelUp(ulong clientId, int newLevel)
        {
            if (!IsServer) return;

            int points = pointsPerLevel;
            // 보너스 레벨 (5의 배수)
            if (newLevel % bonusPointLevelInterval == 0)
                points += 1;

            if (!availablePoints.ContainsKey(clientId))
                availablePoints[clientId] = 0;

            availablePoints[clientId] += points;
            NotifyPointsChangedClientRpc(availablePoints[clientId], clientId);
        }

        /// <summary>
        /// 패시브 노드 활성화
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ActivateNodeServerRpc(string nodeId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            // 노드 찾기
            var node = FindNode(nodeId);
            if (node == null)
            {
                NotifyMessageClientRpc("존재하지 않는 노드입니다.", clientId);
                return;
            }

            // 이미 활성화 체크
            var activated = GetOrCreateActivatedSet(clientId);
            if (activated.Contains(nodeId))
            {
                NotifyMessageClientRpc("이미 활성화된 노드입니다.", clientId);
                return;
            }

            // 포인트 체크
            int points = availablePoints.ContainsKey(clientId) ? availablePoints[clientId] : 0;
            if (points < node.PointCost)
            {
                NotifyMessageClientRpc("패시브 포인트가 부족합니다.", clientId);
                return;
            }

            // 플레이어 레벨/종족 체크
            var player = GetPlayerByClientId(clientId);
            if (player == null) return;
            var statsData = player.GetComponent<PlayerStatsManager>()?.CurrentStats;
            if (statsData == null) return;

            if (statsData.CurrentLevel < node.RequiredLevel)
            {
                NotifyMessageClientRpc($"레벨 {node.RequiredLevel} 이상 필요합니다.", clientId);
                return;
            }

            if (statsData.CharacterRace != node.RequiredRace)
            {
                NotifyMessageClientRpc("해당 종족 전용 노드입니다.", clientId);
                return;
            }

            // 선행 노드 체크
            if (node.PrerequisiteNodeIds != null)
            {
                foreach (var prereqId in node.PrerequisiteNodeIds)
                {
                    if (!string.IsNullOrEmpty(prereqId) && !activated.Contains(prereqId))
                    {
                        NotifyMessageClientRpc("선행 노드를 먼저 활성화해야 합니다.", clientId);
                        return;
                    }
                }
            }

            // 활성화
            activated.Add(nodeId);
            availablePoints[clientId] -= node.PointCost;

            // 스탯 적용
            ApplyNodeStats(statsData, node, true);

            NotifyNodeActivatedClientRpc(nodeId, availablePoints[clientId], clientId);
        }

        /// <summary>
        /// 트리 초기화 (포인트 환불)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ResetTreeServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            var player = GetPlayerByClientId(clientId);
            if (player == null) return;
            var statsData = player.GetComponent<PlayerStatsManager>()?.CurrentStats;
            if (statsData == null) return;

            // 초기화 비용 계산
            int resets = resetCounts.ContainsKey(clientId) ? resetCounts[clientId] : 0;
            long cost = (long)(resetCostBase * Mathf.Pow(resetCostMultiplier, resets));

            if (statsData.Gold < cost)
            {
                NotifyMessageClientRpc($"초기화 비용 {cost}G가 부족합니다.", clientId);
                return;
            }

            // 활성 노드 비활성화 및 스탯 복원
            var activated = GetOrCreateActivatedSet(clientId);
            int refundPoints = 0;

            foreach (var nodeId in activated)
            {
                var node = FindNode(nodeId);
                if (node != null)
                {
                    ApplyNodeStats(statsData, node, false);
                    refundPoints += node.PointCost;
                }
            }

            // 초기화
            activated.Clear();
            statsData.ChangeGold(-cost);

            if (!availablePoints.ContainsKey(clientId))
                availablePoints[clientId] = 0;
            availablePoints[clientId] += refundPoints;

            if (!resetCounts.ContainsKey(clientId))
                resetCounts[clientId] = 0;
            resetCounts[clientId]++;

            NotifyTreeResetClientRpc(availablePoints[clientId], clientId);
        }

        /// <summary>
        /// 노드 스탯을 플레이어에 적용/해제
        /// </summary>
        private void ApplyNodeStats(PlayerStatsData statsData, PassiveSkillTreeData node, bool apply)
        {
            int sign = apply ? 1 : -1;
            var bonus = node.StatBonus;

            // 기본 스탯 보너스 (StatBlock은 직접 수정 불가하므로 별도 관리 필요)
            // 여기서는 간단히 HP/MP만 직접 적용
            if (node.HPBonus != 0)
                statsData.ChangeHP(Mathf.RoundToInt(node.HPBonus * sign));

            // 키스톤 효과는 별도 시스템에서 참조
        }

        /// <summary>
        /// 활성 패시브 노드에서 총 스탯 보너스 계산 (외부 참조용)
        /// </summary>
        public PassiveTreeBonuses GetTotalBonuses(ulong clientId)
        {
            var bonuses = new PassiveTreeBonuses();
            var activated = GetOrCreateActivatedSet(clientId);

            foreach (var nodeId in activated)
            {
                var node = FindNode(nodeId);
                if (node == null) continue;

                bonuses.statBonus.strength += node.StatBonus.strength;
                bonuses.statBonus.agility += node.StatBonus.agility;
                bonuses.statBonus.vitality += node.StatBonus.vitality;
                bonuses.statBonus.intelligence += node.StatBonus.intelligence;
                bonuses.statBonus.defense += node.StatBonus.defense;
                bonuses.statBonus.magicDefense += node.StatBonus.magicDefense;
                bonuses.statBonus.luck += node.StatBonus.luck;
                bonuses.statBonus.stability += node.StatBonus.stability;

                bonuses.hpBonus += node.HPBonus;
                bonuses.mpBonus += node.MPBonus;
                bonuses.hpPercentBonus += node.HPPercentBonus;
                bonuses.mpPercentBonus += node.MPPercentBonus;
                bonuses.critChanceBonus += node.CritChanceBonus;
                bonuses.critDamageBonus += node.CritDamageBonus;
                bonuses.moveSpeedBonus += node.MoveSpeedBonus;
                bonuses.attackSpeedBonus += node.AttackSpeedBonus;
                bonuses.cooldownReduction += node.CooldownReduction;
                bonuses.lifestealPercent += node.LifestealPercent;
                bonuses.manaRegenBonus += node.ManaRegenBonus;

                // 키스톤 타입 수집
                if (node.IsKeystone && node.KeystoneType != PassiveKeystoneType.None)
                {
                    bonuses.activeKeystones.Add(node.KeystoneType);
                }
            }

            return bonuses;
        }

        #region Network RPCs

        [ClientRpc]
        private void NotifyNodeActivatedClientRpc(string nodeId, int remainingPoints, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            localActivatedNodes.Add(nodeId);
            localAvailablePoints = remainingPoints;
            OnNodeActivated?.Invoke(nodeId);
            OnPointsChanged?.Invoke(remainingPoints);

            var node = FindNode(nodeId);
            var notif = NotificationManager.Instance;
            if (notif != null && node != null)
            {
                notif.ShowNotification($"패시브 활성화: {node.NodeName}", NotificationType.System);
            }
        }

        [ClientRpc]
        private void NotifyPointsChangedClientRpc(int points, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localAvailablePoints = points;
            OnPointsChanged?.Invoke(points);
        }

        [ClientRpc]
        private void NotifyTreeResetClientRpc(int points, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            localActivatedNodes.Clear();
            localAvailablePoints = points;
            OnTreeReset?.Invoke();
            OnPointsChanged?.Invoke(points);

            var notif = NotificationManager.Instance;
            if (notif != null)
            {
                notif.ShowNotification("패시브 트리가 초기화되었습니다.", NotificationType.System);
            }
        }

        [ClientRpc]
        private void NotifyMessageClientRpc(string message, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            var notif = NotificationManager.Instance;
            if (notif != null)
            {
                notif.ShowNotification(message, NotificationType.Warning);
            }
        }

        #endregion

        #region Utility

        private PassiveSkillTreeData FindNode(string nodeId)
        {
            if (allNodes == null) return null;
            foreach (var node in allNodes)
            {
                if (node.NodeId == nodeId) return node;
            }
            return null;
        }

        private HashSet<string> GetOrCreateActivatedSet(ulong clientId)
        {
            if (!activatedNodes.ContainsKey(clientId))
                activatedNodes[clientId] = new HashSet<string>();
            return activatedNodes[clientId];
        }

        private PlayerStatsManager GetPlayerByClientId(ulong clientId)
        {
            var players = FindObjectsByType<PlayerStatsManager>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p.OwnerClientId == clientId) return p;
            }
            return null;
        }

        // 로컬 접근자
        public IReadOnlyCollection<string> LocalActivatedNodes => localActivatedNodes;
        public int LocalAvailablePoints => localAvailablePoints;
        public bool IsNodeActivated(string nodeId) => localActivatedNodes.Contains(nodeId);

        /// <summary>
        /// 종족별 패시브 노드 목록 (UI용)
        /// </summary>
        public List<PassiveSkillTreeData> GetNodesForRace(Race race)
        {
            return nodesByRace.ContainsKey(race) ? nodesByRace[race] : new List<PassiveSkillTreeData>();
        }

        public PassiveSkillTreeData[] AllNodes => allNodes;

        #endregion

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this)
                Instance = null;
        }
    }

    /// <summary>
    /// 패시브 트리 총 보너스 (외부 시스템 참조용)
    /// </summary>
    public struct PassiveTreeBonuses
    {
        public StatBlock statBonus;
        public float hpBonus;
        public float mpBonus;
        public float hpPercentBonus;
        public float mpPercentBonus;
        public float critChanceBonus;
        public float critDamageBonus;
        public float moveSpeedBonus;
        public float attackSpeedBonus;
        public float cooldownReduction;
        public float lifestealPercent;
        public float manaRegenBonus;
        public List<PassiveKeystoneType> activeKeystones;

        public PassiveTreeBonuses(bool init)
        {
            statBonus = default;
            hpBonus = 0; mpBonus = 0;
            hpPercentBonus = 0; mpPercentBonus = 0;
            critChanceBonus = 0; critDamageBonus = 0;
            moveSpeedBonus = 0; attackSpeedBonus = 0;
            cooldownReduction = 0; lifestealPercent = 0;
            manaRegenBonus = 0;
            activeKeystones = new List<PassiveKeystoneType>();
        }
    }
}
