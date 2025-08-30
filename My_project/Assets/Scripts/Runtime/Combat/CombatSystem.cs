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
        [SerializeField] private LayerMask enemyLayerMask = 1; // Default layer (0)
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
        /// 기본 공격 실행 (클라이언트/서버 공통 진입점)
        /// </summary>
        public void PerformBasicAttack()
        {
            if (!IsOwner || isAttacking) return;
            
            // 가장 가까운 적을 찾아서 공격 (마우스 방향 대신)
            var nearestEnemy = FindNearestEnemy();
            if (nearestEnemy != null)
            {
                // 적 방향으로 공격
                attackDirection = (nearestEnemy.transform.position - transform.position).normalized;
            }
            else
            {
                // 적이 없으면 마우스 방향으로 공격
                attackDirection = playerController.GetMouseDirection();
            }
            
            Vector2 attackPosition = transform.position;
            
            // 서버/클라이언트 분기
            if (!IsServer)
            {
                PerformAttackServerRpc(attackPosition, attackDirection);
                return;
            }
            
            // 서버에서 직접 처리
            ProcessBasicAttack(attackPosition, attackDirection);
        }
        
        /// <summary>
        /// 가장 가까운 적 찾기
        /// </summary>
        private Collider2D FindNearestEnemy()
        {
            float attackRange = 3.0f; // 공격 가능 범위
            var nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayerMask);
            
            Collider2D nearestEnemy = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var enemy in nearbyEnemies)
            {
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemy;
                }
            }
            
            return nearestEnemy;
        }
        
        /// <summary>
        /// 기본 공격 ServerRpc (클라이언트에서 호출)
        /// </summary>
        [ServerRpc]
        private void PerformAttackServerRpc(Vector2 attackPosition, Vector2 attackDirection, ServerRpcParams rpcParams = default)
        {
            ProcessBasicAttack(attackPosition, attackDirection, rpcParams.Receive.SenderClientId);
        }
        
        /// <summary>
        /// 서버에서 실제 기본 공격 처리
        /// </summary>
        private void ProcessBasicAttack(Vector2 attackPosition, Vector2 attackDirection, ulong clientId = 0)
        {
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
                // PvP 종족 밸런스 적용
                if (PvPBalanceSystem.Instance != null && statsManager?.CurrentStats != null)
                {
                    Race attackerRace = statsManager.CurrentStats.CharacterRace;
                    Race targetRace = targetStatsManager.CurrentStats.CharacterRace;
                    attackDamage = PvPBalanceSystem.Instance.CalculateRaceBalancedDamage(attackerRace, targetRace, attackDamage);
                }
                
                ApplyDamageToPlayer(targetStatsManager, attackDamage, damageType, isCritical, attackPosition);
                return;
            }
            
            // 타겟이 MonsterEntity 시스템
            var targetMonsterEntity = target.GetComponent<MonsterEntity>();
            if (targetMonsterEntity != null)
            {
                Debug.Log($"🗡️ Found MonsterEntity: {targetMonsterEntity.name}");
                ApplyDamageToMonsterEntity(targetMonsterEntity, attackDamage, damageType, isCritical, attackPosition);
                return;
            }
            
            Debug.LogWarning($"🗡️ No valid damage target found on {target.name}");
        }
        
        /// <summary>
        /// 플레이어에게 데미지 적용
        /// </summary>//
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
            
            // PvP 킬/데스 처리 (타겟이 죽었을 경우)
            if (targetStatsManager.IsDead)
            {
                var killerStatsManager = GetComponent<PlayerStatsManager>();
                if (killerStatsManager != null)
                {
                    ProcessPvPKillReward(killerStatsManager, targetStatsManager);
                }
            }
        }
        /// <summary>
        /// 몬스터에게 데미지 적용 (MonsterEntity)
        /// </summary>
        private void ApplyDamageToMonsterEntity(MonsterEntity targetMonster, float damage, DamageType damageType, bool isCritical, Vector2 attackPosition)
        {
            var attackerController = GetComponent<PlayerController>();
            float actualDamage = 0f;
            
            try 
            {
                
                // 서버로 데미지 요청 전송 (NetworkBehaviour이므로 RPC 사용)
                var attackerNetworkObject = GetComponent<NetworkObject>();
                ulong attackerClientId = attackerNetworkObject != null ? attackerNetworkObject.OwnerClientId : 0;
                
                targetMonster.TakeDamage(damage, damageType, attackerClientId);
                
                // RPC는 비동기이므로 actualDamage는 예상치로 설정
                actualDamage = damage; // 실제 데미지는 서버에서 계산됨
            }
            catch (System.Exception e)
            {
                Debug.LogError($"🗡️ Exception in TakeDamageServerRpc: {e.Message}");
                Debug.LogError($"🗡️ StackTrace: {e.StackTrace}");
                actualDamage = 0f;
            }
            
            // 데미지 로그
            string critText = isCritical ? " (CRITICAL)" : "";
            Debug.Log($"⚔️ {name} dealt {actualDamage:F1} {damageType} damage to {targetMonster.VariantData?.variantName ?? "Monster"}{critText}");
            
            // 인챈트 효과 적용 (실제 가한 데미지 기반)
            if (enchantManager != null && statsManager != null)
            {
                // 흡혈 인챈트 - 가한 데미지의 일정 비율만큼 체력 회복
                float lifeStealBonus = enchantManager.GetEnchantEffect(EnchantType.LifeSteal);
                if (lifeStealBonus > 0)
                {
                    float healAmount = actualDamage * (lifeStealBonus / 100f);
                    statsManager.Heal(healAmount);
                    Debug.Log($"💚 Life steal: Healed {healAmount:F1} HP ({lifeStealBonus}%)");
                }
            }
            
            // 피격 이펙트 표시 (실제 데미지로)
            Vector2 hitPosition = targetMonster.transform.position;
            ShowDamageEffectClientRpc(hitPosition, actualDamage, isCritical, damageType);
        }
        
        
        /// <summary>
        /// 공격 이펙트 재생
        /// </summary>
        [ClientRpc]
        private void PlayAttackEffectClientRpc(Vector2 attackPosition, Vector2 attackDirection)
        {
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
            
            // 실제 UI 데미지 텍스트 표시는 추후 UI 시스템에서 구현
        }
        
        /// <summary>
        /// 스킬 공격 (추후 스킬 시스템과 연동)
        /// </summary>
        public void PerformSkillAttack(string skillId, Vector2 targetPosition)
        {
            if (!IsOwner) return;
            
            // 서버/클라이언트 분기
            if (!IsServer)
            {
                PerformSkillAttackServerRpc(skillId, targetPosition);
                return;
            }
            
            // 서버에서 직접 처리
            ProcessSkillAttack(skillId, targetPosition);
        }
        
        [ServerRpc]
        private void PerformSkillAttackServerRpc(string skillId, Vector2 targetPosition)
        {
            ProcessSkillAttack(skillId, targetPosition);
        }
        
        /// <summary>
        /// 서버에서 실제 스킬 공격 처리
        /// </summary>
        private void ProcessSkillAttack(string skillId, Vector2 targetPosition)
        {
            // 스킬별 데미지 계산 (추후 스킬 시스템에서 구현)
            float minPercent = 80f; // 스킬별 설정값
            float maxPercent = 200f; // 스킬별 설정값
            DamageType skillType = DamageType.Physical; // 스킬별 설정값
            
            if (statsManager?.CurrentStats != null)
            {
                float skillDamage = statsManager.CurrentStats.CalculateSkillDamage(minPercent, maxPercent, skillType);
                Debug.Log($"Skill {skillId} would deal {skillDamage:F1} damage");
            }
            
            Debug.Log($"Skill attack: {skillId} at {targetPosition}");
        }
        
        /// <summary>
        /// PvP 킬 보상 처리
        /// </summary>
        private void ProcessPvPKillReward(PlayerStatsManager killerStatsManager, PlayerStatsManager victimStatsManager)
        {
            if (PvPBalanceSystem.Instance == null) 
            {
                // 기본 PvP 보상 (PvPBalanceSystem이 없을 때)
                long expGain = victimStatsManager.CurrentStats.CurrentLevel * 100;
                killerStatsManager.AddExperience(expGain);
                Debug.Log($"{killerStatsManager.name} killed {victimStatsManager.name} and gained {expGain} experience!");
                return;
            }
            
            // 고급 PvP 보상 시스템
            var killerNetworkBehaviour = killerStatsManager.GetComponent<NetworkBehaviour>();
            var victimNetworkBehaviour = victimStatsManager.GetComponent<NetworkBehaviour>();
            
            if (killerNetworkBehaviour != null && victimNetworkBehaviour != null)
            {
                ulong killerClientId = killerNetworkBehaviour.OwnerClientId;
                ulong victimClientId = victimNetworkBehaviour.OwnerClientId;
                
                // 킬 보상 계산
                var killReward = PvPBalanceSystem.Instance.CalculatePvPKillReward(
                    killerClientId, victimClientId, victimStatsManager.CurrentStats.CurrentLevel);
                
                // 데스 페널티 계산
                var deathPenalty = PvPBalanceSystem.Instance.CalculatePvPDeathPenalty(
                    victimClientId, victimStatsManager.CurrentStats.CurrentExperience, 
                    victimStatsManager.CurrentStats.CurrentGold);
                
                // 킬러에게 보상 지급
                killerStatsManager.AddExperience(killReward.finalExpReward);
                killerStatsManager.ChangeGold(killReward.finalGoldReward);
                
                // 피해자에게 페널티 적용
                victimStatsManager.AddExperience(-deathPenalty.expLoss); // 경험치 감소
                victimStatsManager.ChangeGold(-deathPenalty.goldDrop);   // 골드 드롭
                
                // 로그 출력
                string revengeText = killReward.isRevenge ? " [REVENGE]" : "";
                int killStreak = PvPBalanceSystem.Instance.GetKillStreak(killerClientId);
                
                Debug.Log($"💀 PvP Kill{revengeText}: {killerStatsManager.name} → {victimStatsManager.name}");
                Debug.Log($"🏆 Killer gained: {killReward.finalExpReward} EXP, {killReward.finalGoldReward} Gold (Streak: {killStreak})");
                Debug.Log($"💔 Victim lost: {deathPenalty.expLoss} EXP, {deathPenalty.goldDrop} Gold");
                
                // 킬 스트릭 알림
                if (killStreak > 0 && killStreak % 3 == 0)
                {
                    NotifyKillStreakClientRpc(killerClientId, killStreak);
                }
            }
        }
        
        /// <summary>
        /// 킬 스트릭 알림 (모든 클라이언트에게)
        /// </summary>
        [ClientRpc]
        private void NotifyKillStreakClientRpc(ulong playerClientId, int killStreak)
        {
            string playerName = $"Player_{playerClientId}"; // 실제로는 플레이어 이름 가져오기
            
            if (killStreak >= 10)
                Debug.Log($"🔥🔥🔥 UNSTOPPABLE! {playerName} has {killStreak} kills in a row!");
            else if (killStreak >= 5)
                Debug.Log($"🔥🔥 RAMPAGE! {playerName} has {killStreak} kills in a row!");
            else if (killStreak >= 3)
                Debug.Log($"🔥 KILLING SPREE! {playerName} has {killStreak} kills in a row!");
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
}
