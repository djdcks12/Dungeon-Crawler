using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 영혼 발광 효과 - 영혼이 신비로운 빛을 발하는 시각적 효과
    /// </summary>
    public class SoulGlow : MonoBehaviour
    {
        [Header("Glow Settings")]
        [SerializeField] private Color glowColor = Color.cyan;
        [SerializeField] private float glowIntensity = 2.0f;
        [SerializeField] private float glowSpeed = 1.5f;
        [SerializeField] private float glowRadius = 1.5f;
        
        [Header("Pulse Settings")]
        [SerializeField] private bool enablePulse = true;
        [SerializeField] private float pulseMinIntensity = 0.5f;
        [SerializeField] private float pulseMaxIntensity = 2.0f;
        [SerializeField] private float pulseSpeed = 2.0f;
        
        // 컴포넌트 참조
        private SpriteRenderer spriteRenderer;
        private SoulLight2D glowLight; // 영혼 전용 라이트
        
        // 애니메이션 변수
        private float glowTimer = 0f;
        private float pulseTimer = 0f;
        private Color originalColor;
        
        private void Start()
        {
            InitializeComponents();
            SetupGlowEffect();
        }
        
        private void Update()
        {
            UpdateGlowAnimation();
        }
        
        /// <summary>
        /// 컴포넌트 초기화
        /// </summary>
        private void InitializeComponents()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            // 2D Light 컴포넌트 확인 (URP 사용 시)
            glowLight = GetComponent<SoulLight2D>();
            
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }
        
        /// <summary>
        /// 발광 효과 설정
        /// </summary>
        private void SetupGlowEffect()
        {
            if (spriteRenderer != null)
            {
                // 스프라이트에 발광 색상 적용
                spriteRenderer.color = glowColor;
                
                // 머티리얼에 Emission 속성 설정 (셰이더가 지원하는 경우)
                var material = spriteRenderer.material;
                if (material != null && material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", glowColor * glowIntensity);
                    material.EnableKeyword("_EMISSION");
                }
            }
            
            // 2D Light 설정 (URP 사용 시)
            if (glowLight != null)
            {
                glowLight.color = glowColor;
                glowLight.intensity = glowIntensity;
                glowLight.pointLightOuterRadius = glowRadius;
            }
        }
        
        /// <summary>
        /// 발광 애니메이션 업데이트
        /// </summary>
        private void UpdateGlowAnimation()
        {
            glowTimer += Time.deltaTime * glowSpeed;
            
            if (enablePulse)
            {
                pulseTimer += Time.deltaTime * pulseSpeed;
                
                // 펄스 강도 계산
                float pulseValue = Mathf.Lerp(pulseMinIntensity, pulseMaxIntensity, 
                    (Mathf.Sin(pulseTimer) + 1f) * 0.5f);
                
                // 스프라이트 색상 애니메이션
                if (spriteRenderer != null)
                {
                    Color animatedColor = glowColor * pulseValue;
                    animatedColor.a = originalColor.a;
                    spriteRenderer.color = animatedColor;
                }
                
                // 2D Light 애니메이션
                if (glowLight != null)
                {
                    glowLight.intensity = glowIntensity * pulseValue;
                }
            }
            
            // 회전 효과 (선택적)
            transform.Rotate(0, 0, 30f * Time.deltaTime);
        }
        
        /// <summary>
        /// 발광 설정 변경
        /// </summary>
        public void SetGlowSettings(Color color, float intensity)
        {
            glowColor = color;
            glowIntensity = intensity;
            SetupGlowEffect();
        }
        
        /// <summary>
        /// 펄스 효과 토글
        /// </summary>
        public void SetPulseEnabled(bool enabled)
        {
            enablePulse = enabled;
        }
        
        /// <summary>
        /// 발광 강도 변경
        /// </summary>
        public void SetGlowIntensity(float intensity)
        {
            glowIntensity = intensity;
            
            if (glowLight != null)
            {
                glowLight.intensity = intensity;
            }
        }
        
        /// <summary>
        /// 발광 색상 변경
        /// </summary>
        public void SetGlowColor(Color color)
        {
            glowColor = color;
            
            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }
            
            if (glowLight != null)
            {
                glowLight.color = color;
            }
        }
        
        /// <summary>
        /// 발광 효과 페이드 아웃
        /// </summary>
        public void FadeOut(float duration)
        {
            StartCoroutine(FadeOutCoroutine(duration));
        }
        
        /// <summary>
        /// 페이드 아웃 코루틴
        /// </summary>
        private System.Collections.IEnumerator FadeOutCoroutine(float duration)
        {
            float startIntensity = glowIntensity;
            Color startColor = spriteRenderer?.color ?? glowColor;
            
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // 강도 감소
                float currentIntensity = Mathf.Lerp(startIntensity, 0f, t);
                SetGlowIntensity(currentIntensity);
                
                // 알파 감소
                if (spriteRenderer != null)
                {
                    Color currentColor = startColor;
                    currentColor.a = Mathf.Lerp(startColor.a, 0f, t);
                    spriteRenderer.color = currentColor;
                }
                
                yield return null;
            }
            
            // 완전히 투명하게
            SetGlowIntensity(0f);
            if (spriteRenderer != null)
            {
                Color finalColor = spriteRenderer.color;
                finalColor.a = 0f;
                spriteRenderer.color = finalColor;
            }
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
    
    /// <summary>
    /// 영혼 전용 라이트 컴포넌트 (Unity 2D Light 시스템과 구분)
    /// </summary>
    public class SoulLight2D : MonoBehaviour
    {
        public Color color { get; set; }
        public float intensity { get; set; }
        public float pointLightOuterRadius { get; set; }
    }
}