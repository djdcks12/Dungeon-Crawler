using UnityEngine;
using UnityEngine.UI;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 보스 러쉬 UI - R키 토글 (로비), HUD (진행 중)
    /// </summary>
    public class BossRushUI : MonoBehaviour
    {
        private GameObject lobbyPanel;
        private GameObject hudPanel;
        private Text waveText;
        private Text bossNameText;
        private Image bossHPBar;
        private Text bossHPText;
        private Text timerText;
        private Text scoreText;
        private Text bestRecordText;
        private Button startButton;
        private Button forfeitButton;
        private bool isInitialized;

        private void Start()
        {
            CreateUI();
            isInitialized = true;
            lobbyPanel.SetActive(false);
            hudPanel.SetActive(false);

            if (BossRushSystem.Instance != null)
            {
                BossRushSystem.Instance.OnWaveStarted += OnWaveStarted;
                BossRushSystem.Instance.OnRestStarted += OnRestStarted;
                BossRushSystem.Instance.OnBossRushEnded += OnBossRushEnded;
                BossRushSystem.Instance.OnBossHPChanged += OnBossHPChanged;
            }
        }

        private void Update()
        {
            if (!isInitialized) return;

            // R키 → 로비 토글 (비활성 시에만)
            if (Input.GetKeyDown(KeyCode.R) && (BossRushSystem.Instance == null || !BossRushSystem.Instance.IsActive))
            {
                lobbyPanel.SetActive(!lobbyPanel.activeSelf);
                if (lobbyPanel.activeSelf) RefreshLobby();
            }

            if (lobbyPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                lobbyPanel.SetActive(false);

            // HUD 업데이트
            if (BossRushSystem.Instance != null && BossRushSystem.Instance.IsActive)
            {
                hudPanel.SetActive(true);
                lobbyPanel.SetActive(false);
                UpdateHUD();
            }
            else
            {
                hudPanel.SetActive(false);
            }
        }

        private void RefreshLobby()
        {
            if (BossRushSystem.Instance != null)
            {
                bestRecordText.text = $"최고 기록: 웨이브 {BossRushSystem.Instance.LocalBestWave}";
            }
        }

        private void UpdateHUD()
        {
            var sys = BossRushSystem.Instance;
            if (sys == null) return;

            waveText.text = $"웨이브 {sys.CurrentWave}";
            bossNameText.text = sys.IsResting ? "휴식 중..." : sys.CurrentBossName;

            float hpPct = sys.BossHPPercent;
            bossHPBar.fillAmount = hpPct;
            bossHPBar.color = hpPct > 0.5f ? Color.green : hpPct > 0.25f ? Color.yellow : Color.red;
            bossHPText.text = sys.IsResting ? "" : $"{sys.BossHP:N0} / {sys.BossMaxHP:N0}";

            float time = sys.RemainingTime;
            timerText.text = sys.IsResting ? $"다음 웨이브: {time:F0}s" : $"남은 시간: {time:F0}s";
            timerText.color = (!sys.IsResting && time < 30f) ? Color.red : Color.white;
        }

        private void OnWaveStarted(int wave)
        {
            if (wave > 0) hudPanel.SetActive(true);
        }

        private void OnRestStarted()
        {
            // 휴식 중 표시는 UpdateHUD에서 처리
        }

        private void OnBossRushEnded(int finalWave, long score)
        {
            hudPanel.SetActive(false);
            RefreshLobby();
        }

        private void OnBossHPChanged(long current, long max)
        {
            // UpdateHUD에서 처리
        }

        #region UI 생성

        private void CreateUI()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 148;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            CreateLobbyPanel();
            CreateHUDPanel();
        }

        private void CreateLobbyPanel()
        {
            lobbyPanel = new GameObject("LobbyPanel");
            lobbyPanel.transform.SetParent(transform, false);
            var rt = lobbyPanel.AddComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(450, 300);
            lobbyPanel.AddComponent<Image>().color = new Color(0.06f, 0.02f, 0.02f, 0.96f);

            CreateText(lobbyPanel.transform, "Title", "<color=#FF4444>보스 러쉬</color>", 22,
                TextAnchor.MiddleCenter, new Vector2(0, 120), new Vector2(400, 35));

            CreateText(lobbyPanel.transform, "Desc",
                "끝없이 강해지는 보스에 도전하세요!\n보스 간 30초 휴식, 5웨이브마다 보너스!\n입장료: 10,000G",
                13, TextAnchor.MiddleCenter, new Vector2(0, 55), new Vector2(380, 80));

            bestRecordText = CreateText(lobbyPanel.transform, "Best", "최고 기록: 웨이브 0", 15,
                TextAnchor.MiddleCenter, new Vector2(0, -10), new Vector2(300, 30)).GetComponent<Text>();

            startButton = CreateButton(lobbyPanel.transform, "StartBtn", "도전 시작!",
                new Vector2(0, -70), new Vector2(200, 45));
            startButton.GetComponent<Image>().color = new Color(0.5f, 0.1f, 0.1f, 1f);
            startButton.onClick.AddListener(() =>
            {
                if (BossRushSystem.Instance != null)
                    BossRushSystem.Instance.StartBossRushServerRpc();
            });

            var closeBtn = CreateButton(lobbyPanel.transform, "CloseBtn", "X",
                new Vector2(195, 120), new Vector2(40, 40));
            closeBtn.onClick.AddListener(() => lobbyPanel.SetActive(false));
        }

        private void CreateHUDPanel()
        {
            hudPanel = new GameObject("HUDPanel");
            hudPanel.transform.SetParent(transform, false);
            var rt = hudPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -10);
            rt.sizeDelta = new Vector2(500, 100);
            hudPanel.AddComponent<Image>().color = new Color(0.05f, 0.02f, 0.02f, 0.85f);

            waveText = CreateText(hudPanel.transform, "Wave", "웨이브 0", 18,
                TextAnchor.MiddleLeft, new Vector2(-180, 30), new Vector2(200, 30)).GetComponent<Text>();

            timerText = CreateText(hudPanel.transform, "Timer", "", 14,
                TextAnchor.MiddleRight, new Vector2(180, 30), new Vector2(200, 30)).GetComponent<Text>();

            bossNameText = CreateText(hudPanel.transform, "BossName", "", 16,
                TextAnchor.MiddleCenter, new Vector2(0, 5), new Vector2(400, 25)).GetComponent<Text>();

            // HP 바 배경
            var hpBg = new GameObject("HPBarBG");
            hpBg.transform.SetParent(hudPanel.transform, false);
            var bgRT = hpBg.AddComponent<RectTransform>();
            bgRT.anchoredPosition = new Vector2(0, -25);
            bgRT.sizeDelta = new Vector2(460, 20);
            hpBg.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // HP 바 필
            var hpFill = new GameObject("HPBarFill");
            hpFill.transform.SetParent(hpBg.transform, false);
            var fillRT = hpFill.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(1, 1);
            fillRT.sizeDelta = Vector2.zero;
            bossHPBar = hpFill.AddComponent<Image>();
            bossHPBar.color = Color.green;
            bossHPBar.type = Image.Type.Filled;
            bossHPBar.fillMethod = Image.FillMethod.Horizontal;

            bossHPText = CreateText(hpBg.transform, "HPText", "", 11,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(460, 20)).GetComponent<Text>();

            // 포기 버튼
            forfeitButton = CreateButton(hudPanel.transform, "Forfeit", "포기",
                new Vector2(210, 30), new Vector2(60, 25));
            forfeitButton.GetComponent<Image>().color = new Color(0.4f, 0.1f, 0.1f, 0.8f);
            forfeitButton.onClick.AddListener(() =>
            {
                if (BossRushSystem.Instance != null)
                    BossRushSystem.Instance.ForfeitBossRushServerRpc();
            });
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

        #endregion

        private void OnDestroy()
        {
            if (BossRushSystem.Instance != null)
            {
                BossRushSystem.Instance.OnWaveStarted -= OnWaveStarted;
                BossRushSystem.Instance.OnRestStarted -= OnRestStarted;
                BossRushSystem.Instance.OnBossRushEnded -= OnBossRushEnded;
                BossRushSystem.Instance.OnBossHPChanged -= OnBossHPChanged;
            }
        }
    }
}
