using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    using Unity.Template.Multiplayer.NGO.Runtime;

    /// <summary>
    /// 208개 SkillData 에셋에 스킬 아이콘을 자동 배정
    /// </summary>
    public static class SkillIconAssigner
    {
        // 직업 순서 (SkillDataGenerator와 동일)
        private static readonly string[] JobOrder = {
            "Navigator", "Scout", "Tracker", "Trapper",
            "Guardian", "Templar", "Berserker", "Assassin",
            "Duelist", "ElementalBruiser", "Sniper", "Mage",
            "Warlock", "Cleric", "Druid", "Amplifier"
        };

        // 스킬 순서 (각 직업당 13개)
        private static readonly string[] SkillOrder = {
            "Level1_A", "Level1_B", "Level1_C",
            "Level3_A", "Level3_B", "Level3_C",
            "Level5_A", "Level5_B", "Level5_C",
            "Level7_A", "Level7_B", "Level7_C",
            "Ultimate"
        };

        [MenuItem("Dungeon Crawler/Assign Skill Icons")]
        public static void AssignAllSkillIcons()
        {
            int assigned = 0;
            int skipped = 0;
            int iconIndex = 0; // fc1000부터 시작

            for (int jobIdx = 0; jobIdx < JobOrder.Length; jobIdx++)
            {
                string jobName = JobOrder[jobIdx];

                for (int skillIdx = 0; skillIdx < SkillOrder.Length; skillIdx++)
                {
                    string skillName = SkillOrder[skillIdx];
                    string skillAssetPath = $"Assets/Resources/Skills/{jobName}/{skillName}.asset";

                    var skillData = AssetDatabase.LoadAssetAtPath<SkillData>(skillAssetPath);
                    if (skillData == null)
                    {
                        Debug.LogWarning($"[IconAssign] SkillData not found: {skillAssetPath}");
                        skipped++;
                        iconIndex++;
                        continue;
                    }

                    // 아이콘 파일 로드
                    string iconFileName = $"fc{1000 + iconIndex}";
                    string iconAssetPath = $"Assets/Resources/Icon/Skill/{iconFileName}.png";

                    var iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconAssetPath);
                    if (iconSprite == null)
                    {
                        // Texture2D로 시도 후 Sprite 추출
                        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(iconAssetPath);
                        if (texture != null)
                        {
                            // TextureImporter로 Sprite 모드 확인/설정
                            var importer = AssetImporter.GetAtPath(iconAssetPath) as TextureImporter;
                            if (importer != null && importer.textureType != TextureImporterType.Sprite)
                            {
                                importer.textureType = TextureImporterType.Sprite;
                                importer.spriteImportMode = SpriteImportMode.Single;
                                importer.SaveAndReimport();
                            }
                            iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconAssetPath);
                        }
                    }

                    if (iconSprite == null)
                    {
                        Debug.LogWarning($"[IconAssign] Icon not found: {iconAssetPath}");
                        skipped++;
                        iconIndex++;
                        continue;
                    }

                    // 아이콘 배정
                    var so = new SerializedObject(skillData);
                    var iconProp = so.FindProperty("skillIcon");
                    if (iconProp != null)
                    {
                        iconProp.objectReferenceValue = iconSprite;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        assigned++;
                    }
                    else
                    {
                        skipped++;
                    }

                    iconIndex++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[IconAssign] 완료: {assigned}개 배정, {skipped}개 건너뜀 (총 {iconIndex}개 처리)");
        }
    }
}
