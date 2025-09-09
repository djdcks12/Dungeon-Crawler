using UnityEngine;
using UnityEngine.UI;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 스킬 학습 UI - NPC와 상호작용시 표시되는 스킬 선택 인터페이스
    /// </summary>
    public class SkillLearningUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private GameObject skillLearningPanel;
        [SerializeField] private Text npcNameText;
        [SerializeField] private Text levelText;
        [SerializeField] private Button[] skillChoiceButtons = new Button[3];
        [SerializeField] private Text[] skillNameTexts = new Text[3];
        [SerializeField] private Text[] skillDescriptionTexts = new Text[3];
        [SerializeField] private Text[] skillCostTexts = new Text[3];
        [SerializeField] private Button closeButton;
        
        // 현재 상호작용 정보
        private SkillMasterNPC currentNPC;
        private NewSkillLearningSystem currentSkillSystem;
        private int currentLevel;
        private SkillChoice currentSkillChoice;
        
        public bool IsOpen => skillLearningPanel.activeInHierarchy;
        
        private void Awake()
        {
            // 버튼 이벤트 설정
            for (int i = 0; i < skillChoiceButtons.Length; i++)
            {
                int choiceIndex = i; // 클로저 변수 캡처
                skillChoiceButtons[i].onClick.AddListener(() => OnSkillChoiceSelected(choiceIndex));
            }
            
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseSkillLearningUI);
            }
            
            // 시작시 UI 숨기기
            if (skillLearningPanel != null)
            {
                skillLearningPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// 스킬 학습 UI 열기
        /// </summary>
        public void OpenSkillLearningUI(SkillMasterNPC npc, NewSkillLearningSystem skillSystem, int[] availableLevels)
        {
            if (availableLevels.Length == 0) return;
            
            currentNPC = npc;
            currentSkillSystem = skillSystem;
            currentLevel = availableLevels[0]; // 첫 번째 가용 레벨 사용
            
            // NPC 이름 설정
            if (npcNameText != null)
            {
                npcNameText.text = GetJobDisplayName(npc.NPCJobType) + " 마스터";
            }
            
            // 레벨 텍스트 설정
            if (levelText != null)
            {
                levelText.text = $"레벨 {currentLevel} 스킬 학습";
            }
            
            // 스킬 선택지 정보 가져오기
            LoadSkillChoiceForCurrentLevel();
            
            // UI 패널 활성화
            if (skillLearningPanel != null)
            {
                skillLearningPanel.SetActive(true);
            }
            
            // 커서 상태 변경 (게임 일시정지)
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        /// <summary>
        /// 스킬 학습 UI 닫기
        /// </summary>
        public void CloseSkillLearningUI()
        {
            if (skillLearningPanel != null)
            {
                skillLearningPanel.SetActive(false);
            }
            
            // 게임 재개
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // 참조 정리
            currentNPC = null;
            currentSkillSystem = null;
            currentSkillChoice = null;
        }
        
        /// <summary>
        /// 현재 레벨의 스킬 선택지 로드
        /// </summary>
        private void LoadSkillChoiceForCurrentLevel()
        {
            // 임시로 더미 데이터 사용 (실제로는 JobData에서 가져와야 함)
            currentSkillChoice = CreateDummySkillChoice();
            
            // UI에 스킬 정보 표시
            UpdateSkillChoiceUI();
        }
        
        /// <summary>
        /// 스킬 선택지 UI 업데이트
        /// </summary>
        private void UpdateSkillChoiceUI()
        {
            if (currentSkillChoice == null) return;
            
            // 선택지 A
            UpdateSkillButton(0, currentSkillChoice.choiceA, currentSkillChoice.goldCostA, currentSkillChoice.conceptA);
            
            // 선택지 B
            UpdateSkillButton(1, currentSkillChoice.choiceB, currentSkillChoice.goldCostB, currentSkillChoice.conceptB);
            
            // 선택지 C  
            UpdateSkillButton(2, currentSkillChoice.choiceC, currentSkillChoice.goldCostC, currentSkillChoice.conceptC);
        }
        
        /// <summary>
        /// 개별 스킬 버튼 업데이트
        /// </summary>
        private void UpdateSkillButton(int index, SkillData skill, long cost, string concept)
        {
            if (index >= skillChoiceButtons.Length) return;
            
            bool hasSkill = skill != null;
            skillChoiceButtons[index].gameObject.SetActive(hasSkill);
            
            if (!hasSkill) return;
            
            // 스킬 이름
            if (skillNameTexts[index] != null)
            {
                skillNameTexts[index].text = skill.skillName;
            }
            
            // 스킬 설명
            if (skillDescriptionTexts[index] != null)
            {
                string description = string.IsNullOrEmpty(concept) ? skill.description : $"{concept}\n\n{skill.description}";
                skillDescriptionTexts[index].text = description;
            }
            
            // 골드 비용
            if (skillCostTexts[index] != null)
            {
                skillCostTexts[index].text = $"{cost} Gold";
            }
            
            // 골드 부족시 버튼 비활성화 - PlayerStatsManager에서 골드 정보 가져오기
            var playerStatsManager = FindObjectOfType<PlayerStatsManager>();
            bool canAfford = playerStatsManager != null && playerStatsManager.CurrentStats.CurrentGold >= cost;
            skillChoiceButtons[index].interactable = canAfford;
        }
        
        /// <summary>
        /// 스킬 선택지 클릭 처리
        /// </summary>
        private void OnSkillChoiceSelected(int choiceIndex)
        {
            if (currentNPC == null || currentSkillSystem == null) return;
            
            // 스킬 학습 시도
            currentNPC.ProcessSkillLearning(currentLevel, choiceIndex);
            
            // UI 닫기
            CloseSkillLearningUI();
        }
        
        /// <summary>
        /// 더미 스킬 선택지 생성 (테스트용)
        /// </summary>
        private SkillChoice CreateDummySkillChoice()
        {
            // 실제 구현에서는 JobData에서 가져와야 함
            // 지금은 테스트용 더미 데이터
            return new SkillChoice
            {
                conceptA = "공격형",
                conceptB = "방어형", 
                conceptC = "유틸형",
                goldCostA = 100,
                goldCostB = 100,
                goldCostC = 100
            };
        }
        
        /// <summary>
        /// 직업 타입의 표시 이름 반환
        /// </summary>
        private string GetJobDisplayName(JobType job)
        {
            return job switch
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
                _ => job.ToString()
            };
        }
        
        private void OnDestroy()
        {
            // 시간 스케일 복구
            Time.timeScale = 1f;
        }
    }
}