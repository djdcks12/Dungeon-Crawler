using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 게임 세션 관리용 플레이어 (기존 템플릿 유지)
    /// 실제 게임플레이 로직은 PlayerController에서 처리
    /// </summary>
    internal class Player : NetworkBehaviour
    {
        [Header("Player References")]
        [SerializeField] private PlayerController playerController;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // PlayerController 컴포넌트 찾기
            if (playerController == null)
            {
                playerController = GetComponent<PlayerController>();
            }
        }
        
        [ClientRpc]
        internal void OnClientPrepareGameClientRpc()
        {
            if (!IsLocalPlayer)
            {
                return;
            }
            if (MetagameApplication.Instance)
            {
                // 던전 크롤러 테스트 환경에서는 MatchEnteredEvent 사용하지 않음 - 비활성화
                // MetagameApplication.Instance.Broadcast(new MatchEnteredEvent());
            }
            Debug.Log("[Local client] Preparing game [Showing loading screen]");
            OnClientReadyToStart();
        }

        internal void OnClientReadyToStart()
        {
            Debug.Log("[Local client] Notifying server I'm ready");
            OnServerNotifiedOfClientReadinessServerRpc();
        }

        [ServerRpc]
        internal void OnServerNotifiedOfClientReadinessServerRpc()
        {
            Debug.Log("[Server] I'm ready");
            CustomNetworkManager.Singleton.OnServerPlayerIsReady(this);
        }

        [ClientRpc]
        internal void OnClientStartGameClientRpc()
        {
            if (!IsLocalPlayer) { return; }
        }

        [ServerRpc]
        internal void OnPlayerAskedToWinServerRpc()
        {
            OnServerPlayerAskedToWin();
        }

        internal void OnServerPlayerAskedToWin()
        {
        }
        
        // PlayerController 접근용 메서드들
        public PlayerController GetPlayerController()
        {
            return playerController;
        }
        
        public void TakeDamage(float damage)
        {
            if (playerController != null)
            {
                playerController.TakeDamage(damage);
            }
        }
        
        public void SetMoveSpeed(float speed)
        {
            if (playerController != null)
            {
                playerController.SetMoveSpeed(speed);
            }
        }
    }
}
