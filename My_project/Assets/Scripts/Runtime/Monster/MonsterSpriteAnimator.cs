using UnityEngine;
using System.Collections;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 몬스터 스프라이트 애니메이션 상태
    /// </summary>
    public enum MonsterAnimationState
    {
        Idle,
        Move,
        Attack,
        Casting
    }
    
    /// <summary>
    /// 몬스터 스프라이트 애니메이션 컨트롤러
    /// </summary>
    public class MonsterSpriteAnimator : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private MonsterVariantData currentVariantData;
        private MonsterAnimationState currentState = MonsterAnimationState.Idle;
        
        // 애니메이션 데이터
        private Sprite[] idleSprites;
        private Sprite[] moveSprites;
        private Sprite[] attackSprites;
        private Sprite[] castingSprites;
        
        private float idleFrameRate = 6f;
        private float moveFrameRate = 8f;
        private float attackFrameRate = 12f;
        private float castingFrameRate = 10f;
        
        // 애니메이션 상태
        private Coroutine currentAnimationCoroutine;
        private int currentFrameIndex = 0;
        private bool isPlaying = false;
        
        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }
        
        /// <summary>
        /// 몬스터 애니메이션 설정
        /// </summary>
        public void SetupAnimations(MonsterVariantData variantData)
        {
            currentVariantData = variantData;
            
            if (variantData == null)
            {
                Debug.LogWarning("MonsterSpriteAnimator: variantData is null!");
                return;
            }
            
            // 스프라이트 배열 설정
            idleSprites = variantData.IdleSprites;
            moveSprites = variantData.MoveSprites;
            attackSprites = variantData.AttackSprites;
            castingSprites = variantData.CastingSprites;
            
            // 프레임 레이트 설정
            idleFrameRate = variantData.IdleFrameRate;
            moveFrameRate = variantData.MoveFrameRate;
            attackFrameRate = variantData.AttackFrameRate;
            castingFrameRate = variantData.CastingFrameRate;
            
            // 기본적으로 Idle 애니메이션 시작
            PlayAnimation(MonsterAnimationState.Idle);
        }
        
        /// <summary>
        /// 애니메이션 상태 변경
        /// </summary>
        public void PlayAnimation(MonsterAnimationState state)
        {
            if (currentState == state && isPlaying) return; // 같은 상태면 무시
            
            currentState = state;
            
            // 현재 애니메이션 중지
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);
            }
            
            // 새 애니메이션 시작
            currentAnimationCoroutine = StartCoroutine(AnimationCoroutine(state));
        }
        
        /// <summary>
        /// 공격 애니메이션 재생 (한 번만)
        /// </summary>
        public void PlayAttackAnimation(System.Action onComplete = null)
        {
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);
            }
            
            currentAnimationCoroutine = StartCoroutine(PlayOneShotAnimation(MonsterAnimationState.Attack, onComplete));
        }
        
        /// <summary>
        /// 캐스팅 애니메이션 재생 (한 번만)
        /// </summary>
        public void PlayCastingAnimation(System.Action onComplete = null)
        {
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);
            }
            
            currentAnimationCoroutine = StartCoroutine(PlayOneShotAnimation(MonsterAnimationState.Casting, onComplete));
        }
        
        /// <summary>
        /// 모든 애니메이션 중지
        /// </summary>
        public void StopAllAnimations()
        {
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);
                currentAnimationCoroutine = null;
            }
            
            isPlaying = false;
            currentFrameIndex = 0;
            currentState = MonsterAnimationState.Idle;
        }
        
        /// <summary>
        /// 애니메이션 루프 코루틴
        /// </summary>
        private IEnumerator AnimationCoroutine(MonsterAnimationState state)
        {
            var sprites = GetSpritesForState(state);
            var frameRate = GetFrameRateForState(state);
            
            if (sprites == null || sprites.Length == 0)
            {
                Debug.LogWarning($"MonsterSpriteAnimator: No sprites for state {state}");
                yield break;
            }
            
            isPlaying = true;
            currentFrameIndex = 0;
            
            float frameTime = 1f / frameRate;
            
            while (isPlaying)
            {
                // 현재 프레임 표시
                spriteRenderer.sprite = sprites[currentFrameIndex];
                
                // 다음 프레임으로
                currentFrameIndex = (currentFrameIndex + 1) % sprites.Length;
                
                yield return new WaitForSeconds(frameTime);
            }
        }
        
        /// <summary>
        /// 한 번만 재생하는 애니메이션 코루틴
        /// </summary>
        private IEnumerator PlayOneShotAnimation(MonsterAnimationState state, System.Action onComplete = null)
        {
            var sprites = GetSpritesForState(state);
            var frameRate = GetFrameRateForState(state);
            
            if (sprites == null || sprites.Length == 0)
            {
                Debug.LogWarning($"MonsterSpriteAnimator: No sprites for state {state}");
                onComplete?.Invoke();
                // Idle로 복귀
                PlayAnimation(MonsterAnimationState.Idle);
                yield break;
            }
            
            isPlaying = true;
            float frameTime = 1f / frameRate;
            
            // 한 번만 재생
            for (int i = 0; i < sprites.Length; i++)
            {
                spriteRenderer.sprite = sprites[i];
                yield return new WaitForSeconds(frameTime);
            }
            
            onComplete?.Invoke();
            
            // Idle 상태로 복귀
            PlayAnimation(MonsterAnimationState.Idle);
        }
        
        /// <summary>
        /// 상태에 따른 스프라이트 배열 반환
        /// </summary>
        private Sprite[] GetSpritesForState(MonsterAnimationState state)
        {
            return state switch
            {
                MonsterAnimationState.Idle => idleSprites,
                MonsterAnimationState.Move => moveSprites,
                MonsterAnimationState.Attack => attackSprites,
                MonsterAnimationState.Casting => castingSprites,
                _ => idleSprites
            };
        }
        
        /// <summary>
        /// 상태에 따른 프레임 레이트 반환
        /// </summary>
        private float GetFrameRateForState(MonsterAnimationState state)
        {
            return state switch
            {
                MonsterAnimationState.Idle => idleFrameRate,
                MonsterAnimationState.Move => moveFrameRate,
                MonsterAnimationState.Attack => attackFrameRate,
                MonsterAnimationState.Casting => castingFrameRate,
                _ => idleFrameRate
            };
        }
        
        /// <summary>
        /// 현재 애니메이션 상태 반환
        /// </summary>
        public MonsterAnimationState GetCurrentState()
        {
            return currentState;
        }
        
        /// <summary>
        /// 애니메이션이 재생 중인지 확인
        /// </summary>
        public bool IsPlaying()
        {
            return isPlaying;
        }
    }
}