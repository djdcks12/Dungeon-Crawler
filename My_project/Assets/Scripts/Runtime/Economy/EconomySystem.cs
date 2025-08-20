using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 경제 시스템 관리
    /// 골드 드롭량, 귀환 비용, 상점 시스템, 수리 시스템 등을 관리
    /// </summary>
    public class EconomySystem : NetworkBehaviour
    {
        [Header("골드 드롭 설정")]
        [SerializeField] private float baseGoldDropRate = 1.0f;
        [SerializeField] private float floorGoldMultiplier = 0.2f; // 층당 20% 증가
        [SerializeField] private int minGoldPerMonster = 5;
        [SerializeField] private int maxGoldPerMonster = 15;
        
        [Header("귀환 비용 설정")]
        [SerializeField] private int baseReturnCost = 100;
        [SerializeField] private float returnCostMultiplier = 1.5f;
        [SerializeField] private int maxReturnCost = 10000; // 최대 귀환 비용
        
        [Header("상점 설정")]
        [SerializeField] private bool enableShop = true;
        [SerializeField] private float shopPriceMultiplier = 1.2f; // 상점 가격 배율
        
        [Header("수리 시스템 설정")]
        [SerializeField] private bool enableRepairSystem = true;
        [SerializeField] private float repairCostPerDurability = 2.0f; // 내구도 1당 수리 비용
        
        // 플레이어별 귀환 횟수 추적 (서버에서만 관리)
        private Dictionary<ulong, int> playerReturnCounts = new Dictionary<ulong, int>();
        
        // 상점 아이템 리스트
        private List<ShopItem> shopItems = new List<ShopItem>();
        
        // 싱글톤 패턴
        private static EconomySystem instance;
        public static EconomySystem Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<EconomySystem>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("EconomySystem");
                        instance = go.AddComponent<EconomySystem>();
                    }
                }
                return instance;
            }
        }
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeShopItems();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsServer)
            {
                // 서버에서 경제 시스템 초기화
                ResetReturnCounts();
            }
        }
        
        /// <summary>
        /// 상점 아이템 초기화
        /// </summary>
        private void InitializeShopItems()
        {
            shopItems.Clear();
            
            // 기본 무기
            shopItems.Add(new ShopItem
            {
                itemId = "basic_sword",
                itemName = "기본 검",
                basePrice = 100,
                itemType = ShopItemType.Weapon,
                description = "기본적인 한손검"
            });
            
            shopItems.Add(new ShopItem
            {
                itemId = "basic_staff",
                itemName = "기본 지팡이",
                basePrice = 120,
                itemType = ShopItemType.Weapon,
                description = "기본적인 마법 지팡이"
            });
            
            // 기본 방어구
            shopItems.Add(new ShopItem
            {
                itemId = "basic_helmet",
                itemName = "기본 투구",
                basePrice = 80,
                itemType = ShopItemType.Armor,
                description = "기본적인 머리 방어구"
            });
            
            shopItems.Add(new ShopItem
            {
                itemId = "basic_chest",
                itemName = "기본 갑옷",
                basePrice = 150,
                itemType = ShopItemType.Armor,
                description = "기본적인 가슴 방어구"
            });
            
            // 소모품
            shopItems.Add(new ShopItem
            {
                itemId = "health_potion",
                itemName = "체력 물약",
                basePrice = 25,
                itemType = ShopItemType.Consumable,
                description = "HP 50 회복"
            });
            
            shopItems.Add(new ShopItem
            {
                itemId = "mana_potion",
                itemName = "마나 물약",
                basePrice = 30,
                itemType = ShopItemType.Consumable,
                description = "MP 30 회복"
            });
            
            // 수리 도구
            shopItems.Add(new ShopItem
            {
                itemId = "repair_kit",
                itemName = "수리 키트",
                basePrice = 50,
                itemType = ShopItemType.Tool,
                description = "장비 내구도 10 수리"
            });
            
            Debug.Log($"✅ Economy System initialized with {shopItems.Count} shop items");
        }
        
        /// <summary>
        /// 층별 골드 드롭량 계산
        /// </summary>
        public int CalculateGoldDrop(int dungeonFloor, bool isEliteMonster = false, bool isBossMonster = false)
        {
            float floorMultiplier = 1.0f + (dungeonFloor - 1) * floorGoldMultiplier;
            int baseGold = Random.Range(minGoldPerMonster, maxGoldPerMonster + 1);
            
            // 몬스터 타입별 배율
            if (isBossMonster)
                baseGold *= 5; // 보스는 5배
            else if (isEliteMonster)
                baseGold *= 2; // 엘리트는 2배
            
            int finalGold = Mathf.RoundToInt(baseGold * floorMultiplier * baseGoldDropRate);
            
            Debug.Log($"💰 Gold drop calculated: Floor {dungeonFloor}, Base {baseGold}, Final {finalGold}");
            return finalGold;
        }
        
        /// <summary>
        /// 귀환 비용 계산
        /// </summary>
        public int CalculateReturnCost(ulong clientId)
        {
            int returnCount = GetPlayerReturnCount(clientId);
            
            // 첫 귀환은 100골드, 이후 1.5배씩 증가
            float cost = baseReturnCost * Mathf.Pow(returnCostMultiplier, returnCount);
            int finalCost = Mathf.Min(Mathf.RoundToInt(cost), maxReturnCost);
            
            Debug.Log($"🚪 Return cost for Player {clientId}: {finalCost} gold (Return #{returnCount + 1})");
            return finalCost;
        }
        
        /// <summary>
        /// 플레이어 귀환 처리
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ProcessPlayerReturnServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            
            // 플레이어 스탯 매니저 찾기
            var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            if (playerObject == null) return;
            
            var statsManager = playerObject.GetComponent<PlayerStatsManager>();
            if (statsManager == null) return;
            
            // 귀환 비용 계산
            int returnCost = CalculateReturnCost(clientId);
            
            // 골드 충분한지 확인
            if (statsManager.CurrentStats.CurrentGold < returnCost)
            {
                // 골드 부족 알림
                NotifyInsufficientGoldClientRpc(clientId, returnCost);
                return;
            }
            
            // 골드 차감
            statsManager.ChangeGold(-returnCost);
            
            // 귀환 횟수 증가
            IncrementPlayerReturnCount(clientId);
            
            // 플레이어를 안전 지역으로 이동 (실제 구현에서는 씬 전환 등)
            TeleportPlayerToSafeZone(playerObject);
            
            Debug.Log($"🚪 Player {clientId} returned to safe zone. Cost: {returnCost} gold");
        }
        
        /// <summary>
        /// 상점에서 아이템 구매
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void PurchaseItemServerRpc(string itemId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            
            var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            if (playerObject == null) return;
            
            var statsManager = playerObject.GetComponent<PlayerStatsManager>();
            var inventoryManager = playerObject.GetComponent<InventoryManager>();
            
            if (statsManager == null || inventoryManager == null) return;
            
            // 상점 아이템 찾기
            var shopItem = shopItems.Find(item => item.itemId == itemId);
            if (shopItem.itemId == null)
            {
                Debug.LogError($"Shop item not found: {itemId}");
                return;
            }
            
            // 가격 계산
            int finalPrice = Mathf.RoundToInt(shopItem.basePrice * shopPriceMultiplier);
            
            // 골드 확인
            if (statsManager.CurrentStats.CurrentGold < finalPrice)
            {
                NotifyInsufficientGoldClientRpc(clientId, finalPrice);
                return;
            }
            
            // 인벤토리 공간 확인
            if (!inventoryManager.HasSpace())
            {
                NotifyInventoryFullClientRpc(clientId);
                return;
            }
            
            // 구매 처리
            statsManager.ChangeGold(-finalPrice);
            inventoryManager.AddItemServerRpc(itemId, 1);
            
            // 구매 성공 알림
            NotifyPurchaseSuccessClientRpc(clientId, itemId, finalPrice);
            
            Debug.Log($"🛒 Player {clientId} purchased {shopItem.itemName} for {finalPrice} gold");
        }
        
        /// <summary>
        /// 장비 수리
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RepairItemServerRpc(int inventorySlot, ServerRpcParams rpcParams = default)
        {
            if (!enableRepairSystem) return;
            
            ulong clientId = rpcParams.Receive.SenderClientId;
            
            var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            if (playerObject == null) return;
            
            var statsManager = playerObject.GetComponent<PlayerStatsManager>();
            var inventoryManager = playerObject.GetComponent<InventoryManager>();
            
            if (statsManager == null || inventoryManager == null) return;
            
            // 아이템 가져오기 (실제로는 InventoryManager에서 아이템 정보 가져와야 함)
            // 임시로 수리 비용 계산
            int durabilityToRepair = 10; // 수리할 내구도
            int repairCost = Mathf.RoundToInt(durabilityToRepair * repairCostPerDurability);
            
            // 골드 확인
            if (statsManager.CurrentStats.CurrentGold < repairCost)
            {
                NotifyInsufficientGoldClientRpc(clientId, repairCost);
                return;
            }
            
            // 수리 처리
            statsManager.ChangeGold(-repairCost);
            // 실제 아이템 수리는 InventoryManager에서 처리
            // inventoryManager.RepairItem(inventorySlot, durabilityToRepair);
            
            // 수리 성공 알림
            NotifyRepairSuccessClientRpc(clientId, repairCost);
            
            Debug.Log($"🔧 Player {clientId} repaired item for {repairCost} gold");
        }
        
        /// <summary>
        /// 플레이어 귀환 횟수 가져오기
        /// </summary>
        private int GetPlayerReturnCount(ulong clientId)
        {
            if (playerReturnCounts.ContainsKey(clientId))
            {
                return playerReturnCounts[clientId];
            }
            return 0;
        }
        
        /// <summary>
        /// 플레이어 귀환 횟수 증가
        /// </summary>
        private void IncrementPlayerReturnCount(ulong clientId)
        {
            if (playerReturnCounts.ContainsKey(clientId))
            {
                playerReturnCounts[clientId]++;
            }
            else
            {
                playerReturnCounts.Add(clientId, 1);
            }
        }
        
        /// <summary>
        /// 귀환 횟수 리셋 (새 던전 시작 시)
        /// </summary>
        public void ResetReturnCounts()
        {
            if (IsServer)
            {
                playerReturnCounts.Clear();
                Debug.Log("🔄 Player return counts reset");
            }
        }
        
        /// <summary>
        /// 플레이어를 안전 지역으로 이동
        /// </summary>
        private void TeleportPlayerToSafeZone(NetworkObject playerObject)
        {
            // 안전 지역 좌표 (실제로는 게임에 맞게 설정)
            Vector3 safePosition = Vector3.zero;
            playerObject.transform.position = safePosition;
            
            // 던전 UI 숨기기
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UnloadDungeonUI();
            }
        }
        
        /// <summary>
        /// 상점 아이템 목록 가져오기
        /// </summary>
        public List<ShopItem> GetShopItems()
        {
            return new List<ShopItem>(shopItems);
        }
        
        // ClientRpc 메서드들
        [ClientRpc]
        private void NotifyInsufficientGoldClientRpc(ulong targetClientId, int requiredGold)
        {
            if (NetworkManager.Singleton.LocalClientId == targetClientId)
            {
                Debug.Log($"💸 골드가 부족합니다! 필요: {requiredGold} 골드");
            }
        }
        
        [ClientRpc]
        private void NotifyInventoryFullClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId == targetClientId)
            {
                Debug.Log("🎒 인벤토리가 가득찼습니다!");
            }
        }
        
        [ClientRpc]
        private void NotifyPurchaseSuccessClientRpc(ulong targetClientId, string itemId, int price)
        {
            if (NetworkManager.Singleton.LocalClientId == targetClientId)
            {
                Debug.Log($"✅ 구매 완료: {itemId} - {price} 골드");
            }
        }
        
        [ClientRpc]
        private void NotifyRepairSuccessClientRpc(ulong targetClientId, int cost)
        {
            if (NetworkManager.Singleton.LocalClientId == targetClientId)
            {
                Debug.Log($"🔧 수리 완료: {cost} 골드");
            }
        }
        
        /// <summary>
        /// 디버그 정보
        /// </summary>
        [ContextMenu("Show Economy Debug Info")]
        private void ShowDebugInfo()
        {
            Debug.Log("=== Economy System Debug ===");
            Debug.Log($"Shop Items: {shopItems.Count}");
            Debug.Log($"Player Return Counts: {playerReturnCounts.Count}");
            
            foreach (var returnCount in playerReturnCounts)
            {
                Debug.Log($"- Player {returnCount.Key}: {returnCount.Value} returns");
            }
        }
    }
    
    /// <summary>
    /// 상점 아이템 데이터
    /// </summary>
    [System.Serializable]
    public struct ShopItem
    {
        public string itemId;
        public string itemName;
        public int basePrice;
        public ShopItemType itemType;
        public string description;
    }
    
    /// <summary>
    /// 상점 아이템 타입
    /// </summary>
    public enum ShopItemType
    {
        Weapon,
        Armor,
        Consumable,
        Tool,
        Material
    }
}