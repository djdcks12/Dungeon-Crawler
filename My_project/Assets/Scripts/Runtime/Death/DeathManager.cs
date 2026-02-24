using UnityEngine;
using Unity.Netcode;
using System.Collections;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 하드코어 던전 크롤러 사망 처리 관리자
    /// 완전한 데스 페널티 - 죽으면 캐릭터 삭제, 모든 진행도 소실
    /// </summary>
    public class DeathManager : NetworkBehaviour
    {
        [Header("Death Settings")]
        [SerializeField] private float deathProcessDelay = 2.0f;
        [SerializeField] private float itemScatterRadius = 5.0f;
        [SerializeField] private float itemDespawnTime = 3600f; // 1시간
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject deathEffectPrefab;
        [SerializeField] private GameObject itemDropEffectPrefab;
        
        // 컴포넌트 참조
        private PlayerStatsManager statsManager;
        private PlayerController playerController;
        private CharacterDeletion characterDeletion;
        private ItemScatter itemScatter;
        private SoulInheritance soulInheritance;
        private SoulDropSystem soulDropSystem;
        
        // 사망 상태
        private bool isDead = false;
        private bool isProcessingDeath = false;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // 컴포넌트 초기화
            statsManager = GetComponent<PlayerStatsManager>();
            playerController = GetComponent<PlayerController>();
            
            // Death 시스템 컴포넌트들
            characterDeletion = GetComponent<CharacterDeletion>();
            itemScatter = GetComponent<ItemScatter>();
            soulInheritance = FindFirstObjectByType<SoulInheritance>();
            soulDropSystem = GetComponent<SoulDropSystem>();
            
            // 사망 이벤트 구독
            if (statsManager != null)
            {
                statsManager.OnPlayerDeath += HandlePlayerDeath;
            }
        }
        
        public override void OnNetworkDespawn()
        {
            // 이벤트 구독 해제
            if (statsManager != null)
            {
                statsManager.OnPlayerDeath -= HandlePlayerDeath;
            }
            
            base.OnNetworkDespawn();
        }
        
        /// <summary>
        /// 플레이어 사망 처리 시작
        /// </summary>
        private void HandlePlayerDeath()
        {
            if (isDead || isProcessingDeath) return;
            
            Debug.Log($"💀 Player {gameObject.name} has died! Processing death penalty...");
            
            isDead = true;
            isProcessingDeath = true;
            
            // 서버에서 사망 처리
            if (IsServer)
            {
                ProcessDeathServerRpc();
            }
            else if (IsOwner)
            {
                // 클라이언트에서 서버로 사망 처리 요청
                RequestDeathProcessingServerRpc();
            }
        }
        
        /// <summary>
        /// 서버에서 사망 처리 요청
        /// </summary>
        [ServerRpc]
        private void RequestDeathProcessingServerRpc(ServerRpcParams rpcParams = default)
        {
            ProcessDeathServerRpc();
        }
        
        /// <summary>
        /// 서버에서 사망 처리
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void ProcessDeathServerRpc()
        {
            if (!IsServer || !isDead) return;
            
            Debug.Log($"🔥 Server processing death for {gameObject.name}");
            
            StartCoroutine(ProcessDeathSequence());
        }
        
        /// <summary>
        /// 사망 처리 시퀀스
        /// </summary>
        private IEnumerator ProcessDeathSequence()
        {
            // 1. 플레이어 컨트롤 즉시 비활성화
            DisablePlayerControl();

            // 2. 사망 이펙트 재생
            if (IsSpawned)
                PlayDeathEffectClientRpc(transform.position);

            // 3. 잠시 대기 (사망 애니메이션 등)
            yield return new WaitForSeconds(deathProcessDelay);
            if (!IsSpawned || gameObject == null) yield break;

            // 4. 영혼 선택 처리 - 플레이어가 보유한 영혼 중 하나를 선택하여 보존
            if (soulInheritance != null && statsManager?.CurrentStats != null)
            {
                ulong characterId = (ulong)statsManager.CurrentStats.CharacterName.GetHashCode();
                soulInheritance.HandleDeathSoulSelection(characterId);

                // 영혼 선택이 완료될 때까지 잠시 대기
                yield return new WaitForSeconds(1f);
                if (!IsSpawned || gameObject == null) yield break;
            }

            // 6. 아이템 드롭 처리
            if (itemScatter != null)
            {
                itemScatter.ScatterAllItems(transform.position, itemScatterRadius);
            }

            // 7. 사망 기록 저장
            SaveDeathRecord();

            // 8. 캐릭터 삭제 처리
            if (characterDeletion != null)
            {
                characterDeletion.DeleteCharacter();
            }

            // 9. 클라이언트에 사망 완료 알림
            if (IsSpawned)
                NotifyDeathCompletedClientRpc();

            Debug.Log($"Death processing completed for {gameObject.name}");
        }
        
        /// <summary>
        /// 플레이어 컨트롤 비활성화
        /// </summary>
        private void DisablePlayerControl()
        {
            if (playerController != null)
            {
                playerController.enabled = false;
            }
            
            // Rigidbody 정지
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.isKinematic = true;
            }
            
            // 콜라이더 비활성화
            var colliders = GetComponents<Collider2D>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }
        }
        
        /// <summary>
        /// 사망 기록 저장
        /// </summary>
        private void SaveDeathRecord()
        {
            var deathInfo = new DeathInfo
            {
                characterId = statsManager?.CurrentStats?.CharacterName ?? "Unknown",
                level = statsManager?.CurrentStats?.CurrentLevel ?? 1,
                race = statsManager?.CurrentStats?.CharacterRace ?? Race.Human,
                deathTime = System.DateTime.Now.ToBinary(),
                deathPosition = transform.position,
                cause = "Combat", // 추후 사망 원인 추가
                killerName = "", // 추후 PvP 킬러 정보 추가
            };
            
            // 로컬 저장 (추후 서버 저장으로 확장)
            PlayerPrefs.SetString($"DeathRecord_{deathInfo.characterId}_{deathInfo.deathTime}", 
                JsonUtility.ToJson(deathInfo));
            
            Debug.Log($"📝 Death record saved: {deathInfo.characterId} Level {deathInfo.level}");
        }
        
        /// <summary>
        /// 사망 이펙트 재생
        /// </summary>
        [ClientRpc]
        private void PlayDeathEffectClientRpc(Vector3 position)
        {
            // 사망 이펙트 생성
            if (deathEffectPrefab != null)
            {
                var effect = Instantiate(deathEffectPrefab, position, Quaternion.identity);
                Destroy(effect, 3f);
            }
            
            // 화면 효과 (빨간색 플래시 등)
            StartCoroutine(DeathScreenEffect());
            
            Debug.Log($"💀 Death effect played at {position}");
        }
        
        /// <summary>
        /// 사망 화면 효과
        /// </summary>
        private IEnumerator DeathScreenEffect()
        {
            // 추후 UI 시스템에서 구현
            // 예: 화면 빨간색 플래시, 사망 메시지 표시 등
            yield return null;
        }
        
        /// <summary>
        /// 사망 처리 완료 알림
        /// </summary>
        [ClientRpc]
        private void NotifyDeathCompletedClientRpc()
        {
            if (IsOwner)
            {
                // 로컬 플레이어의 사망 완료 처리
                HandleLocalPlayerDeath();
            }
            
            // 모든 클라이언트에서 사망한 플레이어 정리
            StartCoroutine(CleanupDeadPlayer());
        }
        
        /// <summary>
        /// 로컬 플레이어 사망 처리
        /// </summary>
        private void HandleLocalPlayerDeath()
        {
            Debug.Log("💀 Local player death - returning to character selection");
            
            // 네트워크 연결 해제
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
            
            // 캐릭터 선택 화면으로 이동 (추후 Scene 전환 구현)
            // SceneManager.LoadScene("CharacterSelection");
            
            // 임시: 게임 종료 메시지
            Debug.Log("🔄 Game should return to character selection screen");
        }
        
        /// <summary>
        /// 사망한 플레이어 정리
        /// </summary>
        private IEnumerator CleanupDeadPlayer()
        {
            // 잠시 대기 후 오브젝트 제거
            yield return new WaitForSeconds(3f);
            
            if (IsServer)
            {
                // 네트워크 오브젝트 제거
                if (NetworkObject != null && NetworkObject.IsSpawned)
                {
                    NetworkObject.Despawn();
                }
            }
        }
        
        /// <summary>
        /// 플레이어 리스폰 처리
        /// </summary>
        public void RespawnPlayer()
        {
            if (!isDead) return;

            if (IsServer)
            {
                RespawnPlayerServerRpc();
            }
            else if (IsOwner)
            {
                RequestRespawnServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestRespawnServerRpc(ServerRpcParams rpcParams = default)
        {
            RespawnPlayerServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void RespawnPlayerServerRpc()
        {
            if (!IsServer) return;

            // 상태 초기화
            isDead = false;
            isProcessingDeath = false;

            // 플레이어 컨트롤 재활성화
            if (playerController != null)
            {
                playerController.enabled = true;
            }

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
            }

            var colliders = GetComponents<Collider2D>();
            foreach (var collider in colliders)
            {
                collider.enabled = true;
            }

            // 스폰 위치로 이동
            transform.position = Vector3.zero;

            // HP/MP 복구 (NetworkVariable 기반으로 서버/클라이언트 동기화)
            if (statsManager != null)
            {
                statsManager.RestoreFullHealth();
                statsManager.RestoreFullMana();
            }

            // 클라이언트에 리스폰 알림
            NotifyRespawnClientRpc();

            Debug.Log($"Player {gameObject.name} respawned");
        }

        [ClientRpc]
        private void NotifyRespawnClientRpc()
        {
            // DeathUI 숨기기
            var deathUI = FindFirstObjectByType<DeathUI>();
            if (deathUI != null)
            {
                deathUI.HideDeathUI();
            }
        }

        /// <summary>
        /// 강제 사망 (디버그/관리자용)
        /// </summary>
        [ContextMenu("Force Death")]
        public void ForceDeath()
        {
            if (Application.isPlaying && !isDead)
            {
                HandlePlayerDeath();
            }
        }
        
        public override void OnDestroy()
        {
            StopAllCoroutines();
            base.OnDestroy();
        }

        /// <summary>
        /// 사망 상태 확인
        /// </summary>
        public bool IsDead => isDead;

        /// <summary>
        /// 사망 처리 중인지 확인
        /// </summary>
        public bool IsProcessingDeath => isProcessingDeath;
    }
    
    /// <summary>
    /// 사망 정보 구조체
    /// </summary>
    [System.Serializable]
    public struct DeathInfo
    {
        public string characterId;
        public int level;
        public Race race;
        public long deathTime;
        public Vector3 deathPosition;
        public string cause;
        public string killerName;
    }
}