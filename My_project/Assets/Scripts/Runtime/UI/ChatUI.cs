using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 채팅 UI 시스템
    /// 멀티플레이어 채팅 기능 제공
    /// </summary>
    public class ChatUI : NetworkBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject chatPanel;
        [SerializeField] private ScrollRect chatScrollRect;
        [SerializeField] private Transform chatContent;
        [SerializeField] private InputField chatInputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button toggleChatButton;
        
        [Header("Chat Message Prefab")]
        [SerializeField] private GameObject chatMessagePrefab;
        
        [Header("Chat Channels")]
        [SerializeField] private Button allChannelButton;
        [SerializeField] private Button partyChannelButton;
        [SerializeField] private Button systemChannelButton;
        [SerializeField] private Text currentChannelText;
        
        [Header("Settings")]
        [SerializeField] private KeyCode openChatKey = KeyCode.Return;
        [SerializeField] private KeyCode closeChatKey = KeyCode.Escape;
        [SerializeField] private int maxChatMessages = 50;
        [SerializeField] private float chatFadeTime = 10f;
        [SerializeField] private bool autoHideChat = true;
        
        [Header("Colors")]
        [SerializeField] private Color allChatColor = Color.white;
        [SerializeField] private Color partyChatColor = Color.cyan;
        [SerializeField] private Color systemChatColor = Color.yellow;
        [SerializeField] private Color whisperChatColor = Color.magenta;
        [SerializeField] private Color errorChatColor = Color.red;
        
        // 상태
        private List<ChatMessage> chatMessages = new List<ChatMessage>();
        private ChatChannel currentChannel = ChatChannel.All;
        private bool isChatOpen = false;
        private bool isInputFocused = false;
        private Coroutine autoHideCoroutine;
        
        // 컴포넌트 참조
        private PlayerController localPlayer;
        
        // 네트워크 변수
        private NetworkVariable<int> totalMessagesCount = new NetworkVariable<int>(0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        
        // 이벤트
        public System.Action<ChatMessage> OnMessageReceived;
        public System.Action<bool> OnChatToggled;
        
        private void Start()
        {
            InitializeChatUI();
            SetupEventListeners();
            
            // 초기에는 채팅창 숨김
            SetChatVisibility(false);
        }
        
        private void Update()
        {
            HandleInputs();
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // 로컬 플레이어 찾기
            localPlayer = FindLocalPlayer();
            
            if (IsClient)
            {
                // 시스템 메시지 표시
                AddSystemMessage("채팅 시스템이 연결되었습니다.");
            }
        }
        
        /// <summary>
        /// 채팅 UI 초기화
        /// </summary>
        private void InitializeChatUI()
        {
            // 채널 버튼 색상 초기화
            UpdateChannelButtons();
            
            // 입력 필드 초기화
            if (chatInputField != null)
            {
                chatInputField.onEndEdit.AddListener(OnInputFieldEndEdit);
                chatInputField.onValueChanged.AddListener(OnInputFieldChanged);
            }
        }
        
        /// <summary>
        /// 이벤트 리스너 설정
        /// </summary>
        private void SetupEventListeners()
        {
            if (sendButton != null)
                sendButton.onClick.AddListener(SendMessage);
                
            if (toggleChatButton != null)
                toggleChatButton.onClick.AddListener(ToggleChat);
                
            if (allChannelButton != null)
                allChannelButton.onClick.AddListener(() => SetChannel(ChatChannel.All));
                
            if (partyChannelButton != null)
                partyChannelButton.onClick.AddListener(() => SetChannel(ChatChannel.Party));
                
            if (systemChannelButton != null)
                systemChannelButton.onClick.AddListener(() => SetChannel(ChatChannel.System));
        }
        
        /// <summary>
        /// 입력 처리
        /// </summary>
        private void HandleInputs()
        {
            // 채팅창 열기/닫기
            if (Input.GetKeyDown(openChatKey) && !isChatOpen)
            {
                OpenChat();
            }
            else if (Input.GetKeyDown(closeChatKey) && isChatOpen)
            {
                CloseChat();
            }
            
            // 메시지 전송 (Enter키)
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (isInputFocused && !string.IsNullOrEmpty(chatInputField.text))
                {
                    SendMessage();
                }
                else if (!isChatOpen)
                {
                    OpenChat();
                }
            }
        }
        
        /// <summary>
        /// 채팅창 토글
        /// </summary>
        public void ToggleChat()
        {
            if (isChatOpen)
                CloseChat();
            else
                OpenChat();
        }
        
        /// <summary>
        /// 채팅창 열기
        /// </summary>
        public void OpenChat()
        {
            SetChatVisibility(true);
            
            // 입력 필드에 포커스
            if (chatInputField != null)
            {
                chatInputField.Select();
                chatInputField.ActivateInputField();
                isInputFocused = true;
            }
            
            // 자동 숨김 코루틴 중지
            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
                autoHideCoroutine = null;
            }
            
            OnChatToggled?.Invoke(true);
        }
        
        /// <summary>
        /// 채팅창 닫기
        /// </summary>
        public void CloseChat()
        {
            SetChatVisibility(false);
            isInputFocused = false;
            
            // 입력 필드 비우기
            if (chatInputField != null)
            {
                chatInputField.text = "";
                chatInputField.DeactivateInputField();
            }
            
            OnChatToggled?.Invoke(false);
            
            // 자동 숨김 시작
            if (autoHideChat)
            {
                autoHideCoroutine = StartCoroutine(AutoHideChat());
            }
        }
        
        /// <summary>
        /// 채팅창 가시성 설정
        /// </summary>
        private void SetChatVisibility(bool visible)
        {
            isChatOpen = visible;
            
            if (chatPanel != null)
            {
                chatPanel.SetActive(visible);
            }
        }
        
        /// <summary>
        /// 채널 설정
        /// </summary>
        public void SetChannel(ChatChannel channel)
        {
            currentChannel = channel;
            UpdateChannelButtons();
            UpdateChannelText();
        }
        
        /// <summary>
        /// 채널 버튼 업데이트
        /// </summary>
        private void UpdateChannelButtons()
        {
            // 모든 버튼을 기본 색상으로
            var normalColor = Color.white;
            var selectedColor = Color.yellow;
            
            if (allChannelButton != null)
            {
                var colors = allChannelButton.colors;
                colors.normalColor = (currentChannel == ChatChannel.All) ? selectedColor : normalColor;
                allChannelButton.colors = colors;
            }
            
            if (partyChannelButton != null)
            {
                var colors = partyChannelButton.colors;
                colors.normalColor = (currentChannel == ChatChannel.Party) ? selectedColor : normalColor;
                partyChannelButton.colors = colors;
            }
            
            if (systemChannelButton != null)
            {
                var colors = systemChannelButton.colors;
                colors.normalColor = (currentChannel == ChatChannel.System) ? selectedColor : normalColor;
                systemChannelButton.colors = colors;
            }
        }
        
        /// <summary>
        /// 채널 텍스트 업데이트
        /// </summary>
        private void UpdateChannelText()
        {
            if (currentChannelText == null) return;
            
            switch (currentChannel)
            {
                case ChatChannel.All:
                    currentChannelText.text = "[전체]";
                    currentChannelText.color = allChatColor;
                    break;
                case ChatChannel.Party:
                    currentChannelText.text = "[파티]";
                    currentChannelText.color = partyChatColor;
                    break;
                case ChatChannel.System:
                    currentChannelText.text = "[시스템]";
                    currentChannelText.color = systemChatColor;
                    break;
            }
        }
        
        /// <summary>
        /// 메시지 전송
        /// </summary>
        public void SendMessage()
        {
            if (chatInputField == null || string.IsNullOrEmpty(chatInputField.text.Trim()))
                return;
                
            string message = chatInputField.text.Trim();
            
            // 명령어 처리
            if (message.StartsWith("/"))
            {
                ProcessCommand(message);
            }
            else
            {
                // 일반 메시지 전송
                SendChatMessageServerRpc(message, currentChannel);
            }
            
            // 입력 필드 클리어
            chatInputField.text = "";
            chatInputField.Select();
        }
        
        /// <summary>
        /// 채팅 메시지 전송 (ServerRPC)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void SendChatMessageServerRpc(string message, ChatChannel channel, ServerRpcParams rpcParams = default)
        {
            var senderId = rpcParams.Receive.SenderClientId;
            
            // 발신자 정보 가져오기
            string senderName = GetPlayerName(senderId);
            
            // 채팅 메시지 생성
            var chatMessage = new ChatMessage
            {
                senderId = senderId,
                senderName = senderName,
                message = message,
                channel = channel,
                timestamp = System.DateTime.Now
            };
            
            // 모든 클라이언트에 전송
            ReceiveChatMessageClientRpc(chatMessage);
            
            // 서버에서도 메시지 처리
            ProcessReceivedMessage(chatMessage);
        }
        
        /// <summary>
        /// 채팅 메시지 수신 (ClientRPC)
        /// </summary>
        [ClientRpc]
        private void ReceiveChatMessageClientRpc(ChatMessage chatMessage)
        {
            ProcessReceivedMessage(chatMessage);
        }
        
        /// <summary>
        /// 수신된 메시지 처리
        /// </summary>
        private void ProcessReceivedMessage(ChatMessage chatMessage)
        {
            AddChatMessage(chatMessage);
            OnMessageReceived?.Invoke(chatMessage);
            
            // 채팅창이 닫혀있으면 임시로 보이기
            if (!isChatOpen && autoHideChat)
            {
                ShowTemporarily();
            }
        }
        
        /// <summary>
        /// 채팅 메시지 추가
        /// </summary>
        private void AddChatMessage(ChatMessage chatMessage)
        {
            if (chatMessagePrefab == null || chatContent == null) return;
            
            // 메시지 프리팹 생성
            var messageObj = Instantiate(chatMessagePrefab, chatContent);
            var messageUI = messageObj.GetComponent<ChatMessageUI>();
            
            if (messageUI != null)
            {
                messageUI.SetMessage(chatMessage, GetChannelColor(chatMessage.channel));
            }
            
            // 메시지 목록에 추가
            chatMessages.Add(chatMessage);
            
            // 최대 메시지 수 제한
            if (chatMessages.Count > maxChatMessages)
            {
                var oldestMessage = chatContent.GetChild(0);
                if (oldestMessage != null)
                {
                    Destroy(oldestMessage.gameObject);
                }
                chatMessages.RemoveAt(0);
            }
            
            // 스크롤을 아래로
            StartCoroutine(ScrollToBottom());
        }
        
        /// <summary>
        /// 시스템 메시지 추가
        /// </summary>
        public void AddSystemMessage(string message)
        {
            var systemMessage = new ChatMessage
            {
                senderId = 0,
                senderName = "시스템",
                message = message,
                channel = ChatChannel.System,
                timestamp = System.DateTime.Now
            };
            
            AddChatMessage(systemMessage);
        }
        
        /// <summary>
        /// 명령어 처리
        /// </summary>
        private void ProcessCommand(string command)
        {
            var parts = command.Split(' ');
            var cmd = parts[0].ToLower();
            
            switch (cmd)
            {
                case "/help":
                    ShowHelp();
                    break;
                case "/clear":
                    ClearChat();
                    break;
                case "/w":
                case "/whisper":
                    if (parts.Length >= 3)
                    {
                        string targetName = parts[1];
                        string message = string.Join(" ", parts, 2, parts.Length - 2);
                        SendWhisper(targetName, message);
                    }
                    else
                    {
                        AddSystemMessage("사용법: /w [플레이어명] [메시지]");
                    }
                    break;
                default:
                    AddSystemMessage($"알 수 없는 명령어: {cmd}");
                    break;
            }
        }
        
        /// <summary>
        /// 도움말 표시
        /// </summary>
        private void ShowHelp()
        {
            AddSystemMessage("=== 채팅 명령어 ===");
            AddSystemMessage("/help - 도움말 표시");
            AddSystemMessage("/clear - 채팅창 지우기");
            AddSystemMessage("/w [플레이어] [메시지] - 귓속말");
        }
        
        /// <summary>
        /// 채팅창 지우기
        /// </summary>
        private void ClearChat()
        {
            foreach (Transform child in chatContent)
            {
                Destroy(child.gameObject);
            }
            chatMessages.Clear();
        }
        
        /// <summary>
        /// 귓속말 전송
        /// </summary>
        private void SendWhisper(string targetName, string message)
        {
            // 귓속말 구현 (서버RPC 필요)
            AddSystemMessage($"[귓속말] {targetName}에게: {message}");
        }
        
        /// <summary>
        /// 채널 색상 가져오기
        /// </summary>
        private Color GetChannelColor(ChatChannel channel)
        {
            switch (channel)
            {
                case ChatChannel.All: return allChatColor;
                case ChatChannel.Party: return partyChatColor;
                case ChatChannel.System: return systemChatColor;
                case ChatChannel.Whisper: return whisperChatColor;
                default: return allChatColor;
            }
        }
        
        /// <summary>
        /// 플레이어 이름 가져오기
        /// </summary>
        private string GetPlayerName(ulong clientId)
        {
            // 실제로는 플레이어 데이터에서 이름을 가져와야 함
            return $"Player_{clientId}";
        }
        
        /// <summary>
        /// 로컬 플레이어 찾기
        /// </summary>
        private PlayerController FindLocalPlayer()
        {
            var players = FindObjectsOfType<PlayerController>();
            foreach (var player in players)
            {
                if (player.IsLocalPlayer)
                    return player;
            }
            return null;
        }
        
        /// <summary>
        /// 스크롤을 아래로
        /// </summary>
        private IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();
            if (chatScrollRect != null)
            {
                chatScrollRect.verticalNormalizedPosition = 0f;
            }
        }
        
        /// <summary>
        /// 임시로 채팅창 표시
        /// </summary>
        private void ShowTemporarily()
        {
            if (chatPanel != null)
            {
                chatPanel.SetActive(true);
            }
            
            if (autoHideCoroutine != null)
                StopCoroutine(autoHideCoroutine);
                
            autoHideCoroutine = StartCoroutine(AutoHideChat());
        }
        
        /// <summary>
        /// 자동 숨김 코루틴
        /// </summary>
        private IEnumerator AutoHideChat()
        {
            yield return new WaitForSeconds(chatFadeTime);
            
            if (!isChatOpen && chatPanel != null)
            {
                chatPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// 입력 필드 종료 처리
        /// </summary>
        private void OnInputFieldEndEdit(string text)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SendMessage();
            }
        }
        
        /// <summary>
        /// 입력 필드 변경 처리
        /// </summary>
        private void OnInputFieldChanged(string text)
        {
            // 입력 중일 때 자동 숨김 방지
            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
                autoHideCoroutine = null;
            }
        }
        
        private void OnDestroy()
        {
            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
            }
        }
    }
    
    /// <summary>
    /// 채팅 메시지 데이터
    /// </summary>
    [System.Serializable]
    public struct ChatMessage : INetworkSerializable
    {
        public ulong senderId;
        public string senderName;
        public string message;
        public ChatChannel channel;
        public System.DateTime timestamp;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref senderId);
            serializer.SerializeValue(ref senderName);
            serializer.SerializeValue(ref message);
            serializer.SerializeValue(ref channel);
            
            // DateTime 직렬화 (Ticks 사용)
            if (serializer.IsWriter)
            {
                long ticks = timestamp.Ticks;
                serializer.SerializeValue(ref ticks);
            }
            else
            {
                long ticks = 0;
                serializer.SerializeValue(ref ticks);
                timestamp = new System.DateTime(ticks);
            }
        }
    }
    
    /// <summary>
    /// 채팅 채널
    /// </summary>
    public enum ChatChannel
    {
        All,
        Party,
        System,
        Whisper
    }
}