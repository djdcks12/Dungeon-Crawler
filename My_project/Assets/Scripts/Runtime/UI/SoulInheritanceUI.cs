using UnityEngine;
using UnityEngine.UI;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 영혼 상속 UI - 새 캐릭터 생성 시 보존된 영혼 사용 여부 선택
    /// </summary>
    public class SoulInheritanceUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject inheritancePanel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text soulInfoText;
        [SerializeField] private Text benefitsText;
        [SerializeField] private Text warningText;
        [SerializeField] private Button inheritButton;
        [SerializeField] private Button declineButton;
        
        [Header("Soul Display")]
        [SerializeField] private Image soulIcon;
        [SerializeField] private Text soulNameText;
        [SerializeField] private Text soulRarityText;
        [SerializeField] private Text soulStatsText;
        [SerializeField] private Text soulSpecialText;
        
        private SoulData preservedSoul;
        private System.Action<bool> onInheritanceDecision;
        
        private void Awake()
        {
            SetupUI();
        }
        
        /// <summary>
        /// UI 초기 설정
        /// </summary>
        private void SetupUI()
        {
            if (inheritancePanel != null)
            {
                inheritancePanel.SetActive(false);
            }
            
            if (inheritButton != null)
            {
                inheritButton.onClick.AddListener(() => MakeDecision(true));
            }
            
            if (declineButton != null)
            {
                declineButton.onClick.AddListener(() => MakeDecision(false));
            }
            
            // 기본 텍스트 설정
            if (titleText != null)
            {
                titleText.text = "🔮 Soul Inheritance";
            }
            
            if (warningText != null)
            {
                warningText.text = "⚠️ If you decline, this soul will be PERMANENTLY DELETED!";
                warningText.color = Color.red;
            }
        }
        
        /// <summary>
        /// 영혼 상속 UI 표시
        /// </summary>
        public void ShowInheritanceOption(SoulData soul, System.Action<bool> onDecision)
        {
            if (soul.soulId == 0)
            {
                Debug.LogError("Invalid soul data for inheritance UI");
                onDecision?.Invoke(false);
                return;
            }
            
            preservedSoul = soul;
            onInheritanceDecision = onDecision;
            
            DisplaySoulInfo();
            
            if (inheritancePanel != null)
            {
                inheritancePanel.SetActive(true);
            }
            
            Debug.Log($"🔮 Showing inheritance option for soul: {soul.soulName}");
        }
        
        /// <summary>
        /// 영혼 정보 표시
        /// </summary>
        private void DisplaySoulInfo()
        {
            // 영혼 이름
            if (soulNameText != null)
            {
                soulNameText.text = preservedSoul.soulName;
            }
            
            // 희귀도
            if (soulRarityText != null)
            {
                soulRarityText.text = GetRarityText(preservedSoul.rarity);
                soulRarityText.color = GetRarityColor(preservedSoul.rarity);
            }
            
            // 스탯 보너스
            if (soulStatsText != null)
            {
                soulStatsText.text = GetDetailedStatText(preservedSoul.statBonus);
            }
            
            // 특수 효과
            if (soulSpecialText != null)
            {
                soulSpecialText.text = string.IsNullOrEmpty(preservedSoul.specialEffect) 
                    ? "No special effects" 
                    : preservedSoul.specialEffect;
            }
            
            // 종합 정보
            if (soulInfoText != null)
            {
                soulInfoText.text = $"You have a preserved soul from your previous character.\n" +
                                   $"Floor found: {preservedSoul.floorFound}\n" +
                                   $"Acquired: {GetAcquiredTimeText(preservedSoul.acquiredTime)}";
            }
            
            // 이익 설명
            if (benefitsText != null)
            {
                benefitsText.text = "✅ If you inherit this soul:\n" +
                                   "• All stat bonuses will be applied immediately\n" +
                                   "• Special effects will be active from level 1\n" +
                                   "• This soul will be permanently bound to your character\n\n" +
                                   "❌ If you decline:\n" +
                                   "• You start completely fresh with no bonuses\n" +
                                   "• This soul will be permanently deleted\n" +
                                   "• No way to recover it later";
            }
            
            // 영혼 아이콘 색상 설정
            if (soulIcon != null)
            {
                soulIcon.color = GetRarityColor(preservedSoul.rarity);
            }
        }
        
        /// <summary>
        /// 상속 결정 처리
        /// </summary>
        private void MakeDecision(bool inherit)
        {
            string decision = inherit ? "INHERIT" : "DECLINE";
            Debug.Log($"🎯 Soul inheritance decision: {decision} ({preservedSoul.soulName})");
            
            HideInheritanceUI();
            onInheritanceDecision?.Invoke(inherit);
        }
        
        /// <summary>
        /// 상속 UI 숨기기
        /// </summary>
        private void HideInheritanceUI()
        {
            if (inheritancePanel != null)
            {
                inheritancePanel.SetActive(false);
            }
            
            preservedSoul = new SoulData();
            onInheritanceDecision = null;
        }
        
        /// <summary>
        /// 희귀도 텍스트 반환
        /// </summary>
        private string GetRarityText(SoulRarity rarity)
        {
            return rarity switch
            {
                SoulRarity.Common => "Common Soul",
                SoulRarity.Rare => "Rare Soul",
                SoulRarity.Epic => "Epic Soul", 
                SoulRarity.Legendary => "Legendary Soul",
                _ => "Unknown Soul"
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
                SoulRarity.Rare => new Color(0.3f, 0.5f, 1f), // 밝은 파란색
                SoulRarity.Epic => new Color(0.6f, 0.3f, 1f), // 보라색
                SoulRarity.Legendary => new Color(1f, 0.8f, 0f), // 금색
                _ => Color.gray
            };
        }
        
        /// <summary>
        /// 스탯 보너스 상세 텍스트
        /// </summary>
        private string GetDetailedStatText(StatBlock statBonus)
        {
            var bonuses = new System.Collections.Generic.List<string>();
            
            if (statBonus.strength > 0) bonuses.Add($"Strength +{statBonus.strength}");
            if (statBonus.agility > 0) bonuses.Add($"Agility +{statBonus.agility}");
            if (statBonus.vitality > 0) bonuses.Add($"Vitality +{statBonus.vitality}");
            if (statBonus.intelligence > 0) bonuses.Add($"Intelligence +{statBonus.intelligence}");
            if (statBonus.defense > 0) bonuses.Add($"Defense +{statBonus.defense}");
            if (statBonus.magicDefense > 0) bonuses.Add($"Magic Defense +{statBonus.magicDefense}");
            if (statBonus.luck > 0) bonuses.Add($"Luck +{statBonus.luck}");
            if (statBonus.stability > 0) bonuses.Add($"Stability +{statBonus.stability}");
            
            return bonuses.Count > 0 ? string.Join("\n", bonuses) : "No stat bonuses";
        }
        
        private void OnDestroy()
        {
            if (inheritButton != null) inheritButton.onClick.RemoveAllListeners();
            if (declineButton != null) declineButton.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// 획득 시간 텍스트 반환
        /// </summary>
        private string GetAcquiredTimeText(long timestamp)
        {
            try
            {
                var dateTime = System.DateTime.FromBinary(timestamp);
                return dateTime.ToString("yyyy-MM-dd HH:mm");
            }
            catch
            {
                return "Unknown time";
            }
        }
    }
}