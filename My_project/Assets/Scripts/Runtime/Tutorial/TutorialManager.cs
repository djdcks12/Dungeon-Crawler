using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 튜토리얼 매니저
    /// 단계별 튜토리얼, 화살표/하이라이트, 완료 추적
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        [Header("설정")]
        [SerializeField] private bool autoStartTutorial = true;
        [SerializeField] private float stepDelay = 0.5f;

        // 튜토리얼 상태
        private int currentStepIndex = -1;
        private bool tutorialActive = false;
        private bool waitingForAction = false;
        private List<TutorialStep> steps = new List<TutorialStep>();

        // UI 요소
        private Canvas tutorialCanvas;
        private GameObject panelObj;
        private Text titleText;
        private Text descriptionText;
        private Text progressText;
        private Button nextButton;
        private Button skipButton;
        private Image highlightOverlay;

        // PlayerPrefs 키
        private const string KEY_TUTORIAL_COMPLETED = "Tutorial_Completed";
        private const string KEY_TUTORIAL_STEP = "Tutorial_LastStep";

        // 이벤트
        public System.Action<int> OnStepCompleted;
        public System.Action OnTutorialCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeTutorialSteps();
            CreateUI();
        }

        private void Start()
        {
            if (autoStartTutorial && !IsTutorialCompleted())
            {
                // 약간의 딜레이 후 시작
                Invoke(nameof(StartTutorial), 2f);
            }
        }

        private void Update()
        {
            if (!tutorialActive) return;

            // 자동 완료 조건 체크
            if (waitingForAction && currentStepIndex >= 0 && currentStepIndex < steps.Count)
            {
                var step = steps[currentStepIndex];
                if (CheckStepCompletion(step))
                {
                    CompleteCurrentStep();
                }
            }
        }

        /// <summary>
        /// 튜토리얼 시작
        /// </summary>
        public void StartTutorial()
        {
            if (tutorialActive) return;

            tutorialActive = true;
            currentStepIndex = -1;

            if (panelObj != null)
                panelObj.SetActive(true);

            NextStep();
        }

        /// <summary>
        /// 튜토리얼 건너뛰기
        /// </summary>
        public void SkipTutorial()
        {
            tutorialActive = false;
            currentStepIndex = steps.Count;

            if (panelObj != null)
                panelObj.SetActive(false);

            PlayerPrefs.SetInt(KEY_TUTORIAL_COMPLETED, 1);
            PlayerPrefs.Save();

            OnTutorialCompleted?.Invoke();
            Debug.Log("[Tutorial] Tutorial skipped");
        }

        /// <summary>
        /// 다음 단계로
        /// </summary>
        public void NextStep()
        {
            currentStepIndex++;

            if (currentStepIndex >= steps.Count)
            {
                // 튜토리얼 완료
                CompleteTutorial();
                return;
            }

            var step = steps[currentStepIndex];
            ShowStep(step);

            PlayerPrefs.SetInt(KEY_TUTORIAL_STEP, currentStepIndex);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 현재 단계 완료
        /// </summary>
        public void CompleteCurrentStep()
        {
            waitingForAction = false;
            OnStepCompleted?.Invoke(currentStepIndex);

            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ShowNotification(
                    $"튜토리얼 {currentStepIndex + 1}/{steps.Count} 완료!",
                    NotificationType.System);
            }

            // 다음 단계로 자동 진행
            Invoke(nameof(NextStep), stepDelay);
        }

        /// <summary>
        /// 튜토리얼 완료
        /// </summary>
        private void CompleteTutorial()
        {
            tutorialActive = false;

            if (panelObj != null)
                panelObj.SetActive(false);

            PlayerPrefs.SetInt(KEY_TUTORIAL_COMPLETED, 1);
            PlayerPrefs.Save();

            OnTutorialCompleted?.Invoke();

            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ShowNotification(
                    "튜토리얼 완료! 던전 크롤링을 시작하세요!",
                    NotificationType.Achievement);
            }

            Debug.Log("[Tutorial] Tutorial completed!");
        }

        /// <summary>
        /// 튜토리얼 완료 여부
        /// </summary>
        public bool IsTutorialCompleted()
        {
            return PlayerPrefs.GetInt(KEY_TUTORIAL_COMPLETED, 0) == 1;
        }

        /// <summary>
        /// 튜토리얼 리셋
        /// </summary>
        public void ResetTutorial()
        {
            PlayerPrefs.DeleteKey(KEY_TUTORIAL_COMPLETED);
            PlayerPrefs.DeleteKey(KEY_TUTORIAL_STEP);
            PlayerPrefs.Save();
            currentStepIndex = -1;
            tutorialActive = false;
        }

        /// <summary>
        /// 단계 표시
        /// </summary>
        private void ShowStep(TutorialStep step)
        {
            if (titleText != null)
                titleText.text = step.title;
            if (descriptionText != null)
                descriptionText.text = step.description;
            if (progressText != null)
                progressText.text = $"{currentStepIndex + 1} / {steps.Count}";

            // 자동 완료 조건이 있으면 대기
            if (step.completionType != TutorialCompletionType.Manual)
            {
                waitingForAction = true;
                if (nextButton != null) nextButton.gameObject.SetActive(false);
            }
            else
            {
                waitingForAction = false;
                if (nextButton != null) nextButton.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 단계 완료 조건 체크
        /// </summary>
        private bool CheckStepCompletion(TutorialStep step)
        {
            switch (step.completionType)
            {
                case TutorialCompletionType.PressKey:
                    return Input.GetKeyDown(step.requiredKey);

                case TutorialCompletionType.MoveDistance:
                    var player = FindFirstObjectByType<PlayerController>();
                    if (player != null)
                    {
                        float dist = Vector3.Distance(player.transform.position, step.startPosition);
                        return dist >= step.requiredDistance;
                    }
                    return false;

                case TutorialCompletionType.KillMonster:
                    // 전투 로그에서 처치 확인 (간단한 체크)
                    return step.conditionMet;

                case TutorialCompletionType.OpenUI:
                    return step.conditionMet;

                case TutorialCompletionType.Manual:
                default:
                    return false;
            }
        }

        /// <summary>
        /// 외부에서 튜토리얼 조건 충족 알림
        /// </summary>
        public void NotifyConditionMet(string conditionId)
        {
            if (!tutorialActive || currentStepIndex < 0 || currentStepIndex >= steps.Count)
                return;

            var step = steps[currentStepIndex];
            if (step.conditionId == conditionId)
            {
                step.conditionMet = true;
                steps[currentStepIndex] = step;
            }
        }

        /// <summary>
        /// 튜토리얼 단계 초기화
        /// </summary>
        private void InitializeTutorialSteps()
        {
            steps.Clear();

            // Step 1: 이동
            steps.Add(new TutorialStep
            {
                title = "이동하기",
                description = "WASD 키 또는 화살표 키로 캐릭터를 이동합니다.\n조금 움직여 보세요!",
                completionType = TutorialCompletionType.PressKey,
                requiredKey = KeyCode.W
            });

            // Step 2: 기본 공격
            steps.Add(new TutorialStep
            {
                title = "기본 공격",
                description = "마우스 좌클릭으로 기본 공격을 합니다.\n적에게 가까이 다가가서 공격해 보세요!",
                completionType = TutorialCompletionType.PressKey,
                requiredKey = KeyCode.Mouse0
            });

            // Step 3: 스킬 사용
            steps.Add(new TutorialStep
            {
                title = "스킬 사용",
                description = "숫자키 1~5로 학습한 스킬을 사용합니다.\nNPC에서 스킬을 배운 뒤 사용해 보세요!",
                completionType = TutorialCompletionType.Manual
            });

            // Step 4: 인벤토리
            steps.Add(new TutorialStep
            {
                title = "인벤토리",
                description = "I 키를 눌러 인벤토리를 열 수 있습니다.\n아이템을 관리하고 장비를 착용해 보세요!",
                completionType = TutorialCompletionType.PressKey,
                requiredKey = KeyCode.I
            });

            // Step 5: 퀘스트
            steps.Add(new TutorialStep
            {
                title = "퀘스트",
                description = "J 키를 눌러 퀘스트 목록을 확인합니다.\nNPC(! 표시)에게 F 키로 말을 걸어 퀘스트를 받으세요!",
                completionType = TutorialCompletionType.PressKey,
                requiredKey = KeyCode.J
            });

            // Step 6: NPC 상호작용
            steps.Add(new TutorialStep
            {
                title = "NPC 상호작용",
                description = "NPC에게 가까이 다가가 F 키로 상호작용합니다.\n상인 NPC에서 아이템을 구매하거나\n스킬 마스터 NPC에서 스킬을 배울 수 있습니다.",
                completionType = TutorialCompletionType.Manual
            });

            // Step 7: 채팅
            steps.Add(new TutorialStep
            {
                title = "채팅",
                description = "Enter 키를 눌러 채팅을 열 수 있습니다.\n/help 명령어로 사용 가능한 명령어를 확인하세요!",
                completionType = TutorialCompletionType.PressKey,
                requiredKey = KeyCode.Return
            });

            // Step 8: 던전 입장
            steps.Add(new TutorialStep
            {
                title = "던전 입장",
                description = "던전 입구에서 F 키를 눌러 던전에 입장합니다.\n파티를 구성하면 더 쉽게 클리어할 수 있습니다!",
                completionType = TutorialCompletionType.Manual
            });

            // Step 9: 자동 전투
            steps.Add(new TutorialStep
            {
                title = "자동 전투",
                description = "V 키를 눌러 자동 전투를 켜고 끌 수 있습니다.\n자동 전투 중에는 가까운 적을 자동으로 공격합니다.",
                completionType = TutorialCompletionType.PressKey,
                requiredKey = KeyCode.V
            });

            // Step 10: 완료
            steps.Add(new TutorialStep
            {
                title = "준비 완료!",
                description = "기본적인 조작법을 모두 배웠습니다!\n\n" +
                    "조작 요약:\n" +
                    "WASD: 이동 | 좌클릭: 공격 | 1-5: 스킬\n" +
                    "I: 인벤토리 | J: 퀘스트 | M: 미니맵\n" +
                    "F: NPC 상호작용 | V: 자동전투 | H: 도움말\n" +
                    "\n던전을 탐험하고 강력한 장비를 모으세요!",
                completionType = TutorialCompletionType.Manual
            });
        }

        /// <summary>
        /// UI 생성
        /// </summary>
        private void CreateUI()
        {
            // Canvas
            var canvasObj = new GameObject("TutorialCanvas");
            canvasObj.transform.SetParent(transform, false);
            tutorialCanvas = canvasObj.AddComponent<Canvas>();
            tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            tutorialCanvas.sortingOrder = 150;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            // 패널
            panelObj = new GameObject("TutorialPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            var panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.05f, 0.05f, 0.15f, 0.9f);

            var panelRt = panelObj.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0f);
            panelRt.anchorMax = new Vector2(0.5f, 0f);
            panelRt.pivot = new Vector2(0.5f, 0f);
            panelRt.sizeDelta = new Vector2(500, 200);
            panelRt.anchoredPosition = new Vector2(0, 20);

            // 제목
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panelObj.transform, false);
            titleText = titleObj.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 22;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1f, 0.84f, 0f);
            var titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1);
            titleRt.sizeDelta = new Vector2(0, 35);
            titleRt.anchoredPosition = new Vector2(0, -5);

            // 설명
            var descObj = new GameObject("Description");
            descObj.transform.SetParent(panelObj.transform, false);
            descriptionText = descObj.AddComponent<Text>();
            descriptionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descriptionText.fontSize = 15;
            descriptionText.alignment = TextAnchor.UpperCenter;
            descriptionText.color = Color.white;
            var descRt = descObj.GetComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0, 0);
            descRt.anchorMax = new Vector2(1, 1);
            descRt.offsetMin = new Vector2(15, 45);
            descRt.offsetMax = new Vector2(-15, -40);

            // 진행도
            var progObj = new GameObject("Progress");
            progObj.transform.SetParent(panelObj.transform, false);
            progressText = progObj.AddComponent<Text>();
            progressText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            progressText.fontSize = 14;
            progressText.alignment = TextAnchor.MiddleLeft;
            progressText.color = new Color(0.7f, 0.7f, 0.7f);
            var progRt = progObj.GetComponent<RectTransform>();
            progRt.anchorMin = new Vector2(0, 0);
            progRt.anchorMax = new Vector2(0.3f, 0);
            progRt.pivot = new Vector2(0, 0);
            progRt.sizeDelta = new Vector2(0, 30);
            progRt.anchoredPosition = new Vector2(10, 5);

            // 다음 버튼
            var nextObj = new GameObject("NextButton");
            nextObj.transform.SetParent(panelObj.transform, false);
            var nextBg = nextObj.AddComponent<Image>();
            nextBg.color = new Color(0.2f, 0.5f, 0.2f);
            nextButton = nextObj.AddComponent<Button>();
            nextButton.onClick.AddListener(NextStep);
            var nextRt = nextObj.GetComponent<RectTransform>();
            nextRt.anchorMin = new Vector2(1, 0);
            nextRt.anchorMax = new Vector2(1, 0);
            nextRt.pivot = new Vector2(1, 0);
            nextRt.sizeDelta = new Vector2(80, 30);
            nextRt.anchoredPosition = new Vector2(-10, 5);

            var nextText = new GameObject("Text").AddComponent<Text>();
            nextText.transform.SetParent(nextObj.transform, false);
            nextText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nextText.fontSize = 14;
            nextText.alignment = TextAnchor.MiddleCenter;
            nextText.color = Color.white;
            nextText.text = "다음";
            var ntRt = nextText.GetComponent<RectTransform>();
            ntRt.anchorMin = Vector2.zero;
            ntRt.anchorMax = Vector2.one;
            ntRt.offsetMin = Vector2.zero;
            ntRt.offsetMax = Vector2.zero;

            // 건너뛰기 버튼
            var skipObj = new GameObject("SkipButton");
            skipObj.transform.SetParent(panelObj.transform, false);
            var skipBg = skipObj.AddComponent<Image>();
            skipBg.color = new Color(0.5f, 0.2f, 0.2f);
            skipButton = skipObj.AddComponent<Button>();
            skipButton.onClick.AddListener(SkipTutorial);
            var skipRt = skipObj.GetComponent<RectTransform>();
            skipRt.anchorMin = new Vector2(1, 0);
            skipRt.anchorMax = new Vector2(1, 0);
            skipRt.pivot = new Vector2(1, 0);
            skipRt.sizeDelta = new Vector2(80, 30);
            skipRt.anchoredPosition = new Vector2(-100, 5);

            var skipText = new GameObject("Text").AddComponent<Text>();
            skipText.transform.SetParent(skipObj.transform, false);
            skipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            skipText.fontSize = 14;
            skipText.alignment = TextAnchor.MiddleCenter;
            skipText.color = Color.white;
            skipText.text = "건너뛰기";
            var stRt = skipText.GetComponent<RectTransform>();
            stRt.anchorMin = Vector2.zero;
            stRt.anchorMax = Vector2.one;
            stRt.offsetMin = Vector2.zero;
            stRt.offsetMax = Vector2.zero;

            panelObj.SetActive(false);
        }

        private void OnDestroy()
        {
            CancelInvoke();
            if (nextButton != null) nextButton.onClick.RemoveAllListeners();
            if (skipButton != null) skipButton.onClick.RemoveAllListeners();

            if (Instance == this)
                Instance = null;
        }
    }

    /// <summary>
    /// 튜토리얼 단계 데이터
    /// </summary>
    [System.Serializable]
    public struct TutorialStep
    {
        public string title;
        public string description;
        public TutorialCompletionType completionType;
        public KeyCode requiredKey;
        public float requiredDistance;
        public Vector3 startPosition;
        public string conditionId;
        public bool conditionMet;
    }

    public enum TutorialCompletionType
    {
        Manual,         // 수동 (다음 버튼)
        PressKey,       // 특정 키 입력
        MoveDistance,    // 일정 거리 이동
        KillMonster,    // 몬스터 처치
        OpenUI          // UI 열기
    }
}
