using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 바닥에 떨어진 아이템을 나타내는 컴포넌트
    /// 플레이어가 근처에 가면 자동으로 픽업되는 시스템
    /// </summary>
    public class DroppedItem : NetworkBehaviour
    {
        [Header("픽업 설정")]
        [SerializeField] private float pickupRange = 1.5f;
        [SerializeField] private float autoPickupDelay = 1f; // 드롭 후 1초 대기
        [SerializeField] private bool enableAutoPickup = true;
        
        [Header("시각적 효과")]
        [SerializeField] private float bobSpeed = 2f;       // 위아래 움직임 속도
        [SerializeField] private float bobHeight = 0.2f;    // 위아래 움직임 높이
        [SerializeField] private float rotateSpeed = 90f;   // 회전 속도
        [SerializeField] private float glowIntensity = 1.5f; // 발광 강도
        
        // 아이템 정보
        private ItemInstance itemInstance;
        private ulong? droppedByClientId; // 누가 드롭했는지
        private float dropTime;
        private Vector3 basePosition;
        
        // 시각적 효과용
        private SpriteRenderer spriteRenderer;
        private float bobTimer = 0f;
        
        // 픽업 쿨다운
        private float lastPickupAttempt = 0f;
        private const float pickupCooldown = 0.5f;
        
        // 프로퍼티
        public ItemInstance ItemInstance => itemInstance;
        public float DropTime => dropTime;
        public ulong? DroppedByClientId => droppedByClientId;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            spriteRenderer = GetComponent<SpriteRenderer>();
            basePosition = transform.position;
            dropTime = Time.time;
            
            // 초기 위치를 약간 랜덤하게 조정
            bobTimer = Random.Range(0f, 2f * Mathf.PI);
            
            SetupVisuals();
        }
        
        /// <summary>
        /// 아이템 인스턴스로 초기화
        /// </summary>
        public void Initialize(ItemInstance item, ulong? droppedBy = null)
        {
            itemInstance = item;
            droppedByClientId = droppedBy;
            dropTime = Time.time;
            
            if (spriteRenderer != null)
            {
                SetupVisuals();
            }
        }
        
        /// <summary>
        /// 시각적 설정
        /// </summary>
        private void SetupVisuals()
        {
            if (spriteRenderer == null || itemInstance?.ItemData == null) return;
            
            // 스프라이트 설정
            if (itemInstance.ItemData.ItemIcon != null)
            {
                spriteRenderer.sprite = itemInstance.ItemData.ItemIcon;
            }
            else
            {
                // 기본 아이템 스프라이트 생성
                spriteRenderer.sprite = CreateDefaultItemSprite();
            }
            
            // 등급별 색상 적용
            spriteRenderer.color = itemInstance.ItemData.GradeColor;
            
            // 정렬 순서
            spriteRenderer.sortingLayerName = "Items";
            spriteRenderer.sortingOrder = 1;
            
            // 발광 효과 (고등급 아이템)
            if (itemInstance.ItemData.Grade >= ItemGrade.Rare)
            {
                AddGlowEffect();
            }
        }
        
        /// <summary>
        /// 기본 아이템 스프라이트 생성
        /// </summary>
        private Sprite CreateDefaultItemSprite()
        {
            Texture2D texture = new Texture2D(16, 16);
            Color itemColor = itemInstance.ItemData.GradeColor;
            
            // 간단한 사각형 아이템 생성
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x == 0 || x == 15 || y == 0 || y == 15)
                    {
                        texture.SetPixel(x, y, Color.black); // 테두리
                    }
                    else
                    {
                        texture.SetPixel(x, y, itemColor); // 내부
                    }
                }
            }
            
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        }
        
        /// <summary>
        /// 발광 효과 추가
        /// </summary>
        private void AddGlowEffect()
        {
            // 발광 효과를 위한 두 번째 스프라이트 렌더러 추가
            GameObject glowObject = new GameObject("Glow");
            glowObject.transform.SetParent(transform);
            glowObject.transform.localPosition = Vector3.zero;
            glowObject.transform.localScale = Vector3.one * 1.2f;
            
            var glowRenderer = glowObject.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = spriteRenderer.sprite;
            glowRenderer.color = new Color(itemInstance.ItemData.GradeColor.r, 
                                          itemInstance.ItemData.GradeColor.g, 
                                          itemInstance.ItemData.GradeColor.b, 
                                          0.3f);
            glowRenderer.sortingLayerName = "Items";
            glowRenderer.sortingOrder = 0;
            
            // 발광 애니메이션 컴포넌트 추가
            var glowAnimation = glowObject.AddComponent<ItemGlowAnimation>();
            glowAnimation.Initialize(glowIntensity);
        }
        
        private void Update()
        {
            UpdateVisualEffects();
            
            if (IsServer)
            {
                CheckForPlayerPickup();
            }
        }
        
        /// <summary>
        /// 시각적 효과 업데이트
        /// </summary>
        private void UpdateVisualEffects()
        {
            if (spriteRenderer == null) return;
            
            // 위아래 움직임 (Bobbing)
            bobTimer += Time.deltaTime * bobSpeed;
            float bobOffset = Mathf.Sin(bobTimer) * bobHeight;
            transform.position = basePosition + Vector3.up * bobOffset;
            
            // 회전
            transform.Rotate(Vector3.forward, rotateSpeed * Time.deltaTime);
        }
        
        /// <summary>
        /// 플레이어 픽업 체크 (서버에서만)
        /// </summary>
        private void CheckForPlayerPickup()
        {
            if (!enableAutoPickup) return;
            if (Time.time < dropTime + autoPickupDelay) return;
            if (Time.time < lastPickupAttempt + pickupCooldown) return;
            
            // 근처 플레이어 찾기
            Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, pickupRange);
            
            foreach (var collider in nearbyColliders)
            {
                var player = collider.GetComponent<PlayerController>();
                if (player != null && player.IsOwner)
                {
                    Debug.Log($"🎯 Found player {player.OwnerClientId} near item {itemInstance?.ItemData?.ItemName}");
                    
                    // 플레이어가 살아있는지 확인
                    var statsManager = player.GetComponent<PlayerStatsManager>();
                    if (statsManager != null && !statsManager.IsDead)
                    {
                        Debug.Log($"🎯 Attempting pickup for player {player.OwnerClientId}");
                        AttemptPickup(player);
                        return;
                    }
                    else
                    {
                        Debug.Log($"❌ Player {player.OwnerClientId} is dead or no statsManager");
                    }
                }
            }
        }
        
        /// <summary>
        /// 픽업 시도
        /// </summary>
        private void AttemptPickup(PlayerController player)
        {
            if (!IsServer) return;
            
            lastPickupAttempt = Time.time;
            
            Debug.Log($"🎯 AttemptPickup called for player {player.OwnerClientId}");
            
            // 아이템 드롭 시스템에 픽업 요청
            var itemDropSystem = FindObjectOfType<ItemDropSystem>();
            if (itemDropSystem != null)
            {
                Debug.Log($"🎯 Using ItemDropSystem for pickup");
                itemDropSystem.PickupItem(this, player);
            }
            else
            {
                Debug.Log($"🎯 Using direct pickup (no ItemDropSystem)");
                // ItemDropSystem이 없으면 직접 처리
                ProcessDirectPickup(player);
            }
        }
        
        /// <summary>
        /// 직접 픽업 처리 (ItemDropSystem이 없을 때)
        /// </summary>
        private void ProcessDirectPickup(PlayerController player)
        {
            if (itemInstance == null) 
            {
                Debug.LogError("❌ itemInstance is null in ProcessDirectPickup");
                return;
            }
            
            var inventoryManager = player.GetComponent<InventoryManager>();
            if (inventoryManager == null) 
            {
                Debug.LogError($"❌ No InventoryManager found on player {player.OwnerClientId}");
                return;
            }
            
            Debug.Log($"🎯 ProcessDirectPickup: Adding {itemInstance.ItemData.ItemName} to player {player.OwnerClientId} inventory");
            
            // ServerRpc를 통해 아이템 추가 요청
            inventoryManager.AddItemServerRpc(itemInstance.ItemId, itemInstance.Quantity);
            
            NotifyPickupClientRpc(player.OwnerClientId, 
                $"{itemInstance.ItemData.ItemName} x{itemInstance.Quantity} 획득", 
                itemInstance.ItemData.GradeColor);
            
            Debug.Log($"✅ Player {player.OwnerClientId} picked up {itemInstance.ItemData.ItemName} x{itemInstance.Quantity}");
            
            // 오브젝트 제거
            if (NetworkObject != null)
            {
                NetworkObject.Despawn();
            }
        }
        
        /// <summary>
        /// 수동 픽업 (플레이어가 E키 등으로 직접 픽업)
        /// </summary>
        public void ManualPickup(PlayerController player)
        {
            if (!IsServer) return;
            
            // 거리 체크
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance > pickupRange * 1.5f) return;
            
            AttemptPickup(player);
        }
        
        /// <summary>
        /// 픽업 알림 (클라이언트)
        /// </summary>
        [ClientRpc]
        private void NotifyPickupClientRpc(ulong targetClientId, string message, Color color)
        {
            if (NetworkManager.Singleton.LocalClientId == targetClientId)
            {
                Debug.Log($"📦 {message}");
                // 추후 UI 시스템에서 픽업 알림 표시
            }
        }
        
        /// <summary>
        /// 아이템 정보 표시 (UI용)
        /// </summary>
        public string GetDisplayText()
        {
            if (itemInstance?.ItemData == null) return "Unknown Item";
            
            string text = itemInstance.ItemData.ItemName;
            
            if (itemInstance.Quantity > 1)
            {
                text += $" x{itemInstance.Quantity}";
            }
            
            return text;
        }
        
        /// <summary>
        /// 픽업 범위 기즈모 (에디터용)
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, pickupRange);
        }
        
        /// <summary>
        /// 트리거 기반 픽업 (대안)
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer) return;
            
            var player = other.GetComponent<PlayerController>();
            if (player != null && player.IsOwner)
            {
                if (Time.time >= dropTime + autoPickupDelay)
                {
                    AttemptPickup(player);
                }
            }
        }
    }
    
    /// <summary>
    /// 아이템 발광 애니메이션 컴포넌트
    /// </summary>
    public class ItemGlowAnimation : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Color baseColor;
        private float glowIntensity = 1.5f;
        private float glowSpeed = 2f;
        
        public void Initialize(float intensity)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseColor = spriteRenderer.color;
            glowIntensity = intensity;
        }
        
        private void Update()
        {
            if (spriteRenderer == null) return;
            
            // 발광 효과 (알파값 조정)
            float glow = (Mathf.Sin(Time.time * glowSpeed) + 1f) * 0.5f;
            float alpha = baseColor.a + (glow * glowIntensity * 0.3f);
            
            spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }
    }
}