using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 개별 인벤토리 슬롯 UI
    /// 드래그&드롭, 클릭 이벤트, 아이템 표시 처리
    /// </summary>
    public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI 컴포넌트")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image itemIconImage;
        [SerializeField] private Text quantityText;
        [SerializeField] private Image gradeFrame;
        [SerializeField] private Image highlightImage;
        
        [Header("색상 설정")]
        [SerializeField] private Color emptySlotColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color occupiedSlotColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        [SerializeField] private Color highlightColor = Color.yellow;
        [SerializeField] private Color dragOverColor = Color.green;
        
        // 슬롯 정보
        private int slotIndex;
        private InventorySlot currentSlot;
        private InventoryUI inventoryUI;
        private bool isDragging = false;
        private bool isHighlighted = false;
        
        // 프로퍼티
        public int SlotIndex => slotIndex;
        public bool IsEmpty => currentSlot?.IsEmpty ?? true;
        public ItemInstance Item => currentSlot?.Item;
        
        /// <summary>
        /// 슬롯 UI 초기화
        /// </summary>
        public void Initialize(int index, InventoryUI ui)
        {
            slotIndex = index;
            inventoryUI = ui;
            
            SetupComponents();
            UpdateSlot(null);
        }
        
        /// <summary>
        /// UI 컴포넌트 설정
        /// </summary>
        private void SetupComponents()
        {
            // 컴포넌트 자동 찾기 (없으면 생성)
            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
                if (backgroundImage == null)
                {
                    backgroundImage = gameObject.AddComponent<Image>();
                }
            }
            
            if (itemIconImage == null)
            {
                var iconObject = transform.Find("Icon");
                if (iconObject == null)
                {
                    iconObject = new GameObject("Icon").transform;
                    iconObject.SetParent(transform, false);
                    iconObject.GetComponent<RectTransform>().anchorMin = Vector2.zero;
                    iconObject.GetComponent<RectTransform>().anchorMax = Vector2.one;
                    iconObject.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                    iconObject.GetComponent<RectTransform>().offsetMax = Vector2.zero;
                }
                itemIconImage = iconObject.GetComponent<Image>();
                if (itemIconImage == null)
                {
                    itemIconImage = iconObject.gameObject.AddComponent<Image>();
                }
                itemIconImage.raycastTarget = false;
            }
            
            if (quantityText == null)
            {
                var textObject = transform.Find("Quantity");
                if (textObject == null)
                {
                    textObject = new GameObject("Quantity").transform;
                    textObject.SetParent(transform, false);
                    var rectTransform = textObject.GetComponent<RectTransform>();
                    rectTransform.anchorMin = new Vector2(0.6f, 0f);
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                }
                quantityText = textObject.GetComponent<Text>();
                if (quantityText == null)
                {
                    quantityText = textObject.gameObject.AddComponent<Text>();
                    quantityText.fontSize = 12;
                    quantityText.color = Color.white;
                    quantityText.alignment = TextAnchor.LowerRight;
                    // 기본 폰트 설정 (Unity 기본 폰트)
                    quantityText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                quantityText.raycastTarget = false;
            }
            
            if (gradeFrame == null)
            {
                var frameObject = transform.Find("GradeFrame");
                if (frameObject == null)
                {
                    frameObject = new GameObject("GradeFrame").transform;
                    frameObject.SetParent(transform, false);
                    frameObject.SetAsFirstSibling(); // 배경 위에 표시
                    var rectTransform = frameObject.GetComponent<RectTransform>();
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                }
                gradeFrame = frameObject.GetComponent<Image>();
                if (gradeFrame == null)
                {
                    gradeFrame = frameObject.gameObject.AddComponent<Image>();
                }
                gradeFrame.raycastTarget = false;
            }
            
            if (highlightImage == null)
            {
                var highlightObject = transform.Find("Highlight");
                if (highlightObject == null)
                {
                    highlightObject = new GameObject("Highlight").transform;
                    highlightObject.SetParent(transform, false);
                    highlightObject.SetAsLastSibling(); // 최상위에 표시
                    var rectTransform = highlightObject.GetComponent<RectTransform>();
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                }
                highlightImage = highlightObject.GetComponent<Image>();
                if (highlightImage == null)
                {
                    highlightImage = highlightObject.gameObject.AddComponent<Image>();
                }
                highlightImage.raycastTarget = false;
                highlightImage.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// 슬롯 업데이트
        /// </summary>
        public void UpdateSlot(InventorySlot slot)
        {
            currentSlot = slot;
            
            if (slot == null || slot.IsEmpty)
            {
                ShowEmptySlot();
            }
            else
            {
                ShowItemSlot(slot.Item);
            }
        }
        
        /// <summary>
        /// 빈 슬롯 표시
        /// </summary>
        private void ShowEmptySlot()
        {
            backgroundImage.color = emptySlotColor;
            itemIconImage.sprite = null;
            itemIconImage.color = Color.clear;
            quantityText.text = "";
            gradeFrame.color = Color.clear;
        }
        
        /// <summary>
        /// 아이템 슬롯 표시
        /// </summary>
        private void ShowItemSlot(ItemInstance item)
        {
            backgroundImage.color = occupiedSlotColor;
            
            // 아이템 아이콘
            if (item.ItemData.ItemIcon != null)
            {
                itemIconImage.sprite = item.ItemData.ItemIcon;
                itemIconImage.color = Color.white;
            }
            else
            {
                itemIconImage.sprite = null;
                itemIconImage.color = item.ItemData.GradeColor;
            }
            
            // 수량 표시
            if (item.Quantity > 1)
            {
                quantityText.text = item.Quantity.ToString();
            }
            else
            {
                quantityText.text = "";
            }
            
            // 등급 프레임
            gradeFrame.color = item.ItemData.GradeColor;
            
            // 내구도가 낮으면 빨간색 틴트
            if (item.ItemData.HasDurability && item.GetDurabilityPercentage() < 0.25f)
            {
                itemIconImage.color = Color.red;
            }
        }
        
        /// <summary>
        /// 하이라이트 설정
        /// </summary>
        public void SetHighlight(bool highlight)
        {
            isHighlighted = highlight;
            highlightImage.gameObject.SetActive(highlight);
            
            if (highlight)
            {
                highlightImage.color = highlightColor;
            }
        }
        
        /// <summary>
        /// 드래그 오버 표시
        /// </summary>
        private void SetDragOver(bool dragOver)
        {
            if (dragOver)
            {
                highlightImage.gameObject.SetActive(true);
                highlightImage.color = dragOverColor;
            }
            else if (!isHighlighted)
            {
                highlightImage.gameObject.SetActive(false);
            }
        }
        
        #region Event Handlers
        
        /// <summary>
        /// 클릭 이벤트
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDragging) return;
            
            bool isRightClick = eventData.button == PointerEventData.InputButton.Right;
            inventoryUI.OnSlotClick(slotIndex, isRightClick);
        }
        
        /// <summary>
        /// 드래그 시작
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsEmpty) return;
            
            isDragging = true;
            inventoryUI.StartDrag(this);
        }
        
        /// <summary>
        /// 드래그 중
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            // 드래그 프리뷰는 InventoryUI에서 처리
        }
        
        /// <summary>
        /// 드래그 종료
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            
            // 드롭 대상 찾기
            InventorySlotUI targetSlot = null;
            
            if (eventData.pointerCurrentRaycast.gameObject != null)
            {
                targetSlot = eventData.pointerCurrentRaycast.gameObject.GetComponent<InventorySlotUI>();
            }
            
            inventoryUI.EndDrag(targetSlot);
        }
        
        /// <summary>
        /// 드롭 이벤트
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            SetDragOver(false);
        }
        
        /// <summary>
        /// 마우스 진입
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            // 드래그 중이면 드래그 오버 표시
            if (eventData.dragging)
            {
                SetDragOver(true);
            }
            
            // 툴팁 표시
            if (!IsEmpty)
            {
                inventoryUI.ShowTooltip(Item, transform.position);
            }
        }
        
        /// <summary>
        /// 마우스 나감
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            SetDragOver(false);
            inventoryUI.HideTooltip();
        }
        
        #endregion
    }
}