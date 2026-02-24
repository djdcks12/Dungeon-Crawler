using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// NPC 상점 UI 시스템
    /// 상점 아이템 목록 표시, 구매/판매 처리
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        [Header("UI 패널들")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private GameObject shopItemListPanel;
        [SerializeField] private GameObject playerInventoryPanel;
        
        [Header("상점 정보 UI")]
        [SerializeField] private Text shopNameText;
        [SerializeField] private Text playerGoldText;
        [SerializeField] private Button closeButton;
        
        [Header("상점 아이템 리스트 UI")]
        [SerializeField] private Transform shopItemsContent;
        [SerializeField] private GameObject shopItemPrefab;
        [SerializeField] private ScrollRect shopScrollRect;
        
        [Header("구매/판매 UI")]
        [SerializeField] private GameObject purchaseConfirmPanel;
        [SerializeField] private Text purchaseItemNameText;
        [SerializeField] private Text purchaseItemPriceText;
        [SerializeField] private Text purchaseItemDescriptionText;
        [SerializeField] private Button confirmPurchaseButton;
        [SerializeField] private Button cancelPurchaseButton;
        
        [Header("카테고리 필터 UI")]
        [SerializeField] private Button allCategoryButton;
        [SerializeField] private Button weaponCategoryButton;
        [SerializeField] private Button armorCategoryButton;
        [SerializeField] private Button consumableCategoryButton;
        [SerializeField] private Button toolCategoryButton;
        
        // 상점 상태
        private List<ShopItem> currentShopItems = new List<ShopItem>();
        private ShopItemType currentCategory = ShopItemType.Weapon; // 전체 보기용 기본값
        private ShopItem selectedItem;
        private bool isShopOpen = false;
        
        // 컴포넌트 참조
        private PlayerStatsManager playerStatsManager;
        
        private void Awake()
        {
            // 기본적으로 상점 UI 숨김
            SetShopActive(false);
            SetPurchaseConfirmActive(false);
        }
        
        private void Start()
        {
            // 버튼 이벤트 연결
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseShop);
                
            if (confirmPurchaseButton != null)
                confirmPurchaseButton.onClick.AddListener(ConfirmPurchase);
                
            if (cancelPurchaseButton != null)
                cancelPurchaseButton.onClick.AddListener(CancelPurchase);
                
            // 카테고리 버튼 이벤트 연결
            if (allCategoryButton != null)
                allCategoryButton.onClick.AddListener(() => FilterByCategory(ShopItemType.Weapon)); // 전체용 임시
                
            if (weaponCategoryButton != null)
                weaponCategoryButton.onClick.AddListener(() => FilterByCategory(ShopItemType.Weapon));
                
            if (armorCategoryButton != null)
                armorCategoryButton.onClick.AddListener(() => FilterByCategory(ShopItemType.Armor));
                
            if (consumableCategoryButton != null)
                consumableCategoryButton.onClick.AddListener(() => FilterByCategory(ShopItemType.Consumable));
                
            if (toolCategoryButton != null)
                toolCategoryButton.onClick.AddListener(() => FilterByCategory(ShopItemType.Tool));
        }
        
        /// <summary>
        /// 상점 열기
        /// </summary>
        public void OpenShop(string shopName = "상점")
        {
            // 플레이어 스탯 매니저 찾기
            if (playerStatsManager == null)
            {
                var localPlayer = Unity.Netcode.NetworkManager.Singleton?.SpawnManager?.GetLocalPlayerObject();
                if (localPlayer != null)
                {
                    playerStatsManager = localPlayer.GetComponent<PlayerStatsManager>();
                }
            }
            
            if (playerStatsManager == null)
            {
                Debug.LogError("PlayerStatsManager not found! Cannot open shop.");
                return;
            }
            
            // 상점 데이터 로드
            LoadShopItems();
            
            // UI 설정
            SetText(shopNameText, shopName);
            UpdatePlayerGoldDisplay();
            
            // 상점 활성화
            SetShopActive(true);
            isShopOpen = true;
            
            // 기본 카테고리로 필터링
            FilterByCategory(ShopItemType.Weapon);
            
            Debug.Log($"🏪 Shop opened: {shopName}");
        }
        
        /// <summary>
        /// 상점 닫기
        /// </summary>
        public void CloseShop()
        {
            SetShopActive(false);
            SetPurchaseConfirmActive(false);
            isShopOpen = false;
            
            Debug.Log("🏪 Shop closed");
        }
        
        /// <summary>
        /// 상점 아이템 로드
        /// </summary>
        private void LoadShopItems()
        {
            currentShopItems.Clear();
            
            if (EconomySystem.Instance != null)
            {
                currentShopItems = EconomySystem.Instance.GetShopItems();
            }
            else
            {
                // EconomySystem이 없을 때 기본 아이템들
                CreateDefaultShopItems();
            }
            
            Debug.Log($"📦 Loaded {currentShopItems.Count} shop items");
        }
        
        /// <summary>
        /// 기본 상점 아이템 생성 (테스트용)
        /// </summary>
        private void CreateDefaultShopItems()
        {
            currentShopItems.Add(new ShopItem
            {
                itemId = "basic_sword",
                itemName = "기본 검",
                basePrice = 100,
                itemType = ShopItemType.Weapon,
                description = "기본적인 한손검"
            });
            
            currentShopItems.Add(new ShopItem
            {
                itemId = "health_potion",
                itemName = "체력 물약",
                basePrice = 25,
                itemType = ShopItemType.Consumable,
                description = "HP 50 회복"
            });
        }
        
        /// <summary>
        /// 카테고리별 필터링
        /// </summary>
        private void FilterByCategory(ShopItemType category)
        {
            currentCategory = category;
            RefreshShopItemList();
            
            // 카테고리 버튼 하이라이트 (간단히 색상 변경)
            ResetCategoryButtonColors();
            HighlightCategoryButton(category);
        }
        
        /// <summary>
        /// 상점 아이템 리스트 새로고침
        /// </summary>
        private void RefreshShopItemList()
        {
            // 기존 아이템 UI들 삭제
            foreach (Transform child in shopItemsContent)
            {
                Destroy(child.gameObject);
            }
            
            // 필터링된 아이템들 표시
            var filteredItems = currentShopItems.FindAll(item => 
                currentCategory == ShopItemType.Weapon || item.itemType == currentCategory);
            
            foreach (var shopItem in filteredItems)
            {
                CreateShopItemUI(shopItem);
            }
            
            Debug.Log($"🔍 Filtered shop items: {filteredItems.Count} items in {currentCategory} category");
        }
        
        /// <summary>
        /// 상점 아이템 UI 생성
        /// </summary>
        private void CreateShopItemUI(ShopItem shopItem)
        {
            if (shopItemPrefab == null) return;
            
            GameObject itemUI = Instantiate(shopItemPrefab, shopItemsContent);
            
            // UI 컴포넌트들 설정
            var nameText = itemUI.transform.Find("ItemName")?.GetComponent<Text>();
            var priceText = itemUI.transform.Find("ItemPrice")?.GetComponent<Text>();
            var buyButton = itemUI.transform.Find("BuyButton")?.GetComponent<Button>();
            var iconImage = itemUI.transform.Find("ItemIcon")?.GetComponent<Image>();
            
            if (nameText != null)
                nameText.text = shopItem.itemName;
                
            if (priceText != null)
            {
                int finalPrice = Mathf.RoundToInt(shopItem.basePrice * 1.2f); // 상점 가격 배율
                priceText.text = $"{finalPrice} G";
            }
            
            if (buyButton != null)
            {
                buyButton.onClick.AddListener(() => SelectItemForPurchase(shopItem));
            }
            
            // 아이템 타입별 색상 설정
            if (iconImage != null)
            {
                iconImage.color = GetItemTypeColor(shopItem.itemType);
            }
        }
        
        /// <summary>
        /// 구매할 아이템 선택
        /// </summary>
        private void SelectItemForPurchase(ShopItem shopItem)
        {
            selectedItem = shopItem;
            
            // 구매 확인 패널 표시
            int finalPrice = Mathf.RoundToInt(shopItem.basePrice * 1.2f);
            
            SetText(purchaseItemNameText, shopItem.itemName);
            SetText(purchaseItemPriceText, $"{finalPrice} 골드");
            SetText(purchaseItemDescriptionText, shopItem.description);
            
            SetPurchaseConfirmActive(true);
            
            Debug.Log($"🛒 Selected item for purchase: {shopItem.itemName} ({finalPrice} gold)");
        }
        
        /// <summary>
        /// 구매 확인
        /// </summary>
        private void ConfirmPurchase()
        {
            if (selectedItem.itemId == null || EconomySystem.Instance == null)
            {
                Debug.LogError("Cannot confirm purchase: Invalid item or EconomySystem missing");
                return;
            }
            
            // 골드 확인
            int finalPrice = Mathf.RoundToInt(selectedItem.basePrice * 1.2f);
            if (playerStatsManager.CurrentStats.Gold < finalPrice)
            {
                Debug.Log($"💸 골드가 부족합니다! 필요: {finalPrice} 골드");
                SetPurchaseConfirmActive(false);
                return;
            }
            
            // EconomySystem을 통해 구매 처리
            EconomySystem.Instance.PurchaseItemServerRpc(selectedItem.itemId);
            
            SetPurchaseConfirmActive(false);
            UpdatePlayerGoldDisplay();
            
            Debug.Log($"✅ Purchase confirmed: {selectedItem.itemName}");
        }
        
        /// <summary>
        /// 구매 취소
        /// </summary>
        private void CancelPurchase()
        {
            SetPurchaseConfirmActive(false);
            selectedItem = new ShopItem(); // 선택 해제
            
            Debug.Log("❌ Purchase cancelled");
        }
        
        /// <summary>
        /// 플레이어 골드 표시 업데이트
        /// </summary>
        private void UpdatePlayerGoldDisplay()
        {
            if (playerStatsManager?.CurrentStats != null)
            {
                long currentGold = playerStatsManager.CurrentStats.Gold;
                SetText(playerGoldText, $"보유 골드: {currentGold:N0}");
            }
        }
        
        /// <summary>
        /// 아이템 타입별 색상 가져오기
        /// </summary>
        private Color GetItemTypeColor(ShopItemType itemType)
        {
            switch (itemType)
            {
                case ShopItemType.Weapon: return Color.red;
                case ShopItemType.Armor: return Color.blue;
                case ShopItemType.Consumable: return Color.green;
                case ShopItemType.Tool: return Color.yellow;
                default: return Color.white;
            }
        }
        
        /// <summary>
        /// 카테고리 버튼 색상 리셋
        /// </summary>
        private void ResetCategoryButtonColors()
        {
            SetButtonColor(allCategoryButton, Color.white);
            SetButtonColor(weaponCategoryButton, Color.white);
            SetButtonColor(armorCategoryButton, Color.white);
            SetButtonColor(consumableCategoryButton, Color.white);
            SetButtonColor(toolCategoryButton, Color.white);
        }
        
        /// <summary>
        /// 카테고리 버튼 하이라이트
        /// </summary>
        private void HighlightCategoryButton(ShopItemType category)
        {
            Color highlightColor = Color.yellow;
            
            switch (category)
            {
                case ShopItemType.Weapon:
                    SetButtonColor(weaponCategoryButton, highlightColor);
                    break;
                case ShopItemType.Armor:
                    SetButtonColor(armorCategoryButton, highlightColor);
                    break;
                case ShopItemType.Consumable:
                    SetButtonColor(consumableCategoryButton, highlightColor);
                    break;
                case ShopItemType.Tool:
                    SetButtonColor(toolCategoryButton, highlightColor);
                    break;
            }
        }
        
        /// <summary>
        /// 버튼 색상 설정
        /// </summary>
        private void SetButtonColor(Button button, Color color)
        {
            if (button != null)
            {
                var colors = button.colors;
                colors.normalColor = color;
                button.colors = colors;
            }
        }
        
        /// <summary>
        /// 상점 패널 활성화/비활성화
        /// </summary>
        private void SetShopActive(bool active)
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(active);
            }
        }
        
        /// <summary>
        /// 구매 확인 패널 활성화/비활성화
        /// </summary>
        private void SetPurchaseConfirmActive(bool active)
        {
            if (purchaseConfirmPanel != null)
            {
                purchaseConfirmPanel.SetActive(active);
            }
        }
        
        /// <summary>
        /// 안전한 텍스트 설정
        /// </summary>
        private void SetText(Text textComponent, string text)
        {
            if (textComponent != null)
            {
                textComponent.text = text;
            }
        }
        
        /// <summary>
        /// 업데이트 (골드 표시 갱신 등)
        /// </summary>
        private void Update()
        {
            if (isShopOpen)
            {
                UpdatePlayerGoldDisplay();
            }
        }
        
        /// <summary>
        /// 상점 토글 (외부에서 호출)
        /// </summary>
        public void ToggleShop()
        {
            if (isShopOpen)
            {
                CloseShop();
            }
            else
            {
                OpenShop();
            }
        }
        
        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();
            if (confirmPurchaseButton != null) confirmPurchaseButton.onClick.RemoveAllListeners();
            if (cancelPurchaseButton != null) cancelPurchaseButton.onClick.RemoveAllListeners();
            if (allCategoryButton != null) allCategoryButton.onClick.RemoveAllListeners();
            if (weaponCategoryButton != null) weaponCategoryButton.onClick.RemoveAllListeners();
            if (armorCategoryButton != null) armorCategoryButton.onClick.RemoveAllListeners();
            if (consumableCategoryButton != null) consumableCategoryButton.onClick.RemoveAllListeners();
            if (toolCategoryButton != null) toolCategoryButton.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// 디버그 정보
        /// </summary>
        [ContextMenu("Show Shop Debug Info")]
        private void ShowDebugInfo()
        {
            Debug.Log("=== Shop UI Debug ===");
            Debug.Log($"Shop Open: {isShopOpen}");
            Debug.Log($"Current Category: {currentCategory}");
            Debug.Log($"Shop Items: {currentShopItems.Count}");
            Debug.Log($"Selected Item: {selectedItem.itemName}");
        }
    }
}