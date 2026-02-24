using UnityEngine;
using UnityEditor;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 몬스터 종족 데이터 7개 자동 생성 (Goblin 제외)
    /// </summary>
    public class MonsterRaceDataGenerator
    {
        private static string basePath = "Assets/Resources/ScriptableObjects/MonsterData/MonsterRaceData";

        [MenuItem("Dungeon Crawler/Generate Machina Player Race")]
        public static void GenerateMachina()
        {
            string folderPath = "Assets/Resources/ScriptableObjects/PlayerData/PlayerRaceData";
            string assetPath = $"{folderPath}/Machina_RaceData.asset";
            EnsureFolder(folderPath);

            var existing = AssetDatabase.LoadAssetAtPath<RaceData>(assetPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(assetPath);

            var raceData = ScriptableObject.CreateInstance<RaceData>();
            raceData.raceType = Race.Machina;
            raceData.raceName = "Machina";
            raceData.description = "기계족 - 높은 방어력과 안정성을 지닌 방어 특화 종족.\n" +
                "견고한 외피와 정밀한 내부 구조로 어떤 상황에서도 안정적인 전투를 수행합니다.\n" +
                "느리지만 강인한, 팀의 든든한 방벽입니다.";

            AssetDatabase.CreateAsset(raceData, assetPath);
            var so = new SerializedObject(raceData);

            var baseStats = so.FindProperty("baseStats");
            baseStats.FindPropertyRelative("strength").floatValue = 10f;
            baseStats.FindPropertyRelative("agility").floatValue = 6f;
            baseStats.FindPropertyRelative("vitality").floatValue = 16f;
            baseStats.FindPropertyRelative("intelligence").floatValue = 12f;
            baseStats.FindPropertyRelative("defense").floatValue = 14f;
            baseStats.FindPropertyRelative("magicDefense").floatValue = 8f;
            baseStats.FindPropertyRelative("luck").floatValue = 4f;
            baseStats.FindPropertyRelative("stability").floatValue = 12f;

            var statGrowth = so.FindProperty("statGrowth");
            statGrowth.FindPropertyRelative("strengthGrowth").floatValue = 1.5f;
            statGrowth.FindPropertyRelative("agilityGrowth").floatValue = 0.8f;
            statGrowth.FindPropertyRelative("vitalityGrowth").floatValue = 2.5f;
            statGrowth.FindPropertyRelative("intelligenceGrowth").floatValue = 1.2f;
            statGrowth.FindPropertyRelative("defenseGrowth").floatValue = 2.0f;
            statGrowth.FindPropertyRelative("magicDefenseGrowth").floatValue = 1.0f;
            statGrowth.FindPropertyRelative("luckGrowth").floatValue = 0.5f;
            statGrowth.FindPropertyRelative("stabilityGrowth").floatValue = 1.5f;

            var elemental = so.FindProperty("elementalAffinity");
            elemental.FindPropertyRelative("fireAttack").floatValue = 0f;
            elemental.FindPropertyRelative("fireResist").floatValue = 5f;
            elemental.FindPropertyRelative("iceAttack").floatValue = 0f;
            elemental.FindPropertyRelative("iceResist").floatValue = 5f;
            elemental.FindPropertyRelative("lightningAttack").floatValue = 5f;
            elemental.FindPropertyRelative("lightningResist").floatValue = -10f;
            elemental.FindPropertyRelative("poisonAttack").floatValue = 0f;
            elemental.FindPropertyRelative("poisonResist").floatValue = 15f;
            elemental.FindPropertyRelative("darkAttack").floatValue = 0f;
            elemental.FindPropertyRelative("darkResist").floatValue = 5f;
            elemental.FindPropertyRelative("holyAttack").floatValue = 0f;
            elemental.FindPropertyRelative("holyResist").floatValue = 0f;

            var specialties = so.FindProperty("specialties");
            specialties.arraySize = 3;
            var spec0 = specialties.GetArrayElementAtIndex(0);
            spec0.FindPropertyRelative("specialtyType").enumValueIndex = (int)RaceSpecialtyType.TechnicalMastery;
            spec0.FindPropertyRelative("value").floatValue = 15f;
            spec0.FindPropertyRelative("description").stringValue = "+15% 기술 숙련도 보너스";
            var spec1 = specialties.GetArrayElementAtIndex(1);
            spec1.FindPropertyRelative("specialtyType").enumValueIndex = (int)RaceSpecialtyType.PhysicalMastery;
            spec1.FindPropertyRelative("value").floatValue = 10f;
            spec1.FindPropertyRelative("description").stringValue = "+10% 물리 방어 보너스";
            var spec2 = specialties.GetArrayElementAtIndex(2);
            spec2.FindPropertyRelative("specialtyType").enumValueIndex = (int)RaceSpecialtyType.ElementalResistance;
            spec2.FindPropertyRelative("value").floatValue = 10f;
            spec2.FindPropertyRelative("description").stringValue = "+10% 속성 저항 보너스";

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(raceData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MonsterRaceDataGenerator] Machina_RaceData.asset 생성 완료: {assetPath}");
        }

        [MenuItem("Dungeon Crawler/Generate Monster Race Data (7)")]
        public static void Generate()
        {
            EnsureFolder(basePath);
            int created = 0;

            // Orc - 힘형
            created += CreateRace("Orc", MonsterRace.Orc, "오크족 - 강력한 물리 공격과 높은 체력을 지닌 전사형 종족",
                new StatBlock(15, 6, 14, 4, 8, 3, 5, 5),
                new StatGrowth(2.5f, 0.8f, 2.0f, 0.5f, 1.2f, 0.4f, 0.6f, 0.7f),
                80, 15, 0.0012f,
                fireRes: 5, iceRes: -5, poisonRes: 10) ? 1 : 0;

            // Undead - 마법형
            created += CreateRace("Undead", MonsterRace.Undead, "언데드족 - 죽음의 마법과 높은 마법 저항을 지닌 주술형 종족",
                new StatBlock(6, 7, 8, 14, 4, 10, 3, 6),
                new StatGrowth(0.8f, 1.0f, 1.0f, 2.5f, 0.5f, 1.5f, 0.4f, 0.8f),
                100, 20, 0.0015f,
                darkAttack: 10, darkRes: 15, holyRes: -15, poisonRes: 20) ? 1 : 0;

            // Beast - 물리형
            created += CreateRace("Beast", MonsterRace.Beast, "야수족 - 높은 민첩성과 물리 공격력을 지닌 사냥형 종족",
                new StatBlock(12, 10, 15, 5, 6, 4, 6, 4),
                new StatGrowth(2.0f, 1.5f, 2.2f, 0.6f, 0.8f, 0.5f, 0.8f, 0.5f),
                70, 12, 0.001f,
                poisonRes: -5) ? 1 : 0;

            // Elemental - 원소형
            created += CreateRace("Elemental", MonsterRace.Elemental, "정령족 - 순수한 원소의 힘을 지닌 마법 특화형 종족",
                new StatBlock(4, 8, 7, 16, 3, 12, 7, 8),
                new StatGrowth(0.5f, 1.2f, 0.8f, 3.0f, 0.4f, 2.0f, 1.0f, 1.2f),
                120, 25, 0.002f,
                fireAttack: 10, fireRes: 10, iceAttack: 10, iceRes: 10,
                lightningAttack: 10, lightningRes: 10) ? 1 : 0;

            // Demon - 혼합형
            created += CreateRace("Demon", MonsterRace.Demon, "악마족 - 물리와 마법 모두 강력한 혼합형 상위 종족",
                new StatBlock(11, 9, 11, 11, 7, 7, 10, 7),
                new StatGrowth(1.8f, 1.5f, 1.5f, 1.8f, 1.0f, 1.0f, 1.5f, 1.0f),
                150, 30, 0.0025f,
                darkAttack: 15, darkRes: 20, holyRes: -20, fireRes: 10) ? 1 : 0;

            // Dragon - 최상급
            created += CreateRace("Dragon", MonsterRace.Dragon, "드래곤족 - 모든 스탯이 최상급인 최강 종족",
                new StatBlock(18, 8, 20, 14, 12, 11, 12, 10),
                new StatGrowth(3.0f, 1.2f, 3.5f, 2.5f, 2.0f, 1.8f, 1.5f, 1.5f),
                300, 100, 0.005f,
                fireAttack: 20, fireRes: 20, iceRes: 5, lightningRes: 5, darkRes: 10, holyRes: 5) ? 1 : 0;

            // Construct - 방어형
            created += CreateRace("Construct", MonsterRace.Construct, "기계족 - 극도로 높은 방어력과 안정성을 지닌 방어형 종족",
                new StatBlock(10, 5, 16, 6, 14, 8, 4, 12),
                new StatGrowth(1.5f, 0.6f, 2.5f, 0.8f, 2.5f, 1.2f, 0.5f, 2.0f),
                90, 18, 0.0008f,
                lightningRes: -15, poisonRes: 25) ? 1 : 0;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MonsterRaceDataGenerator] 몬스터 종족 {created}개 생성 완료");
        }

        private static bool CreateRace(string raceName, MonsterRace raceType, string desc,
            StatBlock baseStats, StatGrowth growth,
            long baseExp, long baseGold, float soulRate,
            float fireAttack = 0, float fireRes = 0, float iceAttack = 0, float iceRes = 0,
            float lightningAttack = 0, float lightningRes = 0, float poisonAttack = 0, float poisonRes = 0,
            float darkAttack = 0, float darkRes = 0, float holyAttack = 0, float holyRes = 0)
        {
            string assetPath = $"{basePath}/{raceName}_RaceData.asset";

            if (AssetDatabase.LoadAssetAtPath<MonsterRaceData>(assetPath) != null)
                return false;

            var race = ScriptableObject.CreateInstance<MonsterRaceData>();
            race.raceType = raceType;
            race.raceName = raceName;
            race.description = desc;

            AssetDatabase.CreateAsset(race, assetPath);
            var so = new SerializedObject(race);

            // Base Stats
            var bs = so.FindProperty("baseStats");
            bs.FindPropertyRelative("strength").floatValue = baseStats.strength;
            bs.FindPropertyRelative("agility").floatValue = baseStats.agility;
            bs.FindPropertyRelative("vitality").floatValue = baseStats.vitality;
            bs.FindPropertyRelative("intelligence").floatValue = baseStats.intelligence;
            bs.FindPropertyRelative("defense").floatValue = baseStats.defense;
            bs.FindPropertyRelative("magicDefense").floatValue = baseStats.magicDefense;
            bs.FindPropertyRelative("luck").floatValue = baseStats.luck;
            bs.FindPropertyRelative("stability").floatValue = baseStats.stability;

            // Growth
            var gr = so.FindProperty("gradeGrowth");
            gr.FindPropertyRelative("strengthGrowth").floatValue = growth.strengthGrowth;
            gr.FindPropertyRelative("agilityGrowth").floatValue = growth.agilityGrowth;
            gr.FindPropertyRelative("vitalityGrowth").floatValue = growth.vitalityGrowth;
            gr.FindPropertyRelative("intelligenceGrowth").floatValue = growth.intelligenceGrowth;
            gr.FindPropertyRelative("defenseGrowth").floatValue = growth.defenseGrowth;
            gr.FindPropertyRelative("magicDefenseGrowth").floatValue = growth.magicDefenseGrowth;
            gr.FindPropertyRelative("luckGrowth").floatValue = growth.luckGrowth;
            gr.FindPropertyRelative("stabilityGrowth").floatValue = growth.stabilityGrowth;

            // Experience & Drops
            so.FindProperty("baseExperience").longValue = baseExp;
            so.FindProperty("baseGold").longValue = baseGold;
            so.FindProperty("soulDropRate").floatValue = soulRate;

            // Elemental Affinity
            var ea = so.FindProperty("elementalAffinity");
            ea.FindPropertyRelative("fireAttack").floatValue = fireAttack;
            ea.FindPropertyRelative("fireResist").floatValue = fireRes;
            ea.FindPropertyRelative("iceAttack").floatValue = iceAttack;
            ea.FindPropertyRelative("iceResist").floatValue = iceRes;
            ea.FindPropertyRelative("lightningAttack").floatValue = lightningAttack;
            ea.FindPropertyRelative("lightningResist").floatValue = lightningRes;
            ea.FindPropertyRelative("poisonAttack").floatValue = poisonAttack;
            ea.FindPropertyRelative("poisonResist").floatValue = poisonRes;
            ea.FindPropertyRelative("darkAttack").floatValue = darkAttack;
            ea.FindPropertyRelative("darkResist").floatValue = darkRes;
            ea.FindPropertyRelative("holyAttack").floatValue = holyAttack;
            ea.FindPropertyRelative("holyResist").floatValue = holyRes;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(race);
            return true;
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
