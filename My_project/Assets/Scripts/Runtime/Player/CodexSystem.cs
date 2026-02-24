using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public enum CodexCategory
    {
        Combat,      // 전투 서고 (10권)
        Defense,     // 방어 서고 (10권)
        Exploration, // 탐험 서고 (8권)
        Crafting,    // 제작 서고 (8권)
        Wisdom       // 지혜 서고 (4권)
    }

    public enum CodexUnlockCondition
    {
        DungeonFirstClear,
        BossKill,
        SecretDiscovery,
        MonsterKillCount,
        CraftingMilestone,
        EnhanceMilestone,
        ExplorationMilestone,
        CollectionComplete
    }

    [Serializable]
    public class CodexEntry
    {
        public int id;
        public string name;
        public string description;
        public CodexCategory category;
        public CodexUnlockCondition condition;
        public string conditionKey;    // dungeon id, boss name, etc.
        public int conditionValue;     // kill count, craft count, etc.
        public string bonusType;       // stat key
        public float bonusValue;
        public bool isPercentage;

        public CodexEntry(int id, string name, string desc, CodexCategory cat,
            CodexUnlockCondition cond, string condKey, int condVal,
            string bonusType, float bonusValue, bool isPercent)
        {
            this.id = id;
            this.name = name;
            this.description = desc;
            this.category = cat;
            this.condition = cond;
            this.conditionKey = condKey;
            this.conditionValue = condVal;
            this.bonusType = bonusType;
            this.bonusValue = bonusValue;
            this.isPercentage = isPercent;
        }
    }

    /// <summary>
    /// Codex of Knowledge - Account-wide permanent bonuses unlocked through gameplay.
    /// 5 categories, 40 total entries. Progress saved via PlayerPrefs.
    /// </summary>
    public class CodexSystem : NetworkBehaviour
    {
        public static CodexSystem Instance { get; private set; }

        // Category milestone bonuses (granted when all entries in a category are unlocked)
        private static readonly Dictionary<CodexCategory, string> CategoryMilestoneBonus = new Dictionary<CodexCategory, string>
        {
            { CodexCategory.Combat, "전체 데미지 +10%" },
            { CodexCategory.Defense, "전체 방어력 +15" },
            { CodexCategory.Exploration, "이동 속도 +20%" },
            { CodexCategory.Crafting, "제작 성공률 +15%" },
            { CodexCategory.Wisdom, "모든 획득량 +10%" }
        };

        // All 40 codex entries
        private static readonly CodexEntry[] AllEntries = new CodexEntry[]
        {
            // === Combat (10) ===
            new CodexEntry(0, "물리의 서", "물리 데미지 +1%", CodexCategory.Combat,
                CodexUnlockCondition.DungeonFirstClear, "GoblinCave", 1, "PhysicalDamage", 1f, true),
            new CodexEntry(1, "화염의 서", "화염 데미지 +2%", CodexCategory.Combat,
                CodexUnlockCondition.BossKill, "FireElemental_Boss", 1, "FireDamage", 2f, true),
            new CodexEntry(2, "냉기의 서", "냉기 데미지 +2%", CodexCategory.Combat,
                CodexUnlockCondition.BossKill, "IceElemental_Boss", 1, "IceDamage", 2f, true),
            new CodexEntry(3, "번개의 서", "번개 데미지 +2%", CodexCategory.Combat,
                CodexUnlockCondition.BossKill, "LightningElemental_Boss", 1, "LightningDamage", 2f, true),
            new CodexEntry(4, "독의 서", "독 데미지 +2%", CodexCategory.Combat,
                CodexUnlockCondition.MonsterKillCount, "Beast", 100, "PoisonDamage", 2f, true),
            new CodexEntry(5, "암흑의 서", "암흑 데미지 +3%", CodexCategory.Combat,
                CodexUnlockCondition.DungeonFirstClear, "DemonLair", 1, "DarkDamage", 3f, true),
            new CodexEntry(6, "신성의 서", "신성 데미지 +3%", CodexCategory.Combat,
                CodexUnlockCondition.MonsterKillCount, "Undead", 200, "HolyDamage", 3f, true),
            new CodexEntry(7, "치명의 서", "크리티컬 확률 +2%", CodexCategory.Combat,
                CodexUnlockCondition.BossKill, "Dragon_Boss", 1, "CriticalChance", 2f, true),
            new CodexEntry(8, "파괴의 서", "크리티컬 데미지 +5%", CodexCategory.Combat,
                CodexUnlockCondition.DungeonFirstClear, "DragonNest", 1, "CriticalDamage", 5f, true),
            new CodexEntry(9, "관통의 서", "방어 관통 +3%", CodexCategory.Combat,
                CodexUnlockCondition.BossKill, "Construct_Boss", 1, "ArmorPenetration", 3f, true),

            // === Defense (10) ===
            new CodexEntry(10, "철벽의 서", "물리 방어력 +5", CodexCategory.Defense,
                CodexUnlockCondition.DungeonFirstClear, "GoblinCave", 1, "Defense", 5f, false),
            new CodexEntry(11, "마법 장벽의 서", "마법 저항 +5", CodexCategory.Defense,
                CodexUnlockCondition.DungeonFirstClear, "DarkForest", 1, "MagicDefense", 5f, false),
            new CodexEntry(12, "생명력의 서", "최대 HP +3%", CodexCategory.Defense,
                CodexUnlockCondition.MonsterKillCount, "Orc", 50, "MaxHP", 3f, true),
            new CodexEntry(13, "재생의 서", "HP 재생 +2", CodexCategory.Defense,
                CodexUnlockCondition.SecretDiscovery, "HiddenSpring", 1, "HPRegen", 2f, false),
            new CodexEntry(14, "회피의 서", "회피율 +2%", CodexCategory.Defense,
                CodexUnlockCondition.MonsterKillCount, "Demon", 75, "Evasion", 2f, true),
            new CodexEntry(15, "화염 저항의 서", "화염 저항 +3%", CodexCategory.Defense,
                CodexUnlockCondition.BossKill, "FireDragon_Boss", 1, "FireResist", 3f, true),
            new CodexEntry(16, "냉기 저항의 서", "냉기 저항 +3%", CodexCategory.Defense,
                CodexUnlockCondition.BossKill, "IceDragon_Boss", 1, "IceResist", 3f, true),
            new CodexEntry(17, "번개 저항의 서", "번개 저항 +3%", CodexCategory.Defense,
                CodexUnlockCondition.SecretDiscovery, "StormShrine", 1, "LightningResist", 3f, true),
            new CodexEntry(18, "독 저항의 서", "독 저항 +3%", CodexCategory.Defense,
                CodexUnlockCondition.MonsterKillCount, "Beast", 150, "PoisonResist", 3f, true),
            new CodexEntry(19, "불멸의 서", "사망 시 HP 10% 회복 (60초 쿨다운)", CodexCategory.Defense,
                CodexUnlockCondition.BossKill, "AbyssBoss", 1, "DeathSave", 10f, true),

            // === Exploration (8) ===
            new CodexEntry(20, "바람의 서", "이동 속도 +3%", CodexCategory.Exploration,
                CodexUnlockCondition.ExplorationMilestone, "TotalZones", 3, "MoveSpeed", 3f, true),
            new CodexEntry(21, "탐험가의 서", "미니맵 범위 +10%", CodexCategory.Exploration,
                CodexUnlockCondition.ExplorationMilestone, "TotalZones", 5, "MinimapRange", 10f, true),
            new CodexEntry(22, "보물 감지의 서", "보물 상자 발견 확률 +5%", CodexCategory.Exploration,
                CodexUnlockCondition.SecretDiscovery, "HiddenTreasure", 5, "TreasureFind", 5f, true),
            new CodexEntry(23, "함정 감지의 서", "함정 회피율 +10%", CodexCategory.Exploration,
                CodexUnlockCondition.DungeonFirstClear, "UndeadCrypt", 1, "TrapEvasion", 10f, true),
            new CodexEntry(24, "지도 제작의 서", "미발견 구역 표시", CodexCategory.Exploration,
                CodexUnlockCondition.ExplorationMilestone, "TotalFloors", 50, "MapReveal", 1f, false),
            new CodexEntry(25, "차원 이동의 서", "귀환 스크롤 쿨다운 -20%", CodexCategory.Exploration,
                CodexUnlockCondition.DungeonFirstClear, "DemonLair", 1, "TeleportCooldown", 20f, true),
            new CodexEntry(26, "야간 투시의 서", "어둠 속 시야 +30%", CodexCategory.Exploration,
                CodexUnlockCondition.SecretDiscovery, "DeepCavern", 3, "DarkVision", 30f, true),
            new CodexEntry(27, "비밀의 서", "비밀 문 발견 확률 +10%", CodexCategory.Exploration,
                CodexUnlockCondition.SecretDiscovery, "HiddenDoor", 10, "SecretDoorFind", 10f, true),

            // === Crafting (8) ===
            new CodexEntry(28, "대장장이의 서", "강화 성공률 +3%", CodexCategory.Crafting,
                CodexUnlockCondition.EnhanceMilestone, "TotalEnhance", 10, "EnhanceRate", 3f, true),
            new CodexEntry(29, "연금술의 서", "포션 효과 +10%", CodexCategory.Crafting,
                CodexUnlockCondition.CraftingMilestone, "TotalCraft", 20, "PotionEffect", 10f, true),
            new CodexEntry(30, "인챈트의 서", "인챈트 성공률 +5%", CodexCategory.Crafting,
                CodexUnlockCondition.EnhanceMilestone, "TotalEnchant", 15, "EnchantRate", 5f, true),
            new CodexEntry(31, "템퍼링의 서", "템퍼링 접사 범위 +5%", CodexCategory.Crafting,
                CodexUnlockCondition.CraftingMilestone, "TotalTemper", 10, "TemperRange", 5f, true),
            new CodexEntry(32, "분해의 서", "분해 추가 재료 +15%", CodexCategory.Crafting,
                CodexUnlockCondition.CraftingMilestone, "TotalSalvage", 50, "SalvageBonus", 15f, true),
            new CodexEntry(33, "보석 세공의 서", "보석 합성 성공률 +10%", CodexCategory.Crafting,
                CodexUnlockCondition.CraftingMilestone, "TotalGemCraft", 10, "GemCraftRate", 10f, true),
            new CodexEntry(34, "룬 새김의 서", "룬 장착 비용 -10%", CodexCategory.Crafting,
                CodexUnlockCondition.CraftingMilestone, "TotalRuneSocket", 10, "RuneCostReduction", 10f, true),
            new CodexEntry(35, "각성의 서", "각성 성공률 +5%", CodexCategory.Crafting,
                CodexUnlockCondition.EnhanceMilestone, "TotalAwaken", 5, "AwakenRate", 5f, true),

            // === Wisdom (4) ===
            new CodexEntry(36, "경험의 서", "경험치 획득량 +5%", CodexCategory.Wisdom,
                CodexUnlockCondition.DungeonFirstClear, "DragonNest", 1, "ExpBonus", 5f, true),
            new CodexEntry(37, "재물의 서", "골드 획득량 +5%", CodexCategory.Wisdom,
                CodexUnlockCondition.DungeonFirstClear, "DemonLair", 1, "GoldBonus", 5f, true),
            new CodexEntry(38, "행운의 서", "아이템 드롭률 +3%", CodexCategory.Wisdom,
                CodexUnlockCondition.CollectionComplete, "MonsterCollection", 50, "DropRateBonus", 3f, true),
            new CodexEntry(39, "현자의 서", "희귀+ 아이템 드롭률 +2%", CodexCategory.Wisdom,
                CodexUnlockCondition.BossKill, "AbyssBoss", 1, "RareDropBonus", 2f, true)
        };

        // Per-player unlocked entries (saved to PlayerPrefs)
        private Dictionary<ulong, HashSet<int>> playerUnlocks = new Dictionary<ulong, HashSet<int>>();

        public Action<ulong, int> OnEntryUnlocked;           // clientId, entryId
        public Action<ulong, CodexCategory> OnCategoryComplete; // clientId, category

        private const string PrefsKeyPrefix = "Codex_";

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

        public static CodexEntry GetEntry(int id)
        {
            if (id < 0 || id >= AllEntries.Length) return null;
            return AllEntries[id];
        }

        public static CodexEntry[] GetEntriesByCategory(CodexCategory category)
        {
            return AllEntries.Where(e => e.category == category).ToArray();
        }

        public static int GetCategoryTotal(CodexCategory category)
        {
            return AllEntries.Count(e => e.category == category);
        }

        public bool IsUnlocked(ulong clientId, int entryId)
        {
            if (!playerUnlocks.TryGetValue(clientId, out var unlocks)) return false;
            return unlocks.Contains(entryId);
        }

        public int GetCategoryProgress(ulong clientId, CodexCategory category)
        {
            if (!playerUnlocks.TryGetValue(clientId, out var unlocks)) return 0;
            return AllEntries.Count(e => e.category == category && unlocks.Contains(e.id));
        }

        public bool IsCategoryComplete(ulong clientId, CodexCategory category)
        {
            return GetCategoryProgress(clientId, category) >= GetCategoryTotal(category);
        }

        public int GetTotalUnlocks(ulong clientId)
        {
            if (!playerUnlocks.TryGetValue(clientId, out var unlocks)) return 0;
            return unlocks.Count;
        }

        /// <summary>
        /// Get all active bonus values for a player from unlocked codex entries.
        /// </summary>
        public Dictionary<string, float> GetAllBonuses(ulong clientId)
        {
            var bonuses = new Dictionary<string, float>();
            if (!playerUnlocks.TryGetValue(clientId, out var unlocks)) return bonuses;

            foreach (int id in unlocks)
            {
                var entry = GetEntry(id);
                if (entry == null) continue;

                if (bonuses.ContainsKey(entry.bonusType))
                    bonuses[entry.bonusType] += entry.bonusValue;
                else
                    bonuses[entry.bonusType] = entry.bonusValue;
            }

            return bonuses;
        }

        public float GetBonus(ulong clientId, string bonusType)
        {
            var bonuses = GetAllBonuses(clientId);
            return bonuses.TryGetValue(bonusType, out float val) ? val : 0f;
        }

        #endregion

        #region Progress Reporting

        /// <summary>
        /// Called by external systems when a qualifying event occurs.
        /// Checks if any locked entry can be unlocked.
        /// </summary>
        public void ReportProgress(ulong clientId, CodexUnlockCondition condition, string conditionKey, int value = 1)
        {
            if (!IsServer) return;

            EnsurePlayerData(clientId);
            var unlocks = playerUnlocks[clientId];

            foreach (var entry in AllEntries)
            {
                if (unlocks.Contains(entry.id)) continue;
                if (entry.condition != condition) continue;
                if (entry.conditionKey != conditionKey) continue;

                // For count-based conditions, check if value meets threshold
                if (entry.conditionValue > 1 && value < entry.conditionValue) continue;

                // Unlock!
                unlocks.Add(entry.id);
                SavePlayerCodex(clientId);

                NotifyUnlockClientRpc(clientId, entry.id, entry.name, entry.description,
                    (int)entry.category, entry.bonusType, entry.bonusValue, entry.isPercentage);
                OnEntryUnlocked?.Invoke(clientId, entry.id);

                Debug.Log($"[CodexSystem] Entry unlocked: {entry.name} for client {clientId}");

                // Check category completion
                if (IsCategoryComplete(clientId, entry.category))
                {
                    string milestoneBonus = CategoryMilestoneBonus.TryGetValue(entry.category, out string bonus)
                        ? bonus : "추가 보너스";
                    NotifyCategoryCompleteClientRpc(clientId, (int)entry.category, milestoneBonus);
                    OnCategoryComplete?.Invoke(clientId, entry.category);
                    Debug.Log($"[CodexSystem] Category complete: {entry.category} for client {clientId}");
                }
            }
        }

        #endregion

        #region ServerRpc

        /// <summary>
        /// Request codex status for UI display.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestCodexDataServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            EnsurePlayerData(clientId);

            var unlocks = playerUnlocks[clientId];
            string unlockedIds = string.Join(",", unlocks.OrderBy(x => x));
            int totalUnlocked = unlocks.Count;
            int totalEntries = AllEntries.Length;

            // Build category progress string
            var catProgress = new List<string>();
            foreach (CodexCategory cat in Enum.GetValues(typeof(CodexCategory)))
            {
                int progress = GetCategoryProgress(clientId, cat);
                int total = GetCategoryTotal(cat);
                catProgress.Add($"{(int)cat}:{progress}/{total}");
            }
            string categoryData = string.Join("|", catProgress);

            NotifyCodexDataClientRpc(clientId, unlockedIds, totalUnlocked, totalEntries, categoryData);
        }

        #endregion

        #region Save/Load (PlayerPrefs)

        private void EnsurePlayerData(ulong clientId)
        {
            if (playerUnlocks.ContainsKey(clientId)) return;

            playerUnlocks[clientId] = new HashSet<int>();
            LoadPlayerCodex(clientId);
        }

        private void SavePlayerCodex(ulong clientId)
        {
            if (!playerUnlocks.TryGetValue(clientId, out var unlocks)) return;
            string key = $"{PrefsKeyPrefix}{clientId}";
            string data = string.Join(",", unlocks.OrderBy(x => x));
            PlayerPrefs.SetString(key, data);
            PlayerPrefs.Save();
        }

        private void LoadPlayerCodex(ulong clientId)
        {
            string key = $"{PrefsKeyPrefix}{clientId}";
            if (!PlayerPrefs.HasKey(key)) return;

            string data = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(data)) return;

            var unlocks = playerUnlocks[clientId];
            foreach (string idStr in data.Split(','))
            {
                if (int.TryParse(idStr.Trim(), out int id))
                {
                    unlocks.Add(id);
                }
            }
        }

        #endregion

        #region ClientRpc

        [ClientRpc]
        private void NotifyUnlockClientRpc(ulong targetClientId, int entryId, string name,
            string description, int category, string bonusType, float bonusValue, bool isPercent)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            string catName = ((CodexCategory)category) switch
            {
                CodexCategory.Combat => "전투",
                CodexCategory.Defense => "방어",
                CodexCategory.Exploration => "탐험",
                CodexCategory.Crafting => "제작",
                CodexCategory.Wisdom => "지혜",
                _ => "기타"
            };

            string valueStr = isPercent ? $"+{bonusValue:F1}%" : $"+{bonusValue:F0}";
            NotificationManager.Instance?.ShowNotification(
                $"<color=#FFD700>[서고] {catName} 지식 해금!</color> {name}: {bonusType} {valueStr}",
                NotificationType.Achievement);
        }

        [ClientRpc]
        private void NotifyCategoryCompleteClientRpc(ulong targetClientId, int category, string milestoneBonus)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            string catName = ((CodexCategory)category) switch
            {
                CodexCategory.Combat => "전투",
                CodexCategory.Defense => "방어",
                CodexCategory.Exploration => "탐험",
                CodexCategory.Crafting => "제작",
                CodexCategory.Wisdom => "지혜",
                _ => "기타"
            };

            NotificationManager.Instance?.ShowNotification(
                $"<color=#FF8800>[서고 마일스톤]</color> {catName} 서고 완성! 보너스: {milestoneBonus}",
                NotificationType.Achievement);
        }

        [ClientRpc]
        private void NotifyCodexDataClientRpc(ulong targetClientId, string unlockedIds,
            int totalUnlocked, int totalEntries, string categoryData)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            // UI should parse this data to display codex progress
            Debug.Log($"[CodexSystem] Codex: {totalUnlocked}/{totalEntries} unlocked. Categories: {categoryData}");
        }

        #endregion
    }
}
