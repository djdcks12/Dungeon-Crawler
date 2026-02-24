using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 크래프팅 레시피 데이터 (ScriptableObject)
    /// 재료 아이템들을 조합하여 새 아이템을 제작하는 레시피
    /// </summary>
    [CreateAssetMenu(fileName = "New Recipe", menuName = "Dungeon Crawler/Crafting Recipe")]
    public class CraftingRecipeData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string recipeId;
        [SerializeField] private string recipeName;
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private Sprite recipeIcon;

        [Header("분류")]
        [SerializeField] private CraftingCategory category;
        [SerializeField] private int requiredLevel = 1;
        [SerializeField] private int requiredCraftingLevel = 0; // 제작 숙련도 레벨

        [Header("재료")]
        [SerializeField] private CraftingMaterial[] materials;

        [Header("결과물")]
        [SerializeField] private string resultItemId;         // 결과 아이템 ID
        [SerializeField] private int resultCount = 1;          // 결과 개수
        [SerializeField] private ItemGrade resultGrade = ItemGrade.Common;

        [Header("제작 설정")]
        [SerializeField] private float craftTime = 2f;         // 제작 소요시간 (초)
        [SerializeField] private long goldCost = 0;            // 추가 골드 비용
        [SerializeField] private float successRate = 1f;       // 성공 확률 (0~1)
        [SerializeField] private float criticalRate = 0.05f;   // 크리티컬 제작 확률 (등급 +1)
        [SerializeField] private int craftingExpReward = 10;    // 제작 경험치 보상

        [Header("해금 조건")]
        [SerializeField] private bool isDefaultUnlocked = true; // 기본 해금 여부
        [SerializeField] private string unlockQuestId;           // 해금 퀘스트 ID
        [SerializeField] private string prerequisiteRecipeId;    // 선행 레시피 ID

        // Properties
        public string RecipeId => recipeId;
        public string RecipeName => recipeName;
        public string Description => description;
        public Sprite RecipeIcon => recipeIcon;
        public CraftingCategory Category => category;
        public int RequiredLevel => requiredLevel;
        public int RequiredCraftingLevel => requiredCraftingLevel;
        public CraftingMaterial[] Materials => materials;
        public string ResultItemId => resultItemId;
        public int ResultCount => resultCount;
        public ItemGrade ResultGrade => resultGrade;
        public float CraftTime => craftTime;
        public long GoldCost => goldCost;
        public float SuccessRate => successRate;
        public float CriticalRate => criticalRate;
        public int CraftingExpReward => craftingExpReward;
        public bool IsDefaultUnlocked => isDefaultUnlocked;
        public string UnlockQuestId => unlockQuestId;
        public string PrerequisiteRecipeId => prerequisiteRecipeId;
    }

    /// <summary>
    /// 크래프팅 카테고리
    /// </summary>
    public enum CraftingCategory
    {
        Weapon,         // 무기 제작
        Armor,          // 방어구 제작
        Consumable,     // 소모품 제작
        Enhancement,    // 강화 재료 제작
        Accessory,      // 장신구 제작
        Special         // 특수 아이템 제작
    }

    /// <summary>
    /// 크래프팅 재료 정의
    /// </summary>
    [System.Serializable]
    public struct CraftingMaterial
    {
        public string itemId;       // 재료 아이템 ID
        public int quantity;        // 필요 수량

        public CraftingMaterial(string id, int qty)
        {
            itemId = id;
            quantity = qty;
        }
    }
}
