using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 16개 직업별 SkillMasterNPC 프리팹 자동 생성
    /// </summary>
    public static class SkillMasterNPCPrefabGenerator
    {
        private struct NPCDef
        {
            public JobType jobType;
            public string npcName;
            public string greeting;
            public string learningMessage;
            public string farewellMessage;
        }

        [MenuItem("Dungeon Crawler/Generate SkillMaster NPC Prefabs")]
        public static void GenerateAllNPCPrefabs()
        {
            string folderPath = "Assets/Prefabs/NPC";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                AssetDatabase.CreateFolder("Assets/Prefabs", "NPC");
            }

            var npcs = new NPCDef[]
            {
                new NPCDef { jobType = JobType.Navigator, npcName = "항해사 마스터", greeting = "어서 오게, 젊은 항해사여.", learningMessage = "바다의 기술을 전수해 주마.", farewellMessage = "좋은 바람이 함께하기를!" },
                new NPCDef { jobType = JobType.Scout, npcName = "정찰병 마스터", greeting = "좋은 눈이로군.", learningMessage = "그림자의 기술을 가르쳐 주지.", farewellMessage = "언제나 경계를 늦추지 마라." },
                new NPCDef { jobType = JobType.Tracker, npcName = "추적자 마스터", greeting = "사냥의 냄새가 나는군.", learningMessage = "추적의 기술을 전수해 주마.", farewellMessage = "좋은 사냥이 되길!" },
                new NPCDef { jobType = JobType.Trapper, npcName = "함정 마스터", greeting = "조심해, 주변에 함정이 있다.", learningMessage = "함정 설치 기술을 알려주지.", farewellMessage = "적들이 함정에 걸려들기를!" },
                new NPCDef { jobType = JobType.Guardian, npcName = "수호기사 마스터", greeting = "수호의 길을 걷는 자여.", learningMessage = "방패의 기술을 전수하겠다.", farewellMessage = "동료를 지키는 자에게 축복이 있기를!" },
                new NPCDef { jobType = JobType.Templar, npcName = "성기사 마스터", greeting = "신의 뜻을 따르는 자여.", learningMessage = "신성한 기술을 전수하겠다.", farewellMessage = "신의 가호가 함께하기를!" },
                new NPCDef { jobType = JobType.Berserker, npcName = "광전사 마스터", greeting = "분노의 냄새가 나는군!", learningMessage = "파괴의 기술을 가르쳐 주마!", farewellMessage = "분노를 무기로 삼아라!" },
                new NPCDef { jobType = JobType.Assassin, npcName = "암살자 마스터", greeting = "...잘 찾아왔군.", learningMessage = "그림자의 기술을 전수하지.", farewellMessage = "소리 없이 임무를 완수하라." },
                new NPCDef { jobType = JobType.Duelist, npcName = "결투가 마스터", greeting = "좋은 자세로군.", learningMessage = "검의 정수를 가르쳐 주마.", farewellMessage = "검이 그대와 함께하기를!" },
                new NPCDef { jobType = JobType.ElementalBruiser, npcName = "원소 투사 마스터", greeting = "원소의 기운이 느껴지는군.", learningMessage = "원소의 힘을 다루는 법을 알려주지.", farewellMessage = "원소의 힘이 함께하기를!" },
                new NPCDef { jobType = JobType.Sniper, npcName = "저격수 마스터", greeting = "좋은 눈을 가졌군.", learningMessage = "정밀 사격 기술을 전수하마.", farewellMessage = "단 한 발로 승부를 결정지어라." },
                new NPCDef { jobType = JobType.Mage, npcName = "마법사 마스터", greeting = "마력의 재능이 보이는군.", learningMessage = "원소 마법을 가르쳐 주마.", farewellMessage = "마법의 힘이 함께하기를!" },
                new NPCDef { jobType = JobType.Warlock, npcName = "흑마법사 마스터", greeting = "어둠의 힘에 이끌려 왔군.", learningMessage = "금지된 기술을 전수하지.", farewellMessage = "어둠을 두려워하지 마라..." },
                new NPCDef { jobType = JobType.Cleric, npcName = "성직자 마스터", greeting = "빛의 축복이 함께하는구나.", learningMessage = "치유와 신성의 기술을 가르쳐 주마.", farewellMessage = "빛이 그대의 길을 밝히기를!" },
                new NPCDef { jobType = JobType.Druid, npcName = "드루이드 마스터", greeting = "자연의 기운이 느껴지는군.", learningMessage = "자연의 힘을 다루는 법을 알려주지.", farewellMessage = "대자연이 그대를 지켜주기를!" },
                new NPCDef { jobType = JobType.Amplifier, npcName = "증폭술사 마스터", greeting = "증폭의 재능이 보이는군.", learningMessage = "에너지 증폭 기술을 전수하마.", farewellMessage = "힘을 증폭시켜 동료를 도와라!" },
            };

            int created = 0;
            foreach (var npc in npcs)
            {
                string prefabPath = $"{folderPath}/SkillMaster_{npc.jobType}.prefab";

                // 이미 존재하면 건너뛰기
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                {
                    Debug.Log($"[NPCGen] {npc.jobType} prefab already exists, skipping.");
                    continue;
                }

                // GameObject 생성
                var go = new GameObject($"SkillMaster_{npc.jobType}");

                // NetworkObject 추가 (NetworkBehaviour 전제조건)
                go.AddComponent<NetworkObject>();

                // SpriteRenderer 추가
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = "Default";
                sr.sortingOrder = 5;

                // CircleCollider2D (Trigger) 추가
                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 3f;

                // SkillMasterNPC 컴포넌트 추가
                var npcComponent = go.AddComponent<SkillMasterNPC>();

                // SerializedObject를 통해 private 필드 설정
                var so = new SerializedObject(npcComponent);
                so.FindProperty("npcName").stringValue = npc.npcName;
                so.FindProperty("jobType").intValue = (int)npc.jobType;
                so.FindProperty("greeting").stringValue = npc.greeting;
                so.FindProperty("learningMessage").stringValue = npc.learningMessage;
                so.FindProperty("farewellMessage").stringValue = npc.farewellMessage;
                so.FindProperty("interactionRange").floatValue = 3f;
                so.FindProperty("spriteRenderer").objectReferenceValue = sr;
                so.ApplyModifiedPropertiesWithoutUndo();

                // 프리팹 저장
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                Object.DestroyImmediate(go);

                created++;
                Debug.Log($"[NPCGen] Created prefab: {npc.jobType}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[NPCGen] {created}개 SkillMaster NPC 프리팹 생성 완료");
        }
    }
}
