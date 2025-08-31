using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// EquipmentSlotUI 프리팹 자동 생성기
    /// </summary>
    public static class EquipmentSlotUIPrefabGenerator
    {
        [MenuItem("Dungeon Crawler/Generate Equipment Slot UI Prefab")]
        public static void GenerateEquipmentSlotUI()
        {
            // 메인 슬롯 오브젝트 생성
            GameObject slotObject = new GameObject("EquipmentSlot");
            
            // RectTransform 설정
            RectTransform rectTransform = slotObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(60f, 60f);
            
            // 배경 이미지
            Image backgroundImage = slotObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            
            // EquipmentSlotUI 컴포넌트
            EquipmentSlotUI equipmentSlotUI = slotObject.AddComponent<EquipmentSlotUI>();
            
            // 하위 UI 요소들 생성
            CreateSlotComponents(slotObject, equipmentSlotUI);
            
            // 프리팹 저장
            string prefabPath = "Assets/Resources/UI/EquipmentSlot.prefab";
            
            // Resources/UI 폴더가 없으면 생성
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Resources/UI"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "UI");
            }
            
            // 프리팹 저장
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(slotObject, prefabPath);
            
            // 임시 오브젝트 삭제
            Object.DestroyImmediate(slotObject);
            
            // 생성된 프리팹 선택
            Selection.activeObject = prefab;
            
            Debug.Log("✅ EquipmentSlot 프리팹이 생성되었습니다: " + prefabPath);
        }
        
        /// <summary>
        /// 슬롯 하위 컴포넌트들 생성
        /// </summary>
        private static void CreateSlotComponents(GameObject parent, EquipmentSlotUI slotUI)
        {
            // 1. 아이템 아이콘 이미지
            GameObject iconObject = new GameObject("ItemIcon");
            iconObject.transform.SetParent(parent.transform, false);
            
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            
            Image itemIcon = iconObject.AddComponent<Image>();
            itemIcon.color = Color.clear; // 초기에는 투명
            itemIcon.preserveAspect = true;
            
            // 2. 등급 프레임
            GameObject gradeFrameObject = new GameObject("GradeFrame");
            gradeFrameObject.transform.SetParent(parent.transform, false);
            
            RectTransform gradeRect = gradeFrameObject.AddComponent<RectTransform>();
            gradeRect.anchorMin = Vector2.zero;
            gradeRect.anchorMax = Vector2.one;
            gradeRect.offsetMin = Vector2.zero;
            gradeRect.offsetMax = Vector2.zero;
            
            Image gradeFrame = gradeFrameObject.AddComponent<Image>();
            gradeFrame.color = Color.clear; // 초기에는 투명
            
            // 3. 아이템 수량 텍스트
            GameObject countTextObject = new GameObject("ItemCount");
            countTextObject.transform.SetParent(parent.transform, false);
            
            RectTransform countRect = countTextObject.AddComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.6f, 0f);
            countRect.anchorMax = new Vector2(1f, 0.4f);
            countRect.offsetMin = Vector2.zero;
            countRect.offsetMax = Vector2.zero;
            
            Text itemCountText = countTextObject.AddComponent<Text>();
            itemCountText.text = "";
            itemCountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            itemCountText.fontSize = 12;
            itemCountText.color = Color.white;
            itemCountText.alignment = TextAnchor.LowerRight;
            
            // 4. 내구도 바
            GameObject durabilityBarObject = new GameObject("DurabilityBar");
            durabilityBarObject.transform.SetParent(parent.transform, false);
            
            RectTransform durabilityRect = durabilityBarObject.AddComponent<RectTransform>();
            durabilityRect.anchorMin = new Vector2(0f, 0f);
            durabilityRect.anchorMax = new Vector2(1f, 0.15f);
            durabilityRect.offsetMin = new Vector2(2f, 2f);
            durabilityRect.offsetMax = new Vector2(-2f, -2f);
            
            Image durabilityBar = durabilityBarObject.AddComponent<Image>();
            durabilityBar.color = Color.green;
            durabilityBar.type = Image.Type.Filled;
            durabilityBar.fillMethod = Image.FillMethod.Horizontal;
            durabilityBar.fillAmount = 1f;
            
            // 5. 슬롯 라벨
            GameObject slotLabelObject = new GameObject("SlotLabel");
            slotLabelObject.transform.SetParent(parent.transform, false);
            
            RectTransform labelRect = slotLabelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.sizeDelta = new Vector2(0f, 15f);
            labelRect.anchoredPosition = new Vector2(2f, 0f);
            
            Text slotLabel = slotLabelObject.AddComponent<Text>();
            slotLabel.text = "슬롯";
            slotLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            slotLabel.fontSize = 10;
            slotLabel.color = Color.white;
            slotLabel.alignment = TextAnchor.UpperLeft;
            
            // SerializedObject를 사용하여 private 필드들 설정
            SetupEquipmentSlotUIReferences(slotUI, parent.GetComponent<Image>(), itemIcon, 
                gradeFrame, itemCountText, durabilityBar, slotLabel);
        }
        
        /// <summary>
        /// EquipmentSlotUI 컴포넌트의 private 필드들 설정
        /// </summary>
        private static void SetupEquipmentSlotUIReferences(EquipmentSlotUI slotUI, Image background,
            Image itemIcon, Image gradeFrame, Text itemCountText, Image durabilityBar, Text slotLabel)
        {
            var serializedSlot = new SerializedObject(slotUI);
            
            // Private 필드들 설정
            serializedSlot.FindProperty("slotBackground").objectReferenceValue = background;
            serializedSlot.FindProperty("itemIcon").objectReferenceValue = itemIcon;
            serializedSlot.FindProperty("gradeFrame").objectReferenceValue = gradeFrame;
            serializedSlot.FindProperty("itemCountText").objectReferenceValue = itemCountText;
            serializedSlot.FindProperty("durabilityBar").objectReferenceValue = durabilityBar;
            serializedSlot.FindProperty("slotLabel").objectReferenceValue = slotLabel;
            
            // 색상 설정
            serializedSlot.FindProperty("emptySlotColor").colorValue = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            serializedSlot.FindProperty("occupiedSlotColor").colorValue = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            serializedSlot.FindProperty("hoverColor").colorValue = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            
            serializedSlot.ApplyModifiedProperties();
            
            Debug.Log("📋 EquipmentSlotUI 컴포넌트 참조 설정 완료");
        }
    }
}