using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 던전 시스템 통합 컨트롤러
    /// 던전 매니저, UI, 플레이어 상호작용을 연결
    /// </summary>
    public class DungeonController : NetworkBehaviour
    {
        [Header("던전 매니저")]
        [SerializeField] private DungeonManager dungeonManager;
        [SerializeField] private DungeonUI dungeonUI;
        
        [Header("던전 입구 설정")]
        [SerializeField] private Transform dungeonEntrance;
        [SerializeField] private float entranceRadius = 3.0f;
        [SerializeField] private KeyCode enterDungeonKey = KeyCode.F;
        [SerializeField] private KeyCode toggleUIKey = KeyCode.Tab;
        
        [Header("테스트 설정")]
        [SerializeField] private bool enableTestMode = false;
        [SerializeField] private KeyCode testStartKey = KeyCode.T;
        [SerializeField] private int testDungeonIndex = 0;
        
        // 상태 관리
        private bool playerNearEntrance = false;
        private PlayerController localPlayer;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // 로컬 플레이어 찾기
            if (IsOwner)
            {
                FindLocalPlayer();
            }
            
            // 던전 매니저가 없으면 자동으로 찾기
            if (dungeonManager == null)
            {
                dungeonManager = FindObjectOfType<DungeonManager>();
            }
            
            // 던전 UI가 없으면 자동으로 찾기
            if (dungeonUI == null)
            {
                dungeonUI = FindObjectOfType<DungeonUI>();
            }
            
            Debug.Log($"DungeonController initialized (IsOwner: {IsOwner})");
        }
        
        private void Update()
        {
            if (!IsOwner) return;
            
            // 던전 입구 근처 체크
            CheckPlayerNearEntrance();
            
            // 키 입력 처리
            HandleInputs();
            
            // 테스트 모드
            if (enableTestMode)
            {
                HandleTestInputs();
            }
        }
        
        /// <summary>
        /// 로컬 플레이어 찾기
        /// </summary>
        private void FindLocalPlayer()
        {
            var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (playerObject != null)
            {
                localPlayer = playerObject.GetComponent<PlayerController>();
            }
        }
        
        /// <summary>
        /// 플레이어가 던전 입구 근처에 있는지 확인
        /// </summary>
        private void CheckPlayerNearEntrance()
        {
            if (localPlayer == null || dungeonEntrance == null) return;
            
            float distance = Vector3.Distance(localPlayer.transform.position, dungeonEntrance.position);
            bool wasNearEntrance = playerNearEntrance;
            playerNearEntrance = distance <= entranceRadius;
            
            // 상태 변경 시 UI 업데이트
            if (wasNearEntrance != playerNearEntrance)
            {
                if (playerNearEntrance)
                {
                    ShowEntrancePrompt();
                }
                else
                {
                    HideEntrancePrompt();
                }
            }
        }
        
        /// <summary>
        /// 키 입력 처리
        /// </summary>
        private void HandleInputs()
        {
            // 던전 입장 키
            if (Input.GetKeyDown(enterDungeonKey) && playerNearEntrance)
            {
                RequestEnterDungeon();
            }
            
            // UI 토글 키
            if (Input.GetKeyDown(toggleUIKey))
            {
                ToggleDungeonUI();
            }
        }
        
        /// <summary>
        /// 테스트 모드 키 입력
        /// </summary>
        private void HandleTestInputs()
        {
            if (Input.GetKeyDown(testStartKey))
            {
                StartTestDungeon();
            }
        }
        
        /// <summary>
        /// 던전 입장 요청
        /// </summary>
        private void RequestEnterDungeon()
        {
            if (dungeonManager == null)
            {
                Debug.LogWarning("No DungeonManager found!");
                return;
            }
            
            if (dungeonManager.IsActive)
            {
                Debug.LogWarning("Dungeon is already active!");
                return;
            }
            
            // 기본 던전 시작 (첫 번째 던전)
            dungeonManager.StartDungeonServerRpc(0);
            Debug.Log("🏰 Requested to start dungeon");
        }
        
        /// <summary>
        /// 테스트 던전 시작
        /// </summary>
        private void StartTestDungeon()
        {
            if (dungeonManager == null) return;
            
            dungeonManager.StartDungeonServerRpc(testDungeonIndex);
            Debug.Log($"🧪 Started test dungeon (Index: {testDungeonIndex})");
        }
        
        /// <summary>
        /// 던전 UI 토글
        /// </summary>
        private void ToggleDungeonUI()
        {
            if (dungeonUI != null)
            {
                dungeonUI.ToggleDungeonUI();
            }
        }
        
        /// <summary>
        /// 던전 입구 안내 표시
        /// </summary>
        private void ShowEntrancePrompt()
        {
            Debug.Log($"🚪 Press [{enterDungeonKey}] to enter dungeon");
            // 실제로는 UI에 키 안내 표시
        }
        
        /// <summary>
        /// 던전 입구 안내 숨김
        /// </summary>
        private void HideEntrancePrompt()
        {
            Debug.Log("Left dungeon entrance area");
            // UI 안내 숨김
        }
        
        /// <summary>
        /// 던전 상태 확인
        /// </summary>
        public bool IsDungeonActive()
        {
            return dungeonManager != null && dungeonManager.IsActive;
        }
        
        /// <summary>
        /// 현재 던전 정보 가져오기
        /// </summary>
        public DungeonInfo? GetCurrentDungeonInfo()
        {
            if (dungeonManager != null && dungeonManager.IsActive)
            {
                return dungeonManager.CurrentDungeon;
            }
            return null;
        }
        
        /// <summary>
        /// 플레이어를 던전 시작 위치로 이동
        /// </summary>
        [ClientRpc]
        public void TeleportToStartPositionClientRpc(Vector3 position)
        {
            if (localPlayer != null)
            {
                localPlayer.transform.position = position;
                Debug.Log($"🚀 Teleported to dungeon start: {position}");
            }
        }
        
        /// <summary>
        /// 던전 완료 후 마을로 복귀
        /// </summary>
        [ClientRpc]
        public void ReturnToTownClientRpc(Vector3 townPosition)
        {
            if (localPlayer != null)
            {
                localPlayer.transform.position = townPosition;
                Debug.Log($"🏠 Returned to town: {townPosition}");
            }
        }
        
        /// <summary>
        /// 긴급 던전 탈출 (관리자 전용)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void EmergencyExitDungeonServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            
            // 관리자 권한 확인 (추후 구현)
            // if (!IsAdmin(rpcParams.Receive.SenderClientId)) return;
            
            if (dungeonManager != null && dungeonManager.IsActive)
            {
                // 던전 강제 종료
                Debug.LogWarning("⚠️ Emergency dungeon exit triggered!");
                // dungeonManager.EndDungeonServerRpc(false, "Emergency exit");
            }
        }
        
        /// <summary>
        /// 던전 통계 정보
        /// </summary>
        public void ShowDungeonStats()
        {
            if (dungeonManager != null)
            {
                var dungeonInfo = dungeonManager.CurrentDungeon;
                var players = dungeonManager.Players;
                
                Debug.Log($"=== Dungeon Statistics ===");
                Debug.Log($"Name: {dungeonInfo.GetDungeonName()}");
                Debug.Log($"Floor: {dungeonManager.CurrentFloor}/{dungeonInfo.maxFloors}");
                Debug.Log($"State: {dungeonManager.State}");
                Debug.Log($"Time Remaining: {dungeonManager.RemainingTime:F1}s");
                Debug.Log($"Players: {players.Count}");
                
                foreach (var player in players)
                {
                    string status = player.isAlive ? "Alive" : "Dead";
                    Debug.Log($"  - {player.GetPlayerName()} (Lv.{player.playerLevel}) [{status}]");
                }
            }
        }
        
        /// <summary>
        /// 디버그 기즈모
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (dungeonEntrance != null)
            {
                // 던전 입구 표시
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(dungeonEntrance.position, entranceRadius);
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawIcon(dungeonEntrance.position, "Portal", true);
            }
            
            // 현재 던전 상태 표시
            if (Application.isPlaying && dungeonManager != null)
            {
                Gizmos.color = dungeonManager.IsActive ? Color.green : Color.gray;
                Gizmos.DrawCube(transform.position + Vector3.up * 2, Vector3.one * 0.5f);
            }
        }
        
        /// <summary>
        /// 설정 유효성 검사
        /// </summary>
        [ContextMenu("Validate Settings")]
        private void ValidateSettings()
        {
            if (dungeonManager == null)
            {
                Debug.LogWarning("DungeonManager is not assigned!");
            }
            
            if (dungeonUI == null)
            {
                Debug.LogWarning("DungeonUI is not assigned!");
            }
            
            if (dungeonEntrance == null)
            {
                Debug.LogWarning("Dungeon entrance transform is not assigned!");
            }
            
            Debug.Log("DungeonController settings validation complete.");
        }
    }
}