using UnityEngine;
using UnityEditor;
using System.IO;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 장비 세트 데이터 자동 생성 - 10세트
    /// </summary>
    public class EquipmentSetDataGenerator : EditorWindow
    {
        [MenuItem("Dungeon Crawler/Generate Equipment Set Data (10)")]
        public static void Generate()
        {
            string basePath = "Assets/Resources/ScriptableObjects/EquipmentSets";
            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            int count = 0;

            // === 세트 1: 전사의 맹세 (SwordShield 세트, Common) ===
            CreateSet(basePath, "set_warrior_oath", "전사의 맹세", "칼과 방패로 싸우는 전사를 위한 세트",
                new[] { "Longsword_Common", "ChestArmor_Common", "Helmet_Common", "Greaves_Common", "Boots_Common" },
                ItemGrade.Common, Race.None,
                // 2피스
                new StatBlock { strength = 3, defense = 2 }, 20, 0, "STR+3, DEF+2, HP+20",
                // 3피스
                new StatBlock { strength = 5, vitality = 3 }, 30, 0, 2, 0, "STR+5, VIT+3, HP+30, 크리+2%",
                // 4피스
                new StatBlock { strength = 8, defense = 5, vitality = 4 }, 50, 10, 3, 5, 3, 0, "STR+8, DEF+5, VIT+4, HP+50, 크리+3%",
                // 5피스
                new StatBlock { strength = 12, defense = 8, vitality = 6, stability = 3 }, 80, 20, 5, 10, 5, 0, 5, 2, 0, "전스탯 대폭 증가, 생명흡수+2%"
            );
            count++;

            // === 세트 2: 그림자 암살자 (Dagger 세트, Uncommon) ===
            CreateSet(basePath, "set_shadow_assassin", "그림자 암살자", "어둠에서 치명적인 일격을 가하는 세트",
                new[] { "Dagger_Uncommon", "CurvedDagger_Uncommon", "Helmet_Uncommon", "ChestArmor_Uncommon", "Boots_Uncommon" },
                ItemGrade.Uncommon, Race.None,
                new StatBlock { agility = 4, luck = 2 }, 0, 10, "AGI+4, LUK+2, MP+10",
                new StatBlock { agility = 7, luck = 4 }, 0, 20, 5, 10, "AGI+7, LUK+4, 크리+5%, 크뎀+10%",
                new StatBlock { agility = 10, luck = 6 }, 10, 30, 8, 20, 5, 5, "AGI+10, LUK+6, 크리+8%, 이동+5%",
                new StatBlock { agility = 15, luck = 10, stability = 2 }, 20, 40, 12, 35, 8, 8, 5, 3, 0, "크리+12%, 크뎀+35%, 생명흡수+3%"
            );
            count++;

            // === 세트 3: 아크메이지 (Staff 세트, Rare) ===
            CreateSet(basePath, "set_archmage", "아크메이지", "대마법사의 지혜가 깃든 세트",
                new[] { "CrystalStaff_Rare", "Helmet_Rare", "ChestArmor_Rare", "Greaves_Rare", "Boots_Rare" },
                ItemGrade.Rare, Race.None,
                new StatBlock { intelligence = 5, magicDefense = 3 }, 0, 30, "INT+5, MDEF+3, MP+30",
                new StatBlock { intelligence = 8, magicDefense = 5 }, 0, 50, 0, 0, "INT+8, MDEF+5, MP+50",
                new StatBlock { intelligence = 12, magicDefense = 8 }, 20, 80, 3, 0, 0, 5, "INT+12, MDEF+8, MP+80, 공속+5%",
                new StatBlock { intelligence = 18, magicDefense = 12, vitality = 5 }, 40, 120, 5, 10, 3, 8, 10, 0, 5, "INT+18, 쿨감+10%, 경험치+5%"
            );
            count++;

            // === 세트 4: 수호자의 서약 (방어 세트, Rare) ===
            CreateSet(basePath, "set_guardian_vow", "수호자의 서약", "동료를 지키는 수호자를 위한 세트",
                new[] { "Longsword_Rare", "Helmet_Rare", "ChestArmor_Rare", "Greaves_Rare", "Boots_Rare" },
                ItemGrade.Rare, Race.None,
                new StatBlock { defense = 6, vitality = 4 }, 40, 0, "DEF+6, VIT+4, HP+40",
                new StatBlock { defense = 10, vitality = 7, magicDefense = 4 }, 70, 15, 0, 0, "DEF+10, VIT+7, MDEF+4, HP+70",
                new StatBlock { defense = 15, vitality = 10, magicDefense = 7 }, 120, 25, 0, 0, 0, 0, "DEF+15, VIT+10, HP+120",
                new StatBlock { defense = 22, vitality = 15, magicDefense = 12, stability = 5 }, 200, 40, 0, 0, 3, 0, 0, 5, 0, "DEF+22, HP+200, 생명흡수+5%"
            );
            count++;

            // === 세트 5: 폭풍의 궁사 (Bow 세트, Epic) ===
            CreateSet(basePath, "set_storm_archer", "폭풍의 궁사", "바람을 다스리는 궁수의 세트",
                new[] { "Longbow_Epic", "Helmet_Epic", "ChestArmor_Epic", "Greaves_Epic", "Boots_Epic" },
                ItemGrade.Epic, Race.None,
                new StatBlock { agility = 6, luck = 3 }, 15, 15, "AGI+6, LUK+3, HP+15",
                new StatBlock { agility = 10, luck = 5 }, 25, 25, 6, 12, "AGI+10, 크리+6%, 크뎀+12%",
                new StatBlock { agility = 15, luck = 8 }, 40, 35, 9, 20, 8, 8, "AGI+15, 크리+9%, 이동+8%, 공속+8%",
                new StatBlock { agility = 22, luck = 12, stability = 4 }, 60, 50, 13, 30, 12, 12, 8, 2, 3, "AGI+22, 크리+13%, 쿨감+8%, 경험치+3%"
            );
            count++;

            // === 세트 6: 인간 수호 (종족: 인간, Epic) ===
            CreateSet(basePath, "set_human_guardian", "인간의 결의", "인간 전사의 균형잡힌 세트",
                new[] { "Broadsword_Epic", "Helmet_Epic", "ChestArmor_Epic", "Greaves_Epic", "Boots_Epic" },
                ItemGrade.Epic, Race.Human,
                new StatBlock { strength = 5, defense = 5 }, 30, 15, "STR+5, DEF+5, HP+30",
                new StatBlock { strength = 8, defense = 8, agility = 4 }, 50, 25, 4, 8, "전투 스탯 균형 증가",
                new StatBlock { strength = 12, defense = 12, agility = 6, vitality = 5 }, 80, 35, 6, 15, 5, 5, "모든 전투 스탯 대폭 증가",
                new StatBlock { strength = 18, defense = 18, agility = 10, vitality = 8, luck = 5 }, 120, 50, 8, 20, 8, 8, 8, 3, 5, "인간의 만능 세트 풀 보너스"
            );
            count++;

            // === 세트 7: 엘프 마법 (종족: 엘프, Epic) ===
            CreateSet(basePath, "set_elf_arcana", "엘프의 비전", "엘프 마법사의 신비로운 세트",
                new[] { "CrystalStaff_Epic", "Helmet_Epic", "ChestArmor_Epic", "Greaves_Epic", "Boots_Epic" },
                ItemGrade.Epic, Race.Elf,
                new StatBlock { intelligence = 7, magicDefense = 4 }, 0, 40, "INT+7, MDEF+4, MP+40",
                new StatBlock { intelligence = 12, magicDefense = 7 }, 0, 70, 3, 5, "INT+12, MDEF+7, MP+70",
                new StatBlock { intelligence = 18, magicDefense = 10, luck = 5 }, 15, 100, 5, 10, 3, 8, "INT+18, MP+100, 공속+8%",
                new StatBlock { intelligence = 25, magicDefense = 15, luck = 8 }, 30, 150, 8, 15, 5, 12, 15, 0, 8, "INT+25, MP+150, 쿨감+15%, 경험치+8%"
            );
            count++;

            // === 세트 8: 야수 광전사 (종족: 비스트, Epic) ===
            CreateSet(basePath, "set_beast_berserker", "야수의 본능", "야수족 광전사의 원시적 세트",
                new[] { "Claws_Epic", "Helmet_Epic", "ChestArmor_Epic", "Greaves_Epic", "Boots_Epic" },
                ItemGrade.Epic, Race.Beast,
                new StatBlock { strength = 6, agility = 4 }, 25, 0, "STR+6, AGI+4, HP+25",
                new StatBlock { strength = 10, agility = 7 }, 40, 0, 7, 15, "STR+10, AGI+7, 크리+7%",
                new StatBlock { strength = 15, agility = 10, vitality = 5 }, 60, 10, 10, 25, 8, 10, "STR+15, AGI+10, 크리+10%, 이동+8%",
                new StatBlock { strength = 22, agility = 15, vitality = 8 }, 100, 20, 15, 40, 12, 15, 5, 5, 0, "STR+22, 크리+15%, 크뎀+40%, 생명흡수+5%"
            );
            count++;

            // === 세트 9: 마키나 철벽 (종족: 마키나, Epic) ===
            CreateSet(basePath, "set_machina_bulwark", "마키나의 철벽", "마키나족의 최첨단 방어 세트",
                new[] { "Warhammer_Epic", "Helmet_Epic", "ChestArmor_Epic", "Greaves_Epic", "Boots_Epic" },
                ItemGrade.Epic, Race.Machina,
                new StatBlock { defense = 8, vitality = 5 }, 50, 0, "DEF+8, VIT+5, HP+50",
                new StatBlock { defense = 14, vitality = 8, stability = 4 }, 90, 10, 0, 0, "DEF+14, VIT+8, STAB+4, HP+90",
                new StatBlock { defense = 20, vitality = 12, magicDefense = 8, stability = 6 }, 140, 20, 2, 0, 0, 0, "DEF+20, VIT+12, MDEF+8",
                new StatBlock { defense = 30, vitality = 18, magicDefense = 15, stability = 10 }, 250, 35, 3, 0, 3, 0, 0, 8, 0, "DEF+30, VIT+18, HP+250, 생명흡수+8%"
            );
            count++;

            // === 세트 10: 전설의 용사 (Legendary 풀세트) ===
            CreateSet(basePath, "set_legendary_hero", "전설의 용사", "전설에 걸맞는 영웅의 세트",
                new[] { "Greatsword_Legendary", "Helmet_Legendary", "ChestArmor_Legendary", "Greaves_Legendary", "Boots_Legendary" },
                ItemGrade.Legendary, Race.None,
                new StatBlock { strength = 8, agility = 6, vitality = 5 }, 50, 30, "STR+8, AGI+6, VIT+5, HP+50",
                new StatBlock { strength = 14, agility = 10, vitality = 8, intelligence = 6 }, 80, 50, 8, 15, "전투 스탯 대폭 증가",
                new StatBlock { strength = 20, agility = 15, vitality = 12, intelligence = 10, defense = 10 }, 120, 70, 12, 25, 8, 8, "전 스탯 대폭 증가, 크리+12%",
                new StatBlock { strength = 30, agility = 22, vitality = 18, intelligence = 15, defense = 15, magicDefense = 10, luck = 10, stability = 8 }, 200, 100, 18, 40, 12, 12, 12, 8, 10, "최강 풀세트: 모든 스탯 극대화"
            );
            count++;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[EquipmentSetGenerator] {count}개 장비 세트 생성 완료!");
            EditorUtility.DisplayDialog("완료", $"{count}개 장비 세트가 생성되었습니다.", "OK");
        }

        private static void CreateSet(string basePath, string setId, string setName, string desc,
            string[] itemIds, ItemGrade grade, Race race,
            // 2피스
            StatBlock b2Stat, float b2HP, float b2MP, string b2Desc,
            // 3피스
            StatBlock b3Stat, float b3HP, float b3MP, float b3Crit, float b3CritDmg, string b3Desc,
            // 4피스
            StatBlock b4Stat, float b4HP, float b4MP, float b4Crit, float b4CritDmg, float b4MoveSpd, float b4AtkSpd, string b4Desc,
            // 5피스
            StatBlock b5Stat, float b5HP, float b5MP, float b5Crit, float b5CritDmg, float b5MoveSpd, float b5AtkSpd, float b5CDR, float b5Life, float b5Exp, string b5Desc)
        {
            var asset = ScriptableObject.CreateInstance<EquipmentSetData>();
            var so = new SerializedObject(asset);

            so.FindProperty("setId").stringValue = setId;
            so.FindProperty("setName").stringValue = setName;
            so.FindProperty("description").stringValue = desc;
            so.FindProperty("setGrade").intValue = (int)grade;
            so.FindProperty("requiredRace").enumValueIndex = (int)race;

            // 아이템 ID 배열
            var itemsProp = so.FindProperty("itemIds");
            itemsProp.arraySize = itemIds.Length;
            for (int i = 0; i < itemIds.Length; i++)
                itemsProp.GetArrayElementAtIndex(i).stringValue = itemIds[i];

            // 2피스
            SetStatBlock(so.FindProperty("bonus2Piece"), b2Stat);
            so.FindProperty("bonus2HP").floatValue = b2HP;
            so.FindProperty("bonus2MP").floatValue = b2MP;
            so.FindProperty("bonus2Desc").stringValue = b2Desc;

            // 3피스
            SetStatBlock(so.FindProperty("bonus3Piece"), b3Stat);
            so.FindProperty("bonus3HP").floatValue = b3HP;
            so.FindProperty("bonus3MP").floatValue = b3MP;
            so.FindProperty("bonus3CritChance").floatValue = b3Crit;
            so.FindProperty("bonus3CritDamage").floatValue = b3CritDmg;
            so.FindProperty("bonus3Desc").stringValue = b3Desc;

            // 4피스
            SetStatBlock(so.FindProperty("bonus4Piece"), b4Stat);
            so.FindProperty("bonus4HP").floatValue = b4HP;
            so.FindProperty("bonus4MP").floatValue = b4MP;
            so.FindProperty("bonus4CritChance").floatValue = b4Crit;
            so.FindProperty("bonus4CritDamage").floatValue = b4CritDmg;
            so.FindProperty("bonus4MoveSpeed").floatValue = b4MoveSpd;
            so.FindProperty("bonus4AttackSpeed").floatValue = b4AtkSpd;
            so.FindProperty("bonus4Desc").stringValue = b4Desc;

            // 5피스
            SetStatBlock(so.FindProperty("bonus5Piece"), b5Stat);
            so.FindProperty("bonus5HP").floatValue = b5HP;
            so.FindProperty("bonus5MP").floatValue = b5MP;
            so.FindProperty("bonus5CritChance").floatValue = b5Crit;
            so.FindProperty("bonus5CritDamage").floatValue = b5CritDmg;
            so.FindProperty("bonus5MoveSpeed").floatValue = b5MoveSpd;
            so.FindProperty("bonus5AttackSpeed").floatValue = b5AtkSpd;
            so.FindProperty("bonus5CooldownReduction").floatValue = b5CDR;
            so.FindProperty("bonus5Lifesteal").floatValue = b5Life;
            so.FindProperty("bonus5ExpBonus").floatValue = b5Exp;
            so.FindProperty("bonus5Desc").stringValue = b5Desc;

            so.ApplyModifiedProperties();

            string path = $"{basePath}/{setId}.asset";
            AssetDatabase.CreateAsset(asset, path);
        }

        private static void SetStatBlock(SerializedProperty prop, StatBlock stat)
        {
            prop.FindPropertyRelative("strength").floatValue = stat.strength;
            prop.FindPropertyRelative("agility").floatValue = stat.agility;
            prop.FindPropertyRelative("vitality").floatValue = stat.vitality;
            prop.FindPropertyRelative("intelligence").floatValue = stat.intelligence;
            prop.FindPropertyRelative("defense").floatValue = stat.defense;
            prop.FindPropertyRelative("magicDefense").floatValue = stat.magicDefense;
            prop.FindPropertyRelative("luck").floatValue = stat.luck;
            prop.FindPropertyRelative("stability").floatValue = stat.stability;
        }
    }
}
