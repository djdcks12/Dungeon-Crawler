using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 아이템 툴팁 UI 프리팹 자동 생성기
    /// </summary>
    public static class ItemTooltipPrefabGenerator
    {
        [MenuItem("Dungeon Crawler/Generate Item Tooltip UI")]
        public static void GenerateItemTooltipUI()
        {
            // Canvas 생성
            GameObject canvasObject = new GameObject("ItemTooltipCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // 최상위에 표시
            
            CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            
            GraphicRaycaster graphicRaycaster = canvasObject.AddComponent<GraphicRaycaster>();
            
            // ItemTooltipManager 컴포넌트 추가
            ItemTooltipManager tooltipManager = canvasObject.AddComponent<ItemTooltipManager>();
            
            // 툴팁 패널 생성
            GameObject tooltipPanel = CreateTooltipPanel();
            tooltipPanel.transform.SetParent(canvasObject.transform, false);
            
            // 배경 이미지
            GameObject backgroundObject = CreateBackgroundPanel(tooltipPanel);
            
            // 아이템 아이콘
            GameObject iconObject = CreateItemIcon(tooltipPanel);
            
            // 아이템 이름 텍스트
            GameObject nameTextObject = CreateItemNameText(tooltipPanel);
            
            // 아이템 설명 텍스트
            GameObject descriptionTextObject = CreateItemDescriptionText(tooltipPanel);
            
            // 아이템 스탯 텍스트
            GameObject statsTextObject = CreateItemStatsText(tooltipPanel);
            
            // ItemTooltipManager에 참조 연결
            SetupTooltipManagerReferences(tooltipManager, tooltipPanel, backgroundObject, iconObject, 
                nameTextObject, descriptionTextObject, statsTextObject);
            
            // 프리팹으로 저장
            string prefabPath = "Assets/Prefabs/UI/ItemTooltipCanvas.prefab";
            
            // 폴더가 없으면 생성
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(prefabPath));
            
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(canvasObject, prefabPath);
            
            // 씬에서 제거
            Object.DestroyImmediate(canvasObject);
            
            // 생성된 프리팹 선택
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            
            Debug.Log($"✅ ItemTooltip UI 프리팹이 생성되었습니다: {prefabPath}");
        }
        
        private static GameObject CreateTooltipPanel()
        {
            GameObject tooltipPanel = new GameObject("TooltipPanel");
            RectTransform panelRect = tooltipPanel.AddComponent<RectTransform>();
            
            // 크기와 위치 설정
            panelRect.sizeDelta = new Vector2(350f, 200f);
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = Vector2.zero;
            
            // 초기에는 비활성화
            tooltipPanel.SetActive(false);
            
            return tooltipPanel;
        }
        
        private static GameObject CreateBackgroundPanel(GameObject parent)
        {
            GameObject backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(parent.transform, false);
            
            RectTransform bgRect = backgroundObject.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            Image backgroundImage = backgroundObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f); // 반투명 검은색
            backgroundImage.type = Image.Type.Sliced;
            
            // 테두리 추가
            Outline outline = backgroundObject.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(2f, 2f);
            
            return backgroundObject;
        }
        
        private static GameObject CreateItemIcon(GameObject parent)
        {
            GameObject iconObject = new GameObject("ItemIcon");
            iconObject.transform.SetParent(parent.transform, false);
            
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 1f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);
            iconRect.sizeDelta = new Vector2(48f, 48f);
            iconRect.anchoredPosition = new Vector2(10f, -10f);
            
            Image iconImage = iconObject.AddComponent<Image>();
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
            
            return iconObject;
        }
        
        private static GameObject CreateItemNameText(GameObject parent)
        {
            GameObject nameTextObject = new GameObject("ItemNameText");
            nameTextObject.transform.SetParent(parent.transform, false);
            
            RectTransform nameRect = nameTextObject.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0f, 1f);
            nameRect.sizeDelta = new Vector2(-70f, 30f);
            nameRect.anchoredPosition = new Vector2(65f, -10f);
            
            Text nameText = nameTextObject.AddComponent<Text>();
            nameText.text = "아이템 이름";
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 16;
            nameText.fontStyle = FontStyle.Bold;
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleLeft;
            
            return nameTextObject;
        }
        
        private static GameObject CreateItemDescriptionText(GameObject parent)
        {
            GameObject descriptionTextObject = new GameObject("ItemDescriptionText");
            descriptionTextObject.transform.SetParent(parent.transform, false);
            
            RectTransform descRect = descriptionTextObject.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0f, 1f);
            descRect.anchorMax = new Vector2(1f, 1f);
            descRect.pivot = new Vector2(0f, 1f);
            descRect.sizeDelta = new Vector2(-20f, 40f);
            descRect.anchoredPosition = new Vector2(10f, -45f);
            
            Text descriptionText = descriptionTextObject.AddComponent<Text>();
            descriptionText.text = "아이템 설명";
            descriptionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descriptionText.fontSize = 12;
            descriptionText.color = new Color(0.8f, 0.8f, 0.8f);
            descriptionText.alignment = TextAnchor.UpperLeft;
            descriptionText.verticalOverflow = VerticalWrapMode.Overflow;
            
            return descriptionTextObject;
        }
        
        private static GameObject CreateItemStatsText(GameObject parent)
        {
            GameObject statsTextObject = new GameObject("ItemStatsText");
            statsTextObject.transform.SetParent(parent.transform, false);
            
            RectTransform statsRect = statsTextObject.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0f, 0f);
            statsRect.anchorMax = new Vector2(1f, 1f);
            statsRect.pivot = new Vector2(0f, 1f);
            statsRect.offsetMin = new Vector2(10f, 10f);
            statsRect.offsetMax = new Vector2(-10f, -90f);
            
            Text statsText = statsTextObject.AddComponent<Text>();
            statsText.text = "스탯 정보";
            statsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statsText.fontSize = 11;
            statsText.color = new Color(0.7f, 0.9f, 0.7f);
            statsText.alignment = TextAnchor.UpperLeft;
            statsText.verticalOverflow = VerticalWrapMode.Overflow;
            
            return statsTextObject;
        }
        
        private static void SetupTooltipManagerReferences(ItemTooltipManager manager, GameObject panel, 
            GameObject background, GameObject icon, GameObject nameText, GameObject descText, GameObject statsText)
        {
            // SerializedObject를 사용하여 private 필드에 값 설정
            SerializedObject serializedManager = new SerializedObject(manager);
            
            serializedManager.FindProperty("tooltipPanel").objectReferenceValue = panel;
            serializedManager.FindProperty("backgroundImage").objectReferenceValue = background.GetComponent<Image>();
            serializedManager.FindProperty("itemIconImage").objectReferenceValue = icon.GetComponent<Image>();
            serializedManager.FindProperty("itemNameText").objectReferenceValue = nameText.GetComponent<Text>();
            serializedManager.FindProperty("itemDescriptionText").objectReferenceValue = descText.GetComponent<Text>();
            serializedManager.FindProperty("itemStatsText").objectReferenceValue = statsText.GetComponent<Text>();
            
            // 기본 설정값 적용
            serializedManager.FindProperty("offset").vector2Value = new Vector2(10f, 10f);
            serializedManager.FindProperty("maxWidth").floatValue = 350f;
            
            serializedManager.ApplyModifiedProperties();
        }
    }
}