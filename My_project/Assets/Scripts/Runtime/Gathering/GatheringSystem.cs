using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public enum GatheringNodeType
    {
        Ore,        // 광석
        Herb,       // 약초
        Wood,       // 나무
        Fishing     // 낚시 포인트
    }

    /// <summary>
    /// 채집 노드 데이터
    /// </summary>
    [System.Serializable]
    public struct GatheringNodeData
    {
        public string nodeId;
        public string nodeName;
        public GatheringNodeType nodeType;
        public int requiredLevel;       // 채집 레벨 요구
        public float gatherTime;        // 채집 시간 (초)
        public string[] possibleItems;  // 얻을 수 있는 아이템 ID
        public float[] itemChances;     // 아이템별 확률
        public int expReward;           // 채집 경험치
        public float respawnTime;       // 리스폰 시간 (초)
    }

    /// <summary>
    /// 채집 시스템 - 서버 권위적
    /// 채집 노드 상호작용, 레벨/경험치, 재료 아이템 획득
    /// </summary>
    public class GatheringSystem : NetworkBehaviour
    {
        public static GatheringSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int maxGatheringLevel = 50;
        [SerializeField] private float interactRange = 2f;

        // 채집 레벨별 필요 경험치
        private readonly int[] levelExpReqs = {
            0, 50, 120, 210, 320, 450, 600, 780, 1000, 1260,
            1560, 1900, 2300, 2760, 3280, 3870, 4530, 5270, 6100, 7020,
            8040, 9170, 10420, 11800, 13320, 14990, 16830, 18850, 21060, 23480,
            26120, 29000, 32130, 35530, 39220, 43220, 47560, 52260, 57340, 62840,
            68780, 75200, 82120, 89580, 97620, 106280, 115600, 125620, 136400, 148000
        };

        // 노드 정의
        private GatheringNodeData[] nodeDefinitions;

        // 서버: 플레이어 채집 레벨
        private Dictionary<ulong, int> playerGatherLevel = new Dictionary<ulong, int>();
        private Dictionary<ulong, int> playerGatherExp = new Dictionary<ulong, int>();

        // 서버: 노드 상태 (instanceId → 리스폰 시간)
        private Dictionary<int, float> depletedNodes = new Dictionary<int, float>();

        // 서버: 진행 중인 채집
        private Dictionary<ulong, (int nodeInstanceId, string nodeId, float startTime, float duration)> gatheringInProgress =
            new Dictionary<ulong, (int, string, float, float)>();

        // 로컬
        private int localGatherLevel;
        private int localGatherExp;
        private bool localIsGathering;

        // 이벤트
        public System.Action<int, int> OnGatherLevelChanged; // level, exp
        public System.Action<string, int> OnItemGathered;    // itemId, quantity
        public System.Action OnGatheringStarted;
        public System.Action OnGatheringCompleted;
        public System.Action OnGatheringCancelled;

        // 접근자
        public int LocalGatherLevel => localGatherLevel;
        public int LocalGatherExp => localGatherExp;
        public bool IsGathering => localIsGathering;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeNodeDefinitions();
        }

        private void Update()
        {
            if (!IsServer) return;
            ProcessGatheringProgress();
            ProcessNodeRespawns();
        }

        private void InitializeNodeDefinitions()
        {
            nodeDefinitions = new GatheringNodeData[]
            {
                // 광석
                new GatheringNodeData { nodeId = "node_copper", nodeName = "구리 광맥", nodeType = GatheringNodeType.Ore,
                    requiredLevel = 1, gatherTime = 3f, possibleItems = new[] { "mat_iron_ore", "mat_crystal" },
                    itemChances = new[] { 0.8f, 0.2f }, expReward = 10, respawnTime = 30f },
                new GatheringNodeData { nodeId = "node_iron", nodeName = "철 광맥", nodeType = GatheringNodeType.Ore,
                    requiredLevel = 10, gatherTime = 5f, possibleItems = new[] { "mat_iron_ore", "mat_steel_ingot", "mat_crystal" },
                    itemChances = new[] { 0.6f, 0.3f, 0.1f }, expReward = 25, respawnTime = 60f },
                new GatheringNodeData { nodeId = "node_mithril", nodeName = "미스릴 광맥", nodeType = GatheringNodeType.Ore,
                    requiredLevel = 25, gatherTime = 8f, possibleItems = new[] { "mat_mithril_ore", "mat_crystal", "mat_moonstone" },
                    itemChances = new[] { 0.5f, 0.3f, 0.2f }, expReward = 50, respawnTime = 120f },
                new GatheringNodeData { nodeId = "node_dragon", nodeName = "용의 광맥", nodeType = GatheringNodeType.Ore,
                    requiredLevel = 40, gatherTime = 12f, possibleItems = new[] { "mat_dragon_scale", "mat_mithril_ore", "mat_elemental_core" },
                    itemChances = new[] { 0.4f, 0.4f, 0.2f }, expReward = 100, respawnTime = 300f },

                // 약초
                new GatheringNodeData { nodeId = "node_herb_basic", nodeName = "풀꽃", nodeType = GatheringNodeType.Herb,
                    requiredLevel = 1, gatherTime = 2f, possibleItems = new[] { "mat_herb" },
                    itemChances = new[] { 1f }, expReward = 8, respawnTime = 20f },
                new GatheringNodeData { nodeId = "node_herb_poison", nodeName = "독초", nodeType = GatheringNodeType.Herb,
                    requiredLevel = 8, gatherTime = 3f, possibleItems = new[] { "mat_poison_gland", "mat_herb" },
                    itemChances = new[] { 0.6f, 0.4f }, expReward = 20, respawnTime = 45f },
                new GatheringNodeData { nodeId = "node_herb_rare", nodeName = "영지버섯", nodeType = GatheringNodeType.Herb,
                    requiredLevel = 20, gatherTime = 5f, possibleItems = new[] { "mat_herb", "mat_ancient_rune" },
                    itemChances = new[] { 0.7f, 0.3f }, expReward = 40, respawnTime = 90f },
                new GatheringNodeData { nodeId = "node_herb_legendary", nodeName = "세계수 잎", nodeType = GatheringNodeType.Herb,
                    requiredLevel = 35, gatherTime = 10f, possibleItems = new[] { "mat_ancient_rune", "mat_soul_fragment", "mat_moonstone" },
                    itemChances = new[] { 0.4f, 0.3f, 0.3f }, expReward = 80, respawnTime = 240f },

                // 나무
                new GatheringNodeData { nodeId = "node_wood_basic", nodeName = "참나무", nodeType = GatheringNodeType.Wood,
                    requiredLevel = 1, gatherTime = 3f, possibleItems = new[] { "mat_wood" },
                    itemChances = new[] { 1f }, expReward = 10, respawnTime = 25f },
                new GatheringNodeData { nodeId = "node_wood_hard", nodeName = "흑단나무", nodeType = GatheringNodeType.Wood,
                    requiredLevel = 15, gatherTime = 6f, possibleItems = new[] { "mat_wood", "mat_leather" },
                    itemChances = new[] { 0.7f, 0.3f }, expReward = 30, respawnTime = 70f },
                new GatheringNodeData { nodeId = "node_wood_ancient", nodeName = "고대 나무", nodeType = GatheringNodeType.Wood,
                    requiredLevel = 30, gatherTime = 9f, possibleItems = new[] { "mat_wood", "mat_enchant_dust", "mat_ancient_rune" },
                    itemChances = new[] { 0.5f, 0.3f, 0.2f }, expReward = 60, respawnTime = 180f },

                // 낚시
                new GatheringNodeData { nodeId = "node_fish_pond", nodeName = "연못", nodeType = GatheringNodeType.Fishing,
                    requiredLevel = 1, gatherTime = 5f, possibleItems = new[] { "mat_bone", "mat_cloth" },
                    itemChances = new[] { 0.6f, 0.4f }, expReward = 15, respawnTime = 15f },
                new GatheringNodeData { nodeId = "node_fish_river", nodeName = "강", nodeType = GatheringNodeType.Fishing,
                    requiredLevel = 12, gatherTime = 7f, possibleItems = new[] { "mat_bone", "mat_crystal", "mat_iron_ore" },
                    itemChances = new[] { 0.4f, 0.35f, 0.25f }, expReward = 30, respawnTime = 30f },
                new GatheringNodeData { nodeId = "node_fish_ocean", nodeName = "깊은 바다", nodeType = GatheringNodeType.Fishing,
                    requiredLevel = 28, gatherTime = 10f, possibleItems = new[] { "mat_crystal", "mat_moonstone", "mat_elemental_core" },
                    itemChances = new[] { 0.4f, 0.35f, 0.25f }, expReward = 55, respawnTime = 60f },
            };
        }

        #region 채집 진행

        /// <summary>
        /// 채집 시작
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void StartGatheringServerRpc(int nodeInstanceId, string nodeId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            // 이미 채집 중이면 거부
            if (gatheringInProgress.ContainsKey(clientId))
            {
                SendMessageClientRpc("이미 채집 중입니다.", clientId);
                return;
            }

            // 노드가 고갈 상태인지 확인
            if (depletedNodes.ContainsKey(nodeInstanceId))
            {
                SendMessageClientRpc("이 채집 노드는 고갈되었습니다.", clientId);
                return;
            }

            // 노드 정보 찾기
            GatheringNodeData? nodeData = null;
            foreach (var n in nodeDefinitions)
            {
                if (n.nodeId == nodeId) { nodeData = n; break; }
            }

            if (nodeData == null)
            {
                SendMessageClientRpc("유효하지 않은 채집 노드입니다.", clientId);
                return;
            }

            var node = nodeData.Value;

            // 레벨 체크
            int level = GetPlayerLevel(clientId);
            if (level < node.requiredLevel)
            {
                SendMessageClientRpc($"채집 레벨 {node.requiredLevel} 이상 필요합니다.", clientId);
                return;
            }

            // 채집 시작
            float gatherTime = node.gatherTime;

            // 마운트 채집 속도 보너스
            if (MountSystem.Instance != null)
            {
                float gatherBonus = 0f;
                string mountId = null;
                // MountData의 gatherSpeedBonus는 MountSystem을 통해 접근
                // 단순화: 채집 시 마운트 하차
                if (MountSystem.Instance != null && MountSystem.Instance.IsPlayerMounted(clientId))
                {
                    // Auto-dismount handled by MountSystem
                }
            }

            gatheringInProgress[clientId] = (nodeInstanceId, nodeId, Time.time, gatherTime);
            NotifyGatheringStartedClientRpc(node.nodeName, gatherTime, clientId);
        }

        /// <summary>
        /// 채집 취소
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CancelGatheringServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (gatheringInProgress.Remove(clientId))
            {
                NotifyGatheringCancelledClientRpc(clientId);
            }
        }

        private void ProcessGatheringProgress()
        {
            var completed = new List<ulong>();

            foreach (var kvp in gatheringInProgress)
            {
                if (Time.time - kvp.Value.startTime >= kvp.Value.duration)
                    completed.Add(kvp.Key);
            }

            foreach (var clientId in completed)
            {
                var progress = gatheringInProgress[clientId];
                gatheringInProgress.Remove(clientId);
                CompleteGathering(clientId, progress.nodeInstanceId, progress.nodeId);
            }
        }

        private void CompleteGathering(ulong clientId, int nodeInstanceId, string nodeId)
        {
            // nodeId로 정확한 노드 데이터 찾기
            GatheringNodeData? nodeData = null;
            foreach (var n in nodeDefinitions)
            {
                if (n.nodeId == nodeId) { nodeData = n; break; }
            }

            if (nodeData == null) return;
            var node = nodeData.Value;

            // 아이템 드롭 결정
            string droppedItemId = null;
            float roll = Random.Range(0f, 1f);
            float cumulative = 0f;
            for (int i = 0; i < node.possibleItems.Length; i++)
            {
                cumulative += node.itemChances[i];
                if (roll <= cumulative)
                {
                    droppedItemId = node.possibleItems[i];
                    break;
                }
            }

            if (droppedItemId == null && node.possibleItems.Length > 0)
                droppedItemId = node.possibleItems[0];

            // 아이템 지급
            if (!string.IsNullOrEmpty(droppedItemId))
            {
                var inventoryMgr = GetInventoryManager(clientId);
                var itemData = ItemDatabase.GetItem(droppedItemId);
                if (inventoryMgr != null && itemData != null)
                    inventoryMgr.AddItem(new ItemInstance(itemData, 1));
            }

            // 경험치 지급
            AddGatherExp(clientId, node.expReward);

            // 노드 고갈
            depletedNodes[nodeInstanceId] = Time.time + node.respawnTime;

            NotifyGatheringCompletedClientRpc(droppedItemId ?? "", node.expReward, clientId);
        }

        private void ProcessNodeRespawns()
        {
            var respawned = new List<int>();
            foreach (var kvp in depletedNodes)
            {
                if (Time.time >= kvp.Value)
                    respawned.Add(kvp.Key);
            }
            foreach (var id in respawned)
                depletedNodes.Remove(id);
        }

        #endregion

        #region 레벨 시스템

        private int GetPlayerLevel(ulong clientId)
        {
            return playerGatherLevel.TryGetValue(clientId, out int level) ? level : 1;
        }

        private void AddGatherExp(ulong clientId, int exp)
        {
            if (!playerGatherLevel.ContainsKey(clientId))
                playerGatherLevel[clientId] = 1;
            if (!playerGatherExp.ContainsKey(clientId))
                playerGatherExp[clientId] = 0;

            playerGatherExp[clientId] += exp;

            // 레벨업 체크
            while (playerGatherLevel[clientId] < maxGatheringLevel &&
                   playerGatherLevel[clientId] < levelExpReqs.Length &&
                   playerGatherExp[clientId] >= levelExpReqs[playerGatherLevel[clientId]])
            {
                playerGatherExp[clientId] -= levelExpReqs[playerGatherLevel[clientId]];
                playerGatherLevel[clientId]++;

                SendMessageClientRpc($"채집 레벨 {playerGatherLevel[clientId]} 달성!", clientId);
            }

            NotifyGatherLevelClientRpc(playerGatherLevel[clientId], playerGatherExp[clientId], clientId);
        }

        #endregion

        #region ClientRPCs

        [ClientRpc]
        private void NotifyGatheringStartedClientRpc(string nodeName, float duration, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localIsGathering = true;
            OnGatheringStarted?.Invoke();
        }

        [ClientRpc]
        private void NotifyGatheringCompletedClientRpc(string itemId, int exp, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localIsGathering = false;
            OnGatheringCompleted?.Invoke();

            if (!string.IsNullOrEmpty(itemId))
            {
                var itemData = ItemDatabase.GetItem(itemId);
                string name = itemData != null ? itemData.ItemName : itemId;
                OnItemGathered?.Invoke(itemId, 1);

                var notif = NotificationManager.Instance;
                if (notif != null)
                    notif.ShowNotification($"채집: {name} 획득! (경험치+{exp})", NotificationType.ItemAcquire);
            }
        }

        [ClientRpc]
        private void NotifyGatheringCancelledClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localIsGathering = false;
            OnGatheringCancelled?.Invoke();
        }

        [ClientRpc]
        private void NotifyGatherLevelClientRpc(int level, int exp, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localGatherLevel = level;
            localGatherExp = exp;
            OnGatherLevelChanged?.Invoke(level, exp);
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

        private InventoryManager GetInventoryManager(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject?.GetComponent<InventoryManager>();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }
}
