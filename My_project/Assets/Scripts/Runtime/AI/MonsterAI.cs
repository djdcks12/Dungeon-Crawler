using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 기본 몬스터 AI 시스템
    /// 상태 기반 AI로 플레이어 추적, 공격, 순찰 등을 처리
    /// </summary>
    public class MonsterAI : NetworkBehaviour
    {
        [Header("AI 설정")]
        [SerializeField] protected MonsterAIType aiType = MonsterAIType.Aggressive;
        [SerializeField] protected float detectionRange = 5f;
        [SerializeField] protected float attackRange = 1.5f;
        [SerializeField] protected float loseTargetRange = 8f;
        [SerializeField] protected float moveSpeed = 2f;
        
        [Header("공격 설정")]
        [SerializeField] protected float attackCooldown = 2f;
        [SerializeField] protected float attackDamage = 20f;
        [SerializeField] protected DamageType damageType = DamageType.Physical;
        
        [Header("순찰 설정")]
        [SerializeField] private float patrolRadius = 3f;
        [SerializeField] private float patrolWaitTime = 2f;
        [SerializeField] private bool enablePatrol = true;
        
        [Header("디버그")]
        [SerializeField] private bool showDebugGizmos = true;
        
        // AI 상태
        protected MonsterAIState currentState = MonsterAIState.Idle;
        protected PlayerController currentTarget;
        protected Vector3 spawnPosition;
        protected Vector3 patrolTarget;
        protected float lastAttackTime = 0f;
        protected float stateTimer = 0f;
        protected float patrolTimer = 0f;
        
        // 컴포넌트 참조
        protected Rigidbody2D rb;
        protected MonsterEntity monsterEntity;
        protected SpriteRenderer spriteRenderer;
        protected MonsterSpriteAnimator spriteAnimator;
        
        // 네트워크 동기화
        private NetworkVariable<MonsterAIState> networkState = new NetworkVariable<MonsterAIState>(
            MonsterAIState.Idle, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>(
            Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        // 프로퍼티
        public MonsterAIState CurrentState => currentState;
        public PlayerController CurrentTarget => currentTarget;
        public bool HasTarget => currentTarget != null;
        
        /// <summary>
        /// 몬스터 AI 초기화 (NetworkBehaviour 대신 명시적 호출)
        /// </summary>
        public void InitializeAI()
        {
            Debug.Log($"🤖 MonsterAI.InitializeAI() called for {name}");
            
            // 컴포넌트 초기화
            rb = GetComponent<Rigidbody2D>();
            monsterEntity = GetComponent<MonsterEntity>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteAnimator = GetComponent<MonsterSpriteAnimator>();
            
            // 스폰 위치 저장
            spawnPosition = transform.position;
            
            // 초기 순찰 지점 설정
            SetNewPatrolTarget();
            
            // MonsterEntity 이벤트 구독 (서버에서만)
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && monsterEntity != null)
            {
                monsterEntity.OnDeath += OnMonsterDeath;
            }
            
            // 네트워크 이벤트 구독
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            {
                networkState.OnValueChanged += OnNetworkStateChanged;
                networkPosition.OnValueChanged += OnNetworkPositionChanged;
            }
            
            Debug.Log($"🤖 MonsterAI initialized: {name} at {spawnPosition}");
        }
        
        /// <summary>
        /// 몬스터 AI 정리 (NetworkBehaviour 대신 명시적 호출)
        /// </summary>
        public void CleanupAI()
        {
            Debug.Log($"🤖 MonsterAI.CleanupAI() called for {name}");
            
            // MonsterEntity 이벤트 구독 해제
            if (monsterEntity != null)
            {
                monsterEntity.OnDeath -= OnMonsterDeath;
            }
            
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            {
                networkState.OnValueChanged -= OnNetworkStateChanged;
                networkPosition.OnValueChanged -= OnNetworkPositionChanged;
            }
            
            // 상태 초기화
            currentState = MonsterAIState.Idle;
            currentTarget = null;
            stateTimer = 0f;
            patrolTimer = 0f;
            
            // 이동 중단
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
        
        protected virtual void Update()
        {
            if (!NetworkManager.IsServer) return;
            
            // 사망 상태 체크
            if (monsterEntity != null && monsterEntity.IsDead)
            {
                ChangeState(MonsterAIState.Dead);
                return;
            }
            
            // AI 업데이트
            UpdateAI();
            
            // 네트워크 동기화
            UpdateNetworkSync();
        }
        
        /// <summary>
        /// AI 메인 업데이트 로직
        /// </summary>
        protected virtual void UpdateAI()
        {
            stateTimer += Time.deltaTime;
            
            switch (currentState)
            {
                case MonsterAIState.Idle:
                    UpdateIdleState();
                    break;
                case MonsterAIState.Patrol:
                    UpdatePatrolState();
                    break;
                case MonsterAIState.Chase:
                    UpdateChaseState();
                    break;
                case MonsterAIState.Attack:
                    UpdateAttackState();
                    break;
                case MonsterAIState.Return:
                    UpdateReturnState();
                    break;
                case MonsterAIState.Dead:
                    UpdateDeadState();
                    break;
            }
            
            // 타겟 유효성 검사
            ValidateTarget();
        }
        
        /// <summary>
        /// Idle 상태 업데이트
        /// </summary>
        protected virtual void UpdateIdleState()
        {
            // 플레이어 탐지
            PlayerController nearestPlayer = FindNearestPlayer();
            if (nearestPlayer != null)
            {
                SetTarget(nearestPlayer);
                ChangeState(MonsterAIState.Chase);
                return;
            }
            
            // 순찰 시작
            if (enablePatrol && stateTimer > 1f)
            {
                ChangeState(MonsterAIState.Patrol);
            }
        }
        
        /// <summary>
        /// Patrol 상태 업데이트
        /// </summary>
        private void UpdatePatrolState()
        {
            // 플레이어 탐지 (순찰 중에도)
            PlayerController nearestPlayer = FindNearestPlayer();
            if (nearestPlayer != null)
            {
                SetTarget(nearestPlayer);
                ChangeState(MonsterAIState.Chase);
                return;
            }
            
            // 순찰 지점으로 이동
            MoveTowards(patrolTarget, moveSpeed * 0.5f);
            
            // 순찰 지점에 도착했는지 확인
            if (Vector3.Distance(transform.position, patrolTarget) < 0.5f)
            {
                patrolTimer += Time.deltaTime;
                
                if (patrolTimer >= patrolWaitTime)
                {
                    SetNewPatrolTarget();
                    patrolTimer = 0f;
                }
            }
        }
        
        /// <summary>
        /// Chase 상태 업데이트
        /// </summary>
        private void UpdateChaseState()
        {
            if (currentTarget == null)
            {
                ChangeState(MonsterAIState.Return);
                return;
            }
            
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
            
            // 공격 범위 내에 들어옴
            if (distanceToTarget <= attackRange)
            {
                ChangeState(MonsterAIState.Attack);
                return;
            }
            
            // 타겟이 너무 멀어짐
            if (distanceToTarget > loseTargetRange)
            {
                SetTarget(null);
                ChangeState(MonsterAIState.Return);
                return;
            }
            
            // 타겟을 향해 이동
            MoveTowards(currentTarget.transform.position, moveSpeed);
        }
        
        /// <summary>
        /// Attack 상태 업데이트
        /// </summary>
        protected virtual void UpdateAttackState()
        {
            if (currentTarget == null)
            {
                ChangeState(MonsterAIState.Return);
                return;
            }
            
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
            
            // 타겟이 공격 범위를 벗어남
            if (distanceToTarget > attackRange * 1.2f)
            {
                ChangeState(MonsterAIState.Chase);
                return;
            }
            
            // 타겟을 바라보기
            LookAt(currentTarget.transform.position);
            
            // 공격 실행
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                PerformAttack();
            }
        }
        
        /// <summary>
        /// Return 상태 업데이트 (스폰 지점으로 복귀)
        /// </summary>
        private void UpdateReturnState()
        {
            // 스폰 지점으로 이동
            MoveTowards(spawnPosition, moveSpeed * 0.8f);
            
            // 스폰 지점에 도착
            if (Vector3.Distance(transform.position, spawnPosition) < 1f)
            {
                // 체력 회복 (MonsterEntity 사용)
                if (monsterEntity != null)
                {
                    // MonsterEntity의 체력 회복 메서드가 있다면 사용
                    // monsterEntity.HealToFull(); // 필요시 추가
                }
                
                ChangeState(MonsterAIState.Idle);
            }
        }
        
        /// <summary>
        /// Dead 상태 업데이트
        /// </summary>
        private void UpdateDeadState()
        {
            // 사망 상태에서는 아무것도 하지 않음
            // MonsterHealth에서 오브젝트 삭제 처리
        }
        
        /// <summary>
        /// 가장 가까운 플레이어 찾기
        /// </summary>
        protected PlayerController FindNearestPlayer()
        {
            Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, detectionRange);
            PlayerController nearestPlayer = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var collider in nearbyColliders)
            {
                var player = collider.GetComponent<PlayerController>();
                if (player != null)
                {
                    // 플레이어가 살아있는지 확인
                    var statsManager = player.GetComponent<PlayerStatsManager>();
                    if (statsManager != null && !statsManager.IsDead)
                    {
                        float distance = Vector3.Distance(transform.position, player.transform.position);
                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                            nearestPlayer = player;
                        }
                    }
                }
            }
            
            return nearestPlayer;
        }
        
        /// <summary>
        /// 타겟 설정
        /// </summary>
        protected virtual void SetTarget(PlayerController target)
        {
            currentTarget = target;
            
            if (target != null)
            {
                Debug.Log($"{name} is now targeting {target.name}");
            }
            else
            {
                Debug.Log($"{name} lost target");
            }
        }
        
        /// <summary>
        /// 상태 변경
        /// </summary>
        protected virtual void ChangeState(MonsterAIState newState)
        {
            if (currentState == newState) return;
            
            MonsterAIState previousState = currentState;
            currentState = newState;
            stateTimer = 0f;
            
            // 상태 진입 처리
            OnStateEnter(newState, previousState);
            
            // 네트워크 동기화
            networkState.Value = newState;
            
            Debug.Log($"{name} state changed: {previousState} → {newState}");

            if (newState == MonsterAIState.Dead) return;
            // 애니메이션 상태 업데이트
            UpdateAnimationState(newState);
        }
        
        /// <summary>
        /// 상태 진입 처리
        /// </summary>
        private void OnStateEnter(MonsterAIState newState, MonsterAIState previousState)
        {
            switch (newState)
            {
                case MonsterAIState.Patrol:
                    SetNewPatrolTarget();
                    break;
                case MonsterAIState.Attack:
                    // 공격 시작 시 약간의 딜레이
                    lastAttackTime = Time.time - attackCooldown + 0.5f;
                    break;
                case MonsterAIState.Dead:
                    // 사망 시 이동 중단
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector2.zero;
                    }
                    break;
            }
        }
        
        /// <summary>
        /// 새로운 순찰 지점 설정
        /// </summary>
        private void SetNewPatrolTarget()
        {
            Vector2 randomDirection = Random.insideUnitCircle * patrolRadius;
            patrolTarget = spawnPosition + new Vector3(randomDirection.x, randomDirection.y, 0);
            patrolTimer = 0f;
        }
        
        /// <summary>
        /// 특정 위치로 이동
        /// </summary>
        protected virtual void MoveTowards(Vector3 targetPosition, float speed)
        {
            if (rb == null) return;
            
            Vector3 direction = (targetPosition - transform.position).normalized;
            rb.linearVelocity = direction * speed;
            
            // 이동 방향으로 회전
            LookAt(targetPosition);
        }
        
        /// <summary>
        /// 특정 위치를 바라보기 (Scale 기반)
        /// </summary>
        protected void LookAt(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - transform.position);
            if (Mathf.Abs(direction.x) > 0.1f) // 최소 거리 임계값
            {
                // 좌우 방향에 따라 Scale 변경
                float lookDirection = direction.x > 0 ? 1f : -1f;
                
                Vector3 currentScale = transform.localScale;
                transform.localScale = new Vector3(lookDirection * Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
            }
        }
        
        /// <summary>
        /// 공격 실행
        /// </summary>
        protected virtual void PerformAttack()
        {
            lastAttackTime = Time.time;
            
            // 공격 애니메이션 재생
            PlayAttackAnimation(()=>
            {
                // 실제 데미지 적용
                var targetStatsManager = currentTarget.GetComponent<PlayerStatsManager>();
                
                if (targetStatsManager != null)
                {
                    float actualDamage = targetStatsManager.TakeDamage(attackDamage, damageType);
                    
                    // 모든 클라이언트에 공격 이펙트 및 애니메이션 동기화
                    TriggerAttackAnimationClientRpc(currentTarget.transform.position, actualDamage);
                    
                    Debug.Log($"👹 {name} attacked {currentTarget.name} for {actualDamage} damage");
                }
            });
            
            
        }
        
        /// <summary>
        /// 몬스터의 공격 이펙트 가져오기
        /// </summary>
        protected virtual EffectData GetMonsterAttackEffect(MonsterEntity monsterEntity)
        {
            // 개체별 공격 이펙트 사용 (MonsterVariantData.AttackEffect)
            if (monsterEntity.VariantData?.AttackEffect != null)
            {
                return monsterEntity.VariantData.AttackEffect;
            }
            
            // 기본값: null (이펙트 없음)
            return null;
        }
        
        /// <summary>
        /// 공격 애니메이션 트리거
        /// </summary>
        protected virtual void TriggerAttackAnimation()
        {
            // 간단한 색상 변화로 공격 표시
            if (spriteRenderer != null)
            {
                StartCoroutine(AttackColorAnimation());
            }
        }
        
        /// <summary>
        /// 공격 시 색상 애니메이션
        /// </summary>
        private System.Collections.IEnumerator AttackColorAnimation()
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            
            yield return new WaitForSeconds(0.2f);
            
            spriteRenderer.color = originalColor;
        }
        
        /// <summary>
        /// 타겟 유효성 검사
        /// </summary>
        private void ValidateTarget()
        {
            if (currentTarget != null)
            {
                var statsManager = currentTarget.GetComponent<PlayerStatsManager>();
                if (statsManager == null || statsManager.IsDead)
                {
                    SetTarget(null);
                    ChangeState(MonsterAIState.Return);
                }
            }
        }
        
        /// <summary>
        /// AI 상태에 따른 애니메이션 상태 업데이트
        /// </summary>
        private void UpdateAnimationState(MonsterAIState aiState)
        {
            if (spriteAnimator == null) return;
            
            MonsterAnimationState animationState = aiState switch
            {
                MonsterAIState.Idle => MonsterAnimationState.Idle,
                MonsterAIState.Patrol => MonsterAnimationState.Move,
                MonsterAIState.Chase => MonsterAnimationState.Move,
                MonsterAIState.Return => MonsterAnimationState.Move,
                MonsterAIState.Attack => MonsterAnimationState.Idle, // 기본 대기, 실제 공격 시 별도 처리
                MonsterAIState.Dead => MonsterAnimationState.Idle,
                _ => MonsterAnimationState.Idle
            };
            
            spriteAnimator.PlayAnimation(animationState);
        }
        
        /// <summary>
        /// 공격 애니메이션 재생
        /// </summary>
        protected void PlayAttackAnimation(System.Action onDamageFrame = null)
        {
            if (spriteAnimator == null) return;
            
            spriteAnimator.PlayAttackAnimation(
            () =>
            {
                // 공격 애니메이션 완료 후 원래 상태로 복귀
                UpdateAnimationState(currentState);
            },
            () =>
            {
                // 공격 애니메이션 중 데미지 프레임 콜백
                onDamageFrame?.Invoke();
            });
        }
        
        /// <summary>
        /// 스킬 캐스팅 애니메이션 재생
        /// </summary>
        protected void PlayCastingAnimation()
        {
            if (spriteAnimator == null) return;
            
            spriteAnimator.PlayCastingAnimation(() =>
            {
                // 캐스팅 애니메이션 완료 후 원래 상태로 복귀
                UpdateAnimationState(currentState);
            });
        }
        
        /// <summary>
        /// 네트워크 동기화 업데이트
        /// </summary>
        private void UpdateNetworkSync()
        {
            if (!IsServer) return;
            
            // 위치 동기화 (1초에 10번)
            if (Time.fixedTime % 0.1f < Time.fixedDeltaTime)
            {
                networkPosition.Value = transform.position;
            }
        }
        
        /// <summary>
        /// 네트워크 상태 변경 이벤트
        /// </summary>
        private void OnNetworkStateChanged(MonsterAIState previousValue, MonsterAIState newValue)
        {
            if (IsServer) return;
            
            currentState = newValue;
            Debug.Log($"{name} network state changed to {newValue}");
            
            // 클라이언트에서도 애니메이션 상태 업데이트
            UpdateAnimationState(newValue);
        }
        
        /// <summary>
        /// 네트워크 위치 변경 이벤트
        /// </summary>
        private void OnNetworkPositionChanged(Vector3 previousValue, Vector3 newValue)
        {
            if (IsServer) return;
            
            // 클라이언트에서는 부드럽게 위치 보간
            StartCoroutine(SmoothMoveToPosition(newValue));
        }
        
        /// <summary>
        /// 부드러운 위치 이동
        /// </summary>
        private System.Collections.IEnumerator SmoothMoveToPosition(Vector3 targetPosition)
        {
            Vector3 startPosition = transform.position;
            float duration = 0.1f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
                yield return null;
            }
            
            transform.position = targetPosition;
        }

        /// <summary>
        /// 공격 애니메이션 및 이펙트 (모든 클라이언트)
        /// </summary>
        [ClientRpc]
        private void TriggerAttackAnimationClientRpc(Vector3 targetPosition, float damage)
        {
            // 공격 애니메이션 트리거
            if (spriteRenderer != null)
            {
                StartCoroutine(AttackColorAnimation());
            }

            if (EffectManager.Instance == null) return;

            var monsterEntity = GetComponent<MonsterEntity>();
            if (monsterEntity == null) return;

            EffectData attackEffect = GetMonsterAttackEffect(monsterEntity);
            if (attackEffect != null)
            {
                EffectManager.Instance.PlayHitEffect(attackEffect.name, targetPosition);
            }
            
            // 추후 사운드 등 추가 가능
        }
        
        /// <summary>
        /// 강제 타겟 설정 (외부에서 호출용)
        /// </summary>
        public void ForceSetTarget(PlayerController target)
        {
            if (!IsServer) return;
            
            SetTarget(target);
            if (target != null)
            {
                ChangeState(MonsterAIState.Chase);
            }
        }
        
        /// <summary>
        /// AI 타입 변경
        /// </summary>
        public void SetAIType(MonsterAIType newType)
        {
            aiType = newType;
            
            // AI 타입에 따른 설정 조정
            switch (aiType)
            {
                case MonsterAIType.Passive:
                    detectionRange = 0f; // 먼저 공격받기 전까지 반응 안함
                    break;
                case MonsterAIType.Defensive:
                    detectionRange = 3f;
                    loseTargetRange = 5f;
                    break;
                case MonsterAIType.Aggressive:
                    detectionRange = 5f;
                    loseTargetRange = 8f;
                    break;
                case MonsterAIType.Territorial:
                    detectionRange = 4f;
                    loseTargetRange = 6f;
                    enablePatrol = true;
                    break;
            }
        }
        
        /// <summary>
        /// 공격 데미지 설정 (보스 전용)
        /// </summary>
        public void SetAttackDamage(float damage)
        {
            attackDamage = damage;
        }
        
        /// <summary>
        /// 공격 데미지 가져오기
        /// </summary>
        public float AttackDamage => attackDamage;
        
        /// <summary>
        /// 몬스터 사망 시 호출되는 메서드 (MonsterHealth.OnDeath 이벤트)
        /// </summary>
        private void OnMonsterDeath()
        {
            Debug.Log($"☠️ {name} has died!");
            
            ChangeState(MonsterAIState.Dead);
            
            // 이동 중단
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            
            // MonsterEntity가 있으면 그쪽에서 보상 처리, 없으면 기존 방식
            var monsterEntity = GetComponent<MonsterEntity>();
            if (monsterEntity == null)
            {
                // 기존 몬스터 시스템용 경험치 보상 처리
                GiveRewardToNearbyPlayers();
                
                // 기존 영혼 드롭 시스템 (기본 0.1% 확률)
                var soulDropSystem = FindObjectOfType<SoulDropSystem>();
                if (soulDropSystem != null)
                {
                    soulDropSystem.CheckSoulDrop(transform.position, 1, name);
                }
            }
            // MonsterEntity가 있으면 해당 시스템에서 보상 처리
        }
        
        /// <summary>
        /// 주변 플레이어들에게 경험치 보상 지급 (기존 시스템용)
        /// </summary>
        private void GiveRewardToNearbyPlayers()
        {
            if (!IsServer) return;
            
            Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, 10f);
            
            foreach (var collider in nearbyColliders)
            {
                var player = collider.GetComponent<PlayerController>();
                if (player != null)
                {
                    var statsManager = player.GetComponent<PlayerStatsManager>();
                    if (statsManager != null && !statsManager.IsDead)
                    {
                        long expReward = 40; // 기본 경험치 (MonsterEntity가 없는 구형 몬스터용)
                        
                        // 서버에서 직접 경험치 지급 (NetworkVariable을 통해 동기화됨)
                        statsManager.AddExperience(expReward);
                    }
                }
            }
        }
        
        
        /// <summary>
        /// 디버그 기즈모
        /// </summary>
        protected virtual void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;
            
            // 탐지 범위
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 공격 범위
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 타겟 추적 한계 범위
            Gizmos.color = Color.orange;
            Gizmos.DrawWireSphere(transform.position, loseTargetRange);
            
            // 순찰 범위
            if (enablePatrol)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(spawnPosition, patrolRadius);
                
                // 현재 순찰 목표
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(patrolTarget, 0.2f);
                Gizmos.DrawLine(transform.position, patrolTarget);
            }
            
            // 현재 타겟
            if (currentTarget != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, currentTarget.transform.position);
            }
        }
    }
    
    /// <summary>
    /// 몬스터 AI 상태
    /// </summary>
    public enum MonsterAIState
    {
        Idle,       // 대기
        Patrol,     // 순찰
        Chase,      // 추적
        Attack,     // 공격
        Return,     // 복귀
        Dead        // 사망
    }
    
    /// <summary>
    /// 몬스터 AI 타입
    /// </summary>
    public enum MonsterAIType
    {
        Passive,        // 소극적 - 공격받기 전까지 반응 안함
        Defensive,      // 방어적 - 가까이 오면 반응, 짧게 추적
        Aggressive,     // 공격적 - 멀리서도 탐지, 길게 추적
        Territorial     // 영역형 - 일정 영역을 순찰하며 지킴
    }
}