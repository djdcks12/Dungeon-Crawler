using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// Codex UI - Knowledge library with 5 categories and permanent bonuses.
    /// Hotkey: F9
    /// Shows unlocked/locked entries, category progress, and total bonuses.
    /// </summary>
    public class CodexUI : MonoBehaviour
    {
        private GameObject mainPanel;
        private Text titleText;
        private Text progressText;
        private Image progressBar;
        private Transform contentArea;
        private Button closeButton;

        private Button[] tabButtons = new Button[5];
        private int currentTab;
        private string[] tabNames = { "전투", "방어", "탐험", "제작", "지혜" };
        private CodexCategory[] tabCategories =
        {
            CodexCategory.Combat, CodexCategory.Defense, CodexCategory.Exploration,
            CodexCategory.Crafting, CodexCategory.Wisdom
        };

        private List<GameObject> entries = new List<GameObject>();
        private bool isInitialized;

        private void Start()
        {
            CreateUI();
            isInitialized = true;
            mainPanel.SetActive(false);

            if (CodexSystem.Instance != null)
                CodexSystem.Instance.OnEntryUnlocked += OnCodexEntryUnlocked;
        }

        private void OnCodexEntryUnlocked(ulong clientId, int entryId)
        {
            if (mainPanel != null && mainPanel.activeSelf) RefreshTab();
        }

        private void OnDestroy()
        {
            if (CodexSystem.Instance != null)
                CodexSystem.Instance.OnEntryUnlocked -= OnCodexEntryUnlocked;
        }

        private void Update()
        {
            if (!isInitialized) return;

            if (Input.GetKeyDown(KeyCode.F9))
            {
                if (mainPanel.activeSelf) mainPanel.SetActive(false);
                else OpenCodex();
            }

            if (mainPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                mainPanel.SetActive(false);
        }

        public void OpenCodex()
        {
            mainPanel.SetActive(true);
            SwitchTab(0);
        }

        private void SwitchTab(int tabIndex)
        {
            currentTab = tabIndex;

            for (int i = 0; i < tabButtons.Length; i++)
            {
                var img = tabButtons[i].GetComponent<Image>();
                img.color = i == currentTab
                    ? new Color(0.3f, 0.25f, 0.15f, 1f)
                    : new Color(0.15f, 0.15f, 0.2f, 0.8f);
            }

            RefreshTab();
        }

        private void RefreshTab()
        {
            ClearEntries();
            if (CodexSystem.Instance == null) return;

            var clientId = Unity.Netcode.NetworkManager.Singleton?.LocalClientId ?? 0;
            var category = tabCategories[currentTab];

            if (CodexSystem.Instance == null) return;
            int catProgress = CodexSystem.Instance.GetCategoryProgress(clientId, category);
            int catTotal = CodexSystem.GetCategoryTotal(category);
            int totalUnlocked = CodexSystem.Instance.GetTotalUnlocks(clientId);
            bool complete = CodexSystem.Instance.IsCategoryComplete(clientId, category);

            titleText.text = $"{tabNames[currentTab]} 서고";
            progressText.text = $"진행: {catProgress}/{catTotal} | 전체: {totalUnlocked}/40";
            float fillAmount = catTotal > 0 ? (float)catProgress / catTotal : 0f;
            progressBar.fillAmount = fillAmount;
            progressBar.color = complete ? new Color(1f, 0.84f, 0f) : new Color(0.3f, 0.5f, 0.3f);

            // Show entries for this category
            var categoryEntries = CodexSystem.GetEntriesByCategory(category);
            foreach (var entry in categoryEntries)
            {
                bool unlocked = CodexSystem.Instance.IsUnlocked(clientId, entry.id);
                CreateEntryRow(entry, unlocked);
            }

            // Category milestone
            if (complete)
            {
                var milestoneGo = new GameObject("Milestone");
                milestoneGo.transform.SetParent(contentArea, false);
                var layout = milestoneGo.AddComponent<LayoutElement>();
                layout.preferredHeight = 40;
                var bg = milestoneGo.AddComponent<Image>();
                bg.color = new Color(0.3f, 0.25f, 0.1f, 0.9f);
                var txt = milestoneGo.AddComponent<Text>();
                txt.text = $"<color=#FFD700>서고 완성 보너스 활성!</color>";
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 15;
                txt.color = new Color(1f, 0.84f, 0f);
                txt.alignment = TextAnchor.MiddleCenter;
                entries.Add(milestoneGo);
            }
        }

        private void CreateEntryRow(CodexEntry entry, bool unlocked)
        {
            var go = new GameObject("CodexEntry");
            go.transform.SetParent(contentArea, false);
            var layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = 45;
            var bg = go.AddComponent<Image>();
            bg.color = unlocked
                ? new Color(0.15f, 0.2f, 0.1f, 0.9f)
                : new Color(0.1f, 0.1f, 0.1f, 0.6f);

            // Icon/status
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0);
            iconRect.anchorMax = new Vector2(0.08f, 1);
            iconRect.offsetMin = new Vector2(5, 5);
            iconRect.offsetMax = new Vector2(-5, -5);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = unlocked ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.3f, 0.3f, 0.3f);

            // Name
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(go.transform, false);
            var nameRect = nameGo.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.1f, 0.5f);
            nameRect.anchorMax = new Vector2(0.5f, 1);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            var nameText = nameGo.AddComponent<Text>();
            nameText.text = unlocked ? entry.name : "???";
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 14;
            nameText.color = unlocked ? Color.white : new Color(0.4f, 0.4f, 0.4f);
            nameText.alignment = TextAnchor.MiddleLeft;

            // Description/bonus
            var descGo = new GameObject("Desc");
            descGo.transform.SetParent(go.transform, false);
            var descRect = descGo.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.1f, 0);
            descRect.anchorMax = new Vector2(0.7f, 0.5f);
            descRect.offsetMin = Vector2.zero;
            descRect.offsetMax = Vector2.zero;
            var descText = descGo.AddComponent<Text>();
            descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descText.fontSize = 12;
            descText.alignment = TextAnchor.MiddleLeft;

            if (unlocked)
            {
                string valueStr = entry.isPercentage ? $"+{entry.bonusValue:F1}%" : $"+{entry.bonusValue:F0}";
                descText.text = $"{entry.bonusType} {valueStr}";
                descText.color = new Color(0.5f, 1f, 0.5f);
            }
            else
            {
                descText.text = GetConditionHint(entry);
                descText.color = new Color(0.5f, 0.5f, 0.5f);
            }

            // Status
            var statusGo = new GameObject("Status");
            statusGo.transform.SetParent(go.transform, false);
            var statusRect = statusGo.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.72f, 0);
            statusRect.anchorMax = new Vector2(1, 1);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = new Vector2(-5, 0);
            var statusText = statusGo.AddComponent<Text>();
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText.fontSize = 12;
            statusText.alignment = TextAnchor.MiddleRight;
            statusText.color = unlocked ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.5f, 0.5f, 0.5f);
            statusText.text = unlocked ? "해금됨" : "미해금";

            entries.Add(go);
        }

        private string GetConditionHint(CodexEntry entry)
        {
            return entry.condition switch
            {
                CodexUnlockCondition.DungeonFirstClear => $"던전 클리어: {entry.conditionKey}",
                CodexUnlockCondition.BossKill => $"보스 처치: {entry.conditionKey}",
                CodexUnlockCondition.SecretDiscovery => $"비밀 발견: {entry.conditionKey} ×{entry.conditionValue}",
                CodexUnlockCondition.MonsterKillCount => $"{entry.conditionKey} {entry.conditionValue}마리 처치",
                CodexUnlockCondition.CraftingMilestone => $"제작 마일스톤: {entry.conditionValue}회",
                CodexUnlockCondition.EnhanceMilestone => $"강화 마일스톤: {entry.conditionValue}회",
                CodexUnlockCondition.ExplorationMilestone => $"탐험 마일스톤: {entry.conditionValue}",
                CodexUnlockCondition.CollectionComplete => $"도감 완성: {entry.conditionValue}%",
                _ => "조건 불명"
            };
        }

        private void ClearEntries()
        {
            foreach (var e in entries)
                if (e != null) Destroy(e);
            entries.Clear();
        }

        #region UI Construction

        private void CreateUI()
        {
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 124;
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
            }

            mainPanel = new GameObject("CodexPanel");
            mainPanel.transform.SetParent(transform, false);
            var panelRect = mainPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(600, 480);
            var panelImg = mainPanel.AddComponent<Image>();
            panelImg.color = new Color(0.1f, 0.08f, 0.05f, 0.95f);

            titleText = CreateText(mainPanel.transform, "Title", "지식 서고", 20,
                new Vector2(0, 205), new Vector2(400, 40));
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(1f, 0.84f, 0f);

            progressText = CreateText(mainPanel.transform, "Progress", "", 13,
                new Vector2(0, 170), new Vector2(500, 25));
            progressText.alignment = TextAnchor.MiddleCenter;

            // Progress bar
            var barBg = new GameObject("BarBg");
            barBg.transform.SetParent(mainPanel.transform, false);
            var barBgRect = barBg.AddComponent<RectTransform>();
            barBgRect.anchoredPosition = new Vector2(0, 150);
            barBgRect.sizeDelta = new Vector2(500, 10);
            var barBgImg = barBg.AddComponent<Image>();
            barBgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            var barFill = new GameObject("BarFill");
            barFill.transform.SetParent(barBg.transform, false);
            var barFillRect = barFill.AddComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = new Vector2(0, 1);
            barFillRect.offsetMin = Vector2.zero;
            barFillRect.offsetMax = Vector2.zero;
            progressBar = barFill.AddComponent<Image>();
            progressBar.color = new Color(0.3f, 0.5f, 0.3f);

            // Tab bar
            float tabWidth = 100f;
            float startX = -2f * tabWidth;
            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                tabButtons[i] = CreateButton(mainPanel.transform, $"Tab_{tabNames[i]}",
                    tabNames[i], new Vector2(startX + i * (tabWidth + 10), 120),
                    new Vector2(tabWidth, 30), () => SwitchTab(idx));
            }

            // Scroll content
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(mainPanel.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchoredPosition = new Vector2(0, -40);
            scrollRect.sizeDelta = new Vector2(560, 300);
            var scroll = scrollGo.AddComponent<ScrollRect>();

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var cRect = content.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1);
            cRect.sizeDelta = new Vector2(0, 0);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 3;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = cRect;
            scroll.viewport = vpRect;
            contentArea = content.transform;

            closeButton = CreateButton(mainPanel.transform, "CloseBtn", "X",
                new Vector2(270, 210), new Vector2(40, 40), () => mainPanel.SetActive(false));
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
            img.color = new Color(0.25f, 0.2f, 0.15f, 1f);
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
            txt.alignment = TextAnchor.MiddleCenter;

            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }

        #endregion
    }
}
