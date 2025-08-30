using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 스킬 창 UI 시스템
    /// 플레이어가 보유한 스킬들을 표시하고 관리
    /// </summary>
    public class SkillUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject skillPanel;
        [SerializeField] private Button toggleSkillButton;
        [SerializeField] private Button closeButton;
        
        [Header("Skill Tabs")]
        [SerializeField] private Button playerSkillTabButton;
        [SerializeField] private Button soulSkillTabButton;
        [SerializeField] private Text playerSkillTabText;
        [SerializeField] private Text soulSkillTabText;
        
        [Header("Skill Categories")]
        [SerializeField] private Transform skillCategoryContainer;
        [SerializeField] private Button[] categoryButtons;
        [SerializeField] private Text[] categoryTexts;
        
        [Header("Skill Display")]
        [SerializeField] private Transform skillSlotContainer;
        [SerializeField] private GameObject skillSlotPrefab;
        [SerializeField] private ScrollRect skillScrollRect;
        [SerializeField] private Text skillCountText;
        
        [Header("Skill Info Panel")]
        [SerializeField] private GameObject skillInfoPanel;
        [SerializeField] private Image skillIcon;
        [SerializeField] private Text skillNameText;
        [SerializeField] private Text skillDescriptionText;
        [SerializeField] private Text skillCooldownText;
        [SerializeField] private Text skillManaCostText;
        [SerializeField] private Text skillDamageText;
        [SerializeField] private Button learnSkillButton;
        // [SerializeField] private Button upgradeSkillButton; // 업그레이드 시스템 제거
        
        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.K;
        [SerializeField] private int skillsPerRow = 4;
        [SerializeField] private float skillSlotSize = 80f;
        
        // 상태
        private List<SkillSlotUI> skillSlotUIs = new List<SkillSlotUI>();
        private SkillCategory currentCategory = SkillCategory.Warrior;
        private SkillData selectedSkill;
        private bool isOpen = false;
        
        // 탭 상태
        private SkillTabType currentTab = SkillTabType.PlayerSkills;
        private bool showPlayerSkills = true;
        
        // 컴포넌트 참조
        private PlayerStatsManager statsManager;
        private SkillManager skillManager;
        
        // 이벤트
        public System.Action<bool> OnSkillUIToggled;
        public System.Action<SkillData> OnSkillSelected;
        
        private void Start()
        {
            InitializeUI();
            SetupEventListeners();
            CloseSkillUI();
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleSkillUI();
            }
        }
        
        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            // 플레이어 컴포넌트 찾기
            var localPlayer = FindLocalPlayer();
            if (localPlayer != null)
            {
                statsManager = localPlayer.GetComponent<PlayerStatsManager>();
                skillManager = localPlayer.GetComponent<SkillManager>();
            }
            
            // 카테고리 버튼 설정
            SetupCategoryButtons();
            
            // 스킬 슬롯들 생성
            CreateSkillSlots();
            
            // 초기 스킬 정보 패널 숨김
            if (skillInfoPanel != null)
                skillInfoPanel.SetActive(false);
        }
        
        /// <summary>
        /// 이벤트 리스너 설정
        /// </summary>
        private void SetupEventListeners()
        {
            if (toggleSkillButton != null)
                toggleSkillButton.onClick.AddListener(ToggleSkillUI);
                
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseSkillUI);
                
            if (learnSkillButton != null)
                learnSkillButton.onClick.AddListener(LearnSelectedSkill);
                
            // 탭 버튼 이벤트 설정
            if (playerSkillTabButton != null)
                playerSkillTabButton.onClick.AddListener(() => SetSkillTab(SkillTabType.PlayerSkills));
                
            if (soulSkillTabButton != null)
                soulSkillTabButton.onClick.AddListener(() => SetSkillTab(SkillTabType.SoulSkills));
        }
        
        /// <summary>
        /// 카테고리 버튼 설정
        /// </summary>
        private void SetupCategoryButtons()
        {
            if (categoryButtons == null) return;
            
            var categories = System.Enum.GetValues(typeof(SkillCategory));
            
            for (int i = 0; i < categoryButtons.Length && i < categories.Length; i++)
            {
                var category = (SkillCategory)categories.GetValue(i);
                int categoryIndex = i; // 클로저 캡처용
                
                categoryButtons[i].onClick.AddListener(() => SetCategory(category));
                
                if (categoryTexts != null && i < categoryTexts.Length)
                {
                    categoryTexts[i].text = GetCategoryDisplayName(category);
                }
            }
        }
        
        /// <summary>
        /// 스킬 슬롯들 생성
        /// </summary>
        private void CreateSkillSlots()
        {
            if (skillSlotPrefab == null || skillSlotContainer == null) return;
            
            // 기존 슬롯들 정리
            foreach (var slot in skillSlotUIs)
            {
                if (slot != null && slot.gameObject != null)
                    Destroy(slot.gameObject);
            }
            skillSlotUIs.Clear();
            
            // 새 슬롯들 생성 (최대 20개 스킬 슬롯)
            for (int i = 0; i < 20; i++)
            {
                var slotObj = Instantiate(skillSlotPrefab, skillSlotContainer);
                var slotUI = slotObj.GetComponent<SkillSlotUI>();
                
                if (slotUI != null)
                {
                    slotUI.Initialize(i);
                    slotUI.OnSkillClicked += OnSkillSlotClicked;
                    skillSlotUIs.Add(slotUI);
                }
            }
            
            // 그리드 레이아웃 설정
            var gridLayout = skillSlotContainer.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                gridLayout.cellSize = Vector2.one * skillSlotSize;
                gridLayout.constraintCount = skillsPerRow;
            }
        }
        
        /// <summary>
        /// 스킬 UI 토글
        /// </summary>
        public void ToggleSkillUI()
        {
            if (isOpen)
                CloseSkillUI();
            else
                OpenSkillUI();
        }
        
        /// <summary>
        /// 스킬 UI 열기
        /// </summary>
        public void OpenSkillUI()
        {
            if (skillPanel == null) return;
            
            skillPanel.SetActive(true);
            isOpen = true;
            
            RefreshSkillDisplay();
            UpdateTabButtons();
            OnSkillUIToggled?.Invoke(true);
        }
        
        /// <summary>
        /// 스킬 UI 닫기
        /// </summary>
        public void CloseSkillUI()
        {
            if (skillPanel == null) return;
            
            skillPanel.SetActive(false);
            isOpen = false;
            
            OnSkillUIToggled?.Invoke(false);
        }
        
        /// <summary>
        /// 카테고리 설정
        /// </summary>
        public void SetCategory(SkillCategory category)
        {
            currentCategory = category;
            RefreshSkillDisplay();
            UpdateCategoryButtons();
        }
        
        /// <summary>
        /// 스킬 표시 갱신
        /// </summary>
        private void RefreshSkillDisplay()
        {
            if (skillManager == null) return;
            
            var availableSkills = GetFilteredSkills();
            var learnedSkills = skillManager.GetLearnedSkills();
            
            // 스킬 슬롯 업데이트
            for (int i = 0; i < skillSlotUIs.Count; i++)
            {
                if (i < availableSkills.Count)
                {
                    var skill = availableSkills[i];
                    var isLearned = learnedSkills.Contains(skill.skillId);
                    
                    skillSlotUIs[i].SetSkill(skill, isLearned, 1); // 레벨 시스템 없으므로 1로 고정
                    skillSlotUIs[i].gameObject.SetActive(true);
                }
                else
                {
                    skillSlotUIs[i].ClearSkill();
                    skillSlotUIs[i].gameObject.SetActive(false);
                }
            }
            
            // 스킬 개수 표시
            if (skillCountText != null)
            {
                skillCountText.text = $"Skills: {learnedSkills.Count}/{availableSkills.Count}";
            }
        }
        
        /// <summary>
        /// 필터링된 스킬 목록 가져오기
        /// </summary>
        private List<SkillData> GetFilteredSkills()
        {
            if (skillManager == null) return new List<SkillData>();
            
            List<SkillData> allSkills;
            
            if (currentTab == SkillTabType.PlayerSkills)
            {
                allSkills = skillManager.GetLearnableSkills();
            }
            else
            {
                allSkills = skillManager.GetLearnedSkills()
                    .Where(skillId => skillId.StartsWith("monster_"))
                    .Select(skillId => skillManager.GetSkillById(skillId))
                    .Where(skill => skill != null)
                    .ToList();
            }
            
            return allSkills.Where(skill => skill.category == currentCategory).ToList();
        }
        
        /// <summary>
        /// 스킬 슬롯 클릭 처리
        /// </summary>
        private void OnSkillSlotClicked(SkillData skill)
        {
            SelectSkill(skill);
        }
        
        /// <summary>
        /// 스킬 선택
        /// </summary>
        private void SelectSkill(SkillData skill)
        {
            selectedSkill = skill;
            UpdateSkillInfo();
            OnSkillSelected?.Invoke(skill);
        }
        
        /// <summary>
        /// 스킬 정보 패널 업데이트
        /// </summary>
        private void UpdateSkillInfo()
        {
            if (selectedSkill == null || skillInfoPanel == null)
            {
                if (skillInfoPanel != null)
                    skillInfoPanel.SetActive(false);
                return;
            }
            
            skillInfoPanel.SetActive(true);
            
            // 기본 정보
            if (skillIcon != null)
                skillIcon.sprite = selectedSkill.skillIcon;
                
            if (skillNameText != null)
                skillNameText.text = selectedSkill.skillName;
                
            if (skillDescriptionText != null)
                skillDescriptionText.text = selectedSkill.description;
                
            // 스킬 스탯
            var playerLevel = statsManager?.CurrentStats?.CurrentLevel ?? 1;
            
            if (skillCooldownText != null)
                skillCooldownText.text = $"Cooldown: {selectedSkill.cooldown:F1}s";
                
            if (skillManaCostText != null)
                skillManaCostText.text = $"Mana Cost: {selectedSkill.GetManaCost(playerLevel):F0}";
                
            if (skillDamageText != null && selectedSkill.baseDamage > 0)
            {
                var damage = selectedSkill.CalculateDamage(statsManager.CurrentStats);
                skillDamageText.text = $"Damage: {damage:F0}";
            }
            
            // 버튼 상태 업데이트
            UpdateSkillButtons();
        }
        
        /// <summary>
        /// 스킬 버튼 상태 업데이트
        /// </summary>
        private void UpdateSkillButtons()
        {
            if (selectedSkill == null || skillManager == null) return;
            
            bool isLearned = skillManager.GetLearnedSkills().Contains(selectedSkill.skillId);
            bool canLearn = selectedSkill.CanLearn(statsManager.CurrentStats, skillManager.GetLearnedSkills());
            bool isSoulSkill = selectedSkill.skillId.StartsWith("monster_");
            
            // 학습 버튼 - 영혼 스킬은 학습 불가 (이미 획득된 상태)
            if (learnSkillButton != null)
            {
                learnSkillButton.gameObject.SetActive(!isLearned && !isSoulSkill);
                learnSkillButton.interactable = canLearn && !isSoulSkill;
            }
        }
        
        /// <summary>
        /// 선택된 스킬 학습
        /// </summary>
        private void LearnSelectedSkill()
        {
            if (selectedSkill == null || skillManager == null) return;
            
            if (skillManager.LearnSkill(selectedSkill.skillId))
            {
                RefreshSkillDisplay();
                UpdateSkillInfo();
                
                Debug.Log($"Learned skill: {selectedSkill.skillName}");
            }
        }
        
        // 업그레이드 시스템 제거
        // private void UpgradeSelectedSkill() { }
        
        /// <summary>
        /// 카테고리 버튼 상태 업데이트
        /// </summary>
        private void UpdateCategoryButtons()
        {
            if (categoryButtons == null) return;
            
            var categories = System.Enum.GetValues(typeof(SkillCategory));
            
            for (int i = 0; i < categoryButtons.Length && i < categories.Length; i++)
            {
                var category = (SkillCategory)categories.GetValue(i);
                bool isSelected = (category == currentCategory);
                
                // 버튼 색상 변경
                var colors = categoryButtons[i].colors;
                colors.normalColor = isSelected ? Color.yellow : Color.white;
                categoryButtons[i].colors = colors;
            }
        }
        
        /// <summary>
        /// 로컬 플레이어 찾기
        /// </summary>
        private GameObject FindLocalPlayer()
        {
            var players = FindObjectsOfType<PlayerController>();
            foreach (var player in players)
            {
                if (player.IsLocalPlayer)
                    return player.gameObject;
            }
            return null;
        }
        
        /// <summary>
        /// 카테고리 표시명 반환
        /// </summary>
        private string GetCategoryDisplayName(SkillCategory category)
        {
            switch (category)
            {
                // 인간
                case SkillCategory.Warrior: return "전사";
                case SkillCategory.Paladin: return "성기사";
                case SkillCategory.Rogue: return "도적";
                case SkillCategory.Archer: return "궁수";
                
                // 엘프
                case SkillCategory.ElementalMage: return "원소마법";
                case SkillCategory.PureMage: return "순수마법";
                case SkillCategory.NatureMage: return "자연마법";
                case SkillCategory.PsychicMage: return "정신마법";
                case SkillCategory.Nature: return "자연";
                
                // 수인
                case SkillCategory.Berserker: return "광전사";
                case SkillCategory.Hunter: return "사냥꾼";
                case SkillCategory.Assassin: return "암살자";
                case SkillCategory.Beast: return "야수";
                case SkillCategory.Wild: return "야성";
                case SkillCategory.ShapeShift: return "변신";
                case SkillCategory.Hunt: return "사냥";
                case SkillCategory.Combat: return "전투";
                
                // 기계족
                case SkillCategory.HeavyArmor: return "중장갑";
                case SkillCategory.Engineer: return "기술자";
                case SkillCategory.Artillery: return "포격수";
                case SkillCategory.Nanotech: return "나노기술";
                case SkillCategory.Engineering: return "공학";
                case SkillCategory.Energy: return "에너지";
                case SkillCategory.Defense: return "방어";
                case SkillCategory.Hacking: return "해킹";
                
                // 기타
                case SkillCategory.Archery: return "궁술";
                case SkillCategory.Stealth: return "은신";
                case SkillCategory.Spirit: return "정령술";
                
                default: return category.ToString();
            }
        }
        
        /// <summary>
        /// 스킬 탭 설정
        /// </summary>
        public void SetSkillTab(SkillTabType tabType)
        {
            currentTab = tabType;
            showPlayerSkills = (tabType == SkillTabType.PlayerSkills);
            
            RefreshSkillDisplay();
            UpdateTabButtons();
        }
        
        /// <summary>
        /// 탭 버튼 상태 업데이트
        /// </summary>
        private void UpdateTabButtons()
        {
            if (playerSkillTabButton != null)
            {
                var colors = playerSkillTabButton.colors;
                colors.normalColor = (currentTab == SkillTabType.PlayerSkills) ? Color.yellow : Color.white;
                playerSkillTabButton.colors = colors;
            }
            
            if (soulSkillTabButton != null)
            {
                var colors = soulSkillTabButton.colors;
                colors.normalColor = (currentTab == SkillTabType.SoulSkills) ? Color.yellow : Color.white;
                soulSkillTabButton.colors = colors;
            }
            
            if (playerSkillTabText != null)
                playerSkillTabText.text = "플레이어 스킬";
                
            if (soulSkillTabText != null)
                soulSkillTabText.text = "영혼 스킬";
        }
        
        /// <summary>
        /// UI 정리
        /// </summary>
        private void OnDestroy()
        {
            foreach (var slot in skillSlotUIs)
            {
                if (slot != null)
                    slot.OnSkillClicked -= OnSkillSlotClicked;
            }
        }
    }
    
    /// <summary>
    /// 스킬 탭 타입
    /// </summary>
    public enum SkillTabType
    {
        PlayerSkills,   // 플레이어 스킬 (골드로 구매)
        SoulSkills      // 영혼 스킬 (몬스터에서 획득)
    }
}