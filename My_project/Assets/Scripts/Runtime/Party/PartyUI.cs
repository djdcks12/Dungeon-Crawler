using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 파티 UI 시스템
    /// 파티 생성, 참가, 멤버 관리 등의 UI를 담당
    /// </summary>
    public class PartyUI : MonoBehaviour
    {
        [Header("메인 파티 패널")]
        [SerializeField] private GameObject partyPanel;
        [SerializeField] private GameObject createPartyPanel;
        [SerializeField] private GameObject partyListPanel;
        [SerializeField] private GameObject partyMembersPanel;
        
        [Header("파티 생성 UI")]
        [SerializeField] private InputField partyNameInput;
        [SerializeField] private Slider maxMembersSlider;
        [SerializeField] private Text maxMembersText;
        [SerializeField] private Toggle isPublicToggle;
        [SerializeField] private Button createPartyButton;
        [SerializeField] private Button cancelCreateButton;
        
        [Header("파티 정보 UI")]
        [SerializeField] private Text currentPartyNameText;
        [SerializeField] private Text partyStateText;
        [SerializeField] private Text memberCountText;
        [SerializeField] private Button leavePartyButton;
        [SerializeField] private Button disbandPartyButton;
        [SerializeField] private Button toggleReadyButton;
        [SerializeField] private Text readyButtonText;
        
        [Header("멤버 리스트 UI")]
        [SerializeField] private Transform memberListContent;
        [SerializeField] private GameObject memberListItemPrefab;
        
        [Header("파티 검색 UI")]
        [SerializeField] private InputField searchInput;
        [SerializeField] private Dropdown levelFilterDropdown;
        [SerializeField] private Toggle publicOnlyToggle;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Transform partyListContent;
        [SerializeField] private GameObject partyListItemPrefab;
        
        [Header("초대 UI")]
        [SerializeField] private GameObject invitationPopup;
        [SerializeField] private Text invitationText;
        [SerializeField] private Button acceptInviteButton;
        [SerializeField] private Button declineInviteButton;
        
        // 상태 관리
        private PartyManager partyManager;
        private bool isPartyUIActive = false;
        private PartyInvitation? currentInvitation;
        private List<GameObject> memberUIItems = new List<GameObject>();
        private List<GameObject> partyListUIItems = new List<GameObject>();
        
        // 키 설정
        [SerializeField] private KeyCode togglePartyUIKey = KeyCode.P;
        
        private void Awake()
        {
            SetupUIEvents();
            SetPanelActive(partyPanel, false);
            SetPanelActive(invitationPopup, false);
        }
        
        private void Start()
        {
            // 파티 매니저 찾기
            partyManager = FindFirstObjectByType<PartyManager>();
            if (partyManager != null)
            {
                SubscribeToPartyEvents();
            }
            
            // 초기 UI 상태 설정
            RefreshUI();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromPartyEvents();
        }
        
        private void Update()
        {
            HandleInput();
        }
        
        private void HandleInput()
        {
            if (Input.GetKeyDown(togglePartyUIKey))
            {
                TogglePartyUI();
            }
        }
        
        private void SetupUIEvents()
        {
            // 파티 생성 이벤트
            if (createPartyButton != null)
                createPartyButton.onClick.AddListener(OnCreatePartyClicked);
            
            if (cancelCreateButton != null)
                cancelCreateButton.onClick.AddListener(OnCancelCreateClicked);
            
            // 파티 관리 이벤트
            if (leavePartyButton != null)
                leavePartyButton.onClick.AddListener(OnLeavePartyClicked);
            
            if (disbandPartyButton != null)
                disbandPartyButton.onClick.AddListener(OnDisbandPartyClicked);
            
            if (toggleReadyButton != null)
                toggleReadyButton.onClick.AddListener(OnToggleReadyClicked);
            
            // 파티 검색 이벤트
            if (refreshButton != null)
                refreshButton.onClick.AddListener(OnRefreshPartyListClicked);
            
            // 초대 응답 이벤트
            if (acceptInviteButton != null)
                acceptInviteButton.onClick.AddListener(OnAcceptInviteClicked);
            
            if (declineInviteButton != null)
                declineInviteButton.onClick.AddListener(OnDeclineInviteClicked);
            
            // 슬라이더 이벤트
            if (maxMembersSlider != null)
                maxMembersSlider.onValueChanged.AddListener(OnMaxMembersSliderChanged);
        }
        
        private void SubscribeToPartyEvents()
        {
            if (partyManager == null) return;
            
            partyManager.OnPartyCreated += OnPartyCreated;
            partyManager.OnPartyDisbanded += OnPartyDisbanded;
            partyManager.OnMemberJoined += OnMemberJoined;
            partyManager.OnMemberLeft += OnMemberLeft;
            partyManager.OnInvitationReceived += OnInvitationReceived;
            partyManager.OnInvitationExpired += OnInvitationExpired;
            partyManager.OnPartyStateChanged += OnPartyStateChanged;
        }
        
        private void UnsubscribeFromPartyEvents()
        {
            if (partyManager == null) return;
            
            partyManager.OnPartyCreated -= OnPartyCreated;
            partyManager.OnPartyDisbanded -= OnPartyDisbanded;
            partyManager.OnMemberJoined -= OnMemberJoined;
            partyManager.OnMemberLeft -= OnMemberLeft;
            partyManager.OnInvitationReceived -= OnInvitationReceived;
            partyManager.OnInvitationExpired -= OnInvitationExpired;
            partyManager.OnPartyStateChanged -= OnPartyStateChanged;
        }
        
        // =========================
        // UI 이벤트 핸들러들
        // =========================
        
        public void TogglePartyUI()
        {
            isPartyUIActive = !isPartyUIActive;
            SetPanelActive(partyPanel, isPartyUIActive);
            
            if (isPartyUIActive)
            {
                RefreshUI();
            }
        }
        
        private void OnCreatePartyClicked()
        {
            if (partyManager == null) return;
            
            string partyName = partyNameInput?.text ?? "새 파티";
            int maxMembers = Mathf.RoundToInt(maxMembersSlider?.value ?? 4);
            bool isPublic = isPublicToggle?.isOn ?? true;
            
            if (string.IsNullOrEmpty(partyName.Trim()))
            {
                ShowMessage("파티명을 입력해주세요.");
                return;
            }
            
            partyManager.CreatePartyServerRpc(partyName, maxMembers, isPublic);
            
            // 생성 패널 닫기
            SetPanelActive(createPartyPanel, false);
        }
        
        private void OnCancelCreateClicked()
        {
            SetPanelActive(createPartyPanel, false);
        }
        
        private void OnLeavePartyClicked()
        {
            if (partyManager == null) return;
            
            // 확인 다이얼로그 (간단하게)
            if (Application.isEditor || Debug.isDebugBuild)
            {
                partyManager.LeavePartyServerRpc();
            }
        }
        
        private void OnDisbandPartyClicked()
        {
            if (partyManager == null) return;
            
            // 확인 다이얼로그 (간단하게)
            if (Application.isEditor || Debug.isDebugBuild)
            {
                partyManager.DisbandPartyServerRpc();
            }
        }
        
        private void OnToggleReadyClicked()
        {
            if (partyManager == null) return;
            
            partyManager.ToggleReadyStateServerRpc();
        }
        
        private void OnRefreshPartyListClicked()
        {
            RefreshPartyList();
        }
        
        private void OnAcceptInviteClicked()
        {
            if (partyManager == null || !currentInvitation.HasValue) return;
            
            partyManager.RespondToInvitationServerRpc(currentInvitation.Value.partyId, true);
            SetPanelActive(invitationPopup, false);
            currentInvitation = null;
        }
        
        private void OnDeclineInviteClicked()
        {
            if (partyManager == null || !currentInvitation.HasValue) return;
            
            partyManager.RespondToInvitationServerRpc(currentInvitation.Value.partyId, false);
            SetPanelActive(invitationPopup, false);
            currentInvitation = null;
        }
        
        private void OnMaxMembersSliderChanged(float value)
        {
            int maxMembers = Mathf.RoundToInt(value);
            SetText(maxMembersText, $"최대 인원: {maxMembers}명");
        }
        
        // =========================
        // 파티 매니저 이벤트 핸들러들
        // =========================
        
        private void OnPartyCreated(PartyInfo partyInfo)
        {
            RefreshUI();
            ShowMessage($"파티 '{partyInfo.GetPartyName()}' 생성 완료!");
        }
        
        private void OnPartyDisbanded(PartyInfo partyInfo)
        {
            RefreshUI();
            ShowMessage($"파티 '{partyInfo.GetPartyName()}'가 해산되었습니다.");
        }
        
        private void OnMemberJoined(int partyId, PartyMember member)
        {
            RefreshMemberList();
            ShowMessage($"{member.GetPlayerName()}님이 파티에 참가했습니다.");
        }
        
        private void OnMemberLeft(int partyId, ulong clientId)
        {
            RefreshMemberList();
        }
        
        private void OnInvitationReceived(PartyInvitation invitation)
        {
            // 로컬 플레이어에게 온 초대인지 확인
            if (Unity.Netcode.NetworkManager.Singleton.LocalClientId == invitation.inviteeClientId)
            {
                currentInvitation = invitation;
                ShowInvitationPopup(invitation);
            }
        }
        
        private void OnInvitationExpired(int partyId)
        {
            SetPanelActive(invitationPopup, false);
            currentInvitation = null;
            ShowMessage("파티 초대가 만료되었습니다.");
        }
        
        private void OnPartyStateChanged(int partyId, PartyState newState)
        {
            RefreshUI();
        }
        
        // =========================
        // UI 업데이트 메서드들
        // =========================
        
        private void RefreshUI()
        {
            if (partyManager == null) return;
            
            var localClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            var currentParty = partyManager.GetPlayerParty(localClientId);
            
            if (currentParty.HasValue)
            {
                ShowPartyMemberUI(currentParty.Value);
            }
            else
            {
                ShowPartyCreationUI();
            }
            
            RefreshPartyList();
        }
        
        private void ShowPartyMemberUI(PartyInfo partyInfo)
        {
            SetPanelActive(createPartyPanel, false);
            SetPanelActive(partyListPanel, false);
            SetPanelActive(partyMembersPanel, true);
            
            // 파티 정보 업데이트
            SetText(currentPartyNameText, partyInfo.GetPartyName());
            SetText(partyStateText, GetStateDisplayText(partyInfo.state));
            
            var localClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            var members = partyManager.GetPlayerPartyMembers(localClientId);
            SetText(memberCountText, $"멤버: {members.Count}/{partyInfo.maxMembers}");
            
            // 버튼 상태 업데이트
            var localMember = members.FirstOrDefault(m => m.clientId == localClientId);
            bool isLeader = localMember.role == PartyRole.Leader;
            
            SetButtonActive(disbandPartyButton, isLeader);
            SetButtonActive(leavePartyButton, !isLeader);
            
            if (localMember.isReady)
            {
                SetText(readyButtonText, "준비 해제");
            }
            else
            {
                SetText(readyButtonText, "준비 완료");
            }
            
            RefreshMemberList();
        }
        
        private void ShowPartyCreationUI()
        {
            SetPanelActive(createPartyPanel, true);
            SetPanelActive(partyListPanel, true);
            SetPanelActive(partyMembersPanel, false);
            
            // 기본값 설정
            if (partyNameInput != null)
                partyNameInput.text = "";
            
            if (maxMembersSlider != null)
            {
                maxMembersSlider.value = 4;
                OnMaxMembersSliderChanged(4);
            }
            
            if (isPublicToggle != null)
                isPublicToggle.isOn = true;
        }
        
        private void RefreshMemberList()
        {
            if (memberListContent == null || memberListItemPrefab == null) return;
            
            // 기존 멤버 UI 제거
            foreach (var item in memberUIItems)
            {
                if (item != null) Destroy(item);
            }
            memberUIItems.Clear();
            
            var localClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            var members = partyManager.GetPlayerPartyMembers(localClientId);
            
            foreach (var member in members)
            {
                CreateMemberUIItem(member);
            }
        }
        
        private void CreateMemberUIItem(PartyMember member)
        {
            var itemObj = Instantiate(memberListItemPrefab, memberListContent);
            memberUIItems.Add(itemObj);
            
            // 멤버 정보 설정
            var nameText = itemObj.transform.Find("NameText")?.GetComponent<Text>();
            var levelText = itemObj.transform.Find("LevelText")?.GetComponent<Text>();
            var roleText = itemObj.transform.Find("RoleText")?.GetComponent<Text>();
            var readyText = itemObj.transform.Find("ReadyText")?.GetComponent<Text>();
            var onlineIndicator = itemObj.transform.Find("OnlineIndicator")?.GetComponent<Image>();
            
            SetText(nameText, member.GetPlayerName());
            SetText(levelText, $"Lv.{member.playerLevel}");
            SetText(roleText, GetRoleDisplayText(member.role));
            SetText(readyText, member.isReady ? "준비완료" : "대기중");
            
            if (onlineIndicator != null)
            {
                onlineIndicator.color = member.isOnline ? Color.green : Color.red;
            }
            
            // 파티장 표시
            if (member.role == PartyRole.Leader)
            {
                var crownIcon = itemObj.transform.Find("CrownIcon");
                if (crownIcon != null)
                    crownIcon.gameObject.SetActive(true);
            }
        }
        
        private void RefreshPartyList()
        {
            // 현재는 간단하게 구현 (실제로는 서버에서 파티 목록을 받아와야 함)
            // PartyManager에 공개 파티 목록 API가 필요
        }
        
        private void ShowInvitationPopup(PartyInvitation invitation)
        {
            SetPanelActive(invitationPopup, true);
            SetText(invitationText, $"{invitation.GetInviterName()}님이 파티 '{invitation.GetPartyName()}'에 초대했습니다.");
        }
        
        // =========================
        // 유틸리티 메서드들
        // =========================
        
        private string GetStateDisplayText(PartyState state)
        {
            switch (state)
            {
                case PartyState.Forming: return "구성 중";
                case PartyState.Ready: return "준비 완료";
                case PartyState.InDungeon: return "던전 진행 중";
                case PartyState.Disbanded: return "해산됨";
                default: return "알 수 없음";
            }
        }
        
        private string GetRoleDisplayText(PartyRole role)
        {
            switch (role)
            {
                case PartyRole.Leader: return "파티장";
                case PartyRole.SubLeader: return "부파티장";
                case PartyRole.Member: return "멤버";
                default: return "";
            }
        }
        
        private void ShowMessage(string message)
        {
            Debug.Log($"🎉 Party UI: {message}");
            // 실제로는 UI에 토스트 메시지 표시
        }
        
        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }
        
        private void SetText(Text textComponent, string text)
        {
            if (textComponent != null)
                textComponent.text = text;
        }
        
        private void SetButtonActive(Button button, bool active)
        {
            if (button != null)
                button.gameObject.SetActive(active);
        }
        
        // =========================
        // 공개 API 메서드들
        // =========================
        
        /// <summary>
        /// 특정 플레이어 초대 (우클릭 메뉴 등에서 호출)
        /// </summary>
        public void InvitePlayer(ulong targetClientId)
        {
            if (partyManager != null)
            {
                partyManager.InvitePlayerServerRpc(targetClientId);
            }
        }
        
        /// <summary>
        /// 파티 생성 패널 직접 열기
        /// </summary>
        public void ShowCreatePartyPanel()
        {
            if (!isPartyUIActive)
            {
                TogglePartyUI();
            }
            SetPanelActive(createPartyPanel, true);
        }
    }
}