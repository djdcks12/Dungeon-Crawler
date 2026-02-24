using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 크래프팅 재료 아이템 20종 + 레시피 30종 자동 생성
    /// </summary>
    public class CraftingDataGenerator
    {
        private static string materialPath = "Assets/Resources/Items/Materials";
        private static string recipePath = "Assets/Resources/ScriptableObjects/CraftingRecipes";

        [MenuItem("Dungeon Crawler/Generate Crafting Data")]
        public static void Generate()
        {
            EnsureFolder(materialPath);
            EnsureFolder(recipePath);

            int matCount = GenerateMaterials();
            int recipeCount = GenerateRecipes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CraftingDataGenerator] 재료 {matCount}개 + 레시피 {recipeCount}개 생성 완료");
        }

        // ===================== 재료 아이템 20종 =====================
        private static int GenerateMaterials()
        {
            int count = 0;

            // 기본 재료 (드롭)
            count += CreateMaterial("mat_iron_ore", "철광석", "기본 금속 재료. 다양한 무기와 방어구의 기초.", 10, 20);
            count += CreateMaterial("mat_leather", "가죽", "동물 가죽. 가벼운 방어구와 장갑에 사용.", 8, 20);
            count += CreateMaterial("mat_wood", "목재", "튼튼한 나무. 지팡이와 활의 재료.", 5, 20);
            count += CreateMaterial("mat_cloth", "천 조각", "마법이 깃든 천. 마법사 장비에 사용.", 6, 20);
            count += CreateMaterial("mat_bone", "뼈 조각", "언데드에서 얻은 뼈. 특수 무기에 사용.", 12, 20);
            count += CreateMaterial("mat_crystal", "마력 수정", "마력이 응축된 수정. 마법 장비에 핵심 재료.", 25, 15);
            count += CreateMaterial("mat_herb", "약초", "치유 효과가 있는 풀. 포션 제작에 사용.", 3, 30);
            count += CreateMaterial("mat_poison_gland", "독 분비선", "독을 가진 몬스터에서 채취. 독 무기/포션에 사용.", 15, 15);

            // 중급 재료
            count += CreateMaterial("mat_steel_ingot", "강철 주괴", "정제된 강철. 고급 무기와 방어구에 필요.", 50, 10);
            count += CreateMaterial("mat_mithril_ore", "미스릴 광석", "희귀한 금속. 최고급 장비의 핵심 재료.", 200, 5);
            count += CreateMaterial("mat_dragon_scale", "드래곤 비늘", "드래곤에서 획득. 최강의 방어구 재료.", 500, 3);
            count += CreateMaterial("mat_demon_essence", "악마의 정수", "악마에서 추출한 에너지. 강력한 무기에 사용.", 300, 5);
            count += CreateMaterial("mat_elemental_core", "원소 핵", "원소 몬스터에서 획득. 마법 장비 강화에 필수.", 150, 5);

            // 특수 재료
            count += CreateMaterial("mat_enchant_dust", "인챈트 가루", "마법 분해로 얻은 가루. 강화 재료에 사용.", 30, 20);
            count += CreateMaterial("mat_soul_fragment", "영혼 파편", "몬스터 영혼의 조각. 특수 제작에 사용.", 80, 10);
            count += CreateMaterial("mat_ancient_rune", "고대 룬", "고대 문명의 룬. 전설급 장비에 필요.", 1000, 3);
            count += CreateMaterial("mat_moonstone", "달빛석", "밤에만 빛나는 보석. 야간 장비에 사용.", 120, 8);
            count += CreateMaterial("mat_fire_essence", "화염 정수", "화염 원소에서 추출. 화염 무기에 사용.", 100, 10);
            count += CreateMaterial("mat_ice_shard", "얼음 파편", "얼음 원소에서 채취. 냉기 장비에 사용.", 100, 10);
            count += CreateMaterial("mat_lightning_pearl", "번개 구슬", "번개의 에너지가 담긴 구슬. 전기 장비에 사용.", 100, 10);

            return count;
        }

        // ===================== 레시피 30종 =====================
        private static int GenerateRecipes()
        {
            int count = 0;

            // --- 소모품 레시피 (10개) ---
            count += CreateRecipe("recipe_health_small", "소형 체력 포션 제작", "약초로 기본 회복 포션을 만든다.",
                CraftingCategory.Consumable, 1, 0,
                new CM[] { new CM("mat_herb", 3) },
                "HealthPotion_Small", 2, ItemGrade.Common, 1f, 50, 1f, 0.05f, 5, true);

            count += CreateRecipe("recipe_health_medium", "중형 체력 포션 제작", "더 강력한 회복 포션.",
                CraftingCategory.Consumable, 3, 2,
                new CM[] { new CM("mat_herb", 5), new CM("mat_crystal", 1) },
                "HealthPotion_Medium", 2, ItemGrade.Common, 1.5f, 100, 1f, 0.05f, 10, true);

            count += CreateRecipe("recipe_health_large", "대형 체력 포션 제작", "대량 회복 포션.",
                CraftingCategory.Consumable, 5, 5,
                new CM[] { new CM("mat_herb", 8), new CM("mat_crystal", 2) },
                "HealthPotion_Large", 2, ItemGrade.Common, 2f, 200, 0.95f, 0.08f, 15, true);

            count += CreateRecipe("recipe_mana_small", "소형 마나 포션 제작", "마나 회복 포션.",
                CraftingCategory.Consumable, 1, 0,
                new CM[] { new CM("mat_herb", 2), new CM("mat_crystal", 1) },
                "ManaPotion_Small", 2, ItemGrade.Common, 1f, 50, 1f, 0.05f, 5, true);

            count += CreateRecipe("recipe_mana_medium", "중형 마나 포션 제작", "더 강력한 마나 포션.",
                CraftingCategory.Consumable, 3, 2,
                new CM[] { new CM("mat_herb", 4), new CM("mat_crystal", 2) },
                "ManaPotion_Medium", 2, ItemGrade.Common, 1.5f, 100, 1f, 0.05f, 10, true);

            count += CreateRecipe("recipe_antidote", "해독제 제작", "독을 치료하는 해독제.",
                CraftingCategory.Consumable, 2, 1,
                new CM[] { new CM("mat_herb", 3), new CM("mat_poison_gland", 1) },
                "PoisonAntidote", 3, ItemGrade.Common, 1f, 80, 1f, 0.03f, 8, true);

            count += CreateRecipe("recipe_str_scroll", "힘의 두루마리 제작", "임시 힘 증가 두루마리.",
                CraftingCategory.Consumable, 4, 3,
                new CM[] { new CM("mat_cloth", 2), new CM("mat_enchant_dust", 3) },
                "StrengthScroll", 1, ItemGrade.Common, 2f, 150, 0.95f, 0.05f, 12, true);

            count += CreateRecipe("recipe_speed_scroll", "속도 두루마리 제작", "임시 속도 증가 두루마리.",
                CraftingCategory.Consumable, 4, 3,
                new CM[] { new CM("mat_cloth", 2), new CM("mat_enchant_dust", 3) },
                "SpeedScroll", 1, ItemGrade.Common, 2f, 150, 0.95f, 0.05f, 12, true);

            count += CreateRecipe("recipe_protection_scroll", "보호 두루마리 제작", "임시 방어력 증가 두루마리.",
                CraftingCategory.Consumable, 4, 3,
                new CM[] { new CM("mat_cloth", 2), new CM("mat_enchant_dust", 3) },
                "ProtectionScroll", 1, ItemGrade.Common, 2f, 150, 0.95f, 0.05f, 12, true);

            count += CreateRecipe("recipe_resurrection", "부활 두루마리 제작", "죽은 자를 부활시키는 두루마리.",
                CraftingCategory.Consumable, 8, 10,
                new CM[] { new CM("mat_cloth", 5), new CM("mat_crystal", 5), new CM("mat_soul_fragment", 3) },
                "ResurrectionScroll", 1, ItemGrade.Common, 5f, 1000, 0.7f, 0.1f, 30, false, "quest_main_07");

            // --- 무기 레시피 (10개) ---
            count += CreateRecipe("recipe_iron_sword", "철 장검 제작", "기본 철 장검.",
                CraftingCategory.Weapon, 2, 1,
                new CM[] { new CM("mat_iron_ore", 5), new CM("mat_wood", 2) },
                "Longsword_Common", 1, ItemGrade.Common, 3f, 100, 1f, 0.08f, 10, true);

            count += CreateRecipe("recipe_steel_sword", "강철 장검 제작", "정제된 강철로 만든 장검.",
                CraftingCategory.Weapon, 5, 5,
                new CM[] { new CM("mat_steel_ingot", 3), new CM("mat_wood", 2), new CM("mat_leather", 1) },
                "Longsword_Uncommon", 1, ItemGrade.Uncommon, 5f, 300, 0.9f, 0.1f, 20, true);

            count += CreateRecipe("recipe_fire_staff", "화염 지팡이 제작", "화염 마법이 깃든 지팡이.",
                CraftingCategory.Weapon, 4, 3,
                new CM[] { new CM("mat_wood", 4), new CM("mat_fire_essence", 3), new CM("mat_crystal", 2) },
                "FireStaff_Uncommon", 1, ItemGrade.Uncommon, 4f, 250, 0.9f, 0.1f, 18, true);

            count += CreateRecipe("recipe_ice_wand", "얼음 완드 제작", "냉기 마법이 깃든 완드.",
                CraftingCategory.Weapon, 4, 3,
                new CM[] { new CM("mat_wood", 3), new CM("mat_ice_shard", 3), new CM("mat_crystal", 2) },
                "CrystalWand_Uncommon", 1, ItemGrade.Uncommon, 4f, 250, 0.9f, 0.1f, 18, true);

            count += CreateRecipe("recipe_bone_dagger", "뼈 단검 제작", "언데드의 뼈로 만든 날카로운 단검.",
                CraftingCategory.Weapon, 3, 2,
                new CM[] { new CM("mat_bone", 4), new CM("mat_leather", 2) },
                "Dagger_Uncommon", 1, ItemGrade.Uncommon, 3f, 200, 0.95f, 0.08f, 15, true);

            count += CreateRecipe("recipe_mithril_greatsword", "미스릴 대검 제작", "미스릴로 단조한 최고급 대검.",
                CraftingCategory.Weapon, 8, 12,
                new CM[] { new CM("mat_mithril_ore", 5), new CM("mat_steel_ingot", 3), new CM("mat_leather", 2) },
                "Greatsword_Rare", 1, ItemGrade.Rare, 8f, 1000, 0.7f, 0.15f, 40, false, "quest_main_10");

            count += CreateRecipe("recipe_dragon_axe", "드래곤 전투도끼 제작", "드래곤 재료로 만든 전투도끼.",
                CraftingCategory.Weapon, 10, 15,
                new CM[] { new CM("mat_dragon_scale", 3), new CM("mat_mithril_ore", 3), new CM("mat_fire_essence", 5) },
                "Battleaxe_Epic", 1, ItemGrade.Epic, 10f, 3000, 0.5f, 0.2f, 60, false, "quest_main_12");

            count += CreateRecipe("recipe_demon_blade", "악마의 검 제작", "악마의 정수로 만든 저주받은 검.",
                CraftingCategory.Weapon, 10, 15,
                new CM[] { new CM("mat_demon_essence", 5), new CM("mat_steel_ingot", 4), new CM("mat_soul_fragment", 3) },
                "Broadsword_Epic", 1, ItemGrade.Epic, 10f, 3000, 0.5f, 0.2f, 60, false);

            count += CreateRecipe("recipe_lightning_bow", "번개 활 제작", "번개의 힘이 깃든 활.",
                CraftingCategory.Weapon, 6, 8,
                new CM[] { new CM("mat_wood", 5), new CM("mat_lightning_pearl", 3), new CM("mat_leather", 3) },
                "Longbow_Rare", 1, ItemGrade.Rare, 6f, 600, 0.8f, 0.12f, 25, true);

            count += CreateRecipe("recipe_ancient_staff", "고대 지팡이 제작", "고대 룬으로 강화된 전설의 지팡이.",
                CraftingCategory.Weapon, 12, 20,
                new CM[] { new CM("mat_ancient_rune", 2), new CM("mat_elemental_core", 5), new CM("mat_crystal", 8) },
                "HolyStaff_Legendary", 1, ItemGrade.Legendary, 15f, 10000, 0.3f, 0.25f, 100, false);

            // --- 방어구 레시피 (5개) ---
            count += CreateRecipe("recipe_iron_helmet", "철 투구 제작", "기본 철 투구.",
                CraftingCategory.Armor, 2, 1,
                new CM[] { new CM("mat_iron_ore", 4), new CM("mat_leather", 1) },
                "Helmet_Common", 1, ItemGrade.Common, 3f, 80, 1f, 0.08f, 8, true);

            count += CreateRecipe("recipe_steel_chest", "강철 흉갑 제작", "정제된 강철 흉갑.",
                CraftingCategory.Armor, 5, 5,
                new CM[] { new CM("mat_steel_ingot", 5), new CM("mat_leather", 3) },
                "ChestArmor_Uncommon", 1, ItemGrade.Uncommon, 6f, 400, 0.9f, 0.1f, 22, true);

            count += CreateRecipe("recipe_mithril_greaves", "미스릴 경갑 제작", "미스릴 경갑.",
                CraftingCategory.Armor, 8, 10,
                new CM[] { new CM("mat_mithril_ore", 4), new CM("mat_leather", 3), new CM("mat_cloth", 2) },
                "Greaves_Rare", 1, ItemGrade.Rare, 7f, 800, 0.75f, 0.12f, 35, false);

            count += CreateRecipe("recipe_dragon_boots", "드래곤 장화 제작", "드래곤 비늘로 만든 장화.",
                CraftingCategory.Armor, 10, 15,
                new CM[] { new CM("mat_dragon_scale", 2), new CM("mat_leather", 4), new CM("mat_steel_ingot", 2) },
                "Boots_Epic", 1, ItemGrade.Epic, 8f, 2000, 0.6f, 0.15f, 50, false);

            count += CreateRecipe("recipe_ancient_armor", "고대 전신갑 제작", "고대 문명의 전설적 갑옷.",
                CraftingCategory.Armor, 12, 20,
                new CM[] { new CM("mat_ancient_rune", 2), new CM("mat_dragon_scale", 3), new CM("mat_mithril_ore", 5) },
                "ChestArmor_Legendary", 1, ItemGrade.Legendary, 15f, 10000, 0.3f, 0.2f, 100, false);

            // --- 강화 재료 레시피 (5개) ---
            count += CreateRecipe("recipe_steel_ingot", "강철 주괴 정련", "철광석을 정제하여 강철 주괴를 만든다.",
                CraftingCategory.Enhancement, 3, 2,
                new CM[] { new CM("mat_iron_ore", 5) },
                "mat_steel_ingot", 1, ItemGrade.Common, 2f, 50, 1f, 0.1f, 8, true);

            count += CreateRecipe("recipe_enchant_dust", "인챈트 가루 제조", "수정을 가루로 만들어 인챈트 재료를 얻는다.",
                CraftingCategory.Enhancement, 3, 2,
                new CM[] { new CM("mat_crystal", 3) },
                "mat_enchant_dust", 3, ItemGrade.Common, 2f, 30, 1f, 0.05f, 8, true);

            count += CreateRecipe("recipe_elemental_core", "원소 핵 합성", "원소 정수들을 합성하여 원소 핵을 만든다.",
                CraftingCategory.Enhancement, 6, 8,
                new CM[] { new CM("mat_fire_essence", 2), new CM("mat_ice_shard", 2), new CM("mat_lightning_pearl", 2) },
                "mat_elemental_core", 1, ItemGrade.Common, 4f, 200, 0.8f, 0.1f, 20, false);

            count += CreateRecipe("recipe_soul_fragment", "영혼 파편 응축", "인챈트 가루와 뼈를 합성하여 영혼 파편을 만든다.",
                CraftingCategory.Enhancement, 5, 5,
                new CM[] { new CM("mat_enchant_dust", 5), new CM("mat_bone", 5) },
                "mat_soul_fragment", 1, ItemGrade.Common, 3f, 150, 0.85f, 0.08f, 15, false);

            count += CreateRecipe("recipe_moonstone", "달빛석 연마", "수정과 영혼 파편을 밤에 합성하여 달빛석을 만든다.",
                CraftingCategory.Enhancement, 7, 10,
                new CM[] { new CM("mat_crystal", 5), new CM("mat_soul_fragment", 2) },
                "mat_moonstone", 1, ItemGrade.Common, 5f, 300, 0.7f, 0.1f, 25, false);

            return count;
        }

        // ===================== 헬퍼: 재료 아이템 생성 =====================
        private static int CreateMaterial(string id, string name, string desc, long price, int maxStack)
        {
            string assetPath = $"{materialPath}/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<ItemData>(assetPath) != null) return 0;

            var data = ScriptableObject.CreateInstance<ItemData>();
            var so = new SerializedObject(data);

            so.FindProperty("itemId").stringValue = id;
            so.FindProperty("itemName").stringValue = name;
            so.FindProperty("description").stringValue = desc;
            so.FindProperty("itemType").enumValueIndex = (int)ItemType.Material;
            so.FindProperty("grade").enumValueIndex = 0; // Common
            so.FindProperty("equipmentSlot").enumValueIndex = 0; // None
            so.FindProperty("stackSize").intValue = maxStack;
            so.FindProperty("sellPrice").longValue = price;
            so.FindProperty("isDroppable").boolValue = true;
            so.FindProperty("isDestroyable").boolValue = true;

            // 등급 색상 (재료는 회색)
            so.FindProperty("gradeColor").colorValue = new Color(0.7f, 0.7f, 0.7f);

            so.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(data, assetPath);
            return 1;
        }

        // ===================== 헬퍼: 레시피 생성 =====================
        // CM = CraftingMaterial 축약
        private struct CM
        {
            public string itemId;
            public int quantity;
            public CM(string id, int qty) { itemId = id; quantity = qty; }
        }

        private static int CreateRecipe(string id, string name, string desc,
            CraftingCategory category, int reqLevel, int reqCraftLevel,
            CM[] materials, string resultItemId, int resultCount, ItemGrade resultGrade,
            float craftTime, long goldCost, float successRate, float critRate, int craftExp,
            bool defaultUnlocked, string unlockQuest = "")
        {
            string assetPath = $"{recipePath}/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<CraftingRecipeData>(assetPath) != null) return 0;

            var data = ScriptableObject.CreateInstance<CraftingRecipeData>();
            var so = new SerializedObject(data);

            so.FindProperty("recipeId").stringValue = id;
            so.FindProperty("recipeName").stringValue = name;
            so.FindProperty("description").stringValue = desc;
            so.FindProperty("category").enumValueIndex = (int)category;
            so.FindProperty("requiredLevel").intValue = reqLevel;
            so.FindProperty("requiredCraftingLevel").intValue = reqCraftLevel;
            so.FindProperty("resultItemId").stringValue = resultItemId;
            so.FindProperty("resultCount").intValue = resultCount;
            so.FindProperty("resultGrade").enumValueIndex = (int)resultGrade - 1; // ItemGrade starts at 1
            so.FindProperty("craftTime").floatValue = craftTime;
            so.FindProperty("goldCost").longValue = goldCost;
            so.FindProperty("successRate").floatValue = successRate;
            so.FindProperty("criticalRate").floatValue = critRate;
            so.FindProperty("craftingExpReward").intValue = craftExp;
            so.FindProperty("isDefaultUnlocked").boolValue = defaultUnlocked;
            so.FindProperty("unlockQuestId").stringValue = unlockQuest ?? "";

            // 재료 배열 설정
            var matProp = so.FindProperty("materials");
            matProp.arraySize = materials.Length;
            for (int i = 0; i < materials.Length; i++)
            {
                var elem = matProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("itemId").stringValue = materials[i].itemId;
                elem.FindPropertyRelative("quantity").intValue = materials[i].quantity;
            }

            so.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(data, assetPath);
            return 1;
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] parts = path.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }
        }
    }
}
