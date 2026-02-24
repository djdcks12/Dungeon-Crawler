using UnityEngine;
using UnityEditor;
using System.IO;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 마을 NPC 프리팹 자동 생성 에디터 스크립트
    /// 상인(3종), 퀘스트 NPC(5명), 던전 입구 포탈(5개), 대장장이 NPC
    /// </summary>
    public class WorldNPCGenerator
    {
        private const string PREFAB_PATH = "Assets/Prefabs/NPC/Town";
        private const string PORTAL_PATH = "Assets/Prefabs/NPC/DungeonPortal";

        [MenuItem("Dungeon Crawler/Generate Town NPC Prefabs")]
        public static void GenerateAllTownNPCs()
        {
            EnsureFolders();

            int count = 0;
            count += GenerateMerchantNPCs();
            count += GenerateQuestNPCs();
            count += GenerateDungeonPortals();
            count += GenerateBlacksmithNPC();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[WorldNPCGenerator] 총 {count}개 마을 NPC 프리팹 생성 완료!");
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/NPC"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "NPC");
            if (!AssetDatabase.IsValidFolder(PREFAB_PATH))
                AssetDatabase.CreateFolder("Assets/Prefabs/NPC", "Town");
            if (!AssetDatabase.IsValidFolder(PORTAL_PATH))
                AssetDatabase.CreateFolder("Assets/Prefabs/NPC", "DungeonPortal");
        }

        // === 상인 NPC 3종 ===
        private static int GenerateMerchantNPCs()
        {
            var merchants = new[]
            {
                new MerchantInfo
                {
                    name = "Merchant_Weapon",
                    shopName = "무기 상점",
                    type = MerchantType.Weapon,
                    color = new Color(0.8f, 0.3f, 0.3f) // 빨강
                },
                new MerchantInfo
                {
                    name = "Merchant_Armor",
                    shopName = "방어구 상점",
                    type = MerchantType.Armor,
                    color = new Color(0.3f, 0.5f, 0.8f) // 파랑
                },
                new MerchantInfo
                {
                    name = "Merchant_Consumable",
                    shopName = "잡화 상점",
                    type = MerchantType.Consumable,
                    color = new Color(0.3f, 0.8f, 0.3f) // 초록
                }
            };

            int count = 0;
            foreach (var m in merchants)
            {
                string path = $"{PREFAB_PATH}/{m.name}.prefab";
                if (File.Exists(path))
                {
                    Debug.Log($"[WorldNPCGenerator] 이미 존재: {m.name}");
                    count++;
                    continue;
                }

                var go = new GameObject(m.name);

                // SpriteRenderer
                var sr = go.AddComponent<SpriteRenderer>();
                sr.color = m.color;
                sr.sortingOrder = 5;

                // Collider (trigger)
                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 2f;

                // MerchantNPC
                var merchant = go.AddComponent<MerchantNPC>();
                var so = new SerializedObject(merchant);
                so.FindProperty("shopName").stringValue = m.shopName;
                so.FindProperty("merchantType").enumValueIndex = (int)m.type;
                so.FindProperty("interactionRange").floatValue = 2f;
                so.FindProperty("sellPriceRatio").floatValue = 0.3f;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(go, path);
                Object.DestroyImmediate(go);
                count++;
                Debug.Log($"[WorldNPCGenerator] 상인 NPC 생성: {m.name}");
            }

            return count;
        }

        // === 퀘스트 NPC 5명 ===
        private static int GenerateQuestNPCs()
        {
            var questNPCs = new[]
            {
                new QuestNPCInfo
                {
                    name = "QuestNPC_Beginner",
                    npcName = "마을 장로",
                    minLevel = 1, maxLevel = 5,
                    questIds = new[] { "main_first_hunt", "main_goblin_slayer", "main_weapon_mastery" },
                    color = new Color(0.9f, 0.85f, 0.5f) // 금색
                },
                new QuestNPCInfo
                {
                    name = "QuestNPC_Intermediate",
                    npcName = "모험가 길드장",
                    minLevel = 3, maxLevel = 8,
                    questIds = new[] { "main_dark_forest", "main_orc_commander", "main_beast_hunter" },
                    color = new Color(0.6f, 0.8f, 0.9f) // 하늘색
                },
                new QuestNPCInfo
                {
                    name = "QuestNPC_Advanced",
                    npcName = "은퇴한 기사",
                    minLevel = 5, maxLevel = 10,
                    questIds = new[] { "main_undead_crypt", "main_lich_king", "main_demon_gate" },
                    color = new Color(0.7f, 0.7f, 0.8f) // 은색
                },
                new QuestNPCInfo
                {
                    name = "QuestNPC_Expert",
                    npcName = "현자",
                    minLevel = 8, maxLevel = 13,
                    questIds = new[] { "main_dragon_awakening", "main_elemental_chaos", "main_ancient_dragon" },
                    color = new Color(0.9f, 0.6f, 0.2f) // 주황
                },
                new QuestNPCInfo
                {
                    name = "QuestNPC_Daily",
                    npcName = "현상금 게시판",
                    minLevel = 1, maxLevel = 15,
                    questIds = new[] { "daily_kill_30", "daily_boss_kill", "daily_goblin_hunt",
                                       "daily_beast_hunt", "daily_undead_hunt", "daily_elemental_hunt",
                                       "daily_dungeon_explore", "daily_dungeon_clear", "daily_level_up" },
                    color = new Color(0.5f, 0.4f, 0.3f) // 갈색
                }
            };

            int count = 0;
            foreach (var q in questNPCs)
            {
                string path = $"{PREFAB_PATH}/{q.name}.prefab";
                if (File.Exists(path))
                {
                    Debug.Log($"[WorldNPCGenerator] 이미 존재: {q.name}");
                    count++;
                    continue;
                }

                var go = new GameObject(q.name);

                // SpriteRenderer
                var sr = go.AddComponent<SpriteRenderer>();
                sr.color = q.color;
                sr.sortingOrder = 5;

                // Collider (trigger)
                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 2f;

                // QuestNPC
                var questNPC = go.AddComponent<QuestNPC>();
                var so = new SerializedObject(questNPC);
                so.FindProperty("npcName").stringValue = q.npcName;
                so.FindProperty("minLevel").intValue = q.minLevel;
                so.FindProperty("maxLevel").intValue = q.maxLevel;

                var questIdsProperty = so.FindProperty("questIds");
                questIdsProperty.arraySize = q.questIds.Length;
                for (int i = 0; i < q.questIds.Length; i++)
                {
                    questIdsProperty.GetArrayElementAtIndex(i).stringValue = q.questIds[i];
                }

                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(go, path);
                Object.DestroyImmediate(go);
                count++;
                Debug.Log($"[WorldNPCGenerator] 퀘스트 NPC 생성: {q.name} ({q.npcName})");
            }

            return count;
        }

        // === 던전 입구 포탈 5개 ===
        private static int GenerateDungeonPortals()
        {
            var portals = new[]
            {
                new PortalInfo
                {
                    name = "Portal_GoblinCave",
                    displayName = "고블린 동굴 입구",
                    dungeonId = "goblin_cave",
                    requiredLevel = 1,
                    color = new Color(0.4f, 0.7f, 0.3f) // 초록
                },
                new PortalInfo
                {
                    name = "Portal_DarkForest",
                    displayName = "어둠의 숲 입구",
                    dungeonId = "dark_forest",
                    requiredLevel = 3,
                    color = new Color(0.2f, 0.4f, 0.2f) // 진녹
                },
                new PortalInfo
                {
                    name = "Portal_UndeadCrypt",
                    displayName = "언데드 지하묘지 입구",
                    dungeonId = "undead_crypt",
                    requiredLevel = 5,
                    color = new Color(0.5f, 0.3f, 0.6f) // 보라
                },
                new PortalInfo
                {
                    name = "Portal_DragonNest",
                    displayName = "드래곤의 둥지 입구",
                    dungeonId = "dragon_nest",
                    requiredLevel = 8,
                    color = new Color(0.9f, 0.4f, 0.1f) // 주황
                },
                new PortalInfo
                {
                    name = "Portal_DemonLord",
                    displayName = "마왕의 영역 입구",
                    dungeonId = "demon_lord",
                    requiredLevel = 10,
                    color = new Color(0.8f, 0.1f, 0.1f) // 빨강
                }
            };

            int count = 0;
            foreach (var p in portals)
            {
                string path = $"{PORTAL_PATH}/{p.name}.prefab";
                if (File.Exists(path))
                {
                    Debug.Log($"[WorldNPCGenerator] 이미 존재: {p.name}");
                    count++;
                    continue;
                }

                var go = new GameObject(p.name);

                // SpriteRenderer
                var sr = go.AddComponent<SpriteRenderer>();
                sr.color = p.color;
                sr.sortingOrder = 3;

                // Collider (trigger)
                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 1.5f;

                // DungeonPortal 컴포넌트 (간단한 MonoBehaviour)
                var portal = go.AddComponent<DungeonPortalNPC>();
                var so = new SerializedObject(portal);
                so.FindProperty("portalName").stringValue = p.displayName;
                so.FindProperty("dungeonId").stringValue = p.dungeonId;
                so.FindProperty("requiredLevel").intValue = p.requiredLevel;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(go, path);
                Object.DestroyImmediate(go);
                count++;
                Debug.Log($"[WorldNPCGenerator] 던전 포탈 생성: {p.name} ({p.displayName})");
            }

            return count;
        }

        // === 대장장이 NPC (아이템 강화) ===
        private static int GenerateBlacksmithNPC()
        {
            string path = $"{PREFAB_PATH}/Blacksmith.prefab";
            if (File.Exists(path))
            {
                Debug.Log("[WorldNPCGenerator] 이미 존재: Blacksmith");
                return 1;
            }

            var go = new GameObject("Blacksmith");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.6f, 0.4f, 0.2f); // 갈색
            sr.sortingOrder = 5;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 2f;

            var blacksmith = go.AddComponent<BlacksmithNPC>();
            var so = new SerializedObject(blacksmith);
            so.FindProperty("npcName").stringValue = "대장장이";
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log("[WorldNPCGenerator] 대장장이 NPC 생성: Blacksmith");
            return 1;
        }

        // === 데이터 구조체 ===

        private struct MerchantInfo
        {
            public string name;
            public string shopName;
            public MerchantType type;
            public Color color;
        }

        private struct QuestNPCInfo
        {
            public string name;
            public string npcName;
            public int minLevel;
            public int maxLevel;
            public string[] questIds;
            public Color color;
        }

        private struct PortalInfo
        {
            public string name;
            public string displayName;
            public string dungeonId;
            public int requiredLevel;
            public Color color;
        }
    }
}
