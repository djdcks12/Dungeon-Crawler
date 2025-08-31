using UnityEngine;
using UnityEngine.UI;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 아이템 툴팁 UI 관리자
    /// </summary>
    public class ItemTooltipManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private Text itemNameText;
        [SerializeField] private Text itemDescriptionText;
        [SerializeField] private Text itemStatsText;
        [SerializeField] private Image backgroundImage;
        
        [Header("Settings")]
        [SerializeField] private Vector2 offset = new Vector2(10f, 10f);
        [SerializeField] private float maxWidth = 300f;
        
        private RectTransform tooltipRect;
        private Canvas canvas;
        
        public static ItemTooltipManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            
            // 초기에는 숨김
            tooltipPanel.SetActive(false);
        }
        
        /// <summary>
        /// 툴팁 표시
        /// </summary>
        public void ShowTooltip(ItemInstance itemInstance, Vector3 mousePosition)
        {
            if (itemInstance?.ItemData == null) return;
            
            var itemData = itemInstance.ItemData;
            
            // 아이템 정보 설정
            itemNameText.text = itemData.ItemName;
            itemNameText.color = itemData.GradeColor;
            
            itemDescriptionText.text = itemData.Description;
            
            // 아이템 스탯 정보
            SetupItemStats(itemInstance);
            
            // 배경색 설정 (등급별)
            if (backgroundImage != null)
            {
                Color bgColor = itemData.GradeColor;
                bgColor.a = 0.3f;
                backgroundImage.color = bgColor;
            }
            
            // 위치 설정
            SetTooltipPosition(mousePosition);
            
            // 툴팁 표시
            tooltipPanel.SetActive(true);
        }
        
        /// <summary>
        /// 툴팁 숨김
        /// </summary>
        public void HideTooltip()
        {
            tooltipPanel.SetActive(false);
        }
        
        /// <summary>
        /// 아이템 스탯 정보 설정
        /// </summary>
        private void SetupItemStats(ItemInstance itemInstance)
        {
            if (itemStatsText == null) return;
            
            var itemData = itemInstance.ItemData;
            string statsText = "";
            
            // 수량 표시
            if (itemInstance.Quantity > 1)
            {
                statsText += $"수량: {itemInstance.Quantity}\n";
            }
            
            // 아이템 타입
            statsText += $"타입: {GetItemTypeString(itemData.ItemType)}\n";
            
            // 등급
            statsText += $"등급: {GetGradeString(itemData.Grade)}\n";
            
            // 장비 아이템인 경우 스탯 표시
            if (itemData.IsEquippable)
            {
                if (itemData.StatBonuses.HasAnyStats())
                {
                    statsText += "\n능력치:\n";
                    statsText += FormatStatBlock(itemData.StatBonuses);
                }
                
                // 내구도 표시
                statsText += $"\n내구도: {itemInstance.CurrentDurability}/{itemData.MaxDurability}";
                
                // 무기인 경우 공격력 표시
                if (itemData.IsWeapon)
                {
                    var damageRange = itemData.WeaponDamageRange;
                    statsText += $"\n공격력: {damageRange.minDamage:F0}-{damageRange.maxDamage:F0}";
                }
            }
            
            // 소모품인 경우 효과 표시
            if (itemData.IsConsumable)
            {
                if (itemData.HealAmount > 0)
                {
                    statsText += $"\nHP 회복: +{itemData.HealAmount:F0}";
                }
                if (itemData.ManaAmount > 0)
                {
                    statsText += $"\nMP 회복: +{itemData.ManaAmount:F0}";
                }
            }
            
            itemStatsText.text = statsText.TrimEnd();
        }
        
        /// <summary>
        /// 아이템 타입 문자열 변환
        /// </summary>
        private string GetItemTypeString(ItemType itemType)
        {
            return itemType switch
            {
                ItemType.Equipment => "장비",
                ItemType.Consumable => "소모품",
                ItemType.Material => "재료",
                ItemType.Quest => "퀘스트",
                _ => "기타"
            };
        }
        
        /// <summary>
        /// 등급 문자열 변환
        /// </summary>
        private string GetGradeString(ItemGrade grade)
        {
            return grade switch
            {
                ItemGrade.Common => "일반",
                ItemGrade.Uncommon => "고급",
                ItemGrade.Rare => "희귀",
                ItemGrade.Epic => "영웅",
                ItemGrade.Legendary => "전설",
                _ => "알 수 없음"
            };
        }
        
        /// <summary>
        /// 스탯 블록 포맷팅
        /// </summary>
        private string FormatStatBlock(StatBlock statBlock)
        {
            string result = "";
            
            if (statBlock.strength > 0) result += $"힘 +{statBlock.strength}\n";
            if (statBlock.agility > 0) result += $"민첩 +{statBlock.agility}\n";
            if (statBlock.vitality > 0) result += $"체력 +{statBlock.vitality}\n";
            if (statBlock.intelligence > 0) result += $"지능 +{statBlock.intelligence}\n";
            if (statBlock.defense > 0) result += $"방어력 +{statBlock.defense}\n";
            if (statBlock.magicDefense > 0) result += $"마법 방어력 +{statBlock.magicDefense}\n";
            if (statBlock.luck > 0) result += $"운 +{statBlock.luck}\n";
            if (statBlock.stability > 0) result += $"안정성 +{statBlock.stability}\n";
            
            return result.TrimEnd();
        }
        
        /// <summary>
        /// 툴팁 위치 설정
        /// </summary>
        private void SetTooltipPosition(Vector3 mousePosition)
        {
            if (canvas == null) return;
            
            // 마우스 위치를 캔버스 좌표계로 변환
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, 
                mousePosition, 
                canvas.worldCamera, 
                out localPoint);
            
            // 오프셋 적용
            localPoint += offset;
            
            // 화면 경계 체크 및 조정
            Vector2 canvasSize = (canvas.transform as RectTransform).sizeDelta;
            Vector2 tooltipSize = tooltipRect.sizeDelta;
            
            // 오른쪽 경계 체크
            if (localPoint.x + tooltipSize.x > canvasSize.x * 0.5f)
            {
                localPoint.x -= tooltipSize.x + offset.x * 2;
            }
            
            // 위쪽 경계 체크
            if (localPoint.y + tooltipSize.y > canvasSize.y * 0.5f)
            {
                localPoint.y -= tooltipSize.y + offset.y * 2;
            }
            
            tooltipRect.localPosition = localPoint;
        }
    }
}