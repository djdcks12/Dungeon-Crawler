using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 파괴 가능한 던전 오브젝트
    /// 플레이어가 공격해서 파괴할 수 있는 오브젝트
    /// </summary>
    public class DestructibleObject : NetworkBehaviour
    {
        [Header("파괴 오브젝트 설정")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool dropRewards = true;
        [SerializeField] private float dropChance = 0.3f;
        
        [Header("시각 효과")]
        [SerializeField] private GameObject destroyEffectPrefab;
        [SerializeField] private AudioSource destructibleAudioSource;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip destroySound;
        
        // 오브젝트 상태
        private NetworkVariable<float> currentHealth = new NetworkVariable<float>();
        private NetworkVariable<bool> isDestroyed = new NetworkVariable<bool>(false);
        
        // 오브젝트 데이터
        private int floorLevel = 1;
        
        // 프로퍼티
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth.Value;
        public bool IsDestroyed => isDestroyed.Value;
        
        private void Awake()
        {
            if (destructibleAudioSource == null)
            {
                destructibleAudioSource = gameObject.AddComponent<AudioSource>();
                destructibleAudioSource.playOnAwake = false;
            }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsServer)
            {
                currentHealth.Value = maxHealth;
            }
            
            currentHealth.OnValueChanged += OnHealthChanged;
            isDestroyed.OnValueChanged += OnDestroyedStateChanged;
        }
        
        public override void OnNetworkDespawn()
        {
            currentHealth.OnValueChanged -= OnHealthChanged;
            isDestroyed.OnValueChanged -= OnDestroyedStateChanged;
            
            base.OnNetworkDespawn();
        }
        
        /// <summary>
        /// 파괴 오브젝트 초기화
        /// </summary>
        public void Initialize(float health, int floor)
        {
            maxHealth = health;
            floorLevel = floor;
            
            if (IsServer)
            {
                currentHealth.Value = maxHealth;
            }
        }
        
        /// <summary>
        /// 데미지 받기
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(float damage, ulong attackerId)
        {
            if (!IsServer || isDestroyed.Value) return;
            
            currentHealth.Value = Mathf.Max(0f, currentHealth.Value - damage);
            
            // 히트 효과
            PlayHitEffectClientRpc();
            
            if (currentHealth.Value <= 0f)
            {
                DestroyObject(attackerId);
            }
        }
        
        /// <summary>
        /// 오브젝트 파괴
        /// </summary>
        private void DestroyObject(ulong attackerId)
        {
            if (!IsServer) return;
            
            isDestroyed.Value = true;
            
            // 보상 드롭
            if (dropRewards && Random.value < dropChance)
            {
                DropRewards(attackerId);
            }
            
            // 파괴 효과
            PlayDestroyEffectClientRpc();
            
            // 오브젝트 제거
            StartCoroutine(DestroyAfterDelay(2f));
        }
        
        /// <summary>
        /// 보상 드롭
        /// </summary>
        private void DropRewards(ulong attackerId)
        {
            // 골드 드롭
            int goldAmount = Random.Range(5, 25) * floorLevel;
            
            // 아이템 드롭 (낮은 확률)
            if (Random.value < 0.1f)
            {
                // 아이템 드롭 로직 (추후 구현)
            }
            
            Debug.Log($"💰 Destructible object dropped {goldAmount} gold");
        }
        
        /// <summary>
        /// 지연 후 파괴
        /// </summary>
        private System.Collections.IEnumerator DestroyAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (IsSpawned && NetworkObject != null)
            {
                NetworkObject.Despawn();
            }
        }
        
        /// <summary>
        /// 체력 변경 이벤트
        /// </summary>
        private void OnHealthChanged(float previousValue, float newValue)
        {
            // 체력바 업데이트 등 (추후 구현)
        }
        
        /// <summary>
        /// 파괴 상태 변경 이벤트
        /// </summary>
        private void OnDestroyedStateChanged(bool previousValue, bool newValue)
        {
            if (newValue)
            {
                // 콜라이더 비활성화
                var collider = GetComponent<Collider2D>();
                if (collider != null)
                {
                    collider.enabled = false;
                }
                
                // 시각적 변화
                var spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.gray;
                }
            }
        }
        
        // ClientRpc 메서드들
        [ClientRpc]
        private void PlayHitEffectClientRpc()
        {
            if (destructibleAudioSource != null && hitSound != null)
            {
                destructibleAudioSource.PlayOneShot(hitSound);
            }
        }
        
        [ClientRpc]
        private void PlayDestroyEffectClientRpc()
        {
            if (destroyEffectPrefab != null)
            {
                var effect = Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 3f);
            }
            
            if (destructibleAudioSource != null && destroySound != null)
            {
                destructibleAudioSource.PlayOneShot(destroySound);
            }
        }

        public override void OnDestroy()
        {
            StopAllCoroutines();
            base.OnDestroy();
        }
    }
}