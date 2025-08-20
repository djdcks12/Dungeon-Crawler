using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 캐릭터 생성 UI 시스템
    /// 종족 선택, 이름 설정, 초기 스탯 확인
    /// </summary>
    public class CharacterCreationUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject characterCreationPanel;
        [SerializeField] private GameObject raceSelectionPanel;
        [SerializeField] private GameObject nameInputPanel;
        [SerializeField] private GameObject statsPreviewPanel;
        [SerializeField] private GameObject confirmationPanel;
        
        [Header("Race Selection")]
        [SerializeField] private Button humanRaceButton;
        [SerializeField] private Button elfRaceButton;
        [SerializeField] private Button beastRaceButton;
        [SerializeField] private Button machinaRaceButton;
        [SerializeField] private Text raceDescriptionText;
        [SerializeField] private Image racePreviewImage;
        
        [Header("Name Input")]
        [SerializeField] private InputField characterNameInput;
        [SerializeField] private Button randomNameButton;
        [SerializeField] private Text nameValidationText;
        
        [Header("Stats Preview")]
        [SerializeField] private Text baseStatsText;
        [SerializeField] private Text racialBonusText;
        [SerializeField] private Text totalStatsText;
        [SerializeField] private Text specialAbilitiesText;
        
        [Header("Navigation Buttons")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        
        [Header("Settings")]
        [SerializeField] private int minNameLength = 3;
        [SerializeField] private int maxNameLength = 12;
        [SerializeField] private string[] randomNames = {
            "Adventurer", "Hero", "Warrior", "Mage", "Rogue", "Paladin",
            "Ranger", "Monk", "Sorcerer", "Barbarian", "Druid", "Bard"
        };
        
        // 상태 변수들
        private Race selectedRace = Race.Human;
        private string selectedName = "";
        private int currentStep = 0; // 0: Race, 1: Name, 2: Stats, 3: Confirm
        private CharacterData previewCharacterData;
        
        // 이벤트
        public System.Action<CharacterData> OnCharacterCreated;
        public System.Action OnCharacterCreationCancelled;
        
        private void Start()
        {
            InitializeUI();
            SetCurrentStep(0);
        }
        
        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            // 종족 선택 버튼 이벤트
            if (humanRaceButton != null)
                humanRaceButton.onClick.AddListener(() => SelectRace(Race.Human));
            if (elfRaceButton != null)
                elfRaceButton.onClick.AddListener(() => SelectRace(Race.Elf));
            if (beastRaceButton != null)
                beastRaceButton.onClick.AddListener(() => SelectRace(Race.Beast));
            if (machinaRaceButton != null)
                machinaRaceButton.onClick.AddListener(() => SelectRace(Race.Machina));
            
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
            
            // 기본값 설정
            SelectRace(Race.Human);
            GenerateRandomName();
        }
        
        /// <summary>
        /// 현재 단계 설정
        /// </summary>
        private void SetCurrentStep(int step)
        {
            currentStep = step;
            
            // 모든 패널 비활성화
            if (raceSelectionPanel != null)
                raceSelectionPanel.SetActive(step == 0);
            if (nameInputPanel != null)
                nameInputPanel.SetActive(step == 1);
            if (statsPreviewPanel != null)
                statsPreviewPanel.SetActive(step == 2);
            if (confirmationPanel != null)
                confirmationPanel.SetActive(step == 3);
            
            // 네비게이션 버튼 상태 업데이트
            UpdateNavigationButtons();
        }
        
        /// <summary>
        /// 종족 선택
        /// </summary>
        public void SelectRace(Race race)
        {
            selectedRace = race;
            UpdateRaceDisplay();
            UpdateStatsPreview();
            
            // 종족 선택 버튼 시각적 피드백
            UpdateRaceButtonStates();
        }
        
        /// <summary>
        /// 종족 표시 업데이트
        /// </summary>
        private void UpdateRaceDisplay()
        {
            if (raceDescriptionText == null) return;
            
            string description = selectedRace switch
            {
                Race.Human => "인간\\n균형 잡힌 능력치를 가진 다재다능한 종족입니다.\\n\\n특징:\\n• 모든 스탯 균등 성장\\n• 빠른 레벨업\\n• 다양한 직업 적성",
                Race.Elf => "엘프\\n마법에 특화된 우아한 종족입니다.\\n\\n특징:\\n• 높은 지능과 마나\\n• 마법 데미지 증가\\n• 마법 저항력",
                Race.Beast => "수인\\n강력한 물리 능력을 가진 야생 종족입니다.\\n\\n특징:\\n• 높은 힘과 체력\\n• 물리 공격력 증가\\n• 빠른 이동속도",
                Race.Machina => "기계족\\n높은 방어력과 내구성을 가진 기계 종족입니다.\\n\\n특징:\\n• 높은 방어력\\n• 상태이상 저항\\n• 독특한 기계 스킬",
                _ => "알 수 없는 종족"
            };
            
            raceDescriptionText.text = description;
        }
        
        /// <summary>
        /// 종족 버튼 상태 업데이트
        /// </summary>
        private void UpdateRaceButtonStates()
        {
            // 선택된 종족 버튼 하이라이트 처리
            var selectedColor = Color.yellow;
            var normalColor = Color.white;
            
            if (humanRaceButton != null)
                humanRaceButton.GetComponent<Image>().color = selectedRace == Race.Human ? selectedColor : normalColor;
            if (elfRaceButton != null)
                elfRaceButton.GetComponent<Image>().color = selectedRace == Race.Elf ? selectedColor : normalColor;
            if (beastRaceButton != null)
                beastRaceButton.GetComponent<Image>().color = selectedRace == Race.Beast ? selectedColor : normalColor;
            if (machinaRaceButton != null)
                machinaRaceButton.GetComponent<Image>().color = selectedRace == Race.Machina ? selectedColor : normalColor;
        }
        
        /// <summary>
        /// 랜덤 이름 생성
        /// </summary>
        public void GenerateRandomName()
        {
            if (randomNames.Length == 0) return;
            
            string baseName = randomNames[Random.Range(0, randomNames.Length)];
            string racePrefix = selectedRace.ToString()[0..2]; // "Hu", "El", "Be", "Ma"
            int randomNumber = Random.Range(10, 100);
            
            selectedName = $"{racePrefix}{baseName}{randomNumber}";
            
            if (characterNameInput != null)
                characterNameInput.text = selectedName;
        }
        
        /// <summary>
        /// 이름 입력 변경 처리
        /// </summary>
        private void OnNameInputChanged(string newName)
        {
            selectedName = newName;
            ValidateName();
        }
        
        /// <summary>
        /// 이름 유효성 검사
        /// </summary>
        private bool ValidateName()
        {
            string validationMessage = "";
            bool isValid = true;
            
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
            }
            
            if (nameValidationText != null)
            {
                nameValidationText.text = validationMessage;
                nameValidationText.color = isValid ? Color.green : Color.red;
            }
            
            return isValid;
        }
        
        /// <summary>
        /// 스탯 미리보기 업데이트
        /// </summary>
        private void UpdateStatsPreview()
        {
            var raceData = GetRaceDataByType(selectedRace);
            if (raceData == null) return;
            
            // 기본 스탯 표시
            if (baseStatsText != null)
            {
                baseStatsText.text = $"기본 스탯:\\n" +
                                   $"레벨: 1\\n" +
                                   $"체력: 100\\n" +
                                   $"마나: 50\\n" +
                                   $"공격력: 10";
            }

            // 종족 보너스 표시
            if (racialBonusText != null)
            {
                var bonus = raceData.StatGrowth;
                racialBonusText.text = $"종족 보너스 (레벨당):\\n" +
                                     $"힘: +{bonus.strengthGrowth}\\n" +
                                     $"민첩: +{bonus.agilityGrowth}\\n" +
                                     $"체력: +{bonus.vitalityGrowth}\\n" +
                                     $"지능: +{bonus.intelligenceGrowth}\\n" +
                                     $"방어력: +{bonus.defenseGrowth}\\n" +
                                     $"마법 방어력: +{bonus.magicDefenseGrowth}\\n" +
                                     $"행운: +{bonus.luckGrowth}" +
                                     $"안정성: +{bonus.stabilityGrowth}";
            }
            
            // 특수 능력 표시
            if (specialAbilitiesText != null)
            {
                string abilities = selectedRace switch
                {
                    Race.Human => "• 경험치 +10% 보너스\\n• 모든 스킬 습득 가능",
                    Race.Elf => "• 마법 데미지 +20%\\n• 마나 재생 +50%",
                    Race.Beast => "• 물리 공격력 +20%\\n• 이동속도 +15%",
                    Race.Machina => "• 물리 방어력 +30%\\n• 상태이상 저항 +50%",
                    _ => "• 특수 능력 없음"
                };
                
                specialAbilitiesText.text = $"특수 능력:\\n{abilities}";
            }
        }
        
        /// <summary>
        /// 종족 데이터 가져오기
        /// </summary>
        private RaceData GetRaceDataByType(Race raceType)
        {
            return raceType switch
            {
                Race.Human => RaceDataCreator.CreateHumanRaceData(),
                Race.Elf => RaceDataCreator.CreateElfRaceData(),
                Race.Beast => RaceDataCreator.CreateBeastRaceData(),
                Race.Machina => RaceDataCreator.CreateMachinaRaceData(),
                _ => RaceDataCreator.CreateHumanRaceData()
            };
        }
        
        /// <summary>
        /// 이전 단계로
        /// </summary>
        public void GoToPreviousStep()
        {
            if (currentStep > 0)
            {
                SetCurrentStep(currentStep - 1);
            }
        }
        
        /// <summary>
        /// 다음 단계로
        /// </summary>
        public void GoToNextStep()
        {
            // 현재 단계 유효성 검사
            if (!ValidateCurrentStep())
                return;
            
            if (currentStep < 3)
            {
                SetCurrentStep(currentStep + 1);
            }
        }
        
        /// <summary>
        /// 현재 단계 유효성 검사
        /// </summary>
        private bool ValidateCurrentStep()
        {
            switch (currentStep)
            {
                case 0: // 종족 선택
                    return true; // 기본값이 있으므로 항상 유효
                    
                case 1: // 이름 입력
                    return ValidateName();
                    
                case 2: // 스탯 미리보기
                    return true; // 확인만 하는 단계
                    
                default:
                    return true;
            }
        }
        
        /// <summary>
        /// 네비게이션 버튼 상태 업데이트
        /// </summary>
        private void UpdateNavigationButtons()
        {
            if (backButton != null)
                backButton.interactable = currentStep > 0;
                
            if (nextButton != null)
            {
                nextButton.interactable = currentStep < 3 && ValidateCurrentStep();
                nextButton.gameObject.SetActive(currentStep < 3);
            }
            
            if (confirmButton != null)
                confirmButton.gameObject.SetActive(currentStep == 3);
        }
        
        /// <summary>
        /// 캐릭터 생성 확인
        /// </summary>
        public void ConfirmCharacterCreation()
        {
            if (!ValidateCurrentStep())
                return;
            
            // CharacterData 생성
            previewCharacterData = CreateCharacterData();
            
            // 이벤트 발생
            OnCharacterCreated?.Invoke(previewCharacterData);
            
            // UI 숨김
            HideCharacterCreationUI();
            
            Debug.Log($"✅ Character created: {previewCharacterData.characterName} ({previewCharacterData.race})");
        }
        
        /// <summary>
        /// 캐릭터 생성 취소
        /// </summary>
        public void CancelCharacterCreation()
        {
            OnCharacterCreationCancelled?.Invoke();
            HideCharacterCreationUI();
            
            Debug.Log("❌ Character creation cancelled");
        }
        
        /// <summary>
        /// CharacterData 생성
        /// </summary>
        private CharacterData CreateCharacterData()
        {
            var characterData = new CharacterData();
            
            // 기본 정보
            characterData.characterName = selectedName;
            characterData.race = selectedRace;
            characterData.level = 1;
            characterData.experience = 0;
            
            // 초기 스탯 (기본값)
            characterData.soulBonusStats = new StatBlock();
            
            // 생성 시간
            characterData.creationTime = System.DateTime.Now.ToBinary();
            
            return characterData;
        }
        
        /// <summary>
        /// 캐릭터 생성 UI 표시
        /// </summary>
        public void ShowCharacterCreationUI()
        {
            if (characterCreationPanel != null)
                characterCreationPanel.SetActive(true);
                
            SetCurrentStep(0);
            SelectRace(Race.Human);
            GenerateRandomName();
        }
        
        /// <summary>
        /// 캐릭터 생성 UI 숨김
        /// </summary>
        public void HideCharacterCreationUI()
        {
            if (characterCreationPanel != null)
                characterCreationPanel.SetActive(false);
        }
        
        /// <summary>
        /// 외부에서 호출할 수 있는 캐릭터 생성 시작
        /// </summary>
        public static void StartCharacterCreation(System.Action<CharacterData> onCreated, System.Action onCancelled = null)
        {
            var ui = FindObjectOfType<CharacterCreationUI>();
            if (ui != null)
            {
                ui.OnCharacterCreated = onCreated;
                ui.OnCharacterCreationCancelled = onCancelled;
                ui.ShowCharacterCreationUI();
            }
            else
            {
                Debug.LogError("❌ CharacterCreationUI not found in scene!");
            }
        }
    }
}