using UnityEngine;
using UnityEditor;
using System.IO;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 직업 특성화 데이터 32개 자동 생성 (16직업 × 2경로)
    /// </summary>
    public class JobSpecializationDataGenerator : EditorWindow
    {
        [MenuItem("Dungeon Crawler/Generate Specialization Data (32)")]
        public static void Generate()
        {
            string basePath = "Assets/Resources/ScriptableObjects/Specializations";
            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            int count = 0;

            // ===== Information (4직업) =====

            // Navigator 항해사
            count += CreateSpec(basePath, "spec_navigator_a", "폭풍 항해사", JobType.Navigator, 0,
                "폭풍을 다루는 전투형 항해사. 바람과 번개로 적을 쓸어버린다.",
                new StatBlock(2, 4, 0, 3, 0, 2, 1, 0), 0, 10,
                5, 15, 8, 0, 0,
                "폭풍의 일격", SpecPassiveType.ElementalDamage, 12,
                "번개 마스터리", SpecPassiveType.SkillDamageIncrease, 10,
                "바람의 가호", SpecPassiveType.MovementSpeed, 8,
                "폭풍 소환", "주변 적에게 번개 데미지를 연쇄로 가한다.", 2.0f, 25, 60);

            count += CreateSpec(basePath, "spec_navigator_b", "탐험 항해사", JobType.Navigator, 1,
                "탐험과 발견에 특화된 항해사. 보물과 자원을 더 잘 찾는다.",
                new StatBlock(1, 3, 2, 2, 0, 0, 4, 0), 5, 5,
                3, 0, 5, 0, 0,
                "보물 사냥꾼", SpecPassiveType.DropRateIncrease, 15,
                "행운의 별", SpecPassiveType.GoldBonusPercent, 20,
                "경험의 항해", SpecPassiveType.ExpBonusPercent, 10,
                "보물 감지", "주변 숨겨진 보물과 아이템의 등급을 높인다.", 1.0f, 20, 45);

            // Scout 정찰병
            count += CreateSpec(basePath, "spec_scout_a", "암습자", JobType.Scout, 0,
                "그림자에서 적을 급습하는 암살 전문 정찰병.",
                new StatBlock(3, 5, 0, 0, 0, 0, 3, 0), 0, 0,
                10, 25, 12, 5, 3,
                "급소 공격", SpecPassiveType.CriticalImprove, 15,
                "그림자 이동", SpecPassiveType.MovementSpeed, 10,
                "암살 본능", SpecPassiveType.DamageIncrease, 8,
                "그림자 일격", "은신 후 강력한 일격을 가한다. 크리티컬 확정.", 3.0f, 35, 45);

            count += CreateSpec(basePath, "spec_scout_b", "척후병", JobType.Scout, 1,
                "정보 수집과 팀 지원에 특화된 정찰병.",
                new StatBlock(1, 4, 2, 1, 1, 0, 3, 0), 5, 5,
                5, 10, 10, 0, 0,
                "선제 정찰", SpecPassiveType.DodgeChance, 10,
                "약점 파악", SpecPassiveType.DamageIncrease, 6,
                "위기 감지", SpecPassiveType.DamageReduction, 8,
                "취약점 노출", "적의 방어력을 30% 감소시킨다 (10초).", 1.5f, 25, 40);

            // Tracker 추적자
            count += CreateSpec(basePath, "spec_tracker_a", "사냥꾼", JobType.Tracker, 0,
                "대형 몬스터 사냥에 특화된 추적자.",
                new StatBlock(4, 3, 2, 0, 0, 0, 2, 1), 0, 0,
                7, 20, 10, 0, 2,
                "큰 사냥감", SpecPassiveType.DamageIncrease, 12,
                "추적 본능", SpecPassiveType.CriticalImprove, 10,
                "사냥의 쾌감", SpecPassiveType.ExpBonusPercent, 8,
                "맹수의 올가미", "적을 속박하고 연속 공격한다.", 2.5f, 30, 50);

            count += CreateSpec(basePath, "spec_tracker_b", "조련사", JobType.Tracker, 1,
                "야수를 길들이고 함께 전투하는 추적자.",
                new StatBlock(2, 2, 3, 1, 1, 0, 2, 1), 5, 5,
                3, 10, 5, 0, 0,
                "야수 유대", SpecPassiveType.SummonStrength, 20,
                "야생의 지혜", SpecPassiveType.HealingIncrease, 10,
                "팩 리더", SpecPassiveType.DamageIncrease, 6,
                "야수 소환", "강력한 야수를 소환하여 함께 싸운다.", 2.0f, 40, 60);

            // Trapper 함정 전문가
            count += CreateSpec(basePath, "spec_trapper_a", "폭파 전문가", JobType.Trapper, 0,
                "폭발물과 광역 함정에 특화된 함정사.",
                new StatBlock(2, 3, 1, 3, 0, 0, 2, 1), 0, 5,
                5, 15, 5, 3, 0,
                "폭발 강화", SpecPassiveType.AoEDamageIncrease, 20,
                "연쇄 폭발", SpecPassiveType.SkillDamageIncrease, 10,
                "잔해 효과", SpecPassiveType.DotDamageIncrease, 8,
                "대폭발", "거대 폭발로 광역 데미지를 입힌다.", 3.0f, 35, 55);

            count += CreateSpec(basePath, "spec_trapper_b", "제어 전문가", JobType.Trapper, 1,
                "CC와 상태이상 함정에 특화된 함정사.",
                new StatBlock(1, 4, 2, 2, 1, 0, 2, 0), 5, 10,
                3, 10, 8, 5, 0,
                "함정 마스터", SpecPassiveType.CooldownReduction, 15,
                "독성 강화", SpecPassiveType.DotDamageIncrease, 15,
                "둔화 효과", SpecPassiveType.ManaReduction, 10,
                "속박의 그물", "넓은 범위에 속박 함정을 설치한다.", 1.5f, 30, 50);

            // ===== Defense (2직업) =====

            // Guardian 수호기사
            count += CreateSpec(basePath, "spec_guardian_a", "철벽 수호자", JobType.Guardian, 0,
                "극한의 방어력으로 아군을 보호하는 수호기사.",
                new StatBlock(2, 0, 6, 0, 5, 3, 0, 2), 15, 0,
                0, 0, 0, 0, 2,
                "철벽 방어", SpecPassiveType.DamageReduction, 15,
                "방패 마스터", SpecPassiveType.BlockChance, 12,
                "수호 서약", SpecPassiveType.HealingIncrease, 8,
                "절대 방어", "10초간 받는 데미지를 80% 감소시킨다.", 1.0f, 40, 90);

            count += CreateSpec(basePath, "spec_guardian_b", "복수의 기사", JobType.Guardian, 1,
                "맞으면서 강해지는 반격 특화 수호기사.",
                new StatBlock(4, 1, 4, 0, 3, 1, 0, 1), 10, 0,
                5, 20, 5, 0, 3,
                "복수의 일격", SpecPassiveType.CounterAttackChance, 15,
                "분노의 방패", SpecPassiveType.DamageIncrease, 10,
                "인내의 대가", SpecPassiveType.CriticalImprove, 8,
                "복수의 폭풍", "받은 데미지의 200%를 주변 적에게 반사한다.", 2.5f, 30, 60);

            // Templar 성기사
            count += CreateSpec(basePath, "spec_templar_a", "심판자", JobType.Templar, 0,
                "신성한 힘으로 악을 심판하는 성기사.",
                new StatBlock(4, 1, 3, 2, 2, 2, 0, 0), 5, 5,
                5, 15, 5, 0, 2,
                "신성 심판", SpecPassiveType.ElementalDamage, 15,
                "정의의 일격", SpecPassiveType.SkillDamageIncrease, 10,
                "천벌", SpecPassiveType.CriticalImprove, 8,
                "최후의 심판", "신성한 빛으로 광역 심판 데미지를 입힌다.", 3.0f, 45, 75);

            count += CreateSpec(basePath, "spec_templar_b", "성스러운 방패", JobType.Templar, 1,
                "아군을 치유하고 보호하는 서포터형 성기사.",
                new StatBlock(1, 0, 4, 3, 3, 3, 0, 0), 10, 15,
                0, 0, 0, 5, 3,
                "신성한 치유", SpecPassiveType.HealingIncrease, 20,
                "보호의 축복", SpecPassiveType.DamageReduction, 10,
                "성스러운 오라", SpecPassiveType.ManaReduction, 8,
                "신성 결계", "아군 전체에게 보호막과 치유를 부여한다.", 1.5f, 50, 80);

            // ===== Melee DPS (4직업) =====

            // Berserker 광전사
            count += CreateSpec(basePath, "spec_berserker_a", "혈전사", JobType.Berserker, 0,
                "피에 미친 전투광. HP가 낮을수록 강해진다.",
                new StatBlock(6, 2, 3, 0, 0, 0, 1, 0), 0, 0,
                8, 25, 15, 0, 5,
                "피의 광기", SpecPassiveType.DamageIncrease, 15,
                "흡혈 강타", SpecPassiveType.CriticalImprove, 12,
                "불멸의 투지", SpecPassiveType.DamageReduction, 5,
                "피의 광전", "HP를 소모하여 공격력 300% 폭풍 타격.", 3.5f, 0, 30);

            count += CreateSpec(basePath, "spec_berserker_b", "폭풍전사", JobType.Berserker, 1,
                "빠르고 연속적인 공격으로 적을 압도하는 광전사.",
                new StatBlock(4, 4, 2, 0, 0, 0, 2, 0), 0, 0,
                10, 15, 20, 3, 2,
                "폭풍 연타", SpecPassiveType.DamageIncrease, 10,
                "빠른 손", SpecPassiveType.CooldownReduction, 12,
                "연타 마스터", SpecPassiveType.ManaReduction, 8,
                "무한 연격", "3초간 공격속도 200% 증가.", 2.0f, 20, 45);

            // Assassin 암살자
            count += CreateSpec(basePath, "spec_assassin_a", "독의 대가", JobType.Assassin, 0,
                "치명적인 독으로 적을 서서히 죽이는 암살자.",
                new StatBlock(2, 5, 0, 2, 0, 0, 3, 0), 0, 5,
                8, 20, 10, 0, 2,
                "맹독 강화", SpecPassiveType.DotDamageIncrease, 20,
                "독 마스터", SpecPassiveType.SkillDamageIncrease, 10,
                "치명적 독", SpecPassiveType.CriticalImprove, 8,
                "독 폭발", "적에게 걸린 독을 모두 폭발시켜 즉발 데미지.", 3.0f, 30, 40);

            count += CreateSpec(basePath, "spec_assassin_b", "그림자 칼날", JobType.Assassin, 1,
                "순간적인 폭발 데미지에 특화된 암살자.",
                new StatBlock(3, 5, 0, 0, 0, 0, 4, 0), 0, 0,
                15, 30, 12, 0, 0,
                "급소 파악", SpecPassiveType.CriticalImprove, 20,
                "그림자 강타", SpecPassiveType.DamageIncrease, 12,
                "은신 마스터", SpecPassiveType.DodgeChance, 8,
                "사신의 일격", "확정 크리티컬 + 300% 데미지 1회 공격.", 4.0f, 35, 60);

            // Duelist 결투가
            count += CreateSpec(basePath, "spec_duelist_a", "검술 달인", JobType.Duelist, 0,
                "완벽한 검술로 적의 공격을 흘리고 반격하는 결투가.",
                new StatBlock(3, 4, 1, 0, 2, 0, 2, 2), 0, 0,
                7, 18, 10, 3, 2,
                "완벽한 패리", SpecPassiveType.CounterAttackChance, 18,
                "정밀 검술", SpecPassiveType.CriticalImprove, 12,
                "반격 강화", SpecPassiveType.DamageIncrease, 8,
                "만검귀류: 극", "연속 10회 정밀 타격.", 3.5f, 35, 50);

            count += CreateSpec(basePath, "spec_duelist_b", "쌍검사", JobType.Duelist, 1,
                "두 자루 검으로 빠르게 공격하는 결투가.",
                new StatBlock(4, 5, 0, 0, 0, 0, 3, 0), 0, 0,
                12, 15, 18, 2, 3,
                "이도류", SpecPassiveType.DamageIncrease, 12,
                "질풍 검무", SpecPassiveType.MovementSpeed, 10,
                "칼날 폭풍", SpecPassiveType.AoEDamageIncrease, 8,
                "쌍검 폭풍", "주변 모든 적을 쌍검으로 난도질.", 2.5f, 30, 40);

            // ElementalBruiser 원소 투사
            count += CreateSpec(basePath, "spec_elementalbruiser_a", "화염 투사", JobType.ElementalBruiser, 0,
                "화염의 힘을 주먹에 담는 원소 투사.",
                new StatBlock(4, 2, 2, 3, 0, 1, 0, 0), 0, 5,
                5, 20, 8, 0, 2,
                "화염 마스터", SpecPassiveType.ElementalDamage, 18,
                "불꽃 주먹", SpecPassiveType.DamageIncrease, 10,
                "용암 저항", SpecPassiveType.DamageReduction, 5,
                "불꽃 용권", "화염을 두른 상승 주먹 공격.", 3.0f, 35, 45);

            count += CreateSpec(basePath, "spec_elementalbruiser_b", "뇌전 투사", JobType.ElementalBruiser, 1,
                "번개의 속도와 힘을 가진 원소 투사.",
                new StatBlock(3, 4, 1, 3, 0, 0, 1, 0), 0, 5,
                8, 15, 15, 3, 0,
                "뇌전 강화", SpecPassiveType.ElementalDamage, 15,
                "번개 속도", SpecPassiveType.MovementSpeed, 12,
                "감전 효과", SpecPassiveType.DotDamageIncrease, 10,
                "뇌신의 일격", "번개를 모아 단일 대상에게 극대 데미지.", 3.5f, 30, 50);

            // ===== Ranged DPS (3직업) =====

            // Sniper 저격수
            count += CreateSpec(basePath, "spec_sniper_a", "정밀 저격수", JobType.Sniper, 0,
                "한 발의 총알에 모든 것을 거는 저격 전문가.",
                new StatBlock(2, 5, 0, 0, 0, 0, 4, 3), 0, 0,
                15, 35, 0, 5, 0,
                "저격 마스터", SpecPassiveType.CriticalImprove, 20,
                "정밀 조준", SpecPassiveType.DamageIncrease, 15,
                "약점 사격", SpecPassiveType.SkillDamageIncrease, 10,
                "치명 저격", "3초 조준 후 800% 데미지 단발.", 5.0f, 40, 90);

            count += CreateSpec(basePath, "spec_sniper_b", "속사수", JobType.Sniper, 1,
                "빠른 연사로 적을 제압하는 저격수.",
                new StatBlock(3, 5, 1, 0, 0, 0, 2, 1), 0, 0,
                8, 12, 20, 3, 1,
                "속사", SpecPassiveType.DamageIncrease, 10,
                "탄창 관리", SpecPassiveType.CooldownReduction, 12,
                "관통탄", SpecPassiveType.AoEDamageIncrease, 8,
                "탄막 사격", "3초간 전방에 탄막을 쏟아붓는다.", 2.0f, 25, 40);

            // Mage 마법사
            count += CreateSpec(basePath, "spec_mage_a", "원소 대마법사", JobType.Mage, 0,
                "원소 마법의 극한을 추구하는 대마법사.",
                new StatBlock(0, 1, 0, 7, 0, 3, 1, 0), 0, 15,
                3, 20, 0, 5, 0,
                "원소 마스터", SpecPassiveType.ElementalDamage, 20,
                "마력 증폭", SpecPassiveType.SkillDamageIncrease, 15,
                "원소 친화", SpecPassiveType.ManaReduction, 10,
                "아마겟돈", "하늘에서 원소 폭풍을 내린다.", 4.0f, 60, 90);

            count += CreateSpec(basePath, "spec_mage_b", "시간의 마법사", JobType.Mage, 1,
                "시간을 조작하여 전투를 유리하게 이끄는 마법사.",
                new StatBlock(0, 3, 2, 5, 0, 2, 0, 0), 5, 10,
                5, 10, 10, 10, 0,
                "시간 왜곡", SpecPassiveType.CooldownReduction, 20,
                "가속", SpecPassiveType.MovementSpeed, 12,
                "되감기", SpecPassiveType.HealingIncrease, 10,
                "시간 정지", "적 전체를 3초간 정지시킨다.", 1.0f, 50, 120);

            // Warlock 흑마법사
            count += CreateSpec(basePath, "spec_warlock_a", "저주술사", JobType.Warlock, 0,
                "저주와 디버프로 적을 무력화하는 흑마법사.",
                new StatBlock(0, 2, 1, 6, 0, 2, 1, 0), 0, 10,
                5, 15, 5, 3, 2,
                "저주 강화", SpecPassiveType.DotDamageIncrease, 20,
                "생명력 흡수", SpecPassiveType.HealingIncrease, 10,
                "저주 전파", SpecPassiveType.AoEDamageIncrease, 10,
                "만능 저주", "적 전체에게 모든 스탯 30% 감소 저주.", 2.0f, 45, 70);

            count += CreateSpec(basePath, "spec_warlock_b", "소환술사", JobType.Warlock, 1,
                "악마를 소환하여 대리전을 벌이는 흑마법사.",
                new StatBlock(0, 1, 3, 5, 0, 1, 2, 0), 5, 10,
                3, 10, 3, 0, 0,
                "소환 마스터", SpecPassiveType.SummonStrength, 25,
                "악마 계약", SpecPassiveType.DamageIncrease, 8,
                "영혼 수확", SpecPassiveType.ExpBonusPercent, 10,
                "악마왕 소환", "강력한 악마를 소환하여 30초간 전투.", 2.5f, 60, 120);

            // ===== Support (3직업) =====

            // Cleric 성직자
            count += CreateSpec(basePath, "spec_cleric_a", "치유사제", JobType.Cleric, 0,
                "치유에 특화된 최고의 힐러.",
                new StatBlock(0, 1, 3, 5, 1, 3, 0, 0), 10, 20,
                0, 0, 0, 5, 0,
                "치유 마스터", SpecPassiveType.HealingIncrease, 25,
                "마나 순환", SpecPassiveType.ManaReduction, 15,
                "축복의 손길", SpecPassiveType.CooldownReduction, 8,
                "대치유", "아군 전체 HP를 50% 회복한다.", 1.5f, 60, 90);

            count += CreateSpec(basePath, "spec_cleric_b", "심판 사제", JobType.Cleric, 1,
                "신성한 힘으로 직접 전투하는 전투형 사제.",
                new StatBlock(3, 1, 2, 4, 1, 2, 0, 0), 5, 10,
                5, 15, 5, 0, 2,
                "신성 데미지", SpecPassiveType.ElementalDamage, 15,
                "신성한 분노", SpecPassiveType.DamageIncrease, 10,
                "응보", SpecPassiveType.CounterAttackChance, 8,
                "신성 폭풍", "신성한 빛으로 광역 데미지 + 아군 치유.", 2.5f, 40, 60);

            // Druid 드루이드
            count += CreateSpec(basePath, "spec_druid_a", "야수 변신술사", JobType.Druid, 0,
                "강력한 야수로 변신하여 싸우는 드루이드.",
                new StatBlock(5, 3, 4, 0, 2, 0, 0, 0), 10, 0,
                5, 15, 10, 0, 3,
                "변신 마스터", SpecPassiveType.DamageIncrease, 15,
                "야수의 힘", SpecPassiveType.CriticalImprove, 10,
                "자연 회복", SpecPassiveType.HealingIncrease, 8,
                "궁극 변신", "30초간 전설의 야수로 변신. 모든 스탯 50% 증가.", 2.0f, 50, 120);

            count += CreateSpec(basePath, "spec_druid_b", "자연의 수호자", JobType.Druid, 1,
                "자연의 힘으로 아군을 치유하고 보호하는 드루이드.",
                new StatBlock(1, 1, 3, 4, 1, 2, 0, 0), 10, 15,
                0, 0, 3, 5, 0,
                "자연 치유", SpecPassiveType.HealingIncrease, 20,
                "대지의 축복", SpecPassiveType.DamageReduction, 10,
                "생명의 순환", SpecPassiveType.ManaReduction, 12,
                "세계수의 축복", "아군 전체에게 재생 버프 + 보호막.", 1.5f, 55, 80);

            // Amplifier 증폭술사
            count += CreateSpec(basePath, "spec_amplifier_a", "파괴 증폭술사", JobType.Amplifier, 0,
                "아군의 공격력을 극한으로 증폭하는 증폭술사.",
                new StatBlock(2, 2, 1, 5, 0, 1, 1, 0), 0, 10,
                5, 15, 8, 5, 0,
                "공격 증폭", SpecPassiveType.DamageIncrease, 12,
                "크리티컬 증폭", SpecPassiveType.CriticalImprove, 10,
                "스킬 증폭", SpecPassiveType.SkillDamageIncrease, 10,
                "궁극 증폭", "10초간 아군 전체 데미지 50% 증가.", 1.5f, 50, 90);

            count += CreateSpec(basePath, "spec_amplifier_b", "방어 증폭술사", JobType.Amplifier, 1,
                "아군의 방어력과 생존을 증폭하는 증폭술사.",
                new StatBlock(0, 1, 3, 4, 2, 2, 0, 0), 10, 15,
                0, 0, 5, 8, 0,
                "방어 증폭", SpecPassiveType.DamageReduction, 12,
                "회복 증폭", SpecPassiveType.HealingIncrease, 15,
                "마나 증폭", SpecPassiveType.ManaReduction, 12,
                "궁극 보호", "10초간 아군 전체 받는 데미지 40% 감소.", 1.0f, 50, 90);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SpecializationGenerator] {count}개 특성화 에셋 생성 완료!");
            EditorUtility.DisplayDialog("완료", $"{count}개 특성화가 생성되었습니다.", "OK");
        }

        private static int CreateSpec(string basePath, string id, string name, JobType parentJob, int specIndex,
            string desc, StatBlock statBonus, float hpBonus, float mpBonus,
            float critRate, float critDmg, float atkSpd, float cdr, float lifesteal,
            string p1Name, SpecPassiveType p1Type, float p1Value,
            string p2Name, SpecPassiveType p2Type, float p2Value,
            string p3Name, SpecPassiveType p3Type, float p3Value,
            string skillName, string skillDesc, float skillDmgMult, int skillMana, float skillCooldown)
        {
            string path = $"{basePath}/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<JobSpecializationData>(path) != null)
                return 0;

            var asset = ScriptableObject.CreateInstance<JobSpecializationData>();
            var so = new SerializedObject(asset);

            so.FindProperty("specId").stringValue = id;
            so.FindProperty("specName").stringValue = name;
            so.FindProperty("description").stringValue = desc;
            so.FindProperty("parentJob").enumValueIndex = GetJobEnumIndex(parentJob);
            so.FindProperty("specIndex").intValue = specIndex;
            so.FindProperty("requiredLevel").intValue = 10;
            so.FindProperty("resetGoldCost").intValue = 50000;

            // Stat bonus
            var statProp = so.FindProperty("statBonus");
            statProp.FindPropertyRelative("strength").floatValue = statBonus.strength;
            statProp.FindPropertyRelative("agility").floatValue = statBonus.agility;
            statProp.FindPropertyRelative("vitality").floatValue = statBonus.vitality;
            statProp.FindPropertyRelative("intelligence").floatValue = statBonus.intelligence;
            statProp.FindPropertyRelative("defense").floatValue = statBonus.defense;
            statProp.FindPropertyRelative("magicDefense").floatValue = statBonus.magicDefense;
            statProp.FindPropertyRelative("luck").floatValue = statBonus.luck;
            statProp.FindPropertyRelative("stability").floatValue = statBonus.stability;

            so.FindProperty("hpBonusPercent").floatValue = hpBonus;
            so.FindProperty("mpBonusPercent").floatValue = mpBonus;
            so.FindProperty("critRateBonusPercent").floatValue = critRate;
            so.FindProperty("critDamageBonusPercent").floatValue = critDmg;
            so.FindProperty("attackSpeedBonusPercent").floatValue = atkSpd;
            so.FindProperty("cooldownReductionPercent").floatValue = cdr;
            so.FindProperty("lifestealPercent").floatValue = lifesteal;

            // Passives
            SetPassive(so.FindProperty("passive1"), p1Name, p1Type, p1Value);
            SetPassive(so.FindProperty("passive2"), p2Name, p2Type, p2Value);
            SetPassive(so.FindProperty("passive3"), p3Name, p3Type, p3Value);

            // Spec skill
            so.FindProperty("specSkillName").stringValue = skillName;
            so.FindProperty("specSkillDescription").stringValue = skillDesc;
            so.FindProperty("specSkillDamageMultiplier").floatValue = skillDmgMult;
            so.FindProperty("specSkillCooldown").floatValue = skillCooldown;
            so.FindProperty("specSkillManaCost").intValue = skillMana;

            so.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(asset, path);
            return 1;
        }

        private static void SetPassive(SerializedProperty prop, string name, SpecPassiveType type, float value)
        {
            prop.FindPropertyRelative("passiveName").stringValue = name;
            prop.FindPropertyRelative("passiveDescription").stringValue = $"{name}: {value}%";
            prop.FindPropertyRelative("passiveType").enumValueIndex = (int)type;
            prop.FindPropertyRelative("value").floatValue = value;
        }

        private static int GetJobEnumIndex(JobType jobType)
        {
            var values = System.Enum.GetValues(typeof(JobType));
            for (int i = 0; i < values.Length; i++)
            {
                if ((JobType)values.GetValue(i) == jobType)
                    return i;
            }
            return 0;
        }
    }
}
