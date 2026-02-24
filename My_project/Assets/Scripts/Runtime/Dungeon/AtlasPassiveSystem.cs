using UnityEngine;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class AtlasPassiveSystem : MonoBehaviour
    {
        public static AtlasPassiveSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int pointsPerDungeonClear = 1;
        [SerializeField] private int pointsPerBossKill = 2;
        [SerializeField] private int baseResetCost = 10000;

        private int availablePoints;
        private int totalPointsEarned;
        private int resetCount;
        private Dictionary<string, int> allocatedNodes = new Dictionary<string, int>();
        private List<AtlasPath> atlasPaths;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializePaths();
            LoadData();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void InitializePaths()
        {
            atlasPaths = new List<AtlasPath>();

            var combatNodes = new List<AtlasNode>
            {
                new AtlasNode("combat_1", "Monster Density I", "Increases monster density by 5%", AtlasBonusType.MonsterDensity, 0.05f, 1),
                new AtlasNode("combat_2", "Monster Damage I", "Increases monster damage by 3%", AtlasBonusType.MonsterDamage, 0.03f, 1),
                new AtlasNode("combat_3", "Monster Density II", "Increases monster density by 8%", AtlasBonusType.MonsterDensity, 0.08f, 2),
                new AtlasNode("combat_4", "Pack Size", "Increases pack size by 10%", AtlasBonusType.PackSize, 0.10f, 2),
                new AtlasNode("combat_5", "Monster Life", "Increases monster life by 5%", AtlasBonusType.MonsterLife, 0.05f, 3),
                new AtlasNode("combat_6", "Monster Density III", "Increases monster density by 12%", AtlasBonusType.MonsterDensity, 0.12f, 3),
                new AtlasNode("combat_7", "Elite Packs", "Increases elite pack chance by 15%", AtlasBonusType.EliteChance, 0.15f, 4),
                new AtlasNode("combat_key", "Keystone: Warzone", "All combat bonuses x1.5", AtlasBonusType.CombatKeystone, 1.5f, 5, true)
            };
            atlasPaths.Add(new AtlasPath("combat", "Combat", combatNodes));

            var treasureNodes = new List<AtlasNode>
            {
                new AtlasNode("treasure_1", "Item Quantity I", "Increases item drop rate by 5%", AtlasBonusType.ItemQuantity, 0.05f, 1),
                new AtlasNode("treasure_2", "Item Quality I", "Increases rare item chance by 3%", AtlasBonusType.ItemQuality, 0.03f, 1),
                new AtlasNode("treasure_3", "Gold Find I", "Increases gold drops by 8%", AtlasBonusType.GoldFind, 0.08f, 2),
                new AtlasNode("treasure_4", "Item Quantity II", "Increases item drop rate by 10%", AtlasBonusType.ItemQuantity, 0.10f, 2),
                new AtlasNode("treasure_5", "Item Quality II", "Increases rare item chance by 8%", AtlasBonusType.ItemQuality, 0.08f, 3),
                new AtlasNode("treasure_6", "Gold Find II", "Increases gold drops by 12%", AtlasBonusType.GoldFind, 0.12f, 3),
                new AtlasNode("treasure_7", "Legendary Chance", "Increases legendary drop chance by 5%", AtlasBonusType.LegendaryChance, 0.05f, 4),
                new AtlasNode("treasure_key", "Keystone: Fortune", "All treasure bonuses x1.4", AtlasBonusType.TreasureKeystone, 1.4f, 5, true)
            };
            atlasPaths.Add(new AtlasPath("treasure", "Treasure", treasureNodes));

            var bossNodes = new List<AtlasNode>
            {
                new AtlasNode("boss_1", "Boss Life I", "Increases boss life by 10%", AtlasBonusType.BossLife, 0.10f, 1),
                new AtlasNode("boss_2", "Boss Drops I", "Increases boss drop quantity by 5%", AtlasBonusType.BossDrops, 0.05f, 1),
                new AtlasNode("boss_3", "Boss Damage I", "Increases boss damage by 8%", AtlasBonusType.BossDamage, 0.08f, 2),
                new AtlasNode("boss_4", "Boss Drops II", "Increases boss drop quantity by 10%", AtlasBonusType.BossDrops, 0.10f, 2),
                new AtlasNode("boss_5", "Boss Exp", "Increases boss experience by 15%", AtlasBonusType.BossExp, 0.15f, 3),
                new AtlasNode("boss_6", "Boss Gold", "Increases boss gold drops by 20%", AtlasBonusType.BossGold, 0.20f, 3),
                new AtlasNode("boss_7", "Extra Boss", "Chance for additional boss spawn 10%", AtlasBonusType.ExtraBoss, 0.10f, 4),
                new AtlasNode("boss_key", "Keystone: Slayer", "All boss bonuses x1.35", AtlasBonusType.BossKeystone, 1.35f, 5, true)
            };
            atlasPaths.Add(new AtlasPath("boss", "Boss", bossNodes));

            var eventNodes = new List<AtlasNode>
            {
                new AtlasNode("event_1", "Event Frequency I", "Increases event chance by 5%", AtlasBonusType.EventFrequency, 0.05f, 1),
                new AtlasNode("event_2", "Event Reward I", "Increases event rewards by 5%", AtlasBonusType.EventReward, 0.05f, 1),
                new AtlasNode("event_3", "Shrine Effect", "Increases shrine duration by 10%", AtlasBonusType.ShrineDuration, 0.10f, 2),
                new AtlasNode("event_4", "Event Frequency II", "Increases event chance by 10%", AtlasBonusType.EventFrequency, 0.10f, 2),
                new AtlasNode("event_5", "Event Reward II", "Increases event rewards by 10%", AtlasBonusType.EventReward, 0.10f, 3),
                new AtlasNode("event_6", "Treasure Room", "Increases treasure room chance by 8%", AtlasBonusType.TreasureRoom, 0.08f, 3),
                new AtlasNode("event_7", "Event Mastery", "All events give bonus soul stones 5%", AtlasBonusType.EventSoulBonus, 0.05f, 4),
                new AtlasNode("event_key", "Keystone: Explorer", "All event bonuses x1.25", AtlasBonusType.EventKeystone, 1.25f, 5, true)
            };
            atlasPaths.Add(new AtlasPath("event", "Event", eventNodes));

            var synergyNodes = new List<AtlasNode>
            {
                new AtlasNode("synergy_1", "Experience I", "Increases all experience by 3%", AtlasBonusType.AllExp, 0.03f, 1),
                new AtlasNode("synergy_2", "Defense I", "Reduces damage taken by 2%", AtlasBonusType.DamageReduction, 0.02f, 1),
                new AtlasNode("synergy_3", "Experience II", "Increases all experience by 5%", AtlasBonusType.AllExp, 0.05f, 2),
                new AtlasNode("synergy_4", "Movement", "Increases movement speed by 3%", AtlasBonusType.MoveSpeed, 0.03f, 2),
                new AtlasNode("synergy_5", "Defense II", "Reduces damage taken by 4%", AtlasBonusType.DamageReduction, 0.04f, 3),
                new AtlasNode("synergy_6", "All Bonuses I", "Increases all other bonuses by 3%", AtlasBonusType.AllBonus, 0.03f, 3),
                new AtlasNode("synergy_7", "Dungeon Speed", "Reduces dungeon timer by 5%", AtlasBonusType.DungeonSpeed, 0.05f, 4),
                new AtlasNode("synergy_key", "Keystone: Mastermind", "All synergy bonuses x1.3", AtlasBonusType.SynergyKeystone, 1.3f, 5, true)
            };
            atlasPaths.Add(new AtlasPath("synergy", "Synergy", synergyNodes));
        }

        public void OnDungeonClear()
        {
            availablePoints += pointsPerDungeonClear;
            totalPointsEarned += pointsPerDungeonClear;
            SaveData();
        }

        public void OnBossKill()
        {
            availablePoints += pointsPerBossKill;
            totalPointsEarned += pointsPerBossKill;
            SaveData();
        }

        public bool AllocateNode(string nodeId)
        {
            if (availablePoints <= 0) return false;

            AtlasNode node = FindNode(nodeId);
            if (node == null) return false;

            int pathIndex = GetNodePathIndex(nodeId);
            int nodeIndex = GetNodeIndex(nodeId);
            if (pathIndex < 0 || nodeIndex < 0) return false;

            if (nodeIndex > 0)
            {
                string prevNodeId = atlasPaths[pathIndex].nodes[nodeIndex - 1].nodeId;
                if (!allocatedNodes.ContainsKey(prevNodeId))
                    return false;
            }

            if (allocatedNodes.ContainsKey(nodeId))
                return false;

            if (node.isKeystone && nodeIndex > 0)
            {
                for (int i = 0; i < nodeIndex; i++)
                {
                    string reqId = atlasPaths[pathIndex].nodes[i].nodeId;
                    if (!allocatedNodes.ContainsKey(reqId))
                        return false;
                }
            }

            allocatedNodes[nodeId] = 1;
            availablePoints--;
            SaveData();

            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(
                    "Atlas node allocated: " + node.nodeName, NotificationType.System);

            return true;
        }

        public bool ResetAll()
        {
            if (allocatedNodes.Count == 0) return false;

            long cost = resetCount == 0 ? 0 : (long)baseResetCost * (1L << Mathf.Min(resetCount, 10));

            var localPlayer = FindFirstObjectByType<PlayerController>();
            if (localPlayer == null) return false;

            var stats = localPlayer.GetComponent<PlayerStatsManager>()?.CurrentStats;
            if (stats == null) return false;

            if (resetCount > 0 && stats.Gold < cost)
                return false;

            if (cost > 0)
                stats.ChangeGold(-cost);

            int refundPoints = allocatedNodes.Count;
            allocatedNodes.Clear();
            availablePoints += refundPoints;
            resetCount++;
            SaveData();

            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(
                    "Atlas tree reset! " + refundPoints + " points refunded.", NotificationType.System);

            return true;
        }

        public float GetPathBonus(AtlasBonusType bonusType)
        {
            float total = 0f;
            float keystoneMultiplier = 1f;

            foreach (var path in atlasPaths)
            {
                foreach (var node in path.nodes)
                {
                    if (!allocatedNodes.ContainsKey(node.nodeId)) continue;

                    if (node.isKeystone)
                    {
                        if (IsKeystoneForBonus(node.bonusType, bonusType))
                            keystoneMultiplier = node.value;
                    }
                    else if (node.bonusType == bonusType)
                    {
                        total += node.value;
                    }
                }
            }

            return total * keystoneMultiplier;
        }

        public float GetTotalBonus(AtlasBonusType bonusType)
        {
            float pathBonus = GetPathBonus(bonusType);
            float allBonus = GetPathBonus(AtlasBonusType.AllBonus);
            return pathBonus * (1f + allBonus);
        }

        private bool IsKeystoneForBonus(AtlasBonusType keystoneType, AtlasBonusType targetType)
        {
            switch (keystoneType)
            {
                case AtlasBonusType.CombatKeystone:
                    return targetType == AtlasBonusType.MonsterDensity ||
                           targetType == AtlasBonusType.MonsterDamage ||
                           targetType == AtlasBonusType.PackSize ||
                           targetType == AtlasBonusType.MonsterLife ||
                           targetType == AtlasBonusType.EliteChance;
                case AtlasBonusType.TreasureKeystone:
                    return targetType == AtlasBonusType.ItemQuantity ||
                           targetType == AtlasBonusType.ItemQuality ||
                           targetType == AtlasBonusType.GoldFind ||
                           targetType == AtlasBonusType.LegendaryChance;
                case AtlasBonusType.BossKeystone:
                    return targetType == AtlasBonusType.BossLife ||
                           targetType == AtlasBonusType.BossDrops ||
                           targetType == AtlasBonusType.BossDamage ||
                           targetType == AtlasBonusType.BossExp ||
                           targetType == AtlasBonusType.BossGold ||
                           targetType == AtlasBonusType.ExtraBoss;
                case AtlasBonusType.EventKeystone:
                    return targetType == AtlasBonusType.EventFrequency ||
                           targetType == AtlasBonusType.EventReward ||
                           targetType == AtlasBonusType.ShrineDuration ||
                           targetType == AtlasBonusType.TreasureRoom ||
                           targetType == AtlasBonusType.EventSoulBonus;
                case AtlasBonusType.SynergyKeystone:
                    return targetType == AtlasBonusType.AllExp ||
                           targetType == AtlasBonusType.DamageReduction ||
                           targetType == AtlasBonusType.MoveSpeed ||
                           targetType == AtlasBonusType.AllBonus ||
                           targetType == AtlasBonusType.DungeonSpeed;
                default:
                    return false;
            }
        }

        private AtlasNode FindNode(string nodeId)
        {
            foreach (var path in atlasPaths)
            {
                foreach (var node in path.nodes)
                {
                    if (node.nodeId == nodeId) return node;
                }
            }
            return null;
        }

        private int GetNodePathIndex(string nodeId)
        {
            for (int i = 0; i < atlasPaths.Count; i++)
            {
                foreach (var node in atlasPaths[i].nodes)
                {
                    if (node.nodeId == nodeId) return i;
                }
            }
            return -1;
        }

        private int GetNodeIndex(string nodeId)
        {
            foreach (var path in atlasPaths)
            {
                for (int i = 0; i < path.nodes.Count; i++)
                {
                    if (path.nodes[i].nodeId == nodeId) return i;
                }
            }
            return -1;
        }

        public bool IsNodeAllocated(string nodeId)
        {
            return allocatedNodes.ContainsKey(nodeId);
        }

        public int AvailablePoints => availablePoints;
        public int TotalPointsEarned => totalPointsEarned;
        public int AllocatedCount => allocatedNodes.Count;
        public int ResetCount => resetCount;
        public List<AtlasPath> GetPaths() => atlasPaths;

        public long GetResetCost()
        {
            if (resetCount == 0) return 0;
            return (long)baseResetCost * (1L << Mathf.Min(resetCount, 10));
        }

        private void SaveData()
        {
            PlayerPrefs.SetInt("Atlas_AvailablePoints", availablePoints);
            PlayerPrefs.SetInt("Atlas_TotalEarned", totalPointsEarned);
            PlayerPrefs.SetInt("Atlas_ResetCount", resetCount);

            string nodeData = "";
            foreach (var kvp in allocatedNodes)
            {
                if (nodeData.Length > 0) nodeData += "|";
                nodeData += kvp.Key + ":" + kvp.Value;
            }
            PlayerPrefs.SetString("Atlas_Nodes", nodeData);
            PlayerPrefs.Save();
        }

        private void LoadData()
        {
            availablePoints = PlayerPrefs.GetInt("Atlas_AvailablePoints", 0);
            totalPointsEarned = PlayerPrefs.GetInt("Atlas_TotalEarned", 0);
            resetCount = PlayerPrefs.GetInt("Atlas_ResetCount", 0);

            string nodeData = PlayerPrefs.GetString("Atlas_Nodes", "");
            allocatedNodes.Clear();
            if (!string.IsNullOrEmpty(nodeData))
            {
                char pipeChar = (char)124;
                char colonChar = (char)58;
                string[] entries = nodeData.Split(pipeChar);
                foreach (string entry in entries)
                {
                    string[] parts = entry.Split(colonChar);
                    if (parts.Length == 2 && int.TryParse(parts[1], out int val))
                    {
                        allocatedNodes[parts[0]] = val;
                    }
                }
            }
        }
    }

    [System.Serializable]
    public class AtlasNode
    {
        public string nodeId;
        public string nodeName;
        public string description;
        public AtlasBonusType bonusType;
        public float value;
        public int tier;
        public bool isKeystone;

        public AtlasNode(string id, string name, string desc, AtlasBonusType type, float val, int t, bool keystone = false)
        {
            nodeId = id;
            nodeName = name;
            description = desc;
            bonusType = type;
            value = val;
            tier = t;
            isKeystone = keystone;
        }
    }

    [System.Serializable]
    public class AtlasPath
    {
        public string pathId;
        public string pathName;
        public List<AtlasNode> nodes;

        public AtlasPath(string id, string name, List<AtlasNode> n)
        {
            pathId = id;
            pathName = name;
            nodes = n;
        }
    }

    public enum AtlasBonusType
    {
        MonsterDensity, MonsterDamage, PackSize, MonsterLife, EliteChance, CombatKeystone,
        ItemQuantity, ItemQuality, GoldFind, LegendaryChance, TreasureKeystone,
        BossLife, BossDrops, BossDamage, BossExp, BossGold, ExtraBoss, BossKeystone,
        EventFrequency, EventReward, ShrineDuration, TreasureRoom, EventSoulBonus, EventKeystone,
        AllExp, DamageReduction, MoveSpeed, AllBonus, DungeonSpeed, SynergyKeystone
    }
}
