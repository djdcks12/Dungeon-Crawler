using UnityEngine;
using UnityEngine.UI;
// using TMPro; // TextMeshPro not available
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 스탯 UI 시스템
    /// 플레이어 스탯 표시 및 스탯 포인트 분배 UI
    /// </summary>
    public class StatsUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject statsPanel;
        [SerializeField] private Button closeStatsButton;
        
        [Header("Player Info")]
        [SerializeField] private Text playerNameText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text expText;
        [SerializeField] private Slider expSlider;
        [SerializeField] private Text availablePointsText;
        
        [Header("Health & Mana")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Text healthText;
        [SerializeField] private Slider manaSlider;
        [SerializeField] private Text manaText;
        
        [Header("Primary Stats")]
        [SerializeField] private StatUIElement strStat;
        [SerializeField] private StatUIElement agiStat;
        [SerializeField] private StatUIElement vitStat;
        [SerializeField] private StatUIElement intStat;
        [SerializeField] private StatUIElement defStat;
        [SerializeField] private StatUIElement mdefStat;
        [SerializeField] private StatUIElement lukStat;
        
        [Header("Derived Stats")]
        [SerializeField] private Text attackDamageText;
        [SerializeField] private Text magicDamageText;
        [SerializeField] private Text moveSpeedText;
        [SerializeField] private Text attackSpeedText;
        [SerializeField] private Text critChanceText;
        [SerializeField] private Text critDamageText;
        
        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.C;
        [SerializeField] private bool showStatsOnStart = false;
        
        // 참조
        private PlayerStatsManager statsManager;
        private Dictionary<StatType, StatUIElement> statElements;
        private bool isStatsVisible = false;
        
        private void Awake()
        {
            InitializeStatElements();
            SetupButtonEvents();
        }
        
        private void Start()
        {
            // 로컬 플레이어의 StatsManager 찾기
            FindLocalPlayerStatsManager();
            
            // 초기 상태 설정 - statsPanel은 처음에 꺼진 상태
            if (statsPanel != null)
            {
                statsPanel.SetActive(false);
                isStatsVisible = false;
            }
            
        }
        
        
        private void OnEnable()
        {
            // 스탯 매니저 이벤트 구독
            if (statsManager != null)
            {
                statsManager.OnStatsUpdated += OnStatsUpdated;
                statsManager.OnHealthChanged += OnHealthChanged;
                statsManager.OnLevelChanged += OnLevelChanged;
            }
        }
        
        private void OnDisable()
        {
            // 이벤트 구독 해제
            if (statsManager != null)
            {
                statsManager.OnStatsUpdated -= OnStatsUpdated;
                statsManager.OnHealthChanged -= OnHealthChanged;
                statsManager.OnLevelChanged -= OnLevelChanged;
                
                // EquipmentManager 이벤트 구독 해제
                var equipmentManager = statsManager.GetComponent<EquipmentManager>();
                if (equipmentManager != null)
                {
                    equipmentManager.OnEquipmentChanged -= OnEquipmentChanged;
                }
            }
        }
        
        private void InitializeStatElements()
        {
            statElements = new Dictionary<StatType, StatUIElement>
            {
                { StatType.STR, strStat },
                { StatType.AGI, agiStat },
                { StatType.VIT, vitStat },
                { StatType.INT, intStat },
                { StatType.DEF, defStat },
                { StatType.MDEF, mdefStat },
                { StatType.LUK, lukStat }
            };
            
            // 각 스탯 UI 요소 초기화
            foreach (var kvp in statElements)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.Initialize(kvp.Key, GetStatDisplayName(kvp.Key));
                    kvp.Value.OnStatIncreased += OnStatIncreaseClicked;
                    kvp.Value.OnStatDecreased += OnStatDecreaseClicked;
                }
            }
        }
        
        private string GetStatDisplayName(StatType statType)
        {
            switch (statType)
            {
                case StatType.STR: return "힘 (STR)";
                case StatType.AGI: return "민첩 (AGI)";
                case StatType.VIT: return "체력 (VIT)";
                case StatType.INT: return "지능 (INT)";
                case StatType.DEF: return "물리방어 (DEF)";
                case StatType.MDEF: return "마법방어 (MDEF)";
                case StatType.LUK: return "운 (LUK)";
                default: return statType.ToString();
            }
        }
        
        private void SetupButtonEvents()
        {
            if (closeStatsButton != null)
            {
                closeStatsButton.onClick.AddListener(CloseStatsPanel);
            }
        }
        
        private void FindLocalPlayerStatsManager()
        {
            // NetworkManager로 로컬 플레이어 찾기
            var netManager = Unity.Netcode.NetworkManager.Singleton;
            if (netManager != null && netManager.LocalClient != null && netManager.LocalClient.PlayerObject != null)
            {
                var playerObj = netManager.LocalClient.PlayerObject;
                statsManager = playerObj.GetComponent<PlayerStatsManager>();

                // EquipmentManager 이벤트도 구독
                var equipmentManager = playerObj.GetComponent<EquipmentManager>();
                if (equipmentManager != null)
                {
                    equipmentManager.OnEquipmentChanged += OnEquipmentChanged;
                    Debug.Log("📊 StatsUI subscribed to EquipmentManager events");
                }
            }
            
            // 이벤트 구독
            if (statsManager != null)
            {
                statsManager.OnStatsUpdated += OnStatsUpdated;
                statsManager.OnHealthChanged += OnHealthChanged;
                statsManager.OnLevelChanged += OnLevelChanged;
                
                // 초기 UI 업데이트
                UpdateAllUI();
            }
        }
        
        public void ToggleStatsPanel()
        {
            isStatsVisible = !isStatsVisible;
            
            if (statsPanel != null)
            {
                // 디버깅: 어떤 GameObject를 켜고 끄는지 확인
                Debug.Log($"Toggling GameObject: {statsPanel.name}");
                Debug.Log($"This StatsUI is on GameObject: {gameObject.name}");
                Debug.Log($"Are they the same? {statsPanel == gameObject}");
                
                statsPanel.SetActive(isStatsVisible);
            }
            
            if (isStatsVisible)
            {
                UpdateAllUI();
            }
        }
        
        public void CloseStatsPanel()
        {
            isStatsVisible = false;
            if (statsPanel != null)
            {
                statsPanel.SetActive(false);
            }
        }
        
        // 스탯 포인트 수동 분배 시스템 제거됨 (종족별 고정 성장)
        public void OnStatIncreaseClicked(StatType statType)
        {
            // 더 이상 수동으로 스탯을 올릴 수 없음 
            // 레벨업 시 종족별로 자동 성장
            Debug.Log("Manual stat allocation is no longer available. Stats grow automatically based on race.");
        }
        
        public void OnStatDecreaseClicked(StatType statType)
        {
            // 더 이상 수동으로 스탯을 내릴 수 없음 
            // 레벨업 시 종족별로 자동 성장
            Debug.Log("Manual stat allocation is no longer available. Stats grow automatically based on race.");
        }
        
        private void UpdateAllUI()
        {
            if (statsManager == null || statsManager.CurrentStats == null) return;
            
            UpdatePlayerInfo();
            UpdateHealthMana();
            UpdatePrimaryStats();
            UpdateDerivedStats();
        }
        
        private void UpdatePlayerInfo()
        {
            var stats = statsManager.CurrentStats;
            
            if (playerNameText != null)
            {
                playerNameText.text = $"Player {statsManager.gameObject.name}";
            }
            
            if (levelText != null)
            {
                levelText.text = $"Level {stats.CurrentLevel}";
            }
            
            if (expText != null)
            {
                expText.text = $"EXP: {stats.CurrentExperience:N0} / {stats.ExpToNextLevel:N0}";
            }
            
            if (expSlider != null)
            {
                expSlider.value = stats.ExpToNextLevel > 0 ? (float)stats.CurrentExperience / stats.ExpToNextLevel : 0f;
            }
            
            if (availablePointsText != null)
            {
                // 스탯 포인트 시스템 제거됨 - 종족별 자동 성장 정보 표시
                availablePointsText.text = $"Race: {stats.CharacterRace} (Auto Growth)";
            }
        }
        
        private void UpdateHealthMana()
        {
            var stats = statsManager.CurrentStats;
            
            if (healthSlider != null)
            {
                healthSlider.value = stats.MaxHP > 0 ? stats.CurrentHP / stats.MaxHP : 0f;
            }
            
            if (healthText != null)
            {
                healthText.text = $"{stats.CurrentHP:F0} / {stats.MaxHP:F0}";
            }
            
            if (manaSlider != null)
            {
                manaSlider.value = stats.MaxMP > 0 ? stats.CurrentMP / stats.MaxMP : 0f;
            }
            
            if (manaText != null)
            {
                manaText.text = $"{stats.CurrentMP:F0} / {stats.MaxMP:F0}";
            }
        }
        
        private void UpdatePrimaryStats()
        {
            if (statsManager == null) return;
            
            foreach (var kvp in statElements)
            {
                if (kvp.Value != null)
                {
                    var statInfo = statsManager.GetStatInfo(kvp.Key);
                    kvp.Value.UpdateValues((int)statInfo.baseValue, (int)statInfo.bonusValue);
                    kvp.Value.SetButtonsInteractable(false, false); // 수동 할당 불가
                }
            }
        }
        
        private void UpdateDerivedStats()
        {
            var stats = statsManager.CurrentStats;
            
            if (attackDamageText != null)
            {
                // 민댐/맥댐 범위로 표시 (더 정확함)
                var physicalDamage = stats.CombatStats.physicalDamage;
                attackDamageText.text = $"Attack: {physicalDamage.minDamage:F0}-{physicalDamage.maxDamage:F0} (Avg: {stats.AttackDamage:F1})";
            }
            
            if (magicDamageText != null)
            {
                // 마법 데미지도 범위로 표시
                var magicalDamage = stats.CombatStats.magicalDamage;
                magicDamageText.text = $"Magic: {magicalDamage.minDamage:F0}-{magicalDamage.maxDamage:F0} (Avg: {stats.MagicDamage:F1})";
            }
            
            if (moveSpeedText != null)
            {
                moveSpeedText.text = $"Speed: {stats.MoveSpeed:F1}";
            }
            
            if (attackSpeedText != null)
            {
                attackSpeedText.text = $"AS: {stats.AttackSpeed:F2}";
            }
            
            if (critChanceText != null)
            {
                critChanceText.text = $"Crit: {stats.CriticalChance:P1}";
            }
            
            if (critDamageText != null)
            {
                critDamageText.text = $"Crit DMG: {stats.CriticalDamage:P0}";
            }
        }
        
        // 이벤트 콜백들
        private void OnStatsUpdated(PlayerStatsData stats)
        {
            if (isStatsVisible)
            {
                UpdateAllUI();
            }
        }
        
        private void OnEquipmentChanged(EquipmentSlot slot, ItemInstance item)
        {
            Debug.Log($"📊 StatsUI detected equipment change: {slot} -> {item?.ItemData?.ItemName ?? "Empty"}");
            
            // 장비 변경 시 UI 즉시 업데이트
            if (isStatsVisible)
            {
                UpdateAllUI();
            }
        }
        
        private void OnHealthChanged(float currentHP, float maxHP)
        {
            if (healthSlider != null)
            {
                healthSlider.value = maxHP > 0 ? currentHP / maxHP : 0f;
            }
            
            if (healthText != null)
            {
                healthText.text = $"{currentHP:F0} / {maxHP:F0}";
            }
        }
        
        private void OnLevelChanged(int newLevel)
        {
            if (levelText != null)
            {
                levelText.text = $"Level {newLevel}";
            }
            
            // 레벨업 시 전체 UI 업데이트
            if (isStatsVisible)
            {
                UpdateAllUI();
            }
        }
    }
}