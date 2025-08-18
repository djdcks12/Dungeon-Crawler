using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 개별 장비 슬롯 UI 컴포넌트
    /// </summary>
    public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI Components")]
        [SerializeField] private Image slotBackground;
        [SerializeField] private Image itemIcon;
        [SerializeField] private Image gradeFrame;
        [SerializeField] private Text itemCountText; // 스택 가능한 아이템용
        [SerializeField] private Image durabilityBar;
        [SerializeField] private Text slotLabel;
        
        [Header("Visual Settings")]
        [SerializeField] private Color emptySlotColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        [SerializeField] private Color occupiedSlotColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color hoverColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        
        // 데이터
        private EquipmentSlot equipmentSlot;
        private ItemInstance currentItem;
        private EquipmentUI parentUI;
        private bool isHovered = false;
        
        public EquipmentSlot Slot => equipmentSlot;
        public ItemInstance CurrentItem => currentItem;
        public bool IsEmpty => currentItem == null;
        
        /// <summary>
        /// 슬롯 초기화
        /// </summary>
        public void Initialize(EquipmentSlot slot, EquipmentUI parent)
        {
            equipmentSlot = slot;
            parentUI = parent;
            
            // 슬롯 라벨 설정
            if (slotLabel != null)
            {
                slotLabel.text = GetSlotDisplayName(slot);
            }
            
            // 초기 상태 설정
            UpdateSlot(null);
        }
        
        /// <summary>
        /// 슬롯 업데이트
        /// </summary>
        public void UpdateSlot(ItemInstance item)
        {
            currentItem = item;
            
            if (item == null)
            {
                // 빈 슬롯
                SetEmptySlot();
            }
            else
            {
                // 아이템이 있는 슬롯
                SetOccupiedSlot(item);
            }
        }
        
        /// <summary>
        /// 빈 슬롯 설정
        /// </summary>
        private void SetEmptySlot()
        {
            if (slotBackground != null)
            {
                slotBackground.color = emptySlotColor;
            }
            
            if (itemIcon != null)
            {
                itemIcon.sprite = null;
                itemIcon.color = Color.clear;
            }
            
            if (gradeFrame != null)
            {
                gradeFrame.color = Color.clear;
            }
            
            if (itemCountText != null)
            {
                itemCountText.text = "";
            }
            
            if (durabilityBar != null)
            {
                durabilityBar.fillAmount = 0f;
                durabilityBar.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// 아이템이 있는 슬롯 설정
        /// </summary>
        private void SetOccupiedSlot(ItemInstance item)
        {
            if (slotBackground != null)
            {
                slotBackground.color = occupiedSlotColor;
            }
            
            if (itemIcon != null && item.ItemData?.ItemIcon != null)
            {
                itemIcon.sprite = item.ItemData.ItemIcon;
                itemIcon.color = Color.white;
            }
            
            // 등급별 프레임 색상
            if (gradeFrame != null)
            {
                gradeFrame.color = GetGradeColor(item.ItemData.Grade);
            }
            
            // 스택 수량 표시 (스택 가능한 아이템인 경우)
            if (itemCountText != null)
            {
                if (item.ItemData.CanStack && item.Quantity > 1)
                {
                    itemCountText.text = item.Quantity.ToString();
                }
                else
                {
                    itemCountText.text = "";
                }
            }
            
            // 내구도 표시 (내구도가 있는 아이템인 경우)
            if (durabilityBar != null && item.ItemData.HasDurability)
            {
                durabilityBar.gameObject.SetActive(true);
                durabilityBar.fillAmount = item.CurrentDurability / (float)item.ItemData.MaxDurability;
                
                // 내구도에 따른 색상 변경
                if (item.CurrentDurability <= item.ItemData.MaxDurability * 0.25f)
                {
                    durabilityBar.color = Color.red;
                }
                else if (item.CurrentDurability <= item.ItemData.MaxDurability * 0.5f)
                {
                    durabilityBar.color = Color.yellow;
                }
                else
                {
                    durabilityBar.color = Color.green;
                }
            }
            else if (durabilityBar != null)
            {
                durabilityBar.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// 등급별 색상 반환
        /// </summary>
        private Color GetGradeColor(ItemGrade grade)
        {
            return grade switch
            {
                ItemGrade.Common => Color.white,
                ItemGrade.Uncommon => Color.green,
                ItemGrade.Rare => Color.blue,
                ItemGrade.Epic => Color.magenta,
                ItemGrade.Legendary => Color.yellow,
                _ => Color.white
            };
        }
        
        /// <summary>
        /// 슬롯 표시명 가져오기
        /// </summary>
        private string GetSlotDisplayName(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.Head => "머리",
                EquipmentSlot.Chest => "가슴",
                EquipmentSlot.Legs => "다리",
                EquipmentSlot.Feet => "발",
                EquipmentSlot.Hands => "손",
                EquipmentSlot.MainHand => "주무기",
                EquipmentSlot.OffHand => "보조",
                EquipmentSlot.TwoHand => "양손무기",
                EquipmentSlot.Ring1 => "반지1",
                EquipmentSlot.Ring2 => "반지2",
                EquipmentSlot.Necklace => "목걸이",
                _ => "빈슬롯"
            };
        }
        
        /// <summary>
        /// 클릭 이벤트 처리
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                // 좌클릭: 장비 해제
                parentUI?.OnSlotClicked(equipmentSlot);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                // 우클릭: 아이템 정보 표시
                if (currentItem != null)
                {
                    ShowItemInfo();
                }
            }
        }
        
        /// <summary>
        /// 드롭 이벤트 처리
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            // 드래그된 아이템 확인
            var draggedObject = eventData.pointerDrag;
            if (draggedObject == null) return;
            
            // InventorySlotUI에서 드래그된 아이템 가져오기 (간소화 버전)
            var inventorySlot = draggedObject.GetComponent<InventorySlotUI>();
            if (inventorySlot != null)
            {
                // InventorySlotUI 연동은 추후 구현
                Debug.Log("Inventory to equipment drag-drop not fully implemented yet");
            }
            
            // 다른 장비 슬롯에서 드래그된 아이템 처리
            var equipmentSlotUI = draggedObject.GetComponent<EquipmentSlotUI>();
            if (equipmentSlotUI != null && !equipmentSlotUI.IsEmpty)
            {
                // 장비 슬롯간 교체는 추후 구현
                Debug.Log("Equipment slot swapping not implemented yet");
            }
        }
        
        /// <summary>
        /// 마우스 호버 시작
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            
            if (slotBackground != null)
            {
                slotBackground.color = hoverColor;
            }
            
            // 툴팁 표시
            if (currentItem != null)
            {
                parentUI?.ShowEquipmentTooltip(currentItem, transform.position);
            }
        }
        
        /// <summary>
        /// 마우스 호버 종료
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            
            // 원래 색상으로 복원
            if (slotBackground != null)
            {
                slotBackground.color = IsEmpty ? emptySlotColor : occupiedSlotColor;
            }
            
            // 툴팁 숨기기
            parentUI?.HideEquipmentTooltip();
        }
        
        /// <summary>
        /// 아이템 정보 표시
        /// </summary>
        private void ShowItemInfo()
        {
            if (currentItem?.ItemData == null) return;
            
            var itemData = currentItem.ItemData;
            string info = $"=== {itemData.ItemName} ===\n" +
                         $"등급: {itemData.Grade}\n" +
                         $"타입: {itemData.ItemType}\n" +
                         $"설명: {itemData.Description}\n";
            
            if (itemData.HasDurability)
            {
                info += $"내구도: {currentItem.CurrentDurability:F0}/{currentItem.ItemData.MaxDurability:F0}\n";
            }
            
            if (itemData.StatBonuses.HasAnyStats())
            {
                info += "스탯 보너스:\n";
                if (itemData.StatBonuses.strength > 0) info += $"  STR +{itemData.StatBonuses.strength}\n";
                if (itemData.StatBonuses.agility > 0) info += $"  AGI +{itemData.StatBonuses.agility}\n";
                if (itemData.StatBonuses.vitality > 0) info += $"  VIT +{itemData.StatBonuses.vitality}\n";
                if (itemData.StatBonuses.intelligence > 0) info += $"  INT +{itemData.StatBonuses.intelligence}\n";
                if (itemData.StatBonuses.defense > 0) info += $"  DEF +{itemData.StatBonuses.defense}\n";
                if (itemData.StatBonuses.magicDefense > 0) info += $"  MDEF +{itemData.StatBonuses.magicDefense}\n";
                if (itemData.StatBonuses.luck > 0) info += $"  LUK +{itemData.StatBonuses.luck}\n";
            }
            
            Debug.Log(info);
        }
        
        /// <summary>
        /// 슬롯 강조 표시
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (highlighted)
            {
                if (slotBackground != null)
                {
                    slotBackground.color = Color.cyan;
                }
            }
            else
            {
                if (slotBackground != null)
                {
                    slotBackground.color = isHovered ? hoverColor : 
                                         (IsEmpty ? emptySlotColor : occupiedSlotColor);
                }
            }
        }
        
        /// <summary>
        /// 아이템 착용 가능 여부 확인
        /// </summary>
        public bool CanEquipItem(ItemInstance item)
        {
            if (item?.ItemData == null) return false;
            
            // 아이템 타입에 따른 슬롯 호환성 확인
            return equipmentSlot switch
            {
                EquipmentSlot.Head => item.ItemData.ItemType == ItemType.Equipment,
                EquipmentSlot.Chest => item.ItemData.ItemType == ItemType.Equipment,
                EquipmentSlot.Legs => item.ItemData.ItemType == ItemType.Equipment,
                EquipmentSlot.Feet => item.ItemData.ItemType == ItemType.Equipment,
                EquipmentSlot.Hands => item.ItemData.ItemType == ItemType.Equipment,
                EquipmentSlot.MainHand => item.ItemData.ItemType == ItemType.Equipment,
                EquipmentSlot.OffHand => item.ItemData.ItemType == ItemType.Equipment || 
                                       item.ItemData.WeaponCategory == WeaponCategory.Shield,
                EquipmentSlot.TwoHand => item.ItemData.ItemType == ItemType.Equipment &&
                                       item.ItemData.WeaponCategory == WeaponCategory.Bow,
                EquipmentSlot.Ring1 => item.ItemData.ItemType == ItemType.Equipment,
                EquipmentSlot.Ring2 => item.ItemData.ItemType == ItemType.Equipment,
                EquipmentSlot.Necklace => item.ItemData.ItemType == ItemType.Equipment,
                _ => false
            };
        }
    }
}