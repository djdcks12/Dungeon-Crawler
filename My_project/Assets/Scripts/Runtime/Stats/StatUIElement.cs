using UnityEngine;
using UnityEngine.UI;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 개별 스탯 UI 요소
    /// 스탯 표시 및 포인트 분배 버튼 포함
    /// </summary>
    public class StatUIElement : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Text statNameText;
        [SerializeField] private Text baseValueText;
        [SerializeField] private Text bonusValueText;
        [SerializeField] private Text totalValueText;
        [SerializeField] private Button increaseButton;
        [SerializeField] private Button decreaseButton;
        
        [Header("Settings")]
        [SerializeField] private StatType statType;
        [SerializeField] private string statDisplayName;
        
        // 이벤트
        public System.Action<StatType> OnStatIncreased;
        public System.Action<StatType> OnStatDecreased;
        
        private int currentBaseValue;
        private int currentBonusValue;
        
        /// <summary>
        /// 초기화
        /// </summary>
        public void Initialize(StatType type, string displayName)
        {
            statType = type;
            statDisplayName = displayName;
            
            if (statNameText != null)
                statNameText.text = displayName;
            
            SetupButtons();
        }
        
        /// <summary>
        /// 버튼 이벤트 설정
        /// </summary>
        private void SetupButtons()
        {
            if (increaseButton != null)
            {
                increaseButton.onClick.RemoveAllListeners();
                increaseButton.onClick.AddListener(() => OnStatIncreased?.Invoke(statType));
            }
            
            if (decreaseButton != null)
            {
                decreaseButton.onClick.RemoveAllListeners();
                decreaseButton.onClick.AddListener(() => OnStatDecreased?.Invoke(statType));
            }
        }
        
        /// <summary>
        /// 스탯 값 업데이트
        /// </summary>
        public void UpdateValues(int baseValue, int bonusValue)
        {
            currentBaseValue = baseValue;
            currentBonusValue = bonusValue;
            int totalValue = baseValue + bonusValue;
            
            if (baseValueText != null)
                baseValueText.text = baseValue.ToString();
            
            if (bonusValueText != null)
                bonusValueText.text = bonusValue > 0 ? $"+{bonusValue}" : "";
            
            if (totalValueText != null)
                totalValueText.text = totalValue.ToString();
        }
        
        /// <summary>
        /// 버튼 활성화/비활성화
        /// </summary>
        public void SetButtonsInteractable(bool canIncrease, bool canDecrease)
        {
            if (increaseButton != null)
                increaseButton.interactable = canIncrease;
                
            if (decreaseButton != null)
                decreaseButton.interactable = canDecrease;
        }
        
        public StatType StatType => statType;
        public string DisplayName => statDisplayName;
        public int BaseValue => currentBaseValue;
        public int BonusValue => currentBonusValue;
        public int TotalValue => currentBaseValue + currentBonusValue;
    }
}