using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 하드코어 던전 크롤러 탑다운 2D 플레이어 컨트롤러
    /// WASD 이동, 좌우 Scale 기반 방향 전환, 좌클릭 공격, 우클릭 스킬
    /// </summary>
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float baseMoveSpeed = 5.0f;
        
        [Header("Attack Settings")]
        [SerializeField] private float baseAttackCooldown = 0.5f;
        [SerializeField] private float attackRange = 2.0f;
        
        [Header("Components")]
        private Rigidbody2D rb;
        private PlayerInput playerInput;
        private PlayerNetwork playerNetwork;
        private PlayerStatsManager statsManager;
        private EquipmentManager equipmentManager;
        private CombatSystem combatSystem;
        private PlayerSpriteAnimator spriteAnimator;
        private Animator animator;
        private DeathManager deathManager;
        private SkillManager skillManager;
        
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
            
            // PlayerInput이 없으면 자동 추가
            playerInput = GetComponent<PlayerInput>();
            playerNetwork = GetComponent<PlayerNetwork>();
            statsManager = GetComponent<PlayerStatsManager>();
            equipmentManager = GetComponent<EquipmentManager>();
            combatSystem = GetComponent<CombatSystem>();
            spriteAnimator = GetComponent<PlayerSpriteAnimator>();
            animator = GetComponent<Animator>();
            skillManager = GetComponent<SkillManager>();
            
            // PlayerSpriteAnimator가 없으면 추가
            if (spriteAnimator == null)
            {
                spriteAnimator = gameObject.AddComponent<PlayerSpriteAnimator>();
            }
            
            // Death 시스템 컴포넌트들 자동 추가
            SetupDeathSystem();
            
            // 초기 스탯 적용
            InitializeStats();
            
            // 스프라이트 애니메이션 초기화
            InitializeSpriteAnimator();
            
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
            HandleDirection();
            HandleAttack();
            HandleSkill();
            HandleUI();
        }
        
        private void FixedUpdate()
        {
            if (!IsLocalPlayer) return;
            
            HandleMovement();
        }
        
        private void HandleMovement()
        {
            if (playerInput == null || rb == null) return;

            if (statsManager != null && statsManager.CurrentStats != null && statsManager.IsDead)
            {
                // 죽은 상태에서는 이동 불가
                rb.linearVelocity = Vector2.zero;
                return;
            }
            if(spriteAnimator.IsAttackAnimationPlaying())
            {
                // 공격 애니메이션 중에는 이동 불가
                rb.linearVelocity = Vector2.zero;
                return;
            }
            
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
            
            // NetworkRigidbody2D와 호환되는 velocity 기반 이동
            Vector2 targetVelocity = moveInput * actualMoveSpeed;
            
            // velocity를 직접 설정 (NetworkRigidbody2D가 자동으로 네트워크 동기화)
            rb.linearVelocity = targetVelocity;
            
            // 스프라이트 애니메이션 업데이트 (공격 애니메이션 중이 아닐 때만)
            if (spriteAnimator != null && spriteAnimator.IsMovingOrIdleAnimationPlaying())
            {
                if (moveInput.magnitude > 0.1f)
                {
                    spriteAnimator.PlayAnimation(PlayerAnimationState.Walk);
                }
                else
                {
                    spriteAnimator.PlayAnimation(PlayerAnimationState.Idle);
                }
            }
            
            
            // 애니메이션 파라미터 설정 (기존 애니메이터와 호환)
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetFloat("Speed", moveInput.magnitude);
                animator.SetFloat("Horizontal", moveInput.x);
                animator.SetFloat("Vertical", moveInput.y);
            }
        }
        
        private void HandleDirection()
        {
            // 이동 입력에 따른 좌우 방향 전환 (Scale 기반)
            Vector2 moveInput = playerInput.GetMoveInput();
            
            if (moveInput.x != 0)
            {
                // 왼쪽(-1) 또는 오른쪽(1)으로 스케일 설정
                float direction = moveInput.x > 0 ? 1f : -1f;
                
                // Y, Z는 원래 값 유지, X만 변경
                Vector3 currentScale = transform.localScale;
                transform.localScale = new Vector3(direction * Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
            }
        }
        
        /// <summary>
        /// 마우스 방향 벡터 반환 (스킬 시전용)
        /// </summary>
        public Vector2 GetMouseDirection()
        {
            if (playerInput == null || playerCamera == null) return Vector2.right;
            
            Vector2 mousePosition = playerInput.GetMousePosition();
            Vector3 worldMousePosition = playerCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, playerCamera.nearClipPlane));
            worldMousePosition.z = 0f;
            
            return (worldMousePosition - transform.position).normalized;
        }
        
        /// <summary>
        /// 마우스 월드 좌표 반환 (스킬 타겟팅용)
        /// </summary>
        public Vector3 GetMouseWorldPosition()
        {
            if (playerInput == null || playerCamera == null) return transform.position;
            
            Vector2 mousePosition = playerInput.GetMousePosition();
            Vector3 worldMousePosition = playerCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, playerCamera.nearClipPlane));
            worldMousePosition.z = 0f;
            
            return worldMousePosition;
        }
        
        private void HandleAttack()
        {
            if (playerInput == null) return;
            
            // 마우스 버튼을 누르고 있는 동안 계속 공격 시도
            if (playerInput.IsAttackHeld() && CanAttack())
            {
                PerformAttack();
            }
        }
        
        private bool CanAttack()
        {
            // 공격 애니메이션이 재생 중이면 공격 불가
            if (spriteAnimator?.IsAttackAnimationPlaying() ?? false)
            {
                return false;
            }
            
            // 스탯에서 실시간 공격 속도 가져오기
            float attackSpeed = statsManager?.CurrentStats?.AttackSpeed ?? 1.0f;
            float actualCooldown = baseAttackCooldown / attackSpeed; // 공격속도가 높을수록 쿨다운 짧아짐
            
            return Time.time >= lastAttackTime + actualCooldown;
        }
        
        private void PerformAttack()
        {
            lastAttackTime = Time.time;
            
            // 공격 속도에 따른 애니메이션 속도 계산
            float attackSpeed = statsManager?.CurrentStats?.AttackSpeed ?? 1.0f;
            
            // 스프라이트 애니메이션 공격 재생 (데미지 프레임에서 실제 공격 실행)
            if (spriteAnimator != null)
            {
                spriteAnimator.PlayAttackAnimation(
                    onComplete: null, 
                    speedMultiplier: attackSpeed,
                    onDamageFrame: () => ExecuteActualAttack() // 데미지 프레임에서 실제 공격 실행
                );
            }
            else
            {
                // 스프라이트 애니메이터가 없으면 즉시 공격
                ExecuteActualAttack();
            }
            
            // 기존 애니메이터와 호환
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger("Attack");
            }
        }
        
        /// <summary>
        /// 실제 공격 실행 (데미지 프레임에서 호출)
        /// </summary>
        private void ExecuteActualAttack()
        {
            // CombatSystem을 통한 실제 공격 처리
            if (combatSystem != null)
            {
                combatSystem.PerformBasicAttack();
            }
            
            // 네트워크를 통해 다른 클라이언트에 공격 알림
            if (playerNetwork != null)
            {
                playerNetwork.TriggerAttackServerRpc();
            }
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
            if (skillManager != null && playerCamera != null)
            {
                // 스프라이트 애니메이션 캐스팅 재생
                if (spriteAnimator != null)
                {
                    spriteAnimator.PlayCastingAnimation();
                }
                
                // 마우스 위치를 월드 좌표로 변환하여 스킬 대상 위치로 사용
                Vector2 mousePosition = playerInput.GetMousePosition();
                Vector3 worldMousePosition = playerCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, playerCamera.nearClipPlane));
                worldMousePosition.z = 0f;
                
                // 기본 스킬 사용 (첫 번째 학습한 스킬 사용)
                var learnedSkills = skillManager.GetLearnedSkills();
                if (learnedSkills.Count > 0)
                {
                    skillManager.UseSkill(learnedSkills[0], worldMousePosition);
                }
                else
                {
                    Debug.Log("No skills learned yet!");
                }
            }
            
            Debug.Log("Skill activated");
        }
        
        /// <summary>
        /// UI 입력 처리
        /// </summary>
        private void HandleUI()
        {
            // UIManager를 통한 UI 입력 처리
            if (UIManager.Instance != null)
            {
                UIManager.Instance.HandleUIInput();
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.I))
                {
                    Debug.LogError("❌ UIManager.Instance is null! Cannot handle UI input.");
                }
            }
        }
        
        /// <summary>
        /// Death 시스템 컴포넌트 설정
        /// </summary>
        private void SetupDeathSystem()
        {
            // DeathManager 추가
            if (GetComponent<DeathManager>() == null)
            {
                deathManager = gameObject.AddComponent<DeathManager>();
            }
            else
            {
                deathManager = GetComponent<DeathManager>();
            }
            
            // CharacterDeletion 추가
            if (GetComponent<CharacterDeletion>() == null)
            {
                gameObject.AddComponent<CharacterDeletion>();
            }
            
            // ItemScatter 추가
            if (GetComponent<ItemScatter>() == null)
            {
                gameObject.AddComponent<ItemScatter>();
            }
            
            // SoulInheritance는 전역 단일 인스턴스로 관리되므로 여기서 추가하지 않음
            
            // SoulDropSystem 추가
            if (GetComponent<SoulDropSystem>() == null)
            {
                gameObject.AddComponent<SoulDropSystem>();
            }
            
            // EquipmentManager 추가
            if (GetComponent<EquipmentManager>() == null)
            {
                gameObject.AddComponent<EquipmentManager>();
            }
            
            // SkillManager 추가
            if (GetComponent<SkillManager>() == null)
            {
                gameObject.AddComponent<SkillManager>();
            }
            
            // ItemDropSystem 추가
            if (GetComponent<ItemDropSystem>() == null)
            {
                gameObject.AddComponent<ItemDropSystem>();
            }
            
            // InventoryManager 추가
            if (GetComponent<InventoryManager>() == null)
            {
                gameObject.AddComponent<InventoryManager>();
            }
            
            // EnchantManager 추가
            if (GetComponent<EnchantManager>() == null)
            {
                gameObject.AddComponent<EnchantManager>();
            }
            
            Debug.Log("Death system components setup completed");
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
        
        private void OnStatsUpdated(PlayerStatsData stats)
        {
            ApplyStatsFromManager();
            
            // 종족 변경 시 스프라이트 애니메이션도 업데이트
            if (spriteAnimator != null)
            {
                RaceData raceData = stats.RaceData;
                if (raceData != null)
                {
                    spriteAnimator.ChangeRaceData(raceData);
                }
            }
        }
        
        private void ApplyStatsFromManager()
        {
            if (statsManager == null || statsManager.CurrentStats == null) return;
            
            var stats = statsManager.CurrentStats;
            
            // AGI에 따른 이동속도 적용
            currentMoveSpeed = stats.MoveSpeed;
            
            // AGI에 따른 공격속도 적용 (공격 쿨다운 감소)
            currentAttackCooldown = baseAttackCooldown / stats.AttackSpeed;
            
            Debug.Log($"Stats Applied - Race: {stats.CharacterRace}, Level: {stats.CurrentLevel}");
            Debug.Log($"  MoveSpeed: {currentMoveSpeed:F2}, AttackCooldown: {currentAttackCooldown:F2}");
            Debug.Log($"  STR: {stats.TotalSTR:F1}, AGI: {stats.TotalAGI:F1}, VIT: {stats.TotalVIT:F1}, INT: {stats.TotalINT:F1}");
        }
        
        /// <summary>
        /// 스프라이트 애니메이션 초기화
        /// </summary>
        private void InitializeSpriteAnimator()
        {
            if (spriteAnimator == null || statsManager == null) return;
            
            // 현재 플레이어의 종족 정보 가져오기
            var currentStats = statsManager.CurrentStats;
            if (currentStats != null)
            {
                // 종족에 해당하는 RaceData 찾기
                RaceData raceData = statsManager.CurrentStats.RaceData;
                if (raceData != null)
                {
                    spriteAnimator.SetupAnimations(raceData);
                    Debug.Log($"🎭 PlayerSpriteAnimator initialized for race: {currentStats.CharacterRace}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ RaceData not found for race: {currentStats.CharacterRace}");
                }
            }
        }
        
        public void SetMoveSpeed(float speed)
        {
            currentMoveSpeed = speed;
        }
        
        public void TakeDamage()
        {
            if (statsManager != null)
            {
                
                // 피격 이펙트 재생
                if (spriteAnimator != null)
                {
                    spriteAnimator.PlayHitAnimation();
                }
                
                // 죽음 처리
                if (statsManager.IsDead)
                {
                    OnPlayerDeath();
                }
            }
        }
        
        public PlayerStatsData GetCurrentStats()
        {
            return statsManager?.CurrentStats;
        }
        
        private void OnPlayerDeath()
        {
            Debug.Log($"💀 Player {gameObject.name} died! Death penalty system will handle this.");
            
            // 던전 시스템에 플레이어 사망 알림
            NotifyDungeonManagerOfDeath();
            
            // DeathManager가 이제 모든 사망 처리를 담당하므로
            // 여기서는 최소한의 처리만 수행
            
            // 스프라이트 애니메이션 사망 재생
            if (spriteAnimator != null)
            {
                spriteAnimator.PlayDeathAnimation();
            }
            
            // 애니메이션 트리거 (DeathManager에서도 처리하지만 즉시 반응을 위해)
            if (animator != null)
            {
                animator.SetTrigger("Death");
            }
            
            // DeathManager가 플레이어 컨트롤 비활성화를 처리함
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
        
        /// <summary>
        /// 던전 매니저에게 플레이어 사망 알림
        /// </summary>
        private void NotifyDungeonManagerOfDeath()
        {
            if (IsOwner) // 플레이어 소유자만 던전 매니저에 알림
            {
                var dungeonManager = FindObjectOfType<DungeonManager>();
                if (dungeonManager != null && dungeonManager.IsActive)
                {
                    dungeonManager.OnPlayerDied(OwnerClientId);
                    Debug.Log($"🏰 Notified DungeonManager: Player {OwnerClientId} died");
                }
            }
        }
        
        /// <summary>
        /// 근처 아이템 픽업 시도
        /// </summary>
        private void TryPickupNearbyItems()
        {
            // 근처 드롭된 아이템 찾기
            Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, 2f); // 2미터 반경
            
            foreach (var collider in nearbyColliders)
            {
                // DroppedItem 체크
                var droppedItem = collider.GetComponent<DroppedItem>();
                if (droppedItem != null)
                {
                    Debug.Log($"📦 Found DroppedItem: {droppedItem.ItemInstance?.ItemData?.ItemName}");
                   
                    return; // 한 번에 하나씩만 픽업
                }
                
                // ItemDrop 체크 (레거시)
                var itemDrop = collider.GetComponent<ItemDrop>();
                if (itemDrop != null)
                {
                    Debug.Log($"📦 Found ItemDrop: {itemDrop.ItemInstance?.ItemData?.ItemName}");
                    itemDrop.PickupItem(this);
                    return; // 한 번에 하나씩만 픽업
                }
            }
        }
        
        
    }
}