using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// UI 프리팹 자동 생성 에디터 도구
    /// Resources/UI 폴더에 기본 UI 프리팹들을 생성
    /// </summary>
    public class UIPrefabGenerator : EditorWindow
    {
        [MenuItem("Tools/UI/Generate UI Prefabs")]
        public static void ShowWindow()
        {
            GetWindow<UIPrefabGenerator>("UI Prefab Generator");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("UI Prefab Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            GUILayout.Label("Generate basic UI prefabs in Resources/UI folder:");
            GUILayout.Space(5);
            
            if (GUILayout.Button("Generate PlayerHUD Prefab"))
            {
                GeneratePlayerHUDPrefab();
            }
            
            if (GUILayout.Button("Generate StatsUI Prefab"))
            {
                GenerateStatsUIPrefab();
            }
            
            if (GUILayout.Button("Generate InventoryUI Prefab"))
            {
                GenerateInventoryUIPrefab();
            }
            
            if (GUILayout.Button("Generate All UI Prefabs"))
            {
                GenerateAllPrefabs();
            }
            
            GUILayout.Space(10);
            GUILayout.Label("Note: Generated prefabs are basic structures.", EditorStyles.helpBox);
            GUILayout.Label("You need to customize them according to your needs.", EditorStyles.helpBox);
        }
        
        private void GenerateAllPrefabs()
        {
            GeneratePlayerHUDPrefab();
            GenerateStatsUIPrefab();
            GenerateInventoryUIPrefab();
            
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
            GameObject statsRoot = new GameObject("StatsUI");
            statsRoot.AddComponent<StatsUI>();
            
            // StatsPanel 생성
            GameObject statsPanel = CreateUIPanel("StatsPanel", statsRoot.transform);
            statsPanel.SetActive(false); // 기본적으로 숨김
            
            // Header 생성
            GameObject header = CreateUIPanel("Header", statsPanel.transform);
            GameObject titleText = CreateText("TitleText", header.transform, "캐릭터 정보");
            GameObject closeButton = CreateButton("CloseButton", header.transform, "X");
            
            // PlayerInfoSection 생성
            GameObject playerInfo = CreateUIPanel("PlayerInfoSection", statsPanel.transform);
            GameObject playerNameText = CreateText("PlayerNameText", playerInfo.transform, "Player_12345");
            GameObject levelText = CreateText("LevelText", playerInfo.transform, "Lv.1");
            GameObject expSlider = CreateSlider("ExpSlider", playerInfo.transform, Color.yellow);
            GameObject expText = CreateText("ExpText", playerInfo.transform, "0 / 100");
            GameObject pointsText = CreateText("AvailablePointsText", playerInfo.transform, "사용 가능 포인트: 0");
            
            // 프리팹 저장
            SavePrefab(statsRoot, "UI/StatsUI");
            
            Debug.Log("✅ StatsUI prefab generated!");
        }
        
        private void GenerateInventoryUIPrefab()
        {
            GameObject invRoot = new GameObject("InventoryUI");
            invRoot.AddComponent<InventoryUI>();
            
            // InventoryPanel 생성
            GameObject invPanel = CreateUIPanel("InventoryPanel", invRoot.transform);
            invPanel.SetActive(false); // 기본적으로 숨김
            
            // Header 생성
            GameObject header = CreateUIPanel("Header", invPanel.transform);
            GameObject titleText = CreateText("TitleText", header.transform, "인벤토리");
            GameObject closeButton = CreateButton("CloseButton", header.transform, "X");
            
            // InventoryGrid 생성
            GameObject invGrid = new GameObject("InventoryGrid");
            invGrid.transform.SetParent(invPanel.transform);
            GridLayoutGroup gridLayout = invGrid.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(64, 64);
            gridLayout.spacing = new Vector2(2, 2);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 6;
            
            // 30개 슬롯 생성
            for (int i = 0; i < 30; i++)
            {
                CreateInventorySlot($"Slot{i:00}", invGrid.transform);
            }
            
            // 프리팹 저장
            SavePrefab(invRoot, "UI/InventoryUI");
            
            Debug.Log("✅ InventoryUI prefab generated!");
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