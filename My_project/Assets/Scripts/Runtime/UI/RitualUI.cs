using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// Ritual UI - Displayed when interacting with ritual altars in dungeons.
    /// Sacrifice items for points, defend waves, select rewards.
    /// </summary>
    public class RitualUI : MonoBehaviour
    {
        private GameObject mainPanel;
        private Text titleText;
        private Text pointsText;
        private Text phaseText;
        private Transform contentArea;
        private Button sacrificeButton;
        private Button startWaveButton;
        private Button closeButton;

        private List<GameObject> entries = new List<GameObject>();
        private string currentAltarKey;
        private bool isInitialized;

        private void Start()
        {
            CreateUI();
            isInitialized = true;
            mainPanel.SetActive(false);

            if (RitualSystem.Instance != null)
            {
                RitualSystem.Instance.OnSacrificeAdded += OnSacrifice;
                RitualSystem.Instance.OnWaveComplete += OnWaveComplete;
                RitualSystem.Instance.OnRewardClaimed += OnRewardClaimed;
            }
        }

        private void Update()
        {
            if (mainPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                mainPanel.SetActive(false);
        }

        /// <summary>
        /// Open ritual UI for a specific altar.
        /// </summary>
        public void OpenRitual(string altarKey)
        {
            currentAltarKey = altarKey;
            mainPanel.SetActive(true);
            RefreshDisplay();
        }

        public void CloseRitual()
        {
            mainPanel.SetActive(false);
            currentAltarKey = null;
        }

        private void OnSacrifice(string altarKey, int totalPoints)
        {
            if (altarKey == currentAltarKey) RefreshDisplay();
        }

        private void OnWaveComplete(string altarKey)
        {
            if (altarKey == currentAltarKey) RefreshDisplay();
        }

        private void OnRewardClaimed(string altarKey, RitualRewardType type)
        {
            if (altarKey == currentAltarKey) RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            ClearEntries();
            if (string.IsNullOrEmpty(currentAltarKey)) return;

            titleText.text = "의식 제단";
            pointsText.text = "포인트: 0";
            phaseText.text = "제물을 투입하세요";

            sacrificeButton.gameObject.SetActive(true);
            startWaveButton.gameObject.SetActive(true);
        }

        private void OnSacrificeItem()
        {
            if (RitualSystem.Instance == null || string.IsNullOrEmpty(currentAltarKey)) return;
            // Sacrifice the lowest-grade item from inventory (simplified)
            RitualSystem.Instance.SacrificeItemServerRpc(currentAltarKey, 0);
        }

        private void OnStartWave()
        {
            if (RitualSystem.Instance == null || string.IsNullOrEmpty(currentAltarKey)) return;
            RitualSystem.Instance.StartRitualWaveServerRpc(currentAltarKey);
            phaseText.text = "<color=#FF4444>웨이브 진행 중...</color>";
            startWaveButton.interactable = false;
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
                canvas.sortingOrder = 122;
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
            }

            mainPanel = new GameObject("RitualPanel");
            mainPanel.transform.SetParent(transform, false);
            var panelRect = mainPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(500, 400);
            var panelImg = mainPanel.AddComponent<Image>();
            panelImg.color = new Color(0.1f, 0.05f, 0.12f, 0.95f);

            titleText = CreateText(mainPanel.transform, "Title", "의식 제단", 20,
                new Vector2(0, 165), new Vector2(400, 40));
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.8f, 0.5f, 1f);

            pointsText = CreateText(mainPanel.transform, "Points", "포인트: 0", 16,
                new Vector2(-100, 125), new Vector2(300, 30));

            phaseText = CreateText(mainPanel.transform, "Phase", "제물을 투입하세요", 14,
                new Vector2(-100, 100), new Vector2(350, 25));

            // Scroll content
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(mainPanel.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchoredPosition = new Vector2(0, -20);
            scrollRect.sizeDelta = new Vector2(460, 220);
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
            contentArea = content.transform;

            // Buttons
            sacrificeButton = CreateButton(mainPanel.transform, "SacrificeBtn", "제물 투입",
                new Vector2(-100, -165), new Vector2(160, 40), OnSacrificeItem);

            startWaveButton = CreateButton(mainPanel.transform, "WaveBtn", "웨이브 시작",
                new Vector2(100, -165), new Vector2(160, 40), OnStartWave);

            closeButton = CreateButton(mainPanel.transform, "CloseBtn", "X",
                new Vector2(220, 170), new Vector2(40, 40), CloseRitual);
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
            img.color = new Color(0.3f, 0.15f, 0.4f, 1f);
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

        private void OnDestroy()
        {
            if (RitualSystem.Instance != null)
            {
                RitualSystem.Instance.OnSacrificeAdded -= OnSacrifice;
                RitualSystem.Instance.OnWaveComplete -= OnWaveComplete;
                RitualSystem.Instance.OnRewardClaimed -= OnRewardClaimed;
            }
        }
    }
}
