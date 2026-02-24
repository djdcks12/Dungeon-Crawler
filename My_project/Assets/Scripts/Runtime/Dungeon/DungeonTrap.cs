using UnityEngine;
using Unity.Netcode;
using System.Collections;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 던전 내 함정 시스템
    /// 플레이어가 트리거할 수 있는 다양한 함정들
    /// </summary>
    public class DungeonTrap : NetworkBehaviour
    {
        [Header("함정 설정")]
        [SerializeField] private TrapType trapType = TrapType.SpikeTrap;
        [SerializeField] private float damage = 25f;
        [SerializeField] private float triggerRadius = 1.5f;
        [SerializeField] private bool isOneTime = true;
        [SerializeField] private float cooldownTime = 3f;
        
        [Header("시각 효과")]
        [SerializeField] private GameObject trapVisualPrefab;
        [SerializeField] private ParticleSystem activationEffect;
        [SerializeField] private AudioSource trapAudioSource;
        [SerializeField] private AudioClip activationSound;
        
        // 함정 상태
        private NetworkVariable<bool> isTriggered = new NetworkVariable<bool>(false);
        private NetworkVariable<bool> isActive = new NetworkVariable<bool>(true);
        private float lastTriggerTime = 0f;
        private Collider2D trapCollider;
        private DungeonEnvironment dungeonEnvironment;
        
        // 함정 데이터
        private float floorMultiplier = 1f;
        
        // 프로퍼티
        public TrapType TrapType => trapType;
        public bool IsActive => isActive.Value;
        public float Damage => damage * floorMultiplier;
        
        private void Awake()
        {
            trapCollider = GetComponent<Collider2D>();
            if (trapCollider == null)
            {
                trapCollider = gameObject.AddComponent<CircleCollider2D>();
                ((CircleCollider2D)trapCollider).radius = triggerRadius;
                trapCollider.isTrigger = true;
            }
            
            if (trapAudioSource == null)
            {
                trapAudioSource = gameObject.AddComponent<AudioSource>();
                trapAudioSource.playOnAwake = false;
            }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            isTriggered.OnValueChanged += OnTrapStateChanged;
            isActive.OnValueChanged += OnTrapActiveStateChanged;
            
            SetupTrapVisual();
        }
        
        public override void OnNetworkDespawn()
        {
            isTriggered.OnValueChanged -= OnTrapStateChanged;
            isActive.OnValueChanged -= OnTrapActiveStateChanged;
            
            base.OnNetworkDespawn();
        }
        
        /// <summary>
        /// 함정 초기화
        /// </summary>
        public void Initialize(TrapType type, float multiplier, DungeonEnvironment environment)
        {
            trapType = type;
            floorMultiplier = multiplier;
            dungeonEnvironment = environment;
            
            ConfigureTrapByType();
        }
        
        /// <summary>
        /// 함정 타입별 설정
        /// </summary>
        private void ConfigureTrapByType()
        {
            switch (trapType)
            {
                case TrapType.SpikeTrap:
                    damage = 30f;
                    triggerRadius = 1.2f;
                    isOneTime = false;
                    cooldownTime = 2f;
                    break;
                    
                case TrapType.PoisonTrap:
                    damage = 15f;
                    triggerRadius = 2f;
                    isOneTime = false;
                    cooldownTime = 5f;
                    break;
                    
                case TrapType.FireTrap:
                    damage = 45f;
                    triggerRadius = 1.5f;
                    isOneTime = false;
                    cooldownTime = 4f;
                    break;
                    
                case TrapType.FreezeTrap:
                    damage = 20f;
                    triggerRadius = 1.8f;
                    isOneTime = false;
                    cooldownTime = 6f;
                    break;
                    
                case TrapType.ExplosionTrap:
                    damage = 80f;
                    triggerRadius = 3f;
                    isOneTime = true;
                    cooldownTime = 0f;
                    break;
                    
                case TrapType.PitTrap:
                    damage = 50f;
                    triggerRadius = 1.5f;
                    isOneTime = true;
                    cooldownTime = 0f;
                    break;
            }
        }
        
        /// <summary>
        /// 함정 시각적 표현 설정
        /// </summary>
        private void SetupTrapVisual()
        {
            if (trapVisualPrefab != null)
            {
                var visual = Instantiate(trapVisualPrefab, transform);
                visual.transform.localPosition = Vector3.zero;
            }
            
            // 함정 타입별 색상 설정
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = GetTrapColor();
            }
        }
        
        /// <summary>
        /// 함정 타입별 색상
        /// </summary>
        private Color GetTrapColor()
        {
            return trapType switch
            {
                TrapType.SpikeTrap => Color.gray,
                TrapType.PoisonTrap => Color.green,
                TrapType.FireTrap => Color.red,
                TrapType.FreezeTrap => Color.cyan,
                TrapType.ExplosionTrap => Color.yellow,
                TrapType.PitTrap => Color.black,
                _ => Color.white
            };
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer || !isActive.Value) return;
            
            var playerController = other.GetComponent<PlayerController>();
            if (playerController == null) return;
            
            // 쿨다운 체크
            if (!isOneTime && Time.time < lastTriggerTime + cooldownTime) return;
            
            TriggerTrap(playerController);
        }
        
        /// <summary>
        /// 함정 발동
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void TriggerTrapServerRpc(ulong playerId)
        {
            if (!IsServer || !isActive.Value) return;
            
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(playerId, out var trapClient)) return;
            var playerObject = trapClient.PlayerObject;
            if (playerObject == null) return;

            var playerController = playerObject.GetComponent<PlayerController>();
            if (playerController == null) return;

            TriggerTrap(playerController);
        }
        
        /// <summary>
        /// 함정 발동 처리
        /// </summary>
        private void TriggerTrap(PlayerController player)
        {
            if (!IsServer) return;
            
            lastTriggerTime = Time.time;
            isTriggered.Value = true;
            
            // 데미지 적용
            ApplyTrapDamage(player);
            
            // 특수 효과 적용
            ApplyTrapEffect(player);
            
            // 환경 매니저에 알림
            dungeonEnvironment?.OnTrapTriggeredByPlayer(this, player);
            
            // 클라이언트에 효과 표시
            ShowTrapEffectClientRpc(player.NetworkObjectId, trapType);
            
            // 일회성 함정이면 비활성화
            if (isOneTime)
            {
                isActive.Value = false;
                StartCoroutine(DestroyTrapAfterDelay(2f));
            }
            else
            {
                StartCoroutine(ResetTrapAfterDelay(cooldownTime));
            }
        }
        
        /// <summary>
        /// 함정 데미지 적용
        /// </summary>
        private void ApplyTrapDamage(PlayerController player)
        {
            var statsManager = player.GetComponent<PlayerStatsManager>();
            if (statsManager == null) return;
            
            float finalDamage = Damage;
            DamageType damageType = GetTrapDamageType();
            
            statsManager.TakeDamage(finalDamage, damageType);
        }
        
        /// <summary>
        /// 함정별 데미지 타입
        /// </summary>
        private DamageType GetTrapDamageType()
        {
            return trapType switch
            {
                TrapType.SpikeTrap => DamageType.Physical,
                TrapType.PoisonTrap => DamageType.Magical,
                TrapType.FireTrap => DamageType.Magical,
                TrapType.FreezeTrap => DamageType.Magical,
                TrapType.ExplosionTrap => DamageType.Physical,
                TrapType.PitTrap => DamageType.Physical,
                _ => DamageType.Physical
            };
        }
        
        /// <summary>
        /// 함정 특수 효과 적용
        /// </summary>
        private void ApplyTrapEffect(PlayerController player)
        {
            switch (trapType)
            {
                case TrapType.PoisonTrap:
                    ApplyPoisonEffect(player);
                    break;
                case TrapType.FreezeTrap:
                    ApplyFreezeEffect(player);
                    break;
                case TrapType.ExplosionTrap:
                    ApplyExplosionEffect(player);
                    break;
            }
        }
        
        /// <summary>
        /// 독 효과 적용
        /// </summary>
        private void ApplyPoisonEffect(PlayerController player)
        {
            StartCoroutine(PoisonDamageCoroutine(player, 5f, 3f)); // 3초간 5초마다 데미지
        }
        
        /// <summary>
        /// 빙결 효과 적용
        /// </summary>
        private void ApplyFreezeEffect(PlayerController player)
        {
            // PlayerController에 이동 속도 감소 적용 (구현 필요)
            ApplyMovementSlowClientRpc(player.NetworkObjectId, 0.5f, 3f);
        }
        
        /// <summary>
        /// 폭발 효과 적용
        /// </summary>
        private void ApplyExplosionEffect(PlayerController player)
        {
            // 넉백 효과 (구현 필요)
            ApplyKnockbackClientRpc(player.NetworkObjectId, (player.transform.position - transform.position).normalized * 5f);
        }
        
        /// <summary>
        /// 독 데미지 코루틴
        /// </summary>
        private IEnumerator PoisonDamageCoroutine(PlayerController player, float damagePerTick, float duration)
        {
            float elapsed = 0f;
            var statsManager = player != null ? player.GetComponent<PlayerStatsManager>() : null;

            while (elapsed < duration)
            {
                yield return new WaitForSeconds(1f);
                elapsed += 1f;

                // yield 후 유효성 검증
                if (player == null || statsManager == null || statsManager.IsDead) yield break;
                if (!IsSpawned) yield break;

                statsManager.TakeDamage(damagePerTick, DamageType.Magical);
            }
        }
        
        /// <summary>
        /// 함정 리셋 코루틴
        /// </summary>
        private IEnumerator ResetTrapAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            isTriggered.Value = false;
        }
        
        /// <summary>
        /// 함정 파괴 코루틴
        /// </summary>
        private IEnumerator DestroyTrapAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (IsSpawned && NetworkObject != null)
            {
                NetworkObject.Despawn();
            }
        }
        
        /// <summary>
        /// 함정 상태 변경 이벤트
        /// </summary>
        private void OnTrapStateChanged(bool previousValue, bool newValue)
        {
            if (newValue)
            {
                PlayActivationEffect();
            }
        }
        
        /// <summary>
        /// 함정 활성 상태 변경 이벤트
        /// </summary>
        private void OnTrapActiveStateChanged(bool previousValue, bool newValue)
        {
            trapCollider.enabled = newValue;
            
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = newValue ? GetTrapColor() : Color.gray;
            }
        }
        
        /// <summary>
        /// 활성화 효과 재생
        /// </summary>
        private void PlayActivationEffect()
        {
            if (activationEffect != null)
            {
                activationEffect.Play();
            }
            
            if (trapAudioSource != null && activationSound != null)
            {
                trapAudioSource.PlayOneShot(activationSound);
            }
        }
        
        // ClientRpc 메서드들
        [ClientRpc]
        private void ShowTrapEffectClientRpc(ulong targetPlayerId, TrapType type)
        {
            if (NetworkManager.Singleton.LocalClientId == targetPlayerId)
            {
                string effectMessage = type switch
                {
                    TrapType.SpikeTrap => "가시 함정에 찔렸습니다!",
                    TrapType.PoisonTrap => "독 함정에 중독되었습니다!",
                    TrapType.FireTrap => "화염 함정에 화상을 입었습니다!",
                    TrapType.FreezeTrap => "빙결 함정에 얼어붙었습니다!",
                    TrapType.ExplosionTrap => "폭발 함정에 휘말렸습니다!",
                    TrapType.PitTrap => "함정에 빠졌습니다!",
                    _ => "함정에 걸렸습니다!"
                };
                
                Debug.Log($"⚠️ {effectMessage}");
            }
        }
        
        [ClientRpc]
        private void ApplyMovementSlowClientRpc(ulong targetPlayerId, float slowMultiplier, float duration)
        {
            if (NetworkManager.Singleton.LocalClientId == targetPlayerId)
            {
                // PlayerController에 이동 속도 감소 적용 (구현 필요)
                Debug.Log($"🧊 Movement slowed: {slowMultiplier}x for {duration}s");
            }
        }
        
        [ClientRpc]
        private void ApplyKnockbackClientRpc(ulong targetPlayerId, Vector3 knockbackForce)
        {
            if (NetworkManager.Singleton.LocalClientId == targetPlayerId)
            {
                // PlayerController에 넉백 적용 (구현 필요)
                Debug.Log($"💥 Knockback applied: {knockbackForce}");
            }
        }

        public override void OnDestroy()
        {
            StopAllCoroutines();
            base.OnDestroy();
        }
    }
    
    /// <summary>
    /// 함정 타입 열거형
    /// </summary>
    public enum TrapType
    {
        SpikeTrap,      // 가시 함정
        PoisonTrap,     // 독 함정
        FireTrap,       // 화염 함정
        FreezeTrap,     // 빙결 함정
        ExplosionTrap,  // 폭발 함정
        PitTrap         // 구덩이 함정
    }
}