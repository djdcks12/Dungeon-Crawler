using UnityEngine;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 보스 보상 시스템 - 보스 처치 시 확정 레어+ 드롭, 첫 클리어 보너스
    /// </summary>
    public class BossRewardSystem : MonoBehaviour
    {
        public static BossRewardSystem Instance { get; private set; }

        [Header("보상 설정")]
        [SerializeField] private float bossExpMultiplier = 5f;
        [SerializeField] private float bossGoldMultiplier = 3f;
        [SerializeField] private int guaranteedDropCount = 2;

        [Header("첫 클리어 보너스")]
        [SerializeField] private float firstClearExpBonus = 2f;
        [SerializeField] private float firstClearGoldBonus = 2f;

        // 첫 클리어 기록 (보스ID → 클리어 여부)
        private HashSet<string> clearedBosses = new HashSet<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 보스 처치 보상 생성
        /// </summary>
        public BossReward GenerateBossReward(BossMonsterAI boss, MonsterEntity bossEntity, int dungeonFloor)
        {
            if (boss == null || bossEntity == null)
                return new BossReward();

            var reward = new BossReward();
            string bossId = $"{boss.BossType}_{boss.TargetFloor}";

            // 기본 보상 (몬스터 종족 기본 경험치/골드의 배율)
            long baseExp = bossEntity.RaceData != null ? bossEntity.RaceData.BaseExperience : 100;
            long baseGold = bossEntity.RaceData != null ? bossEntity.RaceData.BaseGold : 50;
            reward.expReward = (long)(baseExp * bossExpMultiplier);
            reward.goldReward = (long)(baseGold * bossGoldMultiplier);

            // 보스 타입별 보상 배율
            float typeMultiplier = GetBossTypeMultiplier(boss.BossType);
            reward.expReward = (long)(reward.expReward * typeMultiplier);
            reward.goldReward = (long)(reward.goldReward * typeMultiplier);

            // 확정 드롭 아이템 생성
            reward.guaranteedItems = GenerateGuaranteedDrops(boss.BossType, dungeonFloor);

            // 첫 클리어 보너스
            if (!clearedBosses.Contains(bossId))
            {
                reward.isFirstClear = true;
                reward.expReward = (long)(reward.expReward * firstClearExpBonus);
                reward.goldReward = (long)(reward.goldReward * firstClearGoldBonus);
                clearedBosses.Add(bossId);
            }

            // 전투 로그
            if (CombatLogUI.Instance != null)
            {
                CombatLogUI.Instance.LogSystem($"보스 {boss.BossName} 처치!");
                CombatLogUI.Instance.LogExpGain(reward.expReward);
                CombatLogUI.Instance.LogGoldPickup("파티", reward.goldReward);
                if (reward.isFirstClear)
                    CombatLogUI.Instance.LogSystem("첫 클리어 보너스 적용!");
            }

            return reward;
        }

        /// <summary>
        /// 확정 드롭 아이템 생성
        /// </summary>
        private List<ItemInstance> GenerateGuaranteedDrops(BossType bossType, int floor)
        {
            var items = new List<ItemInstance>();
            ItemDatabase.Initialize();

            // 보스 타입별 최소 등급 결정
            ItemGrade minGrade;
            switch (bossType)
            {
                case BossType.FloorGuardian:
                    minGrade = ItemGrade.Uncommon;
                    break;
                case BossType.EliteBoss:
                    minGrade = ItemGrade.Rare;
                    break;
                case BossType.FinalBoss:
                    minGrade = ItemGrade.Epic;
                    break;
                case BossType.HiddenBoss:
                    minGrade = ItemGrade.Epic;
                    break;
                default:
                    minGrade = ItemGrade.Uncommon;
                    break;
            }

            for (int i = 0; i < guaranteedDropCount; i++)
            {
                ItemGrade grade = RollItemGrade(minGrade, bossType);
                var itemsOfGrade = ItemDatabase.GetItemsByGrade(grade);

                if (itemsOfGrade != null && itemsOfGrade.Count > 0)
                {
                    var randomItem = itemsOfGrade[Random.Range(0, itemsOfGrade.Count)];
                    items.Add(new ItemInstance(randomItem, 1));
                }
            }

            // 최종/히든 보스: 전설 아이템 추가 확률
            if (bossType == BossType.FinalBoss || bossType == BossType.HiddenBoss)
            {
                float legendaryChance = bossType == BossType.HiddenBoss ? 0.25f : 0.15f;
                if (Random.value < legendaryChance)
                {
                    var legendaryItems = ItemDatabase.GetItemsByGrade(ItemGrade.Legendary);
                    if (legendaryItems != null && legendaryItems.Count > 0)
                    {
                        var legendaryItem = legendaryItems[Random.Range(0, legendaryItems.Count)];
                        items.Add(new ItemInstance(legendaryItem, 1));
                    }
                }
            }

            return items;
        }

        /// <summary>
        /// 아이템 등급 롤
        /// </summary>
        private ItemGrade RollItemGrade(ItemGrade minGrade, BossType bossType)
        {
            float roll = Random.value;

            switch (minGrade)
            {
                case ItemGrade.Uncommon:
                    if (roll < 0.05f) return ItemGrade.Epic;
                    if (roll < 0.25f) return ItemGrade.Rare;
                    return ItemGrade.Uncommon;

                case ItemGrade.Rare:
                    if (roll < 0.10f) return ItemGrade.Legendary;
                    if (roll < 0.30f) return ItemGrade.Epic;
                    return ItemGrade.Rare;

                case ItemGrade.Epic:
                    if (roll < 0.15f) return ItemGrade.Legendary;
                    return ItemGrade.Epic;

                default:
                    return minGrade;
            }
        }

        /// <summary>
        /// 보스 타입별 보상 배율
        /// </summary>
        private float GetBossTypeMultiplier(BossType bossType)
        {
            switch (bossType)
            {
                case BossType.FloorGuardian: return 1.0f;
                case BossType.EliteBoss: return 1.5f;
                case BossType.FinalBoss: return 3.0f;
                case BossType.HiddenBoss: return 4.0f;
                default: return 1.0f;
            }
        }

        /// <summary>
        /// 보스 첫 클리어 여부 확인
        /// </summary>
        public bool IsBossCleared(BossType bossType, int floor)
        {
            string bossId = $"{bossType}_{floor}";
            return clearedBosses.Contains(bossId);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }

    /// <summary>
    /// 보스 보상 데이터
    /// </summary>
    [System.Serializable]
    public struct BossReward
    {
        public long expReward;
        public long goldReward;
        public List<ItemInstance> guaranteedItems;
        public bool isFirstClear;
    }
}
