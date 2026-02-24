using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 장비 세트 효과 시스템
    /// 장착 장비 기반으로 세트 보너스를 계산하여 스탯에 반영
    /// </summary>
    public class EquipmentSetSystem : MonoBehaviour
    {
        public static EquipmentSetSystem Instance { get; private set; }

        // 모든 세트 데이터
        private EquipmentSetData[] allSets;

        // 아이템ID → 세트 매핑 (빠른 조회용)
        private Dictionary<string, List<EquipmentSetData>> itemToSets = new Dictionary<string, List<EquipmentSetData>>();

        // 이벤트
        public System.Action<EquipmentSetData, int> OnSetBonusChanged; // set, activePieces

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            LoadSetData();
        }

        private void LoadSetData()
        {
            allSets = Resources.LoadAll<EquipmentSetData>("ScriptableObjects/EquipmentSets");
            Debug.Log($"[EquipmentSet] {allSets.Length}개 세트 데이터 로드됨");

            itemToSets.Clear();
            foreach (var set in allSets)
            {
                if (set.ItemIds == null) continue;
                foreach (var itemId in set.ItemIds)
                {
                    if (string.IsNullOrEmpty(itemId)) continue;
                    if (!itemToSets.ContainsKey(itemId))
                        itemToSets[itemId] = new List<EquipmentSetData>();
                    itemToSets[itemId].Add(set);
                }
            }
        }

        /// <summary>
        /// 장착 아이템 목록에서 활성 세트와 피스 수 계산
        /// </summary>
        public Dictionary<EquipmentSetData, int> CalculateActiveSets(IEnumerable<string> equippedItemIds)
        {
            var result = new Dictionary<EquipmentSetData, int>();
            var equippedSet = new HashSet<string>(equippedItemIds);

            foreach (var set in allSets)
            {
                if (set.ItemIds == null) continue;

                int count = 0;
                foreach (var itemId in set.ItemIds)
                {
                    if (equippedSet.Contains(itemId))
                        count++;
                }

                if (count >= 2)
                    result[set] = count;
            }

            return result;
        }

        /// <summary>
        /// 세트 보너스 총합 계산
        /// </summary>
        public SetBonusTotal GetTotalSetBonuses(IEnumerable<string> equippedItemIds)
        {
            var total = new SetBonusTotal();
            var activeSets = CalculateActiveSets(equippedItemIds);

            foreach (var kvp in activeSets)
            {
                var set = kvp.Key;
                int pieces = kvp.Value;

                // 2피스 보너스
                if (pieces >= 2)
                {
                    AddStatBlock(ref total.statBonus, set.Bonus2Piece);
                    total.hpBonus += set.Bonus2HP;
                    total.mpBonus += set.Bonus2MP;
                }

                // 3피스 보너스
                if (pieces >= 3)
                {
                    AddStatBlock(ref total.statBonus, set.Bonus3Piece);
                    total.hpBonus += set.Bonus3HP;
                    total.mpBonus += set.Bonus3MP;
                    total.critChanceBonus += set.Bonus3CritChance;
                    total.critDamageBonus += set.Bonus3CritDamage;
                }

                // 4피스 보너스
                if (pieces >= 4)
                {
                    AddStatBlock(ref total.statBonus, set.Bonus4Piece);
                    total.hpBonus += set.Bonus4HP;
                    total.mpBonus += set.Bonus4MP;
                    total.critChanceBonus += set.Bonus4CritChance;
                    total.critDamageBonus += set.Bonus4CritDamage;
                    total.moveSpeedBonus += set.Bonus4MoveSpeed;
                    total.attackSpeedBonus += set.Bonus4AttackSpeed;
                }

                // 5피스 풀세트 보너스
                if (pieces >= 5)
                {
                    AddStatBlock(ref total.statBonus, set.Bonus5Piece);
                    total.hpBonus += set.Bonus5HP;
                    total.mpBonus += set.Bonus5MP;
                    total.critChanceBonus += set.Bonus5CritChance;
                    total.critDamageBonus += set.Bonus5CritDamage;
                    total.moveSpeedBonus += set.Bonus5MoveSpeed;
                    total.attackSpeedBonus += set.Bonus5AttackSpeed;
                    total.cooldownReduction += set.Bonus5CooldownReduction;
                    total.lifestealPercent += set.Bonus5Lifesteal;
                    total.expBonusPercent += set.Bonus5ExpBonus;
                }
            }

            return total;
        }

        /// <summary>
        /// 특정 아이템이 포함된 세트 목록
        /// </summary>
        public List<EquipmentSetData> GetSetsForItem(string itemId)
        {
            return itemToSets.TryGetValue(itemId, out var sets) ? sets : new List<EquipmentSetData>();
        }

        /// <summary>
        /// 세트 정보 텍스트 (아이템 툴팁용)
        /// </summary>
        public string GetSetTooltipText(string itemId, IEnumerable<string> equippedItemIds)
        {
            var sets = GetSetsForItem(itemId);
            if (sets.Count == 0) return "";

            string text = "";
            var equippedSet = new HashSet<string>(equippedItemIds);

            foreach (var set in sets)
            {
                int count = 0;
                foreach (var id in set.ItemIds)
                {
                    if (equippedSet.Contains(id))
                        count++;
                }
                text += "\n" + set.GetSetInfoText(count);
            }

            return text;
        }

        public EquipmentSetData[] AllSets => allSets;

        private void AddStatBlock(ref StatBlock target, StatBlock source)
        {
            target.strength += source.strength;
            target.agility += source.agility;
            target.vitality += source.vitality;
            target.intelligence += source.intelligence;
            target.defense += source.defense;
            target.magicDefense += source.magicDefense;
            target.luck += source.luck;
            target.stability += source.stability;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                OnSetBonusChanged = null;
                Instance = null;
            }
        }
    }

    /// <summary>
    /// 세트 보너스 총합
    /// </summary>
    public struct SetBonusTotal
    {
        public StatBlock statBonus;
        public float hpBonus;
        public float mpBonus;
        public float critChanceBonus;
        public float critDamageBonus;
        public float moveSpeedBonus;
        public float attackSpeedBonus;
        public float cooldownReduction;
        public float lifestealPercent;
        public float expBonusPercent;
    }
}
