using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 플레이어 네트워크 동기화 시스템
    /// 공격 액션 동기화
    /// </summary>
    public class PlayerNetwork : NetworkBehaviour
    {
        // 네트워크 변수들
        private NetworkVariable<bool> isAttacking = new NetworkVariable<bool>(
            writePerm: NetworkVariableWritePermission.Owner);
        
        // 컴포넌트 참조
        private Animator animator;
        private Transform myTransform;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            myTransform = transform;
            animator = GetComponent<Animator>();
            
            // 네트워크 변수 변경 이벤트 구독
            if (!IsLocalPlayer)
            {
                isAttacking.OnValueChanged += OnAttackingChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            CancelInvoke();
            if (!IsLocalPlayer)
            {
                isAttacking.OnValueChanged -= OnAttackingChanged;
            }
            base.OnNetworkDespawn();
        }

        // 공격 트리거 (서버 RPC)
        [ServerRpc]
        public void TriggerAttackServerRpc()
        {
            // 서버에서 공격 유효성 검증
            if (CanPerformAttack())
            {
                // 모든 클라이언트에게 공격 알림
                TriggerAttackClientRpc();
            }
        }
        
        [ClientRpc]
        private void TriggerAttackClientRpc()
        {
            // NetworkVariable은 Owner만 수정 가능 (writePerm: Owner)
            if (IsOwner)
                isAttacking.Value = true;

            // 애니메이션 트리거 (모든 클라이언트에서 재생)
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }

            // 일정 시간 후 공격 상태 해제
            Invoke(nameof(ResetAttackState), 0.3f);
        }
        
        private void ResetAttackState()
        {
            if (IsOwner)
            {
                isAttacking.Value = false;
            }
        }
        
        private bool CanPerformAttack()
        {
            // 서버에서 공격 가능 여부 검증
            // 쿨다운, 사거리, 상태 등 검증
            return true; // 임시로 항상 true
        }
        
        // 네트워크 변수 변경 콜백들
        private void OnAttackingChanged(bool previousValue, bool newValue)
        {
            if (animator != null && newValue)
            {
                animator.SetTrigger("Attack");
            }
        }
        
    }
}