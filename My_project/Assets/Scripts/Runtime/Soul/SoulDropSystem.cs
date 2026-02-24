using UnityEngine;
using Unity.Netcode;
using System.Collections;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 영혼 드롭 시스템 - 0.1% 확률로 영혼 드롭
    /// 하드코어 던전 크롤러의 유일한 영구 진행도
    /// </summary>
    public class SoulDropSystem : NetworkBehaviour
    {
        [Header("Soul Drop Settings")]
        [SerializeField] private float soulDropRate = 0.001f; // 0.1% 확률
        [SerializeField] private bool enableSoulDrop = true;
        [SerializeField] private float soulDropRadius = 2.0f;
        
        [Header("Soul Prefabs")]
        [SerializeField] private GameObject soulDropPrefab;
        [SerializeField] private GameObject soulCollectEffectPrefab;
        
        [Header("Visual Effects")]
        [SerializeField] private float soulGlowIntensity = 2.0f;
        [SerializeField] private Color soulGlowColor = Color.cyan;
        
        // 드롭된 영혼들 추적
        private static int totalSoulsDropped = 0;
        private static int totalSoulsCollected = 0;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // CombatSystem 이벤트 구독 (적 처치 시)
            var combatSystem = GetComponent<CombatSystem>();
            if (combatSystem != null)
            {
                // 추후 CombatSystem에 적 처치 이벤트 추가 예정
            }
        }
        
        /// <summary>
        /// 적 처치 시 영혼 드롭 확인 (CombatSystem에서 호출)
        /// </summary>
        public void CheckSoulDrop(Vector3 killPosition, int enemyLevel, string enemyName)
        {
            if (!IsServer || !enableSoulDrop) return;
            
            // 0.1% 확률 계산
            float randomValue = Random.Range(0f, 1f);
            
            if (randomValue <= soulDropRate)
            {
                CreateSoulDrop(killPosition, enemyLevel, enemyName);
            }
            else
            {
                Debug.Log($"🎲 Soul drop check failed: {randomValue:F4} > {soulDropRate:F4}");
            }
        }
        
        /// <summary>
        /// 영혼 드롭 생성
        /// </summary>
        private void CreateSoulDrop(Vector3 position, int sourceLevel, string sourceName, PlayerStatsData sourceStats = null)
        {
            if (!IsServer) return;
            
            // 영혼 드롭 위치 계산 (장애물 피하기)
            Vector3 dropPosition = GetValidDropPosition(position);
            
            // 영혼 데이터 생성
            SoulData soulData = GenerateSoulData(sourceLevel, sourceName, sourceStats);
            
            // 영혼 드롭 오브젝트 생성
            GameObject soulDrop = CreateSoulDropObject(dropPosition, soulData);
            
            // 통계 업데이트
            totalSoulsDropped++;
            
            // 모든 클라이언트에 알림
            NotifySoulDropClientRpc(dropPosition, soulData.soulName, soulData.floorFound);
            
            Debug.Log($"✨ Soul dropped! {soulData.soulName} (Floor {soulData.floorFound}) - Total dropped: {totalSoulsDropped}");
        }
        
        /// <summary>
        /// 영혼 데이터 생성
        /// </summary>
        private SoulData GenerateSoulData(int sourceLevel, string sourceName, PlayerStatsData sourceStats = null)
        {
            string currentLocation = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            var soulData = new SoulData
            {
                soulId = GenerateUniqueSoulId(),
                soulName = sourceName,
                sourceLevel = sourceLevel,
                acquiredTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                obtainLocation = currentLocation,
                floorFound = sourceLevel,
                description = $"Level {sourceLevel} soul from {currentLocation}"
            };

            if (sourceStats != null)
            {
                // 플레이어 영혼: 원래 스탯의 일부를 보너스로 변환
                soulData.sourceRace = sourceStats.CharacterRace.ToString();
                soulData.statBonus = CalculatePlayerSoulBonus(sourceStats);
            }
            else
            {
                // 몬스터 영혼: 레벨 기반 랜덤 스탯 보너스
                soulData.sourceRace = "Monster";
                soulData.statBonus = CalculateMonsterSoulBonus(sourceLevel);
            }
            
            return soulData;
        }
        
        /// <summary>
        /// 플레이어 영혼 스탯 보너스 계산
        /// </summary>
        private StatBlock CalculatePlayerSoulBonus(PlayerStatsData playerStats)
        {
            // 플레이어 스탯의 10-20%를 영혼 보너스로 변환
            float bonusPercentage = Random.Range(0.1f, 0.2f);
            
            return new StatBlock
            {
                strength = Mathf.RoundToInt(playerStats.TotalSTR * bonusPercentage),
                agility = Mathf.RoundToInt(playerStats.TotalAGI * bonusPercentage),
                vitality = Mathf.RoundToInt(playerStats.TotalVIT * bonusPercentage),
                intelligence = Mathf.RoundToInt(playerStats.TotalINT * bonusPercentage),
                defense = Mathf.RoundToInt(playerStats.TotalDEF * bonusPercentage),
                magicDefense = Mathf.RoundToInt(playerStats.TotalMDEF * bonusPercentage),
                luck = Mathf.RoundToInt(playerStats.TotalLUK * bonusPercentage),
                stability = Mathf.RoundToInt(playerStats.TotalSTAB * bonusPercentage)
            };
        }
        
        /// <summary>
        /// 몬스터 영혼 스탯 보너스 계산
        /// </summary>
        private StatBlock CalculateMonsterSoulBonus(int monsterLevel)
        {
            // 몬스터 레벨 기반 랜덤 스탯 보너스 (1-3 범위)
            int bonusRange = Mathf.Clamp(monsterLevel / 5 + 1, 1, 5);
            
            return new StatBlock
            {
                strength = Random.Range(1, bonusRange + 1),
                agility = Random.Range(1, bonusRange + 1),
                vitality = Random.Range(1, bonusRange + 1),
                intelligence = Random.Range(1, bonusRange + 1),
                defense = Random.Range(0, bonusRange),
                magicDefense = Random.Range(0, bonusRange),
                luck = Random.Range(0, bonusRange),
                stability = Random.Range(0, bonusRange)
            };
        }
        
        /// <summary>
        /// 고유 영혼 ID 생성
        /// </summary>
        private ulong GenerateUniqueSoulId()
        {
            // 현재 시간과 랜덤값을 조합하여 고유 ID 생성
            long timestamp = System.DateTime.Now.Ticks;
            int random = Random.Range(1000, 9999);
            
            return (ulong)(timestamp + random);
        }
        
        /// <summary>
        /// 유효한 드롭 위치 계산
        /// </summary>
        private Vector3 GetValidDropPosition(Vector3 basePosition)
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(0.5f, soulDropRadius);
                
                Vector3 testPosition = basePosition + new Vector3(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance,
                    0f
                );
                
                // 장애물 체크 (추후 구현)
                if (!Physics2D.OverlapCircle(testPosition, 0.3f))
                {
                    return testPosition;
                }
            }
            
            return basePosition; // 실패 시 원래 위치
        }
        
        /// <summary>
        /// 영혼 드롭 오브젝트 생성
        /// </summary>
        private GameObject CreateSoulDropObject(Vector3 position, SoulData soulData)
        {
            GameObject soulDrop;
            
            if (soulDropPrefab != null)
            {
                soulDrop = Instantiate(soulDropPrefab, position, Quaternion.identity);
            }
            else
            {
                soulDrop = CreateDefaultSoulDropPrefab(position);
            }
            
            // SoulPickup 컴포넌트 설정
            var soulPickup = soulDrop.GetComponent<SoulPickup>();
            if (soulPickup == null)
            {
                soulPickup = soulDrop.AddComponent<SoulPickup>();
            }
            
            soulPickup.SetSoulData(soulData);
            
            // 시각적 효과 설정
            SetupSoulVisuals(soulDrop);
            
            // 네트워크 스폰
            var networkObject = soulDrop.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
            }
            
            return soulDrop;
        }
        
        /// <summary>
        /// 기본 영혼 드롭 프리팹 생성
        /// </summary>
        private GameObject CreateDefaultSoulDropPrefab(Vector3 position)
        {
            var soulDrop = new GameObject("SoulDrop");
            soulDrop.transform.position = position;
            
            // 기본 컴포넌트들
            var spriteRenderer = soulDrop.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateSoulSprite();
            spriteRenderer.color = soulGlowColor;
            spriteRenderer.sortingOrder = 10; // 다른 오브젝트 위에 표시
            
            var collider = soulDrop.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.8f;
            
            var networkObject = soulDrop.AddComponent<NetworkObject>();
            
            return soulDrop;
        }
        
        /// <summary>
        /// 영혼 스프라이트 생성 (임시)
        /// </summary>
        private Sprite CreateSoulSprite()
        {
            // 임시로 작은 원형 텍스처 생성
            Texture2D texture = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];
            
            Vector2 center = new Vector2(16, 16);
            float radius = 12f;
            
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - distance / radius);
                    pixels[y * 32 + x] = new Color(soulGlowColor.r, soulGlowColor.g, soulGlowColor.b, alpha);
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        }
        
        /// <summary>
        /// 영혼 시각적 효과 설정
        /// </summary>
        private void SetupSoulVisuals(GameObject soulDrop)
        {
            // 발광 효과 (SoulGlow 컴포넌트 추가)
            var soulGlow = soulDrop.AddComponent<SoulGlow>();
            soulGlow.SetGlowSettings(soulGlowColor, soulGlowIntensity);
            
            // 부유 애니메이션
            var floatAnimation = soulDrop.AddComponent<SoulFloatAnimation>();
            floatAnimation.StartFloating();
        }
        
        /// <summary>
        /// 영혼 드롭 알림
        /// </summary>
        [ClientRpc]
        private void NotifySoulDropClientRpc(Vector3 position, string soulName, int level)
        {
            // 특별한 사운드/이펙트 재생
            PlaySoulDropEffect(position);
            
            // UI 알림 (추후 UI 시스템에서 구현)
            Debug.Log($"🌟 RARE SOUL DROP! {soulName} (Level {level}) appeared!");
            
            // 화면 이펙트 (빛나는 효과 등)
            StartCoroutine(SoulDropScreenEffect());
        }
        
        /// <summary>
        /// 영혼 드롭 이펙트 재생
        /// </summary>
        private void PlaySoulDropEffect(Vector3 position)
        {
            if (soulCollectEffectPrefab != null)
            {
                var effect = Instantiate(soulCollectEffectPrefab, position, Quaternion.identity);
                Destroy(effect, 3f);
            }
        }
        
        /// <summary>
        /// 영혼 드롭 화면 효과
        /// </summary>
        private IEnumerator SoulDropScreenEffect()
        {
            // 추후 UI 시스템에서 구현
            // 예: 화면 가장자리 파란색 글로우, 특별한 사운드 등
            yield return null;
        }
        
        /// <summary>
        /// 영혼 수집 처리 (SoulPickup에서 호출)
        /// </summary>
        public static void OnSoulCollected()
        {
            totalSoulsCollected++;
            Debug.Log($"📊 Soul collected! Total: {totalSoulsCollected}/{totalSoulsDropped}");
        }
        
        public override void OnDestroy()
        {
            StopAllCoroutines();
            base.OnDestroy();
        }

        /// <summary>
        /// 영혼 드롭 통계
        /// </summary>
        public static (int dropped, int collected) GetSoulStatistics()
        {
            return (totalSoulsDropped, totalSoulsCollected);
        }
    }
}