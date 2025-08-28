using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 드롭된 아이템 관리 클래스 - 실제 ItemInstance 객체를 다룸
    /// </summary>
    public class ItemDrop : NetworkBehaviour
    {
        [Header("Drop Settings")]
        [SerializeField] private float pickupRange = 2f;
        [SerializeField] private LayerMask playerLayerMask = 1 << 6; // Player layer
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.2f;
        
        // 아이템 데이터
        private ItemInstance itemInstance;
        private Vector3 originalPosition;
        private float bobTimer = 0f;
        
        // 컴포넌트 참조
        private SpriteRenderer spriteRenderer;
        private Collider2D pickupCollider;
        
        public ItemInstance ItemInstance => itemInstance;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            InitializeComponents();
            originalPosition = transform.position;
            
            // 랜덤 bob 타이머 시작 (여러 아이템이 동시에 생성될 때 동기화 방지)
            bobTimer = Random.Range(0f, Mathf.PI * 2f);
        }
        
        private void Update()
        {
            UpdateBobAnimation();
        }
        
        /// <summary>
        /// 컴포넌트 초기화
        /// </summary>
        private void InitializeComponents()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
            
            pickupCollider = GetComponent<Collider2D>();
            if (pickupCollider == null)
            {
                var circleCollider = gameObject.AddComponent<CircleCollider2D>();
                circleCollider.isTrigger = true;
                circleCollider.radius = pickupRange;
                pickupCollider = circleCollider;
            }
        }
        
        /// <summary>
        /// ItemInstance로 아이템 드롭 설정
        /// </summary>
        public void SetItemInstance(ItemInstance item)
        {
            itemInstance = item;
            UpdateVisuals();
        }
        
        /// <summary>
        /// ItemData로 아이템 드롭 설정 (호환성)
        /// </summary>
        public void SetItemData(ItemData itemData)
        {
            // ItemData에서 새로운 ItemInstance 생성
            itemInstance = new ItemInstance(itemData);
            UpdateVisuals();
        }
        
        /// <summary>
        /// 시각적 요소 업데이트
        /// </summary>
        private void UpdateVisuals()
        {
            if (itemInstance?.ItemData == null || spriteRenderer == null) return;
            
            // 아이템 아이콘 설정
            spriteRenderer.sprite = itemInstance.ItemData.ItemIcon;
            
            // 등급별 색상 오버레이
            spriteRenderer.color = GetGradeColor(itemInstance.ItemData.Grade);
            
            // 정렬 순서 설정
            spriteRenderer.sortingLayerName = "Items";
            spriteRenderer.sortingOrder = 1;
            
            // 게임 오브젝트 이름 설정
            gameObject.name = $"ItemDrop_{itemInstance.ItemData.ItemName}";
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
        /// 떠다니는 애니메이션
        /// </summary>
        private void UpdateBobAnimation()
        {
            bobTimer += Time.deltaTime * bobSpeed;
            
            float yOffset = Mathf.Sin(bobTimer) * bobHeight;
            transform.position = originalPosition + Vector3.up * yOffset;
        }
        
        
        /// <summary>
        /// 아이템 픽업 시도
        /// </summary>
        private bool TryPickupItem(InventoryManager inventoryManager)
        {
            if (itemInstance == null || inventoryManager?.Inventory == null) return false;
            
            int slotIndex;
            return inventoryManager.Inventory.TryAddItem(itemInstance, out slotIndex);
        }
        
        /// <summary>
        /// 픽업 이펙트 재생
        /// </summary>
        [ClientRpc]
        private void PlayPickupEffectClientRpc()
        {
            // 픽업 사운드나 파티클 효과 재생
            // TODO: 픽업 효과 구현
            Debug.Log($"🎁 Picked up item: {itemInstance?.ItemData?.ItemName}");
        }
        
        /// <summary>
        /// 수동 픽업 (상호작용 키)
        /// </summary>
        public void PickupItem(PlayerController player)
        {
            if (!IsServer) return;
            
            var inventoryManager = player.GetComponent<InventoryManager>();
            if (inventoryManager != null && TryPickupItem(inventoryManager))
            {
                PlayPickupEffectClientRpc();
                
                if (NetworkObject.IsSpawned)
                {
                    NetworkObject.Despawn();
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
        
        /// <summary>
        /// 드롭 위치 설정
        /// </summary>
        public void SetDropPosition(Vector3 position)
        {
            transform.position = position;
            originalPosition = position;
        }
        
        /// <summary>
        /// 픽업 가능한 플레이어가 있는지 확인
        /// </summary>
        public bool HasNearbyPlayer()
        {
            Collider2D[] players = Physics2D.OverlapCircleAll(transform.position, pickupRange, playerLayerMask);
            return players.Length > 0;
        }
        
        // 디버그용 기즈모
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pickupRange);
        }
    }
}