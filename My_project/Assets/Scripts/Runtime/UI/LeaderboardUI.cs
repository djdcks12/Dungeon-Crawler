using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 리더보드 UI - L키 토글
    /// 카테고리 탭별 순위 표시, 내 순위 하이라이트
    /// </summary>
    public class LeaderboardUI : MonoBehaviour
    {
        public static LeaderboardUI Instance { get; private set; }

        private Canvas leaderboardCanvas;
        private GameObject panelObj;
        private Text titleText;
        private Text rankListText;
        private Text myRankText;
        private bool isVisible = false;

        private LeaderboardCategory currentCategory = LeaderboardCategory.Level;
        private string currentPlayerName = "";

        private readonly string[] categoryNames = { "레벨", "던전 기록", "PvP 킬", "골드", "몬스터 처치" };
        private readonly Color[] categoryColors = {
            new Color(1f, 0.84f, 0f),     // 금
            new Color(0.4f, 0.8f, 1f),    // 하늘
            new Color(1f, 0.3f, 0.3f),    // 빨강
            new Color(1f, 0.85f, 0.3f),   // 주황
            new Color(0.5f, 1f, 0.5f)     // 초록
        };

        private List<Button> tabButtons = new List<Button>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            CreateUI();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                Toggle();
            }

            if (!isVisible) return;

            if (Input.GetKeyDown(KeyCode.Escape))
                Hide();

            // 숫자키로 탭 전환
            for (int i = 0; i < 5; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SwitchCategory((LeaderboardCategory)i);
                }
            }
        }

        public void Toggle()
        {
            if (isVisible) Hide();
            else Show();
        }

        public void Show()
        {
            // 현재 플레이어 이름 가져오기
            var localPlayer = FindLocalPlayerStats();
            if (localPlayer != null)
                currentPlayerName = localPlayer.CharacterName;

            isVisible = true;
            if (panelObj != null) panelObj.SetActive(true);
            RefreshDisplay();
        }

        public void Hide()
        {
            isVisible = false;
            if (panelObj != null) panelObj.SetActive(false);
        }

        public void SwitchCategory(LeaderboardCategory category)
        {
            currentCategory = category;
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (LeaderboardSystem.Instance == null) return;

            int catIdx = (int)currentCategory;
            titleText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(categoryColors[catIdx])}>{categoryNames[catIdx]} 랭킹</color>";

            // 탭 색상 업데이트
            for (int i = 0; i < tabButtons.Count; i++)
            {
                var img = tabButtons[i].GetComponent<Image>();
                img.color = (i == catIdx)
                    ? new Color(categoryColors[i].r * 0.5f, categoryColors[i].g * 0.5f, categoryColors[i].b * 0.5f, 0.9f)
                    : new Color(0.2f, 0.2f, 0.25f, 0.8f);
            }

            // 순위 목록
            var entries = LeaderboardSystem.Instance.GetTopEntries(currentCategory, 20);
            string list = "";

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                string rankStr = GetRankDisplay(e.rank);
                string scoreStr = LeaderboardSystem.Instance.ToString();
                bool isMe = e.playerName == currentPlayerName;

                if (isMe)
                    list += $"<color=#FFD700>>{rankStr} {e.playerName,-15} {scoreStr}</color>\n";
                else
                    list += $" {rankStr} {e.playerName,-15} {scoreStr}\n";
            }

            if (entries.Count == 0)
                list = "\n\n<color=#888888>아직 기록이 없습니다.</color>";

            rankListText.text = list;

            // 내 순위
            int myRank = LeaderboardSystem.Instance.GetPlayerRank(0, currentCategory);
            if (myRank > 0)
            {
                string myScore = "N/A";
                myRankText.text = $"<color=#FFD700>내 순위: {myRank}위 ({myScore})</color>";
            }
            else
            {
                myRankText.text = "<color=#888888>기록 없음</color>";
            }
        }

        private string GetRankDisplay(int rank)
        {
            return rank switch
            {
                1 => "<color=#FFD700>#1</color>",
                2 => "<color=#C0C0C0>#2</color>",
                3 => "<color=#CD7F32>#3</color>",
                _ => $"<color=#AAAAAA>#{rank}</color>"
            };
        }

        private PlayerStatsData FindLocalPlayerStats()
        {
            var netManager = Unity.Netcode.NetworkManager.Singleton;
            if (netManager != null && netManager.LocalClient != null && netManager.LocalClient.PlayerObject != null)
            {
                var statsManager = netManager.LocalClient.PlayerObject.GetComponent<PlayerStatsManager>();
                if (statsManager != null)
                    return statsManager.CurrentStats;
            }
            return null;
        }

        private void CreateUI()
        {
            var canvasObj = new GameObject("LeaderboardCanvas");
            canvasObj.transform.SetParent(transform, false);
            leaderboardCanvas = canvasObj.AddComponent<Canvas>();
            leaderboardCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            leaderboardCanvas.sortingOrder = 170;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            // 배경
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.6f);
            StretchRectTransform(bgObj);

            // 패널
            panelObj = new GameObject("LeaderboardPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            var panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.06f, 0.06f, 0.15f, 0.95f);
            var panelRt = panelObj.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(500, 550);

            // 타이틀
            titleText = CreateTextElement(panelObj.transform, "Title", "레벨 랭킹", 22, FontStyle.Bold,
                TextAnchor.MiddleCenter, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(10, -10), new Vector2(-10, -10), 35);
            titleText.color = new Color(1f, 0.84f, 0f);

            // 카테고리 탭 (5개)
            float tabWidth = 90f;
            float tabStartX = -(tabWidth * 5 + 4 * 4) / 2f + tabWidth / 2f;
            for (int i = 0; i < 5; i++)
            {
                int catIdx = i;
                var tabObj = new GameObject($"Tab_{categoryNames[i]}");
                tabObj.transform.SetParent(panelObj.transform, false);
                var tabImg = tabObj.AddComponent<Image>();
                tabImg.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);
                var tabBtn = tabObj.AddComponent<Button>();
                tabBtn.onClick.AddListener(() => SwitchCategory((LeaderboardCategory)catIdx));
                tabButtons.Add(tabBtn);

                var tabRt = tabObj.GetComponent<RectTransform>();
                tabRt.anchorMin = new Vector2(0.5f, 1);
                tabRt.anchorMax = new Vector2(0.5f, 1);
                tabRt.pivot = new Vector2(0.5f, 1);
                tabRt.sizeDelta = new Vector2(tabWidth, 28);
                tabRt.anchoredPosition = new Vector2(tabStartX + i * (tabWidth + 4), -50);

                var tabTextObj = new GameObject("Text");
                tabTextObj.transform.SetParent(tabObj.transform, false);
                var tabText = tabTextObj.AddComponent<Text>();
                tabText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                tabText.fontSize = 12;
                tabText.alignment = TextAnchor.MiddleCenter;
                tabText.color = Color.white;
                tabText.text = categoryNames[i];
                StretchRectTransform(tabTextObj);
            }

            // 순위 목록
            rankListText = CreateTextElement(panelObj.transform, "RankList", "", 14, FontStyle.Normal,
                TextAnchor.UpperLeft, new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(20, 50), new Vector2(-20, -85), 0);

            // 내 순위 (하단)
            myRankText = CreateTextElement(panelObj.transform, "MyRank", "", 15, FontStyle.Bold,
                TextAnchor.MiddleCenter, new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(10, 10), new Vector2(-10, 10), 35);

            // 닫기 버튼
            CreateButton(panelObj.transform, "CloseBtn", "X", new Vector2(1, 1),
                new Vector2(30, 30), new Vector2(-5, -5), Hide);

            // 안내 텍스트
            CreateTextElement(panelObj.transform, "Hint", "<color=#666666>1~5: 탭 전환 | ESC: 닫기</color>",
                11, FontStyle.Normal, TextAnchor.MiddleCenter,
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(10, 40), new Vector2(-10, 40), 15);

            panelObj.SetActive(false);
        }

        private Text CreateTextElement(Transform parent, string name, string content,
            int fontSize, FontStyle style, TextAnchor anchor,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, float height)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var text = obj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.supportRichText = true;
            text.text = content;

            var rt = obj.GetComponent<RectTransform>();
            if (height > 0)
            {
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.pivot = new Vector2(0.5f, anchorMin.y > 0.5f ? 1 : 0);
                rt.sizeDelta = new Vector2(0, height);
                rt.anchoredPosition = offsetMin;
            }
            else
            {
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.offsetMin = offsetMin;
                rt.offsetMax = offsetMax;
            }

            return text;
        }

        private void CreateButton(Transform parent, string name, string label, Vector2 anchor,
            Vector2 size, Vector2 pos, UnityEngine.Events.UnityAction action)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.4f, 0.2f, 0.2f);
            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(action);
            var rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            StretchRectTransform(textObj);
        }

        private void StretchRectTransform(GameObject obj)
        {
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
