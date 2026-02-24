using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 하우징 UI - F5키 토글
    /// 가구 목록/배치된 가구/버프 현황/집 확장
    /// </summary>
    public class HousingUI : MonoBehaviour
    {
        private GameObject mainPanel;
        private Text titleText;
        private Transform contentArea;
        private Button closeButton;

        // 탭
        private Button shopTab;
        private Button placedTab;
        private Button buffTab;
        private int currentTab; // 0=상점, 1=배치, 2=버프

        private List<GameObject> entries = new List<GameObject>();
        private bool isInitialized;

        private void Start()
        {
            CreateUI();
            isInitialized = true;
            mainPanel.SetActive(false);

            if (HousingSystem.Instance != null)
                HousingSystem.Instance.OnHousingUpdated += RefreshCurrentTab;
        }

        private void Update()
        {
            if (!isInitialized) return;

            if (Input.GetKeyDown(KeyCode.F5))
            {
                if (mainPanel.activeSelf) mainPanel.SetActive(false);
                else OpenPanel();
            }

            if (mainPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                mainPanel.SetActive(false);
        }

        public void OpenPanel()
        {
            mainPanel.SetActive(true);
            if (HousingSystem.Instance != null)
                HousingSystem.Instance.RequestSyncServerRpc();
            SwitchTab(0);
        }

        private void SwitchTab(int tab)
        {
            currentTab = tab;
            Color activeC = new Color(0.2f, 0.3f, 0.5f, 1f);
            Color inactiveC = new Color(0.15f, 0.15f, 0.2f, 0.8f);
            shopTab.GetComponent<Image>().color = tab == 0 ? activeC : inactiveC;
            placedTab.GetComponent<Image>().color = tab == 1 ? activeC : inactiveC;
            buffTab.GetComponent<Image>().color = tab == 2 ? activeC : inactiveC;
            RefreshCurrentTab();
        }

        private void RefreshCurrentTab()
        {
            ClearEntries();
            switch (currentTab)
            {
                case 0: ShowShop(); break;
                case 1: ShowPlaced(); break;
                case 2: ShowBuffs(); break;
            }
        }

        private void ShowShop()
        {
            if (HousingSystem.Instance == null) return;
            var sys = HousingSystem.Instance;
            var housing = sys.LocalHousing;

            titleText.text = $"가구 상점 ({housing?.placedFurniture.Count ?? 0}/{housing?.maxSlots ?? 10})";

            // 확장 버튼
            long upgradeCost = sys.GetUpgradeCost();
            if (housing != null && housing.maxSlots < 50)
            {
                CreateActionEntry($"집 확장 (+5칸, 현재 {housing.maxSlots}칸)", $"{upgradeCost:N0}G",
                    new Color(0.1f, 0.2f, 0.1f, 0.9f), () => sys.UpgradeHouseServerRpc());
            }

            // 카테고리별 표시
            ShowFurnitureCategory("장식 가구", FurnitureCategory.Decoration);
            ShowFurnitureCategory("설비 (버프)", FurnitureCategory.Facility);
            ShowFurnitureCategory("프리미엄", FurnitureCategory.Premium);
        }

        private void ShowFurnitureCategory(string header, FurnitureCategory category)
        {
            var sys = HousingSystem.Instance;
            var items = sys.GetFurnitureByCategory(category);

            CreateHeaderEntry(header);

            foreach (var info in items)
            {
                string desc = info.buffType != HousingBuffType.None ? info.desc : $"위엄 +{info.comfort}";
                CreateFurnitureEntry(info.name, desc, $"{info.cost:N0}G",
                    () => sys.PlaceFurnitureServerRpc(info.id));
            }
        }

        private void ShowPlaced()
        {
            if (HousingSystem.Instance == null) return;
            var sys = HousingSystem.Instance;
            var housing = sys.LocalHousing;

            int comfort = sys.GetTotalComfort();
            titleText.text = $"배치된 가구 (위엄: {comfort})";

            if (housing == null || housing.placedFurniture.Count == 0)
            {
                CreateInfoEntry("배치된 가구가 없습니다.");
                return;
            }

            for (int i = 0; i < housing.placedFurniture.Count; i++)
            {
                var info = sys.GetFurnitureInfo(housing.placedFurniture[i]);
                if (info == null) continue;

                int idx = i;
                string desc = info.buffType != HousingBuffType.None ? info.desc : $"위엄 +{info.comfort}";
                string refund = $"제거 (환불: {info.cost / 2:N0}G)";

                CreatePlacedEntry(info.name, desc, refund, () => sys.RemoveFurnitureServerRpc(idx));
            }
        }

        private void ShowBuffs()
        {
            if (HousingSystem.Instance == null) return;
            var sys = HousingSystem.Instance;

            titleText.text = "하우징 버프 현황";

            var buffs = sys.GetBuffSummary();

            CreateBuffLine("경험치 보너스", buffs.expBonusPercent, "%");
            CreateBuffLine("골드 보너스", buffs.goldBonusPercent, "%");
            CreateBuffLine("쿨다운 감소", buffs.cooldownReductionPercent, "%");
            CreateBuffLine("강화 보너스", buffs.enhanceBonusPercent, "%");
            CreateBuffLine("HP 자연회복", buffs.hpRegenPercent, "%/초");
            CreateBuffLine("MP 자연회복", buffs.mpRegenPercent, "%/초");
            CreateBuffLine("드롭률 보너스", buffs.dropRateBonusPercent, "%");

            CreateInfoEntry("");
            int comfort = sys.GetTotalComfort();
            CreateInfoEntry($"총 위엄 점수: {comfort}");

            // 위엄 등급
            string rank = comfort >= 100 ? "<color=#FFD700>궁전</color>" :
                          comfort >= 50 ? "<color=#FF55FF>저택</color>" :
                          comfort >= 25 ? "<color=#5555FF>주택</color>" :
                          comfort >= 10 ? "<color=#55FF55>오두막</color>" : "텐트";
            CreateInfoEntry($"주거 등급: {rank}");
        }

        private void CreateHeaderEntry(string header)
        {
            var entry = new GameObject("Header");
            entry.transform.SetParent(contentArea, false);
            var rt = entry.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(620, 30);
            entry.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.25f, 0.8f);
            CreateText(entry.transform, "Text", $"<color=#FFD700>[ {header} ]</color>", 14,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(600, 28));
            entries.Add(entry);
        }

        private void CreateFurnitureEntry(string name, string desc, string cost, System.Action onBuy)
        {
            var entry = new GameObject("Furniture");
            entry.transform.SetParent(contentArea, false);
            var rt = entry.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(620, 55);
            entry.AddComponent<Image>().color = new Color(0.1f, 0.12f, 0.16f, 0.9f);

            CreateText(entry.transform, "Name", name, 14, TextAnchor.MiddleLeft,
                new Vector2(-180, 10), new Vector2(280, 25));
            CreateText(entry.transform, "Desc", $"<color=#AAAAAA>{desc}</color>", 11,
                TextAnchor.MiddleLeft, new Vector2(-180, -10), new Vector2(280, 20));

            var btn = CreateButton(entry.transform, "Buy", cost, new Vector2(240, 0), new Vector2(100, 35));
            btn.GetComponent<Image>().color = new Color(0.15f, 0.3f, 0.15f, 1f);
            btn.onClick.AddListener(() => onBuy?.Invoke());

            entries.Add(entry);
        }

        private void CreatePlacedEntry(string name, string desc, string removeText, System.Action onRemove)
        {
            var entry = new GameObject("Placed");
            entry.transform.SetParent(contentArea, false);
            var rt = entry.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(620, 50);
            entry.AddComponent<Image>().color = new Color(0.1f, 0.14f, 0.1f, 0.9f);

            CreateText(entry.transform, "Name", name, 14, TextAnchor.MiddleLeft,
                new Vector2(-180, 8), new Vector2(280, 25));
            CreateText(entry.transform, "Desc", $"<color=#88FF88>{desc}</color>", 11,
                TextAnchor.MiddleLeft, new Vector2(-180, -10), new Vector2(280, 20));

            var btn = CreateButton(entry.transform, "Remove", removeText, new Vector2(220, 0), new Vector2(150, 30));
            btn.GetComponent<Image>().color = new Color(0.35f, 0.15f, 0.15f, 1f);
            btn.onClick.AddListener(() => onRemove?.Invoke());

            entries.Add(entry);
        }

        private void CreateActionEntry(string text, string btnLabel, Color bgColor, System.Action onClick)
        {
            var entry = new GameObject("Action");
            entry.transform.SetParent(contentArea, false);
            var rt = entry.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(620, 45);
            entry.AddComponent<Image>().color = bgColor;

            CreateText(entry.transform, "Text", text, 14, TextAnchor.MiddleLeft,
                new Vector2(-130, 0), new Vector2(350, 35));

            var btn = CreateButton(entry.transform, "Btn", btnLabel, new Vector2(240, 0), new Vector2(100, 35));
            btn.GetComponent<Image>().color = new Color(0.2f, 0.35f, 0.2f, 1f);
            btn.onClick.AddListener(() => onClick?.Invoke());

            entries.Add(entry);
        }

        private void CreateBuffLine(string label, float value, string unit)
        {
            var entry = new GameObject("Buff");
            entry.transform.SetParent(contentArea, false);
            var rt = entry.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(620, 30);

            string colorTag = value > 0 ? "#55FF55" : "#888888";
            CreateText(entry.transform, "Text",
                $"{label}: <color={colorTag}>{(value > 0 ? "+" : "")}{value:F1}{unit}</color>",
                14, TextAnchor.MiddleLeft, new Vector2(-100, 0), new Vector2(500, 28));

            entries.Add(entry);
        }

        private void CreateInfoEntry(string message)
        {
            var entry = new GameObject("Info");
            entry.transform.SetParent(contentArea, false);
            var rt = entry.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(620, 30);
            CreateText(entry.transform, "Text", $"<color=#888888>{message}</color>",
                13, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(600, 28));
            entries.Add(entry);
        }

        private void ClearEntries()
        {
            foreach (var e in entries) if (e != null) Destroy(e);
            entries.Clear();
        }

        #region UI 생성

        private void CreateUI()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            mainPanel = new GameObject("HousingPanel");
            mainPanel.transform.SetParent(transform, false);
            var panelRT = mainPanel.AddComponent<RectTransform>();
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(700, 520);
            mainPanel.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.1f, 0.96f);

            var titleObj = CreateText(mainPanel.transform, "Title", "하우징", 22, TextAnchor.MiddleCenter,
                new Vector2(0, 230), new Vector2(300, 35));
            titleText = titleObj.GetComponent<Text>();

            closeButton = CreateButton(mainPanel.transform, "CloseBtn", "X",
                new Vector2(320, 230), new Vector2(40, 40));
            closeButton.onClick.AddListener(() => mainPanel.SetActive(false));

            // 탭
            shopTab = CreateButton(mainPanel.transform, "ShopTab", "가구 상점",
                new Vector2(-180, 195), new Vector2(150, 30));
            shopTab.onClick.AddListener(() => SwitchTab(0));

            placedTab = CreateButton(mainPanel.transform, "PlacedTab", "배치된 가구",
                new Vector2(-20, 195), new Vector2(150, 30));
            placedTab.onClick.AddListener(() => SwitchTab(1));

            buffTab = CreateButton(mainPanel.transform, "BuffTab", "버프 현황",
                new Vector2(140, 195), new Vector2(150, 30));
            buffTab.onClick.AddListener(() => SwitchTab(2));

            // 스크롤뷰
            var scrollObj = CreateScrollView(mainPanel.transform, "Scroll",
                new Vector2(0, -25), new Vector2(670, 400));
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
            img.color = new Color(0.2f, 0.2f, 0.28f, 1f);
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
            txt.fontSize = 13;
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
            layout.spacing = 3;
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
            if (HousingSystem.Instance != null)
                HousingSystem.Instance.OnHousingUpdated -= RefreshCurrentTab;
        }
    }
}
