using UnityEngine;
using UnityEditor;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// ItemData ScriptableObject들을 생성하는 에디터 도구
    /// </summary>
    public class ItemDataCreator : EditorWindow
    {
        [MenuItem("Tools/Dungeon Crawler/Create Basic Items")]
        public static void CreateBasicItems()
        {
            // Resources/Items 폴더가 없으면 생성
            string folderPath = "Assets/Resources/Items";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Items");
            }
            
            Debug.Log("📦 Creating basic test items...");
            
            // 기본 무기들
            CreateSword();
            CreateBow();
            CreateStaff();
            
            // 기본 방어구들
            CreateHelmet();
            CreateChestArmor();
            
            // 기본 소모품들
            CreateHealthPotion();
            CreateManaPotion();
            
            // 기본 재료들
            CreateIronOre();
            CreateMagicStone();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("✅ Basic items created successfully!");
        }
        
        private static void CreateSword()
        {
            var sword = ScriptableObject.CreateInstance<ItemData>();
            
            // 리플렉션으로 private 필드 설정
            var type = typeof(ItemData);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            
            type.GetField("itemId", flags)?.SetValue(sword, "weapon_sword_basic");
            type.GetField("itemName", flags)?.SetValue(sword, "낡은 검");
            type.GetField("description", flags)?.SetValue(sword, "초보자를 위한 기본적인 검이다.");
            type.GetField("itemType", flags)?.SetValue(sword, ItemType.Equipment);
            type.GetField("grade", flags)?.SetValue(sword, ItemGrade.Common);
            type.GetField("equipmentSlot", flags)?.SetValue(sword, EquipmentSlot.MainHand);
            type.GetField("weaponCategory", flags)?.SetValue(sword, WeaponCategory.Sword);
            type.GetField("stackSize", flags)?.SetValue(sword, 1);
            type.GetField("sellPrice", flags)?.SetValue(sword, 100L);
            type.GetField("durability", flags)?.SetValue(sword, 100);
            type.GetField("maxDurability", flags)?.SetValue(sword, 100);
            type.GetField("gradeColor", flags)?.SetValue(sword, Color.white);
            
            // 스탯 보너스 설정
            var stats = new StatBlock { strength = 2 };
            type.GetField("statBonuses", flags)?.SetValue(sword, stats);
            
            // 무기 데미지 설정
            var damageRange = new DamageRange(8, 12, 0);
            type.GetField("weaponDamageRange", flags)?.SetValue(sword, damageRange);
            
            AssetDatabase.CreateAsset(sword, "Assets/Resources/Items/BasicSword.asset");
        }
        
        private static void CreateBow()
        {
            var bow = ScriptableObject.CreateInstance<ItemData>();
            
            var type = typeof(ItemData);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            
            type.GetField("itemId", flags)?.SetValue(bow, "weapon_bow_basic");
            type.GetField("itemName", flags)?.SetValue(bow, "낡은 활");
            type.GetField("description", flags)?.SetValue(bow, "초보자를 위한 기본적인 활이다.");
            type.GetField("itemType", flags)?.SetValue(bow, ItemType.Equipment);
            type.GetField("grade", flags)?.SetValue(bow, ItemGrade.Common);
            type.GetField("equipmentSlot", flags)?.SetValue(bow, EquipmentSlot.TwoHand);
            type.GetField("weaponCategory", flags)?.SetValue(bow, WeaponCategory.Bow);
            type.GetField("stackSize", flags)?.SetValue(bow, 1);
            type.GetField("sellPrice", flags)?.SetValue(bow, 80L);
            type.GetField("durability", flags)?.SetValue(bow, 100);
            type.GetField("maxDurability", flags)?.SetValue(bow, 100);
            type.GetField("gradeColor", flags)?.SetValue(bow, Color.white);
            
            var stats = new StatBlock { agility = 2 };
            type.GetField("statBonuses", flags)?.SetValue(bow, stats);
            
            var damageRange = new DamageRange(6, 10, 0);
            type.GetField("weaponDamageRange", flags)?.SetValue(bow, damageRange);
            
            AssetDatabase.CreateAsset(bow, "Assets/Resources/Items/BasicBow.asset");
        }
        
        private static void CreateStaff()
        {
            var staff = ScriptableObject.CreateInstance<ItemData>();
            
            var type = typeof(ItemData);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            
            type.GetField("itemId", flags)?.SetValue(staff, "weapon_staff_basic");
            type.GetField("itemName", flags)?.SetValue(staff, "낡은 지팡이");
            type.GetField("description", flags)?.SetValue(staff, "초보자를 위한 기본적인 지팡이다.");
            type.GetField("itemType", flags)?.SetValue(staff, ItemType.Equipment);
            type.GetField("grade", flags)?.SetValue(staff, ItemGrade.Common);
            type.GetField("equipmentSlot", flags)?.SetValue(staff, EquipmentSlot.TwoHand);
            type.GetField("weaponCategory", flags)?.SetValue(staff, WeaponCategory.Staff);
            type.GetField("stackSize", flags)?.SetValue(staff, 1);
            type.GetField("sellPrice", flags)?.SetValue(staff, 90L);
            type.GetField("durability", flags)?.SetValue(staff, 100);
            type.GetField("maxDurability", flags)?.SetValue(staff, 100);
            type.GetField("gradeColor", flags)?.SetValue(staff, Color.white);
            
            var stats = new StatBlock { intelligence = 2 };
            type.GetField("statBonuses", flags)?.SetValue(staff, stats);
            
            var damageRange = new DamageRange(5, 8, 0);
            type.GetField("weaponDamageRange", flags)?.SetValue(staff, damageRange);
            
            AssetDatabase.CreateAsset(staff, "Assets/Resources/Items/BasicStaff.asset");
        }
        
        private static void CreateHelmet()
        {
            var helmet = ScriptableObject.CreateInstance<ItemData>();
            
            var type = typeof(ItemData);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            
            type.GetField("itemId", flags)?.SetValue(helmet, "armor_helmet_basic");
            type.GetField("itemName", flags)?.SetValue(helmet, "가죽 모자");
            type.GetField("description", flags)?.SetValue(helmet, "기본적인 가죽으로 만든 모자이다.");
            type.GetField("itemType", flags)?.SetValue(helmet, ItemType.Equipment);
            type.GetField("grade", flags)?.SetValue(helmet, ItemGrade.Common);
            type.GetField("equipmentSlot", flags)?.SetValue(helmet, EquipmentSlot.Head);
            type.GetField("weaponCategory", flags)?.SetValue(helmet, WeaponCategory.None);
            type.GetField("stackSize", flags)?.SetValue(helmet, 1);
            type.GetField("sellPrice", flags)?.SetValue(helmet, 50L);
            type.GetField("durability", flags)?.SetValue(helmet, 100);
            type.GetField("maxDurability", flags)?.SetValue(helmet, 100);
            type.GetField("gradeColor", flags)?.SetValue(helmet, Color.white);
            
            var stats = new StatBlock { defense = 2, vitality = 1 };
            type.GetField("statBonuses", flags)?.SetValue(helmet, stats);
            
            AssetDatabase.CreateAsset(helmet, "Assets/Resources/Items/BasicHelmet.asset");
        }
        
        private static void CreateChestArmor()
        {
            var chest = ScriptableObject.CreateInstance<ItemData>();
            
            var type = typeof(ItemData);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            
            type.GetField("itemId", flags)?.SetValue(chest, "armor_chest_basic");
            type.GetField("itemName", flags)?.SetValue(chest, "가죽 갑옷");
            type.GetField("description", flags)?.SetValue(chest, "기본적인 가죽으로 만든 갑옷이다.");
            type.GetField("itemType", flags)?.SetValue(chest, ItemType.Equipment);
            type.GetField("grade", flags)?.SetValue(chest, ItemGrade.Common);
            type.GetField("equipmentSlot", flags)?.SetValue(chest, EquipmentSlot.Chest);
            type.GetField("weaponCategory", flags)?.SetValue(chest, WeaponCategory.None);
            type.GetField("stackSize", flags)?.SetValue(chest, 1);
            type.GetField("sellPrice", flags)?.SetValue(chest, 100L);
            type.GetField("durability", flags)?.SetValue(chest, 100);
            type.GetField("maxDurability", flags)?.SetValue(chest, 100);
            type.GetField("gradeColor", flags)?.SetValue(chest, Color.white);
            
            var stats = new StatBlock { defense = 5, vitality = 2 };
            type.GetField("statBonuses", flags)?.SetValue(chest, stats);
            
            AssetDatabase.CreateAsset(chest, "Assets/Resources/Items/BasicChestArmor.asset");
        }
        
        private static void CreateHealthPotion()
        {
            var potion = ScriptableObject.CreateInstance<ItemData>();
            
            var type = typeof(ItemData);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            
            type.GetField("itemId", flags)?.SetValue(potion, "consumable_hp_small");
            type.GetField("itemName", flags)?.SetValue(potion, "소형 체력 포션");
            type.GetField("description", flags)?.SetValue(potion, "체력을 50 회복시켜준다.");
            type.GetField("itemType", flags)?.SetValue(potion, ItemType.Consumable);
            type.GetField("grade", flags)?.SetValue(potion, ItemGrade.Common);
            type.GetField("equipmentSlot", flags)?.SetValue(potion, EquipmentSlot.None);
            type.GetField("weaponCategory", flags)?.SetValue(potion, WeaponCategory.None);
            type.GetField("stackSize", flags)?.SetValue(potion, 20);
            type.GetField("sellPrice", flags)?.SetValue(potion, 20L);
            type.GetField("durability", flags)?.SetValue(potion, 0);
            type.GetField("maxDurability", flags)?.SetValue(potion, 0);
            type.GetField("gradeColor", flags)?.SetValue(potion, Color.white);
            type.GetField("healAmount", flags)?.SetValue(potion, 50f);
            
            AssetDatabase.CreateAsset(potion, "Assets/Resources/Items/HealthPotion.asset");
        }
        
        private static void CreateManaPotion()
        {
            var potion = ScriptableObject.CreateInstance<ItemData>();
            
            var type = typeof(ItemData);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            
            type.GetField("itemId", flags)?.SetValue(potion, "consumable_mp_small");
            type.GetField("itemName", flags)?.SetValue(potion, "소형 마나 포션");
            type.GetField("description", flags)?.SetValue(potion, "마나를 30 회복시켜준다.");
            type.GetField("itemType", flags)?.SetValue(potion, ItemType.Consumable);
            type.GetField("grade", flags)?.SetValue(potion, ItemGrade.Common);
            type.GetField("equipmentSlot", flags)?.SetValue(potion, EquipmentSlot.None);
            type.GetField("weaponCategory", flags)?.SetValue(potion, WeaponCategory.None);
            type.GetField("stackSize", flags)?.SetValue(potion, 20);
            type.GetField("sellPrice", flags)?.SetValue(potion, 25L);
            type.GetField("durability", flags)?.SetValue(potion, 0);
            type.GetField("maxDurability", flags)?.SetValue(potion, 0);
            type.GetField("gradeColor", flags)?.SetValue(potion, Color.white);
            type.GetField("manaAmount", flags)?.SetValue(potion, 30f);
            
            AssetDatabase.CreateAsset(potion, "Assets/Resources/Items/ManaPotion.asset");
        }
        
        private static void CreateIronOre()
        {
            var ore = ScriptableObject.CreateInstance<ItemData>();
            
            var type = typeof(ItemData);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            
            type.GetField("itemId", flags)?.SetValue(ore, "material_iron_ore");
            type.GetField("itemName", flags)?.SetValue(ore, "철광석");
            type.GetField("description", flags)?.SetValue(ore, "무기와 방어구 제작에 사용되는 기본 재료이다.");
            type.GetField("itemType", flags)?.SetValue(ore, ItemType.Material);
            type.GetField("grade", flags)?.SetValue(ore, ItemGrade.Common);
            type.GetField("equipmentSlot", flags)?.SetValue(ore, EquipmentSlot.None);
            type.GetField("weaponCategory", flags)?.SetValue(ore, WeaponCategory.None);
            type.GetField("stackSize", flags)?.SetValue(ore, 99);
            type.GetField("sellPrice", flags)?.SetValue(ore, 5L);
            type.GetField("durability", flags)?.SetValue(ore, 0);
            type.GetField("maxDurability", flags)?.SetValue(ore, 0);
            type.GetField("gradeColor", flags)?.SetValue(ore, Color.white);
            
            AssetDatabase.CreateAsset(ore, "Assets/Resources/Items/IronOre.asset");
        }
        
        private static void CreateMagicStone()
        {
            var stone = ScriptableObject.CreateInstance<ItemData>();
            
            var type = typeof(ItemData);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            
            type.GetField("itemId", flags)?.SetValue(stone, "material_magic_stone");
            type.GetField("itemName", flags)?.SetValue(stone, "마법석");
            type.GetField("description", flags)?.SetValue(stone, "마법 무기 제작에 필요한 신비한 돌이다.");
            type.GetField("itemType", flags)?.SetValue(stone, ItemType.Material);
            type.GetField("grade", flags)?.SetValue(stone, ItemGrade.Rare);
            type.GetField("equipmentSlot", flags)?.SetValue(stone, EquipmentSlot.None);
            type.GetField("weaponCategory", flags)?.SetValue(stone, WeaponCategory.None);
            type.GetField("stackSize", flags)?.SetValue(stone, 50);
            type.GetField("sellPrice", flags)?.SetValue(stone, 100L);
            type.GetField("durability", flags)?.SetValue(stone, 0);
            type.GetField("maxDurability", flags)?.SetValue(stone, 0);
            type.GetField("gradeColor", flags)?.SetValue(stone, Color.blue);
            
            AssetDatabase.CreateAsset(stone, "Assets/Resources/Items/MagicStone.asset");
        }
    }
}