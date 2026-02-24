using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 우편함 UI - N키 토글
    /// 수신함, 우편 읽기, 첨부물 수령, 우편 쓰기
    /// </summary>
    public class MailUI : MonoBehaviour
    {
        // 자동 생성 UI 참조
        private GameObject mainPanel;
        private GameObject mailListPanel;
        private GameObject mailDetailPanel;
        private GameObject composePanel;

        // 메일 리스트
        private Transform mailListContent;
        private Text unreadCountText;
        private Button composeButton;
        private Button claimAllButton;
        private Button deleteReadButton;
        private Button closeButton;

        // 메일 상세
        private Text detailSender;
        private Text detailSubject;
        private Text detailBody;
        private Text detailAttachment;
        private Button claimButton;
        private Button deleteButton;
        private Button backButton;

        // 우편 작성
        private InputField recipientInput;
        private InputField subjectInput;
        private InputField bodyInput;
        private InputField goldInput;
        private Button sendButton;
        private Button cancelButton;

        private bool isInitialized;
        private int selectedMailId = -1;
        private List<GameObject> mailEntries = new List<GameObject>();

        private void Start()
        {
            CreateUI();
            isInitialized = true;
            mainPanel.SetActive(false);

            if (MailSystem.Instance != null)
            {
                MailSystem.Instance.OnMailboxUpdated += RefreshMailList;
                MailSystem.Instance.OnMailReceived += OnNewMail;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.N) && !IsInputFieldFocused())
            {
                ToggleUI();
            }
        }

        private void ToggleUI()
        {
            if (!isInitialized) return;
            bool show = !mainPanel.activeSelf;
            mainPanel.SetActive(show);

            if (show)
            {
                ShowMailList();
                // 동기화 요청
                if (MailSystem.Instance != null)
                    MailSystem.Instance.RequestMailboxSyncServerRpc();
            }
        }

        #region UI 생성

        private void CreateUI()
        {
            // Canvas
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            // 메인 패널
            mainPanel = CreatePanel(transform, "MailPanel", new Vector2(600, 500));

            // 타이틀
            CreateText(mainPanel.transform, "Title", "우편함", 22, TextAnchor.MiddleCenter,
                new Vector2(0, 220), new Vector2(400, 40));

            // 읽지 않은 수
            unreadCountText = CreateText(mainPanel.transform, "UnreadCount", "", 14, TextAnchor.MiddleRight,
                new Vector2(200, 220), new Vector2(150, 30)).GetComponent<Text>();

            // 닫기 버튼
            closeButton = CreateButton(mainPanel.transform, "CloseBtn", "X",
                new Vector2(270, 220), new Vector2(40, 40));
            closeButton.onClick.AddListener(() => mainPanel.SetActive(false));

            // 메일 리스트 패널
            mailListPanel = new GameObject("MailListPanel");
            mailListPanel.transform.SetParent(mainPanel.transform, false);
            var listRT = mailListPanel.AddComponent<RectTransform>();
            listRT.anchoredPosition = new Vector2(0, -20);
            listRT.sizeDelta = new Vector2(560, 350);

            // 스크롤뷰
            var scrollObj = CreateScrollView(mailListPanel.transform, "MailScroll",
                new Vector2(0, 30), new Vector2(560, 300));
            mailListContent = scrollObj.transform.Find("Viewport/Content");

            // 하단 버튼
            composeButton = CreateButton(mailListPanel.transform, "ComposeBtn", "우편 쓰기",
                new Vector2(-200, -170), new Vector2(120, 35));
            composeButton.onClick.AddListener(ShowCompose);

            claimAllButton = CreateButton(mailListPanel.transform, "ClaimAllBtn", "전체 수령",
                new Vector2(-60, -170), new Vector2(120, 35));
            claimAllButton.onClick.AddListener(() =>
            {
                if (MailSystem.Instance != null)
                    MailSystem.Instance.ClaimAllAttachmentsServerRpc();
            });

            deleteReadButton = CreateButton(mailListPanel.transform, "DeleteReadBtn", "읽은것 삭제",
                new Vector2(80, -170), new Vector2(120, 35));
            deleteReadButton.onClick.AddListener(() =>
            {
                if (MailSystem.Instance != null)
                    MailSystem.Instance.DeleteAllReadMailsServerRpc();
            });

            // 메일 상세 패널
            mailDetailPanel = new GameObject("MailDetailPanel");
            mailDetailPanel.transform.SetParent(mainPanel.transform, false);
            var detailRT = mailDetailPanel.AddComponent<RectTransform>();
            detailRT.anchoredPosition = new Vector2(0, -20);
            detailRT.sizeDelta = new Vector2(560, 350);

            detailSender = CreateText(mailDetailPanel.transform, "Sender", "", 14, TextAnchor.MiddleLeft,
                new Vector2(0, 150), new Vector2(500, 25)).GetComponent<Text>();
            detailSubject = CreateText(mailDetailPanel.transform, "Subject", "", 18, TextAnchor.MiddleLeft,
                new Vector2(0, 120), new Vector2(500, 30)).GetComponent<Text>();
            detailBody = CreateText(mailDetailPanel.transform, "Body", "", 14, TextAnchor.UpperLeft,
                new Vector2(0, 30), new Vector2(500, 140)).GetComponent<Text>();
            detailAttachment = CreateText(mailDetailPanel.transform, "Attachment", "", 14, TextAnchor.MiddleLeft,
                new Vector2(0, -60), new Vector2(500, 25)).GetComponent<Text>();

            backButton = CreateButton(mailDetailPanel.transform, "BackBtn", "뒤로",
                new Vector2(-200, -170), new Vector2(100, 35));
            backButton.onClick.AddListener(ShowMailList);

            claimButton = CreateButton(mailDetailPanel.transform, "ClaimBtn", "수령",
                new Vector2(-60, -170), new Vector2(100, 35));
            claimButton.onClick.AddListener(() =>
            {
                if (MailSystem.Instance != null && selectedMailId >= 0)
                    MailSystem.Instance.ClaimAttachmentServerRpc(selectedMailId);
            });

            deleteButton = CreateButton(mailDetailPanel.transform, "DeleteBtn", "삭제",
                new Vector2(80, -170), new Vector2(100, 35));
            deleteButton.onClick.AddListener(() =>
            {
                if (MailSystem.Instance != null && selectedMailId >= 0)
                {
                    MailSystem.Instance.DeleteMailServerRpc(selectedMailId);
                    ShowMailList();
                }
            });

            // 우편 작성 패널
            composePanel = new GameObject("ComposePanel");
            composePanel.transform.SetParent(mainPanel.transform, false);
            var composeRT = composePanel.AddComponent<RectTransform>();
            composeRT.anchoredPosition = new Vector2(0, -20);
            composeRT.sizeDelta = new Vector2(560, 350);

            CreateText(composePanel.transform, "LabelTo", "받는 사람:", 14, TextAnchor.MiddleLeft,
                new Vector2(-200, 150), new Vector2(100, 25));
            recipientInput = CreateInputField(composePanel.transform, "RecipientInput",
                new Vector2(50, 150), new Vector2(300, 30));

            CreateText(composePanel.transform, "LabelSubject", "제목:", 14, TextAnchor.MiddleLeft,
                new Vector2(-200, 110), new Vector2(100, 25));
            subjectInput = CreateInputField(composePanel.transform, "SubjectInput",
                new Vector2(50, 110), new Vector2(300, 30));

            CreateText(composePanel.transform, "LabelBody", "내용:", 14, TextAnchor.MiddleLeft,
                new Vector2(-200, 50), new Vector2(100, 25));
            bodyInput = CreateInputField(composePanel.transform, "BodyInput",
                new Vector2(50, 40), new Vector2(300, 80));

            CreateText(composePanel.transform, "LabelGold", "골드 첨부:", 14, TextAnchor.MiddleLeft,
                new Vector2(-200, -20), new Vector2(100, 25));
            goldInput = CreateInputField(composePanel.transform, "GoldInput",
                new Vector2(50, -20), new Vector2(300, 30));
            goldInput.contentType = InputField.ContentType.IntegerNumber;

            sendButton = CreateButton(composePanel.transform, "SendBtn", "보내기",
                new Vector2(-80, -170), new Vector2(120, 35));
            sendButton.onClick.AddListener(SendMail);

            cancelButton = CreateButton(composePanel.transform, "CancelBtn", "취소",
                new Vector2(80, -170), new Vector2(120, 35));
            cancelButton.onClick.AddListener(ShowMailList);
        }

        #endregion

        #region UI 전환

        private void ShowMailList()
        {
            mailListPanel.SetActive(true);
            mailDetailPanel.SetActive(false);
            composePanel.SetActive(false);
            RefreshMailList();
        }

        private void ShowMailDetail(int mailId)
        {
            mailListPanel.SetActive(false);
            mailDetailPanel.SetActive(true);
            composePanel.SetActive(false);
            selectedMailId = mailId;

            if (MailSystem.Instance == null) return;

            // 읽음 처리
            MailSystem.Instance.ReadMailServerRpc(mailId);

            // 상세 표시
            var mailbox = MailSystem.Instance.LocalMailbox;
            MailData mail = default;
            bool found = false;
            foreach (var m in mailbox)
            {
                if (m.mailId == mailId) { mail = m; found = true; break; }
            }
            if (!found) { ShowMailList(); return; }

            detailSender.text = $"보낸 사람: <color=#FFD700>{mail.senderName}</color>";
            detailSubject.text = mail.subject;
            detailBody.text = mail.body;

            // 첨부물 표시
            if (mail.hasAttachment)
            {
                string attachStr = "첨부: ";
                if (mail.attachment.gold > 0)
                    attachStr += $"<color=#FFD700>{mail.attachment.gold}G</color> ";
                string itemIdStr = mail.attachment.itemId;
                if (!string.IsNullOrEmpty(itemIdStr) && mail.attachment.quantity > 0)
                    attachStr += $"아이템 ×{mail.attachment.quantity}";

                if (mail.attachmentClaimed)
                    attachStr += " <color=#888888>(수령 완료)</color>";

                detailAttachment.text = attachStr;
                claimButton.gameObject.SetActive(!mail.attachmentClaimed);
            }
            else
            {
                detailAttachment.text = "";
                claimButton.gameObject.SetActive(false);
            }
        }

        private void ShowCompose()
        {
            mailListPanel.SetActive(false);
            mailDetailPanel.SetActive(false);
            composePanel.SetActive(true);
            recipientInput.text = "";
            subjectInput.text = "";
            bodyInput.text = "";
            goldInput.text = "0";
        }

        #endregion

        #region 기능

        private void RefreshMailList()
        {
            // 기존 엔트리 제거
            foreach (var entry in mailEntries)
                if (entry != null) Destroy(entry);
            mailEntries.Clear();

            if (MailSystem.Instance == null) return;

            var mailbox = MailSystem.Instance.LocalMailbox;

            // 읽지 않은 수 표시
            int unread = MailSystem.Instance.UnreadCount;
            unreadCountText.text = unread > 0 ? $"<color=#FF4444>안 읽은 편지: {unread}</color>" : "";

            // 최신 우편부터 표시
            for (int i = mailbox.Count - 1; i >= 0; i--)
            {
                var mail = mailbox[i];
                CreateMailEntry(mail);
            }
        }

        private void CreateMailEntry(MailData mail)
        {
            var entry = new GameObject("MailEntry");
            entry.transform.SetParent(mailListContent, false);
            var entryRT = entry.AddComponent<RectTransform>();
            entryRT.sizeDelta = new Vector2(540, 40);

            var bg = entry.AddComponent<Image>();
            bg.color = mail.isRead ? new Color(0.15f, 0.15f, 0.15f, 0.9f) : new Color(0.2f, 0.15f, 0.1f, 0.9f);

            var btn = entry.AddComponent<Button>();
            int mailId = mail.mailId;
            btn.onClick.AddListener(() => ShowMailDetail(mailId));

            // 타입 아이콘
            string typeStr = mail.mailType switch
            {
                MailType.SystemReward => "<color=#FFD700>[보상]</color>",
                MailType.AuctionResult => "<color=#00BFFF>[경매]</color>",
                MailType.GuildNotice => "<color=#00FF88>[길드]</color>",
                MailType.QuestReward => "<color=#FFAA00>[퀘스트]</color>",
                MailType.AdminNotice => "<color=#FF4444>[공지]</color>",
                _ => "<color=#CCCCCC>[편지]</color>"
            };

            // 보낸 사람
            string sender = mail.senderName;
            if (sender.Length > 8) sender = sender.Substring(0, 8) + "..";

            // 제목
            string subject = mail.subject;
            if (subject.Length > 20) subject = subject.Substring(0, 20) + "..";

            // 읽지 않은 표시
            string unreadMark = mail.isRead ? "" : "<color=#FF4444>●</color> ";

            // 첨부물 표시
            string attachMark = (mail.hasAttachment && !mail.attachmentClaimed) ? " <color=#FFD700>📎</color>" : "";

            string text = $"{unreadMark}{typeStr} {sender} - {subject}{attachMark}";

            var textComp = CreateText(entry.transform, "Text", text, 13, TextAnchor.MiddleLeft,
                Vector2.zero, new Vector2(520, 35));

            mailEntries.Add(entry);
        }

        private void SendMail()
        {
            if (MailSystem.Instance == null) return;

            string recipient = recipientInput.text.Trim();
            string subject = subjectInput.text.Trim();
            string body = bodyInput.text.Trim();

            if (string.IsNullOrEmpty(recipient))
            {
                var notif = NotificationManager.Instance;
                if (notif != null) notif.ShowNotification("받는 사람을 입력해주세요.", NotificationType.Warning);
                return;
            }

            if (string.IsNullOrEmpty(subject))
                subject = "(제목 없음)";

            int gold = 0;
            if (!string.IsNullOrEmpty(goldInput.text))
                int.TryParse(goldInput.text, out gold);

            var attachment = new MailAttachment
            {
                gold = gold,
                itemId = default,
                quantity = 0,
                enhanceLevel = 0
            };

            bool hasAttachment = gold > 0;

            if (MailSystem.Instance == null) return;
            MailSystem.Instance.SendMailServerRpc(
                recipient,
                subject,
                body,
                attachment,
                hasAttachment
            );

            ShowMailList();
        }

        private void OnNewMail(MailData mail)
        {
            // 열려있으면 새로고침
            if (mainPanel.activeSelf && mailListPanel.activeSelf)
                RefreshMailList();
        }

        #endregion

        #region UI 헬퍼

        private bool IsInputFieldFocused()
        {
            var selected = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
            if (selected == null) return false;
            return selected.GetComponent<InputField>() != null;
        }

        private GameObject CreatePanel(Transform parent, string name, Vector2 size)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
            var img = panel.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);
            return panel;
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

        private InputField CreateInputField(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var img = obj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.25f, 1f);

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(obj.transform, false);
            var txtRT = txtObj.AddComponent<RectTransform>();
            txtRT.anchorMin = new Vector2(0, 0);
            txtRT.anchorMax = new Vector2(1, 1);
            txtRT.offsetMin = new Vector2(5, 2);
            txtRT.offsetMax = new Vector2(-5, -2);
            var txt = txtObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 14;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = Color.white;
            txt.supportRichText = false;

            var input = obj.AddComponent<InputField>();
            input.textComponent = txt;
            input.targetGraphic = img;

            return input;
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
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
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

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.viewport = vpRT;
            scroll.content = contentRT;
            scroll.horizontal = false;
            scroll.vertical = true;

            return scrollObj;
        }

        #endregion

        private void OnDestroy()
        {
            if (MailSystem.Instance != null)
            {
                MailSystem.Instance.OnMailboxUpdated -= RefreshMailList;
                MailSystem.Instance.OnMailReceived -= OnNewMail;
            }
        }
    }
}
