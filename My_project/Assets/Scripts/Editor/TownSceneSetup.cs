using UnityEngine;
using UnityEditor;
using System.IO;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 타운 씬에 NPC를 자동 배치하는 에디터 도구
    /// 기존 프리팹을 씬에 인스턴스화하고 스프라이트/이름 라벨 설정
    /// </summary>
    public class TownSceneSetup
    {
        // NPC 프리팹 경로
        private const string SkillMasterPath = "Assets/Prefabs/NPC/";
        private const string TownNPCPath = "Assets/Prefabs/NPC/Town/";
        private const string PortalPath = "Assets/Prefabs/NPC/DungeonPortal/";

        [MenuItem("Tools/Town/Setup Town NPCs")]
        public static void SetupTownNPCs()
        {
            // 기존 TownNPCs 루트 제거 (재실행 대비)
            var existingRoot = GameObject.Find("TownNPCs");
            if (existingRoot != null)
            {
                Undo.DestroyObjectImmediate(existingRoot);
            }

            var townRoot = new GameObject("TownNPCs");
            Undo.RegisterCreatedObjectUndo(townRoot, "Setup Town NPCs");

            // 1. 스킬마스터 구역 (왼쪽, 원형 배치)
            SetupSkillMasters(townRoot.transform);

            // 2. 상점 구역 (위쪽)
            SetupShops(townRoot.transform);

            // 3. 던전 포탈 구역 (오른쪽)
            SetupDungeonPortals(townRoot.transform);

            // 4. 퀘스트 NPC 구역 (아래)
            SetupQuestNPCs(townRoot.transform);

            EditorUtility.SetDirty(townRoot);
            Debug.Log("Town NPC setup complete! 30 NPCs placed.");
        }

        private static void SetupSkillMasters(Transform parent)
        {
            var group = new GameObject("SkillMasters");
            group.transform.SetParent(parent);

            string[] jobs = {
                "Guardian", "Templar", "Berserker", "Assassin",
                "Duelist", "ElementalBruiser", "Sniper", "Scout",
                "Tracker", "Trapper", "Navigator", "Mage",
                "Warlock", "Cleric", "Druid", "Amplifier"
            };

            float radius = 8f;
            Vector2 center = new Vector2(-12f, 0f);

            for (int i = 0; i < jobs.Length; i++)
            {
                float angle = (360f / jobs.Length) * i * Mathf.Deg2Rad;
                float x = center.x + Mathf.Cos(angle) * radius;
                float y = center.y + Mathf.Sin(angle) * radius;

                string prefabPath = $"{SkillMasterPath}SkillMaster_{jobs[i]}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                GameObject npc;
                if (prefab != null)
                {
                    npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group.transform);
                }
                else
                {
                    npc = new GameObject($"SkillMaster_{jobs[i]}");
                    npc.transform.SetParent(group.transform);
                    Debug.LogWarning($"Prefab not found: {prefabPath}, created empty object");
                }

                npc.transform.position = new Vector3(x, y, 0);
                EnsureNPCVisuals(npc, $"Skill Master\n{jobs[i]}", "SkillMaster");
            }

            Debug.Log("16 SkillMaster NPCs placed in circle formation");
        }

        private static void SetupShops(Transform parent)
        {
            var group = new GameObject("Shops");
            group.transform.SetParent(parent);

            var shopData = new (string prefab, string label, Vector3 pos)[]
            {
                ("Merchant_Weapon", "Weapon Shop", new Vector3(-3f, 12f, 0)),
                ("Merchant_Armor", "Armor Shop", new Vector3(1f, 12f, 0)),
                ("Merchant_Consumable", "Potion Shop", new Vector3(5f, 12f, 0)),
                ("Blacksmith", "Blacksmith", new Vector3(9f, 12f, 0)),
            };

            foreach (var (prefabName, label, pos) in shopData)
            {
                string prefabPath = $"{TownNPCPath}{prefabName}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                GameObject npc;
                if (prefab != null)
                {
                    npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group.transform);
                }
                else
                {
                    npc = new GameObject(prefabName);
                    npc.transform.SetParent(group.transform);
                    Debug.LogWarning($"Prefab not found: {prefabPath}");
                }

                npc.transform.position = pos;
                string spriteType = prefabName.Contains("Blacksmith") ? "Blacksmith" : "Merchant";
                EnsureNPCVisuals(npc, label, spriteType);
            }

            Debug.Log("4 Shop NPCs placed");
        }

        private static void SetupDungeonPortals(Transform parent)
        {
            var group = new GameObject("DungeonPortals");
            group.transform.SetParent(parent);

            var portalData = new (string prefab, string label, Vector3 pos)[]
            {
                ("Portal_GoblinCave", "Goblin Cave\nLv.1-10", new Vector3(18f, 4f, 0)),
                ("Portal_DarkForest", "Dark Forest\nLv.10-20", new Vector3(18f, 1f, 0)),
                ("Portal_UndeadCrypt", "Undead Crypt\nLv.20-30", new Vector3(18f, -2f, 0)),
                ("Portal_DragonNest", "Dragon Nest\nLv.30-40", new Vector3(18f, -5f, 0)),
                ("Portal_DemonLord", "Demon Lord\nLv.40-50", new Vector3(18f, -8f, 0)),
            };

            foreach (var (prefabName, label, pos) in portalData)
            {
                string prefabPath = $"{PortalPath}{prefabName}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                GameObject npc;
                if (prefab != null)
                {
                    npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group.transform);
                }
                else
                {
                    npc = new GameObject(prefabName);
                    npc.transform.SetParent(group.transform);
                    Debug.LogWarning($"Prefab not found: {prefabPath}");
                }

                npc.transform.position = pos;
                EnsureNPCVisuals(npc, label, "DungeonPortal");
            }

            Debug.Log("5 Dungeon Portals placed");
        }

        private static void SetupQuestNPCs(Transform parent)
        {
            var group = new GameObject("QuestNPCs");
            group.transform.SetParent(parent);

            var questData = new (string prefab, string label, Vector3 pos)[]
            {
                ("QuestNPC_Beginner", "Quest Board\nBeginner", new Vector3(-2f, -12f, 0)),
                ("QuestNPC_Intermediate", "Quest Board\nIntermediate", new Vector3(1f, -12f, 0)),
                ("QuestNPC_Advanced", "Quest Board\nAdvanced", new Vector3(4f, -12f, 0)),
                ("QuestNPC_Expert", "Quest Board\nExpert", new Vector3(7f, -12f, 0)),
                ("QuestNPC_Daily", "Daily Quests", new Vector3(10f, -12f, 0)),
            };

            foreach (var (prefabName, label, pos) in questData)
            {
                string prefabPath = $"{TownNPCPath}{prefabName}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                GameObject npc;
                if (prefab != null)
                {
                    npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group.transform);
                }
                else
                {
                    npc = new GameObject(prefabName);
                    npc.transform.SetParent(group.transform);
                    Debug.LogWarning($"Prefab not found: {prefabPath}");
                }

                npc.transform.position = pos;
                EnsureNPCVisuals(npc, label, "QuestNPC");
            }

            Debug.Log("5 Quest NPCs placed");
        }

        /// <summary>
        /// NPC에 스프라이트, 콜라이더, 이름 라벨이 있는지 확인하고 없으면 추가
        /// </summary>
        private static void EnsureNPCVisuals(GameObject npc, string labelText, string spriteType)
        {
            // SpriteRenderer 확인/추가
            var sr = npc.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = npc.AddComponent<SpriteRenderer>();
            }

            // placeholder 스프라이트 할당
            string spritePath = $"Sprites/NPC/{spriteType}";
            var sprite = Resources.Load<Sprite>(spritePath);
            if (sprite != null)
            {
                sr.sprite = sprite;
            }
            sr.sortingLayerName = "PlayerOrMonster";
            sr.sortingOrder = 0;

            // BoxCollider2D 확인/추가 (상호작용용)
            var col = npc.GetComponent<BoxCollider2D>();
            if (col == null)
            {
                col = npc.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(1.5f, 1.5f);
            }

            // 이름 라벨 (자식 TextMesh)
            var labelTransform = npc.transform.Find("NameLabel");
            if (labelTransform == null)
            {
                var labelObj = new GameObject("NameLabel");
                labelObj.transform.SetParent(npc.transform, false);
                labelObj.transform.localPosition = new Vector3(0, 1.2f, 0);

                var tm = labelObj.AddComponent<TextMesh>();
                tm.text = labelText;
                tm.fontSize = 24;
                tm.characterSize = 0.08f;
                tm.anchor = TextAnchor.LowerCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = Color.white;

                // 라벨이 항상 앞에 보이도록
                var labelRenderer = labelObj.GetComponent<MeshRenderer>();
                if (labelRenderer != null)
                {
                    labelRenderer.sortingLayerName = "UI";
                    labelRenderer.sortingOrder = 100;
                }
            }
        }
    }
}
