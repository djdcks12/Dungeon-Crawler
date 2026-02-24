using UnityEngine;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 상인 NPC - 카테고리별 아이템 판매, F키로 상호작용
    /// </summary>
    public class MerchantNPC : MonoBehaviour
    {
        [Header("상점 설정")]
        [SerializeField] private string shopName = "마을 상점";
        [SerializeField] private MerchantType merchantType = MerchantType.General;
        [SerializeField] private float interactionRange = 2f;
        [SerializeField] private float sellPriceRatio = 0.3f;

        [Header("시각 효과")]
        [SerializeField] private GameObject interactionPrompt;

        private bool playerInRange = false;
        private PlayerController nearbyPlayer;

        private void Start()
        {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
        }

        private void Update()
        {
            if (!playerInRange || nearbyPlayer == null) return;

            if (Input.GetKeyDown(KeyCode.F))
            {
                OpenShop();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null && player.IsOwner)
            {
                playerInRange = true;
                nearbyPlayer = player;
                if (interactionPrompt != null)
                    interactionPrompt.SetActive(true);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null && player.IsOwner)
            {
                playerInRange = false;
                nearbyPlayer = null;
                if (interactionPrompt != null)
                    interactionPrompt.SetActive(false);
            }
        }

        /// <summary>
        /// 상점 열기
        /// </summary>
        private void OpenShop()
        {
            // EconomySystem에 실제 아이템 로드
            if (EconomySystem.Instance != null)
            {
                LoadItemsToEconomy();
            }

            // ShopUI 열기
            var shopUI = FindFirstObjectByType<ShopUI>();
            if (shopUI == null)
            {
                var uiManager = UIManager.Instance;
                if (uiManager != null)
                    shopUI = uiManager.GetUI<ShopUI>();
            }
            if (shopUI != null)
            {
                shopUI.OpenShop(shopName);
            }
        }

        /// <summary>
        /// 상인 타입별 아이템을 EconomySystem에 로드
        /// </summary>
        private void LoadItemsToEconomy()
        {
            ItemDatabase.Initialize();
            var shopItems = new List<ShopItem>();

            var allItems = ItemDatabase.GetAllItems();
            if (allItems == null) return;

            foreach (var item in allItems)
            {
                if (item == null) continue;

                // 상인 타입에 맞는 아이템만 필터링
                if (!ShouldSellItem(item)) continue;

                shopItems.Add(new ShopItem
                {
                    itemId = item.ItemName,
                    itemName = item.ItemName,
                    basePrice = (int)item.SellPrice,
                    itemType = GetShopItemType(item),
                    description = item.Description
                });
            }

            if (EconomySystem.Instance != null)
                EconomySystem.Instance.SetShopItems(shopItems);
        }

        /// <summary>
        /// 상인 타입에 따라 판매할 아이템인지 확인
        /// </summary>
        private bool ShouldSellItem(ItemData item)
        {
            switch (merchantType)
            {
                case MerchantType.General:
                    return true;
                case MerchantType.Weapon:
                    return item.ItemType == ItemType.Equipment &&
                           item.EquipmentSlot != EquipmentSlot.None &&
                           (item.EquipmentSlot == EquipmentSlot.MainHand ||
                            item.EquipmentSlot == EquipmentSlot.TwoHand);
                case MerchantType.Armor:
                    return item.ItemType == ItemType.Equipment &&
                           item.EquipmentSlot != EquipmentSlot.None &&
                           item.EquipmentSlot != EquipmentSlot.MainHand &&
                           item.EquipmentSlot != EquipmentSlot.TwoHand;
                case MerchantType.Consumable:
                    return item.ItemType == ItemType.Consumable;
                default:
                    return true;
            }
        }

        /// <summary>
        /// ItemData의 타입을 ShopItemType으로 변환
        /// </summary>
        private ShopItemType GetShopItemType(ItemData item)
        {
            if (item.ItemType == ItemType.Consumable) return ShopItemType.Consumable;
            if (item.ItemType == ItemType.Equipment)
            {
                if (item.EquipmentSlot == EquipmentSlot.MainHand || item.EquipmentSlot == EquipmentSlot.TwoHand)
                    return ShopItemType.Weapon;
                return ShopItemType.Armor;
            }
            return ShopItemType.Material;
        }
    }

    public enum MerchantType
    {
        General,    // 전체
        Weapon,     // 무기
        Armor,      // 방어구
        Consumable  // 소모품
    }
}
