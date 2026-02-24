using UnityEngine;
using UnityEditor;
using System.Text;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 밸런스 테이블 생성 & 조정 에디터 스크립트
    /// - 레벨별 필요 경험치 확인
    /// - 몬스터 보상 밸런스 조정
    /// - 아이템 가격 일괄 조정
    /// - 던전 보상 확인
    /// </summary>
    public class BalanceTableGenerator
    {
        // === 밸런스 상수 ===
        // 목표: 레벨 X에서 해당 레벨 권장 던전을 1회 클리어하면 ~40% 경험치 획득
        // 몬스터 30마리 처치 + 던전 보상으로 레벨업 가능하도록

        [MenuItem("Dungeon Crawler/Balance/Print Balance Table")]
        public static void PrintBalanceTable()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== 밸런스 테이블 ===\n");

            // 1. 레벨별 필요 경험치
            sb.AppendLine("--- 레벨별 필요 경험치 ---");
            sb.AppendLine("Lv | 필요 EXP | 누적 EXP | HP | MP");
            long cumulativeExp = 0;
            for (int lv = 1; lv <= 15; lv++)
            {
                long expNeeded = (long)(100 * Mathf.Pow(lv, 1.5f));
                cumulativeExp += expNeeded;
                // 기본 VIT=10 기준 (종족마다 다름)
                float hp = 100f + (10 + lv * 2) * 10f;
                float mp = 50f + (10 + lv * 1.5f) * 5f;
                sb.AppendLine($"Lv{lv,2} | {expNeeded,8} | {cumulativeExp,9} | {hp,6:F0} | {mp,5:F0}");
            }

            // 2. 몬스터 종족별 보상
            sb.AppendLine("\n--- 몬스터 종족별 보상 (Grade 100 기준) ---");
            sb.AppendLine("종족 | Base EXP | Base Gold | 처치 횟수(Lv1→2) | 처치 횟수(Lv10→11)");

            long lvl1Exp = (long)(100 * Mathf.Pow(1, 1.5f));   // 100
            long lvl10Exp = (long)(100 * Mathf.Pow(10, 1.5f));  // ~3162

            var raceData = Resources.LoadAll<MonsterRaceData>("");
            foreach (var race in raceData)
            {
                long exp = race.BaseExperience;
                long gold = race.BaseGold;
                int kills1 = exp > 0 ? (int)Mathf.Ceil((float)lvl1Exp / exp) : 999;
                int kills10 = exp > 0 ? (int)Mathf.Ceil((float)lvl10Exp / exp) : 999;
                sb.AppendLine($"{race.name,-25} | {exp,8} | {gold,9} | {kills1,17} | {kills10,18}");
            }

            // 3. 던전별 보상 요약
            sb.AppendLine("\n--- 던전별 보상 요약 ---");
            sb.AppendLine("던전 | Base EXP | Base Gold | 10층 EXP | 10층 Gold | 완주 보너스");

            var dungeonData = Resources.LoadAll<DungeonData>("");
            foreach (var dungeon in dungeonData)
            {
                long baseExp = dungeon.BaseExpReward;
                long baseGold = dungeon.BaseGoldReward;
                long floor10Exp = dungeon.CalculateExpReward(10);
                long floor10Gold = dungeon.CalculateGoldReward(10);
                float bonus = dungeon.CompletionBonusMultiplier;
                sb.AppendLine($"{dungeon.name,-25} | {baseExp,8} | {baseGold,9} | {floor10Exp,8} | {floor10Gold,9} | x{bonus:F1}");
            }

            // 4. 아이템 등급별 가격 분포
            sb.AppendLine("\n--- 아이템 등급별 가격 분포 ---");
            var items = Resources.LoadAll<ItemData>("");
            int[] counts = new int[6];
            long[] totalPrices = new long[6];
            foreach (var item in items)
            {
                int grade = (int)item.Grade;
                if (grade >= 0 && grade < 6)
                {
                    counts[grade]++;
                    totalPrices[grade] += item.GetTotalValue();
                }
            }

            sb.AppendLine("등급 | 개수 | 평균 가격");
            string[] gradeNames = { "None", "Common", "Uncommon", "Rare", "Epic", "Legendary" };
            for (int i = 1; i <= 5; i++)
            {
                long avg = counts[i] > 0 ? totalPrices[i] / counts[i] : 0;
                sb.AppendLine($"{gradeNames[i],-12} | {counts[i],4} | {avg,10}");
            }

            sb.AppendLine($"\n총 아이템 수: {items.Length}");

            Debug.Log(sb.ToString());
        }

        [MenuItem("Dungeon Crawler/Balance/Adjust Monster Rewards")]
        public static void AdjustMonsterRewards()
        {
            // 밸런스 목표:
            // Lv1 권장 몬스터(Goblin): ~20-30마리 처치로 레벨업
            // Lv5 권장 몬스터: ~25-35마리 처치로 레벨업
            // Lv10 권장 몬스터: ~30-40마리 처치로 레벨업

            var targetKillsForLevelUp = new (string raceName, int targetLevel, int targetKills, long targetExp, long targetGold)[]
            {
                // 종족, 해당 레벨, 목표 처치 수, EXP, Gold
                ("Goblin", 1, 25, 0, 0),
                ("Orc", 3, 28, 0, 0),
                ("Beast", 3, 30, 0, 0),
                ("Undead", 5, 30, 0, 0),
                ("Elemental", 7, 32, 0, 0),
                ("Demon", 8, 35, 0, 0),
                ("Construct", 6, 30, 0, 0),
                ("Dragon", 10, 40, 0, 0)
            };

            int adjusted = 0;
            var raceData = Resources.LoadAll<MonsterRaceData>("");

            foreach (var race in raceData)
            {
                for (int i = 0; i < targetKillsForLevelUp.Length; i++)
                {
                    var target = targetKillsForLevelUp[i];
                    if (!race.name.Contains(target.raceName)) continue;

                    long expNeeded = (long)(100 * Mathf.Pow(target.targetLevel, 1.5f));
                    long newBaseExp = expNeeded / target.targetKills;
                    long newBaseGold = (long)Mathf.Max(5, newBaseExp / 4);

                    if (newBaseExp < 1) newBaseExp = 1;

                    var so = new SerializedObject(race);
                    so.FindProperty("baseExperience").longValue = newBaseExp;
                    so.FindProperty("baseGold").longValue = newBaseGold;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(race);

                    Debug.Log($"[Balance] {race.name}: EXP={newBaseExp}, Gold={newBaseGold} (Lv{target.targetLevel}에서 {target.targetKills}마리로 레벨업)");
                    adjusted++;
                    break;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[BalanceTableGenerator] {adjusted}개 몬스터 종족 보상 조정 완료!");
        }

        [MenuItem("Dungeon Crawler/Balance/Adjust Item Prices")]
        public static void AdjustItemPrices()
        {
            // 아이템 가격 조정 - 등급별 배율 기반
            // Common: 기본가
            // Uncommon: 1.5x
            // Rare: 3x
            // Epic: 7x
            // Legendary: 20x

            var items = Resources.LoadAll<ItemData>("");
            int adjusted = 0;

            foreach (var item in items)
            {
                var so = new SerializedObject(item);

                long currentPrice = item.GetTotalValue();
                long newPrice = currentPrice;

                // 무기/방어구: 기본 데미지/방어 기반으로 가격 재계산
                var itemType = so.FindProperty("itemType");
                if (itemType != null)
                {
                    int typeVal = itemType.enumValueIndex;

                    // Equipment (0)
                    if (typeVal == 0)
                    {
                        var gradeProperty = so.FindProperty("grade");
                        if (gradeProperty != null)
                        {
                            int grade = gradeProperty.enumValueIndex;
                            float gradeMultiplier = grade switch
                            {
                                1 => 1.0f,    // Common
                                2 => 1.5f,    // Uncommon
                                3 => 3.0f,    // Rare
                                4 => 7.0f,    // Epic
                                5 => 20.0f,   // Legendary
                                _ => 1.0f
                            };

                            // 기본가 = 데미지 또는 방어 기반
                            var damageRange = so.FindProperty("weaponDamageRange");
                            if (damageRange != null)
                            {
                                var maxDmg = damageRange.FindPropertyRelative("maxDamage");
                                if (maxDmg != null && maxDmg.floatValue > 0)
                                {
                                    // 무기: 최대 데미지 × 10 × 등급배율
                                    newPrice = (long)(maxDmg.floatValue * 10 * gradeMultiplier);
                                }
                            }

                            if (newPrice == currentPrice && currentPrice > 0)
                            {
                                // 방어구: 현재 가격에 등급 보정만
                                float basePriceForGrade = currentPrice / gradeMultiplier;
                                if (basePriceForGrade < 10) basePriceForGrade = 10;
                                newPrice = (long)(basePriceForGrade * gradeMultiplier);
                            }
                        }
                    }
                }

                if (newPrice != currentPrice && newPrice > 0)
                {
                    so.FindProperty("sellPrice").longValue = newPrice;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(item);
                    adjusted++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[BalanceTableGenerator] {adjusted}개 아이템 가격 조정 완료!");
        }

        [MenuItem("Dungeon Crawler/Balance/Verify Economy")]
        public static void VerifyEconomy()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== 경제 검증 보고서 ===\n");

            // 1. 레벨업에 필요한 몬스터 처치 수 vs 던전 층 수
            sb.AppendLine("--- 레벨별 성장 속도 ---");
            sb.AppendLine("Lv | EXP필요 | Goblin | Orc | Undead | Dragon | 던전1층EXP");

            var raceData = Resources.LoadAll<MonsterRaceData>("");
            long goblinExp = 0, orcExp = 0, undeadExp = 0, dragonExp = 0;
            foreach (var race in raceData)
            {
                if (race.name.Contains("Goblin")) goblinExp = race.BaseExperience;
                else if (race.name.Contains("Orc")) orcExp = race.BaseExperience;
                else if (race.name.Contains("Undead")) undeadExp = race.BaseExperience;
                else if (race.name.Contains("Dragon")) dragonExp = race.BaseExperience;
            }

            for (int lv = 1; lv <= 15; lv++)
            {
                long expNeeded = (long)(100 * Mathf.Pow(lv, 1.5f));
                int gob = goblinExp > 0 ? (int)Mathf.Ceil((float)expNeeded / goblinExp) : 999;
                int orc = orcExp > 0 ? (int)Mathf.Ceil((float)expNeeded / orcExp) : 999;
                int und = undeadExp > 0 ? (int)Mathf.Ceil((float)expNeeded / undeadExp) : 999;
                int drg = dragonExp > 0 ? (int)Mathf.Ceil((float)expNeeded / dragonExp) : 999;

                sb.AppendLine($"Lv{lv,2} | {expNeeded,7} | {gob,6} | {orc,3} | {und,6} | {drg,6} |");
            }

            // 2. 골드 수입 vs 장비 가격
            sb.AppendLine("\n--- 골드 수입 vs 장비 비용 ---");

            var items = Resources.LoadAll<ItemData>("");
            long commonWeaponAvg = 0, rareWeaponAvg = 0;
            int commonCount = 0, rareCount = 0;
            foreach (var item in items)
            {
                if (item.Grade == ItemGrade.Common && item.GetTotalValue() > 50)
                {
                    commonWeaponAvg += item.GetTotalValue();
                    commonCount++;
                }
                else if (item.Grade == ItemGrade.Rare && item.GetTotalValue() > 50)
                {
                    rareWeaponAvg += item.GetTotalValue();
                    rareCount++;
                }
            }

            if (commonCount > 0) commonWeaponAvg /= commonCount;
            if (rareCount > 0) rareWeaponAvg /= rareCount;

            sb.AppendLine($"Common 장비 평균가: {commonWeaponAvg:N0} Gold");
            sb.AppendLine($"Rare 장비 평균가: {rareWeaponAvg:N0} Gold");

            long goblinGold = 0;
            foreach (var race in raceData)
            {
                if (race.name.Contains("Goblin")) goblinGold = race.BaseGold;
            }

            if (goblinGold > 0)
            {
                sb.AppendLine($"\nGoblin 1마리 골드: {goblinGold}");
                sb.AppendLine($"Common 장비 구매까지: Goblin {(commonWeaponAvg > 0 ? commonWeaponAvg / goblinGold : 0)}마리 처치");
                sb.AppendLine($"Rare 장비 구매까지: Goblin {(rareWeaponAvg > 0 ? rareWeaponAvg / goblinGold : 0)}마리 처치");
            }

            // 3. 강화 비용
            sb.AppendLine("\n--- 강화 비용 (누적) ---");
            long totalEnhanceCost = 0;
            for (int i = 0; i <= 9; i++)
            {
                long cost = (long)(100 * Mathf.Pow(1.8f, i));
                totalEnhanceCost += cost;
                sb.AppendLine($"+{i}→+{i + 1}: {cost:N0} Gold (누적: {totalEnhanceCost:N0})");
            }

            Debug.Log(sb.ToString());
        }
    }
}
