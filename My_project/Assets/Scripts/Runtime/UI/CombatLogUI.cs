using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 전투 로그 UI - 데미지, 힐, 아이템 획득 등 전투 이벤트를 텍스트로 표시
    /// </summary>
    public class CombatLogUI : MonoBehaviour
    {
        public static CombatLogUI Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Text logText;
        [SerializeField] private GameObject logPanel;

        [Header("Settings")]
        [SerializeField] private int maxLines = 100;
        [SerializeField] private bool autoScroll = true;

        private List<string> logLines = new List<string>();
        private bool isDirty = false;

        // 색상 코드
        private const string COLOR_DAMAGE = "#FF4444";
        private const string COLOR_HEAL = "#44FF44";
        private const string COLOR_CRIT = "#FFFF44";
        private const string COLOR_ITEM = "#FFD700";
        private const string COLOR_EXP = "#88CCFF";
        private const string COLOR_SYSTEM = "#AAAAAA";
        private const string COLOR_SKILL = "#CC88FF";
        private const string COLOR_GOLD = "#FFB800";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void LateUpdate()
        {
            if (isDirty)
            {
                RefreshDisplay();
                isDirty = false;
            }
        }

        /// <summary>
        /// 데미지 로그
        /// </summary>
        public void LogDamage(string attackerName, string targetName, float damage, bool isCritical, DamageType damageType)
        {
            string dmgColor = isCritical ? COLOR_CRIT : COLOR_DAMAGE;
            string critText = isCritical ? " <b>치명타!</b>" : "";
            string typeText = GetDamageTypeName(damageType);
            AddLine($"<color={dmgColor}>{attackerName}</color>이(가) {targetName}에게 <color={dmgColor}>{damage:F0}{typeText}</color> 데미지{critText}");
        }

        /// <summary>
        /// 힐 로그
        /// </summary>
        public void LogHeal(string targetName, float amount, string source)
        {
            AddLine($"<color={COLOR_HEAL}>{targetName}</color>이(가) {source}으로 <color={COLOR_HEAL}>+{amount:F0}</color> 회복");
        }

        /// <summary>
        /// 스킬 사용 로그
        /// </summary>
        public void LogSkillUse(string userName, string skillName)
        {
            AddLine($"<color={COLOR_SKILL}>{userName}</color>이(가) <color={COLOR_SKILL}>{skillName}</color> 사용");
        }

        /// <summary>
        /// 아이템 획득 로그
        /// </summary>
        public void LogItemPickup(string playerName, string itemName, int quantity)
        {
            string qtyText = quantity > 1 ? $" x{quantity}" : "";
            AddLine($"<color={COLOR_ITEM}>{playerName}</color>이(가) <color={COLOR_ITEM}>{itemName}{qtyText}</color> 획득");
        }

        /// <summary>
        /// 골드 획득 로그
        /// </summary>
        public void LogGoldPickup(string playerName, long amount)
        {
            AddLine($"<color={COLOR_GOLD}>{playerName}</color>이(가) <color={COLOR_GOLD}>{amount:N0}G</color> 획득");
        }

        /// <summary>
        /// 경험치 획득 로그
        /// </summary>
        public void LogExpGain(long amount)
        {
            AddLine($"<color={COLOR_EXP}>경험치 +{amount:N0}</color>");
        }

        /// <summary>
        /// 몬스터 처치 로그
        /// </summary>
        public void LogMonsterKill(string monsterName)
        {
            AddLine($"<color={COLOR_SYSTEM}>{monsterName}</color> 처치!");
        }

        /// <summary>
        /// 시스템 메시지
        /// </summary>
        public void LogSystem(string message)
        {
            AddLine($"<color={COLOR_SYSTEM}>[시스템] {message}</color>");
        }

        /// <summary>
        /// 상태이상 적용 로그
        /// </summary>
        public void LogStatusEffect(string targetName, string effectName, bool applied)
        {
            string action = applied ? "걸림" : "해제";
            AddLine($"<color={COLOR_SKILL}>{targetName}</color>에게 <color={COLOR_SKILL}>{effectName}</color> {action}");
        }

        private void AddLine(string line)
        {
            logLines.Add(line);

            // 최대 줄 수 초과 시 오래된 것 제거
            while (logLines.Count > maxLines)
            {
                logLines.RemoveAt(0);
            }

            isDirty = true;
        }

        private void RefreshDisplay()
        {
            if (logText == null) return;

            logText.text = string.Join("\n", logLines);

            if (autoScroll && scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private string GetDamageTypeName(DamageType type)
        {
            switch (type)
            {
                case DamageType.Fire: return "(화염)";
                case DamageType.Ice: return "(냉기)";
                case DamageType.Poison: return "(독)";
                case DamageType.Holy: return "(신성)";
                case DamageType.Dark: return "(암흑)";
                default: return "";
            }
        }

        /// <summary>
        /// 로그 패널 토글
        /// </summary>
        public void ToggleLog()
        {
            if (logPanel != null)
                logPanel.SetActive(!logPanel.activeSelf);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
