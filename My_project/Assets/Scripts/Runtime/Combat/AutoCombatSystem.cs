using UnityEngine;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 자동 전투 시스템 - V키로 토글
    /// 가장 가까운 적으로 이동 + 자동 공격 + 스킬 자동 사용
    /// </summary>
    public class AutoCombatSystem : MonoBehaviour
    {
        [Header("설정")]
        [SerializeField] private KeyCode toggleKey = KeyCode.V;
        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private float attackInterval = 0.5f;
        [SerializeField] private float moveToEnemyStopDistance = 1.5f;
        [SerializeField] private bool useSkillsAutomatically = true;

        private PlayerController playerController;
        private CombatSystem combatSystem;
        private SkillManager skillManager;
        private PlayerStatsManager statsManager;
        private Rigidbody2D cachedRigidbody;

        private bool isAutoMode = false;
        private float lastAttackTime;
        private float lastSearchTime;
        private MonsterEntity currentTarget;

        // 타겟 검색용 버퍼 (GC 방지)
        private static readonly Collider2D[] s_SearchBuffer = new Collider2D[32];

        public bool IsAutoMode => isAutoMode;
        public System.Action<bool> OnAutoModeChanged;

        private void Start()
        {
            playerController = GetComponent<PlayerController>();
            combatSystem = GetComponent<CombatSystem>();
            skillManager = GetComponent<SkillManager>();
            statsManager = GetComponent<PlayerStatsManager>();
            cachedRigidbody = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (playerController == null || !playerController.IsOwner) return;

            if (Input.GetKeyDown(toggleKey))
            {
                ToggleAutoMode();
            }

            if (!isAutoMode) return;

            UpdateAutoMode();
        }

        /// <summary>
        /// 자동 전투 토글
        /// </summary>
        public void ToggleAutoMode()
        {
            isAutoMode = !isAutoMode;
            OnAutoModeChanged?.Invoke(isAutoMode);

            if (CombatLogUI.Instance != null)
                CombatLogUI.Instance.LogSystem(isAutoMode ? "자동 전투 ON (V)" : "자동 전투 OFF");

            if (!isAutoMode)
                currentTarget = null;
        }

        /// <summary>
        /// 자동 전투 로직
        /// </summary>
        private void UpdateAutoMode()
        {
            // 타겟 유효성 확인
            if (currentTarget != null && (currentTarget.IsDead || !currentTarget.gameObject.activeInHierarchy))
                currentTarget = null;

            // 타겟이 없으면 찾기
            if (currentTarget == null)
            {
                currentTarget = FindNearestEnemy();
                if (currentTarget == null) return; // 주변에 적 없음
            }

            float distToTarget = Vector2.Distance(transform.position, currentTarget.transform.position);

            // 공격 범위 밖이면 이동
            if (distToTarget > moveToEnemyStopDistance)
            {
                MoveToTarget(currentTarget.transform.position);
            }
            else
            {
                // 공격 범위 안 → 공격
                if (Time.time - lastAttackTime >= attackInterval)
                {
                    lastAttackTime = Time.time;

                    // 스킬 사용 시도
                    if (useSkillsAutomatically && TryUseSkill())
                        return;

                    // 기본 공격
                    if (combatSystem != null)
                        combatSystem.PerformBasicAttack();
                }
            }
        }

        /// <summary>
        /// 가장 가까운 적 찾기 (Physics2D NonAlloc - 0 GC)
        /// </summary>
        private MonsterEntity FindNearestEnemy()
        {
            // 검색 쿨다운 (0.2초) - 매 프레임 검색 방지
            if (Time.time - lastSearchTime < 0.2f) return null;
            lastSearchTime = Time.time;

            int count = Physics2D.OverlapCircleNonAlloc(
                transform.position, detectionRange, s_SearchBuffer);

            MonsterEntity nearest = null;
            float minDist = detectionRange;

            for (int i = 0; i < count; i++)
            {
                var monster = s_SearchBuffer[i].GetComponent<MonsterEntity>();
                if (monster == null || monster.IsDead) continue;

                float dist = Vector2.Distance(transform.position, monster.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = monster;
                }
            }

            return nearest;
        }

        /// <summary>
        /// 타겟으로 이동
        /// </summary>
        private void MoveToTarget(Vector3 targetPos)
        {
            Vector2 direction = (targetPos - transform.position).normalized;

            // PlayerController의 이동 시스템을 활용
            if (cachedRigidbody != null && statsManager != null)
            {
                float speed = statsManager.CurrentStats.TotalAGI * 0.5f + 3f;
                speed = Mathf.Clamp(speed, 2f, 8f);
                cachedRigidbody.linearVelocity = direction * speed;
            }
        }

        /// <summary>
        /// 스킬 자동 사용 시도
        /// </summary>
        private bool TryUseSkill()
        {
            if (skillManager == null || combatSystem == null) return false;

            var learnedSkills = skillManager.GetLearnedSkills();
            if (learnedSkills == null || learnedSkills.Count == 0) return false;

            // 사용 가능한 스킬 중 가장 강한 것 사용
            for (int i = 0; i < learnedSkills.Count; i++)
            {
                var skillId = learnedSkills[i];
                var skillData = skillManager.GetSkillData(skillId);
                if (skillData == null) continue;

                // 쿨다운 체크
                if (skillManager.IsSkillOnCooldown(skillId)) continue;

                // MP 체크
                if (statsManager != null && statsManager.CurrentStats.CurrentMP < skillData.manaCost)
                    continue;

                // 스킬 발동 (타겟 위치로)
                if (currentTarget != null)
                {
                    combatSystem.PerformSkillAttack(skillId, currentTarget.transform.position);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 자동 전투 중지 (죽었을 때 등)
        /// </summary>
        public void StopAutoMode()
        {
            if (isAutoMode)
            {
                isAutoMode = false;
                currentTarget = null;
                OnAutoModeChanged?.Invoke(false);
            }
        }

        private void OnDestroy()
        {
            OnAutoModeChanged = null;
        }
    }
}
