using UnityEngine;
using UnityEditor;
using System.IO;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 마운트 데이터 자동 생성 - 8종
    /// </summary>
    public class MountDataGenerator : EditorWindow
    {
        [MenuItem("Dungeon Crawler/Generate Mount Data (8)")]
        public static void Generate()
        {
            string basePath = "Assets/Resources/ScriptableObjects/Mounts";
            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            int count = 0;

            // 지상 마운트 5종
            count += CreateMount(basePath, "mount_horse", "군마", "튼튼하고 빠른 전쟁마",
                MountType.Ground, MountRarity.Common, 50f, 0, 2000, 5, "상인에게 구매", 0, 0, 0);

            count += CreateMount(basePath, "mount_wolf", "전쟁 늑대", "야수의 본능으로 달리는 늑대",
                MountType.Ground, MountRarity.Uncommon, 65f, 0, 5000, 10, "야수족 전용 상인", 1, 0, 0);

            count += CreateMount(basePath, "mount_raptor", "랩터", "민첩하고 포악한 파충류 탈것",
                MountType.Ground, MountRarity.Rare, 80f, 0, 12000, 15, "던전 보상", 0, 3, 0);

            count += CreateMount(basePath, "mount_nightmare", "악몽의 말", "불꽃을 내뿜는 지옥의 군마",
                MountType.Ground, MountRarity.Epic, 100f, 0, 30000, 25, "보스 드롭", 2, 5, 0);

            count += CreateMount(basePath, "mount_mech_spider", "기계 거미", "마키나가 만든 8족 보행 기계",
                MountType.Ground, MountRarity.Rare, 70f, 0, 15000, 20, "마키나 전용 상인", 0, 0, 15);

            // 비행 마운트 3종
            count += CreateMount(basePath, "mount_griffin", "그리폰", "독수리 머리에 사자의 몸을 가진 생물",
                MountType.Flying, MountRarity.Epic, 90f, 60f, 50000, 30, "업적 보상", 0, 5, 0);

            count += CreateMount(basePath, "mount_wyvern", "와이번", "비늘로 덮인 용족 비행 생물",
                MountType.Flying, MountRarity.Epic, 100f, 70f, 80000, 35, "드래곤 던전 클리어", 3, 8, 0);

            count += CreateMount(basePath, "mount_phoenix", "불사조", "영원히 타오르는 전설의 새",
                MountType.Flying, MountRarity.Legendary, 120f, 90f, 0, 40, "최종 보스 처치 업적", 5, 10, 10);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MountDataGenerator] {count}개 마운트 에셋 생성 완료!");
            EditorUtility.DisplayDialog("완료", $"{count}개 마운트가 생성되었습니다.", "OK");
        }

        private static int CreateMount(string basePath, string id, string name, string desc,
            MountType type, MountRarity rarity, float speed, float flySpeed,
            int price, int reqLevel, string unlockCond, float hpRegen, float expBonus, float gatherBonus)
        {
            var asset = ScriptableObject.CreateInstance<MountData>();
            var so = new SerializedObject(asset);

            so.FindProperty("mountId").stringValue = id;
            so.FindProperty("mountName").stringValue = name;
            so.FindProperty("description").stringValue = desc;
            so.FindProperty("mountType").enumValueIndex = (int)type;
            so.FindProperty("rarity").intValue = (int)rarity;
            so.FindProperty("speedBonus").floatValue = speed;
            so.FindProperty("flySpeedBonus").floatValue = flySpeed;
            so.FindProperty("purchasePrice").intValue = price;
            so.FindProperty("requiredLevel").intValue = reqLevel;
            so.FindProperty("unlockCondition").stringValue = unlockCond;
            so.FindProperty("hpRegenBonus").floatValue = hpRegen;
            so.FindProperty("expBonusPercent").floatValue = expBonus;
            so.FindProperty("gatherSpeedBonus").floatValue = gatherBonus;

            so.ApplyModifiedProperties();

            string path = $"{basePath}/{id}.asset";
            AssetDatabase.CreateAsset(asset, path);
            return 1;
        }
    }
}
