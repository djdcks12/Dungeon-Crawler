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
        Hit,
        Death,
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
        private Sprite[] hitSprites;
        private Sprite[] deathSprites;
        
        private float idleFrameRate = 6f;
        private float moveFrameRate = 8f;
        private float attackFrameRate = 12f;
        private float castingFrameRate = 10f;
        private float hitFrameRate = 10f;
        private float deathFrameRate = 6f;
        
        // 애니메이션 상태
        private Coroutine currentAnimationCoroutine;
        private int currentFrameIndex = 0;
        private bool isPlaying = false;
        
        // 외부에서 접근 가능한 프로퍼티
        public MonsterAnimationState CurrentState => currentState;
        public bool IsPlaying => isPlaying;
        
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
            hitSprites = variantData.HitSprites;
            deathSprites = variantData.DeathSprites;

            // 프레임 레이트 설정
            idleFrameRate = variantData.IdleFrameRate;
            moveFrameRate = variantData.MoveFrameRate;
            attackFrameRate = variantData.AttackFrameRate;
            castingFrameRate = variantData.CastingFrameRate;
            hitFrameRate = variantData.HitFrameRate;
            deathFrameRate = variantData.DeathFrameRate;

            // 기본적으로 Idle 애니메이션 시작
            PlayAnimation(MonsterAnimationState.Idle);
        }
        
        /// <summary>
        /// 애니메이션 상태 변경 (Hit 애니메이션 우선순위 보장)
        /// </summary>
        public void PlayAnimation(MonsterAnimationState state)
        {
            // Hit 애니메이션이 재생 중이면 다른 애니메이션은 무시 (Death 제외)
            if (currentState == MonsterAnimationState.Hit && isPlaying && state != MonsterAnimationState.Death)
            {
                return;
            }
            
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
        /// Hit 애니메이션 전용 (강제 재생)
        /// </summary>
        public void PlayHitAnimation(System.Action onComplete = null)
        {
            // Hit 애니메이션은 항상 우선 재생
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);
            }
            
            currentState = MonsterAnimationState.Hit;
            currentAnimationCoroutine = StartCoroutine(PlayOneShotAnimation(MonsterAnimationState.Hit, () => {
                // Hit 애니메이션 완료 후 상태를 명시적으로 Idle로 설정
                currentState = MonsterAnimationState.Idle;
                onComplete?.Invoke();
            }));
        }
        
        /// <summary>
        /// 공격 애니메이션 재생 (한 번만)
        /// </summary>
        public void PlayAttackAnimation(System.Action onComplete = null, System.Action onDamageFrame = null)
        {
            if(currentState == MonsterAnimationState.Hit) return; // 피격 애니메이션 중에는 공격 애니메이션 재생 안 함
            
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);
            }
            
            currentAnimationCoroutine = StartCoroutine(PlayOneShotAnimationWithDamage(MonsterAnimationState.Attack, onComplete,onDamageFrame));
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
        

        public void PlayDeathAnimation(System.Action onComplete = null)
        {
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);
            }
            
            currentAnimationCoroutine = StartCoroutine(PlayOneShotAnimation(MonsterAnimationState.Death, onComplete));
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
                if (state != MonsterAnimationState.Death)
                {
                    PlayAnimation(MonsterAnimationState.Idle);
                }
                yield break;
            }
            
            isPlaying = true;
            currentState = state; // 현재 상태 설정
            float frameTime = 1f / frameRate;
            
            // 한 번만 재생
            for (int i = 0; i < sprites.Length; i++)
            {
                spriteRenderer.sprite = sprites[i];
                yield return new WaitForSeconds(frameTime);
            }
            
            // 애니메이션 완료 처리
            isPlaying = false;
            currentAnimationCoroutine = null;
            
            // onComplete 콜백 실행 (여기서 상태가 변경될 수 있음)
            onComplete?.Invoke();

            // onComplete에서 상태가 변경되지 않았으면 Idle로 복귀
            if (currentState == state && state != MonsterAnimationState.Death)
            {
                PlayAnimation(MonsterAnimationState.Idle);
            }
            else if (state == MonsterAnimationState.Death)
            {
                // 사망 애니메이션은 마지막 프레임에서 정지 (isPlaying은 이미 false)
                currentState = MonsterAnimationState.Death;
            }
        }

        /// <summary>
        /// 데미지 프레임 콜백이 있는 한 번만 재생하는 애니메이션 코루틴
        /// </summary>
        private IEnumerator PlayOneShotAnimationWithDamage(MonsterAnimationState state, System.Action onComplete = null, System.Action onDamageFrame = null)
        {
            var sprites = GetSpritesForState(state);
            var frameRate = GetFrameRateForState(state);
            
            if (sprites == null || sprites.Length == 0)
            {
                Debug.LogWarning($"PlayerSpriteAnimator: No sprites for state {state}");
                onComplete?.Invoke();
                
                // 사망 애니메이션이 아니면 Idle로 복귀
                if (state != MonsterAnimationState.Death)
                {
                    PlayAnimation(MonsterAnimationState.Idle);
                }
                yield break;
            }
            
            isPlaying = true;
            currentState = state; // 현재 상태 설정
            float frameTime = 1f / frameRate;
            
            // 공격 속도에 따라 애니메이션 속도 조정

            // 데미지 적용 프레임 가져오기 (공격 애니메이션일 때만)
            int damageFrame = -1;
            if (state == MonsterAnimationState.Attack && currentVariantData != null)
            {
                damageFrame = currentVariantData.AttackDamageFrame;
            }
            
            // 한 번만 재생
            for (int i = 0; i < sprites.Length; i++)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = sprites[i];
                }
                
                // 데미지 프레임에 도달하면 콜백 호출
                if (i == damageFrame && onDamageFrame != null)
                {
                    onDamageFrame.Invoke();
                }
                
                yield return new WaitForSeconds(frameTime);
            }
            
            // 애니메이션 완료 처리
            isPlaying = false;
            currentAnimationCoroutine = null;
            
            // onComplete 콜백 실행
            onComplete?.Invoke();
            
            // onComplete에서 상태가 변경되지 않았으면 Idle로 복귀
            if (currentState == state && state != MonsterAnimationState.Death)
            {
                PlayAnimation(MonsterAnimationState.Idle);
            }
            else if (state == MonsterAnimationState.Death)
            {
                // 사망 애니메이션은 마지막 프레임에서 정지 (isPlaying은 이미 false)
                currentState = MonsterAnimationState.Death;
            }
        }
        
        /// <summary>
        /// 상태에 따른 스프라이트 배열 반환
        /// </summary>
        private Sprite[] GetSpritesForState(MonsterAnimationState state)
        {
            return state switch
            {
                MonsterAnimationState.Idle => idleSprites,
                MonsterAnimationState.Hit => hitSprites,
                MonsterAnimationState.Death => deathSprites,
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
                MonsterAnimationState.Hit => hitFrameRate,
                MonsterAnimationState.Death => deathFrameRate,
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
        
    }
}