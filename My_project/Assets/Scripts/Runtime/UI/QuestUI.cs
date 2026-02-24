using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 퀘스트 UI - 트래커(화면 우측) + 퀘스트 목록/상세 패널
    /// </summary>
    public class QuestUI : MonoBehaviour
    {
        public static QuestUI Instance { get; private set; }

        [Header("트래커 UI")]
        [SerializeField] private GameObject trackerPanel;
        [SerializeField] private Transform trackerContent;

        [Header("퀘스트 목록 UI")]
        [SerializeField] private GameObject questListPanel;
        [SerializeField] private Transform questListContent;
        [SerializeField] private Text questDetailNameText;
        [SerializeField] private Text questDetailDescText;
        [SerializeField] private Text questDetailObjectivesText;
        [SerializeField] private Text questDetailRewardText;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button abandonButton;
        [SerializeField] private Button claimButton;
        [SerializeField] private Button closeButton;

        [Header("설정")]
        [SerializeField] private KeyCode toggleKey = KeyCode.J;
        [SerializeField] private int maxTrackerEntries = 5;

        private string selectedQuestId;
        private List<GameObject> trackerEntries = new List<GameObject>();
        private List<GameObject> listEntries = new List<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (trackerPanel == null)
                CreateTrackerUI();
            if (questListPanel == null)
                CreateQuestListUI();

            if (questListPanel != null)
                questListPanel.SetActive(false);
        }

        private void Start()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestStatusChanged += OnQuestStatusChanged;
                QuestManager.Instance.OnQuestProgressUpdated += OnQuestProgressUpdated;
                QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleQuestList();
            }
        }

        /// <summary>
        /// 퀘스트 목록 토글
        /// </summary>
        public void ToggleQuestList()
        {
            if (questListPanel == null) return;
            bool show = !questListPanel.activeSelf;
            questListPanel.SetActive(show);
            if (show) RefreshQuestList();
        }

        /// <summary>
        /// NPC에서 수락 가능한 퀘스트 표시
        /// </summary>
        public void ShowAvailableQuests(int playerLevel)
        {
            if (questListPanel == null) return;
            questListPanel.SetActive(true);
            RefreshQuestList();
        }

        /// <summary>
        /// 트래커 갱신
        /// </summary>
        public void RefreshTracker()
        {
            // 기존 엔트리 제거
            foreach (var entry in trackerEntries)
            {
                if (entry != null) Destroy(entry);
            }
            trackerEntries.Clear();

            if (QuestManager.Instance == null || trackerContent == null) return;

            var activeQuests = QuestManager.Instance.GetActiveQuests();
            int count = Mathf.Min(activeQuests.Count, maxTrackerEntries);

            for (int i = 0; i < count; i++)
            {
                var progress = activeQuests[i];
                var questData = QuestManager.Instance.GetQuestData(progress.questId);
                if (questData == null) continue;

                var entry = CreateTrackerEntry(questData, progress);
                trackerEntries.Add(entry);
            }
        }

        /// <summary>
        /// 퀘스트 목록 갱신
        /// </summary>
        private void RefreshQuestList()
        {
            foreach (var entry in listEntries)
            {
                if (entry != null) Destroy(entry);
            }
            listEntries.Clear();

            if (QuestManager.Instance == null || questListContent == null) return;

            // 활성 퀘스트
            var activeQuests = QuestManager.Instance.GetActiveQuests();
            foreach (var progress in activeQuests)
            {
                var questData = QuestManager.Instance.GetQuestData(progress.questId);
                if (questData == null) continue;

                var entry = CreateListEntry(questData, progress);
                listEntries.Add(entry);
            }

            // 첫 번째 퀘스트 선택
            if (activeQuests.Count > 0)
                SelectQuest(activeQuests[0].questId);
            else
                ClearDetail();
        }

        /// <summary>
        /// 퀘스트 선택
        /// </summary>
        private void SelectQuest(string questId)
        {
            selectedQuestId = questId;
            var questData = QuestManager.Instance?.GetQuestData(questId);
            var progress = QuestManager.Instance?.GetQuestProgress(questId);

            if (questData == null)
            {
                ClearDetail();
                return;
            }

            if (questDetailNameText != null)
            {
                questDetailNameText.text = questData.QuestName;
                questDetailNameText.color = questData.GetDifficultyColor();
            }

            if (questDetailDescText != null)
                questDetailDescText.text = questData.Description;

            // 목표 텍스트
            if (questDetailObjectivesText != null)
            {
                string objText = "<b>목표</b>\n";
                for (int i = 0; i < questData.Objectives.Length; i++)
                {
                    var obj = questData.Objectives[i];
                    int current = (progress != null && i < progress.currentCounts.Length) ? progress.currentCounts[i] : 0;
                    bool done = current >= obj.requiredCount;
                    string color = done ? "#44FF44" : "#FFFFFF";
                    string check = done ? "[V]" : "[ ]";
                    objText += $"<color={color}>{check} {obj.targetName}: {current}/{obj.requiredCount}</color>\n";
                }
                questDetailObjectivesText.text = objText;
            }

            // 보상 텍스트
            if (questDetailRewardText != null)
            {
                var reward = questData.Reward;
                string rewardText = "<b>보상</b>\n";
                if (reward.experienceReward > 0) rewardText += $"EXP: {reward.experienceReward}\n";
                if (reward.goldReward > 0) rewardText += $"Gold: {reward.goldReward}\n";
                if (!string.IsNullOrEmpty(reward.itemRewardId))
                {
                    var item = ItemDatabase.GetItem(reward.itemRewardId);
                    string itemName = item != null ? item.ItemName : reward.itemRewardId;
                    rewardText += $"아이템: {itemName} x{reward.itemRewardCount}\n";
                }
                questDetailRewardText.text = rewardText;
            }

            // 버튼 상태
            UpdateButtons(progress);
        }

        /// <summary>
        /// 버튼 상태 업데이트
        /// </summary>
        private void UpdateButtons(QuestProgress progress)
        {
            if (acceptButton != null)
            {
                acceptButton.gameObject.SetActive(progress == null);
                acceptButton.onClick.RemoveAllListeners();
                acceptButton.onClick.AddListener(() => OnAcceptClicked());
            }

            if (abandonButton != null)
            {
                abandonButton.gameObject.SetActive(progress != null && progress.status == QuestStatus.Active);
                abandonButton.onClick.RemoveAllListeners();
                abandonButton.onClick.AddListener(() => OnAbandonClicked());
            }

            if (claimButton != null)
            {
                claimButton.gameObject.SetActive(progress != null && progress.status == QuestStatus.Completed);
                claimButton.onClick.RemoveAllListeners();
                claimButton.onClick.AddListener(() => OnClaimClicked());
            }
        }

        private void ClearDetail()
        {
            if (questDetailNameText != null) questDetailNameText.text = "";
            if (questDetailDescText != null) questDetailDescText.text = "퀘스트를 선택하세요";
            if (questDetailObjectivesText != null) questDetailObjectivesText.text = "";
            if (questDetailRewardText != null) questDetailRewardText.text = "";
            if (acceptButton != null) acceptButton.gameObject.SetActive(false);
            if (abandonButton != null) abandonButton.gameObject.SetActive(false);
            if (claimButton != null) claimButton.gameObject.SetActive(false);
        }

        // === 버튼 핸들러 ===

        private void OnAcceptClicked()
        {
            if (string.IsNullOrEmpty(selectedQuestId) || QuestManager.Instance == null) return;
            QuestManager.Instance.AcceptQuestServerRpc(selectedQuestId);
        }

        private void OnAbandonClicked()
        {
            if (string.IsNullOrEmpty(selectedQuestId) || QuestManager.Instance == null) return;
            QuestManager.Instance.AbandonQuestServerRpc(selectedQuestId);
        }

        private void OnClaimClicked()
        {
            if (string.IsNullOrEmpty(selectedQuestId) || QuestManager.Instance == null) return;
            QuestManager.Instance.ClaimRewardServerRpc(selectedQuestId);
        }

        // === 이벤트 핸들러 ===

        private void OnQuestStatusChanged(string questId, QuestStatus status)
        {
            RefreshTracker();
            if (questListPanel != null && questListPanel.activeSelf)
                RefreshQuestList();
        }

        private void OnQuestProgressUpdated(string questId, int objIndex, int current, int required)
        {
            RefreshTracker();
            if (selectedQuestId == questId)
                SelectQuest(questId);
        }

        private void OnQuestCompleted(QuestData questData)
        {
            Debug.Log($"퀘스트 완료! {questData.QuestName}");
            if (CombatLogUI.Instance != null)
                CombatLogUI.Instance.LogSystem($"퀘스트 완료: {questData.QuestName}");
        }

        // === UI 생성 ===

        private GameObject CreateTrackerEntry(QuestData data, QuestProgress progress)
        {
            var obj = new GameObject("TrackerEntry");
            obj.transform.SetParent(trackerContent, false);

            var text = obj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 11;
            text.supportRichText = true;
            text.color = Color.white;

            string statusIcon = progress.status == QuestStatus.Completed ? "[!]" : "";
            string line = $"<color=#FFD700>{statusIcon}{data.QuestName}</color>\n";

            for (int i = 0; i < data.Objectives.Length; i++)
            {
                var obj2 = data.Objectives[i];
                int cur = i < progress.currentCounts.Length ? progress.currentCounts[i] : 0;
                bool done = cur >= obj2.requiredCount;
                string c = done ? "#44FF44" : "#AAAAAA";
                line += $"  <color={c}>{obj2.targetName}: {cur}/{obj2.requiredCount}</color>\n";
            }
            text.text = line;

            var le = obj.AddComponent<LayoutElement>();
            le.preferredHeight = 14 + data.Objectives.Length * 13;

            return obj;
        }

        private GameObject CreateListEntry(QuestData data, QuestProgress progress)
        {
            var obj = new GameObject("QuestEntry");
            obj.transform.SetParent(questListContent, false);

            var bg = obj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);

            var btn = obj.AddComponent<Button>();
            string qId = data.QuestId;
            btn.onClick.AddListener(() => SelectQuest(qId));

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            var text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 13;
            text.supportRichText = true;
            text.color = data.GetDifficultyColor();

            string statusStr = progress.status == QuestStatus.Completed ? " [완료!]" : "";
            float progressRatio = progress.GetProgressRatio(data);
            text.text = $"{data.QuestName}{statusStr} ({progressRatio:P0})";

            var trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.05f, 0f);
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            var le = obj.AddComponent<LayoutElement>();
            le.preferredHeight = 28;

            return obj;
        }

        private void CreateTrackerUI()
        {
            trackerPanel = new GameObject("QuestTracker");
            trackerPanel.transform.SetParent(transform, false);

            var rt = trackerPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.75f, 0.4f);
            rt.anchorMax = new Vector2(0.98f, 0.85f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = trackerPanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.3f);

            // 제목
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(trackerPanel.transform, false);
            var titleText = titleObj.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 13;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(1f, 0.84f, 0f);
            titleText.text = "퀘스트";
            titleText.alignment = TextAnchor.UpperCenter;
            var titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.92f);
            titleRt.anchorMax = Vector2.one;
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;

            // 컨텐츠
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(trackerPanel.transform, false);
            trackerContent = contentObj.transform;
            var crt = contentObj.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.02f, 0f);
            crt.anchorMax = new Vector2(0.98f, 0.9f);
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;

            var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperLeft;
        }

        private void CreateQuestListUI()
        {
            questListPanel = new GameObject("QuestListPanel");
            questListPanel.transform.SetParent(transform, false);

            var rt = questListPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.15f, 0.1f);
            rt.anchorMax = new Vector2(0.85f, 0.9f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = questListPanel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

            // 제목
            CreateLabel(questListPanel.transform, "Title", "퀘스트 목록 (J)",
                new Vector2(0f, 0.93f), new Vector2(1f, 1f), 18, FontStyle.Bold, new Color(1f, 0.84f, 0f));

            // 왼쪽: 퀘스트 목록
            var listObj = new GameObject("QuestList");
            listObj.transform.SetParent(questListPanel.transform, false);
            var listRt = listObj.AddComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0.02f, 0.08f);
            listRt.anchorMax = new Vector2(0.38f, 0.92f);
            listRt.offsetMin = Vector2.zero;
            listRt.offsetMax = Vector2.zero;

            var listBg = listObj.AddComponent<Image>();
            listBg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(listObj.transform, false);
            questListContent = contentObj.transform;
            var crt = contentObj.AddComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;

            var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 2;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(4, 4, 4, 4);

            // 오른쪽: 상세 정보
            questDetailNameText = CreateLabel(questListPanel.transform, "DetailName", "",
                new Vector2(0.4f, 0.85f), new Vector2(0.98f, 0.92f), 16, FontStyle.Bold, Color.white);

            questDetailDescText = CreateLabel(questListPanel.transform, "DetailDesc", "",
                new Vector2(0.4f, 0.7f), new Vector2(0.98f, 0.85f), 12, FontStyle.Normal, Color.gray);

            questDetailObjectivesText = CreateLabel(questListPanel.transform, "DetailObj", "",
                new Vector2(0.4f, 0.4f), new Vector2(0.98f, 0.7f), 12, FontStyle.Normal, Color.white);

            questDetailRewardText = CreateLabel(questListPanel.transform, "DetailReward", "",
                new Vector2(0.4f, 0.2f), new Vector2(0.98f, 0.4f), 12, FontStyle.Normal, new Color(1f, 0.84f, 0f));

            // 버튼들
            acceptButton = CreateButton(questListPanel.transform, "AcceptBtn", "수락",
                new Vector2(0.42f, 0.09f), new Vector2(0.56f, 0.17f), new Color(0.2f, 0.5f, 0.2f));
            abandonButton = CreateButton(questListPanel.transform, "AbandonBtn", "포기",
                new Vector2(0.58f, 0.09f), new Vector2(0.72f, 0.17f), new Color(0.5f, 0.2f, 0.2f));
            claimButton = CreateButton(questListPanel.transform, "ClaimBtn", "보상 수령",
                new Vector2(0.74f, 0.09f), new Vector2(0.92f, 0.17f), new Color(0.5f, 0.4f, 0.1f));
            closeButton = CreateButton(questListPanel.transform, "CloseBtn", "X",
                new Vector2(0.94f, 0.93f), new Vector2(0.99f, 0.99f), new Color(0.5f, 0.2f, 0.2f));

            if (closeButton != null)
                closeButton.onClick.AddListener(() => questListPanel.SetActive(false));
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
            var r = obj.GetComponent<RectTransform>();
            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
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
            var r = obj.GetComponent<RectTransform>();
            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            var t = textObj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 13;
            t.color = Color.white;
            t.text = label;
            t.alignment = TextAnchor.MiddleCenter;
            var tr = textObj.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;

            return btn;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestStatusChanged -= OnQuestStatusChanged;
                QuestManager.Instance.OnQuestProgressUpdated -= OnQuestProgressUpdated;
                QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;
            }
        }
    }
}
