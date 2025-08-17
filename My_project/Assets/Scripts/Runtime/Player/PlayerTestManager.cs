using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 플레이어 컨트롤러 테스트 매니저
    /// 개발 및 디버깅용 테스트 기능 제공
    /// </summary>
    public class PlayerTestManager : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool enableDebugUI = true;
        [SerializeField] private bool enablePerformanceMonitoring = true;
        [SerializeField] private KeyCode testKeyCode = KeyCode.F1;
        
        [Header("Test References")]
        [SerializeField] private PlayerController[] testPlayers;
        
        private bool showDebugPanel = false;
        private float frameTime = 0f;
        private int frameCount = 0;
        private float averageFPS = 0f;
        private float timeAccumulator = 0f;
        
        private void Start()
        {
            // 씬의 모든 PlayerController 찾기
            FindAllPlayerControllers();
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(testKeyCode))
            {
                showDebugPanel = !showDebugPanel;
            }
            
            if (enablePerformanceMonitoring)
            {
                UpdatePerformanceMetrics();
            }
        }
        
        private void OnGUI()
        {
            if (!enableDebugUI || !showDebugPanel) return;
            
            GUI.Box(new Rect(10, 10, 300, 400), "Player Controller Debug Panel");
            
            GUILayout.BeginArea(new Rect(20, 40, 280, 360));
            
            // 성능 정보
            GUILayout.Label($"FPS: {averageFPS:F1}");
            GUILayout.Label($"Frame Time: {frameTime * 1000:F2}ms");
            GUILayout.Space(10);
            
            // 플레이어 수 정보
            GUILayout.Label($"Total Players: {testPlayers?.Length ?? 0}");
            
            if (testPlayers != null)
            {
                foreach (var player in testPlayers)
                {
                    if (player != null)
                    {
                        DrawPlayerInfo(player);
                        GUILayout.Space(5);
                    }
                }
            }
            
            GUILayout.Space(10);
            
            // 테스트 버튼들
            if (GUILayout.Button("Refresh Player List"))
            {
                FindAllPlayerControllers();
            }
            
            if (GUILayout.Button("Test Attack All"))
            {
                TestAttackAllPlayers();
            }
            
            if (GUILayout.Button("Test Movement All"))
            {
                TestMovementAllPlayers();
            }
            
            if (GUILayout.Button("Log Network State"))
            {
                LogNetworkState();
            }
            
            GUILayout.EndArea();
        }
        
        private void DrawPlayerInfo(PlayerController player)
        {
            if (player == null) return;
            
            var networkBehaviour = player.GetComponent<NetworkBehaviour>();
            bool isLocalPlayer = networkBehaviour != null && networkBehaviour.IsLocalPlayer;
            bool isOwner = networkBehaviour != null && networkBehaviour.IsOwner;
            
            GUILayout.Label($"Player {player.name}:");
            GUILayout.Label($"  Local: {isLocalPlayer} | Owner: {isOwner}");
            GUILayout.Label($"  Position: {player.transform.position}");
            
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                GUILayout.Label($"  Velocity: {rb.linearVelocity.magnitude:F2}");
            }
        }
        
        private void FindAllPlayerControllers()
        {
            testPlayers = FindObjectsOfType<PlayerController>();
            Debug.Log($"Found {testPlayers.Length} PlayerController instances");
        }
        
        private void TestAttackAllPlayers()
        {
            foreach (var player in testPlayers)
            {
                if (player != null)
                {
                    Debug.Log($"Testing attack for player: {player.name}");
                    // 실제 공격 테스트는 플레이어가 로컬일 때만 가능
                }
            }
        }
        
        private void TestMovementAllPlayers()
        {
            foreach (var player in testPlayers)
            {
                if (player != null)
                {
                    Debug.Log($"Testing movement for player: {player.name}");
                    // 이동 테스트 로직
                }
            }
        }
        
        private void LogNetworkState()
        {
            Debug.Log("=== Network State Log ===");
            
            if (NetworkManager.Singleton != null)
            {
                Debug.Log($"Network Manager: {NetworkManager.Singleton.IsHost} (Host) | {NetworkManager.Singleton.IsServer} (Server) | {NetworkManager.Singleton.IsClient} (Client)");
                Debug.Log($"Connected Clients: {NetworkManager.Singleton.ConnectedClients.Count}");
            }
            
            foreach (var player in testPlayers)
            {
                if (player != null)
                {
                    var playerNetwork = player.GetComponent<PlayerNetwork>();
                    if (playerNetwork != null)
                    {
                        playerNetwork.LogNetworkState();
                    }
                }
            }
        }
        
        private void UpdatePerformanceMetrics()
        {
            frameTime = Time.unscaledDeltaTime;
            frameCount++;
            timeAccumulator += frameTime;
            
            if (frameCount >= 30)
            {
                averageFPS = frameCount / timeAccumulator;
                frameCount = 0;
                timeAccumulator = 0f;
            }
        }
        
        // 테스트 케이스들
        public void RunTestSuite()
        {
            Debug.Log("=== Player Controller Test Suite ===");
            
            TestSinglePlayerMovement();
            TestMultiplayerSync();
            TestAttackSystem();
            TestInputSystem();
            TestPerformance();
            
            Debug.Log("=== Test Suite Complete ===");
        }
        
        private void TestSinglePlayerMovement()
        {
            Debug.Log("Testing Single Player Movement...");
            // WASD 이동 테스트
            // 8방향 이동 테스트
            // 대각선 속도 정규화 테스트
        }
        
        private void TestMultiplayerSync()
        {
            Debug.Log("Testing Multiplayer Synchronization...");
            // 2-4명 클라이언트 동기화 테스트
            // 네트워크 지연 시뮬레이션 테스트
        }
        
        private void TestAttackSystem()
        {
            Debug.Log("Testing Attack System...");
            // 좌클릭 공격 테스트
            // 0.5초 쿨다운 테스트
            // 공격 사거리 테스트
        }
        
        private void TestInputSystem()
        {
            Debug.Log("Testing Input System...");
            // WASD 키 응답 테스트
            // 마우스 회전 테스트
            // 우클릭 스킬 테스트
        }
        
        private void TestPerformance()
        {
            Debug.Log("Testing Performance...");
            // 16명 동시 접속 시뮬레이션
            // 60fps 유지 테스트
            // 메모리 사용량 측정
        }
    }
}