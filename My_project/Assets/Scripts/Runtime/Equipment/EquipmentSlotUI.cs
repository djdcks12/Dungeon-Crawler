using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 개별 장비 슬롯 UI 컴포넌트 - UnifiedInventoryUI와 호환
    /// </summary>
    public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
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
        private UnifiedInventoryUI unifiedUI;
        private EquipmentManager equipmentManager;
        private bool isHovered = false;
        private bool isDragging = false;
        
        public EquipmentSlot Slot => equipmentSlot;
        public ItemInstance CurrentItem => currentItem;
        public bool IsEmpty => currentItem == null;
        
        /// <summary>
        /// 슬롯 초기화 (UnifiedInventoryUI용)
        /// </summary>
        public void Initialize(EquipmentSlot slot, UnifiedInventoryUI ui, EquipmentManager manager)
        {
            equipmentSlot = slot;
            unifiedUI = ui;
            equipmentManager = manager;
            
            // 슬롯 라벨 설정
            if (slotLabel != null)
            {
                slotLabel.text = GetSlotDisplayName(slot);
            }
            
            // 장비 매니저 이벤트 구독
            if (equipmentManager != null)
            {
                equipmentManager.OnEquipmentChanged += OnEquipmentChanged;
            }
            
            // 초기 상태 설정
            UpdateSlot(null);
        }
        
        private void OnDestroy()
        {
            if (equipmentManager != null)
            {
                equipmentManager.OnEquipmentChanged -= OnEquipmentChanged;
            }
        }
        
        /// <summary>
        /// 장비 변경 이벤트 핸들러
        /// </summary>
        private void OnEquipmentChanged(EquipmentSlot slot, ItemInstance item)
        {
            if (slot == equipmentSlot)
            {
                UpdateSlot(item);
            }
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
                EquipmentSlot.Chest => "상의",
                EquipmentSlot.Legs => "하의",
                EquipmentSlot.Feet => "신발",
                EquipmentSlot.Hands => "장갑",
                EquipmentSlot.Belt => "허리",
                EquipmentSlot.MainHand => "주무기",
                EquipmentSlot.OffHand => "보조",
                EquipmentSlot.TwoHand => "양손무기",
                EquipmentSlot.Ring1 => "반지1",
                EquipmentSlot.Ring2 => "반지2",
                EquipmentSlot.Necklace => "목걸이",
                EquipmentSlot.Earring1 => "귀걸이1",
                EquipmentSlot.Earring2 => "귀걸이2",
                _ => "빈슬롯"
            };
        }
        
        /// <summary>
        /// 클릭 이벤트 처리
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDragging) return;
            
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                // UnifiedInventoryUI용
                if (unifiedUI != null)
                {
                    unifiedUI.OnEquipmentSlotClick(equipmentSlot);
                }
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                // 우클릭: 장비 해제
                if (!IsEmpty && equipmentManager != null)
                {
                    equipmentManager.UnequipItem(equipmentSlot, true);
                }
                else if (currentItem != null)
                {
                    ShowItemInfo();
                }
            }
        }
        
        /// <summary>
        /// 드래그 시작
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsEmpty) return;
            
            isDragging = true;
            unifiedUI?.StartEquipmentDrag(this);
        }
        
        /// <summary>
        /// 드래그 중
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            // 드래그 프리뷰는 UnifiedInventoryUI에서 처리
        }
        
        /// <summary>
        /// 드래그 종료
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            
            // 드롭 대상 찾기
            GameObject target = eventData.pointerCurrentRaycast.gameObject;
            unifiedUI?.EndEquipmentDrag(this, target);
        }
        
        /// <summary>
        /// 드롭 이벤트 처리
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            var draggedItem = unifiedUI?.GetDraggedItem();
            if (draggedItem != null)
            {
                if (CanEquipItem(draggedItem))
                {
                    unifiedUI?.ProcessItemDrop(draggedItem, this);
                }
            }
            else
            {
                // 기존 방식 (간소화된 처리)
                var draggedObject = eventData.pointerDrag;
                if (draggedObject == null) return;
                
                var inventorySlot = draggedObject.GetComponent<InventorySlotUI>();
                if (inventorySlot != null)
                {
                    Debug.Log("Inventory to equipment drag-drop via legacy system");
                }
                
                var equipmentSlotUI = draggedObject.GetComponent<EquipmentSlotUI>();
                if (equipmentSlotUI != null && !equipmentSlotUI.IsEmpty)
                {
                    Debug.Log("Equipment slot swapping via legacy system");
                }
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
            
            // 드래그 중이면 드래그 오버 표시
            if (eventData.dragging && unifiedUI != null)
            {
                var draggedItem = unifiedUI.GetDraggedItem();
                bool canDrop = draggedItem != null && CanEquipItem(draggedItem);
                SetDragOverVisual(true, !canDrop);
            }
            
            // 툴팁 표시
            if (currentItem != null)
            {
                if (unifiedUI != null)
                {
                    unifiedUI.ShowTooltip(currentItem, transform.position);
                }
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
            
            SetDragOverVisual(false);
            
            // 툴팁 숨기기
            if (unifiedUI != null)
            {
                unifiedUI.HideTooltip();
            }
        }
        
        /// <summary>
        /// 드래그 오버 시각적 피드백
        /// </summary>
        private void SetDragOverVisual(bool dragOver, bool isInvalid = false)
        {
            if (slotBackground == null) return;
            
            if (dragOver)
            {
                if (isInvalid)
                {
                    slotBackground.color = Color.red;
                }
                else
                {
                    slotBackground.color = Color.green;
                }
            }
            else if (!isHovered)
            {
                slotBackground.color = IsEmpty ? emptySlotColor : occupiedSlotColor;
            }
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
            if (item?.ItemData == null || !item.ItemData.IsEquippable)
                return false;
            
            // 슬롯 타입 확인
            if (item.ItemData.EquipmentSlot == equipmentSlot)
                return true;
            
            // 무기 카테고리 확인
            return IsWeaponCompatible(item);
        }
        
        /// <summary>
        /// 무기 호환성 확인
        /// </summary>
        private bool IsWeaponCompatible(ItemInstance item)
        {
            var weaponCategory = item.ItemData.WeaponCategory;
            
            return equipmentSlot switch
            {
                EquipmentSlot.MainHand => weaponCategory == WeaponCategory.Sword ||
                                        weaponCategory == WeaponCategory.Dagger ||
                                        weaponCategory == WeaponCategory.Axe ||
                                        weaponCategory == WeaponCategory.Mace,
                EquipmentSlot.OffHand => weaponCategory == WeaponCategory.Shield ||
                                       weaponCategory == WeaponCategory.Dagger,
                EquipmentSlot.TwoHand => weaponCategory == WeaponCategory.Bow ||
                                       weaponCategory == WeaponCategory.Staff,
                _ => false
            };
        }
    }
}