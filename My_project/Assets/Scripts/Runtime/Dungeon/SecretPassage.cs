using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 던전 비밀 통로 시스템
    /// 숨겨진 지역이나 특별한 보상을 제공하는 비밀 통로
    /// </summary>
    public class SecretPassage : NetworkBehaviour
    {
        [Header("비밀 통로 설정")]
        [SerializeField] private SecretType secretType = SecretType.HiddenRoom;
        [SerializeField] private bool isDiscovered = false;
        [SerializeField] private float discoveryRadius = 1.5f;
        [SerializeField] private int discoveryRequirement = 1; // 발견에 필요한 플레이어 수
        
        [Header("시각 효과")]
        [SerializeField] private GameObject hiddenVisualPrefab;
        [SerializeField] private GameObject revealedVisualPrefab;
        [SerializeField] private ParticleSystem discoveryEffect;
        [SerializeField] private AudioSource secretAudioSource;
        [SerializeField] private AudioClip discoverySound;
        
        [Header("보상 설정")]
        [SerializeField] private bool giveReward = true;
        [SerializeField] private int bonusExp = 100;
        [SerializeField] private int bonusGold = 50;
        
        // 비밀 통로 상태
        private NetworkVariable<bool> isRevealed = new NetworkVariable<bool>(false);
        private Collider2D secretCollider;
        private DungeonEnvironment dungeonEnvironment;
        
        // 비밀 통로 데이터
        private int floorLevel = 1;
        private Vector3 teleportDestination;
        
        // 프로퍼티
        public SecretType SecretType => secretType;
        public bool IsRevealed => isRevealed.Value;
        public bool IsDiscovered => isDiscovered;
        
        private void Awake()
        {
            secretCollider = GetComponent<Collider2D>();
            if (secretCollider == null)
            {
                secretCollider = gameObject.AddComponent<CircleCollider2D>();
                ((CircleCollider2D)secretCollider).radius = discoveryRadius;
                secretCollider.isTrigger = true;
            }
            
            if (secretAudioSource == null)
            {
                secretAudioSource = gameObject.AddComponent<AudioSource>();
                secretAudioSource.playOnAwake = false;
            }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            isRevealed.OnValueChanged += OnSecretStateChanged;
            
            SetupSecretVisual();
            ConfigureSecret();
        }
        
        public override void OnNetworkDespawn()
        {
            isRevealed.OnValueChanged -= OnSecretStateChanged;
            
            base.OnNetworkDespawn();
        }
        
        /// <summary>
        /// 비밀 통로 초기화
        /// </summary>
        public void Initialize(int floor, DungeonEnvironment environment)
        {
            floorLevel = floor;
            dungeonEnvironment = environment;
            
            ConfigureSecretByFloor();
        }
        
        /// <summary>
        /// 층별 비밀 통로 설정
        /// </summary>
        private void ConfigureSecretByFloor()
        {
            // 층별 비밀 타입 및 보상 조정
            float floorMultiplier = 1f + (floorLevel * 0.3f);
            bonusExp = (int)(bonusExp * floorMultiplier);
            bonusGold = (int)(bonusGold * floorMultiplier);
            
            // 높은 층일수록 더 좋은 비밀 통로
            if (floorLevel >= 7)
            {
                secretType = SecretType.TreasureVault;
                discoveryRequirement = 2; // 2명이 동시에 있어야 발견
            }
            else if (floorLevel >= 4)
            {
                secretType = SecretType.ShortCut;
                discoveryRequirement = 1;
            }
            else
            {
                secretType = SecretType.HiddenRoom;
                discoveryRequirement = 1;
            }
        }
        
        /// <summary>
        /// 비밀 통로 기본 설정
        /// </summary>
        private void ConfigureSecret()
        {
            switch (secretType)
            {
                case SecretType.HiddenRoom:
                    // 숨겨진 방 - 추가 아이템 스폰
                    break;
                    
                case SecretType.ShortCut:
                    // 지름길 - 다음 층으로 바로 이동
                    break;
                    
                case SecretType.TreasureVault:
                    // 보물 창고 - 고급 아이템 보장
                    bonusGold *= 3;
                    bonusExp *= 2;
                    break;
                    
                case SecretType.HealingSpring:
                    // 치유의 샘 - 체력/마나 회복
                    break;
            }
        }
        
        /// <summary>
        /// 비밀 통로 시각적 표현 설정
        /// </summary>
        private void SetupSecretVisual()
        {
            if (!isRevealed.Value && hiddenVisualPrefab != null)
            {
                var visual = Instantiate(hiddenVisualPrefab, transform);
                visual.transform.localPosition = Vector3.zero;
                visual.SetActive(false); // 처음엔 숨김
            }
            
            // 매우 희미하게 표시
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Color color = GetSecretColor();
                color.a = isRevealed.Value ? 1f : 0.1f; // 발견되기 전엔 거의 투명
                spriteRenderer.color = color;
            }
        }
        
        /// <summary>
        /// 비밀 타입별 색상
        /// </summary>
        private Color GetSecretColor()
        {
            return secretType switch
            {
                SecretType.HiddenRoom => Color.blue,
                SecretType.ShortCut => Color.green,
                SecretType.TreasureVault => Color.gold,
                SecretType.HealingSpring => Color.cyan,
                _ => Color.white
            };
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isRevealed.Value) return;
            
            var playerController = other.GetComponent<PlayerController>();
            if (playerController == null) return;
            
            // 서버에서 발견 체크
            if (IsServer)
            {
                CheckDiscovery();
            }
        }
        
        private void OnTriggerStay2D(Collider2D other)
        {
            if (!isRevealed.Value) return;
            
            var playerController = other.GetComponent<PlayerController>();
            if (playerController == null) return;
            
            // E키 입력으로 비밀 통로 활성화
            if (Input.GetKeyDown(KeyCode.E) && NetworkManager.Singleton.LocalClientId == playerController.NetworkObjectId)
            {
                ActivateSecretServerRpc(playerController.NetworkObjectId);
            }
        }
        
        /// <summary>
        /// 비밀 통로 발견 체크
        /// </summary>
        private void CheckDiscovery()
        {
            if (!IsServer || isRevealed.Value) return;
            
            // 주변 플레이어 수 체크
            Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, discoveryRadius);
            int playerCount = 0;
            
            foreach (var collider in nearbyColliders)
            {
                if (collider.GetComponent<PlayerController>() != null)
                {
                    playerCount++;
                }
            }
            
            if (playerCount >= discoveryRequirement)
            {
                RevealSecret();
            }
        }
        
        /// <summary>
        /// 비밀 통로 발견
        /// </summary>
        private void RevealSecret()
        {
            if (!IsServer) return;
            
            isRevealed.Value = true;
            isDiscovered = true;
            
            // 발견 효과 표시
            ShowDiscoveryEffectClientRpc();
            
            Debug.Log($"🔍 Secret passage revealed: {secretType}");
        }
        
        /// <summary>
        /// 비밀 통로 활성화
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ActivateSecretServerRpc(ulong playerId)
        {
            if (!IsServer || !isRevealed.Value) return;
            
            var playerObject = NetworkManager.Singleton.ConnectedClients[playerId].PlayerObject;
            if (playerObject == null) return;
            
            var playerController = playerObject.GetComponent<PlayerController>();
            if (playerController == null) return;
            
            ActivateSecret(playerController);
        }
        
        /// <summary>
        /// 비밀 통로 활성화 처리
        /// </summary>
        public void ActivateSecret(PlayerController player)
        {
            if (!IsServer) return;
            
            switch (secretType)
            {
                case SecretType.HiddenRoom:
                    ActivateHiddenRoom(player);
                    break;
                    
                case SecretType.ShortCut:
                    ActivateShortCut(player);
                    break;
                    
                case SecretType.TreasureVault:
                    ActivateTreasureVault(player);
                    break;
                    
                case SecretType.HealingSpring:
                    ActivateHealingSpring(player);
                    break;
            }
            
            // 환경 매니저에 알림
            dungeonEnvironment?.OnSecretFoundByPlayer(this, player);
            
            // 보상 지급
            if (giveReward)
            {
                GiveSecretReward(player);
            }
        }
        
        /// <summary>
        /// 숨겨진 방 활성화
        /// </summary>
        private void ActivateHiddenRoom(PlayerController player)
        {
            // 추가 아이템 스폰 또는 특별한 몬스터 소환
            ShowSecretMessageClientRpc(player.NetworkObjectId, "숨겨진 방을 발견했습니다!");
        }
        
        /// <summary>
        /// 지름길 활성화
        /// </summary>
        private void ActivateShortCut(PlayerController player)
        {
            // 다음 층으로 즉시 이동 (DungeonManager와 연동 필요)
            ShowSecretMessageClientRpc(player.NetworkObjectId, "비밀 지름길을 발견했습니다!");
        }
        
        /// <summary>
        /// 보물 창고 활성화
        /// </summary>
        private void ActivateTreasureVault(PlayerController player)
        {
            // 고급 아이템 보장 드롭
            ShowSecretMessageClientRpc(player.NetworkObjectId, "비밀 보물 창고를 발견했습니다!");
        }
        
        /// <summary>
        /// 치유의 샘 활성화
        /// </summary>
        private void ActivateHealingSpring(PlayerController player)
        {
            // 체력/마나 완전 회복
            var statsManager = player.GetComponent<PlayerStatsManager>();
            if (statsManager != null)
            {
                statsManager.RestoreFullHealth();
                statsManager.RestoreFullMana();
            }
            
            ShowSecretMessageClientRpc(player.NetworkObjectId, "치유의 샘을 발견했습니다! 체력과 마나가 회복되었습니다!");
        }
        
        /// <summary>
        /// 비밀 통로 보상 지급
        /// </summary>
        private void GiveSecretReward(PlayerController player)
        {
            var statsManager = player.GetComponent<PlayerStatsManager>();
            if (statsManager == null) return;
            
            // 경험치 및 골드 보상
            statsManager.AddExperience(bonusExp);
            statsManager.AddGold(bonusGold);
            
            ShowRewardMessageClientRpc(player.NetworkObjectId, bonusExp, bonusGold);
        }
        
        /// <summary>
        /// 비밀 상태 변경 이벤트
        /// </summary>
        private void OnSecretStateChanged(bool previousValue, bool newValue)
        {
            if (newValue)
            {
                PlayDiscoveryEffect();
                UpdateSecretVisual();
            }
        }
        
        /// <summary>
        /// 발견 효과 재생
        /// </summary>
        private void PlayDiscoveryEffect()
        {
            if (discoveryEffect != null)
            {
                discoveryEffect.Play();
            }
            
            if (secretAudioSource != null && discoverySound != null)
            {
                secretAudioSource.PlayOneShot(discoverySound);
            }
        }
        
        /// <summary>
        /// 비밀 통로 시각 업데이트
        /// </summary>
        private void UpdateSecretVisual()
        {
            // 숨겨진 모델 제거하고 발견된 모델 생성
            Transform hiddenModel = transform.Find("HiddenVisual");
            if (hiddenModel != null)
            {
                hiddenModel.gameObject.SetActive(false);
            }
            
            if (revealedVisualPrefab != null)
            {
                var revealedModel = Instantiate(revealedVisualPrefab, transform);
                revealedModel.transform.localPosition = Vector3.zero;
            }
            
            // 스프라이트 완전히 표시
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Color color = GetSecretColor();
                color.a = 1f;
                spriteRenderer.color = color;
            }
        }
        
        // ClientRpc 메서드들
        [ClientRpc]
        private void ShowDiscoveryEffectClientRpc()
        {
            PlayDiscoveryEffect();
            UpdateSecretVisual();
        }
        
        [ClientRpc]
        private void ShowSecretMessageClientRpc(ulong targetPlayerId, string message)
        {
            if (NetworkManager.Singleton.LocalClientId == targetPlayerId)
            {
                Debug.Log($"🔍 {message}");
            }
        }
        
        [ClientRpc]
        private void ShowRewardMessageClientRpc(ulong targetPlayerId, int expReward, int goldReward)
        {
            if (NetworkManager.Singleton.LocalClientId == targetPlayerId)
            {
                Debug.Log($"🎁 비밀 보상: 경험치 {expReward}, 골드 {goldReward}");
            }
        }
    }
    
    /// <summary>
    /// 비밀 통로 타입 열거형
    /// </summary>
    public enum SecretType
    {
        HiddenRoom,     // 숨겨진 방
        ShortCut,       // 지름길
        TreasureVault,  // 보물 창고
        HealingSpring   // 치유의 샘
    }
}