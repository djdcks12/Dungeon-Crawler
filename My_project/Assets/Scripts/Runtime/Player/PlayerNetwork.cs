using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 플레이어 네트워크 동기화 시스템
    /// 위치, 회전, 애니메이션 상태, 공격 액션 동기화
    /// </summary>
    public class PlayerNetwork : NetworkBehaviour
    {
        [Header("Network Settings")]
        [SerializeField] private float positionSyncThreshold = 0.1f;
        [SerializeField] private float rotationSyncThreshold = 1.0f;
        [SerializeField] private float interpolationSpeed = 15.0f;
        
        // 네트워크 변수들
        private NetworkVariable<Vector2> networkPosition = new NetworkVariable<Vector2>(
            writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<float> networkRotation = new NetworkVariable<float>(
            writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<bool> isAttacking = new NetworkVariable<bool>(
            writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<bool> isMoving = new NetworkVariable<bool>(
            writePerm: NetworkVariableWritePermission.Owner);
        
        // 로컬 캐시
        private Vector2 lastSentPosition;
        private float lastSentRotation;
        
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
                networkPosition.OnValueChanged += OnPositionChanged;
                networkRotation.OnValueChanged += OnRotationChanged;
                isAttacking.OnValueChanged += OnAttackingChanged;
                isMoving.OnValueChanged += OnMovingChanged;
            }
        }
        
        private void Update()
        {
            if (!IsLocalPlayer)
            {
                // 원격 플레이어의 경우 네트워크 값으로 보간
                InterpolatePosition();
                InterpolateRotation();
            }
        }
        
        // 위치 업데이트 (로컬 플레이어만 호출)
        public void UpdatePosition(Vector3 position)
        {
            if (!IsLocalPlayer) return;
            
            Vector2 pos2D = new Vector2(position.x, position.y);
            
            // 임계값을 넘어선 경우에만 네트워크 업데이트
            if (Vector2.Distance(pos2D, lastSentPosition) > positionSyncThreshold)
            {
                networkPosition.Value = pos2D;
                lastSentPosition = pos2D;
            }
            
            // 이동 상태 업데이트
            bool moving = Vector2.Distance(pos2D, lastSentPosition) > 0.01f;
            if (isMoving.Value != moving)
            {
                isMoving.Value = moving;
            }
        }
        
        // 회전 업데이트 (로컬 플레이어만 호출)
        public void UpdateRotation(float rotation)
        {
            if (!IsLocalPlayer) return;
            
            // 임계값을 넘어선 경우에만 네트워크 업데이트
            if (Mathf.Abs(Mathf.DeltaAngle(rotation, lastSentRotation)) > rotationSyncThreshold)
            {
                networkRotation.Value = rotation;
                lastSentRotation = rotation;
            }
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
            // 공격 상태 설정
            isAttacking.Value = true;
            
            // 애니메이션 트리거
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
            
            // 일정 시간 후 공격 상태 해제
            Invoke(nameof(ResetAttackState), 0.3f);
        }
        
        private void ResetAttackState()
        {
            if (IsServer)
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
        private void OnPositionChanged(Vector2 previousValue, Vector2 newValue)
        {
            // 원격 플레이어의 위치가 변경되었을 때
        }
        
        private void OnRotationChanged(float previousValue, float newValue)
        {
            // 원격 플레이어의 회전이 변경되었을 때
        }
        
        private void OnAttackingChanged(bool previousValue, bool newValue)
        {
            if (animator != null && newValue)
            {
                animator.SetTrigger("Attack");
            }
        }
        
        private void OnMovingChanged(bool previousValue, bool newValue)
        {
            if (animator != null)
            {
                animator.SetBool("IsMoving", newValue);
            }
        }
        
        // 보간 처리
        private void InterpolatePosition()
        {
            Vector3 targetPosition = new Vector3(networkPosition.Value.x, networkPosition.Value.y, myTransform.position.z);
            myTransform.position = Vector3.Lerp(myTransform.position, targetPosition, interpolationSpeed * Time.deltaTime);
        }
        
        private void InterpolateRotation()
        {
            Quaternion targetRotation = Quaternion.Euler(0, 0, networkRotation.Value);
            myTransform.rotation = Quaternion.Lerp(myTransform.rotation, targetRotation, interpolationSpeed * Time.deltaTime);
        }
        
        // 디버그 정보
        public void LogNetworkState()
        {
            Debug.Log($"Network State - Pos: {networkPosition.Value}, Rot: {networkRotation.Value}, " +
                     $"Attacking: {isAttacking.Value}, Moving: {isMoving.Value}");
        }
        
        // 네트워크 상태 정보 반환
        public NetworkPlayerState GetNetworkState()
        {
            return new NetworkPlayerState
            {
                position = networkPosition.Value,
                rotation = networkRotation.Value,
                isAttacking = isAttacking.Value,
                isMoving = isMoving.Value
            };
        }
    }
    
    // 네트워크 상태 구조체
    [System.Serializable]
    public struct NetworkPlayerState
    {
        public Vector2 position;
        public float rotation;
        public bool isAttacking;
        public bool isMoving;
    }
}