using UnityEngine;
using UnityEditor;
using System.IO;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 퀘스트 데이터 자동 생성 에디터 스크립트
    /// 메인 퀘스트 + 일일 퀘스트 생성
    /// </summary>
    public class QuestDataGenerator : EditorWindow
    {
        private bool overwriteExisting = false;

        [MenuItem("Dungeon Crawler/Generate Quest Data")]
        public static void ShowWindow()
        {
            GetWindow<QuestDataGenerator>("Quest Data Generator");
        }

        [MenuItem("Dungeon Crawler/Auto Generate All Quests Now")]
        public static void AutoGenerateAll()
        {
            var generator = CreateInstance<QuestDataGenerator>();
            generator.overwriteExisting = true;
            generator.GenerateAll();
            DestroyImmediate(generator);
        }

        private void OnGUI()
        {
            GUILayout.Label("퀘스트 데이터 생성기", EditorStyles.boldLabel);
            GUILayout.Space(5);
            overwriteExisting = EditorGUILayout.Toggle("기존 데이터 덮어쓰기", overwriteExisting);
            GUILayout.Space(10);

            if (GUILayout.Button("메인 퀘스트 생성 (15개)", GUILayout.Height(30)))
                GenerateMainQuests();
            if (GUILayout.Button("일일 퀘스트 템플릿 생성 (9개)", GUILayout.Height(30)))
                GenerateDailyQuests();
            if (GUILayout.Button("전체 생성", GUILayout.Height(40)))
                GenerateAll();
        }

        private void GenerateAll()
        {
            GenerateMainQuests();
            GenerateDailyQuests();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Quest generation complete!");
        }

        private void GenerateMainQuests()
        {
            string folder = "Assets/Resources/ScriptableObjects/QuestData/Main";
            EnsureFolder(folder);

            int count = 0;

            // === 초보자 퀘스트 체인 ===
            count += CreateQuest(folder, "main_001", "첫 번째 사냥", "고블린 3마리를 처치하세요.",
                QuestDifficulty.Easy, 1, false, "",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.Kill, targetId = "Goblin", targetName = "고블린 처치", requiredCount = 3 }
                },
                new QuestReward { experienceReward = 100, goldReward = 50 });

            count += CreateQuest(folder, "main_002", "더 강한 적", "고블린 10마리를 처치하세요.",
                QuestDifficulty.Easy, 2, false, "main_001",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.Kill, targetId = "Goblin", targetName = "고블린 처치", requiredCount = 10 }
                },
                new QuestReward { experienceReward = 300, goldReward = 150 });

            count += CreateQuest(folder, "main_003", "동굴 탐험", "고블린 동굴 5층에 도달하세요.",
                QuestDifficulty.Normal, 3, false, "main_002",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.Explore, targetId = "GoblinCave", targetName = "고블린 동굴 탐험", requiredCount = 1 }
                },
                new QuestReward { experienceReward = 500, goldReward = 300 });

            count += CreateQuest(folder, "main_004", "동굴의 주인", "고블린 동굴 보스를 처치하세요.",
                QuestDifficulty.Normal, 4, false, "main_003",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.BossKill, targetId = "Goblin", targetName = "고블린 보스 처치", requiredCount = 1 }
                },
                new QuestReward { experienceReward = 1000, goldReward = 500 });

            // === 중급 퀘스트 ===
            count += CreateQuest(folder, "main_005", "어둠의 숲으로", "오크 5마리를 처치하세요.",
                QuestDifficulty.Normal, 5, false, "main_004",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.Kill, targetId = "Orc", targetName = "오크 처치", requiredCount = 5 }
                },
                new QuestReward { experienceReward = 800, goldReward = 400 });

            count += CreateQuest(folder, "main_006", "야수 사냥꾼", "야수 15마리를 처치하세요.",
                QuestDifficulty.Normal, 5, false, "main_005",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.Kill, targetId = "Beast", targetName = "야수 처치", requiredCount = 15 }
                },
                new QuestReward { experienceReward = 1200, goldReward = 600 });

            count += CreateQuest(folder, "main_007", "숲의 수호자", "어둠의 숲 보스를 처치하세요.",
                QuestDifficulty.Hard, 6, false, "main_006",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.BossKill, targetId = "Beast", targetName = "숲 보스 처치", requiredCount = 1 },
                    new QuestObjective { objectiveType = QuestType.BossKill, targetId = "Orc", targetName = "오크 족장 처치", requiredCount = 1 }
                },
                new QuestReward { experienceReward = 2000, goldReward = 1000 });

            // === 상급 퀘스트 ===
            count += CreateQuest(folder, "main_008", "언데드의 위협", "언데드 20마리를 처치하세요.",
                QuestDifficulty.Hard, 7, false, "main_007",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.Kill, targetId = "Undead", targetName = "언데드 처치", requiredCount = 20 }
                },
                new QuestReward { experienceReward = 2500, goldReward = 1200 });

            count += CreateQuest(folder, "main_009", "악마의 소환", "악마 10마리를 처치하세요.",
                QuestDifficulty.Hard, 8, false, "main_008",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.Kill, targetId = "Demon", targetName = "악마 처치", requiredCount = 10 }
                },
                new QuestReward { experienceReward = 3000, goldReward = 1500 });

            count += CreateQuest(folder, "main_010", "지하묘지의 왕", "언데드 지하묘지 보스를 처치하세요.",
                QuestDifficulty.Hard, 9, false, "main_009",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.BossKill, targetId = "Undead", targetName = "지하묘지 보스 처치", requiredCount = 1 }
                },
                new QuestReward { experienceReward = 5000, goldReward = 2500 });

            // === 최종 퀘스트 ===
            count += CreateQuest(folder, "main_011", "용의 둥지", "드래곤 처치",
                QuestDifficulty.Epic, 10, false, "main_010",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.Kill, targetId = "Dragon", targetName = "드래곤 처치", requiredCount = 5 },
                    new QuestObjective { objectiveType = QuestType.BossKill, targetId = "Dragon", targetName = "드래곤 보스 처치", requiredCount = 1 }
                },
                new QuestReward { experienceReward = 10000, goldReward = 5000 });

            count += CreateQuest(folder, "main_012", "마왕에 도전", "마왕의 영역 최종 보스를 처치하세요.",
                QuestDifficulty.Epic, 12, false, "main_011",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.BossKill, targetId = "Demon", targetName = "마왕 처치", requiredCount = 1 }
                },
                new QuestReward { experienceReward = 20000, goldReward = 10000 });

            // === 반복 퀘스트 ===
            count += CreateQuest(folder, "repeat_001", "사냥꾼의 일과", "몬스터 50마리를 처치하세요.",
                QuestDifficulty.Normal, 3, true, "",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.Kill, targetId = "", targetName = "몬스터 처치", requiredCount = 50 }
                },
                new QuestReward { experienceReward = 500, goldReward = 250 });

            count += CreateQuest(folder, "repeat_002", "보스 헌터", "보스 1마리를 처치하세요.",
                QuestDifficulty.Hard, 5, true, "",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.BossKill, targetId = "", targetName = "보스 처치", requiredCount = 1 }
                },
                new QuestReward { experienceReward = 1000, goldReward = 500 });

            count += CreateQuest(folder, "repeat_003", "장비 강화사", "장비를 강화하세요.",
                QuestDifficulty.Easy, 3, true, "",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.Enhance, targetId = "", targetName = "장비 강화", requiredCount = 3 }
                },
                new QuestReward { experienceReward = 200, goldReward = 100 });

            Debug.Log($"Main quests generated: {count}");
        }

        private void GenerateDailyQuests()
        {
            string folder = "Assets/Resources/ScriptableObjects/QuestData/Daily";
            EnsureFolder(folder);

            int count = 0;

            // 처치 일일 퀘스트
            count += CreateQuest(folder, "daily_kill_easy", "[일일] 몬스터 토벌 (쉬움)", "몬스터 10마리를 처치하세요.",
                QuestDifficulty.Easy, 1, true, "",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.Kill, targetId = "", targetName = "몬스터 처치", requiredCount = 10 }
                },
                new QuestReward { experienceReward = 200, goldReward = 100 },
                true);

            count += CreateQuest(folder, "daily_kill_normal", "[일일] 몬스터 토벌 (보통)", "몬스터 25마리를 처치하세요.",
                QuestDifficulty.Normal, 3, true, "",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.Kill, targetId = "", targetName = "몬스터 처치", requiredCount = 25 }
                },
                new QuestReward { experienceReward = 500, goldReward = 250 },
                true);

            count += CreateQuest(folder, "daily_kill_hard", "[일일] 몬스터 토벌 (어려움)", "몬스터 50마리를 처치하세요.",
                QuestDifficulty.Hard, 5, true, "",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.Kill, targetId = "", targetName = "몬스터 처치", requiredCount = 50 }
                },
                new QuestReward { experienceReward = 1000, goldReward = 500 },
                true);

            // 보스 일일 퀘스트
            count += CreateQuest(folder, "daily_boss", "[일일] 보스 토벌", "보스 몬스터를 1마리 처치하세요.",
                QuestDifficulty.Hard, 5, true, "",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.BossKill, targetId = "", targetName = "보스 처치", requiredCount = 1 }
                },
                new QuestReward { experienceReward = 1500, goldReward = 750 },
                true);

            // 종족별 일일 퀘스트
            count += CreateQuest(folder, "daily_goblin", "[일일] 고블린 소탕", "고블린 15마리를 처치하세요.",
                QuestDifficulty.Easy, 1, true, "",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.Kill, targetId = "Goblin", targetName = "고블린 처치", requiredCount = 15 }
                },
                new QuestReward { experienceReward = 300, goldReward = 150 },
                true);

            count += CreateQuest(folder, "daily_orc", "[일일] 오크 소탕", "오크 10마리를 처치하세요.",
                QuestDifficulty.Normal, 4, true, "",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.Kill, targetId = "Orc", targetName = "오크 처치", requiredCount = 10 }
                },
                new QuestReward { experienceReward = 600, goldReward = 300 },
                true);

            count += CreateQuest(folder, "daily_undead", "[일일] 언데드 소탕", "언데드 10마리를 처치하세요.",
                QuestDifficulty.Normal, 6, true, "",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.Kill, targetId = "Undead", targetName = "언데드 처치", requiredCount = 10 }
                },
                new QuestReward { experienceReward = 800, goldReward = 400 },
                true);

            // 탐험 일일 퀘스트
            count += CreateQuest(folder, "daily_explore", "[일일] 던전 탐험", "아무 던전이나 탐험하세요.",
                QuestDifficulty.Easy, 1, true, "",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.Explore, targetId = "", targetName = "던전 탐험", requiredCount = 1 }
                },
                new QuestReward { experienceReward = 400, goldReward = 200 },
                true);

            // 레벨업 일일 퀘스트
            count += CreateQuest(folder, "daily_levelup", "[일일] 성장", "레벨업 1회 달성하세요.",
                QuestDifficulty.Normal, 1, true, "",
                new QuestObjective[] {
                    new QuestObjective { objectiveType = QuestType.LevelUp, targetId = "", targetName = "레벨업", requiredCount = 1 }
                },
                new QuestReward { experienceReward = 500, goldReward = 300 },
                true);

            Debug.Log($"Daily quests generated: {count}");
        }

        private int CreateQuest(string folder, string questId, string questName, string description,
            QuestDifficulty difficulty, int requiredLevel, bool isRepeatable, string prerequisite,
            QuestObjective[] objectives, QuestReward reward, bool isDaily = false)
        {
            string path = $"{folder}/{questId}.asset";

            if (!overwriteExisting && File.Exists(path))
                return 0;

            var asset = ScriptableObject.CreateInstance<QuestData>();
            var so = new SerializedObject(asset);

            so.FindProperty("questId").stringValue = questId;
            so.FindProperty("questName").stringValue = questName;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("difficulty").enumValueIndex = (int)difficulty;
            so.FindProperty("requiredLevel").intValue = requiredLevel;
            so.FindProperty("isRepeatable").boolValue = isRepeatable;
            so.FindProperty("isDaily").boolValue = isDaily;
            so.FindProperty("prerequisiteQuestId").stringValue = prerequisite;

            // Objectives
            var objProp = so.FindProperty("objectives");
            objProp.arraySize = objectives.Length;
            for (int i = 0; i < objectives.Length; i++)
            {
                var elem = objProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("objectiveType").enumValueIndex = (int)objectives[i].objectiveType;
                elem.FindPropertyRelative("targetId").stringValue = objectives[i].targetId;
                elem.FindPropertyRelative("targetName").stringValue = objectives[i].targetName;
                elem.FindPropertyRelative("requiredCount").intValue = objectives[i].requiredCount;
            }

            // Reward
            var rewardProp = so.FindProperty("reward");
            rewardProp.FindPropertyRelative("experienceReward").longValue = reward.experienceReward;
            rewardProp.FindPropertyRelative("goldReward").longValue = reward.goldReward;
            rewardProp.FindPropertyRelative("itemRewardId").stringValue = reward.itemRewardId ?? "";
            rewardProp.FindPropertyRelative("itemRewardCount").intValue = reward.itemRewardCount;

            so.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(asset, path);

            return 1;
        }

        private void EnsureFolder(string path)
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
