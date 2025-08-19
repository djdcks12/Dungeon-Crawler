using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 인챈트 북 드롭 시스템 - 1% 확률로 드롭
    /// 하드코어 던전 크롤러의 극악 드롭률 시스템
    /// </summary>
    public class EnchantDropSystem : MonoBehaviour
    {
        [Header("드롭 설정")]
        [SerializeField] private float baseDropRate = 0.01f; // 1% 기본 드롭률
        [SerializeField] private float luckBonus = 0.0001f;  // LUK당 0.01% 보너스
        
        [Header("인챈트 북 프리팹")]
        [SerializeField] private GameObject enchantBookPrefab;
        
        // 드롭률 통계
        private static int totalDropChecks = 0;
        private static int successfulDrops = 0;
        
        /// <summary>
        /// 몬스터 처치 시 인챈트 북 드롭 체크
        /// </summary>
        public void CheckEnchantDrop(Vector3 dropPosition, int monsterLevel, string monsterName, PlayerController killer)
        {
            if (killer?.GetComponent<PlayerStatsManager>() == null) return;
            
            totalDropChecks++;
            
            // LUK 기반 드롭률 계산
            float finalDropRate = CalculateFinalDropRate(killer);
            
            // 드롭 판정
            if (Random.value < finalDropRate)
            {
                successfulDrops++;
                CreateEnchantBookDrop(dropPosition, monsterLevel, monsterName);
                
                Debug.Log($"✨ ENCHANT BOOK DROP! Rate: {finalDropRate:P3} ({successfulDrops}/{totalDropChecks})");
            }
        }
        
        /// <summary>
        /// 최종 드롭률 계산 (LUK 포함)
        /// </summary>
        private float CalculateFinalDropRate(PlayerController player)
        {
            var statsManager = player.GetComponent<PlayerStatsManager>();
            if (statsManager?.CurrentStats == null) return baseDropRate;
            
            float playerLuck = statsManager.CurrentStats.TotalLUK;
            float luckBonus = playerLuck * this.luckBonus;
            
            return baseDropRate + luckBonus;
        }
        
        /// <summary>
        /// 인챈트 북 드롭 생성
        /// </summary>
        private void CreateEnchantBookDrop(Vector3 position, int monsterLevel, string monsterName)
        {
            // 몬스터 레벨에 따른 인챈트 생성
            EnchantData enchant = GenerateRandomEnchant(monsterLevel);
            
            // 인챈트 북 아이템 생성
            CreateEnchantBookItem(position, enchant, monsterName);
            
            Debug.Log($"📖 Created enchant book: {enchant.GetEnchantName()} from {monsterName}");
        }
        
        /// <summary>
        /// 랜덤 인챈트 생성
        /// </summary>
        private EnchantData GenerateRandomEnchant(int monsterLevel)
        {
            // 희귀도 결정 (몬스터 레벨에 따른 보정)
            EnchantRarity rarity = DetermineEnchantRarity(monsterLevel);
            
            // 인챈트 타입 결정
            EnchantType enchantType = GetRandomEnchantType();
            
            // 레벨 결정 (희귀도와 몬스터 레벨에 따라)
            int enchantLevel = DetermineEnchantLevel(rarity, monsterLevel);
            
            // 효과 값 계산
            float power = CalculateEnchantPower(enchantType, rarity, enchantLevel);
            
            var enchant = new EnchantData
            {
                enchantType = enchantType,
                rarity = rarity,
                level = enchantLevel,
                power = power,
                description = GenerateEnchantDescription(enchantType, rarity, enchantLevel),
                enchantId = GenerateEnchantId()
            };
            
            return enchant;
        }
        
        /// <summary>
        /// 인챈트 희귀도 결정
        /// </summary>
        private EnchantRarity DetermineEnchantRarity(int monsterLevel)
        {
            float random = Random.value;
            float levelBonus = monsterLevel * 0.005f; // 레벨당 0.5% 보너스
            
            // 전설 1%, 영웅 9%, 희귀 30%, 일반 60%
            if (random < 0.01f + levelBonus) return EnchantRarity.Legendary;
            if (random < 0.10f + levelBonus) return EnchantRarity.Epic;
            if (random < 0.40f + levelBonus) return EnchantRarity.Rare;
            return EnchantRarity.Common;
        }
        
        /// <summary>
        /// 랜덤 인챈트 타입 결정
        /// </summary>
        private EnchantType GetRandomEnchantType()
        {
            var enchantTypes = new EnchantType[]
            {
                EnchantType.Sharpness, EnchantType.CriticalHit, EnchantType.LifeSteal,
                EnchantType.Protection, EnchantType.Thorns, EnchantType.Regeneration,
                EnchantType.Fortune, EnchantType.Speed, EnchantType.MagicBoost, EnchantType.Durability
            };
            
            return enchantTypes[Random.Range(0, enchantTypes.Length)];
        }
        
        /// <summary>
        /// 인챈트 레벨 결정
        /// </summary>
        private int DetermineEnchantLevel(EnchantRarity rarity, int monsterLevel)
        {
            int baseLevel = rarity switch
            {
                EnchantRarity.Common => Random.Range(1, 3),      // 1-2레벨
                EnchantRarity.Rare => Random.Range(2, 4),        // 2-3레벨
                EnchantRarity.Epic => Random.Range(3, 5),        // 3-4레벨
                EnchantRarity.Legendary => Random.Range(4, 6),   // 4-5레벨
                _ => 1
            };
            
            // 몬스터 레벨 보정 (고레벨 몬스터일수록 높은 레벨 인챈트)
            if (monsterLevel >= 10 && Random.value < 0.3f) baseLevel = Mathf.Min(5, baseLevel + 1);
            
            return Mathf.Clamp(baseLevel, 1, 5);
        }
        
        /// <summary>
        /// 인챈트 효과 값 계산
        /// </summary>
        private float CalculateEnchantPower(EnchantType enchantType, EnchantRarity rarity, int level)
        {
            float basePower = enchantType switch
            {
                EnchantType.Sharpness => 5f,      // 5% per level
                EnchantType.CriticalHit => 2f,    // 2% per level
                EnchantType.LifeSteal => 3f,      // 3% per level
                EnchantType.Protection => 4f,     // 4% per level
                EnchantType.Thorns => 10f,        // 10% per level
                EnchantType.Regeneration => 2f,   // 2 HP/sec per level
                EnchantType.Fortune => 8f,        // 8% per level
                EnchantType.Speed => 6f,          // 6% per level
                EnchantType.MagicBoost => 7f,     // 7% per level
                EnchantType.Durability => 15f,    // 15% per level
                _ => 1f
            };
            
            float rarityMultiplier = rarity switch
            {
                EnchantRarity.Common => 1.0f,
                EnchantRarity.Rare => 1.3f,
                EnchantRarity.Epic => 1.6f,
                EnchantRarity.Legendary => 2.0f,
                _ => 1.0f
            };
            
            return basePower * level * rarityMultiplier;
        }
        
        /// <summary>
        /// 인챈트 북 아이템 생성
        /// </summary>
        private void CreateEnchantBookItem(Vector3 position, EnchantData enchant, string sourceName)
        {
            // 인챈트 북을 ItemInstance로 생성
            var enchantBookItem = CreateEnchantBookItemInstance(enchant, sourceName);
            
            // DroppedItem으로 생성
            if (enchantBookPrefab != null)
            {
                var droppedItem = Instantiate(enchantBookPrefab, position, Quaternion.identity);
                var droppedItemComponent = droppedItem.GetComponent<DroppedItem>();
                
                if (droppedItemComponent != null)
                {
                    droppedItemComponent.Initialize(enchantBookItem);
                    
                    // 네트워크 스폰
                    var networkObject = droppedItem.GetComponent<NetworkObject>();
                    if (networkObject != null)
                    {
                        networkObject.Spawn();
                    }
                }
            }
        }
        
        /// <summary>
        /// 인챈트 북 ItemInstance 생성
        /// </summary>
        private ItemInstance CreateEnchantBookItemInstance(EnchantData enchant, string sourceName)
        {
            // ItemDatabase 초기화 확인
            ItemDatabase.Initialize();
            
            // ItemDatabase에서 인챈트 북 데이터 가져오기
            var enchantBookData = ItemDatabase.GetItem("enchant_book");
            if (enchantBookData == null)
            {
                Debug.LogError("Enchant book item not found in database!");
                return null;
            }
            
            var enchantBook = new ItemInstance(enchantBookData, 1);
            
            // 인챈트 정보를 JSON으로 저장
            string enchantJson = JsonUtility.ToJson(enchant);
            enchantBook.CustomData["EnchantData"] = enchantJson;
            enchantBook.CustomData["SourceMonster"] = sourceName;
            
            return enchantBook;
        }
        
        /// <summary>
        /// 인챈트 설명 생성
        /// </summary>
        private string GenerateEnchantDescription(EnchantType enchantType, EnchantRarity rarity, int level)
        {
            string effect = enchantType switch
            {
                EnchantType.Sharpness => "무기의 예리함을 증가시킵니다",
                EnchantType.CriticalHit => "치명타 확률을 증가시킵니다",
                EnchantType.LifeSteal => "공격 시 체력을 흡수합니다",
                EnchantType.Protection => "받는 피해를 감소시킵니다",
                EnchantType.Thorns => "공격받을 때 반격 피해를 줍니다",
                EnchantType.Regeneration => "체력을 서서히 회복합니다",
                EnchantType.Fortune => "아이템 드롭률을 증가시킵니다",
                EnchantType.Speed => "이동 속도를 증가시킵니다",
                EnchantType.MagicBoost => "마법 공격력을 증가시킵니다",
                EnchantType.Durability => "아이템의 내구성을 증가시킵니다",
                _ => "알 수 없는 효과입니다"
            };
            
            string rarityDescription = rarity switch
            {
                EnchantRarity.Legendary => "전설적인 힘이 깃든 인챈트입니다.",
                EnchantRarity.Epic => "강력한 마력이 담긴 인챈트입니다.",
                EnchantRarity.Rare => "희귀한 효과를 지닌 인챈트입니다.",
                EnchantRarity.Common => "기본적인 효과를 지닌 인챈트입니다.",
                _ => ""
            };
            
            return $"{effect} {rarityDescription}";
        }
        
        /// <summary>
        /// 고유 인챈트 ID 생성
        /// </summary>
        private ulong GenerateEnchantId()
        {
            return (ulong)(System.DateTime.Now.Ticks + Random.Range(10000, 99999));
        }
        
        /// <summary>
        /// 드롭 통계 반환
        /// </summary>
        public static (int checks, int drops, float rate) GetDropStatistics()
        {
            float rate = totalDropChecks > 0 ? (float)successfulDrops / totalDropChecks : 0f;
            return (totalDropChecks, successfulDrops, rate);
        }
        
        /// <summary>
        /// 드롭 통계 리셋
        /// </summary>
        [ContextMenu("Reset Drop Statistics")]
        public static void ResetDropStatistics()
        {
            totalDropChecks = 0;
            successfulDrops = 0;
            Debug.Log("📊 Enchant drop statistics reset");
        }
    }
}