using UnityEngine;
using UnityEngine.UI;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 던전 진행 HUD - 층수, 타이머, 몬스터 수 표시
    /// </summary>
    public class DungeonHUD : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private Text floorText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text monsterCountText;
        [SerializeField] private Text dungeonNameText;
        [SerializeField] private Image timerBackground;

        [Header("Warning Settings")]
        [SerializeField] private float warningTime = 30f;
        [SerializeField] private float criticalTime = 10f;
        [SerializeField] private Color normalTimerColor = Color.white;
        [SerializeField] private Color warningTimerColor = Color.yellow;
        [SerializeField] private Color criticalTimerColor = Color.red;

        private DungeonManager dungeonManager;
        private bool isFlashing = false;
        private float flashTimer = 0f;
        private int lastDisplayedSeconds = -1; // 타이머 문자열 캐싱용

        private void Start()
        {
            dungeonManager = DungeonManager.Instance;
            if (dungeonManager == null)
                dungeonManager = FindFirstObjectByType<DungeonManager>();

            if (dungeonManager != null)
            {
                dungeonManager.OnDungeonStarted += OnDungeonStarted;
                dungeonManager.OnFloorChanged += OnFloorChanged;
                dungeonManager.OnDungeonStateChanged += OnDungeonStateChanged;
            }

            if (hudPanel != null)
                hudPanel.SetActive(false);
        }

        private void Update()
        {
            if (dungeonManager == null || !dungeonManager.IsActive) return;

            UpdateTimer();
            UpdateFlashEffect();
        }

        private void OnDungeonStarted(DungeonInfo info)
        {
            if (hudPanel != null)
                hudPanel.SetActive(true);

            if (dungeonNameText != null)
                dungeonNameText.text = info.GetDungeonName();

            UpdateFloorDisplay();
        }

        private void OnFloorChanged(int newFloor)
        {
            UpdateFloorDisplay();
        }

        private void OnDungeonStateChanged(DungeonState state)
        {
            if (state == DungeonState.Completed || state == DungeonState.Failed)
            {
                if (hudPanel != null)
                    hudPanel.SetActive(false);
            }
        }

        private void UpdateFloorDisplay()
        {
            if (floorText != null && dungeonManager != null)
            {
                var info = dungeonManager.CurrentDungeon;
                floorText.text = $"{dungeonManager.CurrentFloor}F / {info.maxFloors}F";
            }
        }

        private void UpdateTimer()
        {
            float remaining = dungeonManager.CurrentFloorRemainingTime;

            if (timerText != null)
            {
                int totalSeconds = Mathf.FloorToInt(remaining);
                // 초가 변경된 경우에만 문자열 갱신 (GC 최적화)
                if (totalSeconds != lastDisplayedSeconds)
                {
                    lastDisplayedSeconds = totalSeconds;
                    int minutes = totalSeconds / 60;
                    int seconds = totalSeconds % 60;
                    timerText.text = $"{minutes:00}:{seconds:00}";
                }

                // 시간 경고 색상
                if (remaining <= criticalTime)
                {
                    timerText.color = criticalTimerColor;
                    isFlashing = true;
                }
                else if (remaining <= warningTime)
                {
                    timerText.color = warningTimerColor;
                    isFlashing = false;
                }
                else
                {
                    timerText.color = normalTimerColor;
                    isFlashing = false;
                }
            }
        }

        private void UpdateFlashEffect()
        {
            if (!isFlashing || timerText == null) return;

            flashTimer += Time.deltaTime * 4f;
            float alpha = Mathf.PingPong(flashTimer, 1f);
            Color c = criticalTimerColor;
            c.a = 0.3f + alpha * 0.7f;
            timerText.color = c;

            if (timerBackground != null)
            {
                Color bgColor = new Color(0.5f, 0f, 0f, alpha * 0.3f);
                timerBackground.color = bgColor;
            }
        }

        /// <summary>
        /// 몬스터 수 업데이트 (외부에서 호출)
        /// </summary>
        public void UpdateMonsterCount(int current, int total)
        {
            if (monsterCountText != null)
                monsterCountText.text = $"몬스터: {current}/{total}";
        }

        private void OnDestroy()
        {
            if (dungeonManager != null)
            {
                dungeonManager.OnDungeonStarted -= OnDungeonStarted;
                dungeonManager.OnFloorChanged -= OnFloorChanged;
                dungeonManager.OnDungeonStateChanged -= OnDungeonStateChanged;
            }
        }
    }
}
