using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 시스템 알림 매니저
    /// 화면 중앙 상단에 시스템 메시지 표시 (레벨업, 퀘스트, 아이템 등)
    /// </summary>
    public class NotificationManager : MonoBehaviour
    {
        public static NotificationManager Instance { get; private set; }

        [Header("설정")]
        [SerializeField] private float displayDuration = 3f;
        [SerializeField] private float fadeOutDuration = 1f;
        [SerializeField] private float slideDistance = 30f;
        [SerializeField] private int maxVisibleNotifications = 5;

        // UI 요소
        private Canvas notificationCanvas;
        private RectTransform containerRect;
        private List<NotificationEntry> activeNotifications = new List<NotificationEntry>();
        private Queue<PendingNotification> pendingQueue = new Queue<PendingNotification>();

        // 알림 타입별 색상
        private static readonly Dictionary<NotificationType, Color> typeColors = new Dictionary<NotificationType, Color>
        {
            { NotificationType.System, new Color(0.8f, 0.8f, 0.8f) },
            { NotificationType.LevelUp, new Color(1f, 0.84f, 0f) },
            { NotificationType.QuestComplete, new Color(0.27f, 1f, 0.27f) },
            { NotificationType.QuestAccept, new Color(0.6f, 0.8f, 1f) },
            { NotificationType.ItemAcquire, new Color(1f, 0.65f, 0f) },
            { NotificationType.ItemRare, new Color(0.53f, 0.33f, 1f) },
            { NotificationType.Achievement, new Color(1f, 0.92f, 0.35f) },
            { NotificationType.Warning, new Color(1f, 0.4f, 0.4f) },
            { NotificationType.PartyMessage, new Color(0f, 1f, 1f) },
            { NotificationType.SkillLearned, new Color(0.5f, 1f, 0.5f) }
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            CreateUI();
        }

        private void Update()
        {
            // 활성 알림 업데이트
            for (int i = activeNotifications.Count - 1; i >= 0; i--)
            {
                var entry = activeNotifications[i];
                entry.elapsed += Time.deltaTime;

                if (entry.elapsed >= displayDuration + fadeOutDuration)
                {
                    // 완전히 사라짐
                    Destroy(entry.gameObject);
                    activeNotifications.RemoveAt(i);
                }
                else if (entry.elapsed >= displayDuration)
                {
                    // 페이드아웃
                    float fadeProgress = (entry.elapsed - displayDuration) / fadeOutDuration;
                    float alpha = 1f - fadeProgress;
                    SetAlpha(entry, alpha);

                    // 위로 슬라이드
                    float slideY = fadeProgress * slideDistance;
                    entry.rectTransform.anchoredPosition = entry.originalPosition + new Vector2(0, slideY);
                }
            }

            // 대기 큐에서 표시
            while (pendingQueue.Count > 0 && activeNotifications.Count < maxVisibleNotifications)
            {
                var pending = pendingQueue.Dequeue();
                ShowNotificationImmediate(pending.message, pending.type);
            }
        }

        /// <summary>
        /// 알림 표시
        /// </summary>
        public void ShowNotification(string message, NotificationType type = NotificationType.System)
        {
            if (activeNotifications.Count >= maxVisibleNotifications)
            {
                pendingQueue.Enqueue(new PendingNotification { message = message, type = type });
                return;
            }

            ShowNotificationImmediate(message, type);
        }

        /// <summary>
        /// 즉시 알림 표시
        /// </summary>
        private void ShowNotificationImmediate(string message, NotificationType type)
        {
            // 기존 알림들 아래로 이동
            float offsetY = -45f;
            foreach (var existing in activeNotifications)
            {
                existing.originalPosition += new Vector2(0, offsetY);
                existing.rectTransform.anchoredPosition = existing.originalPosition;
            }

            // 새 알림 생성
            var entryObj = new GameObject("Notification");
            entryObj.transform.SetParent(containerRect, false);

            // 배경
            var bg = entryObj.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.7f);

            var rt = entryObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(500, 40);
            rt.anchoredPosition = new Vector2(0, -10);

            // 텍스트
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(entryObj.transform, false);
            var text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = message;

            Color textColor = typeColors.ContainsKey(type) ? typeColors[type] : Color.white;
            text.color = textColor;

            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10, 0);
            textRt.offsetMax = new Vector2(-10, 0);

            // 타입 아이콘 (텍스트 접두사)
            string prefix = GetTypePrefix(type);
            if (!string.IsNullOrEmpty(prefix))
            {
                text.text = $"{prefix} {message}";
            }

            var entry = new NotificationEntry
            {
                gameObject = entryObj,
                rectTransform = rt,
                text = text,
                background = bg,
                originalPosition = rt.anchoredPosition,
                elapsed = 0f,
                type = type
            };

            activeNotifications.Insert(0, entry);
        }

        /// <summary>
        /// 편의 메서드: 레벨업 알림
        /// </summary>
        public void NotifyLevelUp(int level)
        {
            ShowNotification($"레벨 업! Lv.{level} 달성!", NotificationType.LevelUp);
        }

        /// <summary>
        /// 편의 메서드: 퀘스트 완료
        /// </summary>
        public void NotifyQuestComplete(string questName)
        {
            ShowNotification($"퀘스트 완료: {questName}", NotificationType.QuestComplete);
        }

        /// <summary>
        /// 편의 메서드: 퀘스트 수락
        /// </summary>
        public void NotifyQuestAccepted(string questName)
        {
            ShowNotification($"퀘스트 수락: {questName}", NotificationType.QuestAccept);
        }

        /// <summary>
        /// 편의 메서드: 아이템 획득
        /// </summary>
        public void NotifyItemAcquired(string itemName, int quantity, ItemGrade grade = ItemGrade.Common)
        {
            var type = grade >= ItemGrade.Rare ? NotificationType.ItemRare : NotificationType.ItemAcquire;
            string qtyStr = quantity > 1 ? $" x{quantity}" : "";
            ShowNotification($"{itemName}{qtyStr} 획득!", type);
        }

        /// <summary>
        /// 편의 메서드: 스킬 학습
        /// </summary>
        public void NotifySkillLearned(string skillName)
        {
            ShowNotification($"새 스킬 습득: {skillName}", NotificationType.SkillLearned);
        }

        /// <summary>
        /// 편의 메서드: 경고
        /// </summary>
        public void NotifyWarning(string message)
        {
            ShowNotification(message, NotificationType.Warning);
        }

        private string GetTypePrefix(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.LevelUp: return "[LEVEL UP]";
                case NotificationType.QuestComplete: return "[QUEST]";
                case NotificationType.QuestAccept: return "[QUEST]";
                case NotificationType.ItemAcquire: return "[ITEM]";
                case NotificationType.ItemRare: return "[RARE]";
                case NotificationType.Achievement: return "[ACHIEVE]";
                case NotificationType.Warning: return "[!]";
                case NotificationType.PartyMessage: return "[PARTY]";
                case NotificationType.SkillLearned: return "[SKILL]";
                default: return "";
            }
        }

        private void SetAlpha(NotificationEntry entry, float alpha)
        {
            if (entry.text != null)
            {
                var c = entry.text.color;
                c.a = alpha;
                entry.text.color = c;
            }
            if (entry.background != null)
            {
                var c = entry.background.color;
                c.a = 0.7f * alpha;
                entry.background.color = c;
            }
        }

        private void CreateUI()
        {
            // Canvas
            var canvasObj = new GameObject("NotificationCanvas");
            canvasObj.transform.SetParent(transform, false);
            notificationCanvas = canvasObj.AddComponent<Canvas>();
            notificationCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            notificationCanvas.sortingOrder = 200;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            // Container
            var containerObj = new GameObject("NotificationContainer");
            containerObj.transform.SetParent(canvasObj.transform, false);
            containerRect = containerObj.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // 내부 클래스
        private class NotificationEntry
        {
            public GameObject gameObject;
            public RectTransform rectTransform;
            public Text text;
            public Image background;
            public Vector2 originalPosition;
            public float elapsed;
            public NotificationType type;
        }

        private struct PendingNotification
        {
            public string message;
            public NotificationType type;
        }
    }

    /// <summary>
    /// 알림 타입
    /// </summary>
    public enum NotificationType
    {
        System,
        LevelUp,
        QuestComplete,
        QuestAccept,
        ItemAcquire,
        ItemRare,
        Achievement,
        Warning,
        PartyMessage,
        SkillLearned
    }
}
