using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 네트워크 테스트 전용 UI
    /// 간단한 Host/Client 연결 및 상태 확인
    /// </summary>
    public class NetworkTestUI : MonoBehaviour
    {
        [Header("Network Buttons")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button clientButton;
        [SerializeField] private Button serverButton;
        [SerializeField] private Button shutdownButton;
        
        [Header("Status Display")]
        [SerializeField] private Text statusText;
        [SerializeField] private Text connectionInfoText;
        [SerializeField] private Text playersListText;
        
        [Header("Connection Settings")]
        [SerializeField] private InputField ipAddressInput;
        [SerializeField] private InputField portInput;
        [SerializeField] private Toggle autoConnectToggle;
        
        [Header("Test Features")]
        [SerializeField] private Button spawnTestPlayerButton;
        [SerializeField] private Button sendTestMessageButton;
        [SerializeField] private InputField testMessageInput;
        
        private NetworkManager networkManager;
        
        private void Start()
        {
            networkManager = NetworkManager.Singleton;
            
            if (networkManager == null)
            {
                Debug.LogError("❌ NetworkManager not found! Please add NetworkManager to the scene.");
                return;
            }
            
            SetupUI();
            StartCoroutine(UpdateStatusCoroutine());
        }
        
        /// <summary>
        /// UI 초기 설정
        /// </summary>
        private void SetupUI()
        {
            // 버튼 이벤트 연결
            if (hostButton != null)
                hostButton.onClick.AddListener(StartHost);
                
            if (clientButton != null)
                clientButton.onClick.AddListener(StartClient);
                
            if (serverButton != null)
                serverButton.onClick.AddListener(StartServer);
                
            if (shutdownButton != null)
                shutdownButton.onClick.AddListener(Shutdown);
                
            if (spawnTestPlayerButton != null)
                spawnTestPlayerButton.onClick.AddListener(SpawnTestPlayer);
                
            if (sendTestMessageButton != null)
                sendTestMessageButton.onClick.AddListener(SendTestMessage);
            
            // 기본값 설정
            if (ipAddressInput != null)
                ipAddressInput.text = "127.0.0.1";
                
            if (portInput != null)
                portInput.text = "7777";
                
            if (testMessageInput != null)
                testMessageInput.text = "Hello from client!";
            
            // 초기 버튼 상태 설정
            UpdateButtonStates();
        }
        
        /// <summary>
        /// Host로 시작
        /// </summary>
        public void StartHost()
        {
            if (networkManager.StartHost())
            {
                Debug.Log("🔸 Started as Host");
                UpdateStatus("Starting as Host...", Color.yellow);
            }
            else
            {
                Debug.LogError("❌ Failed to start Host");
                UpdateStatus("Failed to start Host", Color.red);
            }
            
            UpdateButtonStates();
        }
        
        /// <summary>
        /// Client로 시작
        /// </summary>
        public void StartClient()
        {
            // IP와 포트 설정
            if (ipAddressInput != null && portInput != null)
            {
                string ip = ipAddressInput.text;
                if (int.TryParse(portInput.text, out int port))
                {
                    var transport = networkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                    if (transport != null)
                    {
                        transport.ConnectionData.Address = ip;
                        transport.ConnectionData.Port = (ushort)port;
                    }
                }
            }
            
            if (networkManager.StartClient())
            {
                Debug.Log($"🔸 Starting as Client (connecting to {ipAddressInput?.text ?? "localhost"})");
                UpdateStatus($"Connecting to {ipAddressInput?.text ?? "localhost"}...", Color.yellow);
            }
            else
            {
                Debug.LogError("❌ Failed to start Client");
                UpdateStatus("Failed to start Client", Color.red);
            }
            
            UpdateButtonStates();
        }
        
        /// <summary>
        /// Dedicated Server로 시작
        /// </summary>
        public void StartServer()
        {
            if (networkManager.StartServer())
            {
                Debug.Log("🔹 Started as Server");
                UpdateStatus("Starting as Server...", Color.blue);
            }
            else
            {
                Debug.LogError("❌ Failed to start Server");
                UpdateStatus("Failed to start Server", Color.red);
            }
            
            UpdateButtonStates();
        }
        
        /// <summary>
        /// 네트워크 종료
        /// </summary>
        public void Shutdown()
        {
            networkManager.Shutdown();
            Debug.Log("❌ Network shutdown");
            UpdateStatus("Network shutdown", Color.white);
            UpdateButtonStates();
        }
        
        /// <summary>
        /// 테스트 플레이어 스폰
        /// </summary>
        public void SpawnTestPlayer()
        {
            if (!networkManager.IsServer)
            {
                Debug.LogWarning("⚠️ Only server can spawn players");
                return;
            }
            
            // 플레이어 프리팹이 있다면 스폰
            var playerPrefab = networkManager.NetworkConfig.PlayerPrefab;
            if (playerPrefab != null)
            {
                var spawnPosition = Vector3.zero + Vector3.right * UnityEngine.Random.Range(-5f, 5f);
                var playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
                var networkObject = playerInstance.GetComponent<NetworkObject>();
                
                if (networkObject != null)
                {
                    networkObject.Spawn();
                    Debug.Log("🧪 Test player spawned");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ No player prefab configured");
            }
        }
        
        /// <summary>
        /// 테스트 메시지 전송
        /// </summary>
        public void SendTestMessage()
        {
            if (!networkManager.IsClient)
            {
                Debug.LogWarning("⚠️ Must be connected to send messages");
                return;
            }
            
            string message = testMessageInput != null ? testMessageInput.text : "Test message";
            
            // TestGameManager를 통해 메시지 전송
            if (TestGameManager.Instance != null)
            {
                TestGameManager.Instance.TestBroadcastMessage();
                Debug.Log($"📤 Sent test message: {message}");
            }
        }
        
        /// <summary>
        /// 상태 텍스트 업데이트
        /// </summary>
        private void UpdateStatus(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }
        }
        
        /// <summary>
        /// 버튼 상태 업데이트
        /// </summary>
        private void UpdateButtonStates()
        {
            bool isListening = networkManager.IsListening;
            
            if (hostButton != null)
                hostButton.interactable = !isListening;
                
            if (clientButton != null)
                clientButton.interactable = !isListening;
                
            if (serverButton != null)
                serverButton.interactable = !isListening;
                
            if (shutdownButton != null)
                shutdownButton.interactable = isListening;
                
            if (spawnTestPlayerButton != null)
                spawnTestPlayerButton.interactable = networkManager.IsServer;
                
            if (sendTestMessageButton != null)
                sendTestMessageButton.interactable = networkManager.IsClient;
        }
        
        /// <summary>
        /// 상태 정보 주기적 업데이트
        /// </summary>
        private IEnumerator UpdateStatusCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.5f);
                
                if (networkManager == null) continue;
                
                UpdateConnectionInfo();
                UpdatePlayersList();
                UpdateButtonStates();
            }
        }
        
        /// <summary>
        /// 연결 정보 업데이트
        /// </summary>
        private void UpdateConnectionInfo()
        {
            if (connectionInfoText == null) return;
            
            string info = "Connection: ";
            
            if (!networkManager.IsListening)
            {
                info += "Not Connected";
            }
            else if (networkManager.IsHost)
            {
                info += $"Host (Port: {GetCurrentPort()})";
            }
            else if (networkManager.IsServer)
            {
                info += $"Server (Port: {GetCurrentPort()})";
            }
            else if (networkManager.IsClient)
            {
                info += $"Client → {GetServerAddress()}";
            }
            
            connectionInfoText.text = info;
        }
        
        /// <summary>
        /// 플레이어 목록 업데이트
        /// </summary>
        private void UpdatePlayersList()
        {
            if (playersListText == null) return;
            
            if (!networkManager.IsServer)
            {
                playersListText.text = "Players: N/A (Not Server)";
                return;
            }
            
            var clients = networkManager.ConnectedClients;
            string playersList = $"Players ({clients.Count}):\\n";
            
            foreach (var kvp in clients)
            {
                var clientId = kvp.Key;
                var client = kvp.Value;
                
                string playerName = $"Player {clientId}";
                if (client.PlayerObject != null)
                {
                    playerName += " ✅";
                }
                else
                {
                    playerName += " ❌";
                }
                
                playersList += $"• {playerName}\\n";
            }
            
            playersListText.text = playersList;
        }
        
        /// <summary>
        /// 현재 포트 가져오기
        /// </summary>
        private string GetCurrentPort()
        {
            var transport = networkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            return transport?.ConnectionData.Port.ToString() ?? "Unknown";
        }
        
        /// <summary>
        /// 서버 주소 가져오기
        /// </summary>
        private string GetServerAddress()
        {
            var transport = networkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport != null)
            {
                return $"{transport.ConnectionData.Address}:{transport.ConnectionData.Port}";
            }
            return "Unknown";
        }
        
        private void OnDestroy()
        {
            // 이벤트 정리
            StopAllCoroutines();
        }
    }
}