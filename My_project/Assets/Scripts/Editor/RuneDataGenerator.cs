using UnityEngine;
using UnityEditor;
using System.IO;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 룬 데이터 자동 생성 에디터 스크립트
    /// 공격10 + 방어10 + 유틸10 = 30종 × 5등급 = 150개 룬 에셋
    /// </summary>
    public class RuneDataGenerator : EditorWindow
    {
        [MenuItem("Dungeon Crawler/Generate Rune Data (150)")]
        public static void Generate()
        {
            string basePath = "Assets/Resources/ScriptableObjects/Runes";
            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            int count = 0;

            // 등급별 배율
            float[] statMult = { 1f, 1.5f, 2.2f, 3f, 4.2f };
            int[] priceMult = { 100, 250, 600, 1500, 4000 };
            string[] gradeNames = { "Chipped", "Flawed", "Normal", "Flawless", "Perfect" };
            string[] gradeKorean = { "깨진", "결함있는", "보통", "완벽한", "완전한" };
            RuneGrade[] grades = { RuneGrade.Chipped, RuneGrade.Flawed, RuneGrade.Normal, RuneGrade.Flawless, RuneGrade.Perfect };

            // ===== 공격 룬 10종 =====
            var attackRunes = new (string id, string name, string desc, float str, float agi, float crit, float critDmg, float atkSpd, float eleBonus, DamageType eleType, string comboId)[]
            {
                ("rune_atk_power", "힘의 룬", "순수한 힘을 담은 룬", 3, 0, 0, 0, 0, 0, DamageType.Physical, "rune_atk_fury"),
                ("rune_atk_fury", "분노의 룬", "전투 분노를 증폭하는 룬", 2, 1, 2, 0, 0, 0, DamageType.Physical, "rune_atk_power"),
                ("rune_atk_precision", "정밀의 룬", "정확한 타격을 위한 룬", 0, 2, 4, 0, 0, 0, DamageType.Physical, "rune_atk_deadly"),
                ("rune_atk_deadly", "치명의 룬", "치명타 데미지를 높이는 룬", 0, 1, 2, 8, 0, 0, DamageType.Physical, "rune_atk_precision"),
                ("rune_atk_speed", "신속의 룬", "공격 속도를 높이는 룬", 0, 2, 0, 0, 5, 0, DamageType.Physical, ""),
                ("rune_atk_fire", "화염의 룬", "불꽃의 힘을 담은 룬", 1, 0, 0, 0, 0, 5, DamageType.Fire, "rune_atk_ice"),
                ("rune_atk_ice", "빙결의 룬", "서리의 힘을 담은 룬", 0, 0, 0, 0, 0, 5, DamageType.Ice, "rune_atk_fire"),
                ("rune_atk_lightning", "번개의 룬", "번개의 힘을 담은 룬", 0, 1, 1, 0, 3, 4, DamageType.Lightning, ""),
                ("rune_atk_poison", "독의 룬", "맹독을 담은 룬", 0, 0, 0, 0, 0, 6, DamageType.Poison, ""),
                ("rune_atk_holy", "신성의 룬", "빛의 힘을 담은 룬", 1, 0, 0, 5, 0, 4, DamageType.Holy, "rune_atk_dark"),
            };

            // ===== 방어 룬 10종 =====
            var defenseRunes = new (string id, string name, string desc, float vit, float def, float mdef, float hp, float mp, float lifesteal, string comboId)[]
            {
                ("rune_def_iron", "철벽의 룬", "강철 같은 방어력의 룬", 0, 4, 0, 0, 0, 0, "rune_def_fortress"),
                ("rune_def_fortress", "요새의 룬", "튼튼한 수비를 위한 룬", 2, 3, 1, 0, 0, 0, "rune_def_iron"),
                ("rune_def_arcane", "마법방어 룬", "마법 저항을 높이는 룬", 0, 0, 4, 0, 10, 0, "rune_def_ward"),
                ("rune_def_ward", "수호의 룬", "보호 결계의 룬", 0, 2, 3, 0, 5, 0, "rune_def_arcane"),
                ("rune_def_vitality", "생명력 룬", "생명력을 증가시키는 룬", 4, 0, 0, 20, 0, 0, "rune_def_regen"),
                ("rune_def_regen", "재생의 룬", "체력 재생의 룬", 2, 0, 0, 15, 0, 1, "rune_def_vitality"),
                ("rune_def_mana", "마나의 룬", "마력을 증가시키는 룬", 0, 0, 1, 0, 25, 0, ""),
                ("rune_def_absorb", "흡수의 룬", "데미지를 생명력으로 변환", 0, 1, 0, 10, 0, 2, ""),
                ("rune_def_balance", "균형의 룬", "공방 균형을 위한 룬", 1, 2, 2, 10, 10, 0, ""),
                ("rune_def_endure", "인내의 룬", "고난을 견디는 룬", 3, 3, 0, 25, 0, 0.5f, ""),
            };

            // ===== 유틸리티 룬 10종 =====
            var utilityRunes = new (string id, string name, string desc, float luk, float stab, float moveSpd, float cdReduce, float expBonus, float goldBonus, string comboId)[]
            {
                ("rune_util_fortune", "행운의 룬", "행운을 가져다 주는 룬", 4, 0, 0, 0, 0, 3, "rune_util_wealth"),
                ("rune_util_wealth", "재물의 룬", "금전 운을 높이는 룬", 2, 0, 0, 0, 0, 5, "rune_util_fortune"),
                ("rune_util_wisdom", "지혜의 룬", "경험치 획득을 높이는 룬", 0, 0, 0, 0, 5, 0, "rune_util_scholar"),
                ("rune_util_scholar", "학자의 룬", "심도 있는 학습의 룬", 0, 0, 0, 2, 4, 0, "rune_util_wisdom"),
                ("rune_util_wind", "바람의 룬", "이동속도를 높이는 룬", 0, 0, 5, 0, 0, 0, "rune_util_storm"),
                ("rune_util_storm", "폭풍의 룬", "폭풍의 기운의 룬", 0, 0, 4, 2, 0, 0, "rune_util_wind"),
                ("rune_util_time", "시간의 룬", "쿨다운을 줄여주는 룬", 0, 0, 0, 5, 0, 0, ""),
                ("rune_util_stability", "안정의 룬", "데미지 편차를 줄이는 룬", 0, 4, 0, 0, 0, 0, ""),
                ("rune_util_allround", "만능의 룬", "다양한 보너스의 룬", 2, 1, 2, 1, 2, 2, ""),
                ("rune_util_discovery", "발견의 룬", "탐험 보상을 높이는 룬", 3, 0, 3, 0, 3, 3, ""),
            };

            // 공격 룬 생성
            foreach (var r in attackRunes)
            {
                for (int g = 0; g < 5; g++)
                {
                    float m = statMult[g];
                    string id = $"{r.id}_{gradeNames[g].ToLower()}";
                    string name = $"{gradeKorean[g]} {r.name}";

                    var asset = ScriptableObject.CreateInstance<RuneData>();
                    var so = new SerializedObject(asset);
                    so.FindProperty("runeId").stringValue = id;
                    so.FindProperty("runeName").stringValue = name;
                    so.FindProperty("description").stringValue = r.desc;
                    so.FindProperty("runeType").enumValueIndex = (int)RuneType.Attack;
                    so.FindProperty("runeGrade").intValue = (int)grades[g];
                    so.FindProperty("socketColor").enumValueIndex = (int)SocketColor.Red;
                    so.FindProperty("buyPrice").intValue = priceMult[g];
                    so.FindProperty("sellPrice").intValue = priceMult[g] / 4;

                    var stat = so.FindProperty("statBonus");
                    stat.FindPropertyRelative("strength").floatValue = r.str * m;
                    stat.FindPropertyRelative("agility").floatValue = r.agi * m;

                    so.FindProperty("critChanceBonus").floatValue = r.crit * m;
                    so.FindProperty("critDamageBonus").floatValue = r.critDmg * m;
                    so.FindProperty("attackSpeedBonus").floatValue = r.atkSpd * m;
                    so.FindProperty("elementalType").enumValueIndex = (int)r.eleType;
                    so.FindProperty("elementalDamageBonus").floatValue = r.eleBonus * m;

                    if (!string.IsNullOrEmpty(r.comboId))
                    {
                        so.FindProperty("comboRuneId").stringValue = $"{r.comboId}_{gradeNames[g].ToLower()}";
                        so.FindProperty("comboEffectDesc").stringValue = "조합 시 효과 1.2배 증폭";
                        so.FindProperty("comboBonusMultiplier").floatValue = 0.2f;
                    }

                    so.ApplyModifiedProperties();

                    string path = $"{basePath}/{id}.asset";
                    AssetDatabase.CreateAsset(asset, path);
                    count++;
                }
            }

            // 방어 룬 생성
            foreach (var r in defenseRunes)
            {
                for (int g = 0; g < 5; g++)
                {
                    float m = statMult[g];
                    string id = $"{r.id}_{gradeNames[g].ToLower()}";
                    string name = $"{gradeKorean[g]} {r.name}";

                    var asset = ScriptableObject.CreateInstance<RuneData>();
                    var so = new SerializedObject(asset);
                    so.FindProperty("runeId").stringValue = id;
                    so.FindProperty("runeName").stringValue = name;
                    so.FindProperty("description").stringValue = r.desc;
                    so.FindProperty("runeType").enumValueIndex = (int)RuneType.Defense;
                    so.FindProperty("runeGrade").intValue = (int)grades[g];
                    so.FindProperty("socketColor").enumValueIndex = (int)SocketColor.Blue;
                    so.FindProperty("buyPrice").intValue = priceMult[g];
                    so.FindProperty("sellPrice").intValue = priceMult[g] / 4;

                    var stat = so.FindProperty("statBonus");
                    stat.FindPropertyRelative("vitality").floatValue = r.vit * m;
                    stat.FindPropertyRelative("defense").floatValue = r.def * m;
                    stat.FindPropertyRelative("magicDefense").floatValue = r.mdef * m;

                    so.FindProperty("hpBonus").floatValue = r.hp * m;
                    so.FindProperty("mpBonus").floatValue = r.mp * m;
                    so.FindProperty("lifestealPercent").floatValue = r.lifesteal * m;

                    if (!string.IsNullOrEmpty(r.comboId))
                    {
                        so.FindProperty("comboRuneId").stringValue = $"{r.comboId}_{gradeNames[g].ToLower()}";
                        so.FindProperty("comboEffectDesc").stringValue = "조합 시 효과 1.2배 증폭";
                        so.FindProperty("comboBonusMultiplier").floatValue = 0.2f;
                    }

                    so.ApplyModifiedProperties();

                    string path = $"{basePath}/{id}.asset";
                    AssetDatabase.CreateAsset(asset, path);
                    count++;
                }
            }

            // 유틸리티 룬 생성
            foreach (var r in utilityRunes)
            {
                for (int g = 0; g < 5; g++)
                {
                    float m = statMult[g];
                    string id = $"{r.id}_{gradeNames[g].ToLower()}";
                    string name = $"{gradeKorean[g]} {r.name}";

                    var asset = ScriptableObject.CreateInstance<RuneData>();
                    var so = new SerializedObject(asset);
                    so.FindProperty("runeId").stringValue = id;
                    so.FindProperty("runeName").stringValue = name;
                    so.FindProperty("description").stringValue = r.desc;
                    so.FindProperty("runeType").enumValueIndex = (int)RuneType.Utility;
                    so.FindProperty("runeGrade").intValue = (int)grades[g];
                    so.FindProperty("socketColor").enumValueIndex = (int)SocketColor.Green;
                    so.FindProperty("buyPrice").intValue = priceMult[g];
                    so.FindProperty("sellPrice").intValue = priceMult[g] / 4;

                    var stat = so.FindProperty("statBonus");
                    stat.FindPropertyRelative("luck").floatValue = r.luk * m;
                    stat.FindPropertyRelative("stability").floatValue = r.stab * m;

                    so.FindProperty("moveSpeedBonus").floatValue = r.moveSpd * m;
                    so.FindProperty("cooldownReduction").floatValue = r.cdReduce * m;
                    so.FindProperty("expBonusPercent").floatValue = r.expBonus * m;
                    so.FindProperty("goldBonusPercent").floatValue = r.goldBonus * m;

                    if (!string.IsNullOrEmpty(r.comboId))
                    {
                        so.FindProperty("comboRuneId").stringValue = $"{r.comboId}_{gradeNames[g].ToLower()}";
                        so.FindProperty("comboEffectDesc").stringValue = "조합 시 효과 1.2배 증폭";
                        so.FindProperty("comboBonusMultiplier").floatValue = 0.2f;
                    }

                    so.ApplyModifiedProperties();

                    string path = $"{basePath}/{id}.asset";
                    AssetDatabase.CreateAsset(asset, path);
                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RuneDataGenerator] {count}개 룬 에셋 생성 완료!");
            EditorUtility.DisplayDialog("완료", $"{count}개 룬 데이터가 생성되었습니다.", "OK");
        }
    }
}
