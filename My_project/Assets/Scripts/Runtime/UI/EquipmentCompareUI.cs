using UnityEngine;
using UnityEngine.UI;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 장비 비교 UI - 현재 장착 아이템과 새 아이템을 나란히 비교
    /// </summary>
    public class EquipmentCompareUI : MonoBehaviour
    {
        public static EquipmentCompareUI Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject comparePanel;
        [SerializeField] private Text currentItemNameText;
        [SerializeField] private Text currentItemStatsText;
        [SerializeField] private Text newItemNameText;
        [SerializeField] private Text newItemStatsText;
        [SerializeField] private Text comparisonText;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button closeButton;

        private ItemInstance currentItem;
        private ItemInstance newItem;
        private EquipmentManager equipmentManager;
        private bool autoCreated = false;

        private const string COLOR_UP = "#44FF44";
        private const string COLOR_DOWN = "#FF4444";
        private const string COLOR_SAME = "#CCCCCC";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (comparePanel == null)
                CreateCompareUI();

            if (comparePanel != null)
                comparePanel.SetActive(false);
        }

        /// <summary>
        /// 장비 비교 표시
        /// </summary>
        public void ShowComparison(ItemInstance newEquipment, EquipmentManager eqManager)
        {
            if (newEquipment == null || newEquipment.ItemData == null) return;

            equipmentManager = eqManager;
            newItem = newEquipment;

            // 현재 장착 아이템 가져오기
            EquipmentSlot slot = newEquipment.ItemData.EquipmentSlot;
            currentItem = equipmentManager != null ? equipmentManager.GetEquippedItem(slot) : null;

            // UI 업데이트
            UpdateComparisonDisplay();

            if (comparePanel != null)
                comparePanel.SetActive(true);
        }

        /// <summary>
        /// 비교 화면 업데이트
        /// </summary>
        private void UpdateComparisonDisplay()
        {
            // 새 아이템 정보
            if (newItemNameText != null)
            {
                newItemNameText.text = newItem.ItemData.ItemName;
                newItemNameText.color = GetGradeColor(newItem.ItemData.Grade);
            }
            if (newItemStatsText != null)
                newItemStatsText.text = GetItemStatsString(newItem);

            // 현재 아이템 정보
            if (currentItem != null && currentItem.ItemData != null)
            {
                if (currentItemNameText != null)
                {
                    currentItemNameText.text = currentItem.ItemData.ItemName;
                    currentItemNameText.color = GetGradeColor(currentItem.ItemData.Grade);
                }
                if (currentItemStatsText != null)
                    currentItemStatsText.text = GetItemStatsString(currentItem);
            }
            else
            {
                if (currentItemNameText != null)
                {
                    currentItemNameText.text = "(없음)";
                    currentItemNameText.color = Color.gray;
                }
                if (currentItemStatsText != null)
                    currentItemStatsText.text = "";
            }

            // 비교 결과
            if (comparisonText != null)
                comparisonText.text = GetComparisonString();

            // 장착 버튼
            if (equipButton != null)
            {
                equipButton.onClick.RemoveAllListeners();
                equipButton.onClick.AddListener(OnEquipClicked);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
            }
        }

        /// <summary>
        /// 아이템 스탯 문자열 생성
        /// </summary>
        private string GetItemStatsString(ItemInstance item)
        {
            if (item == null || item.ItemData == null) return "";

            var data = item.ItemData;
            string stats = "";

            // 등급
            stats += $"등급: {GetGradeName(data.Grade)}\n";

            // 무기 데미지
            var dmg = data.WeaponDamageRange;
            if (dmg.minDamage > 0 || dmg.maxDamage > 0)
                stats += $"공격력: {dmg.minDamage:F0} ~ {dmg.maxDamage:F0}\n";

            // 슬롯
            stats += $"슬롯: {data.EquipmentSlot}\n";

            // 판매가
            if (data.SellPrice > 0)
                stats += $"가격: {data.SellPrice}G\n";

            return stats;
        }

        private string GetGradeName(ItemGrade grade)
        {
            switch (grade)
            {
                case ItemGrade.Common: return "일반";
                case ItemGrade.Uncommon: return "고급";
                case ItemGrade.Rare: return "희귀";
                case ItemGrade.Epic: return "영웅";
                case ItemGrade.Legendary: return "전설";
                default: return "???";
            }
        }

        /// <summary>
        /// 비교 결과 문자열 생성
        /// </summary>
        private string GetComparisonString()
        {
            if (newItem == null || newItem.ItemData == null) return "";

            string result = "<b>비교 결과</b>\n";

            var newDmg = newItem.ItemData.WeaponDamageRange;
            float newAvgDmg = (newDmg.minDamage + newDmg.maxDamage) / 2f;

            if (currentItem != null && currentItem.ItemData != null)
            {
                var curDmg = currentItem.ItemData.WeaponDamageRange;
                float curAvgDmg = (curDmg.minDamage + curDmg.maxDamage) / 2f;

                if (newAvgDmg > 0 || curAvgDmg > 0)
                {
                    float dmgDiff = newAvgDmg - curAvgDmg;
                    result += FormatDiff("평균 공격력", dmgDiff);
                }

                // 판매가 비교
                long priceDiff = newItem.ItemData.SellPrice - currentItem.ItemData.SellPrice;
                result += FormatDiff("가치", priceDiff);
            }
            else
            {
                if (newAvgDmg > 0)
                    result += $"<color={COLOR_UP}>평균 공격력: +{newAvgDmg:F0}</color>\n";
                result += $"<color={COLOR_UP}>새 장비 장착</color>\n";
            }

            return result;
        }

        /// <summary>
        /// 차이값 포맷
        /// </summary>
        private string FormatDiff(string label, float diff)
        {
            if (diff > 0.1f)
                return $"<color={COLOR_UP}>{label}: +{diff:F0}</color>\n";
            if (diff < -0.1f)
                return $"<color={COLOR_DOWN}>{label}: {diff:F0}</color>\n";
            return $"<color={COLOR_SAME}>{label}: 동일</color>\n";
        }

        private string FormatDiff(string label, long diff)
        {
            if (diff > 0)
                return $"<color={COLOR_UP}>{label}: +{diff}</color>\n";
            if (diff < 0)
                return $"<color={COLOR_DOWN}>{label}: {diff}</color>\n";
            return $"<color={COLOR_SAME}>{label}: 동일</color>\n";
        }

        /// <summary>
        /// 장착 버튼 클릭
        /// </summary>
        private void OnEquipClicked()
        {
            if (equipmentManager != null && newItem != null)
            {
                equipmentManager.TryEquipItem(newItem);
                Hide();
            }
        }

        /// <summary>
        /// 숨기기
        /// </summary>
        public void Hide()
        {
            if (comparePanel != null)
                comparePanel.SetActive(false);
        }

        /// <summary>
        /// 등급 색상
        /// </summary>
        private Color GetGradeColor(ItemGrade grade)
        {
            switch (grade)
            {
                case ItemGrade.Common: return Color.gray;
                case ItemGrade.Uncommon: return Color.green;
                case ItemGrade.Rare: return new Color(0.3f, 0.5f, 1f);
                case ItemGrade.Epic: return new Color(0.7f, 0.3f, 1f);
                case ItemGrade.Legendary: return new Color(1f, 0.6f, 0f);
                default: return Color.white;
            }
        }

        /// <summary>
        /// 자동 UI 생성
        /// </summary>
        private void CreateCompareUI()
        {
            autoCreated = true;

            comparePanel = new GameObject("ComparePanel");
            comparePanel.transform.SetParent(transform, false);

            var panelRT = comparePanel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.2f, 0.15f);
            panelRT.anchorMax = new Vector2(0.8f, 0.85f);
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            var bg = comparePanel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // 제목
            CreateLabel(comparePanel.transform, "Title", "장비 비교",
                new Vector2(0f, 0.9f), new Vector2(1f, 1f), 20, FontStyle.Bold, Color.white);

            // 현재 아이템 패널 (왼쪽)
            CreateLabel(comparePanel.transform, "CurrentTitle", "현재 장비",
                new Vector2(0.02f, 0.8f), new Vector2(0.48f, 0.88f), 14, FontStyle.Bold, Color.cyan);

            currentItemNameText = CreateLabel(comparePanel.transform, "CurrentName", "",
                new Vector2(0.02f, 0.72f), new Vector2(0.48f, 0.8f), 16, FontStyle.Bold, Color.white);

            currentItemStatsText = CreateLabel(comparePanel.transform, "CurrentStats", "",
                new Vector2(0.02f, 0.3f), new Vector2(0.48f, 0.72f), 12, FontStyle.Normal, Color.white);

            // 새 아이템 패널 (오른쪽)
            CreateLabel(comparePanel.transform, "NewTitle", "새 장비",
                new Vector2(0.52f, 0.8f), new Vector2(0.98f, 0.88f), 14, FontStyle.Bold, Color.yellow);

            newItemNameText = CreateLabel(comparePanel.transform, "NewName", "",
                new Vector2(0.52f, 0.72f), new Vector2(0.98f, 0.8f), 16, FontStyle.Bold, Color.white);

            newItemStatsText = CreateLabel(comparePanel.transform, "NewStats", "",
                new Vector2(0.52f, 0.3f), new Vector2(0.98f, 0.72f), 12, FontStyle.Normal, Color.white);

            // 비교 결과
            comparisonText = CreateLabel(comparePanel.transform, "Comparison", "",
                new Vector2(0.02f, 0.1f), new Vector2(0.6f, 0.28f), 13, FontStyle.Normal, Color.white);

            // 장착 버튼
            equipButton = CreateButton(comparePanel.transform, "EquipBtn", "장착",
                new Vector2(0.62f, 0.12f), new Vector2(0.78f, 0.22f), new Color(0.2f, 0.6f, 0.2f));

            // 닫기 버튼
            closeButton = CreateButton(comparePanel.transform, "CloseBtn", "닫기",
                new Vector2(0.8f, 0.12f), new Vector2(0.96f, 0.22f), new Color(0.6f, 0.2f, 0.2f));

            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
        }

        private Text CreateLabel(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, int fontSize, FontStyle style, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var t = obj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.color = color;
            t.text = text;
            t.alignment = TextAnchor.UpperLeft;
            t.supportRichText = true;
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return t;
        }

        private Button CreateButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var img = obj.AddComponent<Image>();
            img.color = bgColor;
            var btn = obj.AddComponent<Button>();
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            var t = textObj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 14;
            t.color = Color.white;
            t.text = label;
            t.alignment = TextAnchor.MiddleCenter;
            var trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            return btn;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
