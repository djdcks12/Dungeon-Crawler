using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 하드코어 던전 크롤러 탑다운 2D 플레이어 컨트롤러
    /// WASD 이동, 마우스 회전, 좌클릭 공격, 우클릭 스킬
    /// </summary>
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float baseMoveSpeed = 5.0f;
        [SerializeField] private float rotationSpeed = 720.0f;
        
        [Header("Attack Settings")]
        [SerializeField] private float baseAttackCooldown = 0.5f;
        [SerializeField] private float attackRange = 2.0f;
        
        [Header("Components")]
        private Rigidbody2D rb;
        private PlayerInput playerInput;
        private PlayerNetwork playerNetwork;
        private PlayerStatsManager statsManager;
        private Animator animator;
        
        // 계산된 값들 (스탯 반영)
        private float currentMoveSpeed;
        private float currentAttackCooldown;
        
        private float lastAttackTime;
        private Camera playerCamera;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // 컴포넌트 초기화
            rb = GetComponent<Rigidbody2D>();
            playerInput = GetComponent<PlayerInput>();
            playerNetwork = GetComponent<PlayerNetwork>();
            statsManager = GetComponent<PlayerStatsManager>();
            animator = GetComponent<Animator>();
            
            // 초기 스탯 적용
            InitializeStats();
            
            if (IsLocalPlayer)
            {
                playerCamera = Camera.main;
                if (playerCamera == null)
                {
                    playerCamera = FindObjectOfType<Camera>();
                }
            }
            
            // 스탯 변경 이벤트 구독
            if (statsManager != null)
            {
                statsManager.OnStatsUpdated += OnStatsUpdated;
            }
        }
        
        public override void OnNetworkDespawn()
        {
            // 이벤트 구독 해제
            if (statsManager != null)
            {
                statsManager.OnStatsUpdated -= OnStatsUpdated;
            }
            
            base.OnNetworkDespawn();
        }
        
        private void Update()
        {
            if (!IsLocalPlayer) return;
            
            HandleRotation();
            HandleAttack();
            HandleSkill();
        }
        
        private void FixedUpdate()
        {
            if (!IsLocalPlayer) return;
            
            HandleMovement();
        }
        
        private void HandleMovement()
        {
            if (playerInput == null) return;
            
            Vector2 moveInput = playerInput.GetMoveInput();
            
            // 대각선 이동 시 속도 정규화 (동일한 속도 유지)
            if (moveInput.magnitude > 1f)
            {
                moveInput = moveInput.normalized;
            }
            
            // 스탯 시스템에서 이동 속도 가져오기
            float actualMoveSpeed = currentMoveSpeed;
            if (statsManager != null && statsManager.CurrentStats != null)
            {
                actualMoveSpeed = statsManager.CurrentStats.MoveSpeed;
            }
            
            Vector2 movement = moveInput * actualMoveSpeed;
            rb.linearVelocity = movement;
            
            // 네트워크 동기화
            if (playerNetwork != null)
            {
                playerNetwork.UpdatePosition(transform.position);
            }
            
            // 애니메이션 파라미터 설정
            if (animator != null)
            {
                animator.SetFloat("Speed", moveInput.magnitude);
                animator.SetFloat("Horizontal", moveInput.x);
                animator.SetFloat("Vertical", moveInput.y);
            }
        }
        
        private void HandleRotation()
        {
            if (playerInput == null || playerCamera == null) return;
            
            Vector2 mousePosition = playerInput.GetMousePosition();
            Vector3 worldMousePosition = playerCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, playerCamera.nearClipPlane));
            worldMousePosition.z = 0f;
            
            Vector2 direction = (worldMousePosition - transform.position).normalized;
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            
            // 부드러운 회전
            float currentAngle = transform.eulerAngles.z;
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
            
            // 네트워크 동기화
            if (playerNetwork != null)
            {
                playerNetwork.UpdateRotation(newAngle);
            }
        }
        
        private void HandleAttack()
        {
            if (playerInput == null) return;
            
            if (playerInput.GetAttackInput() && CanAttack())
            {
                PerformAttack();
            }
        }
        
        private bool CanAttack()
        {
            return Time.time >= lastAttackTime + currentAttackCooldown;
        }
        
        private void PerformAttack()
        {
            lastAttackTime = Time.time;
            
            // 공격 애니메이션 트리거
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
            
            // 네트워크를 통해 다른 클라이언트에 공격 알림
            if (playerNetwork != null)
            {
                playerNetwork.TriggerAttackServerRpc();
            }
            
            Debug.Log($"Player attacked! Cooldown: {currentAttackCooldown}s, Range: {attackRange}f");
        }
        
        private void HandleSkill()
        {
            if (playerInput == null) return;
            
            if (playerInput.GetSkillInput())
            {
                ActivateSkill();
            }
        }
        
        private void ActivateSkill()
        {
            Debug.Log("Skill activated");
            // 추후 스킬 시스템 확장 예정
        }
        
        // 스탯 시스템 연동 메서드들
        private void InitializeStats()
        {
            currentMoveSpeed = baseMoveSpeed;
            currentAttackCooldown = baseAttackCooldown;
            
            // 스탯 매니저가 있으면 스탯 적용
            if (statsManager != null && statsManager.CurrentStats != null)
            {
                ApplyStatsFromManager();
            }
        }
        
        private void OnStatsUpdated(PlayerStats stats)
        {
            ApplyStatsFromManager();
        }
        
        private void ApplyStatsFromManager()
        {
            if (statsManager == null || statsManager.CurrentStats == null) return;
            
            var stats = statsManager.CurrentStats;
            
            // AGI에 따른 이동속도 적용
            currentMoveSpeed = stats.MoveSpeed;
            
            // AGI에 따른 공격속도 적용 (공격 쿨다운 감소)
            currentAttackCooldown = baseAttackCooldown / stats.AttackSpeed;
            
            Debug.Log($"Stats Applied - MoveSpeed: {currentMoveSpeed:F2}, AttackCooldown: {currentAttackCooldown:F2}");
        }
        
        public void SetMoveSpeed(float speed)
        {
            currentMoveSpeed = speed;
        }
        
        public void TakeDamage(float damage, DamageType damageType = DamageType.Physical)
        {
            if (statsManager != null)
            {
                float actualDamage = statsManager.TakeDamage(damage, damageType);
                Debug.Log($"Player took {actualDamage:F1} damage (reduced from {damage:F1})");
                
                // 죽음 처리
                if (statsManager.IsDead)
                {
                    OnPlayerDeath();
                }
            }
            else
            {
                Debug.Log($"Player took {damage} damage (no stats manager)");
            }
        }
        
        public PlayerStats GetCurrentStats()
        {
            return statsManager?.CurrentStats;
        }
        
        private void OnPlayerDeath()
        {
            Debug.Log($"Player {gameObject.name} died!");
            
            // 플레이어 컨트롤 비활성화
            if (IsLocalPlayer)
            {
                enabled = false;
            }
            
            // 죽음 애니메이션 또는 이펙트 재생
            if (animator != null)
            {
                animator.SetTrigger("Death");
            }
        }
        
        // 경험치 획득 (적 처치 시 호출)
        public void GainExperience(long amount)
        {
            if (statsManager != null && IsOwner)
            {
                statsManager.AddExperienceServerRpc(amount);
            }
        }
        
        // 힐링
        public void Heal(float amount)
        {
            if (statsManager != null)
            {
                statsManager.Heal(amount);
            }
        }
        
        // 현재 체력 비율
        public float GetHealthPercentage()
        {
            return statsManager?.GetHealthPercentage() ?? 1f;
        }
        
        // 공격력 가져오기 (새로운 민댐/맥댐 시스템)
        public float GetAttackDamage()
        {
            if (statsManager?.CurrentStats != null)
            {
                var stats = statsManager.CurrentStats;
                
                // 새로운 민댐/맥댐 시스템으로 데미지 계산
                return stats.CalculateAttackDamage(DamageType.Physical);
            }
            
            return 10f; // 기본 공격력
        }
        
        // 스킬 데미지 계산 (스킬별 민댐/맥댐 배율 적용)
        public float GetSkillDamage(float minPercent, float maxPercent, DamageType skillType = DamageType.Physical)
        {
            if (statsManager?.CurrentStats != null)
            {
                var stats = statsManager.CurrentStats;
                return stats.CalculateSkillDamage(minPercent, maxPercent, skillType);
            }
            
            return 15f; // 기본 스킬 데미지
        }
        
        // 디버그 기능
        private void OnDrawGizmosSelected()
        {
            // 공격 사거리 시각화
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 이동 방향 시각화
            if (Application.isPlaying && rb != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)rb.linearVelocity.normalized * 2f);
            }
        }
    }
}