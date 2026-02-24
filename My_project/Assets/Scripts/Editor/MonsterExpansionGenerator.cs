using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 몬스터 대규모 확장 - 신규 8종족 + 64변종 + 60스킬
    /// 디아블로/다크스트던전 패턴 기반 콘텐츠
    /// </summary>
    public class MonsterExpansionGenerator
    {
        private static string racePath = "Assets/Resources/ScriptableObjects/MonsterData/MonsterRaceData";
        private static string variantPath = "Assets/Resources/ScriptableObjects/MonsterData/MonsterVariantData";
        private static string skillPath = "Assets/Resources/ScriptableObjects/MonsterData/MonsterSkillData";

        [MenuItem("Dungeon Crawler/Generate Monster Expansion")]
        public static void GenerateAll()
        {
            EnsureFolder(racePath);
            EnsureFolder(variantPath);
            EnsureFolder(skillPath);

            int races = GenerateNewRaces();
            int skills = GenerateNewSkills();
            int variants = GenerateNewVariants();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[MonsterExpansion] 종족 {races}개, 스킬 {skills}개, 변종 {variants}개 생성 완료");
        }

        // ===================== 8 신규 종족 =====================
        private static int GenerateNewRaces()
        {
            int count = 0;

            // Spider (거미) - 속박+독, 민첩
            count += CreateRace("Spider", MonsterRace.Spider, "거미족",
                "거미줄로 먹잇감을 속박하고 독으로 약화시키는 교활한 사냥꾼",
                str: 8, agi: 12, vit: 8, intel: 7, def: 5, mdef: 4, luk: 6, stab: 3,
                strG: 1.0f, agiG: 1.8f, vitG: 1.0f, intG: 0.8f, defG: 0.6f, mdefG: 0.5f, lukG: 0.8f, stabG: 0.4f,
                baseExp: 65, baseGold: 12, soulRate: 0.10f);

            // Serpent (뱀) - 독+민첩
            count += CreateRace("Serpent", MonsterRace.Serpent, "뱀족",
                "빠르고 치명적인 독을 가진 파충류 몬스터",
                str: 7, agi: 14, vit: 7, intel: 6, def: 4, mdef: 5, luk: 8, stab: 2,
                strG: 0.8f, agiG: 2.0f, vitG: 0.8f, intG: 0.7f, defG: 0.5f, mdefG: 0.6f, lukG: 1.0f, stabG: 0.3f,
                baseExp: 70, baseGold: 14, soulRate: 0.11f);

            // Fungal (균류) - 포자+DoT
            count += CreateRace("Fungal", MonsterRace.Fungal, "균류족",
                "포자를 뿌려 적을 약화시키는 기이한 버섯 생물체",
                str: 5, agi: 4, vit: 18, intel: 10, def: 8, mdef: 6, luk: 3, stab: 10,
                strG: 0.6f, agiG: 0.5f, vitG: 2.5f, intG: 1.2f, defG: 1.0f, mdefG: 0.8f, lukG: 0.4f, stabG: 1.2f,
                baseExp: 75, baseGold: 10, soulRate: 0.13f);

            // Bandit (산적) - 인간형+강탈
            count += CreateRace("Bandit", MonsterRace.Bandit, "산적족",
                "골드를 노리는 무법자들. 처치 시 많은 골드를 드롭",
                str: 11, agi: 10, vit: 10, intel: 6, def: 7, mdef: 4, luk: 10, stab: 5,
                strG: 1.3f, agiG: 1.2f, vitG: 1.2f, intG: 0.7f, defG: 0.8f, mdefG: 0.5f, lukG: 1.2f, stabG: 0.6f,
                baseExp: 60, baseGold: 30, soulRate: 0.08f);

            // Cultist (광신도) - 암흑마법+소환
            count += CreateRace("Cultist", MonsterRace.Cultist, "광신도족",
                "어둠의 의식을 통해 악마를 소환하는 사이비 집단",
                str: 6, agi: 7, vit: 9, intel: 15, def: 5, mdef: 10, luk: 5, stab: 7,
                strG: 0.7f, agiG: 0.8f, vitG: 1.0f, intG: 2.0f, defG: 0.6f, mdefG: 1.3f, lukG: 0.6f, stabG: 0.8f,
                baseExp: 100, baseGold: 20, soulRate: 0.18f);

            // Drowned (수몰) - 물+저주
            count += CreateRace("Drowned", MonsterRace.Drowned, "수몰족",
                "물에 빠져 죽은 후 되살아난 저주받은 존재들",
                str: 9, agi: 5, vit: 14, intel: 8, def: 6, mdef: 9, luk: 4, stab: 8,
                strG: 1.1f, agiG: 0.6f, vitG: 1.8f, intG: 1.0f, defG: 0.7f, mdefG: 1.1f, lukG: 0.5f, stabG: 1.0f,
                baseExp: 85, baseGold: 16, soulRate: 0.14f);

            // Insect (곤충) - 떼+빠름
            count += CreateRace("Insect", MonsterRace.Insect, "곤충족",
                "떼를 지어 덮치는 작지만 위협적인 곤충 무리",
                str: 4, agi: 16, vit: 5, intel: 3, def: 3, mdef: 2, luk: 7, stab: 1,
                strG: 0.5f, agiG: 2.2f, vitG: 0.6f, intG: 0.4f, defG: 0.4f, mdefG: 0.3f, lukG: 0.9f, stabG: 0.2f,
                baseExp: 30, baseGold: 5, soulRate: 0.05f);

            // Golem (골렘) - 돌+방어
            count += CreateRace("Golem", MonsterRace.Golem, "골렘족",
                "마법으로 만들어진 거대한 돌 인형. 높은 방어력과 체력",
                str: 14, agi: 3, vit: 20, intel: 4, def: 16, mdef: 6, luk: 2, stab: 14,
                strG: 1.8f, agiG: 0.4f, vitG: 2.8f, intG: 0.5f, defG: 2.2f, mdefG: 0.7f, lukG: 0.3f, stabG: 1.8f,
                baseExp: 110, baseGold: 22, soulRate: 0.09f);

            return count;
        }

        // ===================== 60 신규 몬스터 스킬 =====================
        private static int GenerateNewSkills()
        {
            int count = 0;

            // === 거미 전용 스킬 (8개) ===
            count += Skill("SpiderWeb", "거미줄 투사", MonsterSkillCategory.SpecialAbility, MonsterSkillTrigger.Manual,
                5f, 0f, 4f, dmgMult: R(0.5f, 0.8f), status: StatusType.Root, sChance: R(0.8f, 1f), sDur: R(3f, 5f));
            count += Skill("VenomBite", "독니 물기", MonsterSkillCategory.PhysicalAttack, MonsterSkillTrigger.Manual,
                3f, 0f, 1.5f, dmgMult: R(1.3f, 1.8f), status: StatusType.Poison, sChance: R(0.7f, 0.95f), sDur: R(5f, 8f));
            count += Skill("SpiderSwarm", "거미떼 소환", MonsterSkillCategory.Summoning, MonsterSkillTrigger.OnLowHealth,
                20f, 0f, 0f, healRange: R(0f, 0f));
            count += Skill("CocoopWrap", "거미줄 감싸기", MonsterSkillCategory.SpecialAbility, MonsterSkillTrigger.Manual,
                8f, 0f, 2f, status: StatusType.Stun, sChance: R(0.6f, 0.85f), sDur: R(2f, 3f));
            count += Skill("AcidSpray", "산성 분사", MonsterSkillCategory.MagicalAttack, MonsterSkillTrigger.Manual,
                6f, 10f, 3f, dmgMult: R(1.5f, 2.2f));
            count += Skill("WebTrap", "거미줄 함정", MonsterSkillCategory.SpecialAbility, MonsterSkillTrigger.OnCombatStart,
                15f, 0f, 5f, status: StatusType.Slow, sChance: R(0.9f, 1f), sDur: R(4f, 6f));
            count += Skill("SpiderAmbush", "매복 공격", MonsterSkillCategory.PhysicalAttack, MonsterSkillTrigger.OnCombatStart,
                0f, 0f, 2f, dmgMult: R(2.0f, 3.0f));
            count += Skill("NestDefense", "둥지 방어", MonsterSkillCategory.PhysicalDefense, MonsterSkillTrigger.OnTakeDamage,
                12f, 0f, 0f, defBonus: R(5f, 10f), dur: R(8f, 12f));

            // === 뱀 전용 스킬 (7개) ===
            count += Skill("CobraStrike", "코브라 타격", MonsterSkillCategory.PhysicalAttack, MonsterSkillTrigger.Manual,
                2f, 0f, 2f, dmgMult: R(1.5f, 2.0f), status: StatusType.Poison, sChance: R(0.5f, 0.7f), sDur: R(4f, 6f));
            count += Skill("Constrict", "조이기", MonsterSkillCategory.SpecialAbility, MonsterSkillTrigger.Manual,
                7f, 0f, 1.5f, dmgMult: R(0.8f, 1.2f), status: StatusType.Root, sChance: R(0.7f, 0.9f), sDur: R(2f, 4f));
            count += Skill("VenomSpit", "독액 분사", MonsterSkillCategory.MagicalAttack, MonsterSkillTrigger.Manual,
                4f, 8f, 4f, dmgMult: R(1.2f, 1.8f), status: StatusType.Poison, sChance: R(0.8f, 1f), sDur: R(6f, 10f));
            count += Skill("ShedSkin", "탈피", MonsterSkillCategory.HealthBonus, MonsterSkillTrigger.OnLowHealth,
                25f, 0f, 0f, healRange: R(20f, 40f));
            count += Skill("Hypnosis", "최면", MonsterSkillCategory.SpecialAbility, MonsterSkillTrigger.Manual,
                10f, 15f, 3f, status: StatusType.Stun, sChance: R(0.5f, 0.7f), sDur: R(2f, 3f));
            count += Skill("TailWhip", "꼬리 채찍", MonsterSkillCategory.PhysicalAttack, MonsterSkillTrigger.Manual,
                3f, 0f, 2f, dmgMult: R(1.3f, 1.7f));
            count += Skill("PoisonFang", "맹독 이빨", MonsterSkillCategory.PhysicalAttack, MonsterSkillTrigger.Manual,
                5f, 0f, 1.5f, dmgMult: R(1.8f, 2.5f), status: StatusType.Poison, sChance: R(0.9f, 1f), sDur: R(8f, 12f));

            // === 균류 전용 스킬 (7개) ===
            count += Skill("SporeCloud", "포자 구름", MonsterSkillCategory.MagicalAttack, MonsterSkillTrigger.Manual,
                6f, 10f, 4f, dmgMult: R(0.8f, 1.3f), status: StatusType.Poison, sChance: R(0.9f, 1f), sDur: R(8f, 12f));
            count += Skill("MushroomBurst", "버섯 폭발", MonsterSkillCategory.MagicalAttack, MonsterSkillTrigger.OnTakeDamage,
                10f, 15f, 3f, dmgMult: R(1.5f, 2.5f));
            count += Skill("Mycelium", "균사 연결", MonsterSkillCategory.Regeneration, MonsterSkillTrigger.OnCooldown,
                8f, 0f, 0f, healRange: R(5f, 15f), dur: R(10f, 15f));
            count += Skill("ParasiticSpore", "기생 포자", MonsterSkillCategory.SpecialAbility, MonsterSkillTrigger.Manual,
                12f, 20f, 3f, status: StatusType.Weakness, sChance: R(0.7f, 0.9f), sDur: R(6f, 10f));
            count += Skill("FungalShield", "균류 갑옷", MonsterSkillCategory.PhysicalDefense, MonsterSkillTrigger.OnCombatStart,
                20f, 0f, 0f, defBonus: R(8f, 15f), dur: R(15f, 20f));
            count += Skill("ToxicExplosion", "독소 폭발", MonsterSkillCategory.MagicalAttack, MonsterSkillTrigger.OnLowHealth,
                0f, 0f, 5f, dmgMult: R(2.0f, 3.5f), status: StatusType.Poison, sChance: R(1f, 1f), sDur: R(10f, 15f));
            count += Skill("SporeInfection", "포자 감염", MonsterSkillCategory.SpecialAbility, MonsterSkillTrigger.OnDealDamage,
                15f, 0f, 0f, status: StatusType.Slow, sChance: R(0.4f, 0.6f), sDur: R(3f, 5f));

            // === 산적 전용 스킬 (7개) ===
            count += Skill("Backstab", "뒤치기", MonsterSkillCategory.PhysicalAttack, MonsterSkillTrigger.Manual,
                4f, 0f, 1.5f, dmgMult: R(2.0f, 3.0f));
            count += Skill("GoldSteal", "금화 강탈", MonsterSkillCategory.SpecialAbility, MonsterSkillTrigger.OnDealDamage,
                15f, 0f, 1.5f, dmgMult: R(0.5f, 0.8f));
            count += Skill("SmokeScreen", "연막탄", MonsterSkillCategory.SpecialAbility, MonsterSkillTrigger.OnLowHealth,
                20f, 0f, 5f, spdMult: R(1.3f, 1.5f), dur: R(5f, 8f));
            count += Skill("DirtyFight", "비열한 공격", MonsterSkillCategory.PhysicalAttack, MonsterSkillTrigger.Manual,
                5f, 0f, 1.5f, dmgMult: R(1.5f, 2.0f), status: StatusType.Stun, sChance: R(0.3f, 0.5f), sDur: R(1f, 2f));
            count += Skill("BanditCall", "동료 호출", MonsterSkillCategory.Summoning, MonsterSkillTrigger.OnLowHealth,
                30f, 0f, 0f);
            count += Skill("ThrowKnife", "투척 단검", MonsterSkillCategory.PhysicalAttack, MonsterSkillTrigger.Manual,
                3f, 0f, 5f, dmgMult: R(1.2f, 1.6f));
            count += Skill("Pillage", "약탈", MonsterSkillCategory.PhysicalAttack, MonsterSkillTrigger.Manual,
                6f, 0f, 1.5f, dmgMult: R(1.8f, 2.5f));

            // === 광신도 전용 스킬 (7개) ===
            count += Skill("DarkRitual", "어둠의 의식", MonsterSkillCategory.Summoning, MonsterSkillTrigger.OnCombatStart,
                25f, 30f, 0f);
            count += Skill("ShadowBolt", "암흑 탄환", MonsterSkillCategory.MagicalAttack, MonsterSkillTrigger.Manual,
                3f, 12f, 5f, dmgMult: R(1.5f, 2.2f));
            count += Skill("CursedChant", "저주의 주문", MonsterSkillCategory.SpecialAbility, MonsterSkillTrigger.Manual,
                8f, 20f, 4f, status: StatusType.Weakness, sChance: R(0.8f, 1f), sDur: R(5f, 8f));
            count += Skill("BloodSacrifice", "피의 제물", MonsterSkillCategory.HealthBonus, MonsterSkillTrigger.OnLowHealth,
                30f, 0f, 0f, healRange: R(30f, 50f));
            count += Skill("DemonicPact", "악마 계약", MonsterSkillCategory.DamageBonus, MonsterSkillTrigger.OnCombatStart,
                0f, 0f, 0f, dmgMult: R(1.5f, 2.0f), dur: R(20f, 30f));
            count += Skill("SoulBurn", "영혼 소각", MonsterSkillCategory.MagicalAttack, MonsterSkillTrigger.Manual,
                5f, 18f, 3f, dmgMult: R(1.8f, 2.8f), status: StatusType.Burn, sChance: R(0.6f, 0.8f), sDur: R(4f, 6f));
            count += Skill("DarkBarrier", "암흑 결계", MonsterSkillCategory.MagicalDefense, MonsterSkillTrigger.OnTakeDamage,
                15f, 15f, 0f, mdefBonus: R(8f, 15f), dur: R(8f, 12f));

            // === 수몰 전용 스킬 (7개) ===
            count += Skill("TidalWave", "해일", MonsterSkillCategory.MagicalAttack, MonsterSkillTrigger.Manual,
                8f, 15f, 5f, dmgMult: R(1.5f, 2.5f), status: StatusType.Slow, sChance: R(0.7f, 0.9f), sDur: R(3f, 5f));
            count += Skill("DrownGrasp", "익사의 손길", MonsterSkillCategory.SpecialAbility, MonsterSkillTrigger.Manual,
                6f, 10f, 2f, dmgMult: R(1.0f, 1.5f), status: StatusType.Root, sChance: R(0.6f, 0.8f), sDur: R(2f, 4f));
            count += Skill("CursedWaters", "저주받은 물", MonsterSkillCategory.MagicalAttack, MonsterSkillTrigger.Manual,
                5f, 12f, 3f, dmgMult: R(1.2f, 1.8f), status: StatusType.Weakness, sChance: R(0.5f, 0.7f), sDur: R(5f, 8f));
            count += Skill("WaterRegen", "물의 치유", MonsterSkillCategory.Regeneration, MonsterSkillTrigger.OnCooldown,
                10f, 0f, 0f, healRange: R(8f, 20f), dur: R(8f, 12f));
            count += Skill("AbyssalCall", "심연의 부름", MonsterSkillCategory.Summoning, MonsterSkillTrigger.OnLowHealth,
                30f, 20f, 0f);
            count += Skill("Whirlpool", "소용돌이", MonsterSkillCategory.MagicalAttack, MonsterSkillTrigger.Manual,
                10f, 20f, 4f, dmgMult: R(2.0f, 3.0f), status: StatusType.Slow, sChance: R(0.9f, 1f), sDur: R(4f, 6f));
            count += Skill("GhostlyWail", "유령의 울부짖음", MonsterSkillCategory.SpecialAbility, MonsterSkillTrigger.Manual,
                12f, 15f, 5f, status: StatusType.Stun, sChance: R(0.4f, 0.6f), sDur: R(1.5f, 2.5f));

            // === 곤충 전용 스킬 (6개) ===
            count += Skill("SwarmAttack", "떼 공격", MonsterSkillCategory.PhysicalAttack, MonsterSkillTrigger.Manual,
                2f, 0f, 1.5f, dmgMult: R(0.5f, 0.8f));
            count += Skill("ParalyzeSting", "마비 침", MonsterSkillCategory.PhysicalAttack, MonsterSkillTrigger.Manual,
                5f, 0f, 1.5f, dmgMult: R(1.0f, 1.5f), status: StatusType.Stun, sChance: R(0.4f, 0.6f), sDur: R(1f, 2f));
            count += Skill("HiveMind", "군체 지능", MonsterSkillCategory.AllyBuff, MonsterSkillTrigger.OnCombatStart,
                0f, 0f, 0f, atkSpdMult: R(1.2f, 1.5f), dur: R(30f, 60f));
            count += Skill("BurrowEscape", "땅파기 도주", MonsterSkillCategory.MovementSpeed, MonsterSkillTrigger.OnLowHealth,
                20f, 0f, 0f, spdMult: R(1.5f, 2.0f), dur: R(5f, 8f));
            count += Skill("InsectSwarm", "곤충 폭풍", MonsterSkillCategory.MagicalAttack, MonsterSkillTrigger.Manual,
                8f, 10f, 4f, dmgMult: R(1.0f, 1.8f));
            count += Skill("AcidSpit", "산성 침", MonsterSkillCategory.MagicalAttack, MonsterSkillTrigger.Manual,
                4f, 5f, 3f, dmgMult: R(1.2f, 1.6f), status: StatusType.Weakness, sChance: R(0.3f, 0.5f), sDur: R(3f, 5f));

            // === 골렘 전용 스킬 (6개) ===
            count += Skill("RockThrow", "바위 던지기", MonsterSkillCategory.PhysicalAttack, MonsterSkillTrigger.Manual,
                5f, 0f, 5f, dmgMult: R(1.8f, 2.5f));
            count += Skill("StoneArmor", "석화 갑옷", MonsterSkillCategory.PhysicalDefense, MonsterSkillTrigger.OnCombatStart,
                25f, 0f, 0f, defBonus: R(10f, 20f), dur: R(20f, 30f));
            count += Skill("Earthquake", "지진", MonsterSkillCategory.PhysicalAttack, MonsterSkillTrigger.Manual,
                10f, 0f, 5f, dmgMult: R(2.0f, 3.5f), status: StatusType.Stun, sChance: R(0.5f, 0.7f), sDur: R(1.5f, 2.5f));
            count += Skill("IronFist", "철권", MonsterSkillCategory.PhysicalAttack, MonsterSkillTrigger.Manual,
                4f, 0f, 1.5f, dmgMult: R(1.5f, 2.0f));
            count += Skill("StoneSkin", "돌 피부", MonsterSkillCategory.PhysicalDefense, MonsterSkillTrigger.OnTakeDamage,
                15f, 0f, 0f, defBonus: R(15f, 25f), dur: R(5f, 8f));
            count += Skill("MagmaPunch", "용암 주먹", MonsterSkillCategory.PhysicalAttack, MonsterSkillTrigger.Manual,
                6f, 5f, 1.5f, dmgMult: R(2.0f, 3.0f), status: StatusType.Burn, sChance: R(0.6f, 0.8f), sDur: R(4f, 6f));

            // === 범용 엘리트 전용 스킬 (5개) ===
            count += Skill("EliteTeleport", "엘리트 순간이동", MonsterSkillCategory.SpecialAbility, MonsterSkillTrigger.OnCooldown,
                8f, 0f, 10f);
            count += Skill("EliteReflect", "엘리트 반사 방벽", MonsterSkillCategory.MagicalDefense, MonsterSkillTrigger.OnTakeDamage,
                12f, 0f, 0f, mdefBonus: R(10f, 20f), dur: R(5f, 8f));
            count += Skill("EliteEnrage", "엘리트 분노 폭발", MonsterSkillCategory.DamageBonus, MonsterSkillTrigger.OnLowHealth,
                0f, 0f, 0f, dmgMult: R(1.5f, 2.0f), dur: R(15f, 20f));
            count += Skill("EliteHeal", "엘리트 회복", MonsterSkillCategory.HealthBonus, MonsterSkillTrigger.OnLowHealth,
                30f, 0f, 0f, healRange: R(25f, 40f));
            count += Skill("EliteAura", "엘리트 강화 오라", MonsterSkillCategory.AuraEffect, MonsterSkillTrigger.OnCombatStart,
                0f, 0f, 5f, strBonus: R(3f, 6f), agiBonus: R(3f, 6f), dur: R(30f, 60f));

            return count;
        }

        // ===================== 64 신규 변종 (8종족×8변종) =====================
        private static int GenerateNewVariants()
        {
            int count = 0;

            // 각 신규 종족에 대해 8변종 생성
            var raceConfigs = new[]
            {
                // race, nameKr, aiDefault
                (MonsterRace.Spider, "거미", MonsterAIType.Aggressive),
                (MonsterRace.Serpent, "뱀", MonsterAIType.Aggressive),
                (MonsterRace.Fungal, "버섯", MonsterAIType.Defensive),
                (MonsterRace.Bandit, "산적", MonsterAIType.Aggressive),
                (MonsterRace.Cultist, "광신도", MonsterAIType.Defensive),
                (MonsterRace.Drowned, "수몰자", MonsterAIType.Aggressive),
                (MonsterRace.Insect, "곤충", MonsterAIType.Aggressive),
                (MonsterRace.Golem, "골렘", MonsterAIType.Defensive),
            };

            foreach (var (race, nameKr, aiDefault) in raceConfigs)
            {
                string raceStr = race.ToString();

                // Normal - 기본
                count += CreateVariant($"{raceStr}_Normal", $"{nameKr}", $"기본 {nameKr} 몬스터",
                    race,
                    sMin: new int[] { -2, -2, -2, -2, -2, -2, -2, -2 },
                    sMax: new int[] { 2, 2, 2, 2, 2, 2, 2, 2 },
                    spawnWeight: 50, minFloor: 1, maxFloor: 10, aiType: aiDefault, aggression: 1f);

                // Swarmer - 떼거리 (약하지만 많이 출현)
                count += CreateVariant($"{raceStr}_Swarmer", $"떼{nameKr}", $"약하지만 무리 지어 다니는 {nameKr}",
                    race,
                    sMin: new int[] { -4, 0, -4, -3, -4, -3, 0, -4 },
                    sMax: new int[] { -1, 3, -1, 0, -1, 0, 2, -1 },
                    spawnWeight: 35, minFloor: 1, maxFloor: 8, aiType: aiDefault, aggression: 1.3f);

                // Elite - 정예
                count += CreateVariant($"{raceStr}_Elite", $"정예 {nameKr}", $"강화된 정예 {nameKr}",
                    race,
                    sMin: new int[] { 0, 0, 0, 0, 0, 0, 0, 0 },
                    sMax: new int[] { 4, 4, 4, 4, 4, 4, 4, 4 },
                    spawnWeight: 15, minFloor: 3, maxFloor: 10, aiType: aiDefault, aggression: 1.2f);

                // Bruiser - 탱커 (HP 높고 느림)
                count += CreateVariant($"{raceStr}_Bruiser", $"거대 {nameKr}", $"거대하고 느리지만 강력한 {nameKr}",
                    race,
                    sMin: new int[] { 2, -3, 4, -2, 3, 0, -2, 3 },
                    sMax: new int[] { 5, 0, 8, 0, 6, 2, 0, 6 },
                    spawnWeight: 12, minFloor: 2, maxFloor: 10, aiType: MonsterAIType.Aggressive, aggression: 0.8f);

                // Shaman - 지원 (마법형)
                count += CreateVariant($"{raceStr}_Shaman", $"{nameKr} 주술사", $"동료를 치유하고 강화하는 {nameKr} 주술사",
                    race,
                    sMin: new int[] { -1, -1, -1, 2, -1, 2, 0, -1 },
                    sMax: new int[] { 1, 1, 1, 5, 1, 5, 2, 1 },
                    spawnWeight: 12, minFloor: 2, maxFloor: 8, aiType: MonsterAIType.Defensive, aggression: 0.6f);

                // Berserker - 광전사 (공격 특화)
                count += CreateVariant($"{raceStr}_Berserker", $"광폭 {nameKr}", $"분노에 휩싸인 {nameKr}. 공격적이지만 방어가 약하다",
                    race,
                    sMin: new int[] { 2, 1, -1, -2, -2, -2, 0, -2 },
                    sMax: new int[] { 6, 4, 2, 0, 0, 0, 2, 0 },
                    spawnWeight: 12, minFloor: 2, maxFloor: 9, aiType: MonsterAIType.Aggressive, aggression: 1.5f);

                // Leader - 지도자 (균형 강화)
                count += CreateVariant($"{raceStr}_Leader", $"{nameKr} 대장", $"{nameKr} 무리의 지도자. 균형 잡힌 강화 스탯",
                    race,
                    sMin: new int[] { 1, 1, 1, 1, 1, 1, 1, 1 },
                    sMax: new int[] { 3, 3, 3, 3, 3, 3, 3, 3 },
                    spawnWeight: 6, minFloor: 5, maxFloor: 10, aiType: MonsterAIType.Strategic, aggression: 1.0f);

                // Boss - 보스 (최강)
                count += CreateVariant($"{raceStr}_Boss", $"{nameKr} 군주", $"{nameKr} 종족의 군주. 최강의 개체",
                    race,
                    sMin: new int[] { 3, 3, 3, 3, 3, 3, 3, 3 },
                    sMax: new int[] { 6, 6, 6, 6, 6, 6, 6, 6 },
                    spawnWeight: 3, minFloor: 8, maxFloor: 10, aiType: MonsterAIType.Strategic, aggression: 1.0f);
            }

            return count;
        }

        // ===================== 유틸리티 =====================

        private static int CreateRace(string fileName, MonsterRace raceType, string raceName, string desc,
            int str, int agi, int vit, int intel, int def, int mdef, int luk, int stab,
            float strG, float agiG, float vitG, float intG, float defG, float mdefG, float lukG, float stabG,
            long baseExp, long baseGold, float soulRate)
        {
            string path = $"{racePath}/{fileName}_RaceData.asset";
            if (AssetDatabase.LoadAssetAtPath<MonsterRaceData>(path) != null) return 0;

            var data = ScriptableObject.CreateInstance<MonsterRaceData>();
            var so = new SerializedObject(data);

            so.FindProperty("raceType").enumValueIndex = (int)raceType;
            so.FindProperty("raceName").stringValue = raceName;
            so.FindProperty("description").stringValue = desc;

            // Base stats (float 타입)
            var stats = so.FindProperty("baseStats");
            stats.FindPropertyRelative("strength").floatValue = str;
            stats.FindPropertyRelative("agility").floatValue = agi;
            stats.FindPropertyRelative("vitality").floatValue = vit;
            stats.FindPropertyRelative("intelligence").floatValue = intel;
            stats.FindPropertyRelative("defense").floatValue = def;
            stats.FindPropertyRelative("magicDefense").floatValue = mdef;
            stats.FindPropertyRelative("luck").floatValue = luk;
            stats.FindPropertyRelative("stability").floatValue = stab;

            // Growth
            var growth = so.FindProperty("gradeGrowth");
            growth.FindPropertyRelative("strengthGrowth").floatValue = strG;
            growth.FindPropertyRelative("agilityGrowth").floatValue = agiG;
            growth.FindPropertyRelative("vitalityGrowth").floatValue = vitG;
            growth.FindPropertyRelative("intelligenceGrowth").floatValue = intG;
            growth.FindPropertyRelative("defenseGrowth").floatValue = defG;
            growth.FindPropertyRelative("magicDefenseGrowth").floatValue = mdefG;
            growth.FindPropertyRelative("luckGrowth").floatValue = lukG;
            growth.FindPropertyRelative("stabilityGrowth").floatValue = stabG;

            so.FindProperty("baseExperience").longValue = baseExp;
            so.FindProperty("baseGold").longValue = baseGold;
            so.FindProperty("soulDropRate").floatValue = soulRate;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(data);
            AssetDatabase.CreateAsset(data, path);
            return 1;
        }

        private static int CreateVariant(string id, string name, string desc, MonsterRace race,
            int[] sMin, int[] sMax, float spawnWeight, int minFloor, int maxFloor,
            MonsterAIType aiType, float aggression)
        {
            string path = $"{variantPath}/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<MonsterVariantData>(path) != null) return 0;

            var data = ScriptableObject.CreateInstance<MonsterVariantData>();
            var so = new SerializedObject(data);

            so.FindProperty("variantId").stringValue = id;
            so.FindProperty("variantName").stringValue = name;
            so.FindProperty("description").stringValue = desc;

            // baseRace 참조 - raceType으로 찾기
            var allRaces = LoadAllRaces();
            MonsterRaceData raceData = null;
            foreach (var r in allRaces)
            {
                var rso = new SerializedObject(r);
                if (rso.FindProperty("raceType").enumValueIndex == (int)race)
                {
                    raceData = r;
                    break;
                }
            }
            if (raceData != null)
                so.FindProperty("baseRace").objectReferenceValue = raceData;

            // Stat variances (float 타입)
            var statMin = so.FindProperty("statMinVariance");
            statMin.FindPropertyRelative("strength").floatValue = sMin[0];
            statMin.FindPropertyRelative("agility").floatValue = sMin[1];
            statMin.FindPropertyRelative("vitality").floatValue = sMin[2];
            statMin.FindPropertyRelative("intelligence").floatValue = sMin[3];
            statMin.FindPropertyRelative("defense").floatValue = sMin[4];
            statMin.FindPropertyRelative("magicDefense").floatValue = sMin[5];
            statMin.FindPropertyRelative("luck").floatValue = sMin[6];
            statMin.FindPropertyRelative("stability").floatValue = sMin[7];

            var statMax = so.FindProperty("statMaxVariance");
            statMax.FindPropertyRelative("strength").floatValue = sMax[0];
            statMax.FindPropertyRelative("agility").floatValue = sMax[1];
            statMax.FindPropertyRelative("vitality").floatValue = sMax[2];
            statMax.FindPropertyRelative("intelligence").floatValue = sMax[3];
            statMax.FindPropertyRelative("defense").floatValue = sMax[4];
            statMax.FindPropertyRelative("magicDefense").floatValue = sMax[5];
            statMax.FindPropertyRelative("luck").floatValue = sMax[6];
            statMax.FindPropertyRelative("stability").floatValue = sMax[7];

            so.FindProperty("spawnWeight").floatValue = spawnWeight;
            so.FindProperty("minFloor").intValue = minFloor;
            so.FindProperty("maxFloor").intValue = maxFloor;
            so.FindProperty("preferredAIType").enumValueIndex = (int)aiType;
            so.FindProperty("aggressionMultiplier").floatValue = aggression;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(data);
            AssetDatabase.CreateAsset(data, path);
            return 1;
        }

        private static MonsterRaceData[] LoadAllRaces()
        {
            return Resources.LoadAll<MonsterRaceData>("ScriptableObjects/MonsterData/MonsterRaceData");
        }

        private static int Skill(string name, string desc, MonsterSkillCategory cat, MonsterSkillTrigger trigger,
            float cd, float mana, float range,
            SkillEffectRange? dmgMult = null, SkillEffectRange? healRange = null,
            SkillEffectRange? defBonus = null, SkillEffectRange? mdefBonus = null,
            SkillEffectRange? strBonus = null, SkillEffectRange? agiBonus = null,
            SkillEffectRange? spdMult = null, SkillEffectRange? atkSpdMult = null,
            SkillEffectRange? dur = null,
            StatusType status = StatusType.None, SkillEffectRange? sChance = null, SkillEffectRange? sDur = null)
        {
            string path = $"{skillPath}/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<MonsterSkillData>(path) != null) return 0;

            var data = ScriptableObject.CreateInstance<MonsterSkillData>();
            var so = new SerializedObject(data);

            so.FindProperty("skillName").stringValue = name;
            so.FindProperty("description").stringValue = desc;
            so.FindProperty("skillType").enumValueIndex = (int)MonsterSkillType.Active;
            so.FindProperty("category").enumValueIndex = (int)cat;
            so.FindProperty("trigger").enumValueIndex = (int)trigger;
            so.FindProperty("cooldown").floatValue = cd;
            so.FindProperty("manaCost").floatValue = mana;
            so.FindProperty("range").floatValue = range;

            var effect = so.FindProperty("skillEffect");

            if (dmgMult.HasValue) SetRange(effect, "damageMultiplierRange", dmgMult.Value);
            if (healRange.HasValue) SetRange(effect, "healingAmountRange", healRange.Value);
            if (defBonus.HasValue) SetRange(effect, "defenseBonus", defBonus.Value);
            if (mdefBonus.HasValue) SetRange(effect, "magicDefenseBonus", mdefBonus.Value);
            if (strBonus.HasValue) SetRange(effect, "strengthBonus", strBonus.Value);
            if (agiBonus.HasValue) SetRange(effect, "agilityBonus", agiBonus.Value);
            if (spdMult.HasValue) SetRange(effect, "speedMultiplierRange", spdMult.Value);
            if (dur.HasValue) SetRange(effect, "durationRange", dur.Value);

            if (status != StatusType.None)
            {
                effect.FindPropertyRelative("inflictStatus").enumValueIndex = (int)status;
                if (sChance.HasValue) SetRange(effect, "statusChanceRange", sChance.Value);
                if (sDur.HasValue) SetRange(effect, "statusDurationRange", sDur.Value);
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(data);
            AssetDatabase.CreateAsset(data, path);
            return 1;
        }

        private static void SetRange(SerializedProperty parent, string propName, SkillEffectRange range)
        {
            var prop = parent.FindPropertyRelative(propName);
            if (prop != null)
            {
                prop.FindPropertyRelative("minValue").floatValue = range.minValue;
                prop.FindPropertyRelative("maxValue").floatValue = range.maxValue;
            }
        }

        private static SkillEffectRange R(float min, float max) => new SkillEffectRange { minValue = min, maxValue = max };

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] parts = path.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }
        }
    }
}
