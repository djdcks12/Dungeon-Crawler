using UnityEngine;
using Unity.Netcode;

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
        public WeaponCategory category;
        [Header("New Weapon System")]
        public WeaponGroup weaponGroup = WeaponGroup.SwordShield; // 새로운 무기군 시스템
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
        public WeaponCategory Category => category;
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
            if (category != WeaponCategory.Staff && category != WeaponCategory.Wand)
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
                case WeaponType.Mace:
                    weapon.minDamagePercent = 50f;
                    weapon.maxDamagePercent = 150f;
                    weapon.stabilityBonus = -2f;
                    break;
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
    /// 무기 타입 열거형
    /// </summary>
    public enum WeaponType
    {
        // 검류
        Longsword,
        Rapier,
        Broadsword,
        
        // 둔기류  
        Mace,
        Warhammer,
        
        // 단검류
        Dagger,
        CurvedDagger,
        
        // 활/석궁류
        Longbow,
        Crossbow,
        
        // 지팡이류
        OakStaff,
        CrystalStaff,
        
        // 기타
        Fists,      // 맨손
        Shield      // 방패
    }
    
    /// <summary>
    /// 무기 카테고리
    /// </summary>
    public enum WeaponCategory
    {
        None,       // 없음 (기본값)
        Sword,      // 검
        Blunt,      // 둔기 (Mace로도 사용)
        Dagger,     // 단검
        Axe,        // 도끼
        Mace,       // 메이스
        Bow,        // 활
        Staff,      // 지팡이
        Wand,       // 완드
        Shield,     // 방패
        Fists       // 맨손
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