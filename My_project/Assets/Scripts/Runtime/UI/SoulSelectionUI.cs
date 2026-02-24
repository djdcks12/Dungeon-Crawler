using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 사망 시 영혼 선택 UI - 하드코어 던전 크롤러
    /// 보유한 영혼 중 하나만 선택하여 보존
    /// </summary>
    public class SoulSelectionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject soulSelectionPanel;
        [SerializeField] private Transform soulButtonContainer;
        [SerializeField] private Button soulButtonPrefab;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button skipButton;
        
        [Header("Info Display")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text selectedSoulInfoText;
        [SerializeField] private Text warningText;
        
        private List<SoulData> availableSouls;
        private SoulData selectedSoul;
        private int selectedIndex = -1;
        private System.Action<SoulData> onSoulSelected;
        private System.Action onSoulSkipped;
        
        private void Awake()
        {
            SetupUI();
        }
        
        /// <summary>
        /// UI 초기 설정
        /// </summary>
        private void SetupUI()
        {
            if (soulSelectionPanel != null)
            {
                soulSelectionPanel.SetActive(false);
            }
            
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(ConfirmSelection);
                confirmButton.interactable = false;
            }
            
            if (skipButton != null)
            {
                skipButton.onClick.AddListener(SkipSelection);
            }
            
            // 기본 텍스트 설정
            if (titleText != null)
            {
                titleText.text = "💀 Choose ONE Soul to Preserve";
            }
            
            if (warningText != null)
            {
                warningText.text = "⚠️ All other souls will be PERMANENTLY DELETED!";
                warningText.color = Color.red;
            }
        }
        
        /// <summary>
        /// 영혼 선택 UI 표시
        /// </summary>
        public void ShowSoulSelection(List<SoulData> souls, System.Action<SoulData> onSelected, System.Action onSkipped = null)
        {
            if (souls == null || souls.Count == 0)
            {
                Debug.Log("💀 No souls to preserve - character dies completely");
                onSkipped?.Invoke();
                return;
            }
            
            availableSouls = new List<SoulData>(souls);
            onSoulSelected = onSelected;
            onSoulSkipped = onSkipped;
            
            CreateSoulButtons();
            
            if (soulSelectionPanel != null)
            {
                soulSelectionPanel.SetActive(true);
            }
            
            // 게임 일시정지
            Time.timeScale = 0f;
            
            Debug.Log($"🔮 Showing soul selection UI with {souls.Count} souls");
        }
        
        /// <summary>
        /// 영혼 버튼들 생성
        /// </summary>
        private void CreateSoulButtons()
        {
            // 기존 버튼들 제거
            ClearSoulButtons();
            
            for (int i = 0; i < availableSouls.Count; i++)
            {
                var soul = availableSouls[i];
                var button = CreateSoulButton(soul, i);
                
                if (button != null && soulButtonContainer != null)
                {
                    button.transform.SetParent(soulButtonContainer, false);
                }
            }
        }
        
        /// <summary>
        /// 개별 영혼 버튼 생성
        /// </summary>
        private Button CreateSoulButton(SoulData soul, int index)
        {
            if (soulButtonPrefab == null || soulButtonContainer == null)
            {
                Debug.LogError("Soul button prefab or container is missing!");
                return null;
            }
            
            var button = Instantiate(soulButtonPrefab);
            var buttonText = button.GetComponentInChildren<Text>();
            
            if (buttonText != null)
            {
                buttonText.text = $"{soul.soulName}\n{GetRarityText(soul.rarity)}\n{GetStatBonusText(soul.statBonus)}";
            }
            
            // 버튼 색상 설정 (희귀도별)
            var buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = GetRarityColor(soul.rarity);
            }
            
            // 클릭 이벤트 설정
            int capturedIndex = index;
            button.onClick.AddListener(() => SelectSoul(capturedIndex));
            
            return button;
        }
        
        /// <summary>
        /// 영혼 선택 처리
        /// </summary>
        private void SelectSoul(int index)
        {
            if (index < 0 || index >= availableSouls.Count) return;
            
            selectedIndex = index;
            selectedSoul = availableSouls[index];
            
            // 선택된 영혼 정보 표시
            if (selectedSoulInfoText != null)
            {
                selectedSoulInfoText.text = $"Selected: {selectedSoul.soulName}\n" +
                                          $"Rarity: {GetRarityText(selectedSoul.rarity)}\n" +
                                          $"Bonuses: {GetDetailedStatText(selectedSoul.statBonus)}\n" +
                                          $"Special: {selectedSoul.specialEffect}";
            }
            
            // 확인 버튼 활성화
            if (confirmButton != null)
            {
                confirmButton.interactable = true;
            }
            
            // 다른 버튼들 시각적 피드백
            UpdateButtonSelection();
            
            Debug.Log($"👆 Selected soul: {selectedSoul.soulName}");
        }
        
        /// <summary>
        /// 버튼 선택 상태 업데이트
        /// </summary>
        private void UpdateButtonSelection()
        {
            if (soulButtonContainer == null) return;
            
            for (int i = 0; i < soulButtonContainer.childCount; i++)
            {
                var button = soulButtonContainer.GetChild(i).GetComponent<Button>();
                if (button != null)
                {
                    var colors = button.colors;
                    colors.normalColor = (i == selectedIndex) ? Color.yellow : Color.white;
                    button.colors = colors;
                }
            }
        }
        
        /// <summary>
        /// 선택 확인
        /// </summary>
        private void ConfirmSelection()
        {
            if (selectedSoul.soulId == 0)
            {
                Debug.LogWarning("No soul selected!");
                return;
            }
            
            Debug.Log($"✅ Confirmed soul preservation: {selectedSoul.soulName}");
            
            HideSoulSelection();
            onSoulSelected?.Invoke(selectedSoul);
        }
        
        /// <summary>
        /// 선택 건너뛰기 (모든 영혼 삭제)
        /// </summary>
        private void SkipSelection()
        {
            Debug.Log("❌ Skipped soul preservation - all souls will be deleted");
            
            HideSoulSelection();
            onSoulSkipped?.Invoke();
        }
        
        /// <summary>
        /// 영혼 선택 UI 숨기기
        /// </summary>
        private void HideSoulSelection()
        {
            if (soulSelectionPanel != null)
            {
                soulSelectionPanel.SetActive(false);
            }
            
            // 게임 재개
            Time.timeScale = 1f;
            
            ClearSoulButtons();
            selectedSoul = new SoulData();
            selectedIndex = -1;
            
            if (confirmButton != null)
            {
                confirmButton.interactable = false;
            }
        }
        
        /// <summary>
        /// 영혼 버튼들 제거
        /// </summary>
        private void ClearSoulButtons()
        {
            if (soulButtonContainer == null) return;
            
            for (int i = soulButtonContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(soulButtonContainer.GetChild(i).gameObject);
            }
        }
        
        /// <summary>
        /// 희귀도 텍스트 반환
        /// </summary>
        private string GetRarityText(SoulRarity rarity)
        {
            return rarity switch
            {
                SoulRarity.Common => "일반",
                SoulRarity.Rare => "희귀",
                SoulRarity.Epic => "영웅",
                SoulRarity.Legendary => "전설",
                _ => "알 수 없음"
            };
        }
        
        /// <summary>
        /// 희귀도별 색상 반환
        /// </summary>
        private Color GetRarityColor(SoulRarity rarity)
        {
            return rarity switch
            {
                SoulRarity.Common => Color.white,
                SoulRarity.Rare => Color.blue,
                SoulRarity.Epic => Color.magenta,
                SoulRarity.Legendary => Color.yellow,
                _ => Color.gray
            };
        }
        
        /// <summary>
        /// 스탯 보너스 간단 텍스트
        /// </summary>
        private string GetStatBonusText(StatBlock statBonus)
        {
            var bonuses = new List<string>();
            
            if (statBonus.strength > 0) bonuses.Add($"STR+{statBonus.strength}");
            if (statBonus.agility > 0) bonuses.Add($"AGI+{statBonus.agility}");
            if (statBonus.vitality > 0) bonuses.Add($"VIT+{statBonus.vitality}");
            if (statBonus.intelligence > 0) bonuses.Add($"INT+{statBonus.intelligence}");
            
            return bonuses.Count > 0 ? string.Join(", ", bonuses) : "No bonuses";
        }
        
        /// <summary>
        /// 스탯 보너스 상세 텍스트
        /// </summary>
        private string GetDetailedStatText(StatBlock statBonus)
        {
            var bonuses = new List<string>();
            
            if (statBonus.strength > 0) bonuses.Add($"STR +{statBonus.strength}");
            if (statBonus.agility > 0) bonuses.Add($"AGI +{statBonus.agility}");
            if (statBonus.vitality > 0) bonuses.Add($"VIT +{statBonus.vitality}");
            if (statBonus.intelligence > 0) bonuses.Add($"INT +{statBonus.intelligence}");
            if (statBonus.defense > 0) bonuses.Add($"DEF +{statBonus.defense}");
            if (statBonus.magicDefense > 0) bonuses.Add($"MDEF +{statBonus.magicDefense}");
            if (statBonus.luck > 0) bonuses.Add($"LUK +{statBonus.luck}");
            if (statBonus.stability > 0) bonuses.Add($"STAB +{statBonus.stability}");
            
            return bonuses.Count > 0 ? string.Join(", ", bonuses) : "No stat bonuses";
        }
        
        private void OnDestroy()
        {
            if (confirmButton != null) confirmButton.onClick.RemoveAllListeners();
            if (skipButton != null) skipButton.onClick.RemoveAllListeners();

            // 게임 일시정지 해제
            Time.timeScale = 1f;
        }
    }
}