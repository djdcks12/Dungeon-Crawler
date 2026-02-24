using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 아레나 PvP UI - K키 토글
    /// 레이팅/랭크 표시, 매칭 버튼, 전적 표시
    /// </summary>
    public class ArenaUI : MonoBehaviour
    {
        private GameObject mainPanel;
        private Text rankText;
        private Text ratingText;
        private Text recordText;
        private Text streakText;
        private Text queueStatusText;
        private Button joinQueueButton;
        private Button leaveQueueButton;
        private Button closeButton;

        // 랭킹 리스트
        private Transform rankingContent;
        private List<GameObject> rankingEntries = new List<GameObject>();
        private List<ArenaPlayerData> rankingData = new List<ArenaPlayerData>();

        private bool isInitialized;

        private void Start()
        {
            CreateUI();
            isInitialized = true;
            mainPanel.SetActive(false);

            if (ArenaSystem.Instance != null)
            {
                ArenaSystem.Instance.OnArenaDataUpdated += OnArenaDataUpdated;
                ArenaSystem.Instance.OnQueueJoined += OnQueueJoined;
                ArenaSystem.Instance.OnQueueLeft += OnQueueLeft;
                ArenaSystem.Instance.OnMatchFound += OnMatchFound;
                ArenaSystem.Instance.OnMatchResult += OnMatchResult;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.K) && !IsInputFieldFocused())
                ToggleUI();

            // 대기 시간 업데이트
            if (mainPanel.activeSelf && ArenaSystem.Instance != null && ArenaSystem.Instance.InQueue)
            {
                float waitTime = ArenaSystem.Instance.QueueTime;
                queueStatusText.text = $"<color=#FFAA00>매칭 대기 중... {waitTime:F0}초</color>";
            }
        }

        private void ToggleUI()
        {
            if (!isInitialized) return;
            mainPanel.SetActive(!mainPanel.activeSelf);
            if (mainPanel.activeSelf)
            {
                RefreshUI();
                // 랭킹 요청
                if (ArenaSystem.Instance != null)
                {
                    rankingData.Clear();
                    ArenaSystem.Instance.RequestRankingServerRpc(10);
                }
            }
        }

        private void RefreshUI()
        {
            if (ArenaSystem.Instance == null) return;

            var data = ArenaSystem.Instance.LocalData;
            string rankColor = GetRankColor(data.rank);
            rankText.text = $"랭크: <color={rankColor}>{GetRankName(data.rank)}</color>";
            ratingText.text = $"레이팅: {data.rating}";
            recordText.text = $"전적: {data.wins}승 {data.losses}패 (시즌 {data.seasonWins}승)";
            streakText.text = data.winStreak > 0
                ? $"<color=#FFD700>{data.winStreak}연승 중!</color> (최고: {data.bestWinStreak}연승)"
                : $"최고 연승: {data.bestWinStreak}";

            bool inQueue = ArenaSystem.Instance.InQueue;
            bool inMatch = ArenaSystem.Instance.InMatch;
            joinQueueButton.gameObject.SetActive(!inQueue && !inMatch);
            leaveQueueButton.gameObject.SetActive(inQueue);

            if (!inQueue)
                queueStatusText.text = inMatch ? "<color=#FF4444>대전 중!</color>" : "";
        }

        #region 이벤트 핸들러

        private void OnArenaDataUpdated(ArenaPlayerData data)
        {
            // 랭킹 데이터 수집
            if (!rankingData.Exists(d => d.clientId == data.clientId))
                rankingData.Add(data);
            else
            {
                int idx = rankingData.FindIndex(d => d.clientId == data.clientId);
                rankingData[idx] = data;
            }

            RefreshRankingList();
            RefreshUI();
        }

        private void OnQueueJoined() => RefreshUI();
        private void OnQueueLeft() => RefreshUI();
        private void OnMatchFound(ulong opponentId) => RefreshUI();
        private void OnMatchResult(bool win, int ratingChange) => RefreshUI();

        #endregion

        #region 랭킹 표시

        private void RefreshRankingList()
        {
            foreach (var entry in rankingEntries)
                if (entry != null) Destroy(entry);
            rankingEntries.Clear();

            rankingData.Sort((a, b) => b.rating.CompareTo(a.rating));

            int rank = 1;
            foreach (var data in rankingData)
            {
                var entry = new GameObject("RankEntry");
                entry.transform.SetParent(rankingContent, false);
                var rt = entry.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(460, 30);
                entry.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f, 0.9f);

                string rankColor = GetRankColor(data.rank);
                string nameColor = data.clientId == Unity.Netcode.NetworkManager.Singleton?.LocalClientId
                    ? "#FFD700" : "#FFFFFF";
                string text = $"#{rank} <color={nameColor}>{data.playerName}</color> " +
                    $"<color={rankColor}>[{GetRankName(data.rank)}]</color> " +
                    $"레이팅: {data.rating} ({data.wins}승 {data.losses}패)";

                CreateText(entry.transform, "Text", text, 12, TextAnchor.MiddleLeft,
                    new Vector2(10, 0), new Vector2(440, 25));

                rankingEntries.Add(entry);
                rank++;
            }
        }

        #endregion

        #region UI 생성

        private void CreateUI()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 125;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            // 메인 패널
            mainPanel = CreatePanel(transform, "ArenaPanel", new Vector2(500, 500));

            // 제목
            CreateText(mainPanel.transform, "Title", "아레나", 22, TextAnchor.MiddleCenter,
                new Vector2(0, 220), new Vector2(300, 40));

            closeButton = CreateButton(mainPanel.transform, "CloseBtn", "X",
                new Vector2(220, 220), new Vector2(40, 40));
            closeButton.onClick.AddListener(() => mainPanel.SetActive(false));

            // 내 정보
            rankText = CreateText(mainPanel.transform, "Rank", "랭크: Bronze", 18, TextAnchor.MiddleCenter,
                new Vector2(0, 175), new Vector2(400, 30)).GetComponent<Text>();
            ratingText = CreateText(mainPanel.transform, "Rating", "레이팅: 1000", 16, TextAnchor.MiddleCenter,
                new Vector2(0, 150), new Vector2(400, 25)).GetComponent<Text>();
            recordText = CreateText(mainPanel.transform, "Record", "전적: 0승 0패", 14, TextAnchor.MiddleCenter,
                new Vector2(0, 125), new Vector2(400, 25)).GetComponent<Text>();
            streakText = CreateText(mainPanel.transform, "Streak", "", 14, TextAnchor.MiddleCenter,
                new Vector2(0, 102), new Vector2(400, 25)).GetComponent<Text>();

            // 매칭 버튼
            joinQueueButton = CreateButton(mainPanel.transform, "JoinQueueBtn", "매칭 시작",
                new Vector2(-80, 70), new Vector2(140, 40));
            joinQueueButton.onClick.AddListener(() =>
            {
                if (ArenaSystem.Instance != null) ArenaSystem.Instance.JoinQueueServerRpc();
            });
            joinQueueButton.GetComponent<Image>().color = new Color(0.2f, 0.4f, 0.2f);

            leaveQueueButton = CreateButton(mainPanel.transform, "LeaveQueueBtn", "매칭 취소",
                new Vector2(-80, 70), new Vector2(140, 40));
            leaveQueueButton.onClick.AddListener(() =>
            {
                if (ArenaSystem.Instance != null) ArenaSystem.Instance.LeaveQueueServerRpc();
            });
            leaveQueueButton.GetComponent<Image>().color = new Color(0.4f, 0.2f, 0.2f);

            queueStatusText = CreateText(mainPanel.transform, "QueueStatus", "", 14, TextAnchor.MiddleCenter,
                new Vector2(80, 70), new Vector2(200, 30)).GetComponent<Text>();

            // 랭킹 리스트
            CreateText(mainPanel.transform, "RankingTitle", "랭킹 TOP 10", 16, TextAnchor.MiddleCenter,
                new Vector2(0, 35), new Vector2(300, 25));

            var scrollObj = CreateScrollView(mainPanel.transform, "RankingScroll",
                new Vector2(0, -80), new Vector2(480, 200));
            rankingContent = scrollObj.transform.Find("Viewport/Content");
        }

        #endregion

        #region UI 헬퍼

        private string GetRankName(ArenaRank rank)
        {
            return rank switch
            {
                ArenaRank.Bronze => "브론즈",
                ArenaRank.Silver => "실버",
                ArenaRank.Gold => "골드",
                ArenaRank.Platinum => "플래티넘",
                ArenaRank.Diamond => "다이아몬드",
                ArenaRank.Master => "마스터",
                ArenaRank.Grandmaster => "그랜드마스터",
                _ => "없음"
            };
        }

        private string GetRankColor(ArenaRank rank)
        {
            return rank switch
            {
                ArenaRank.Bronze => "#CD7F32",
                ArenaRank.Silver => "#C0C0C0",
                ArenaRank.Gold => "#FFD700",
                ArenaRank.Platinum => "#00CED1",
                ArenaRank.Diamond => "#00BFFF",
                ArenaRank.Master => "#FF4500",
                ArenaRank.Grandmaster => "#FF00FF",
                _ => "#888888"
            };
        }

        private bool IsInputFieldFocused()
        {
            var selected = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
            return selected != null && selected.GetComponent<InputField>() != null;
        }

        private GameObject CreatePanel(Transform parent, string name, Vector2 size)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
            panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
            return panel;
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
            txt.fontSize = 14;
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
            scroll.vertical = true;
            return scrollObj;
        }

        #endregion

        private void OnDestroy()
        {
            if (ArenaSystem.Instance != null)
            {
                ArenaSystem.Instance.OnArenaDataUpdated -= OnArenaDataUpdated;
                ArenaSystem.Instance.OnQueueJoined -= OnQueueJoined;
                ArenaSystem.Instance.OnQueueLeft -= OnQueueLeft;
                ArenaSystem.Instance.OnMatchFound -= OnMatchFound;
                ArenaSystem.Instance.OnMatchResult -= OnMatchResult;
            }
        }
    }
}
