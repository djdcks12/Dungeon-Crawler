using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 스킬 콤보 & 원소 상호작용 시스템
    /// - 연속 스킬 사용 시 콤보 게이지 → 보너스 데미지
    /// - 원소 반응: 화염+빙결=증기(실명), 번개+물=감전(연쇄), 독+화염=폭발(광역) 등
    /// - 콤보 체인: 특정 스킬 순서로 사용 시 추가 효과
    /// </summary>
    public class SkillComboSystem : MonoBehaviour
    {
        public static SkillComboSystem Instance { get; private set; }

        [Header("Combo Settings")]
        [SerializeField] private float comboWindow = 3f;
        [SerializeField] private int maxComboCount = 10;
        [SerializeField] private float comboDamagePerStack = 0.05f; // 콤보당 5% 데미지 증가
        [SerializeField] private float maxComboDamageBonus = 0.5f;  // 최대 50% 보너스

        [Header("Elemental Reaction Settings")]
        [SerializeField] private float reactionDamageMultiplier = 1.5f;
        [SerializeField] private float reactionAoERadius = 3f;

        // 콤보 추적 (플레이어별)
        private Dictionary<ulong, ComboState> playerCombos = new Dictionary<ulong, ComboState>();

        // 원소 상호작용 테이블
        private static readonly Dictionary<(DamageType, DamageType), ElementalReaction> reactionTable = new Dictionary<(DamageType, DamageType), ElementalReaction>();

        // 이벤트
        public System.Action<ulong, int, float> OnComboUpdated;       // clientId, comboCount, multiplier
        public System.Action<ulong, ElementalReaction> OnElementalReaction; // clientId, reaction

        // GC 최적화: 재사용 버퍼
        private static readonly Collider2D[] s_OverlapBuffer = new Collider2D[16];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeReactionTable();
        }

        private void Update()
        {
            UpdateCombos();
        }

        #region Combo System

        /// <summary>
        /// 스킬 사용 시 콤보 등록 (SkillManager.OnSkillUsed에서 호출)
        /// </summary>
        public void RegisterSkillUse(ulong clientId, string skillId, DamageType damageType)
        {
            if (!playerCombos.TryGetValue(clientId, out var state))
            {
                state = new ComboState();
                playerCombos[clientId] = state;
            }

            // 콤보 윈도우 내 사용인지 확인
            if (Time.time - state.lastSkillTime <= comboWindow)
            {
                state.comboCount = Mathf.Min(state.comboCount + 1, maxComboCount);
            }
            else
            {
                state.comboCount = 1; // 콤보 리셋
            }

            state.lastSkillTime = Time.time;
            state.lastSkillId = skillId;
            state.lastDamageType = damageType;
            state.skillHistory.Add(new SkillUseRecord
            {
                skillId = skillId,
                damageType = damageType,
                timestamp = Time.time
            });

            // 히스토리 5개까지만 유지
            if (state.skillHistory.Count > 5)
                state.skillHistory.RemoveAt(0);

            float multiplier = GetComboDamageMultiplier(state.comboCount);
            OnComboUpdated?.Invoke(clientId, state.comboCount, multiplier);
        }

        /// <summary>
        /// 현재 콤보 데미지 배율 가져오기
        /// </summary>
        public float GetComboDamageMultiplier(ulong clientId)
        {
            if (!playerCombos.TryGetValue(clientId, out var state))
                return 1f;

            if (Time.time - state.lastSkillTime > comboWindow)
                return 1f;

            return GetComboDamageMultiplier(state.comboCount);
        }

        private float GetComboDamageMultiplier(int comboCount)
        {
            if (comboCount <= 1) return 1f;
            float bonus = (comboCount - 1) * comboDamagePerStack;
            return 1f + Mathf.Min(bonus, maxComboDamageBonus);
        }

        /// <summary>
        /// 현재 콤보 수 가져오기
        /// </summary>
        public int GetComboCount(ulong clientId)
        {
            if (!playerCombos.TryGetValue(clientId, out var state))
                return 0;

            if (Time.time - state.lastSkillTime > comboWindow)
                return 0;

            return state.comboCount;
        }

        private void UpdateCombos()
        {
            // 콤보 타임아웃 체크
            foreach (var kvp in playerCombos)
            {
                if (Time.time - kvp.Value.lastSkillTime > comboWindow && kvp.Value.comboCount > 0)
                {
                    kvp.Value.comboCount = 0;
                    OnComboUpdated?.Invoke(kvp.Key, 0, 1f);
                }
            }
        }

        #endregion

        #region Elemental Reaction System

        private void InitializeReactionTable()
        {
            reactionTable.Clear();

            // 화염 + 빙결 = 증기 폭발 (Vaporize) - 광역 데미지 + 실명
            AddReaction(DamageType.Fire, DamageType.Ice, new ElementalReaction
            {
                reactionName = "증기 폭발",
                reactionNameEn = "Vaporize",
                damageMultiplier = 2.0f,
                aoeRadius = 3f,
                appliedStatus = StatusType.Slow,
                statusDuration = 3f,
                statusChance = 0.8f,
                particleColor = new Color(0.8f, 0.8f, 1f)
            });

            // 화염 + 독 = 독염 폭발 (Toxic Explosion) - 큰 광역 + DoT
            AddReaction(DamageType.Fire, DamageType.Poison, new ElementalReaction
            {
                reactionName = "독염 폭발",
                reactionNameEn = "ToxicExplosion",
                damageMultiplier = 2.5f,
                aoeRadius = 4f,
                appliedStatus = StatusType.Burn,
                statusDuration = 5f,
                statusChance = 1.0f,
                particleColor = new Color(0.5f, 0.8f, 0f)
            });

            // 번개 + 빙결 = 빙뢰 (Superconduct) - 방어력 감소
            AddReaction(DamageType.Lightning, DamageType.Ice, new ElementalReaction
            {
                reactionName = "빙뢰",
                reactionNameEn = "Superconduct",
                damageMultiplier = 1.5f,
                aoeRadius = 2.5f,
                appliedStatus = StatusType.Weakness,
                statusDuration = 8f,
                statusChance = 0.9f,
                particleColor = new Color(0.5f, 0.5f, 1f)
            });

            // 번개 + 독 = 감전 (Electro-Charged) - 연쇄 데미지
            AddReaction(DamageType.Lightning, DamageType.Poison, new ElementalReaction
            {
                reactionName = "감전",
                reactionNameEn = "ElectroCharged",
                damageMultiplier = 1.8f,
                aoeRadius = 3.5f,
                appliedStatus = StatusType.Stun,
                statusDuration = 1.5f,
                statusChance = 0.6f,
                particleColor = new Color(0.7f, 0f, 1f)
            });

            // 화염 + 번개 = 과부하 (Overloaded) - 큰 폭발
            AddReaction(DamageType.Fire, DamageType.Lightning, new ElementalReaction
            {
                reactionName = "과부하",
                reactionNameEn = "Overloaded",
                damageMultiplier = 2.2f,
                aoeRadius = 5f,
                appliedStatus = StatusType.Stun,
                statusDuration = 2f,
                statusChance = 0.7f,
                particleColor = new Color(1f, 0.5f, 0f)
            });

            // 빙결 + 독 = 결정화 (Crystallize) - 보호막 생성
            AddReaction(DamageType.Ice, DamageType.Poison, new ElementalReaction
            {
                reactionName = "결정화",
                reactionNameEn = "Crystallize",
                damageMultiplier = 1.3f,
                aoeRadius = 2f,
                appliedStatus = StatusType.Shield,
                statusDuration = 10f,
                statusChance = 1.0f,
                isDefensive = true,
                particleColor = new Color(0f, 1f, 0.5f)
            });

            // 암흑 + 신성 = 심판 (Judgment) - 고정 데미지
            AddReaction(DamageType.Dark, DamageType.Holy, new ElementalReaction
            {
                reactionName = "심판",
                reactionNameEn = "Judgment",
                damageMultiplier = 3.0f,
                aoeRadius = 2f,
                appliedStatus = StatusType.None,
                statusDuration = 0f,
                statusChance = 0f,
                isTrueDamage = true,
                particleColor = new Color(1f, 1f, 0f)
            });

            // 화염 + 암흑 = 지옥불 (Hellfire) - 강력 DoT
            AddReaction(DamageType.Fire, DamageType.Dark, new ElementalReaction
            {
                reactionName = "지옥불",
                reactionNameEn = "Hellfire",
                damageMultiplier = 2.0f,
                aoeRadius = 3f,
                appliedStatus = StatusType.Burn,
                statusDuration = 8f,
                statusChance = 1.0f,
                particleColor = new Color(0.3f, 0f, 0.3f)
            });

            // 빙결 + 신성 = 정화 (Purify) - 디버프 제거 + 힐
            AddReaction(DamageType.Ice, DamageType.Holy, new ElementalReaction
            {
                reactionName = "정화",
                reactionNameEn = "Purify",
                damageMultiplier = 1.2f,
                aoeRadius = 3f,
                appliedStatus = StatusType.Regeneration,
                statusDuration = 5f,
                statusChance = 1.0f,
                isDefensive = true,
                removesDebuffs = true,
                particleColor = new Color(0.8f, 1f, 1f)
            });

            // 번개 + 신성 = 천벌 (Divine Lightning) - 연쇄 신성 데미지
            AddReaction(DamageType.Lightning, DamageType.Holy, new ElementalReaction
            {
                reactionName = "천벌",
                reactionNameEn = "DivineLightning",
                damageMultiplier = 2.5f,
                aoeRadius = 4f,
                appliedStatus = StatusType.Stun,
                statusDuration = 2.5f,
                statusChance = 0.8f,
                particleColor = new Color(1f, 1f, 0.5f)
            });

            // 독 + 암흑 = 역병 (Plague) - 광역 독 확산
            AddReaction(DamageType.Poison, DamageType.Dark, new ElementalReaction
            {
                reactionName = "역병",
                reactionNameEn = "Plague",
                damageMultiplier = 1.8f,
                aoeRadius = 5f,
                appliedStatus = StatusType.Poison,
                statusDuration = 10f,
                statusChance = 1.0f,
                particleColor = new Color(0.2f, 0f, 0.2f)
            });

            // 번개 + 암흑 = 공허 (Void) - 루트 + 데미지
            AddReaction(DamageType.Lightning, DamageType.Dark, new ElementalReaction
            {
                reactionName = "공허",
                reactionNameEn = "Void",
                damageMultiplier = 2.0f,
                aoeRadius = 2.5f,
                appliedStatus = StatusType.Root,
                statusDuration = 3f,
                statusChance = 0.9f,
                particleColor = new Color(0.1f, 0f, 0.3f)
            });
        }

        private void AddReaction(DamageType a, DamageType b, ElementalReaction reaction)
        {
            // 양방향 등록 (A+B = B+A)
            reactionTable[(a, b)] = reaction;
            reactionTable[(b, a)] = reaction;
        }

        /// <summary>
        /// 원소 반응 확인 - 대상에 이미 원소 상태이상이 있고 다른 원소로 공격할 때
        /// </summary>
        public ElementalReaction? CheckElementalReaction(DamageType attackElement, SkillManager targetSkillManager)
        {
            if (targetSkillManager == null) return null;

            // 대상에 걸린 원소 상태이상 확인
            DamageType? existingElement = GetExistingElementOnTarget(targetSkillManager);
            if (!existingElement.HasValue) return null;

            // 같은 원소면 반응 없음
            if (existingElement.Value == attackElement) return null;

            // 반응 테이블에서 찾기
            if (reactionTable.TryGetValue((existingElement.Value, attackElement), out var reaction))
            {
                return reaction;
            }

            return null;
        }

        /// <summary>
        /// 원소 반응 실행
        /// </summary>
        public float ExecuteElementalReaction(ElementalReaction reaction, float baseDamage,
            Vector3 position, ulong attackerClientId, SkillManager targetSkillManager)
        {
            float reactionDamage = baseDamage * reaction.damageMultiplier * reactionDamageMultiplier;

            // 방어적 반응 (보호막, 치유 등)
            if (reaction.isDefensive)
            {
                if (reaction.removesDebuffs && targetSkillManager != null)
                {
                    // 디버프 제거
                    targetSkillManager.RemoveStatusEffect(StatusType.Poison);
                    targetSkillManager.RemoveStatusEffect(StatusType.Burn);
                    targetSkillManager.RemoveStatusEffect(StatusType.Slow);
                    targetSkillManager.RemoveStatusEffect(StatusType.Weakness);
                }
            }

            // 광역 데미지 (공격적 반응)
            if (!reaction.isDefensive && reaction.aoeRadius > 0)
            {
                int reactionHitCount = Physics2D.OverlapCircleNonAlloc(position, reaction.aoeRadius, s_OverlapBuffer);
                for (int i = 0; i < reactionHitCount; i++)
                {
                    var monster = s_OverlapBuffer[i].GetComponent<MonsterEntity>();
                    if (monster != null)
                    {
                        DamageType dmgType = reaction.isTrueDamage ? DamageType.True : DamageType.Magical;
                        monster.TakeDamage(reactionDamage, dmgType, attackerClientId);
                    }
                }
            }

            // 상태이상 적용
            if (reaction.appliedStatus != StatusType.None && reaction.statusChance > 0)
            {
                if (Random.value < reaction.statusChance)
                {
                    if (targetSkillManager != null)
                    {
                        targetSkillManager.ApplyStatusEffectToSelf(new StatusEffect
                        {
                            type = reaction.appliedStatus,
                            value = reaction.isDefensive ? baseDamage * 0.3f : baseDamage * 0.1f,
                            duration = reaction.statusDuration,
                            tickInterval = 1f,
                            stackable = false
                        });
                    }
                }
            }

            // 카메라 이펙트
            var cam = CameraEffects.Instance;
            if (cam != null)
            {
                cam.ShakeMedium();
                cam.Flash(reaction.particleColor, 0.5f, 0.3f);
            }

            // 알림
            var notif = NotificationManager.Instance;
            if (notif != null)
            {
                notif.ShowNotification($"원소 반응: {reaction.reactionName}!", NotificationType.System);
            }

            OnElementalReaction?.Invoke(attackerClientId, reaction);
            return reactionDamage;
        }

        /// <summary>
        /// 대상에 걸린 원소 타입 확인
        /// </summary>
        private DamageType? GetExistingElementOnTarget(SkillManager skillManager)
        {
            if (skillManager.HasStatusEffect(StatusType.Burn))
                return DamageType.Fire;
            if (skillManager.HasStatusEffect(StatusType.Freeze))
                return DamageType.Ice;
            if (skillManager.HasStatusEffect(StatusType.Poison))
                return DamageType.Poison;

            // 커스텀 원소 상태 확인 (번개, 암흑, 신성은 상태이상에 직접 매핑 없음)
            // → 원소 부착 시스템으로 별도 관리
            if (elementalAuras.TryGetValue(skillManager, out var aura))
            {
                if (Time.time - aura.timestamp < aura.duration)
                    return aura.element;
            }

            return null;
        }

        // 원소 부착 (번개/암흑/신성 등 상태이상에 직접 없는 원소)
        private Dictionary<SkillManager, ElementalAura> elementalAuras = new Dictionary<SkillManager, ElementalAura>();

        /// <summary>
        /// 원소 부착 - 스킬 공격 시 대상에 원소를 부착
        /// </summary>
        public void ApplyElementalAura(SkillManager target, DamageType element, float duration = 5f)
        {
            if (target == null) return;

            // 기본 상태이상으로 매핑되는 원소는 상태이상으로 처리
            if (element == DamageType.Fire || element == DamageType.Ice || element == DamageType.Poison)
                return; // SkillManager의 상태이상으로 처리됨

            elementalAuras[target] = new ElementalAura
            {
                element = element,
                timestamp = Time.time,
                duration = duration
            };
        }

        /// <summary>
        /// 대상의 원소 부착 제거
        /// </summary>
        public void ClearElementalAura(SkillManager target)
        {
            elementalAuras.Remove(target);
        }

        #endregion

        #region Skill Chain System

        // 스킬 체인 정의 (특정 순서로 사용 시 보너스)
        private static readonly SkillChainDefinition[] skillChains = new SkillChainDefinition[]
        {
            // 화염 연쇄: 화염계 3연타 → 대폭발
            new SkillChainDefinition
            {
                chainName = "화염 연쇄",
                requiredDamageTypes = new[] { DamageType.Fire, DamageType.Fire, DamageType.Fire },
                bonusDamageMultiplier = 2.0f,
                bonusStatus = StatusType.Burn,
                bonusStatusDuration = 8f,
                bonusAoERadius = 5f
            },
            // 빙뢰 연쇄: 빙결 → 번개 → 빙결 → 방어 분쇄
            new SkillChainDefinition
            {
                chainName = "빙뢰 연쇄",
                requiredDamageTypes = new[] { DamageType.Ice, DamageType.Lightning, DamageType.Ice },
                bonusDamageMultiplier = 1.8f,
                bonusStatus = StatusType.Weakness,
                bonusStatusDuration = 10f,
                bonusAoERadius = 3f
            },
            // 암독 연쇄: 독 → 암흑 → 독 → 역병 확산
            new SkillChainDefinition
            {
                chainName = "암독 연쇄",
                requiredDamageTypes = new[] { DamageType.Poison, DamageType.Dark, DamageType.Poison },
                bonusDamageMultiplier = 1.7f,
                bonusStatus = StatusType.Poison,
                bonusStatusDuration = 12f,
                bonusAoERadius = 6f
            },
            // 신성 심판: 신성 → 화염 → 신성 → 정화의 불꽃
            new SkillChainDefinition
            {
                chainName = "신성 심판",
                requiredDamageTypes = new[] { DamageType.Holy, DamageType.Fire, DamageType.Holy },
                bonusDamageMultiplier = 2.5f,
                bonusStatus = StatusType.None,
                bonusStatusDuration = 0f,
                bonusAoERadius = 4f
            },
            // 물리 연타: 물리 5연타 → 관통 일격
            new SkillChainDefinition
            {
                chainName = "물리 연타",
                requiredDamageTypes = new[] { DamageType.Physical, DamageType.Physical, DamageType.Physical, DamageType.Physical, DamageType.Physical },
                bonusDamageMultiplier = 2.0f,
                bonusStatus = StatusType.Stun,
                bonusStatusDuration = 3f,
                bonusAoERadius = 2f
            },
        };

        /// <summary>
        /// 스킬 체인 완성 확인
        /// </summary>
        public SkillChainDefinition CheckSkillChain(ulong clientId)
        {
            if (!playerCombos.TryGetValue(clientId, out var state))
                return null;

            if (state.skillHistory.Count < 3)
                return null;

            foreach (var chain in skillChains)
            {
                if (MatchesChain(state.skillHistory, chain))
                    return chain;
            }

            return null;
        }

        private bool MatchesChain(List<SkillUseRecord> history, SkillChainDefinition chain)
        {
            int chainLen = chain.requiredDamageTypes.Length;
            if (history.Count < chainLen) return false;

            // 최근 N개가 체인과 일치하는지 확인
            int startIdx = history.Count - chainLen;
            for (int i = 0; i < chainLen; i++)
            {
                if (history[startIdx + i].damageType != chain.requiredDamageTypes[i])
                    return false;

                // 체인 내 스킬 간 시간 간격 체크 (comboWindow 이내)
                if (i > 0)
                {
                    float timeDiff = history[startIdx + i].timestamp - history[startIdx + i - 1].timestamp;
                    if (timeDiff > comboWindow) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 스킬 체인 보너스 적용
        /// </summary>
        public float ApplySkillChainBonus(SkillChainDefinition chain, float baseDamage,
            Vector3 position, ulong attackerClientId)
        {
            float chainDamage = baseDamage * chain.bonusDamageMultiplier;

            // 광역 데미지
            if (chain.bonusAoERadius > 0)
            {
                int chainHitCount = Physics2D.OverlapCircleNonAlloc(position, chain.bonusAoERadius, s_OverlapBuffer);
                for (int i = 0; i < chainHitCount; i++)
                {
                    var monster = s_OverlapBuffer[i].GetComponent<MonsterEntity>();
                    if (monster != null)
                    {
                        monster.TakeDamage(chainDamage * 0.5f, DamageType.True, attackerClientId);

                        // 상태이상 적용
                        if (chain.bonusStatus != StatusType.None)
                        {
                            var skillMgr = monster.GetComponent<SkillManager>();
                            if (skillMgr != null)
                            {
                                skillMgr.ApplyStatusEffectToSelf(new StatusEffect
                                {
                                    type = chain.bonusStatus,
                                    value = baseDamage * 0.05f,
                                    duration = chain.bonusStatusDuration,
                                    tickInterval = 1f,
                                    stackable = false
                                });
                            }
                        }
                    }
                }
            }

            // 카메라 이펙트
            var cam = CameraEffects.Instance;
            if (cam != null)
            {
                cam.ShakeHeavy();
                cam.FlashGold();
            }

            // 알림
            var notif = NotificationManager.Instance;
            if (notif != null)
            {
                notif.ShowNotification($"스킬 체인: {chain.chainName}!", NotificationType.Achievement);
            }

            // 히스토리 리셋 (체인 소비)
            if (playerCombos.TryGetValue(attackerClientId, out var state))
            {
                state.skillHistory.Clear();
            }

            return chainDamage;
        }

        #endregion

        private void OnDestroy()
        {
            if (Instance == this)
            {
                OnComboUpdated = null;
                OnElementalReaction = null;
                Instance = null;
            }
        }
    }

    #region Data Structures

    /// <summary>
    /// 플레이어 콤보 상태
    /// </summary>
    public class ComboState
    {
        public int comboCount;
        public float lastSkillTime;
        public string lastSkillId;
        public DamageType lastDamageType;
        public List<SkillUseRecord> skillHistory = new List<SkillUseRecord>();
    }

    /// <summary>
    /// 스킬 사용 기록
    /// </summary>
    public struct SkillUseRecord
    {
        public string skillId;
        public DamageType damageType;
        public float timestamp;
    }

    /// <summary>
    /// 원소 반응 정의
    /// </summary>
    [System.Serializable]
    public struct ElementalReaction
    {
        public string reactionName;
        public string reactionNameEn;
        public float damageMultiplier;
        public float aoeRadius;
        public StatusType appliedStatus;
        public float statusDuration;
        public float statusChance;
        public bool isDefensive;
        public bool isTrueDamage;
        public bool removesDebuffs;
        public Color particleColor;
    }

    /// <summary>
    /// 원소 부착 상태 (번개/암흑/신성 등)
    /// </summary>
    public struct ElementalAura
    {
        public DamageType element;
        public float timestamp;
        public float duration;
    }

    /// <summary>
    /// 스킬 체인 정의
    /// </summary>
    public class SkillChainDefinition
    {
        public string chainName;
        public DamageType[] requiredDamageTypes;
        public float bonusDamageMultiplier;
        public StatusType bonusStatus;
        public float bonusStatusDuration;
        public float bonusAoERadius;
    }

    #endregion
}
