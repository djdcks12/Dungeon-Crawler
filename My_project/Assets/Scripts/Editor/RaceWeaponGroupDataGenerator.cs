using UnityEngine;
using UnityEditor;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 종족-무기군 조합별 RaceWeaponGroupData 에셋 자동 생성 (13개)
    /// Human(4) + Elf(3) + Beast(5) + Machina(1)
    /// </summary>
    public class RaceWeaponGroupDataGenerator
    {
        private static string basePath = "Assets/Resources/RaceWeaponGroups";

        [MenuItem("Dungeon Crawler/Generate/Race Weapon Group Data")]
        public static void Generate()
        {
            EnsureFolder(basePath);

            int created = 0;
            int skipped = 0;

            // Human: SwordShield, TwoHandedSword, Bow, Fist
            created += CreateAsset(Race.Human, WeaponGroup.SwordShield,
                "인간 한손검/방패 - 균형 잡힌 공방 조합. 방어와 공격 전환이 자유롭습니다.",
                8f, 10f, 14f, 14f, 12f, 10f, 3, ref skipped);
            created += CreateAsset(Race.Human, WeaponGroup.TwoHandedSword,
                "인간 양손 대검 - 강력한 일격에 특화된 전투 스타일.",
                6f, 8f, 12f, 12f, 10f, 8f, 3, ref skipped);
            created += CreateAsset(Race.Human, WeaponGroup.Bow,
                "인간 활 - 원거리 정밀 사격과 기동성을 겸비한 스타일.",
                6f, 8f, 14f, 14f, 10f, 8f, 4, ref skipped);
            created += CreateAsset(Race.Human, WeaponGroup.Fist,
                "인간 격투 - 빠른 연타와 근접 기술로 적을 압도합니다.",
                8f, 10f, 16f, 14f, 12f, 10f, 2, ref skipped);

            // Elf: Bow, Staff, Wand
            created += CreateAsset(Race.Elf, WeaponGroup.Bow,
                "엘프 활 - 자연의 힘을 담은 정밀한 장거리 사격.",
                6f, 8f, 14f, 14f, 10f, 8f, 4, ref skipped);
            created += CreateAsset(Race.Elf, WeaponGroup.Staff,
                "엘프 지팡이 - 강력한 마법 공격과 광역 마법에 특화.",
                6f, 8f, 12f, 12f, 10f, 8f, 3, ref skipped);
            created += CreateAsset(Race.Elf, WeaponGroup.Wand,
                "엘프 마법봉 - 빠른 시전과 정밀 마법 컨트롤.",
                6f, 8f, 14f, 14f, 10f, 8f, 3, ref skipped);

            // Beast: TwoHandedAxe, Dagger, Bow, Fist, Staff
            created += CreateAsset(Race.Beast, WeaponGroup.TwoHandedAxe,
                "수인 양손 도끼 - 야수의 힘을 담은 파괴적인 타격.",
                6f, 8f, 12f, 12f, 10f, 8f, 3, ref skipped);
            created += CreateAsset(Race.Beast, WeaponGroup.Dagger,
                "수인 단검 - 민첩한 야수의 본능으로 빈틈을 파고드는 암살 스타일.",
                8f, 10f, 16f, 14f, 12f, 10f, 2, ref skipped);
            created += CreateAsset(Race.Beast, WeaponGroup.Bow,
                "수인 활 - 야수의 감각으로 먹잇감을 포착하는 사냥꾼 스타일.",
                6f, 8f, 14f, 14f, 10f, 8f, 4, ref skipped);
            created += CreateAsset(Race.Beast, WeaponGroup.Fist,
                "수인 격투 - 타고난 야수의 발톱과 이빨로 싸우는 원시 격투.",
                8f, 10f, 16f, 14f, 12f, 10f, 2, ref skipped);
            created += CreateAsset(Race.Beast, WeaponGroup.Staff,
                "수인 지팡이 - 자연의 정령과 교감하는 원시 마법.",
                6f, 8f, 12f, 12f, 10f, 8f, 3, ref skipped);

            // Machina: Fist (default/fallback)
            created += CreateAsset(Race.Machina, WeaponGroup.Fist,
                "기계족 격투 - 강화된 금속 주먹과 정밀 기어로 강력한 타격.",
                8f, 10f, 16f, 14f, 12f, 10f, 2, ref skipped);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[RaceWeaponGroupDataGenerator] Complete! Created: {created}, Skipped (existing): {skipped}");
        }

        private static int CreateAsset(Race race, WeaponGroup weaponGroup, string description,
            float idleFps, float walkFps, float attackFps, float hitFps, float castFps, float deathFps,
            int attackDamageFrame, ref int skipped)
        {
            string assetName = $"{race}_{weaponGroup}";
            string assetPath = $"{basePath}/{assetName}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<RaceWeaponGroupData>(assetPath);
            if (existing != null)
            {
                skipped++;
                return 0;
            }

            var data = ScriptableObject.CreateInstance<RaceWeaponGroupData>();

            // Public fields
            data.race = race;
            data.weaponGroup = weaponGroup;
            data.combinationName = assetName;
            data.description = description;

            AssetDatabase.CreateAsset(data, assetPath);

            // Private SerializeField access
            var so = new SerializedObject(data);
            so.FindProperty("idleFrameRate").floatValue = idleFps;
            so.FindProperty("walkFrameRate").floatValue = walkFps;
            so.FindProperty("attackFrameRate").floatValue = attackFps;
            so.FindProperty("attackDamageFrame").intValue = attackDamageFrame;
            so.FindProperty("hitFrameRate").floatValue = hitFps;
            so.FindProperty("castingFrameRate").floatValue = castFps;
            so.FindProperty("deathFrameRate").floatValue = deathFps;

            // Sprite arrays are left empty - sprites need to be added manually
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(data);

            return 1;
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
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
}
