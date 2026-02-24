using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 창고 UI - 창고 NPC 상호작용 시 표시
    /// 아이템 보관/인출, 창고 확장
    /// </summary>
    public class StorageUI : MonoBehaviour
    {
        private GameObject mainPanel;
        private Transform storageContent;
        private Text slotInfoText;
        private Button expandButton;
        private Button closeButton;

        private List<GameObject> storageEntries = new List<GameObject>();
        private bool isInitialized;

        private void Start()
        {
            CreateUI();
            isInitialized = true;
            mainPanel.SetActive(false);

            if (StorageSystem.Instance != null)
            {
                StorageSystem.Instance.OnStorageUpdated += RefreshStorage;
                StorageSystem.Instance.OnStorageExpanded += OnExpanded;
            }
        }

        /// <summary>
        /// 창고 열기 (외부 호출용)
        /// </summary>
        public void OpenStorage()
        {
            if (!isInitialized) return;
            mainPanel.SetActive(true);
            if (StorageSystem.Instance != null)
                StorageSystem.Instance.OpenStorageServerRpc();
        }

        public void CloseStorage()
        {
            mainPanel.SetActive(false);
        }

        private void Update()
        {
            if (mainPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                CloseStorage();
        }

        private void RefreshStorage()
        {
            foreach (var entry in storageEntries)
                if (entry != null) Destroy(entry);
            storageEntries.Clear();

            if (StorageSystem.Instance == null) return;

            var storage = StorageSystem.Instance.LocalStorage;
            int maxSlots = StorageSystem.Instance.LocalMaxSlots;
            slotInfoText.text = $"창고: {storage.Count}/{maxSlots}칸";

            for (int i = 0; i < storage.Count; i++)
            {
                var item = storage[i];
                if (item == null) continue;
                CreateStorageEntry(i, item);
            }
        }

        private void CreateStorageEntry(int slot, ItemInstance item)
        {
            var entry = new GameObject("StorageEntry");
            entry.transform.SetParent(storageContent, false);
            var rt = entry.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(440, 40);
            entry.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f, 0.9f);

            string itemName = item.ItemData != null ? item.ItemData.ItemName : item.ItemId;
            string gradeColor = GetGradeColor(item);
            string text = $"<color={gradeColor}>{itemName}</color> ×{item.Quantity}";

            if (item.EnhanceLevel > 0)
                text += $" <color=#00FF88>+{item.EnhanceLevel}</color>";

            CreateText(entry.transform, "ItemText", text, 13, TextAnchor.MiddleLeft,
                new Vector2(-40, 0), new Vector2(320, 35));

            // 인출 버튼
            var withdrawBtn = CreateButton(entry.transform, "WithdrawBtn", "인출",
                new Vector2(190, 0), new Vector2(60, 30));
            int slotIndex = slot;
            withdrawBtn.onClick.AddListener(() =>
            {
                if (StorageSystem.Instance != null)
                    StorageSystem.Instance.WithdrawItemServerRpc(slotIndex);
            });

            storageEntries.Add(entry);
        }

        private void OnExpanded(int newMaxSlots)
        {
            RefreshStorage();
        }

        private string GetGradeColor(ItemInstance item)
        {
            if (item.ItemData == null) return "#FFFFFF";
            int grade = (int)item.ItemData.Grade;
            return grade switch
            {
                1 => "#CCCCCC",  // Common
                2 => "#00FF00",  // Uncommon
                3 => "#0088FF",  // Rare
                4 => "#AA00FF",  // Epic
                5 => "#FF8800",  // Legendary
                _ => "#FFFFFF"
            };
        }

        #region UI 생성

        private void CreateUI()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 130;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            mainPanel = new GameObject("StoragePanel");
            mainPanel.transform.SetParent(transform, false);
            var panelRT = mainPanel.AddComponent<RectTransform>();
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(500, 450);
            mainPanel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

            CreateText(mainPanel.transform, "Title", "창고", 22, TextAnchor.MiddleCenter,
                new Vector2(0, 195), new Vector2(300, 40));

            slotInfoText = CreateText(mainPanel.transform, "SlotInfo", "창고: 0/30칸", 14, TextAnchor.MiddleRight,
                new Vector2(150, 195), new Vector2(150, 30)).GetComponent<Text>();

            closeButton = CreateButton(mainPanel.transform, "CloseBtn", "X",
                new Vector2(220, 195), new Vector2(40, 40));
            closeButton.onClick.AddListener(CloseStorage);

            // 스크롤뷰
            var scrollObj = CreateScrollView(mainPanel.transform, "StorageScroll",
                new Vector2(0, 10), new Vector2(460, 310));
            storageContent = scrollObj.transform.Find("Viewport/Content");

            // 확장 버튼
            expandButton = CreateButton(mainPanel.transform, "ExpandBtn", "창고 확장 (+10칸)",
                new Vector2(0, -195), new Vector2(200, 35));
            expandButton.onClick.AddListener(() =>
            {
                if (StorageSystem.Instance != null)
                    StorageSystem.Instance.ExpandStorageServerRpc();
            });
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
            img.color = new Color(0.25f, 0.25f, 0.3f, 1f);
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
            if (StorageSystem.Instance != null)
            {
                StorageSystem.Instance.OnStorageUpdated -= RefreshStorage;
                StorageSystem.Instance.OnStorageExpanded -= OnExpanded;
            }
        }
    }
}
