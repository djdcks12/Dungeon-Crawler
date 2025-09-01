using UnityEngine;
using System.Collections;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 플레이어 스프라이트 애니메이션 상태
    /// </summary>
    public enum PlayerAnimationState
    {
        Idle,
        Walk,
        Attack,
        Casting,
        Death
    }
    
    /// <summary>
    /// 플레이어 스프라이트 애니메이션 컨트롤러
    /// RaceData에서 스프라이트 배열을 받아서 애니메이션 처리
    /// </summary>
    public class PlayerSpriteAnimator : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private RaceData currentRaceData;
        private PlayerAnimationState currentState = PlayerAnimationState.Idle;
        
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
            
            // 플레이어 레이어 설정
            spriteRenderer.sortingLayerName = "PlayerOrMonster";
            spriteRenderer.sortingOrder = 1;
        }
        
        /// <summary>
        /// RaceData로 애니메이션 설정
        /// </summary>
        public void SetupAnimations(RaceData raceData)
        {
            currentRaceData = raceData;
            
            if (raceData == null)
            {
                Debug.LogWarning("PlayerSpriteAnimator: raceData is null!");
                return;
            }
            
            // 기본 스프라이트 설정
            if (raceData.HasValidIdleAnimation)
            {
                spriteRenderer.sprite = raceData.GetDefaultSprite();
            }
            
            // 기본적으로 Idle 애니메이션 시작
            PlayAnimation(PlayerAnimationState.Idle);
        }
        
        /// <summary>
        /// 애니메이션 상태 변경
        /// </summary>
        public void PlayAnimation(PlayerAnimationState state)
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
            
            currentAnimationCoroutine = StartCoroutine(PlayOneShotAnimation(PlayerAnimationState.Attack, onComplete));
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
            
            currentAnimationCoroutine = StartCoroutine(PlayOneShotAnimation(PlayerAnimationState.Casting, onComplete));
        }
        
        /// <summary>
        /// 사망 애니메이션 재생 (한 번만, 루프 없음)
        /// </summary>
        public void PlayDeathAnimation(System.Action onComplete = null)
        {
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);
            }
            
            currentAnimationCoroutine = StartCoroutine(PlayOneShotAnimation(PlayerAnimationState.Death, onComplete));
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
            currentState = PlayerAnimationState.Idle;
        }
        
        /// <summary>
        /// 애니메이션 루프 코루틴
        /// </summary>
        private IEnumerator AnimationCoroutine(PlayerAnimationState state)
        {
            var sprites = GetSpritesForState(state);
            var frameRate = GetFrameRateForState(state);
            
            if (sprites == null || sprites.Length == 0)
            {
                Debug.LogWarning($"PlayerSpriteAnimator: No sprites for state {state}");
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
        private IEnumerator PlayOneShotAnimation(PlayerAnimationState state, System.Action onComplete = null)
        {
            var sprites = GetSpritesForState(state);
            var frameRate = GetFrameRateForState(state);
            
            if (sprites == null || sprites.Length == 0)
            {
                Debug.LogWarning($"PlayerSpriteAnimator: No sprites for state {state}");
                onComplete?.Invoke();
                
                // 사망 애니메이션이 아니면 Idle로 복귀
                if (state != PlayerAnimationState.Death)
                {
                    PlayAnimation(PlayerAnimationState.Idle);
                }
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
            
            // 사망 애니메이션이 아니면 Idle 상태로 복귀
            if (state != PlayerAnimationState.Death)
            {
                PlayAnimation(PlayerAnimationState.Idle);
            }
            else
            {
                // 사망 애니메이션은 마지막 프레임에서 정지
                isPlaying = false;
            }
        }
        
        /// <summary>
        /// 상태에 따른 스프라이트 배열 반환
        /// </summary>
        private Sprite[] GetSpritesForState(PlayerAnimationState state)
        {
            if (currentRaceData == null) return null;
            
            return state switch
            {
                PlayerAnimationState.Idle => currentRaceData.IdleSprites,
                PlayerAnimationState.Walk => currentRaceData.WalkSprites,
                PlayerAnimationState.Attack => currentRaceData.AttackSprites,
                PlayerAnimationState.Casting => currentRaceData.CastingSprites,
                PlayerAnimationState.Death => currentRaceData.DeathSprites,
                _ => currentRaceData.IdleSprites
            };
        }
        
        /// <summary>
        /// 상태에 따른 프레임 레이트 반환
        /// </summary>
        private float GetFrameRateForState(PlayerAnimationState state)
        {
            if (currentRaceData == null) return 6f;
            
            return state switch
            {
                PlayerAnimationState.Idle => currentRaceData.IdleFrameRate,
                PlayerAnimationState.Walk => currentRaceData.WalkFrameRate,
                PlayerAnimationState.Attack => currentRaceData.AttackFrameRate,
                PlayerAnimationState.Casting => currentRaceData.CastingFrameRate,
                PlayerAnimationState.Death => currentRaceData.DeathFrameRate,
                _ => currentRaceData.IdleFrameRate
            };
        }
        
        /// <summary>
        /// 현재 애니메이션 상태 반환
        /// </summary>
        public PlayerAnimationState GetCurrentState()
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
        
        /// <summary>
        /// RaceData 변경 (종족 변경 시 사용)
        /// </summary>
        public void ChangeRaceData(RaceData newRaceData)
        {
            StopAllAnimations();
            SetupAnimations(newRaceData);
        }
        
        /// <summary>
        /// 피격 이펙트 재생
        /// </summary>
        public void PlayHitEffect()
        {
            if (spriteRenderer != null)
            {
                StartCoroutine(HitFlashCoroutine());
            }
        }
        
        /// <summary>
        /// 피격 플래시 효과
        /// </summary>
        private System.Collections.IEnumerator HitFlashCoroutine()
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            
            yield return new WaitForSeconds(0.1f);
            
            spriteRenderer.color = originalColor;
        }
    }
}