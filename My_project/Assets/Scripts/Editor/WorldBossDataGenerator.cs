using UnityEngine;
using UnityEditor;
using System.IO;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 월드보스 데이터 5종 자동 생성
    /// </summary>
    public class WorldBossDataGenerator : EditorWindow
    {
        [MenuItem("Dungeon Crawler/Generate World Boss Data (5)")]
        public static void Generate()
        {
            string basePath = "Assets/Resources/ScriptableObjects/WorldBoss";
            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            int count = 0;

            // 1. 고대 골렘 (입문용)
            count += CreateBoss(basePath, "wb_ancient_golem", "고대 골렘", 10,
                "고대 유적에서 깨어난 거대한 돌 골렘. 느리지만 강력한 공격을 한다.",
                500000, 100, 30, 30,
                0.70f, 0.45f, 0.20f,
                1800, 600, 60,
                5000, 2000,
                new string[] { "weapon_warhammer_rare", "armor_chest_rare" },
                new string[] { "weapon_greatsword_epic" },
                0.05f,
                6f, 2.0f, 1.5f, 45f, "");

            // 2. 심연의 크라켄 (수중 보스)
            count += CreateBoss(basePath, "wb_abyss_kraken", "심연의 크라켄", 13,
                "심해에서 올라온 거대 크라켄. 촉수 공격과 먹물 공격이 치명적이다.",
                800000, 150, 40, 25,
                0.75f, 0.50f, 0.25f,
                1800, 600, 60,
                8000, 3500,
                new string[] { "weapon_crossbow_rare", "armor_boots_epic" },
                new string[] { "weapon_crystalstaff_epic" },
                0.07f,
                8f, 2.5f, 1.6f, 35f, "");

            // 3. 화염 군주 이프리트 (화염 보스)
            count += CreateBoss(basePath, "wb_ifrit", "화염 군주 이프리트", 15,
                "불의 원소계에서 소환된 불의 군주. 주변이 불바다가 된다.",
                1200000, 200, 35, 50,
                0.75f, 0.50f, 0.20f,
                2400, 600, 60,
                12000, 5000,
                new string[] { "weapon_firestaff_epic", "armor_chest_epic" },
                new string[] { "weapon_firestaff_legendary" },
                0.08f,
                10f, 3.0f, 1.8f, 30f, "");

            // 4. 공허의 드래곤 (용족 보스)
            count += CreateBoss(basePath, "wb_void_dragon", "공허의 드래곤", 18,
                "차원의 틈에서 나타난 공허의 용. 브레스 한 번에 전멸할 수 있다.",
                2000000, 280, 50, 45,
                0.75f, 0.50f, 0.20f,
                3600, 600, 60,
                18000, 8000,
                new string[] { "weapon_greatsword_epic", "armor_helmet_epic", "armor_greaves_epic" },
                new string[] { "weapon_claymore_legendary", "armor_chest_legendary" },
                0.10f,
                12f, 3.5f, 2.0f, 25f, "");

            // 5. 마왕 아자젤 (최종 월드보스)
            count += CreateBoss(basePath, "wb_azazel", "마왕 아자젤", 20,
                "어둠의 차원에서 강림한 마왕. 모든 것을 멸망시키려 한다.",
                5000000, 400, 60, 60,
                0.80f, 0.55f, 0.25f,
                7200, 900, 120,
                30000, 15000,
                new string[] { "weapon_zweihander_epic", "armor_chest_epic", "armor_helmet_epic" },
                new string[] { "weapon_zweihander_legendary", "armor_chest_legendary" },
                0.12f,
                15f, 4.0f, 2.5f, 20f, "");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[WorldBossGenerator] {count}개 월드보스 에셋 생성 완료!");
            EditorUtility.DisplayDialog("완료", $"{count}개 월드보스가 생성되었습니다.", "OK");
        }

        private static int CreateBoss(string basePath, string id, string name, int level, string desc,
            long maxHP, float atkDmg, float def, float mdef,
            float p2Thresh, float p3Thresh, float enrageThresh,
            float spawnInterval, float despawnTimeout, float announceBefore,
            int expReward, int goldReward,
            string[] guaranteedDrops, string[] rareDrops, float rareDropChance,
            float aoeRadius, float aoeDmgMult, float enrageDmgMult, float summonInterval, string summonMonsterId)
        {
            string path = $"{basePath}/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<WorldBossData>(path) != null)
                return 0;

            var asset = ScriptableObject.CreateInstance<WorldBossData>();
            var so = new SerializedObject(asset);

            so.FindProperty("bossId").stringValue = id;
            so.FindProperty("bossName").stringValue = name;
            so.FindProperty("description").stringValue = desc;
            so.FindProperty("level").intValue = level;
            so.FindProperty("maxHP").longValue = maxHP;
            so.FindProperty("attackDamage").floatValue = atkDmg;
            so.FindProperty("defense").floatValue = def;
            so.FindProperty("magicDefense").floatValue = mdef;
            so.FindProperty("phase2Threshold").floatValue = p2Thresh;
            so.FindProperty("phase3Threshold").floatValue = p3Thresh;
            so.FindProperty("enrageThreshold").floatValue = enrageThresh;
            so.FindProperty("spawnInterval").floatValue = spawnInterval;
            so.FindProperty("despawnTimeout").floatValue = despawnTimeout;
            so.FindProperty("announceBefore").floatValue = announceBefore;
            so.FindProperty("baseExpReward").intValue = expReward;
            so.FindProperty("baseGoldReward").intValue = goldReward;
            so.FindProperty("rareDropChance").floatValue = rareDropChance;
            so.FindProperty("aoeRadius").floatValue = aoeRadius;
            so.FindProperty("aoeDamageMultiplier").floatValue = aoeDmgMult;
            so.FindProperty("enrageDamageMultiplier").floatValue = enrageDmgMult;
            so.FindProperty("summonInterval").floatValue = summonInterval;
            so.FindProperty("summonMonsterId").stringValue = summonMonsterId;

            // 배열 설정
            SetStringArray(so.FindProperty("guaranteedDrops"), guaranteedDrops);
            SetStringArray(so.FindProperty("rareDrops"), rareDrops);

            so.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(asset, path);
            return 1;
        }

        private static void SetStringArray(SerializedProperty prop, string[] values)
        {
            if (values == null || values.Length == 0)
            {
                prop.ClearArray();
                return;
            }
            prop.ClearArray();
            for (int i = 0; i < values.Length; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).stringValue = values[i];
            }
        }
    }
}
