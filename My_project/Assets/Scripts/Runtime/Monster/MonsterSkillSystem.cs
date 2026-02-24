using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 몬스터 스킬 시스템 - 패시브/액티브 스킬 관리
    /// </summary>
    public class MonsterSkillSystem : NetworkBehaviour
    {
        [Header("Skill System")]
        [SerializeField] private List<MonsterSkillInstance> skills = new List<MonsterSkillInstance>();
        [SerializeField] private MonsterEntity monsterEntity;
        
        [Header("Skill Execution")]
        [SerializeField] private float skillUpdateInterval = 0.5f;
        [SerializeField] private LayerMask targetLayers = -1;
        
        // 스킬 실행 타이머
        private float lastSkillUpdate = 0f;
        private bool isExecutingSkill = false;
        
        // 컴포넌트 참조
        private MonsterAI monsterAI;
        private Collider2D monsterCollider;

        // GC 최적화: 재사용 버퍼
        private static readonly Collider2D[] s_OverlapBuffer = new Collider2D[16];
        
        // 이벤트
        public System.Action<MonsterSkillData, float> OnSkillActivated;
        public System.Action<MonsterSkillData, float> OnSkillCooldownStarted;

        public void InitializeSkillSystem()
        { 
            monsterEntity = GetComponent<MonsterEntity>();
            monsterAI = GetComponent<MonsterAI>();
            monsterCollider = GetComponent<Collider2D>();

            if (IsServer)
            {
                // 몬스터 엔티티 생성 완료 이벤트 구독
                if (monsterEntity != null)
                {
                    monsterEntity.OnEntityGenerated += OnMonsterEntityGenerated;
                }

                // 주기적 스킬 체크 시작
                InvokeRepeating(nameof(UpdateSkills), 1f, skillUpdateInterval);
            }
        }
        
        public void CleanupSkillSystem()
        {
            if (monsterEntity != null)
            {
                monsterEntity.OnEntityGenerated -= OnMonsterEntityGenerated;
            }

            CancelInvoke();
        }
        
        /// <summary>
        /// 몬스터 엔티티 생성 완료 시 호출
        /// </summary>
        private void OnMonsterEntityGenerated(MonsterEntity entity)
        {
            // 엔티티에서 생성된 스킬 가져오기
            skills = new List<MonsterSkillInstance>(entity.ActiveSkills);
            
            // 패시브 스킬 즉시 적용
            ApplyPassiveSkills();
            
            Debug.Log($"MonsterSkillSystem initialized with {skills.Count} skills");
        }
        
        /// <summary>
        /// 패시브 스킬 적용
        /// </summary>
        private void ApplyPassiveSkills()
        {
            foreach (var skill in skills)
            {
                if (skill.skillData.IsPassive && skill.isActive)
                {
                    ApplyPassiveSkill(skill);
                }
            }
        }
        
        /// <summary>
        /// 개별 패시브 스킬 적용
        /// </summary>
        private void ApplyPassiveSkill(MonsterSkillInstance skill)
        {
            var effect = skill.GetCurrentEffect();
            
            // 패시브 스킬은 항상 적용됨 (이미 MonsterEntity.ApplySkillBonusesToStats에서 처리)
            Debug.Log($"🔮 Passive skill applied: {skill.skillData.skillName} (Grade: {skill.effectGrade})");
        }
        
        /// <summary>
        /// 스킬 업데이트 (서버에서만)
        /// </summary>
        private void UpdateSkills()
        {
            if (!IsServer || isExecutingSkill || monsterEntity.IsDead) return;
            
            // 액티브 스킬 중에서 사용 가능한 것들 체크
            foreach (var skill in skills)
            {
                if (skill.skillData.IsActive && skill.isActive && skill.CanUse)
                {
                    if (ShouldActivateSkill(skill))
                    {
                        StartCoroutine(ExecuteActiveSkill(skill));
                        break; // 한 번에 하나의 스킬만 실행
                    }
                }
            }
        }
        
        /// <summary>
        /// 스킬 활성화 조건 체크
        /// </summary>
        private bool ShouldActivateSkill(MonsterSkillInstance skill)
        {
            switch (skill.skillData.Trigger)
            {
                case MonsterSkillTrigger.Manual:
                    return false; // 수동 발동은 다른 조건에서
                    
                case MonsterSkillTrigger.OnCombatStart:
                    return monsterAI != null && monsterAI.CurrentState == MonsterAIState.Attack;
                    
                case MonsterSkillTrigger.OnLowHealth:
                    return monsterEntity.CurrentHP / monsterEntity.MaxHP < 0.3f;
                    
                case MonsterSkillTrigger.OnCooldown:
                    return true; // 쿨다운마다 발동
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// 액티브 스킬 실행
        /// </summary>
        private IEnumerator ExecuteActiveSkill(MonsterSkillInstance skill)
        {
            isExecutingSkill = true;
            
            // 쿨다운 시작
            int skillIndex = skills.FindIndex(s => s.skillData == skill.skillData);
            if (skillIndex >= 0)
            {
                var updatedSkill = skills[skillIndex];
                updatedSkill.lastUsedTime = Time.time;
                skills[skillIndex] = updatedSkill;
            }
            
            // 스킬 이펙트 적용
            ApplyActiveSkillEffect(skill);
            
            // 클라이언트에 스킬 사용 알림
            TriggerSkillEffectClientRpc(skill.skillData.skillName, skill.effectGrade, transform.position);
            
            // 이벤트 발생
            OnSkillActivated?.Invoke(skill.skillData, skill.effectGrade);
            OnSkillCooldownStarted?.Invoke(skill.skillData, skill.skillData.Cooldown);
            
            Debug.Log($"⚔️ {monsterEntity.VariantData.variantName} used {skill.skillData.skillName} (Grade: {skill.effectGrade})");
            
            yield return new WaitForSeconds(0.5f);
            isExecutingSkill = false;
        }
        
        /// <summary>
        /// 액티브 스킬 효과 적용
        /// </summary>
        private void ApplyActiveSkillEffect(MonsterSkillInstance skill)
        {
            var effect = skill.GetCurrentEffect();
            var skillData = skill.skillData;
            
            // 스킬 카테고리별 처리
            switch (skillData.Category)
            {
                case MonsterSkillCategory.PhysicalAttack:
                    ExecutePhysicalAttack(effect, skillData, skill.effectGrade);
                    break;
                    
                case MonsterSkillCategory.MagicalAttack:
                    ExecuteMagicalAttack(effect, skillData, skill.effectGrade);
                    break;
                    
                case MonsterSkillCategory.Regeneration:
                    ExecuteRegeneration(effect, skill.effectGrade);
                    break;
                    
                case MonsterSkillCategory.SpecialAbility:
                    ExecuteSpecialAbility(effect, skillData, skill.effectGrade);
                    break;
                    
                default:
                    // 기본 효과 적용
                    ApplyGenericEffect(effect, skillData);
                    break;
            }
        }
        
        /// <summary>
        /// 물리 공격 스킬 실행
        /// </summary>
        private void ExecutePhysicalAttack(MonsterSkillEffect effect, MonsterSkillData skillData, float skillGrade)
        {
            // 범위 내 타겟 찾기
            var targets = FindTargetsInRange(skillData.Range);
            
            foreach (var target in targets)
            {
                var player = target.GetComponent<PlayerController>();
                if (player != null)
                {
                    var statsManager = player.GetComponent<PlayerStatsManager>();
                    if (statsManager != null && !statsManager.IsDead)
                    {
                        // 데미지 계산 (몬스터의 물리 데미지 * 스킬 배율)
                        float baseDamage = Random.Range(monsterEntity.CombatStats.physicalDamage.minDamage, 
                                                       monsterEntity.CombatStats.physicalDamage.maxDamage);
                        float skillDamage = baseDamage * effect.damageMultiplierRange.GetValueForGrade(skillGrade);
                        
                        // 데미지 적용
                        float actualDamage = statsManager.TakeDamage(skillDamage, DamageType.Physical);
                        
                        // 상태이상 적용
                        if (effect.inflictStatus != StatusType.None && Random.value < effect.statusChanceRange.GetValueForGrade(skillGrade))
                        {
                            // 상태이상 시스템 연동 필요
                            Debug.Log($"💥 Applied {effect.inflictStatus} to {player.name}");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 마법 공격 스킬 실행
        /// </summary>
        private void ExecuteMagicalAttack(MonsterSkillEffect effect, MonsterSkillData skillData, float skillGrade)
        {
            var targets = FindTargetsInRange(skillData.Range);
            
            foreach (var target in targets)
            {
                var player = target.GetComponent<PlayerController>();
                if (player != null)
                {
                    var statsManager = player.GetComponent<PlayerStatsManager>();
                    if (statsManager != null && !statsManager.IsDead)
                    {
                        // 마법 데미지 계산
                        float baseDamage = Random.Range(monsterEntity.CombatStats.magicalDamage.minDamage, 
                                                       monsterEntity.CombatStats.magicalDamage.maxDamage);
                        float skillDamage = baseDamage * effect.damageMultiplierRange.GetValueForGrade(skillGrade);
                        
                        float actualDamage = statsManager.TakeDamage(skillDamage, DamageType.Magical);
                    }
                }
            }
        }
        
        /// <summary>
        /// 재생 스킬 실행
        /// </summary>
        private void ExecuteRegeneration(MonsterSkillEffect effect, float skillGrade)
        {
            if (monsterEntity != null && !monsterEntity.IsDead)
            {
                // 체력 회복 (MonsterEntity에 회복 메서드 추가 필요)
                float healAmount = effect.healingAmountRange.GetValueForGrade(skillGrade);
                Debug.Log($"💚 {monsterEntity.VariantData.variantName} healed for {healAmount}");
            }
        }
        
        /// <summary>
        /// 특수 능력 스킬 실행
        /// </summary>
        private void ExecuteSpecialAbility(MonsterSkillEffect effect, MonsterSkillData skillData, float skillGrade)
        {
            // 특수 능력은 개별적으로 구현
            Debug.Log($"✨ Special ability: {skillData.skillName} activated");
        }
        
        /// <summary>
        /// 일반 효과 적용
        /// </summary>
        private void ApplyGenericEffect(MonsterSkillEffect effect, MonsterSkillData skillData)
        {
            // 버프/디버프 등의 일반적인 효과
            if (effect.durationRange.maxValue > 0)
            {
                StartCoroutine(ApplyTemporaryEffect(effect));
            }
        }
        
        /// <summary>
        /// 임시 효과 적용 (버프/디버프)
        /// </summary>
        private IEnumerator ApplyTemporaryEffect(MonsterSkillEffect effect)
        {
            // 임시 효과 시작 (기본 등급 100으로 설정)
            float duration = effect.durationRange.GetValueForGrade(100f);
            Debug.Log($"🔥 Temporary effect started for {duration} seconds");
            
            yield return new WaitForSeconds(duration);
            
            // 임시 효과 종료
            Debug.Log($"❄️ Temporary effect ended");
        }
        
        /// <summary>
        /// 범위 내 타겟 찾기
        /// </summary>
        private List<Collider2D> FindTargetsInRange(float range)
        {
            var targets = new List<Collider2D>();
            
            if (range <= 0) return targets;
            
            int findCount = Physics2D.OverlapCircleNonAlloc(transform.position, range, s_OverlapBuffer, targetLayers);

            for (int i = 0; i < findCount; i++)
            {
                if (s_OverlapBuffer[i] != monsterCollider) // 자기 자신 제외
                {
                    var player = s_OverlapBuffer[i].GetComponent<PlayerController>();
                    if (player != null)
                    {
                        targets.Add(s_OverlapBuffer[i]);
                    }
                }
            }
            
            return targets;
        }
        
        /// <summary>
        /// 특정 조건으로 스킬 강제 발동
        /// </summary>
        public void TriggerSkill(MonsterSkillTrigger trigger)
        {
            if (!IsServer) return;
            
            foreach (var skill in skills)
            {
                if (skill.skillData.IsActive && skill.isActive && 
                    skill.skillData.Trigger == trigger && skill.CanUse)
                {
                    StartCoroutine(ExecuteActiveSkill(skill));
                    break;
                }
            }
        }
        
        /// <summary>
        /// 데미지를 받을 때 스킬 트리거
        /// </summary>
        public void OnTakeDamage(float damage)
        {
            TriggerSkill(MonsterSkillTrigger.OnTakeDamage);
        }
        
        /// <summary>
        /// 데미지를 줄 때 스킬 트리거
        /// </summary>
        public void OnDealDamage(float damage)
        {
            TriggerSkill(MonsterSkillTrigger.OnDealDamage);
        }
        
        /// <summary>
        /// 전투 시작 시 스킬 트리거
        /// </summary>
        public void OnCombatStart()
        {
            TriggerSkill(MonsterSkillTrigger.OnCombatStart);
        }
        
        /// <summary>
        /// 스킬 이펙트 클라이언트 동기화
        /// </summary>
        [ClientRpc]
        private void TriggerSkillEffectClientRpc(string skillName, float grade, Vector3 position)
        {
            // 클라이언트에서 스킬 이펙트 재생
            Debug.Log($"🎭 Skill effect: {skillName} ({grade}) at {position}");
            
            // 이펙트 파티클, 사운드 등 재생
            PlaySkillEffect(skillName, grade, position);
        }
        
        /// <summary>
        /// 스킬 이펙트 재생 (클라이언트)
        /// </summary>
        private void PlaySkillEffect(string skillName, float grade, Vector3 position)
        {
            // 스킬별 이펙트 재생 (추후 구현)
            // 파티클 시스템, 사운드, 애니메이션 등
        }
        
        /// <summary>
        /// 스킬 정보 가져오기
        /// </summary>
        public List<MonsterSkillInstance> GetActiveSkills()
        {
            return new List<MonsterSkillInstance>(skills);
        }
        
        /// <summary>
        /// 특정 스킬 가져오기
        /// </summary>
        public MonsterSkillInstance? GetSkill(string skillName)
        {
            return skills.Find(s => s.skillData.skillName == skillName);
        }
        
        /// <summary>
        /// 디버그 정보
        /// </summary>
        [ContextMenu("Show Skill Debug Info")]
        public void ShowSkillDebugInfo()
        {
            Debug.Log($"=== MonsterSkillSystem ({name}) ===");
            Debug.Log($"Total Skills: {skills.Count}");
            
            foreach (var skill in skills)
            {
                string cooldownInfo = skill.CanUse ? "Ready" : $"Cooldown: {skill.skillData.Cooldown - (Time.time - skill.lastUsedTime):F1}s";
                Debug.Log($"- {skill.skillData.skillName} ({skill.skillData.SkillType}, {skill.effectGrade}): {cooldownInfo}");
            }
        }

        public override void OnDestroy()
        {
            OnSkillActivated = null;
            OnSkillCooldownStarted = null;
            CancelInvoke();
            StopAllCoroutines();
            base.OnDestroy();
        }
    }
}