using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 스킬 데이터 ScriptableObject - 골드 기반 스킬 시스템
    /// </summary>
    [CreateAssetMenu(fileName = "New Skill", menuName = "Dungeon Crawler/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("Basic Info")]
        public string skillName;
        public string skillId; // 고유 식별자
        [TextArea(3, 5)]
        public string description;
        public Sprite skillIcon;
        
        [Header("Requirements")]
        public int requiredLevel = 3;
        public long goldCost = 100;
        public Race requiredRace = Race.Human;
        public SkillCategory category = SkillCategory.Warrior;
        public int skillTier = 1; // 1-5티어
        
        [Header("Prerequisites")]
        public SkillData[] prerequisiteSkills; // 선행 스킬들
        
        [Header("Skill Effects")]
        public SkillType skillType = SkillType.Active;
        public DamageType damageType = DamageType.Physical;
        public float cooldown = 5f;
        public float manaCost = 10f;
        public float castTime = 1f;
        public float range = 2f;
        
        [Header("Damage/Healing")]
        public float baseDamage = 10f;
        public float damageScaling = 1f; // STR/INT 스케일링
        public float minDamagePercent = 80f; // 민댐 %
        public float maxDamagePercent = 120f; // 맥댐 %
        
        [Header("Stat Bonuses (Passive Skills)")]
        public StatBlock statBonus;
        public float healthBonus = 0f;
        public float manaBonus = 0f;
        public float moveSpeedBonus = 0f;
        public float attackSpeedBonus = 0f;
        
        [Header("Special Effects")]
        public StatusEffect[] statusEffects;
        public float statusDuration = 5f;
        public float statusChance = 100f; // 상태이상 적용 확률
        
        [Header("Visual Effects")]
        public GameObject castEffectPrefab;
        public GameObject hitEffectPrefab;
        public GameObject buffEffectPrefab;
        public AudioClip castSound;
        public AudioClip hitSound;
        
        /// <summary>
        /// 스킬 학습 가능 여부 확인
        /// </summary>
        public bool CanLearn(PlayerStats playerStats, System.Collections.Generic.List<string> learnedSkills)
        {
            // 레벨 요구사항 확인
            if (playerStats.CurrentLevel < requiredLevel)
            {
                return false;
            }
            
            // 종족 요구사항 확인
            if (playerStats.CharacterRace != requiredRace)
            {
                return false;
            }
            
            // 골드 요구사항 확인
            if (playerStats.Gold < goldCost)
            {
                return false;
            }
            
            // 이미 학습했는지 확인
            if (learnedSkills.Contains(skillId))
            {
                return false;
            }
            
            // 선행 스킬 확인
            if (prerequisiteSkills != null && prerequisiteSkills.Length > 0)
            {
                foreach (var prerequisite in prerequisiteSkills)
                {
                    if (prerequisite != null && !learnedSkills.Contains(prerequisite.skillId))
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 스킬 데미지 계산
        /// </summary>
        public float CalculateDamage(PlayerStats playerStats)
        {
            float baseStat = damageType == DamageType.Physical ? playerStats.TotalSTR : playerStats.TotalINT;
            float scaledDamage = baseDamage + (baseStat * damageScaling);
            
            // 민댐/맥댐 시스템 적용
            float minDamage = scaledDamage * (minDamagePercent / 100f);
            float maxDamage = scaledDamage * (maxDamagePercent / 100f);
            
            return Random.Range(minDamage, maxDamage);
        }
        
        /// <summary>
        /// 스킬 힐링량 계산
        /// </summary>
        public float CalculateHealing(PlayerStats playerStats)
        {
            float healingStat = playerStats.TotalINT; // 힐링은 INT 기반
            float scaledHealing = baseDamage + (healingStat * damageScaling);
            
            return scaledHealing;
        }
        
        /// <summary>
        /// 마나 소모량 계산 (레벨에 따른 조정)
        /// </summary>
        public float GetManaCost(int playerLevel)
        {
            // 높은 레벨일수록 마나 효율 개선
            float efficiency = 1f - (playerLevel * 0.01f); // 레벨당 1% 효율 증가
            return manaCost * Mathf.Max(0.5f, efficiency);
        }
        
        /// <summary>
        /// 쿨다운 계산 (AGI에 따른 조정)
        /// </summary>
        public float GetCooldown(PlayerStats playerStats)
        {
            float agiReduction = playerStats.TotalAGI * 0.01f; // AGI당 1% 쿨다운 감소
            return cooldown * (1f - Mathf.Min(0.5f, agiReduction)); // 최대 50% 감소
        }
        
        /// <summary>
        /// 스킬 설명 생성 (UI용)
        /// </summary>
        public string GetDetailedDescription(PlayerStats playerStats = null)
        {
            string desc = description + "\n\n";
            
            // 기본 정보
            desc += $"<color=yellow>필요 레벨:</color> {requiredLevel}\n";
            desc += $"<color=yellow>비용:</color> {goldCost} 골드\n";
            desc += $"<color=yellow>티어:</color> {skillTier}\n\n";
            
            // 스킬 효과
            if (skillType == SkillType.Active)
            {
                desc += $"<color=cyan>쿨다운:</color> {cooldown}초\n";
                desc += $"<color=cyan>마나 소모:</color> {manaCost}\n";
                desc += $"<color=cyan>사거리:</color> {range}m\n";
                
                if (baseDamage > 0)
                {
                    if (playerStats != null)
                    {
                        float damage = CalculateDamage(playerStats);
                        desc += $"<color=red>데미지:</color> {damage:F0}\n";
                    }
                    else
                    {
                        desc += $"<color=red>기본 데미지:</color> {baseDamage}\n";
                    }
                }
            }
            else
            {
                // 패시브 스킬 보너스
                if (statBonus.strength > 0) desc += $"<color=orange>STR +{statBonus.strength}</color>\n";
                if (statBonus.agility > 0) desc += $"<color=orange>AGI +{statBonus.agility}</color>\n";
                if (statBonus.vitality > 0) desc += $"<color=orange>VIT +{statBonus.vitality}</color>\n";
                if (statBonus.intelligence > 0) desc += $"<color=orange>INT +{statBonus.intelligence}</color>\n";
                if (healthBonus > 0) desc += $"<color=green>HP +{healthBonus}</color>\n";
                if (manaBonus > 0) desc += $"<color=blue>MP +{manaBonus}</color>\n";
            }
            
            // 선행 스킬
            if (prerequisiteSkills != null && prerequisiteSkills.Length > 0)
            {
                desc += "\n<color=gray>선행 스킬:</color>\n";
                foreach (var prereq in prerequisiteSkills)
                {
                    if (prereq != null)
                    {
                        desc += $"- {prereq.skillName}\n";
                    }
                }
            }
            
            return desc;
        }
    }
    
    /// <summary>
    /// 스킬 카테고리 (종족별 계열)
    /// </summary>
    public enum SkillCategory
    {
        // 인간
        Warrior,     // 전사계열
        Paladin,     // 성기사계열
        Rogue,       // 도적계열
        Archer,      // 궁수계열
        
        // 엘프
        ElementalMage,  // 원소마법
        PureMage,      // 순수마법
        NatureMage,    // 자연마법
        PsychicMage,   // 정신마법
        Nature,        // 자연 (추가)
        
        // 수인
        Berserker,     // 광전사
        Hunter,        // 사냥꾼
        Assassin,      // 암살자
        Beast,         // 야수
        
        // 기계족
        HeavyArmor,    // 중장갑
        Engineer,      // 기술자
        Artillery,     // 포격수
        Nanotech,      // 나노기술
        Engineering,   // 공학
        Energy,        // 에너지
        Defense,       // 방어
        Hacking,       // 해킹
        
        // 수인 추가
        Wild,          // 야성
        ShapeShift,    // 변신
        Hunt,          // 사냥
        Combat,        // 전투
        
        // 기타
        Archery,       // 궁술
        Stealth,       // 은신
        Spirit         // 정령술
    }
    
    /// <summary>
    /// 스킬 타입
    /// </summary>
    public enum SkillType
    {
        Active,     // 능동 스킬 (사용하는 스킬)
        Passive,    // 수동 스킬 (영구 효과)
        Toggle,     // 토글 스킬 (on/off)
        Triggered   // 조건부 발동 스킬
    }
    
    /// <summary>
    /// 상태이상 효과
    /// </summary>
    [System.Serializable]
    public struct StatusEffect
    {
        public StatusType type;
        public float value;         // 효과 수치
        public float duration;      // 지속시간
        public float tickInterval;  // DoT 틱 간격
        public bool stackable;      // 중첩 가능 여부
    }
    
    /// <summary>
    /// 상태이상 타입
    /// </summary>
    public enum StatusType
    {
        None,        // 상태이상 없음
        
        // 디버프
        Poison,      // 독
        Burn,        // 화상
        Freeze,      // 빙결
        Stun,        // 기절
        Slow,        // 감속
        Weakness,    // 약화 (데미지 감소)
        
        // 버프
        Strength,    // 힘 증가
        Speed,       // 속도 증가
        Regeneration,// 재생
        Shield,      // 보호막
        Blessing,    // 축복 (모든 스탯 증가)
        Berserk,     // 광폭화 (공격력 증가, 방어력 감소)
        Enhancement, // 능력 향상
        Root,        // 속박 (움직임 제한)
        Invisibility // 투명 (은신)
    }
}