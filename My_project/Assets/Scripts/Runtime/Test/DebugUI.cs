using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 개발자용 디버그 UI
    /// 실시간 정보 표시 및 테스트 기능 제공
    /// </summary>
    public class DebugUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas debugCanvas;
        [SerializeField] private GameObject cheatMenuPanel;
        [SerializeField] private Text networkStatusText;
        [SerializeField] private Text playerStatsText;
        [SerializeField] private Text systemInfoText;
        [SerializeField] private Text fpsText;
        
        [Header("Test Buttons")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button clientButton;
        [SerializeField] private Button disconnectButton;
        
        [Header("Cheat Buttons")]
        [SerializeField] private Button levelUpButton;
        [SerializeField] private Button addGoldButton;
        [SerializeField] private Button addItemButton;
        [SerializeField] private Button spawnMonsterButton;
        [SerializeField] private Button healPlayerButton;
        
        [Header("Settings")]
        [SerializeField] private float updateInterval = 0.5f;
        [SerializeField] private bool showFPS = true;
        [SerializeField] private bool showPlayerStats = true;
        [SerializeField] private bool showSystemInfo = true;
        
        private float lastUpdateTime = 0f;
        private float lastFPSUpdateTime = 0f;
        private float frameCount = 0;
        private float fps = 0;
        
        private void Start()
        {
            InitializeUI();
            SetupButtonEvents();
            
            // 초기에는 치트 메뉴 숨김
            SetCheatMenuVisible(false);
        }
        
        private void Update()
        {
            UpdateFPS();
            
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateDebugInfo();
                lastUpdateTime = Time.time;
            }
        }
        
        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            if (debugCanvas == null)
            {
                Debug.LogWarning("⚠️ Debug Canvas not assigned!");
                return;
            }
            
            // Canvas가 항상 최상위에 렌더링되도록 설정
            debugCanvas.sortingOrder = 1000;
        }
        
        /// <summary>
        /// 버튼 이벤트 설정
        /// </summary>
        private void SetupButtonEvents()
        {
            // 네트워크 버튼들
            if (hostButton != null)
                hostButton.onClick.AddListener(StartHost);
                
            if (clientButton != null)
                clientButton.onClick.AddListener(StartClient);
                
            if (disconnectButton != null)
                disconnectButton.onClick.AddListener(Disconnect);
            
            // 치트 버튼들
            if (levelUpButton != null)
                levelUpButton.onClick.AddListener(() => TestGameManager.Instance?.CheatLevelUp());
                
            if (addGoldButton != null)
                addGoldButton.onClick.AddListener(() => TestGameManager.Instance?.CheatAddGold(1000));
                
            if (addItemButton != null)
                addItemButton.onClick.AddListener(() => TestGameManager.Instance?.CheatCreateTestItem());
                
            if (spawnMonsterButton != null)
                spawnMonsterButton.onClick.AddListener(() => TestGameManager.Instance?.CheatSpawnMonster());
                
            if (healPlayerButton != null)
                healPlayerButton.onClick.AddListener(() => TestGameManager.Instance?.CheatHealPlayer());
        }
        
        /// <summary>
        /// FPS 업데이트
        /// </summary>
        private void UpdateFPS()
        {
            frameCount++;
            
            if (Time.unscaledTime - lastFPSUpdateTime >= 1.0f)
            {
                fps = frameCount / (Time.unscaledTime - lastFPSUpdateTime);
                frameCount = 0;
                lastFPSUpdateTime = Time.unscaledTime;
            }
        }
        
        /// <summary>
        /// 디버그 정보 업데이트
        /// </summary>
        private void UpdateDebugInfo()
        {
            UpdateNetworkStatus();
            UpdatePlayerStats();
            UpdateSystemInfo();
            UpdateFPSDisplay();
        }
        
        /// <summary>
        /// 네트워크 상태 업데이트
        /// </summary>
        private void UpdateNetworkStatus()
        {
            if (networkStatusText == null) return;
            
            string status = "❌ Network Not Started";
            
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                if (NetworkManager.Singleton.IsHost)
                    status = $"🔸 Host ({NetworkManager.Singleton.ConnectedClients.Count} players)";
                else if (NetworkManager.Singleton.IsServer)
                    status = $"🔹 Server ({NetworkManager.Singleton.ConnectedClients.Count} players)";
                else if (NetworkManager.Singleton.IsClient)
                    status = "🔸 Client Connected";
            }
            
            networkStatusText.text = $"Network: {status}";
        }
        
        /// <summary>
        /// 플레이어 스탯 업데이트
        /// </summary>
        private void UpdatePlayerStats()
        {
            if (!showPlayerStats || playerStatsText == null) return;
            
            var localPlayer = GetLocalPlayer();
            if (localPlayer == null)
            {
                playerStatsText.text = "Player: Not Found";
                return;
            }
            
            var statsManager = localPlayer.GetComponent<PlayerStatsManager>();
            if (statsManager == null)
            {
                playerStatsText.text = $"Stats: No StatsManager on {localPlayer.name}";
                return;
            }
            
            // NetworkVariable에서 직접 읽기 (Owner가 아닐 수도 있으므로)
            string statsText = $"Player: {localPlayer.name}\\n" +
                              $"IsOwner: {statsManager.IsOwner}\\n" +
                              $"Level: {statsManager.NetworkLevel}\\n" +
                              $"HP: {statsManager.NetworkCurrentHP:F0}/{statsManager.NetworkMaxHP:F0}\\n" +
                              $"MP: {statsManager.NetworkCurrentMP:F0}/{statsManager.NetworkMaxMP:F0}\\n";
                              
            // Owner인 경우에만 EXP와 Gold 표시 (currentStats 필요)
            if (statsManager.IsOwner && statsManager.CurrentStats != null)
            {
                statsText += $"EXP: {statsManager.CurrentStats.CurrentExperience:N0}\\n" +
                           $"Gold: {statsManager.CurrentStats.Gold:N0}";
            }
            else
            {
                statsText += "EXP: N/A (Not Owner)\\n" +
                           "Gold: N/A (Not Owner)";
            }
            
            playerStatsText.text = statsText;
        }
        
        /// <summary>
        /// 시스템 정보 업데이트
        /// </summary>
        private void UpdateSystemInfo()
        {
            if (!showSystemInfo || systemInfoText == null) return;
            
            string info = $"System Info:\\n" +
                         $"Platform: {Application.platform}\\n" +
                         $"Unity: {Application.unityVersion}\\n" +
                         $"Memory: {System.GC.GetTotalMemory(false) / 1024 / 1024:F1}MB\\n" +
                         $"Time: {Time.time:F1}s";
            
            systemInfoText.text = info;
        }
        
        /// <summary>
        /// FPS 표시 업데이트
        /// </summary>
        private void UpdateFPSDisplay()
        {
            if (!showFPS || fpsText == null) return;
            
            Color fpsColor = Color.green;
            if (fps < 30) fpsColor = Color.red;
            else if (fps < 60) fpsColor = Color.yellow;
            
            fpsText.text = $"FPS: {fps:F0}";
            fpsText.color = fpsColor;
        }
        
        /// <summary>
        /// 호스트 시작
        /// </summary>
        public void StartHost()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.StartHost();
                Debug.Log("🔸 Starting as Host...");
            }
        }
        
        /// <summary>
        /// 클라이언트 시작
        /// </summary>
        public void StartClient()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.StartClient();
                Debug.Log("🔸 Starting as Client...");
            }
        }
        
        /// <summary>
        /// 네트워크 연결 해제
        /// </summary>
        public void Disconnect()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
                Debug.Log("❌ Network disconnected");
            }
        }
        
        /// <summary>
        /// 치트 메뉴 표시/숨김
        /// </summary>
        public void SetCheatMenuVisible(bool visible)
        {
            if (cheatMenuPanel != null)
            {
                cheatMenuPanel.SetActive(visible);
            }
        }
        
        /// <summary>
        /// 디버그 UI 전체 표시/숨김
        /// </summary>
        public void SetDebugUIVisible(bool visible)
        {
            if (debugCanvas != null)
            {
                debugCanvas.gameObject.SetActive(visible);
            }
        }
        
        /// <summary>
        /// 로컬 플레이어 찾기
        /// </summary>
        private PlayerController GetLocalPlayer()
        {
            var players = FindObjectsOfType<PlayerController>();
            foreach (var player in players)
            {
                var networkBehaviour = player.GetComponent<NetworkBehaviour>();
                if (networkBehaviour != null && networkBehaviour.IsLocalPlayer)
                {
                    return player;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 디버그 메시지 로그
        /// </summary>
        public void LogMessage(string message, LogType logType = LogType.Log)
        {
            string prefix = logType switch
            {
                LogType.Error => "❌",
                LogType.Warning => "⚠️",
                LogType.Log => "ℹ️",
                _ => "📝"
            };
            
            Debug.Log($"{prefix} [DebugUI] {message}");
        }
        
        private void OnValidate()
        {
            // Inspector에서 설정 변경 시 즉시 반영
            if (Application.isPlaying)
            {
                if (debugCanvas != null)
                    debugCanvas.sortingOrder = 1000;
            }
        }
    }
}