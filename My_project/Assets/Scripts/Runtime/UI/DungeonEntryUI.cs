using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 던전 입장 UI - 던전 선택, 정보 표시, 입장 버튼
    /// </summary>
    public class DungeonEntryUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform dungeonListContent;
        [SerializeField] private GameObject dungeonSlotPrefab; // 없으면 자동 생성

        [Header("Detail Panel")]
        [SerializeField] private Text dungeonNameText;
        [SerializeField] private Text dungeonDescText;
        [SerializeField] private Text difficultyText;
        [SerializeField] private Text recommendedLevelText;
        [SerializeField] private Text rewardText;
        [SerializeField] private Text floorCountText;
        [SerializeField] private Text timeLimitText;
        [SerializeField] private Button enterButton;
        [SerializeField] private Text enterButtonText;

        private DungeonManager dungeonManager;
        private List<DungeonData> dungeonList = new List<DungeonData>();
        private int selectedIndex = -1;

        private void Awake()
        {
            if (panel != null)
                panel.SetActive(false);

            if (enterButton != null)
                enterButton.onClick.AddListener(OnEnterClicked);
        }

        private void Start()
        {
            dungeonManager = DungeonManager.Instance;
            if (dungeonManager == null)
                dungeonManager = FindFirstObjectByType<DungeonManager>();

            LoadDungeonList();
        }

        private void LoadDungeonList()
        {
            // Resources에서 던전 데이터 로드
            var loadedDungeons = Resources.LoadAll<DungeonData>("ScriptableObjects/DungeonData");
            dungeonList.Clear();
            dungeonList.AddRange(loadedDungeons);

            BuildDungeonSlots();
        }

        private void BuildDungeonSlots()
        {
            if (dungeonListContent == null) return;

            // 기존 자식 제거
            for (int i = dungeonListContent.childCount - 1; i >= 0; i--)
                Destroy(dungeonListContent.GetChild(i).gameObject);

            for (int i = 0; i < dungeonList.Count; i++)
            {
                var dungeon = dungeonList[i];
                var slotGo = CreateDungeonSlot(dungeon, i);
                slotGo.transform.SetParent(dungeonListContent, false);
            }
        }

        private GameObject CreateDungeonSlot(DungeonData data, int index)
        {
            var go = new GameObject($"DungeonSlot_{data.DungeonName}");

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 60);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 5, 5);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleLeft;

            // 던전 이름
            var nameObj = new GameObject("Name");
            nameObj.transform.SetParent(go.transform, false);
            var nameText = nameObj.AddComponent<Text>();
            nameText.text = data.DungeonName;
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 16;
            nameText.color = Color.white;
            var nameLE = nameObj.AddComponent<LayoutElement>();
            nameLE.flexibleWidth = 1;

            // 난이도 표시
            var diffObj = new GameObject("Difficulty");
            diffObj.transform.SetParent(go.transform, false);
            var diffText = diffObj.AddComponent<Text>();
            diffText.text = GetDifficultyDisplay(data.Difficulty);
            diffText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            diffText.fontSize = 14;
            diffText.color = GetDifficultyColor(data.Difficulty);
            diffText.alignment = TextAnchor.MiddleRight;
            var diffLE = diffObj.AddComponent<LayoutElement>();
            diffLE.preferredWidth = 80;

            // 버튼 기능
            var btn = go.AddComponent<Button>();
            int capturedIndex = index;
            btn.onClick.AddListener(() => SelectDungeon(capturedIndex));

            return go;
        }

        private void SelectDungeon(int index)
        {
            if (index < 0 || index >= dungeonList.Count) return;
            selectedIndex = index;
            var data = dungeonList[index];

            if (dungeonNameText != null)
                dungeonNameText.text = data.DungeonName;
            if (dungeonDescText != null)
                dungeonDescText.text = data.Description;
            if (difficultyText != null)
            {
                difficultyText.text = GetDifficultyDisplay(data.Difficulty);
                difficultyText.color = GetDifficultyColor(data.Difficulty);
            }
            if (recommendedLevelText != null)
                recommendedLevelText.text = $"권장 레벨: {data.RecommendedLevel}";
            if (floorCountText != null)
                floorCountText.text = $"층수: {data.MaxFloors}층";
            if (timeLimitText != null)
                timeLimitText.text = $"시간: {data.TimeLimit / 60f:F0}분";
            if (rewardText != null)
                rewardText.text = $"경험치 x{data.ExpMultiplierPerFloor:F1} | 골드 x{data.GoldMultiplierPerFloor:F1}";

            if (enterButton != null)
                enterButton.interactable = true;
        }

        private void OnEnterClicked()
        {
            if (selectedIndex < 0 || dungeonManager == null) return;

            dungeonManager.StartDungeonServerRpc(selectedIndex);
            Hide();
        }

        public void Show()
        {
            if (panel != null)
                panel.SetActive(true);
            selectedIndex = -1;

            if (enterButton != null)
                enterButton.interactable = false;
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        public void Toggle()
        {
            if (panel != null)
            {
                if (panel.activeSelf) Hide();
                else Show();
            }
        }

        private string GetDifficultyDisplay(DungeonDifficulty diff)
        {
            switch (diff)
            {
                case DungeonDifficulty.Easy: return "쉬움";
                case DungeonDifficulty.Normal: return "보통";
                case DungeonDifficulty.Hard: return "어려움";
                case DungeonDifficulty.Nightmare: return "악몽";
                default: return "???";
            }
        }

        private Color GetDifficultyColor(DungeonDifficulty diff)
        {
            switch (diff)
            {
                case DungeonDifficulty.Easy: return Color.green;
                case DungeonDifficulty.Normal: return Color.yellow;
                case DungeonDifficulty.Hard: return new Color(1f, 0.5f, 0f);
                case DungeonDifficulty.Nightmare: return Color.red;
                default: return Color.white;
            }
        }
    }
}
