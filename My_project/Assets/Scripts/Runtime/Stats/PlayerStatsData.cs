using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// PlayerStatsManager에서 관리됨
    /// </summary>
    [System.Serializable]
    public class PlayerStatsData
    {
        [Header("Race Information")]
        [SerializeField] private Race characterRace = Race.Human;
        [SerializeField] private RaceData raceData;

        [Header("Current Stats (Calculated)")]
        [SerializeField] private StatBlock currentStats;

        [Header("레벨 및 경험치")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private long currentExp = 0;
        [SerializeField] private long expToNextLevel = 100;
        [SerializeField] private int maxLevel = 15; // 최대 레벨 15

        [Header("영혼 보너스 스탯")]
        [SerializeField] private StatBlock soulBonusStats;

        [Header("장비 보너스 스탯")]
        [SerializeField] private StatBlock equipmentBonusStats;

        [Header("인챈트 보너스 스탯")]
        [SerializeField] private StatBlock enchantBonusStats;

        [Header("계산된 능력치")]
        [SerializeField] private float maxHP = 100f;
        [SerializeField] private float currentHP = 100f;
        [SerializeField] private float maxMP = 50f;
        [SerializeField] private float currentMP = 50f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float magicDamage = 5f;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float attackSpeed = 1f;
        [SerializeField] private float criticalChance = 0.05f;
        [SerializeField] private float criticalDamage = 1.5f;

        [Header("재화")]
        [SerializeField] private long gold = 0;

        [Header("전투 스탯 (민댐/맥댐 시스템)")]
        [SerializeField] private CombatStats combatStats;
        [SerializeField] private ItemInstance equippedWeaponItem;

        // 캐릭터 이름
        [SerializeField] private string characterName = "Unknown";

        // 장착된 영혼 ID 목록 (영혼 시스템용)
        [SerializeField] private List<ulong> equippedSoulIds = new List<ulong>();

        // 이벤트
        public static event Action<PlayerStatsData> OnStatsChanged;
        public static event Action<int> OnLevelUp;
        public static event Action<float, float> OnHPChanged;
        public static event Action<float, float> OnMPChanged;

        // 프로퍼티들
        public Race CharacterRace => characterRace;
        public RaceData RaceData => raceData;
        public StatBlock CurrentStats => currentStats;

        // 총 스탯 (종족 + 영혼 + 장비 + 인챈트)
        public float TotalSTR => currentStats.strength + soulBonusStats.strength + equipmentBonusStats.strength + enchantBonusStats.strength;
        public float TotalAGI => currentStats.agility + soulBonusStats.agility + equipmentBonusStats.agility + enchantBonusStats.agility;
        public float TotalVIT => currentStats.vitality + soulBonusStats.vitality + equipmentBonusStats.vitality + enchantBonusStats.vitality;
        public float TotalINT => currentStats.intelligence + soulBonusStats.intelligence + equipmentBonusStats.intelligence + enchantBonusStats.intelligence;
        public float TotalDEF => currentStats.defense + soulBonusStats.defense + equipmentBonusStats.defense + enchantBonusStats.defense;
        public float TotalMDEF => currentStats.magicDefense + soulBonusStats.magicDefense + equipmentBonusStats.magicDefense + enchantBonusStats.magicDefense;
        public float TotalLUK => currentStats.luck + soulBonusStats.luck + equipmentBonusStats.luck + enchantBonusStats.luck;
        public float TotalSTAB => currentStats.stability + soulBonusStats.stability + equipmentBonusStats.stability + enchantBonusStats.stability;

        public int CurrentLevel => currentLevel;
        public long CurrentExperience => currentExp;
        public long ExpToNextLevel => expToNextLevel;

        public float MaxHP => maxHP;
        public float CurrentHP => currentHP;
        public float MaxMP => maxMP;
        public float CurrentMP => currentMP;
        public float AttackDamage => attackDamage;
        public float MagicDamage => magicDamage;
        public float MoveSpeed => moveSpeed;
        public float AttackSpeed => attackSpeed;
        public float CriticalChance => criticalChance;
        public float CriticalDamage => criticalDamage;
        public CombatStats CombatStats => combatStats;
        public ItemInstance EquippedWeapon => equippedWeaponItem;
        public long Gold => gold;
        public long CurrentGold => gold;

        public string CharacterName => characterName;
        public List<ulong> EquippedSoulIds => equippedSoulIds;

        // 생성자
        public PlayerStatsData()
        {
            Initialize();
        }

        // 종족 설정 (캐릭터 생성 시에만)
        public void SetRace(Race race, RaceData data)
        {
            characterRace = race;
            raceData = data;

            currentStats = raceData.CalculateStatsAtLevel(currentLevel);
            RecalculateStats();
            OnStatsChanged?.Invoke(this);
        }

        // 캐릭터 이름 설정
        public void SetCharacterName(string name)
        {
            characterName = !string.IsNullOrEmpty(name) ? name.Replace("(Clone)", "") : "Unknown";
        }

        // 영혼 보너스 스탯 추가
        public void AddSoulBonusStats(StatBlock bonusStats)
        {
            soulBonusStats = soulBonusStats + bonusStats;
            RecalculateStats();
            OnStatsChanged?.Invoke(this);
        }

        // 장비 보너스 스탯 설정
        public void SetEquipmentBonusStats(StatBlock bonusStats)
        {
            equipmentBonusStats = bonusStats;
            RecalculateStats();
            OnStatsChanged?.Invoke(this);
        }

        // 경험치 추가
        public void AddExperience(long amount)
        {
            if (currentLevel >= maxLevel)
            {
                Debug.Log("Max level reached! No more experience can be gained.");
                return;
            }

            currentExp += amount;

            while (currentExp >= expToNextLevel && currentLevel < maxLevel)
            {
                LevelUp();
            }
        }

        // 레벨업 (종족별 고정 성장)
        private void LevelUp()
        {
            currentLevel++;
            currentExp -= expToNextLevel;

            // 종족별 스탯 자동 성장
            currentStats = raceData.CalculateStatsAtLevel(currentLevel);
            
            // 스탯 재계산 (체력/마나는 현재 상태 유지)
            RecalculateStats();

            // 다음 레벨 경험치 계산
            expToNextLevel = CalculateExpForLevel(currentLevel + 1);

            OnLevelUp?.Invoke(currentLevel);
            OnStatsChanged?.Invoke(this);

            Debug.Log($"Level Up! Now Level {currentLevel} - Race: {characterRace}");
        }

        // 레벨별 필요 경험치 계산
        private long CalculateExpForLevel(int level)
        {
            return (long)(100 * Mathf.Pow(level, 1.5f));
        }

        // 능력치 재계산
        public void RecalculateStats()
        {
            // HP = 100 + (VIT * 10)
            maxHP = 100f + (TotalVIT * 10f);

            // MP = 50 + (INT * 5)
            maxMP = 50f + (TotalINT * 5f);

            // 물리 공격력 = STR * 2
            attackDamage = TotalSTR * 2f;

            // 마법 공격력 = INT * 2
            magicDamage = TotalINT * 2f;

            // 이동속도 = 5.0 + (AGI * 0.1)
            moveSpeed = 5.0f + (TotalAGI * 0.1f);

            // 공격속도 = 1.0 + (AGI * 0.01)
            attackSpeed = 1.0f + (TotalAGI * 0.01f);

            // 크리티컬 확률 = LUK * 0.05%
            criticalChance = TotalLUK * 0.0005f;

            // 크리티컬 데미지 = 200% (고정)
            criticalDamage = 2.0f;

            // HP/MP가 최대치를 초과하지 않도록 제한
            currentHP = Mathf.Min(currentHP, maxHP);
            currentMP = Mathf.Min(currentMP, maxMP);

            // 새로운 전투 스탯 계산
            RecalculateCombatStats();
        }

        // 전투 스탯 계산
        private void RecalculateCombatStats()
        {
            // 물리 데미지 범위 계산
            DamageRange physicalRange;
            if (equippedWeaponItem != null && equippedWeaponItem.ItemData != null && equippedWeaponItem.ItemData.IsWeapon)
            {
                physicalRange = equippedWeaponItem.ItemData.CalculateWeaponDamage(TotalSTR, TotalSTAB);
                Debug.Log($"⚔️ Using equipped weapon: {equippedWeaponItem.ItemData.ItemName} (Damage: {physicalRange.minDamage:F1}-{physicalRange.maxDamage:F1})");
            }
            else
            {
                // 맨손 공격 시 기본 데미지
                float baseMin = TotalSTR * 1.5f;
                float baseMax = TotalSTR * 2.5f;
                physicalRange = new DamageRange(baseMin, baseMax, 0f).GetStabilizedRange(TotalSTAB);
                Debug.Log($"👊 Using bare hands (Damage: {physicalRange.minDamage:F1}-{physicalRange.maxDamage:F1})");
            }

            // 마법 데미지 범위 계산
            DamageRange magicalRange;
            if (equippedWeaponItem != null && equippedWeaponItem.ItemData != null && 
                (equippedWeaponItem.ItemData.WeaponCategory == WeaponCategory.Staff || equippedWeaponItem.ItemData.WeaponCategory == WeaponCategory.Wand))
            {
                magicalRange = equippedWeaponItem.ItemData.CalculateWeaponDamage(TotalINT, TotalSTAB);
            }
            else
            {
                // 기본 마법 데미지
                float baseMin = TotalINT * 1.5f;
                float baseMax = TotalINT * 2.5f;
                magicalRange = new DamageRange(baseMin, baseMax, 0f).GetStabilizedRange(TotalSTAB);
            }

            // 치명타 계산
            float totalCritChance = TotalLUK * 0.0005f;
            if (equippedWeaponItem != null && equippedWeaponItem.ItemData != null)
            {
                totalCritChance += equippedWeaponItem.ItemData.CriticalBonus;
            }

            // 전투 스탯 업데이트
            combatStats = new CombatStats(
                physicalRange,
                magicalRange,
                totalCritChance,
                2.0f,
                TotalSTAB
            );

            // 기존 시스템과의 호환성을 위한 평균 데미지 계산
            attackDamage = (physicalRange.minDamage + physicalRange.maxDamage) * 0.5f;
            magicDamage = (magicalRange.minDamage + magicalRange.maxDamage) * 0.5f;
            criticalChance = totalCritChance;
        }

        // HP 변경
        public void ChangeHP(float amount)
        {
            float oldHP = currentHP;
            currentHP = Mathf.Clamp(currentHP + amount, 0f, maxHP);

            if (oldHP != currentHP)
            {
                OnHPChanged?.Invoke(currentHP, maxHP);
            }
        }

        // MP 변경
        public void ChangeMP(float amount)
        {
            float oldMP = currentMP;
            currentMP = Mathf.Clamp(currentMP + amount, 0f, maxMP);

            if (oldMP != currentMP)
            {
                OnMPChanged?.Invoke(currentMP, maxMP);
            }
        }

        // HP/MP 직접 설정
        public void SetCurrentHP(float value)
        {
            float oldHP = currentHP;
            currentHP = Mathf.Clamp(value, 0f, maxHP);

            if (oldHP != currentHP)
            {
                OnHPChanged?.Invoke(currentHP, maxHP);
            }
        }

        public void SetMaxHP(float value)
        {
            maxHP = Mathf.Max(1f, value);
            currentHP = Mathf.Min(currentHP, maxHP);
        }

        public void SetCurrentMP(float value)
        {
            float oldMP = currentMP;
            currentMP = Mathf.Clamp(value, 0f, maxMP);

            if (oldMP != currentMP)
            {
                OnMPChanged?.Invoke(currentMP, maxMP);
            }
        }

        public void SetMaxMP(float value)
        {
            maxMP = Mathf.Max(1f, value);
            currentMP = Mathf.Min(currentMP, maxMP);
        }

        // 골드 변경
        public void ChangeGold(long amount)
        {
            gold = Math.Max(0L, gold + amount);
            OnStatsChanged?.Invoke(this);
        }

        // 공격 데미지 계산
        public float CalculateAttackDamage(DamageType attackType = DamageType.Physical)
        {
            float baseDamage;

            switch (attackType)
            {
                case DamageType.Physical:
                    baseDamage = combatStats.physicalDamage.GetRandomDamage();
                    break;
                case DamageType.Magical:
                    baseDamage = combatStats.magicalDamage.GetRandomDamage();
                    break;
                default:
                    baseDamage = combatStats.physicalDamage.GetRandomDamage();
                    break;
            }

            // 치명타 판정
            if (UnityEngine.Random.value < combatStats.criticalChance)
            {
                float criticalDamage = baseDamage * combatStats.criticalMultiplier;
                Debug.Log($"Critical Hit! {baseDamage:F1} → {criticalDamage:F1} damage");
                return criticalDamage;
            }

            return baseDamage;
        }

        // 스킬 데미지 계산
        public float CalculateSkillDamage(float minDamagePercent, float maxDamagePercent, DamageType skillType = DamageType.Physical)
        {
            DamageRange baseRange;

            switch (skillType)
            {
                case DamageType.Physical:
                    baseRange = combatStats.physicalDamage;
                    break;
                case DamageType.Magical:
                    baseRange = combatStats.magicalDamage;
                    break;
                default:
                    baseRange = combatStats.physicalDamage;
                    break;
            }

            // 스킬별 민댐/맥댐 배율 적용
            float skillMinDamage = baseRange.minDamage * (minDamagePercent / 100f);
            float skillMaxDamage = baseRange.maxDamage * (maxDamagePercent / 100f);

            DamageRange skillRange = new DamageRange(skillMinDamage, skillMaxDamage, baseRange.stability);
            float skillDamage = skillRange.GetRandomDamage();

            // 치명타 판정
            if (UnityEngine.Random.value < combatStats.criticalChance)
            {
                float criticalDamage = skillDamage * combatStats.criticalMultiplier;
                Debug.Log($"Skill Critical Hit! {skillDamage:F1} → {criticalDamage:F1} damage");
                return criticalDamage;
            }

            return skillDamage;
        }

        // 무기 장착 (새로운 시스템)
        public void EquipWeapon(ItemInstance weaponItem)
        {
            equippedWeaponItem = weaponItem;
            RecalculateStats();
            OnStatsChanged?.Invoke(this);
            Debug.Log($"⚔️ Equipped weapon: {weaponItem?.ItemData?.ItemName ?? "None"}");
        }

        // 무기 해제
        public void UnequipWeapon()
        {
            equippedWeaponItem = null;
            RecalculateStats();
            OnStatsChanged?.Invoke(this);
            Debug.Log($"👊 Weapon unequipped - using bare hands");
        }

        // 데미지 받기
        public float TakeDamage(float damage, DamageType damageType = DamageType.Physical)
        {
            float finalDamage = damage;

            switch (damageType)
            {
                case DamageType.Physical:
                    float physicalReduction = TotalDEF / (TotalDEF + 100f);
                    finalDamage = damage * (1f - physicalReduction);
                    break;
                case DamageType.Magical:
                    float magicalReduction = TotalMDEF / (TotalMDEF + 100f);
                    finalDamage = damage * (1f - magicalReduction);
                    break;
                case DamageType.True:
                    break;
            }

            finalDamage = Mathf.Max(1f, finalDamage);

            // 회피 확률 체크
            float dodgeChance = TotalAGI * 0.001f;
            if (UnityEngine.Random.value < dodgeChance)
            {
                Debug.Log("Attack dodged!");
                return 0f;
            }

            ChangeHP(-finalDamage);
            return finalDamage;
        }

        // 죽었는지 확인
        public bool IsDead()
        {
            return currentHP <= 0f;
        }

        // 영혼 보너스 리셋
        public void ResetSoulBonusStats()
        {
            soulBonusStats = new StatBlock();
            RecalculateStats();
            OnStatsChanged?.Invoke(this);
        }

        // 인챈트 보너스 스탯 설정
        public void SetEnchantBonusStats(StatBlock enchantStats)
        {
            enchantBonusStats = enchantStats;
            RecalculateStats();
            OnStatsChanged?.Invoke(this);
        }

        public StatBlock GetEnchantBonusStats()
        {
            return enchantBonusStats;
        }

        public void ResetEnchantBonusStats()
        {
            enchantBonusStats = new StatBlock();
            RecalculateStats();
            OnStatsChanged?.Invoke(this);
        }

        public StatBlock GetTotalEquipmentStats()
        {
            StatBlock totalStats = new StatBlock();
            totalStats.strength = equipmentBonusStats.strength + enchantBonusStats.strength;
            totalStats.agility = equipmentBonusStats.agility + enchantBonusStats.agility;
            totalStats.vitality = equipmentBonusStats.vitality + enchantBonusStats.vitality;
            totalStats.intelligence = equipmentBonusStats.intelligence + enchantBonusStats.intelligence;
            totalStats.defense = equipmentBonusStats.defense + enchantBonusStats.defense;
            totalStats.magicDefense = equipmentBonusStats.magicDefense + enchantBonusStats.magicDefense;
            totalStats.luck = equipmentBonusStats.luck + enchantBonusStats.luck;
            totalStats.stability = equipmentBonusStats.stability + enchantBonusStats.stability;
            return totalStats;
        }

        // 초기화
        public void Initialize()
        {
            currentLevel = 1;
            currentExp = 0;
            expToNextLevel = CalculateExpForLevel(2);
            gold = 1000;

            // 보너스 스탯 초기화
            soulBonusStats = new StatBlock();
            equipmentBonusStats = new StatBlock();
            enchantBonusStats = new StatBlock();

            RecalculateStats();
            currentHP = maxHP;
            currentMP = maxMP;
        }

        // 디버그 정보
        public void LogStats()
        {
            Debug.Log($"=== {characterRace} Character Stats ===");
            Debug.Log($"Level {currentLevel} | EXP: {currentExp}/{expToNextLevel}");
            Debug.Log($"Base Stats - STR: {currentStats.strength} | AGI: {currentStats.agility} | VIT: {currentStats.vitality} | INT: {currentStats.intelligence}");
            Debug.Log($"Base Stats - DEF: {currentStats.defense} | MDEF: {currentStats.magicDefense} | LUK: {currentStats.luck}");
            Debug.Log($"Total Stats - STR: {TotalSTR} | AGI: {TotalAGI} | VIT: {TotalVIT} | INT: {TotalINT}");
            Debug.Log($"Total Stats - DEF: {TotalDEF} | MDEF: {TotalMDEF} | LUK: {TotalLUK}");
            Debug.Log($"HP: {currentHP}/{maxHP} | MP: {currentMP}/{maxMP}");
            Debug.Log($"Physical ATK: {attackDamage} | Magic ATK: {magicDamage} | Move Speed: {moveSpeed}");
            Debug.Log($"Attack Speed: {attackSpeed} | Crit Rate: {criticalChance:P2} | Dodge Rate: {(TotalAGI * 0.001f):P2}");
        }
    }
    // 열거형들
    public enum StatType
    {
        STR,    // 힘
        AGI,    // 민첩
        VIT,    // 체력
        INT,    // 지능
        DEF,    // 물리 방어력
        MDEF,   // 마법 방어력
        LUK,    // 운
        STAB    // 안정성
    }
    
    public enum DamageType
    {
        Physical,   // 물리 데미지
        Magical,    // 마법 데미지
        Fire,       // 화염 데미지
        Ice,        // 빙결 데미지
        Lightning,  // 번개 데미지
        Poison,     // 독 데미지
        Dark,       // 암흑 데미지
        Holy,       // 신성 데미지
        True        // 고정 데미지 (방어력 무시)
    }
}