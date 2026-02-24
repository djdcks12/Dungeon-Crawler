using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 캐릭터 생성 UI 시스템
    /// 5단계: 종족 → 무기군 → 직업 → 이름 → 확인
    /// NewCharacterCreationManager와 연동
    /// </summary>
    public class CharacterCreationUI : MonoBehaviour
    {
        [Header("Manager Reference")]
        [SerializeField] private NewCharacterCreationManager creationManager;

        [Header("UI Panels")]
        [SerializeField] private GameObject characterCreationPanel;
        [SerializeField] private GameObject raceSelectionPanel;
        [SerializeField] private GameObject weaponGroupSelectionPanel;
        [SerializeField] private GameObject jobSelectionPanel;
        [SerializeField] private GameObject nameInputPanel;
        [SerializeField] private GameObject confirmationPanel;

        [Header("Race Selection")]
        [SerializeField] private Button humanRaceButton;
        [SerializeField] private Button elfRaceButton;
        [SerializeField] private Button beastRaceButton;
        [SerializeField] private Text raceDescriptionText;
        [SerializeField] private Image racePreviewImage;

        [Header("WeaponGroup Selection")]
        [SerializeField] private Transform weaponGroupButtonContainer;
        [SerializeField] private Button weaponGroupButtonTemplate;
        [SerializeField] private Text weaponGroupDescriptionText;

        [Header("Job Selection")]
        [SerializeField] private Transform jobButtonContainer;
        [SerializeField] private Button jobButtonTemplate;
        [SerializeField] private Text jobDescriptionText;

        [Header("Name Input")]
        [SerializeField] private InputField characterNameInput;
        [SerializeField] private Button randomNameButton;
        [SerializeField] private Text nameValidationText;

        [Header("Confirmation")]
        [SerializeField] private Text confirmSummaryText;

        [Header("Navigation Buttons")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        [Header("Step Indicator")]
        [SerializeField] private Text stepIndicatorText;

        [Header("Settings")]
        [SerializeField] private int minNameLength = 3;
        [SerializeField] private int maxNameLength = 12;
        [SerializeField] private string[] randomNames = {
            "Adventurer", "Hero", "Warrior", "Mage", "Rogue", "Paladin",
            "Ranger", "Monk", "Sorcerer", "Barbarian", "Druid", "Bard"
        };

        // 상태 변수들
        private Race selectedRace = Race.Human;
        private WeaponGroup selectedWeaponGroup;
        private JobData selectedJob;
        private string selectedName = "";
        private int currentStep = 0; // 0: Race, 1: WeaponGroup, 2: Job, 3: Name, 4: Confirm

        // 동적 생성된 버튼 추적
        private List<GameObject> spawnedWeaponGroupButtons = new List<GameObject>();
        private List<GameObject> spawnedJobButtons = new List<GameObject>();

        // 이벤트
        public System.Action<CharacterCreationData> OnCharacterCreated;
        public System.Action OnCharacterCreationCancelled;

        private readonly string[] stepNames = { "종족 선택", "무기군 선택", "직업 선택", "이름 입력", "확인" };

        private void Start()
        {
            InitializeUI();

            // 템플릿 버튼 숨기기
            if (weaponGroupButtonTemplate != null)
                weaponGroupButtonTemplate.gameObject.SetActive(false);
            if (jobButtonTemplate != null)
                jobButtonTemplate.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            SubscribeToManagerEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromManagerEvents();
        }

        /// <summary>
        /// NewCharacterCreationManager 이벤트 구독
        /// </summary>
        private void SubscribeToManagerEvents()
        {
            if (creationManager == null) return;

            creationManager.OnRaceOptionsUpdated += OnRaceOptionsReceived;
            creationManager.OnWeaponGroupOptionsUpdated += OnWeaponGroupOptionsReceived;
            creationManager.OnJobOptionsUpdated += OnJobOptionsReceived;
            creationManager.OnCharacterCreated += OnCharacterCreationCompleted;
            creationManager.OnCreationError += OnCreationErrorReceived;
        }

        /// <summary>
        /// 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeFromManagerEvents()
        {
            if (creationManager == null) return;

            creationManager.OnRaceOptionsUpdated -= OnRaceOptionsReceived;
            creationManager.OnWeaponGroupOptionsUpdated -= OnWeaponGroupOptionsReceived;
            creationManager.OnJobOptionsUpdated -= OnJobOptionsReceived;
            creationManager.OnCharacterCreated -= OnCharacterCreationCompleted;
            creationManager.OnCreationError -= OnCreationErrorReceived;
        }

        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            // 종족 선택 버튼 이벤트
            if (humanRaceButton != null)
                humanRaceButton.onClick.AddListener(() => OnRaceButtonClicked(Race.Human));
            if (elfRaceButton != null)
                elfRaceButton.onClick.AddListener(() => OnRaceButtonClicked(Race.Elf));
            if (beastRaceButton != null)
                beastRaceButton.onClick.AddListener(() => OnRaceButtonClicked(Race.Beast));

            // 이름 관련 버튼
            if (randomNameButton != null)
                randomNameButton.onClick.AddListener(GenerateRandomName);
            if (characterNameInput != null)
                characterNameInput.onValueChanged.AddListener(OnNameInputChanged);

            // 네비게이션 버튼
            if (backButton != null)
                backButton.onClick.AddListener(GoToPreviousStep);
            if (nextButton != null)
                nextButton.onClick.AddListener(GoToNextStep);
            if (confirmButton != null)
                confirmButton.onClick.AddListener(ConfirmCharacterCreation);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(CancelCharacterCreation);
        }

        // ── Manager 이벤트 핸들러 ──

        private void OnRaceOptionsReceived(Race[] races)
        {
            // 종족 버튼 활성화/비활성화
            if (humanRaceButton != null)
                humanRaceButton.gameObject.SetActive(System.Array.IndexOf(races, Race.Human) >= 0);
            if (elfRaceButton != null)
                elfRaceButton.gameObject.SetActive(System.Array.IndexOf(races, Race.Elf) >= 0);
            if (beastRaceButton != null)
                beastRaceButton.gameObject.SetActive(System.Array.IndexOf(races, Race.Beast) >= 0);
        }

        private void OnWeaponGroupOptionsReceived(WeaponGroup[] weaponGroups)
        {
            PopulateWeaponGroupButtons(weaponGroups);
            SetCurrentStep(1);
        }

        private void OnJobOptionsReceived(JobData[] jobs)
        {
            PopulateJobButtons(jobs);
            SetCurrentStep(2);
        }

        private void OnCharacterCreationCompleted(CharacterCreationData data)
        {
            // 이름은 UI에서 관리하므로 여기서 설정
            data.characterName = selectedName;

            OnCharacterCreated?.Invoke(data);
            HideCharacterCreationUI();
            Debug.Log($"Character created: {selectedName} - {data.race} {data.jobType} ({data.weaponGroup})");
        }

        private void OnCreationErrorReceived(string errorMessage)
        {
            Debug.LogWarning($"Character creation error: {errorMessage}");

            // 에러 메시지를 현재 단계의 설명 텍스트에 표시
            switch (currentStep)
            {
                case 0:
                    if (raceDescriptionText != null)
                    {
                        raceDescriptionText.text = errorMessage;
                        raceDescriptionText.color = Color.red;
                    }
                    break;
                case 1:
                    if (weaponGroupDescriptionText != null)
                    {
                        weaponGroupDescriptionText.text = errorMessage;
                        weaponGroupDescriptionText.color = Color.red;
                    }
                    break;
                case 2:
                    if (jobDescriptionText != null)
                    {
                        jobDescriptionText.text = errorMessage;
                        jobDescriptionText.color = Color.red;
                    }
                    break;
            }
        }

        // ── 단계 관리 ──

        /// <summary>
        /// 현재 단계 설정
        /// </summary>
        private void SetCurrentStep(int step)
        {
            currentStep = step;

            // 모든 패널 비활성화 후 해당 패널만 활성화
            if (raceSelectionPanel != null)
                raceSelectionPanel.SetActive(step == 0);
            if (weaponGroupSelectionPanel != null)
                weaponGroupSelectionPanel.SetActive(step == 1);
            if (jobSelectionPanel != null)
                jobSelectionPanel.SetActive(step == 2);
            if (nameInputPanel != null)
                nameInputPanel.SetActive(step == 3);
            if (confirmationPanel != null)
                confirmationPanel.SetActive(step == 4);

            // 단계 표시 업데이트
            if (stepIndicatorText != null && step < stepNames.Length)
            {
                stepIndicatorText.text = $"단계 {step + 1}/5: {stepNames[step]}";
            }

            // 네비게이션 버튼 상태 업데이트
            UpdateNavigationButtons();

            // 확인 단계일 때 요약 정보 표시
            if (step == 4)
            {
                UpdateConfirmationSummary();
            }
        }

        // ── 종족 선택 (Step 0) ──

        private void OnRaceButtonClicked(Race race)
        {
            selectedRace = race;
            UpdateRaceDisplay();
            UpdateRaceButtonStates();

            // Manager에 종족 선택 전달 → Manager가 OnWeaponGroupOptionsUpdated 발생
            if (creationManager != null)
            {
                creationManager.SelectRace(race);
            }
            else
            {
                // Manager 없이 독립 실행 (fallback)
                WeaponGroup[] groups = WeaponTypeMapper.GetAvailableWeaponGroups(race);
                PopulateWeaponGroupButtons(groups);
                SetCurrentStep(1);
            }
        }

        private void UpdateRaceDisplay()
        {
            if (raceDescriptionText == null) return;

            raceDescriptionText.color = Color.white;
            raceDescriptionText.text = selectedRace switch
            {
                Race.Human => "인간\n균형 잡힌 능력치를 가진 다재다능한 종족입니다.\n\n특징:\n- 모든 스탯 균등 성장\n- 빠른 레벨업\n- 다양한 직업 적성",
                Race.Elf => "엘프\n마법에 특화된 우아한 종족입니다.\n\n특징:\n- 높은 지능과 마나\n- 마법 데미지 증가\n- 마법 저항력",
                Race.Beast => "수인\n강력한 물리 능력을 가진 야생 종족입니다.\n\n특징:\n- 높은 힘과 체력\n- 물리 공격력 증가\n- 빠른 이동속도",
                _ => "종족을 선택하세요."
            };
        }

        private void UpdateRaceButtonStates()
        {
            var selectedColor = Color.yellow;
            var normalColor = Color.white;

            if (humanRaceButton != null)
                humanRaceButton.GetComponent<Image>().color = selectedRace == Race.Human ? selectedColor : normalColor;
            if (elfRaceButton != null)
                elfRaceButton.GetComponent<Image>().color = selectedRace == Race.Elf ? selectedColor : normalColor;
            if (beastRaceButton != null)
                beastRaceButton.GetComponent<Image>().color = selectedRace == Race.Beast ? selectedColor : normalColor;
        }

        // ── 무기군 선택 (Step 1) ──

        private void PopulateWeaponGroupButtons(WeaponGroup[] weaponGroups)
        {
            // 기존 동적 버튼 제거
            ClearSpawnedButtons(spawnedWeaponGroupButtons);

            if (weaponGroupButtonTemplate == null || weaponGroupButtonContainer == null)
            {
                Debug.LogWarning("WeaponGroup button template or container not assigned.");
                return;
            }

            foreach (var wg in weaponGroups)
            {
                var btnObj = Instantiate(weaponGroupButtonTemplate.gameObject, weaponGroupButtonContainer);
                btnObj.SetActive(true);

                // 텍스트 설정
                var btnText = btnObj.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    btnText.text = GetWeaponGroupDisplayName(wg);
                }

                // 클릭 이벤트
                var btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    var capturedWG = wg;
                    btn.onClick.AddListener(() => OnWeaponGroupButtonClicked(capturedWG));
                }

                spawnedWeaponGroupButtons.Add(btnObj);
            }

            // 설명 초기화
            if (weaponGroupDescriptionText != null)
            {
                weaponGroupDescriptionText.color = Color.white;
                weaponGroupDescriptionText.text = "무기군을 선택하세요.\n선택한 무기군에 따라 직업이 결정됩니다.";
            }
        }

        private void OnWeaponGroupButtonClicked(WeaponGroup weaponGroup)
        {
            selectedWeaponGroup = weaponGroup;

            // 설명 업데이트
            if (weaponGroupDescriptionText != null)
            {
                weaponGroupDescriptionText.color = Color.white;
                weaponGroupDescriptionText.text = GetWeaponGroupDescription(weaponGroup);
            }

            // 선택 시각 피드백
            HighlightSelectedButton(spawnedWeaponGroupButtons, weaponGroup.ToString());

            // Manager에 무기군 선택 전달 → Manager가 OnJobOptionsUpdated 발생
            if (creationManager != null)
            {
                creationManager.SelectWeaponGroup(weaponGroup);
            }
            else
            {
                // Fallback
                JobType[] jobTypes = WeaponTypeMapper.GetAvailableJobTypes(selectedRace, weaponGroup);
                JobData[] jobs = new JobData[0];
                // fallback에서는 JobData를 Resources에서 로드
                var jobList = new List<JobData>();
                foreach (var jt in jobTypes)
                {
                    var jd = Resources.Load<JobData>($"Jobs/{jt}");
                    if (jd != null) jobList.Add(jd);
                }
                PopulateJobButtons(jobList.ToArray());
                SetCurrentStep(2);
            }
        }

        // ── 직업 선택 (Step 2) ──

        private void PopulateJobButtons(JobData[] jobs)
        {
            ClearSpawnedButtons(spawnedJobButtons);

            if (jobButtonTemplate == null || jobButtonContainer == null)
            {
                Debug.LogWarning("Job button template or container not assigned.");
                return;
            }

            foreach (var job in jobs)
            {
                if (job == null) continue;

                var btnObj = Instantiate(jobButtonTemplate.gameObject, jobButtonContainer);
                btnObj.SetActive(true);
                btnObj.name = $"JobBtn_{job.jobType}";

                var btnText = btnObj.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    btnText.text = GetJobDisplayName(job.jobType);
                }

                var btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    var capturedJob = job;
                    btn.onClick.AddListener(() => OnJobButtonClicked(capturedJob));
                }

                spawnedJobButtons.Add(btnObj);
            }

            if (jobDescriptionText != null)
            {
                jobDescriptionText.color = Color.white;
                jobDescriptionText.text = "직업을 선택하세요.";
            }
        }

        private void OnJobButtonClicked(JobData job)
        {
            selectedJob = job;

            // 설명 업데이트
            if (jobDescriptionText != null)
            {
                jobDescriptionText.color = Color.white;
                jobDescriptionText.text = GetJobDescription(job.jobType);
            }

            // 시각 피드백
            HighlightSelectedButton(spawnedJobButtons, $"JobBtn_{job.jobType}");

            // 이름 입력 단계로 이동
            SetCurrentStep(3);
            GenerateRandomName();
        }

        // ── 이름 입력 (Step 3) ──

        public void GenerateRandomName()
        {
            if (randomNames.Length == 0) return;

            string baseName = randomNames[Random.Range(0, randomNames.Length)];
            string racePrefix = selectedRace.ToString()[0..2];
            int randomNumber = Random.Range(10, 100);

            selectedName = $"{racePrefix}{baseName}{randomNumber}";

            if (characterNameInput != null)
                characterNameInput.text = selectedName;
        }

        private void OnNameInputChanged(string newName)
        {
            selectedName = newName;
            ValidateName();
        }

        private bool ValidateName()
        {
            string validationMessage;
            bool isValid;

            if (string.IsNullOrEmpty(selectedName))
            {
                validationMessage = "이름을 입력해주세요.";
                isValid = false;
            }
            else if (selectedName.Length < minNameLength)
            {
                validationMessage = $"이름은 최소 {minNameLength}글자 이상이어야 합니다.";
                isValid = false;
            }
            else if (selectedName.Length > maxNameLength)
            {
                validationMessage = $"이름은 최대 {maxNameLength}글자까지 가능합니다.";
                isValid = false;
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(selectedName, @"[^a-zA-Z0-9가-힣]"))
            {
                validationMessage = "이름에는 영문, 숫자, 한글만 사용할 수 있습니다.";
                isValid = false;
            }
            else
            {
                validationMessage = "사용 가능한 이름입니다.";
                isValid = true;
            }

            if (nameValidationText != null)
            {
                nameValidationText.text = validationMessage;
                nameValidationText.color = isValid ? Color.green : Color.red;
            }

            return isValid;
        }

        // ── 확인 (Step 4) ──

        private void UpdateConfirmationSummary()
        {
            if (confirmSummaryText == null) return;

            string raceName = GetRaceDisplayName(selectedRace);
            string weaponGroupName = GetWeaponGroupDisplayName(selectedWeaponGroup);
            string jobName = selectedJob != null ? GetJobDisplayName(selectedJob.jobType) : "미선택";

            confirmSummaryText.text =
                $"캐릭터 생성 확인\n\n" +
                $"이름: {selectedName}\n" +
                $"종족: {raceName}\n" +
                $"무기군: {weaponGroupName}\n" +
                $"직업: {jobName}\n\n" +
                $"이대로 생성하시겠습니까?";
        }

        // ── 네비게이션 ──

        public void GoToPreviousStep()
        {
            if (currentStep > 0)
            {
                SetCurrentStep(currentStep - 1);

                // Step 0으로 돌아갈 때 종족 재선택 표시
                if (currentStep == 0)
                {
                    UpdateRaceDisplay();
                    UpdateRaceButtonStates();
                }
            }
        }

        public void GoToNextStep()
        {
            if (!ValidateCurrentStep())
                return;

            // Step 3(Name)에서 Step 4(Confirm)로 이동
            if (currentStep == 3)
            {
                SetCurrentStep(4);
            }
        }

        private bool ValidateCurrentStep()
        {
            switch (currentStep)
            {
                case 0: return true;
                case 1: return true;
                case 2: return selectedJob != null;
                case 3: return ValidateName();
                default: return true;
            }
        }

        private void UpdateNavigationButtons()
        {
            if (backButton != null)
            {
                backButton.interactable = currentStep > 0;
                // Step 0, 1, 2에서는 뒤로가기 가능 (단, 0에서는 비활성)
                backButton.gameObject.SetActive(currentStep > 0);
            }

            if (nextButton != null)
            {
                // Next 버튼은 Step 3(Name) 에서만 표시 (Step 4로 이동)
                // Step 0,1,2는 선택 버튼 클릭으로 자동 진행
                bool showNext = currentStep == 3;
                nextButton.gameObject.SetActive(showNext);
                nextButton.interactable = showNext && ValidateCurrentStep();
            }

            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(currentStep == 4);
            }

            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(true);
            }
        }

        // ── 확인/취소 ──

        public void ConfirmCharacterCreation()
        {
            if (!ValidateName()) return;
            if (selectedJob == null) return;

            // Manager를 통해 최종 캐릭터 생성
            if (creationManager != null)
            {
                creationManager.SelectJobAndCreateCharacter(selectedJob);
                // Manager의 OnCharacterCreated 이벤트에서 UI 숨김 처리
            }
            else
            {
                // Fallback: Manager 없이 직접 생성
                var data = new CharacterCreationData
                {
                    race = selectedRace,
                    jobType = selectedJob.jobType,
                    weaponGroup = selectedWeaponGroup,
                    startingLevel = 1,
                    startingGold = 100,
                    inheritedSouls = new SoulData[0]
                };
                OnCharacterCreated?.Invoke(data);
                HideCharacterCreationUI();
            }
        }

        public void CancelCharacterCreation()
        {
            OnCharacterCreationCancelled?.Invoke();
            HideCharacterCreationUI();
            Debug.Log("Character creation cancelled");
        }

        // ── UI 표시/숨김 ──

        public void ShowCharacterCreationUI()
        {
            if (characterCreationPanel != null)
                characterCreationPanel.SetActive(true);

            // 상태 초기화
            selectedRace = Race.Human;
            selectedWeaponGroup = WeaponGroup.SwordShield;
            selectedJob = null;
            selectedName = "";

            ClearSpawnedButtons(spawnedWeaponGroupButtons);
            ClearSpawnedButtons(spawnedJobButtons);

            // Manager를 통해 생성 시작
            if (creationManager != null)
            {
                creationManager.StartCharacterCreation();
            }

            SetCurrentStep(0);
            UpdateRaceDisplay();
            UpdateRaceButtonStates();
        }

        public void HideCharacterCreationUI()
        {
            if (characterCreationPanel != null)
                characterCreationPanel.SetActive(false);

            ClearSpawnedButtons(spawnedWeaponGroupButtons);
            ClearSpawnedButtons(spawnedJobButtons);
        }

        /// <summary>
        /// 외부에서 호출 - 캐릭터 생성 시작
        /// </summary>
        public static void StartCharacterCreation(System.Action<CharacterCreationData> onCreated, System.Action onCancelled = null)
        {
            var ui = FindFirstObjectByType<CharacterCreationUI>();
            if (ui != null)
            {
                ui.OnCharacterCreated = onCreated;
                ui.OnCharacterCreationCancelled = onCancelled;
                ui.ShowCharacterCreationUI();
            }
            else
            {
                Debug.LogError("CharacterCreationUI not found in scene!");
            }
        }

        // ── 유틸리티 ──

        private void OnDestroy()
        {
            OnCharacterCreated = null;
            OnCharacterCreationCancelled = null;

            // 버튼 리스너 정리
            if (humanRaceButton != null) humanRaceButton.onClick.RemoveAllListeners();
            if (elfRaceButton != null) elfRaceButton.onClick.RemoveAllListeners();
            if (beastRaceButton != null) beastRaceButton.onClick.RemoveAllListeners();
            if (randomNameButton != null) randomNameButton.onClick.RemoveAllListeners();
            if (characterNameInput != null) characterNameInput.onValueChanged.RemoveAllListeners();
            if (backButton != null) backButton.onClick.RemoveAllListeners();
            if (nextButton != null) nextButton.onClick.RemoveAllListeners();
            if (confirmButton != null) confirmButton.onClick.RemoveAllListeners();
            if (cancelButton != null) cancelButton.onClick.RemoveAllListeners();
        }

        private void ClearSpawnedButtons(List<GameObject> buttons)
        {
            foreach (var btn in buttons)
            {
                if (btn != null) Destroy(btn);
            }
            buttons.Clear();
        }

        private void HighlightSelectedButton(List<GameObject> buttons, string selectedName)
        {
            foreach (var btnObj in buttons)
            {
                if (btnObj == null) continue;
                var img = btnObj.GetComponent<Image>();
                if (img != null)
                {
                    img.color = btnObj.name.Contains(selectedName) ? Color.yellow : Color.white;
                }
            }
        }

        // ── 표시 이름 헬퍼 ──

        private string GetRaceDisplayName(Race race)
        {
            return race switch
            {
                Race.Human => "인간",
                Race.Elf => "엘프",
                Race.Beast => "수인",
                Race.Machina => "기계족",
                _ => "알 수 없음"
            };
        }

        private string GetWeaponGroupDisplayName(WeaponGroup wg)
        {
            return wg switch
            {
                WeaponGroup.SwordShield => "한손검 / 방패",
                WeaponGroup.TwoHandedSword => "양손 대검",
                WeaponGroup.TwoHandedAxe => "양손 도끼",
                WeaponGroup.Dagger => "단검",
                WeaponGroup.Bow => "활",
                WeaponGroup.Staff => "지팡이",
                WeaponGroup.Wand => "마법봉",
                WeaponGroup.Fist => "격투",
                _ => wg.ToString()
            };
        }

        private string GetWeaponGroupDescription(WeaponGroup wg)
        {
            return wg switch
            {
                WeaponGroup.SwordShield => "한손검 / 방패\n한 손에 검, 다른 손에 방패를 장비합니다.\n공격과 방어의 균형이 뛰어납니다.",
                WeaponGroup.TwoHandedSword => "양손 대검\n강력한 양손 대검을 휘둘러 높은 데미지를 줍니다.\n공격 범위가 넓고 파괴력이 강합니다.",
                WeaponGroup.TwoHandedAxe => "양손 도끼\n무거운 도끼로 적을 분쇄합니다.\n느리지만 압도적인 한 방을 자랑합니다.",
                WeaponGroup.Dagger => "단검\n빠른 공격 속도와 치명적인 급소 공격이 특징입니다.\n은밀한 전투에 적합합니다.",
                WeaponGroup.Bow => "활\n원거리에서 정밀한 사격을 합니다.\n안전한 거리에서 적을 제압합니다.",
                WeaponGroup.Staff => "지팡이\n강력한 마법을 사용하기 위한 매개체입니다.\n마법 데미지가 크게 증가합니다.",
                WeaponGroup.Wand => "마법봉\n빠른 마법 시전이 가능한 도구입니다.\n보조 마법과 치유에 적합합니다.",
                WeaponGroup.Fist => "격투\n맨손 또는 격투 무기로 싸웁니다.\n빠른 연타와 근접 제압에 유리합니다.",
                _ => "무기군을 선택하세요."
            };
        }

        private string GetJobDisplayName(JobType jobType)
        {
            return jobType switch
            {
                JobType.Navigator => "항해사",
                JobType.Scout => "정찰병",
                JobType.Tracker => "추적자",
                JobType.Trapper => "함정 전문가",
                JobType.Guardian => "수호기사",
                JobType.Templar => "성기사",
                JobType.Berserker => "광전사",
                JobType.Assassin => "암살자",
                JobType.Duelist => "결투가",
                JobType.ElementalBruiser => "원소 투사",
                JobType.Sniper => "저격수",
                JobType.Mage => "마법사",
                JobType.Warlock => "흑마법사",
                JobType.Cleric => "성직자",
                JobType.Druid => "드루이드",
                JobType.Amplifier => "증폭술사",
                _ => jobType.ToString()
            };
        }

        private string GetJobDescription(JobType jobType)
        {
            return jobType switch
            {
                JobType.Navigator => "항해사\n바다의 기술을 활용하는 탐험가입니다.\n정찰과 항해에 뛰어난 능력을 발휘합니다.",
                JobType.Scout => "정찰병\n은밀하게 적을 정찰하고 급습합니다.\n높은 민첩성과 회피 능력을 가집니다.",
                JobType.Tracker => "추적자\n사냥감을 끝까지 추적하는 사냥꾼입니다.\n원거리 공격과 추적 기술에 뛰어납니다.",
                JobType.Trapper => "함정 전문가\n교묘한 함정으로 적을 제압합니다.\n전략적 전투에 강점이 있습니다.",
                JobType.Guardian => "수호기사\n동료를 지키는 철벽 방어의 기사입니다.\n높은 방어력과 보호 기술을 가집니다.",
                JobType.Templar => "성기사\n신성한 힘으로 싸우는 성스러운 기사입니다.\n공격과 치유를 겸비합니다.",
                JobType.Berserker => "광전사\n분노의 힘으로 적을 압도하는 전사입니다.\n높은 공격력과 체력을 가집니다.",
                JobType.Assassin => "암살자\n그림자에서 치명적인 일격을 가합니다.\n높은 치명타와 독 공격이 특징입니다.",
                JobType.Duelist => "결투가\n화려한 검술로 적과 일대일 승부합니다.\n정밀한 연속 공격이 특징입니다.",
                JobType.ElementalBruiser => "원소 투사\n원소의 힘을 주먹에 담아 싸웁니다.\n물리와 마법의 융합 전투를 합니다.",
                JobType.Sniper => "저격수\n정밀한 조준으로 적의 급소를 노립니다.\n단발 고데미지에 특화됩니다.",
                JobType.Mage => "마법사\n강력한 원소 마법을 구사합니다.\n광역 피해와 원소 제어에 뛰어납니다.",
                JobType.Warlock => "흑마법사\n금지된 마법으로 적을 저주합니다.\n지속 피해와 디버프에 강합니다.",
                JobType.Cleric => "성직자\n신의 축복으로 동료를 치유합니다.\n회복과 버프 지원에 특화됩니다.",
                JobType.Druid => "드루이드\n자연의 힘을 다루는 현자입니다.\n변신과 자연 마법이 특징입니다.",
                JobType.Amplifier => "증폭술사\n동료의 힘을 증폭시키는 지원가입니다.\n버프와 에너지 증폭에 뛰어납니다.",
                _ => "직업을 선택하세요."
            };
        }
    }
}
