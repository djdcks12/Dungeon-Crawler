using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 스킬 슬롯 UI 컴포넌트
    /// 개별 스킬을 표시하고 상호작용 처리
    /// </summary>
    public class SkillSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI Components")]
        [SerializeField] private Image skillIcon;
        [SerializeField] private Image background;
        [SerializeField] private Text skillLevelText;
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private Text cooldownText;
        [SerializeField] private GameObject learnedIndicator;
        [SerializeField] private GameObject canLearnIndicator;
        
        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color learnedColor = Color.green;
        [SerializeField] private Color canLearnColor = Color.yellow;
        [SerializeField] private Color lockedColor = Color.gray;
        [SerializeField] private Color hoverColor = Color.cyan;
        
        // 스킬 정보
        private SkillData skillData;
        private bool isLearned;
        private int skillLevel;
        private int slotIndex;
        private bool isEmpty = true;
        
        // 쿨다운 관리
        private float cooldownEndTime;
        private float cooldownTotalDuration;
        private bool isOnCooldown = false;
        
        // 이벤트
        public System.Action<SkillData> OnSkillClicked;
        public System.Action<SkillData> OnSkillHover;
        
        private void Update()
        {
            UpdateCooldown();
        }
        
        /// <summary>
        /// 슬롯 초기화
        /// </summary>
        public void Initialize(int index)
        {
            slotIndex = index;
            ClearSkill();
        }
        
        /// <summary>
        /// 스킬 설정
        /// </summary>
        public void SetSkill(SkillData skill, bool learned, int level)
        {
            skillData = skill;
            isLearned = learned;
            skillLevel = level;
            isEmpty = false;
            
            UpdateDisplay();
        }
        
        /// <summary>
        /// 스킬 클리어
        /// </summary>
        public void ClearSkill()
        {
            skillData = null;
            isLearned = false;
            skillLevel = 0;
            isEmpty = true;
            
            UpdateDisplay();
        }
        
        /// <summary>
        /// 표시 업데이트
        /// </summary>
        private void UpdateDisplay()
        {
            if (isEmpty || skillData == null)
            {
                // 빈 슬롯
                SetIcon(null);
                SetBackgroundColor(normalColor);
                SetLevelText("");
                SetIndicators(false, false);
            }
            else
            {
                // 스킬 아이콘
                SetIcon(skillData.skillIcon);
                
                // 배경 색상 - 영혼 스킬 구분
                Color bgColor;
                if (isLearned)
                {
                    bgColor = skillData.skillId.StartsWith("monster_") ? Color.magenta : learnedColor;
                }
                else
                {
                    bgColor = CanLearnSkill() ? canLearnColor : lockedColor;
                }
                SetBackgroundColor(bgColor);
                
                // 레벨 텍스트
                if (isLearned && skillLevel > 1)
                {
                    SetLevelText(skillLevel.ToString());
                }
                else
                {
                    SetLevelText("");
                }
                
                // 인디케이터
                SetIndicators(isLearned, CanLearnSkill());
            }
        }
        
        /// <summary>
        /// 아이콘 설정
        /// </summary>
        private void SetIcon(Sprite icon)
        {
            if (skillIcon != null)
            {
                skillIcon.sprite = icon;
                skillIcon.color = icon != null ? Color.white : Color.clear;
            }
        }
        
        /// <summary>
        /// 배경 색상 설정
        /// </summary>
        private void SetBackgroundColor(Color color)
        {
            if (background != null)
            {
                background.color = color;
            }
        }
        
        /// <summary>
        /// 레벨 텍스트 설정
        /// </summary>
        private void SetLevelText(string text)
        {
            if (skillLevelText != null)
            {
                skillLevelText.text = text;
                skillLevelText.gameObject.SetActive(!string.IsNullOrEmpty(text));
            }
        }
        
        /// <summary>
        /// 인디케이터 설정
        /// </summary>
        private void SetIndicators(bool learned, bool canLearn)
        {
            if (learnedIndicator != null)
                learnedIndicator.SetActive(learned);
                
            if (canLearnIndicator != null)
                canLearnIndicator.SetActive(!learned && canLearn);
        }
        
        /// <summary>
        /// 쿨다운 시작
        /// </summary>
        public void StartCooldown(float duration)
        {
            cooldownTotalDuration = duration;
            cooldownEndTime = Time.time + duration;
            isOnCooldown = true;

            if (cooldownOverlay != null)
                cooldownOverlay.gameObject.SetActive(true);
        }
        
        /// <summary>
        /// 쿨다운 업데이트
        /// </summary>
        private void UpdateCooldown()
        {
            if (!isOnCooldown) return;
            
            float remainingTime = cooldownEndTime - Time.time;
            
            if (remainingTime <= 0)
            {
                // 쿨다운 종료
                isOnCooldown = false;
                
                if (cooldownOverlay != null)
                    cooldownOverlay.gameObject.SetActive(false);
                    
                if (cooldownText != null)
                    cooldownText.gameObject.SetActive(false);
            }
            else
            {
                // 쿨다운 진행 중
                if (cooldownOverlay != null)
                {
                    float progress = cooldownTotalDuration > 0 ? remainingTime / cooldownTotalDuration : 0f;
                    cooldownOverlay.fillAmount = progress;
                }

                if (cooldownText != null)
                {
                    cooldownText.text = remainingTime.ToString("F1");
                    cooldownText.gameObject.SetActive(true);
                }
            }
        }
        
        /// <summary>
        /// 스킬 학습 가능 여부 확인
        /// </summary>
        private bool CanLearnSkill()
        {
            if (skillData == null) return false;
            
            // 실제 SkillManager를 통해 확인해야 하지만, 
            // 여기서는 기본적인 조건만 체크
            var localPlayer = FindLocalPlayer();
            if (localPlayer == null) return false;
            
            var skillManager = localPlayer.GetComponent<SkillManager>();
            var statsManager = localPlayer.GetComponent<PlayerStatsManager>();
            return skillManager != null && statsManager != null && 
                   skillData.CanLearn(statsManager.CurrentStats, skillManager.GetLearnedSkills());
        }
        
        /// <summary>
        /// 로컬 플레이어 찾기
        /// </summary>
        private GameObject FindLocalPlayer()
        {
            var netManager = Unity.Netcode.NetworkManager.Singleton;
            if (netManager != null && netManager.LocalClient != null && netManager.LocalClient.PlayerObject != null)
                return netManager.LocalClient.PlayerObject.gameObject;
            return null;
        }
        
        /// <summary>
        /// 클릭 처리
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isEmpty && skillData != null)
            {
                OnSkillClicked?.Invoke(skillData);
            }
        }
        
        /// <summary>
        /// 마우스 호버 시작
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isEmpty && skillData != null)
            {
                // 호버 효과
                if (background != null)
                {
                    var currentColor = background.color;
                    background.color = Color.Lerp(currentColor, hoverColor, 0.3f);
                }
                
                OnSkillHover?.Invoke(skillData);
            }
        }
        
        /// <summary>
        /// 마우스 호버 종료
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            // 원래 색상으로 복원
            UpdateDisplay();
        }
        
        /// <summary>
        /// 스킬 데이터 반환
        /// </summary>
        public SkillData GetSkillData()
        {
            return skillData;
        }
        
        /// <summary>
        /// 빈 슬롯 여부 반환
        /// </summary>
        public bool IsEmpty()
        {
            return isEmpty;
        }
        
        /// <summary>
        /// 학습된 스킬 여부 반환
        /// </summary>
        public bool IsLearned()
        {
            return isLearned;
        }
        
        /// <summary>
        /// 스킬 레벨 반환
        /// </summary>
        public int GetSkillLevel()
        {
            return skillLevel;
        }

        private void OnDestroy()
        {
            OnSkillClicked = null;
            OnSkillHover = null;
        }
    }
}