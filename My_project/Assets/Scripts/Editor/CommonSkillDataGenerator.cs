using UnityEngine;
using UnityEditor;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 공통 스킬 100개 생성 (직업 무관, 누구나 학습 가능)
    /// 원소30 + 생존15 + 이동10 + 소환15 + 버프15 + 디버프15 = 100개
    /// </summary>
    public class CommonSkillDataGenerator
    {
        private static string basePath = "Assets/Resources/Skills/Common";

        [MenuItem("Dungeon Crawler/Generate Common Skills 100")]
        public static void GenerateAll()
        {
            EnsureFolder(basePath);
            EnsureFolder(basePath + "/Elemental");
            EnsureFolder(basePath + "/Survival");
            EnsureFolder(basePath + "/Movement");
            EnsureFolder(basePath + "/Summon");
            EnsureFolder(basePath + "/Buff");
            EnsureFolder(basePath + "/Debuff");

            int total = 0;
            total += GenerateElementalSkills();
            total += GenerateSurvivalSkills();
            total += GenerateMovementSkills();
            total += GenerateSummonSkills();
            total += GenerateBuffSkills();
            total += GenerateDebuffSkills();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CommonSkills] 공통 스킬 {total}개 생성 완료");
        }

        // ==================== 원소 스킬 30개 ====================
        private static int GenerateElementalSkills()
        {
            int c = 0;
            string p = basePath + "/Elemental";

            // 화염계 (4개)
            c += S(p, "FireBolt", "화염탄", "작은 화염구를 발사한다", SkillCategory.CommonElemental,
                DamageType.Fire, 15, 8, 3, 4, 1, 1.0f, Race.Human, 500,
                StatusType.Burn, 3, 0.3f, 3, SkillBehaviorType.Projectile);
            c += S(p, "Fireball", "화염구", "중형 화염구를 던져 범위 피해를 준다", SkillCategory.CommonElemental,
                DamageType.Fire, 35, 20, 6, 5, 3, 1.2f, Race.Human, 1500,
                StatusType.Burn, 5, 0.5f, 5, SkillBehaviorType.Projectile);
            c += S(p, "FlameWall", "화염벽", "화염의 벽을 세워 관통하는 적에게 데미지", SkillCategory.CommonElemental,
                DamageType.Fire, 25, 25, 8, 4, 5, 1.3f, Race.Human, 3000,
                StatusType.Burn, 4, 0.7f, 6, SkillBehaviorType.Summon);
            c += S(p, "Inferno", "인페르노", "지역을 화염으로 뒤덮는 강력한 마법", SkillCategory.CommonElemental,
                DamageType.Fire, 60, 40, 12, 6, 7, 1.5f, Race.Human, 6000,
                StatusType.Burn, 6, 0.9f, 8, SkillBehaviorType.Summon);

            // 빙결계 (4개)
            c += S(p, "IceShard", "얼음 파편", "날카로운 얼음 조각을 발사한다", SkillCategory.CommonElemental,
                DamageType.Ice, 12, 7, 3, 5, 1, 1.0f, Race.Human, 500,
                StatusType.Slow, 3, 0.4f, 3, SkillBehaviorType.Projectile);
            c += S(p, "FrostNova", "서리 폭발", "주변 적을 서리로 동결시킨다", SkillCategory.CommonElemental,
                DamageType.Ice, 25, 18, 6, 3, 3, 1.1f, Race.Human, 1500,
                StatusType.Freeze, 2, 0.4f, 4, SkillBehaviorType.Instant);
            c += S(p, "Blizzard", "눈보라", "광범위 눈보라로 지속 피해를 준다", SkillCategory.CommonElemental,
                DamageType.Ice, 20, 30, 10, 6, 5, 1.3f, Race.Human, 3000,
                StatusType.Slow, 5, 0.8f, 6, SkillBehaviorType.Summon);
            c += S(p, "AbsoluteZero", "절대영도", "모든 것을 얼리는 극강의 빙결마법", SkillCategory.CommonElemental,
                DamageType.Ice, 50, 45, 15, 5, 7, 1.5f, Race.Human, 6000,
                StatusType.Freeze, 3, 0.7f, 8, SkillBehaviorType.Summon);

            // 번개계 (4개)
            c += S(p, "Spark", "전기 불꽃", "전기 불꽃으로 적을 공격한다", SkillCategory.CommonElemental,
                DamageType.Lightning, 14, 9, 2, 5, 1, 1.0f, Race.Human, 500,
                StatusType.Stun, 1, 0.2f, 3, SkillBehaviorType.Instant);
            c += S(p, "LightningBolt", "번개", "하늘에서 번개를 내리친다", SkillCategory.CommonElemental,
                DamageType.Lightning, 40, 22, 5, 6, 3, 1.2f, Race.Human, 1500,
                StatusType.Stun, 1.5f, 0.35f, 5, SkillBehaviorType.Instant);
            c += S(p, "ChainLightning", "연쇄 번개", "적 사이를 뛰어다니는 번개", SkillCategory.CommonElemental,
                DamageType.Lightning, 30, 25, 7, 7, 5, 1.3f, Race.Human, 3000,
                StatusType.Stun, 1, 0.3f, 5, SkillBehaviorType.Projectile);
            c += S(p, "Thunderstorm", "뇌우", "광역 번개 폭풍을 소환한다", SkillCategory.CommonElemental,
                DamageType.Lightning, 55, 40, 12, 6, 7, 1.5f, Race.Human, 6000,
                StatusType.Stun, 2, 0.5f, 7, SkillBehaviorType.Summon);

            // 독계 (4개)
            c += S(p, "PoisonDart", "독침", "독이 묻은 작은 바늘을 발사한다", SkillCategory.CommonElemental,
                DamageType.Poison, 8, 6, 3, 5, 1, 0.8f, Race.Human, 500,
                StatusType.Poison, 6, 0.6f, 4, SkillBehaviorType.Projectile);
            c += S(p, "ToxicCloud", "독안개", "독 구름을 생성하여 지속 피해", SkillCategory.CommonElemental,
                DamageType.Poison, 15, 18, 8, 4, 3, 1.0f, Race.Human, 1500,
                StatusType.Poison, 8, 0.8f, 6, SkillBehaviorType.Summon);
            c += S(p, "VenomStrike", "맹독 일격", "독이 응축된 강력한 일격", SkillCategory.CommonElemental,
                DamageType.Poison, 30, 20, 6, 2, 5, 1.2f, Race.Human, 3000,
                StatusType.Poison, 10, 0.9f, 8, SkillBehaviorType.Instant);
            c += S(p, "Pandemic", "전염병", "치명적인 독이 적들 사이에 전염된다", SkillCategory.CommonElemental,
                DamageType.Poison, 20, 35, 12, 6, 7, 1.3f, Race.Human, 6000,
                StatusType.Poison, 12, 1.0f, 10, SkillBehaviorType.Summon);

            // 암흑계 (4개)
            c += S(p, "ShadowBolt", "어둠의 화살", "암흑 에너지를 발사한다", SkillCategory.CommonElemental,
                DamageType.Dark, 16, 10, 3, 5, 1, 1.0f, Race.Human, 500,
                StatusType.Weakness, 4, 0.3f, 3, SkillBehaviorType.Projectile);
            c += S(p, "DarkPulse", "암흑 파동", "주변에 암흑 충격파를 방출한다", SkillCategory.CommonElemental,
                DamageType.Dark, 28, 18, 6, 3, 3, 1.1f, Race.Human, 1500,
                StatusType.Weakness, 5, 0.5f, 4, SkillBehaviorType.Instant);
            c += S(p, "VoidRift", "공허의 균열", "공간을 찢어 적을 집어삼킨다", SkillCategory.CommonElemental,
                DamageType.Dark, 40, 28, 9, 5, 5, 1.3f, Race.Human, 3000,
                StatusType.Root, 3, 0.6f, 5, SkillBehaviorType.Summon);
            c += S(p, "Oblivion", "망각", "존재를 지우는 궁극의 암흑 마법", SkillCategory.CommonElemental,
                DamageType.Dark, 65, 45, 14, 5, 7, 1.5f, Race.Human, 6000,
                StatusType.Weakness, 8, 0.8f, 6, SkillBehaviorType.Summon);

            // 신성계 (4개)
            c += S(p, "HolyLight", "성스러운 빛", "신성한 빛으로 적을 공격한다", SkillCategory.CommonElemental,
                DamageType.Holy, 14, 10, 3, 5, 1, 1.0f, Race.Human, 500,
                StatusType.None, 0, 0, 0, SkillBehaviorType.Projectile);
            c += S(p, "Smite", "천벌", "하늘에서 신성한 빛이 내리친다", SkillCategory.CommonElemental,
                DamageType.Holy, 35, 22, 6, 5, 3, 1.2f, Race.Human, 1500,
                StatusType.Stun, 1.5f, 0.3f, 4, SkillBehaviorType.Instant);
            c += S(p, "DivineJudgment", "신성 심판", "신의 심판이 내려진다", SkillCategory.CommonElemental,
                DamageType.Holy, 45, 30, 10, 5, 5, 1.4f, Race.Human, 3000,
                StatusType.None, 0, 0, 0, SkillBehaviorType.Summon);
            c += S(p, "HolyNova", "홀리 노바", "강력한 신성 폭발로 광역 정화", SkillCategory.CommonElemental,
                DamageType.Holy, 55, 40, 12, 4, 7, 1.5f, Race.Human, 6000,
                StatusType.None, 0, 0, 0, SkillBehaviorType.Summon);

            // 바람계 (3개)
            c += S(p, "WindBlade", "바람의 칼날", "날카로운 바람으로 베어낸다", SkillCategory.CommonElemental,
                DamageType.Physical, 12, 8, 2, 4, 1, 1.0f, Race.Human, 500,
                StatusType.None, 0, 0, 0, SkillBehaviorType.Projectile);
            c += S(p, "Tornado", "토네이도", "거대한 회오리바람을 소환한다", SkillCategory.CommonElemental,
                DamageType.Physical, 30, 25, 8, 5, 4, 1.2f, Race.Human, 2500,
                StatusType.Slow, 3, 0.6f, 5, SkillBehaviorType.Summon);
            c += S(p, "Cyclone", "사이클론", "자신을 중심으로 회전하는 폭풍", SkillCategory.CommonElemental,
                DamageType.Physical, 20, 15, 6, 3, 3, 1.0f, Race.Human, 1500,
                StatusType.None, 0, 0, 0, SkillBehaviorType.Instant);

            // 대지계 (3개)
            c += S(p, "RockSpike", "암석 돌출", "땅에서 바위 기둥을 솟아오르게 한다", SkillCategory.CommonElemental,
                DamageType.Physical, 18, 12, 4, 3, 2, 1.0f, Race.Human, 800,
                StatusType.Stun, 1, 0.3f, 3, SkillBehaviorType.Summon);
            c += S(p, "EarthquakeSkill", "지진", "대지를 흔들어 광역 피해를 준다", SkillCategory.CommonElemental,
                DamageType.Physical, 40, 30, 10, 5, 5, 1.3f, Race.Human, 3500,
                StatusType.Stun, 2, 0.5f, 5, SkillBehaviorType.Summon);
            c += S(p, "StoneWall", "돌벽", "단단한 돌벽을 세워 피해를 흡수", SkillCategory.CommonElemental,
                DamageType.Physical, 0, 20, 8, 2, 4, 0, Race.Human, 2000,
                StatusType.Shield, 10, 1.0f, 50, SkillBehaviorType.Instant);

            return c;
        }

        // ==================== 생존 스킬 15개 ====================
        private static int GenerateSurvivalSkills()
        {
            int c = 0;
            string p = basePath + "/Survival";

            // 회피/방어 (5개)
            c += S(p, "Dodge", "회피", "순간적으로 적의 공격을 회피한다", SkillCategory.CommonSurvival,
                DamageType.Physical, 0, 0, 2, 0, 1, 0, Race.Human, 300,
                StatusType.Speed, 1, 1.0f, 3, SkillBehaviorType.Instant);
            c += S(p, "Block", "방패 막기", "앞으로 오는 공격을 방어한다", SkillCategory.CommonSurvival,
                DamageType.Physical, 0, 5, 3, 0, 1, 0, Race.Human, 400,
                StatusType.Shield, 3, 1.0f, 30, SkillBehaviorType.Instant);
            c += S(p, "Parry", "패리", "적의 공격을 받아쳐 스턴시킨다", SkillCategory.CommonSurvival,
                DamageType.Physical, 10, 8, 4, 1.5f, 2, 0.8f, Race.Human, 800,
                StatusType.Stun, 1.5f, 0.6f, 3, SkillBehaviorType.Instant);
            c += S(p, "IronSkin", "강철 피부", "잠시 물리 방어력이 크게 증가한다", SkillCategory.CommonSurvival,
                DamageType.Physical, 0, 15, 8, 0, 3, 0, Race.Human, 1500,
                StatusType.Shield, 8, 1.0f, 50, SkillBehaviorType.Instant);
            c += S(p, "MagicBarrier", "마법 장벽", "마법 데미지를 흡수하는 보호막", SkillCategory.CommonSurvival,
                DamageType.Magical, 0, 18, 10, 0, 4, 0, Race.Human, 2500,
                StatusType.Shield, 10, 1.0f, 80, SkillBehaviorType.Instant);

            // 치유 (5개)
            c += S(p, "FirstAid", "응급 치료", "간단한 치료로 소량의 HP를 회복한다", SkillCategory.CommonSurvival,
                DamageType.Holy, -20, 10, 5, 0, 1, 0.5f, Race.Human, 300,
                StatusType.None, 0, 0, 0, SkillBehaviorType.Instant);
            c += S(p, "Bandage", "붕대 감기", "붕대로 지속적인 HP 회복", SkillCategory.CommonSurvival,
                DamageType.Holy, -8, 8, 6, 0, 1, 0.3f, Race.Human, 400,
                StatusType.Regeneration, 10, 1.0f, 5, SkillBehaviorType.Instant);
            c += S(p, "HealingSpring", "치유의 샘", "주변 아군을 천천히 회복시키는 영역", SkillCategory.CommonSurvival,
                DamageType.Holy, -10, 25, 12, 4, 4, 0.8f, Race.Human, 2500,
                StatusType.Regeneration, 15, 1.0f, 10, SkillBehaviorType.Summon);
            c += S(p, "Purification", "정화", "모든 디버프를 제거한다", SkillCategory.CommonSurvival,
                DamageType.Holy, 0, 15, 8, 0, 3, 0, Race.Human, 1500,
                StatusType.Blessing, 3, 1.0f, 2, SkillBehaviorType.Instant);
            c += S(p, "LifeSteal", "생명력 흡수", "적의 HP를 빼앗아 자신을 회복한다", SkillCategory.CommonSurvival,
                DamageType.Dark, 25, 20, 8, 2, 5, 1.0f, Race.Human, 3500,
                StatusType.None, 0, 0, 0, SkillBehaviorType.Instant);

            // 카운터 (5개)
            c += S(p, "CounterAttack", "반격", "피격 시 즉시 반격한다", SkillCategory.CommonSurvival,
                DamageType.Physical, 20, 5, 4, 1.5f, 2, 1.2f, Race.Human, 800,
                StatusType.None, 0, 0, 0, SkillBehaviorType.Instant);
            c += S(p, "Reflect", "반사", "받은 마법 데미지를 되돌린다", SkillCategory.CommonSurvival,
                DamageType.Magical, 0, 20, 10, 0, 4, 0, Race.Human, 2500,
                StatusType.Shield, 5, 1.0f, 40, SkillBehaviorType.Instant);
            c += S(p, "SecondWind", "재기", "체력이 낮을 때 HP 대량 회복", SkillCategory.CommonSurvival,
                DamageType.Holy, -80, 30, 30, 0, 5, 1.0f, Race.Human, 4000,
                StatusType.Regeneration, 5, 1.0f, 15, SkillBehaviorType.Instant);
            c += S(p, "DeathResist", "죽음의 저항", "치명타를 한번 무효화한다", SkillCategory.CommonSurvival,
                DamageType.Physical, 0, 40, 60, 0, 6, 0, Race.Human, 5000,
                StatusType.Shield, 15, 1.0f, 999, SkillBehaviorType.Instant);
            c += S(p, "Fortify", "요새화", "이동 불가 대신 방어력 대폭 증가", SkillCategory.CommonSurvival,
                DamageType.Physical, 0, 10, 8, 0, 3, 0, Race.Human, 1200,
                StatusType.Root, 5, 1.0f, 0, SkillBehaviorType.Instant);

            return c;
        }

        // ==================== 이동 스킬 10개 ====================
        private static int GenerateMovementSkills()
        {
            int c = 0;
            string p = basePath + "/Movement";

            c += S(p, "Dash", "돌진", "짧은 거리를 순간 이동한다", SkillCategory.CommonMovement,
                DamageType.Physical, 5, 5, 2, 3, 1, 0.5f, Race.Human, 300,
                StatusType.Speed, 0.5f, 1.0f, 5, SkillBehaviorType.Instant);
            c += S(p, "Blink", "점멸", "짧은 거리를 순간이동한다", SkillCategory.CommonMovement,
                DamageType.Magical, 0, 15, 5, 5, 3, 0, Race.Human, 2000,
                StatusType.None, 0, 0, 0, SkillBehaviorType.Instant);
            c += S(p, "Sprint", "질주", "이동 속도가 크게 증가한다", SkillCategory.CommonMovement,
                DamageType.Physical, 0, 8, 6, 0, 1, 0, Race.Human, 500,
                StatusType.Speed, 5, 1.0f, 5, SkillBehaviorType.Instant);
            c += S(p, "LeapSlam", "도약 강타", "높이 뛰어올라 착지 시 광역 데미지", SkillCategory.CommonMovement,
                DamageType.Physical, 30, 15, 5, 4, 3, 1.2f, Race.Human, 1500,
                StatusType.Stun, 1, 0.4f, 4, SkillBehaviorType.Summon);
            c += S(p, "ChargeAttack", "돌격", "적을 향해 돌진하며 공격한다", SkillCategory.CommonMovement,
                DamageType.Physical, 25, 12, 4, 5, 2, 1.1f, Race.Human, 1000,
                StatusType.Stun, 1, 0.3f, 3, SkillBehaviorType.Instant);
            c += S(p, "ShadowStep", "그림자 걸음", "그림자를 통해 적 뒤로 이동", SkillCategory.CommonMovement,
                DamageType.Dark, 15, 12, 6, 4, 3, 0.8f, Race.Human, 1500,
                StatusType.None, 0, 0, 0, SkillBehaviorType.Instant);
            c += S(p, "Whirlwind", "회오리", "회전하며 이동, 닿는 적에게 피해", SkillCategory.CommonMovement,
                DamageType.Physical, 18, 15, 5, 2, 2, 1.0f, Race.Human, 1200,
                StatusType.None, 0, 0, 0, SkillBehaviorType.Instant);
            c += S(p, "RocketJump", "로켓 점프", "폭발 추진으로 높이 뛰어오른다", SkillCategory.CommonMovement,
                DamageType.Fire, 20, 18, 6, 5, 3, 1.0f, Race.Human, 2000,
                StatusType.Burn, 2, 0.3f, 3, SkillBehaviorType.Instant);
            c += S(p, "PhaseShift", "위상 변환", "잠시 비물질화하여 공격을 피한다", SkillCategory.CommonMovement,
                DamageType.Magical, 0, 25, 10, 0, 5, 0, Race.Human, 3500,
                StatusType.Invisibility, 2, 1.0f, 0, SkillBehaviorType.Instant);
            c += S(p, "GrapplingHook", "갈고리 발사", "갈고리로 적에게 달라붙어 공격", SkillCategory.CommonMovement,
                DamageType.Physical, 15, 10, 4, 6, 2, 0.9f, Race.Human, 1000,
                StatusType.Root, 1, 0.4f, 3, SkillBehaviorType.Projectile);

            return c;
        }

        // ==================== 소환 스킬 15개 ====================
        private static int GenerateSummonSkills()
        {
            int c = 0;
            string p = basePath + "/Summon";

            c += S(p, "SummonSkeleton", "해골 소환", "해골 전사를 소환하여 싸우게 한다", SkillCategory.CommonSummon,
                DamageType.Dark, 15, 20, 12, 3, 2, 0.8f, Race.Human, 1000,
                StatusType.None, 0, 0, 0, SkillBehaviorType.Summon);
            c += S(p, "SummonWolf", "늑대 소환", "늑대를 소환하여 적을 추격시킨다", SkillCategory.CommonSummon,
                DamageType.Physical, 18, 18, 10, 4, 2, 0.9f, Race.Human, 1000,
                StatusType.None, 0, 0, 0, SkillBehaviorType.Summon);
            c += S(p, "SummonGolem", "골렘 소환", "돌 골렘을 소환하여 방어시킨다", SkillCategory.CommonSummon,
                DamageType.Physical, 10, 30, 15, 3, 4, 0.6f, Race.Human, 2500,
                StatusType.Shield, 10, 1.0f, 30, SkillBehaviorType.Summon);
            c += S(p, "FireElemental", "화염 정령", "화염 정령을 소환한다", SkillCategory.CommonSummon,
                DamageType.Fire, 25, 25, 12, 4, 4, 1.1f, Race.Human, 2500,
                StatusType.Burn, 5, 0.5f, 5, SkillBehaviorType.Summon);
            c += S(p, "IceElemental", "얼음 정령", "얼음 정령을 소환한다", SkillCategory.CommonSummon,
                DamageType.Ice, 20, 25, 12, 4, 4, 1.0f, Race.Human, 2500,
                StatusType.Slow, 5, 0.6f, 4, SkillBehaviorType.Summon);
            c += S(p, "LightningSpirit", "번개 정령", "번개 정령을 소환한다", SkillCategory.CommonSummon,
                DamageType.Lightning, 22, 25, 12, 5, 4, 1.1f, Race.Human, 2500,
                StatusType.Stun, 1, 0.3f, 4, SkillBehaviorType.Summon);
            c += S(p, "ShadowClone", "분신술", "자신의 분신을 만들어 싸우게 한다", SkillCategory.CommonSummon,
                DamageType.Physical, 15, 22, 10, 3, 3, 0.7f, Race.Human, 2000,
                StatusType.None, 0, 0, 0, SkillBehaviorType.Summon);
            c += S(p, "SummonSpider", "거미 소환", "독거미를 소환하여 적을 공격", SkillCategory.CommonSummon,
                DamageType.Poison, 12, 15, 10, 3, 2, 0.7f, Race.Human, 1000,
                StatusType.Poison, 6, 0.5f, 3, SkillBehaviorType.Summon);
            c += S(p, "PlantGrowth", "식물 성장", "덩굴을 소환하여 적을 속박한다", SkillCategory.CommonSummon,
                DamageType.Physical, 10, 18, 8, 4, 3, 0.6f, Race.Human, 1500,
                StatusType.Root, 4, 0.7f, 4, SkillBehaviorType.Summon);
            c += S(p, "SummonBear", "곰 소환", "강력한 곰을 소환하여 싸우게 한다", SkillCategory.CommonSummon,
                DamageType.Physical, 25, 30, 15, 3, 5, 1.0f, Race.Human, 3500,
                StatusType.None, 0, 0, 0, SkillBehaviorType.Summon);
            c += S(p, "SpiritArmy", "영혼 군대", "여러 영혼 전사를 소환한다", SkillCategory.CommonSummon,
                DamageType.Dark, 20, 40, 20, 5, 6, 0.9f, Race.Human, 5000,
                StatusType.Weakness, 5, 0.4f, 5, SkillBehaviorType.Summon);
            c += S(p, "SummonPhoenix", "불사조 소환", "불사조를 소환하여 화염 공격", SkillCategory.CommonSummon,
                DamageType.Fire, 45, 45, 25, 5, 7, 1.3f, Race.Human, 6000,
                StatusType.Burn, 6, 0.8f, 8, SkillBehaviorType.Summon);
            c += S(p, "TrapSetup", "함정 설치", "보이지 않는 함정을 설치한다", SkillCategory.CommonSummon,
                DamageType.Physical, 30, 12, 6, 3, 2, 1.0f, Race.Human, 800,
                StatusType.Stun, 2, 0.7f, 3, SkillBehaviorType.Summon);
            c += S(p, "MagicTurret", "마법 포탑", "자동으로 적을 공격하는 포탑 설치", SkillCategory.CommonSummon,
                DamageType.Magical, 15, 25, 15, 5, 4, 0.8f, Race.Human, 2500,
                StatusType.None, 0, 0, 0, SkillBehaviorType.Summon);
            c += S(p, "HealingTotem", "치유 토템", "주변 아군을 회복시키는 토템", SkillCategory.CommonSummon,
                DamageType.Holy, -8, 22, 12, 4, 3, 0.5f, Race.Human, 2000,
                StatusType.Regeneration, 15, 1.0f, 8, SkillBehaviorType.Summon);

            return c;
        }

        // ==================== 버프/오라 스킬 15개 ====================
        private static int GenerateBuffSkills()
        {
            int c = 0;
            string p = basePath + "/Buff";

            c += S(p, "WarCry", "전투 함성", "아군의 공격력을 증가시킨다", SkillCategory.CommonBuff,
                DamageType.Physical, 0, 15, 10, 5, 2, 0, Race.Human, 1000,
                StatusType.Strength, 15, 1.0f, 5, SkillBehaviorType.Instant);
            c += S(p, "Haste", "신속", "이동속도와 공격속도 증가", SkillCategory.CommonBuff,
                DamageType.Physical, 0, 12, 8, 0, 2, 0, Race.Human, 800,
                StatusType.Speed, 12, 1.0f, 4, SkillBehaviorType.Instant);
            c += S(p, "BlessingOfLight", "빛의 축복", "모든 스탯이 소폭 증가한다", SkillCategory.CommonBuff,
                DamageType.Holy, 0, 20, 12, 0, 3, 0, Race.Human, 1500,
                StatusType.Blessing, 15, 1.0f, 3, SkillBehaviorType.Instant);
            c += S(p, "BerserkerRage", "광전사의 분노", "공격력 대폭 증가, 방어력 감소", SkillCategory.CommonBuff,
                DamageType.Physical, 0, 10, 10, 0, 3, 0, Race.Human, 1500,
                StatusType.Berserk, 10, 1.0f, 8, SkillBehaviorType.Instant);
            c += S(p, "MagicAmplify", "마력 증폭", "마법 데미지를 증폭시킨다", SkillCategory.CommonBuff,
                DamageType.Magical, 0, 18, 10, 0, 3, 0, Race.Human, 1500,
                StatusType.Enhancement, 12, 1.0f, 5, SkillBehaviorType.Instant);
            c += S(p, "FocusAura", "집중의 오라", "주변 아군의 크리티컬 확률 증가", SkillCategory.CommonBuff,
                DamageType.Physical, 0, 20, 15, 5, 4, 0, Race.Human, 2500,
                StatusType.Enhancement, 20, 1.0f, 4, SkillBehaviorType.Instant);
            c += S(p, "ProtectionAura", "보호의 오라", "주변 아군의 방어력 증가", SkillCategory.CommonBuff,
                DamageType.Holy, 0, 22, 15, 5, 4, 0, Race.Human, 2500,
                StatusType.Shield, 20, 1.0f, 25, SkillBehaviorType.Instant);
            c += S(p, "Bloodlust", "피의 갈증", "공격 시 체력을 회복한다", SkillCategory.CommonBuff,
                DamageType.Dark, 0, 20, 12, 0, 4, 0, Race.Human, 3000,
                StatusType.Regeneration, 15, 1.0f, 5, SkillBehaviorType.Instant);
            c += S(p, "ElementalMastery", "원소 숙련", "모든 원소 데미지 증가", SkillCategory.CommonBuff,
                DamageType.Magical, 0, 25, 15, 0, 5, 0, Race.Human, 3500,
                StatusType.Enhancement, 20, 1.0f, 6, SkillBehaviorType.Instant);
            c += S(p, "Invincibility", "무적", "잠시 모든 피해를 무효화한다", SkillCategory.CommonBuff,
                DamageType.Holy, 0, 50, 30, 0, 7, 0, Race.Human, 6000,
                StatusType.Shield, 3, 1.0f, 9999, SkillBehaviorType.Instant);
            c += S(p, "SpiritLink", "영혼 연결", "파티원과 HP를 공유한다", SkillCategory.CommonBuff,
                DamageType.Holy, 0, 25, 15, 5, 5, 0, Race.Human, 3500,
                StatusType.Regeneration, 20, 1.0f, 3, SkillBehaviorType.Instant);
            c += S(p, "WarBanner", "전쟁 깃발", "설치 지점 주변 아군 강화", SkillCategory.CommonBuff,
                DamageType.Physical, 0, 20, 12, 4, 3, 0, Race.Human, 2000,
                StatusType.Strength, 15, 1.0f, 4, SkillBehaviorType.Summon);
            c += S(p, "Sharpen", "무기 연마", "무기 데미지를 일시적으로 강화", SkillCategory.CommonBuff,
                DamageType.Physical, 0, 8, 6, 0, 1, 0, Race.Human, 500,
                StatusType.Enhancement, 20, 1.0f, 3, SkillBehaviorType.Instant);
            c += S(p, "ManaShield", "마나 방패", "마나로 데미지를 흡수한다", SkillCategory.CommonBuff,
                DamageType.Magical, 0, 20, 10, 0, 4, 0, Race.Human, 2500,
                StatusType.Shield, 15, 1.0f, 60, SkillBehaviorType.Instant);
            c += S(p, "LastStand", "최후의 저항", "HP가 1이 되어도 죽지 않는다", SkillCategory.CommonBuff,
                DamageType.Physical, 0, 40, 45, 0, 7, 0, Race.Human, 5500,
                StatusType.Shield, 5, 1.0f, 999, SkillBehaviorType.Instant);

            return c;
        }

        // ==================== 디버프 스킬 15개 ====================
        private static int GenerateDebuffSkills()
        {
            int c = 0;
            string p = basePath + "/Debuff";

            c += S(p, "Curse", "저주", "적의 모든 스탯을 감소시킨다", SkillCategory.CommonDebuff,
                DamageType.Dark, 5, 15, 8, 4, 2, 0.5f, Race.Human, 1000,
                StatusType.Weakness, 10, 0.8f, 5, SkillBehaviorType.Projectile);
            c += S(p, "Slow", "감속", "적의 이동속도를 크게 감소시킨다", SkillCategory.CommonDebuff,
                DamageType.Ice, 5, 10, 5, 4, 1, 0.5f, Race.Human, 500,
                StatusType.Slow, 8, 0.9f, 5, SkillBehaviorType.Projectile);
            c += S(p, "Silence", "침묵", "적의 스킬 사용을 일시적으로 막는다", SkillCategory.CommonDebuff,
                DamageType.Dark, 5, 15, 8, 4, 3, 0.5f, Race.Human, 1500,
                StatusType.Root, 4, 0.7f, 3, SkillBehaviorType.Projectile);
            c += S(p, "ArmorBreak", "방어구 파괴", "적의 방어력을 크게 감소시킨다", SkillCategory.CommonDebuff,
                DamageType.Physical, 15, 12, 6, 2, 2, 0.8f, Race.Human, 1000,
                StatusType.Weakness, 8, 0.8f, 8, SkillBehaviorType.Instant);
            c += S(p, "MagicBreak", "마법 파괴", "적의 마법 방어력을 감소시킨다", SkillCategory.CommonDebuff,
                DamageType.Magical, 15, 12, 6, 2, 2, 0.8f, Race.Human, 1000,
                StatusType.Weakness, 8, 0.8f, 8, SkillBehaviorType.Instant);
            c += S(p, "PoisonBlade", "독날", "무기에 독을 발라 지속 피해를 준다", SkillCategory.CommonDebuff,
                DamageType.Poison, 10, 8, 4, 2, 2, 0.7f, Race.Human, 800,
                StatusType.Poison, 10, 0.9f, 5, SkillBehaviorType.Instant);
            c += S(p, "Fear", "공포", "적을 공포에 빠뜨려 도주시킨다", SkillCategory.CommonDebuff,
                DamageType.Dark, 5, 18, 10, 4, 4, 0.5f, Race.Human, 2000,
                StatusType.Slow, 5, 0.6f, 8, SkillBehaviorType.Instant);
            c += S(p, "Entangle", "속박", "덩굴로 적을 묶어 움직이지 못하게 한다", SkillCategory.CommonDebuff,
                DamageType.Physical, 10, 15, 8, 4, 3, 0.6f, Race.Human, 1500,
                StatusType.Root, 5, 0.8f, 4, SkillBehaviorType.Summon);
            c += S(p, "Blind", "실명", "적의 명중률을 크게 감소시킨다", SkillCategory.CommonDebuff,
                DamageType.Holy, 5, 12, 6, 3, 2, 0.5f, Race.Human, 1000,
                StatusType.Weakness, 6, 0.7f, 6, SkillBehaviorType.Instant);
            c += S(p, "MarkOfDeath", "죽음의 표식", "표적이 받는 모든 데미지 증가", SkillCategory.CommonDebuff,
                DamageType.Dark, 10, 20, 10, 4, 4, 0.5f, Race.Human, 2500,
                StatusType.Weakness, 10, 0.9f, 10, SkillBehaviorType.Projectile);
            c += S(p, "Confusion", "혼란", "적이 아군을 공격하게 만든다", SkillCategory.CommonDebuff,
                DamageType.Dark, 5, 22, 12, 3, 5, 0.5f, Race.Human, 3000,
                StatusType.Stun, 3, 0.5f, 4, SkillBehaviorType.Projectile);
            c += S(p, "Exhaust", "탈진", "적의 마나를 소진시킨다", SkillCategory.CommonDebuff,
                DamageType.Dark, 10, 15, 8, 3, 3, 0.6f, Race.Human, 1500,
                StatusType.Weakness, 8, 0.7f, 5, SkillBehaviorType.Instant);
            c += S(p, "Petrify", "석화", "적을 돌로 만들어 행동불능 상태로 만든다", SkillCategory.CommonDebuff,
                DamageType.Physical, 20, 25, 12, 3, 5, 0.8f, Race.Human, 3500,
                StatusType.Stun, 4, 0.5f, 5, SkillBehaviorType.Instant);
            c += S(p, "LifeDrain", "생명력 흡수", "적의 HP를 지속적으로 흡수한다", SkillCategory.CommonDebuff,
                DamageType.Dark, 15, 18, 8, 3, 4, 0.8f, Race.Human, 2000,
                StatusType.Poison, 8, 0.9f, 6, SkillBehaviorType.Projectile);
            c += S(p, "DoomMark", "파멸의 낙인", "시간 후 대폭발로 큰 피해", SkillCategory.CommonDebuff,
                DamageType.Dark, 50, 30, 15, 4, 6, 1.5f, Race.Human, 5000,
                StatusType.Burn, 5, 1.0f, 10, SkillBehaviorType.Projectile);

            return c;
        }

        // ==================== 헬퍼 ====================

        private static int S(string folder, string id, string name, string desc,
            SkillCategory cat, DamageType dmgType, float baseDmg, float manaCost,
            float cooldown, float range, int tier, float scaling, Race race, long gold,
            StatusType statusType, float statusDur, float statusChance, float statusValue,
            SkillBehaviorType behavior)
        {
            string path = $"{folder}/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<SkillData>(path) != null) return 0;

            var data = ScriptableObject.CreateInstance<SkillData>();

            data.skillId = $"Common_{id}";
            data.skillName = name;
            data.description = desc;
            data.category = cat;
            data.damageType = dmgType;
            data.baseDamage = baseDmg;
            data.manaCost = manaCost;
            data.cooldown = cooldown;
            data.range = range;
            data.skillTier = tier;
            data.damageScaling = scaling;
            data.requiredRace = race;
            data.goldCost = gold;
            data.requiredLevel = tier; // tier = required level
            data.skillType = SkillType.Active;
            data.behaviorType = behavior;
            data.castTime = cooldown * 0.1f;
            data.minDamagePercent = 85f;
            data.maxDamagePercent = 115f;
            data.statusDuration = statusDur;
            data.statusChance = statusChance * 100f; // 0~1 → 0~100

            if (statusType != StatusType.None)
            {
                data.statusEffects = new StatusEffect[]
                {
                    new StatusEffect
                    {
                        type = statusType,
                        value = statusValue,
                        duration = statusDur,
                        tickInterval = (statusType == StatusType.Poison || statusType == StatusType.Burn || statusType == StatusType.Regeneration) ? 1f : 0f,
                        stackable = false
                    }
                };
            }

            EditorUtility.SetDirty(data);
            AssetDatabase.CreateAsset(data, path);
            return 1;
        }

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
