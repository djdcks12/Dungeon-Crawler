using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 원정대 UI - O키 토글
    /// 원정 목록/진행중/결과
    /// </summary>
    public class ExpeditionUI : MonoBehaviour
    {
        private GameObject mainPanel;
        private Text titleText;
        private Transform contentArea;
        private Button closeButton;

        // 탭
        private Button availableTab;
        private Button activeTab;
        private int currentTab; // 0=가능한 원정, 1=진행중

        private List<GameObject> entries = new List<GameObject>();
        private bool isInitialized;

        private void Start()
        {
            CreateUI();
            isInitialized = true;
            mainPanel.SetActive(false);

            if (ExpeditionSystem.Instance != null)
                ExpeditionSystem.Instance.OnExpeditionUpdated += RefreshCurrentTab;
        }

        private void Update()
        {
            if (!isInitialized) return;

            if (Input.GetKeyDown(KeyCode.O))
            {
                if (mainPanel.activeSelf) mainPanel.SetActive(false);
                else OpenPanel();
            }

            if (mainPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                mainPanel.SetActive(false);

            // 진행중 탭은 실시간 갱신
            if (mainPanel.activeSelf && currentTab == 1 && Time.frameCount % 30 == 0)
                RefreshCurrentTab();
        }

        public void OpenPanel()
        {
            mainPanel.SetActive(true);
            if (ExpeditionSystem.Instance != null)
                ExpeditionSystem.Instance.RequestSyncServerRpc();
            SwitchTab(0);
        }

        private void SwitchTab(int tab)
        {
            currentTab = tab;
            availableTab.GetComponent<Image>().color = tab == 0
                ? new Color(0.2f, 0.3f, 0.5f, 1f)
                : new Color(0.15f, 0.15f, 0.2f, 0.8f);
            activeTab.GetComponent<Image>().color = tab == 1
                ? new Color(0.2f, 0.3f, 0.5f, 1f)
                : new Color(0.15f, 0.15f, 0.2f, 0.8f);
            RefreshCurrentTab();
        }

        private void RefreshCurrentTab()
        {
            ClearEntries();
            if (currentTab == 0) ShowAvailableExpeditions();
            else ShowActiveExpeditions();
        }

        private void ShowAvailableExpeditions()
        {
            if (ExpeditionSystem.Instance == null) return;
            var sys = ExpeditionSystem.Instance;
            var templates = sys.GetTemplates();

            titleText.text = $"원정 목록 (진행중: {sys.LocalExpeditions.Count}/{sys.MaxActiveExpeditions})";

            for (int i = 0; i < templates.Length; i++)
            {
                CreateAvailableEntry(i, templates[i], sys);
            }
        }

        private void ShowActiveExpeditions()
        {
            if (ExpeditionSystem.Instance == null) return;
            var sys = ExpeditionSystem.Instance;

            titleText.text = $"진행중 원정 ({sys.LocalExpeditions.Count}/{sys.MaxActiveExpeditions})";

            if (sys.LocalExpeditions.Count == 0)
            {
                CreateInfoEntry("진행중인 원정이 없습니다.");
                return;
            }

            var templates = sys.GetTemplates();
            foreach (var exp in sys.LocalExpeditions)
            {
                CreateActiveEntry(exp, templates);
            }
        }

        private void CreateAvailableEntry(int index, ExpeditionTemplate template, ExpeditionSystem sys)
        {
            var entry = new GameObject($"Expedition_{index}");
            entry.transform.SetParent(contentArea, false);
            var rt = entry.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(620, 80);
            entry.AddComponent<Image>().color = new Color(0.1f, 0.12f, 0.16f, 0.9f);

            // 이름
            CreateText(entry.transform, "Name", $"<color=#FFD700>{template.expeditionName}</color>",
                15, TextAnchor.MiddleLeft, new Vector2(-180, 20), new Vector2(300, 25));

            // 설명
            CreateText(entry.transform, "Desc", $"<color=#AAAAAA>{template.description}</color>",
                12, TextAnchor.MiddleLeft, new Vector2(-180, 0), new Vector2(300, 20));

            // 시간 / 비용
            string timeStr = FormatDuration(template.durationSeconds);
            CreateText(entry.transform, "Info",
                $"소요: {timeStr} | 비용: {template.goldCost}G | 레벨: {template.requiredLevel}+",
                11, TextAnchor.MiddleLeft, new Vector2(-180, -20), new Vector2(350, 20));

            // 성공률
            CreateText(entry.transform, "Rate",
                $"성공률: <color=#44FF44>{template.baseSuccessRate:P0}</color>",
                13, TextAnchor.MiddleCenter, new Vector2(160, 15), new Vector2(130, 25));

            // 보상
            CreateText(entry.transform, "Reward",
                $"<color=#CCCCCC>{template.rewardDescription}</color>",
                11, TextAnchor.MiddleCenter, new Vector2(160, -8), new Vector2(180, 20));

            // 출발 버튼
            bool canStart = sys.LocalExpeditions.Count < sys.MaxActiveExpeditions;
            var btn = CreateButton(entry.transform, "StartBtn", canStart ? "출발" : "만원",
                new Vector2(250, -20), new Vector2(70, 28));
            btn.interactable = canStart;
            if (!canStart) btn.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            int idx = index;
            btn.onClick.AddListener(() => sys.StartExpeditionServerRpc(idx));

            entries.Add(entry);
        }

        private void CreateActiveEntry(ActiveExpedition exp, ExpeditionTemplate[] templates)
        {
            var template = templates[exp.templateIndex];

            var entry = new GameObject($"Active_{exp.expeditionId}");
            entry.transform.SetParent(contentArea, false);
            var rt = entry.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(620, 70);

            Color bgColor = exp.isCompleted
                ? (exp.isSuccess ? new Color(0.1f, 0.2f, 0.1f, 0.9f) : new Color(0.2f, 0.1f, 0.1f, 0.9f))
                : new Color(0.1f, 0.12f, 0.18f, 0.9f);
            entry.AddComponent<Image>().color = bgColor;

            // 이름
            CreateText(entry.transform, "Name", $"<color=#FFD700>{template.expeditionName}</color>",
                14, TextAnchor.MiddleLeft, new Vector2(-180, 15), new Vector2(300, 25));

            if (exp.isCompleted)
            {
                string resultText = exp.isSuccess
                    ? "<color=#44FF44>성공!</color>"
                    : "<color=#FF4444>실패</color>";
                CreateText(entry.transform, "Status", resultText,
                    14, TextAnchor.MiddleLeft, new Vector2(-180, -10), new Vector2(100, 25));

                // 수령 버튼
                var claimBtn = CreateButton(entry.transform, "ClaimBtn", "보상 수령",
                    new Vector2(220, 0), new Vector2(100, 35));
                claimBtn.GetComponent<Image>().color = new Color(0.15f, 0.35f, 0.15f, 1f);
                string expId = exp.expeditionId;
                claimBtn.onClick.AddListener(() =>
                {
                    if (ExpeditionSystem.Instance != null)
                        ExpeditionSystem.Instance.ClaimExpeditionServerRpc(expId);
                });
            }
            else
            {
                // 진행 바
                float progress = exp.Progress;
                string remaining = FormatDuration(exp.RemainingTime);
                CreateText(entry.transform, "Progress",
                    $"진행: {progress:P0} | 남은 시간: {remaining}",
                    12, TextAnchor.MiddleLeft, new Vector2(-180, -10), new Vector2(300, 20));

                // 즉시 완료 버튼
                int rushCost = Mathf.Max(100, Mathf.RoundToInt(exp.RemainingTime / 60f * 50));
                var rushBtn = CreateButton(entry.transform, "RushBtn", $"즉시완료 ({rushCost}G)",
                    new Vector2(220, 0), new Vector2(130, 30));
                rushBtn.GetComponent<Image>().color = new Color(0.4f, 0.35f, 0.1f, 1f);
                string expId = exp.expeditionId;
                rushBtn.onClick.AddListener(() =>
                {
                    if (ExpeditionSystem.Instance != null)
                        ExpeditionSystem.Instance.RushExpeditionServerRpc(expId);
                });
            }

            entries.Add(entry);
        }

        private void CreateInfoEntry(string message)
        {
            var entry = new GameObject("Info");
            entry.transform.SetParent(contentArea, false);
            var rt = entry.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(620, 40);
            CreateText(entry.transform, "Text", $"<color=#888888>{message}</color>",
                14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(600, 35));
            entries.Add(entry);
        }

        private string FormatDuration(float seconds)
        {
            if (seconds <= 0) return "완료";
            int h = Mathf.FloorToInt(seconds / 3600);
            int m = Mathf.FloorToInt((seconds % 3600) / 60);
            int s = Mathf.FloorToInt(seconds % 60);
            if (h > 0) return $"{h}시간 {m}분";
            if (m > 0) return $"{m}분 {s}초";
            return $"{s}초";
        }

        private void ClearEntries()
        {
            foreach (var e in entries) if (e != null) Destroy(e);
            entries.Clear();
        }

        #region UI 생성

        private void CreateUI()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 142;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            mainPanel = new GameObject("ExpeditionPanel");
            mainPanel.transform.SetParent(transform, false);
            var panelRT = mainPanel.AddComponent<RectTransform>();
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(680, 480);
            mainPanel.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.1f, 0.96f);

            var titleObj = CreateText(mainPanel.transform, "Title", "원정대", 20, TextAnchor.MiddleCenter,
                new Vector2(0, 210), new Vector2(300, 35));
            titleText = titleObj.GetComponent<Text>();

            closeButton = CreateButton(mainPanel.transform, "CloseBtn", "X",
                new Vector2(310, 210), new Vector2(40, 40));
            closeButton.onClick.AddListener(() => mainPanel.SetActive(false));

            // 탭
            availableTab = CreateButton(mainPanel.transform, "AvailTab", "원정 목록",
                new Vector2(-130, 175), new Vector2(180, 30));
            availableTab.onClick.AddListener(() => SwitchTab(0));

            activeTab = CreateButton(mainPanel.transform, "ActiveTab", "진행중",
                new Vector2(60, 175), new Vector2(180, 30));
            activeTab.onClick.AddListener(() => SwitchTab(1));

            // 스크롤뷰
            var scrollObj = CreateScrollView(mainPanel.transform, "ExpScroll",
                new Vector2(0, -25), new Vector2(650, 370));
            contentArea = scrollObj.transform.Find("Viewport/Content");
        }

        private GameObject CreateText(Transform parent, string name, string text, int fontSize,
            TextAnchor alignment, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = Color.white;
            txt.supportRichText = true;
            return obj;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var img = obj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.28f, 1f);
            var btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(obj.transform, false);
            var txtRT = txtObj.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.sizeDelta = Vector2.zero;
            var txt = txtObj.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 13;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            return btn;
        }

        private GameObject CreateScrollView(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var scrollObj = new GameObject(name);
            scrollObj.transform.SetParent(parent, false);
            var scrollRT = scrollObj.AddComponent<RectTransform>();
            scrollRT.anchoredPosition = position;
            scrollRT.sizeDelta = size;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.sizeDelta = Vector2.zero;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>().color = Color.clear;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRT = content.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.sizeDelta = new Vector2(0, 0);
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(5, 5, 5, 5);
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.viewport = vpRT;
            scroll.content = contentRT;
            scroll.horizontal = false;
            return scrollObj;
        }

        #endregion

        private void OnDestroy()
        {
            if (ExpeditionSystem.Instance != null)
                ExpeditionSystem.Instance.OnExpeditionUpdated -= RefreshCurrentTab;
        }
    }
}
