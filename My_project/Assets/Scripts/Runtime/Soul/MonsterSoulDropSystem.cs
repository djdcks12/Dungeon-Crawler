using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 개편된 몬스터 영혼 드롭 시스템
    /// 몬스터 스킬이 포함된 영혼을 생성
    /// </summary>
    public class MonsterSoulDropSystem : NetworkBehaviour
    {
        [Header("Soul Drop Settings")]
        [SerializeField] private bool enableSoulDrop = true;
        [SerializeField] private float soulDropRadius = 2.0f;
        
        [Header("Soul Prefabs")]
        [SerializeField] private GameObject soulDropPrefab;
        [SerializeField] private GameObject soulCollectEffectPrefab;
        
        [Header("Visual Effects")]
        [SerializeField] private float soulGlowIntensity = 2.0f;
        [SerializeField] private Color soulGlowColor = Color.cyan;
        
        // 통계
        private static int totalMonsterSoulsDropped = 0;
        private static int totalMonsterSoulsCollected = 0;
        
        /// <summary>
        /// 몬스터 사망 시 영혼 드롭 체크
        /// </summary>
        public void CheckMonsterSoulDrop(MonsterEntity monsterEntity)
        {
            if (!IsServer || !enableSoulDrop || monsterEntity == null) return;
            
            // 몬스터 종족 데이터에서 드롭률 가져오기
            float dropRate = monsterEntity.RaceData.CalculateSoulDropRateForGrade(monsterEntity.Grade);
            
            // 확률 체크
            if (Random.value <= dropRate)
            {
                CreateMonsterSoulDrop(monsterEntity);
            }
            else
            {
                Debug.Log($"🎲 Monster soul drop failed: {monsterEntity.VariantData.variantName} ({dropRate:P3})");
            }
        }
        
        /// <summary>
        /// 몬스터 영혼 드롭 생성
        /// </summary>
        private void CreateMonsterSoulDrop(MonsterEntity monsterEntity)
        {
            if (!IsServer) return;
            
            // 영혼 드롭 위치 계산
            Vector3 dropPosition = GetValidDropPosition(monsterEntity.transform.position);
            
            // 영혼 데이터 생성 (스킬 포함)
            MonsterSoulData soulData = GenerateMonsterSoulData(monsterEntity);
            
            // 영혼 드롭 오브젝트 생성
            GameObject soulDrop = CreateMonsterSoulDropObject(dropPosition, soulData);
            
            // 통계 업데이트
            totalMonsterSoulsDropped++;
            
            // 모든 클라이언트에 알림
            NotifyMonsterSoulDropClientRpc(dropPosition, soulData.soulName, soulData.grade, soulData.skillCount);
            
            Debug.Log($"💎 Monster soul dropped! {soulData.soulName} ({soulData.grade}) with {soulData.skillCount} skills - Total: {totalMonsterSoulsDropped}");
        }
        
        /// <summary>
        /// 몬스터 영혼 데이터 생성 (스킬 포함)
        /// </summary>
        private MonsterSoulData GenerateMonsterSoulData(MonsterEntity monsterEntity)
        {
            var soulData = new MonsterSoulData
            {
                soulId = GenerateUniqueSoulId(),
                soulName = $"{monsterEntity.VariantData.variantName} Soul",
                race = monsterEntity.RaceData.raceType,
                grade = monsterEntity.Grade,
                acquiredTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                description = $"Soul of {monsterEntity.VariantData.variantName} from {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}",
                skillCount = monsterEntity.ActiveSkills.Count
            };
            
            // 몬스터가 가진 스킬들을 영혼에 복사 (정확한 수치 포함)
            soulData.containedSkills = new List<MonsterSoulSkill>();
            foreach (var skill in monsterEntity.ActiveSkills)
            {
                var soulSkill = new MonsterSoulSkill
                {
                    skillData = skill.skillData,
                    skillGrade = skill.effectGrade,
                    // 실제 몬스터가 사용한 정확한 스킬 효과 값들을 저장
                    skillEffect = skill.GetCurrentEffect()
                };
                soulData.containedSkills.Add(soulSkill);
                
                // 정확한 수치가 포함되었음을 로그로 확인
                var actualStatBonus = skill.GetActualStatBlock();
                Debug.Log($"🔮 Captured skill: {skill.skillData.skillName} (Grade {skill.effectGrade:F1}) - Exact StatBlock: STR +{actualStatBonus.strength:F1}, AGI +{actualStatBonus.agility:F1}, VIT +{actualStatBonus.vitality:F1}, INT +{actualStatBonus.intelligence:F1}");
            }
            
            // 몬스터의 최종 스탯도 포함 (참고용)
            soulData.monsterStats = monsterEntity.FinalStats;
            
            return soulData;
        }
        
        /// <summary>
        /// 고유 영혼 ID 생성
        /// </summary>
        private ulong GenerateUniqueSoulId()
        {
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
                
                // 장애물 체크
                if (!Physics2D.OverlapCircle(testPosition, 0.3f))
                {
                    return testPosition;
                }
            }
            
            return basePosition; // 실패 시 원래 위치
        }
        
        /// <summary>
        /// 몬스터 영혼 드롭 오브젝트 생성
        /// </summary>
        private GameObject CreateMonsterSoulDropObject(Vector3 position, MonsterSoulData soulData)
        {
            GameObject soulDrop;
            
            if (soulDropPrefab != null)
            {
                soulDrop = Instantiate(soulDropPrefab, position, Quaternion.identity);
            }
            else
            {
                soulDrop = CreateDefaultMonsterSoulDropPrefab(position, soulData);
            }
            
            // MonsterSoulPickup 컴포넌트 설정
            var soulPickup = soulDrop.GetComponent<MonsterSoulPickup>();
            if (soulPickup == null)
            {
                soulPickup = soulDrop.AddComponent<MonsterSoulPickup>();
            }
            
            soulPickup.SetMonsterSoulData(soulData);
            
            // 시각적 효과 설정 (등급별 차별화)
            SetupMonsterSoulVisuals(soulDrop, soulData);
            
            // 네트워크 스폰
            var networkObject = soulDrop.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
            }
            
            return soulDrop;
        }
        
        /// <summary>
        /// 기본 몬스터 영혼 드롭 프리팹 생성
        /// </summary>
        private GameObject CreateDefaultMonsterSoulDropPrefab(Vector3 position, MonsterSoulData soulData)
        {
            var soulDrop = new GameObject($"MonsterSoul_{soulData.soulName}");
            soulDrop.transform.position = position;
            
            // 기본 컴포넌트들
            var spriteRenderer = soulDrop.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateMonsterSoulSprite(soulData.grade);
            spriteRenderer.color = GetGradeColor(soulData.grade);
            spriteRenderer.sortingOrder = 15; // 일반 영혼보다 높게
            
            var collider = soulDrop.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = GetGradeRadius(soulData.grade);
            
            var networkObject = soulDrop.AddComponent<NetworkObject>();
            
            return soulDrop;
        }
        
        /// <summary>
        /// 등급별 색상 반환 (80-120 범위)
        /// </summary>
        private Color GetGradeColor(float grade)
        {
            // 80~120을 0~1 범위로 정규화
            float normalized = (grade - 80f) / 40f;
            
            if (normalized < 0.3f) return Color.white;      // 80-92: Common
            else if (normalized < 0.5f) return Color.green; // 92-100: Uncommon
            else if (normalized < 0.7f) return Color.blue;  // 100-108: Rare
            else if (normalized < 0.9f) return Color.magenta; // 108-116: Epic
            else return Color.yellow;                         // 116-120: Legendary
        }
        
        /// <summary>
        /// 등급별 크기 반환 (80-120 범위)
        /// </summary>
        private float GetGradeRadius(float grade)
        {
            // 80~120을 0.8~2.0 크기로 매핑
            float normalized = (grade - 80f) / 40f;
            return 0.8f + (normalized * 1.2f); // 0.8부터 2.0까지 선형 스케일
        }
        
        /// <summary>
        /// 몬스터 영혼 스프라이트 생성 (등급별)
        /// </summary>
        private Sprite CreateMonsterSoulSprite(float grade)
        {
            // 80-120을 32-64 픽셀 크기로 매핑
            int size = Mathf.RoundToInt(32 + ((grade - 80f) / 40f) * 32f);
            Texture2D texture = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];
            
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 4f;
            Color gradeColor = GetGradeColor(grade);
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - distance / radius);
                    
                    // 등급이 높을수록 더 밝고 복잡한 패턴
                    float intensity = 1f + ((grade - 80f) / 40f * 0.8f); // 80=1.0, 120=1.8
                    alpha *= intensity;
                    
                    pixels[y * size + x] = new Color(gradeColor.r, gradeColor.g, gradeColor.b, alpha);
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
        
        /// <summary>
        /// 몬스터 영혼 시각적 효과 설정
        /// </summary>
        private void SetupMonsterSoulVisuals(GameObject soulDrop, MonsterSoulData soulData)
        {
            // 등급별 발광 효과
            var soulGlow = soulDrop.AddComponent<SoulGlow>();
            Color gradeColor = GetGradeColor(soulData.grade);
            float gradeIntensity = soulGlowIntensity + ((soulData.grade - 80f) / 40f * 2f); // 80=base, 120=+2.0
            soulGlow.SetGlowSettings(gradeColor, gradeIntensity);
            
            // 부유 애니메이션 (등급별 속도)
            var floatAnimation = soulDrop.AddComponent<SoulFloatAnimation>();
            float animSpeed = 1f + ((soulData.grade - 80f) / 40f * 2f); // 80=1.0, 120=3.0 범위
            floatAnimation.SetFloatSpeed(animSpeed);
            floatAnimation.StartFloating();
            
            // 등급이 높을수록 추가 이펙트
            if (soulData.grade >= 100f) // 100+ 등급 (Champion 수준)
            {
                // 파티클 이펙트 추가 (추후 구현)
                Debug.Log($"✨ Adding special effects for {soulData.grade} grade soul");
            }
        }
        
        /// <summary>
        /// 몬스터 영혼 드롭 알림
        /// </summary>
        [ClientRpc]
        private void NotifyMonsterSoulDropClientRpc(Vector3 position, string soulName, float grade, int skillCount)
        {
            // 등급별 차별화된 이펙트
            PlayMonsterSoulDropEffect(position, grade);
            
            // 등급별 차별화된 UI 알림
            string gradeText = GetGradeDisplayText(grade);
            Debug.Log($"🌟 {gradeText} MONSTER SOUL! {soulName} with {skillCount} skills!");
            
            // 등급에 따른 화면 이펙트
            StartCoroutine(MonsterSoulDropScreenEffect(grade));
        }
        
        /// <summary>
        /// 등급 표시 텍스트 반환 (80-120 범위)
        /// </summary>
        private string GetGradeDisplayText(float grade)
        {
            float normalized = (grade - 80f) / 40f;
            
            if (normalized < 0.3f) return "COMMON";      // 80-92
            else if (normalized < 0.5f) return "UNCOMMON"; // 92-100
            else if (normalized < 0.7f) return "RARE";     // 100-108
            else if (normalized < 0.9f) return "EPIC";     // 108-116
            else return "LEGENDARY";                        // 116-120
        }
        
        /// <summary>
        /// 몬스터 영혼 드롭 이펙트 재생
        /// </summary>
        private void PlayMonsterSoulDropEffect(Vector3 position, float grade)
        {
            if (soulCollectEffectPrefab != null)
            {
                var effect = Instantiate(soulCollectEffectPrefab, position, Quaternion.identity);
                
                // 등급별 이펙트 스케일 조정
                float scale = 1f + ((grade - 80f) / 40f * 1.2f); // 80=1.0, 120=2.2
                effect.transform.localScale = Vector3.one * scale;
                
                // 등급별 색상 적용
                var particles = effect.GetComponentsInChildren<ParticleSystem>();
                Color gradeColor = GetGradeColor(grade);
                foreach (var particle in particles)
                {
                    var main = particle.main;
                    main.startColor = gradeColor;
                }
                
                Destroy(effect, 3f + ((grade - 80f) / 40f * 2f)); // 80=3s, 120=5s
            }
        }
        
        /// <summary>
        /// 몬스터 영혼 드롭 화면 효과
        /// </summary>
        private IEnumerator MonsterSoulDropScreenEffect(float grade)
        {
            // 등급별 차별화된 화면 효과
            float effectDuration = 1f + ((grade - 80f) / 40f * 2f); // 80=1s, 120=3s
            
            // 추후 UI 시스템에서 구현
            // 예: 등급별 다른 색상의 화면 글로우, 사운드 등
            
            yield return new WaitForSeconds(effectDuration);
        }
        
        /// <summary>
        /// 몬스터 영혼 수집 처리
        /// </summary>
        public static void OnMonsterSoulCollected(float grade)
        {
            totalMonsterSoulsCollected++;
            Debug.Log($"📊 Monster soul collected! Grade: {grade}, Total: {totalMonsterSoulsCollected}/{totalMonsterSoulsDropped}");
        }
        
        /// <summary>
        /// 몬스터 영혼 드롭 통계
        /// </summary>
        public static (int dropped, int collected) GetMonsterSoulStatistics()
        {
            return (totalMonsterSoulsDropped, totalMonsterSoulsCollected);
        }
        
        public override void OnDestroy()
        {
            StopAllCoroutines();
            base.OnDestroy();
        }

        /// <summary>
        /// 강제 몬스터 영혼 드롭 (테스트용)
        /// </summary>
        [ContextMenu("Force Monster Soul Drop")]
        public void ForceMonsterSoulDrop()
        {
            if (Application.isPlaying && IsServer)
            {
                // 테스트용 몬스터 엔티티 찾기
                var testMonster = FindFirstObjectByType<MonsterEntity>();
                if (testMonster != null)
                {
                    CreateMonsterSoulDrop(testMonster);
                }
                else
                {
                    Debug.LogWarning("No MonsterEntity found for testing");
                }
            }
        }
    }
    
    /// <summary>
    /// 몬스터 영혼 데이터 (스킬 포함)
    /// </summary>
    [System.Serializable]
    public class MonsterSoulData
    {
        public ulong soulId;
        public string soulName;
        public MonsterRace race;
        public float grade;
        public long acquiredTime;
        public string description;
        public int skillCount;
        
        // 포함된 스킬들
        public List<MonsterSoulSkill> containedSkills;
        
        // 참고용 몬스터 스탯
        public StatBlock monsterStats;
    }
    
    /// <summary>
    /// 영혼에 포함된 스킬 데이터
    /// </summary>
    [System.Serializable]
    public class MonsterSoulSkill
    {
        public MonsterSkillData skillData;
        public float skillGrade;
        public MonsterSkillEffect skillEffect;
        
        /// <summary>
        /// 플레이어가 사용할 수 있는 형태로 변환
        /// </summary>
        public StatBlock GetPlayerStatBonus()
        {
            // 몬스터 스킬을 플레이어 스탯 보너스로 변환
            return skillEffect.GetStatBlockForGrade(skillGrade);
        }
        
        /// <summary>
        /// 스킬 설명 생성 (정확한 수치 포함)
        /// </summary>
        public string GetSkillDescription()
        {
            var statBonus = GetPlayerStatBonus();
            var effects = new List<string>();
            
            if (statBonus.strength > 0) effects.Add($"STR +{statBonus.strength:F1}");
            if (statBonus.agility > 0) effects.Add($"AGI +{statBonus.agility:F1}");
            if (statBonus.vitality > 0) effects.Add($"VIT +{statBonus.vitality:F1}");
            if (statBonus.intelligence > 0) effects.Add($"INT +{statBonus.intelligence:F1}");
            if (statBonus.defense > 0) effects.Add($"DEF +{statBonus.defense:F1}");
            if (statBonus.magicDefense > 0) effects.Add($"M.DEF +{statBonus.magicDefense:F1}");
            if (statBonus.luck > 0) effects.Add($"LUK +{statBonus.luck:F1}");
            if (statBonus.stability > 0) effects.Add($"STAB +{statBonus.stability:F1}");
            
            string effectsText = effects.Count > 0 ? $" [{string.Join(", ", effects)}]" : "";
            return $"{skillData.skillName} (Grade {skillGrade:F1}){effectsText}";
        }
    }
}