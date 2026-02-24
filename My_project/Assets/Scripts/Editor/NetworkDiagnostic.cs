using UnityEngine;
using UnityEditor;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime.Editor
{
    /// <summary>
    /// 네트워크 호스트 시작 실패 문제를 진단하는 도구
    /// </summary>
    public static class NetworkDiagnostic
    {
        [MenuItem("Dungeon Crawler/Diagnose Network Issues")]
        public static void DiagnoseNetworkIssues()
        {
            Debug.Log("=== 네트워크 설정 진단 시작 ===");
            
            // 1. NetworkManager 존재 확인
            var networkManager = Object.FindFirstObjectByType<NetworkManager>();
            if (networkManager == null)
            {
                Debug.LogError("❌ NetworkManager를 찾을 수 없습니다!");
                Debug.LogError("   해결방법: TestSceneSetup을 사용하거나 NetworkManager를 수동으로 추가하세요.");
                return;
            }
            else
            {
                Debug.Log("✅ NetworkManager 발견됨");
            }
            
            // 2. NetworkConfig 확인
            if (networkManager.NetworkConfig == null)
            {
                Debug.LogError("❌ NetworkManager.NetworkConfig가 null입니다!");
                Debug.LogError("   해결방법: NetworkManager에 NetworkConfig를 할당하세요.");
                return;
            }
            else
            {
                Debug.Log("✅ NetworkConfig 설정됨");
            }
            
            // 3. Player Prefab 확인
            var playerPrefab = networkManager.NetworkConfig.PlayerPrefab;
            if (playerPrefab == null)
            {
                Debug.LogError("❌ Player Prefab이 설정되지 않았습니다!");
                Debug.LogError("   해결방법: NetworkManager의 PlayerPrefab을 설정하세요.");
                return;
            }
            else
            {
                Debug.Log($"✅ Player Prefab 설정됨: {playerPrefab.name}");
                
                // Player Prefab의 NetworkObject 확인
                var networkObject = playerPrefab.GetComponent<NetworkObject>();
                if (networkObject == null)
                {
                    Debug.LogError("❌ Player Prefab에 NetworkObject 컴포넌트가 없습니다!");
                    Debug.LogError("   해결방법: Player Prefab에 NetworkObject를 추가하세요.");
                    return;
                }
                else
                {
                    Debug.Log("✅ Player Prefab에 NetworkObject 있음");
                }
            }
            
            // 4. Transport 확인
            var transport = networkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport == null)
            {
                Debug.LogWarning("⚠️ UnityTransport를 찾을 수 없습니다. 기본 Transport를 사용 중일 수 있습니다.");
            }
            else
            {
                Debug.Log("✅ UnityTransport 발견됨");
                Debug.Log($"   연결 주소: {transport.ConnectionData.Address}:{transport.ConnectionData.Port}");
            }
            
            // 5. 포트 사용 확인 (Windows)
            CheckPortAvailability(7777);
            
            // 6. 현재 상태 확인
            Debug.Log($"현재 NetworkManager 상태:");
            Debug.Log($"   IsServer: {networkManager.IsServer}");
            Debug.Log($"   IsClient: {networkManager.IsClient}");
            Debug.Log($"   IsHost: {networkManager.IsHost}");
            Debug.Log($"   IsListening: {networkManager.IsListening}");
            
            Debug.Log("=== 진단 완료 ===");
            
            EditorUtility.DisplayDialog(
                "네트워크 진단 완료",
                "Console 창에서 진단 결과를 확인하세요.\n\n문제가 발견되면 로그에 표시된 해결방법을 따라해주세요.",
                "확인"
            );
        }
        
        [MenuItem("Dungeon Crawler/Fix Common Network Issues")]
        public static void FixCommonNetworkIssues()
        {
            Debug.Log("=== 일반적인 네트워크 문제 자동 수정 ===");
            
            var networkManager = Object.FindFirstObjectByType<NetworkManager>();
            if (networkManager == null)
            {
                Debug.LogError("NetworkManager를 찾을 수 없어 자동 수정할 수 없습니다.");
                return;
            }
            
            bool hasChanges = false;
            
            // 1. NetworkConfig 자동 생성
            if (networkManager.NetworkConfig == null)
            {
                networkManager.NetworkConfig = new NetworkConfig();
                Debug.Log("✅ NetworkConfig 자동 생성됨");
                hasChanges = true;
            }
            
            // 2. Player Prefab 자동 할당
            if (networkManager.NetworkConfig.PlayerPrefab == null)
            {
                var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
                if (playerPrefab != null)
                {
                    networkManager.NetworkConfig.PlayerPrefab = playerPrefab;
                    Debug.Log("✅ Player Prefab 자동 할당됨");
                    hasChanges = true;
                }
                else
                {
                    Debug.LogError("❌ Player.prefab을 찾을 수 없습니다. 먼저 Player Prefab을 생성하세요.");
                }
            }
            
            // 3. UnityTransport 자동 추가
            var transport = networkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport == null)
            {
                transport = networkManager.gameObject.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                Debug.Log("✅ UnityTransport 자동 추가됨");
                hasChanges = true;
            }
            
            // Transport를 NetworkConfig에 할당
            if (networkManager.NetworkConfig.NetworkTransport == null)
            {
                networkManager.NetworkConfig.NetworkTransport = transport;
                Debug.Log("✅ Transport를 NetworkConfig에 할당됨");
                hasChanges = true;
            }
            
            // 4. 기본 포트 설정
            if (transport != null)
            {
                transport.ConnectionData.Address = "127.0.0.1";
                transport.ConnectionData.Port = 7777;
                transport.ConnectionData.ServerListenAddress = "0.0.0.0";
                Debug.Log("✅ Transport 설정 완료");
                hasChanges = true;
            }
            
            if (hasChanges)
            {
                EditorUtility.SetDirty(networkManager);
                Debug.Log("=== 자동 수정 완료 ===");
                EditorUtility.DisplayDialog(
                    "네트워크 설정 자동 수정 완료",
                    "NetworkManager 설정이 자동으로 수정되었습니다.\n\n이제 호스트 시작을 다시 시도해보세요!",
                    "확인"
                );
            }
            else
            {
                Debug.Log("모든 설정이 이미 올바르게 되어 있습니다.");
                EditorUtility.DisplayDialog(
                    "설정 확인 완료",
                    "모든 네트워크 설정이 올바르게 되어 있습니다.\n\n다른 문제가 있을 수 있으니 Console 로그를 확인해주세요.",
                    "확인"
                );
            }
        }
        
        private static void CheckPortAvailability(int port)
        {
            try
            {
                var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, port);
                listener.Start();
                listener.Stop();
                Debug.Log($"✅ 포트 {port} 사용 가능");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠️ 포트 {port} 사용 불가: {e.Message}");
                Debug.LogWarning($"   다른 프로그램이 포트를 사용 중이거나 권한 문제일 수 있습니다.");
            }
        }
        
        [MenuItem("Dungeon Crawler/Create Debug NetworkManager")]
        public static void CreateDebugNetworkManager()
        {
            // 기존 NetworkManager 제거
            var existingNetworkManager = Object.FindFirstObjectByType<NetworkManager>();
            if (existingNetworkManager != null)
            {
                Object.DestroyImmediate(existingNetworkManager.gameObject);
                Debug.Log("기존 NetworkManager 제거됨");
            }
            
            // 새 NetworkManager 생성
            var nmObject = new GameObject("NetworkManager (Debug)");
            var networkManager = nmObject.AddComponent<NetworkManager>();
            
            // Transport 추가
            var transport = nmObject.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            
            // NetworkConfig 설정
            networkManager.NetworkConfig = new NetworkConfig();
            
            // Transport를 NetworkConfig에 할당 (중요!)
            networkManager.NetworkConfig.NetworkTransport = transport;
            
            // Player Prefab 할당
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab != null)
            {
                networkManager.NetworkConfig.PlayerPrefab = playerPrefab;
                Debug.Log($"Player Prefab assigned: {playerPrefab.name}");
            }
            else
            {
                Debug.LogWarning("Player Prefab not found at Assets/Prefabs/Player.prefab");
            }
            
            // Transport 설정
            transport.ConnectionData.Address = "127.0.0.1";
            transport.ConnectionData.Port = 7777;
            transport.ConnectionData.ServerListenAddress = "0.0.0.0";
            
            // 디버그 로그 활성화
            networkManager.LogLevel = LogLevel.Developer;
            
            
            Debug.Log("✅ 디버그용 NetworkManager 생성 완료");
            
            EditorUtility.DisplayDialog(
                "디버그 NetworkManager 생성됨",
                "새로운 디버그용 NetworkManager가 생성되었습니다.\n\n상세한 로그를 확인할 수 있으며, 모든 설정이 기본값으로 설정되었습니다.",
                "확인"
            );
        }
    }
}