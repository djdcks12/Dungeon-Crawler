using UnityEngine;
using System.Collections;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 간단한 스프라이트 시퀀스 애니메이션을 재생하는 컴포넌트
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SimpleEffectAnimator : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Coroutine animationCoroutine;
        
        [Header("Animation Settings")]
        [SerializeField] private float frameRate = 12f; // FPS
        [SerializeField] private bool loop = false;
        [SerializeField] private bool destroyOnComplete = true;
        
        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        /// <summary>
        /// 텍스처 배열을 사용해서 애니메이션 재생
        /// </summary>
        public void PlayAnimation(Texture2D[] frames, float? customFrameRate = null, bool? customLoop = null, bool? customDestroyOnComplete = null)
        {
            if (frames == null || frames.Length == 0) return;
            
            // 파라미터 설정
            float finalFrameRate = customFrameRate ?? frameRate;
            bool finalLoop = customLoop ?? loop;
            bool finalDestroy = customDestroyOnComplete ?? destroyOnComplete;
            
            // 기존 애니메이션 중지
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }
            
            // 새 애니메이션 시작
            animationCoroutine = StartCoroutine(AnimationCoroutine(frames, finalFrameRate, finalLoop, finalDestroy));
        }
        
        /// <summary>
        /// 스프라이트 배열을 사용해서 애니메이션 재생
        /// </summary>
        public void PlayAnimation(Sprite[] sprites, float? customFrameRate = null, bool? customLoop = null, bool? customDestroyOnComplete = null)
        {
            if (sprites == null || sprites.Length == 0) return;
            
            // 파라미터 설정
            float finalFrameRate = customFrameRate ?? frameRate;
            bool finalLoop = customLoop ?? loop;
            bool finalDestroy = customDestroyOnComplete ?? destroyOnComplete;
            
            // 기존 애니메이션 중지
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }
            
            // 새 애니메이션 시작
            animationCoroutine = StartCoroutine(SpriteAnimationCoroutine(sprites, finalFrameRate, finalLoop, finalDestroy));
        }
        
        private IEnumerator AnimationCoroutine(Texture2D[] frames, float frameRate, bool loop, bool destroyOnComplete)
        {
            if (frameRate <= 0f) frameRate = 12f;
            float frameTime = 1f / frameRate;
            
            do
            {
                foreach (var frame in frames)
                {
                    if (frame != null)
                    {
                        // Texture2D를 Sprite로 변환
                        var sprite = Sprite.Create(frame, new Rect(0, 0, frame.width, frame.height), Vector2.one * 0.5f, 100f);
                        spriteRenderer.sprite = sprite;
                    }
                    
                    yield return new WaitForSeconds(frameTime);
                }
            }
            while (loop);
            
            if (destroyOnComplete)
            {
                Destroy(gameObject);
            }
        }
        
        private IEnumerator SpriteAnimationCoroutine(Sprite[] sprites, float frameRate, bool loop, bool destroyOnComplete)
        {
            if (frameRate <= 0f) frameRate = 12f;
            float frameTime = 1f / frameRate;
            
            do
            {
                foreach (var sprite in sprites)
                {
                    if (sprite != null)
                    {
                        spriteRenderer.sprite = sprite;
                    }
                    
                    yield return new WaitForSeconds(frameTime);
                }
            }
            while (loop);
            
            if (destroyOnComplete)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 애니메이션 중지
        /// </summary>
        public void StopAnimation()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }
        }
        
        private void OnDestroy()
        {
            StopAllCoroutines();
            StopAnimation();
        }
    }
}