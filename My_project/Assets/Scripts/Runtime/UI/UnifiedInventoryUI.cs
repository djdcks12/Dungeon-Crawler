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
            inventoryManager = FindFirstObjectByType<InventoryManager>();
            equipmentManager = FindFirstObjectByType<EquipmentManager>();
            
            if (tooltipManager == null)
            {
                tooltipManager = FindFirstObjectByType<ItemTooltipManager>();
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
            OnUIToggled = null;
            UnsubscribeFromEvents();

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
            }
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
            
            try
            {
                // 1. 먼저 빈 슬롯 찾기
                int emptySlot = FindEmptyInventorySlot();
                if (emptySlot == -1)
                {
                    Debug.LogWarning($"❌ No empty slots available for {item.ItemData.ItemName}");
                    return;
                }
                
                // 2. 먼저 장비 슬롯에서 제거
                bool unequipped = equipmentManager.UnequipItem(slot, false);
                if (!unequipped)
                {
                    Debug.LogError($"❌ Failed to unequip {item.ItemData.ItemName} from slot");
                    return;
                }
                
                // 3. 직접 인벤토리 슬롯에 배치 (InventoryManager 우회)
                var inventorySlot = inventoryManager.Inventory?.GetSlot(emptySlot);
                if (inventorySlot != null)
                {
                    inventorySlot.SetItem(item);
                    
                    // 4. UI 즉시 새로고침
                    RefreshInventoryUI();
                    
                    // 5. 이벤트 발생 (네트워크 동기화 트리거)
                    inventoryManager.OnInventoryUpdated?.Invoke();
                    
                    Debug.Log($"🎒 {item.ItemData.ItemName} 해제 완료 - 슬롯 {emptySlot}에 직접 배치");
                }
                else
                {
                    Debug.LogError($"❌ Failed to get inventory slot {emptySlot}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Exception during unequip {item.ItemData.ItemName}: {e.Message}");
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
            if (!item.ItemData.IsWeapon) return false;
            
            WeaponGroup weaponGroup = item.ItemData.WeaponGroup;
            EquipmentSlot requiredSlot = WeaponTypeMapper.GetEquipmentSlot(weaponGroup);
            
            // 기본적으로 WeaponGroup이 요구하는 슬롯과 일치해야 함
            if (requiredSlot == slot) return true;
            
            // 예외 케이스: 단검은 MainHand와 OffHand 둘 다 가능
            if (weaponGroup == WeaponGroup.Dagger && 
                (slot == EquipmentSlot.MainHand || slot == EquipmentSlot.OffHand))
                return true;
            
            return false;
        }
        
        // ======================== 드래그&드롭 시스템 ========================
        
        
        
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
        /// 드래그 프리뷰 생성
        /// </summary>
        private void CreateDragPreview(ItemInstance item)
        {
            if (dragPreview == null)
            {
                // 드래그 프리뷰 오브젝트 생성
                dragPreview = new GameObject("DragPreview");
                var canvasGroup = dragPreview.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0.8f;
                canvasGroup.blocksRaycasts = false;
                
                var image = dragPreview.AddComponent<Image>();
                var rectTransform = dragPreview.GetComponent<RectTransform>();
                rectTransform.SetParent(transform, false);
                rectTransform.sizeDelta = new Vector2(64, 64);
                
                dragPreview.SetActive(false);
            }
            
            if (item?.ItemData?.ItemIcon != null)
            {
                var image = dragPreview.GetComponent<Image>();
                image.sprite = item.ItemData.ItemIcon;
                image.color = Color.white;
                dragPreview.SetActive(true);
                
                // 마우스 위치에 프리뷰 배치
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
        
        /// <summary>
        /// 드래그 실패 시 UI 즉시 복원
        /// </summary>
        private void RestoreUIAfterFailedDrag()
        {
            // UI 즉시 새로고침으로 원래 상태 복원
            RefreshUI();
            
            // 드래그 정리
            CleanupDrag();
            
            Debug.Log("🔄 UI restored after failed drag operation");
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
        
        // ======================== 드래그 앤 드롭 핸들러 ========================
        
        /// <summary>
        /// 인벤토리 드래그 시작
        /// </summary>
        public void StartInventoryDrag(InventorySlotUI sourceSlot)
        {
            if (sourceSlot == null || sourceSlot.IsEmpty) return;
            
            draggedInventorySlot = sourceSlot;
            draggedItem = sourceSlot.Item;
            
            // 드래그 프리뷰 생성
            CreateDragPreview(draggedItem);
            
            Debug.Log($"🔥 Started dragging {draggedItem.ItemData.ItemName} from inventory slot {sourceSlot.SlotIndex}");
        }
        
        /// <summary>
        /// 장비 드래그 시작
        /// </summary>
        public void StartEquipmentDrag(EquipmentSlotUI sourceSlot)
        {
            if (sourceSlot == null || sourceSlot.IsEmpty) return;
            
            draggedEquipmentSlot = sourceSlot;
            draggedItem = sourceSlot.CurrentItem;
            
            // 드래그 프리뷰 생성
            CreateDragPreview(draggedItem);
            
            Debug.Log($"🔥 Started dragging {draggedItem.ItemData.ItemName} from equipment slot {sourceSlot.Slot}");
        }
        
        /// <summary>
        /// 장비 드래그 종료
        /// </summary>
        public void EndEquipmentDrag(EquipmentSlotUI sourceSlot, GameObject target)
        {
            bool processed = false;
            
            if (target != null && draggedItem != null)
            {
                Debug.Log($"🎯 Attempting to drop {draggedItem.ItemData.ItemName} from equipment slot {sourceSlot.Slot} to {target.name}");
                
                // 인벤토리 슬롯으로 드롭 (장비 해제)
                var inventorySlotUI = target.GetComponent<InventorySlotUI>();
                if (inventorySlotUI != null)
                {
                    processed = TryUnequipItemToInventory(sourceSlot.Slot, inventorySlotUI.SlotIndex);
                }
                // 다른 장비 슬롯으로 드롭 (장비 교환)
                else if (target.GetComponent<EquipmentSlotUI>() != null)
                {
                    var targetEquipmentSlot = target.GetComponent<EquipmentSlotUI>();
                    if (targetEquipmentSlot != sourceSlot)
                    {
                        processed = TrySwapEquipment(sourceSlot.Slot, targetEquipmentSlot.Slot);
                    }
                    else
                    {
                        processed = true; // 같은 슬롯에 드롭하면 성공으로 처리
                    }
                }
            }
            
            if (!processed)
            {
                Debug.Log($"❌ 드롭 실패 - 유효하지 않은 대상: {target?.name}");
                RestoreUIAfterFailedDrag(); // UI 즉시 복원
            }
            else
            {
                CleanupDrag(); // 성공 시에만 일반 정리
            }
        }
        
        /// <summary>
        /// 아이템 드롭 처리 (EquipmentSlotUI에서 호출)
        /// </summary>
        public void ProcessItemDrop(ItemInstance item, EquipmentSlotUI targetSlot)
        {
            if (item != null && targetSlot.CanEquipItem(item) && draggedInventorySlot != null)
            {
                TryEquipItemFromInventory(item, draggedInventorySlot.SlotIndex, targetSlot.Slot);
            }
        }
        
        
        /// <summary>
        /// 인벤토리 드래그 종료
        /// </summary>
        public void EndInventoryDrag(InventorySlotUI sourceSlot, GameObject target)
        {
            bool processed = false;
            
            Debug.Log($"🔍 EndInventoryDrag called - Source: {sourceSlot.SlotIndex}, Target: {target?.name}, DraggedItem: {draggedItem?.ItemData?.ItemName}");
            
            if (target != null && draggedItem != null)
            {
                Debug.Log($"🎯 Attempting to drop {draggedItem.ItemData.ItemName} on {target.name}");
                
                // 장비 슬롯으로 드롭 (장착 시도)
                var equipmentSlotUI = target.GetComponent<EquipmentSlotUI>();
                if (equipmentSlotUI != null)
                {
                    Debug.Log($"🔧 Trying to equip to {equipmentSlotUI.Slot}");
                    if (equipmentSlotUI.CanEquipItem(draggedItem))
                    {
                        processed = TryEquipItemFromInventory(draggedItem, sourceSlot.SlotIndex, equipmentSlotUI.Slot);
                        Debug.Log($"⚔️ Equipment result: {processed}");
                    }
                    else
                    {
                        Debug.Log($"❌ Cannot equip {draggedItem.ItemData.ItemName} to {equipmentSlotUI.Slot} slot");
                        processed = false;
                    }
                }
                // 인벤토리 슬롯으로 드롭 (슬롯 간 이동)
                else if (target.GetComponent<InventorySlotUI>() != null)
                {
                    var targetSlot = target.GetComponent<InventorySlotUI>();
                    Debug.Log($"📦 Trying to move from slot {sourceSlot.SlotIndex} to slot {targetSlot.SlotIndex}");
                    if (targetSlot != sourceSlot)
                    {
                        processed = TrySwapInventoryItems(sourceSlot.SlotIndex, targetSlot.SlotIndex);
                        Debug.Log($"🔄 Move result: {processed}");
                    }
                    else
                    {
                        processed = true; // 같은 슬롯에 드롭하면 성공으로 처리
                        Debug.Log($"✅ Same slot drop - treated as success");
                    }
                }
                else
                {
                    Debug.Log($"❌ Target has no valid component: {target.name}");
                }
            }
            else
            {
                Debug.Log($"❌ Missing requirements - Target: {target != null}, DraggedItem: {draggedItem != null}");
            }
            
            if (!processed)
            {
                Debug.Log($"❌ 드롭 실패 - 유효하지 않은 대상: {target?.name}");
                RestoreUIAfterFailedDrag(); // UI 즉시 복원
            }
            else
            {
                Debug.Log($"✅ 드롭 성공!");
                CleanupDrag(); // 성공 시에만 일반 정리
            }
        }
        
        /// <summary>
        /// 인벤토리에서 장비로 아이템 장착 시도
        /// </summary>
        private bool TryEquipItemFromInventory(ItemInstance item, int inventoryIndex, EquipmentSlot equipmentSlot)
        {
            if (equipmentManager == null || inventoryManager == null)
            {
                Debug.LogError("❌ EquipmentManager or InventoryManager is null");
                return false;
            }
            
            try
            {
                // 먼저 인벤토리에서 아이템 제거
                bool removed = inventoryManager.RemoveItem(inventoryIndex);
                if (!removed)
                {
                    Debug.LogError($"❌ Failed to remove item from inventory slot {inventoryIndex}");
                    return false;
                }
                
                Debug.Log($"📦 Removed {item.ItemData.ItemName} from inventory slot {inventoryIndex}");
                
                // 특정 슬롯에 장비 착용 시도
                bool equipped = TryEquipToSpecificSlot(item, equipmentSlot);
                if (equipped)
                {
                    Debug.Log($"⚔ {item.ItemData.ItemName} 착용 성공");
                    return true;
                }
                else
                {
                    Debug.LogError($"❌ Failed to equip {item.ItemData.ItemName}");
                    
                    // 장착 실패 시 인벤토리로 복원
                    bool restored = AddItemToSpecificSlot(item, inventoryIndex);
                    if (!restored)
                    {
                        // 원래 슬롯에 복원 실패하면 빈 슬롯에 추가
                        restored = inventoryManager.AddItem(item);
                    }
                    
                    if (restored)
                    {
                        Debug.Log($"🔄 {item.ItemData.ItemName} restored to inventory");
                        // UI 즉시 새로고침
                        RefreshInventoryUI();
                    }
                    else
                    {
                        Debug.LogError($"💥 CRITICAL: Failed to restore {item.ItemData.ItemName} to inventory!");
                    }
                    
                    return false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Exception during equipment: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 특정 슬롯에 아이템 추가
        /// </summary>
        private bool AddItemToSpecificSlot(ItemInstance item, int slotIndex)
        {
            if (inventoryManager?.Inventory == null) return false;
            
            // 해당 슬롯이 비어있는지 확인
            var slot = inventoryManager.Inventory.GetSlot(slotIndex);
            if (slot != null && slot.IsEmpty)
            {
                // 슬롯이 비어있으면 직접 추가
                slot.SetItem(item);
                return true;
            }
            else
            {
                // 슬롯이 차있으면 실패
                return false;
            }
        }
        
        /// <summary>
        /// 인벤토리 슬롯 간 아이템 이동/교환 (자유 배치 지원)
        /// </summary>
        private bool TrySwapInventoryItems(int fromIndex, int toIndex)
        {
            if (inventoryManager?.Inventory == null) return false;
            
            var fromSlot = inventoryManager.Inventory.GetSlot(fromIndex);
            var toSlot = inventoryManager.Inventory.GetSlot(toIndex);
            
            if (fromSlot == null || toSlot == null) return false;
            
            var fromItem = fromSlot.Item;
            var toItem = toSlot.Item;
            
            // 빈 슬롯으로 이동 (자유 배치)
            if (toItem == null)
            {
                // 단순 이동
                toSlot.SetItem(fromItem);
                fromSlot.SetItem(null);
                
                // UI 즉시 새로고침
                RefreshInventoryUI();
                
                // 이벤트 발생 (네트워크 동기화 트리거)
                inventoryManager.OnInventoryUpdated?.Invoke();
                
                Debug.Log($"📦 Moved {fromItem?.ItemData.ItemName} from slot {fromIndex} to {toIndex}");
                return true;
            }
            else
            {
                // 아이템 교환
                toSlot.SetItem(fromItem);
                fromSlot.SetItem(toItem);
                
                // UI 즉시 새로고침
                RefreshInventoryUI();
                
                // 이벤트 발생 (네트워크 동기화 트리거)
                inventoryManager.OnInventoryUpdated?.Invoke();
                
                Debug.Log($"🔄 Swapped items between slots {fromIndex} and {toIndex}");
                return true;
            }
        }
        
        /// <summary>
        /// 인벤토리 슬롯 클릭 처리
        /// </summary>
        public void OnInventorySlotClick(int slotIndex)
        {
            if (inventoryManager == null) return;
            
            var item = inventoryManager.Inventory?.GetItem(slotIndex);
            if (item != null)
            {
                Debug.Log($"🖱️ Left-clicked on {item.ItemData.ItemName} in slot {slotIndex} - showing info only");

                // 아이템 정보 툴팁 표시
                if (tooltipManager != null)
                {
                    tooltipManager.ShowTooltip(item, Input.mousePosition);
                }
            }
        }
        
        /// <summary>
        /// 특정 슬롯에 아이템 장착
        /// </summary>
        private bool TryEquipToSpecificSlot(ItemInstance item, EquipmentSlot slot)
        {
            if (equipmentManager?.Equipment == null) return false;
            
            try
            {
                // 기존 장착된 아이템이 있으면 인벤토리로 이동
                var existingItem = equipmentManager.GetEquippedItem(slot);
                if (existingItem != null)
                {
                    bool unequipped = inventoryManager.AddItem(existingItem);
                    if (!unequipped)
                    {
                        Debug.LogError($"❌ Cannot unequip {existingItem.ItemData.ItemName} - inventory full");
                        return false;
                    }
                }
                
                // 새 아이템 장착
                equipmentManager.Equipment.SetEquippedItem(slot, item);
                equipmentManager.OnEquipmentChanged?.Invoke(slot, item);
                
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Exception during equipment to slot {slot}: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 장비를 인벤토리로 해제 (드래그 앤 드롭용)
        /// </summary>
        private bool TryUnequipItemToInventory(EquipmentSlot fromSlot, int toInventorySlot)
        {
            Debug.Log($"🔧 TryUnequipItemToInventory: {fromSlot} to inventory slot {toInventorySlot}");
            
            if (equipmentManager?.Equipment == null)
            {
                Debug.LogError($"❌ EquipmentManager or Equipment is null");
                return false;
            }
            
            if (inventoryManager?.Inventory == null)
            {
                Debug.LogError($"❌ InventoryManager or Inventory is null");
                return false;
            }
            
            var item = equipmentManager.GetEquippedItem(fromSlot);
            if (item == null)
            {
                Debug.LogWarning($"❌ No item equipped in slot {fromSlot}");
                return false;
            }
            
            Debug.Log($"🎯 Attempting to unequip {item.ItemData.ItemName} from {fromSlot}");
            
            try
            {
                // 1. 목표 슬롯이 비어있는지 확인
                var targetSlot = inventoryManager.Inventory.GetSlot(toInventorySlot);
                if (targetSlot == null)
                {
                    Debug.LogError($"❌ Target inventory slot {toInventorySlot} is invalid (null)");
                    return false;
                }
                
                if (!targetSlot.IsEmpty)
                {
                    Debug.LogWarning($"❌ Target inventory slot {toInventorySlot} is occupied by {targetSlot.Item?.ItemData?.ItemName}");
                    return false;
                }
                
                Debug.Log($"✅ Target slot {toInventorySlot} is empty and ready");
                
                // 2. 장비 슬롯에서 직접 제거 (UnequipItem 메서드 우회)
                equipmentManager.Equipment.SetEquippedItem(fromSlot, null);
                equipmentManager.OnEquipmentChanged?.Invoke(fromSlot, null);
                Debug.Log($"✅ Removed item from equipment slot {fromSlot}");
                
                // 3. 직접 특정 슬롯에 배치 (InventoryManager 우회)
                targetSlot.SetItem(item);
                Debug.Log($"✅ Placed item in inventory slot {toInventorySlot}");
                
                // 4. UI 즉시 새로고침
                RefreshInventoryUI();
                RefreshEquipmentUI();
                Debug.Log($"✅ UI refreshed");
                
                // 5. 이벤트 발생 (네트워크 동기화 트리거)  
                inventoryManager.OnInventoryUpdated?.Invoke();
                Debug.Log($"✅ Network sync triggered");
                
                Debug.Log($"🎉 Successfully unequipped {item.ItemData.ItemName} from {fromSlot} to inventory slot {toInventorySlot}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Exception during unequip from {fromSlot}: {e.Message}\nStackTrace: {e.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// 장비 슬롯 간 아이템 교환
        /// </summary>
        private bool TrySwapEquipment(EquipmentSlot fromSlot, EquipmentSlot toSlot)
        {
            if (equipmentManager?.Equipment == null) return false;
            
            var fromItem = equipmentManager.GetEquippedItem(fromSlot);
            var toItem = equipmentManager.GetEquippedItem(toSlot);
            
            if (fromItem == null) return false;
            
            try
            {
                // 교환
                equipmentManager.Equipment.SetEquippedItem(fromSlot, toItem);
                equipmentManager.Equipment.SetEquippedItem(toSlot, fromItem);
                
                // 이벤트 호출
                equipmentManager.OnEquipmentChanged?.Invoke(fromSlot, toItem);
                equipmentManager.OnEquipmentChanged?.Invoke(toSlot, fromItem);
                
                Debug.Log($"🔄 Swapped equipment: {fromSlot} <-> {toSlot}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Exception during equipment swap {fromSlot}<->{toSlot}: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 장비 데이터에 호환되는 슬롯 찾기
        /// </summary>
        private EquipmentSlot GetCompatibleEquipmentSlot(ItemData itemData)
        {
            // ItemData의 EquipmentSlot을 직접 사용
            var equipmentSlot = itemData.EquipmentSlot;
            
            // 반지나 귀걸이의 경우 빈 슬롯 찾기
            if (equipmentSlot == EquipmentSlot.Ring1)
            {
                if (equipmentManager.GetEquippedItem(EquipmentSlot.Ring1) == null)
                    return EquipmentSlot.Ring1;
                else
                    return EquipmentSlot.Ring2;
            }
            else if (equipmentSlot == EquipmentSlot.Earring1)
            {
                if (equipmentManager.GetEquippedItem(EquipmentSlot.Earring1) == null)
                    return EquipmentSlot.Earring1;
                else
                    return EquipmentSlot.Earring2;
            }
            
            return equipmentSlot;
        }
        
        // ======================== 유틸리티 ========================
        
        /// <summary>
        /// 빈 인벤토리 슬롯 찾기
        /// </summary>
        private int FindEmptyInventorySlot()
        {
            if (inventoryManager?.Inventory == null) return -1;
            
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                var slot = inventoryManager.Inventory.GetSlot(i);
                if (slot != null && slot.IsEmpty)
                {
                    return i;
                }
            }
            
            return -1; // 빈 슬롯 없음
        }
        
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