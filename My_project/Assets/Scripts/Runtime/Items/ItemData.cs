using UnityEngine;
using System;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 5등급 아이템 시스템의 기본 아이템 데이터
    /// Common(1등급) → Uncommon(2등급) → Rare(3등급) → Epic(4등급) → Legendary(5등급)
    /// </summary>
    [CreateAssetMenu(fileName = "New Item", menuName = "Dungeon Crawler/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string itemId = "";
        [SerializeField] private string itemName = "";
        [SerializeField] private string description = "";
        [SerializeField] private Sprite itemIcon;
        
        [Header("아이템 분류")]
        [SerializeField] private ItemType itemType = ItemType.Equipment;
        [SerializeField] private ItemGrade grade = ItemGrade.Common;
        [SerializeField] private EquipmentSlot equipmentSlot = EquipmentSlot.None;
        [SerializeField] private WeaponCategory weaponCategory = WeaponCategory.None;
        
        [Header("기본 속성")]
        [SerializeField] private int stackSize = 1;
        [SerializeField] private long sellPrice = 10;
        [SerializeField] private bool isDroppable = true;
        [SerializeField] private bool isDestroyable = true;
        
        [Header("장비 스탯 (장비 아이템만)")]
        [SerializeField] private StatBlock statBonuses = new StatBlock();
        [SerializeField] private int durability = 100;
        [SerializeField] private int maxDurability = 100;
        
        [Header("무기 속성 (무기만)")]
        [SerializeField] private DamageRange weaponDamageRange = new DamageRange(10, 15, 0);
        [SerializeField] private float criticalBonus = 0f;
        [SerializeField] private DamageType weaponDamageType = DamageType.Physical;
        
        [Header("소모품 속성 (소모품만)")]
        [SerializeField] private float healAmount = 0f;
        [SerializeField] private float manaAmount = 0f;
        [SerializeField] private StatusEffect[] consumableEffects = new StatusEffect[0];
        
        [Header("등급별 색상")]
        [SerializeField] private Color gradeColor = Color.white;
        
        // 프로퍼티들
        public string ItemId => itemId;
        public string ItemName => itemName;
        public string Description => description;
        public Sprite ItemIcon => itemIcon;
        public ItemType ItemType => itemType;
        public ItemGrade Grade => grade;
        public EquipmentSlot EquipmentSlot => equipmentSlot;
        public WeaponCategory WeaponCategory => weaponCategory;
        public int StackSize => stackSize;
        public long SellPrice => sellPrice;
        public bool IsDroppable => isDroppable;
        public bool IsDestroyable => isDestroyable;
        public StatBlock StatBonuses => statBonuses;
        public int Durability => durability;
        public int MaxDurability => maxDurability;
        public DamageRange WeaponDamageRange => weaponDamageRange;
        public float CriticalBonus => criticalBonus;
        public DamageType WeaponDamageType => weaponDamageType;
        public float HealAmount => healAmount;
        public float ManaAmount => manaAmount;
        public StatusEffect[] ConsumableEffects => consumableEffects;
        public Color GradeColor => gradeColor;
        
        /// <summary>
        /// 아이템이 장비 가능한지 확인
        /// </summary>
        public bool IsEquippable => itemType == ItemType.Equipment && equipmentSlot != EquipmentSlot.None;
        
        /// <summary>
        /// 아이템이 무기인지 확인
        /// </summary>
        public bool IsWeapon => itemType == ItemType.Equipment && 
                               (equipmentSlot == EquipmentSlot.MainHand || equipmentSlot == EquipmentSlot.TwoHand);
        
        /// <summary>
        /// 아이템이 소모품인지 확인
        /// </summary>
        public bool IsConsumable => itemType == ItemType.Consumable;
        
        /// <summary>
        /// 아이템이 스택 가능한지 확인
        /// </summary>
        public bool CanStack => itemType == ItemType.Consumable || itemType == ItemType.Material;
        
        /// <summary>
        /// 최대 스택 크기
        /// </summary>
        public int MaxStackSize => CanStack ? stackSize : 1;
        
        /// <summary>
        /// 내구도가 있는지 확인
        /// </summary>
        public bool HasDurability => IsEquippable;
        
        /// <summary>
        /// 등급별 색상 자동 설정
        /// </summary>
        private void OnValidate()
        {
            gradeColor = GetGradeColor(grade);
            
            // 아이템 ID가 비어있으면 자동 생성
            if (string.IsNullOrEmpty(itemId))
            {
                itemId = $"{itemType}_{grade}_{System.Guid.NewGuid().ToString("N")[..8]}";
            }
        }
        
        /// <summary>
        /// 등급별 기본 색상 반환
        /// </summary>
        public static Color GetGradeColor(ItemGrade grade)
        {
            return grade switch
            {
                ItemGrade.Common => new Color(0.8f, 0.8f, 0.8f), // 회색
                ItemGrade.Uncommon => new Color(0.3f, 1f, 0.3f), // 초록
                ItemGrade.Rare => new Color(0.3f, 0.3f, 1f),     // 파랑
                ItemGrade.Epic => new Color(0.6f, 0.3f, 1f),     // 보라
                ItemGrade.Legendary => new Color(1f, 0.6f, 0f),  // 주황
                _ => Color.white
            };
        }
        
        /// <summary>
        /// 등급별 드롭 확률 반환
        /// </summary>
        public static float GetGradeDropRate(ItemGrade grade)
        {
            return grade switch
            {
                ItemGrade.Common => 0.6f,      // 60%
                ItemGrade.Uncommon => 0.25f,   // 25%
                ItemGrade.Rare => 0.1f,        // 10%
                ItemGrade.Epic => 0.04f,       // 4%
                ItemGrade.Legendary => 0.01f,  // 1%
                _ => 0f
            };
        }
        
        /// <summary>
        /// 등급별 기본 판매가 배수 반환
        /// </summary>
        public static float GetGradePriceMultiplier(ItemGrade grade)
        {
            return grade switch
            {
                ItemGrade.Common => 1f,
                ItemGrade.Uncommon => 3f,
                ItemGrade.Rare => 10f,
                ItemGrade.Epic => 30f,
                ItemGrade.Legendary => 100f,
                _ => 1f
            };
        }
        
        /// <summary>
        /// 아이템의 총 가치 계산 (기본가 + 등급 배수)
        /// </summary>
        public long GetTotalValue()
        {
            return (long)(sellPrice * GetGradePriceMultiplier(grade));
        }
        
        /// <summary>
        /// 내구도 감소
        /// </summary>
        public void DecreaseDurability(int amount = 1)
        {
            if (IsEquippable)
            {
                durability = Mathf.Max(0, durability - amount);
            }
        }
        
        /// <summary>
        /// 내구도 수리
        /// </summary>
        public void RepairDurability(int amount)
        {
            if (IsEquippable)
            {
                durability = Mathf.Min(maxDurability, durability + amount);
            }
        }
        
        /// <summary>
        /// 아이템이 사용 가능한지 확인
        /// </summary>
        public bool CanUse()
        {
            if (IsEquippable && durability <= 0)
                return false;
                
            return true;
        }
        
        /// <summary>
        /// 무기 데미지 계산 (STR과 안정성 적용)
        /// </summary>
        public DamageRange CalculateWeaponDamage(float strength, float stability)
        {
            if (!IsWeapon) return new DamageRange(0, 0, 0);
            
            // 기본 무기 데미지에 STR 적용
            float strMultiplier = 1f + (strength * 0.1f);
            float minDamage = weaponDamageRange.minDamage * strMultiplier;
            float maxDamage = weaponDamageRange.maxDamage * strMultiplier;
            
            // 등급별 데미지 보너스
            float gradeBonus = GetGradePriceMultiplier(grade) * 0.1f;
            minDamage *= (1f + gradeBonus);
            maxDamage *= (1f + gradeBonus);
            
            // 내구도에 따른 데미지 감소
            float durabilityRatio = durability / (float)maxDurability;
            minDamage *= durabilityRatio;
            maxDamage *= durabilityRatio;
            
            return new DamageRange(minDamage, maxDamage, stability);
        }
        
        /// <summary>
        /// 아이템 정보 텍스트 생성
        /// </summary>
        public string GetInfoText()
        {
            string info = $"<color=#{ColorUtility.ToHtmlStringRGBA(gradeColor)}>{itemName}</color>\n";
            info += $"등급: {GetGradeText(grade)}\n";
            info += $"타입: {GetTypeText(itemType)}\n";
            
            if (!string.IsNullOrEmpty(description))
            {
                info += $"\n{description}\n";
            }
            
            if (IsEquippable)
            {
                info += $"\n내구도: {durability}/{maxDurability}";
                if (statBonuses.HasAnyStats())
                {
                    info += $"\n{statBonuses.GetStatsText()}";
                }
            }
            
            if (IsWeapon)
            {
                info += $"\n공격력: {weaponDamageRange.minDamage:F0}-{weaponDamageRange.maxDamage:F0}";
                if (criticalBonus > 0)
                {
                    info += $"\n치명타 보너스: +{criticalBonus:P1}";
                }
            }
            
            if (IsConsumable)
            {
                if (healAmount > 0) info += $"\nHP 회복: +{healAmount:F0}";
                if (manaAmount > 0) info += $"\nMP 회복: +{manaAmount:F0}";
            }
            
            info += $"\n\n판매가: {GetTotalValue():N0} 골드";
            
            return info;
        }
        
        private string GetGradeText(ItemGrade grade)
        {
            return grade switch
            {
                ItemGrade.Common => "일반",
                ItemGrade.Uncommon => "고급",
                ItemGrade.Rare => "희귀",
                ItemGrade.Epic => "영웅",
                ItemGrade.Legendary => "전설",
                _ => "알 수 없음"
            };
        }
        
        private string GetTypeText(ItemType type)
        {
            return type switch
            {
                ItemType.Equipment => "장비",
                ItemType.Consumable => "소모품",
                ItemType.Material => "재료",
                ItemType.Quest => "퀘스트",
                _ => "기타"
            };
        }
    }
    
    /// <summary>
    /// 아이템 타입
    /// </summary>
    public enum ItemType
    {
        Equipment,  // 장비 (무기, 방어구)
        Consumable, // 소모품 (포션, 스크롤)
        Material,   // 재료 (제작 재료)
        Quest,      // 퀘스트 아이템
        Other       // 기타
    }
    
    /// <summary>
    /// 아이템 등급 (5등급 시스템)
    /// </summary>
    public enum ItemGrade
    {
        Common = 1,     // 1등급 - 일반 (회색)
        Uncommon = 2,   // 2등급 - 고급 (초록)
        Rare = 3,       // 3등급 - 희귀 (파랑)
        Epic = 4,       // 4등급 - 영웅 (보라)
        Legendary = 5   // 5등급 - 전설 (주황)
    }
}