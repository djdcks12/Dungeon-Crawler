using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 스킬 관리자 - 플레이어의 스킬 학습, 사용, 관리를 담당
    /// </summary>
    public class SkillManager : NetworkBehaviour
    {
        [Header("Skill Settings")]
        [SerializeField] private bool enableSkillSystem = true;
        [SerializeField] private float globalCooldownTime = 1f; // 글로벌 쿨다운
        
        // 컴포넌트 참조
        private PlayerStatsManager statsManager;
        private PlayerController playerController;
        private CombatSystem combatSystem;
        
        // 스킬 데이터
        private List<string> learnedSkillIds = new List<string>();
        private Dictionary<string, float> skillCooldowns = new Dictionary<string, float>();
        private Dictionary<string, SkillData> availableSkills = new Dictionary<string, SkillData>();
        
        // 네트워크 동기화
        private NetworkVariable<SkillListWrapper> networkLearnedSkills = new NetworkVariable<SkillListWrapper>(
            new SkillListWrapper(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        // 상태
        private float lastSkillUseTime = 0f;
        private bool isGlobalCooldown = false;

        // 활성 상태이상 효과
        private List<ActiveStatusEffect> activeEffects = new List<ActiveStatusEffect>();

        // GC 최적화: 재사용 버퍼
        private static readonly Collider2D[] s_OverlapBuffer = new Collider2D[16];
        // GC 최적화: 쿨다운 키 캐시 (UpdateCooldowns에서 매프레임 new List 방지)
        private readonly List<string> cooldownKeysCache = new List<string>();
        private readonly List<string> cooldownExpiredCache = new List<string>();
        
        // 이벤트
        public System.Action<string> OnSkillLearned;
        public System.Action<string, float> OnSkillUsed;
        public System.Action<string, float> OnSkillCooldownUpdated;
        public System.Action<StatusType, bool> OnStatusEffectChanged; // type, isApplied
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // 컴포넌트 초기화
            statsManager = GetComponent<PlayerStatsManager>();
            playerController = GetComponent<PlayerController>();
            combatSystem = GetComponent<CombatSystem>();
            
            // 네트워크 변수 이벤트 구독
            networkLearnedSkills.OnValueChanged += OnNetworkSkillsChanged;
            
            // 사용 가능한 스킬들 로드
            LoadAvailableSkills();
            
            // 저장된 스킬들 로드
            if (IsOwner)
            {
                LoadLearnedSkills();
            }
        }
        
        public override void OnNetworkDespawn()
        {
            networkLearnedSkills.OnValueChanged -= OnNetworkSkillsChanged;
            OnSkillLearned = null;
            OnSkillUsed = null;
            OnSkillCooldownUpdated = null;
            OnStatusEffectChanged = null;
            base.OnNetworkDespawn();
        }
        
        private void Update()
        {
            if (!IsOwner) return;

            // 쿨다운 업데이트
            UpdateCooldowns();

            // 글로벌 쿨다운 체크
            UpdateGlobalCooldown();

            // 상태이상 효과 업데이트
            UpdateActiveStatusEffects();
        }
        
        /// <summary>
        /// 사용 가능한 스킬들 로드
        /// </summary>
        private void LoadAvailableSkills()
        {
            availableSkills.Clear();
            
            // 종족별 스킬 생성
            var humanSkills = HumanSkills.CreateAllHumanSkills();
            var elfSkills = ElfSkills.CreateAllElfSkills();
            var beastSkills = BeastSkills.CreateAllBeastSkills();
            var machinaSkills = MachinaSkills.CreateAllMachinaSkills();
            
            // 모든 스킬을 사전에 추가
            AddSkillsToDatabase(humanSkills);
            AddSkillsToDatabase(elfSkills);
            AddSkillsToDatabase(beastSkills);
            AddSkillsToDatabase(machinaSkills);
            
            Debug.Log($"Loaded {availableSkills.Count} available skills from all races");
        }
        
        /// <summary>
        /// 스킬들을 데이터베이스에 추가
        /// </summary>
        private void AddSkillsToDatabase(SkillData[] skills)
        {
            foreach (var skill in skills)
            {
                if (skill != null && !string.IsNullOrEmpty(skill.skillId))
                {
                    availableSkills[skill.skillId] = skill;
                }
            }
        }
        
        /// <summary>
        /// 학습한 스킬들 로드
        /// </summary>
        private void LoadLearnedSkills()
        {
            if (statsManager?.CurrentStats == null) return;
            
            string characterId = $"Player_{statsManager.CurrentStats.CharacterRace}_{statsManager.CurrentStats.CurrentLevel}";
            string skillsJson = PlayerPrefs.GetString($"Character_{characterId}_Skills", "");
            
            if (!string.IsNullOrEmpty(skillsJson))
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<SkillListWrapper>(skillsJson);
                    learnedSkillIds = new List<string>(wrapper.skillIds);
                    
                    // 네트워크 동기화
                    networkLearnedSkills.Value = wrapper;
                    
                    Debug.Log($"Loaded {learnedSkillIds.Count} learned skills for {characterId}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to load skills: {e.Message}");
                    learnedSkillIds.Clear();
                }
            }
        }
        
        /// <summary>
        /// 학습한 스킬들 저장
        /// </summary>
        private void SaveLearnedSkills()
        {
            if (statsManager?.CurrentStats == null) return;
            
            string characterId = $"Player_{statsManager.CurrentStats.CharacterRace}_{statsManager.CurrentStats.CurrentLevel}";
            var wrapper = new SkillListWrapper { skillIds = learnedSkillIds.ToArray() };
            string skillsJson = JsonUtility.ToJson(wrapper);
            
            PlayerPrefs.SetString($"Character_{characterId}_Skills", skillsJson);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// 스킬 학습 (클라이언트/서버 공통 진입점)
        /// </summary>
        public bool LearnSkill(string skillId)
        {
            if (!enableSkillSystem || !IsOwner) return false;
            
            if (!availableSkills.ContainsKey(skillId))
            {
                Debug.LogError($"Skill not found: {skillId}");
                return false;
            }
            
            var skillData = availableSkills[skillId];
            
            // 학습 가능 여부 확인
            if (!skillData.CanLearn(statsManager.CurrentStats, learnedSkillIds))
            {
                Debug.LogWarning($"Cannot learn skill: {skillData.skillName}");
                return false;
            }
            
            // 서버/클라이언트 분기
            if (!IsServer)
            {
                LearnSkillServerRpc(skillId);
                return true; // 클라이언트는 요청만 전송
            }
            
            // 서버에서 직접 처리
            return ProcessSkillLearning(skillId);
        }
        
        /// <summary>
        /// 스킬 학습 ServerRpc (클라이언트에서 호출)
        /// </summary>
        [ServerRpc]
        private void LearnSkillServerRpc(string skillId)
        {
            ProcessSkillLearning(skillId);
        }
        
        /// <summary>
        /// 서버에서 실제 스킬 학습 처리
        /// </summary>
        private bool ProcessSkillLearning(string skillId)
        {
            if (!availableSkills.ContainsKey(skillId)) return false;
            
            var skillData = availableSkills[skillId];
            
            // 재검증 (서버에서만)
            if (!skillData.CanLearn(statsManager.CurrentStats, learnedSkillIds))
            {
                return false;
            }
            
            // 골드 차감
            if (statsManager.CurrentStats.Gold >= skillData.goldCost)
            {
                statsManager.ChangeGold(-skillData.goldCost);
                
                // 스킬 학습
                learnedSkillIds.Add(skillId);
                
                // 네트워크 동기화
                var wrapper = new SkillListWrapper { skillIds = learnedSkillIds.ToArray() };
                networkLearnedSkills.Value = wrapper;
                
                // 저장
                SaveLearnedSkills();
                
                // 이벤트 발생
                OnSkillLearned?.Invoke(skillId);
                
                // 클라이언트에 알림
                NotifySkillLearnedClientRpc(skillId);
                
                Debug.Log($"✅ Skill learned: {skillData.skillName} for {skillData.goldCost} gold");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 스킬 사용 (클라이언트/서버 공통 진입점)
        /// </summary>
        public bool UseSkill(string skillId, Vector3 targetPosition = default)
        {
            if (!enableSkillSystem || !IsOwner || isGlobalCooldown) return false;
            
            if (!learnedSkillIds.Contains(skillId))
            {
                Debug.LogWarning($"Skill not learned: {skillId}");
                return false;
            }
            
            if (!availableSkills.ContainsKey(skillId))
            {
                Debug.LogError($"Skill data not found: {skillId}");
                return false;
            }
            
            var skillData = availableSkills[skillId];
            
            // 쿨다운 확인
            if (IsSkillOnCooldown(skillId))
            {
                Debug.LogWarning($"Skill on cooldown: {skillData.skillName}");
                return false;
            }
            
            // 마나 확인
            if (statsManager.CurrentStats.CurrentMP < skillData.manaCost)
            {
                Debug.LogWarning($"Not enough mana for {skillData.skillName}");
                return false;
            }
            
            // 서버/클라이언트 분기
            if (!IsServer)
            {
                UseSkillServerRpc(skillId, targetPosition);
                return true; // 클라이언트는 요청만 전송
            }
            
            // 서버에서 직접 처리
            return ProcessSkillUse(skillId, targetPosition);
        }
        
        /// <summary>
        /// 스킬 사용 ServerRpc (클라이언트에서 호출)
        /// </summary>
        [ServerRpc]
        private void UseSkillServerRpc(string skillId, Vector3 targetPosition)
        {
            ProcessSkillUse(skillId, targetPosition);
        }
        
        /// <summary>
        /// 서버에서 실제 스킬 사용 처리
        /// </summary>
        private bool ProcessSkillUse(string skillId, Vector3 targetPosition)
        {
            if (!availableSkills.ContainsKey(skillId) || !learnedSkillIds.Contains(skillId)) return false;
            
            var skillData = availableSkills[skillId];
            
            // 마나 소모
            statsManager.ChangeMP(-skillData.manaCost);
            
            // 쿨다운 설정
            skillCooldowns[skillId] = skillData.cooldown;
            
            // 글로벌 쿨다운 설정
            lastSkillUseTime = Time.time;
            isGlobalCooldown = true;
            
            // 스킬 효과 실행
            ExecuteSkillEffect(skillData, targetPosition);
            
            // 이벤트 발생
            OnSkillUsed?.Invoke(skillId, skillData.cooldown);
            
            // 클라이언트에 알림
            NotifySkillUsedClientRpc(skillId, targetPosition);
            
            Debug.Log($"🔥 Skill used: {skillData.skillName}");
            return true;
        }
        
        /// <summary>
        /// 스킬 효과 실행
        /// </summary>
        private void ExecuteSkillEffect(SkillData skillData, Vector3 targetPosition)
        {
            switch (skillData.skillType)
            {
                case SkillType.Active:
                    ExecuteActiveSkill(skillData, targetPosition);
                    break;
                    
                case SkillType.Passive:
                    // 패시브 스킬은 학습 시점에 영구 적용
                    ApplyPassiveSkill(skillData);
                    break;
                    
                case SkillType.Toggle:
                    ToggleSkill(skillData);
                    break;
            }
        }
        
        /// <summary>
        /// 액티브 스킬 실행
        /// </summary>
        private void ExecuteActiveSkill(SkillData skillData, Vector3 targetPosition)
        {
            // 사거리 체크
            if (Vector3.Distance(transform.position, targetPosition) > skillData.range)
            {
                targetPosition = transform.position + (targetPosition - transform.position).normalized * skillData.range;
            }
            
            // 데미지 스킬
            if (skillData.baseDamage > 0)
            {
                ExecuteDamageSkill(skillData, targetPosition);
            }
            
            // 힐링 스킬
            if (skillData.category == SkillCategory.Paladin && skillData.skillName.Contains("치유"))
            {
                ExecuteHealingSkill(skillData, targetPosition);
            }
            
            // 버프/디버프 스킬
            if (skillData.statusEffects != null && skillData.statusEffects.Length > 0)
            {
                ApplyStatusEffects(skillData, targetPosition);
            }
        }
        
        /// <summary>
        /// 데미지 스킬 실행
        /// </summary>
        private void ExecuteDamageSkill(SkillData skillData, Vector3 targetPosition)
        {
            // 범위 내 적들 찾기
            int targetCount = Physics2D.OverlapCircleNonAlloc(targetPosition, skillData.range, s_OverlapBuffer);

            for (int i = 0; i < targetCount; i++)
            {
                // 플레이어는 제외
                if (s_OverlapBuffer[i].GetComponent<PlayerController>() != null) continue;

                // 몬스터 타겟인지 확인
                var monsterEntity = s_OverlapBuffer[i].GetComponent<MonsterEntity>();
                if (monsterEntity != null)
                {
                    // 데미지 계산 (새로운 민댐/맥댐 시스템 사용)
                    float damage = statsManager.CurrentStats.CalculateSkillDamage(
                        skillData.minDamagePercent, skillData.maxDamagePercent, skillData.damageType);
                    
                    // NetworkManager를 통한 클라이언트 ID 가져오기
                    ulong attackerClientId = NetworkManager.Singleton?.LocalClientId ?? 0;
                    
                    // 몬스터에 데미지 적용 (새로운 네트워킹 시스템 사용)
                    monsterEntity.TakeDamage(damage, skillData.damageType, attackerClientId);
                    
                    Debug.Log($"🔥 Skill damage: {damage:F0} to {monsterEntity.name} via {skillData.skillName}");
                }
            }
        }
        
        /// <summary>
        /// 힐링 스킬 실행
        /// </summary>
        private void ExecuteHealingSkill(SkillData skillData, Vector3 targetPosition)
        {
            // 힐링량 계산 (INT 기반)
            float healAmount = skillData.baseDamage + (statsManager.CurrentStats.TotalINT * skillData.damageScaling);
            
            // 자기 자신 힐링
            statsManager.Heal(healAmount);
            
            Debug.Log($"💚 Healed for {healAmount:F0} HP");
        }
        
        /// <summary>
        /// 상태이상 효과 적용 (범위 내 대상에게)
        /// </summary>
        private void ApplyStatusEffects(SkillData skillData, Vector3 targetPosition)
        {
            if (skillData.statusEffects == null || skillData.statusEffects.Length == 0) return;

            foreach (var effect in skillData.statusEffects)
            {
                // 적용 확률 체크
                if (Random.value * 100f > skillData.statusChance) continue;

                bool isBuff = IsBuff(effect.type);

                if (isBuff)
                {
                    // 버프는 자신에게 적용
                    ApplyStatusEffectToSelf(effect);
                }
                else
                {
                    // 디버프는 범위 내 적에게 적용
                    int debuffCount = Physics2D.OverlapCircleNonAlloc(targetPosition, skillData.range, s_OverlapBuffer);
                    for (int j = 0; j < debuffCount; j++)
                    {
                        if (s_OverlapBuffer[j].transform == transform) continue;

                        var targetSkillManager = s_OverlapBuffer[j].GetComponent<SkillManager>();
                        if (targetSkillManager != null)
                        {
                            targetSkillManager.ApplyStatusEffectToSelf(effect);
                        }
                    }
                }
            }

            Debug.Log($"Applied status effects from {skillData.skillName}");
        }

        /// <summary>
        /// 자신에게 상태이상 효과 적용
        /// </summary>
        public void ApplyStatusEffectToSelf(StatusEffect effect)
        {
            // 중첩 불가능 시 기존 같은 타입 제거
            if (!effect.stackable)
            {
                RemoveStatusEffect(effect.type);
            }

            var activeEffect = new ActiveStatusEffect
            {
                effect = effect,
                remainingDuration = effect.duration,
                tickTimer = effect.tickInterval
            };

            activeEffects.Add(activeEffect);
            OnApplyStatusEffect(effect);
            OnStatusEffectChanged?.Invoke(effect.type, true);

            Debug.Log($"Status effect applied: {effect.type} ({effect.value} for {effect.duration}s)");
        }

        /// <summary>
        /// 상태이상 효과 적용 시 즉시 처리
        /// </summary>
        private void OnApplyStatusEffect(StatusEffect effect)
        {
            if (statsManager?.CurrentStats == null) return;

            switch (effect.type)
            {
                case StatusType.Strength:
                    statsManager.AddSoulBonusStats(new StatBlock { strength = effect.value });
                    break;
                case StatusType.Speed:
                    statsManager.AddSoulBonusStats(new StatBlock { agility = effect.value });
                    break;
                case StatusType.Shield:
                    // 보호막: 임시 HP 증가
                    statsManager.Heal(effect.value);
                    break;
                case StatusType.Blessing:
                    statsManager.AddSoulBonusStats(new StatBlock
                    {
                        strength = effect.value, agility = effect.value,
                        vitality = effect.value, intelligence = effect.value
                    });
                    break;
                case StatusType.Berserk:
                    statsManager.AddSoulBonusStats(new StatBlock { strength = effect.value * 2, defense = -effect.value });
                    break;
                case StatusType.Enhancement:
                    statsManager.AddSoulBonusStats(new StatBlock { strength = effect.value, intelligence = effect.value });
                    break;
                case StatusType.Stun:
                case StatusType.Root:
                    // 이동/공격 제한은 PlayerController에서 체크
                    break;
            }
        }

        /// <summary>
        /// 상태이상 효과 제거 시 역효과 처리
        /// </summary>
        private void OnRemoveStatusEffect(StatusEffect effect)
        {
            if (statsManager?.CurrentStats == null) return;

            switch (effect.type)
            {
                case StatusType.Strength:
                    statsManager.AddSoulBonusStats(new StatBlock { strength = -effect.value });
                    break;
                case StatusType.Speed:
                    statsManager.AddSoulBonusStats(new StatBlock { agility = -effect.value });
                    break;
                case StatusType.Blessing:
                    statsManager.AddSoulBonusStats(new StatBlock
                    {
                        strength = -effect.value, agility = -effect.value,
                        vitality = -effect.value, intelligence = -effect.value
                    });
                    break;
                case StatusType.Berserk:
                    statsManager.AddSoulBonusStats(new StatBlock { strength = -effect.value * 2, defense = effect.value });
                    break;
                case StatusType.Enhancement:
                    statsManager.AddSoulBonusStats(new StatBlock { strength = -effect.value, intelligence = -effect.value });
                    break;
            }
        }

        /// <summary>
        /// 활성 상태이상 효과 업데이트 (매 프레임)
        /// </summary>
        private void UpdateActiveStatusEffects()
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                var active = activeEffects[i];
                active.remainingDuration -= Time.deltaTime;

                // DoT/HoT 틱 처리
                if (active.effect.tickInterval > 0)
                {
                    active.tickTimer -= Time.deltaTime;
                    if (active.tickTimer <= 0f)
                    {
                        active.tickTimer = active.effect.tickInterval;
                        ProcessStatusTick(active.effect);
                    }
                }

                activeEffects[i] = active;

                // 만료 체크
                if (active.remainingDuration <= 0f)
                {
                    OnRemoveStatusEffect(active.effect);
                    OnStatusEffectChanged?.Invoke(active.effect.type, false);
                    activeEffects.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// DoT/HoT 틱 처리
        /// </summary>
        private void ProcessStatusTick(StatusEffect effect)
        {
            if (statsManager == null) return;

            switch (effect.type)
            {
                case StatusType.Poison:
                    statsManager.TakeDamage(effect.value, DamageType.Poison);
                    break;
                case StatusType.Burn:
                    statsManager.TakeDamage(effect.value, DamageType.Fire);
                    break;
                case StatusType.Regeneration:
                    statsManager.Heal(effect.value);
                    break;
            }
        }

        /// <summary>
        /// 특정 타입의 상태이상 제거
        /// </summary>
        public void RemoveStatusEffect(StatusType type)
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (activeEffects[i].effect.type == type)
                {
                    OnRemoveStatusEffect(activeEffects[i].effect);
                    OnStatusEffectChanged?.Invoke(type, false);
                    activeEffects.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 특정 상태이상이 활성 중인지 확인
        /// </summary>
        public bool HasStatusEffect(StatusType type)
        {
            for (int i = 0; i < activeEffects.Count; i++)
            {
                if (activeEffects[i].effect.type == type) return true;
            }
            return false;
        }

        /// <summary>
        /// 활성 상태이상 효과 리스트 (읽기 전용)
        /// </summary>
        public IReadOnlyList<ActiveStatusEffect> ActiveEffects => activeEffects;

        /// <summary>
        /// 버프 타입인지 확인
        /// </summary>
        private bool IsBuff(StatusType type)
        {
            return type == StatusType.Strength || type == StatusType.Speed ||
                   type == StatusType.Regeneration || type == StatusType.Shield ||
                   type == StatusType.Blessing || type == StatusType.Berserk ||
                   type == StatusType.Enhancement || type == StatusType.Invisibility;
        }
        
        /// <summary>
        /// 패시브 스킬 적용
        /// </summary>
        private void ApplyPassiveSkill(SkillData skillData)
        {
            if (statsManager?.CurrentStats != null)
            {
                // 스탯 보너스 적용 (영혼 보너스 스탯으로 추가)
                statsManager.AddSoulBonusStats(skillData.statBonus);
                Debug.Log($"🔸 Passive skill applied: {skillData.skillName}");
            }
        }
        
        /// <summary>
        /// 토글 스킬
        /// </summary>
        private void ToggleSkill(SkillData skillData)
        {
            // 추후 토글 시스템 구현
            Debug.Log($"🔄 Toggled skill: {skillData.skillName}");
        }
        
        /// <summary>
        /// 쿨다운 업데이트
        /// </summary>
        private void UpdateCooldowns()
        {
            if (skillCooldowns.Count == 0) return;

            cooldownKeysCache.Clear();
            cooldownExpiredCache.Clear();

            // 키 목록 캐시에 복사 (Dictionary 열거 중 수정 방지)
            foreach (var kvp in skillCooldowns)
                cooldownKeysCache.Add(kvp.Key);

            for (int i = 0; i < cooldownKeysCache.Count; i++)
            {
                string skillId = cooldownKeysCache[i];
                float remaining = skillCooldowns[skillId] - Time.deltaTime;

                if (remaining <= 0f)
                {
                    cooldownExpiredCache.Add(skillId);
                }
                else
                {
                    skillCooldowns[skillId] = remaining;
                    OnSkillCooldownUpdated?.Invoke(skillId, remaining);
                }
            }

            for (int i = 0; i < cooldownExpiredCache.Count; i++)
                skillCooldowns.Remove(cooldownExpiredCache[i]);
        }
        
        /// <summary>
        /// 글로벌 쿨다운 업데이트
        /// </summary>
        private void UpdateGlobalCooldown()
        {
            if (isGlobalCooldown && Time.time >= lastSkillUseTime + globalCooldownTime)
            {
                isGlobalCooldown = false;
            }
        }
        
        /// <summary>
        /// 스킬 자원 검증 및 소모 (CombatSystem에서 호출)
        /// 마나/쿨다운 확인 후 소모 처리. 데미지는 CombatSystem이 담당.
        /// </summary>
        public bool ValidateAndConsumeSkillResources(string skillId)
        {
            if (!enableSkillSystem) return false;
            if (!learnedSkillIds.Contains(skillId)) return false;
            if (!availableSkills.ContainsKey(skillId)) return false;
            if (isGlobalCooldown) return false;
            if (IsSkillOnCooldown(skillId)) return false;

            var skillData = availableSkills[skillId];
            if (statsManager.CurrentStats.CurrentMP < skillData.manaCost) return false;

            // 자원 소모
            statsManager.ChangeMP(-skillData.manaCost);
            skillCooldowns[skillId] = skillData.cooldown;
            lastSkillUseTime = Time.time;
            isGlobalCooldown = true;

            OnSkillUsed?.Invoke(skillId, skillData.cooldown);
            return true;
        }

        /// <summary>
        /// 스킬이 쿨다운 중인지 확인
        /// </summary>
        public bool IsSkillOnCooldown(string skillId)
        {
            return skillCooldowns.ContainsKey(skillId);
        }
        
        /// <summary>
        /// 스킬 쿨다운 시간 가져오기
        /// </summary>
        public float GetSkillCooldown(string skillId)
        {
            return skillCooldowns.ContainsKey(skillId) ? skillCooldowns[skillId] : 0f;
        }
        
        /// <summary>
        /// 학습한 스킬 목록 가져오기 (읽기 전용 - GC 방지)
        /// </summary>
        public IReadOnlyList<string> GetLearnedSkills()
        {
            return learnedSkillIds;
        }
        
        /// <summary>
        /// 학습 가능한 스킬 목록 가져오기
        /// </summary>
        public List<SkillData> GetLearnableSkills()
        {
            if (statsManager?.CurrentStats == null) return new List<SkillData>();
            
            var learnableSkills = new List<SkillData>();
            
            foreach (var kvp in availableSkills)
            {
                var skillData = kvp.Value;
                if (skillData.requiredRace == statsManager.CurrentStats.CharacterRace && 
                    skillData.CanLearn(statsManager.CurrentStats, learnedSkillIds))
                {
                    learnableSkills.Add(skillData);
                }
            }
            
            return learnableSkills.OrderBy(s => s.skillTier).ThenBy(s => s.goldCost).ToList();
        }
        
        /// <summary>
        /// 스킬 ID로 스킬 데이터 가져오기
        /// </summary>
        public SkillData GetSkillById(string skillId)
        {
            availableSkills.TryGetValue(skillId, out SkillData skillData);
            return skillData;
        }
        
        /// <summary>
        /// 스킬 데이터 가져오기
        /// </summary>
        public SkillData GetSkillData(string skillId)
        {
            return availableSkills.ContainsKey(skillId) ? availableSkills[skillId] : null;
        }
        
        /// <summary>
        /// 몬스터 영혼 스킬 학습
        /// </summary>
        public bool LearnMonsterSkill(MonsterSkillData monsterSkillData, float skillGrade)
        {
            if (!enableSkillSystem || !IsOwner) return false;
            
            // 몬스터 스킬을 플레이어 스킬 데이터로 변환
            SkillData convertedSkill = ConvertMonsterSkillToPlayerSkill(monsterSkillData, skillGrade);
            
            if (convertedSkill == null)
            {
                Debug.LogError($"Failed to convert monster skill: {monsterSkillData.skillName}");
                return false;
            }
            
            // 이미 같은 스킬을 보유하고 있는지 확인
            if (learnedSkillIds.Contains(convertedSkill.skillId))
            {
                Debug.LogWarning($"Already learned skill: {convertedSkill.skillName}");
                return false;
            }
            
            // 서버/클라이언트 분기
            if (!IsServer)
            {
                LearnMonsterSkillServerRpc(convertedSkill.skillId, skillGrade);
                return true;
            }
            
            // 서버에서 직접 처리
            return ProcessMonsterSkillLearning(convertedSkill.skillId, skillGrade);
        }
        
        /// <summary>
        /// 몬스터 스킬을 플레이어 스킬로 변환
        /// </summary>
        private SkillData ConvertMonsterSkillToPlayerSkill(MonsterSkillData monsterSkillData, float skillGrade)
        {
            // 동적으로 SkillData 생성
            var skillData = ScriptableObject.CreateInstance<SkillData>();
            
            // 기본 정보 복사
            skillData.skillName = monsterSkillData.skillName;
            skillData.skillId = $"monster_{monsterSkillData.skillName.Replace(" ", "_").ToLower()}_{skillGrade:F0}";
            skillData.description = monsterSkillData.description;
            skillData.skillIcon = monsterSkillData.skillIcon;
            
            // 몬스터 스킬 타입을 플레이어 스킬 타입으로 변환
            skillData.skillType = monsterSkillData.SkillType == MonsterSkillType.Active ? SkillType.Active : SkillType.Passive;
            
            // 몬스터 스킬 카테고리를 플레이어 카테고리로 매핑 (기본값 사용)
            skillData.category = MapMonsterCategoryToPlayerCategory(monsterSkillData.Category);
            
            // 몬스터 스킬 효과를 플레이어 스킬 스탯으로 변환
            var skillEffect = monsterSkillData.GetSkillEffect();
            skillData.statBonus = skillEffect.GetStatBlockForGrade(skillGrade);
            
            // 스킬 설정 (몬스터 스킬은 무료, 레벨 제한 없음)
            skillData.requiredLevel = 1;
            skillData.goldCost = 0; // 몬스터 스킬은 무료
            skillData.requiredRace = Race.Human; // 모든 종족이 사용 가능하게
            skillData.skillTier = Mathf.RoundToInt(skillGrade / 20f); // 80-120을 1-5티어로 변환
            
            // 액티브 스킬 설정
            if (skillData.skillType == SkillType.Active)
            {
                skillData.cooldown = monsterSkillData.Cooldown;
                skillData.manaCost = monsterSkillData.ManaCost;
                skillData.range = monsterSkillData.Range;
                
                // 데미지 계산 (스킬 효과에서 추출)
                float damageMultiplier = skillEffect.damageMultiplierRange.GetValueForGrade(skillGrade);
                skillData.baseDamage = damageMultiplier * 20f; // 기본 데미지에 배율 적용
                skillData.damageScaling = 1f;
                skillData.minDamagePercent = 80f;
                skillData.maxDamagePercent = 120f;
                skillData.damageType = DamageType.Physical; // 기본값
            }
            
            return skillData;
        }
        
        /// <summary>
        /// 몬스터 스킬 카테고리를 플레이어 카테고리로 매핑
        /// </summary>
        private SkillCategory MapMonsterCategoryToPlayerCategory(MonsterSkillCategory monsterCategory)
        {
            switch (monsterCategory)
            {
                case MonsterSkillCategory.PhysicalAttack:
                case MonsterSkillCategory.DamageBonus:
                    return SkillCategory.Warrior;
                    
                case MonsterSkillCategory.MagicalAttack:
                    return SkillCategory.ElementalMage;
                    
                case MonsterSkillCategory.PhysicalDefense:
                case MonsterSkillCategory.HealthBonus:
                    return SkillCategory.Paladin;
                    
                case MonsterSkillCategory.MagicalDefense:
                    return SkillCategory.PureMage;
                    
                case MonsterSkillCategory.MovementSpeed:
                case MonsterSkillCategory.AttackSpeed:
                    return SkillCategory.Rogue;
                    
                case MonsterSkillCategory.Regeneration:
                    return SkillCategory.NatureMage;
                    
                default:
                    return SkillCategory.Warrior; // 기본값
            }
        }
        
        /// <summary>
        /// 몬스터 스킬 학습 ServerRpc (클라이언트에서 호출)
        /// </summary>
        [ServerRpc]
        private void LearnMonsterSkillServerRpc(string skillId, float skillGrade)
        {
            ProcessMonsterSkillLearning(skillId, skillGrade);
        }
        
        /// <summary>
        /// 서버에서 실제 몬스터 스킬 학습 처리
        /// </summary>
        private bool ProcessMonsterSkillLearning(string skillId, float skillGrade)
        {
            // 스킬 학습 (골드 없이)
            learnedSkillIds.Add(skillId);
            
            // 네트워크 동기화
            var wrapper = new SkillListWrapper { skillIds = learnedSkillIds.ToArray() };
            networkLearnedSkills.Value = wrapper;
            
            // 저장
            SaveLearnedSkills();
            
            // 이벤트 발생
            OnSkillLearned?.Invoke(skillId);
            
            // 클라이언트에 알림
            NotifyMonsterSkillLearnedClientRpc(skillId, skillGrade);
            
            Debug.Log($"✅ Monster skill learned: {skillId} (Grade {skillGrade:F0})");
            return true;
        }
        
        /// <summary>
        /// 몬스터 스킬 학습 알림
        /// </summary>
        [ClientRpc]
        private void NotifyMonsterSkillLearnedClientRpc(string skillId, float skillGrade)
        {
            Debug.Log($"🌟 MONSTER SKILL LEARNED! {skillId} (Grade {skillGrade:F0})");
        }
        
        /// <summary>
        /// 네트워크 스킬 변경 이벤트
        /// </summary>
        private void OnNetworkSkillsChanged(SkillListWrapper previousValue, SkillListWrapper newValue)
        {
            if (!IsOwner)
            {
                learnedSkillIds = new List<string>(newValue.skillIds);
            }
        }
        
        /// <summary>
        /// 스킬 학습 알림
        /// </summary>
        [ClientRpc]
        private void NotifySkillLearnedClientRpc(string skillId)
        {
            var skillData = GetSkillData(skillId);
            if (skillData != null)
            {
                Debug.Log($"🌟 SKILL LEARNED! {skillData.skillName}");
            }
        }
        
        /// <summary>
        /// 스킬 사용 알림
        /// </summary>
        [ClientRpc]
        private void NotifySkillUsedClientRpc(string skillId, Vector3 targetPosition)
        {
            var skillData = GetSkillData(skillId);
            if (skillData != null)
            {
                // 시각적 효과는 추후 이팩트 시스템에서 구현
                Debug.Log($"✨ {skillData.skillName} visual effect played");
            }
        }
    }
    
    /// <summary>
    /// 활성 상태이상 효과 인스턴스 (런타임 추적용)
    /// </summary>
    [System.Serializable]
    public struct ActiveStatusEffect
    {
        public StatusEffect effect;
        public float remainingDuration;
        public float tickTimer;
    }

    /// <summary>
    /// 스킬 목록 래퍼 (네트워크 직렬화용)
    /// </summary>
    [System.Serializable]
    public struct SkillListWrapper : INetworkSerializable
    {
        public string[] skillIds;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // string[] 직렬화 - Unity Netcode가 지원하지 않으므로 수동 처리
            if (serializer.IsReader)
            {
                int skillCount = 0;
                serializer.SerializeValue(ref skillCount);
                skillIds = new string[skillCount];
                for (int i = 0; i < skillCount; i++)
                {
                    serializer.SerializeValue(ref skillIds[i]);
                }
            }
            else
            {
                int skillCount = skillIds?.Length ?? 0;
                serializer.SerializeValue(ref skillCount);
                if (skillIds != null)
                {
                    for (int i = 0; i < skillCount; i++)
                    {
                        serializer.SerializeValue(ref skillIds[i]);
                    }
                }
            }
        }
    }
}