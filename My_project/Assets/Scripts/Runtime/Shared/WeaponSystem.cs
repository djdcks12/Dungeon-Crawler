using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 무기 시스템 - 민댐/맥댐 및 무기별 특성 관리
    /// </summary>
    [CreateAssetMenu(fileName = "New Weapon", menuName = "Dungeon Crawler/Weapon")]
    public class WeaponData : ScriptableObject
    {
        [Header("Weapon Information")]
        public string weaponName;
        public WeaponType weaponType;
        public ItemGrade rarity = ItemGrade.Common;
        [TextArea(2, 4)]
        public string description;
        public Sprite weaponIcon;
        
        [Header("Base Damage Range")]
        [SerializeField] private float baseDamage = 10f;
        [SerializeField] private float minDamagePercent = 80f;  // 80% 기본값
        [SerializeField] private float maxDamagePercent = 120f; // 120% 기본값
        
        [Header("Weapon Stats")]
        [SerializeField] private StatBlock statBonuses;
        [SerializeField] private float attackSpeed = 1.0f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float criticalBonus = 0f;
        [SerializeField] private float stabilityBonus = 0f;
        
        [Header("Elemental Properties")]
        [SerializeField] private ElementalDamageRange elementalDamage;
        
        [Header("Special Effects")]
        [SerializeField] private WeaponEffect[] specialEffects;
        
        [Header("Hit Effects")]
        [SerializeField] private EffectData hitEffect; // 타격 시 이펙트
        
        // 프로퍼티들
        public WeaponType Type => weaponType;
        public WeaponGroup WeaponGroup => WeaponTypeMapper.GetWeaponGroup(weaponType);
        public ItemGrade Rarity => rarity;
        public StatBlock StatBonuses => statBonuses;
        public float AttackSpeed => attackSpeed;
        public float AttackRange => attackRange;
        public float CriticalBonus => criticalBonus;
        public float StabilityBonus => stabilityBonus;
        public ElementalDamageRange ElementalDamage => elementalDamage;
        public WeaponEffect[] SpecialEffects => specialEffects;
        public EffectData HitEffect => hitEffect;
        
        /// <summary>
        /// 무기의 데미지 범위 계산 (플레이어 스탯 적용)
        /// </summary>
        public DamageRange CalculateDamageRange(float playerSTR, float playerStability = 0f)
        {
            // 기본 민댐/맥댐 계산
            float weaponMinDamage = baseDamage * (minDamagePercent / 100f);
            float weaponMaxDamage = baseDamage * (maxDamagePercent / 100f);
            
            // 플레이어 STR 보너스 적용
            float finalMinDamage = weaponMinDamage + (playerSTR * 1.5f);
            float finalMaxDamage = weaponMaxDamage + (playerSTR * 2.5f);
            
            // 등급별 편차 조정
            ApplyRarityModifiers(ref finalMinDamage, ref finalMaxDamage);
            
            return new DamageRange(finalMinDamage, finalMaxDamage, stabilityBonus)
                .GetStabilizedRange(playerStability);
        }
        
        /// <summary>
        /// 무기의 마법 데미지 범위 계산
        /// </summary>
        public DamageRange CalculateMagicDamageRange(float playerINT, float playerStability = 0f)
        {
            // 마법 무기가 아니면 0 반환
            if (!WeaponTypeMapper.IsMagicalWeaponGroup(WeaponGroup))
                return new DamageRange(0, 0, 0);
                
            // 기본 민댐/맥댐 계산 (마법용)
            float weaponMinDamage = baseDamage * (minDamagePercent / 100f);
            float weaponMaxDamage = baseDamage * (maxDamagePercent / 100f);
            
            // 플레이어 INT 보너스 적용
            float finalMinDamage = weaponMinDamage + (playerINT * 1.5f);
            float finalMaxDamage = weaponMaxDamage + (playerINT * 2.5f);
            
            // 등급별 편차 조정
            ApplyRarityModifiers(ref finalMinDamage, ref finalMaxDamage);
            
            return new DamageRange(finalMinDamage, finalMaxDamage, stabilityBonus)
                .GetStabilizedRange(playerStability);
        }
        
        /// <summary>
        /// 등급별 편차 적용
        /// </summary>
        private void ApplyRarityModifiers(ref float minDamage, ref float maxDamage)
        {
            float rarityMinPercent, rarityMaxPercent;
            
            switch (rarity)
            {
                case ItemGrade.Common:     // 일반 (White)
                    rarityMinPercent = Random.Range(80f, 90f);
                    rarityMaxPercent = Random.Range(110f, 120f);
                    break;
                case ItemGrade.Uncommon:   // 고급 (Green)
                    rarityMinPercent = Random.Range(78f, 88f);
                    rarityMaxPercent = Random.Range(112f, 122f);
                    break;
                case ItemGrade.Rare:       // 희귀 (Blue)
                    rarityMinPercent = Random.Range(75f, 85f);
                    rarityMaxPercent = Random.Range(115f, 125f);
                    break;
                case ItemGrade.Epic:       // 영웅 (Purple)
                    rarityMinPercent = Random.Range(70f, 80f);
                    rarityMaxPercent = Random.Range(120f, 130f);
                    break;
                case ItemGrade.Legendary:  // 전설 (Orange)
                    rarityMinPercent = Random.Range(60f, 75f);
                    rarityMaxPercent = Random.Range(125f, 140f);
                    break;
                default:
                    rarityMinPercent = 80f;
                    rarityMaxPercent = 120f;
                    break;
            }
            
            minDamage *= (rarityMinPercent / 100f);
            maxDamage *= (rarityMaxPercent / 100f);
        }
        
        /// <summary>
        /// 무기 타입별 기본 편차 설정
        /// </summary>
        public static void SetWeaponTypeDefaults(WeaponData weapon)
        {
            switch (weapon.weaponType)
            {
                // 검류 (균형형)
                case WeaponType.Longsword:
                    weapon.minDamagePercent = 80f;
                    weapon.maxDamagePercent = 120f;
                    weapon.stabilityBonus = 2f;
                    break;
                case WeaponType.Rapier:
                    weapon.minDamagePercent = 60f;
                    weapon.maxDamagePercent = 140f;
                    weapon.stabilityBonus = 0f;
                    break;
                case WeaponType.Broadsword:
                    weapon.minDamagePercent = 85f;
                    weapon.maxDamagePercent = 115f;
                    weapon.stabilityBonus = 5f;
                    break;
                    
                // 둔기류 (고댐 불안정)
                case WeaponType.Warhammer:
                    weapon.minDamagePercent = 40f;
                    weapon.maxDamagePercent = 160f;
                    weapon.stabilityBonus = -5f;
                    break;
                    
                // 단검류 (저댐 안정)
                case WeaponType.Dagger:
                    weapon.minDamagePercent = 90f;
                    weapon.maxDamagePercent = 110f;
                    weapon.stabilityBonus = 3f;
                    break;
                case WeaponType.CurvedDagger:
                    weapon.minDamagePercent = 85f;
                    weapon.maxDamagePercent = 115f;
                    weapon.stabilityBonus = 2f;
                    break;
                    
                // 활/석궁류
                case WeaponType.Longbow:
                    weapon.minDamagePercent = 70f;
                    weapon.maxDamagePercent = 130f;
                    weapon.stabilityBonus = 1f;
                    break;
                case WeaponType.Crossbow:
                    weapon.minDamagePercent = 60f;
                    weapon.maxDamagePercent = 140f;
                    weapon.stabilityBonus = 0f;
                    break;
                    
                // 지팡이류 (마법)
                case WeaponType.OakStaff:
                    weapon.minDamagePercent = 75f;
                    weapon.maxDamagePercent = 125f;
                    weapon.stabilityBonus = 2f;
                    break;
                case WeaponType.CrystalStaff:
                    weapon.minDamagePercent = 50f;
                    weapon.maxDamagePercent = 150f;
                    weapon.stabilityBonus = -1f;
                    break;
            }
        }
    }
    
    /// <summary>
    /// 무기 타입 열거형 - 숙련도 및 개별 아이템 관리 단위
    /// </summary>
    public enum WeaponType
    {
        // SwordShield 그룹 (한손검/방패)
        Longsword,
        Rapier,
        Broadsword,
        Gladius,
        Scimitar,
        
        // TwoHandedSword 그룹 (양손 대검)
        Greatsword,
        Claymore,
        Flamberge,
        Zweihander,
        
        // TwoHandedAxe 그룹 (양손 도끼/둔기)
        Battleaxe,
        Warhammer,
        Maul,
        Greataxe,
        
        // Dagger 그룹 (단검)
        Dagger,
        CurvedDagger,
        Stiletto,
        Kris,
        
        // Bow 그룹 (활/석궁)
        Longbow,
        Crossbow,
        CompoundBow,
        Shortbow,
        
        // Staff 그룹 (지팡이)
        OakStaff,
        CrystalStaff,
        FireStaff,
        IceStaff,
        HolyStaff,
        
        // Wand 그룹 (마법봉)
        MagicWand,
        CrystalWand,
        RuneWand,
        ElementalWand,
        
        // Fist 그룹 (격투 무기)
        Fists,      // 맨손
        Knuckles,
        Claws,
        Gauntlets
    }
    
    /// <summary>
    /// WeaponType을 WeaponGroup으로 매핑하는 유틸리티
    /// </summary>
    public static class WeaponTypeMapper
    {
        public static WeaponGroup GetWeaponGroup(WeaponType weaponType)
        {
            return weaponType switch
            {
                // SwordShield 그룹
                WeaponType.Longsword or WeaponType.Rapier or WeaponType.Broadsword or 
                WeaponType.Gladius or WeaponType.Scimitar => WeaponGroup.SwordShield,
                
                // TwoHandedSword 그룹
                WeaponType.Greatsword or WeaponType.Claymore or WeaponType.Flamberge or 
                WeaponType.Zweihander => WeaponGroup.TwoHandedSword,
                
                // TwoHandedAxe 그룹
                WeaponType.Battleaxe or WeaponType.Warhammer or WeaponType.Maul or 
                WeaponType.Greataxe => WeaponGroup.TwoHandedAxe,
                
                // Dagger 그룹
                WeaponType.Dagger or WeaponType.CurvedDagger or WeaponType.Stiletto or 
                WeaponType.Kris => WeaponGroup.Dagger,
                
                // Bow 그룹
                WeaponType.Longbow or WeaponType.Crossbow or WeaponType.CompoundBow or 
                WeaponType.Shortbow => WeaponGroup.Bow,
                
                // Staff 그룹
                WeaponType.OakStaff or WeaponType.CrystalStaff or WeaponType.FireStaff or 
                WeaponType.IceStaff or WeaponType.HolyStaff => WeaponGroup.Staff,
                
                // Wand 그룹
                WeaponType.MagicWand or WeaponType.CrystalWand or WeaponType.RuneWand or 
                WeaponType.ElementalWand => WeaponGroup.Wand,
                
                // Fist 그룹
                WeaponType.Fists or WeaponType.Knuckles or WeaponType.Claws or 
                WeaponType.Gauntlets => WeaponGroup.Fist,
                
                _ => WeaponGroup.SwordShield // 기본값
            };
        }
        
        /// <summary>
        /// WeaponGroup이 마법 데미지를 사용하는지 확인
        /// </summary>
        public static bool IsMagicalWeaponGroup(WeaponGroup weaponGroup)
        {
            return weaponGroup == WeaponGroup.Staff || weaponGroup == WeaponGroup.Wand;
        }
        
        /// <summary>
        /// WeaponGroup에 따른 장비 슬롯 결정
        /// </summary>
        public static EquipmentSlot GetEquipmentSlot(WeaponGroup weaponGroup)
        {
            return weaponGroup switch
            {
                WeaponGroup.SwordShield => EquipmentSlot.MainHand,
                WeaponGroup.Dagger => EquipmentSlot.MainHand,
                WeaponGroup.TwoHandedSword => EquipmentSlot.TwoHand,
                WeaponGroup.TwoHandedAxe => EquipmentSlot.TwoHand,
                WeaponGroup.Bow => EquipmentSlot.TwoHand,
                WeaponGroup.Staff => EquipmentSlot.TwoHand,
                WeaponGroup.Wand => EquipmentSlot.MainHand,
                WeaponGroup.Fist => EquipmentSlot.MainHand,
                _ => EquipmentSlot.MainHand
            };
        }
        
        /// <summary>
        /// 종족별 사용 가능한 WeaponGroup 목록
        /// </summary>
        public static WeaponGroup[] GetAvailableWeaponGroups(Race race)
        {
            return race switch
            {
                Race.Human => new[] { 
                    WeaponGroup.SwordShield, 
                    WeaponGroup.TwoHandedSword, 
                    WeaponGroup.Bow, 
                    WeaponGroup.Fist 
                },
                Race.Elf => new[] { 
                    WeaponGroup.Bow, 
                    WeaponGroup.Staff, 
                    WeaponGroup.Wand 
                },
                Race.Beast => new[] { 
                    WeaponGroup.TwoHandedAxe, 
                    WeaponGroup.Dagger, 
                    WeaponGroup.Bow, 
                    WeaponGroup.Fist, 
                    WeaponGroup.Staff 
                },
                _ => new[] { WeaponGroup.Fist }
            };
        }
        
        /// <summary>
        /// 종족-무기군 조합별 선택 가능한 직업 목록
        /// </summary>
        public static JobType[] GetAvailableJobTypes(Race race, WeaponGroup weaponGroup)
        {
            return (race, weaponGroup) switch
            {
                // Human 조합
                (Race.Human, WeaponGroup.SwordShield) => new[] { 
                    JobType.Navigator, JobType.Scout, JobType.Guardian, JobType.Templar 
                },
                (Race.Human, WeaponGroup.TwoHandedSword) => new[] { 
                    JobType.Guardian, JobType.Templar, JobType.Berserker, JobType.Duelist 
                },
                (Race.Human, WeaponGroup.Bow) => new[] { 
                    JobType.Navigator, JobType.Scout, JobType.Tracker, JobType.Sniper 
                },
                (Race.Human, WeaponGroup.Fist) => new[] { 
                    JobType.Berserker, JobType.Duelist, JobType.Trapper 
                },
                
                // Elf 조합
                (Race.Elf, WeaponGroup.Bow) => new[] { 
                    JobType.Scout, JobType.Tracker, JobType.Sniper 
                },
                (Race.Elf, WeaponGroup.Staff) => new[] { 
                    JobType.Mage, JobType.Warlock, JobType.Druid 
                },
                (Race.Elf, WeaponGroup.Wand) => new[] { 
                    JobType.Mage, JobType.Cleric, JobType.Amplifier 
                },
                
                // Beast 조합
                (Race.Beast, WeaponGroup.TwoHandedAxe) => new[] { 
                    JobType.Berserker, JobType.Duelist, JobType.ElementalBruiser 
                },
                (Race.Beast, WeaponGroup.Dagger) => new[] { 
                    JobType.Assassin, JobType.Tracker, JobType.Trapper 
                },
                (Race.Beast, WeaponGroup.Bow) => new[] { 
                    JobType.Tracker, JobType.Sniper 
                },
                (Race.Beast, WeaponGroup.Fist) => new[] { 
                    JobType.Berserker, JobType.Assassin, JobType.ElementalBruiser 
                },
                (Race.Beast, WeaponGroup.Staff) => new[] { 
                    JobType.Druid, JobType.ElementalBruiser 
                },
                
                _ => new[] { JobType.Navigator } // 기본값
            };
        }
        
        /// <summary>
        /// 특정 종족이 특정 무기군을 사용할 수 있는지 확인
        /// </summary>
        public static bool CanRaceUseWeaponGroup(Race race, WeaponGroup weaponGroup)
        {
            var availableGroups = GetAvailableWeaponGroups(race);
            return availableGroups.Contains(weaponGroup);
        }
    }
    
    // ItemGrade는 ItemData.cs에서 정의됨 (통합된 아이템 등급 시스템)
    
    /// <summary>
    /// 무기 특수 효과
    /// </summary>
    [System.Serializable]
    public struct WeaponEffect
    {
        public WeaponEffectType effectType;
        public float value;
        public float chance;  // 발동 확률 (0~1)
        public string description;
    }
    
    /// <summary>
    /// 무기 특수 효과 타입
    /// </summary>
    public enum WeaponEffectType
    {
        LifeSteal,      // 생명력 흡수
        ManaSteal,      // 마나 흡수
        ElementalBurst, // 속성 폭발
        Stun,           // 기절
        Poison,         // 독
        Freeze,         // 동결
        Burn,           // 화상
        CriticalBoost,  // 치명타 확률 증가
        DamageBoost,    // 데미지 증가
        SpeedBoost      // 공격 속도 증가
    }
}