using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 장비 세트 효과 데이터 (ScriptableObject)
    /// 특정 장비 조합 착용 시 세트 보너스 부여
    /// </summary>
    [CreateAssetMenu(fileName = "New Equipment Set", menuName = "Dungeon Crawler/Equipment Set")]
    public class EquipmentSetData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string setId;
        [SerializeField] private string setName;
        [TextArea(2, 3)]
        [SerializeField] private string description;

        [Header("세트 구성 아이템")]
        [SerializeField] private string[] itemIds;  // 세트에 포함되는 아이템 ID들

        [Header("2피스 보너스")]
        [SerializeField] private StatBlock bonus2Piece;
        [SerializeField] private float bonus2HP;
        [SerializeField] private float bonus2MP;
        [TextArea(1, 2)]
        [SerializeField] private string bonus2Desc;

        [Header("3피스 보너스")]
        [SerializeField] private StatBlock bonus3Piece;
        [SerializeField] private float bonus3HP;
        [SerializeField] private float bonus3MP;
        [SerializeField] private float bonus3CritChance;
        [SerializeField] private float bonus3CritDamage;
        [TextArea(1, 2)]
        [SerializeField] private string bonus3Desc;

        [Header("4피스 보너스")]
        [SerializeField] private StatBlock bonus4Piece;
        [SerializeField] private float bonus4HP;
        [SerializeField] private float bonus4MP;
        [SerializeField] private float bonus4CritChance;
        [SerializeField] private float bonus4CritDamage;
        [SerializeField] private float bonus4MoveSpeed;
        [SerializeField] private float bonus4AttackSpeed;
        [TextArea(1, 2)]
        [SerializeField] private string bonus4Desc;

        [Header("5피스 풀세트 보너스")]
        [SerializeField] private StatBlock bonus5Piece;
        [SerializeField] private float bonus5HP;
        [SerializeField] private float bonus5MP;
        [SerializeField] private float bonus5CritChance;
        [SerializeField] private float bonus5CritDamage;
        [SerializeField] private float bonus5MoveSpeed;
        [SerializeField] private float bonus5AttackSpeed;
        [SerializeField] private float bonus5CooldownReduction;
        [SerializeField] private float bonus5Lifesteal;
        [SerializeField] private float bonus5ExpBonus;
        [TextArea(1, 2)]
        [SerializeField] private string bonus5Desc;

        [Header("테마")]
        [SerializeField] private ItemGrade setGrade;
        [SerializeField] private Race requiredRace;  // None이면 모든 종족

        // Properties
        public string SetId => setId;
        public string SetName => setName;
        public string Description => description;
        public string[] ItemIds => itemIds;
        public ItemGrade SetGrade => setGrade;
        public Race RequiredRace => requiredRace;

        public StatBlock Bonus2Piece => bonus2Piece;
        public float Bonus2HP => bonus2HP;
        public float Bonus2MP => bonus2MP;
        public string Bonus2Desc => bonus2Desc;

        public StatBlock Bonus3Piece => bonus3Piece;
        public float Bonus3HP => bonus3HP;
        public float Bonus3MP => bonus3MP;
        public float Bonus3CritChance => bonus3CritChance;
        public float Bonus3CritDamage => bonus3CritDamage;
        public string Bonus3Desc => bonus3Desc;

        public StatBlock Bonus4Piece => bonus4Piece;
        public float Bonus4HP => bonus4HP;
        public float Bonus4MP => bonus4MP;
        public float Bonus4CritChance => bonus4CritChance;
        public float Bonus4CritDamage => bonus4CritDamage;
        public float Bonus4MoveSpeed => bonus4MoveSpeed;
        public float Bonus4AttackSpeed => bonus4AttackSpeed;
        public string Bonus4Desc => bonus4Desc;

        public StatBlock Bonus5Piece => bonus5Piece;
        public float Bonus5HP => bonus5HP;
        public float Bonus5MP => bonus5MP;
        public float Bonus5CritChance => bonus5CritChance;
        public float Bonus5CritDamage => bonus5CritDamage;
        public float Bonus5MoveSpeed => bonus5MoveSpeed;
        public float Bonus5AttackSpeed => bonus5AttackSpeed;
        public float Bonus5CooldownReduction => bonus5CooldownReduction;
        public float Bonus5Lifesteal => bonus5Lifesteal;
        public float Bonus5ExpBonus => bonus5ExpBonus;
        public string Bonus5Desc => bonus5Desc;

        /// <summary>
        /// 활성 피스 수에 따른 세트 보너스 정보 텍스트
        /// </summary>
        public string GetSetInfoText(int activePieces)
        {
            string text = $"<color=#FFD700>{setName}</color> ({activePieces}/{itemIds.Length})\n";

            if (itemIds.Length >= 2)
                text += FormatBonusLine(2, bonus2Desc, activePieces >= 2);
            if (itemIds.Length >= 3)
                text += FormatBonusLine(3, bonus3Desc, activePieces >= 3);
            if (itemIds.Length >= 4)
                text += FormatBonusLine(4, bonus4Desc, activePieces >= 4);
            if (itemIds.Length >= 5)
                text += FormatBonusLine(5, bonus5Desc, activePieces >= 5);

            return text;
        }

        private string FormatBonusLine(int pieces, string desc, bool active)
        {
            string color = active ? "#00FF00" : "#808080";
            return $"<color={color}>({pieces}) {desc}</color>\n";
        }
    }
}
