using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 인간 종족 스킬 생성기 - 전사계열, 성기사계열, 도적계열, 궁수계열
    /// </summary>
    public static class HumanSkills
    {
        /// <summary>
        /// 모든 인간 스킬 생성
        /// </summary>
        public static SkillData[] CreateAllHumanSkills()
        {
            var skills = new System.Collections.Generic.List<SkillData>();
            
            // 전사계열 스킬
            skills.AddRange(CreateWarriorSkills());
            
            // 성기사계열 스킬
            skills.AddRange(CreatePaladinSkills());
            
            // 도적계열 스킬
            skills.AddRange(CreateRogueSkills());
            
            // 궁수계열 스킬
            skills.AddRange(CreateArcherSkills());
            
            return skills.ToArray();
        }
        
        /// <summary>
        /// 전사계열 스킬 생성
        /// </summary>
        private static SkillData[] CreateWarriorSkills()
        {
            return new SkillData[]
            {
                // 1티어 (3레벨)
                CreateSkill("human_warrior_power_strike", "강타", "강력한 일격으로 적에게 큰 피해를 입힙니다.", 
                    3, 100, SkillCategory.Warrior, 1, 
                    baseDamage: 25f, damageScaling: 1.5f, cooldown: 5f, manaCost: 15f),
                    
                CreateSkill("human_warrior_defensive_stance", "방어 자세", "방어력을 증가시키고 받는 피해를 감소시킵니다.",
                    3, 150, SkillCategory.Warrior, 1, skillType: SkillType.Toggle,
                    statBonus: new StatBlock { defense = 5 }),
                
                // 2티어 (6레벨)
                CreateSkill("human_warrior_charge", "돌진", "적에게 돌진하여 피해를 주고 기절시킵니다.",
                    6, 500, SkillCategory.Warrior, 2,
                    baseDamage: 40f, damageScaling: 1.2f, cooldown: 8f, manaCost: 25f, range: 5f,
                    statusEffects: new StatusEffect[] { new StatusEffect { type = StatusType.Stun, duration = 1.5f } }),
                    
                CreateSkill("human_warrior_weapon_mastery", "무기 숙련", "모든 무기에 대한 숙련도가 증가합니다.",
                    6, 700, SkillCategory.Warrior, 2, skillType: SkillType.Passive,
                    statBonus: new StatBlock { strength = 3, agility = 2 }),
                
                // 3티어 (9레벨)
                CreateSkill("human_warrior_berserk", "광폭화", "공격력이 크게 증가하지만 방어력이 감소합니다.",
                    9, 2000, SkillCategory.Warrior, 3, skillType: SkillType.Toggle,
                    statusEffects: new StatusEffect[] { 
                        new StatusEffect { type = StatusType.Strength, value = 10f, duration = 30f },
                        new StatusEffect { type = StatusType.Weakness, value = -5f, duration = 30f }
                    }),
                    
                CreateSkill("human_warrior_combo_attack", "연속 공격", "빠른 연속 공격으로 여러 번 타격합니다.",
                    9, 2500, SkillCategory.Warrior, 3,
                    baseDamage: 20f, damageScaling: 0.8f, cooldown: 10f, manaCost: 35f),
                
                // 4티어 (12레벨)
                CreateSkill("human_warrior_battlefield_domination", "전장의 지배자", "주변 적들의 능력을 약화시키고 아군을 강화합니다.",
                    12, 8000, SkillCategory.Warrior, 4,
                    cooldown: 30f, manaCost: 50f, range: 8f,
                    statusEffects: new StatusEffect[] { 
                        new StatusEffect { type = StatusType.Blessing, value = 5f, duration = 20f }
                    }),
                    
                CreateSkill("human_warrior_unbreakable_will", "불굴의 의지", "모든 상태이상에 면역이 되고 체력이 증가합니다.",
                    12, 10000, SkillCategory.Warrior, 4, skillType: SkillType.Passive,
                    statBonus: new StatBlock { vitality = 8, stability = 5 }, healthBonus: 50f),
                
                // 5티어 (15레벨)
                CreateSkill("human_warrior_mythic_strike", "신화적 일격", "전설적인 공격으로 엄청난 피해를 입힙니다.",
                    15, 50000, SkillCategory.Warrior, 5,
                    baseDamage: 200f, damageScaling: 3f, cooldown: 60f, manaCost: 100f, range: 3f)
            };
        }
        
        /// <summary>
        /// 성기사계열 스킬 생성
        /// </summary>
        private static SkillData[] CreatePaladinSkills()
        {
            return new SkillData[]
            {
                // 1티어 (3레벨)
                CreateSkill("human_paladin_heal", "치유술", "자신이나 아군의 체력을 회복시킵니다.",
                    3, 200, SkillCategory.Paladin, 1,
                    baseDamage: 30f, damageScaling: 1.2f, cooldown: 3f, manaCost: 20f, range: 5f),
                    
                CreateSkill("human_paladin_holy_light", "신성한 빛", "언데드에게 큰 피해를 주고 아군을 치유합니다.",
                    3, 250, SkillCategory.Paladin, 1,
                    baseDamage: 40f, damageScaling: 1.5f, cooldown: 6f, manaCost: 25f, 
                    damageType: DamageType.Holy),
                
                // 2티어 (6레벨)
                CreateSkill("human_paladin_shield", "방어막", "마법 보호막을 생성하여 피해를 흡수합니다.",
                    6, 800, SkillCategory.Paladin, 2,
                    cooldown: 15f, manaCost: 40f,
                    statusEffects: new StatusEffect[] { 
                        new StatusEffect { type = StatusType.Shield, value = 100f, duration = 20f }
                    }),
                    
                CreateSkill("human_paladin_purify", "정화술", "모든 디버프와 독을 제거합니다.",
                    6, 600, SkillCategory.Paladin, 2,
                    cooldown: 8f, manaCost: 30f, range: 5f),
                
                // 3티어 (9레벨)
                CreateSkill("human_paladin_divine_wrath", "신성한 분노", "신의 분노로 주변 적들을 심판합니다.",
                    9, 3000, SkillCategory.Paladin, 3,
                    baseDamage: 80f, damageScaling: 2f, cooldown: 20f, manaCost: 60f, range: 6f,
                    damageType: DamageType.Holy),
                    
                CreateSkill("human_paladin_greater_heal", "대치유술", "강력한 치유로 많은 체력을 회복합니다.",
                    9, 2800, SkillCategory.Paladin, 3,
                    baseDamage: 80f, damageScaling: 2f, cooldown: 8f, manaCost: 50f, range: 5f),
                
                // 4티어 (12레벨)
                CreateSkill("human_paladin_divine_protection", "천사의 가호", "강력한 보호막과 재생 효과를 부여합니다.",
                    12, 12000, SkillCategory.Paladin, 4,
                    cooldown: 45f, manaCost: 80f,
                    statusEffects: new StatusEffect[] { 
                        new StatusEffect { type = StatusType.Shield, value = 200f, duration = 30f },
                        new StatusEffect { type = StatusType.Regeneration, value = 5f, duration = 30f }
                    }),
                    
                CreateSkill("human_paladin_judgment_hammer", "심판의 망치", "신의 심판으로 적을 강타합니다.",
                    12, 15000, SkillCategory.Paladin, 4,
                    baseDamage: 150f, damageScaling: 2.5f, cooldown: 25f, manaCost: 70f, range: 4f,
                    damageType: DamageType.Holy),
                
                // 5티어 (15레벨)
                CreateSkill("human_paladin_divine_descent", "신의 강림", "신이 강림하여 주변을 정화하고 적들을 심판합니다.",
                    15, 80000, SkillCategory.Paladin, 5,
                    baseDamage: 300f, damageScaling: 4f, cooldown: 120f, manaCost: 150f, range: 10f,
                    damageType: DamageType.Holy)
            };
        }
        
        /// <summary>
        /// 도적계열 스킬 생성
        /// </summary>
        private static SkillData[] CreateRogueSkills()
        {
            return new SkillData[]
            {
                // 1티어 (3레벨)
                CreateSkill("human_rogue_stealth", "은신", "잠시 동안 모습을 감춰 적의 공격을 회피합니다.",
                    3, 120, SkillCategory.Rogue, 1,
                    cooldown: 12f, manaCost: 25f,
                    statusEffects: new StatusEffect[] { 
                        new StatusEffect { type = StatusType.Speed, value = 2f, duration = 5f }
                    }),
                    
                CreateSkill("human_rogue_backstab", "백스탭", "적의 뒤에서 치명적인 공격을 가합니다.",
                    3, 150, SkillCategory.Rogue, 1,
                    baseDamage: 35f, damageScaling: 1.8f, cooldown: 6f, manaCost: 20f,
                    minDamagePercent: 150f, maxDamagePercent: 250f), // 높은 크리티컬 확률
                
                // 2티어 (6레벨)
                CreateSkill("human_rogue_poison_blade", "독날", "무기에 독을 발라 지속 피해를 입힙니다.",
                    6, 450, SkillCategory.Rogue, 2,
                    baseDamage: 20f, damageScaling: 1f, cooldown: 4f, manaCost: 15f,
                    statusEffects: new StatusEffect[] { 
                        new StatusEffect { type = StatusType.Poison, value = 5f, duration = 10f, tickInterval = 1f }
                    }),
                    
                CreateSkill("human_rogue_quick_step", "순보", "순간적으로 이동 속도가 크게 증가합니다.",
                    6, 600, SkillCategory.Rogue, 2,
                    cooldown: 10f, manaCost: 30f,
                    statusEffects: new StatusEffect[] { 
                        new StatusEffect { type = StatusType.Speed, value = 5f, duration = 8f }
                    }),
                
                // 3티어 (9레벨) 이하 생략...
            };
        }
        
        /// <summary>
        /// 궁수계열 스킬 생성
        /// </summary>
        private static SkillData[] CreateArcherSkills()
        {
            return new SkillData[]
            {
                // 1티어 (3레벨)
                CreateSkill("human_archer_power_shot", "강력한 사격", "관통력이 높은 화살을 발사합니다.",
                    3, 110, SkillCategory.Archer, 1,
                    baseDamage: 30f, damageScaling: 1.4f, cooldown: 4f, manaCost: 15f, range: 8f),
                    
                CreateSkill("human_archer_eagle_eye", "독수리의 눈", "명중률과 사거리가 증가합니다.",
                    3, 180, SkillCategory.Archer, 1, skillType: SkillType.Passive,
                    statBonus: new StatBlock { agility = 3, luck = 2 }),
                
                // 추가 스킬들...
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
            skill.requiredRace = Race.Human;
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