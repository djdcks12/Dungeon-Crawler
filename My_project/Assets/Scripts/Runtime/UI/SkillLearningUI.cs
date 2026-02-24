using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 스킬 학습 UI - NPC와 상호작용시 표시되는 스킬 선택 인터페이스
    /// NewSkillLearningSystem과 연동하여 실제 JobData 기반 스킬 선택지 표시
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
        [SerializeField] private Image[] skillIconImages = new Image[3];
        [SerializeField] private Button closeButton;

        [Header("Level Navigation")]
        [SerializeField] private Button prevLevelButton;
        [SerializeField] private Button nextLevelButton;

        [Header("Error Display")]
        [SerializeField] private Text errorMessageText;

        // 현재 상호작용 정보
        private SkillMasterNPC currentNPC;
        private NewSkillLearningSystem currentSkillSystem;
        private int currentLevel;
        private SkillChoice currentSkillChoice;
        private int[] availableLevels;
        private int currentLevelIndex;

        public bool IsOpen => skillLearningPanel != null && skillLearningPanel.activeInHierarchy;

        private void Awake()
        {
            // 스킬 선택 버튼 이벤트 설정
            for (int i = 0; i < skillChoiceButtons.Length; i++)
            {
                if (skillChoiceButtons[i] == null) continue;
                int choiceIndex = i;
                skillChoiceButtons[i].onClick.AddListener(() => OnSkillChoiceSelected(choiceIndex));
            }

            if (closeButton != null)
                closeButton.onClick.AddListener(CloseSkillLearningUI);

            // 레벨 네비게이션 버튼
            if (prevLevelButton != null)
                prevLevelButton.onClick.AddListener(ShowPreviousLevel);
            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(ShowNextLevel);

            // 시작시 UI 숨기기
            if (skillLearningPanel != null)
                skillLearningPanel.SetActive(false);
        }

        /// <summary>
        /// 스킬 학습 UI 열기
        /// </summary>
        public void OpenSkillLearningUI(SkillMasterNPC npc, NewSkillLearningSystem skillSystem, int[] levels)
        {
            if (levels == null || levels.Length == 0) return;

            currentNPC = npc;
            currentSkillSystem = skillSystem;
            availableLevels = levels;
            currentLevelIndex = 0;
            currentLevel = availableLevels[0];

            // 에러 이벤트 구독
            if (currentSkillSystem != null)
            {
                currentSkillSystem.OnSkillLearningError += ShowErrorMessage;
                currentSkillSystem.OnSkillLearned += OnSkillLearnedHandler;
            }

            // NPC 이름 설정
            if (npcNameText != null)
                npcNameText.text = GetJobDisplayName(npc.NPCJobType) + " 마스터";

            // 현재 레벨의 스킬 선택지 로드
            LoadSkillChoiceForCurrentLevel();

            // UI 패널 활성화
            if (skillLearningPanel != null)
                skillLearningPanel.SetActive(true);

            // 에러 메시지 초기화
            ClearErrorMessage();

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
                skillLearningPanel.SetActive(false);

            // 이벤트 구독 해제
            if (currentSkillSystem != null)
            {
                currentSkillSystem.OnSkillLearningError -= ShowErrorMessage;
                currentSkillSystem.OnSkillLearned -= OnSkillLearnedHandler;
            }

            // 게임 재개
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // 참조 정리
            currentNPC = null;
            currentSkillSystem = null;
            currentSkillChoice = null;
            availableLevels = null;
        }

        /// <summary>
        /// 현재 레벨의 스킬 선택지 로드 (실제 JobData 기반)
        /// </summary>
        private void LoadSkillChoiceForCurrentLevel()
        {
            if (currentSkillSystem == null) return;

            // NewSkillLearningSystem에서 실제 스킬 선택지 가져오기
            currentSkillChoice = currentSkillSystem.GetSkillChoiceForLevel(currentLevel);

            if (currentSkillChoice == null)
            {
                ShowErrorMessage($"레벨 {currentLevel}에 해당하는 스킬 데이터가 없습니다.");
                return;
            }

            // 레벨 텍스트 설정
            if (levelText != null)
            {
                string levelLabel = currentLevel == 10 ? "궁극기" : $"레벨 {currentLevel} 스킬";
                levelText.text = levelLabel;
            }

            // 레벨 네비게이션 버튼 상태
            UpdateLevelNavigationButtons();

            // UI에 스킬 정보 표시
            UpdateSkillChoiceUI();
        }

        /// <summary>
        /// 스킬 선택지 UI 업데이트
        /// </summary>
        private void UpdateSkillChoiceUI()
        {
            if (currentSkillChoice == null) return;

            bool isUltimate = currentLevel == 10;

            // 선택지 A (항상 표시)
            UpdateSkillButton(0, currentSkillChoice.choiceA, currentSkillChoice.goldCostA, currentSkillChoice.conceptA);

            // 선택지 B, C (궁극기가 아닌 경우만 표시)
            if (isUltimate)
            {
                // 궁극기는 선택지가 하나뿐
                if (skillChoiceButtons.Length > 1 && skillChoiceButtons[1] != null)
                    skillChoiceButtons[1].gameObject.SetActive(false);
                if (skillChoiceButtons.Length > 2 && skillChoiceButtons[2] != null)
                    skillChoiceButtons[2].gameObject.SetActive(false);
            }
            else
            {
                UpdateSkillButton(1, currentSkillChoice.choiceB, currentSkillChoice.goldCostB, currentSkillChoice.conceptB);
                UpdateSkillButton(2, currentSkillChoice.choiceC, currentSkillChoice.goldCostC, currentSkillChoice.conceptC);
            }
        }

        /// <summary>
        /// 개별 스킬 버튼 업데이트
        /// </summary>
        private void UpdateSkillButton(int index, SkillData skill, long cost, string concept)
        {
            if (index >= skillChoiceButtons.Length || skillChoiceButtons[index] == null) return;

            bool hasSkill = skill != null;
            skillChoiceButtons[index].gameObject.SetActive(hasSkill);

            if (!hasSkill) return;

            // 스킬 이름
            if (index < skillNameTexts.Length && skillNameTexts[index] != null)
                skillNameTexts[index].text = skill.skillName;

            // 스킬 설명 (컨셉 + 상세 설명)
            if (index < skillDescriptionTexts.Length && skillDescriptionTexts[index] != null)
            {
                string description = "";

                // 스킬 타입 표시
                description += $"[{GetSkillTypeDisplay(skill)}] ";

                // 컨셉이 있으면 추가
                if (!string.IsNullOrEmpty(concept))
                    description += $"{concept}\n";

                // 스킬 설명
                description += skill.description;

                // 스킬 수치 정보
                description += $"\n\n쿨다운: {skill.cooldown:F1}초";
                if (skill.manaCost > 0)
                    description += $" | 마나: {skill.manaCost}";
                if (skill.baseDamage > 0)
                    description += $" | 데미지: {skill.baseDamage:F0}";

                skillDescriptionTexts[index].text = description;
            }

            // 골드 비용
            if (index < skillCostTexts.Length && skillCostTexts[index] != null)
                skillCostTexts[index].text = $"{cost} Gold";

            // 스킬 아이콘
            if (index < skillIconImages.Length && skillIconImages[index] != null)
            {
                skillIconImages[index].sprite = skill.skillIcon;
                skillIconImages[index].gameObject.SetActive(skill.skillIcon != null);
            }

            // 골드 부족시 버튼 비활성화
            var playerStatsManager = FindFirstObjectByType<PlayerStatsManager>();
            bool canAfford = playerStatsManager?.CurrentStats != null && playerStatsManager.CurrentStats.CurrentGold >= cost;
            skillChoiceButtons[index].interactable = canAfford;

            // 비용 텍스트 색상 (부족하면 빨강)
            if (index < skillCostTexts.Length && skillCostTexts[index] != null)
                skillCostTexts[index].color = canAfford ? Color.yellow : Color.red;
        }

        /// <summary>
        /// 스킬 선택지 클릭 처리
        /// </summary>
        private void OnSkillChoiceSelected(int choiceIndex)
        {
            if (currentNPC == null || currentSkillSystem == null) return;

            ClearErrorMessage();

            // 스킬 학습 시도 (NewSkillLearningSystem을 통해)
            currentNPC.ProcessSkillLearning(currentLevel, choiceIndex);
        }

        /// <summary>
        /// 스킬 학습 완료 핸들러
        /// </summary>
        private void OnSkillLearnedHandler(int level, SkillData skill)
        {
            if (level == currentLevel)
            {
                Debug.Log($"스킬 학습 완료: {skill.skillName}");

                // 다른 배울 수 있는 레벨이 있으면 갱신, 없으면 닫기
                if (currentSkillSystem != null)
                {
                    int[] newAvailableLevels = currentSkillSystem.GetAvailableSkillLevels();
                    if (newAvailableLevels.Length > 0)
                    {
                        availableLevels = newAvailableLevels;
                        currentLevelIndex = 0;
                        currentLevel = availableLevels[0];
                        LoadSkillChoiceForCurrentLevel();
                    }
                    else
                    {
                        CloseSkillLearningUI();
                    }
                }
                else
                {
                    CloseSkillLearningUI();
                }
            }
        }

        // ── 레벨 네비게이션 ──

        private void ShowPreviousLevel()
        {
            if (availableLevels == null || currentLevelIndex <= 0) return;

            currentLevelIndex--;
            currentLevel = availableLevels[currentLevelIndex];
            LoadSkillChoiceForCurrentLevel();
        }

        private void ShowNextLevel()
        {
            if (availableLevels == null || currentLevelIndex >= availableLevels.Length - 1) return;

            currentLevelIndex++;
            currentLevel = availableLevels[currentLevelIndex];
            LoadSkillChoiceForCurrentLevel();
        }

        private void UpdateLevelNavigationButtons()
        {
            bool hasMultipleLevels = availableLevels != null && availableLevels.Length > 1;

            if (prevLevelButton != null)
            {
                prevLevelButton.gameObject.SetActive(hasMultipleLevels);
                prevLevelButton.interactable = currentLevelIndex > 0;
            }

            if (nextLevelButton != null)
            {
                nextLevelButton.gameObject.SetActive(hasMultipleLevels);
                nextLevelButton.interactable = availableLevels != null && currentLevelIndex < availableLevels.Length - 1;
            }
        }

        // ── 에러/메시지 ──

        private void ShowErrorMessage(string message)
        {
            if (errorMessageText != null)
            {
                errorMessageText.text = message;
                errorMessageText.color = Color.red;
                errorMessageText.gameObject.SetActive(true);
            }

            Debug.LogWarning($"스킬 학습 에러: {message}");
        }

        private void ClearErrorMessage()
        {
            if (errorMessageText != null)
            {
                errorMessageText.text = "";
                errorMessageText.gameObject.SetActive(false);
            }
        }

        // ── 유틸리티 ──

        private string GetSkillTypeDisplay(SkillData skill)
        {
            return skill.skillType switch
            {
                SkillType.Active => "액티브",
                SkillType.Passive => "패시브",
                SkillType.Toggle => "토글",
                SkillType.Triggered => "조건부",
                _ => "스킬"
            };
        }

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
            // 이벤트 구독 해제
            if (currentSkillSystem != null)
            {
                currentSkillSystem.OnSkillLearningError -= ShowErrorMessage;
                currentSkillSystem.OnSkillLearned -= OnSkillLearnedHandler;
            }

            // 버튼 리스너 정리
            for (int i = 0; i < skillChoiceButtons.Length; i++)
            {
                if (skillChoiceButtons[i] != null)
                    skillChoiceButtons[i].onClick.RemoveAllListeners();
            }
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();
            if (prevLevelButton != null) prevLevelButton.onClick.RemoveAllListeners();
            if (nextLevelButton != null) nextLevelButton.onClick.RemoveAllListeners();

            // 시간 스케일 복구
            Time.timeScale = 1f;
        }
    }
}
