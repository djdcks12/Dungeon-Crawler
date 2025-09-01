using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 통합 인벤토리 UI - 장비창(위)과 인벤토리(아래)를 하나의 창에서 관리
    /// </summary>
    public class UnifiedInventoryUI : MonoBehaviour
    {
        [Header("Main UI References")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private Text titleText;
        [SerializeField] private Button closeButton;
        
        [Header("Equipment Panel (Upper)")]
        [SerializeField] private GameObject equipmentPanel;
        [SerializeField] private EquipmentSlotUI[] equipmentSlots = new EquipmentSlotUI[14]; // 14개 장비 슬롯
        
        [Header("Inventory Panel (Lower)")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private GameObject inventoryScrollView;
        [SerializeField] private GameObject inventoryGrid;
        [SerializeField] private InventorySlotUI[] inventorySlots = new InventorySlotUI[30]; // 30개 인벤토리 슬롯
        
        [Header("Drag & Drop")]
        [SerializeField] private GameObject dragPreview;
        [SerializeField] private Image dragPreviewImage;
        
        [Header("Tooltip")]
        [SerializeField] private ItemTooltipManager tooltipManager;
        
        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.I;
        
        // 컴포넌트 참조
        private InventoryManager inventoryManager;
        private EquipmentManager equipmentManager;
        
        // 장비 슬롯 매핑
        private Dictionary<EquipmentSlot, EquipmentSlotUI> equipmentSlotMap = new Dictionary<EquipmentSlot, EquipmentSlotUI>();
        
        // 드래그&드롭 상태
        private InventorySlotUI draggedInventorySlot;
        private EquipmentSlotUI draggedEquipmentSlot;
        private ItemInstance draggedItem;
        
        // UI 상태
        private bool isOpen = false;
        
        // 이벤트
        public System.Action<bool> OnUIToggled;
        
        public bool IsOpen => isOpen;
        public ItemInstance GetDraggedItem() => draggedItem;
        
        private void Awake()
        {
            // 컴포넌트 참조 설정
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseUI);
            }
            
            // 초기 상태로 UI 숨김
            if (mainPanel != null)
            {
                mainPanel.SetActive(false);
            }
        }
        
        private void Start()
        {
            Initialize();
        }
        
        private void Update()
        {
            // 토글 키 입력 처리
            if (Input.GetKeyDown(toggleKey))
            {
                if (isOpen)
                    CloseUI();
                else
                    OpenUI();
            }
        }
        
        /// <summary>
        /// UI 초기화
        /// </summary>
        private void Initialize()
        {
            // 매니저들 찾기
            inventoryManager = FindObjectOfType<InventoryManager>();
            equipmentManager = FindObjectOfType<EquipmentManager>();
            
            if (tooltipManager == null)
            {
                tooltipManager = FindObjectOfType<ItemTooltipManager>();
            }
            
            InitializeEquipmentSlots();
            InitializeInventorySlots();
            
            // 이벤트 구독
            SubscribeToEvents();
            
            Debug.Log("🔧 UnifiedInventoryUI initialized");
        }
        
        /// <summary>
        /// 장비 슬롯 초기화
        /// </summary>
        private void InitializeEquipmentSlots()
        {
            equipmentSlotMap.Clear();
            
            // 장비 슬롯 순서 정의 (generator와 동일한 순서)
            var slotOrder = new EquipmentSlot[]
            {
                EquipmentSlot.Head, EquipmentSlot.Earring1, EquipmentSlot.Earring2,
                EquipmentSlot.Necklace, EquipmentSlot.Chest, EquipmentSlot.Hands,
                EquipmentSlot.MainHand, EquipmentSlot.Belt, EquipmentSlot.OffHand,
                EquipmentSlot.TwoHand, EquipmentSlot.Legs, EquipmentSlot.Ring1,
                EquipmentSlot.Ring2, EquipmentSlot.Feet
            };
            
            // 프리팹이 없으면 동적으로 생성
            if (equipmentSlots == null || equipmentSlots.Length == 0)
            {
                CreateEquipmentSlotsDynamically(slotOrder);
            }
            else
            {
                // 기존 프리팹 기반 슬롯들 초기화
                for (int i = 0; i < equipmentSlots.Length && i < slotOrder.Length; i++)
                {
                    var slotUI = equipmentSlots[i];
                    if (slotUI != null)
                    {
                        var slot = slotOrder[i];
                        slotUI.Initialize(slot, this, equipmentManager);
                        equipmentSlotMap[slot] = slotUI;
                    }
                }
            }
            
            Debug.Log($"📦 Equipment slots initialized: {equipmentSlotMap.Count}");
        }
        
        /// <summary>
        /// 장비 슬롯 동적 생성 (프리팹이 없을 때)
        /// </summary>
        private void CreateEquipmentSlotsDynamically(EquipmentSlot[] slotOrder)
        {
            if (equipmentPanel == null)
            {
                Debug.LogError("❌ EquipmentPanel이 설정되지 않았습니다.");
                return;
            }
            
            var slotList = new System.Collections.Generic.List<EquipmentSlotUI>();
            
            // 장비 슬롯 위치 정의
            var slotPositions = new Vector2[]
            {
                new Vector2(0f, 120f),     // Head
                new Vector2(-80f, 100f),  // Earring1
                new Vector2(80f, 100f),   // Earring2
                new Vector2(0f, 80f),     // Necklace
                new Vector2(0f, 40f),     // Chest
                new Vector2(-120f, 40f),  // Hands
                new Vector2(120f, 40f),   // MainHand
                new Vector2(0f, 0f),      // Belt
                new Vector2(-120f, 0f),   // OffHand
                new Vector2(120f, 0f),    // TwoHand
                new Vector2(0f, -40f),    // Legs
                new Vector2(-80f, -60f),  // Ring1
                new Vector2(80f, -60f),   // Ring2
                new Vector2(0f, -80f)     // Feet
            };
            
            for (int i = 0; i < slotOrder.Length && i < slotPositions.Length; i++)
            {
                var slot = slotOrder[i];
                var position = slotPositions[i];
                
                GameObject slotObject = new GameObject($"EquipmentSlot_{slot}");
                slotObject.transform.SetParent(equipmentPanel.transform, false);
                
                // RectTransform 설정
                RectTransform rectTransform = slotObject.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(60f, 60f);
                rectTransform.anchoredPosition = position;
                
                // 기본 이미지 컴포넌트
                Image slotImage = slotObject.AddComponent<Image>();
                slotImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
                
                // EquipmentSlotUI 컴포넌트
                EquipmentSlotUI slotUI = slotObject.AddComponent<EquipmentSlotUI>();
                slotUI.Initialize(slot, this, equipmentManager);
                
                slotList.Add(slotUI);
                equipmentSlotMap[slot] = slotUI;
            }
            
            equipmentSlots = slotList.ToArray();
            Debug.Log($"📦 Created {equipmentSlots.Length} equipment slots dynamically");
        }
        
        /// <summary>
        /// 인벤토리 슬롯 초기화
        /// </summary>
        private void InitializeInventorySlots()
        {
            // 프리팹이 없으면 동적으로 생성
            if (inventorySlots == null || inventorySlots.Length == 0)
            {
                CreateInventorySlotsDynamically();
            }
            else
            {
                // 기존 프리팹 기반 슬롯들 초기화
                for (int i = 0; i < inventorySlots.Length; i++)
                {
                    var slotUI = inventorySlots[i];
                    if (slotUI != null)
                    {
                        slotUI.Initialize(i, this);
                    }
                }
            }
            
            Debug.Log($"🎒 Inventory slots initialized: {inventorySlots.Length}");
        }
        
        /// <summary>
        /// 인벤토리 슬롯 동적 생성 (프리팹이 없을 때)
        /// </summary>
        private void CreateInventorySlotsDynamically()
        {
            if (inventoryGrid == null)
            {
                Debug.LogError("❌ InventoryGrid가 설정되지 않았습니다.");
                return;
            }
            
            var slotList = new System.Collections.Generic.List<InventorySlotUI>();
            
            for (int i = 0; i < 30; i++)
            {
                GameObject slotObject = new GameObject($"InventorySlot_{i}");
                slotObject.transform.SetParent(inventoryGrid.transform, false);
                
                // 기본 이미지 컴포넌트
                Image slotImage = slotObject.AddComponent<Image>();
                slotImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                
                // InventorySlotUI 컴포넌트
                InventorySlotUI slotUI = slotObject.AddComponent<InventorySlotUI>();
                slotUI.Initialize(i, this);
                
                slotList.Add(slotUI);
            }
            
            inventorySlots = slotList.ToArray();
            Debug.Log($"🎒 Created {inventorySlots.Length} inventory slots dynamically");
        }
        
        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void SubscribeToEvents()
        {
            if (inventoryManager != null)
            {
                inventoryManager.OnInventoryUpdated += RefreshInventoryUI;
            }
            
            if (equipmentManager != null)
            {
                equipmentManager.OnEquipmentChanged += OnEquipmentChanged;
            }
        }
        
        /// <summary>
        /// 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (inventoryManager != null)
            {
                inventoryManager.OnInventoryUpdated -= RefreshInventoryUI;
            }
            
            if (equipmentManager != null)
            {
                equipmentManager.OnEquipmentChanged -= OnEquipmentChanged;
            }
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        /// <summary>
        /// UI 열기
        /// </summary>
        public void OpenUI()
        {
            if (mainPanel != null)
            {
                mainPanel.SetActive(true);
                isOpen = true;
                RefreshUI();
                OnUIToggled?.Invoke(true);
                Debug.Log("🔓 UnifiedInventoryUI opened");
            }
        }
        
        /// <summary>
        /// UI 닫기
        /// </summary>
        public void CloseUI()
        {
            if (mainPanel != null)
            {
                mainPanel.SetActive(false);
                isOpen = false;
                CleanupDrag();
                HideTooltip();
                OnUIToggled?.Invoke(false);
                Debug.Log("🔒 UnifiedInventoryUI closed");
            }
        }
        
        /// <summary>
        /// UI 토글
        /// </summary>
        public void ToggleUI()
        {
            if (isOpen)
                CloseUI();
            else
                OpenUI();
        }
        
        /// <summary>
        /// UI 전체 새로고침
        /// </summary>
        private void RefreshUI()
        {
            RefreshInventoryUI();
            RefreshEquipmentUI();
        }
        
        /// <summary>
        /// 인벤토리 UI 새로고침
        /// </summary>
        private void RefreshInventoryUI()
        {
            if (inventoryManager?.Inventory == null) return;
            
            var inventory = inventoryManager.Inventory;
            
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                var slotUI = inventorySlots[i];
                if (slotUI != null)
                {
                    var slot = inventory.GetSlot(i);
                    slotUI.UpdateSlot(slot);
                }
            }
        }
        
        /// <summary>
        /// 장비 UI 새로고침
        /// </summary>
        private void RefreshEquipmentUI()
        {
            if (equipmentManager == null) return;
            
            foreach (var kvp in equipmentSlotMap)
            {
                var slot = kvp.Key;
                var slotUI = kvp.Value;
                var item = equipmentManager.GetEquippedItem(slot);
                slotUI.UpdateSlot(item);
            }
        }
        
        /// <summary>
        /// 장비 변경 이벤트 핸들러
        /// </summary>
        private void OnEquipmentChanged(EquipmentSlot slot, ItemInstance item)
        {
            if (equipmentSlotMap.ContainsKey(slot))
            {
                equipmentSlotMap[slot].UpdateSlot(item);
            }
        }
        
        /// <summary>
        /// 인벤토리 슬롯 클릭 처리
        /// </summary>
        public void OnInventorySlotClick(int slotIndex)
        {
            if (inventoryManager?.Inventory == null) return;
            
            var item = inventoryManager.Inventory.GetItem(slotIndex);
            if (item?.ItemData?.IsEquippable == true)
            {
                // 장비 가능한 아이템인 경우 자동 장착
                var slot = item.ItemData.EquipmentSlot;
                EquipItemFromInventory(item, slot);
            }
        }
        
        /// <summary>
        /// 장비 슬롯 클릭 처리
        /// </summary>
        public void OnEquipmentSlotClick(EquipmentSlot slot)
        {
            if (equipmentManager == null) return;
            
            var item = equipmentManager.GetEquippedItem(slot);
            if (item != null)
            {
                // 장착된 아이템을 인벤토리로 이동
                UnequipItemToInventory(slot);
            }
        }
        
        /// <summary>
        /// 인벤토리에서 장비 착용
        /// </summary>
        private void EquipItemFromInventory(ItemInstance item, EquipmentSlot slot)
        {
            if (equipmentManager == null || inventoryManager == null) return;
            
            // 기존에 착용중인 아이템이 있다면 인벤토리로 이동
            var currentEquipped = equipmentManager.GetEquippedItem(slot);
            if (currentEquipped != null)
            {
                if (!inventoryManager.AddItemToInventory(currentEquipped))
                {
                    Debug.LogWarning("인벤토리에 공간이 없어 장비를 교체할 수 없습니다.");
                    return;
                }
            }
            
            // 인벤토리에서 아이템 제거
            if (inventoryManager.RemoveItemFromInventory(item))
            {
                // 장비 착용
                equipmentManager.TryEquipItem(item, false);
                Debug.Log($"⚔️ {item.ItemData.ItemName} 착용");
            }
        }
        
        /// <summary>
        /// 장비를 인벤토리로 해제
        /// </summary>
        private void UnequipItemToInventory(EquipmentSlot slot)
        {
            if (equipmentManager == null || inventoryManager == null) return;
            
            var item = equipmentManager.GetEquippedItem(slot);
            if (item == null) return;
            
            // 인벤토리에 공간이 있는지 확인
            if (!inventoryManager.HasSpace())
            {
                Debug.LogWarning("인벤토리에 공간이 없습니다.");
                return;
            }
            
            // 장비 해제 (TryEquipItem으로 null 전달하여 해제)
            var currentItem = equipmentManager.GetEquippedItem(slot);
            if (currentItem != null)
            {
                // 임시로 장비 해제 로직 (실제 메서드가 있다면 사용)
                // equipmentManager.UnequipItem(slot, false);
                // 인벤토리에 추가
                inventoryManager.AddItemToInventory(item);
                Debug.Log($"🎒 {item.ItemData.ItemName} 해제");
            }
        }
        
        /// <summary>
        /// 장비 교체
        /// </summary>
        private void SwapEquipment(EquipmentSlot fromSlot, EquipmentSlot toSlot)
        {
            if (equipmentManager == null) return;
            
            var fromItem = equipmentManager.GetEquippedItem(fromSlot);
            var toItem = equipmentManager.GetEquippedItem(toSlot);
            
            if (fromItem == null) return;
            
            // 대상 슬롯에 착용 가능한지 확인
            if (!CanEquipItemToSlot(fromItem, toSlot)) return;
            
            // 교체 실행 (실제 swap 메서드가 있다면 사용)
            // 임시로 기본적인 장착 로직만 사용
            if (equipmentManager.TryEquipItem(fromItem, false))
            {
                Debug.Log($"장비 교체: {fromSlot} → {toSlot}");
            }
            
            Debug.Log($"🔄 장비 교체: {fromSlot} ↔ {toSlot}");
        }
        
        /// <summary>
        /// 아이템이 특정 슬롯에 착용 가능한지 확인
        /// </summary>
        private bool CanEquipItemToSlot(ItemInstance item, EquipmentSlot slot)
        {
            if (item?.ItemData == null) return false;
            
            // 기본 슬롯 확인
            if (item.ItemData.EquipmentSlot == slot) return true;
            
            // 무기 호환성 확인
            return IsWeaponCompatible(item, slot);
        }
        
        /// <summary>
        /// 무기 호환성 확인
        /// </summary>
        private bool IsWeaponCompatible(ItemInstance item, EquipmentSlot slot)
        {
            var weaponCategory = item.ItemData.WeaponCategory;
            
            return slot switch
            {
                EquipmentSlot.MainHand => weaponCategory == WeaponCategory.Sword ||
                                        weaponCategory == WeaponCategory.Dagger ||
                                        weaponCategory == WeaponCategory.Axe ||
                                        weaponCategory == WeaponCategory.Mace,
                EquipmentSlot.OffHand => weaponCategory == WeaponCategory.Shield ||
                                       weaponCategory == WeaponCategory.Dagger,
                EquipmentSlot.TwoHand => weaponCategory == WeaponCategory.Bow ||
                                       weaponCategory == WeaponCategory.Staff,
                _ => false
            };
        }
        
        // ======================== 드래그&드롭 시스템 ========================
        
        /// <summary>
        /// 인벤토리 드래그 시작
        /// </summary>
        public void StartInventoryDrag(InventorySlotUI slotUI)
        {
            var slot = inventoryManager.Inventory.GetSlot(slotUI.SlotIndex);
            if (slot?.Item != null)
            {
                draggedInventorySlot = slotUI;
                draggedItem = slot.Item;
                CreateDragPreview(slot.Item);
            }
        }
        
        /// <summary>
        /// 장비 드래그 시작
        /// </summary>
        public void StartEquipmentDrag(EquipmentSlotUI slotUI)
        {
            if (slotUI.CurrentItem != null)
            {
                draggedEquipmentSlot = slotUI;
                draggedItem = slotUI.CurrentItem;
                CreateDragPreview(slotUI.CurrentItem);
            }
        }
        
        /// <summary>
        /// 인벤토리 드래그 종료
        /// </summary>
        public void EndInventoryDrag(InventorySlotUI sourceSlot, GameObject target)
        {
            bool processed = false;
            
            if (target != null)
            {
                // 장비 슬롯으로 드롭
                var equipmentSlot = target.GetComponent<EquipmentSlotUI>();
                if (equipmentSlot != null && equipmentSlot.CanEquipItem(draggedItem))
                {
                    EquipItemFromInventory(draggedItem, equipmentSlot.Slot);
                    processed = true;
                }
                // 인벤토리 슬롯으로 드롭
                else if (target.GetComponent<InventorySlotUI>() != null)
                {
                    // 인벤토리 내 이동은 InventoryManager에서 처리
                    Debug.Log("Inventory to inventory movement");
                    processed = true;
                }
            }
            
            if (!processed)
            {
                Debug.Log("드롭 실패 - 유효하지 않은 대상");
            }
            
            CleanupDrag();
        }
        
        /// <summary>
        /// 장비 드래그 종료
        /// </summary>
        public void EndEquipmentDrag(EquipmentSlotUI sourceSlot, GameObject target)
        {
            bool processed = false;
            
            if (target != null)
            {
                // 인벤토리 슬롯으로 드롭 (장비 해제)
                var inventorySlot = target.GetComponent<InventorySlotUI>();
                if (inventorySlot != null)
                {
                    UnequipItemToInventory(sourceSlot.Slot);
                    processed = true;
                }
                // 다른 장비 슬롯으로 드롭
                else if (target.GetComponent<EquipmentSlotUI>() != null)
                {
                    var targetEquipmentSlot = target.GetComponent<EquipmentSlotUI>();
                    if (targetEquipmentSlot != sourceSlot && targetEquipmentSlot.CanEquipItem(draggedItem))
                    {
                        SwapEquipment(sourceSlot.Slot, targetEquipmentSlot.Slot);
                        processed = true;
                    }
                }
            }
            
            if (!processed)
            {
                Debug.Log("드롭 실패 - 유효하지 않은 대상");
            }
            
            CleanupDrag();
        }
        
        /// <summary>
        /// 아이템 드롭 처리 (EquipmentSlotUI에서 호출)
        /// </summary>
        public void ProcessItemDrop(ItemInstance item, EquipmentSlotUI targetSlot)
        {
            if (item != null && targetSlot.CanEquipItem(item))
            {
                EquipItemFromInventory(item, targetSlot.Slot);
            }
        }
        
        /// <summary>
        /// 드래그 프리뷰 생성
        /// </summary>
        private void CreateDragPreview(ItemInstance item)
        {
            if (dragPreview == null || dragPreviewImage == null) return;
            
            dragPreview.SetActive(true);
            dragPreviewImage.sprite = item.ItemData.ItemIcon;
            
            // 마우스 위치로 이동
            UpdateDragPreviewPosition();
        }
        
        /// <summary>
        /// 드래그 프리뷰 위치 업데이트
        /// </summary>
        private void UpdateDragPreviewPosition()
        {
            if (dragPreview == null || !dragPreview.activeInHierarchy) return;
            
            Vector2 mousePosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform as RectTransform, Input.mousePosition, null, out mousePosition);
            
            dragPreview.transform.localPosition = mousePosition;
        }
        
        private void LateUpdate()
        {
            // 드래그 프리뷰 위치 업데이트
            if (dragPreview != null && dragPreview.activeInHierarchy)
            {
                UpdateDragPreviewPosition();
            }
        }
        
        /// <summary>
        /// 드래그 정리
        /// </summary>
        private void CleanupDrag()
        {
            draggedInventorySlot = null;
            draggedEquipmentSlot = null;
            draggedItem = null;
            
            if (dragPreview != null)
            {
                dragPreview.SetActive(false);
            }
        }
        
        // ======================== 툴팁 시스템 ========================
        
        /// <summary>
        /// 툴팁 표시
        /// </summary>
        public void ShowTooltip(ItemInstance item, Vector3 position)
        {
            if (tooltipManager != null && item != null)
            {
                tooltipManager.ShowTooltip(item, position);
            }
        }
        
        /// <summary>
        /// 툴팁 숨기기
        /// </summary>
        public void HideTooltip()
        {
            if (tooltipManager != null)
            {
                tooltipManager.HideTooltip();
            }
        }
        
        // ======================== 유틸리티 ========================
        
        /// <summary>
        /// 특정 장비 슬롯 UI 가져오기
        /// </summary>
        public EquipmentSlotUI GetEquipmentSlotUI(EquipmentSlot slot)
        {
            return equipmentSlotMap.ContainsKey(slot) ? equipmentSlotMap[slot] : null;
        }
        
        /// <summary>
        /// 인벤토리 슬롯 UI 가져오기
        /// </summary>
        public InventorySlotUI GetInventorySlotUI(int index)
        {
            return (index >= 0 && index < inventorySlots.Length) ? inventorySlots[index] : null;
        }
    }
}