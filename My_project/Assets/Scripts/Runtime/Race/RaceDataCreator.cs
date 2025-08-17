using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 4종족 데이터를 생성하는 헬퍼 클래스
    /// 에디터에서 미리 정의된 종족 데이터를 생성
    /// </summary>
    public static class RaceDataCreator
    {
        /// <summary>
        /// 인간 종족 데이터 생성
        /// 균형형 - 모든 스탯 10 시작, 레벨당 모든 스탯 +1
        /// </summary>
        public static RaceData CreateHumanRaceData()
        {
            var humanData = ScriptableObject.CreateInstance<RaceData>();
            
            // 기본 정보
            humanData.raceType = Race.Human;
            humanData.raceName = "인간 (Human)";
            humanData.description = "균형잡힌 만능형 종족. 모든 계열의 스킬을 학습할 수 있으며, 안정적인 성장이 특징입니다.";
            
            // 기본 스탯 (레벨 1)
            var baseStats = new StatBlock(10f, 10f, 10f, 10f, 10f, 10f, 10f);
            typeof(RaceData).GetField("baseStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(humanData, baseStats);
            
            // 성장 스탯 (레벨당)
            var statGrowth = new StatGrowth(1f, 1f, 1f, 1f, 1f, 1f, 1f);
            typeof(RaceData).GetField("statGrowth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(humanData, statGrowth);
            
            // 속성 친화도 (중립)
            var elementalAffinity = new ElementalStats();
            typeof(RaceData).GetField("elementalAffinity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(humanData, elementalAffinity);
            
            // 종족 특성
            var specialties = new RaceSpecialty[]
            {
                new RaceSpecialty { specialtyType = RaceSpecialtyType.ExpBonus, value = 0.1f, description = "경험치 10% 추가 획득" }
            };
            typeof(RaceData).GetField("specialties", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(humanData, specialties);
            
            return humanData;
        }
        
        /// <summary>
        /// 엘프 종족 데이터 생성
        /// 마법 특화형
        /// </summary>
        public static RaceData CreateElfRaceData()
        {
            var elfData = ScriptableObject.CreateInstance<RaceData>();
            
            // 기본 정보
            elfData.raceType = Race.Elf;
            elfData.raceName = "엘프 (Elf)";
            elfData.description = "마법에 특화된 종족. 높은 지능과 마법방어를 가지고 있으며, 원소마법에 뛰어납니다.";
            
            // 기본 스탯 (레벨 1) - STR 8/AGI 12/VIT 8/INT 15/DEF 7/MDEF 12/LUK 10
            var baseStats = new StatBlock(8f, 12f, 8f, 15f, 7f, 12f, 10f);
            typeof(RaceData).GetField("baseStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(elfData, baseStats);
            
            // 성장 스탯 - STR +0.5/AGI +1/VIT +0.5/INT +2/DEF +0.5/MDEF +1.5/LUK +1
            var statGrowth = new StatGrowth(0.5f, 1f, 0.5f, 2f, 0.5f, 1.5f, 1f);
            typeof(RaceData).GetField("statGrowth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(elfData, statGrowth);
            
            // 속성 친화도 (마법 친화적)
            var elementalAffinity = new ElementalStats
            {
                fireAttack = 0.1f, fireResist = 0.05f,
                iceAttack = 0.1f, iceResist = 0.05f,
                lightningAttack = 0.1f, lightningResist = 0.05f,
                holyAttack = 0.15f, holyResist = 0.1f
            };
            typeof(RaceData).GetField("elementalAffinity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(elfData, elementalAffinity);
            
            // 종족 특성
            var specialties = new RaceSpecialty[]
            {
                new RaceSpecialty { specialtyType = RaceSpecialtyType.MagicMastery, value = 0.2f, description = "마법 데미지 20% 증가" },
                new RaceSpecialty { specialtyType = RaceSpecialtyType.ElementalResistance, value = 0.1f, description = "모든 속성 저항 10%" }
            };
            typeof(RaceData).GetField("specialties", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(elfData, specialties);
            
            return elfData;
        }
        
        /// <summary>
        /// 수인 종족 데이터 생성
        /// 물리 특화형
        /// </summary>
        public static RaceData CreateBeastRaceData()
        {
            var beastData = ScriptableObject.CreateInstance<RaceData>();
            
            // 기본 정보
            beastData.raceType = Race.Beast;
            beastData.raceName = "수인 (Beast)";
            beastData.description = "물리 전투에 특화된 종족. 강력한 근접전투 능력과 높은 기동력을 자랑합니다.";
            
            // 기본 스탯 (레벨 1) - STR 15/AGI 13/VIT 12/INT 6/DEF 10/MDEF 6/LUK 8
            var baseStats = new StatBlock(15f, 13f, 12f, 6f, 10f, 6f, 8f);
            typeof(RaceData).GetField("baseStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(beastData, baseStats);
            
            // 성장 스탯 - STR +2/AGI +1.5/VIT +1.5/INT +0.5/DEF +1/MDEF +0.5/LUK +0.5
            var statGrowth = new StatGrowth(2f, 1.5f, 1.5f, 0.5f, 1f, 0.5f, 0.5f);
            typeof(RaceData).GetField("statGrowth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(beastData, statGrowth);
            
            // 속성 친화도 (자연/어둠 친화적)
            var elementalAffinity = new ElementalStats
            {
                poisonAttack = 0.1f, poisonResist = 0.1f,
                darkAttack = 0.05f, darkResist = 0.05f
            };
            typeof(RaceData).GetField("elementalAffinity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(beastData, elementalAffinity);
            
            // 종족 특성
            var specialties = new RaceSpecialty[]
            {
                new RaceSpecialty { specialtyType = RaceSpecialtyType.PhysicalMastery, value = 0.25f, description = "물리 데미지 25% 증가" },
                new RaceSpecialty { specialtyType = RaceSpecialtyType.MovementBonus, value = 0.15f, description = "이동속도 15% 증가" },
                new RaceSpecialty { specialtyType = RaceSpecialtyType.CriticalBonus, value = 0.05f, description = "치명타 확률 5% 추가" }
            };
            typeof(RaceData).GetField("specialties", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(beastData, specialties);
            
            return beastData;
        }
        
        /// <summary>
        /// 기계족 종족 데이터 생성
        /// 방어 특화형
        /// </summary>
        public static RaceData CreateMachinaRaceData()
        {
            var machinaData = ScriptableObject.CreateInstance<RaceData>();
            
            // 기본 정보
            machinaData.raceType = Race.Machina;
            machinaData.raceName = "기계족 (Machina)";
            machinaData.description = "방어에 특화된 기술 종족. 높은 체력과 방어력을 가지고 있으며, 기술 계열 스킬에 특화되어 있습니다.";
            
            // 기본 스탯 (레벨 1) - STR 10/AGI 7/VIT 15/INT 8/DEF 15/MDEF 10/LUK 5
            var baseStats = new StatBlock(10f, 7f, 15f, 8f, 15f, 10f, 5f);
            typeof(RaceData).GetField("baseStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(machinaData, baseStats);
            
            // 성장 스탯 - STR +1/AGI +0.5/VIT +2/INT +1/DEF +2/MDEF +1.5/LUK +0.5
            var statGrowth = new StatGrowth(1f, 0.5f, 2f, 1f, 2f, 1.5f, 0.5f);
            typeof(RaceData).GetField("statGrowth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(machinaData, statGrowth);
            
            // 속성 친화도 (번개/신성 친화적)
            var elementalAffinity = new ElementalStats
            {
                lightningAttack = 0.15f, lightningResist = 0.15f,
                holyAttack = 0.1f, holyResist = 0.1f,
                fireResist = 0.05f, iceResist = 0.05f
            };
            typeof(RaceData).GetField("elementalAffinity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(machinaData, elementalAffinity);
            
            // 종족 특성
            var specialties = new RaceSpecialty[]
            {
                new RaceSpecialty { specialtyType = RaceSpecialtyType.TechnicalMastery, value = 0.3f, description = "기술 스킬 효과 30% 증가" },
                new RaceSpecialty { specialtyType = RaceSpecialtyType.ElementalResistance, value = 0.15f, description = "모든 속성 저항 15%" }
            };
            typeof(RaceData).GetField("specialties", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(machinaData, specialties);
            
            return machinaData;
        }
        
        /// <summary>
        /// 모든 종족 데이터를 디버깅용으로 출력
        /// </summary>
        public static void LogAllRaceData()
        {
            Debug.Log("=== All Race Data ===");
            
            var human = CreateHumanRaceData();
            human.LogRaceInfo();
            
            var elf = CreateElfRaceData();
            elf.LogRaceInfo();
            
            var beast = CreateBeastRaceData();
            beast.LogRaceInfo();
            
            var machina = CreateMachinaRaceData();
            machina.LogRaceInfo();
        }
    }
}