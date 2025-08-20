using UnityEngine;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 층별 환경적 특징 설정
    /// 유연하게 설정 가능한 던전 환경 효과들을 관리
    /// </summary>
    [CreateAssetMenu(fileName = "New Floor Environment Config", menuName = "Dungeon Crawler/Floor Environment Config")]
    public class FloorEnvironmentConfig : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private int floorNumber = 1;
        [SerializeField] private string environmentName = "기본 환경";
        [TextArea(3, 5)]
        [SerializeField] private string description = "층별 환경적 특징 설명";
        
        [Header("함정 밀도 설정")]
        [SerializeField] private bool modifyTrapDensity = false;
        [Range(0f, 5f)]
        [SerializeField] private float trapDensityMultiplier = 1f;
        [SerializeField] private TrapType[] preferredTrapTypes;
        
        [Header("시야 제한 효과")]
        [SerializeField] private bool enableVisionLimit = false;
        [Range(1f, 20f)]
        [SerializeField] private float visionRange = 10f;
        [SerializeField] private Color fogColor = Color.black;
        [Range(0f, 1f)]
        [SerializeField] private float fogDensity = 0.5f;
        
        [Header("지속 환경 데미지")]
        [SerializeField] private bool enableEnvironmentDamage = false;
        [SerializeField] private EnvironmentDamageType environmentDamageType = EnvironmentDamageType.Toxic;
        [SerializeField] private float damagePerSecond = 5f;
        [SerializeField] private float damageInterval = 1f;
        [SerializeField] private DamageType damageType = DamageType.Magical;
        
        [Header("이동 제약")]
        [SerializeField] private bool enableMovementRestriction = false;
        [Range(0.1f, 2f)]
        [SerializeField] private float movementSpeedMultiplier = 1f;
        [SerializeField] private MovementRestrictionType restrictionType = MovementRestrictionType.Mud;
        
        [Header("특수 환경 효과")]
        [SerializeField] private List<SpecialEnvironmentEffect> specialEffects = new List<SpecialEnvironmentEffect>();
        
        [Header("조명 설정")]
        [SerializeField] private bool modifyLighting = false;
        [Range(0f, 2f)]
        [SerializeField] private float ambientLightIntensity = 1f;
        [SerializeField] private Color ambientLightColor = Color.white;
        [SerializeField] private bool enableFlickeringLights = false;
        
        [Header("오디오 환경")]
        [SerializeField] private AudioClip ambientSound;
        [Range(0f, 1f)]
        [SerializeField] private float ambientVolume = 0.3f;
        [SerializeField] private bool enableEchoEffect = false;
        
        // 프로퍼티
        public int FloorNumber => floorNumber;
        public string EnvironmentName => environmentName;
        public string Description => description;
        
        // 함정 관련
        public bool ModifyTrapDensity => modifyTrapDensity;
        public float TrapDensityMultiplier => trapDensityMultiplier;
        public TrapType[] PreferredTrapTypes => preferredTrapTypes;
        
        // 시야 관련
        public bool EnableVisionLimit => enableVisionLimit;
        public float VisionRange => visionRange;
        public Color FogColor => fogColor;
        public float FogDensity => fogDensity;
        
        // 환경 데미지 관련
        public bool EnableEnvironmentDamage => enableEnvironmentDamage;
        public EnvironmentDamageType EnvironmentDamageType => environmentDamageType;
        public float DamagePerSecond => damagePerSecond;
        public float DamageInterval => damageInterval;
        public DamageType DamageType => damageType;
        
        // 이동 제약 관련
        public bool EnableMovementRestriction => enableMovementRestriction;
        public float MovementSpeedMultiplier => movementSpeedMultiplier;
        public MovementRestrictionType RestrictionType => restrictionType;
        
        // 특수 효과
        public List<SpecialEnvironmentEffect> SpecialEffects => specialEffects;
        
        // 조명 관련
        public bool ModifyLighting => modifyLighting;
        public float AmbientLightIntensity => ambientLightIntensity;
        public Color AmbientLightColor => ambientLightColor;
        public bool EnableFlickeringLights => enableFlickeringLights;
        
        // 오디오 관련
        public AudioClip AmbientSound => ambientSound;
        public float AmbientVolume => ambientVolume;
        public bool EnableEchoEffect => enableEchoEffect;
        
        /// <summary>
        /// 설정 검증
        /// </summary>
        public bool ValidateConfig()
        {
            if (floorNumber < 1 || floorNumber > 11)
            {
                Debug.LogError($"Invalid floor number: {floorNumber}. Must be between 1-11.");
                return false;
            }
            
            if (enableEnvironmentDamage && damagePerSecond < 0f)
            {
                Debug.LogError("Environment damage per second cannot be negative.");
                return false;
            }
            
            if (enableMovementRestriction && movementSpeedMultiplier <= 0f)
            {
                Debug.LogError("Movement speed multiplier must be greater than 0.");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 기본 설정값으로 초기화
        /// </summary>
        [ContextMenu("Reset to Default")]
        public void ResetToDefault()
        {
            modifyTrapDensity = false;
            trapDensityMultiplier = 1f;
            preferredTrapTypes = new TrapType[0];
            
            enableVisionLimit = false;
            visionRange = 10f;
            fogColor = Color.black;
            fogDensity = 0.5f;
            
            enableEnvironmentDamage = false;
            environmentDamageType = EnvironmentDamageType.Toxic;
            damagePerSecond = 5f;
            damageInterval = 1f;
            damageType = DamageType.Magical;
            
            enableMovementRestriction = false;
            movementSpeedMultiplier = 1f;
            restrictionType = MovementRestrictionType.Mud;
            
            specialEffects.Clear();
            
            modifyLighting = false;
            ambientLightIntensity = 1f;
            ambientLightColor = Color.white;
            enableFlickeringLights = false;
            
            ambientSound = null;
            ambientVolume = 0.3f;
            enableEchoEffect = false;
        }
        
        /// <summary>
        /// 특정 층에 맞는 프리셋 설정
        /// </summary>
        public void ApplyFloorPreset(int floor)
        {
            floorNumber = floor;
            
            switch (floor)
            {
                case 1:
                case 2:
                    // 초보자 층 - 기본 설정
                    ResetToDefault();
                    environmentName = "초보자 던전";
                    description = "기본적인 던전 환경";
                    break;
                    
                case 3:
                case 4:
                    // 함정이 많은 층
                    ResetToDefault();
                    environmentName = "함정 던전";
                    description = "곳곳에 함정이 설치된 위험한 던전";
                    modifyTrapDensity = true;
                    trapDensityMultiplier = 2f;
                    preferredTrapTypes = new TrapType[] { TrapType.SpikeTrap, TrapType.PoisonTrap };
                    break;
                    
                case 5:
                case 6:
                    // 시야가 제한된 어둠의 층
                    ResetToDefault();
                    environmentName = "어둠의 던전";
                    description = "짙은 어둠으로 시야가 제한된 던전";
                    enableVisionLimit = true;
                    visionRange = 5f;
                    fogColor = Color.black;
                    fogDensity = 0.8f;
                    modifyLighting = true;
                    ambientLightIntensity = 0.3f;
                    break;
                    
                case 7:
                case 8:
                    // 독성 환경
                    ResetToDefault();
                    environmentName = "독성 던전";
                    description = "독성 가스가 가득한 위험한 던전";
                    enableEnvironmentDamage = true;
                    environmentDamageType = EnvironmentDamageType.Toxic;
                    damagePerSecond = 8f;
                    damageInterval = 2f;
                    damageType = DamageType.Magical;
                    fogColor = Color.green;
                    fogDensity = 0.6f;
                    break;
                    
                case 9:
                case 10:
                    // 화염 지옥
                    ResetToDefault();
                    environmentName = "화염 던전";
                    description = "뜨거운 용암과 화염이 가득한 지옥같은 던전";
                    enableEnvironmentDamage = true;
                    environmentDamageType = EnvironmentDamageType.Burning;
                    damagePerSecond = 12f;
                    damageInterval = 1.5f;
                    damageType = DamageType.Magical;
                    modifyTrapDensity = true;
                    trapDensityMultiplier = 1.5f;
                    preferredTrapTypes = new TrapType[] { TrapType.FireTrap, TrapType.ExplosionTrap };
                    ambientLightColor = Color.red;
                    ambientLightIntensity = 1.5f;
                    break;
                    
                case 11:
                    // 히든 층 - 모든 효과 복합
                    ResetToDefault();
                    environmentName = "혼돈의 던전";
                    description = "모든 위험이 복합된 궁극의 시련";
                    modifyTrapDensity = true;
                    trapDensityMultiplier = 3f;
                    enableVisionLimit = true;
                    visionRange = 7f;
                    enableEnvironmentDamage = true;
                    environmentDamageType = EnvironmentDamageType.Chaotic;
                    damagePerSecond = 15f;
                    damageInterval = 1f;
                    enableMovementRestriction = true;
                    movementSpeedMultiplier = 0.8f;
                    enableFlickeringLights = true;
                    break;
            }
        }
    }
    
    /// <summary>
    /// 환경 데미지 타입
    /// </summary>
    public enum EnvironmentDamageType
    {
        Toxic,      // 독성 (지속 독 데미지)
        Burning,    // 화상 (화염 데미지)
        Freezing,   // 동상 (빙결 데미지)
        Cursed,     // 저주 (어둠 데미지)
        Chaotic     // 혼돈 (랜덤 데미지)
    }
    
    /// <summary>
    /// 이동 제약 타입
    /// </summary>
    public enum MovementRestrictionType
    {
        Mud,        // 진흙 (이동 속도 감소)
        Ice,        // 빙판 (미끄러짐)
        Thorns,     // 가시밭 (이동 시 데미지)
        Quicksand   // 유사 (점진적 속도 감소)
    }
    
    /// <summary>
    /// 특수 환경 효과
    /// </summary>
    [System.Serializable]
    public class SpecialEnvironmentEffect
    {
        public string effectName;
        public EffectType effectType;
        public float intensity;
        public float duration;
        [TextArea(2, 3)]
        public string description;
    }
    
    /// <summary>
    /// 효과 타입
    /// </summary>
    public enum EffectType
    {
        HealthRegenBoost,   // 체력 재생 증가
        ManaRegenBoost,     // 마나 재생 증가
        AttackSpeedBoost,   // 공격 속도 증가
        DefenseBoost,       // 방어력 증가
        LuckBoost,          // 운 증가
        ExpBoost,           // 경험치 증가
        GoldBoost,          // 골드 증가
        DropRateBoost       // 드롭률 증가
    }
}