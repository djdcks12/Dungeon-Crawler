using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 시즌패스 UI - P키 토글
    /// 보상 트랙 스크롤뷰, 단계/경험치 바, 보상 수령
    /// </summary>
    public class SeasonPassUI : MonoBehaviour
    {
        private GameObject mainPanel;
        private Text seasonInfoText;
        private Text tierText;
        private Image expBar;
        private Text expText;
        private Transform rewardContent;
        private Button premiumButton;
        private Button claimAllButton;
        private Button closeButton;

        private List<GameObject> rewardEntries = new List<GameObject>();
        private bool isInitialized;

        private void Start()
        {
            CreateUI();
            isInitialized = true;
            mainPanel.SetActive(false);

            if (SeasonPassSystem.Instance != null)
            {
                SeasonPassSystem.Instance.OnSeasonDataUpdated += RefreshUI;
                SeasonPassSystem.Instance.OnTierUp += OnTierUp;
            }
        }

        private void Update()
        {
            if (!isInitialized) return;

            if (Input.GetKeyDown(KeyCode.P))
            {
                if (mainPanel.activeSelf)
                    ClosePanel();
                else
                    OpenPanel();
            }

            if (mainPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                ClosePanel();
        }

        public void OpenPanel()
        {
            mainPanel.SetActive(true);
            if (SeasonPassSystem.Instance != null)
                SeasonPassSystem.Instance.RequestSyncServerRpc();
        }

        public void ClosePanel()
        {
            mainPanel.SetActive(false);
        }

        private void OnTierUp(int newTier)
        {
            if (mainPanel.activeSelf) RefreshUI();
        }

        private void RefreshUI()
        {
            if (SeasonPassSystem.Instance == null) return;
            var sys = SeasonPassSystem.Instance;

            seasonInfoText.text = $"시즌 {sys.SeasonNumber}";
            tierText.text = $"단계: <color=#FFD700>{sys.LocalTier}</color> / {sys.MaxTier}";

            float expPercent = sys.LocalExpToNext > 0 ? (float)sys.LocalExp / sys.LocalExpToNext : 1f;
            expBar.fillAmount = expPercent;
            expText.text = sys.LocalTier >= sys.MaxTier ? "MAX" : $"{sys.LocalExp} / {sys.LocalExpToNext}";

            premiumButton.gameObject.SetActive(!sys.LocalIsPremium);

            RefreshRewardList();
        }

        private void RefreshRewardList()
        {
            foreach (var entry in rewardEntries)
                if (entry != null) Destroy(entry);
            rewardEntries.Clear();

            if (SeasonPassSystem.Instance == null) return;
            var sys = SeasonPassSystem.Instance;

            for (int tier = 1; tier <= sys.MaxTier; tier++)
            {
                CreateRewardEntry(tier, sys);
            }
        }

        private void CreateRewardEntry(int tier, SeasonPassSystem sys)
        {
            bool reached = sys.LocalTier >= tier;
            var freeReward = sys.GetFreeReward(tier);
            var premReward = sys.GetPremiumReward(tier);
            bool freeClaimed = sys.IsFreeClaimed(tier);
            bool premClaimed = sys.IsPremiumClaimed(tier);

            var entry = new GameObject($"Tier_{tier}");
            entry.transform.SetParent(rewardContent, false);
            var rt = entry.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(700, 50);

            Color bgColor = reached
                ? new Color(0.12f, 0.18f, 0.12f, 0.9f)
                : new Color(0.1f, 0.1f, 0.12f, 0.7f);
            entry.AddComponent<Image>().color = bgColor;

            // 단계 번호
            string tierColor = reached ? "#FFD700" : "#666666";
            bool isMilestone = tier % 10 == 0;
            string tierStr = isMilestone ? $"<b><color={tierColor}>★{tier}</color></b>" : $"<color={tierColor}>{tier}</color>";
            CreateText(entry.transform, "TierNum", tierStr, 14, TextAnchor.MiddleCenter,
                new Vector2(-320, 0), new Vector2(50, 40));

            // 무료 보상
            string freeStr = freeReward.description ?? "";
            string freeColor = freeClaimed ? "#888888" : (reached ? "#FFFFFF" : "#666666");
            CreateText(entry.transform, "FreeReward", $"<color={freeColor}>{freeStr}</color>",
                12, TextAnchor.MiddleLeft, new Vector2(-140, 0), new Vector2(220, 40));

            // 무료 수령 버튼
            if (reached && !freeClaimed)
            {
                var freeBtn = CreateButton(entry.transform, "ClaimFree", "수령",
                    new Vector2(0, 0), new Vector2(50, 28));
                int t = tier;
                freeBtn.onClick.AddListener(() => sys.ClaimFreeRewardServerRpc(t));
            }
            else if (freeClaimed)
            {
                CreateText(entry.transform, "FreeCheck", "<color=#44FF44>✓</color>", 16,
                    TextAnchor.MiddleCenter, new Vector2(0, 0), new Vector2(30, 30));
            }

            // 프리미엄 보상
            string premStr = premReward.description ?? "";
            string premColor = premClaimed ? "#888888" : (sys.LocalIsPremium && reached ? "#FFD700" : "#555555");
            CreateText(entry.transform, "PremReward", $"<color={premColor}>★ {premStr}</color>",
                12, TextAnchor.MiddleLeft, new Vector2(140, 0), new Vector2(220, 40));

            // 프리미엄 수령 버튼
            if (sys.LocalIsPremium && reached && !premClaimed)
            {
                var premBtn = CreateButton(entry.transform, "ClaimPrem", "수령",
                    new Vector2(290, 0), new Vector2(50, 28));
                premBtn.GetComponent<Image>().color = new Color(0.4f, 0.35f, 0.1f, 1f);
                int t = tier;
                premBtn.onClick.AddListener(() => sys.ClaimPremiumRewardServerRpc(t));
            }
            else if (premClaimed)
            {
                CreateText(entry.transform, "PremCheck", "<color=#FFD700>✓</color>", 16,
                    TextAnchor.MiddleCenter, new Vector2(290, 0), new Vector2(30, 30));
            }

            rewardEntries.Add(entry);
        }

        #region UI 생성

        private void CreateUI()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 145;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            mainPanel = new GameObject("SeasonPassPanel");
            mainPanel.transform.SetParent(transform, false);
            var panelRT = mainPanel.AddComponent<RectTransform>();
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(780, 550);
            mainPanel.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.1f, 0.96f);

            // 타이틀
            CreateText(mainPanel.transform, "Title", "시즌패스", 24, TextAnchor.MiddleCenter,
                new Vector2(0, 245), new Vector2(300, 40));

            // 시즌 정보
            var infoObj = CreateText(mainPanel.transform, "SeasonInfo", "시즌 1", 16, TextAnchor.MiddleLeft,
                new Vector2(-250, 245), new Vector2(150, 30));
            seasonInfoText = infoObj.GetComponent<Text>();

            // 닫기 버튼
            closeButton = CreateButton(mainPanel.transform, "CloseBtn", "X",
                new Vector2(360, 245), new Vector2(40, 40));
            closeButton.onClick.AddListener(ClosePanel);

            // 단계 & 경험치 바
            var tierObj = CreateText(mainPanel.transform, "TierText", "단계: 0 / 50", 16, TextAnchor.MiddleLeft,
                new Vector2(-200, 205), new Vector2(200, 30));
            tierText = tierObj.GetComponent<Text>();

            // 경험치 바 배경
            var expBg = new GameObject("ExpBarBg");
            expBg.transform.SetParent(mainPanel.transform, false);
            var expBgRT = expBg.AddComponent<RectTransform>();
            expBgRT.anchoredPosition = new Vector2(80, 205);
            expBgRT.sizeDelta = new Vector2(350, 22);
            expBg.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 1f);

            var expFill = new GameObject("ExpBarFill");
            expFill.transform.SetParent(expBg.transform, false);
            var expFillRT = expFill.AddComponent<RectTransform>();
            expFillRT.anchorMin = Vector2.zero;
            expFillRT.anchorMax = new Vector2(0, 1);
            expFillRT.pivot = new Vector2(0, 0.5f);
            expFillRT.offsetMin = Vector2.zero;
            expFillRT.offsetMax = Vector2.zero;
            expBar = expFill.AddComponent<Image>();
            expBar.color = new Color(0.2f, 0.6f, 1f, 1f);
            expBar.type = Image.Type.Filled;
            expBar.fillMethod = Image.FillMethod.Horizontal;

            var expTxtObj = CreateText(expBg.transform, "ExpText", "0 / 1000", 12,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(350, 22));
            expText = expTxtObj.GetComponent<Text>();

            // 프리미엄 구매 버튼
            premiumButton = CreateButton(mainPanel.transform, "PremiumBtn", "프리미엄 패스 (50,000G)",
                new Vector2(-200, 170), new Vector2(200, 30));
            premiumButton.GetComponent<Image>().color = new Color(0.5f, 0.4f, 0.1f, 1f);
            premiumButton.onClick.AddListener(() =>
            {
                if (SeasonPassSystem.Instance != null)
                    SeasonPassSystem.Instance.ActivatePremiumServerRpc();
            });

            // 일괄 수령 버튼
            claimAllButton = CreateButton(mainPanel.transform, "ClaimAllBtn", "일괄 수령",
                new Vector2(200, 170), new Vector2(120, 30));
            claimAllButton.onClick.AddListener(() =>
            {
                if (SeasonPassSystem.Instance != null)
                    SeasonPassSystem.Instance.ClaimAllAvailableServerRpc();
            });

            // 헤더 (무료/프리미엄 라벨)
            CreateText(mainPanel.transform, "FreeLabel", "무료 트랙", 14, TextAnchor.MiddleCenter,
                new Vector2(-100, 145), new Vector2(150, 25));
            CreateText(mainPanel.transform, "PremLabel", "<color=#FFD700>★ 프리미엄 트랙</color>", 14,
                TextAnchor.MiddleCenter, new Vector2(180, 145), new Vector2(180, 25));

            // 보상 스크롤뷰
            var scrollObj = CreateScrollView(mainPanel.transform, "RewardScroll",
                new Vector2(0, -55), new Vector2(740, 350));
            rewardContent = scrollObj.transform.Find("Viewport/Content");
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
            img.color = new Color(0.25f, 0.25f, 0.3f, 1f);
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
            layout.spacing = 2;
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
            if (SeasonPassSystem.Instance != null)
            {
                SeasonPassSystem.Instance.OnSeasonDataUpdated -= RefreshUI;
                SeasonPassSystem.Instance.OnTierUp -= OnTierUp;
            }
        }
    }
}
