using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 아이템 드롭 시스템 - 몬스터 처치 시 아이템 드롭 관리
    /// 등급별 드롭 확률과 아이템 생성을 담당
    /// </summary>
    public class ItemDropSystem : NetworkBehaviour
    {
        [Header("드롭 설정")]
        [SerializeField] private bool enableItemDrop = true;
        [SerializeField] private float baseDropRate = 0.3f; // 30% 기본 드롭률
        [SerializeField] private int maxDropsPerKill = 3;
        [SerializeField] private float dropScatterRadius = 2f;
        
        [Header("등급별 드롭 확률")]
        [SerializeField] private float commonDropRate = 0.6f;      // 60%
        [SerializeField] private float uncommonDropRate = 0.25f;   // 25%
        [SerializeField] private float rareDropRate = 0.1f;        // 10%
        [SerializeField] private float epicDropRate = 0.04f;       // 4%
        [SerializeField] private float legendaryDropRate = 0.01f;  // 1%
        
        [Header("레벨별 드롭 보너스")]
        [SerializeField] private float dropRatePerLevel = 0.01f; // 레벨당 1% 증가
        [SerializeField] private float maxLevelBonus = 0.5f;     // 최대 50% 보너스
        
        [Header("골드 드롭")]
        [SerializeField] private bool enableGoldDrop = true;
        [SerializeField] private int baseGoldAmount = 10;
        [SerializeField] private float goldVariance = 0.3f; // ±30% 변동
        
        // 드롭된 아이템 관리
        private List<DroppedItem> droppedItems = new List<DroppedItem>();
        
        // 컴포넌트 참조
        private PlayerStatsManager statsManager;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            statsManager = GetComponent<PlayerStatsManager>();
            
            // 아이템 데이터베이스 초기화
            ItemDatabase.Initialize();
        }
        
        /// <summary>
        /// 몬스터 처치 시 아이템 드롭 체크
        /// </summary>
        public void CheckItemDrop(Vector3 dropPosition, int monsterLevel, string monsterType, PlayerController killer)
        {
            bool isServer = NetworkManager.Singleton != null ? NetworkManager.Singleton.IsServer : true;
            if (!isServer || !enableItemDrop) return;
            
            float finalDropRate = CalculateFinalDropRate(killer);
            
            // 드롭 여부 결정
            if (Random.Range(0f, 1f) > finalDropRate) return;
            
            // 드롭할 아이템 수 결정
            int dropCount = Random.Range(1, maxDropsPerKill + 1);
            
            for (int i = 0; i < dropCount; i++)
            {
                // 몬스터 레벨에 따른 최대 등급 결정
                ItemGrade maxGrade = GetMaxGradeForLevel(monsterLevel);
                
                // 랜덤 아이템 드롭
                var droppedItem = ItemDatabase.GetRandomItemDrop(maxGrade);
                if (droppedItem != null)
                {
                    CreateItemDrop(dropPosition, droppedItem, killer);
                }
            }
            
            // 골드 드롭
            if (enableGoldDrop)
            {
                CreateGoldDrop(dropPosition, monsterLevel, killer);
            }
        }
        
        /// <summary>
        /// 플레이어 사망 시 아이템 드롭 (DeathManager에서 호출)
        /// </summary>
        public void DropPlayerItems(Vector3 dropPosition, List<ItemInstance> items)
        {
            bool isServer = NetworkManager.Singleton != null ? NetworkManager.Singleton.IsServer : true;
            if (!isServer) return;
            
            foreach (var item in items)
            {
                if (item.ItemData.IsDroppable)
                {
                    CreateItemDrop(dropPosition, item, null);
                }
            }
        }
        
        /// <summary>
        /// 아이템 드롭 생성
        /// </summary>
        private void CreateItemDrop(Vector3 position, ItemInstance itemInstance, PlayerController dropper)
        {
            // NetworkManager를 통한 서버 체크
            bool isServer = NetworkManager.Singleton != null ? NetworkManager.Singleton.IsServer : true;
            if (!isServer) 
            {
                Debug.LogWarning($"🎁 CreateItemDrop blocked - not server for {itemInstance.ItemData.ItemName}");
                return;
            }
            
            // 드롭 위치 계산 (랜덤 스캐터)
            Vector2 randomOffset = Random.insideUnitCircle * dropScatterRadius;
            Vector3 finalPosition = position + new Vector3(randomOffset.x, randomOffset.y, 0);
            
            // 드롭된 아이템 오브젝트 생성
            GameObject dropObject = new GameObject($"DroppedItem_{itemInstance.ItemData.ItemName}");
            dropObject.transform.position = finalPosition;
            
            // ItemDrop 컴포넌트 추가
            var itemDrop = dropObject.AddComponent<ItemDrop>();
            itemDrop.SetItemInstance(itemInstance);
            itemDrop.SetDropPosition(finalPosition);
            
            Debug.Log($"💎 Item dropped: {itemInstance.ItemData.ItemName} (Grade: {itemInstance.ItemData.Grade})");
        }
        
        
        /// <summary>
        /// 골드 드롭 생성
        /// </summary>
        private void CreateGoldDrop(Vector3 position, int monsterLevel, PlayerController killer)
        {
            bool isServer = NetworkManager.Singleton != null ? NetworkManager.Singleton.IsServer : true;
            if (!isServer) return;
            
            // 골드량 계산
            int goldAmount = CalculateGoldDrop(monsterLevel, killer);
            if (goldAmount <= 0) return;
            
            // 골드 아이템 생성 (특별한 아이템으로 처리)
            var goldItem = CreateGoldItem(goldAmount);
            CreateItemDrop(position, goldItem, killer);
        }
        
        /// <summary>
        /// 골드 아이템 생성
        /// </summary>
        private ItemInstance CreateGoldItem(int amount)
        {
            // 임시 골드 ItemData 생성
            var goldData = ScriptableObject.CreateInstance<ItemData>();
            
            // 리플렉션을 사용하여 골드 데이터 설정
            var itemType = typeof(ItemData);
            itemType.GetField("itemId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(goldData, "gold_coin");
            itemType.GetField("itemName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(goldData, $"{amount} 골드");
            itemType.GetField("description", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(goldData, "게임 내 화폐");
            itemType.GetField("itemType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(goldData, ItemType.Other);
            itemType.GetField("grade", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(goldData, ItemGrade.Common);
            itemType.GetField("sellPrice", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(goldData, (long)amount);
            itemType.GetField("gradeColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(goldData, Color.yellow);
            
            return new ItemInstance(goldData, 1);
        }
        
        /// <summary>
        /// 최종 드롭률 계산 (운 스탯 포함)
        /// </summary>
        private float CalculateFinalDropRate(PlayerController killer)
        {
            float dropRate = baseDropRate;
            
            if (killer?.GetComponent<PlayerStatsManager>()?.CurrentStats != null)
            {
                var stats = killer.GetComponent<PlayerStatsManager>().CurrentStats;
                
                // 운 스탯에 따른 드롭률 증가 (LUK * 0.01%)
                dropRate += stats.TotalLUK * 0.0001f;
                
                // 레벨에 따른 드롭률 증가
                float levelBonus = stats.CurrentLevel * dropRatePerLevel;
                levelBonus = Mathf.Min(levelBonus, maxLevelBonus);
                dropRate += levelBonus;
            }
            
            return Mathf.Clamp01(dropRate);
        }
        
        /// <summary>
        /// 몬스터 레벨에 따른 최대 등급 결정
        /// </summary>
        private ItemGrade GetMaxGradeForLevel(int monsterLevel)
        {
            return monsterLevel switch
            {
                <= 3 => ItemGrade.Common,
                <= 6 => ItemGrade.Uncommon,
                <= 9 => ItemGrade.Rare,
                <= 12 => ItemGrade.Epic,
                _ => ItemGrade.Legendary
            };
        }
        
        /// <summary>
        /// 골드 드롭량 계산
        /// </summary>
        private int CalculateGoldDrop(int monsterLevel, PlayerController killer)
        {
            int baseAmount = baseGoldAmount + (monsterLevel * 5);
            
            // 변동성 적용
            float variance = Random.Range(-goldVariance, goldVariance);
            int finalAmount = Mathf.RoundToInt(baseAmount * (1f + variance));
            
            // 운 스탯 보너스
            if (killer?.GetComponent<PlayerStatsManager>()?.CurrentStats != null)
            {
                var stats = killer.GetComponent<PlayerStatsManager>().CurrentStats;
                float luckBonus = stats.TotalLUK * 0.01f; // LUK당 1% 증가
                finalAmount = Mathf.RoundToInt(finalAmount * (1f + luckBonus));
            }
            
            return Mathf.Max(1, finalAmount);
        }
        
        /// <summary>
        /// 특정 위치에 아이템 드롭 (공용 메서드)
        /// </summary>
        public void DropItemAtPosition(Vector3 position, ItemInstance itemInstance, PlayerController dropper)
        {
            // NetworkManager를 통한 서버 체크 (더 안전함)
            bool isServer = NetworkManager.Singleton != null ? NetworkManager.Singleton.IsServer : true;
            Debug.Log($"🎁 DropItemAtPosition: IsServer={isServer}, NetworkManager.IsServer={NetworkManager.Singleton?.IsServer}, this.IsServer={IsServer}");
            
            if (!isServer) 
            {
                Debug.LogWarning($"🎁 DropItemAtPosition blocked - not server for {itemInstance.ItemData.ItemName}");
                return;
            }
            
            Debug.Log($"🎁 DropItemAtPosition proceeding: {itemInstance.ItemData.ItemName} at {position}");
            CreateItemDrop(position, itemInstance, dropper);
        }
        
        /// <summary>
        /// 아이템 픽업 처리
        /// </summary>
        public void PickupItem(DroppedItem droppedItem, PlayerController picker)
        {
            bool isServer = NetworkManager.Singleton != null ? NetworkManager.Singleton.IsServer : true;
            if (!isServer) return;
            
            if (droppedItem == null || picker == null) return;
            
            var itemInstance = droppedItem.ItemInstance;
            
            // 골드 아이템인지 확인
            if (itemInstance.ItemId == "gold_coin")
            {
                // 골드 추가
                var statsManager = picker.GetComponent<PlayerStatsManager>();
                if (statsManager != null)
                {
                    statsManager.ChangeGold(itemInstance.ItemData.SellPrice);
                    
                    // 픽업 알림
                    NotifyItemPickedUpClientRpc(picker.OwnerClientId, $"+{itemInstance.ItemData.SellPrice} 골드", Color.yellow);
                }
            }
            else
            {
                // 일반 아이템 - 인벤토리에 추가 (추후 인벤토리 시스템에서 구현)
                // 현재는 즉시 골드로 변환
                var statsManager = picker.GetComponent<PlayerStatsManager>();
                if (statsManager != null)
                {
                    long sellValue = itemInstance.ItemData.GetTotalValue();
                    statsManager.ChangeGold(sellValue);
                    
                    // 픽업 알림
                    NotifyItemPickedUpClientRpc(picker.OwnerClientId, $"{itemInstance.ItemData.ItemName} (+{sellValue} 골드)", itemInstance.ItemData.GradeColor);
                }
            }
            
            // 드롭된 아이템 목록에서 제거
            droppedItems.Remove(droppedItem);
            
            // 오브젝트 제거
            if (droppedItem.NetworkObject != null)
            {
                droppedItem.NetworkObject.Despawn();
            }
        }
        
        /// <summary>
        /// 만료된 아이템들 정리 (5분 후 자동 삭제)
        /// </summary>
        private void Update()
        {
            bool isServer = NetworkManager.Singleton != null ? NetworkManager.Singleton.IsServer : true;
            if (!isServer) return;
            
            float currentTime = Time.time;
            var itemsToRemove = new List<DroppedItem>();
            
            foreach (var droppedItem in droppedItems)
            {
                if (droppedItem != null && currentTime - droppedItem.DropTime > 300f) // 5분
                {
                    itemsToRemove.Add(droppedItem);
                }
            }
            
            foreach (var item in itemsToRemove)
            {
                droppedItems.Remove(item);
                if (item.NetworkObject != null)
                {
                    item.NetworkObject.Despawn();
                }
            }
        }
        
        /// <summary>
        /// 아이템 드롭 알림 (클라이언트)
        /// </summary>
        [ClientRpc]
        private void NotifyItemDroppedClientRpc(Vector3 position, string itemName, Color color)
        {
            // 드롭 이펙트 재생 (추후 이펙트 시스템에서 구현)
            Debug.Log($"💎 {itemName} dropped at {position}");
        }
        
        /// <summary>
        /// 아이템 픽업 알림 (클라이언트)
        /// </summary>
        [ClientRpc]
        private void NotifyItemPickedUpClientRpc(ulong targetClientId, string message, Color color)
        {
            if (NetworkManager.Singleton.LocalClientId == targetClientId)
            {
                Debug.Log($"📦 {message}");
                // 추후 UI 시스템에서 픽업 알림 표시
            }
        }
        
        /// <summary>
        /// 드롭 확률 설정 (디버그용)
        /// </summary>
        [ContextMenu("Test Item Drop")]
        private void TestItemDrop()
        {
            if (IsServer)
            {
                CheckItemDrop(transform.position, 5, "TestMonster", GetComponent<PlayerController>());
            }
        }
        
        /// <summary>
        /// 드롭 통계 로그
        /// </summary>
        public void LogDropStatistics()
        {
            Debug.Log($"=== Item Drop Statistics ===");
            Debug.Log($"Base Drop Rate: {baseDropRate:P1}");
            Debug.Log($"Active Dropped Items: {droppedItems.Count}");
            Debug.Log($"Drop Scatter Radius: {dropScatterRadius}m");
        }
    }
}