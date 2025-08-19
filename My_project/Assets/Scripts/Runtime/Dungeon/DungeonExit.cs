using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 던전 층 출구 시스템
    /// 플레이어가 출구에 도달하면 다음 층으로 이동
    /// </summary>
    public class DungeonExit : NetworkBehaviour
    {
        [Header("출구 설정")]
        [SerializeField] private float triggerRadius = 2.0f;
        [SerializeField] private bool requireAllPlayers = false; // 모든 플레이어가 모여야 하는지
        [SerializeField] private float activationDelay = 3.0f; // 활성화까지 대기 시간
        [SerializeField] private GameObject exitEffectPrefab;
        
        // 출구 상태
        private NetworkVariable<bool> isActive = new NetworkVariable<bool>(false);
        private NetworkVariable<float> activationTimer = new NetworkVariable<float>(0f);
        
        // 참조
        private DungeonManager dungeonManager;
        private Collider2D exitTrigger;
        private HashSet<ulong> playersInRange = new HashSet<ulong>();
        private bool isActivating = false;
        
        // 이벤트
        public System.Action<DungeonExit> OnExitActivated;
        public System.Action<DungeonExit> OnExitUsed;
        
        // 프로퍼티
        public bool IsActive => isActive.Value;
        public float ActivationProgress => activationTimer.Value / activationDelay;
        public int PlayersInRange => playersInRange.Count;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            SetupTrigger();
            SetupVisuals();
            
            if (IsServer)
            {
                isActive.OnValueChanged += OnActiveStateChanged;
            }
            
            Debug.Log($"DungeonExit spawned at {transform.position}");
        }
        
        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                isActive.OnValueChanged -= OnActiveStateChanged;
            }
            
            base.OnNetworkDespawn();
        }
        
        private void Update()
        {
            if (!IsServer) return;
            
            // 활성화 중인 경우 타이머 업데이트
            if (isActivating && !isActive.Value)
            {
                activationTimer.Value += Time.deltaTime;
                
                if (activationTimer.Value >= activationDelay)
                {
                    ActivateExit();
                }
            }
            
            // 플레이어 범위 체크
            CheckPlayersInRange();
        }
        
        /// <summary>
        /// 던전 매니저 참조 설정
        /// </summary>
        public void Initialize(DungeonManager manager)
        {
            dungeonManager = manager;
            
            // 층 클리어 이벤트 구독
            if (dungeonManager != null)
            {
                dungeonManager.OnFloorChanged += OnFloorChanged;
            }
        }
        
        /// <summary>
        /// 트리거 설정
        /// </summary>
        private void SetupTrigger()
        {
            exitTrigger = GetComponent<Collider2D>();
            if (exitTrigger == null)
            {
                exitTrigger = gameObject.AddComponent<CircleCollider2D>();
                var circleCollider = exitTrigger as CircleCollider2D;
                circleCollider.radius = triggerRadius;
                circleCollider.isTrigger = true;
            }
        }
        
        /// <summary>
        /// 시각적 효과 설정
        /// </summary>
        private void SetupVisuals()
        {
            // 기본 스프라이트 설정 (출구 포털)
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
            
            // 기본 출구 스프라이트 (임시로 원형)
            var texture = new Texture2D(64, 64);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), Vector2.one * 0.5f);
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.blue;
            
            // 발광 효과 추가
            var glowObject = new GameObject("Exit Glow");
            glowObject.transform.SetParent(transform);
            glowObject.transform.localPosition = Vector3.zero;
            glowObject.transform.localScale = Vector3.one * 1.2f;
            
            var glowRenderer = glowObject.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = sprite;
            glowRenderer.color = new Color(0.2f, 0.5f, 1f, 0.3f);
            glowRenderer.sortingOrder = -1;
        }
        
        /// <summary>
        /// 플레이어 범위 체크
        /// </summary>
        private void CheckPlayersInRange()
        {
            if (dungeonManager == null || !dungeonManager.IsActive) return;
            
            playersInRange.Clear();
            
            // 던전 내 모든 살아있는 플레이어 체크
            foreach (var dungeonPlayer in dungeonManager.Players)
            {
                if (!dungeonPlayer.isAlive) continue;
                
                var clientObject = NetworkManager.Singleton.ConnectedClients[dungeonPlayer.clientId].PlayerObject;
                if (clientObject != null)
                {
                    float distance = Vector3.Distance(transform.position, clientObject.transform.position);
                    if (distance <= triggerRadius)
                    {
                        playersInRange.Add(dungeonPlayer.clientId);
                    }
                }
            }
            
            // 출구 활성화 조건 체크
            CheckExitActivationConditions();
        }
        
        /// <summary>
        /// 출구 활성화 조건 확인
        /// </summary>
        private void CheckExitActivationConditions()
        {
            if (isActive.Value) return;
            
            bool shouldActivate = false;
            
            if (requireAllPlayers)
            {
                // 모든 살아있는 플레이어가 범위 내에 있어야 함
                int alivePlayers = 0;
                foreach (var player in dungeonManager.Players)
                {
                    if (player.isAlive) alivePlayers++;
                }
                
                shouldActivate = playersInRange.Count > 0 && playersInRange.Count == alivePlayers;
            }
            else
            {
                // 한 명이라도 범위 내에 있으면 됨
                shouldActivate = playersInRange.Count > 0;
            }
            
            // 몬스터를 잡지 않아도 출구 발견 시 활성화 가능 (하드코어 룰)
            // shouldActivate = shouldActivate && IsFloorCleared(); // 제거: 몬스터 처치 필수 조건 해제
            
            if (shouldActivate && !isActivating)
            {
                StartActivation();
            }
            else if (!shouldActivate && isActivating)
            {
                StopActivation();
            }
        }
        
        /// <summary>
        /// 층 클리어 여부 확인
        /// </summary>
        private bool IsFloorCleared()
        {
            // 던전 매니저에서 현재 층 몬스터 상태 확인
            // 모든 MonsterSpawner에 살아있는 몬스터가 없으면 클리어
            
            if (dungeonManager == null) return true;
            
            // 간단한 구현: 일정 시간이 지나면 자동으로 클리어된 것으로 간주
            // 실제로는 MonsterSpawner와 연동해야 함
            return Time.time > 10f; // 임시 구현
        }
        
        /// <summary>
        /// 출구 활성화 시작
        /// </summary>
        private void StartActivation()
        {
            if (isActivating) return;
            
            isActivating = true;
            activationTimer.Value = 0f;
            
            NotifyActivationStartedClientRpc();
            Debug.Log($"Exit activation started. Delay: {activationDelay}s");
        }
        
        /// <summary>
        /// 출구 활성화 중단
        /// </summary>
        private void StopActivation()
        {
            if (!isActivating) return;
            
            isActivating = false;
            activationTimer.Value = 0f;
            
            NotifyActivationStoppedClientRpc();
            Debug.Log("Exit activation stopped.");
        }
        
        /// <summary>
        /// 출구 활성화
        /// </summary>
        private void ActivateExit()
        {
            isActivating = false;
            isActive.Value = true;
            activationTimer.Value = activationDelay;
            
            OnExitActivated?.Invoke(this);
            NotifyExitActivatedClientRpc();
            
            Debug.Log("🔺 Exit activated! Players can now advance to next floor.");
        }
        
        /// <summary>
        /// 출구 사용 (다음 층으로 이동)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void UseExitServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer || !isActive.Value) return;
            
            ulong clientId = rpcParams.Receive.SenderClientId;
            
            // 플레이어가 범위 내에 있는지 확인
            if (!playersInRange.Contains(clientId))
            {
                Debug.LogWarning($"Player {clientId} tried to use exit but is not in range!");
                return;
            }
            
            // 던전 매니저에 다음 층 이동 요청
            if (dungeonManager != null)
            {
                dungeonManager.AdvanceToNextFloorServerRpc();
                OnExitUsed?.Invoke(this);
            }
        }
        
        /// <summary>
        /// 층 변경 이벤트 처리
        /// </summary>
        private void OnFloorChanged(int newFloor)
        {
            // 새로운 층으로 이동하면 출구 비활성화
            if (IsServer)
            {
                isActive.Value = false;
                isActivating = false;
                activationTimer.Value = 0f;
                playersInRange.Clear();
            }
        }
        
        /// <summary>
        /// 활성화 상태 변경 이벤트
        /// </summary>
        private void OnActiveStateChanged(bool previousValue, bool newValue)
        {
            Debug.Log($"Exit active state changed: {previousValue} → {newValue}");
            
            // 시각적 효과 업데이트
            UpdateVisualEffects(newValue);
        }
        
        /// <summary>
        /// 시각적 효과 업데이트
        /// </summary>
        private void UpdateVisualEffects(bool active)
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = active ? Color.green : Color.blue;
            }
            
            // 활성화 이펙트 생성
            if (active && exitEffectPrefab != null)
            {
                var effect = Instantiate(exitEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 3f);
            }
        }
        
        // Trigger 이벤트들
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer) return;
            
            var playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                var networkObject = playerController.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned)
                {
                    Debug.Log($"Player {networkObject.OwnerClientId} entered exit range");
                }
            }
        }
        
        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsServer) return;
            
            var playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                var networkObject = playerController.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned)
                {
                    Debug.Log($"Player {networkObject.OwnerClientId} left exit range");
                }
            }
        }
        
        // ClientRpc 메서드들
        [ClientRpc]
        private void NotifyActivationStartedClientRpc()
        {
            Debug.Log("🔄 Exit activation started...");
        }
        
        [ClientRpc]
        private void NotifyActivationStoppedClientRpc()
        {
            Debug.Log("❌ Exit activation stopped");
        }
        
        [ClientRpc]
        private void NotifyExitActivatedClientRpc()
        {
            Debug.Log("✅ Exit activated! Interact to advance to next floor.");
            
            // UI 알림 등 클라이언트 측 효과
            UpdateVisualEffects(true);
        }
        
        /// <summary>
        /// 디버그용 기즈모 그리기
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isActive.Value ? Color.green : Color.blue;
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawIcon(transform.position, "Portal", true);
        }
    }
}