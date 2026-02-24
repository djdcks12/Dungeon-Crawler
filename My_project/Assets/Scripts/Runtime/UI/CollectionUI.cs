using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 도감 UI - B키 토글
    /// 4탭 (몬스터/아이템/스킬/통계), 완성률 프로그레스 바
    /// </summary>
    public class CollectionUI : MonoBehaviour
    {
        private GameObject mainPanel;
        private Text titleText;
        private Text completionText;
        private Image completionBar;
        private Transform contentArea;
        private Button closeButton;

        // 탭 버튼들
        private Button[] tabButtons = new Button[4];
        private int currentTab;
        private string[] tabNames = { "몬스터", "아이템", "스킬", "통계" };

        private List<GameObject> entries = new List<GameObject>();
        private bool isInitialized;

        private void Start()
        {
            CreateUI();
            isInitialized = true;
            mainPanel.SetActive(false);

            if (CollectionSystem.Instance != null)
                CollectionSystem.Instance.OnCollectionUpdated += RefreshCurrentTab;
        }

        private void Update()
        {
            if (!isInitialized) return;

            if (Input.GetKeyDown(KeyCode.B))
            {
                if (mainPanel.activeSelf) mainPanel.SetActive(false);
                else OpenCollection();
            }

            if (mainPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                mainPanel.SetActive(false);
        }

        public void OpenCollection()
        {
            mainPanel.SetActive(true);
            SwitchTab(0);
        }

        private void SwitchTab(int tabIndex)
        {
            currentTab = tabIndex;

            for (int i = 0; i < tabButtons.Length; i++)
            {
                var img = tabButtons[i].GetComponent<Image>();
                img.color = i == currentTab
                    ? new Color(0.2f, 0.3f, 0.5f, 1f)
                    : new Color(0.15f, 0.15f, 0.2f, 0.8f);
            }

            RefreshCurrentTab();
        }

        private void RefreshCurrentTab()
        {
            ClearEntries();
            if (CollectionSystem.Instance == null) return;
            var sys = CollectionSystem.Instance;

            // 완성률 업데이트
            float totalRate = sys.GetTotalCompletionRate();
            completionText.text = $"전체 완성률: {totalRate:P1}";
            completionBar.fillAmount = totalRate;

            switch (currentTab)
            {
                case 0: ShowMonsterTab(sys); break;
                case 1: ShowItemTab(sys); break;
                case 2: ShowSkillTab(sys); break;
                case 3: ShowStatsTab(sys); break;
            }
        }

        private void ShowMonsterTab(CollectionSystem sys)
        {
            titleText.text = $"몬스터 도감 ({sys.CollectedMonsterCount}/{sys.TotalMonsterCount})";

            // 몬스터 종족 데이터 로드
            var races = Resources.LoadAll<MonsterRaceData>("");
            foreach (var race in races)
            {
                bool collected = sys.HasCollectedMonster(race.name);
                int kills = sys.GetMonsterKillCount(race.name);
                string displayName = !string.IsNullOrEmpty(race.raceName) ? race.raceName : race.name;
                string nameDisplay = collected
                    ? $"<color=#FFFFFF>{displayName}</color>"
                    : "<color=#444444>???</color>";
                string killDisplay = collected ? $"처치: {kills}" : "";

                CreateEntry(nameDisplay, killDisplay, collected);
            }

            // 변종 데이터
            var variants = Resources.LoadAll<MonsterVariantData>("");
            foreach (var variant in variants)
            {
                bool collected = sys.HasCollectedMonster(variant.name);
                int kills = sys.GetMonsterKillCount(variant.name);
                string displayName = !string.IsNullOrEmpty(variant.variantName) ? variant.variantName : variant.name;
                string nameDisplay = collected
                    ? $"<color=#FFAA44>{displayName}</color>"
                    : "<color=#444444>???</color>";
                string killDisplay = collected ? $"처치: {kills}" : "";

                CreateEntry(nameDisplay, killDisplay, collected);
            }
        }

        private void ShowItemTab(CollectionSystem sys)
        {
            titleText.text = $"아이템 도감 ({sys.CollectedItemCount}/{sys.TotalItemCount})";

            var items = Resources.LoadAll<ItemData>("");

            // 등급별 정렬
            System.Array.Sort(items, (a, b) =>
            {
                int gradeComp = a.Grade.CompareTo(b.Grade);
                if (gradeComp != 0) return gradeComp;
                return string.Compare(a.ItemName, b.ItemName, System.StringComparison.Ordinal);
            });

            foreach (var item in items)
            {
                bool collected = sys.HasCollectedItem(item.ItemId);
                string gradeColor = ColorUtility.ToHtmlStringRGB(item.GradeColor);
                string nameDisplay = collected
                    ? $"<color=#{gradeColor}>{item.ItemName}</color>"
                    : "<color=#444444>???</color>";
                string typeDisplay = collected ? GetItemTypeText(item) : "";

                CreateEntry(nameDisplay, typeDisplay, collected);
            }
        }

        private void ShowSkillTab(CollectionSystem sys)
        {
            titleText.text = $"스킬 도감 ({sys.CollectedSkillCount}/{sys.TotalSkillCount})";

            var skills = Resources.LoadAll<SkillData>("");
            foreach (var skill in skills)
            {
                bool collected = sys.HasCollectedSkill(skill.skillId);
                string nameDisplay = collected
                    ? $"<color=#88CCFF>{skill.skillName}</color>"
                    : "<color=#444444>???</color>";
                string descDisplay = collected ? TruncateText(skill.description, 30) : "";

                CreateEntry(nameDisplay, descDisplay, collected);
            }
        }

        private void ShowStatsTab(CollectionSystem sys)
        {
            titleText.text = "도감 통계";

            float monsterRate = sys.GetCompletionRate(CollectionCategory.Monster);
            float itemRate = sys.GetCompletionRate(CollectionCategory.Item);
            float skillRate = sys.GetCompletionRate(CollectionCategory.Skill);
            float totalRate = sys.GetTotalCompletionRate();

            CreateStatEntry("몬스터 도감", sys.CollectedMonsterCount, sys.TotalMonsterCount, monsterRate, "#FF8844");
            CreateStatEntry("아이템 도감", sys.CollectedItemCount, sys.TotalItemCount, itemRate, "#44FF88");
            CreateStatEntry("스킬 도감", sys.CollectedSkillCount, sys.TotalSkillCount, skillRate, "#4488FF");
            CreateStatEntry("전체", sys.CollectedMonsterCount + sys.CollectedItemCount + sys.CollectedSkillCount,
                sys.TotalMonsterCount + sys.TotalItemCount + sys.TotalSkillCount, totalRate, "#FFD700");

            // 마일스톤 보상 표시
            CreateSeparator();
            CreateEntry("<color=#FFD700>마일스톤 보상</color>", "", true);
            CreateMilestoneEntry("도감 25%", totalRate >= 0.25f, "골드 1,000 + 경험치 500");
            CreateMilestoneEntry("도감 50%", totalRate >= 0.50f, "골드 3,000 + 경험치 1,500");
            CreateMilestoneEntry("도감 75%", totalRate >= 0.75f, "골드 5,000 + 경험치 3,000");
            CreateMilestoneEntry("도감 100%", totalRate >= 1.00f, "골드 10,000 + 경험치 5,000");
        }

        private void CreateEntry(string name, string info, bool collected)
        {
            var entry = new GameObject("Entry");
            entry.transform.SetParent(contentArea, false);
            var rt = entry.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(660, 32);

            Color bg = collected ? new Color(0.12f, 0.14f, 0.18f, 0.8f) : new Color(0.08f, 0.08f, 0.1f, 0.5f);
            entry.AddComponent<Image>().color = bg;

            CreateText(entry.transform, "Name", name, 13, TextAnchor.MiddleLeft,
                new Vector2(-150, 0), new Vector2(350, 28));

            if (!string.IsNullOrEmpty(info))
            {
                CreateText(entry.transform, "Info", $"<color=#AAAAAA>{info}</color>", 12,
                    TextAnchor.MiddleRight, new Vector2(150, 0), new Vector2(250, 28));
            }

            entries.Add(entry);
        }

        private void CreateStatEntry(string label, int current, int total, float rate, string color)
        {
            var entry = new GameObject("StatEntry");
            entry.transform.SetParent(contentArea, false);
            var rt = entry.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(660, 45);
            entry.AddComponent<Image>().color = new Color(0.1f, 0.12f, 0.16f, 0.9f);

            CreateText(entry.transform, "Label", $"<color={color}>{label}</color>", 15,
                TextAnchor.MiddleLeft, new Vector2(-230, 0), new Vector2(200, 35));
            CreateText(entry.transform, "Count", $"{current} / {total}", 14,
                TextAnchor.MiddleCenter, new Vector2(50, 0), new Vector2(100, 35));
            CreateText(entry.transform, "Rate", $"<color={color}>{rate:P1}</color>", 14,
                TextAnchor.MiddleRight, new Vector2(230, 0), new Vector2(100, 35));

            entries.Add(entry);
        }

        private void CreateMilestoneEntry(string milestone, bool achieved, string reward)
        {
            string checkmark = achieved ? "<color=#44FF44>✓</color>" : "<color=#666666>○</color>";
            string textColor = achieved ? "#FFFFFF" : "#666666";
            CreateEntry($"{checkmark} <color={textColor}>{milestone}</color>",
                $"<color={textColor}>{reward}</color>", true);
        }

        private void CreateSeparator()
        {
            var sep = new GameObject("Separator");
            sep.transform.SetParent(contentArea, false);
            var rt = sep.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(660, 10);
            entries.Add(sep);
        }

        private void ClearEntries()
        {
            foreach (var e in entries)
                if (e != null) Destroy(e);
            entries.Clear();
        }

        private string GetItemTypeText(ItemData item)
        {
            string gradeText = item.Grade switch
            {
                ItemGrade.Common => "일반",
                ItemGrade.Uncommon => "고급",
                ItemGrade.Rare => "희귀",
                ItemGrade.Epic => "영웅",
                ItemGrade.Legendary => "전설",
                _ => ""
            };
            string typeText = item.ItemType switch
            {
                ItemType.Equipment => item.IsWeapon ? "무기" : "방어구",
                ItemType.Consumable => "소모품",
                ItemType.Material => "재료",
                _ => "기타"
            };
            return $"{gradeText} {typeText}";
        }

        private string TruncateText(string text, int maxLen)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Length <= maxLen ? text : text.Substring(0, maxLen) + "...";
        }

        #region UI 생성

        private void CreateUI()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 140;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            mainPanel = new GameObject("CollectionPanel");
            mainPanel.transform.SetParent(transform, false);
            var panelRT = mainPanel.AddComponent<RectTransform>();
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(720, 520);
            mainPanel.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.1f, 0.96f);

            // 타이틀
            var titleObj = CreateText(mainPanel.transform, "Title", "도감", 20, TextAnchor.MiddleCenter,
                new Vector2(0, 230), new Vector2(300, 35));
            titleText = titleObj.GetComponent<Text>();

            // 닫기
            closeButton = CreateButton(mainPanel.transform, "CloseBtn", "X",
                new Vector2(330, 230), new Vector2(40, 40));
            closeButton.onClick.AddListener(() => mainPanel.SetActive(false));

            // 완성률 바
            var compObj = CreateText(mainPanel.transform, "CompText", "전체 완성률: 0%", 13,
                TextAnchor.MiddleLeft, new Vector2(-200, 195), new Vector2(200, 25));
            completionText = compObj.GetComponent<Text>();

            var barBg = new GameObject("CompBarBg");
            barBg.transform.SetParent(mainPanel.transform, false);
            var barBgRT = barBg.AddComponent<RectTransform>();
            barBgRT.anchoredPosition = new Vector2(100, 195);
            barBgRT.sizeDelta = new Vector2(350, 16);
            barBg.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 1f);

            var barFill = new GameObject("CompBarFill");
            barFill.transform.SetParent(barBg.transform, false);
            var barFillRT = barFill.AddComponent<RectTransform>();
            barFillRT.anchorMin = Vector2.zero;
            barFillRT.anchorMax = new Vector2(0, 1);
            barFillRT.pivot = new Vector2(0, 0.5f);
            barFillRT.offsetMin = Vector2.zero;
            barFillRT.offsetMax = Vector2.zero;
            completionBar = barFill.AddComponent<Image>();
            completionBar.color = new Color(0.2f, 0.7f, 0.3f, 1f);
            completionBar.type = Image.Type.Filled;
            completionBar.fillMethod = Image.FillMethod.Horizontal;

            // 탭 버튼들
            for (int i = 0; i < 4; i++)
            {
                float x = -240 + i * 160;
                tabButtons[i] = CreateButton(mainPanel.transform, $"Tab{i}", tabNames[i],
                    new Vector2(x, 165), new Vector2(140, 30));
                int tabIndex = i;
                tabButtons[i].onClick.AddListener(() => SwitchTab(tabIndex));
            }

            // 스크롤뷰
            var scrollObj = CreateScrollView(mainPanel.transform, "ContentScroll",
                new Vector2(0, -30), new Vector2(680, 360));
            contentArea = scrollObj.transform.Find("Viewport/Content");
        }

        private GameObject CreateText(Transform parent, string name, string text, int fontSize,
            TextAnchor alignment, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = Color.white;
            txt.supportRichText = true;
            return obj;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var img = obj.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);
            var btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(obj.transform, false);
            var txtRT = txtObj.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.sizeDelta = Vector2.zero;
            var txt = txtObj.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 14;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            return btn;
        }

        private GameObject CreateScrollView(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var scrollObj = new GameObject(name);
            scrollObj.transform.SetParent(parent, false);
            var scrollRT = scrollObj.AddComponent<RectTransform>();
            scrollRT.anchoredPosition = position;
            scrollRT.sizeDelta = size;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.sizeDelta = Vector2.zero;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>().color = Color.clear;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRT = content.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.sizeDelta = new Vector2(0, 0);
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(5, 5, 5, 5);
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.viewport = vpRT;
            scroll.content = contentRT;
            scroll.horizontal = false;
            return scrollObj;
        }

        #endregion

        private void OnDestroy()
        {
            if (CollectionSystem.Instance != null)
                CollectionSystem.Instance.OnCollectionUpdated -= RefreshCurrentTab;
        }
    }
}
