using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// Item Crafting UI - Unified interface for Infusion, Corruption, and Tempering.
    /// Hotkey: J
    /// 3 tabs: Infusion (InfusionSystem), Corruption (CorruptionSystem), Tempering (TemperingSystem)
    /// </summary>
    public class ItemCraftingUI : MonoBehaviour
    {
        private GameObject mainPanel;
        private Text titleText;
        private Transform contentArea;
        private Button closeButton;

        // Tab system
        private Button[] tabButtons = new Button[3];
        private int currentTab;
        private string[] tabNames = { "주입", "타락", "템퍼링" };

        // Infusion tab
        private Text infusionSourceText;
        private Text infusionTargetText;
        private Text infusionRateText;
        private Text infusionCostText;
        private Button infuseButton;
        private string selectedSourceId;
        private string selectedTargetId;
        private int selectedSourceGrade;
        private int selectedTargetGrade;

        // Corruption tab
        private Text corruptionItemText;
        private Text corruptionChanceText;
        private Button corruptButton;
        private string corruptionTargetId;

        // Tempering tab
        private Text temperItemText;
        private Text temperRecipeText;
        private Text temperAttemptsText;
        private Transform recipeListContent;
        private Button temperButton;
        private string temperTargetId;
        private int selectedRecipeIndex = -1;

        private List<GameObject> dynamicEntries = new List<GameObject>();
        private bool isInitialized;

        private void Start()
        {
            CreateUI();
            isInitialized = true;
            mainPanel.SetActive(false);
        }

        private void Update()
        {
            if (!isInitialized) return;

            if (Input.GetKeyDown(KeyCode.J))
            {
                if (mainPanel.activeSelf) mainPanel.SetActive(false);
                else OpenCrafting();
            }

            if (mainPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                mainPanel.SetActive(false);
        }

        public void OpenCrafting()
        {
            mainPanel.SetActive(true);
            SwitchTab(0);
        }

        private void SwitchTab(int tabIndex)
        {
            currentTab = tabIndex;
            ClearDynamic();

            for (int i = 0; i < tabButtons.Length; i++)
            {
                var img = tabButtons[i].GetComponent<Image>();
                img.color = i == currentTab
                    ? new Color(0.3f, 0.2f, 0.5f, 1f)
                    : new Color(0.15f, 0.15f, 0.2f, 0.8f);
            }

            titleText.text = tabNames[currentTab];
            RefreshTab();
        }

        private void RefreshTab()
        {
            ClearDynamic();
            switch (currentTab)
            {
                case 0: ShowInfusionTab(); break;
                case 1: ShowCorruptionTab(); break;
                case 2: ShowTemperingTab(); break;
            }
        }

        #region Infusion Tab

        private void ShowInfusionTab()
        {
            string srcName = string.IsNullOrEmpty(selectedSourceId) ? "[소스 장비 선택]" : selectedSourceId;
            string tgtName = string.IsNullOrEmpty(selectedTargetId) ? "[타겟 장비 선택]" : selectedTargetId;

            infusionSourceText.text = $"소스: {srcName}";
            infusionTargetText.text = $"타겟: {tgtName}";

            if (InfusionSystem.Instance != null && !string.IsNullOrEmpty(selectedSourceId) && !string.IsNullOrEmpty(selectedTargetId))
            {
                float rate = InfusionSystem.Instance.GetSuccessRate(selectedSourceGrade, selectedTargetGrade);
                long cost = InfusionSystem.Instance.GetInfusionCost(selectedTargetGrade);
                var data = InfusionSystem.Instance.GetInfusionData(selectedTargetId);

                infusionRateText.text = $"성공률: {rate:P0} | 주입 횟수: {data.infusionCount}/{InfusionSystem.MaxInfusionsPerItem}";
                infusionCostText.text = $"비용: {cost:N0}G";
                infuseButton.interactable = data.infusionCount < InfusionSystem.MaxInfusionsPerItem;
            }
            else
            {
                infusionRateText.text = "소스와 타겟 장비를 선택하세요";
                infusionCostText.text = "";
                infuseButton.interactable = false;
            }
        }

        private void OnInfuseClicked()
        {
            if (InfusionSystem.Instance == null) return;
            if (string.IsNullOrEmpty(selectedSourceId) || string.IsNullOrEmpty(selectedTargetId)) return;
            InfusionSystem.Instance.InfuseItemServerRpc(selectedSourceId, selectedTargetId,
                selectedSourceGrade, selectedTargetGrade);
            selectedSourceId = null;
            RefreshTab();
        }

        #endregion

        #region Corruption Tab

        private void ShowCorruptionTab()
        {
            string itemName = string.IsNullOrEmpty(corruptionTargetId) ? "[장비 선택]" : corruptionTargetId;
            corruptionItemText.text = $"대상: {itemName}";

            if (!string.IsNullOrEmpty(corruptionTargetId))
            {
                corruptionChanceText.text = "결과: 25% 강화 | 25% 변형 | 25% 약화 | 25% 파괴\n<color=#FF4444>주의: 이 작업은 되돌릴 수 없습니다!</color>";
                corruptButton.interactable = true;
            }
            else
            {
                corruptionChanceText.text = "타락시킬 장비를 선택하세요";
                corruptButton.interactable = false;
            }
        }

        private void OnCorruptClicked()
        {
            if (CorruptionSystem.Instance == null) return;
            if (string.IsNullOrEmpty(corruptionTargetId)) return;

            // Show confirmation
            NotificationManager.Instance?.ShowNotification(
                "<color=#FF4444>정말로 타락을 진행합니까? 아이템이 파괴될 수 있습니다!</color>",
                NotificationType.Warning);

            if (CorruptionSystem.Instance != null)
                CorruptionSystem.Instance.CorruptItemServerRpc(corruptionTargetId);
            corruptionTargetId = null;
            RefreshTab();
        }

        #endregion

        #region Tempering Tab

        private void ShowTemperingTab()
        {
            string itemName = string.IsNullOrEmpty(temperTargetId) ? "[장비 선택]" : temperTargetId;
            temperItemText.text = $"대상: {itemName}";

            if (TemperingSystem.Instance != null)
            {
                // Show recipe list
                var recipes = TemperingSystem.Instance.GetAllRecipes();
                if (recipes != null)
                {
                    for (int i = 0; i < recipes.Length; i++)
                    {
                        int idx = i;
                        var recipe = recipes[i];
                        var entry = CreateTextButton(contentArea, $"  [{recipe.Category}] {recipe.Name}",
                            () => SelectRecipe(idx));
                        dynamicEntries.Add(entry);
                    }
                }

                if (selectedRecipeIndex >= 0 && !string.IsNullOrEmpty(temperTargetId))
                {
                    temperButton.interactable = true;
                    temperRecipeText.text = $"선택 레시피: #{selectedRecipeIndex + 1}";
                }
                else
                {
                    temperButton.interactable = false;
                    temperRecipeText.text = "레시피와 장비를 선택하세요";
                }
            }
        }

        private void SelectRecipe(int index)
        {
            selectedRecipeIndex = index;
            RefreshTab();
        }

        private void OnTemperClicked()
        {
            if (TemperingSystem.Instance == null) return;
            if (string.IsNullOrEmpty(temperTargetId) || selectedRecipeIndex < 0) return;
            TemperingSystem.Instance.TemperItemServerRpc(temperTargetId, 0, selectedRecipeIndex);
            RefreshTab();
        }

        #endregion

        #region UI Construction

        private void CreateUI()
        {
            // Canvas
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 120;
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // Main panel
            mainPanel = CreatePanel(transform, "CraftingPanel",
                new Vector2(0.5f, 0.5f), new Vector2(600, 500));

            // Title
            titleText = CreateText(mainPanel.transform, "Title", "아이템 가공", 20,
                new Vector2(0, 210), new Vector2(400, 40));
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;

            // Tab bar
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                tabButtons[i] = CreateButton(mainPanel.transform, $"Tab_{tabNames[i]}",
                    tabNames[i], new Vector2(-180 + i * 180, 170), new Vector2(160, 35),
                    () => SwitchTab(idx));
            }

            // Content area
            var contentGo = new GameObject("ContentArea");
            contentGo.transform.SetParent(mainPanel.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchoredPosition = new Vector2(0, -20);
            contentRect.sizeDelta = new Vector2(560, 320);
            contentArea = contentGo.transform;

            // Scroll view
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(contentArea, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchoredPosition = Vector2.zero;
            scrollRect.sizeDelta = new Vector2(560, 320);
            var scroll = scrollGo.AddComponent<ScrollRect>();

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchoredPosition = Vector2.zero;
            vpRect.sizeDelta = new Vector2(560, 320);
            viewport.AddComponent<RectMask2D>();

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var cRect = content.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1);
            cRect.sizeDelta = new Vector2(0, 0);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = cRect;
            scroll.viewport = vpRect;
            contentArea = content.transform;

            // Infusion fields
            infusionSourceText = CreateText(mainPanel.transform, "SrcText", "소스: [선택]", 14,
                new Vector2(-100, 100), new Vector2(350, 25));
            infusionTargetText = CreateText(mainPanel.transform, "TgtText", "타겟: [선택]", 14,
                new Vector2(-100, 75), new Vector2(350, 25));
            infusionRateText = CreateText(mainPanel.transform, "RateText", "", 14,
                new Vector2(-100, 50), new Vector2(350, 25));
            infusionCostText = CreateText(mainPanel.transform, "CostText", "", 14,
                new Vector2(-100, 25), new Vector2(350, 25));
            infuseButton = CreateButton(mainPanel.transform, "InfuseBtn", "주입 실행", new Vector2(0, -200),
                new Vector2(200, 40), OnInfuseClicked);

            // Corruption fields
            corruptionItemText = CreateText(mainPanel.transform, "CorruptItem", "대상: [선택]", 14,
                new Vector2(-100, 100), new Vector2(350, 25));
            corruptionChanceText = CreateText(mainPanel.transform, "CorruptChance", "", 14,
                new Vector2(-100, 50), new Vector2(350, 50));
            corruptButton = CreateButton(mainPanel.transform, "CorruptBtn", "타락 실행", new Vector2(0, -200),
                new Vector2(200, 40), OnCorruptClicked);

            // Tempering fields
            temperItemText = CreateText(mainPanel.transform, "TemperItem", "대상: [선택]", 14,
                new Vector2(-100, 100), new Vector2(350, 25));
            temperRecipeText = CreateText(mainPanel.transform, "TemperRecipe", "", 14,
                new Vector2(-100, 50), new Vector2(350, 25));
            temperAttemptsText = CreateText(mainPanel.transform, "TemperAttempts", "", 14,
                new Vector2(-100, 25), new Vector2(350, 25));
            temperButton = CreateButton(mainPanel.transform, "TemperBtn", "템퍼링 실행", new Vector2(0, -200),
                new Vector2(200, 40), OnTemperClicked);

            // Close button
            closeButton = CreateButton(mainPanel.transform, "CloseBtn", "X", new Vector2(270, 215),
                new Vector2(40, 40), () => mainPanel.SetActive(false));

            // Initially hide tab-specific elements
            SetTabElementsVisible(false, false, false);
        }

        private void SetTabElementsVisible(bool infusion, bool corruption, bool tempering)
        {
            infusionSourceText.gameObject.SetActive(infusion);
            infusionTargetText.gameObject.SetActive(infusion);
            infusionRateText.gameObject.SetActive(infusion);
            infusionCostText.gameObject.SetActive(infusion);
            infuseButton.gameObject.SetActive(infusion);

            corruptionItemText.gameObject.SetActive(corruption);
            corruptionChanceText.gameObject.SetActive(corruption);
            corruptButton.gameObject.SetActive(corruption);

            temperItemText.gameObject.SetActive(tempering);
            temperRecipeText.gameObject.SetActive(tempering);
            temperAttemptsText.gameObject.SetActive(tempering);
            temperButton.gameObject.SetActive(tempering);
        }

        #endregion

        #region Helpers

        private void ClearDynamic()
        {
            foreach (var e in dynamicEntries)
                if (e != null) Destroy(e);
            dynamicEntries.Clear();

            SetTabElementsVisible(currentTab == 0, currentTab == 1, currentTab == 2);
        }

        private GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
            return go;
        }

        private Text CreateText(Transform parent, string name, string text, int fontSize,
            Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            return t;
        }

        private Button CreateButton(Transform parent, string name, string label,
            Vector2 pos, Vector2 size, System.Action onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.25f, 0.4f, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var txtRect = txtGo.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;
            var txt = txtGo.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 14;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }

        private GameObject CreateTextButton(Transform parent, string label, System.Action onClick)
        {
            var go = new GameObject("Entry");
            go.transform.SetParent(parent, false);
            var layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = 30;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var txtRect = txtGo.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;
            var txt = txtGo.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 13;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;

            btn.onClick.AddListener(() => onClick?.Invoke());
            return go;
        }

        private void OnDestroy()
        {
            if (InfusionSystem.Instance != null)
                InfusionSystem.Instance.OnInfusionSuccess -= OnInfusionResult;
        }

        private void OnInfusionResult(string itemId, InfusionSlotData slot)
        {
            RefreshTab();
        }

        #endregion
    }
}
