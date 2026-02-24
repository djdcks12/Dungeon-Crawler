using UnityEngine;
using UnityEditor;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 몬스터 스킬 데이터 40개 자동 생성 에디터 스크립트
    /// 물리공격 10, 마법공격 10, 방어/버프 8, 특수 8, 광역 4
    /// </summary>
    public class MonsterSkillDataGenerator
    {
        private static string basePath = "Assets/Resources/ScriptableObjects/MonsterData/MonsterSkillData";

        [MenuItem("Dungeon Crawler/Generate Monster Skill Data (40)")]
        public static void Generate()
        {
            EnsureFolder(basePath);

            int created = 0;
            created += GeneratePhysicalAttackSkills();
            created += GenerateMagicalAttackSkills();
            created += GenerateDefenseBuffSkills();
            created += GenerateSpecialSkills();
            created += GenerateAoESkills();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[MonsterSkillDataGenerator] 몬스터 스킬 {created}개 생성 완료");
        }

        // ================= 물리 공격 스킬 10개 =================
        private static int GeneratePhysicalAttackSkills()
        {
            int count = 0;
            // Slash
            count += CreateSkill("Slash", "날카로운 베기 공격", MonsterSkillType.Active, MonsterSkillCategory.PhysicalAttack,
                MonsterSkillTrigger.Manual, 3f, 0f, 1.5f,
                str: new SkillEffectRange(0, 0), dmgMult: new SkillEffectRange(1.2f, 1.8f)) ? 1 : 0;
            // Crush
            count += CreateSkill("Crush", "강력한 으깨기 공격", MonsterSkillType.Active, MonsterSkillCategory.PhysicalAttack,
                MonsterSkillTrigger.Manual, 4f, 0f, 1.5f,
                dmgMult: new SkillEffectRange(1.4f, 2.0f)) ? 1 : 0;
            // Pierce
            count += CreateSkill("Pierce", "방어를 관통하는 찌르기", MonsterSkillType.Active, MonsterSkillCategory.PhysicalAttack,
                MonsterSkillTrigger.Manual, 3.5f, 0f, 2f,
                dmgMult: new SkillEffectRange(1.1f, 1.6f)) ? 1 : 0;
            // Charge
            count += CreateSkill("Charge", "돌진하며 들이받기", MonsterSkillType.Active, MonsterSkillCategory.PhysicalAttack,
                MonsterSkillTrigger.Manual, 6f, 0f, 3f,
                dmgMult: new SkillEffectRange(1.6f, 2.4f)) ? 1 : 0;
            // Slam
            count += CreateSkill("Slam", "지면을 강타하는 내려찍기", MonsterSkillType.Active, MonsterSkillCategory.PhysicalAttack,
                MonsterSkillTrigger.Manual, 5f, 0f, 1.5f,
                dmgMult: new SkillEffectRange(1.5f, 2.2f)) ? 1 : 0;
            // Rend
            count += CreateSkill("Rend", "살을 찢는 공격으로 출혈 유발", MonsterSkillType.Active, MonsterSkillCategory.PhysicalAttack,
                MonsterSkillTrigger.Manual, 4f, 0f, 1.5f,
                dmgMult: new SkillEffectRange(1.2f, 1.7f),
                statusType: StatusType.Poison, statusChance: new SkillEffectRange(0.5f, 0.8f), statusDur: new SkillEffectRange(3f, 5f)) ? 1 : 0;
            // Maul
            count += CreateSkill("Maul", "야수의 난폭한 할퀴기", MonsterSkillType.Active, MonsterSkillCategory.PhysicalAttack,
                MonsterSkillTrigger.Manual, 4.5f, 0f, 1.5f,
                dmgMult: new SkillEffectRange(1.4f, 2.0f)) ? 1 : 0;
            // Gore
            count += CreateSkill("Gore", "뿔로 들이박기", MonsterSkillType.Active, MonsterSkillCategory.PhysicalAttack,
                MonsterSkillTrigger.Manual, 5f, 0f, 2f,
                dmgMult: new SkillEffectRange(1.3f, 1.9f)) ? 1 : 0;
            // Bite
            count += CreateSkill("Bite", "독이 묻은 물기 공격", MonsterSkillType.Active, MonsterSkillCategory.PhysicalAttack,
                MonsterSkillTrigger.Manual, 3f, 0f, 1.5f,
                dmgMult: new SkillEffectRange(1.0f, 1.5f),
                statusType: StatusType.Poison, statusChance: new SkillEffectRange(0.6f, 0.9f), statusDur: new SkillEffectRange(4f, 6f)) ? 1 : 0;
            // Claw
            count += CreateSkill("Claw", "날카로운 발톱 공격", MonsterSkillType.Active, MonsterSkillCategory.PhysicalAttack,
                MonsterSkillTrigger.Manual, 2.5f, 0f, 1.5f,
                dmgMult: new SkillEffectRange(1.1f, 1.6f)) ? 1 : 0;
            return count;
        }

        // ================= 마법 공격 스킬 10개 =================
        private static int GenerateMagicalAttackSkills()
        {
            int count = 0;
            count += CreateSkill("FireBlast", "화염 폭발", MonsterSkillType.Active, MonsterSkillCategory.MagicalAttack,
                MonsterSkillTrigger.Manual, 4f, 10f, 4f,
                dmgMult: new SkillEffectRange(1.4f, 2.0f),
                statusType: StatusType.Burn, statusChance: new SkillEffectRange(0.3f, 0.5f), statusDur: new SkillEffectRange(3f, 5f)) ? 1 : 0;
            count += CreateSkill("IceShard", "얼음 파편 사격", MonsterSkillType.Active, MonsterSkillCategory.MagicalAttack,
                MonsterSkillTrigger.Manual, 3.5f, 8f, 5f,
                dmgMult: new SkillEffectRange(1.2f, 1.7f),
                statusType: StatusType.Slow, statusChance: new SkillEffectRange(0.5f, 0.7f), statusDur: new SkillEffectRange(3f, 5f)) ? 1 : 0;
            count += CreateSkill("LightningBolt", "번개 직격", MonsterSkillType.Active, MonsterSkillCategory.MagicalAttack,
                MonsterSkillTrigger.Manual, 5f, 15f, 6f,
                dmgMult: new SkillEffectRange(1.6f, 2.4f),
                statusType: StatusType.Stun, statusChance: new SkillEffectRange(0.2f, 0.4f), statusDur: new SkillEffectRange(1f, 2f)) ? 1 : 0;
            count += CreateSkill("PoisonCloud", "독성 구름 살포", MonsterSkillType.Active, MonsterSkillCategory.MagicalAttack,
                MonsterSkillTrigger.Manual, 6f, 12f, 3f,
                dmgMult: new SkillEffectRange(1.0f, 1.5f),
                statusType: StatusType.Poison, statusChance: new SkillEffectRange(0.7f, 0.95f), statusDur: new SkillEffectRange(5f, 8f)) ? 1 : 0;
            count += CreateSkill("DarkBolt", "암흑 탄환 발사", MonsterSkillType.Active, MonsterSkillCategory.MagicalAttack,
                MonsterSkillTrigger.Manual, 4f, 12f, 5f,
                dmgMult: new SkillEffectRange(1.5f, 2.2f)) ? 1 : 0;
            count += CreateSkill("HolySmite", "신성한 징벌의 빛", MonsterSkillType.Active, MonsterSkillCategory.MagicalAttack,
                MonsterSkillTrigger.Manual, 6f, 20f, 5f,
                dmgMult: new SkillEffectRange(1.8f, 2.8f)) ? 1 : 0;
            count += CreateSkill("ArcaneBeam", "비전 광선 발사", MonsterSkillType.Active, MonsterSkillCategory.MagicalAttack,
                MonsterSkillTrigger.Manual, 5f, 14f, 6f,
                dmgMult: new SkillEffectRange(1.4f, 2.0f)) ? 1 : 0;
            count += CreateSkill("FrostNova", "주변 냉기 폭발", MonsterSkillType.Active, MonsterSkillCategory.MagicalAttack,
                MonsterSkillTrigger.Manual, 7f, 18f, 3f,
                dmgMult: new SkillEffectRange(1.3f, 1.8f),
                statusType: StatusType.Freeze, statusChance: new SkillEffectRange(0.4f, 0.6f), statusDur: new SkillEffectRange(2f, 4f)) ? 1 : 0;
            count += CreateSkill("Fireball", "거대한 화염구", MonsterSkillType.Active, MonsterSkillCategory.MagicalAttack,
                MonsterSkillTrigger.Manual, 5f, 16f, 5f,
                dmgMult: new SkillEffectRange(1.7f, 2.5f),
                statusType: StatusType.Burn, statusChance: new SkillEffectRange(0.4f, 0.6f), statusDur: new SkillEffectRange(3f, 5f)) ? 1 : 0;
            count += CreateSkill("ChainLightning", "연쇄 번개", MonsterSkillType.Active, MonsterSkillCategory.MagicalAttack,
                MonsterSkillTrigger.Manual, 6f, 18f, 5f,
                dmgMult: new SkillEffectRange(1.5f, 2.2f),
                statusType: StatusType.Stun, statusChance: new SkillEffectRange(0.15f, 0.3f), statusDur: new SkillEffectRange(1f, 1.5f)) ? 1 : 0;
            return count;
        }

        // ================= 방어/버프 스킬 8개 =================
        private static int GenerateDefenseBuffSkills()
        {
            int count = 0;
            count += CreateSkill("IronSkin", "피부를 강철처럼 경화", MonsterSkillType.Passive, MonsterSkillCategory.PhysicalDefense,
                MonsterSkillTrigger.OnCombatStart, 0f, 0f, 0f,
                def: new SkillEffectRange(3f, 8f)) ? 1 : 0;
            count += CreateSkill("MagicShield", "마법 보호막 전개", MonsterSkillType.Passive, MonsterSkillCategory.MagicalDefense,
                MonsterSkillTrigger.OnCombatStart, 0f, 0f, 0f,
                mdef: new SkillEffectRange(3f, 8f)) ? 1 : 0;
            count += CreateSkill("Heal", "생명력 회복", MonsterSkillType.Active, MonsterSkillCategory.Regeneration,
                MonsterSkillTrigger.OnLowHealth, 8f, 10f, 0f,
                heal: new SkillEffectRange(15f, 30f)) ? 1 : 0;
            count += CreateSkill("Haste", "행동 속도 증가", MonsterSkillType.Passive, MonsterSkillCategory.AttackSpeed,
                MonsterSkillTrigger.OnCombatStart, 0f, 0f, 0f,
                agi: new SkillEffectRange(2f, 5f), spdMult: new SkillEffectRange(1.1f, 1.3f)) ? 1 : 0;
            count += CreateSkill("Enrage", "분노하여 공격력 상승, 방어력 하락", MonsterSkillType.Active, MonsterSkillCategory.DamageBonus,
                MonsterSkillTrigger.OnLowHealth, 15f, 0f, 0f,
                str: new SkillEffectRange(3f, 6f), def: new SkillEffectRange(-2f, -1f),
                dmgMult: new SkillEffectRange(1.3f, 1.6f)) ? 1 : 0;
            count += CreateSkill("Fortify", "몸을 단단히 굳혀 방어 강화", MonsterSkillType.Active, MonsterSkillCategory.PhysicalDefense,
                MonsterSkillTrigger.OnTakeDamage, 10f, 5f, 0f,
                def: new SkillEffectRange(5f, 12f), vit: new SkillEffectRange(2f, 5f)) ? 1 : 0;
            count += CreateSkill("Ward", "마법 저항 결계 전개", MonsterSkillType.Active, MonsterSkillCategory.MagicalDefense,
                MonsterSkillTrigger.OnTakeDamage, 10f, 8f, 0f,
                mdef: new SkillEffectRange(4f, 10f)) ? 1 : 0;
            count += CreateSkill("Barrier", "전방위 보호 장벽", MonsterSkillType.Active, MonsterSkillCategory.HealthBonus,
                MonsterSkillTrigger.OnCombatStart, 20f, 15f, 0f,
                def: new SkillEffectRange(5f, 10f), mdef: new SkillEffectRange(5f, 10f)) ? 1 : 0;
            return count;
        }

        // ================= 특수 스킬 8개 =================
        private static int GenerateSpecialSkills()
        {
            int count = 0;
            count += CreateSkill("Teleport", "순간 이동하여 위치 변경", MonsterSkillType.Active, MonsterSkillCategory.MovementSpeed,
                MonsterSkillTrigger.OnLowHealth, 10f, 5f, 0f,
                spdMult: new SkillEffectRange(2.0f, 3.0f), dur: new SkillEffectRange(0.5f, 1f)) ? 1 : 0;
            count += CreateSkill("Summon", "하급 몬스터 소환", MonsterSkillType.Active, MonsterSkillCategory.Summoning,
                MonsterSkillTrigger.OnCooldown, 15f, 20f, 0f) ? 1 : 0;
            count += CreateSkill("Fear", "공포를 주입하여 도주 유발", MonsterSkillType.Active, MonsterSkillCategory.SpecialAbility,
                MonsterSkillTrigger.Manual, 12f, 10f, 4f,
                statusType: StatusType.Weakness, statusChance: new SkillEffectRange(0.6f, 0.9f), statusDur: new SkillEffectRange(2f, 4f)) ? 1 : 0;
            count += CreateSkill("StunStrike", "기절시키는 강타", MonsterSkillType.Active, MonsterSkillCategory.SpecialAbility,
                MonsterSkillTrigger.Manual, 8f, 5f, 1.5f,
                dmgMult: new SkillEffectRange(1.0f, 1.3f),
                statusType: StatusType.Stun, statusChance: new SkillEffectRange(0.5f, 0.8f), statusDur: new SkillEffectRange(1.5f, 3f)) ? 1 : 0;
            count += CreateSkill("RootGrasp", "덩굴로 속박하여 이동 불가", MonsterSkillType.Active, MonsterSkillCategory.SpecialAbility,
                MonsterSkillTrigger.Manual, 10f, 8f, 4f,
                statusType: StatusType.Root, statusChance: new SkillEffectRange(0.7f, 0.95f), statusDur: new SkillEffectRange(3f, 5f)) ? 1 : 0;
            count += CreateSkill("Silence", "침묵의 파동으로 마법 봉인", MonsterSkillType.Active, MonsterSkillCategory.StatusResistance,
                MonsterSkillTrigger.Manual, 12f, 12f, 4f,
                statusType: StatusType.Weakness, statusChance: new SkillEffectRange(0.6f, 0.9f), statusDur: new SkillEffectRange(2f, 4f)) ? 1 : 0;
            count += CreateSkill("SlowAura", "주변의 속도를 저하시키는 오라", MonsterSkillType.Passive, MonsterSkillCategory.AuraEffect,
                MonsterSkillTrigger.OnCombatStart, 0f, 0f, 3f,
                statusType: StatusType.Slow, statusChance: new SkillEffectRange(0.8f, 1f), statusDur: new SkillEffectRange(3f, 5f)) ? 1 : 0;
            count += CreateSkill("PoisonTouch", "독성 접촉 - 지속 피해", MonsterSkillType.Passive, MonsterSkillCategory.SpecialAbility,
                MonsterSkillTrigger.OnDealDamage, 0f, 0f, 0f,
                statusType: StatusType.Poison, statusChance: new SkillEffectRange(0.7f, 0.95f), statusDur: new SkillEffectRange(4f, 7f)) ? 1 : 0;
            return count;
        }

        // ================= 광역 스킬 4개 =================
        private static int GenerateAoESkills()
        {
            int count = 0;
            count += CreateSkill("Earthquake", "대지를 뒤흔드는 지진", MonsterSkillType.Active, MonsterSkillCategory.PhysicalAttack,
                MonsterSkillTrigger.OnCooldown, 12f, 15f, 5f,
                dmgMult: new SkillEffectRange(2.0f, 3.0f),
                statusType: StatusType.Stun, statusChance: new SkillEffectRange(0.3f, 0.5f), statusDur: new SkillEffectRange(1f, 2f)) ? 1 : 0;
            count += CreateSkill("Meteor", "하늘에서 떨어지는 운석", MonsterSkillType.Active, MonsterSkillCategory.MagicalAttack,
                MonsterSkillTrigger.OnCooldown, 15f, 25f, 4f,
                dmgMult: new SkillEffectRange(2.5f, 4.0f),
                statusType: StatusType.Burn, statusChance: new SkillEffectRange(0.5f, 0.7f), statusDur: new SkillEffectRange(3f, 5f)) ? 1 : 0;
            count += CreateSkill("Blizzard", "지속되는 눈보라", MonsterSkillType.Active, MonsterSkillCategory.MagicalAttack,
                MonsterSkillTrigger.OnCooldown, 14f, 20f, 6f,
                dmgMult: new SkillEffectRange(1.5f, 2.5f), dur: new SkillEffectRange(3f, 5f),
                statusType: StatusType.Freeze, statusChance: new SkillEffectRange(0.4f, 0.6f), statusDur: new SkillEffectRange(2f, 3f)) ? 1 : 0;
            count += CreateSkill("Shockwave", "충격파 방출", MonsterSkillType.Active, MonsterSkillCategory.PhysicalAttack,
                MonsterSkillTrigger.OnCooldown, 10f, 10f, 8f,
                dmgMult: new SkillEffectRange(1.5f, 2.2f),
                statusType: StatusType.Slow, statusChance: new SkillEffectRange(0.5f, 0.7f), statusDur: new SkillEffectRange(2f, 4f)) ? 1 : 0;
            return count;
        }

        // ================= 헬퍼 =================
        private static bool CreateSkill(string skillName, string desc,
            MonsterSkillType type, MonsterSkillCategory category,
            MonsterSkillTrigger trigger, float cooldown, float manaCost, float range,
            SkillEffectRange str = default, SkillEffectRange agi = default,
            SkillEffectRange vit = default, SkillEffectRange inte = default,
            SkillEffectRange def = default, SkillEffectRange mdef = default,
            SkillEffectRange luk = default, SkillEffectRange stab = default,
            SkillEffectRange dmgMult = default, SkillEffectRange defMult = default,
            SkillEffectRange spdMult = default, SkillEffectRange heal = default,
            SkillEffectRange dur = default,
            StatusType statusType = StatusType.None,
            SkillEffectRange statusChance = default, SkillEffectRange statusDur = default)
        {
            string assetPath = $"{basePath}/{skillName}_SkillData.asset";

            // 이미 존재하면 스킵
            if (AssetDatabase.LoadAssetAtPath<MonsterSkillData>(assetPath) != null)
            {
                return false;
            }

            var skill = ScriptableObject.CreateInstance<MonsterSkillData>();
            skill.skillName = skillName;
            skill.description = desc;
            skill.skillType = type;
            skill.category = category;

            AssetDatabase.CreateAsset(skill, assetPath);
            var so = new SerializedObject(skill);

            so.FindProperty("cooldown").floatValue = cooldown;
            so.FindProperty("manaCost").floatValue = manaCost;
            so.FindProperty("range").floatValue = range;
            so.FindProperty("trigger").enumValueIndex = (int)trigger;

            // Skill Effect
            var effect = so.FindProperty("skillEffect");
            SetEffectRange(effect, "strengthBonus", str);
            SetEffectRange(effect, "agilityBonus", agi);
            SetEffectRange(effect, "vitalityBonus", vit);
            SetEffectRange(effect, "intelligenceBonus", inte);
            SetEffectRange(effect, "defenseBonus", def);
            SetEffectRange(effect, "magicDefenseBonus", mdef);
            SetEffectRange(effect, "luckBonus", luk);
            SetEffectRange(effect, "stabilityBonus", stab);
            SetEffectRange(effect, "damageMultiplierRange", dmgMult);
            SetEffectRange(effect, "defenseMultiplierRange", defMult);
            SetEffectRange(effect, "speedMultiplierRange", spdMult);
            SetEffectRange(effect, "healingAmountRange", heal);
            SetEffectRange(effect, "durationRange", dur);

            effect.FindPropertyRelative("inflictStatus").enumValueIndex = (int)statusType;
            SetEffectRange(effect, "statusChanceRange", statusChance);
            SetEffectRange(effect, "statusDurationRange", statusDur);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skill);
            return true;
        }

        private static void SetEffectRange(SerializedProperty parent, string fieldName, SkillEffectRange range)
        {
            var prop = parent.FindPropertyRelative(fieldName);
            prop.FindPropertyRelative("minValue").floatValue = range.minValue;
            prop.FindPropertyRelative("maxValue").floatValue = range.maxValue;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
