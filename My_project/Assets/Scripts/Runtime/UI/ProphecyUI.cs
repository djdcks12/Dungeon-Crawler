using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// Prophecy UI - View active prophecies, progress, and discard.
    /// Hotkey: F7
    /// </summary>
    public class ProphecyUI : MonoBehaviour
    {
        private GameObject mainPanel;
        private Text titleText;
        private Text dailyLimitText;
        private Transform contentArea;
        private Button requestButton;
        private Button closeButton;

        private List<GameObject> entries = new List<GameObject>();
        private bool isInitialized;

        private void Start()
        {
            CreateUI();
            isInitialized = true;
            mainPanel.SetActive(false);

            if (ProphecySystem.Instance != null)
            {
                ProphecySystem.Instance.OnProphecyCompleted += OnProphecyCompleted;
                ProphecySystem.Instance.OnProphecyProgress += OnProphecyProgress;
            }
        }

        private void OnProphecyCompleted(int prophecyId) => RefreshList();
        private void OnProphecyProgress(int prophecyId, int progress) => RefreshList();

        private void OnDestroy()
        {
            if (ProphecySystem.Instance != null)
            {
                ProphecySystem.Instance.OnProphecyCompleted -= OnProphecyCompleted;
                ProphecySystem.Instance.OnProphecyProgress -= OnProphecyProgress;
            }
        }

        private void Update()
        {
            if (!isInitialized) return;

            if (Input.GetKeyDown(KeyCode.F7))
            {
                if (mainPanel.activeSelf) mainPanel.SetActive(false);
                else OpenProphecy();
            }

            if (mainPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                mainPanel.SetActive(false);
        }

        public void OpenProphecy()
        {
            mainPanel.SetActive(true);
            RefreshList();
        }

        private void RefreshList()
        {
            ClearEntries();
            if (ProphecySystem.Instance == null) return;

            var clientId = Unity.Netcode.NetworkManager.Singleton?.LocalClientId ?? 0;
            var active = ProphecySystem.Instance.GetActiveProphecies(clientId);

            titleText.text = $"예언 목록 ({active.Count}/{ProphecySystem.MaxActiveProphecies})";

            foreach (var prophecy in active)
            {
                var def = ProphecySystem.GetProphecyDef(prophecy.prophecyId);
                if (def == null) continue;

                string gradeColor = def.grade switch
                {
                    ProphecyGrade.Rare => "#4444FF",
                    ProphecyGrade.Legendary => "#FF8800",
                    _ => "#AAAAAA"
                };

                float progress = def.requiredCount > 0 ? (float)prophecy.currentProgress / def.requiredCount : 0f;

                // Entry container
                var entry = new GameObject("ProphecyEntry");
                entry.transform.SetParent(contentArea, false);
                var layout = entry.AddComponent<LayoutElement>();
                layout.preferredHeight = 70;
                var bg = entry.AddComponent<Image>();
                bg.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);

                // Name
                var nameGo = new GameObject("Name");
                nameGo.transform.SetParent(entry.transform, false);
                var nameRect = nameGo.AddComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0, 0.5f);
                nameRect.anchorMax = new Vector2(0.7f, 1f);
                nameRect.offsetMin = new Vector2(10, 0);
                nameRect.offsetMax = Vector2.zero;
                var nameText = nameGo.AddComponent<Text>();
                nameText.text = $"<color={gradeColor}>[{def.grade}]</color> {def.name}";
                nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                nameText.fontSize = 14;
                nameText.color = Color.white;

                // Description
                var descGo = new GameObject("Desc");
                descGo.transform.SetParent(entry.transform, false);
                var descRect = descGo.AddComponent<RectTransform>();
                descRect.anchorMin = new Vector2(0, 0);
                descRect.anchorMax = new Vector2(0.7f, 0.5f);
                descRect.offsetMin = new Vector2(10, 0);
                descRect.offsetMax = Vector2.zero;
                var descText = descGo.AddComponent<Text>();
                descText.text = $"{def.description} ({prophecy.currentProgress}/{def.requiredCount})";
                descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                descText.fontSize = 12;
                descText.color = new Color(0.7f, 0.7f, 0.7f);

                // Progress bar background
                var barBg = new GameObject("BarBg");
                barBg.transform.SetParent(entry.transform, false);
                var barBgRect = barBg.AddComponent<RectTransform>();
                barBgRect.anchorMin = new Vector2(0.72f, 0.6f);
                barBgRect.anchorMax = new Vector2(0.98f, 0.85f);
                barBgRect.offsetMin = Vector2.zero;
                barBgRect.offsetMax = Vector2.zero;
                var barBgImg = barBg.AddComponent<Image>();
                barBgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

                // Progress bar fill
                var barFill = new GameObject("BarFill");
                barFill.transform.SetParent(barBg.transform, false);
                var barFillRect = barFill.AddComponent<RectTransform>();
                barFillRect.anchorMin = Vector2.zero;
                barFillRect.anchorMax = new Vector2(progress, 1f);
                barFillRect.offsetMin = Vector2.zero;
                barFillRect.offsetMax = Vector2.zero;
                var barFillImg = barFill.AddComponent<Image>();
                barFillImg.color = new Color(0.2f, 0.6f, 0.2f, 1f);

                // Discard button
                int pid = prophecy.prophecyId;
                var discardGo = new GameObject("Discard");
                discardGo.transform.SetParent(entry.transform, false);
                var discardRect = discardGo.AddComponent<RectTransform>();
                discardRect.anchorMin = new Vector2(0.72f, 0.1f);
                discardRect.anchorMax = new Vector2(0.98f, 0.5f);
                discardRect.offsetMin = Vector2.zero;
                discardRect.offsetMax = Vector2.zero;
                var discardImg = discardGo.AddComponent<Image>();
                discardImg.color = new Color(0.5f, 0.15f, 0.15f, 1f);
                var discardBtn = discardGo.AddComponent<Button>();
                discardBtn.targetGraphic = discardImg;
                discardBtn.onClick.AddListener(() =>
                {
                    ProphecySystem.Instance?.DiscardProphecyServerRpc(pid);
                    RefreshList();
                });

                var discardTxt = new GameObject("Text");
                discardTxt.transform.SetParent(discardGo.transform, false);
                var dtRect = discardTxt.AddComponent<RectTransform>();
                dtRect.anchorMin = Vector2.zero;
                dtRect.anchorMax = Vector2.one;
                dtRect.sizeDelta = Vector2.zero;
                var dt = discardTxt.AddComponent<Text>();
                dt.text = $"파기 ({ProphecySystem.DiscardCost}G)";
                dt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                dt.fontSize = 11;
                dt.color = Color.white;
                dt.alignment = TextAnchor.MiddleCenter;

                entries.Add(entry);
            }
        }

        private void OnRequestProphecy()
        {
            ProphecySystem.Instance?.RequestProphecyServerRpc();
            RefreshList();
        }

        private void ClearEntries()
        {
            foreach (var e in entries)
                if (e != null) Destroy(e);
            entries.Clear();
        }

        private void CreateUI()
        {
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 121;
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
            }

            mainPanel = new GameObject("ProphecyPanel");
            mainPanel.transform.SetParent(transform, false);
            var panelRect = mainPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(550, 450);
            var panelImg = mainPanel.AddComponent<Image>();
            panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

            // Title
            titleText = CreateText(mainPanel.transform, "Title", "예언 목록", 20,
                new Vector2(0, 190), new Vector2(400, 40));
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;

            // Request button
            requestButton = CreateButton(mainPanel.transform, "RequestBtn", "새 예언 요청",
                new Vector2(-130, 150), new Vector2(160, 35), OnRequestProphecy);

            // Daily limit text
            dailyLimitText = CreateText(mainPanel.transform, "DailyLimit",
                $"일일 한도: {ProphecySystem.DailyProphecyLimit}개", 12,
                new Vector2(80, 150), new Vector2(200, 35));

            // Scroll content area
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(mainPanel.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchoredPosition = new Vector2(0, -30);
            scrollRect.sizeDelta = new Vector2(520, 320);
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
            vlg.spacing = 5;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = cRect;
            scroll.viewport = vpRect;
            contentArea = content.transform;

            // Close
            closeButton = CreateButton(mainPanel.transform, "CloseBtn", "X",
                new Vector2(245, 195), new Vector2(40, 40), () => mainPanel.SetActive(false));
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
    }
}
