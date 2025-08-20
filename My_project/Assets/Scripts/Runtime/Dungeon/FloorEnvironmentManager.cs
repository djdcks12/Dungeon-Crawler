using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 층별 환경 효과 매니저
    /// FloorEnvironmentConfig 설정을 실제 게임에 적용하는 시스템
    /// </summary>
    public class FloorEnvironmentManager : NetworkBehaviour
    {
        [Header("환경 설정")]
        [SerializeField] private List<FloorEnvironmentConfig> floorConfigs = new List<FloorEnvironmentConfig>();
        [SerializeField] private bool enableEnvironmentEffects = true;
        
        [Header("시각 효과")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Light ambientLight;
        [SerializeField] private GameObject fogEffectPrefab;
        
        [Header("오디오")]
        [SerializeField] private AudioSource ambientAudioSource;
        
        // 현재 환경 상태
        private FloorEnvironmentConfig currentConfig;
        private Dictionary<ulong, Coroutine> playerEnvironmentCoroutines = new Dictionary<ulong, Coroutine>();
        
        // 환경 효과 오브젝트들
        private GameObject currentFogEffect;
        private List<GameObject> environmentEffects = new List<GameObject>();
        
        // 네트워크 변수
        private NetworkVariable<int> currentFloorEnvironment = new NetworkVariable<int>(-1);
        
        // 싱글톤 패턴
        private static FloorEnvironmentManager instance;
        public static FloorEnvironmentManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<FloorEnvironmentManager>();
                }
                return instance;
            }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (instance == null)
            {
                instance = this;
            }
            
            // DungeonManager 이벤트 구독
            if (DungeonManager.Instance != null)
            {
                DungeonManager.Instance.OnFloorChanged += OnFloorChanged;
            }
            
            // 네트워크 변수 이벤트 구독
            currentFloorEnvironment.OnValueChanged += OnFloorEnvironmentChanged;
            
            // 카메라 찾기
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }
        
        public override void OnNetworkDespawn()
        {
            if (DungeonManager.Instance != null)
            {
                DungeonManager.Instance.OnFloorChanged -= OnFloorChanged;
            }
            
            currentFloorEnvironment.OnValueChanged -= OnFloorEnvironmentChanged;
            
            // 모든 환경 효과 정리
            CleanupAllEnvironmentEffects();
            
            base.OnNetworkDespawn();
        }
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
            
            // 오디오 소스 설정
            if (ambientAudioSource == null)
            {
                ambientAudioSource = gameObject.AddComponent<AudioSource>();
                ambientAudioSource.loop = true;
                ambientAudioSource.playOnAwake = false;
            }
        }
        
        /// <summary>
        /// 층 변경 이벤트 처리
        /// </summary>
        private void OnFloorChanged(int newFloor)
        {
            if (!IsServer) return;
            
            ApplyFloorEnvironment(newFloor);
        }
        
        /// <summary>
        /// 층별 환경 효과 적용
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ApplyFloorEnvironmentServerRpc(int floor)
        {
            ApplyFloorEnvironment(floor);
        }
        
        /// <summary>
        /// 층별 환경 효과 적용 실행
        /// </summary>
        private void ApplyFloorEnvironment(int floor)
        {
            if (!enableEnvironmentEffects) return;
            
            // 이전 환경 효과 정리
            CleanupCurrentEnvironmentEffects();
            
            // 해당 층의 설정 찾기
            currentConfig = GetFloorConfig(floor);
            if (currentConfig == null)
            {
                // 기본 설정 생성
                currentConfig = CreateDefaultConfig(floor);
            }
            
            // 설정 검증
            if (!currentConfig.ValidateConfig())
            {
                Debug.LogError($"Invalid floor environment config for floor {floor}");
                return;
            }
            
            // 네트워크 동기화
            currentFloorEnvironment.Value = floor;
            
            // 환경 효과 적용
            StartCoroutine(ApplyEnvironmentEffectsCoroutine());
            
            Debug.Log($"🌍 Applied environment effects for floor {floor}: {currentConfig.EnvironmentName}");
        }
        
        /// <summary>
        /// 층별 설정 가져오기
        /// </summary>
        private FloorEnvironmentConfig GetFloorConfig(int floor)
        {
            foreach (var config in floorConfigs)
            {
                if (config.FloorNumber == floor)
                {
                    return config;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 기본 설정 생성
        /// </summary>
        private FloorEnvironmentConfig CreateDefaultConfig(int floor)
        {
            var config = ScriptableObject.CreateInstance<FloorEnvironmentConfig>();
            config.ApplyFloorPreset(floor);
            return config;
        }
        
        /// <summary>
        /// 환경 효과 적용 코루틴
        /// </summary>
        private IEnumerator ApplyEnvironmentEffectsCoroutine()
        {
            // 조명 효과 적용
            if (currentConfig.ModifyLighting)
            {
                ApplyLightingEffects();
            }
            
            yield return new WaitForSeconds(0.1f);
            
            // 시야 제한 효과
            if (currentConfig.EnableVisionLimit)
            {
                ApplyVisionLimitEffect();
            }
            
            yield return new WaitForSeconds(0.1f);
            
            // 환경 데미지 시작
            if (currentConfig.EnableEnvironmentDamage)
            {
                StartEnvironmentDamage();
            }
            
            yield return new WaitForSeconds(0.1f);
            
            // 이동 제약 효과
            if (currentConfig.EnableMovementRestriction)
            {
                ApplyMovementRestriction();
            }
            
            yield return new WaitForSeconds(0.1f);
            
            // 오디오 효과
            ApplyAudioEffects();
            
            yield return new WaitForSeconds(0.1f);
            
            // 함정 밀도 조정 (DungeonEnvironment에 알림)
            if (currentConfig.ModifyTrapDensity && DungeonEnvironment.Instance != null)
            {
                NotifyTrapDensityChange();
            }
            
            // 클라이언트에게 환경 효과 시작 알림
            NotifyEnvironmentStartClientRpc(currentConfig.EnvironmentName, currentConfig.Description);
        }
        
        /// <summary>
        /// 조명 효과 적용
        /// </summary>
        private void ApplyLightingEffects()
        {
            // 앰비언트 라이트 설정
            RenderSettings.ambientIntensity = currentConfig.AmbientLightIntensity;
            RenderSettings.ambientLight = currentConfig.AmbientLightColor;
            
            // 개별 라이트 설정
            if (ambientLight != null)
            {
                ambientLight.intensity = currentConfig.AmbientLightIntensity;
                ambientLight.color = currentConfig.AmbientLightColor;
                
                // 깜빡이는 효과
                if (currentConfig.EnableFlickeringLights)
                {
                    StartCoroutine(FlickeringLightCoroutine());
                }
            }
            
            Debug.Log($"💡 Applied lighting: Intensity {currentConfig.AmbientLightIntensity}, Color {currentConfig.AmbientLightColor}");
        }
        
        /// <summary>
        /// 시야 제한 효과 적용
        /// </summary>
        private void ApplyVisionLimitEffect()
        {
            // 포그 효과 생성
            if (fogEffectPrefab != null)
            {
                currentFogEffect = Instantiate(fogEffectPrefab);
                environmentEffects.Add(currentFogEffect);
            }
            
            // 렌더링 설정 조정
            if (playerCamera != null)
            {
                playerCamera.farClipPlane = currentConfig.VisionRange;
            }
            
            // 포그 설정
            RenderSettings.fog = true;
            RenderSettings.fogColor = currentConfig.FogColor;
            RenderSettings.fogDensity = currentConfig.FogDensity;
            RenderSettings.fogStartDistance = currentConfig.VisionRange * 0.5f;
            RenderSettings.fogEndDistance = currentConfig.VisionRange;
            
            Debug.Log($"🌫️ Applied vision limit: Range {currentConfig.VisionRange}, Fog density {currentConfig.FogDensity}");
        }
        
        /// <summary>
        /// 환경 데미지 시작
        /// </summary>
        private void StartEnvironmentDamage()
        {
            // 모든 플레이어에게 환경 데미지 적용
            var connectedClients = NetworkManager.Singleton.ConnectedClients;
            foreach (var client in connectedClients.Values)
            {
                if (client.PlayerObject != null)
                {
                    StartEnvironmentDamageForPlayer(client.ClientId);
                }
            }
            
            Debug.Log($"☠️ Started environment damage: {currentConfig.DamagePerSecond}/sec {currentConfig.EnvironmentDamageType}");
        }
        
        /// <summary>
        /// 개별 플레이어 환경 데미지 시작
        /// </summary>
        private void StartEnvironmentDamageForPlayer(ulong clientId)
        {
            if (playerEnvironmentCoroutines.ContainsKey(clientId))
            {
                StopCoroutine(playerEnvironmentCoroutines[clientId]);
            }
            
            var coroutine = StartCoroutine(EnvironmentDamageCoroutine(clientId));
            playerEnvironmentCoroutines[clientId] = coroutine;
        }
        
        /// <summary>
        /// 환경 데미지 코루틴
        /// </summary>
        private IEnumerator EnvironmentDamageCoroutine(ulong clientId)
        {
            while (currentConfig != null && currentConfig.EnableEnvironmentDamage)
            {
                yield return new WaitForSeconds(currentConfig.DamageInterval);
                
                var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
                if (playerObject != null)
                {
                    var statsManager = playerObject.GetComponent<PlayerStatsManager>();
                    if (statsManager != null && !statsManager.IsDead)
                    {
                        float damage = CalculateEnvironmentDamage();
                        statsManager.TakeDamage(damage, currentConfig.DamageType);
                        
                        // 환경 데미지 이펙트 표시
                        ShowEnvironmentDamageEffectClientRpc(clientId, currentConfig.EnvironmentDamageType);
                    }
                }
            }
        }
        
        /// <summary>
        /// 환경 데미지 계산
        /// </summary>
        private float CalculateEnvironmentDamage()
        {
            float baseDamage = currentConfig.DamagePerSecond;
            
            // 타입별 특수 처리
            switch (currentConfig.EnvironmentDamageType)
            {
                case EnvironmentDamageType.Chaotic:
                    // 혼돈 데미지는 랜덤
                    baseDamage *= Random.Range(0.5f, 2f);
                    break;
                case EnvironmentDamageType.Burning:
                    // 화염 데미지는 시간이 지날수록 증가
                    float timeMultiplier = 1f + (Time.time * 0.01f);
                    baseDamage *= timeMultiplier;
                    break;
            }
            
            return baseDamage;
        }
        
        /// <summary>
        /// 이동 제약 효과 적용
        /// </summary>
        private void ApplyMovementRestriction()
        {
            // 모든 플레이어에게 이동 제약 적용
            var connectedClients = NetworkManager.Singleton.ConnectedClients;
            foreach (var client in connectedClients.Values)
            {
                if (client.PlayerObject != null)
                {
                    var playerController = client.PlayerObject.GetComponent<PlayerController>();
                    if (playerController != null)
                    {
                        ApplyMovementRestrictionToPlayerClientRpc(
                            client.ClientId, 
                            currentConfig.MovementSpeedMultiplier, 
                            currentConfig.RestrictionType);
                    }
                }
            }
            
            Debug.Log($"🦶 Applied movement restriction: {currentConfig.MovementSpeedMultiplier}x speed, Type: {currentConfig.RestrictionType}");
        }
        
        /// <summary>
        /// 오디오 효과 적용
        /// </summary>
        private void ApplyAudioEffects()
        {
            if (currentConfig.AmbientSound != null && ambientAudioSource != null)
            {
                ambientAudioSource.clip = currentConfig.AmbientSound;
                ambientAudioSource.volume = currentConfig.AmbientVolume;
                
                // 에코 효과
                if (currentConfig.EnableEchoEffect)
                {
                    var echo = ambientAudioSource.gameObject.GetComponent<AudioEchoFilter>();
                    if (echo == null)
                    {
                        echo = ambientAudioSource.gameObject.AddComponent<AudioEchoFilter>();
                    }
                    echo.enabled = true;
                    echo.delay = 500f;
                    echo.decayRatio = 0.5f;
                }
                
                ambientAudioSource.Play();
                
                Debug.Log($"🔊 Applied audio effects: {currentConfig.AmbientSound.name}");
            }
        }
        
        /// <summary>
        /// 함정 밀도 변경 알림
        /// </summary>
        private void NotifyTrapDensityChange()
        {
            if (DungeonEnvironment.Instance != null)
            {
                // DungeonEnvironment에 함정 밀도 변경 알림 (추후 구현)
                Debug.Log($"🪤 Notified trap density change: {currentConfig.TrapDensityMultiplier}x");
            }
        }
        
        /// <summary>
        /// 깜빡이는 조명 효과 코루틴
        /// </summary>
        private IEnumerator FlickeringLightCoroutine()
        {
            float originalIntensity = ambientLight.intensity;
            
            while (currentConfig != null && currentConfig.EnableFlickeringLights)
            {
                // 랜덤한 강도로 깜빡임
                float flickerIntensity = originalIntensity * Random.Range(0.3f, 1.2f);
                ambientLight.intensity = flickerIntensity;
                
                yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
                
                ambientLight.intensity = originalIntensity;
                
                yield return new WaitForSeconds(Random.Range(0.5f, 2f));
            }
        }
        
        /// <summary>
        /// 현재 환경 효과 정리
        /// </summary>
        private void CleanupCurrentEnvironmentEffects()
        {
            // 환경 데미지 코루틴 정지
            foreach (var coroutine in playerEnvironmentCoroutines.Values)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            playerEnvironmentCoroutines.Clear();
            
            // 환경 오브젝트들 정리
            foreach (var effect in environmentEffects)
            {
                if (effect != null)
                {
                    Destroy(effect);
                }
            }
            environmentEffects.Clear();
            
            // 포그 효과 정리
            if (currentFogEffect != null)
            {
                Destroy(currentFogEffect);
                currentFogEffect = null;
            }
            
            // 렌더링 설정 초기화
            RenderSettings.fog = false;
            if (playerCamera != null)
            {
                playerCamera.farClipPlane = 1000f;
            }
            
            // 오디오 정지
            if (ambientAudioSource != null)
            {
                ambientAudioSource.Stop();
            }
        }
        
        /// <summary>
        /// 모든 환경 효과 정리
        /// </summary>
        private void CleanupAllEnvironmentEffects()
        {
            CleanupCurrentEnvironmentEffects();
            currentConfig = null;
        }
        
        // 네트워크 이벤트 처리
        private void OnFloorEnvironmentChanged(int previousValue, int newValue)
        {
            if (!IsServer && newValue >= 0)
            {
                // 클라이언트에서 시각/오디오 효과만 적용
                var config = GetFloorConfig(newValue);
                if (config != null)
                {
                    currentConfig = config;
                    StartCoroutine(ApplyClientEnvironmentEffects());
                }
            }
        }
        
        /// <summary>
        /// 클라이언트 환경 효과 적용
        /// </summary>
        private IEnumerator ApplyClientEnvironmentEffects()
        {
            if (currentConfig.ModifyLighting)
            {
                ApplyLightingEffects();
            }
            
            yield return null;
            
            if (currentConfig.EnableVisionLimit)
            {
                ApplyVisionLimitEffect();
            }
            
            yield return null;
            
            ApplyAudioEffects();
        }
        
        // ClientRpc 메서드들
        [ClientRpc]
        private void NotifyEnvironmentStartClientRpc(string environmentName, string description)
        {
            Debug.Log($"🌍 환경 변화: {environmentName}");
            Debug.Log($"📝 {description}");
        }
        
        [ClientRpc]
        private void ShowEnvironmentDamageEffectClientRpc(ulong targetClientId, EnvironmentDamageType damageType)
        {
            if (NetworkManager.Singleton.LocalClientId == targetClientId)
            {
                string effectText = damageType switch
                {
                    EnvironmentDamageType.Toxic => "독성 환경 데미지!",
                    EnvironmentDamageType.Burning => "화염 환경 데미지!",
                    EnvironmentDamageType.Freezing => "빙결 환경 데미지!",
                    EnvironmentDamageType.Cursed => "저주 환경 데미지!",
                    EnvironmentDamageType.Chaotic => "혼돈 환경 데미지!",
                    _ => "환경 데미지!"
                };
                
                Debug.Log($"💀 {effectText}");
            }
        }
        
        [ClientRpc]
        private void ApplyMovementRestrictionToPlayerClientRpc(ulong targetClientId, float speedMultiplier, MovementRestrictionType restrictionType)
        {
            if (NetworkManager.Singleton.LocalClientId == targetClientId)
            {
                var playerController = NetworkManager.Singleton.LocalClient.PlayerObject?.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    // PlayerController에 이동 속도 배율 적용 (구현 필요)
                    Debug.Log($"🦶 Movement restricted: {speedMultiplier}x speed ({restrictionType})");
                }
            }
        }
        
        /// <summary>
        /// 현재 환경 설정 가져오기
        /// </summary>
        public FloorEnvironmentConfig GetCurrentConfig()
        {
            return currentConfig;
        }
        
        /// <summary>
        /// 디버그 정보
        /// </summary>
        [ContextMenu("Show Environment Debug Info")]
        private void ShowDebugInfo()
        {
            Debug.Log("=== Floor Environment Manager Debug ===");
            Debug.Log($"Environment Effects Enabled: {enableEnvironmentEffects}");
            Debug.Log($"Current Floor Environment: {currentFloorEnvironment.Value}");
            Debug.Log($"Floor Configs: {floorConfigs.Count}");
            Debug.Log($"Active Environment Effects: {environmentEffects.Count}");
            
            if (currentConfig != null)
            {
                Debug.Log($"Current Config: {currentConfig.EnvironmentName} (Floor {currentConfig.FloorNumber})");
                Debug.Log($"- Trap Density: {currentConfig.TrapDensityMultiplier}x");
                Debug.Log($"- Vision Limit: {currentConfig.EnableVisionLimit} ({currentConfig.VisionRange}m)");
                Debug.Log($"- Environment Damage: {currentConfig.EnableEnvironmentDamage} ({currentConfig.DamagePerSecond}/sec)");
                Debug.Log($"- Movement Restriction: {currentConfig.EnableMovementRestriction} ({currentConfig.MovementSpeedMultiplier}x)");
            }
        }
    }
}