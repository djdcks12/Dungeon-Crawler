using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 통합 인벤토리 + 장비 UI 프리팹 자동 생성기 (세로 배치)
    /// 위쪽: 장비창, 아래쪽: 인벤토리
    /// </summary>
    public static class UnifiedInventoryPrefabGenerator
    {
        [MenuItem("Dungeon Crawler/Generate Unified Inventory UI")]
        public static void GenerateUnifiedInventoryUI()
        {
            // 메인 캔버스 생성
            GameObject canvasObject = CreateMainCanvas();
            
            // UnifiedInventoryUI 컴포넌트 추가
            UnifiedInventoryUI unifiedUI = canvasObject.AddComponent<UnifiedInventoryUI>();
            
            // 메인 패널 생성
            GameObject mainPanel = CreateMainPanel(canvasObject);
            
            // 헤더 섹션 생성 (제목만)
            (Text titleText, Button closeButton) = CreateHeaderSection(mainPanel);
            
            // 컨텐츠 영역 생성 (장비창 + 인벤토리)
            GameObject contentArea = CreateContentArea(mainPanel);
            
            // 장비 패널 생성 (위쪽)
            GameObject equipmentPanel = CreateEquipmentPanel(contentArea);
            var equipmentSlots = CreateEquipmentSlots(equipmentPanel);
            
            // 인벤토리 패널 생성 (아래쪽)
            GameObject inventoryPanel = CreateInventoryPanel(contentArea);
            var (scrollView, inventoryGrid, inventoryContainer) = CreateInventoryScrollView(inventoryPanel);
            var inventorySlots = CreateInventorySlots(inventoryContainer);
            
            // 드래그 프리뷰 생성
            GameObject dragPreview = CreateDragPreview(canvasObject);
            
            // ItemTooltipManager 찾기
            ItemTooltipManager tooltipManager = Object.FindObjectOfType<ItemTooltipManager>();
            
            // UnifiedInventoryUI 참조 설정
            SetupUnifiedUIReferences(unifiedUI, mainPanel, titleText, closeButton, 
                equipmentPanel, inventoryPanel, scrollView, inventoryGrid, 
                dragPreview, tooltipManager, equipmentSlots, inventorySlots);
            
            // 프리팹 저장
            string prefabPath = "Assets/Resources/UI/UnifiedInventoryUI.prefab";
            PrefabUtility.SaveAsPrefabAsset(canvasObject, prefabPath);
            
            Debug.Log("✅ UnifiedInventoryUI 프리팹이 생성되었습니다: " + prefabPath);
            
            // 생성된 오브젝트 선택
            Selection.activeGameObject = canvasObject;
        }
        
        /// <summary>
        /// 메인 캔버스 생성
        /// </summary>
        private static GameObject CreateMainCanvas()
        {
            GameObject canvasObject = new GameObject("UnifiedInventoryCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            
            canvasObject.AddComponent<GraphicRaycaster>();
            
            return canvasObject;
        }
        
        /// <summary>
        /// 메인 패널 생성 (더 큰 사이즈)
        /// </summary>
        private static GameObject CreateMainPanel(GameObject parent)
        {
            GameObject mainPanel = new GameObject("MainPanel");
            mainPanel.transform.SetParent(parent.transform, false);
            
            RectTransform rect = mainPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(900f, 800f); // 더 큰 사이즈
            rect.anchoredPosition = Vector2.zero;
            
            // 배경
            Image background = mainPanel.AddComponent<Image>();
            background.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            
            // 테두리
            Outline outline = mainPanel.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(2f, 2f);
            
            return mainPanel;
        }
        
        /// <summary>
        /// 헤더 섹션 생성 (제목과 닫기 버튼만)
        /// </summary>
        private static (Text titleText, Button closeButton) CreateHeaderSection(GameObject parent)
        {
            GameObject headerPanel = new GameObject("HeaderPanel");
            headerPanel.transform.SetParent(parent.transform, false);
            
            RectTransform headerRect = headerPanel.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(0f, 60f);
            headerRect.anchoredPosition = Vector2.zero;
            
            // 헤더 배경
            Image headerBg = headerPanel.AddComponent<Image>();
            headerBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            
            // 제목 텍스트
            GameObject titleObject = new GameObject("Title");
            titleObject.transform.SetParent(headerPanel.transform, false);
            Text titleText = titleObject.AddComponent<Text>();
            titleText.text = "인벤토리 & 장비";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 24;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(60f, 0f);
            titleRect.offsetMax = new Vector2(-60f, 0f);
            
            // 닫기 버튼
            GameObject closeButtonObject = new GameObject("CloseButton");
            closeButtonObject.transform.SetParent(headerPanel.transform, false);
            Button closeButton = closeButtonObject.AddComponent<Button>();
            
            RectTransform closeRect = closeButtonObject.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.sizeDelta = new Vector2(50f, 50f);
            closeRect.anchoredPosition = new Vector2(-10f, 0f);
            
            Image closeButtonImage = closeButtonObject.AddComponent<Image>();
            closeButtonImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            
            // X 텍스트
            GameObject closeTextObject = new GameObject("CloseText");
            closeTextObject.transform.SetParent(closeButtonObject.transform, false);
            Text closeText = closeTextObject.AddComponent<Text>();
            closeText.text = "X";
            closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeText.fontSize = 20;
            closeText.color = Color.white;
            closeText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform closeTextRect = closeTextObject.GetComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;
            
            return (titleText, closeButton);
        }
        
        /// <summary>
        /// 컨텐츠 영역 생성 (장비창 + 인벤토리 영역)
        /// </summary>
        private static GameObject CreateContentArea(GameObject parent)
        {
            GameObject contentArea = new GameObject("ContentArea");
            contentArea.transform.SetParent(parent.transform, false);
            
            RectTransform contentRect = contentArea.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.offsetMin = new Vector2(10f, 10f);
            contentRect.offsetMax = new Vector2(-10f, -70f); // 헤더 공간 제외
            
            // 세로 레이아웃
            VerticalLayoutGroup verticalLayout = contentArea.AddComponent<VerticalLayoutGroup>();
            verticalLayout.spacing = 10f;
            verticalLayout.padding = new RectOffset(10, 10, 10, 10);
            verticalLayout.childControlHeight = false;
            verticalLayout.childControlWidth = true;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.childForceExpandWidth = true;
            
            return contentArea;
        }
        
        /// <summary>
        /// 장비 패널 생성 (위쪽, 고정 높이)
        /// </summary>
        private static GameObject CreateEquipmentPanel(GameObject parent)
        {
            GameObject equipmentPanel = new GameObject("EquipmentPanel");
            equipmentPanel.transform.SetParent(parent.transform, false);
            
            RectTransform equipmentRect = equipmentPanel.AddComponent<RectTransform>();
            equipmentRect.sizeDelta = new Vector2(0f, 320f); // 고정 높이
            
            // 배경
            Image equipmentBg = equipmentPanel.AddComponent<Image>();
            equipmentBg.color = new Color(0.05f, 0.05f, 0.15f, 0.8f);
            
            // 레이아웃
            LayoutElement layoutElement = equipmentPanel.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 320f;
            layoutElement.flexibleHeight = 0f;
            
            return equipmentPanel;
        }
        
        /// <summary>
        /// 인벤토리 패널 생성 (아래쪽, 유연한 높이)
        /// </summary>
        private static GameObject CreateInventoryPanel(GameObject parent)
        {
            GameObject inventoryPanel = new GameObject("InventoryPanel");
            inventoryPanel.transform.SetParent(parent.transform, false);
            
            RectTransform inventoryRect = inventoryPanel.AddComponent<RectTransform>();
            
            // 배경
            Image inventoryBg = inventoryPanel.AddComponent<Image>();
            inventoryBg.color = new Color(0.15f, 0.1f, 0.05f, 0.8f);
            
            // 레이아웃 (남은 공간 모두 사용)
            LayoutElement layoutElement = inventoryPanel.AddComponent<LayoutElement>();
            layoutElement.flexibleHeight = 1f;
            
            return inventoryPanel;
        }
        
        /// <summary>
        /// 장비 슬롯들 생성 (프리팹 기반, 캐릭터 주변 배치)
        /// </summary>
        private static EquipmentSlotUI[] CreateEquipmentSlots(GameObject parent)
        {
            var equipmentSlots = new System.Collections.Generic.List<EquipmentSlotUI>();
            
            // EquipmentSlot 프리팹 로드
            GameObject equipmentSlotPrefab = Resources.Load<GameObject>("UI/EquipmentSlot");
            if (equipmentSlotPrefab == null)
            {
                Debug.LogError("❌ EquipmentSlot 프리팹을 찾을 수 없습니다. 'Dungeon Crawler > Generate Equipment Slot UI Prefab'을 먼저 실행하세요.");
                return equipmentSlots.ToArray();
            }
            
            // 장비 슬롯 배치 정의 (14개 슬롯)
            var slotPositions = new (EquipmentSlot slot, Vector2 position)[]
            {
                (EquipmentSlot.Head, new Vector2(0f, 120f)),        // 머리 (중앙 위)
                (EquipmentSlot.Earring1, new Vector2(-80f, 100f)),  // 귀걸이1 (왼쪽 위)
                (EquipmentSlot.Earring2, new Vector2(80f, 100f)),   // 귀걸이2 (오른쪽 위)
                (EquipmentSlot.Necklace, new Vector2(0f, 80f)),     // 목걸이 (중앙)
                (EquipmentSlot.Chest, new Vector2(0f, 40f)),        // 상의 (중앙)
                (EquipmentSlot.Hands, new Vector2(-120f, 40f)),     // 장갑 (왼쪽)
                (EquipmentSlot.MainHand, new Vector2(120f, 40f)),   // 주무기 (오른쪽)
                (EquipmentSlot.Belt, new Vector2(0f, 0f)),          // 허리 (중앙)
                (EquipmentSlot.OffHand, new Vector2(-120f, 0f)),    // 보조무기 (왼쪽)
                (EquipmentSlot.TwoHand, new Vector2(120f, 0f)),     // 양손무기 (오른쪽)
                (EquipmentSlot.Legs, new Vector2(0f, -40f)),        // 하의 (중앙 아래)
                (EquipmentSlot.Ring1, new Vector2(-80f, -60f)),     // 반지1 (왼쪽 아래)
                (EquipmentSlot.Ring2, new Vector2(80f, -60f)),      // 반지2 (오른쪽 아래)
                (EquipmentSlot.Feet, new Vector2(0f, -80f))         // 신발 (중앙 맨아래)
            };
            
            foreach (var (slot, position) in slotPositions)
            {
                // 프리팹 인스턴스 생성
                GameObject slotObject = Object.Instantiate(equipmentSlotPrefab, parent.transform);
                slotObject.name = $"EquipmentSlot_{slot}";
                
                // 위치 설정
                RectTransform slotRect = slotObject.GetComponent<RectTransform>();
                slotRect.anchorMin = new Vector2(0.5f, 0.5f);
                slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                slotRect.pivot = new Vector2(0.5f, 0.5f);
                slotRect.anchoredPosition = position;
                
                // EquipmentSlotUI 컴포넌트 가져오기
                EquipmentSlotUI slotUI = slotObject.GetComponent<EquipmentSlotUI>();
                if (slotUI != null)
                {
                    equipmentSlots.Add(slotUI);
                    
                    // 슬롯 라벨 업데이트
                    Transform labelTransform = slotObject.transform.Find("SlotLabel");
                    if (labelTransform != null)
                    {
                        Text labelText = labelTransform.GetComponent<Text>();
                        if (labelText != null)
                        {
                            labelText.text = GetSlotDisplayName(slot);
                        }
                    }
                }
                else
                {
                    Debug.LogError($"❌ {slotObject.name}에서 EquipmentSlotUI 컴포넌트를 찾을 수 없습니다.");
                }
            }
            
            Debug.Log($"📦 Equipment slots created from prefab: {equipmentSlots.Count}");
            return equipmentSlots.ToArray();
        }
        
        /// <summary>
        /// 슬롯 표시명 가져오기
        /// </summary>
        private static string GetSlotDisplayName(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.Head => "머리",
                EquipmentSlot.Chest => "상의",
                EquipmentSlot.Legs => "하의",
                EquipmentSlot.Feet => "신발",
                EquipmentSlot.Hands => "장갑",
                EquipmentSlot.Belt => "허리",
                EquipmentSlot.MainHand => "주무기",
                EquipmentSlot.OffHand => "보조",
                EquipmentSlot.TwoHand => "양손무기",
                EquipmentSlot.Ring1 => "반지1",
                EquipmentSlot.Ring2 => "반지2",
                EquipmentSlot.Necklace => "목걸이",
                EquipmentSlot.Earring1 => "귀걸이1",
                EquipmentSlot.Earring2 => "귀걸이2",
                _ => ""
            };
        }
        
        /// <summary>
        /// 인벤토리 스크롤 뷰 생성
        /// </summary>
        private static (GameObject scrollView, GameObject inventoryGrid, GameObject inventoryContainer) CreateInventoryScrollView(GameObject parent)
        {
            // 스크롤 뷰
            GameObject scrollView = new GameObject("InventoryScrollView");
            scrollView.transform.SetParent(parent.transform, false);
            
            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(10f, 10f);
            scrollRect.offsetMax = new Vector2(-10f, -10f);
            
            ScrollRect scrollComponent = scrollView.AddComponent<ScrollRect>();
            scrollComponent.horizontal = false;
            scrollComponent.vertical = true;
            scrollComponent.movementType = ScrollRect.MovementType.Clamped;
            scrollComponent.scrollSensitivity = 20f;
            
            // 뷰포트
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = Color.clear;
            
            // 컨텐츠
            GameObject inventoryGrid = new GameObject("InventoryGrid");
            inventoryGrid.transform.SetParent(viewport.transform, false);
            
            RectTransform gridRect = inventoryGrid.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0f, 1f);
            gridRect.anchorMax = new Vector2(1f, 1f);
            gridRect.pivot = new Vector2(0.5f, 1f);
            gridRect.anchoredPosition = Vector2.zero;
            
            // 그리드 레이아웃
            GridLayoutGroup gridLayout = inventoryGrid.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(60f, 60f);
            gridLayout.spacing = new Vector2(5f, 5f);
            gridLayout.padding = new RectOffset(10, 10, 10, 10);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 10; // 10열
            
            // 컨텐츠 사이즈 피터
            ContentSizeFitter sizeFitter = inventoryGrid.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // 스크롤바 (세로)
            GameObject scrollbar = new GameObject("Scrollbar Vertical");
            scrollbar.transform.SetParent(scrollView.transform, false);
            
            RectTransform scrollbarRect = scrollbar.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.sizeDelta = new Vector2(20f, 0f);
            scrollbarRect.anchoredPosition = Vector2.zero;
            
            Scrollbar scrollbarComponent = scrollbar.AddComponent<Scrollbar>();
            scrollbarComponent.direction = Scrollbar.Direction.BottomToTop;
            
            Image scrollbarBg = scrollbar.AddComponent<Image>();
            scrollbarBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // 스크롤바 핸들
            GameObject handle = new GameObject("Sliding Area");
            handle.transform.SetParent(scrollbar.transform, false);
            
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = new Vector2(5f, 5f);
            handleRect.offsetMax = new Vector2(-5f, -5f);
            
            GameObject handleChild = new GameObject("Handle");
            handleChild.transform.SetParent(handle.transform, false);
            
            RectTransform handleChildRect = handleChild.AddComponent<RectTransform>();
            handleChildRect.anchorMin = Vector2.zero;
            handleChildRect.anchorMax = Vector2.one;
            handleChildRect.offsetMin = Vector2.zero;
            handleChildRect.offsetMax = Vector2.zero;
            
            Image handleImage = handleChild.AddComponent<Image>();
            handleImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            
            scrollbarComponent.targetGraphic = handleImage;
            scrollbarComponent.handleRect = handleChildRect;
            
            // ScrollRect 설정
            scrollComponent.content = gridRect;
            scrollComponent.viewport = viewportRect;
            scrollComponent.verticalScrollbar = scrollbarComponent;
            scrollComponent.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            
            return (scrollView, inventoryGrid, inventoryGrid);
        }
        
        /// <summary>
        /// 인벤토리 슬롯들 생성 (프리팹 기반)
        /// </summary>
        private static InventorySlotUI[] CreateInventorySlots(GameObject parent)
        {
            var inventorySlots = new System.Collections.Generic.List<InventorySlotUI>();
            
            // InventorySlot 프리팹 로드
            GameObject inventorySlotPrefab = Resources.Load<GameObject>("UI/InventorySlot");
            if (inventorySlotPrefab == null)
            {
                Debug.LogError("❌ InventorySlot 프리팹을 찾을 수 없습니다. Resources/UI/InventorySlot.prefab이 존재하는지 확인하세요.");
                return inventorySlots.ToArray();
            }
            
            for (int i = 0; i < 30; i++) // 30개 슬롯
            {
                // 프리팹 인스턴스 생성
                GameObject slotObject = Object.Instantiate(inventorySlotPrefab, parent.transform);
                slotObject.name = $"InventorySlot_{i}";
                
                // InventorySlotUI 컴포넌트 가져오기
                InventorySlotUI slotUI = slotObject.GetComponent<InventorySlotUI>();
                if (slotUI != null)
                {
                    inventorySlots.Add(slotUI);
                }
                else
                {
                    Debug.LogError($"❌ {slotObject.name}에서 InventorySlotUI 컴포넌트를 찾을 수 없습니다.");
                }
            }
            
            Debug.Log($"🎒 Inventory slots created from prefab: {inventorySlots.Count}");
            return inventorySlots.ToArray();
        }
        
        /// <summary>
        /// 드래그 프리뷰 생성
        /// </summary>
        private static GameObject CreateDragPreview(GameObject parent)
        {
            GameObject dragPreview = new GameObject("DragPreview");
            dragPreview.transform.SetParent(parent.transform, false);
            
            RectTransform previewRect = dragPreview.AddComponent<RectTransform>();
            previewRect.sizeDelta = new Vector2(60f, 60f);
            
            Image previewImage = dragPreview.AddComponent<Image>();
            previewImage.color = new Color(1f, 1f, 1f, 0.7f);
            previewImage.raycastTarget = false;
            
            CanvasGroup canvasGroup = dragPreview.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0.7f;
            canvasGroup.blocksRaycasts = false;
            
            dragPreview.SetActive(false);
            
            return dragPreview;
        }
        
        /// <summary>
        /// UnifiedInventoryUI 컴포넌트 참조 설정
        /// </summary>
        private static void SetupUnifiedUIReferences(UnifiedInventoryUI unifiedUI, GameObject mainPanel,
            Text titleText, Button closeButton, GameObject equipmentPanel, GameObject inventoryPanel,
            GameObject scrollView, GameObject inventoryGrid, GameObject dragPreview, 
            ItemTooltipManager tooltipManager, EquipmentSlotUI[] equipmentSlots, InventorySlotUI[] inventorySlots)
        {
            var serializedUI = new UnityEditor.SerializedObject(unifiedUI);
            
            // 기본 UI 요소
            serializedUI.FindProperty("mainPanel").objectReferenceValue = mainPanel;
            serializedUI.FindProperty("titleText").objectReferenceValue = titleText;
            serializedUI.FindProperty("closeButton").objectReferenceValue = closeButton;
            
            // 패널들
            serializedUI.FindProperty("equipmentPanel").objectReferenceValue = equipmentPanel;
            serializedUI.FindProperty("inventoryPanel").objectReferenceValue = inventoryPanel;
            serializedUI.FindProperty("inventoryScrollView").objectReferenceValue = scrollView;
            serializedUI.FindProperty("inventoryGrid").objectReferenceValue = inventoryGrid;
            
            // 드래그 & 툴팁
            serializedUI.FindProperty("dragPreview").objectReferenceValue = dragPreview;
            serializedUI.FindProperty("dragPreviewImage").objectReferenceValue = dragPreview.GetComponent<Image>();
            serializedUI.FindProperty("tooltipManager").objectReferenceValue = tooltipManager;
            
            // 장비 슬롯 배열
            var equipmentSlotsProperty = serializedUI.FindProperty("equipmentSlots");
            equipmentSlotsProperty.arraySize = equipmentSlots.Length;
            for (int i = 0; i < equipmentSlots.Length; i++)
            {
                equipmentSlotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = equipmentSlots[i];
            }
            
            // 인벤토리 슬롯 배열
            var inventorySlotsProperty = serializedUI.FindProperty("inventorySlots");
            inventorySlotsProperty.arraySize = inventorySlots.Length;
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                inventorySlotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = inventorySlots[i];
            }
            
            serializedUI.ApplyModifiedProperties();
            
            Debug.Log($"📋 UnifiedInventoryUI 참조 설정 완료: 장비슬롯 {equipmentSlots.Length}개, 인벤토리슬롯 {inventorySlots.Length}개");
        }
    }
}