using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 보스 몬스터 AI 시스템
    /// 기본 MonsterAI를 확장하여 보스 전용 패턴과 스킬을 제공
    /// </summary>
    public class BossMonsterAI : MonsterAI
    {
        [Header("보스 전용 설정")]
        [SerializeField] private BossType bossType = BossType.FloorGuardian;
        [SerializeField] private int targetFloor = 1;
        [SerializeField] private bool isUniqueBoss = false;
        
        // Public 프로퍼티
        public BossType BossType => bossType;
        public int TargetFloor => targetFloor;
        
        [Header("보스 스킬 설정")]
        [SerializeField] private float skillCooldown = 5f;
        [SerializeField] private float ultimateSkillCooldown = 15f;
        [SerializeField] private int phaseCount = 3;
        
        [Header("보스 패턴 설정")]
        [SerializeField] private float enrageHealthThreshold = 0.3f; // 30% 이하에서 광폭화
        [SerializeField] private float summonHealthThreshold = 0.5f; // 50% 이하에서 소환
        [SerializeField] private int maxSummonedMinions = 3;
        
        [Header("보스 시각 효과")]
        [SerializeField] private GameObject bossAuraEffect;
        [SerializeField] private Color bossGlowColor = Color.red;
        [SerializeField] private float pulseSpeed = 2f;
        
        // 보스 상태
        private BossState currentBossState = BossState.Normal;
        private int currentPhase = 1;
        private float lastSkillTime = 0f;
        private float lastUltimateTime = 0f;
        private bool hasEnraged = false;
        private bool hasSummoned = false;
        
        // 스킬 패턴
        private List<BossSkill> availableSkills = new List<BossSkill>();
        private List<GameObject> summonedMinions = new List<GameObject>();
        
        // 네트워크 동기화
        private NetworkVariable<BossState> networkBossState = new NetworkVariable<BossState>(
            BossState.Normal, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> networkPhase = new NetworkVariable<int>(
            1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // 보스 전용 초기화
            InitializeBossData();
            SetupBossVisuals();
            
            // 네트워크 이벤트 구독
            if (!IsServer)
            {
                networkBossState.OnValueChanged += OnBossStateChanged;
                networkPhase.OnValueChanged += OnPhaseChanged;
            }
            
            Debug.Log($"🐉 Boss Monster spawned: {bossType} on Floor {targetFloor}");
        }
        
        public override void OnNetworkDespawn()
        {
            if (!IsServer)
            {
                networkBossState.OnValueChanged -= OnBossStateChanged;
                networkPhase.OnValueChanged -= OnPhaseChanged;
            }
            
            // 소환된 미니언들 정리
            CleanupMinions();
            
            base.OnNetworkDespawn();
        }
        
        /// <summary>
        /// 보스 데이터 초기화
        /// </summary>
        private void InitializeBossData()
        {
            // 보스 타입별 설정
            switch (bossType)
            {
                case BossType.FloorGuardian:
                    SetupFloorGuardian();
                    break;
                case BossType.EliteBoss:
                    SetupEliteBoss();
                    break;
                case BossType.FinalBoss:
                    SetupFinalBoss();
                    break;
                case BossType.HiddenBoss:
                    SetupHiddenBoss();
                    break;
            }
            
            // 보스 스킬 초기화
            InitializeBossSkills();
        }
        
        /// <summary>
        /// 층 수호자 설정
        /// </summary>
        private void SetupFloorGuardian()
        {
            phaseCount = 2;
            skillCooldown = 6f;
            ultimateSkillCooldown = 20f;
            maxSummonedMinions = 2;
        }
        
        /// <summary>
        /// 엘리트 보스 설정
        /// </summary>
        private void SetupEliteBoss()
        {
            phaseCount = 3;
            skillCooldown = 4f;
            ultimateSkillCooldown = 15f;
            maxSummonedMinions = 3;
        }
        
        /// <summary>
        /// 최종 보스 설정
        /// </summary>
        private void SetupFinalBoss()
        {
            phaseCount = 5;
            skillCooldown = 3f;
            ultimateSkillCooldown = 10f;
            maxSummonedMinions = 5;
            isUniqueBoss = true;
        }
        
        /// <summary>
        /// 히든 보스 설정
        /// </summary>
        private void SetupHiddenBoss()
        {
            phaseCount = 4;
            skillCooldown = 2f;
            ultimateSkillCooldown = 8f;
            maxSummonedMinions = 4;
            isUniqueBoss = true;
        }
        
        /// <summary>
        /// 보스 스킬 초기화
        /// </summary>
        private void InitializeBossSkills()
        {
            availableSkills.Clear();
            
            // 기본 스킬들
            availableSkills.Add(new BossSkill
            {
                skillId = "charge_attack",
                skillName = "돌진 공격",
                damage = attackDamage * 2f,
                range = attackRange * 3f,
                cooldown = 8f,
                skillType = BossSkillType.Offensive
            });
            
            availableSkills.Add(new BossSkill
            {
                skillId = "area_slam",
                skillName = "광역 강타",
                damage = attackDamage * 1.5f,
                range = 3f,
                cooldown = 10f,
                skillType = BossSkillType.AOE
            });
            
            availableSkills.Add(new BossSkill
            {
                skillId = "summon_minions",
                skillName = "미니언 소환",
                damage = 0f,
                range = 5f,
                cooldown = 25f,
                skillType = BossSkillType.Summon
            });
            
            if (bossType == BossType.FinalBoss || bossType == BossType.HiddenBoss)
            {
                // 고급 스킬들 (최종/히든 보스만)
                availableSkills.Add(new BossSkill
                {
                    skillId = "meteor_strike",
                    skillName = "운석 낙하",
                    damage = attackDamage * 5f,
                    range = 8f,
                    cooldown = 30f,
                    skillType = BossSkillType.Ultimate
                });
            }
        }
        
        /// <summary>
        /// 보스 시각 효과 설정
        /// </summary>
        private void SetupBossVisuals()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                // 보스 크기 증가
                float sizeMultiplier = 1.5f + (targetFloor * 0.1f);
                transform.localScale = Vector3.one * sizeMultiplier;
                
                // 보스 발광 효과 시작
                StartCoroutine(BossGlowEffect(spriteRenderer));
            }
            
            // 오라 이펙트 활성화
            if (bossAuraEffect != null)
            {
                bossAuraEffect.SetActive(true);
            }
        }
        
        /// <summary>
        /// 보스 AI 업데이트 (MonsterAI 확장)
        /// </summary>
        private void Update()
        {
            // 기본 AI 로직 실행
            base.Update();
            
            if (!IsServer) return;
            
            // 보스 전용 로직
            UpdateBossLogic();
        }
        
        /// <summary>
        /// 보스 전용 로직 업데이트
        /// </summary>
        private void UpdateBossLogic()
        {
            // 체력 기반 페이즈 체크
            CheckPhaseTransition();
            
            // 보스 스킬 사용
            UpdateBossSkills();
            
            // 상태별 특수 행동
            switch (currentBossState)
            {
                case BossState.Enraged:
                    UpdateEnragedBehavior();
                    break;
                case BossState.Summoning:
                    UpdateSummoningBehavior();
                    break;
                case BossState.Ultimate:
                    UpdateUltimateBehavior();
                    break;
            }
        }
        
        /// <summary>
        /// 페이즈 전환 체크
        /// </summary>
        private void CheckPhaseTransition()
        {
            var monsterHealth = GetComponent<MonsterHealth>();
            if (monsterHealth == null) return;
            
            float healthPercent = (float)monsterHealth.CurrentHealth / monsterHealth.MaxHealth;
            int expectedPhase = Mathf.FloorToInt((1f - healthPercent) * phaseCount) + 1;
            expectedPhase = Mathf.Clamp(expectedPhase, 1, phaseCount);
            
            if (expectedPhase != currentPhase)
            {
                TransitionToPhase(expectedPhase);
            }
            
            // 특수 상태 전환
            if (!hasEnraged && healthPercent <= enrageHealthThreshold)
            {
                TriggerEnrage();
            }
            
            if (!hasSummoned && healthPercent <= summonHealthThreshold)
            {
                TriggerSummonPhase();
            }
        }
        
        /// <summary>
        /// 페이즈 전환
        /// </summary>
        private void TransitionToPhase(int newPhase)
        {
            int previousPhase = currentPhase;
            currentPhase = newPhase;
            networkPhase.Value = newPhase;
            
            // 페이즈별 특수 효과
            OnPhaseEnter(newPhase, previousPhase);
            
            Debug.Log($"🐉 Boss Phase Transition: {previousPhase} → {newPhase}");
        }
        
        /// <summary>
        /// 페이즈 진입 처리
        /// </summary>
        private void OnPhaseEnter(int newPhase, int previousPhase)
        {
            switch (newPhase)
            {
                case 2:
                    // 공격 속도 증가
                    attackCooldown *= 0.8f;
                    break;
                case 3:
                    // 이동 속도 증가
                    moveSpeed *= 1.2f;
                    break;
                case 4:
                    // 스킬 쿨다운 감소
                    skillCooldown *= 0.7f;
                    break;
                case 5:
                    // 최종 페이즈 - 모든 능력 강화
                    attackDamage *= 1.5f;
                    moveSpeed *= 1.3f;
                    break;
            }
            
            // 페이즈 전환 이펙트
            ShowPhaseTransitionEffectClientRpc(newPhase);
        }
        
        /// <summary>
        /// 광폭화 상태 활성화
        /// </summary>
        private void TriggerEnrage()
        {
            hasEnraged = true;
            ChangeBossState(BossState.Enraged);
            
            // 광폭화 효과
            attackDamage *= 1.3f;
            moveSpeed *= 1.2f;
            attackCooldown *= 0.5f;
            
            Debug.Log($"🔥 Boss {name} has enraged!");
        }
        
        /// <summary>
        /// 소환 페이즈 활성화
        /// </summary>
        private void TriggerSummonPhase()
        {
            hasSummoned = true;
            ChangeBossState(BossState.Summoning);
            
            // 미니언 소환
            StartCoroutine(SummonMinionsCoroutine());
        }
        
        /// <summary>
        /// 보스 상태 변경
        /// </summary>
        private void ChangeBossState(BossState newState)
        {
            if (currentBossState == newState) return;
            
            BossState previousState = currentBossState;
            currentBossState = newState;
            networkBossState.Value = newState;
            
            Debug.Log($"🐉 Boss State: {previousState} → {newState}");
        }
        
        /// <summary>
        /// 보스 스킬 업데이트
        /// </summary>
        private void UpdateBossSkills()
        {
            if (CurrentTarget == null) return;
            
            // 일반 스킬 사용
            if (Time.time >= lastSkillTime + skillCooldown)
            {
                TryUseRandomSkill();
            }
            
            // 궁극기 사용
            if (Time.time >= lastUltimateTime + ultimateSkillCooldown)
            {
                TryUseUltimateSkill();
            }
        }
        
        /// <summary>
        /// 랜덤 스킬 사용 시도
        /// </summary>
        private void TryUseRandomSkill()
        {
            var usableSkills = availableSkills.FindAll(skill => 
                skill.skillType != BossSkillType.Ultimate && 
                Vector3.Distance(transform.position, CurrentTarget.transform.position) <= skill.range);
            
            if (usableSkills.Count > 0)
            {
                var selectedSkill = usableSkills[Random.Range(0, usableSkills.Count)];
                ExecuteBossSkill(selectedSkill);
                lastSkillTime = Time.time;
            }
        }
        
        /// <summary>
        /// 궁극기 사용 시도
        /// </summary>
        private void TryUseUltimateSkill()
        {
            var ultimateSkills = availableSkills.FindAll(skill => skill.skillType == BossSkillType.Ultimate);
            
            if (ultimateSkills.Count > 0)
            {
                var selectedSkill = ultimateSkills[Random.Range(0, ultimateSkills.Count)];
                ExecuteBossSkill(selectedSkill);
                lastUltimateTime = Time.time;
                
                ChangeBossState(BossState.Ultimate);
                StartCoroutine(ResetBossStateAfterDelay(3f));
            }
        }
        
        /// <summary>
        /// 보스 스킬 실행
        /// </summary>
        private void ExecuteBossSkill(BossSkill skill)
        {
            switch (skill.skillId)
            {
                case "charge_attack":
                    ExecuteChargeAttack(skill);
                    break;
                case "area_slam":
                    ExecuteAreaSlam(skill);
                    break;
                case "summon_minions":
                    ExecuteSummonMinions(skill);
                    break;
                case "meteor_strike":
                    ExecuteMeteorStrike(skill);
                    break;
            }
            
            Debug.Log($"🐉 Boss used skill: {skill.skillName}");
        }
        
        /// <summary>
        /// 돌진 공격
        /// </summary>
        private void ExecuteChargeAttack(BossSkill skill)
        {
            if (CurrentTarget == null) return;
            
            Vector3 targetPosition = CurrentTarget.transform.position;
            StartCoroutine(ChargeAttackCoroutine(targetPosition, skill.damage));
        }
        
        /// <summary>
        /// 광역 강타
        /// </summary>
        private void ExecuteAreaSlam(BossSkill skill)
        {
            Collider2D[] nearbyTargets = Physics2D.OverlapCircleAll(transform.position, skill.range);
            
            foreach (var collider in nearbyTargets)
            {
                var player = collider.GetComponent<PlayerController>();
                if (player != null && player.IsOwner)
                {
                    var statsManager = player.GetComponent<PlayerStatsManager>();
                    if (statsManager != null && !statsManager.IsDead)
                    {
                        statsManager.TakeDamage(skill.damage, DamageType.Physical);
                    }
                }
            }
            
            // 이펙트 표시
            ShowAreaSlamEffectClientRpc(transform.position, skill.range);
        }
        
        /// <summary>
        /// 미니언 소환
        /// </summary>
        private void ExecuteSummonMinions(BossSkill skill)
        {
            StartCoroutine(SummonMinionsCoroutine());
        }
        
        /// <summary>
        /// 운석 낙하
        /// </summary>
        private void ExecuteMeteorStrike(BossSkill skill)
        {
            if (CurrentTarget == null) return;
            
            Vector3 targetPosition = CurrentTarget.transform.position;
            StartCoroutine(MeteorStrikeCoroutine(targetPosition, skill.damage, skill.range));
        }
        
        /// <summary>
        /// 돌진 공격 코루틴
        /// </summary>
        private IEnumerator ChargeAttackCoroutine(Vector3 targetPosition, float damage)
        {
            Vector3 startPosition = transform.position;
            float duration = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
                yield return null;
            }
            
            // 도착 시 데미지
            Collider2D[] hitTargets = Physics2D.OverlapCircleAll(transform.position, 2f);
            foreach (var collider in hitTargets)
            {
                var player = collider.GetComponent<PlayerController>();
                if (player != null && player.IsOwner)
                {
                    var statsManager = player.GetComponent<PlayerStatsManager>();
                    if (statsManager != null && !statsManager.IsDead)
                    {
                        statsManager.TakeDamage(damage, DamageType.Physical);
                    }
                }
            }
            
            ShowChargeAttackEffectClientRpc(transform.position);
        }
        
        /// <summary>
        /// 미니언 소환 코루틴
        /// </summary>
        private IEnumerator SummonMinionsCoroutine()
        {
            int minionsToSummon = Mathf.Min(maxSummonedMinions - summonedMinions.Count, 2);
            
            for (int i = 0; i < minionsToSummon; i++)
            {
                Vector3 spawnPosition = transform.position + Random.insideUnitSphere * 3f;
                spawnPosition.z = 0f;
                
                // 미니언 스폰 (실제로는 MonsterSpawner 사용)
                SpawnMinionClientRpc(spawnPosition);
                
                yield return new WaitForSeconds(1f);
            }
        }
        
        /// <summary>
        /// 운석 낙하 코루틴
        /// </summary>
        private IEnumerator MeteorStrikeCoroutine(Vector3 targetPosition, float damage, float range)
        {
            // 2초 후 운석 낙하
            ShowMeteorWarningClientRpc(targetPosition, range);
            yield return new WaitForSeconds(2f);
            
            // 데미지 적용
            Collider2D[] hitTargets = Physics2D.OverlapCircleAll(targetPosition, range);
            foreach (var collider in hitTargets)
            {
                var player = collider.GetComponent<PlayerController>();
                if (player != null && player.IsOwner)
                {
                    var statsManager = player.GetComponent<PlayerStatsManager>();
                    if (statsManager != null && !statsManager.IsDead)
                    {
                        statsManager.TakeDamage(damage, DamageType.Magical);
                    }
                }
            }
            
            ShowMeteorImpactEffectClientRpc(targetPosition, range);
        }
        
        /// <summary>
        /// 광폭화 상태 업데이트
        /// </summary>
        private void UpdateEnragedBehavior()
        {
            // 광폭화 상태에서는 더 공격적으로 행동
            if (CurrentTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, CurrentTarget.transform.position);
                if (distanceToTarget > attackRange * 1.5f)
                {
                    // 더 빠르게 접근
                    MoveTowards(CurrentTarget.transform.position, moveSpeed * 1.5f);
                }
            }
        }
        
        /// <summary>
        /// 소환 상태 업데이트
        /// </summary>
        private void UpdateSummoningBehavior()
        {
            // 소환 중에는 방어적으로 행동
            // 일정 시간 후 정상 상태로 복귀
            if (Time.time >= lastSkillTime + 5f)
            {
                ChangeBossState(BossState.Normal);
            }
        }
        
        /// <summary>
        /// 궁극기 상태 업데이트
        /// </summary>
        private void UpdateUltimateBehavior()
        {
            // 궁극기 사용 중에는 이동 제한
            if (GetComponent<Rigidbody2D>() != null)
            {
                GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            }
        }
        
        /// <summary>
        /// 일정 시간 후 보스 상태 리셋
        /// </summary>
        private IEnumerator ResetBossStateAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ChangeBossState(BossState.Normal);
        }
        
        /// <summary>
        /// 보스 발광 효과
        /// </summary>
        private IEnumerator BossGlowEffect(SpriteRenderer spriteRenderer)
        {
            Color originalColor = spriteRenderer.color;
            
            while (this != null && spriteRenderer != null)
            {
                float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.3f + 0.7f;
                Color glowColor = Color.Lerp(originalColor, bossGlowColor, pulse * 0.5f);
                spriteRenderer.color = glowColor;
                
                yield return null;
            }
        }
        
        /// <summary>
        /// 소환된 미니언들 정리
        /// </summary>
        private void CleanupMinions()
        {
            foreach (var minion in summonedMinions)
            {
                if (minion != null)
                {
                    Destroy(minion);
                }
            }
            summonedMinions.Clear();
        }
        
        // 네트워크 이벤트 처리
        private void OnBossStateChanged(BossState previousValue, BossState newValue)
        {
            if (IsServer) return;
            currentBossState = newValue;
            Debug.Log($"🐉 Boss state changed to {newValue}");
        }
        
        private void OnPhaseChanged(int previousValue, int newValue)
        {
            if (IsServer) return;
            currentPhase = newValue;
            Debug.Log($"🐉 Boss phase changed to {newValue}");
        }
        
        // ClientRpc 메서드들
        [ClientRpc]
        private void ShowPhaseTransitionEffectClientRpc(int newPhase)
        {
            Debug.Log($"🌟 Boss Phase {newPhase} transition effect!");
            // 실제 이펙트는 이펙트 시스템에서 구현
        }
        
        [ClientRpc]
        private void ShowAreaSlamEffectClientRpc(Vector3 position, float range)
        {
            Debug.Log($"💥 Area Slam effect at {position} with range {range}!");
        }
        
        [ClientRpc]
        private void ShowChargeAttackEffectClientRpc(Vector3 position)
        {
            Debug.Log($"⚡ Charge Attack effect at {position}!");
        }
        
        [ClientRpc]
        private void SpawnMinionClientRpc(Vector3 position)
        {
            Debug.Log($"👹 Minion spawned at {position}!");
            // 실제 미니언 스폰은 MonsterSpawner에서 처리
        }
        
        [ClientRpc]
        private void ShowMeteorWarningClientRpc(Vector3 targetPosition, float range)
        {
            Debug.Log($"⚠️ Meteor incoming at {targetPosition}!");
        }
        
        [ClientRpc]
        private void ShowMeteorImpactEffectClientRpc(Vector3 position, float range)
        {
            Debug.Log($"☄️ Meteor impact at {position}!");
        }
        
        /// <summary>
        /// 보스 타입 설정 (외부에서 호출)
        /// </summary>
        public void SetBossType(BossType type)
        {
            bossType = type;
            InitializeBossData();
        }
        
        /// <summary>
        /// 타겟 층 설정 (외부에서 호출)
        /// </summary>
        public void SetTargetFloor(int floor)
        {
            targetFloor = floor;
        }
        
        /// <summary>
        /// 보스 상태 가져오기
        /// </summary>
        public BossState GetBossState()
        {
            return currentBossState;
        }
        
        /// <summary>
        /// 현재 페이즈 가져오기
        /// </summary>
        public int GetCurrentPhase()
        {
            return currentPhase;
        }
        
        /// <summary>
        /// 디버그 기즈모 (MonsterAI 확장)
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            // 보스 전용 기즈모
            if (availableSkills != null)
            {
                foreach (var skill in availableSkills)
                {
                    if (skill.skillType == BossSkillType.AOE)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(transform.position, skill.range);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 보스 타입
    /// </summary>
    public enum BossType
    {
        FloorGuardian,  // 층 수호자 (일반 보스)
        EliteBoss,      // 엘리트 보스 (중간 보스)
        FinalBoss,      // 최종 보스 (10층)
        HiddenBoss      // 히든 보스 (11층)
    }
    
    /// <summary>
    /// 보스 상태
    /// </summary>
    public enum BossState
    {
        Normal,         // 일반 상태
        Enraged,        // 광폭화 상태
        Summoning,      // 소환 상태
        Ultimate        // 궁극기 사용 상태
    }
    
    /// <summary>
    /// 보스 스킬 타입
    /// </summary>
    public enum BossSkillType
    {
        Offensive,      // 단일 공격
        AOE,           // 광역 공격
        Summon,        // 소환
        Ultimate,      // 궁극기
        Buff,          // 버프
        Debuff         // 디버프
    }
    
    /// <summary>
    /// 보스 스킬 데이터
    /// </summary>
    [System.Serializable]
    public struct BossSkill
    {
        public string skillId;
        public string skillName;
        public float damage;
        public float range;
        public float cooldown;
        public BossSkillType skillType;
    }
}