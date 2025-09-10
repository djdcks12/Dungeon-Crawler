using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 종족-무기군 조합별 스프라이트 애니메이션 데이터
    /// 각 종족과 무기군 조합마다 고유한 모션을 가질 수 있도록 함
    /// </summary>
    [CreateAssetMenu(fileName = "New Race-Weapon Data", menuName = "Dungeon Crawler/Race Weapon Group Data")]
    public class RaceWeaponGroupData : ScriptableObject
    {
        [Header("Identity")]
        public Race race;
        public WeaponGroup weaponGroup;
        public string combinationName; // 예: "Human_SwordShield", "Elf_Staff" 등
        [TextArea(2, 4)]
        public string description;
        public Sprite previewSprite; // 에디터 미리보기용
        
        [Header("Idle Animation")]
        [SerializeField] private Sprite[] idleSprites;
        [SerializeField] private float idleFrameRate = 6f;
        
        [Header("Walk Animation")]
        [SerializeField] private Sprite[] walkSprites;
        [SerializeField] private float walkFrameRate = 8f;
        
        [Header("Attack Animation")]
        [SerializeField] private Sprite[] attackSprites;
        [SerializeField] private float attackFrameRate = 12f;
        [SerializeField] private int attackDamageFrame = 2; // 공격 애니메이션 중 데미지 적용 프레임 (0-based)
        
        [Header("Hit Animation")]
        [SerializeField] private Sprite[] hitSprites;
        [SerializeField] private float hitFrameRate = 12f;
        
        [Header("Casting Animation")]
        [SerializeField] private Sprite[] castingSprites;
        [SerializeField] private float castingFrameRate = 10f;
        
        [Header("Death Animation")]
        [SerializeField] private Sprite[] deathSprites;
        [SerializeField] private float deathFrameRate = 8f;
        
        // 프로퍼티들
        public Sprite[] IdleSprites => idleSprites;
        public float IdleFrameRate => idleFrameRate;
        public Sprite[] WalkSprites => walkSprites;
        public float WalkFrameRate => walkFrameRate;
        public Sprite[] AttackSprites => attackSprites;
        public float AttackFrameRate => attackFrameRate;
        public int AttackDamageFrame => attackDamageFrame;
        public Sprite[] HitSprites => hitSprites;
        public float HitFrameRate => hitFrameRate;
        public Sprite[] CastingSprites => castingSprites;
        public float CastingFrameRate => castingFrameRate;
        public Sprite[] DeathSprites => deathSprites;
        public float DeathFrameRate => deathFrameRate;
        
        // 애니메이션 유효성 검사
        public bool HasValidIdleAnimation => idleSprites != null && idleSprites.Length > 0;
        public bool HasValidWalkAnimation => walkSprites != null && walkSprites.Length > 0;
        public bool HasValidAttackAnimation => attackSprites != null && attackSprites.Length > 0;
        public bool HasValidHitAnimation => hitSprites != null && hitSprites.Length > 0;
        public bool HasValidCastingAnimation => castingSprites != null && castingSprites.Length > 0;
        public bool HasValidDeathAnimation => deathSprites != null && deathSprites.Length > 0;
        
        /// <summary>
        /// 기본 스프라이트 가져오기 (첫 번째 Idle 스프라이트)
        /// </summary>
        public Sprite GetDefaultSprite()
        {
            if (HasValidIdleAnimation)
            {
                return idleSprites[0];
            }
            return previewSprite;
        }
        
        /// <summary>
        /// 조합이 올바른지 검증
        /// </summary>
        public bool IsValidCombination()
        {
            // WeaponTypeMapper를 사용하여 종족이 해당 무기군을 사용할 수 있는지 확인
            return WeaponTypeMapper.CanRaceUseWeaponGroup(race, weaponGroup);
        }
        
        /// <summary>
        /// 조합 키 생성 (검색용)
        /// </summary>
        public string GetCombinationKey()
        {
            return $"{race}_{weaponGroup}";
        }
        
        /// <summary>
        /// 에디터용 - 조합명 자동 생성
        /// </summary>
        [ContextMenu("Generate Combination Name")]
        private void GenerateCombinationName()
        {
            combinationName = GetCombinationKey();
        }
        
        /// <summary>
        /// 에디터용 - 조합 유효성 검사
        /// </summary>
        [ContextMenu("Validate Combination")]
        private void ValidateCombination()
        {
            if (IsValidCombination())
            {
                Debug.Log($"✅ Valid combination: {GetCombinationKey()}");
            }
            else
            {
                Debug.LogError($"❌ Invalid combination: {race} cannot use {weaponGroup}");
            }
        }
        
        /// <summary>
        /// 디버그용 정보 출력
        /// </summary>
        public void LogAnimationInfo()
        {
            Debug.Log($"=== {GetCombinationKey()} Animation Info ===");
            Debug.Log($"Idle: {(HasValidIdleAnimation ? idleSprites.Length + " frames" : "None")}");
            Debug.Log($"Walk: {(HasValidWalkAnimation ? walkSprites.Length + " frames" : "None")}");
            Debug.Log($"Attack: {(HasValidAttackAnimation ? attackSprites.Length + " frames" : "None")}");
            Debug.Log($"Hit: {(HasValidHitAnimation ? hitSprites.Length + " frames" : "None")}");
            Debug.Log($"Casting: {(HasValidCastingAnimation ? castingSprites.Length + " frames" : "None")}");
            Debug.Log($"Death: {(HasValidDeathAnimation ? deathSprites.Length + " frames" : "None")}");
        }
        
        private void OnValidate()
        {
            // 에디터에서 값이 변경될 때 자동으로 조합명 업데이트
            if (string.IsNullOrEmpty(combinationName))
            {
                GenerateCombinationName();
            }
        }
    }
}