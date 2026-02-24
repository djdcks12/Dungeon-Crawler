using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// PvP 밸런싱 시스템
    /// 종족 간 상성, 킬/데스 리워드, 복수 시스템 관리
    /// </summary>
    public class PvPBalanceSystem : NetworkBehaviour
    {
        [Header("종족 밸런스 설정")]
        [SerializeField] private bool enableRaceBalance = true;
        [SerializeField] private float balanceMultiplier = 0.2f; // 밸런스 효과 강도 (20%)

        [Header("직업 역할 밸런스")]
        [SerializeField] private bool enableJobRoleBalance = true;
        [SerializeField] private float jobRoleMultiplier = 0.15f; // 직업 상성 효과 강도 (15%)

        [Header("PvP 전용 감쇄")]
        [SerializeField] private float pvpDamageReduction = 0.35f; // PvP 데미지 35% 감소
        [SerializeField] private float pvpCCDurationReduction = 0.40f; // CC 지속시간 40% 감소
        [SerializeField] private float pvpHealingReduction = 0.25f; // 힐링 25% 감소
        [SerializeField] private float pvpDotReduction = 0.30f; // DoT 데미지 30% 감소

        [Header("레벨 차이 보정")]
        [SerializeField] private float levelDiffDamagePerLevel = 0.05f; // 레벨당 5% 데미지 보정
        [SerializeField] private int maxLevelDiffEffect = 5; // 최대 5레벨 차이까지 적용

        [Header("킬/데스 리워드 설정")]
        [SerializeField] private float killExpMultiplier = 1.5f;
        [SerializeField] private float killGoldMultiplier = 1.0f;
        [SerializeField] private float deathExpPenalty = 0.1f; // 사망 시 경험치 10% 감소

        [Header("복수 시스템 설정")]
        [SerializeField] private bool enableRevengeSystem = true;
        [SerializeField] private float revengeBonus = 0.5f; // 복수 성공 시 추가 보너스 50%
        [SerializeField] private int maxRevengeStackTime = 300; // 복수 기회 지속 시간 (초)

        // 종족 상성표 (공격자 종족 -> 피해자 종족 -> 데미지 배율)
        private Dictionary<Race, Dictionary<Race, float>> raceAdvantages = new Dictionary<Race, Dictionary<Race, float>>();

        // 직업 역할 상성표 (공격자 역할 -> 방어자 역할 -> 데미지 배율)
        private Dictionary<JobRole, Dictionary<JobRole, float>> jobRoleAdvantages = new Dictionary<JobRole, Dictionary<JobRole, float>>();
        
        // 복수 시스템 데이터
        private Dictionary<ulong, Dictionary<ulong, RevengeData>> revengeTable = new Dictionary<ulong, Dictionary<ulong, RevengeData>>();
        
        // 킬 스트릭 데이터
        private Dictionary<ulong, int> killStreaks = new Dictionary<ulong, int>();
        
        // 싱글톤 패턴
        private static PvPBalanceSystem instance;
        public static PvPBalanceSystem Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<PvPBalanceSystem>();
                }
                return instance;
            }
        }
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeRaceAdvantages();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (instance == this)
                instance = null;
        }

        /// <summary>
        /// 종족 상성표 초기화
        /// </summary>
        private void InitializeRaceAdvantages()
        {
            raceAdvantages.Clear();
            
            // 인간 - 균형형 (모든 종족에 대해 중립)
            raceAdvantages[Race.Human] = new Dictionary<Race, float>
            {
                { Race.Human, 1.0f },
                { Race.Elf, 1.0f },
                { Race.Beast, 1.0f },
                { Race.Machina, 1.0f }
            };
            
            // 엘프 - 마법형 (기계족에 강함, 수인에 약함)
            raceAdvantages[Race.Elf] = new Dictionary<Race, float>
            {
                { Race.Human, 1.0f },
                { Race.Elf, 1.0f },
                { Race.Beast, 0.8f },     // 수인에게 약함 (마법 저항)
                { Race.Machina, 1.3f }    // 기계족에게 강함 (전자기 간섭)
            };
            
            // 수인 - 물리형 (엘프에 강함, 기계족에 약함)
            raceAdvantages[Race.Beast] = new Dictionary<Race, float>
            {
                { Race.Human, 1.0f },
                { Race.Elf, 1.3f },       // 엘프에게 강함 (높은 마법 저항)
                { Race.Beast, 1.0f },
                { Race.Machina, 0.8f }    // 기계족에게 약함 (장갑 관통 어려움)
            };
            
            // 기계족 - 방어형 (수인에 강함, 엘프에 약함)
            raceAdvantages[Race.Machina] = new Dictionary<Race, float>
            {
                { Race.Human, 1.0f },
                { Race.Elf, 0.8f },       // 엘프에게 약함 (전자기 간섭 취약)
                { Race.Beast, 1.3f },     // 수인에게 강함 (높은 방어력)
                { Race.Machina, 1.0f }
            };
            
            InitializeJobRoleAdvantages();
            Debug.Log("[PvP] Race & Job balance system initialized");
        }

        /// <summary>
        /// 직업 역할 상성표 초기화
        /// MeleeDPS > RangedDPS > Support > Defense > MeleeDPS (순환)
        /// Information은 모든 역할에 중립
        /// </summary>
        private void InitializeJobRoleAdvantages()
        {
            jobRoleAdvantages.Clear();

            // Information - 정보형 (중립, 약간의 범용성)
            jobRoleAdvantages[JobRole.Information] = new Dictionary<JobRole, float>
            {
                { JobRole.Information, 1.0f },
                { JobRole.Defense, 1.0f },
                { JobRole.MeleeDPS, 1.0f },
                { JobRole.RangedDPS, 1.0f },
                { JobRole.Support, 1.0f }
            };

            // Defense - 방어형 (원거리에 강함, 근접에 약함)
            jobRoleAdvantages[JobRole.Defense] = new Dictionary<JobRole, float>
            {
                { JobRole.Information, 1.0f },
                { JobRole.Defense, 1.0f },
                { JobRole.MeleeDPS, 0.8f },      // 근접에 약함 (장기전 소모)
                { JobRole.RangedDPS, 1.25f },     // 원거리에 강함 (접근 후 제압)
                { JobRole.Support, 1.1f }
            };

            // MeleeDPS - 근접 딜러 (방어에 강함, 원거리에 약함)
            jobRoleAdvantages[JobRole.MeleeDPS] = new Dictionary<JobRole, float>
            {
                { JobRole.Information, 1.0f },
                { JobRole.Defense, 1.25f },       // 방어에 강함 (높은 DPS로 돌파)
                { JobRole.MeleeDPS, 1.0f },
                { JobRole.RangedDPS, 0.85f },     // 원거리에 약함 (접근 어려움)
                { JobRole.Support, 1.15f }
            };

            // RangedDPS - 원거리 딜러 (근접에 강함, 방어에 약함)
            jobRoleAdvantages[JobRole.RangedDPS] = new Dictionary<JobRole, float>
            {
                { JobRole.Information, 1.0f },
                { JobRole.Defense, 0.8f },        // 방어에 약함 (방어력으로 버팀)
                { JobRole.MeleeDPS, 1.2f },       // 근접에 강함 (거리 유지)
                { JobRole.RangedDPS, 1.0f },
                { JobRole.Support, 1.1f }
            };

            // Support - 지원형 (정보에 강함, 나머지에 약함)
            jobRoleAdvantages[JobRole.Support] = new Dictionary<JobRole, float>
            {
                { JobRole.Information, 1.15f },
                { JobRole.Defense, 0.9f },
                { JobRole.MeleeDPS, 0.85f },      // 근접에 약함 (낮은 DPS)
                { JobRole.RangedDPS, 0.9f },
                { JobRole.Support, 1.0f }
            };
        }
        
        /// <summary>
        /// 종족 상성을 고려한 데미지 계산
        /// </summary>
        public float CalculateRaceBalancedDamage(Race attackerRace, Race targetRace, float baseDamage)
        {
            if (!enableRaceBalance) return baseDamage;
            
            if (raceAdvantages.ContainsKey(attackerRace) && raceAdvantages[attackerRace].ContainsKey(targetRace))
            {
                float raceMultiplier = raceAdvantages[attackerRace][targetRace];
                float balancedMultiplier = 1.0f + ((raceMultiplier - 1.0f) * balanceMultiplier);
                
                float finalDamage = baseDamage * balancedMultiplier;
                
                if (raceMultiplier != 1.0f)
                {
                    string effectText = raceMultiplier > 1.0f ? "효과적" : "비효과적";
                    Debug.Log($"⚔️ 종족 상성: {attackerRace} vs {targetRace} - {effectText}! (x{balancedMultiplier:F2})");
                }
                
                return finalDamage;
            }
            
            return baseDamage;
        }
        
        /// <summary>
        /// 직업 역할 상성을 고려한 데미지 계산
        /// </summary>
        public float CalculateJobRoleBalancedDamage(JobRole attackerRole, JobRole targetRole, float baseDamage)
        {
            if (!enableJobRoleBalance) return baseDamage;

            if (jobRoleAdvantages.TryGetValue(attackerRole, out var targetMap) &&
                targetMap.TryGetValue(targetRole, out float roleMultiplier))
            {
                float adjustedMultiplier = 1.0f + ((roleMultiplier - 1.0f) * jobRoleMultiplier);
                return baseDamage * adjustedMultiplier;
            }

            return baseDamage;
        }

        /// <summary>
        /// 전체 PvP 데미지 계산 (종족 + 직업 + 레벨 + PvP 감쇄 통합)
        /// </summary>
        public float CalculateFullPvPDamage(
            float baseDamage,
            Race attackerRace, Race targetRace,
            JobRole attackerRole, JobRole targetRole,
            int attackerLevel, int targetLevel)
        {
            float damage = baseDamage;

            // 1. PvP 전용 데미지 감소
            damage *= (1f - pvpDamageReduction);

            // 2. 종족 상성 적용
            damage = CalculateRaceBalancedDamage(attackerRace, targetRace, damage);

            // 3. 직업 역할 상성 적용
            damage = CalculateJobRoleBalancedDamage(attackerRole, targetRole, damage);

            // 4. 레벨 차이 보정
            int levelDiff = Mathf.Clamp(attackerLevel - targetLevel, -maxLevelDiffEffect, maxLevelDiffEffect);
            float levelMultiplier = 1f + (levelDiff * levelDiffDamagePerLevel);
            damage *= levelMultiplier;

            return Mathf.Max(1f, damage);
        }

        /// <summary>
        /// PvP CC(군중제어) 지속시간 감소 적용
        /// </summary>
        public float CalculatePvPCCDuration(float baseDuration)
        {
            return baseDuration * (1f - pvpCCDurationReduction);
        }

        /// <summary>
        /// PvP 힐링 감소 적용
        /// </summary>
        public float CalculatePvPHealing(float baseHealing)
        {
            return baseHealing * (1f - pvpHealingReduction);
        }

        /// <summary>
        /// PvP DoT 데미지 감소 적용
        /// </summary>
        public float CalculatePvPDotDamage(float baseDotDamage)
        {
            return baseDotDamage * (1f - pvpDotReduction);
        }

        /// <summary>
        /// PvP 킬 처리 및 보상 계산
        /// </summary>
        public PvPReward CalculatePvPKillReward(ulong killerClientId, ulong victimClientId, int victimLevel)
        {
            var reward = new PvPReward();
            
            // 기본 보상 계산
            reward.baseExpReward = (long)(victimLevel * 100 * killExpMultiplier);
            reward.baseGoldReward = (long)(victimLevel * 50 * killGoldMultiplier);
            
            // 킬 스트릭 보너스
            int killStreak = GetKillStreak(killerClientId) + 1;
            SetKillStreak(killerClientId, killStreak);
            ResetKillStreak(victimClientId); // 피해자의 킬 스트릭 리셋
            
            reward.killStreakBonus = Mathf.Min(killStreak * 0.1f, 1.0f); // 최대 100% 보너스
            
            // 복수 시스템 체크
            bool isRevenge = CheckAndProcessRevenge(killerClientId, victimClientId);
            if (isRevenge)
            {
                reward.revengeBonus = revengeBonus;
                reward.isRevenge = true;
            }
            
            // 최종 보상 계산
            float totalMultiplier = 1.0f + reward.killStreakBonus + reward.revengeBonus;
            reward.finalExpReward = (long)(reward.baseExpReward * totalMultiplier);
            reward.finalGoldReward = (long)(reward.baseGoldReward * totalMultiplier);
            
            // 복수 등록 (피해자가 가해자를 복수할 수 있도록)
            RegisterRevengeTarget(victimClientId, killerClientId);
            
            Debug.Log($"💀 PvP Kill: {killerClientId} → {victimClientId} | Streak: {killStreak} | Revenge: {isRevenge}");
            
            return reward;
        }
        
        /// <summary>
        /// PvP 사망 페널티 계산
        /// </summary>
        public PvPPenalty CalculatePvPDeathPenalty(ulong victimClientId, long currentExp, long currentGold)
        {
            var penalty = new PvPPenalty();
            
            // 경험치 페널티
            penalty.expLoss = (long)(currentExp * deathExpPenalty);
            
            // 골드 드롭 (일부)
            penalty.goldDrop = (long)(currentGold * 0.05f); // 5% 골드 드롭
            
            // 킬 스트릭 리셋
            penalty.killStreakLost = GetKillStreak(victimClientId);
            ResetKillStreak(victimClientId);
            
            Debug.Log($"💀 PvP Death Penalty: ClientId {victimClientId} | Exp: -{penalty.expLoss} | Gold Drop: {penalty.goldDrop}");
            
            return penalty;
        }
        
        /// <summary>
        /// 킬 스트릭 가져오기
        /// </summary>
        public int GetKillStreak(ulong clientId)
        {
            return killStreaks.ContainsKey(clientId) ? killStreaks[clientId] : 0;
        }
        
        /// <summary>
        /// 킬 스트릭 설정
        /// </summary>
        private void SetKillStreak(ulong clientId, int streak)
        {
            killStreaks[clientId] = streak;
            
            // 킬 스트릭 업적 체크
            if (streak > 0 && streak % 5 == 0)
            {
                Debug.Log($"🔥 Kill Streak Achievement! {clientId} has {streak} kills in a row!");
            }
        }
        
        /// <summary>
        /// 킬 스트릭 리셋
        /// </summary>
        private void ResetKillStreak(ulong clientId)
        {
            if (killStreaks.ContainsKey(clientId))
            {
                killStreaks[clientId] = 0;
            }
        }
        
        /// <summary>
        /// 복수 타겟 등록
        /// </summary>
        private void RegisterRevengeTarget(ulong revengerClientId, ulong targetClientId)
        {
            if (!enableRevengeSystem) return;
            
            if (!revengeTable.ContainsKey(revengerClientId))
            {
                revengeTable[revengerClientId] = new Dictionary<ulong, RevengeData>();
            }
            
            revengeTable[revengerClientId][targetClientId] = new RevengeData
            {
                targetClientId = targetClientId,
                registeredTime = Time.time,
                isActive = true
            };
            
            Debug.Log($"🗡️ Revenge target registered: {revengerClientId} can now take revenge on {targetClientId}");
        }
        
        /// <summary>
        /// 복수 체크 및 처리
        /// </summary>
        private bool CheckAndProcessRevenge(ulong killerClientId, ulong victimClientId)
        {
            if (!enableRevengeSystem) return false;
            
            if (revengeTable.ContainsKey(killerClientId) && 
                revengeTable[killerClientId].ContainsKey(victimClientId))
            {
                var revengeData = revengeTable[killerClientId][victimClientId];
                
                // 복수 시간 제한 체크
                if (revengeData.isActive && Time.time - revengeData.registeredTime < maxRevengeStackTime)
                {
                    // 복수 성공!
                    revengeTable[killerClientId].Remove(victimClientId);
                    Debug.Log($"⚡ REVENGE! {killerClientId} took revenge on {victimClientId}!");
                    return true;
                }
                else
                {
                    // 복수 시간 만료
                    revengeTable[killerClientId].Remove(victimClientId);
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 만료된 복수 데이터 정리 (주기적 호출)
        /// </summary>
        private void Update()
        {
            if (!IsServer || !enableRevengeSystem) return;
            
            // 5초마다 만료된 복수 데이터 정리
            if (Time.time % 5f < Time.deltaTime)
            {
                CleanupExpiredRevengeData();
            }
        }
        
        /// <summary>
        /// 만료된 복수 데이터 정리
        /// </summary>
        private void CleanupExpiredRevengeData()
        {
            var clientsToRemove = new List<ulong>();
            
            foreach (var clientEntry in revengeTable)
            {
                var targetsToRemove = new List<ulong>();
                
                foreach (var targetEntry in clientEntry.Value)
                {
                    if (Time.time - targetEntry.Value.registeredTime > maxRevengeStackTime)
                    {
                        targetsToRemove.Add(targetEntry.Key);
                    }
                }
                
                foreach (var targetId in targetsToRemove)
                {
                    clientEntry.Value.Remove(targetId);
                }
                
                if (clientEntry.Value.Count == 0)
                {
                    clientsToRemove.Add(clientEntry.Key);
                }
            }
            
            foreach (var clientId in clientsToRemove)
            {
                revengeTable.Remove(clientId);
            }
        }
        
        /// <summary>
        /// 현재 복수 대상 목록 가져오기
        /// </summary>
        public List<ulong> GetRevengeTargets(ulong clientId)
        {
            var targets = new List<ulong>();
            
            if (revengeTable.ContainsKey(clientId))
            {
                foreach (var entry in revengeTable[clientId])
                {
                    if (entry.Value.isActive && Time.time - entry.Value.registeredTime < maxRevengeStackTime)
                    {
                        targets.Add(entry.Key);
                    }
                }
            }
            
            return targets;
        }
        
        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        [ContextMenu("Show PvP Debug Info")]
        private void ShowDebugInfo()
        {
            Debug.Log("=== PvP Balance System Debug ===");
            Debug.Log($"Kill Streaks: {killStreaks.Count}");
            Debug.Log($"Revenge Entries: {revengeTable.Count}");
            
            foreach (var entry in killStreaks)
            {
                if (entry.Value > 0)
                {
                    Debug.Log($"- Client {entry.Key}: {entry.Value} kills");
                }
            }
        }
    }
    
    /// <summary>
    /// PvP 킬 보상 데이터
    /// </summary>
    [System.Serializable]
    public struct PvPReward
    {
        public long baseExpReward;
        public long baseGoldReward;
        public float killStreakBonus;
        public float revengeBonus;
        public bool isRevenge;
        public long finalExpReward;
        public long finalGoldReward;
    }
    
    /// <summary>
    /// PvP 사망 페널티 데이터
    /// </summary>
    [System.Serializable]
    public struct PvPPenalty
    {
        public long expLoss;
        public long goldDrop;
        public int killStreakLost;
    }
    
    /// <summary>
    /// 복수 시스템 데이터
    /// </summary>
    [System.Serializable]
    public class RevengeData
    {
        public ulong targetClientId;
        public float registeredTime;
        public bool isActive;
    }
}