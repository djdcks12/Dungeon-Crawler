using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 기계족 종족 스킬 생성기 - 공학, 에너지, 방어, 해킹
    /// </summary>
    public static class MachinaSkills
    {
        /// <summary>
        /// 모든 기계족 스킬 생성
        /// </summary>
        public static SkillData[] CreateAllMachinaSkills()
        {
            var skills = new System.Collections.Generic.List<SkillData>();
            
            // 공학 스킬
            skills.AddRange(CreateEngineeringSkills());
            
            // 에너지 스킬
            skills.AddRange(CreateEnergySkills());
            
            // 방어 스킬
            skills.AddRange(CreateDefenseSkills());
            
            // 해킹 스킬
            skills.AddRange(CreateHackingSkills());
            
            return skills.ToArray();
        }
        
        /// <summary>
        /// 공학 스킬 생성
        /// </summary>
        private static SkillData[] CreateEngineeringSkills()
        {
            return new SkillData[]
            {
                // 1티어 (3레벨)
                CreateSkill("machina_eng_turret", "터렛 설치", "자동으로 적을 공격하는 터렛을 설치합니다.",
                    3, 150, SkillCategory.Engineering, 1,
                    baseDamage: 15f, damageScaling: 1f, cooldown: 20f, manaCost: 30f, range: 3f),
                    
                CreateSkill("machina_eng_repair", "자가 수리", "손상된 부위를 수리하여 체력을 회복합니다.",
                    3, 120, SkillCategory.Engineering, 1,
                    baseDamage: 20f, damageScaling: 1.1f, cooldown: 8f, manaCost: 25f),
                
                // 2티어 (6레벨)
                CreateSkill("machina_eng_upgrade", "시스템 업그레이드", "일시적으로 모든 능력치가 향상됩니다.",
                    6, 600, SkillCategory.Engineering, 2,
                    cooldown: 30f, manaCost: 50f,
                    statusEffects: new StatusEffect[] { 
                        new StatusEffect { type = StatusType.Enhancement, value = 5f, duration = 20f }
                    }),
                
                // 기타 티어들...
            };
        }
        
        /// <summary>
        /// 에너지 스킬 생성
        /// </summary>
        private static SkillData[] CreateEnergySkills()
        {
            return new SkillData[]
            {
                // 1티어 (3레벨)
                CreateSkill("machina_energy_blast", "에너지 폭발", "축적된 에너지를 방출하여 적을 공격합니다.",
                    3, 130, SkillCategory.Energy, 1,
                    baseDamage: 32f, damageScaling: 1.5f, cooldown: 4f, manaCost: 20f, range: 6f,
                    damageType: DamageType.Magical),
                
                // 기타 스킬들...
            };
        }
        
        /// <summary>
        /// 방어 스킬 생성
        /// </summary>
        private static SkillData[] CreateDefenseSkills()
        {
            return new SkillData[]
            {
                // 1티어 (3레벨)
                CreateSkill("machina_def_barrier", "에너지 방벽", "에너지 방벽을 생성하여 공격을 막습니다.",
                    3, 180, SkillCategory.Defense, 1,
                    cooldown: 15f, manaCost: 35f,
                    statusEffects: new StatusEffect[] { 
                        new StatusEffect { type = StatusType.Shield, value = 80f, duration = 15f }
                    }),
                
                // 기타 스킬들...
            };
        }
        
        /// <summary>
        /// 해킹 스킬 생성
        /// </summary>
        private static SkillData[] CreateHackingSkills()
        {
            return new SkillData[]
            {
                // 1티어 (3레벨)
                CreateSkill("machina_hack_disable", "시스템 무력화", "적의 시스템을 해킹하여 능력을 저하시킵니다.",
                    3, 160, SkillCategory.Hacking, 1,
                    cooldown: 12f, manaCost: 30f, range: 5f,
                    statusEffects: new StatusEffect[] { 
                        new StatusEffect { type = StatusType.Weakness, value = -3f, duration = 10f }
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
            skill.requiredRace = Race.Machina;
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