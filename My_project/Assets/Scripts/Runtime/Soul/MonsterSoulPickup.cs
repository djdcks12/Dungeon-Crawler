using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 몬스터 영혼 픽업 시스템 (스킬 포함)
    /// </summary>
    public class MonsterSoulPickup : NetworkBehaviour
    {
        [Header("Monster Soul Settings")]
        [SerializeField] private MonsterSoulData soulData;
        [SerializeField] private float pickupRange = 2f;
        [SerializeField] private bool autoPickup = true;
        
        [Header("Visual Feedback")]
        [SerializeField] private float highlightDistance = 3f;
        [SerializeField] private GameObject pickupPromptUI;
        
        // 컴포넌트 참조
        private SpriteRenderer spriteRenderer;
        private Collider2D soulCollider;
        private SoulGlow soulGlow;
        
        // 상태
        private bool isPickedUp = false;
        private PlayerController nearbyPlayer = null;

        // GC 최적화: 재사용 버퍼
        private static readonly Collider2D[] s_OverlapBuffer = new Collider2D[8];
        
        public MonsterSoulData SoulData => soulData;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // 컴포넌트 참조
            spriteRenderer = GetComponent<SpriteRenderer>();
            soulCollider = GetComponent<Collider2D>();
            soulGlow = GetComponent<SoulGlow>();
            
            // UI 프롬프트 설정
            if (pickupPromptUI != null)
            {
                pickupPromptUI.SetActive(false);
            }
        }
        
        private void Update()
        {
            if (isPickedUp) return;
            
            // 근처 플레이어 체크
            CheckNearbyPlayers();
            
            // 하이라이트 업데이트
            UpdateHighlight();
        }
        
        /// <summary>
        /// 몬스터 영혼 데이터 설정
        /// </summary>
        public void SetMonsterSoulData(MonsterSoulData data)
        {
            soulData = data;
            
            if (soulData != null)
            {
                // 이름 업데이트
                gameObject.name = $"MonsterSoul_{soulData.soulName}";
                
                // 등급별 시각적 업데이트
                UpdateVisualsByGrade();
            }
        }
        
        /// <summary>
        /// 등급별 시각적 요소 업데이트
        /// </summary>
        private void UpdateVisualsByGrade()
        {
            if (soulData == null) return;
            
            // 등급별 색상 및 크기 조정
            if (spriteRenderer != null)
            {
                spriteRenderer.color = GetGradeColor(soulData.grade);
                
                // 등급별 크기 조정
                float scale = 1f + ((int)soulData.grade * 0.2f);
                transform.localScale = Vector3.one * scale;
            }
            
            // 등급별 발광 효과
            if (soulGlow != null)
            {
                Color gradeColor = GetGradeColor(soulData.grade);
                float intensity = 2f + ((int)soulData.grade * 0.5f);
                soulGlow.SetGlowSettings(gradeColor, intensity);
            }
        }
        
        /// <summary>
        /// 등급별 색상 반환
        /// </summary>
        private Color GetGradeColor(float grade)
        {
            float normalized = (grade - 80f) / 40f;
            
            if (normalized < 0.3f) return Color.white;      // 80-92: Common
            else if (normalized < 0.5f) return Color.green; // 92-100: Uncommon
            else if (normalized < 0.7f) return Color.blue;  // 100-108: Rare
            else if (normalized < 0.9f) return Color.magenta; // 108-116: Epic
            else return Color.yellow;                         // 116-120: Legendary
        }
        
        /// <summary>
        /// 근처 플레이어 체크
        /// </summary>
        private void CheckNearbyPlayers()
        {
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, pickupRange, s_OverlapBuffer);
            PlayerController closestPlayer = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var player = s_OverlapBuffer[i].GetComponent<PlayerController>();
                if (player != null && player.IsOwner) // 로컬 플레이어만
                {
                    var statsManager = player.GetComponent<PlayerStatsManager>();
                    if (statsManager != null && !statsManager.IsDead)
                    {
                        float distance = Vector3.Distance(transform.position, player.transform.position);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestPlayer = player;
                        }
                    }
                }
            }
            
            nearbyPlayer = closestPlayer;
            
            // 자동 픽업 처리
            if (autoPickup && nearbyPlayer != null)
            {
                TryPickup(nearbyPlayer);
            }
        }
        
        /// <summary>
        /// 하이라이트 업데이트
        /// </summary>
        private void UpdateHighlight()
        {
            bool shouldHighlight = nearbyPlayer != null;
            
            // UI 프롬프트 표시/숨김
            if (pickupPromptUI != null)
            {
                pickupPromptUI.SetActive(shouldHighlight);
            }
            
            // 발광 강도 조정
            if (soulGlow != null && shouldHighlight)
            {
                float baseIntensity = 2f + ((int)soulData.grade * 0.5f);
                float highlightIntensity = baseIntensity * 1.5f;
                soulGlow.SetGlowIntensity(highlightIntensity);
            }
        }
        
        /// <summary>
        /// 영혼 픽업 시도
        /// </summary>
        public void TryPickup(PlayerController player)
        {
            if (isPickedUp || soulData == null || player == null) return;
            
            if (!player.IsOwner) return; // 로컬 플레이어만
            
            // 거리 체크
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance > pickupRange) return;
            
            // 서버에 픽업 요청
            RequestPickupServerRpc(player.OwnerClientId);
        }
        
        /// <summary>
        /// 픽업 요청 (서버)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void RequestPickupServerRpc(ulong playerId)
        {
            if (isPickedUp) return;
            
            // 플레이어 찾기
            var networkManager = NetworkManager.Singleton;
            if (networkManager.ConnectedClients.TryGetValue(playerId, out var client))
            {
                var player = client.PlayerObject?.GetComponent<PlayerController>();
                if (player != null)
                {
                    ProcessPickup(player);
                }
            }
        }
        
        /// <summary>
        /// 픽업 처리 (서버)
        /// </summary>
        private void ProcessPickup(PlayerController player)
        {
            if (isPickedUp) return;
            
            isPickedUp = true;
            
            // 영혼을 플레이어 인벤토리에 추가
            var soulInheritance = player.GetComponent<SoulInheritance>();
            if (soulInheritance != null)
            {
                // MonsterSoulData를 SoulData로 변환하여 추가
                var convertedSoulData = ConvertMonsterSoulToSoulData(soulData);
                // SoulInheritance는 NetworkBehaviour이므로 ServerRpc를 통해 호출
                var characterId = player.NetworkObject.NetworkObjectId;
                soulInheritance.AcquireSoulServerRpc(characterId, convertedSoulData);
                
                // 스킬 효과 즉시 적용 (플레이어 스탯 보너스)
                ApplySkillBonusesToPlayer(player);
            }
            
            // 픽업 이펙트 재생
            PlayPickupEffectClientRpc(transform.position, soulData.grade);
            
            // 통계 업데이트
            MonsterSoulDropSystem.OnMonsterSoulCollected(soulData.grade);
            
            Debug.Log($"💎 {player.name} collected {soulData.soulName} with {soulData.skillCount} skills!");
            
            // 오브젝트 제거
            if (NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn();
            }
        }
        
        /// <summary>
        /// MonsterSoulData를 SoulData로 변환
        /// </summary>
        private SoulData ConvertMonsterSoulToSoulData(MonsterSoulData monsterSoul)
        {
            var soulData = new SoulData
            {
                soulId = monsterSoul.soulId,
                soulName = monsterSoul.soulName,
                floorFound = (int)monsterSoul.grade + 1, // 등급을 층으로 매핑
                acquiredTime = monsterSoul.acquiredTime,
                description = $"{monsterSoul.description} [Contains {monsterSoul.skillCount} skills]",
                statBonus = CalculateTotalStatBonus(monsterSoul)
            };
            
            return soulData;
        }
        
        /// <summary>
        /// 몬스터 영혼의 총 스탯 보너스 계산
        /// </summary>
        private StatBlock CalculateTotalStatBonus(MonsterSoulData monsterSoul)
        {
            StatBlock totalBonus = new StatBlock();
            
            // 모든 스킬의 스탯 보너스 합산
            if (monsterSoul.containedSkills != null)
            {
                foreach (var skill in monsterSoul.containedSkills)
                {
                    totalBonus = totalBonus + skill.GetPlayerStatBonus();
                }
            }
            
            return totalBonus;
        }
        
        /// <summary>
        /// 스킬 보너스를 플레이어에게 적용
        /// </summary>
        private void ApplySkillBonusesToPlayer(PlayerController player)
        {
            if (soulData?.containedSkills == null) return;
            
            var statsManager = player.GetComponent<PlayerStatsManager>();
            if (statsManager == null || statsManager.CurrentStats == null) return;
            
            // 영혼 보너스 스탯에 스킬 효과 추가
            var additionalBonus = CalculateTotalStatBonus(soulData);
            
            // 스탯 업데이트 (PlayerStats의 영혼 보너스 스탯에 추가)
            statsManager.AddSoulBonusStats(additionalBonus);
            
            Debug.Log($"🔮 Applied skill bonuses to {player.name}: +{additionalBonus.strength} STR, +{additionalBonus.intelligence} INT, etc.");
        }
        
        /// <summary>
        /// 픽업 이펙트 재생
        /// </summary>
        [ClientRpc]
        private void PlayPickupEffectClientRpc(Vector3 position, float grade)
        {
            // 등급별 픽업 이펙트
            Color gradeColor = GetGradeColor(grade);
            
            // 파티클 이펙트 생성 (추후 구현)
            Debug.Log($"✨ Playing {grade} grade pickup effect at {position}");
            
            // 사운드 재생
            PlayPickupSound(grade);
            
            // UI 알림
            ShowPickupNotification(soulData.soulName, grade, soulData.skillCount);
        }
        
        /// <summary>
        /// 픽업 사운드 재생
        /// </summary>
        private void PlayPickupSound(float grade)
        {
            // 등급별 다른 사운드 재생 (추후 구현)
            // AudioManager.PlaySound($"SoulPickup_{grade}");
        }
        
        /// <summary>
        /// 픽업 알림 표시
        /// </summary>
        private void ShowPickupNotification(string soulName, float grade, int skillCount)
        {
            // UI 시스템에서 알림 표시 (추후 구현)
            string gradeText = GetGradeDisplayText(grade);
            string message = $"Acquired {gradeText} Soul: {soulName} ({skillCount} skills)";
            
            // NotificationUI.ShowMessage(message, GetGradeColor(grade));
            Debug.Log($"🎉 {message}");
        }
        
        /// <summary>
        /// 등급 표시 텍스트 반환
        /// </summary>
        private string GetGradeDisplayText(float grade)
        {
            float normalized = (grade - 80f) / 40f;
            
            if (normalized < 0.3f) return "Common";      // 80-92
            else if (normalized < 0.5f) return "Uncommon"; // 92-100
            else if (normalized < 0.7f) return "Rare";     // 100-108
            else if (normalized < 0.9f) return "Epic";     // 108-116
            else return "Legendary";                        // 116-120
        }
        
        /// <summary>
        /// 강제 픽업 (디버그용)
        /// </summary>
        [ContextMenu("Force Pickup")]
        public void ForcePickup()
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null && player.IsOwner)
            {
                TryPickup(player);
            }
        }
        
        /// <summary>
        /// 영혼 정보 표시 (디버그용)
        /// </summary>
        [ContextMenu("Show Soul Info")]
        public void ShowSoulInfo()
        {
            if (soulData == null)
            {
                Debug.Log("No soul data available");
                return;
            }
            
            Debug.Log($"=== {soulData.soulName} ({soulData.grade}) ===");
            Debug.Log($"Race: {soulData.race}");
            Debug.Log($"Skills: {soulData.skillCount}");
            Debug.Log($"Description: {soulData.description}");
            
            if (soulData.containedSkills != null)
            {
                Debug.Log("Contained Skills:");
                foreach (var skill in soulData.containedSkills)
                {
                    Debug.Log($"- {skill.skillData.skillName} ({skill.skillGrade})");
                }
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!autoPickup) return;
            
            var player = other.GetComponent<PlayerController>();
            if (player != null && player.IsOwner)
            {
                TryPickup(player);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // 픽업 범위 표시
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, pickupRange);
            
            // 하이라이트 거리 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, highlightDistance);
        }
    }
}