using UnityEngine;
using UnityEngine.UI;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 보스 전용 HP 바 UI - 화면 상단 대형 HP 바
    /// </summary>
    public class BossHealthBarUI : MonoBehaviour
    {
        public static BossHealthBarUI Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject bossBarPanel;
        [SerializeField] private Image healthFill;
        [SerializeField] private Image healthFillDelay;
        [SerializeField] private Text bossNameText;
        [SerializeField] private Text phaseText;
        [SerializeField] private Text healthPercentText;

        [Header("Settings")]
        [SerializeField] private float delayFillSpeed = 2f;
        [SerializeField] private Color normalHealthColor = new Color(0.8f, 0.1f, 0.1f);
        [SerializeField] private Color enrageHealthColor = new Color(1f, 0.3f, 0f);

        private BossMonsterAI currentBoss;
        private MonsterEntity currentBossEntity;
        private float targetFillAmount = 1f;
        private float delayedFillAmount = 1f;
        private bool isActive = false;
        private bool autoCreated = false;
        private int lastHPPercent = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (bossBarPanel == null)
                CreateBossBarUI();

            if (bossBarPanel != null)
                bossBarPanel.SetActive(false);
        }

        private void Update()
        {
            if (!isActive || currentBossEntity == null) return;

            // HP 비율 업데이트
            float healthRatio = (float)currentBossEntity.CurrentHP / currentBossEntity.MaxHP;
            targetFillAmount = Mathf.Clamp01(healthRatio);

            if (healthFill != null)
                healthFill.fillAmount = targetFillAmount;

            // 지연 HP 바 (데미지 시각화)
            if (healthFillDelay != null)
            {
                if (delayedFillAmount > targetFillAmount)
                {
                    delayedFillAmount -= Time.deltaTime * delayFillSpeed;
                    delayedFillAmount = Mathf.Max(delayedFillAmount, targetFillAmount);
                }
                else
                {
                    delayedFillAmount = targetFillAmount;
                }
                healthFillDelay.fillAmount = delayedFillAmount;
            }

            // HP 퍼센트 텍스트 (변경시만 업데이트)
            int hpPct = (int)(healthRatio * 100f);
            if (healthPercentText != null && hpPct != lastHPPercent)
            {
                lastHPPercent = hpPct;
                healthPercentText.text = $"{hpPct}%";
            }

            // 광폭화 색상 변경
            if (currentBoss != null && currentBoss.GetBossState() == BossState.Enraged)
            {
                if (healthFill != null)
                    healthFill.color = enrageHealthColor;
            }

            // 보스 사망 체크
            if (currentBossEntity.IsDead)
            {
                HideBossBar();
            }
        }

        /// <summary>
        /// 보스 HP 바 표시
        /// </summary>
        public void ShowBossBar(BossMonsterAI boss)
        {
            if (boss == null) return;

            currentBoss = boss;
            currentBossEntity = boss.GetComponent<MonsterEntity>();

            if (currentBossEntity == null) return;

            // UI 세팅
            if (bossNameText != null)
                bossNameText.text = boss.BossName;

            if (phaseText != null)
                phaseText.text = $"Phase 1/{boss.PhaseCount}";

            if (healthFill != null)
            {
                healthFill.fillAmount = 1f;
                healthFill.color = normalHealthColor;
            }

            if (healthFillDelay != null)
                healthFillDelay.fillAmount = 1f;

            targetFillAmount = 1f;
            delayedFillAmount = 1f;
            isActive = true;

            if (bossBarPanel != null)
                bossBarPanel.SetActive(true);
        }

        /// <summary>
        /// 보스 HP 바 숨기기
        /// </summary>
        public void HideBossBar()
        {
            isActive = false;
            currentBoss = null;
            currentBossEntity = null;

            if (bossBarPanel != null)
                bossBarPanel.SetActive(false);
        }

        /// <summary>
        /// 페이즈 변경 알림
        /// </summary>
        public void OnPhaseChanged(int newPhase, int maxPhase)
        {
            if (phaseText != null)
                phaseText.text = $"Phase {newPhase}/{maxPhase}";
        }

        /// <summary>
        /// 자동 UI 생성
        /// </summary>
        private void CreateBossBarUI()
        {
            autoCreated = true;

            // 패널 생성
            bossBarPanel = new GameObject("BossBarPanel");
            bossBarPanel.transform.SetParent(transform, false);

            var panelRT = bossBarPanel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.15f, 0.9f);
            panelRT.anchorMax = new Vector2(0.85f, 0.97f);
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            // 배경
            var bg = bossBarPanel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            // 보스 이름
            var nameObj = new GameObject("BossName");
            nameObj.transform.SetParent(bossBarPanel.transform, false);
            bossNameText = nameObj.AddComponent<Text>();
            bossNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bossNameText.fontSize = 18;
            bossNameText.fontStyle = FontStyle.Bold;
            bossNameText.color = new Color(1f, 0.85f, 0.3f);
            bossNameText.alignment = TextAnchor.MiddleCenter;
            var nameRT = nameObj.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0f, 0.55f);
            nameRT.anchorMax = new Vector2(0.7f, 1f);
            nameRT.offsetMin = new Vector2(10, 0);
            nameRT.offsetMax = new Vector2(-5, -2);

            // 페이즈 텍스트
            var phaseObj = new GameObject("PhaseText");
            phaseObj.transform.SetParent(bossBarPanel.transform, false);
            phaseText = phaseObj.AddComponent<Text>();
            phaseText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            phaseText.fontSize = 14;
            phaseText.color = Color.white;
            phaseText.alignment = TextAnchor.MiddleRight;
            var phaseRT = phaseObj.GetComponent<RectTransform>();
            phaseRT.anchorMin = new Vector2(0.7f, 0.55f);
            phaseRT.anchorMax = new Vector2(1f, 1f);
            phaseRT.offsetMin = new Vector2(5, 0);
            phaseRT.offsetMax = new Vector2(-10, -2);

            // HP 바 배경
            var hpBgObj = new GameObject("HPBarBG");
            hpBgObj.transform.SetParent(bossBarPanel.transform, false);
            var hpBg = hpBgObj.AddComponent<Image>();
            hpBg.color = new Color(0.2f, 0.05f, 0.05f);
            var hpBgRT = hpBgObj.GetComponent<RectTransform>();
            hpBgRT.anchorMin = new Vector2(0.02f, 0.05f);
            hpBgRT.anchorMax = new Vector2(0.98f, 0.5f);
            hpBgRT.offsetMin = Vector2.zero;
            hpBgRT.offsetMax = Vector2.zero;

            // 지연 HP 바 (흰색/노란색)
            var delayObj = new GameObject("HPFillDelay");
            delayObj.transform.SetParent(hpBgObj.transform, false);
            healthFillDelay = delayObj.AddComponent<Image>();
            healthFillDelay.color = new Color(1f, 0.85f, 0.3f, 0.7f);
            healthFillDelay.type = Image.Type.Filled;
            healthFillDelay.fillMethod = Image.FillMethod.Horizontal;
            healthFillDelay.fillAmount = 1f;
            var delayRT = delayObj.GetComponent<RectTransform>();
            delayRT.anchorMin = Vector2.zero;
            delayRT.anchorMax = Vector2.one;
            delayRT.offsetMin = Vector2.zero;
            delayRT.offsetMax = Vector2.zero;

            // HP 바 (빨간색)
            var fillObj = new GameObject("HPFill");
            fillObj.transform.SetParent(hpBgObj.transform, false);
            healthFill = fillObj.AddComponent<Image>();
            healthFill.color = normalHealthColor;
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;
            healthFill.fillAmount = 1f;
            var fillRT = fillObj.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;

            // HP 퍼센트 텍스트
            var percentObj = new GameObject("HPPercent");
            percentObj.transform.SetParent(hpBgObj.transform, false);
            healthPercentText = percentObj.AddComponent<Text>();
            healthPercentText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            healthPercentText.fontSize = 14;
            healthPercentText.fontStyle = FontStyle.Bold;
            healthPercentText.color = Color.white;
            healthPercentText.alignment = TextAnchor.MiddleCenter;
            var outline = percentObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1, -1);
            var percentRT = percentObj.GetComponent<RectTransform>();
            percentRT.anchorMin = Vector2.zero;
            percentRT.anchorMax = Vector2.one;
            percentRT.offsetMin = Vector2.zero;
            percentRT.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
