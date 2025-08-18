using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 엘프 종족 스킬 생성기 - 자연마법, 활술, 은밀, 정령술
    /// </summary>
    public static class ElfSkills
    {
        /// <summary>
        /// 모든 엘프 스킬 생성
        /// </summary>
        public static SkillData[] CreateAllElfSkills()
        {
            var skills = new System.Collections.Generic.List<SkillData>();
            
            // 자연마법 스킬
            skills.AddRange(CreateNatureMagicSkills());
            
            // 활술 스킬
            skills.AddRange(CreateArcherySkills());
            
            // 은밀 스킬
            skills.AddRange(CreateStealthSkills());
            
            // 정령술 스킬
            skills.AddRange(CreateSpiritSkills());
            
            return skills.ToArray();
        }
        
        /// <summary>
        /// 자연마법 스킬 생성
        /// </summary>
        private static SkillData[] CreateNatureMagicSkills()
        {
            return new SkillData[]
            {
                // 1티어 (3레벨)
                CreateSkill("elf_nature_thorn_shot", "가시 발사", "자연의 가시를 발사하여 적을 공격합니다.",
                    3, 120, SkillCategory.Nature, 1,
                    baseDamage: 20f, damageScaling: 1.3f, cooldown: 3f, manaCost: 15f, range: 6f,
                    damageType: DamageType.Magical),
                    
                CreateSkill("elf_nature_heal", "자연 치유", "자연의 힘으로 체력을 회복합니다.",
                    3, 180, SkillCategory.Nature, 1,
                    baseDamage: 25f, damageScaling: 1.2f, cooldown: 4f, manaCost: 20f, range: 5f),
                
                // 2티어 (6레벨)
                CreateSkill("elf_nature_entangle", "덩굴 속박", "적을 덩굴로 속박하여 움직임을 봉쇄합니다.",
                    6, 600, SkillCategory.Nature, 2,
                    baseDamage: 15f, damageScaling: 0.8f, cooldown: 8f, manaCost: 30f, range: 7f,
                    statusEffects: new StatusEffect[] { 
                        new StatusEffect { type = StatusType.Root, duration = 3f }
                    }),
                
                // 기타 티어들...
            };
        }
        
        /// <summary>
        /// 활술 스킬 생성
        /// </summary>
        private static SkillData[] CreateArcherySkills()
        {
            return new SkillData[]
            {
                // 1티어 (3레벨)
                CreateSkill("elf_archery_precise_shot", "정밀 사격", "정확한 사격으로 치명타 확률이 증가합니다.",
                    3, 140, SkillCategory.Archery, 1,
                    baseDamage: 25f, damageScaling: 1.4f, cooldown: 4f, manaCost: 18f, range: 10f,
                    minDamagePercent: 120f, maxDamagePercent: 200f),
                
                // 기타 스킬들...
            };
        }
        
        /// <summary>
        /// 은밀 스킬 생성
        /// </summary>
        private static SkillData[] CreateStealthSkills()
        {
            return new SkillData[]
            {
                // 1티어 (3레벨)
                CreateSkill("elf_stealth_camouflage", "위장술", "자연과 하나가 되어 은신합니다.",
                    3, 160, SkillCategory.Stealth, 1,
                    cooldown: 15f, manaCost: 25f,
                    statusEffects: new StatusEffect[] { 
                        new StatusEffect { type = StatusType.Invisibility, duration = 8f }
                    }),
                
                // 기타 스킬들...
            };
        }
        
        /// <summary>
        /// 정령술 스킬 생성
        /// </summary>
        private static SkillData[] CreateSpiritSkills()
        {
            return new SkillData[]
            {
                // 1티어 (3레벨)
                CreateSkill("elf_spirit_wind_blade", "바람의 칼날", "바람 정령의 힘으로 적을 베어냅니다.",
                    3, 200, SkillCategory.Spirit, 1,
                    baseDamage: 30f, damageScaling: 1.5f, cooldown: 5f, manaCost: 25f, range: 8f,
                    damageType: DamageType.Magical),
                
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
            skill.requiredRace = Race.Elf;
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