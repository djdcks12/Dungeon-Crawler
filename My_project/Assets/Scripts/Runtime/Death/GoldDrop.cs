using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 드롭된 골드 관리 클래스
    /// </summary>
    public class GoldDrop : NetworkBehaviour
    {
        [Header("Gold Drop Settings")]
        [SerializeField] private float pickupRange = 2.5f;
        [SerializeField] private LayerMask playerLayerMask = 1 << 6; // Player layer
        [SerializeField] private float bobSpeed = 3f;
        [SerializeField] private float bobHeight = 0.15f;
        [SerializeField] private float magnetRange = 5f; // 자석 효과 범위
        [SerializeField] private float magnetSpeed = 8f;
        
        // 골드 데이터
        private NetworkVariable<long> goldAmount = new NetworkVariable<long>(0, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server);
        
        private Vector3 originalPosition;
        private float bobTimer = 0f;
        private bool isBeingMagnetized = false;
        private Transform magnetTarget;
        
        // 컴포넌트 참조
        private SpriteRenderer spriteRenderer;
        private Collider2D pickupCollider;
        
        public long GoldAmount => goldAmount.Value;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            InitializeComponents();
            originalPosition = transform.position;
            
            // 랜덤 bob 타이머 시작
            bobTimer = Random.Range(0f, Mathf.PI * 2f);
            
            // 골드량 변경 이벤트 구독
            goldAmount.OnValueChanged += OnGoldAmountChanged;
            
            // 초기 시각 업데이트
            UpdateVisuals();
        }
        
        public override void OnNetworkDespawn()
        {
            goldAmount.OnValueChanged -= OnGoldAmountChanged;
            base.OnNetworkDespawn();
        }
        
        private void Update()
        {
            if (isBeingMagnetized && magnetTarget != null)
            {
                UpdateMagnetMovement();
            }
            else
            {
                UpdateBobAnimation();
                CheckForPickup();
                CheckForMagnetEffect();
            }
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
        /// 골드 액수 설정 (서버에서만)
        /// </summary>
        public void SetGoldAmount(long amount)
        {
            if (!IsServer) return;
            
            goldAmount.Value = amount;
            UpdateVisuals();
        }
        
        /// <summary>
        /// 골드량 변경 콜백
        /// </summary>
        private void OnGoldAmountChanged(long previousValue, long newValue)
        {
            UpdateVisuals();
        }
        
        /// <summary>
        /// 시각적 요소 업데이트
        /// </summary>
        private void UpdateVisuals()
        {
            if (spriteRenderer == null) return;
            
            // 골드량에 따른 크기와 색상 조정
            float scale = GetGoldScale(goldAmount.Value);
            transform.localScale = Vector3.one * scale;
            
            spriteRenderer.color = GetGoldColor(goldAmount.Value);
            
            // 정렬 순서 설정
            spriteRenderer.sortingLayerName = "Items";
            spriteRenderer.sortingOrder = 2; // 아이템보다 위에
            
            // 게임 오브젝트 이름 설정
            gameObject.name = $"GoldDrop_{goldAmount.Value}";
            
            // 기본 골드 스프라이트 생성 (없는 경우)
            if (spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = CreateGoldSprite();
            }
        }
        
        /// <summary>
        /// 골드량에 따른 크기 계산
        /// </summary>
        private float GetGoldScale(long amount)
        {
            if (amount <= 10) return 0.8f;
            if (amount <= 50) return 1.0f;
            if (amount <= 100) return 1.2f;
            if (amount <= 500) return 1.4f;
            return 1.6f;
        }
        
        /// <summary>
        /// 골드량에 따른 색상 계산
        /// </summary>
        private Color GetGoldColor(long amount)
        {
            if (amount <= 10) return new Color(1f, 0.8f, 0.3f); // 연한 금색
            if (amount <= 50) return new Color(1f, 0.9f, 0.1f); // 일반 금색
            if (amount <= 100) return new Color(1f, 0.7f, 0f);  // 진한 금색
            if (amount <= 500) return new Color(1f, 0.5f, 0f);  // 오렌지 금색
            return new Color(1f, 0.3f, 0f); // 적금색
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
        /// 자석 효과 확인
        /// </summary>
        private void CheckForMagnetEffect()
        {
            if (!IsServer) return;
            
            Collider2D[] players = Physics2D.OverlapCircleAll(transform.position, magnetRange, playerLayerMask);
            
            foreach (var playerCollider in players)
            {
                var playerController = playerCollider.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    // 자석 효과 시작
                    StartMagnetEffect(playerController.transform);
                    return;
                }
            }
        }
        
        /// <summary>
        /// 자석 효과 시작
        /// </summary>
        private void StartMagnetEffect(Transform target)
        {
            isBeingMagnetized = true;
            magnetTarget = target;
        }
        
        /// <summary>
        /// 자석 이동 업데이트
        /// </summary>
        private void UpdateMagnetMovement()
        {
            if (magnetTarget == null)
            {
                isBeingMagnetized = false;
                return;
            }
            
            Vector3 direction = (magnetTarget.position - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, magnetTarget.position);
            
            if (distance <= pickupRange)
            {
                // 픽업 범위에 도달하면 자석 효과 중지하고 픽업 시도
                isBeingMagnetized = false;
                CheckForPickup();
            }
            else
            {
                // 플레이어 쪽으로 이동
                transform.position += direction * magnetSpeed * Time.deltaTime;
            }
        }
        
        /// <summary>
        /// 플레이어 픽업 감지
        /// </summary>
        private void CheckForPickup()
        {
            if (!IsServer) return;
            
            Collider2D[] players = Physics2D.OverlapCircleAll(transform.position, pickupRange, playerLayerMask);
            
            foreach (var playerCollider in players)
            {
                var playerController = playerCollider.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    // 플레이어에게 골드 지급
                    var statsManager = playerController.GetComponent<PlayerStatsManager>();
                    if (statsManager != null)
                    {
                        statsManager.ChangeGold(goldAmount.Value);
                        
                        // 픽업 효과 재생
                        PlayPickupEffectClientRpc(goldAmount.Value);
                        
                        // 골드 드롭 제거
                        if (NetworkObject.IsSpawned)
                        {
                            NetworkObject.Despawn();
                        }
                        else
                        {
                            Destroy(gameObject);
                        }
                        return;
                    }
                }
            }
        }
        
        /// <summary>
        /// 픽업 이펙트 재생
        /// </summary>
        [ClientRpc]
        private void PlayPickupEffectClientRpc(long amount)
        {
            // 골드 픽업 사운드나 파티클 효과 재생
            // TODO: 골드 픽업 효과 구현
            Debug.Log($"💰 Picked up {amount} gold!");
        }
        
        /// <summary>
        /// 기본 골드 스프라이트 생성
        /// </summary>
        private Sprite CreateGoldSprite()
        {
            int size = 16;
            Texture2D texture = new Texture2D(size, size);
            
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.4f;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    
                    if (distance <= radius)
                    {
                        // 골드 색상
                        float intensity = 1f - (distance / radius) * 0.3f;
                        texture.SetPixel(x, y, new Color(1f, 0.8f, 0.1f, intensity));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }
            
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
        
        /// <summary>
        /// 수동 픽업 (상호작용 키)
        /// </summary>
        public void PickupGold(PlayerController player)
        {
            if (!IsServer) return;
            
            var statsManager = player.GetComponent<PlayerStatsManager>();
            if (statsManager != null)
            {
                statsManager.ChangeGold(goldAmount.Value);
                PlayPickupEffectClientRpc(goldAmount.Value);
                
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
        
        // 디버그용 기즈모
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pickupRange);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, magnetRange);
        }
    }
}