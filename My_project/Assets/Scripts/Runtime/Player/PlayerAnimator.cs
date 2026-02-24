using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 플레이어 애니메이션 제어 시스템 (선택적)
    /// Idle, Walk, Attack 상태 관리
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float animationTransitionSpeed = 5.0f;
        [SerializeField] private float idleThreshold = 0.1f;
        
        private Animator animator;
        private PlayerController playerController;
        private Rigidbody2D rb;
        
        // 애니메이션 파라미터 해시값들 (성능 최적화)
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
        private static readonly int VerticalHash = Animator.StringToHash("Vertical");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
        
        // 현재 애니메이션 상태
        private Vector2 lastMovementDirection;
        private bool isCurrentlyAttacking;
        
        private void Awake()
        {
            animator = GetComponent<Animator>();
            playerController = GetComponent<PlayerController>();
            rb = GetComponent<Rigidbody2D>();
            
            if (animator == null)
            {
                Debug.LogWarning("PlayerAnimator: Animator component not found!");
            }
        }
        
        private void Update()
        {
            if (animator == null) return;
            
            UpdateMovementAnimation();
            UpdateDirectionalAnimation();
        }
        
        private void UpdateMovementAnimation()
        {
            if (rb == null) return;
            
            Vector2 velocity = rb.linearVelocity;
            float speed = velocity.magnitude;
            bool isMoving = speed > idleThreshold;
            
            // 이동 속도 파라미터 설정
            animator.SetFloat(SpeedHash, speed);
            animator.SetBool(IsMovingHash, isMoving);
            
            // 이동 방향 저장 (마지막 이동 방향 유지)
            if (isMoving)
            {
                lastMovementDirection = velocity.normalized;
            }
        }
        
        private void UpdateDirectionalAnimation()
        {
            // 마지막 이동 방향으로 애니메이션 방향 설정
            if (lastMovementDirection != Vector2.zero)
            {
                animator.SetFloat(HorizontalHash, lastMovementDirection.x);
                animator.SetFloat(VerticalHash, lastMovementDirection.y);
            }
        }
        
        // 공격 애니메이션 트리거
        public void TriggerAttackAnimation()
        {
            if (animator == null) return;
            
            animator.SetTrigger(AttackHash);
            animator.SetBool(IsAttackingHash, true);
            
            isCurrentlyAttacking = true;

            // 이전 대기 중인 리셋 취소 후 새로 예약 (중복 방지)
            CancelInvoke(nameof(ResetAttackAnimation));
            Invoke(nameof(ResetAttackAnimation), 0.5f);
        }
        
        private void ResetAttackAnimation()
        {
            if (animator != null)
            {
                animator.SetBool(IsAttackingHash, false);
            }
            isCurrentlyAttacking = false;
        }
        
        // 특정 애니메이션 상태 강제 설정
        public void SetAnimationState(string stateName)
        {
            if (animator == null) return;
            
            animator.Play(stateName);
        }
        
        // 애니메이션 속도 조절
        public void SetAnimationSpeed(float speed)
        {
            if (animator == null) return;
            
            animator.speed = speed;
        }
        
        // 현재 애니메이션 상태 확인
        public bool IsInState(string stateName)
        {
            if (animator == null) return false;
            
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(stateName);
        }
        
        // 공격 애니메이션 중인지 확인
        public bool IsAttacking()
        {
            return isCurrentlyAttacking;
        }
        
        // 이동 애니메이션 중인지 확인
        public bool IsMoving()
        {
            if (animator == null) return false;
            
            return animator.GetBool(IsMovingHash);
        }
        
        // 애니메이션 이벤트 콜백들 (애니메이션 클립에서 호출)
        public void OnAttackStart()
        {
            // 공격 시작 시 호출되는 이벤트
            Debug.Log("Attack animation started");
        }
        
        public void OnAttackHit()
        {
            // 공격이 적중하는 프레임에 호출되는 이벤트
            Debug.Log("Attack hit frame");
            // 여기서 실제 데미지 처리나 이펙트 재생
        }
        
        public void OnAttackEnd()
        {
            // 공격 종료 시 호출되는 이벤트
            Debug.Log("Attack animation ended");
            ResetAttackAnimation();
        }
        
        // 디버그 정보
        public void LogAnimationState()
        {
            if (animator == null) return;
            
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"Current Animation State: {stateInfo.shortNameHash}, " +
                     $"Speed: {animator.GetFloat(SpeedHash)}, " +
                     $"Moving: {animator.GetBool(IsMovingHash)}, " +
                     $"Attacking: {animator.GetBool(IsAttackingHash)}");
        }
        
        // 애니메이션 파라미터 리셋
        public void ResetAllParameters()
        {
            if (animator == null) return;
            
            animator.SetFloat(SpeedHash, 0f);
            animator.SetFloat(HorizontalHash, 0f);
            animator.SetFloat(VerticalHash, 0f);
            animator.SetBool(IsMovingHash, false);
            animator.SetBool(IsAttackingHash, false);
            
            lastMovementDirection = Vector2.zero;
            isCurrentlyAttacking = false;
        }
    }
}