using UnityEngine;
using Unity.Netcode;
using System.Collections;

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
        
        // GC 최적화: 재사용 버퍼
        private static readonly Collider2D[] s_OverlapBuffer = new Collider2D[8];

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
            Debug.Log($"Picked up item: {itemInstance?.ItemData?.ItemName}");
            SpawnPickupEffect(transform.position, spriteRenderer?.sprite, GetGradeColor(itemInstance?.ItemData?.Grade ?? ItemGrade.Common));
        }

        /// <summary>
        /// 픽업 이펙트 오브젝트 생성 및 애니메이션
        /// </summary>
        private void SpawnPickupEffect(Vector3 position, Sprite itemSprite, Color gradeColor)
        {
            var effectObj = new GameObject("PickupEffect");
            effectObj.transform.position = position;

            var sr = effectObj.AddComponent<SpriteRenderer>();
            sr.sprite = itemSprite;
            sr.color = gradeColor;
            sr.sortingLayerName = "Items";
            sr.sortingOrder = 10;

            var effectRunner = effectObj.AddComponent<PickupEffectRunner>();
            effectRunner.Run(sr, 0.5f);
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
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, pickupRange, s_OverlapBuffer, playerLayerMask);
            return count > 0;
        }
        
        public override void OnDestroy()
        {
            StopAllCoroutines();
            base.OnDestroy();
        }

        // 디버그용 기즈모
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pickupRange);
        }
    }

    /// <summary>
    /// 픽업 이펙트 애니메이션 실행용 헬퍼
    /// </summary>
    public class PickupEffectRunner : MonoBehaviour
    {
        public void Run(SpriteRenderer sr, float duration)
        {
            StartCoroutine(PickupEffectCoroutine(sr, duration));
        }

        private IEnumerator PickupEffectCoroutine(SpriteRenderer sr, float duration)
        {
            Vector3 startScale = transform.localScale;
            Vector3 startPos = transform.position;
            Color startColor = sr.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 위로 올라가면서 커지고 페이드아웃
                transform.localScale = startScale * (1f + t * 0.5f);
                transform.position = startPos + Vector3.up * t * 1.5f;
                sr.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}