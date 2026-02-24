using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 던전 데이터 5개 자동 생성 에디터 스크립트
    /// </summary>
    public class DungeonDataGenerator
    {
        private static string basePath = "Assets/Resources/ScriptableObjects/DungeonData";

        [MenuItem("Dungeon Crawler/Generate Dungeon Data (5)")]
        public static void Generate()
        {
            EnsureFolder(basePath);
            int created = 0;

            // 1. 고블린 동굴 (Easy)
            created += CreateDungeon("GoblinCave", "고블린 동굴",
                "고블린들이 서식하는 어둡고 습한 동굴. 초보 모험가들의 첫 시련.",
                DungeonType.Normal, DungeonDifficulty.Easy,
                maxFloors: 10, maxPlayers: 16, recommendedLevel: 1,
                baseExp: 1000, baseGold: 500,
                expMult: 1.2f, goldMult: 1.1f, completionBonus: 2.0f,
                baseFloorTime: 600f, timeIncrease: 120f,
                allowPvP: true, allowRevive: true) ? 1 : 0;

            // 2. 어둠의 숲 (Normal)
            created += CreateDungeon("DarkForest", "어둠의 숲",
                "짙은 안개로 뒤덮인 숲. 야수와 오크가 영역을 다투는 위험한 곳.",
                DungeonType.Normal, DungeonDifficulty.Normal,
                maxFloors: 10, maxPlayers: 16, recommendedLevel: 3,
                baseExp: 1500, baseGold: 750,
                expMult: 1.25f, goldMult: 1.15f, completionBonus: 2.0f,
                baseFloorTime: 720f, timeIncrease: 120f,
                allowPvP: true, allowRevive: true) ? 1 : 0;

            // 3. 언데드 지하묘지 (Hard)
            created += CreateDungeon("UndeadCatacombs", "언데드 지하묘지",
                "수 세기에 걸쳐 쌓인 시체들이 되살아난 저주받은 묘지. 강력한 언데드와 악마가 도사린다.",
                DungeonType.Elite, DungeonDifficulty.Hard,
                maxFloors: 10, maxPlayers: 16, recommendedLevel: 5,
                baseExp: 2000, baseGold: 1000,
                expMult: 1.3f, goldMult: 1.2f, completionBonus: 2.5f,
                baseFloorTime: 900f, timeIncrease: 180f,
                allowPvP: true, allowRevive: false) ? 1 : 0;

            // 4. 드래곤의 둥지 (Hard+)
            created += CreateDungeon("DragonLair", "드래곤의 둥지",
                "고대 드래곤의 영역. 정령과 드래곤이 지키는 보물이 잠들어 있다.",
                DungeonType.Boss, DungeonDifficulty.Hard,
                maxFloors: 10, maxPlayers: 16, recommendedLevel: 8,
                baseExp: 3000, baseGold: 1500,
                expMult: 1.4f, goldMult: 1.25f, completionBonus: 3.0f,
                baseFloorTime: 1080f, timeIncrease: 180f,
                allowPvP: false, allowRevive: false) ? 1 : 0;

            // 5. 마왕의 영역 (Nightmare)
            created += CreateDungeon("DemonLordDomain", "마왕의 영역",
                "어둠의 심연에서 올라온 마왕의 거처. 부활 불가, 최고 등급의 도전.",
                DungeonType.Challenge, DungeonDifficulty.Nightmare,
                maxFloors: 10, maxPlayers: 16, recommendedLevel: 10,
                baseExp: 5000, baseGold: 2500,
                expMult: 1.5f, goldMult: 1.3f, completionBonus: 3.0f,
                baseFloorTime: 1200f, timeIncrease: 240f,
                allowPvP: false, allowRevive: false) ? 1 : 0;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DungeonDataGenerator] 던전 {created}개 생성 완료");
        }

        private static bool CreateDungeon(string fileName, string dungeonName, string desc,
            DungeonType type, DungeonDifficulty difficulty,
            int maxFloors, int maxPlayers, int recommendedLevel,
            long baseExp, long baseGold,
            float expMult, float goldMult, float completionBonus,
            float baseFloorTime, float timeIncrease,
            bool allowPvP, bool allowRevive)
        {
            string assetPath = $"{basePath}/{fileName}_DungeonData.asset";

            if (AssetDatabase.LoadAssetAtPath<DungeonData>(assetPath) != null)
                return false;

            var dungeon = ScriptableObject.CreateInstance<DungeonData>();
            AssetDatabase.CreateAsset(dungeon, assetPath);
            var so = new SerializedObject(dungeon);

            // 기본 정보
            so.FindProperty("dungeonName").stringValue = dungeonName;
            so.FindProperty("description").stringValue = desc;
            so.FindProperty("dungeonType").enumValueIndex = (int)type;
            so.FindProperty("difficulty").intValue = (int)difficulty; // DungeonDifficulty starts at 1, use intValue

            // 설정
            so.FindProperty("maxFloors").intValue = maxFloors;
            so.FindProperty("maxPlayers").intValue = maxPlayers;
            so.FindProperty("recommendedLevel").intValue = recommendedLevel;
            so.FindProperty("timeLimit").floatValue = baseFloorTime * maxFloors; // 총 시간
            so.FindProperty("allowPvP").boolValue = allowPvP;
            so.FindProperty("allowRevive").boolValue = allowRevive;

            // 보상
            so.FindProperty("baseExpReward").longValue = baseExp;
            so.FindProperty("baseGoldReward").longValue = baseGold;
            so.FindProperty("expMultiplierPerFloor").floatValue = expMult;
            so.FindProperty("goldMultiplierPerFloor").floatValue = goldMult;
            so.FindProperty("completionBonusMultiplier").floatValue = completionBonus;

            // 시간 관리
            so.FindProperty("timeMode").enumValueIndex = (int)DungeonTimeMode.PerFloor;
            so.FindProperty("baseFloorTime").floatValue = baseFloorTime;
            so.FindProperty("timeIncreasePerFloor").floatValue = timeIncrease;

            // 층별 구성 (10층)
            var floorConfigs = so.FindProperty("floorConfigs");
            floorConfigs.arraySize = maxFloors;
            for (int i = 0; i < maxFloors; i++)
            {
                int floor = i + 1;
                var fc = floorConfigs.GetArrayElementAtIndex(i);
                fc.FindPropertyRelative("floorNumber").intValue = floor;
                fc.FindPropertyRelative("floorName").stringValue = $"{dungeonName} {floor}층";
                fc.FindPropertyRelative("floorSize").vector2Value = new Vector2(50 + floor * 5, 50 + floor * 5);
                fc.FindPropertyRelative("monsterCount").intValue = 10 + floor * 2;
                fc.FindPropertyRelative("eliteCount").intValue = Mathf.Max(1, floor / 3);
                fc.FindPropertyRelative("hasBoss").boolValue = (floor % 5 == 0 || floor == maxFloors);
                fc.FindPropertyRelative("hasExit").boolValue = (floor == maxFloors);
                fc.FindPropertyRelative("completionBonus").floatValue = 1.0f + floor * 0.1f;
                fc.FindPropertyRelative("floorTimeLimit").floatValue = baseFloorTime + (floor - 1) * timeIncrease;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(dungeon);
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
