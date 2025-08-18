using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 인벤토리 UI 관리 시스템
    /// 인벤토리 창 표시, 슬롯 관리, 드래그&드롭
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private Transform slotContainer;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private Button sortButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text inventoryTitle;
        [SerializeField] private Text usedSlotsText;
        
        [Header("설정")]
        [SerializeField] private KeyCode toggleKey = KeyCode.I;
        [SerializeField] private int slotsPerRow = 6;
        [SerializeField] private float slotSize = 64f;
        [SerializeField] private float slotSpacing = 4f;
        
        // 인벤토리 슬롯 UI들
        private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
        private InventoryManager inventoryManager;
        private bool isOpen = false;
        
        // 드래그 상태
        private InventorySlotUI draggedSlot;
        private GameObject dragPreview;
        
        // 이벤트
        public System.Action<bool> OnInventoryToggled;
        public System.Action<int> OnSlotClicked;
        public System.Action<int> OnSlotRightClicked;
        
        private void Start()
        {
            // 인벤토리 매니저 찾기
            inventoryManager = FindObjectOfType<InventoryManager>();
            if (inventoryManager == null)
            {
                Debug.LogError("InventoryManager not found!");
                return;
            }
            
            SetupUI();
            CreateSlots();
            
            // 이벤트 구독
            inventoryManager.OnInventoryUpdated += UpdateUI;
            
            // 초기 상태 설정
            inventoryPanel.SetActive(false);
            UpdateUI();
        }
        
        private void OnDestroy()
        {
            if (inventoryManager != null)
            {
                inventoryManager.OnInventoryUpdated -= UpdateUI;
            }
        }
        
        private void Update()
        {
            HandleInput();
            HandleDragPreview();
        }
        
        /// <summary>
        /// UI 초기 설정
        /// </summary>
        private void SetupUI()
        {
            // 버튼 이벤트 연결
            if (sortButton != null)
            {
                sortButton.onClick.AddListener(SortInventory);
            }
            
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseInventory);
            }
            
            // 타이틀 설정
            if (inventoryTitle != null)
            {
                inventoryTitle.text = "인벤토리";
            }
        }
        
        /// <summary>
        /// 인벤토리 슬롯 UI 생성
        /// </summary>
        private void CreateSlots()
        {
            if (slotPrefab == null || slotContainer == null) return;
            
            // 기존 슬롯들 제거
            foreach (Transform child in slotContainer)
            {
                DestroyImmediate(child.gameObject);
            }
            slotUIs.Clear();
            
            // 그리드 레이아웃 설정
            var gridLayout = slotContainer.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                gridLayout.cellSize = new Vector2(slotSize, slotSize);
                gridLayout.spacing = new Vector2(slotSpacing, slotSpacing);
                gridLayout.constraintCount = slotsPerRow;
            }
            
            // 슬롯 생성
            int maxSlots = inventoryManager?.MaxSlots ?? 30;
            for (int i = 0; i < maxSlots; i++)
            {
                CreateSlotUI(i);
            }
        }
        
        /// <summary>
        /// 개별 슬롯 UI 생성
        /// </summary>
        private void CreateSlotUI(int slotIndex)
        {
            var slotObject = Instantiate(slotPrefab, slotContainer);
            var slotUI = slotObject.GetComponent<InventorySlotUI>();
            
            if (slotUI == null)
            {
                slotUI = slotObject.AddComponent<InventorySlotUI>();
            }
            
            slotUI.Initialize(slotIndex, this);
            slotUIs.Add(slotUI);
        }
        
        /// <summary>
        /// 입력 처리
        /// </summary>
        private void HandleInput()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleInventory();
            }
            
            // ESC로 닫기
            if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseInventory();
            }
        }
        
        /// <summary>
        /// 인벤토리 열기/닫기
        /// </summary>
        public void ToggleInventory()
        {
            isOpen = !isOpen;
            inventoryPanel.SetActive(isOpen);
            
            if (isOpen)
            {
                UpdateUI();
            }
            
            OnInventoryToggled?.Invoke(isOpen);
            
            // 커서 표시 상태 변경
            Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isOpen;
        }
        
        /// <summary>
        /// 인벤토리 닫기
        /// </summary>
        public void CloseInventory()
        {
            if (isOpen)
            {
                ToggleInventory();
            }
        }
        
        /// <summary>
        /// UI 업데이트
        /// </summary>
        private void UpdateUI()
        {
            if (inventoryManager?.Inventory == null) return;
            
            var inventory = inventoryManager.Inventory;
            
            // 사용 슬롯 수 업데이트
            if (usedSlotsText != null)
            {
                usedSlotsText.text = $"{inventory.UsedSlots} / {inventory.MaxSlots}";
            }
            
            // 각 슬롯 업데이트
            for (int i = 0; i < slotUIs.Count; i++)
            {
                var slot = inventory.GetSlot(i);
                slotUIs[i].UpdateSlot(slot);
            }
        }
        
        /// <summary>
        /// 슬롯 클릭 처리
        /// </summary>
        public void OnSlotClick(int slotIndex, bool isRightClick = false)
        {
            if (isRightClick)
            {
                OnSlotRightClick(slotIndex);
            }
            else
            {
                OnSlotLeftClick(slotIndex);
            }
        }
        
        /// <summary>
        /// 좌클릭 처리
        /// </summary>
        private void OnSlotLeftClick(int slotIndex)
        {
            OnSlotClicked?.Invoke(slotIndex);
            
            // 드래그 시작 준비 (실제 드래그는 InventorySlotUI에서 처리)
            var slot = inventoryManager.Inventory.GetSlot(slotIndex);
            if (slot != null && !slot.IsEmpty)
            {
                Debug.Log($"Selected slot {slotIndex}: {slot.Item.ItemData.ItemName}");
            }
        }
        
        /// <summary>
        /// 우클릭 처리 (아이템 사용)
        /// </summary>
        private void OnSlotRightClick(int slotIndex)
        {
            OnSlotRightClicked?.Invoke(slotIndex);
            
            var slot = inventoryManager.Inventory.GetSlot(slotIndex);
            if (slot != null && !slot.IsEmpty)
            {
                // 아이템 사용
                inventoryManager.UseItem(slotIndex);
                Debug.Log($"Used item in slot {slotIndex}");
            }
        }
        
        /// <summary>
        /// 드래그 시작
        /// </summary>
        public void StartDrag(InventorySlotUI slotUI)
        {
            draggedSlot = slotUI;
            CreateDragPreview(slotUI);
        }
        
        /// <summary>
        /// 드래그 종료
        /// </summary>
        public void EndDrag(InventorySlotUI targetSlot = null)
        {
            if (draggedSlot != null && targetSlot != null && draggedSlot != targetSlot)
            {
                // 아이템 이동
                inventoryManager.MoveItem(draggedSlot.SlotIndex, targetSlot.SlotIndex);
            }
            
            // 드래그 상태 초기화
            draggedSlot = null;
            DestroyDragPreview();
        }
        
        /// <summary>
        /// 드래그 프리뷰 생성
        /// </summary>
        private void CreateDragPreview(InventorySlotUI slotUI)
        {
            if (dragPreview != null)
            {
                DestroyDragPreview();
            }
            
            var slot = inventoryManager.Inventory.GetSlot(slotUI.SlotIndex);
            if (slot == null || slot.IsEmpty) return;
            
            // 드래그 프리뷰 오브젝트 생성
            dragPreview = new GameObject("DragPreview");
            dragPreview.transform.SetParent(transform, false);
            
            var image = dragPreview.AddComponent<Image>();
            image.sprite = slot.Item.ItemData.ItemIcon;
            image.color = new Color(1f, 1f, 1f, 0.8f);
            image.raycastTarget = false;
            
            var rectTransform = dragPreview.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(slotSize * 0.8f, slotSize * 0.8f);
        }
        
        /// <summary>
        /// 드래그 프리뷰 업데이트
        /// </summary>
        private void HandleDragPreview()
        {
            if (dragPreview != null)
            {
                Vector2 mousePosition = Input.mousePosition;
                dragPreview.transform.position = mousePosition;
            }
        }
        
        /// <summary>
        /// 드래그 프리뷰 제거
        /// </summary>
        private void DestroyDragPreview()
        {
            if (dragPreview != null)
            {
                Destroy(dragPreview);
                dragPreview = null;
            }
        }
        
        /// <summary>
        /// 인벤토리 정렬
        /// </summary>
        private void SortInventory()
        {
            inventoryManager.SortInventory();
            Debug.Log("Inventory sorted");
        }
        
        /// <summary>
        /// 아이템 툴팁 표시
        /// </summary>
        public void ShowTooltip(ItemInstance item, Vector3 position)
        {
            // 툴팁 UI 구현 (추후)
            Debug.Log($"Tooltip: {item.ItemData.ItemName} - {item.ItemData.Description}");
        }
        
        /// <summary>
        /// 툴팁 숨기기
        /// </summary>
        public void HideTooltip()
        {
            // 툴팁 숨기기 (추후)
        }
        
        /// <summary>
        /// 특정 슬롯 하이라이트
        /// </summary>
        public void HighlightSlot(int slotIndex, bool highlight)
        {
            if (slotIndex >= 0 && slotIndex < slotUIs.Count)
            {
                slotUIs[slotIndex].SetHighlight(highlight);
            }
        }
        
        /// <summary>
        /// 인벤토리 열림 상태 확인
        /// </summary>
        public bool IsOpen => isOpen;
    }
}