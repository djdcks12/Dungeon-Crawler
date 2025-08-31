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
        
        [Header("마우스 상호작용")]
        [SerializeField] private Color outlineColor = Color.white;
        [SerializeField] private float outlineWidth = 0.1f;
        [SerializeField] private LayerMask mouseRaycastLayer = -1;
        
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
        
        // 마우스 상호작용용
        private bool isHovered = false;
        private Camera mainCamera;
        private Material originalMaterial;
        private Material outlineMaterial;
        
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
            mainCamera = Camera.allCameras[0];
            
            // 초기 위치를 약간 랜덤하게 조정
            bobTimer = Random.Range(0f, 2f * Mathf.PI);
            
            // 마우스 상호작용을 위한 콜라이더 추가
            SetupMouseInteraction();
            
            SetupVisuals();
            
            // 머티리얼 설정
            SetupMaterials();
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
        /// 머티리얼 설정
        /// </summary>
        private void SetupMaterials()
        {
            if (spriteRenderer == null) return;
            
            // 기존 머티리얼 저장
            originalMaterial = spriteRenderer.material;
            
            // 아웃라인 머티리얼 로드
            outlineMaterial = Resources.Load<Material>("Shader/simpleOutline");
            if (outlineMaterial == null)
            {
                Debug.LogWarning("simpleOutline 머티리얼을 찾을 수 없습니다: Resources/Shader/simpleOutline");
            }
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
            UpdateMouseInteraction();
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
        /// 마우스 상호작용 설정
        /// </summary>
        private void SetupMouseInteraction()
        {
            // 마우스 클릭 감지용 콜라이더 추가
            var collider = GetComponent<Collider2D>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<CircleCollider2D>();
                ((CircleCollider2D)collider).radius = 0.5f;
                collider.isTrigger = true; // 트리거로 설정
            }
        }
        
        /// <summary>
        /// 마우스 상호작용 업데이트
        /// </summary>
        private void UpdateMouseInteraction()
        {
            if (mainCamera == null || isPickedUp) return;
            
            // 마우스 월드 좌표 계산 (2D용으로 수정)
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = mainCamera.WorldToScreenPoint(transform.position).z;
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
            
            float distance = Vector2.Distance(transform.position, mouseWorldPos);
            bool shouldHover = distance <= pickupRange;
            
            if (shouldHover != isHovered)
            {
                isHovered = shouldHover;
                
                if (isHovered)
                {
                    OnMouseHoverEnter();
                }
                else
                {
                    OnMouseHoverExit();
                }
            }
            
            // 클릭 감지
            if (isHovered && Input.GetMouseButtonDown(0)) // 좌클릭
            {
                OnMouseClick();
            }
        }
        
        /// <summary>
        /// 마우스 호버 시작
        /// </summary>
        private void OnMouseHoverEnter()
        {
            Debug.Log($"🖱️ Mouse hover enter: {itemInstance?.ItemData?.ItemName}");
            
            // 아웃라인 활성화
            SetOutlineActive(true);
            
            // 툴팁 표시 (UI 시스템에 요청)
            ShowTooltip();
        }
        
        /// <summary>
        /// 마우스 호버 종료
        /// </summary>
        private void OnMouseHoverExit()
        {
            Debug.Log($"🖱️ Mouse hover exit: {itemInstance?.ItemData?.ItemName}");
            
            // 아웃라인 비활성화
            SetOutlineActive(false);
            
            // 툴팁 숨김
            HideTooltip();
        }
        
        /// <summary>
        /// 마우스 클릭
        /// </summary>
        private void OnMouseClick()
        {
            Debug.Log($"🖱️ Mouse clicked: {itemInstance?.ItemData?.ItemName}");
            
            // 가장 가까운 플레이어 찾기
            var localPlayer = FindLocalPlayer();
            if (localPlayer != null)
            {
                MousePickup(localPlayer);
            }
        }
        
        /// <summary>
        /// 아웃라인 활성화/비활성화
        /// </summary>
        private void SetOutlineActive(bool active)
        {
            if (spriteRenderer == null) return;
            
            if (active && outlineMaterial != null)
            {
                spriteRenderer.material = outlineMaterial;
            }
            else if (!active && originalMaterial != null)
            {
                spriteRenderer.material = originalMaterial;
            }
        }
        
        /// <summary>
        /// 툴팁 표시
        /// </summary>
        private void ShowTooltip()
        {
            // Singleton 인스턴스 우선 사용
            var tooltipManager = ItemTooltipManager.Instance;
            if (tooltipManager == null)
            {
                // Singleton이 없으면 FindObjectOfType으로 찾기
                tooltipManager = FindObjectOfType<ItemTooltipManager>();
            }
            
            if (tooltipManager != null && itemInstance != null)
            {
                tooltipManager.ShowTooltip(itemInstance, Input.mousePosition);
                Debug.Log($"🖱️ Tooltip shown for: {itemInstance.ItemData?.ItemName}");
            }
            else
            {
                Debug.LogWarning($"🖱️ ItemTooltipManager not found or itemInstance is null");
            }
        }
        
        /// <summary>
        /// 툴팁 숨김
        /// </summary>
        private void HideTooltip()
        {
            var tooltipManager = ItemTooltipManager.Instance;
            if (tooltipManager == null)
            {
                tooltipManager = FindObjectOfType<ItemTooltipManager>();
            }
            
            if (tooltipManager != null)
            {
                tooltipManager.HideTooltip();
                Debug.Log($"🖱️ Tooltip hidden");
            }
        }
        
        /// <summary>
        /// 로컬 플레이어 찾기
        /// </summary>
        private PlayerController FindLocalPlayer()
        {
            var networkManager = NetworkManager.Singleton;
            if (networkManager != null && networkManager.LocalClient != null)
            {
                var localPlayerObject = networkManager.LocalClient.PlayerObject;
                if (localPlayerObject != null)
                {
                    return localPlayerObject.GetComponent<PlayerController>();
                }
            }
            return null;
        }
        
        /// <summary>
        /// 마우스 픽업 (새로운 방식)
        /// </summary>
        public void MousePickup(PlayerController player)
        {
            Debug.Log($"🖱️ MousePickup called by {player?.OwnerClientId}");
            
            // 서버에서만 처리 또는 로컬 플레이어가 서버에 요청
            if (IsServer)
            {
                ProcessMousePickup(player);
            }
            else if (player.IsLocalPlayer)
            {
                RequestPickupServerRpc(player.OwnerClientId);
            }
        }
        
        /// <summary>
        /// 마우스 픽업 처리
        /// </summary>
        private void ProcessMousePickup(PlayerController player)
        {
            if (isPickedUp) 
            {
                Debug.Log($"🖱️ Pickup blocked - already processed");
                return;
            }
            
            Debug.Log($"🖱️ Processing mouse pickup for {player.OwnerClientId}");
            
            // 마우스 픽업 처리
            AttemptPickup(player);
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void RequestPickupServerRpc(ulong playerClientId)
        {
            Debug.Log($"🖱️ RequestPickupServerRpc from client {playerClientId}");
            
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
                        ProcessMousePickup(player);
                    }
                }
            }
        }
        
        /// <summary>
        /// 아이템 픽업 시도 (공통 처리)
        /// </summary>
        private void AttemptPickup(PlayerController player)
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