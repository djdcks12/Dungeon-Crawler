using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 스킬 마스터 NPC - 골드로 스킬을 가르치는 직업별 NPC
    /// </summary>
    public class SkillMasterNPC : NetworkBehaviour, IInteractable
    {
        [Header("NPC Settings")]
        [SerializeField] private string npcName = "스킬 마스터";
        [SerializeField] private JobType jobType = JobType.Navigator;
        [SerializeField] private string greeting = "어서 오게, 젊은 모험가여.";
        [SerializeField] private string learningMessage = "어떤 기술을 배우고 싶은가?";
        [SerializeField] private string farewellMessage = "훌륭한 선택이었다. 행운을 빈다!";
        
        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite npcSprite;
        [SerializeField] private GameObject interactionIndicator;
        
        [Header("Interaction")]
        [SerializeField] private float interactionRange = 3.0f;
        [SerializeField] private KeyCode interactionKey = KeyCode.F;
        
        // 상호작용 상태
        private bool playerInRange = false;
        private GameObject currentPlayer = null;
        private NewSkillLearningSystem currentSkillSystem = null;
        
        // UI 참조
        private SkillLearningUI skillLearningUI;
        
        // 이벤트
        public System.Action<PlayerController> OnPlayerEnterRange;
        public System.Action<PlayerController> OnPlayerExitRange;
        
        public string InteractionPrompt => $"[{interactionKey}] {npcName}와 대화하기";
        public float InteractionRange => interactionRange;
        public JobType NPCJobType => jobType;
        
        private void Awake()
        {
            SetupVisuals();
            skillLearningUI = FindFirstObjectByType<SkillLearningUI>();
        }
        
        private void SetupVisuals()
        {
            if (spriteRenderer != null && npcSprite != null)
            {
                spriteRenderer.sprite = npcSprite;
            }
            
            if (interactionIndicator != null)
            {
                interactionIndicator.SetActive(false);
            }
        }
        
        private void Update()
        {
            if (playerInRange && currentPlayer != null)
            {
                if (Input.GetKeyDown(interactionKey))
                {
                    StartSkillLearningInteraction();
                }
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            var playerController = other.GetComponent<PlayerController>();
            if (playerController != null && playerController.IsOwner)
            {
                OnPlayerEnterInteractionRange(playerController.gameObject);
            }
        }
        
        private void OnTriggerExit2D(Collider2D other)
        {
            var playerController = other.GetComponent<PlayerController>();
            if (playerController != null && playerController.IsOwner)
            {
                OnPlayerExitInteractionRange();
            }
        }
        
        private void OnPlayerEnterInteractionRange(GameObject player)
        {
            playerInRange = true;
            currentPlayer = player;
            currentSkillSystem = player.GetComponent<NewSkillLearningSystem>();
            
            // 상호작용 인디케이터 활성화
            if (interactionIndicator != null)
            {
                interactionIndicator.SetActive(true);
            }
            
            // UI에 상호작용 프롬프트 표시
            ShowInteractionPrompt(true);
            
            var playerController = player.GetComponent<PlayerController>();
            OnPlayerEnterRange?.Invoke(playerController);
        }
        
        private void OnPlayerExitInteractionRange()
        {
            playerInRange = false;
            currentPlayer = null;
            currentSkillSystem = null;
            
            // 상호작용 인디케이터 비활성화
            if (interactionIndicator != null)
            {
                interactionIndicator.SetActive(false);
            }
            
            // UI에서 상호작용 프롬프트 숨기기
            ShowInteractionPrompt(false);
            
            // 스킬 학습 UI가 열려있다면 닫기
            if (skillLearningUI != null && skillLearningUI.IsOpen)
            {
                skillLearningUI.CloseSkillLearningUI();
            }
        }
        
        /// <summary>
        /// 스킬 학습 상호작용 시작
        /// </summary>
        private void StartSkillLearningInteraction()
        {
            if (currentSkillSystem == null)
            {
                Debug.LogWarning("플레이어에게 NewSkillLearningSystem이 없습니다.");
                return;
            }
            
            // 플레이어의 직업과 NPC 직업 확인
            var playerStats = currentPlayer.GetComponent<PlayerStatsManager>();
            if (playerStats == null)
            {
                ShowNPCMessage("당신의 정보를 확인할 수 없군요.");
                return;
            }
            
            JobType playerJobType = playerStats.CurrentJobType;
            if (playerJobType != jobType)
            {
                ShowNPCMessage($"죄송하지만, 저는 {GetJobDisplayName(jobType)} 전용 마스터입니다. {GetJobDisplayName(playerJobType)} 마스터를 찾아보세요.");
                return;
            }
            
            // 학습 가능한 스킬 레벨 확인
            int[] availableLevels = currentSkillSystem.GetAvailableSkillLevels();
            if (availableLevels.Length == 0)
            {
                ShowNPCMessage("현재 배울 수 있는 새로운 기술이 없습니다. 레벨을 더 올리고 오세요.");
                return;
            }
            
            // 스킬 학습 UI 열기
            OpenSkillLearningUI(availableLevels);
        }
        
        /// <summary>
        /// 스킬 학습 UI 열기
        /// </summary>
        private void OpenSkillLearningUI(int[] availableLevels)
        {
            if (skillLearningUI == null)
            {
                skillLearningUI = FindFirstObjectByType<SkillLearningUI>();
            }
            if (skillLearningUI == null)
            {
                var uiManager = UIManager.Instance;
                if (uiManager != null)
                {
                    skillLearningUI = uiManager.GetUI<SkillLearningUI>();
                }
            }
            if (skillLearningUI == null)
            {
                Debug.LogError("SkillLearningUI를 찾을 수 없습니다.");
                return;
            }
            
            ShowNPCMessage(learningMessage);
            skillLearningUI.OpenSkillLearningUI(this, currentSkillSystem, availableLevels);
        }
        
        /// <summary>
        /// 스킬 학습 시도 처리
        /// </summary>
        public void ProcessSkillLearning(int level, int choiceIndex)
        {
            if (currentSkillSystem == null) return;
            
            currentSkillSystem.AttemptSkillLearning(level, choiceIndex, this);
            
            // 학습 성공 시 축하 메시지
            ShowNPCMessage(farewellMessage);
        }
        
        /// <summary>
        /// NPC 메시지 표시
        /// </summary>
        private void ShowNPCMessage(string message)
        {
            // 채팅 시스템이나 대화 UI를 통해 메시지 표시
            var chatUI = FindFirstObjectByType<ChatUI>();
            if (chatUI != null)
            {
                chatUI.AddSystemMessage($"[{npcName}] {message}");
            }
            else
            {
                Debug.Log($"[{npcName}] {message}");
            }
        }
        
        /// <summary>
        /// 상호작용 프롬프트 표시/숨기기
        /// </summary>
        private void ShowInteractionPrompt(bool show)
        {
            // UI 시스템을 통해 상호작용 프롬프트 표시
            // 임시로 Debug 메시지로 대체
            if (show)
            {
                Debug.Log($"상호작용 가능: {InteractionPrompt}");
            }
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
        
        /// <summary>
        /// IInteractable 구현
        /// </summary>
        public bool CanInteract(GameObject interactor)
        {
            if (!playerInRange || currentPlayer != interactor) return false;
            
            var playerController = interactor.GetComponent<PlayerController>();
            return playerController != null && playerController.IsOwner;
        }
        
        public void Interact(GameObject interactor)
        {
            if (CanInteract(interactor))
            {
                StartSkillLearningInteraction();
            }
        }

        public override void OnDestroy()
        {
            OnPlayerEnterRange = null;
            OnPlayerExitRange = null;
            base.OnDestroy();
        }
    }
    
    /// <summary>
    /// 상호작용 가능한 객체 인터페이스
    /// </summary>
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        float InteractionRange { get; }
        bool CanInteract(GameObject interactor);
        void Interact(GameObject interactor);
    }
}