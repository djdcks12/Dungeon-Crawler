using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 인챈트 타입 열거형
    /// </summary>
    public enum EnchantType
    {
        None,
        
        // 공격 관련
        Sharpness,      // 예리함 - 공격력 증가
        CriticalHit,    // 치명타 - 치명타 확률 증가
        LifeSteal,      // 흡혈 - 공격 시 체력 회복
        
        // 방어 관련
        Protection,     // 보호 - 방어력 증가
        Thorns,         // 가시 - 반격 데미지
        Regeneration,   // 재생 - 체력 자동 회복
        
        // 유틸리티
        Fortune,        // 행운 - 드롭률 증가
        Speed,          // 신속 - 이동속도 증가
        MagicBoost,     // 마력 증폭 - 마법 공격력 증가
        Durability      // 내구성 - 아이템 내구도 증가
    }
    
    /// <summary>
    /// 인챈트 희귀도
    /// </summary>
    public enum EnchantRarity
    {
        Common,     // 일반 (60% 확률)
        Rare,       // 희귀 (30% 확률) 
        Epic,       // 영웅 (9% 확률)
        Legendary   // 전설 (1% 확률)
    }
    
    /// <summary>
    /// 인챈트 데이터 구조체
    /// </summary>
    [System.Serializable]
    public struct EnchantData : INetworkSerializable
    {
        public EnchantType enchantType;
        public EnchantRarity rarity;
        public int level;           // 인챈트 레벨 (1-5)
        public float power;         // 인챈트 효과 값
        public string description;  // 인챈트 설명
        public ulong enchantId;     // 고유 ID
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref enchantType);
            serializer.SerializeValue(ref rarity);
            serializer.SerializeValue(ref level);
            serializer.SerializeValue(ref power);
            serializer.SerializeValue(ref description);
            serializer.SerializeValue(ref enchantId);
        }
        
        /// <summary>
        /// 인챈트 이름 반환
        /// </summary>
        public string GetEnchantName()
        {
            string rarityPrefix = rarity switch
            {
                EnchantRarity.Common => "",
                EnchantRarity.Rare => "상급 ",
                EnchantRarity.Epic => "마스터 ",
                EnchantRarity.Legendary => "전설의 ",
                _ => ""
            };
            
            string baseName = enchantType switch
            {
                EnchantType.Sharpness => "예리함",
                EnchantType.CriticalHit => "치명타",
                EnchantType.LifeSteal => "흡혈",
                EnchantType.Protection => "보호",
                EnchantType.Thorns => "가시",
                EnchantType.Regeneration => "재생",
                EnchantType.Fortune => "행운",
                EnchantType.Speed => "신속",
                EnchantType.MagicBoost => "마력 증폭",
                EnchantType.Durability => "내구성",
                _ => "알 수 없음"
            };
            
            return $"{rarityPrefix}{baseName} {level}";
        }
        
        /// <summary>
        /// 인챈트 효과 설명
        /// </summary>
        public string GetEffectDescription()
        {
            return enchantType switch
            {
                EnchantType.Sharpness => $"공격력 +{power}%",
                EnchantType.CriticalHit => $"치명타 확률 +{power}%",
                EnchantType.LifeSteal => $"흡혈 {power}%",
                EnchantType.Protection => $"방어력 +{power}%",
                EnchantType.Thorns => $"반격 데미지 {power}%",
                EnchantType.Regeneration => $"체력 재생 +{power}/초",
                EnchantType.Fortune => $"드롭률 +{power}%",
                EnchantType.Speed => $"이동속도 +{power}%",
                EnchantType.MagicBoost => $"마법 공격력 +{power}%",
                EnchantType.Durability => $"내구도 +{power}%",
                _ => "효과 없음"
            };
        }
        
        /// <summary>
        /// 인챈트 색상 (희귀도별)
        /// </summary>
        public Color GetEnchantColor()
        {
            return rarity switch
            {
                EnchantRarity.Common => Color.white,
                EnchantRarity.Rare => Color.blue,
                EnchantRarity.Epic => Color.magenta,
                EnchantRarity.Legendary => Color.yellow,
                _ => Color.gray
            };
        }
    }
    
    /// <summary>
    /// 인챈트 북 아이템 데이터
    /// </summary>
    [System.Serializable]
    public struct EnchantBookData : INetworkSerializable
    {
        public EnchantData enchant;
        public bool isUsed;
        public ulong bookId;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref enchant);
            serializer.SerializeValue(ref isUsed);
            serializer.SerializeValue(ref bookId);
        }
    }
}