using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 아이템 흩뿌리기 시스템 - 사망 시 모든 아이템 드롭
    /// 착용 장비, 인벤토리, 골드 모두 흩어져서 드롭
    /// </summary>
    public class ItemScatter : NetworkBehaviour
    {
        [Header("Scatter Settings")]
        [SerializeField] private float scatterRadius = 5.0f;
        [SerializeField] private float goldScatterRadius = 3.0f;
        [SerializeField] private int maxScatterAttempts = 20;
        [SerializeField] private LayerMask obstacleLayerMask = 1;
        
        [Header("Item Settings")]
        [SerializeField] private float itemDespawnTime = 3600f; // 1시간
        [SerializeField] private GameObject goldDropPrefab;
        [SerializeField] private GameObject itemDropPrefab;
        [SerializeField] private GameObject rareItemDropPrefab;
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject scatterEffectPrefab;
        [SerializeField] private float effectDelay = 0.1f;
        
        // 컴포넌트 참조
        private PlayerStatsManager statsManager;
        
        // 드롭된 아이템들 추적
        private List<GameObject> droppedItems = new List<GameObject>();
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            statsManager = GetComponent<PlayerStatsManager>();
        }
        
        /// <summary>
        /// 모든 아이템 흩뿌리기 실행
        /// </summary>
        public void ScatterAllItems(Vector3 deathPosition, float radius = 0f)
        {
            if (!IsServer)
            {
                Debug.LogError("ItemScatter must be called on server!");
                return;
            }
            
            float actualRadius = radius > 0 ? radius : scatterRadius;
            
            Debug.Log($"💎 Scattering all items from {gameObject.name} at {deathPosition}");
            
            StartCoroutine(ScatterItemsSequence(deathPosition, actualRadius));
        }
        
        /// <summary>
        /// 아이템 흩뿌리기 시퀀스
        /// </summary>
        private IEnumerator ScatterItemsSequence(Vector3 deathPosition, float radius)
        {
            // 1. 골드 드롭
            yield return StartCoroutine(ScatterGold(deathPosition));
            
            // 2. 착용 장비 드롭
            yield return StartCoroutine(ScatterEquippedItems(deathPosition, radius));
            
            // 3. 인벤토리 아이템 드롭
            yield return StartCoroutine(ScatterInventoryItems(deathPosition, radius));
            
            // 4. 드롭된 아이템들 자동 소멸 타이머 시작
            StartItemDespawnTimers();
            
            Debug.Log($"✅ Item scattering completed. {droppedItems.Count} items dropped.");
        }
        
        /// <summary>
        /// 골드 흩뿌리기
        /// </summary>
        private IEnumerator ScatterGold(Vector3 deathPosition)
        {
            if (statsManager?.CurrentStats == null) yield break;
            
            long totalGold = statsManager.CurrentStats.Gold;
            if (totalGold <= 0) yield break;
            
            Debug.Log($"💰 Scattering {totalGold} gold");
            
            // 골드를 여러 뭉치로 나누어 드롭
            int goldPiles = Mathf.Min(10, Mathf.CeilToInt(totalGold / 100f)); // 최대 10개 뭉치
            long goldPerPile = totalGold / goldPiles;
            long remainingGold = totalGold % goldPiles;
            
            for (int i = 0; i < goldPiles; i++)
            {
                long goldAmount = goldPerPile + (i == 0 ? remainingGold : 0);
                Vector3 goldPosition = GetScatterPosition(deathPosition, goldScatterRadius);
                
                CreateGoldDrop(goldPosition, goldAmount);
                
                // 시각적 효과를 위한 짧은 딜레이
                yield return new WaitForSeconds(effectDelay);
            }
        }
        
        /// <summary>
        /// 착용 장비 흩뿌리기
        /// </summary>
        private IEnumerator ScatterEquippedItems(Vector3 deathPosition, float radius)
        {
            // 추후 장비 시스템과 연동
            // 현재는 임시 데이터로 구현
            
            var equippedItems = GetEquippedItems();
            
            foreach (var item in equippedItems)
            {
                Vector3 itemPosition = GetScatterPosition(deathPosition, radius);
                CreateItemDrop(itemPosition, item);
                
                yield return new WaitForSeconds(effectDelay);
            }
            
            Debug.Log($"⚔️ Scattered {equippedItems.Count} equipped items");
        }
        
        /// <summary>
        /// 인벤토리 아이템 흩뿌리기 (실제 인벤토리 시스템과 연동)
        /// </summary>
        private IEnumerator ScatterInventoryItems(Vector3 deathPosition, float radius)
        {
            var inventoryItems = GetInventoryItems();
            
            foreach (var item in inventoryItems)
            {
                // 희귀한 아이템일수록 더 멀리 흩어짐
                float itemRadius = radius * GetGradeMultiplier(item.ItemData.Grade);
                Vector3 itemPosition = GetScatterPosition(deathPosition, itemRadius);
                
                CreateItemDrop(itemPosition, item);
                
                yield return new WaitForSeconds(effectDelay);
            }
            
            Debug.Log($"🎒 Scattered {inventoryItems.Count} inventory items");
        }
        
        /// <summary>
        /// 흩뿌리기 위치 계산
        /// </summary>
        private Vector3 GetScatterPosition(Vector3 center, float radius)
        {
            for (int attempt = 0; attempt < maxScatterAttempts; attempt++)
            {
                // 랜덤한 방향과 거리
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(0.5f, radius);
                
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance,
                    0f
                );
                
                Vector3 targetPosition = center + offset;
                
                // 장애물 체크
                if (!IsPositionBlocked(targetPosition))
                {
                    return targetPosition;
                }
            }
            
            // 모든 시도가 실패하면 중심 위치 사용
            Debug.LogWarning("Could not find clear scatter position, using center");
            return center;
        }
        
        /// <summary>
        /// 위치가 막혀있는지 확인
        /// </summary>
        private bool IsPositionBlocked(Vector3 position)
        {
            Collider2D collision = Physics2D.OverlapCircle(position, 0.3f, obstacleLayerMask);
            return collision != null;
        }
        
        /// <summary>
        /// 골드 드롭 생성
        /// </summary>
        private void CreateGoldDrop(Vector3 position, long amount)
        {
            GameObject goldDrop = Instantiate(goldDropPrefab ?? CreateDefaultGoldPrefab(), position, Quaternion.identity);
            
            // GoldDrop 컴포넌트 설정
            var goldComponent = goldDrop.GetComponent<GoldDrop>();
            if (goldComponent == null)
            {
                goldComponent = goldDrop.AddComponent<GoldDrop>();
            }
            
            goldComponent.SetGoldAmount(amount);
            
            // 네트워크 스폰
            var networkObject = goldDrop.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
            }
            
            droppedItems.Add(goldDrop);
            
            // 시각적 효과
            PlayScatterEffectClientRpc(position);
            
            Debug.Log($"💰 Created gold drop: {amount} at {position}");
        }
        
        /// <summary>
        /// 아이템 드롭 생성 (ItemInstance 사용)
        /// </summary>
        private void CreateItemDrop(Vector3 position, ItemInstance item)
        {
            GameObject prefab = GetItemDropPrefab(item.ItemData.Grade);
            GameObject itemDrop = Instantiate(prefab, position, Quaternion.identity);
            
            // ItemDrop 컴포넌트 설정
            var itemComponent = itemDrop.GetComponent<ItemDrop>();
            if (itemComponent == null)
            {
                itemComponent = itemDrop.AddComponent<ItemDrop>();
            }
            
            itemComponent.SetItemInstance(item);
            
            // 네트워크 스폰
            var networkObject = itemDrop.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
            }
            
            droppedItems.Add(itemDrop);
            
            // 시각적 효과
            PlayScatterEffectClientRpc(position);
            
            Debug.Log($"⚔️ Created item drop: {item.ItemData.ItemName} at {position}");
        }
        
        /// <summary>
        /// 등급별 흩뿌리기 배율 (통합된 아이템 시스템 사용)
        /// </summary>
        private float GetGradeMultiplier(ItemGrade grade)
        {
            return grade switch
            {
                ItemGrade.Common => 1.0f,
                ItemGrade.Uncommon => 1.2f,
                ItemGrade.Rare => 1.5f,
                ItemGrade.Epic => 2.0f,
                ItemGrade.Legendary => 3.0f,
                _ => 1.0f
            };
        }
        
        /// <summary>
        /// 등급별 아이템 드롭 프리팹 선택 (통합된 아이템 시스템 사용)
        /// </summary>
        private GameObject GetItemDropPrefab(ItemGrade grade)
        {
            return grade >= ItemGrade.Rare && rareItemDropPrefab != null 
                ? rareItemDropPrefab 
                : (itemDropPrefab ?? CreateDefaultItemPrefab());
        }
        
        /// <summary>
        /// 흩뿌리기 효과 재생
        /// </summary>
        [ClientRpc]
        private void PlayScatterEffectClientRpc(Vector3 position)
        {
            if (scatterEffectPrefab != null)
            {
                var effect = Instantiate(scatterEffectPrefab, position, Quaternion.identity);
                Destroy(effect, 2f);
            }
        }
        
        /// <summary>
        /// 아이템 자동 소멸 타이머 시작
        /// </summary>
        private void StartItemDespawnTimers()
        {
            foreach (var item in droppedItems)
            {
                if (item != null)
                {
                    StartCoroutine(DespawnItemAfterTime(item));
                }
            }
        }
        
        /// <summary>
        /// 시간 후 아이템 소멸
        /// </summary>
        private IEnumerator DespawnItemAfterTime(GameObject item)
        {
            yield return new WaitForSeconds(itemDespawnTime);
            
            if (item != null)
            {
                var networkObject = item.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned)
                {
                    networkObject.Despawn();
                }
                else
                {
                    Destroy(item);
                }
                
                droppedItems.Remove(item);
            }
        }
        
        /// <summary>
        /// 현재 착용 장비 가져오기 (실제 장비 시스템과 연동)
        /// </summary>
        private List<ItemInstance> GetEquippedItems()
        {
            var equippedItems = new List<ItemInstance>();
            
            // 장비 아이템 가져오기 (NetworkBehaviour 기반 컴포넌트 탐색)
            var allComponents = GetComponents<NetworkBehaviour>();
            bool foundEquipmentManager = false;
            
            foreach (var component in allComponents)
            {
                if (component.GetType().Name == "EquipmentManager")
                {
                    var getAllEquippedItemsMethod = component.GetType().GetMethod("GetAllEquippedItems");
                    if (getAllEquippedItemsMethod != null)
                    {
                        var result = getAllEquippedItemsMethod.Invoke(component, null);
                        if (result is List<ItemInstance> items)
                        {
                            equippedItems.AddRange(items);
                        }
                    }
                    foundEquipmentManager = true;
                    break;
                }
            }
            
            if (!foundEquipmentManager)
            {
                // EquipmentManager가 없는 경우 빈 리스트 반환
                Debug.LogWarning("EquipmentManager not found on player - no equipped items to scatter");
            }
            
            return equippedItems;
        }
        
        /// <summary>
        /// 현재 인벤토리 아이템 가져오기 (실제 인벤토리 시스템과 연동)
        /// </summary>
        private List<ItemInstance> GetInventoryItems()
        {
            var inventoryItems = new List<ItemInstance>();
            
            // InventoryManager에서 인벤토리 아이템 가져오기
            var inventoryManager = GetComponent<InventoryManager>();
            if (inventoryManager != null && inventoryManager.Inventory != null)
            {
                foreach (var slot in inventoryManager.Inventory.Slots)
                {
                    if (!slot.IsEmpty)
                    {
                        inventoryItems.Add(slot.Item);
                    }
                }
            }
            
            return inventoryItems;
        }
        
        /// <summary>
        /// 기본 골드 프리팹 생성
        /// </summary>
        private GameObject CreateDefaultGoldPrefab()
        {
            var gold = new GameObject("GoldDrop");
            gold.AddComponent<SpriteRenderer>().color = Color.yellow;
            gold.AddComponent<CircleCollider2D>().isTrigger = true;
            gold.AddComponent<NetworkObject>();
            return gold;
        }
        
        /// <summary>
        /// 기본 아이템 프리팹 생성
        /// </summary>
        private GameObject CreateDefaultItemPrefab()
        {
            var item = new GameObject("ItemDrop");
            item.AddComponent<SpriteRenderer>().color = Color.white;
            item.AddComponent<CircleCollider2D>().isTrigger = true;
            item.AddComponent<NetworkObject>();
            return item;
        }
    }
    
    // 중복 정의 제거 - 기존 Items/ItemData.cs의 시스템 사용
    // ItemData, ItemGrade, ItemType은 Items 폴더에서 정의됨
}