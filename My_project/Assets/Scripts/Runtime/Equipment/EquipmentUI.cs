using UnityEngine;
using UnityEngine.UI;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 간소화된 장비 UI 시스템 - 컴파일 문제 해결용
    /// </summary>
    public class EquipmentUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject equipmentPanel;
        [SerializeField] private Button toggleEquipmentButton;
        [SerializeField] private Button closeEquipmentButton;
        
        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.E;
        [SerializeField] private bool showEquipmentOnStart = false;
        
        private bool isEquipmentVisible = false;
        
        private void Start()
        {
            SetupButtonEvents();
            
            // 초기 상태 설정
            if (equipmentPanel != null)
            {
                equipmentPanel.SetActive(showEquipmentOnStart);
                isEquipmentVisible = showEquipmentOnStart;
            }
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleEquipmentPanel();
            }
        }
        
        /// <summary>
        /// 버튼 이벤트 설정
        /// </summary>
        private void SetupButtonEvents()
        {
            if (toggleEquipmentButton != null)
            {
                toggleEquipmentButton.onClick.AddListener(ToggleEquipmentPanel);
            }
            
            if (closeEquipmentButton != null)
            {
                closeEquipmentButton.onClick.AddListener(CloseEquipmentPanel);
            }
        }
        
        /// <summary>
        /// 장비 패널 토글
        /// </summary>
        public void ToggleEquipmentPanel()
        {
            isEquipmentVisible = !isEquipmentVisible;
            if (equipmentPanel != null)
            {
                equipmentPanel.SetActive(isEquipmentVisible);
            }
            
            Debug.Log($"Equipment panel {(isEquipmentVisible ? "opened" : "closed")}");
        }
        
        /// <summary>
        /// 장비 패널 닫기
        /// </summary>
        public void CloseEquipmentPanel()
        {
            isEquipmentVisible = false;
            if (equipmentPanel != null)
            {
                equipmentPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// 슬롯 클릭 처리 (간소화 버전)
        /// </summary>
        public void OnSlotClicked(EquipmentSlot slot)
        {
            Debug.Log($"Equipment slot {slot} clicked");
        }
        
        /// <summary>
        /// 아이템 드롭 처리 (간소화 버전)
        /// </summary>
        public void OnItemDroppedToSlot(ItemInstance item, EquipmentSlot slot)
        {
            if (item?.ItemData == null) return;
            
            Debug.Log($"Item {item.ItemData.ItemName} dropped to {slot} slot");
        }
        
        /// <summary>
        /// 장비 정보 툴팁 표시 (간소화 버전)
        /// </summary>
        public void ShowEquipmentTooltip(ItemInstance item, Vector3 position)
        {
            if (item?.ItemData == null) return;
            
            Debug.Log($"Showing tooltip for: {item.ItemData.ItemName}");
        }
        
        /// <summary>
        /// 툴팁 숨기기
        /// </summary>
        public void HideEquipmentTooltip()
        {
            // 툴팁 숨기기 로직
        }
    }
}