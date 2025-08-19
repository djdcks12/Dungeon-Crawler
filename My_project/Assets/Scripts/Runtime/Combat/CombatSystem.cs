using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 기본 전투 시스템
    /// 공격 판정, 데미지 계산, 타겟 감지 처리
    /// </summary>
    public class CombatSystem : NetworkBehaviour
    {
        [Header("Combat Settings")]
        [SerializeField] private LayerMask enemyLayerMask = 1; // Enemy layer
        [SerializeField] private LayerMask playerLayerMask = 1 << 6; // Player layer
        [SerializeField] private bool enablePvP = true;
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private GameObject criticalHitEffectPrefab;
        [SerializeField] private GameObject missEffectPrefab;
        
        // 컴포넌트 참조
        private PlayerController playerController;
        private PlayerStatsManager statsManager;
        private EnchantManager enchantManager;
        
        // 공격 상태
        private bool isAttacking = false;
        private float attackStartTime;
        private Vector2 attackDirection;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            playerController = GetComponent<PlayerController>();
            statsManager = GetComponent<PlayerStatsManager>();
            enchantManager = GetComponent<EnchantManager>();
        }
        
        /// <summary>
        /// 기본 공격 실행 (PlayerController에서 호출)
        /// </summary>
        public void PerformBasicAttack()
        {
            if (!IsOwner || isAttacking) return;
            
            // 공격 방향 설정 (플레이어가 바라보는 방향)
            attackDirection = transform.up; // 플레이어의 forward 방향
            
            // 서버에서 공격 처리
            PerformAttackServerRpc(transform.position, attackDirection);
        }
        
        /// <summary>
        /// 서버에서 공격 처리
        /// </summary>
        [ServerRpc]
        private void PerformAttackServerRpc(Vector2 attackPosition, Vector2 attackDirection, ServerRpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            
            // 공격 범위 내 타겟 감지
            var targets = DetectTargetsInRange(attackPosition, attackDirection);
            
            foreach (var target in targets)
            {
                if (target != null && IsValidTarget(target, clientId))
                {
                    ProcessAttackOnTarget(target, attackPosition);
                }
            }
            
            // 클라이언트에 공격 이펙트 표시
            PlayAttackEffectClientRpc(attackPosition, attackDirection);
        }
        
        /// <summary>
        /// 공격 범위 내 타겟 감지
        /// </summary>
        private List<Collider2D> DetectTargetsInRange(Vector2 attackPosition, Vector2 attackDirection)
        {
            var targets = new List<Collider2D>();
            
            // 현재 스탯에서 공격 사거리 가져오기
            float attackRange = 2.0f; // 기본값
            if (statsManager?.CurrentStats != null)
            {
                // 무기나 스탯에서 사거리 가져올 수 있음
                attackRange = 2.0f; // 임시로 고정값 사용
            }
            
            // 원형 범위로 타겟 감지
            var colliders = Physics2D.OverlapCircleAll(attackPosition, attackRange, enemyLayerMask | playerLayerMask);
            
            foreach (var collider in colliders)
            {
                // 자기 자신은 제외
                if (collider.transform == transform) continue;
                
                targets.Add(collider);
            }
            
            return targets;
        }
        
        /// <summary>
        /// 유효한 타겟인지 확인
        /// </summary>
        private bool IsValidTarget(Collider2D target, ulong attackerClientId)
        {
            // 몬스터는 항상 공격 가능
            if ((enemyLayerMask.value & (1 << target.gameObject.layer)) != 0)
            {
                return true;
            }
            
            // 플레이어 타겟 확인
            if ((playerLayerMask.value & (1 << target.gameObject.layer)) != 0)
            {
                // PvP가 비활성화되어 있으면 플레이어 공격 불가
                if (!enablePvP) return false;
                
                var targetNetworkBehaviour = target.GetComponent<NetworkBehaviour>();
                if (targetNetworkBehaviour != null)
                {
                    // 자기 자신 공격 방지
                    if (targetNetworkBehaviour.OwnerClientId == attackerClientId) return false;
                    
                    // 파티원 공격 방지 (추후 파티 시스템 구현 시 추가)
                    // if (IsPartyMember(attackerClientId, targetNetworkBehaviour.OwnerClientId)) return false;
                }
                
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 타겟에 대한 공격 처리
        /// </summary>
        private void ProcessAttackOnTarget(Collider2D target, Vector2 attackPosition)
        {
            // 스탯 매니저에서 공격력 가져오기
            float attackDamage = 10f; // 기본값
            DamageType damageType = DamageType.Physical;
            bool isCritical = false;
            
            if (statsManager?.CurrentStats != null)
            {
                var stats = statsManager.CurrentStats;
                
                // 민댐/맥댐 시스템으로 데미지 계산
                attackDamage = stats.CalculateAttackDamage(DamageType.Physical);
                
                // 인챈트 효과 적용
                if (enchantManager != null)
                {
                    // 예리함 인챈트 - 공격력 증가
                    float sharpnessBonus = enchantManager.GetEnchantEffect(EnchantType.Sharpness);
                    if (sharpnessBonus > 0)
                    {
                        attackDamage *= (1f + sharpnessBonus / 100f);
                    }
                    
                    // 치명타 인챈트 - 치명타 확률 증가
                    float criticalBonus = enchantManager.GetEnchantEffect(EnchantType.CriticalHit);
                    float baseCritChance = stats.CriticalChance + (criticalBonus / 100f);
                    
                    if (Random.value < baseCritChance)
                    {
                        isCritical = true;
                        attackDamage *= stats.CriticalDamage;
                    }
                }
                else
                {
                    // 인챈트 매니저가 없을 때 기본 치명타 판정
                    float baseDamage = (stats.CombatStats.physicalDamage.minDamage + stats.CombatStats.physicalDamage.maxDamage) * 0.5f;
                    isCritical = attackDamage > baseDamage * 1.5f;
                }
            }
            
            // 타겟이 플레이어인 경우
            var targetStatsManager = target.GetComponent<PlayerStatsManager>();
            if (targetStatsManager != null)
            {
                ApplyDamageToPlayer(targetStatsManager, attackDamage, damageType, isCritical, attackPosition);
                return;
            }
            
            // 타겟이 몬스터인 경우 (추후 몬스터 시스템 구현 시)
            var targetMonster = target.GetComponent<MonsterHealth>();
            if (targetMonster != null)
            {
                ApplyDamageToMonster(targetMonster, attackDamage, damageType, isCritical, attackPosition);
                return;
            }
            
            // 추후 몬스터 시스템과 연동할 예정
            
            Debug.LogWarning($"Unknown target type: {target.name}");
        }
        
        /// <summary>
        /// 플레이어에게 데미지 적용
        /// </summary>
        private void ApplyDamageToPlayer(PlayerStatsManager targetStatsManager, float damage, DamageType damageType, bool isCritical, Vector2 attackPosition)
        {
            // 실제 데미지 적용 (방어력 계산 포함)
            float actualDamage = targetStatsManager.TakeDamage(damage, damageType);
            
            // 데미지 로그
            string critText = isCritical ? " (CRITICAL)" : "";
            Debug.Log($"{name} dealt {actualDamage:F1} {damageType} damage to {targetStatsManager.name}{critText}");
            
            // 피격 이펙트 표시
            Vector2 hitPosition = targetStatsManager.transform.position;
            ShowDamageEffectClientRpc(hitPosition, actualDamage, isCritical, damageType);
            
            // 경험치 획득 (타겟이 죽었을 경우)
            if (targetStatsManager.IsDead)
            {
                var killerStatsManager = GetComponent<PlayerStatsManager>();
                if (killerStatsManager != null)
                {
                    // PvP 킬 경험치: 상대방 레벨 * 100
                    long expGain = targetStatsManager.CurrentStats.CurrentLevel * 100;
                    killerStatsManager.AddExperience(expGain);
                    
                    Debug.Log($"{name} killed {targetStatsManager.name} and gained {expGain} experience!");
                }
            }
        }
        
        /// <summary>
        /// 몬스터에게 데미지 적용
        /// </summary>
        private void ApplyDamageToMonster(MonsterHealth targetMonster, float damage, DamageType damageType, bool isCritical, Vector2 attackPosition)
        {
            targetMonster.TakeDamage(damage, playerController);
            
            // 인챈트 효과 적용
            if (enchantManager != null && statsManager != null)
            {
                // 흡혈 인챈트 - 가한 데미지의 일정 비율만큼 체력 회복
                float lifeStealBonus = enchantManager.GetEnchantEffect(EnchantType.LifeSteal);
                if (lifeStealBonus > 0)
                {
                    float healAmount = damage * (lifeStealBonus / 100f);
                    statsManager.Heal(healAmount);
                    Debug.Log($"💚 Life steal: Healed {healAmount:F1} HP ({lifeStealBonus}%)");
                }
            }
            
            string critText = isCritical ? " (CRITICAL)" : "";
            Debug.Log($"Hit monster {targetMonster.name} for {damage:F1} damage{critText}");
            
            // 피격 이펙트 표시
            ShowDamageEffectClientRpc(attackPosition, damage, isCritical, damageType);
        }
        
        
        /// <summary>
        /// 공격 이펙트 재생
        /// </summary>
        [ClientRpc]
        private void PlayAttackEffectClientRpc(Vector2 attackPosition, Vector2 attackDirection)
        {
            // 공격 이펙트 재생 (파티클, 사운드 등)
            Debug.Log($"Attack effect at {attackPosition} in direction {attackDirection}");
            
            // 실제 이펙트 재생 코드
            if (hitEffectPrefab != null)
            {
                var effect = Instantiate(hitEffectPrefab, attackPosition, Quaternion.LookRotation(Vector3.forward, attackDirection));
                Destroy(effect, 2f);
            }
        }
        
        /// <summary>
        /// 데미지 이펙트 표시
        /// </summary>
        [ClientRpc]
        private void ShowDamageEffectClientRpc(Vector2 hitPosition, float damage, bool isCritical, DamageType damageType)
        {
            // 데미지 텍스트 표시
            string damageText = $"{damage:F0}";
            Color textColor = damageType == DamageType.Physical ? Color.white : Color.cyan;
            
            if (isCritical)
            {
                damageText = $"CRIT! {damageText}";
                textColor = Color.red;
                
                // 치명타 이펙트
                if (criticalHitEffectPrefab != null)
                {
                    var critEffect = Instantiate(criticalHitEffectPrefab, hitPosition, Quaternion.identity);
                    Destroy(critEffect, 1.5f);
                }
            }
            
            Debug.Log($"Damage Effect: {damageText} at {hitPosition}");
            
            // 실제 UI 데미지 텍스트 표시는 추후 UI 시스템에서 구현
        }
        
        /// <summary>
        /// 스킬 공격 (추후 스킬 시스템과 연동)
        /// </summary>
        public void PerformSkillAttack(string skillId, Vector2 targetPosition)
        {
            if (!IsOwner) return;
            
            // 스킬별 데미지 계산 (추후 스킬 시스템에서 구현)
            float minPercent = 80f; // 스킬별 설정값
            float maxPercent = 200f; // 스킬별 설정값
            DamageType skillType = DamageType.Physical; // 스킬별 설정값
            
            if (statsManager?.CurrentStats != null)
            {
                float skillDamage = statsManager.CurrentStats.CalculateSkillDamage(minPercent, maxPercent, skillType);
                Debug.Log($"Skill {skillId} would deal {skillDamage:F1} damage");
            }
            
            // 서버에서 스킬 공격 처리
            PerformSkillAttackServerRpc(skillId, targetPosition);
        }
        
        [ServerRpc]
        private void PerformSkillAttackServerRpc(string skillId, Vector2 targetPosition)
        {
            // 추후 스킬 시스템 구현 시 작성
            Debug.Log($"Skill attack: {skillId} at {targetPosition}");
        }
        
        /// <summary>
        /// 디버그용 공격 범위 시각화
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // 공격 범위 시각화
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 2.0f);
            
            // 공격 방향 시각화
            if (isAttacking)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)attackDirection * 2.0f);
            }
        }
    }
    
    /// <summary>
    /// 몬스터 체력 관리 - 아이템 드롭 시스템 연동
    /// </summary>
    public class MonsterHealth : MonoBehaviour
    {
        [Header("몬스터 정보")]
        [SerializeField] private string monsterName = "몬스터";
        [SerializeField] private int monsterLevel = 1;
        [SerializeField] private string monsterType = "Basic";
        
        [Header("체력")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        
        [Header("경험치 보상")]
        [SerializeField] private long expReward = 50;
        
        // 공격자 추적 (마지막으로 공격한 플레이어)
        private PlayerController lastAttacker;
        
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0;
        
        private void Start()
        {
            currentHealth = maxHealth;
        }
        
        public void TakeDamage(float damage, PlayerController attacker = null)
        {
            if (IsDead) return;
            
            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);
            
            // 공격자 추적
            if (attacker != null)
            {
                lastAttacker = attacker;
            }
            
            Debug.Log($"Monster {monsterName} took {damage} damage. HP: {currentHealth}/{maxHealth}");
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        
        private void Die()
        {
            Debug.Log($"💀 Monster {monsterName} (Level {monsterLevel}) died!");
            
            // 경험치 보상
            if (lastAttacker != null)
            {
                GiveExperienceReward();
                TriggerItemDrop();
                TriggerSoulDrop();
                TriggerEnchantDrop();
                
                // 던전 시스템에 몬스터 처치 알림
                NotifyDungeonManager();
            }
            
            // 몬스터 오브젝트 제거
            Destroy(gameObject);
        }
        
        /// <summary>
        /// 경험치 보상
        /// </summary>
        private void GiveExperienceReward()
        {
            var attackerStats = lastAttacker.GetComponent<PlayerStatsManager>();
            if (attackerStats != null)
            {
                // 몬스터 레벨에 따른 경험치 계산
                long finalExpReward = expReward + (monsterLevel * 25);
                attackerStats.AddExperience(finalExpReward);
                
                Debug.Log($"🌟 {lastAttacker.name} gained {finalExpReward} experience from {monsterName}!");
            }
        }
        
        /// <summary>
        /// 아이템 드롭 트리거
        /// </summary>
        private void TriggerItemDrop()
        {
            var itemDropSystem = lastAttacker.GetComponent<ItemDropSystem>();
            if (itemDropSystem != null)
            {
                itemDropSystem.CheckItemDrop(transform.position, monsterLevel, monsterType, lastAttacker);
            }
        }
        
        /// <summary>
        /// 영혼 드롭 트리거 (0.1% 확률)
        /// </summary>
        private void TriggerSoulDrop()
        {
            var soulDropSystem = lastAttacker.GetComponent<SoulDropSystem>();
            if (soulDropSystem != null)
            {
                soulDropSystem.CheckSoulDrop(transform.position, monsterLevel, monsterName);
            }
        }
        
        /// <summary>
        /// 인챈트 북 드롭 트리거 (1% 확률)
        /// </summary>
        private void TriggerEnchantDrop()
        {
            var enchantDropSystem = FindObjectOfType<EnchantDropSystem>();
            if (enchantDropSystem != null)
            {
                enchantDropSystem.CheckEnchantDrop(transform.position, monsterLevel, monsterName, lastAttacker);
            }
        }
        
        /// <summary>
        /// 몬스터 정보 설정 (동적 생성 시 사용)
        /// </summary>
        public void SetMonsterInfo(string name, int level, string type, float health, long exp)
        {
            monsterName = name;
            monsterLevel = level;
            monsterType = type;
            maxHealth = health;
            currentHealth = health;
            expReward = exp;
        }
        
        /// <summary>
        /// 체력 회복
        /// </summary>
        public void Heal(float amount)
        {
            if (IsDead) return;
            
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }
        
        /// <summary>
        /// 체력 비율
        /// </summary>
        public float GetHealthPercentage()
        {
            return maxHealth > 0 ? currentHealth / maxHealth : 0f;
        }
        
        /// <summary>
        /// 던전 매니저에게 몬스터 처치 알림
        /// </summary>
        private void NotifyDungeonManager()
        {
            // 던전이 활성화된 상태에서만 알림
            var dungeonManager = FindObjectOfType<DungeonManager>();
            if (dungeonManager != null && dungeonManager.IsActive && lastAttacker != null)
            {
                // 공격자의 클라이언트 ID 가져오기
                var playerNetwork = lastAttacker.GetComponent<NetworkBehaviour>();
                if (playerNetwork != null)
                {
                    dungeonManager.OnMonsterKilled(playerNetwork.OwnerClientId);
                    Debug.Log($"🏰 Notified DungeonManager: {monsterName} killed by client {playerNetwork.OwnerClientId}");
                }
            }
        }
    }
}