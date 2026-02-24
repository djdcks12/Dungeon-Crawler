using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 길드 UI - G키 토글
    /// 길드 정보, 멤버 목록, 길드 생성/가입, 공지 수정
    /// </summary>
    public class GuildUI : MonoBehaviour
    {
        private GameObject mainPanel;
        private GameObject createPanel;
        private GameObject invitePanel;

        // 길드 정보 표시
        private Text guildNameText;
        private Text guildLevelText;
        private Text guildExpText;
        private Text guildGoldText;
        private Text noticeText;
        private Text memberCountText;
        private Text buffText;

        // 멤버 목록
        private Transform memberListContent;
        private List<GameObject> memberEntries = new List<GameObject>();

        // 생성 패널
        private InputField createNameInput;

        // 초대 알림
        private int pendingInviteGuildId;
        private string pendingInviteGuildName;

        private bool isVisible;

        private void Start()
        {
            BuildUI();

            var guild = GuildSystem.Instance;
            if (guild != null)
            {
                guild.OnGuildJoined += OnGuildJoined;
                guild.OnGuildLeft += OnGuildLeft;
                guild.OnGuildInfoUpdated += OnGuildInfoUpdated;
                guild.OnMembersUpdated += OnMembersUpdated;
                guild.OnGuildInviteReceived += OnGuildInviteReceived;
                guild.OnGuildMessage += OnGuildMessage;
            }

            mainPanel.SetActive(false);
            createPanel.SetActive(false);
            invitePanel.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.G))
                ToggleUI();
        }

        public void ToggleUI()
        {
            isVisible = !isVisible;
            mainPanel.SetActive(isVisible);

            if (isVisible)
                RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            var guild = GuildSystem.Instance;
            if (guild == null) return;

            if (guild.IsInGuild)
            {
                ShowGuildInfo(guild.LocalGuildInfo);
                UpdateMemberList(new List<GuildMemberInfo>(guild.LocalMembers));
                createPanel.SetActive(false);
            }
            else
            {
                guildNameText.text = "길드 미가입";
                guildLevelText.text = "";
                guildExpText.text = "";
                guildGoldText.text = "";
                noticeText.text = "길드에 가입하거나 새 길드를 만드세요.";
                memberCountText.text = "";
                buffText.text = "";
                ClearMemberList();
            }
        }

        private void ShowGuildInfo(GuildInfo info)
        {
            guildNameText.text = info.guildName.ToString();
            guildLevelText.text = $"Lv.{info.guildLevel}";
            guildExpText.text = $"EXP: {info.guildExp}";
            guildGoldText.text = $"금고: {info.guildGold}G";
            noticeText.text = info.notice.ToString();
            memberCountText.text = $"멤버: {info.memberCount}/{info.maxMembers}";
            buffText.text = $"버프: EXP+{info.expBonusPercent:F1}% Gold+{info.goldBonusPercent:F1}% Drop+{info.dropBonusPercent:F1}%";
        }

        private void UpdateMemberList(List<GuildMemberInfo> members)
        {
            ClearMemberList();

            members.Sort((a, b) => ((int)b.rank).CompareTo((int)a.rank));

            foreach (var member in members)
            {
                var entry = CreateMemberEntry(member);
                memberEntries.Add(entry);
            }
        }

        private void ClearMemberList()
        {
            foreach (var entry in memberEntries)
                Destroy(entry);
            memberEntries.Clear();
        }

        private GameObject CreateMemberEntry(GuildMemberInfo member)
        {
            var go = new GameObject($"Member_{member.playerName}");
            go.transform.SetParent(memberListContent, false);

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.padding = new RectOffset(5, 5, 2, 2);

            var goRect = go.AddComponent<RectTransform>();
            goRect.sizeDelta = new Vector2(0, 30);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 30;

            // 직급 색상
            string rankStr = "";
            Color rankColor = Color.white;
            switch (member.rank)
            {
                case GuildRank.Master: rankStr = "[길마]"; rankColor = new Color(1f, 0.84f, 0f); break;
                case GuildRank.ViceMaster: rankStr = "[부마]"; rankColor = new Color(0.6f, 0.8f, 1f); break;
                case GuildRank.Elite: rankStr = "[엘리트]"; rankColor = new Color(0.6f, 1f, 0.6f); break;
                default: rankStr = "[일반]"; break;
            }

            // 직급
            CreateText(go.transform, rankStr, 70, rankColor);
            // 이름
            CreateText(go.transform, member.playerName.ToString(), 120, Color.white);
            // 레벨
            CreateText(go.transform, $"Lv.{member.playerLevel}", 60, Color.gray);
            // 기여도
            CreateText(go.transform, $"기여:{member.contributionPoints}", 80, Color.yellow);

            return go;
        }

        #region Event Handlers

        private void OnGuildJoined(GuildInfo info)
        {
            if (isVisible) RefreshDisplay();
        }

        private void OnGuildLeft()
        {
            if (isVisible) RefreshDisplay();
        }

        private void OnGuildInfoUpdated(GuildInfo info)
        {
            if (isVisible) ShowGuildInfo(info);
        }

        private void OnMembersUpdated(List<GuildMemberInfo> members)
        {
            if (isVisible) UpdateMemberList(members);
        }

        private void OnGuildInviteReceived(int guildId, string guildName)
        {
            pendingInviteGuildId = guildId;
            pendingInviteGuildName = guildName;
            invitePanel.SetActive(true);

            var inviteText = invitePanel.GetComponentInChildren<Text>();
            if (inviteText != null)
                inviteText.text = $"'{guildName}' 길드에서 초대가 도착했습니다.\n수락하시겠습니까?";
        }

        private void OnGuildMessage(string message)
        {
            // 채팅 로그에 길드 메시지 추가 가능
            Debug.Log($"[Guild] {message}");
        }

        #endregion

        #region Button Actions

        private void OnCreateGuild()
        {
            if (createNameInput == null || string.IsNullOrEmpty(createNameInput.text)) return;
            GuildSystem.Instance?.CreateGuildServerRpc(createNameInput.text);
            createPanel.SetActive(false);
        }

        private void OnShowCreatePanel()
        {
            createPanel.SetActive(true);
        }

        private void OnLeaveGuild()
        {
            GuildSystem.Instance?.LeaveGuildServerRpc();
        }

        private void OnDisbandGuild()
        {
            GuildSystem.Instance?.DisbandGuildServerRpc();
        }

        private void OnAcceptInvite()
        {
            GuildSystem.Instance?.AcceptInviteServerRpc(pendingInviteGuildId);
            invitePanel.SetActive(false);
        }

        private void OnDeclineInvite()
        {
            GuildSystem.Instance?.DeclineInviteServerRpc();
            invitePanel.SetActive(false);
        }

        private void OnDonateGold()
        {
            GuildSystem.Instance?.DonateGoldServerRpc(100);
        }

        #endregion

        #region UI Building

        private void BuildUI()
        {
            // 메인 패널
            mainPanel = CreatePanel("GuildPanel", transform, new Vector2(500, 600));

            // 타이틀
            CreateText(mainPanel.transform, "길드", 0, Color.white, 24, TextAnchor.UpperCenter).GetComponent<RectTransform>()
                .anchoredPosition = new Vector2(0, -15);

            // 길드 정보 영역
            float yPos = -50;
            guildNameText = CreateText(mainPanel.transform, "길드 미가입", 0, Color.white, 20, TextAnchor.MiddleLeft).GetComponent<Text>();
            guildNameText.GetComponent<RectTransform>().anchoredPosition = new Vector2(20, yPos);
            guildNameText.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 30);

            guildLevelText = CreateText(mainPanel.transform, "", 0, Color.yellow, 16, TextAnchor.MiddleRight).GetComponent<Text>();
            guildLevelText.GetComponent<RectTransform>().anchoredPosition = new Vector2(-20, yPos);
            guildLevelText.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 30);

            yPos -= 30;
            guildExpText = CreateText(mainPanel.transform, "", 0, Color.cyan, 14, TextAnchor.MiddleLeft).GetComponent<Text>();
            guildExpText.GetComponent<RectTransform>().anchoredPosition = new Vector2(20, yPos);
            guildExpText.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 25);

            guildGoldText = CreateText(mainPanel.transform, "", 0, Color.yellow, 14, TextAnchor.MiddleRight).GetComponent<Text>();
            guildGoldText.GetComponent<RectTransform>().anchoredPosition = new Vector2(-20, yPos);
            guildGoldText.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 25);

            yPos -= 25;
            memberCountText = CreateText(mainPanel.transform, "", 0, Color.gray, 14, TextAnchor.MiddleLeft).GetComponent<Text>();
            memberCountText.GetComponent<RectTransform>().anchoredPosition = new Vector2(20, yPos);
            memberCountText.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 25);

            yPos -= 25;
            buffText = CreateText(mainPanel.transform, "", 0, new Color(0.5f, 1f, 0.5f), 13, TextAnchor.MiddleLeft).GetComponent<Text>();
            buffText.GetComponent<RectTransform>().anchoredPosition = new Vector2(20, yPos);
            buffText.GetComponent<RectTransform>().sizeDelta = new Vector2(450, 25);

            yPos -= 30;
            var noticeBg = CreateImage(mainPanel.transform, new Color(0.15f, 0.15f, 0.2f, 0.8f), new Vector2(460, 50));
            noticeBg.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, yPos - 10);
            noticeText = CreateText(noticeBg.transform, "", 0, Color.white, 12, TextAnchor.MiddleLeft).GetComponent<Text>();
            noticeText.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            noticeText.GetComponent<RectTransform>().anchorMax = Vector2.one;
            noticeText.GetComponent<RectTransform>().offsetMin = new Vector2(10, 2);
            noticeText.GetComponent<RectTransform>().offsetMax = new Vector2(-10, -2);

            // 멤버 목록 (스크롤)
            yPos -= 70;
            var scrollGo = new GameObject("MemberScroll");
            scrollGo.transform.SetParent(mainPanel.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.anchoredPosition = new Vector2(0, yPos - 100);
            scrollRect.sizeDelta = new Vector2(460, 220);

            var scrollView = scrollGo.AddComponent<ScrollRect>();
            var scrollImage = scrollGo.AddComponent<Image>();
            scrollImage.color = new Color(0.1f, 0.1f, 0.15f, 0.6f);
            scrollView.horizontal = false;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);

            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 2;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(5, 5, 5, 5);

            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollView.content = contentRect;
            memberListContent = contentGo.transform;

            var mask = scrollGo.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            // 버튼 영역
            float btnY = -530;
            CreateButton(mainPanel.transform, "길드 생성", new Vector2(-180, btnY), new Vector2(100, 30), OnShowCreatePanel);
            CreateButton(mainPanel.transform, "탈퇴", new Vector2(-60, btnY), new Vector2(80, 30), OnLeaveGuild);
            CreateButton(mainPanel.transform, "해산", new Vector2(40, btnY), new Vector2(80, 30), OnDisbandGuild);
            CreateButton(mainPanel.transform, "기부 100G", new Vector2(150, btnY), new Vector2(100, 30), OnDonateGold);

            // 닫기 버튼
            CreateButton(mainPanel.transform, "X", new Vector2(230, -15), new Vector2(30, 30), () => { isVisible = false; mainPanel.SetActive(false); });

            // 생성 패널
            createPanel = CreatePanel("CreateGuildPanel", transform, new Vector2(300, 150));
            CreateText(createPanel.transform, "길드 생성", 0, Color.white, 18, TextAnchor.UpperCenter)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -15);

            var inputGo = new GameObject("NameInput");
            inputGo.transform.SetParent(createPanel.transform, false);
            var inputRect = inputGo.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.5f, 0.5f);
            inputRect.anchorMax = new Vector2(0.5f, 0.5f);
            inputRect.anchoredPosition = new Vector2(0, -10);
            inputRect.sizeDelta = new Vector2(250, 35);

            var inputImage = inputGo.AddComponent<Image>();
            inputImage.color = new Color(0.2f, 0.2f, 0.25f, 1f);

            createNameInput = inputGo.AddComponent<InputField>();
            createNameInput.characterLimit = 16;

            var inputTextGo = new GameObject("Text");
            inputTextGo.transform.SetParent(inputGo.transform, false);
            var inputTextRect = inputTextGo.AddComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = new Vector2(5, 2);
            inputTextRect.offsetMax = new Vector2(-5, -2);
            var inputText = inputTextGo.AddComponent<Text>();
            inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            inputText.fontSize = 14;
            inputText.color = Color.white;
            inputText.supportRichText = false;
            createNameInput.textComponent = inputText;

            var placeholderGo = new GameObject("Placeholder");
            placeholderGo.transform.SetParent(inputGo.transform, false);
            var placeholderRect = placeholderGo.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(5, 2);
            placeholderRect.offsetMax = new Vector2(-5, -2);
            var placeholder = placeholderGo.AddComponent<Text>();
            placeholder.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholder.fontSize = 14;
            placeholder.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            placeholder.text = "길드 이름 입력...";
            placeholder.fontStyle = FontStyle.Italic;
            createNameInput.placeholder = placeholder;

            CreateButton(createPanel.transform, "생성 (5000G)", new Vector2(-50, -55), new Vector2(120, 30), OnCreateGuild);
            CreateButton(createPanel.transform, "취소", new Vector2(70, -55), new Vector2(80, 30), () => createPanel.SetActive(false));

            // 초대 패널
            invitePanel = CreatePanel("InvitePanel", transform, new Vector2(300, 120));
            CreateText(invitePanel.transform, "길드 초대", 0, Color.white, 16, TextAnchor.UpperCenter)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -15);
            CreateButton(invitePanel.transform, "수락", new Vector2(-60, -40), new Vector2(90, 30), OnAcceptInvite);
            CreateButton(invitePanel.transform, "거절", new Vector2(60, -40), new Vector2(90, 30), OnDeclineInvite);
        }

        private GameObject CreatePanel(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

            return go;
        }

        private GameObject CreateText(Transform parent, string text, float width, Color color, int fontSize = 14, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width > 0 ? width : 200, 25);

            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.color = color;
            t.text = text;
            t.alignment = anchor;

            if (width > 0)
            {
                var le = go.AddComponent<LayoutElement>();
                le.preferredWidth = width;
            }

            return go;
        }

        private GameObject CreateImage(Transform parent, Color color, Vector2 size)
        {
            var go = new GameObject("Image");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private void CreateButton(Transform parent, string text, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Btn_{text}");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.35f, 1f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var t = textGo.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 12;
            t.color = Color.white;
            t.text = text;
            t.alignment = TextAnchor.MiddleCenter;
        }

        #endregion

        private void OnDestroy()
        {
            var guild = GuildSystem.Instance;
            if (guild != null)
            {
                guild.OnGuildJoined -= OnGuildJoined;
                guild.OnGuildLeft -= OnGuildLeft;
                guild.OnGuildInfoUpdated -= OnGuildInfoUpdated;
                guild.OnMembersUpdated -= OnMembersUpdated;
                guild.OnGuildInviteReceived -= OnGuildInviteReceived;
                guild.OnGuildMessage -= OnGuildMessage;
            }
        }
    }
}
