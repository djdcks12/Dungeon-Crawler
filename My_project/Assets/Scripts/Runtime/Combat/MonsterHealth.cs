using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 몬스터 체력 관리 시스템
    /// 체력, 사망 처리, 드롭 아이템 관리
    /// </summary>
    public class MonsterHealth : NetworkBehaviour
    {
        [Header("체력 설정")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth = 100;
        
        [Header("리젠 설정")]
        [SerializeField] private bool enableRegeneration = false;
        [SerializeField] private float regenRate = 1f; // 초당 회복량
        [SerializeField] private float regenDelay = 5f; // 데미지 후 회복 시작 딜레이
        
        // 네트워크 동기화
        private NetworkVariable<int> networkCurrentHealth = new NetworkVariable<int>();
        private NetworkVariable<int> networkMaxHealth = new NetworkVariable<int>();
        private NetworkVariable<bool> networkIsDead = new NetworkVariable<bool>();
        
        // 상태
        private float lastDamageTime = 0f;
        private bool isDead = false;
        
        // 이벤트
        public System.Action<int, int> OnHealthChanged; // (current, max)
        public System.Action OnDeath;
        public System.Action<float> OnDamageTaken; // (damage)
        
        // 프로퍼티
        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsDead => isDead;
        public float HealthPercentage => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
        
        // 몬스터 정보 설정 (스포너에서 사용)
        public void SetMonsterInfo(string monsterName, int level, string origin, float health, long expReward)
        {
            maxHealth = Mathf.RoundToInt(health);
            currentHealth = maxHealth;
            
            if (IsServer)
            {
                networkMaxHealth.Value = maxHealth;
                networkCurrentHealth.Value = currentHealth;
                networkIsDead.Value = false;
            }
            
            Debug.Log($"Monster {monsterName} (Lv.{level}) spawned with {health} HP from {origin}");
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsServer)
            {
                // 서버에서 초기값 설정
                networkCurrentHealth.Value = currentHealth;
                networkMaxHealth.Value = maxHealth;
                networkIsDead.Value = isDead;
            }
            
            // 네트워크 변수 변경 이벤트 구독
            networkCurrentHealth.OnValueChanged += OnNetworkHealthChanged;
            networkMaxHealth.OnValueChanged += OnNetworkMaxHealthChanged;
            networkIsDead.OnValueChanged += OnNetworkDeathChanged;
        }
        
        public override void OnNetworkDespawn()
        {
            networkCurrentHealth.OnValueChanged -= OnNetworkHealthChanged;
            networkMaxHealth.OnValueChanged -= OnNetworkMaxHealthChanged;
            networkIsDead.OnValueChanged -= OnNetworkDeathChanged;
            
            base.OnNetworkDespawn();
        }
        
        private void Update()
        {
            if (IsServer && enableRegeneration && !isDead)
            {
                // 리젠 처리
                ProcessRegeneration();
            }
        }
        
        /// <summary>
        /// 데미지 받기
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(float damage, DamageType damageType = DamageType.Physical)
        {
            TakeDamage(damage, damageType);
        }
        
        /// <summary>
        /// 데미지 처리 (서버에서만 호출)
        /// </summary>
        public float TakeDamage(float damage, DamageType damageType = DamageType.Physical)
        {
            if (!IsServer || isDead) return 0f;
            
            float finalDamage = damage;
            
            // 최소 1 데미지
            finalDamage = Mathf.Max(1f, finalDamage);
            
            // 체력 감소
            int intDamage = Mathf.RoundToInt(finalDamage);
            currentHealth = Mathf.Max(0, currentHealth - intDamage);
            
            // 네트워크 동기화
            networkCurrentHealth.Value = currentHealth;
            
            // 마지막 데미지 시간 기록
            lastDamageTime = Time.time;
            
            // 이벤트 호출
            OnDamageTaken?.Invoke(finalDamage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            
            // 사망 체크
            if (currentHealth <= 0 && !isDead)
            {
                Die();
            }
            
            Debug.Log($"{name} took {finalDamage:F1} damage. Health: {currentHealth}/{maxHealth}");
            return finalDamage;
        }
        
        /// <summary>
        /// 체력 회복
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void HealServerRpc(int amount)
        {
            Heal(amount);
        }
        
        /// <summary>
        /// 체력 회복 처리
        /// </summary>
        public void Heal(int amount)
        {
            if (!IsServer || isDead) return;
            
            int oldHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            
            if (currentHealth != oldHealth)
            {
                // 네트워크 동기화
                networkCurrentHealth.Value = currentHealth;
                
                // 이벤트 호출
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
                
                Debug.Log($"{name} healed {amount}. Health: {currentHealth}/{maxHealth}");
            }
        }
        
        /// <summary>
        /// 최대 체력 설정
        /// </summary>
        public void SetMaxHealth(int newMaxHealth)
        {
            if (!IsServer) return;
            
            maxHealth = newMaxHealth;
            
            // 현재 체력이 최대 체력을 초과하지 않도록
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            
            // 네트워크 동기화
            networkMaxHealth.Value = maxHealth;
            networkCurrentHealth.Value = currentHealth;
            
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
        
        /// <summary>
        /// 체력 설정 (MonsterAI.TakeDamage에서 사용)
        /// </summary>
        public void SetHealth(float newHealth)
        {
            if (!IsServer) return;
            
            int intHealth = Mathf.RoundToInt(newHealth);
            currentHealth = Mathf.Clamp(intHealth, 0, maxHealth);
            
            // 네트워크 동기화
            networkCurrentHealth.Value = currentHealth;
            
            // 이벤트 호출
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            
            // 사망 체크
            if (currentHealth <= 0 && !isDead)
            {
                Die();
            }
        }
        
        /// <summary>
        /// 체력 완전 회복
        /// </summary>
        public void FullHeal()
        {
            if (!IsServer) return;
            
            currentHealth = maxHealth;
            networkCurrentHealth.Value = currentHealth;
            
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
        
        /// <summary>
        /// 사망 처리
        /// </summary>
        private void Die()
        {
            if (isDead) return;
            
            isDead = true;
            networkIsDead.Value = true;
            
            // 사망 이벤트 호출
            OnDeath?.Invoke();
            
            // 보스인 경우 BossSpawner에 알림
            var bossAI = GetComponent<BossMonsterAI>();
            if (bossAI != null && BossSpawner.Instance != null)
            {
                BossSpawner.Instance.OnBossDefeated(bossAI.TargetFloor, bossAI.BossType);
            }
            
            Debug.Log($"{name} has died");
            
            // 일정 시간 후 오브젝트 삭제
            Invoke(nameof(DestroyMonster), 3f);
        }
        
        /// <summary>
        /// 몬스터 오브젝트 삭제
        /// </summary>
        private void DestroyMonster()
        {
            if (IsServer)
            {
                if (NetworkObject != null && NetworkObject.IsSpawned)
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
        /// 리젠 처리
        /// </summary>
        private void ProcessRegeneration()
        {
            if (currentHealth >= maxHealth) return;
            if (Time.time < lastDamageTime + regenDelay) return;
            
            float regenAmount = regenRate * Time.deltaTime;
            if (regenAmount >= 1f)
            {
                Heal(Mathf.FloorToInt(regenAmount));
            }
        }
        
        /// <summary>
        /// 부활 (보스 전용)
        /// </summary>
        public void Resurrect(float healthPercentage = 1f)
        {
            if (!IsServer) return;
            
            isDead = false;
            networkIsDead.Value = false;
            
            int resurrectHealth = Mathf.RoundToInt(maxHealth * healthPercentage);
            currentHealth = resurrectHealth;
            networkCurrentHealth.Value = currentHealth;
            
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            
            Debug.Log($"{name} resurrected with {currentHealth}/{maxHealth} HP");
        }
        
        // 네트워크 이벤트 처리
        private void OnNetworkHealthChanged(int previousValue, int newValue)
        {
            if (IsServer) return;
            
            currentHealth = newValue;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
        
        private void OnNetworkMaxHealthChanged(int previousValue, int newValue)
        {
            if (IsServer) return;
            
            maxHealth = newValue;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
        
        private void OnNetworkDeathChanged(bool previousValue, bool newValue)
        {
            if (IsServer) return;
            
            isDead = newValue;
            if (isDead)
            {
                OnDeath?.Invoke();
            }
        }
        
        /// <summary>
        /// 디버그 정보
        /// </summary>
        [ContextMenu("Show Health Debug Info")]
        private void ShowDebugInfo()
        {
            Debug.Log($"=== {name} Health Debug ===");
            Debug.Log($"Health: {currentHealth}/{maxHealth} ({HealthPercentage:P1})");
            Debug.Log($"Dead: {isDead}");
            Debug.Log($"Regen: {enableRegeneration} (Rate: {regenRate}/sec)");
            Debug.Log($"Last Damage: {Time.time - lastDamageTime:F1}s ago");
        }
    }
}