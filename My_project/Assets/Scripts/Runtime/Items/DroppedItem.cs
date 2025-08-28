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
        
        // 픽업 상태 플래그 (중복 픽업 방지)
        private bool isPickedUp = false;
        
        // 프로퍼티
        public ItemInstance ItemInstance => itemInstance;
        public float DropTime => dropTime;
        public ulong? DroppedByClientId => droppedByClientId;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            Debug.Log($"🎁 DroppedItem OnNetworkSpawn: {itemInstance?.ItemData?.ItemName ?? "Unknown"} at {transform.position}");
            
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
            if (!gameObject.activeInHierarchy) return;
            UpdateVisualEffects();
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
        /// 수동 픽업 (플레이어가 Z키로 직접 픽업)
        /// </summary>
        public void ManualPickup(PlayerController player)
        {
            Debug.Log($"📦 ManualPickup called by {player?.OwnerClientId}");
            
            // 서버에서만 처리 또는 로컬 플레이어가 서버에 요청
            if (IsServer)
            {
                // 서버에서 직접 처리
                ProcessManualPickup(player);
            }
            else if (player.IsLocalPlayer)
            {
                // 클라이언트에서 서버에 픽업 요청
                RequestPickupServerRpc(player.OwnerClientId);
            }
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void RequestPickupServerRpc(ulong playerClientId)
        {
            Debug.Log($"📦 RequestPickupServerRpc from client {playerClientId}");
            
            // 플레이어 찾기
            var networkManager = NetworkManager.Singleton;
            if (networkManager != null && networkManager.ConnectedClients.TryGetValue(playerClientId, out var clientData))
            {
                var playerObject = clientData.PlayerObject;
                if (playerObject != null)
                {
                    var player = playerObject.GetComponent<PlayerController>();
                    if (player != null)
                    {
                        ProcessManualPickup(player);
                    }
                }
            }
        }
        
        private void ProcessManualPickup(PlayerController player)
        {
            if (isPickedUp) 
            {
                Debug.Log($"📦 Pickup blocked - already processed");
                return;
            }
            
            // 거리 체크
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance > pickupRange * 2f) 
            {
                Debug.Log($"📦 Too far: {distance:F1}m > {pickupRange * 2f}m");
                return;
            }
            
            Debug.Log($"📦 Processing manual pickup for {player.OwnerClientId}");
            
            // 수동 픽업은 전역 플래그 무시하고 직접 처리
            ManualAttemptPickup(player);
        }
        
        /// <summary>
        /// 수동 픽업 전용 - 전역 플래그 무시
        /// </summary>
        private void ManualAttemptPickup(PlayerController player)
        {
            if (!IsServer) return;
            if (isPickedUp) return;
            
            // 즉시 픽업 플래그 설정
            isPickedUp = true;
            
            Debug.Log($"📦 Manual pickup by {player.OwnerClientId}");
            
            // 인벤토리에 추가
            var inventoryManager = player.GetComponent<InventoryManager>();
            if (inventoryManager != null && itemInstance != null)
            {
                bool addSuccess = inventoryManager.AddItem(itemInstance);
                if (!addSuccess)
                {
                    inventoryManager.AddItemServerRpc(itemInstance.ItemId, itemInstance.Quantity);
                }
                
                // 픽업 알림
                NotifyPickupClientRpc(player.OwnerClientId, 
                    $"{itemInstance.ItemData.ItemName} x{itemInstance.Quantity} 획득", 
                    itemInstance.ItemData.GradeColor);
                
                Debug.Log($"✅ {itemInstance.ItemData.ItemName} picked up by {player.OwnerClientId}");
            }
            
            // 즉시 오브젝트 제거
            DestroyImmediate(gameObject);
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