using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 이펙트 타입
    /// </summary>
    public enum SkillEffectType
    {
        HitEffect,      // 단일 타격 이펙트
        ProjectileSkill,// 투사체형 스킬
        SummonSkill     // 소환형 스킬
    }

    /// <summary>
    /// 투사체 이펙트의 단계
    /// </summary>
    public enum ProjectilePhase
    {
        Start,      // 시작 애니메이션
        Repeatable, // 반복 애니메이션 (비행 중)
        Hit         // 타격 시 애니메이션
    }

    /// <summary>
    /// 소환 이펙트의 단계
    /// </summary>
    public enum SummonPhase
    {
        Start,      // 소환 시작
        Active,     // 활성화 (데미지 적용)
        Repeatable, // 반복 단계 (옵션)
        Ending      // 종료
    }

    /// <summary>
    /// 이펙트 데이터 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "New Effect Data", menuName = "Dungeon Crawler/Effect Data")]
    public class EffectData : ScriptableObject
    {
        [Header("Basic Info")]
        public string effectName;
        public SkillEffectType effectType;
        
        [Header("Hit Effect (단일 애니메이션)")]
        [SerializeField] private Sprite[] hitEffectFrames; // 간단한 스프라이트 배열
        [SerializeField] private float hitFrameRate = 12f;
        [SerializeField] private float hitDuration = 1f;
        
        [Header("Projectile Effect")]
        [SerializeField] private Sprite[] projectileStartFrames;
        [SerializeField] private Sprite[] projectileRepeatableFrames;
        [SerializeField] private Sprite[] projectileHitFrames;
        [SerializeField] private float projectileSpeed = 5f;
        [SerializeField] private float projectileFrameRate = 12f;
        [SerializeField] private float startDuration = 0.2f;
        [SerializeField] private float hitEffectDuration = 0.5f;
        
        [Header("Summon Effect")]
        [SerializeField] private Sprite[] summonStartFrames;
        [SerializeField] private Sprite[] summonActiveFrames;
        [SerializeField] private Sprite[] summonRepeatableFrames;
        [SerializeField] private Sprite[] summonEndingFrames;
        [SerializeField] private float summonFrameRate = 12f;
        [SerializeField] private float summonStartDuration = 0.5f;
        [SerializeField] private float summonActiveDuration = 2f;
        [SerializeField] private float summonRepeatableDuration = 0.3f;
        [SerializeField] private float summonEndingDuration = 0.5f;
        [SerializeField] private float damageRadius = 2f;
        [SerializeField] private float damageInterval = 0.5f; // 데미지 적용 간격
        
        // 프로퍼티들
        public Sprite[] HitEffectFrames => hitEffectFrames;
        public float HitFrameRate => hitFrameRate;
        public float HitDuration => hitDuration;

        public Sprite[] ProjectileStartFrames => projectileStartFrames;
        public Sprite[] ProjectileRepeatableFrames => projectileRepeatableFrames;
        public Sprite[] ProjectileHitFrames => projectileHitFrames;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileFrameRate => projectileFrameRate;
        public float StartDuration => startDuration;
        public float HitEffectDuration => hitEffectDuration;

        public Sprite[] SummonStartFrames => summonStartFrames;
        public Sprite[] SummonActiveFrames => summonActiveFrames;
        public Sprite[] SummonRepeatableFrames => summonRepeatableFrames;
        public Sprite[] SummonEndingFrames => summonEndingFrames;
        public float SummonFrameRate => summonFrameRate;
        public float SummonStartDuration => summonStartDuration;
        public float SummonActiveDuration => summonActiveDuration;
        public float SummonRepeatableDuration => summonRepeatableDuration;
        public float SummonEndingDuration => summonEndingDuration;
        public float DamageRadius => damageRadius;
        public float DamageInterval => damageInterval;
    }
}