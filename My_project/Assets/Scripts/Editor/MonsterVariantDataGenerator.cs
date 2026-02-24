using UnityEngine;
using UnityEditor;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 몬스터 변종 데이터 48개 자동 생성 (8종족 × 6변종)
    /// </summary>
    public class MonsterVariantDataGenerator
    {
        private static string basePath = "Assets/Resources/ScriptableObjects/MonsterData/MonsterVariantData";
        private static string racePath = "Assets/Resources/ScriptableObjects/MonsterData/MonsterRaceData";

        // 변종 타입별 설정
        private struct VariantTemplate
        {
            public string suffix;
            public string korName;
            public StatBlock statMin;
            public StatBlock statMax;
            public float spawnWeight;
            public int minFloor, maxFloor;
            public MonsterAIType aiType;
            public float aggressionMult;
        }

        private static readonly VariantTemplate[] Templates = new[]
        {
            // Normal - 일반 개체
            new VariantTemplate {
                suffix = "Normal", korName = "일반",
                statMin = new StatBlock(-2, -2, -2, -2, -2, -2, -2, -2),
                statMax = new StatBlock(2, 2, 2, 2, 2, 2, 2, 2),
                spawnWeight = 50, minFloor = 1, maxFloor = 10,
                aiType = MonsterAIType.Aggressive, aggressionMult = 1f
            },
            // Elite - 정예 개체
            new VariantTemplate {
                suffix = "Elite", korName = "정예",
                statMin = new StatBlock(0, 0, 0, 0, 0, 0, 0, 0),
                statMax = new StatBlock(4, 3, 4, 3, 3, 3, 2, 3),
                spawnWeight = 15, minFloor = 3, maxFloor = 10,
                aiType = MonsterAIType.Aggressive, aggressionMult = 1.3f
            },
            // Shaman - 주술사 개체
            new VariantTemplate {
                suffix = "Shaman", korName = "주술사",
                statMin = new StatBlock(-1, -1, -1, 2, -1, 1, 0, 0),
                statMax = new StatBlock(1, 1, 1, 5, 1, 3, 1, 1),
                spawnWeight = 12, minFloor = 2, maxFloor = 8,
                aiType = MonsterAIType.Defensive, aggressionMult = 0.8f
            },
            // Berserker - 광전사 개체
            new VariantTemplate {
                suffix = "Berserker", korName = "광전사",
                statMin = new StatBlock(2, 0, 0, -2, -1, -2, 0, -1),
                statMax = new StatBlock(5, 2, 3, 0, 1, 0, 1, 1),
                spawnWeight = 12, minFloor = 2, maxFloor = 9,
                aiType = MonsterAIType.Aggressive, aggressionMult = 1.5f
            },
            // Leader - 지도자 개체
            new VariantTemplate {
                suffix = "Leader", korName = "대장",
                statMin = new StatBlock(1, 1, 1, 1, 1, 1, 1, 1),
                statMax = new StatBlock(3, 3, 3, 3, 3, 3, 3, 3),
                spawnWeight = 6, minFloor = 5, maxFloor = 10,
                aiType = MonsterAIType.Territorial, aggressionMult = 1.2f
            },
            // Boss - 보스 개체
            new VariantTemplate {
                suffix = "Boss", korName = "보스",
                statMin = new StatBlock(3, 2, 3, 2, 2, 2, 2, 2),
                statMax = new StatBlock(6, 5, 6, 5, 5, 5, 4, 5),
                spawnWeight = 3, minFloor = 8, maxFloor = 10,
                aiType = MonsterAIType.Territorial, aggressionMult = 1.5f
            },
        };

        [MenuItem("Dungeon Crawler/Repair Variant BaseRace References")]
        public static void RepairBaseRaceReferences()
        {
            string[] guids = AssetDatabase.FindAssets("t:MonsterVariantData");
            int repaired = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var variant = AssetDatabase.LoadAssetAtPath<MonsterVariantData>(path);
                if (variant == null) continue;

                var so = new SerializedObject(variant);
                var baseRaceProp = so.FindProperty("baseRace");

                if (baseRaceProp.objectReferenceValue == null)
                {
                    // variantId에서 종족명 추출 (예: "Goblin_Elite" → "Goblin")
                    string variantId = so.FindProperty("variantId").stringValue;
                    if (string.IsNullOrEmpty(variantId))
                    {
                        // 파일명에서 추출 (예: "Goblin_Elite.asset" → "Goblin")
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                        int underscoreIdx = fileName.IndexOf('_');
                        if (underscoreIdx > 0)
                            variantId = fileName;
                    }

                    if (!string.IsNullOrEmpty(variantId))
                    {
                        int idx = variantId.IndexOf('_');
                        if (idx > 0)
                        {
                            string raceName = variantId.Substring(0, idx);
                            var raceData = FindRaceData(raceName);
                            if (raceData != null)
                            {
                                baseRaceProp.objectReferenceValue = raceData;
                                so.ApplyModifiedPropertiesWithoutUndo();
                                EditorUtility.SetDirty(variant);
                                repaired++;
                                Debug.Log($"[Repair] {path} → baseRace = {raceData.raceType}");
                            }
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[MonsterVariantDataGenerator] baseRace 수리 완료: {repaired}개 수정됨");
        }

        [MenuItem("Dungeon Crawler/Generate Monster Variant Data (48)")]
        public static void Generate()
        {
            EnsureFolder(basePath);
            int created = 0;

            // 8종족 × 6변종
            string[] races = { "Goblin", "Orc", "Undead", "Beast", "Elemental", "Demon", "Dragon", "Construct" };
            string[] raceKor = { "고블린", "오크", "언데드", "야수", "정령", "악마", "드래곤", "기계" };

            for (int r = 0; r < races.Length; r++)
            {
                for (int t = 0; t < Templates.Length; t++)
                {
                    // 기존 Goblin Normal은 스킵
                    if (races[r] == "Goblin" && Templates[t].suffix == "Normal")
                        continue;

                    if (CreateVariant(races[r], raceKor[r], Templates[t]))
                        created++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MonsterVariantDataGenerator] 몬스터 변종 {created}개 생성 완료");
        }

        private static bool CreateVariant(string raceName, string raceKor, VariantTemplate template)
        {
            string variantName = $"{raceName}_{template.suffix}";
            string assetPath = $"{basePath}/{variantName}.asset";

            if (AssetDatabase.LoadAssetAtPath<MonsterVariantData>(assetPath) != null)
                return false;

            // 종족 데이터 참조 찾기
            var raceData = AssetDatabase.LoadAssetAtPath<MonsterRaceData>($"{racePath}/{raceName}_RaceData.asset");
            // Goblin은 다른 경로일 수 있음
            if (raceData == null)
            {
                raceData = FindRaceData(raceName);
            }

            var variant = ScriptableObject.CreateInstance<MonsterVariantData>();
            variant.variantName = $"{raceKor} {template.korName}";
            variant.description = $"{raceKor}족의 {template.korName} 개체";
            variant.baseRace = raceData;

            AssetDatabase.CreateAsset(variant, assetPath);
            var so = new SerializedObject(variant);

            // variantId 설정
            so.FindProperty("variantId").stringValue = variantName;

            // Stat Variance
            SetStatBlock(so, "statMinVariance", template.statMin);
            SetStatBlock(so, "statMaxVariance", template.statMax);

            // Spawn Settings
            so.FindProperty("spawnWeight").floatValue = template.spawnWeight;
            so.FindProperty("minFloor").intValue = template.minFloor;
            so.FindProperty("maxFloor").intValue = template.maxFloor;

            // AI
            so.FindProperty("preferredAIType").enumValueIndex = (int)template.aiType;
            so.FindProperty("aggressionMultiplier").floatValue = template.aggressionMult;

            // Animation Settings (기본값)
            so.FindProperty("idleFrameRate").floatValue = 6f;
            so.FindProperty("moveFrameRate").floatValue = 8f;
            so.FindProperty("attackFrameRate").floatValue = 12f;
            so.FindProperty("castingFrameRate").floatValue = 10f;
            so.FindProperty("hitFrameRate").floatValue = 10f;
            so.FindProperty("deathFrameRate").floatValue = 6f;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(variant);
            return true;
        }

        private static void SetStatBlock(SerializedObject so, string propertyName, StatBlock stats)
        {
            var prop = so.FindProperty(propertyName);
            prop.FindPropertyRelative("strength").floatValue = stats.strength;
            prop.FindPropertyRelative("agility").floatValue = stats.agility;
            prop.FindPropertyRelative("vitality").floatValue = stats.vitality;
            prop.FindPropertyRelative("intelligence").floatValue = stats.intelligence;
            prop.FindPropertyRelative("defense").floatValue = stats.defense;
            prop.FindPropertyRelative("magicDefense").floatValue = stats.magicDefense;
            prop.FindPropertyRelative("luck").floatValue = stats.luck;
            prop.FindPropertyRelative("stability").floatValue = stats.stability;
        }

        private static MonsterRaceData FindRaceData(string raceName)
        {
            // MonsterRace enum으로 매칭 (raceName 문자열이 한국어일 수 있으므로)
            MonsterRace targetRace;
            if (!System.Enum.TryParse(raceName, out targetRace))
            {
                Debug.LogWarning($"[MonsterVariantDataGenerator] '{raceName}'은 유효한 MonsterRace enum 값이 아닙니다.");
                return null;
            }

            // 모든 MonsterRaceData 에셋 검색
            string[] guids = AssetDatabase.FindAssets("t:MonsterRaceData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<MonsterRaceData>(path);
                if (data != null && data.raceType == targetRace)
                    return data;
            }
            Debug.LogWarning($"[MonsterVariantDataGenerator] {raceName} 종족 데이터를 찾을 수 없습니다. 종족 데이터를 먼저 생성하세요.");
            return null;
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
