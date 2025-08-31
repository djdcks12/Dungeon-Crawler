using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// UI 프리팹 자동 생성 에디터 도구
    /// 완전한 UI 시스템을 자동으로 생성하는 고급 도구
    /// </summary>
    public class UIPrefabGenerator : EditorWindow
    {
        [Header("Generation Settings")]
        private string prefabSavePath = "Assets/Prefabs/UI/";
        private bool createCanvas = true;
        private bool addEventSystem = true;
        
        [Header("Style Settings")]
        private Font defaultFont;
        private Color defaultTextColor = Color.white;
        private Color defaultBackgroundColor = new Color(0, 0, 0, 0.8f);
        private Color defaultButtonColor = new Color(0.2f, 0.3f, 0.5f, 1f);
        
        [Header("UI Generation Options")]
        private bool generateSkillUI = true;
        private bool generateChatUI = true;
        private bool generateSettingsUI = true;
        private bool generateMinimapUI = true;
        private bool generateInventoryUI = true;
        
        private Vector2 scrollPosition;
        
        [MenuItem("Tools/UI/Generate UI Prefabs")]
        public static void ShowWindow()
        {
            GetWindow<UIPrefabGenerator>("UI Prefab Generator");
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            GUILayout.Label("Advanced UI Prefab Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            DrawGeneralSettings();
            GUILayout.Space(10);
            
            DrawGenerationOptions();
            GUILayout.Space(10);
            
            DrawStyleSettings();
            GUILayout.Space(10);
            
            DrawGenerationButtons();
            
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// 일반 설정 UI
        /// </summary>
        private void DrawGeneralSettings()
        {
            GUILayout.Label("General Settings", EditorStyles.boldLabel);
            
            prefabSavePath = EditorGUILayout.TextField("Prefab Save Path", prefabSavePath);
            createCanvas = EditorGUILayout.Toggle("Create Canvas", createCanvas);
            addEventSystem = EditorGUILayout.Toggle("Add Event System", addEventSystem);
        }
        
        /// <summary>
        /// 생성 옵션 UI
        /// </summary>
        private void DrawGenerationOptions()
        {
            GUILayout.Label("UI Generation Options", EditorStyles.boldLabel);
            
            generateSkillUI = EditorGUILayout.Toggle("Generate Skill UI", generateSkillUI);
            generateChatUI = EditorGUILayout.Toggle("Generate Chat UI", generateChatUI);
            generateSettingsUI = EditorGUILayout.Toggle("Generate Settings UI", generateSettingsUI);
            generateMinimapUI = EditorGUILayout.Toggle("Generate Minimap UI", generateMinimapUI);
            generateInventoryUI = EditorGUILayout.Toggle("Generate Inventory UI", generateInventoryUI);
        }
        
        /// <summary>
        /// 스타일 설정 UI
        /// </summary>
        private void DrawStyleSettings()
        {
            GUILayout.Label("Style Settings", EditorStyles.boldLabel);
            
            defaultFont = (Font)EditorGUILayout.ObjectField("Default Font", defaultFont, typeof(Font), false);
            defaultTextColor = EditorGUILayout.ColorField("Default Text Color", defaultTextColor);
            defaultBackgroundColor = EditorGUILayout.ColorField("Default Background Color", defaultBackgroundColor);
            defaultButtonColor = EditorGUILayout.ColorField("Default Button Color", defaultButtonColor);
        }
        
        /// <summary>
        /// 생성 버튼 UI
        /// </summary>
        private void DrawGenerationButtons()
        {
            GUILayout.Label("Generate Prefabs", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Generate All Selected", GUILayout.Height(30)))
            {
                GenerateAllSelectedPrefabs();
            }
            
            if (GUILayout.Button("Clear Generated Prefabs", GUILayout.Height(30)))
            {
                ClearGeneratedPrefabs();
            }
            
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // 기존 프리팹들
            GUILayout.Label("Legacy Prefabs", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Generate PlayerHUD Prefab"))
            {
                GeneratePlayerHUDPrefab();
            }
            
            if (GUILayout.Button("Generate StatsUI Prefab"))
            {
                GenerateStatsUIPrefab();
            }
            
            if (GUILayout.Button("Generate All Legacy Prefabs"))
            {
                GenerateAllPrefabs();
            }
            
            GUILayout.Space(10);
            
            // 개별 생성 버튼들
            GUILayout.Label("Individual Advanced Generation", EditorStyles.boldLabel);
            
            if (generateSkillUI && GUILayout.Button("Generate Advanced Skill UI"))
                GenerateAdvancedSkillUIPrefab();
                
            if (generateChatUI && GUILayout.Button("Generate Advanced Chat UI"))
                GenerateAdvancedChatUIPrefab();
                
            if (generateMinimapUI && GUILayout.Button("Generate Advanced Minimap UI"))
                GenerateAdvancedMinimapUIPrefab();
                
            if (generateInventoryUI && GUILayout.Button("Generate Advanced Inventory UI"))
            {
                // GUI 상태를 유지하기 위해 지연 실행
                EditorApplication.delayCall += GenerateAdvancedInventoryUIPrefab;
            }
        }
        
        private void GenerateAllPrefabs()
        {
            GeneratePlayerHUDPrefab();
            GenerateStatsUIPrefab();
            
            Debug.Log("✅ All UI prefabs generated!");
        }
        
        private void GeneratePlayerHUDPrefab()
        {
            // 기본 구조 생성
            GameObject hudRoot = new GameObject("PlayerHUD");
            hudRoot.AddComponent<PlayerHUD>();
            
            // MainHUDPanel 생성
            GameObject mainPanel = CreateUIPanel("MainHUDPanel", hudRoot.transform);
            
            // HealthPanel 생성
            GameObject healthPanel = CreateUIPanel("HealthPanel", mainPanel.transform);
            GameObject healthSlider = CreateSlider("HealthSlider", healthPanel.transform, Color.green);
            GameObject healthText = CreateText("HealthText", healthPanel.transform, "100 / 100");
            
            // ManaPanel 생성
            GameObject manaPanel = CreateUIPanel("ManaPanel", mainPanel.transform);
            GameObject manaSlider = CreateSlider("ManaSlider", manaPanel.transform, Color.blue);
            GameObject manaText = CreateText("ManaText", manaPanel.transform, "50 / 50");
            
            // ExperiencePanel 생성
            GameObject expPanel = CreateUIPanel("ExperiencePanel", mainPanel.transform);
            GameObject expSlider = CreateSlider("ExperienceSlider", expPanel.transform, Color.yellow);
            GameObject levelText = CreateText("LevelText", expPanel.transform, "Lv.1");
            GameObject expText = CreateText("ExpText", expPanel.transform, "0 / 100");
            
            // ResourcePanel 생성
            GameObject resourcePanel = CreateUIPanel("ResourcePanel", mainPanel.transform);
            GameObject goldText = CreateText("GoldText", resourcePanel.transform, "1000");
            GameObject raceText = CreateText("RaceText", resourcePanel.transform, "인간");
            
            // StatusEffectsParent 생성
            GameObject statusParent = new GameObject("StatusEffectsParent");
            statusParent.transform.SetParent(mainPanel.transform);
            
            // PlayerHUD 컴포넌트 연결
            PlayerHUD hudComponent = hudRoot.GetComponent<PlayerHUD>();
            ConnectPlayerHUDComponents(hudComponent, mainPanel, healthPanel, resourcePanel,
                healthSlider, manaSlider, healthText, manaText, expSlider, levelText, 
                expText, goldText, null, raceText, statusParent);
            
            // 프리팹 저장
            SavePrefab(hudRoot, "UI/PlayerHUD");
            
            Debug.Log("✅ PlayerHUD prefab generated!");
        }
        
        private void GenerateStatsUIPrefab()
        {
            try
            {
                Debug.Log("🔧 Starting Advanced StatsUI generation...");
                
                var rootCanvas = CreateAdvancedCanvas("AdvancedStatsUI_Canvas");
                
                // 스탯 패널 - 화면 우측에 세로로 긴 형태
                var statsPanel = CreateAdvancedUIPanel(rootCanvas.transform, "StatsPanel");
                var statsRect = statsPanel.GetComponent<RectTransform>();
                statsRect.anchorMin = new Vector2(0.7f, 0.1f); // 화면의 70% 지점부터
                statsRect.anchorMax = new Vector2(0.98f, 0.9f); // 화면의 98% 지점까지
                statsRect.offsetMin = Vector2.zero;
                statsRect.offsetMax = Vector2.zero;
                
                statsPanel.SetActive(false); // 기본적으로 숨김
                
                Debug.Log($"🔍 StatsPanel created with size: {statsRect.sizeDelta}");
                
                // StatsUI 스크립트 추가
                var statsUIScript = rootCanvas.gameObject.AddComponent<StatsUI>();
                
                // 토글 버튼 (UI 외부, 화면 우측 상단)
                var toggleButton = CreateAdvancedButton(rootCanvas.transform, "ToggleStatsButton", "C");
                SetRectTransform(toggleButton.gameObject, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, -60), new Vector2(-10, -10));
                
                // 헤더
                var header = CreateAdvancedUIPanel(statsPanel.transform, "Header");
                SetRectTransform(header, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -50), new Vector2(0, 0));
                
                var headerText = CreateAdvancedText(header.transform, "HeaderText", "Character Stats", 18);
                SetRectTransform(headerText.gameObject, new Vector2(0, 0), new Vector2(0.8f, 1), Vector2.zero, Vector2.zero);
                
                var closeButton = CreateAdvancedButton(header.transform, "CloseStatsButton", "✕");
                SetRectTransform(closeButton.gameObject, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-40, 0), new Vector2(-5, 0));
                
                // 스크롤 뷰를 위한 콘텐츠 영역
                var scrollView = new GameObject("ScrollView");
                scrollView.transform.SetParent(statsPanel.transform);
                scrollView.AddComponent<RectTransform>();
                SetRectTransform(scrollView, new Vector2(0, 0), new Vector2(1, 1), new Vector2(5, 5), new Vector2(-5, -55));
                
                var scrollRect = scrollView.AddComponent<ScrollRect>();
                scrollRect.vertical = true;
                scrollRect.horizontal = false;
                
                // Content 영역
                var content = CreateAdvancedUIPanel(scrollView.transform, "Content");
                var contentLayout = content.AddComponent<VerticalLayoutGroup>();
                contentLayout.spacing = 15;
                contentLayout.padding = new RectOffset(10, 10, 10, 10);
                contentLayout.childControlHeight = false;
                contentLayout.childForceExpandHeight = false;
                
                var contentFitter = content.AddComponent<ContentSizeFitter>();
                contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                
                scrollRect.content = content.GetComponent<RectTransform>();
                
                // Player Info Section
                var playerInfoSection = CreateStatsSection(content.transform, "PlayerInfoSection", "플레이어 정보");
                var playerNameText = CreateAdvancedText(playerInfoSection.transform, "PlayerNameText", "Player_12345", 16);
                var levelText = CreateAdvancedText(playerInfoSection.transform, "LevelText", "Level 1", 14);
                var expText = CreateAdvancedText(playerInfoSection.transform, "ExpText", "EXP: 0 / 100", 12);
                var expSlider = CreateAdvancedSlider(playerInfoSection.transform, "ExpSlider");
                var availablePointsText = CreateAdvancedText(playerInfoSection.transform, "AvailablePointsText", "Race: Human (Auto Growth)", 12);
                
                // Health & Mana Section
                var healthManaSection = CreateStatsSection(content.transform, "HealthManaSection", "체력 & 마나");
                var healthSlider = CreateAdvancedSlider(healthManaSection.transform, "HealthSlider");
                var healthText = CreateAdvancedText(healthManaSection.transform, "HealthText", "HP: 100 / 100", 12);
                var manaSlider = CreateAdvancedSlider(healthManaSection.transform, "ManaSlider");
                var manaText = CreateAdvancedText(healthManaSection.transform, "ManaText", "MP: 50 / 50", 12);
                
                // Primary Stats Section
                var primaryStatsSection = CreateStatsSection(content.transform, "PrimaryStatsSection", "기본 능력치");
                var strStat = CreateStatUIElement(primaryStatsSection.transform, "StrStat", "힘 (STR)");
                var agiStat = CreateStatUIElement(primaryStatsSection.transform, "AgiStat", "민첩 (AGI)");
                var vitStat = CreateStatUIElement(primaryStatsSection.transform, "VitStat", "체력 (VIT)");
                var intStat = CreateStatUIElement(primaryStatsSection.transform, "IntStat", "지능 (INT)");
                var defStat = CreateStatUIElement(primaryStatsSection.transform, "DefStat", "물리방어 (DEF)");
                var mdefStat = CreateStatUIElement(primaryStatsSection.transform, "MdefStat", "마법방어 (MDEF)");
                var lukStat = CreateStatUIElement(primaryStatsSection.transform, "LukStat", "운 (LUK)");
                
                // Derived Stats Section
                var derivedStatsSection = CreateStatsSection(content.transform, "DerivedStatsSection", "파생 능력치");
                var attackDamageText = CreateAdvancedText(derivedStatsSection.transform, "AttackDamageText", "Attack: 10.0", 12);
                var magicDamageText = CreateAdvancedText(derivedStatsSection.transform, "MagicDamageText", "Magic: 5.0", 12);
                var moveSpeedText = CreateAdvancedText(derivedStatsSection.transform, "MoveSpeedText", "Speed: 5.0", 12);
                var attackSpeedText = CreateAdvancedText(derivedStatsSection.transform, "AttackSpeedText", "AS: 1.00", 12);
                var critChanceText = CreateAdvancedText(derivedStatsSection.transform, "CritChanceText", "Crit: 5.0%", 12);
                var critDamageText = CreateAdvancedText(derivedStatsSection.transform, "CritDamageText", "Crit DMG: 150%", 12);
                
                // StatsUI 스크립트에 UI 요소들 연결
                ConnectStatsUIReferences(statsUIScript, statsPanel, toggleButton, closeButton, 
                    playerNameText, levelText, expText, expSlider.GetComponent<Slider>(), availablePointsText,
                    healthSlider.GetComponent<Slider>(), healthText, manaSlider.GetComponent<Slider>(), manaText,
                    strStat, agiStat, vitStat, intStat, defStat, mdefStat, lukStat,
                    attackDamageText, magicDamageText, moveSpeedText, attackSpeedText, critChanceText, critDamageText);
                
                SaveAdvancedPrefab(rootCanvas.gameObject, "AdvancedStatsUI");
                
                Debug.Log("✅ Advanced StatsUI generation completed successfully!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Error generating Advanced StatsUI: {ex.Message}");
                Debug.LogError($"Stack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 선택된 모든 고급 UI 프리팹 생성
        /// </summary>
        private void GenerateAllSelectedPrefabs()
        {
            if (!Directory.Exists(prefabSavePath))
            {
                Directory.CreateDirectory(prefabSavePath);
            }
            
            int generatedCount = 0;
            
            try
            {
                if (generateSkillUI)
                {
                    GenerateAdvancedSkillUIPrefab();
                    generatedCount++;
                }
                
                if (generateChatUI)
                {
                    GenerateAdvancedChatUIPrefab();
                    generatedCount++;
                }
                
                if (generateMinimapUI)
                {
                    GenerateAdvancedMinimapUIPrefab();
                    generatedCount++;
                }
                
                if (generateInventoryUI)
                {
                    GenerateAdvancedInventoryUIPrefab();
                    generatedCount++;
                }
                
                AssetDatabase.Refresh();
                
                Debug.Log($"Successfully generated {generatedCount} advanced UI prefabs!");
                EditorUtility.DisplayDialog("Success", $"Generated {generatedCount} advanced UI prefabs successfully!", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to generate UI prefabs: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate prefabs: {e.Message}", "OK");
            }
        }
        
        /// <summary>
        /// 고급 스킬 UI 프리팹 생성
        /// </summary>
        private void GenerateAdvancedSkillUIPrefab()
        {
            var rootCanvas = CreateAdvancedCanvas("AdvancedSkillUI_Canvas");
            var skillPanel = CreateAdvancedUIPanel(rootCanvas.transform, "SkillPanel");
            skillPanel.SetActive(false); // 기본적으로 숨김
            
            // 스킬 UI 컴포넌트 추가
            var skillUI = skillPanel.AddComponent<SkillUI>();
            
            // 헤더 영역
            var header = CreateAdvancedUIPanel(skillPanel.transform, "Header");
            SetRectTransform(header, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -60), new Vector2(0, 0));
            
            var headerText = CreateAdvancedText(header.transform, "HeaderText", "Skills", 24);
            SetRectTransform(headerText.gameObject, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            
            var closeButton = CreateAdvancedButton(header.transform, "CloseButton", "✕");
            SetRectTransform(closeButton.gameObject, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-50, 0), new Vector2(-10, 0));
            
            // 카테고리 탭 영역
            var categoryContainer = CreateAdvancedUIPanel(skillPanel.transform, "CategoryContainer");
            SetRectTransform(categoryContainer, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -120), new Vector2(0, -60));
            
            var categoryLayoutGroup = categoryContainer.AddComponent<HorizontalLayoutGroup>();
            categoryLayoutGroup.spacing = 5;
            categoryLayoutGroup.padding = new RectOffset(10, 10, 5, 5);
            
            string[] categories = { "All", "Warrior", "Archer", "Mage", "Support" };
            for (int i = 0; i < categories.Length; i++)
            {
                var categoryButton = CreateAdvancedButton(categoryContainer.transform, $"Category{categories[i]}Button", categories[i]);
                var layoutElement = categoryButton.gameObject.AddComponent<LayoutElement>();
                layoutElement.flexibleWidth = 1;
            }
            
            // 스킬 콘텐츠 영역
            var contentArea = CreateAdvancedUIPanel(skillPanel.transform, "ContentArea");
            SetRectTransform(contentArea, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, -120));
            
            // 스킬 슬롯 스크롤 영역
            var skillScrollRect = CreateAdvancedScrollRect(contentArea.transform, "SkillScrollRect");
            SetRectTransform(skillScrollRect.gameObject, new Vector2(0, 0.3f), new Vector2(0.7f, 1), Vector2.zero, Vector2.zero);
            
            var skillSlotContainer = skillScrollRect.content;
            var gridLayout = skillSlotContainer.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(80, 80);
            gridLayout.spacing = new Vector2(5, 5);
            gridLayout.constraintCount = 4;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            
            // 스킬 정보 패널
            var skillInfoPanel = CreateAdvancedUIPanel(contentArea.transform, "SkillInfoPanel");
            SetRectTransform(skillInfoPanel, new Vector2(0.7f, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            
            var infoLayout = skillInfoPanel.AddComponent<VerticalLayoutGroup>();
            infoLayout.spacing = 10;
            infoLayout.padding = new RectOffset(10, 10, 10, 10);
            infoLayout.childControlHeight = false;
            infoLayout.childForceExpandHeight = false;
            
            CreateAdvancedText(skillInfoPanel.transform, "SkillNameText", "Select a Skill", 20);
            CreateAdvancedText(skillInfoPanel.transform, "SkillDescriptionText", "Click on a skill to see details", 14);
            CreateAdvancedButton(skillInfoPanel.transform, "LearnSkillButton", "Learn Skill");
            CreateAdvancedButton(skillInfoPanel.transform, "UpgradeSkillButton", "Upgrade Skill");
            
            // 스킬 슬롯 프리팹 생성
            CreateAdvancedSkillSlotPrefab();
            
            SaveAdvancedPrefab(rootCanvas.gameObject, "AdvancedSkillUI");
        }
        
        /// <summary>
        /// 고급 채팅 UI 프리팹 생성
        /// </summary>
        private void GenerateAdvancedChatUIPrefab()
        {
            var rootCanvas = CreateAdvancedCanvas("AdvancedChatUI_Canvas");
            var chatPanel = CreateAdvancedUIPanel(rootCanvas.transform, "ChatPanel");
            
            // 채팅 UI 컴포넌트 추가
            var chatUI = chatPanel.AddComponent<ChatUI>();
            
            // 채널 탭 영역
            var channelContainer = CreateAdvancedUIPanel(chatPanel.transform, "ChannelContainer");
            SetRectTransform(channelContainer, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -40), new Vector2(0, 0));
            
            var channelLayoutGroup = channelContainer.AddComponent<HorizontalLayoutGroup>();
            channelLayoutGroup.spacing = 2;
            channelLayoutGroup.padding = new RectOffset(5, 5, 5, 5);
            
            string[] channels = { "All", "Party", "System", "Whisper" };
            for (int i = 0; i < channels.Length; i++)
            {
                var channelButton = CreateAdvancedButton(channelContainer.transform, $"Channel{channels[i]}Button", channels[i]);
                var layoutElement = channelButton.gameObject.AddComponent<LayoutElement>();
                layoutElement.flexibleWidth = 1;
            }
            
            // 메시지 스크롤 영역
            var messageScrollRect = CreateAdvancedScrollRect(chatPanel.transform, "MessageScrollRect");
            SetRectTransform(messageScrollRect.gameObject, new Vector2(0, 0.15f), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, -40));
            
            var messageContainer = messageScrollRect.content;
            var verticalLayout = messageContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayout.spacing = 2;
            verticalLayout.padding = new RectOffset(5, 5, 5, 5);
            verticalLayout.childControlHeight = false;
            verticalLayout.childForceExpandHeight = false;
            
            // 입력 영역
            var inputContainer = CreateAdvancedUIPanel(chatPanel.transform, "InputContainer");
            SetRectTransform(inputContainer, new Vector2(0, 0), new Vector2(1, 0.15f), Vector2.zero, Vector2.zero);
            
            var inputLayoutGroup = inputContainer.AddComponent<HorizontalLayoutGroup>();
            inputLayoutGroup.spacing = 5;
            inputLayoutGroup.padding = new RectOffset(5, 5, 5, 5);
            
            var inputField = CreateAdvancedInputField(inputContainer.transform, "MessageInputField", "Enter message...");
            var inputLayoutElement = inputField.gameObject.AddComponent<LayoutElement>();
            inputLayoutElement.flexibleWidth = 1;
            
            var sendButton = CreateAdvancedButton(inputContainer.transform, "SendButton", "Send");
            var sendLayoutElement = sendButton.gameObject.AddComponent<LayoutElement>();
            sendLayoutElement.minWidth = 60;
            
            // 채팅 메시지 프리팹 생성
            CreateAdvancedChatMessagePrefab();
            
            SaveAdvancedPrefab(rootCanvas.gameObject, "AdvancedChatUI");
        }
        
        /// <summary>
        /// 고급 미니맵 UI 프리팹 생성
        /// </summary>
        private void GenerateAdvancedMinimapUIPrefab()
        {
            var rootCanvas = CreateAdvancedCanvas("AdvancedMinimapUI_Canvas");
            var minimapPanel = CreateAdvancedUIPanel(rootCanvas.transform, "MinimapPanel");
            
            // 미니맵 UI 컴포넌트 추가
            var minimapUI = minimapPanel.AddComponent<MinimapUI>();
            
            // 미니맵 컨테이너 (원형 마스크)
            var minimapContainer = CreateAdvancedUIPanel(minimapPanel.transform, "MinimapContainer");
            SetRectTransform(minimapContainer, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-220, -220), new Vector2(-20, -20));
            
            var mask = minimapContainer.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            
            // 미니맵 이미지
            var minimapImage = CreateAdvancedRawImage(minimapContainer.transform, "MinimapImage");
            SetRectTransform(minimapImage.gameObject, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            
            // 아이콘 컨테이너
            var iconContainer = CreateAdvancedUIPanel(minimapImage.transform, "IconContainer");
            SetRectTransform(iconContainer, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            
            // 컨트롤 영역
            var controlContainer = CreateAdvancedUIPanel(minimapPanel.transform, "ControlContainer");
            SetRectTransform(controlContainer, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-220, -260), new Vector2(-20, -220));
            
            var controlLayoutGroup = controlContainer.AddComponent<HorizontalLayoutGroup>();
            controlLayoutGroup.spacing = 5;
            controlLayoutGroup.padding = new RectOffset(5, 5, 5, 5);
            
            var toggleButton = CreateAdvancedButton(controlContainer.transform, "ToggleButton", "M");
            var toggleLayoutElement = toggleButton.gameObject.AddComponent<LayoutElement>();
            toggleLayoutElement.minWidth = 30;
            
            var zoomSlider = CreateAdvancedSlider(controlContainer.transform, "ZoomSlider");
            var zoomLayoutElement = zoomSlider.gameObject.AddComponent<LayoutElement>();
            zoomLayoutElement.flexibleWidth = 1;
            
            // 미니맵 아이콘 프리팹들 생성
            CreateAdvancedMinimapIconPrefabs();
            
            SaveAdvancedPrefab(rootCanvas.gameObject, "AdvancedMinimapUI");
        }
        
        /// <summary>
        /// 고급 인벤토리 UI 프리팹 생성
        /// </summary>
        private void GenerateAdvancedInventoryUIPrefab()
        {
            try
            {
                Debug.Log("🔧 Starting AdvancedInventoryUI generation...");
                
                var rootCanvas = CreateAdvancedCanvas("AdvancedInventoryUI_Canvas");
                
                // 인벤토리 패널 - 화면 중앙에 적당한 크기로 설정
                var inventoryPanel = CreateAdvancedUIPanel(rootCanvas.transform, "InventoryPanel");
                var inventoryRect = inventoryPanel.GetComponent<RectTransform>();
                inventoryRect.anchorMin = new Vector2(0.2f, 0.1f); // 화면의 20% 지점부터
                inventoryRect.anchorMax = new Vector2(0.8f, 0.9f);  // 화면의 80% 지점까지
                inventoryRect.offsetMin = Vector2.zero;
                inventoryRect.offsetMax = Vector2.zero;
                
                inventoryPanel.SetActive(false); // 기본적으로 숨김
                
                Debug.Log($"🔍 InventoryPanel created with size: {inventoryRect.sizeDelta}");
                
                // 헤더
                var header = CreateAdvancedUIPanel(inventoryPanel.transform, "Header");
                SetRectTransform(header, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -50), new Vector2(0, 0));
            
            var headerText = CreateAdvancedText(header.transform, "HeaderText", "Inventory", 24);
            SetRectTransform(headerText.gameObject, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            
            var closeButton = CreateAdvancedButton(header.transform, "CloseButton", "✕");
            SetRectTransform(closeButton.gameObject, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-50, 0), new Vector2(-10, 0));
            
            // 콘텐츠 영역
            var contentArea = CreateAdvancedUIPanel(inventoryPanel.transform, "ContentArea");
            SetRectTransform(contentArea, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, -50));
            
            // 인벤토리 슬롯 그리드
            var slotsContainer = CreateAdvancedUIPanel(contentArea.transform, "SlotsContainer");
            SetRectTransform(slotsContainer, new Vector2(0, 0.3f), new Vector2(0.7f, 1), Vector2.zero, Vector2.zero);
            
            var gridLayout = slotsContainer.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(64, 64);
            gridLayout.spacing = new Vector2(2, 2);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 8;
            
            // 아이템 정보 패널
            var itemInfoPanel = CreateAdvancedUIPanel(contentArea.transform, "ItemInfoPanel");
            SetRectTransform(itemInfoPanel, new Vector2(0.7f, 0.3f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            
            var infoLayout = itemInfoPanel.AddComponent<VerticalLayoutGroup>();
            infoLayout.spacing = 10;
            infoLayout.padding = new RectOffset(10, 10, 10, 10);
            infoLayout.childControlHeight = false;
            infoLayout.childForceExpandHeight = false;
            
            CreateAdvancedText(itemInfoPanel.transform, "ItemNameText", "Select an Item", 18);
            CreateAdvancedText(itemInfoPanel.transform, "ItemDescriptionText", "Click on an item to see details", 14);
            CreateAdvancedText(itemInfoPanel.transform, "ItemStatsText", "", 12);
            
            // 필터/정렬 영역
            var filterContainer = CreateAdvancedUIPanel(contentArea.transform, "FilterContainer");
            SetRectTransform(filterContainer, new Vector2(0, 0), new Vector2(1, 0.3f), Vector2.zero, Vector2.zero);
            
            var filterLayoutGroup = filterContainer.AddComponent<HorizontalLayoutGroup>();
            filterLayoutGroup.spacing = 10;
            filterLayoutGroup.padding = new RectOffset(10, 10, 10, 10);
            
            var sortButton = CreateAdvancedButton(filterContainer.transform, "SortButton", "Sort");
            CreateAdvancedText(filterContainer.transform, "UsedSlotsText", "0/40", 14);
             
                SaveAdvancedPrefab(rootCanvas.gameObject, "AdvancedInventoryUI");
                
                // 인벤토리 슬롯 프리팹 생성 (AdvancedInventoryUI 저장 후)
                CreateInventorySlotPrefab();
                
                // AdvancedInventoryUI에 슬롯 프리팹 연결
                UpdateInventoryUISlotPrefab();
                
                Debug.Log("✅ AdvancedInventoryUI generation completed successfully!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Error generating AdvancedInventoryUI: {ex.Message}");
                Debug.LogError($"Stack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 인벤토리 슬롯 프리팹 생성
        /// </summary>
        private void CreateInventorySlotPrefab()
        {
            var slotObj = new GameObject("InventorySlot");
            var rectTransform = slotObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(64, 64);
            
            // 배경 이미지
            var backgroundImage = slotObj.AddComponent<Image>();
            backgroundImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            
            // 아이템 아이콘 (InventorySlotUI가 찾는 이름으로 변경)
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);
            iconObj.AddComponent<RectTransform>();
            var iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.one * 4; // 4픽셀 패딩
            iconRect.offsetMax = Vector2.one * -4;
            
            var itemIconImage = iconObj.AddComponent<Image>();
            itemIconImage.color = Color.white;
            
            // 등급 프레임 (선택사항)
            var gradeFrameObj = new GameObject("GradeFrame");
            gradeFrameObj.transform.SetParent(slotObj.transform, false);
            gradeFrameObj.AddComponent<RectTransform>();
            var gradeFrameRect = gradeFrameObj.GetComponent<RectTransform>();
            gradeFrameRect.anchorMin = Vector2.zero;
            gradeFrameRect.anchorMax = Vector2.one;
            gradeFrameRect.offsetMin = Vector2.zero;
            gradeFrameRect.offsetMax = Vector2.zero;
            var gradeFrameImage = gradeFrameObj.AddComponent<Image>();
            gradeFrameImage.color = Color.clear;
            gradeFrameImage.raycastTarget = false;
            
            // 하이라이트 이미지 (선택사항)
            var highlightObj = new GameObject("Highlight");
            highlightObj.transform.SetParent(slotObj.transform, false);
            highlightObj.AddComponent<RectTransform>();
            var highlightRect = highlightObj.GetComponent<RectTransform>();
            highlightRect.anchorMin = Vector2.zero;
            highlightRect.anchorMax = Vector2.one;
            highlightRect.offsetMin = Vector2.zero;
            highlightRect.offsetMax = Vector2.zero;
            var highlightImage = highlightObj.AddComponent<Image>();
            highlightImage.color = Color.clear;
            highlightImage.raycastTarget = false;
            
            // 수량 텍스트 (InventorySlotUI가 찾는 이름으로 변경)
            var countObj = new GameObject("Quantity");
            countObj.transform.SetParent(slotObj.transform, false);
            countObj.AddComponent<RectTransform>();
            var countRect = countObj.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(1, 0);
            countRect.anchorMax = new Vector2(1, 0);
            countRect.pivot = new Vector2(1, 0);
            countRect.anchoredPosition = new Vector2(-2, 2);
            countRect.sizeDelta = new Vector2(30, 20);
            
            var countText = countObj.AddComponent<Text>();
            countText.text = "";
            countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            countText.fontSize = 12;
            countText.color = Color.white;
            countText.alignment = TextAnchor.MiddleRight;
            
            // InventorySlotUI 스크립트 추가
            var slotUIScript = slotObj.AddComponent<InventorySlotUI>();
            
            // SerializedObject로 private 필드들 연결
            var serializedSlot = new SerializedObject(slotUIScript);
            serializedSlot.FindProperty("backgroundImage").objectReferenceValue = backgroundImage;
            serializedSlot.FindProperty("itemIconImage").objectReferenceValue = itemIconImage;
            serializedSlot.FindProperty("quantityText").objectReferenceValue = countText;
            serializedSlot.FindProperty("gradeFrame").objectReferenceValue = gradeFrameImage;
            serializedSlot.FindProperty("highlightImage").objectReferenceValue = highlightImage;
            serializedSlot.ApplyModifiedProperties();
            
            SaveAdvancedPrefab(slotObj, "InventorySlot");
            Debug.Log("✅ InventorySlot prefab created successfully!");
        }
        
        /// <summary>
        /// AdvancedInventoryUI 프리팹에 슬롯 프리팹 연결
        /// </summary>
        private void UpdateInventoryUISlotPrefab()
        {
            // 생성된 프리팹들을 로드
            GameObject inventoryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/AdvancedInventoryUI.prefab");
            GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/InventorySlot.prefab");
            
            if (inventoryPrefab == null || slotPrefab == null)
            {
                Debug.LogError("❌ Could not find generated prefabs to connect!");
                return;
            }
            
        }
        
        // 고급 UI 생성 헬퍼 메서드들
        
        /// <summary>
        /// 고급 캔버스 생성
        /// </summary>
        private Canvas CreateAdvancedCanvas(string name)
        {
            var canvasObj = new GameObject(name);
            var canvas = canvasObj.AddComponent<Canvas>();
            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            var graphicRaycaster = canvasObj.AddComponent<GraphicRaycaster>();
            
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            
            if (addEventSystem && FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            
            return canvas;
        }
        
        /// <summary>
        /// 고급 UI 패널 생성
        /// </summary>
        private GameObject CreateAdvancedUIPanel(Transform parent, string name)
        {
            var panelObj = new GameObject(name);
            panelObj.transform.SetParent(parent, false);
            
            var rectTransform = panelObj.AddComponent<RectTransform>();
            var image = panelObj.AddComponent<Image>();
            
            // 인벤토리 패널은 명확히 보이도록 설정
            if (name == "InventoryPanel")
            {
                image.color = new Color(0.1f, 0.1f, 0.1f, 0.9f); // 어두운 배경
            }
            else if (name.Contains("Header"))
            {
                image.color = new Color(0.2f, 0.2f, 0.2f, 1f); // 헤더는 조금 더 밝게
            }
            else
            {
                image.color = defaultBackgroundColor.a == 0 ? new Color(0.15f, 0.15f, 0.15f, 0.8f) : defaultBackgroundColor;
            }
            
            // 기본 앵커 설정 (전체 영역)
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            
            return panelObj;
        }
        
        /// <summary>
        /// 고급 텍스트 생성
        /// </summary>
        private Text CreateAdvancedText(Transform parent, string name, string text, int fontSize = 14)
        {
            var textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            
            var rectTransform = textObj.AddComponent<RectTransform>();
            var textComponent = textObj.AddComponent<Text>();
            
            textComponent.text = text;
            textComponent.font = defaultFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = fontSize;
            textComponent.color = defaultTextColor;
            textComponent.alignment = TextAnchor.MiddleCenter;
            
            // 기본 크기 설정
            rectTransform.sizeDelta = new Vector2(200, 30);
            
            return textComponent;
        }
        
        /// <summary>
        /// 고급 버튼 생성
        /// </summary>
        private Button CreateAdvancedButton(Transform parent, string name, string text)
        {
            var buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);
            
            var rectTransform = buttonObj.AddComponent<RectTransform>();
            var image = buttonObj.AddComponent<Image>();
            var button = buttonObj.AddComponent<Button>();
            
            image.color = defaultButtonColor;
            rectTransform.sizeDelta = new Vector2(100, 30);
            
            // 버튼 텍스트
            var textObj = CreateAdvancedText(buttonObj.transform, "Text", text);
            textObj.rectTransform.anchorMin = Vector2.zero;
            textObj.rectTransform.anchorMax = Vector2.one;
            textObj.rectTransform.sizeDelta = Vector2.zero;
            textObj.rectTransform.anchoredPosition = Vector2.zero;
            textObj.color = Color.white;
            
            return button;
        }
        
        /// <summary>
        /// 고급 입력 필드 생성
        /// </summary>
        private InputField CreateAdvancedInputField(Transform parent, string name, string placeholder)
        {
            var inputObj = new GameObject(name);
            inputObj.transform.SetParent(parent, false);
            
            var rectTransform = inputObj.AddComponent<RectTransform>();
            var image = inputObj.AddComponent<Image>();
            var inputField = inputObj.AddComponent<InputField>();
            
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            rectTransform.sizeDelta = new Vector2(200, 30);
            
            // 텍스트
            var textObj = CreateAdvancedText(inputObj.transform, "Text", "");
            textObj.rectTransform.anchorMin = Vector2.zero;
            textObj.rectTransform.anchorMax = Vector2.one;
            textObj.rectTransform.sizeDelta = new Vector2(-10, 0);
            textObj.rectTransform.anchoredPosition = Vector2.zero;
            textObj.alignment = TextAnchor.MiddleLeft;
            textObj.color = Color.white;
            
            // 플레이스홀더
            var placeholderObj = CreateAdvancedText(inputObj.transform, "Placeholder", placeholder);
            placeholderObj.rectTransform.anchorMin = Vector2.zero;
            placeholderObj.rectTransform.anchorMax = Vector2.one;
            placeholderObj.rectTransform.sizeDelta = new Vector2(-10, 0);
            placeholderObj.rectTransform.anchoredPosition = Vector2.zero;
            placeholderObj.alignment = TextAnchor.MiddleLeft;
            placeholderObj.color = new Color(0.7f, 0.7f, 0.7f, 0.7f);
            
            inputField.textComponent = textObj;
            inputField.placeholder = placeholderObj;
            
            return inputField;
        }
        
        /// <summary>
        /// 고급 스크롤 렉트 생성
        /// </summary>
        private ScrollRect CreateAdvancedScrollRect(Transform parent, string name)
        {
            var scrollObj = new GameObject(name);
            scrollObj.transform.SetParent(parent, false);
            
            var rectTransform = scrollObj.AddComponent<RectTransform>();
            var image = scrollObj.AddComponent<Image>();
            var scrollRect = scrollObj.AddComponent<ScrollRect>();
            var mask = scrollObj.AddComponent<Mask>();
            
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            rectTransform.sizeDelta = new Vector2(400, 300);
            
            // 뷰포트
            var viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollObj.transform, false);
            var viewportRect = viewportObj.AddComponent<RectTransform>();
            var viewportImage = viewportObj.AddComponent<Image>();
            var viewportMask = viewportObj.AddComponent<Mask>();
            
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;
            viewportImage.color = Color.clear;
            
            // 콘텐츠
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRect = contentObj.AddComponent<RectTransform>();
            
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 300);
            contentRect.anchoredPosition = Vector2.zero;
            
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            
            return scrollRect;
        }
        
        /// <summary>
        /// 고급 Raw 이미지 생성
        /// </summary>
        private RawImage CreateAdvancedRawImage(Transform parent, string name)
        {
            var imageObj = new GameObject(name);
            imageObj.transform.SetParent(parent, false);
            
            var rectTransform = imageObj.AddComponent<RectTransform>();
            var rawImage = imageObj.AddComponent<RawImage>();
            
            rectTransform.sizeDelta = new Vector2(200, 200);
            rawImage.color = Color.white;
            
            return rawImage;
        }
        
        /// <summary>
        /// 고급 슬라이더 생성
        /// </summary>
        private Slider CreateAdvancedSlider(Transform parent, string name)
        {
            var sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(parent, false);
            
            var rectTransform = sliderObj.AddComponent<RectTransform>();
            var slider = sliderObj.AddComponent<Slider>();
            
            rectTransform.sizeDelta = new Vector2(150, 20);
            
            // 백그라운드
            var background = CreateAdvancedUIPanel(sliderObj.transform, "Background");
            var bgImage = background.GetComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            
            // 핸들 슬라이드 영역
            var handleSlideArea = CreateAdvancedUIPanel(sliderObj.transform, "Handle Slide Area");
            handleSlideArea.GetComponent<Image>().color = Color.clear;
            
            // 핸들
            var handle = CreateAdvancedUIPanel(handleSlideArea.transform, "Handle");
            var handleImage = handle.GetComponent<Image>();
            handleImage.color = Color.white;
            handle.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 0);
            
            slider.targetGraphic = handleImage;
            slider.handleRect = handle.GetComponent<RectTransform>();
            
            return slider;
        }
        
        /// <summary>
        /// RectTransform 설정 헬퍼
        /// </summary>
        private void SetRectTransform(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rectTransform = obj.GetComponent<RectTransform>();
            if (rectTransform == null) return;
            
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }
        
        /// <summary>
        /// 스탯 섹션 생성 헬퍼
        /// </summary>
        private GameObject CreateStatsSection(Transform parent, string name, string title)
        {
            var section = CreateAdvancedUIPanel(parent, name);
            var layout = section.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.padding = new RectOffset(5, 5, 5, 5);
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
            
            var titleText = CreateAdvancedText(section.transform, "Title", title, 16);
            titleText.color = Color.yellow;
            
            return section;
        }
        
        /// <summary>
        /// 개별 스탯 UI 요소 생성
        /// </summary>
        private StatUIElement CreateStatUIElement(Transform parent, string name, string statName)
        {
            var statContainer = CreateAdvancedUIPanel(parent, name);
            var layout = statContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.padding = new RectOffset(5, 5, 2, 2);
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;
            
            // 스탯 이름 텍스트
            var nameText = CreateAdvancedText(statContainer.transform, "StatName", statName, 12);
            nameText.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 20);
            
            // 기본 값 텍스트
            var baseValueText = CreateAdvancedText(statContainer.transform, "BaseValue", "10", 12);
            baseValueText.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 20);
            
            // 보너스 값 텍스트
            var bonusValueText = CreateAdvancedText(statContainer.transform, "BonusValue", "+0", 12);
            bonusValueText.color = Color.green;
            bonusValueText.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 20);
            
            // 총합 값 텍스트
            var totalValueText = CreateAdvancedText(statContainer.transform, "TotalValue", "10", 12);
            totalValueText.color = Color.white;
            totalValueText.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 20);
            
            // 증가 버튼
            var increaseButton = CreateAdvancedButton(statContainer.transform, "IncreaseButton", "+");
            increaseButton.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 20);
            
            // 감소 버튼
            var decreaseButton = CreateAdvancedButton(statContainer.transform, "DecreaseButton", "-");
            decreaseButton.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 20);
            
            // StatUIElement 컴포넌트 추가 및 연결
            var statUIElement = statContainer.AddComponent<StatUIElement>();
            
            // SerializedObject를 사용하여 private 필드들 연결
            var serializedStatElement = new SerializedObject(statUIElement);
            serializedStatElement.FindProperty("statNameText").objectReferenceValue = nameText;
            serializedStatElement.FindProperty("baseValueText").objectReferenceValue = baseValueText;
            serializedStatElement.FindProperty("bonusValueText").objectReferenceValue = bonusValueText;
            serializedStatElement.FindProperty("totalValueText").objectReferenceValue = totalValueText;
            serializedStatElement.FindProperty("increaseButton").objectReferenceValue = increaseButton;
            serializedStatElement.FindProperty("decreaseButton").objectReferenceValue = decreaseButton;
            serializedStatElement.ApplyModifiedProperties();
            
            return statUIElement;
        }
        
        /// <summary>
        /// StatsUI 스크립트에 UI 참조들 연결
        /// </summary>
        private void ConnectStatsUIReferences(StatsUI statsUI, GameObject statsPanel, Button toggleButton, Button closeButton,
            Text playerNameText, Text levelText, Text expText, Slider expSlider, Text availablePointsText,
            Slider healthSlider, Text healthText, Slider manaSlider, Text manaText,
            StatUIElement strStat, StatUIElement agiStat, StatUIElement vitStat, StatUIElement intStat,
            StatUIElement defStat, StatUIElement mdefStat, StatUIElement lukStat,
            Text attackDamageText, Text magicDamageText, Text moveSpeedText, Text attackSpeedText,
            Text critChanceText, Text critDamageText)
        {
            var serializedObject = new SerializedObject(statsUI);
            
            // UI References
            serializedObject.FindProperty("statsPanel").objectReferenceValue = statsPanel;
            serializedObject.FindProperty("toggleStatsButton").objectReferenceValue = toggleButton;
            serializedObject.FindProperty("closeStatsButton").objectReferenceValue = closeButton;
            
            // Player Info
            serializedObject.FindProperty("playerNameText").objectReferenceValue = playerNameText;
            serializedObject.FindProperty("levelText").objectReferenceValue = levelText;
            serializedObject.FindProperty("expText").objectReferenceValue = expText;
            serializedObject.FindProperty("expSlider").objectReferenceValue = expSlider;
            serializedObject.FindProperty("availablePointsText").objectReferenceValue = availablePointsText;
            
            // Health & Mana
            serializedObject.FindProperty("healthSlider").objectReferenceValue = healthSlider;
            serializedObject.FindProperty("healthText").objectReferenceValue = healthText;
            serializedObject.FindProperty("manaSlider").objectReferenceValue = manaSlider;
            serializedObject.FindProperty("manaText").objectReferenceValue = manaText;
            
            // Primary Stats
            serializedObject.FindProperty("strStat").objectReferenceValue = strStat;
            serializedObject.FindProperty("agiStat").objectReferenceValue = agiStat;
            serializedObject.FindProperty("vitStat").objectReferenceValue = vitStat;
            serializedObject.FindProperty("intStat").objectReferenceValue = intStat;
            serializedObject.FindProperty("defStat").objectReferenceValue = defStat;
            serializedObject.FindProperty("mdefStat").objectReferenceValue = mdefStat;
            serializedObject.FindProperty("lukStat").objectReferenceValue = lukStat;
            
            // Derived Stats
            serializedObject.FindProperty("attackDamageText").objectReferenceValue = attackDamageText;
            serializedObject.FindProperty("magicDamageText").objectReferenceValue = magicDamageText;
            serializedObject.FindProperty("moveSpeedText").objectReferenceValue = moveSpeedText;
            serializedObject.FindProperty("attackSpeedText").objectReferenceValue = attackSpeedText;
            serializedObject.FindProperty("critChanceText").objectReferenceValue = critChanceText;
            serializedObject.FindProperty("critDamageText").objectReferenceValue = critDamageText;
            
            // Settings
            serializedObject.FindProperty("toggleKey").enumValueIndex = (int)KeyCode.C;
            serializedObject.FindProperty("showStatsOnStart").boolValue = false;
            
            serializedObject.ApplyModifiedProperties();
            
            Debug.Log("✅ StatsUI references connected successfully!");
        }
        
        /// <summary>
        /// 고급 스킬 슬롯 프리팹 생성
        /// </summary>
        private void CreateAdvancedSkillSlotPrefab()
        {
            var slotObj = new GameObject("AdvancedSkillSlot");
            var rectTransform = slotObj.AddComponent<RectTransform>();
            var image = slotObj.AddComponent<Image>();
            var skillSlotUI = slotObj.AddComponent<SkillSlotUI>();
            
            rectTransform.sizeDelta = new Vector2(80, 80);
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            
            // 스킬 아이콘
            var iconObj = new GameObject("SkillIcon");
            iconObj.transform.SetParent(slotObj.transform, false);
            var iconRect = iconObj.AddComponent<RectTransform>();
            var iconImage = iconObj.AddComponent<Image>();
            
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.sizeDelta = new Vector2(-10, -10);
            iconRect.anchoredPosition = Vector2.zero;
            iconImage.color = Color.clear;
            
            // 레벨 텍스트
            var levelText = CreateAdvancedText(slotObj.transform, "LevelText", "", 12);
            SetRectTransform(levelText.gameObject, new Vector2(0.7f, 0), new Vector2(1, 0.3f), Vector2.zero, Vector2.zero);
            
            // 쿨다운 오버레이
            var cooldownOverlay = new GameObject("CooldownOverlay");
            cooldownOverlay.transform.SetParent(slotObj.transform, false);
            var cooldownRect = cooldownOverlay.AddComponent<RectTransform>();
            var cooldownImage = cooldownOverlay.AddComponent<Image>();
            
            cooldownRect.anchorMin = Vector2.zero;
            cooldownRect.anchorMax = Vector2.one;
            cooldownRect.sizeDelta = Vector2.zero;
            cooldownRect.anchoredPosition = Vector2.zero;
            cooldownImage.color = new Color(0, 0, 0, 0.6f);
            cooldownImage.type = Image.Type.Filled;
            cooldownImage.fillMethod = Image.FillMethod.Radial360;
            cooldownOverlay.SetActive(false);
            
            SaveAdvancedPrefab(slotObj, "AdvancedSkillSlot");
        }
        
        /// <summary>
        /// 고급 채팅 메시지 프리팹 생성
        /// </summary>
        private void CreateAdvancedChatMessagePrefab()
        {
            var messageObj = new GameObject("AdvancedChatMessage");
            var rectTransform = messageObj.AddComponent<RectTransform>();
            var image = messageObj.AddComponent<Image>();
            var chatMessageUI = messageObj.AddComponent<ChatMessageUI>();
            var layoutElement = messageObj.AddComponent<LayoutElement>();
            
            rectTransform.sizeDelta = new Vector2(400, 80);
            image.color = new Color(0, 0, 0, 0.3f);
            layoutElement.minHeight = 80;
            layoutElement.flexibleHeight = 0;
            
            // 플레이어 이름
            var playerNameText = CreateAdvancedText(messageObj.transform, "PlayerName", "Player", 14);
            SetRectTransform(playerNameText.gameObject, new Vector2(0, 0.6f), new Vector2(0.3f, 1), new Vector2(5, 0), new Vector2(-5, -5));
            playerNameText.alignment = TextAnchor.MiddleLeft;
            playerNameText.fontStyle = FontStyle.Bold;
            
            // 메시지 텍스트
            var messageText = CreateAdvancedText(messageObj.transform, "MessageText", "Message content", 12);
            SetRectTransform(messageText.gameObject, new Vector2(0, 0), new Vector2(0.8f, 0.6f), new Vector2(5, 5), new Vector2(-5, 0));
            messageText.alignment = TextAnchor.UpperLeft;
            
            // 타임스탬프
            var timestampText = CreateAdvancedText(messageObj.transform, "Timestamp", "00:00", 10);
            SetRectTransform(timestampText.gameObject, new Vector2(0.8f, 0.6f), new Vector2(1, 1), new Vector2(0, 0), new Vector2(-5, -5));
            timestampText.alignment = TextAnchor.MiddleRight;
            timestampText.color = Color.gray;
            
            SaveAdvancedPrefab(messageObj, "AdvancedChatMessage");
        }
        
        /// <summary>
        /// 고급 미니맵 아이콘 프리팹들 생성
        /// </summary>
        private void CreateAdvancedMinimapIconPrefabs()
        {
            string[] iconTypes = { "Player", "Monster", "Item", "Waypoint" };
            Color[] iconColors = { Color.blue, Color.red, Color.yellow, Color.white };
            
            for (int i = 0; i < iconTypes.Length; i++)
            {
                var iconObj = new GameObject($"AdvancedMinimap{iconTypes[i]}Icon");
                var rectTransform = iconObj.AddComponent<RectTransform>();
                var image = iconObj.AddComponent<Image>();
                var minimapIcon = iconObj.AddComponent<MinimapIcon>();
                var canvasGroup = iconObj.AddComponent<CanvasGroup>();
                
                rectTransform.sizeDelta = new Vector2(16, 16);
                image.color = iconColors[i];
                
                // 아이콘에 따라 다른 모양 설정
                switch (iconTypes[i])
                {
                    case "Player":
                        // 플레이어는 화살표 모양으로
                        image.sprite = Resources.Load<Sprite>("UI/PlayerArrow");
                        break;
                    case "Monster":
                        // 몬스터는 원형으로
                        image.sprite = Resources.Load<Sprite>("UI/Circle");
                        break;
                    case "Item":
                        // 아이템은 다이아몬드 모양으로
                        image.sprite = Resources.Load<Sprite>("UI/Diamond");
                        break;
                    case "Waypoint":
                        // 웨이포인트는 별 모양으로
                        image.sprite = Resources.Load<Sprite>("UI/Star");
                        break;
                }
                
                SaveAdvancedPrefab(iconObj, $"AdvancedMinimap{iconTypes[i]}Icon");
            }
        }
        
        /// <summary>
        /// 고급 프리팹 저장
        /// </summary>
        private void SaveAdvancedPrefab(GameObject obj, string name)
        {
            // UIManager가 Resources 폴더에서 찾으므로 Resources/UI/ 경로에 저장
            string resourcesUIPath = "Assets/Resources/UI/";
            string fullPath = Path.Combine(resourcesUIPath, $"{name}.prefab");
            
            // 디렉토리가 없으면 생성
            string directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // 프리팹 생성
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, fullPath);
            
            if (prefab != null)
            {
                Debug.Log($"✅ Advanced prefab created: {fullPath}");
            }
            else
            {
                Debug.LogError($"❌ Failed to create advanced prefab: {fullPath}");
            }
            
            // 씬에서 임시 오브젝트 제거
            DestroyImmediate(obj);
        }
        
        /// <summary>
        /// 생성된 고급 프리팹들 정리
        /// </summary>
        private void ClearGeneratedPrefabs()
        {
            if (EditorUtility.DisplayDialog("Clear Advanced Prefabs", 
                "Are you sure you want to delete all generated advanced UI prefabs?", 
                "Yes", "No"))
            {
                if (Directory.Exists(prefabSavePath))
                {
                    string[] prefabFiles = Directory.GetFiles(prefabSavePath, "Advanced*.prefab");
                    foreach (string file in prefabFiles)
                    {
                        AssetDatabase.DeleteAsset(file);
                    }
                    
                    AssetDatabase.Refresh();
                    Debug.Log($"🗑️ Cleared {prefabFiles.Length} advanced prefab files.");
                    EditorUtility.DisplayDialog("Cleared", $"Cleared {prefabFiles.Length} advanced prefab files.", "OK");
                }
            }
        }
        
        private GameObject CreateUIPanel(string name, Transform parent)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // 반투명 어두운 배경
            
            return panel;
        }
        
        private GameObject CreateSlider(string name, Transform parent, Color fillColor)
        {
            GameObject slider = DefaultControls.CreateSlider(new DefaultControls.Resources());
            slider.name = name;
            slider.transform.SetParent(parent);
            
            // Fill 색상 설정
            Transform fillTransform = slider.transform.Find("Fill Area/Fill");
            if (fillTransform != null)
            {
                Image fillImage = fillTransform.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.color = fillColor;
                }
            }
            
            return slider;
        }
        
        private GameObject CreateText(string name, Transform parent, string text)
        {
            GameObject textObj = DefaultControls.CreateText(new DefaultControls.Resources());
            textObj.name = name;
            textObj.transform.SetParent(parent);
            
            Text textComponent = textObj.GetComponent<Text>();
            textComponent.text = text;
            textComponent.color = Color.white;
            textComponent.fontSize = 14;
            textComponent.alignment = TextAnchor.MiddleCenter;
            
            return textObj;
        }
        
        private GameObject CreateButton(string name, Transform parent, string text)
        {
            GameObject button = DefaultControls.CreateButton(new DefaultControls.Resources());
            button.name = name;
            button.transform.SetParent(parent);
            
            Text buttonText = button.GetComponentInChildren<Text>();
            buttonText.text = text;
            
            return button;
        }
        
        private GameObject CreateInventorySlot(string name, Transform parent)
        {
            GameObject slot = new GameObject(name);
            slot.transform.SetParent(parent);
            
            RectTransform rect = slot.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(64, 64);
            
            Image background = slot.AddComponent<Image>();
            background.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            // ItemIcon
            GameObject icon = CreateText("ItemIcon", slot.transform, "");
            // StackText
            GameObject stackText = CreateText("StackText", slot.transform, "");
            
            return slot;
        }
        
        private void ConnectPlayerHUDComponents(PlayerHUD hudComponent, GameObject mainPanel, 
            GameObject healthPanel, GameObject resourcePanel, GameObject healthSlider, 
            GameObject manaSlider, GameObject healthText, GameObject manaText, 
            GameObject expSlider, GameObject levelText, GameObject expText, 
            GameObject goldText, GameObject goldIcon, GameObject raceText, 
            GameObject statusParent)
        {
            // SerializedObject를 통한 private 필드 연결
            SerializedObject serializedHUD = new SerializedObject(hudComponent);
            
            serializedHUD.FindProperty("mainHUDPanel").objectReferenceValue = mainPanel;
            serializedHUD.FindProperty("healthPanel").objectReferenceValue = healthPanel;
            serializedHUD.FindProperty("resourcePanel").objectReferenceValue = resourcePanel;
            serializedHUD.FindProperty("healthSlider").objectReferenceValue = healthSlider.GetComponent<Slider>();
            serializedHUD.FindProperty("manaSlider").objectReferenceValue = manaSlider.GetComponent<Slider>();
            serializedHUD.FindProperty("healthText").objectReferenceValue = healthText.GetComponent<Text>();
            serializedHUD.FindProperty("manaText").objectReferenceValue = manaText.GetComponent<Text>();
            serializedHUD.FindProperty("experienceSlider").objectReferenceValue = expSlider.GetComponent<Slider>();
            serializedHUD.FindProperty("levelText").objectReferenceValue = levelText.GetComponent<Text>();
            serializedHUD.FindProperty("expText").objectReferenceValue = expText.GetComponent<Text>();
            serializedHUD.FindProperty("goldText").objectReferenceValue = goldText.GetComponent<Text>();
            serializedHUD.FindProperty("raceText").objectReferenceValue = raceText.GetComponent<Text>();
            serializedHUD.FindProperty("statusEffectsParent").objectReferenceValue = statusParent.transform;
            
            serializedHUD.ApplyModifiedProperties();
        }
        
        private void SavePrefab(GameObject prefabObject, string path)
        {
            // Resources 폴더 경로 확인 및 생성
            string resourcesPath = "Assets/Resources";
            string fullPath = $"{resourcesPath}/{path}.prefab";
            
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            string uiFolderPath = $"{resourcesPath}/UI";
            if (!AssetDatabase.IsValidFolder(uiFolderPath))
            {
                AssetDatabase.CreateFolder(resourcesPath, "UI");
            }
            
            // 프리팹 생성 및 저장
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabObject, fullPath);
            
            // Hierarchy에서 임시 오브젝트 삭제
            DestroyImmediate(prefabObject);
            
            // 에셋 데이터베이스 새로고침
            AssetDatabase.Refresh();
            
            Debug.Log($"✅ Prefab saved: {fullPath}");
        }
    }
}