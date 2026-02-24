using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// Relic UI - View, equip, and enhance relics.
    /// Hotkey: F11
    /// </summary>
    public class RelicUI : MonoBehaviour
    {
        private GameObject mainPanel;
        private Text titleText;
        private Text equippedText;
        private Transform contentArea;
        private Button closeButton;

        // Enhance panel
        private Text enhanceInfoText;
        private string selectedBaseId;
        private string selectedMaterialId;

        private List<GameObject> entries = new List<GameObject>();
        private bool isInitialized;

        private static readonly Color[] GradeColors =
        {
            new Color(0.6f, 0.6f, 0.6f),  // Common
            new Color(0.3f, 0.6f, 1f),     // Rare
            new Color(0.7f, 0.3f, 1f)      // Legendary
        };

        private void Start()
        {
            CreateUI();
            isInitialized = true;
            mainPanel.SetActive(false);

            if (RelicSystem.Instance != null)
            {
                RelicSystem.Instance.OnRelicsUpdated += RefreshDisplay;
                RelicSystem.Instance.OnRelicEnhanced += OnRelicEnhanced;
            }
        }

        private void Update()
        {
            if (!isInitialized) return;

            if (Input.GetKeyDown(KeyCode.F11))
            {
                if (mainPanel.activeSelf) mainPanel.SetActive(false);
                else OpenRelics();
            }

            if (mainPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                mainPanel.SetActive(false);
        }

        public void OpenRelics()
        {
            mainPanel.SetActive(true);
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            ClearEntries();
            if (RelicSystem.Instance == null) return;

            var clientId = Unity.Netcode.NetworkManager.Singleton?.LocalClientId ?? 0;
            var relics = RelicSystem.Instance.GetPlayerRelics(clientId);
            var equippedId = RelicSystem.Instance.GetEquippedRelicId(clientId);

            titleText.text = $"유물 ({relics.Count}개 보유)";

            if (!string.IsNullOrEmpty(equippedId))
            {
                var equippedRelic = relics.Find(r => r.instanceId == equippedId);
                string equippedName = equippedId;
                if (equippedRelic != null)
                {
                    var eqTemplate = RelicSystem.Instance.GetTemplate(equippedRelic.templateId);
                    if (eqTemplate != null) equippedName = eqTemplate.displayName;
                }
                equippedText.text = $"장착 유물: <color=#FFD700>{equippedName}</color>";
            }
            else
            {
                equippedText.text = "장착 유물: 없음";
            }

            foreach (var relic in relics)
            {
                var template = RelicSystem.Instance.GetTemplate(relic.templateId);
                if (template == null) continue;

                bool isEquipped = relic.instanceId == equippedId;
                int gradeIdx = (int)relic.grade;
                Color gradeColor = gradeIdx < GradeColors.Length ? GradeColors[gradeIdx] : Color.white;

                var entry = new GameObject("RelicEntry");
                entry.transform.SetParent(contentArea, false);
                var layout = entry.AddComponent<LayoutElement>();
                layout.preferredHeight = 55;
                var bg = entry.AddComponent<Image>();
                bg.color = isEquipped
                    ? new Color(0.2f, 0.15f, 0.05f, 0.95f)
                    : new Color(0.12f, 0.12f, 0.18f, 0.9f);

                // Name + grade
                var nameGo = new GameObject("Name");
                nameGo.transform.SetParent(entry.transform, false);
                var nameRect = nameGo.AddComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0, 0.5f);
                nameRect.anchorMax = new Vector2(0.45f, 1);
                nameRect.offsetMin = new Vector2(10, 0);
                nameRect.offsetMax = Vector2.zero;
                var nameText = nameGo.AddComponent<Text>();
                nameText.text = $"{template.displayName} [{relic.grade}]";
                nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                nameText.fontSize = 14;
                nameText.color = gradeColor;
                nameText.fontStyle = FontStyle.Bold;

                // Bonuses
                var bonusGo = new GameObject("Bonus");
                bonusGo.transform.SetParent(entry.transform, false);
                var bonusRect = bonusGo.AddComponent<RectTransform>();
                bonusRect.anchorMin = new Vector2(0, 0);
                bonusRect.anchorMax = new Vector2(0.6f, 0.5f);
                bonusRect.offsetMin = new Vector2(10, 0);
                bonusRect.offsetMax = Vector2.zero;
                var bonusText = bonusGo.AddComponent<Text>();
                bonusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                bonusText.fontSize = 11;
                bonusText.color = new Color(0.6f, 0.8f, 0.6f);

                float mult = relic.GetEnhanceMultiplier();
                var bonusParts = new List<string>();
                if (template.baseBonuses != null)
                {
                    foreach (var kvp in template.baseBonuses)
                        bonusParts.Add($"{kvp.Key} +{kvp.Value * mult:F1}");
                }
                bonusText.text = string.Join(", ", bonusParts);

                // Equip button
                if (!isEquipped)
                {
                    string capturedId = relic.instanceId;
                    var btnGo = new GameObject("EquipBtn");
                    btnGo.transform.SetParent(entry.transform, false);
                    var btnRect = btnGo.AddComponent<RectTransform>();
                    btnRect.anchorMin = new Vector2(0.62f, 0.15f);
                    btnRect.anchorMax = new Vector2(0.8f, 0.85f);
                    btnRect.offsetMin = Vector2.zero;
                    btnRect.offsetMax = Vector2.zero;
                    var btnImg = btnGo.AddComponent<Image>();
                    btnImg.color = new Color(0.2f, 0.35f, 0.2f, 1f);
                    var btn = btnGo.AddComponent<Button>();
                    btn.targetGraphic = btnImg;
                    btn.onClick.AddListener(() =>
                    {
                        RelicSystem.Instance?.EquipRelicServerRpc(capturedId);
                    });

                    CreateButtonText(btnGo.transform, "장착");
                }
                else
                {
                    var labelGo = new GameObject("Equipped");
                    labelGo.transform.SetParent(entry.transform, false);
                    var labelRect = labelGo.AddComponent<RectTransform>();
                    labelRect.anchorMin = new Vector2(0.62f, 0.15f);
                    labelRect.anchorMax = new Vector2(0.8f, 0.85f);
                    labelRect.offsetMin = Vector2.zero;
                    labelRect.offsetMax = Vector2.zero;
                    var labelText = labelGo.AddComponent<Text>();
                    labelText.text = "장착중";
                    labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    labelText.fontSize = 12;
                    labelText.color = new Color(1f, 0.8f, 0.2f);
                    labelText.alignment = TextAnchor.MiddleCenter;
                }

                // Enhance select button
                string enhInstanceId = relic.instanceId;
                var enhBtnGo = new GameObject("EnhanceBtn");
                enhBtnGo.transform.SetParent(entry.transform, false);
                var enhBtnRect = enhBtnGo.AddComponent<RectTransform>();
                enhBtnRect.anchorMin = new Vector2(0.82f, 0.15f);
                enhBtnRect.anchorMax = new Vector2(0.98f, 0.85f);
                enhBtnRect.offsetMin = Vector2.zero;
                enhBtnRect.offsetMax = Vector2.zero;
                var enhBtnImg = enhBtnGo.AddComponent<Image>();
                enhBtnImg.color = new Color(0.3f, 0.2f, 0.4f, 1f);
                var enhBtn = enhBtnGo.AddComponent<Button>();
                enhBtn.targetGraphic = enhBtnImg;
                enhBtn.onClick.AddListener(() =>
                {
                    if (string.IsNullOrEmpty(selectedBaseId))
                    {
                        selectedBaseId = enhInstanceId;
                        enhanceInfoText.text = $"강화 대상: {template.displayName} → 재료 유물 선택";
                    }
                    else if (selectedBaseId != enhInstanceId)
                    {
                        selectedMaterialId = enhInstanceId;
                        RelicSystem.Instance?.EnhanceRelicServerRpc(selectedBaseId, selectedMaterialId);
                        selectedBaseId = null;
                        selectedMaterialId = null;
                        enhanceInfoText.text = "";
                    }
                });
                CreateButtonText(enhBtnGo.transform, "강화");

                entries.Add(entry);
            }
        }

        private void CreateButtonText(Transform parent, string label)
        {
            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(parent, false);
            var txtRect = txtGo.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;
            var txt = txtGo.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 12;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
        }

        private void ClearEntries()
        {
            foreach (var e in entries)
                if (e != null) Destroy(e);
            entries.Clear();
        }

        private void CreateUI()
        {
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 126;
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
            }

            mainPanel = new GameObject("RelicPanel");
            mainPanel.transform.SetParent(transform, false);
            var panelRect = mainPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(550, 450);
            var panelImg = mainPanel.AddComponent<Image>();
            panelImg.color = new Color(0.08f, 0.06f, 0.1f, 0.95f);

            titleText = CreateText(mainPanel.transform, "Title", "유물", 20,
                new Vector2(0, 190), new Vector2(400, 40));
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.7f, 0.5f, 1f);

            equippedText = CreateText(mainPanel.transform, "Equipped", "장착 유물: 없음", 14,
                new Vector2(0, 155), new Vector2(450, 25));
            equippedText.alignment = TextAnchor.MiddleCenter;

            enhanceInfoText = CreateText(mainPanel.transform, "EnhanceInfo", "", 12,
                new Vector2(0, 135), new Vector2(450, 20));
            enhanceInfoText.alignment = TextAnchor.MiddleCenter;
            enhanceInfoText.color = new Color(0.8f, 0.6f, 1f);

            // Scroll content
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(mainPanel.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchoredPosition = new Vector2(0, -30);
            scrollRect.sizeDelta = new Vector2(510, 290);
            var scroll = scrollGo.AddComponent<ScrollRect>();

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var cRect = content.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1);
            cRect.sizeDelta = new Vector2(0, 0);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = cRect;
            scroll.viewport = vpRect;
            contentArea = content.transform;

            closeButton = CreateButton(mainPanel.transform, "CloseBtn", "X",
                new Vector2(245, 195), new Vector2(40, 40), () => mainPanel.SetActive(false));
        }

        private Text CreateText(Transform parent, string name, string text, int fontSize,
            Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            return t;
        }

        private Button CreateButton(Transform parent, string name, string label,
            Vector2 pos, Vector2 size, System.Action onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.15f, 0.3f, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var txtRect = txtGo.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;
            var txt = txtGo.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 14;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }

        private void OnRelicEnhanced(string relicId) => RefreshDisplay();

        private void OnDestroy()
        {
            if (RelicSystem.Instance != null)
            {
                RelicSystem.Instance.OnRelicsUpdated -= RefreshDisplay;
                RelicSystem.Instance.OnRelicEnhanced -= OnRelicEnhanced;
            }
        }
    }
}
