using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    [Serializable]
    public class CraftingRecipeRequirement
    {
        public string materialId;
        public int count;

        public CraftingRecipeRequirement(string materialId, int count)
        {
            this.materialId = materialId;
            this.count = count;
        }
    }

    [Serializable]
    public class CraftingRecipe
    {
        public string recipeId;
        public string resultItemId;
        public string resultName;
        public CraftingCategory category;
        public CraftingRecipeRequirement[] materials;
        public int requiredCraftingLevel;
        public int goldCost;

        public CraftingRecipe(string recipeId, string resultItemId, string resultName,
            CraftingCategory category, CraftingRecipeRequirement[] materials,
            int requiredCraftingLevel, int goldCost)
        {
            this.recipeId = recipeId;
            this.resultItemId = resultItemId;
            this.resultName = resultName;
            this.category = category;
            this.materials = materials;
            this.requiredCraftingLevel = requiredCraftingLevel;
            this.goldCost = goldCost;
        }
    }

    public class CraftingSystem : NetworkBehaviour
    {
        public static CraftingSystem Instance { get; private set; }

        public event Action OnCraftingComplete;
        public event Action<string> OnRecipeDiscovered;

        private Dictionary<string, CraftingRecipe> recipes = new Dictionary<string, CraftingRecipe>();
        private Dictionary<ulong, int> craftingLevels = new Dictionary<ulong, int>();
        private Dictionary<ulong, HashSet<string>> discoveredRecipes = new Dictionary<ulong, HashSet<string>>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeRecipes();
        }

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                OnCraftingComplete = null;
                OnRecipeDiscovered = null;
                Instance = null;
            }
            base.OnDestroy();
        }

        public int GetCraftingLevel(ulong clientId)
        {
            return craftingLevels.TryGetValue(clientId, out int level) ? level : 0;
        }

        public HashSet<string> GetDiscoveredRecipes(ulong clientId)
        {
            if (!discoveredRecipes.ContainsKey(clientId))
            {
                discoveredRecipes[clientId] = new HashSet<string>();
            }
            return discoveredRecipes[clientId];
        }

        public List<CraftingRecipe> GetAllRecipes()
        {
            return recipes.Values.ToList();
        }

        public CraftingRecipe GetRecipe(string recipeId)
        {
            return recipes.TryGetValue(recipeId, out CraftingRecipe recipe) ? recipe : null;
        }

        public List<CraftingRecipe> GetRecipesByCategory(CraftingCategory category)
        {
            return recipes.Values.Where(r => r.category == category).ToList();
        }

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (NetworkManager.Singleton == null) return null;
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) return null;
            return NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        [ServerRpc(RequireOwnership = false)]
        public void CraftItemServerRpc(string recipeId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!recipes.TryGetValue(recipeId, out CraftingRecipe recipe))
            {
                NotifyCraftResultClientRpc("Unknown", false, clientId);
                return;
            }

            int playerLevel = GetCraftingLevel(clientId);
            if (playerLevel < recipe.requiredCraftingLevel)
            {
                NotifyCraftResultClientRpc(recipe.resultName, false, clientId);
                return;
            }

            PlayerStatsData statsData = GetPlayerStatsData(clientId);
            if (statsData == null)
            {
                NotifyCraftResultClientRpc(recipe.resultName, false, clientId);
                return;
            }

            if (statsData.Gold < recipe.goldCost)
            {
                NotifyCraftResultClientRpc(recipe.resultName, false, clientId);
                return;
            }

            // Deduct gold and grant experience
            statsData.ChangeGold(-recipe.goldCost);
            statsData.AddExperience(recipe.requiredCraftingLevel * 10);

            // Increase crafting proficiency (capped at 100)
            if (!craftingLevels.ContainsKey(clientId))
            {
                craftingLevels[clientId] = 0;
            }
            craftingLevels[clientId] = Mathf.Min(craftingLevels[clientId] + 1, 100);

            Debug.Log($"[CraftingSystem] Player {clientId} crafted {recipe.resultName} (Level: {craftingLevels[clientId]})");

            NotifyCraftResultClientRpc(recipe.resultName, true, clientId);
            OnCraftingComplete?.Invoke();
        }

        [ServerRpc(RequireOwnership = false)]
        public void DiscoverRecipeServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!discoveredRecipes.ContainsKey(clientId))
            {
                discoveredRecipes[clientId] = new HashSet<string>();
            }

            HashSet<string> playerDiscovered = discoveredRecipes[clientId];
            int playerLevel = GetCraftingLevel(clientId);

            foreach (var kvp in recipes)
            {
                CraftingRecipe recipe = kvp.Value;

                if (playerDiscovered.Contains(recipe.recipeId)) continue;
                if (playerLevel < recipe.requiredCraftingLevel) continue;

                // Auto-discover recipe when player meets level requirement
                playerDiscovered.Add(recipe.recipeId);
                NotifyRecipeDiscoveredClientRpc(recipe.recipeId, recipe.resultName, clientId);
                OnRecipeDiscovered?.Invoke(recipe.recipeId);
            }
        }

        [ClientRpc]
        public void NotifyCraftResultClientRpc(string itemName, bool success, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            if (success)
            {
                NotificationManager.Instance?.ShowNotification(
                    $"{itemName} 제작 완료!", NotificationType.System);
            }
            else
            {
                NotificationManager.Instance?.ShowNotification(
                    $"{itemName} 제작 실패. 재료, 골드, 또는 제작 레벨을 확인하세요.", NotificationType.Warning);
            }
        }

        [ClientRpc]
        public void NotifyRecipeDiscoveredClientRpc(string recipeId, string recipeName, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            NotificationManager.Instance?.ShowNotification(
                $"새로운 레시피 발견: {recipeName}", NotificationType.System);
        }

        private void InitializeRecipes()
        {
            // === Weapon Recipes (5) ===
            AddRecipe(new CraftingRecipe("wpn_iron_sword", "item_iron_sword", "Iron Sword",
                CraftingCategory.Weapon,
                new[] { new CraftingRecipeRequirement("mat_iron_ingot", 3), new CraftingRecipeRequirement("mat_wood", 1) },
                0, 100));

            AddRecipe(new CraftingRecipe("wpn_steel_axe", "item_steel_axe", "Steel Axe",
                CraftingCategory.Weapon,
                new[] { new CraftingRecipeRequirement("mat_steel_ingot", 4), new CraftingRecipeRequirement("mat_wood", 2) },
                10, 300));

            AddRecipe(new CraftingRecipe("wpn_mithril_staff", "item_mithril_staff", "Mithril Staff",
                CraftingCategory.Weapon,
                new[] { new CraftingRecipeRequirement("mat_mithril_ingot", 5), new CraftingRecipeRequirement("mat_magic_crystal", 2) },
                30, 800));

            AddRecipe(new CraftingRecipe("wpn_adamant_lance", "item_adamant_lance", "Adamantite Lance",
                CraftingCategory.Weapon,
                new[] { new CraftingRecipeRequirement("mat_adamant_ingot", 6), new CraftingRecipeRequirement("mat_dragon_bone", 1) },
                50, 1500));

            AddRecipe(new CraftingRecipe("wpn_legendary_blade", "item_legendary_blade", "Legendary Blade",
                CraftingCategory.Weapon,
                new[] { new CraftingRecipeRequirement("mat_adamant_ingot", 10), new CraftingRecipeRequirement("mat_dragon_soul", 3), new CraftingRecipeRequirement("mat_star_fragment", 1) },
                80, 5000));

            // === Armor Recipes (4) ===
            AddRecipe(new CraftingRecipe("arm_leather_vest", "item_leather_vest", "Leather Vest",
                CraftingCategory.Armor,
                new[] { new CraftingRecipeRequirement("mat_leather", 4), new CraftingRecipeRequirement("mat_thread", 2) },
                0, 80));

            AddRecipe(new CraftingRecipe("arm_chainmail", "item_chainmail", "Chainmail Armor",
                CraftingCategory.Armor,
                new[] { new CraftingRecipeRequirement("mat_iron_ingot", 6), new CraftingRecipeRequirement("mat_leather", 2) },
                15, 400));

            AddRecipe(new CraftingRecipe("arm_plate_armor", "item_plate_armor", "Plate Armor",
                CraftingCategory.Armor,
                new[] { new CraftingRecipeRequirement("mat_steel_ingot", 8), new CraftingRecipeRequirement("mat_iron_ingot", 4) },
                35, 1000));

            AddRecipe(new CraftingRecipe("arm_legendary_shield", "item_legendary_shield", "Legendary Shield",
                CraftingCategory.Armor,
                new[] { new CraftingRecipeRequirement("mat_adamant_ingot", 8), new CraftingRecipeRequirement("mat_dragon_scale", 5), new CraftingRecipeRequirement("mat_star_fragment", 1) },
                75, 4500));

            // === Consumable Recipes (5) ===
            AddRecipe(new CraftingRecipe("con_health_potion", "item_health_potion", "Health Potion",
                CraftingCategory.Consumable,
                new[] { new CraftingRecipeRequirement("mat_herb_red", 2), new CraftingRecipeRequirement("mat_water_vial", 1) },
                0, 20));

            AddRecipe(new CraftingRecipe("con_mana_potion", "item_mana_potion", "Mana Potion",
                CraftingCategory.Consumable,
                new[] { new CraftingRecipeRequirement("mat_herb_blue", 2), new CraftingRecipeRequirement("mat_water_vial", 1) },
                0, 20));

            AddRecipe(new CraftingRecipe("con_strength_elixir", "item_strength_elixir", "Strength Elixir",
                CraftingCategory.Consumable,
                new[] { new CraftingRecipeRequirement("mat_herb_red", 3), new CraftingRecipeRequirement("mat_beast_blood", 1), new CraftingRecipeRequirement("mat_water_vial", 1) },
                20, 150));

            AddRecipe(new CraftingRecipe("con_defense_elixir", "item_defense_elixir", "Defense Elixir",
                CraftingCategory.Consumable,
                new[] { new CraftingRecipeRequirement("mat_herb_green", 3), new CraftingRecipeRequirement("mat_turtle_shell", 1), new CraftingRecipeRequirement("mat_water_vial", 1) },
                20, 150));

            AddRecipe(new CraftingRecipe("con_resurrection_potion", "item_resurrection_potion", "Resurrection Potion",
                CraftingCategory.Consumable,
                new[] { new CraftingRecipeRequirement("mat_phoenix_feather", 1), new CraftingRecipeRequirement("mat_holy_water", 2), new CraftingRecipeRequirement("mat_herb_gold", 3) },
                60, 2000));

            // === Gem Recipes (3) ===
            AddRecipe(new CraftingRecipe("gem_mid_ruby", "item_mid_ruby", "Refined Ruby",
                CraftingCategory.Enhancement,
                new[] { new CraftingRecipeRequirement("mat_rough_ruby", 3) },
                10, 200));

            AddRecipe(new CraftingRecipe("gem_mid_sapphire", "item_mid_sapphire", "Refined Sapphire",
                CraftingCategory.Enhancement,
                new[] { new CraftingRecipeRequirement("mat_rough_sapphire", 3) },
                10, 200));

            AddRecipe(new CraftingRecipe("gem_high_emerald", "item_high_emerald", "Perfect Emerald",
                CraftingCategory.Enhancement,
                new[] { new CraftingRecipeRequirement("mat_refined_emerald", 3) },
                40, 600));

            // === Material Conversion Recipes (3) ===
            AddRecipe(new CraftingRecipe("cvt_iron_ingot", "mat_iron_ingot", "Iron Ingot",
                CraftingCategory.Special,
                new[] { new CraftingRecipeRequirement("mat_iron_ore", 3) },
                0, 10));

            AddRecipe(new CraftingRecipe("cvt_steel_ingot", "mat_steel_ingot", "Steel Ingot",
                CraftingCategory.Special,
                new[] { new CraftingRecipeRequirement("mat_iron_ingot", 2), new CraftingRecipeRequirement("mat_coal", 3) },
                15, 50));

            AddRecipe(new CraftingRecipe("cvt_mithril_ingot", "mat_mithril_ingot", "Mithril Ingot",
                CraftingCategory.Special,
                new[] { new CraftingRecipeRequirement("mat_mithril_ore", 3), new CraftingRecipeRequirement("mat_magic_dust", 1) },
                25, 200));
        }

        private void AddRecipe(CraftingRecipe recipe)
        {
            if (recipes.ContainsKey(recipe.recipeId))
            {
                Debug.LogWarning($"[CraftingSystem] Duplicate recipe ID: {recipe.recipeId}");
                return;
            }
            recipes[recipe.recipeId] = recipe;
        }
    }
}
