using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 퀘스트 NPC - F키로 상호작용, 머리 위 퀘스트 아이콘 표시
    /// </summary>
    public class QuestNPC : MonoBehaviour
    {
        [Header("NPC 설정")]
        [SerializeField] private string npcName = "퀘스트 게시판";
        [SerializeField] private int minLevel = 1;
        [SerializeField] private int maxLevel = 99;

        [Header("제공 퀘스트")]
        [SerializeField] private string[] questIds;

        [Header("시각 효과")]
        [SerializeField] private GameObject interactionPrompt;

        // 퀘스트 아이콘 UI
        private GameObject questIconObj;
        private Text questIconText;

        private bool playerInRange = false;
        private PlayerController nearbyPlayer;

        private const string ICON_AVAILABLE = "!";     // 수락 가능 퀘스트
        private const string ICON_COMPLETE = "?";      // 완료 가능 퀘스트
        private const string COLOR_AVAILABLE = "#FFD700";
        private const string COLOR_COMPLETE = "#44FF44";

        private void Start()
        {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);

            CreateQuestIcon();
        }

        private void Update()
        {
            UpdateQuestIcon();

            if (!playerInRange || nearbyPlayer == null) return;

            if (Input.GetKeyDown(KeyCode.F))
            {
                OpenQuestPanel();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null && player.IsOwner)
            {
                playerInRange = true;
                nearbyPlayer = player;
                if (interactionPrompt != null)
                    interactionPrompt.SetActive(true);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null && player.IsOwner)
            {
                playerInRange = false;
                nearbyPlayer = null;
                if (interactionPrompt != null)
                    interactionPrompt.SetActive(false);
            }
        }

        /// <summary>
        /// 퀘스트 패널 열기
        /// </summary>
        private void OpenQuestPanel()
        {
            if (QuestUI.Instance == null || QuestManager.Instance == null) return;

            var statsManager = nearbyPlayer.GetComponent<PlayerStatsManager>();
            int playerLevel = statsManager != null ? statsManager.CurrentStats.CurrentLevel : 1;

            if (QuestUI.Instance != null)
                QuestUI.Instance.ShowAvailableQuests(playerLevel);
        }

        /// <summary>
        /// 머리 위 퀘스트 아이콘 생성
        /// </summary>
        private void CreateQuestIcon()
        {
            // 월드 스페이스 Canvas 위에 표시
            var canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("QuestIconCanvas");
                canvasObj.transform.SetParent(transform, false);
                canvasObj.transform.localPosition = new Vector3(0, 1.5f, 0);
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = 100;

                var rt = canvasObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(1f, 1f);
                rt.localScale = new Vector3(0.02f, 0.02f, 0.02f);
            }

            questIconObj = new GameObject("QuestIcon");
            questIconObj.transform.SetParent(canvas.transform, false);

            // 배경
            var bg = questIconObj.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.5f);

            var iconRt = questIconObj.GetComponent<RectTransform>();
            iconRt.sizeDelta = new Vector2(30, 30);
            iconRt.anchoredPosition = Vector2.zero;

            // 텍스트
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(questIconObj.transform, false);
            questIconText = textObj.AddComponent<Text>();
            questIconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            questIconText.fontSize = 24;
            questIconText.fontStyle = FontStyle.Bold;
            questIconText.alignment = TextAnchor.MiddleCenter;
            questIconText.color = Color.yellow;
            questIconText.text = ICON_AVAILABLE;

            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            questIconObj.SetActive(false);
        }

        /// <summary>
        /// 퀘스트 아이콘 상태 업데이트
        /// </summary>
        private void UpdateQuestIcon()
        {
            if (questIconObj == null || questIconText == null) return;
            if (QuestManager.Instance == null)
            {
                questIconObj.SetActive(false);
                return;
            }

            bool hasCompletable = false;
            bool hasAvailable = false;

            // 완료 가능한 퀘스트 확인
            if (QuestManager.Instance == null) return;
            var activeQuests = QuestManager.Instance.GetActiveQuests();
            foreach (var progress in activeQuests)
            {
                if (progress.status == QuestStatus.Completed && IsMyQuest(progress.questId))
                {
                    hasCompletable = true;
                    break;
                }
            }

            // 수락 가능한 퀘스트 확인
            if (!hasCompletable && questIds != null)
            {
                foreach (string qId in questIds)
                {
                    var data = QuestManager.Instance.GetQuestData(qId);
                    if (data == null) continue;

                    var existing = QuestManager.Instance.GetQuestProgress(qId);
                    if (existing == null || (data.IsRepeatable && existing.status == QuestStatus.Rewarded))
                    {
                        hasAvailable = true;
                        break;
                    }
                }
            }

            if (hasCompletable)
            {
                questIconObj.SetActive(true);
                questIconText.text = ICON_COMPLETE;
                questIconText.color = Color.green;
            }
            else if (hasAvailable)
            {
                questIconObj.SetActive(true);
                questIconText.text = ICON_AVAILABLE;
                questIconText.color = Color.yellow;
            }
            else
            {
                questIconObj.SetActive(false);
            }
        }

        /// <summary>
        /// 이 NPC가 해당 퀘스트를 제공하는지 확인
        /// </summary>
        private bool IsMyQuest(string questId)
        {
            if (questIds == null) return false;
            foreach (string qId in questIds)
            {
                if (qId == questId) return true;
            }
            return false;
        }
    }
}
