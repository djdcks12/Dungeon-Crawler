using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// Transformation UI - Shows transformation gauge, available forms, and active bonuses.
    /// Hotkey: F10
    /// </summary>
    public class TransformationUI : MonoBehaviour
    {
        private GameObject mainPanel;
        private Text titleText;
        private Text gaugeText;
        private Image gaugeBar;
        private Text activeFormText;
        private Transform formListContent;
        private Button closeButton;

        private List<GameObject> entries = new List<GameObject>();
        private bool isInitialized;

        // HUD gauge (always visible when gauge > 0)
        private GameObject hudGauge;
        private Image hudGaugeBar;
        private Text hudGaugeText;

        private void Start()
        {
            CreateUI();
            CreateHUDGauge();
            isInitialized = true;
            mainPanel.SetActive(false);

            if (TransformationSystem.Instance != null)
            {
                TransformationSystem.Instance.OnGaugeChanged += OnGaugeChanged;
                TransformationSystem.Instance.OnTransformActivated += OnTransformActivated;
                TransformationSystem.Instance.OnTransformEnded += RefreshDisplay;
            }
        }

        private void Update()
        {
            if (!isInitialized) return;

            if (Input.GetKeyDown(KeyCode.F10))
            {
                if (mainPanel.activeSelf) mainPanel.SetActive(false);
                else OpenTransformation();
            }

            if (mainPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                mainPanel.SetActive(false);

            // Update HUD gauge
            UpdateHUDGauge();
        }

        public void OpenTransformation()
        {
            mainPanel.SetActive(true);
            RefreshDisplay();
        }

        private void OnGaugeChanged(float gauge)
        {
            if (mainPanel.activeSelf) RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            ClearEntries();
            if (TransformationSystem.Instance == null) return;
            var sys = TransformationSystem.Instance;

            float gauge = sys.LocalGauge;
            gaugeText.text = $"변신 게이지: {gauge:F0}/100";
            gaugeBar.fillAmount = gauge / 100f;
            gaugeBar.color = gauge >= 100f ? new Color(1f, 0.5f, 0f) : new Color(0.3f, 0.6f, 0.9f);

            var activeForm = sys.LocalActiveForm;
            if (activeForm.HasValue)
            {
                var data = sys.GetTransformationData(activeForm.Value);
                activeFormText.text = $"<color=#FF8800>변신 활성:</color> {data?.Name ?? activeForm.Value.ToString()}";
            }
            else
            {
                activeFormText.text = "변신 비활성";
            }

            // List available transformations
            var types = System.Enum.GetValues(typeof(TransformationType));
            foreach (TransformationType type in types)
            {
                var data = sys.GetTransformationData(type);
                if (data == null) continue;

                var entryGo = new GameObject("FormEntry");
                entryGo.transform.SetParent(formListContent, false);
                var layout = entryGo.AddComponent<LayoutElement>();
                layout.preferredHeight = 55;
                var bg = entryGo.AddComponent<Image>();

                bool isActive = activeForm.HasValue && activeForm.Value == type;
                bg.color = isActive
                    ? new Color(0.2f, 0.15f, 0.05f, 0.95f)
                    : new Color(0.12f, 0.12f, 0.18f, 0.9f);

                // Name
                var nameGo = new GameObject("Name");
                nameGo.transform.SetParent(entryGo.transform, false);
                var nameRect = nameGo.AddComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0, 0.5f);
                nameRect.anchorMax = new Vector2(0.5f, 1);
                nameRect.offsetMin = new Vector2(10, 0);
                nameRect.offsetMax = Vector2.zero;
                var nameText = nameGo.AddComponent<Text>();
                nameText.text = data.Name;
                nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                nameText.fontSize = 14;
                nameText.color = isActive ? new Color(1f, 0.7f, 0.2f) : Color.white;
                nameText.fontStyle = FontStyle.Bold;

                // Description
                var descGo = new GameObject("Desc");
                descGo.transform.SetParent(entryGo.transform, false);
                var descRect = descGo.AddComponent<RectTransform>();
                descRect.anchorMin = new Vector2(0, 0);
                descRect.anchorMax = new Vector2(0.7f, 0.5f);
                descRect.offsetMin = new Vector2(10, 0);
                descRect.offsetMax = Vector2.zero;
                var descText = descGo.AddComponent<Text>();
                descText.text = data.Description;
                descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                descText.fontSize = 11;
                descText.color = new Color(0.7f, 0.7f, 0.7f);

                // Activate button
                if (!isActive && gauge >= 100f)
                {
                    TransformationType capturedType = type;
                    var btnGo = new GameObject("ActivateBtn");
                    btnGo.transform.SetParent(entryGo.transform, false);
                    var btnRect = btnGo.AddComponent<RectTransform>();
                    btnRect.anchorMin = new Vector2(0.72f, 0.15f);
                    btnRect.anchorMax = new Vector2(0.98f, 0.85f);
                    btnRect.offsetMin = Vector2.zero;
                    btnRect.offsetMax = Vector2.zero;
                    var btnImg = btnGo.AddComponent<Image>();
                    btnImg.color = new Color(0.5f, 0.3f, 0.1f, 1f);
                    var btn = btnGo.AddComponent<Button>();
                    btn.targetGraphic = btnImg;
                    btn.onClick.AddListener(() =>
                    {
                        TransformationSystem.Instance?.ActivateTransformServerRpc((int)capturedType);
                        RefreshDisplay();
                    });

                    var btnTxtGo = new GameObject("Text");
                    btnTxtGo.transform.SetParent(btnGo.transform, false);
                    var btRect = btnTxtGo.AddComponent<RectTransform>();
                    btRect.anchorMin = Vector2.zero;
                    btRect.anchorMax = Vector2.one;
                    btRect.sizeDelta = Vector2.zero;
                    var bt = btnTxtGo.AddComponent<Text>();
                    bt.text = "변신";
                    bt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    bt.fontSize = 13;
                    bt.color = Color.white;
                    bt.alignment = TextAnchor.MiddleCenter;
                }

                entries.Add(entryGo);
            }
        }

        private void UpdateHUDGauge()
        {
            if (TransformationSystem.Instance == null)
            {
                hudGauge.SetActive(false);
                return;
            }

            float gauge = TransformationSystem.Instance.LocalGauge;
            bool show = gauge > 0f || TransformationSystem.Instance.LocalActiveForm.HasValue;
            hudGauge.SetActive(show);

            if (show)
            {
                hudGaugeBar.fillAmount = gauge / 100f;
                hudGaugeBar.color = gauge >= 100f ? new Color(1f, 0.5f, 0f) : new Color(0.3f, 0.6f, 0.9f);
                hudGaugeText.text = TransformationSystem.Instance.LocalActiveForm.HasValue
                    ? "TRANSFORMED" : $"{gauge:F0}%";
            }
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
                canvas.sortingOrder = 125;
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
            }

            mainPanel = new GameObject("TransformPanel");
            mainPanel.transform.SetParent(transform, false);
            var panelRect = mainPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(500, 420);
            var panelImg = mainPanel.AddComponent<Image>();
            panelImg.color = new Color(0.1f, 0.06f, 0.02f, 0.95f);

            titleText = CreateText(mainPanel.transform, "Title", "변신 시스템", 20,
                new Vector2(0, 175), new Vector2(400, 40));
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(1f, 0.6f, 0.2f);

            // Gauge bar
            var gaugeBg = new GameObject("GaugeBg");
            gaugeBg.transform.SetParent(mainPanel.transform, false);
            var gaugeBgRect = gaugeBg.AddComponent<RectTransform>();
            gaugeBgRect.anchoredPosition = new Vector2(0, 140);
            gaugeBgRect.sizeDelta = new Vector2(400, 20);
            var gaugeBgImg = gaugeBg.AddComponent<Image>();
            gaugeBgImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            var gaugeFill = new GameObject("GaugeFill");
            gaugeFill.transform.SetParent(gaugeBg.transform, false);
            var gaugeFillRect = gaugeFill.AddComponent<RectTransform>();
            gaugeFillRect.anchorMin = Vector2.zero;
            gaugeFillRect.anchorMax = new Vector2(0, 1);
            gaugeFillRect.offsetMin = Vector2.zero;
            gaugeFillRect.offsetMax = Vector2.zero;
            gaugeBar = gaugeFill.AddComponent<Image>();

            gaugeText = CreateText(mainPanel.transform, "GaugeText", "게이지: 0/100", 13,
                new Vector2(0, 118), new Vector2(400, 20));
            gaugeText.alignment = TextAnchor.MiddleCenter;

            activeFormText = CreateText(mainPanel.transform, "ActiveForm", "변신 비활성", 14,
                new Vector2(0, 98), new Vector2(400, 25));
            activeFormText.alignment = TextAnchor.MiddleCenter;

            // Scroll content
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(mainPanel.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchoredPosition = new Vector2(0, -30);
            scrollRect.sizeDelta = new Vector2(460, 240);
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
            vlg.spacing = 4;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = cRect;
            scroll.viewport = vpRect;
            formListContent = content.transform;

            closeButton = CreateButton(mainPanel.transform, "CloseBtn", "X",
                new Vector2(220, 180), new Vector2(40, 40), () => mainPanel.SetActive(false));
        }

        private void CreateHUDGauge()
        {
            hudGauge = new GameObject("HUDGauge");
            hudGauge.transform.SetParent(transform, false);
            var rect = hudGauge.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 120);
            rect.sizeDelta = new Vector2(200, 15);

            var bg = hudGauge.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(hudGauge.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            hudGaugeBar = fill.AddComponent<Image>();

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(hudGauge.transform, false);
            var txtRect = txtGo.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;
            hudGaugeText = txtGo.AddComponent<Text>();
            hudGaugeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hudGaugeText.fontSize = 10;
            hudGaugeText.color = Color.white;
            hudGaugeText.alignment = TextAnchor.MiddleCenter;

            hudGauge.SetActive(false);
        }

        #endregion

        #region Helpers

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
            img.color = new Color(0.3f, 0.2f, 0.1f, 1f);
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

        private void OnTransformActivated(TransformationType type) => RefreshDisplay();

        private void OnDestroy()
        {
            if (TransformationSystem.Instance != null)
            {
                TransformationSystem.Instance.OnGaugeChanged -= OnGaugeChanged;
                TransformationSystem.Instance.OnTransformActivated -= OnTransformActivated;
                TransformationSystem.Instance.OnTransformEnded -= RefreshDisplay;
            }
        }

        #endregion
    }
}
