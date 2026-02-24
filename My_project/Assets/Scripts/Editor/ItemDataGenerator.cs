using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 아이템 데이터 자동 생성기 (무기 150 + 방어구 20 + 소모품 15 = 185개)
    /// </summary>
    public class ItemDataGenerator
    {
        private static string weaponPath = "Assets/Resources/Items/Weapons";
        private static string armorPath = "Assets/Resources/Items/Armor";
        private static string consumablePath = "Assets/Resources/Items/Consumables";

        // 등급별 배율
        private static readonly Dictionary<ItemGrade, float> DamageMultiplier = new()
        {
            { ItemGrade.Common, 1.0f },
            { ItemGrade.Uncommon, 1.3f },
            { ItemGrade.Rare, 1.7f },
            { ItemGrade.Epic, 2.2f },
            { ItemGrade.Legendary, 3.0f }
        };
        private static readonly Dictionary<ItemGrade, float> PriceMultiplier = new()
        {
            { ItemGrade.Common, 1f },
            { ItemGrade.Uncommon, 1.5f },
            { ItemGrade.Rare, 3f },
            { ItemGrade.Epic, 7f },
            { ItemGrade.Legendary, 20f }
        };
        private static readonly Dictionary<ItemGrade, float> StatMultiplier = new()
        {
            { ItemGrade.Common, 1.0f },
            { ItemGrade.Uncommon, 1.3f },
            { ItemGrade.Rare, 1.7f },
            { ItemGrade.Epic, 2.2f },
            { ItemGrade.Legendary, 3.0f }
        };

        [MenuItem("Dungeon Crawler/Generate All Item Data (185)")]
        public static void GenerateAll()
        {
            EnsureFolder(weaponPath);
            EnsureFolder(armorPath);
            EnsureFolder(consumablePath);

            int total = 0;
            total += GenerateWeapons();
            total += GenerateArmor();
            total += GenerateConsumables();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ItemDataGenerator] 아이템 총 {total}개 생성 완료");
        }

        [MenuItem("Dungeon Crawler/Generate Weapons Only (150)")]
        public static void GenerateWeaponsOnly()
        {
            EnsureFolder(weaponPath);
            int count = GenerateWeapons();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ItemDataGenerator] 무기 {count}개 생성 완료");
        }

        [MenuItem("Dungeon Crawler/Generate Armor Only (20)")]
        public static void GenerateArmorOnly()
        {
            EnsureFolder(armorPath);
            int count = GenerateArmor();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ItemDataGenerator] 방어구 {count}개 생성 완료");
        }

        [MenuItem("Dungeon Crawler/Generate Consumables Only (15)")]
        public static void GenerateConsumablesOnly()
        {
            EnsureFolder(consumablePath);
            int count = GenerateConsumables();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ItemDataGenerator] 소모품 {count}개 생성 완료");
        }

        // =================== 무기 생성 ===================
        private struct WeaponDef
        {
            public string name;
            public WeaponType weaponType;
            public EquipmentSlot slot;
            public float baseDmg;
            public float critBonus;
            public float stability;
            public long basePrice;
            public DamageType damageType;
            // 스탯 보너스 (Common 기준)
            public float str, agi, vit, inte, def, mdef, luk, stab;
            public string desc;
        }

        private static int GenerateWeapons()
        {
            var weapons = new List<WeaponDef>
            {
                // === SwordShield 그룹 ===
                new() { name="Longsword", weaponType=WeaponType.Longsword, slot=EquipmentSlot.MainHand,
                    baseDmg=15, critBonus=0.05f, stability=2, basePrice=150, damageType=DamageType.Physical,
                    str=1, desc="균형 잡힌 한손검" },
                new() { name="Rapier", weaponType=WeaponType.Rapier, slot=EquipmentSlot.MainHand,
                    baseDmg=13, critBonus=0.08f, stability=1, basePrice=140, damageType=DamageType.Physical,
                    agi=2, desc="빠른 찌르기에 특화된 세검" },
                new() { name="Broadsword", weaponType=WeaponType.Broadsword, slot=EquipmentSlot.MainHand,
                    baseDmg=16, critBonus=0.03f, stability=3, basePrice=160, damageType=DamageType.Physical,
                    str=2, desc="넓은 칼날의 광검" },
                new() { name="Gladius", weaponType=WeaponType.Gladius, slot=EquipmentSlot.MainHand,
                    baseDmg=14, critBonus=0.06f, stability=2, basePrice=145, damageType=DamageType.Physical,
                    str=1, agi=1, desc="고대 전투용 단검" },
                new() { name="Scimitar", weaponType=WeaponType.Scimitar, slot=EquipmentSlot.MainHand,
                    baseDmg=14, critBonus=0.07f, stability=1, basePrice=150, damageType=DamageType.Physical,
                    agi=2, desc="곡선형 베기 검" },

                // === TwoHandedSword 그룹 ===
                new() { name="Greatsword", weaponType=WeaponType.Greatsword, slot=EquipmentSlot.TwoHand,
                    baseDmg=22, critBonus=0.04f, stability=4, basePrice=250, damageType=DamageType.Physical,
                    str=3, desc="거대한 양손 대검" },
                new() { name="Claymore", weaponType=WeaponType.Claymore, slot=EquipmentSlot.TwoHand,
                    baseDmg=24, critBonus=0.03f, stability=5, basePrice=270, damageType=DamageType.Physical,
                    str=4, desc="스코틀랜드식 대검" },
                new() { name="Flamberge", weaponType=WeaponType.Flamberge, slot=EquipmentSlot.TwoHand,
                    baseDmg=20, critBonus=0.06f, stability=3, basePrice=240, damageType=DamageType.Physical,
                    str=2, stab=1, desc="물결 모양 칼날의 대검" },
                new() { name="Zweihander", weaponType=WeaponType.Zweihander, slot=EquipmentSlot.TwoHand,
                    baseDmg=25, critBonus=0.02f, stability=6, basePrice=280, damageType=DamageType.Physical,
                    str=4, stab=2, desc="독일식 초대형 양손검" },

                // === TwoHandedAxe 그룹 ===
                new() { name="Battleaxe", weaponType=WeaponType.Battleaxe, slot=EquipmentSlot.TwoHand,
                    baseDmg=24, critBonus=0.05f, stability=3, basePrice=260, damageType=DamageType.Physical,
                    str=3, vit=1, desc="전투용 대형 도끼" },
                new() { name="Warhammer", weaponType=WeaponType.Warhammer, slot=EquipmentSlot.TwoHand,
                    baseDmg=26, critBonus=0.03f, stability=5, basePrice=280, damageType=DamageType.Physical,
                    str=4, def=1, desc="갑옷 파괴용 전쟁 망치" },
                new() { name="Maul_Weapon", weaponType=WeaponType.Maul, slot=EquipmentSlot.TwoHand,
                    baseDmg=28, critBonus=0.02f, stability=6, basePrice=300, damageType=DamageType.Physical,
                    str=5, stab=2, desc="초대형 둔기" },
                new() { name="Greataxe", weaponType=WeaponType.Greataxe, slot=EquipmentSlot.TwoHand,
                    baseDmg=25, critBonus=0.06f, stability=2, basePrice=270, damageType=DamageType.Physical,
                    str=4, desc="거대한 양날 도끼" },

                // === Dagger 그룹 ===
                new() { name="Dagger", weaponType=WeaponType.Dagger, slot=EquipmentSlot.MainHand,
                    baseDmg=10, critBonus=0.12f, stability=0, basePrice=100, damageType=DamageType.Physical,
                    agi=2, desc="빠른 단검" },
                new() { name="CurvedDagger", weaponType=WeaponType.CurvedDagger, slot=EquipmentSlot.MainHand,
                    baseDmg=11, critBonus=0.14f, stability=0, basePrice=110, damageType=DamageType.Physical,
                    agi=3, desc="곡선형 암살 단검" },
                new() { name="Stiletto", weaponType=WeaponType.Stiletto, slot=EquipmentSlot.MainHand,
                    baseDmg=12, critBonus=0.10f, stability=1, basePrice=120, damageType=DamageType.Physical,
                    agi=2, luk=1, desc="가늘고 긴 찌르기 전용 단검" },
                new() { name="Kris", weaponType=WeaponType.Kris, slot=EquipmentSlot.MainHand,
                    baseDmg=10, critBonus=0.15f, stability=0, basePrice=105, damageType=DamageType.Physical,
                    agi=2, luk=1, desc="물결 모양 날의 의식용 단검" },

                // === Bow 그룹 ===
                new() { name="Longbow", weaponType=WeaponType.Longbow, slot=EquipmentSlot.TwoHand,
                    baseDmg=18, critBonus=0.07f, stability=2, basePrice=180, damageType=DamageType.Physical,
                    agi=2, desc="장거리 사격용 장궁" },
                new() { name="Crossbow", weaponType=WeaponType.Crossbow, slot=EquipmentSlot.TwoHand,
                    baseDmg=20, critBonus=0.05f, stability=3, basePrice=200, damageType=DamageType.Physical,
                    str=1, agi=1, desc="기계식 석궁" },
                new() { name="CompoundBow", weaponType=WeaponType.CompoundBow, slot=EquipmentSlot.TwoHand,
                    baseDmg=16, critBonus=0.09f, stability=1, basePrice=170, damageType=DamageType.Physical,
                    agi=3, desc="정밀한 복합 활" },
                new() { name="Shortbow", weaponType=WeaponType.Shortbow, slot=EquipmentSlot.TwoHand,
                    baseDmg=14, critBonus=0.10f, stability=1, basePrice=150, damageType=DamageType.Physical,
                    agi=2, luk=1, desc="가볍고 빠른 단궁" },

                // === Staff 그룹 ===
                new() { name="OakStaff", weaponType=WeaponType.OakStaff, slot=EquipmentSlot.TwoHand,
                    baseDmg=18, critBonus=0.03f, stability=2, basePrice=180, damageType=DamageType.Magical,
                    inte=2, desc="참나무 마법 지팡이" },
                new() { name="CrystalStaff", weaponType=WeaponType.CrystalStaff, slot=EquipmentSlot.TwoHand,
                    baseDmg=22, critBonus=0.04f, stability=3, basePrice=240, damageType=DamageType.Magical,
                    inte=4, mdef=2, desc="수정 마법 지팡이" },
                new() { name="FireStaff", weaponType=WeaponType.FireStaff, slot=EquipmentSlot.TwoHand,
                    baseDmg=20, critBonus=0.05f, stability=2, basePrice=220, damageType=DamageType.Magical,
                    inte=3, desc="화염 속성 지팡이" },
                new() { name="IceStaff", weaponType=WeaponType.IceStaff, slot=EquipmentSlot.TwoHand,
                    baseDmg=20, critBonus=0.05f, stability=2, basePrice=220, damageType=DamageType.Magical,
                    inte=3, desc="빙결 속성 지팡이" },
                new() { name="HolyStaff", weaponType=WeaponType.HolyStaff, slot=EquipmentSlot.TwoHand,
                    baseDmg=24, critBonus=0.03f, stability=4, basePrice=260, damageType=DamageType.Magical,
                    inte=4, mdef=3, desc="신성한 치유의 지팡이" },

                // === Wand 그룹 ===
                new() { name="MagicWand", weaponType=WeaponType.MagicWand, slot=EquipmentSlot.MainHand,
                    baseDmg=14, critBonus=0.06f, stability=1, basePrice=140, damageType=DamageType.Magical,
                    inte=2, desc="기본 마법봉" },
                new() { name="CrystalWand", weaponType=WeaponType.CrystalWand, slot=EquipmentSlot.MainHand,
                    baseDmg=16, critBonus=0.05f, stability=2, basePrice=160, damageType=DamageType.Magical,
                    inte=3, mdef=1, desc="수정 마법봉" },
                new() { name="RuneWand", weaponType=WeaponType.RuneWand, slot=EquipmentSlot.MainHand,
                    baseDmg=18, critBonus=0.04f, stability=2, basePrice=180, damageType=DamageType.Magical,
                    inte=3, luk=1, desc="룬 문양의 마법봉" },
                new() { name="ElementalWand", weaponType=WeaponType.ElementalWand, slot=EquipmentSlot.MainHand,
                    baseDmg=15, critBonus=0.06f, stability=1, basePrice=150, damageType=DamageType.Magical,
                    inte=2, desc="원소 마법봉" },

                // === Fist 그룹 ===
                new() { name="Fists", weaponType=WeaponType.Fists, slot=EquipmentSlot.MainHand,
                    baseDmg=8, critBonus=0.08f, stability=0, basePrice=0, damageType=DamageType.Physical,
                    desc="맨손 격투" },
                new() { name="Knuckles", weaponType=WeaponType.Knuckles, slot=EquipmentSlot.MainHand,
                    baseDmg=12, critBonus=0.10f, stability=1, basePrice=120, damageType=DamageType.Physical,
                    str=1, agi=1, desc="강철 너클" },
                new() { name="Claws_Weapon", weaponType=WeaponType.Claws, slot=EquipmentSlot.MainHand,
                    baseDmg=14, critBonus=0.12f, stability=0, basePrice=140, damageType=DamageType.Physical,
                    str=2, agi=2, desc="날카로운 전투용 클로" },
                new() { name="Gauntlets", weaponType=WeaponType.Gauntlets, slot=EquipmentSlot.MainHand,
                    baseDmg=13, critBonus=0.07f, stability=2, basePrice=130, damageType=DamageType.Physical,
                    str=2, def=1, desc="강철 건틀릿" },
            };

            int count = 0;
            var grades = new[] { ItemGrade.Common, ItemGrade.Uncommon, ItemGrade.Rare, ItemGrade.Epic, ItemGrade.Legendary };

            foreach (var w in weapons)
            {
                foreach (var grade in grades)
                {
                    if (CreateWeaponAsset(w, grade))
                        count++;
                }
            }

            return count;
        }

        private static bool CreateWeaponAsset(WeaponDef w, ItemGrade grade)
        {
            string gradeName = grade.ToString();
            string itemName = $"{w.name}_{gradeName}";
            string groupFolder = GetWeaponGroupFolder(w.weaponType);
            string folder = $"{weaponPath}/{groupFolder}";
            EnsureFolder(folder);
            string assetPath = $"{folder}/{itemName}.asset";

            if (AssetDatabase.LoadAssetAtPath<ItemData>(assetPath) != null)
                return false;

            float dmgMult = DamageMultiplier[grade];
            float priceMult = PriceMultiplier[grade];
            float statMult = StatMultiplier[grade];

            var item = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(item, assetPath);
            var so = new SerializedObject(item);

            so.FindProperty("itemId").stringValue = $"weapon_{w.name.ToLower()}_{gradeName.ToLower()}";
            so.FindProperty("itemName").stringValue = GetLocalizedWeaponName(w.name, grade);
            so.FindProperty("description").stringValue = w.desc;
            so.FindProperty("itemType").enumValueIndex = (int)ItemType.Equipment;
            so.FindProperty("grade").intValue = (int)grade;
            so.FindProperty("equipmentSlot").enumValueIndex = (int)w.slot;
            so.FindProperty("weaponType").enumValueIndex = (int)w.weaponType;
            so.FindProperty("stackSize").intValue = 1;
            so.FindProperty("sellPrice").longValue = (long)(w.basePrice * priceMult);
            so.FindProperty("criticalBonus").floatValue = w.critBonus * dmgMult;
            so.FindProperty("weaponDamageType").enumValueIndex = (int)w.damageType;

            // 데미지 범위
            var dmgRange = so.FindProperty("weaponDamageRange");
            float baseMin = w.baseDmg * 0.8f * dmgMult;
            float baseMax = w.baseDmg * 1.2f * dmgMult;
            dmgRange.FindPropertyRelative("minDamage").floatValue = Mathf.Round(baseMin);
            dmgRange.FindPropertyRelative("maxDamage").floatValue = Mathf.Round(baseMax);
            dmgRange.FindPropertyRelative("stability").floatValue = w.stability * dmgMult;

            // 스탯 보너스
            var stats = so.FindProperty("statBonuses");
            stats.FindPropertyRelative("strength").floatValue = Mathf.Round(w.str * statMult);
            stats.FindPropertyRelative("agility").floatValue = Mathf.Round(w.agi * statMult);
            stats.FindPropertyRelative("vitality").floatValue = Mathf.Round(w.vit * statMult);
            stats.FindPropertyRelative("intelligence").floatValue = Mathf.Round(w.inte * statMult);
            stats.FindPropertyRelative("defense").floatValue = Mathf.Round(w.def * statMult);
            stats.FindPropertyRelative("magicDefense").floatValue = Mathf.Round(w.mdef * statMult);
            stats.FindPropertyRelative("luck").floatValue = Mathf.Round(w.luk * statMult);
            stats.FindPropertyRelative("stability").floatValue = Mathf.Round(w.stab * statMult);

            // 등급 색상
            so.FindProperty("gradeColor").colorValue = ItemData.GetGradeColor(grade);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            return true;
        }

        // =================== 방어구 생성 ===================
        private struct ArmorDef
        {
            public string name;
            public EquipmentSlot slot;
            public float baseDef;
            public float baseMdef;
            public long basePrice;
            // 추가 스탯
            public float str, agi, vit, inte, luk, stab;
            public string desc;
        }

        private static int GenerateArmor()
        {
            var armors = new List<ArmorDef>
            {
                new() { name="Helmet", slot=EquipmentSlot.Head,
                    baseDef=5, baseMdef=3, basePrice=50,
                    desc="방어용 투구" },
                new() { name="ChestArmor", slot=EquipmentSlot.Chest,
                    baseDef=8, baseMdef=0, basePrice=80, vit=2,
                    desc="전투용 갑옷" },
                new() { name="Greaves", slot=EquipmentSlot.Legs,
                    baseDef=6, baseMdef=0, basePrice=60, agi=1,
                    desc="다리 보호대" },
                new() { name="Boots", slot=EquipmentSlot.Feet,
                    baseDef=4, baseMdef=0, basePrice=40, agi=2,
                    desc="전투용 장화" },
            };

            int count = 0;
            var grades = new[] { ItemGrade.Common, ItemGrade.Uncommon, ItemGrade.Rare, ItemGrade.Epic, ItemGrade.Legendary };

            foreach (var a in armors)
            {
                foreach (var grade in grades)
                {
                    if (CreateArmorAsset(a, grade))
                        count++;
                }
            }

            return count;
        }

        private static bool CreateArmorAsset(ArmorDef a, ItemGrade grade)
        {
            string gradeName = grade.ToString();
            string itemName = $"{a.name}_{gradeName}";
            string assetPath = $"{armorPath}/{itemName}.asset";

            if (AssetDatabase.LoadAssetAtPath<ItemData>(assetPath) != null)
                return false;

            float statMult = StatMultiplier[grade];
            float priceMult = PriceMultiplier[grade];

            var item = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(item, assetPath);
            var so = new SerializedObject(item);

            so.FindProperty("itemId").stringValue = $"armor_{a.name.ToLower()}_{gradeName.ToLower()}";
            so.FindProperty("itemName").stringValue = GetLocalizedArmorName(a.name, grade);
            so.FindProperty("description").stringValue = a.desc;
            so.FindProperty("itemType").enumValueIndex = (int)ItemType.Equipment;
            so.FindProperty("grade").intValue = (int)grade;
            so.FindProperty("equipmentSlot").enumValueIndex = (int)a.slot;
            so.FindProperty("weaponType").enumValueIndex = (int)WeaponType.Fists; // 방어구는 무기 아님
            so.FindProperty("stackSize").intValue = 1;
            so.FindProperty("sellPrice").longValue = (long)(a.basePrice * priceMult);

            // 스탯 보너스
            var stats = so.FindProperty("statBonuses");
            stats.FindPropertyRelative("strength").floatValue = Mathf.Round(a.str * statMult);
            stats.FindPropertyRelative("agility").floatValue = Mathf.Round(a.agi * statMult);
            stats.FindPropertyRelative("vitality").floatValue = Mathf.Round(a.vit * statMult);
            stats.FindPropertyRelative("intelligence").floatValue = Mathf.Round(a.inte * statMult);
            stats.FindPropertyRelative("defense").floatValue = Mathf.Round(a.baseDef * statMult);
            stats.FindPropertyRelative("magicDefense").floatValue = Mathf.Round(a.baseMdef * statMult);
            stats.FindPropertyRelative("luck").floatValue = Mathf.Round(a.luk * statMult);
            stats.FindPropertyRelative("stability").floatValue = Mathf.Round(a.stab * statMult);

            so.FindProperty("gradeColor").colorValue = ItemData.GetGradeColor(grade);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            return true;
        }

        // =================== 소모품 생성 ===================
        private struct ConsumableDef
        {
            public string name;
            public float healAmount;
            public float manaAmount;
            public int stackSize;
            public long price;
            public string desc;
        }

        private static int GenerateConsumables()
        {
            var consumables = new List<ConsumableDef>
            {
                new() { name="HealthPotion_Small", healAmount=50, stackSize=20, price=10, desc="소량의 HP를 회복합니다." },
                new() { name="HealthPotion_Medium", healAmount=150, stackSize=20, price=30, desc="중간량의 HP를 회복합니다." },
                new() { name="HealthPotion_Large", healAmount=400, stackSize=20, price=100, desc="대량의 HP를 회복합니다." },
                new() { name="HealthPotion_Max", healAmount=9999, stackSize=10, price=300, desc="HP를 전부 회복합니다." },
                new() { name="ManaPotion_Small", manaAmount=30, stackSize=20, price=10, desc="소량의 MP를 회복합니다." },
                new() { name="ManaPotion_Medium", manaAmount=100, stackSize=20, price=30, desc="중간량의 MP를 회복합니다." },
                new() { name="ManaPotion_Large", manaAmount=250, stackSize=20, price=100, desc="대량의 MP를 회복합니다." },
                new() { name="ManaPotion_Max", manaAmount=9999, stackSize=10, price=300, desc="MP를 전부 회복합니다." },
                new() { name="StrengthScroll", stackSize=10, price=50, desc="60초간 STR+5 증가" },
                new() { name="SpeedScroll", stackSize=10, price=50, desc="60초간 AGI+5 증가" },
                new() { name="ProtectionScroll", stackSize=10, price=50, desc="60초간 DEF+10 증가" },
                new() { name="TownPortal", stackSize=5, price=200, desc="즉시 마을로 귀환합니다." },
                new() { name="IdentifyScroll", stackSize=20, price=20, desc="미확인 아이템을 식별합니다." },
                new() { name="ResurrectionScroll", stackSize=1, price=500, desc="사망 시 즉시 부활합니다." },
                new() { name="PoisonAntidote", stackSize=10, price=30, desc="독 상태를 해제합니다." },
            };

            int count = 0;
            foreach (var c in consumables)
            {
                if (CreateConsumableAsset(c))
                    count++;
            }
            return count;
        }

        private static bool CreateConsumableAsset(ConsumableDef c)
        {
            string assetPath = $"{consumablePath}/{c.name}.asset";

            if (AssetDatabase.LoadAssetAtPath<ItemData>(assetPath) != null)
                return false;

            var item = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(item, assetPath);
            var so = new SerializedObject(item);

            so.FindProperty("itemId").stringValue = $"consumable_{c.name.ToLower()}";
            so.FindProperty("itemName").stringValue = GetLocalizedConsumableName(c.name);
            so.FindProperty("description").stringValue = c.desc;
            so.FindProperty("itemType").enumValueIndex = (int)ItemType.Consumable;
            so.FindProperty("grade").intValue = (int)ItemGrade.Common;
            so.FindProperty("equipmentSlot").enumValueIndex = (int)EquipmentSlot.None;
            so.FindProperty("stackSize").intValue = c.stackSize;
            so.FindProperty("sellPrice").longValue = c.price;
            so.FindProperty("healAmount").floatValue = c.healAmount;
            so.FindProperty("manaAmount").floatValue = c.manaAmount;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            return true;
        }

        // =================== 유틸리티 ===================
        private static string GetWeaponGroupFolder(WeaponType type)
        {
            return type switch
            {
                WeaponType.Longsword or WeaponType.Rapier or WeaponType.Broadsword
                    or WeaponType.Gladius or WeaponType.Scimitar => "SwordShield",
                WeaponType.Greatsword or WeaponType.Claymore or WeaponType.Flamberge
                    or WeaponType.Zweihander => "TwoHandedSword",
                WeaponType.Battleaxe or WeaponType.Warhammer or WeaponType.Maul
                    or WeaponType.Greataxe => "TwoHandedAxe",
                WeaponType.Dagger or WeaponType.CurvedDagger or WeaponType.Stiletto
                    or WeaponType.Kris => "Dagger",
                WeaponType.Longbow or WeaponType.Crossbow or WeaponType.CompoundBow
                    or WeaponType.Shortbow => "Bow",
                WeaponType.OakStaff or WeaponType.CrystalStaff or WeaponType.FireStaff
                    or WeaponType.IceStaff or WeaponType.HolyStaff => "Staff",
                WeaponType.MagicWand or WeaponType.CrystalWand or WeaponType.RuneWand
                    or WeaponType.ElementalWand => "Wand",
                WeaponType.Fists or WeaponType.Knuckles or WeaponType.Claws
                    or WeaponType.Gauntlets => "Fist",
                _ => "Other"
            };
        }

        private static string GetGradePrefix(ItemGrade grade)
        {
            return grade switch
            {
                ItemGrade.Common => "",
                ItemGrade.Uncommon => "고급 ",
                ItemGrade.Rare => "희귀한 ",
                ItemGrade.Epic => "영웅의 ",
                ItemGrade.Legendary => "전설의 ",
                _ => ""
            };
        }

        private static string GetLocalizedWeaponName(string engName, ItemGrade grade)
        {
            string prefix = GetGradePrefix(grade);
            string korName = engName switch
            {
                "Longsword" => "장검",
                "Rapier" => "레이피어",
                "Broadsword" => "광검",
                "Gladius" => "글라디우스",
                "Scimitar" => "시미터",
                "Greatsword" => "대검",
                "Claymore" => "클레이모어",
                "Flamberge" => "플람베르주",
                "Zweihander" => "츠바이핸더",
                "Battleaxe" => "전투 도끼",
                "Warhammer" => "전쟁 망치",
                "Maul_Weapon" => "대형 둔기",
                "Greataxe" => "대형 도끼",
                "Dagger" => "단검",
                "CurvedDagger" => "곡선 단검",
                "Stiletto" => "스틸레토",
                "Kris" => "크리스",
                "Longbow" => "장궁",
                "Crossbow" => "석궁",
                "CompoundBow" => "복합궁",
                "Shortbow" => "단궁",
                "OakStaff" => "참나무 지팡이",
                "CrystalStaff" => "수정 지팡이",
                "FireStaff" => "화염 지팡이",
                "IceStaff" => "빙결 지팡이",
                "HolyStaff" => "신성 지팡이",
                "MagicWand" => "마법봉",
                "CrystalWand" => "수정 마법봉",
                "RuneWand" => "룬 마법봉",
                "ElementalWand" => "원소 마법봉",
                "Fists" => "맨손",
                "Knuckles" => "너클",
                "Claws_Weapon" => "전투 클로",
                "Gauntlets" => "건틀릿",
                _ => engName
            };
            return $"{prefix}{korName}";
        }

        private static string GetLocalizedArmorName(string engName, ItemGrade grade)
        {
            string prefix = GetGradePrefix(grade);
            string korName = engName switch
            {
                "Helmet" => "투구",
                "ChestArmor" => "갑옷",
                "Greaves" => "다리 보호대",
                "Boots" => "장화",
                _ => engName
            };
            return $"{prefix}{korName}";
        }

        private static string GetLocalizedConsumableName(string engName)
        {
            return engName switch
            {
                "HealthPotion_Small" => "소형 체력 포션",
                "HealthPotion_Medium" => "중형 체력 포션",
                "HealthPotion_Large" => "대형 체력 포션",
                "HealthPotion_Max" => "최대 체력 포션",
                "ManaPotion_Small" => "소형 마나 포션",
                "ManaPotion_Medium" => "중형 마나 포션",
                "ManaPotion_Large" => "대형 마나 포션",
                "ManaPotion_Max" => "최대 마나 포션",
                "StrengthScroll" => "힘의 주문서",
                "SpeedScroll" => "속도의 주문서",
                "ProtectionScroll" => "보호의 주문서",
                "TownPortal" => "마을 귀환석",
                "IdentifyScroll" => "감정 주문서",
                "ResurrectionScroll" => "부활 주문서",
                "PoisonAntidote" => "해독제",
                _ => engName
            };
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
