using UnityEngine;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 16개 직업 데이터 - 새로운 직업 시스템
    /// </summary>
    [CreateAssetMenu(fileName = "New Job", menuName = "Dungeon Crawler/Job Data")]
    public class JobData : ScriptableObject
    {
        [Header("Basic Info")]
        public string jobName;
        public string jobNameEn;
        [TextArea(3, 5)]
        public string description;
        public Sprite jobIcon;
        
        [Header("Job Classification")]
        public JobRole jobRole = JobRole.Information;
        public JobType jobType = JobType.Navigator;
        
        [Header("Stat Growth Per Level")]
        public int strengthGrowth = 1;
        public int dexterityGrowth = 1; 
        public int intelligenceGrowth = 1;
        public int vitalityGrowth = 1;
        public int luckGrowth = 1;
        
        [Header("Primary Stats")]
        public StatType primaryStat1 = StatType.Strength;
        public StatType primaryStat2 = StatType.Dexterity;
        
        [Header("Available Races & Weapon Types")]
        public JobRequirement[] jobRequirements;
        
        [Header("Skills")]
        public JobSkillSet skillSet;
    }
    
    /// <summary>
    /// 직업 역할군 (5가지)
    /// </summary>
    public enum JobRole
    {
        Information,    // 정보
        Defense,        // 방어
        MeleeDPS,       // 근접 딜러
        RangedDPS,      // 원거리 딜러
        Support         // 지원
    }
    
    /// <summary>
    /// 16개 직업 타입
    /// </summary>
    public enum JobType
    {
        // Information (4개)
        Navigator = 1,      // 항해사
        Scout = 2,          // 정찰병
        Tracker = 3,        // 추적자
        Trapper = 4,        // 함정 전문가
        
        // Defense (2개)
        Guardian = 5,       // 수호기사
        Templar = 6,        // 성기사
        
        // Melee DPS (4개)
        Berserker = 7,      // 광전사
        Assassin = 8,       // 암살자
        Duelist = 9,        // 결투가
        ElementalBruiser = 16, // 원소 투사
        
        // Ranged DPS (3개)
        Sniper = 10,        // 저격수
        Mage = 11,          // 마법사
        Warlock = 12,       // 흑마법사
        
        // Support (3개)
        Cleric = 13,        // 성직자
        Druid = 14,         // 드루이드
        Amplifier = 15      // 증폭술사
    }
    
    /// <summary>
    /// 스탯 타입
    /// </summary>
    public enum StatType
    {
        Strength,
        Dexterity, 
        Intelligence,
        Vitality,
        Luck
    }
    
    /// <summary>
    /// 직업 선택 조건 (종족 + 무기 타입)
    /// </summary>
    [System.Serializable]
    public class JobRequirement
    {
        public Race requiredRace;
        public WeaponGroup requiredWeaponGroup;
    }
    
    /// <summary>
    /// 무기군 타입
    /// </summary>
    public enum WeaponGroup
    {
        SwordShield,    // 한손검/방패
        TwoHandedSword, // 양손 대검
        TwoHandedAxe,   // 양손 도끼
        Dagger,         // 단검
        Bow,            // 장궁/활
        Staff,          // 지팡이
        Wand,           // 마법봉
        Fist            // 격투 무기
    }
    
    /// <summary>
    /// 직업별 스킬 세트
    /// </summary>
    [System.Serializable]
    public class JobSkillSet
    {
        [Header("Level 1 Skills (Choose 1 of 3)")]
        public SkillChoice level1Skills;
        
        [Header("Level 3 Skills (Choose 1 of 3)")]
        public SkillChoice level3Skills;
        
        [Header("Level 5 Skills (Choose 1 of 3)")]
        public SkillChoice level5Skills;
        
        [Header("Level 7 Skills (Choose 1 of 3)")]
        public SkillChoice level7Skills;
        
        [Header("Level 10 Ultimate Skill")]
        public SkillData ultimateSkill;
    }
    
    /// <summary>
    /// 스킬 선택지 (A, B, C 중 1개)
    /// </summary>
    [System.Serializable]
    public class SkillChoice
    {
        public SkillData choiceA;
        public SkillData choiceB;
        public SkillData choiceC;
        
        public long goldCostA = 100;
        public long goldCostB = 100;
        public long goldCostC = 100;
        
        public string conceptA = ""; // 컨셉 설명
        public string conceptB = "";
        public string conceptC = "";
    }
}