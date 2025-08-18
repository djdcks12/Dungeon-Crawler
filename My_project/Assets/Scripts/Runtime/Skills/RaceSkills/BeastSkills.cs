using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 수인 종족 스킬 생성기 - 야성, 변신, 사냥, 격투
    /// </summary>
    public static class BeastSkills
    {
        /// <summary>
        /// 모든 수인 스킬 생성
        /// </summary>
        public static SkillData[] CreateAllBeastSkills()
        {
            var skills = new System.Collections.Generic.List<SkillData>();
            
            // 야성 스킬
            skills.AddRange(CreateWildSkills());
            
            // 변신 스킬
            skills.AddRange(CreateShapeShiftSkills());
            
            // 사냥 스킬
            skills.AddRange(CreateHuntSkills());
            
            // 격투 스킬
            skills.AddRange(CreateCombatSkills());
            
            return skills.ToArray();
        }
        
        /// <summary>
        /// 야성 스킬 생성
        /// </summary>
        private static SkillData[] CreateWildSkills()
        {
            return new SkillData[]
            {
                // 1티어 (3레벨)
                CreateSkill("beast_wild_claw_attack", "발톱 공격", "야성의 발톱으로 적을 찢어발깁니다.",
                    3, 100, SkillCategory.Wild, 1,
                    baseDamage: 28f, damageScaling: 1.6f, cooldown: 3f, manaCost: 12f,
                    minDamagePercent: 90f, maxDamagePercent: 140f),
                    
                CreateSkill("beast_wild_instinct", "야생 본능", "전투 본능이 깨어나 능력치가 증가합니다.",
                    3, 150, SkillCategory.Wild, 1, skillType: SkillType.Passive,
                    statBonus: new StatBlock { strength = 2, agility = 3, vitality = 1 }),
                
                // 2티어 (6레벨)
                CreateSkill("beast_wild_frenzy", "광폭화", "야성이 폭주하여 공격력과 속도가 크게 증가합니다.",
                    6, 500, SkillCategory.Wild, 2,
                    cooldown: 20f, manaCost: 40f,
                    statusEffects: new StatusEffect[] { 
                        new StatusEffect { type = StatusType.Strength, value = 8f, duration = 15f },
                        new StatusEffect { type = StatusType.Speed, value = 3f, duration = 15f }
                    }),
                
                // 기타 티어들...
            };
        }
        
        /// <summary>
        /// 변신 스킬 생성
        /// </summary>
        private static SkillData[] CreateShapeShiftSkills()
        {
            return new SkillData[]
            {
                // 1티어 (3레벨)
                CreateSkill("beast_shift_wolf_form", "늑대 변신", "늑대의 모습으로 변신하여 이동속도가 증가합니다.",
                    3, 200, SkillCategory.ShapeShift, 1, skillType: SkillType.Toggle,
                    statusEffects: new StatusEffect[] { 
                        new StatusEffect { type = StatusType.Speed, value = 4f, duration = 60f }
                    }),
                
                // 기타 스킬들...
            };
        }
        
        /// <summary>
        /// 사냥 스킬 생성
        /// </summary>
        private static SkillData[] CreateHuntSkills()
        {
            return new SkillData[]
            {
                // 1티어 (3레벨)
                CreateSkill("beast_hunt_track", "추적", "적의 흔적을 추적하여 위치를 파악합니다.",
                    3, 120, SkillCategory.Hunt, 1,
                    cooldown: 10f, manaCost: 20f, range: 15f),
                
                // 기타 스킬들...
            };
        }
        
        /// <summary>
        /// 격투 스킬 생성
        /// </summary>
        private static SkillData[] CreateCombatSkills()
        {
            return new SkillData[]
            {
                // 1티어 (3레벨)
                CreateSkill("beast_combat_slam", "강타", "강력한 타격으로 적을 기절시킵니다.",
                    3, 140, SkillCategory.Combat, 1,
                    baseDamage: 35f, damageScaling: 1.4f, cooldown: 6f, manaCost: 18f,
                    statusEffects: new StatusEffect[] { 
                        new StatusEffect { type = StatusType.Stun, duration = 1.5f }
                    }),
                
                // 기타 스킬들...
            };
        }
        
        /// <summary>
        /// 스킬 생성 헬퍼 메서드
        /// </summary>
        private static SkillData CreateSkill(string skillId, string skillName, string description,
            int requiredLevel, long goldCost, SkillCategory category, int tier,
            float baseDamage = 0f, float damageScaling = 1f, float cooldown = 0f, float manaCost = 0f,
            float range = 2f, SkillType skillType = SkillType.Active, DamageType damageType = DamageType.Physical,
            StatBlock statBonus = default, float healthBonus = 0f, float manaBonus = 0f,
            StatusEffect[] statusEffects = null, float minDamagePercent = 80f, float maxDamagePercent = 120f)
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            
            skill.skillId = skillId;
            skill.skillName = skillName;
            skill.description = description;
            skill.requiredLevel = requiredLevel;
            skill.goldCost = goldCost;
            skill.requiredRace = Race.Beast;
            skill.category = category;
            skill.skillTier = tier;
            skill.skillType = skillType;
            skill.damageType = damageType;
            skill.baseDamage = baseDamage;
            skill.damageScaling = damageScaling;
            skill.cooldown = cooldown;
            skill.manaCost = manaCost;
            skill.range = range;
            skill.statBonus = statBonus;
            skill.healthBonus = healthBonus;
            skill.manaBonus = manaBonus;
            skill.statusEffects = statusEffects ?? new StatusEffect[0];
            skill.minDamagePercent = minDamagePercent;
            skill.maxDamagePercent = maxDamagePercent;
            
            return skill;
        }
    }
}