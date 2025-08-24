using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 플레이어 메인 HUD 시스템
    /// HP/MP바, 경험치바, 골드 표시 등 핵심 UI 요소들을 관리
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        [Header("HUD 패널들")]
        [SerializeField] private GameObject mainHUDPanel;
        [SerializeField] private GameObject healthPanel;
        [SerializeField] private GameObject resourcePanel;
        
        [Header("체력/마나 UI")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Slider manaSlider;
        [SerializeField] private Text healthText;
        [SerializeField] private Text manaText;
        
        [Header("경험치 UI")]
        [SerializeField] private Slider experienceSlider;
        [SerializeField] private Text levelText;
        [SerializeField] private Text expText;
        
        [Header("골드 UI")]
        [SerializeField] private Text goldText;
        [SerializeField] private Image goldIcon;
        
        [Header("상태 UI")]
        [SerializeField] private Text raceText;
        [SerializeField] private Transform statusEffectsParent;
        
        // 컴포넌트 참조
        private PlayerStatsManager statsManager;
        private bool isInitialized = false;
        
        private void Awake()
        {
            // 기본적으로 HUD 활성화
            SetHUDActive(true);
        }
        
        private void Start()
        {
            // 로컬 플레이어의 스탯 매니저 찾기
            InitializeForLocalPlayer();
        }
        
        // Update 제거 - NetworkVariable 이벤트 기반으로 최적화
        
        /// <summary>
        /// 로컬 플레이어용 초기화
        /// </summary>
        private void InitializeForLocalPlayer()
        {
            // 로컬 플레이어 찾기
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
            {
                var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                if (localPlayer != null)
                {
                    statsManager = localPlayer.GetComponent<PlayerStatsManager>();
                    if (statsManager != null)
                    {
                        isInitialized = true;
                        
                        // NetworkVariable 이벤트 구독 (실시간 동기화)
                        statsManager.OnStatsUpdated += OnStatsChanged;
                        statsManager.OnLevelChanged += OnLevelUp;
                        statsManager.OnHealthChanged += OnHealthChanged;
                        statsManager.OnManaChanged += OnManaChanged;
                        statsManager.OnExperienceChanged += OnExperienceChanged;
                        
                        // 초기 UI 설정
                        InitializeUI();
                        
                        Debug.Log("✅ PlayerHUD initialized for local player");
                    }
                }
            }
            
            // 로컬 플레이어를 찾지 못하면 재시도
            if (!isInitialized)
            {
                Invoke(nameof(InitializeForLocalPlayer), 0.5f);
            }
        }
        
        /// <summary>
        /// UI 초기 설정
        /// </summary>
        private void InitializeUI()
        {
            if (statsManager?.CurrentStats == null) return;
            
            var stats = statsManager.CurrentStats;
            
            // 종족 표시
            SetText(raceText, GetRaceDisplayName(stats.CharacterRace));
            
            // 슬라이더 초기화
            if (healthSlider != null)
            {
                healthSlider.maxValue = stats.MaxHP;
                healthSlider.value = stats.CurrentHP;
            }
            
            if (manaSlider != null)
            {
                manaSlider.maxValue = stats.MaxMP;
                manaSlider.value = stats.CurrentMP;
            }
            
            if (experienceSlider != null)
            {
                experienceSlider.maxValue = stats.ExpToNextLevel;
                experienceSlider.value = stats.CurrentExperience;
            }
            
            UpdateAllTexts();
        }
        
        /// <summary>
        /// HP 변경 이벤트 처리
        /// </summary>
        private void OnHealthChanged(float currentHP, float maxHP)
        {
            UpdateSlider(healthSlider, currentHP, maxHP);
            SetText(healthText, $"{currentHP:F0} / {maxHP:F0}");
        }
        
        /// <summary>
        /// MP 변경 이벤트 처리 
        /// </summary>
        private void OnManaChanged(float currentMP, float maxMP)
        {
            UpdateSlider(manaSlider, currentMP, maxMP);
            SetText(manaText, $"{currentMP:F0} / {maxMP:F0}");
        }
        
        /// <summary>
        /// 경험치 변경 이벤트 처리
        /// </summary>
        private void OnExperienceChanged()
        {
            if (statsManager?.CurrentStats == null) return;
            
            var stats = statsManager.CurrentStats;
            
            // 경험치 슬라이더 업데이트
            if (experienceSlider != null)
            {
                experienceSlider.maxValue = stats.ExpToNextLevel;
                experienceSlider.value = stats.CurrentExperience;
            }
            
            // 경험치 텍스트 업데이트
            SetText(expText, $"{stats.CurrentExperience} / {stats.ExpToNextLevel}");
        }
        
        /// <summary>
        /// 모든 텍스트 업데이트
        /// </summary>
        private void UpdateAllTexts()
        {
            if (statsManager?.CurrentStats == null) return;
            
            var stats = statsManager.CurrentStats;
            
            // HP/MP 텍스트
            SetText(healthText, $"{stats.CurrentHP:F0} / {stats.MaxHP:F0}");
            SetText(manaText, $"{stats.CurrentMP:F0} / {stats.MaxMP:F0}");
            
            // 레벨/경험치 텍스트
            SetText(levelText, $"Lv.{stats.CurrentLevel}");
            SetText(expText, $"{stats.CurrentExperience} / {stats.ExpToNextLevel}");
            
            // 골드 텍스트
            SetText(goldText, $"{stats.Gold:N0}");
        }
        
        /// <summary>
        /// 슬라이더 안전 업데이트
        /// </summary>
        private void UpdateSlider(Slider slider, float current, float max)
        {
            if (slider == null) return;
            
            slider.maxValue = max;
            slider.value = current;
            
            // 색상 변경 (HP 위험 시 빨간색 등)
            if (slider == healthSlider)
            {
                var fillImage = slider.fillRect?.GetComponent<Image>();
                if (fillImage != null)
                {
                    float healthPercent = current / max;
                    if (healthPercent <= 0.25f)
                        fillImage.color = Color.red;
                    else if (healthPercent <= 0.5f)
                        fillImage.color = Color.yellow;
                    else
                        fillImage.color = Color.green;
                }
            }
        }
        
        /// <summary>
        /// 스탯 변경 이벤트 처리
        /// </summary>
        private void OnStatsChanged(PlayerStats stats)
        {
            // 레벨과 골드 텍스트 업데이트 (HP/MP/EXP는 별도 이벤트에서 처리)
            SetText(levelText, $"Lv.{stats.CurrentLevel}");
            SetText(goldText, $"{stats.Gold:N0}");
        }
        
        /// <summary>
        /// 레벨업 이벤트 처리
        /// </summary>
        private void OnLevelUp(int newLevel)
        {
            Debug.Log($"🌟 Level Up! New level: {newLevel}");
            
            // 레벨업 효과 (간단한 텍스트 애니메이션 등)
            if (levelText != null)
            {
                StartCoroutine(LevelUpAnimation());
            }
        }
        
        /// <summary>
        /// 레벨업 애니메이션
        /// </summary>
        private System.Collections.IEnumerator LevelUpAnimation()
        {
            if (levelText == null) yield break;
            
            var originalColor = levelText.color;
            var originalScale = levelText.transform.localScale;
            
            // 반짝임 효과
            for (int i = 0; i < 3; i++)
            {
                levelText.color = Color.yellow;
                levelText.transform.localScale = originalScale * 1.2f;
                yield return new WaitForSeconds(0.2f);
                
                levelText.color = originalColor;
                levelText.transform.localScale = originalScale;
                yield return new WaitForSeconds(0.2f);
            }
        }
        
        /// <summary>
        /// HUD 활성화/비활성화
        /// </summary>
        public void SetHUDActive(bool active)
        {
            if (mainHUDPanel != null)
            {
                mainHUDPanel.SetActive(active);
            }
        }
        
        /// <summary>
        /// HUD 토글
        /// </summary>
        public void ToggleHUD()
        {
            if (mainHUDPanel != null)
            {
                SetHUDActive(!mainHUDPanel.activeInHierarchy);
            }
        }
        
        /// <summary>
        /// 종족 표시명 변환
        /// </summary>
        private string GetRaceDisplayName(Race race)
        {
            switch (race)
            {
                case Race.Human: return "인간";
                case Race.Elf: return "엘프";
                case Race.Beast: return "수인";
                case Race.Machina: return "기계족";
                default: return "알 수 없음";
            }
        }
        
        /// <summary>
        /// 안전한 텍스트 설정
        /// </summary>
        private void SetText(Text textComponent, string text)
        {
            if (textComponent != null)
            {
                textComponent.text = text;
            }
        }
        
        /// <summary>
        /// 컴포넌트 정리
        /// </summary>
        private void OnDestroy()
        {
            if (statsManager != null)
            {
                statsManager.OnStatsUpdated -= OnStatsChanged;
                statsManager.OnLevelChanged -= OnLevelUp;
                statsManager.OnHealthChanged -= OnHealthChanged;
                statsManager.OnManaChanged -= OnManaChanged;
                statsManager.OnExperienceChanged -= OnExperienceChanged;
            }
        }
        
        /// <summary>
        /// 디버그용 HUD 정보 표시
        /// </summary>
        [ContextMenu("Show HUD Debug Info")]
        private void ShowDebugInfo()
        {
            Debug.Log($"=== PlayerHUD Debug Info ===");
            Debug.Log($"Initialized: {isInitialized}");
            Debug.Log($"StatsManager: {(statsManager != null ? "Found" : "Missing")}");
            Debug.Log($"MainHUD Active: {(mainHUDPanel != null ? mainHUDPanel.activeInHierarchy : false)}");
        }
    }
}