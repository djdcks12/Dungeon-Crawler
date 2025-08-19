using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 영혼 픽업 시스템 - 떨어진 영혼을 플레이어가 수집
    /// 수집된 영혼은 계정에 영구 저장됨
    /// </summary>
    public class SoulPickup : NetworkBehaviour
    {
        [Header("Pickup Settings")]
        [SerializeField] private float pickupRange = 1.0f;
        [SerializeField] private float autoPickupRange = 0.5f;
        [SerializeField] private bool allowAutoPickup = true;
        
        [Header("Visual Settings")]
        [SerializeField] private float pulseSpeed = 2.0f;
        [SerializeField] private float despawnTime = 300f; // 5분 후 자동 소멸
        
        // 영혼 데이터
        private SoulData soulData;
        private bool isCollected = false;
        
        // 컴포넌트 참조
        private SpriteRenderer spriteRenderer;
        private Collider2D pickupCollider;
        
        // 시각적 효과
        private float pulseTimer = 0f;
        private Vector3 originalScale;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // 컴포넌트 초기화
            spriteRenderer = GetComponent<SpriteRenderer>();
            pickupCollider = GetComponent<Collider2D>();
            
            originalScale = transform.localScale;
            
            // 자동 소멸 타이머 시작
            if (IsServer)
            {
                Invoke(nameof(DespawnSoul), despawnTime);
            }
            
            Debug.Log($"👻 Soul pickup spawned: {soulData.soulName}");
        }
        
        private void Update()
        {
            if (!IsServer || isCollected) return;
            
            // 펄스 애니메이션
            UpdatePulseAnimation();
            
            // 자동 픽업 체크
            if (allowAutoPickup)
            {
                CheckAutoPickup();
            }
        }
        
        /// <summary>
        /// 영혼 데이터 설정
        /// </summary>
        public void SetSoulData(SoulData data)
        {
            soulData = data;
        }
        
        /// <summary>
        /// 영혼 데이터 가져오기
        /// </summary>
        public SoulData GetSoulData()
        {
            return soulData;
        }
        
        /// <summary>
        /// 펄스 애니메이션 업데이트
        /// </summary>
        private void UpdatePulseAnimation()
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            float pulse = 1f + Mathf.Sin(pulseTimer) * 0.2f;
            transform.localScale = originalScale * pulse;
            
            // 알파 펄스 효과
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = 0.7f + Mathf.Sin(pulseTimer * 1.5f) * 0.3f;
                spriteRenderer.color = color;
            }
        }
        
        /// <summary>
        /// 자동 픽업 체크
        /// </summary>
        private void CheckAutoPickup()
        {
            Collider2D[] nearbyPlayers = Physics2D.OverlapCircleAll(transform.position, autoPickupRange);
            
            foreach (var collider in nearbyPlayers)
            {
                var playerController = collider.GetComponent<PlayerController>();
                if (playerController != null && playerController.IsOwner)
                {
                    CollectSoul(playerController);
                    break;
                }
            }
        }
        
        /// <summary>
        /// 수동 픽업 처리 (트리거 진입)
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer || isCollected) return;
            
            var playerController = other.GetComponent<PlayerController>();
            if (playerController != null && playerController.IsOwner)
            {
                CollectSoul(playerController);
            }
        }
        
        /// <summary>
        /// 영혼 수집 처리
        /// </summary>
        private void CollectSoul(PlayerController player)
        {
            if (isCollected) return;
            
            isCollected = true;
            
            Debug.Log($"✨ Soul collected by {player.name}: {soulData.soulName}");
            
            // 플레이어에게 영혼 추가
            AddSoulToPlayer(player);
            
            // 수집 이펙트 재생
            PlayCollectionEffectClientRpc();
            
            // 통계 업데이트
            SoulDropSystem.OnSoulCollected();
            
            // 오브젝트 제거
            if (IsServer)
            {
                NetworkObject.Despawn();
            }
        }
        
        /// <summary>
        /// 플레이어에게 영혼 추가
        /// </summary>
        private void AddSoulToPlayer(PlayerController player)
        {
            var soulInheritance = FindObjectOfType<SoulInheritance>();
            if (soulInheritance != null)
            {
                // 플레이어 캐릭터 ID 가져오기
                ulong characterId = GetCharacterIdFromPlayer(player);
                
                // 영혼을 SoulInheritance 시스템에 추가
                soulInheritance.AcquireSoulServerRpc(characterId, soulData);
            }
            else
            {
                Debug.LogError("SoulInheritance system not found! Fallback to account storage.");
                // 폴백: 직접 계정에 저장
                AddSoulToAccount(player, soulData);
            }
        }
        
        /// <summary>
        /// 플레이어에서 캐릭터 ID 가져오기
        /// </summary>
        private ulong GetCharacterIdFromPlayer(PlayerController player)
        {
            var statsManager = player.GetComponent<PlayerStatsManager>();
            if (statsManager?.CurrentStats != null)
            {
                // CharacterName을 해시로 변환하여 캐릭터 ID 생성
                return (ulong)statsManager.CurrentStats.CharacterName.GetHashCode();
            }
            
            // 폴백: 플레이어 네트워크 ID 사용
            return player.NetworkObjectId;
        }
        
        /// <summary>
        /// 계정에 영혼 추가
        /// </summary>
        private void AddSoulToAccount(PlayerController player, SoulData soul)
        {
            // 계정 ID 가져오기
            string accountId = GetAccountId();
            
            // 기존 영혼 목록 로드
            var existingSouls = LoadSoulsFromAccount(accountId);
            
            // 새 영혼 추가
            existingSouls.Add(soul);
            
            // 최대 슬롯 수 체크 (15개)
            if (existingSouls.Count > 15)
            {
                // 가장 약한 영혼 제거 (추후 더 정교한 로직 구현)
                existingSouls.Sort((a, b) => GetSoulPower(a).CompareTo(GetSoulPower(b)));
                existingSouls.RemoveAt(0);
                Debug.Log("⚠️ Soul inventory full! Removed weakest soul.");
            }
            
            // 계정에 저장
            SaveSoulsToAccount(accountId, existingSouls);
            
            // 플레이어에게 알림
            NotifyPlayerSoulObtained(player, soul);
            
            Debug.Log($"💎 Soul added to account: {soul.soulName} (+{soul.statBonus.strength} STR, +{soul.statBonus.agility} AGI, etc.)");
        }
        
        /// <summary>
        /// 계정에서 영혼 목록 로드
        /// </summary>
        private System.Collections.Generic.List<SoulData> LoadSoulsFromAccount(string accountId)
        {
            string soulsJson = PlayerPrefs.GetString($"Account_{accountId}_Souls", "");
            
            if (string.IsNullOrEmpty(soulsJson))
            {
                return new System.Collections.Generic.List<SoulData>();
            }
            
            try
            {
                var wrapper = JsonUtility.FromJson<SoulDataWrapper>(soulsJson);
                return new System.Collections.Generic.List<SoulData>(wrapper.souls);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load souls: {e.Message}");
                return new System.Collections.Generic.List<SoulData>();
            }
        }
        
        /// <summary>
        /// 계정에 영혼 목록 저장
        /// </summary>
        private void SaveSoulsToAccount(string accountId, System.Collections.Generic.List<SoulData> souls)
        {
            try
            {
                var wrapper = new SoulDataWrapper { souls = souls.ToArray() };
                string soulsJson = JsonUtility.ToJson(wrapper);
                PlayerPrefs.SetString($"Account_{accountId}_Souls", soulsJson);
                PlayerPrefs.Save();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save souls: {e.Message}");
            }
        }
        
        /// <summary>
        /// 영혼 파워 계산 (정렬용)
        /// </summary>
        private float GetSoulPower(SoulData soul)
        {
            return soul.statBonus.strength + soul.statBonus.agility + soul.statBonus.vitality + 
                   soul.statBonus.intelligence + soul.statBonus.defense + soul.statBonus.magicDefense + 
                   soul.statBonus.luck + soul.statBonus.stability;
        }
        
        /// <summary>
        /// 계정 ID 가져오기
        /// </summary>
        private string GetAccountId()
        {
            return PlayerPrefs.GetString("AccountId", "DefaultAccount");
        }
        
        /// <summary>
        /// 플레이어에게 영혼 획득 알림
        /// </summary>
        private void NotifyPlayerSoulObtained(PlayerController player, SoulData soul)
        {
            // 클라이언트에 알림 전송
            NotifyPlayerClientRpc(soul.soulName, GetSoulPower(soul));
        }
        
        /// <summary>
        /// 수집 이펙트 재생
        /// </summary>
        [ClientRpc]
        private void PlayCollectionEffectClientRpc()
        {
            // 수집 이펙트 재생
            if (spriteRenderer != null)
            {
                StartCoroutine(CollectionAnimation());
            }
            
            // 사운드 재생 (추후 오디오 시스템에서 구현)
            Debug.Log("🎵 Soul collection sound played");
        }
        
        /// <summary>
        /// 플레이어 알림
        /// </summary>
        [ClientRpc]
        private void NotifyPlayerClientRpc(string soulName, float soulPower)
        {
            Debug.Log($"🌟 SOUL OBTAINED! {soulName} (Power: {soulPower:F1})");
            
            // UI 알림 표시 (추후 UI 시스템에서 구현)
            // ShowSoulObtainedUI(soulName, soulPower);
        }
        
        /// <summary>
        /// 수집 애니메이션
        /// </summary>
        private System.Collections.IEnumerator CollectionAnimation()
        {
            float animationTime = 0.5f;
            float elapsed = 0f;
            
            Vector3 startScale = transform.localScale;
            Vector3 endScale = Vector3.zero;
            
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + Vector3.up * 2f;
            
            while (elapsed < animationTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationTime;
                
                // 크기 축소
                transform.localScale = Vector3.Lerp(startScale, endScale, t);
                
                // 위로 이동
                transform.position = Vector3.Lerp(startPos, endPos, t);
                
                // 알파 감소
                if (spriteRenderer != null)
                {
                    Color color = spriteRenderer.color;
                    color.a = 1f - t;
                    spriteRenderer.color = color;
                }
                
                yield return null;
            }
        }
        
        /// <summary>
        /// 영혼 자동 소멸
        /// </summary>
        private void DespawnSoul()
        {
            if (!isCollected && IsServer)
            {
                Debug.Log($"⏰ Soul {soulData.soulName} despawned after {despawnTime} seconds");
                NetworkObject.Despawn();
            }
        }
        
        /// <summary>
        /// 기즈모 그리기 (에디터용)
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // 픽업 범위 시각화
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, pickupRange);
            
            // 자동 픽업 범위 시각화
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, autoPickupRange);
        }
    }
    
    /// <summary>
    /// 영혼 데이터 배열 래퍼 (JSON 직렬화용)
    /// </summary>
    [System.Serializable]
    public class SoulDataWrapper
    {
        public SoulData[] souls;
    }
}