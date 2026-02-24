using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// Bestiary UI - Monster capture codex, milestones, and processing.
    /// Hotkey: F8
    /// 2 tabs: Bestiary (captured monsters) and Processing (unprocessed captures)
    /// </summary>
    public class BestiaryUI : MonoBehaviour
    {
        private GameObject mainPanel;
        private Text titleText;
        private Text statsText;
        private Transform contentArea;
        private Button closeButton;

        private Button[] tabButtons = new Button[2];
        private int currentTab;
        private string[] tabNames = { "도감", "분해" };

        private List<GameObject> entries = new List<GameObject>();
        private bool isInitialized;

        private static readonly string[] MonsterRaces =
        {
            "Goblin", "Orc", "Undead", "Beast", "Elemental", "Demon", "Dragon", "Construct"
        };

        private void Start()
        {
            CreateUI();
            isInitialized = true;
            mainPanel.SetActive(false);
        }

        private void Update()
        {
            if (!isInitialized) return;

            if (Input.GetKeyDown(KeyCode.F8))
            {
                if (mainPanel.activeSelf) mainPanel.SetActive(false);
                else OpenBestiary();
            }

            if (mainPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                mainPanel.SetActive(false);
        }

        public void OpenBestiary()
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
                    ? new Color(0.2f, 0.35f, 0.2f, 1f)
                    : new Color(0.15f, 0.15f, 0.2f, 0.8f);
            }

            RefreshTab();
        }

        private void RefreshTab()
        {
            ClearEntries();
            if (BestiaryHuntSystem.Instance == null) return;

            var clientId = Unity.Netcode.NetworkManager.Singleton?.LocalClientId ?? 0;
            int remaining = BestiaryHuntSystem.Instance.GetDailyRemaining(clientId);
            int totalCaptures = BestiaryHuntSystem.Instance.GetTotalCaptures(clientId);

            statsText.text = $"총 포획: {totalCaptures} | 오늘 남은 횟수: {remaining}/{BestiaryHuntSystem.DailyCaptureLimit}";

            switch (currentTab)
            {
                case 0: ShowBestiaryTab(clientId); break;
                case 1: ShowProcessingTab(clientId); break;
            }
        }

        private void ShowBestiaryTab(ulong clientId)
        {
            titleText.text = "몬스터 도감";
            var bestiary = BestiaryHuntSystem.Instance.GetBestiary(clientId);

            foreach (var race in MonsterRaces)
            {
                bool hasCaptured = bestiary.TryGetValue(race, out var entry);

                var go = new GameObject("BestiaryEntry");
                go.transform.SetParent(contentArea, false);
                var layout = go.AddComponent<LayoutElement>();
                layout.preferredHeight = 50;
                var bg = go.AddComponent<Image>();
                bg.color = hasCaptured
                    ? new Color(0.1f, 0.2f, 0.1f, 0.9f)
                    : new Color(0.15f, 0.15f, 0.15f, 0.6f);

                // Race name
                var nameGo = new GameObject("Name");
                nameGo.transform.SetParent(go.transform, false);
                var nameRect = nameGo.AddComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0, 0);
                nameRect.anchorMax = new Vector2(0.4f, 1);
                nameRect.offsetMin = new Vector2(10, 0);
                nameRect.offsetMax = Vector2.zero;
                var nameText = nameGo.AddComponent<Text>();
                nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                nameText.fontSize = 15;
                nameText.color = hasCaptured ? Color.white : new Color(0.4f, 0.4f, 0.4f);
                nameText.alignment = TextAnchor.MiddleLeft;

                if (hasCaptured)
                {
                    string eliteTag = entry.eliteCaptured ? " <color=#FFD700>[E]</color>" : "";
                    string bossTag = entry.bossCaptured ? " <color=#FF4444>[B]</color>" : "";
                    nameText.text = $"{race}{eliteTag}{bossTag}";
                }
                else
                {
                    nameText.text = $"{race} (미발견)";
                }

                // Capture count
                var countGo = new GameObject("Count");
                countGo.transform.SetParent(go.transform, false);
                var countRect = countGo.AddComponent<RectTransform>();
                countRect.anchorMin = new Vector2(0.4f, 0);
                countRect.anchorMax = new Vector2(1, 1);
                countRect.offsetMin = Vector2.zero;
                countRect.offsetMax = new Vector2(-10, 0);
                var countText = countGo.AddComponent<Text>();
                countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                countText.fontSize = 13;
                countText.color = new Color(0.7f, 0.7f, 0.7f);
                countText.alignment = TextAnchor.MiddleRight;
                countText.text = hasCaptured ? $"포획 수: {entry.captureCount}" : "-";

                entries.Add(go);
            }
        }

        private void ShowProcessingTab(ulong clientId)
        {
            titleText.text = "포획물 분해";
            var unprocessed = BestiaryHuntSystem.Instance.GetUnprocessedCaptures(clientId);

            if (unprocessed.Count == 0)
            {
                var empty = new GameObject("Empty");
                empty.transform.SetParent(contentArea, false);
                var layout = empty.AddComponent<LayoutElement>();
                layout.preferredHeight = 40;
                var txt = empty.AddComponent<Text>();
                txt.text = "분해할 포획물이 없습니다.";
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 14;
                txt.color = new Color(0.5f, 0.5f, 0.5f);
                txt.alignment = TextAnchor.MiddleCenter;
                entries.Add(empty);
                return;
            }

            for (int i = 0; i < unprocessed.Count; i++)
            {
                int idx = i;
                var mon = unprocessed[i];

                var go = new GameObject("ProcessEntry");
                go.transform.SetParent(contentArea, false);
                var layout = go.AddComponent<LayoutElement>();
                layout.preferredHeight = 45;
                var bg = go.AddComponent<Image>();
                bg.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);

                // Monster info
                var infoGo = new GameObject("Info");
                infoGo.transform.SetParent(go.transform, false);
                var infoRect = infoGo.AddComponent<RectTransform>();
                infoRect.anchorMin = new Vector2(0, 0);
                infoRect.anchorMax = new Vector2(0.7f, 1);
                infoRect.offsetMin = new Vector2(10, 0);
                infoRect.offsetMax = Vector2.zero;
                var infoText = infoGo.AddComponent<Text>();
                infoText.text = $"{mon.monsterRace} {mon.monsterVariant} Lv.{mon.monsterLevel}";
                infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                infoText.fontSize = 14;
                infoText.color = Color.white;
                infoText.alignment = TextAnchor.MiddleLeft;

                // Process button
                var btnGo = new GameObject("ProcessBtn");
                btnGo.transform.SetParent(go.transform, false);
                var btnRect = btnGo.AddComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(0.72f, 0.15f);
                btnRect.anchorMax = new Vector2(0.98f, 0.85f);
                btnRect.offsetMin = Vector2.zero;
                btnRect.offsetMax = Vector2.zero;
                var btnImg = btnGo.AddComponent<Image>();
                btnImg.color = new Color(0.2f, 0.4f, 0.2f, 1f);
                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = btnImg;
                btn.onClick.AddListener(() =>
                {
                    BestiaryHuntSystem.Instance?.ProcessCaptureServerRpc(idx);
                    RefreshTab();
                });

                var btnTxtGo = new GameObject("Text");
                btnTxtGo.transform.SetParent(btnGo.transform, false);
                var btRect = btnTxtGo.AddComponent<RectTransform>();
                btRect.anchorMin = Vector2.zero;
                btRect.anchorMax = Vector2.one;
                btRect.sizeDelta = Vector2.zero;
                var bt = btnTxtGo.AddComponent<Text>();
                bt.text = "분해";
                bt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                bt.fontSize = 13;
                bt.color = Color.white;
                bt.alignment = TextAnchor.MiddleCenter;

                entries.Add(go);
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
                canvas.sortingOrder = 123;
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
            }

            mainPanel = new GameObject("BestiaryPanel");
            mainPanel.transform.SetParent(transform, false);
            var panelRect = mainPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(520, 450);
            var panelImg = mainPanel.AddComponent<Image>();
            panelImg.color = new Color(0.08f, 0.1f, 0.08f, 0.95f);

            titleText = CreateText(mainPanel.transform, "Title", "몬스터 도감", 20,
                new Vector2(0, 190), new Vector2(400, 40));
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.5f, 0.9f, 0.5f);

            statsText = CreateText(mainPanel.transform, "Stats", "", 12,
                new Vector2(0, 155), new Vector2(460, 25));
            statsText.alignment = TextAnchor.MiddleCenter;

            // Tabs
            for (int i = 0; i < 2; i++)
            {
                int idx = i;
                tabButtons[i] = CreateButton(mainPanel.transform, $"Tab_{tabNames[i]}",
                    tabNames[i], new Vector2(-100 + i * 200, 125), new Vector2(160, 35),
                    () => SwitchTab(idx));
            }

            // Scroll content
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(mainPanel.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchoredPosition = new Vector2(0, -30);
            scrollRect.sizeDelta = new Vector2(480, 280);
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

            closeButton = CreateButton(mainPanel.transform, "CloseBtn", "X",
                new Vector2(230, 195), new Vector2(40, 40), () => mainPanel.SetActive(false));
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

        #endregion
    }
}
