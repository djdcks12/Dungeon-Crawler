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
        Hit,
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
        private RaceData currentRaceData; // 하위 호환성용 (기존 RaceData도 지원)
        private RaceWeaponGroupData currentCombinationData; // 새로운 종족-무기군 데이터
        private PlayerAnimationState currentState = PlayerAnimationState.Idle;
        
        // 현재 종족-무기군 상태
        private Race currentRace = Race.Human;
        private WeaponGroup currentWeaponGroup = WeaponGroup.Fist;
        
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
        /// 종족-무기군 조합으로 애니메이션 설정 (새로운 방식)
        /// </summary>
        public void SetupAnimations(Race race, WeaponGroup weaponGroup)
        {
            currentRace = race;
            currentWeaponGroup = weaponGroup;
            
            // RaceWeaponGroupManager에서 조합 데이터 로드
            currentCombinationData = RaceWeaponGroupManager.GetCombinationData(race, weaponGroup);
            currentRaceData = null; // 새로운 방식 사용 시 기존 데이터는 null
            
            if (currentCombinationData == null)
            {
                Debug.LogWarning($"PlayerSpriteAnimator: No combination data found for {race}_{weaponGroup}");
                return;
            }
            
            // 기본 스프라이트 설정
            if (currentCombinationData.HasValidIdleAnimation)
            {
                spriteRenderer.sprite = currentCombinationData.GetDefaultSprite();
            }
            
            // 기본적으로 Idle 애니메이션 시작
            PlayAnimation(PlayerAnimationState.Idle);
            
            Debug.Log($"🎭 PlayerSpriteAnimator setup: {race}_{weaponGroup}");
        }
        
        /// <summary>
        /// RaceData로 애니메이션 설정 (하위 호환성용)
        /// </summary>
        public void SetupAnimations(RaceData raceData)
        {
            currentRaceData = raceData;
            currentCombinationData = null; // 기존 방식 사용 시 새 데이터는 null
            
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
            
            Debug.Log($"🎭 PlayerSpriteAnimator setup (legacy): RaceData");
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
        public void PlayAttackAnimation(System.Action onComplete = null, float speedMultiplier = 1.0f, System.Action onDamageFrame = null)
        {
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);
            }

            currentAnimationCoroutine = StartCoroutine(PlayOneShotAnimationWithDamage(PlayerAnimationState.Attack, onComplete, speedMultiplier, onDamageFrame));
        }
        
        /// <summary>
        /// 캐스팅 애니메이션 재생 (한 번만)
        /// </summary>
        public void PlayCastingAnimation(System.Action onComplete = null, float speedMultiplier = 1.0f)
        {
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);
            }
            
            currentAnimationCoroutine = StartCoroutine(PlayOneShotAnimation(PlayerAnimationState.Casting, onComplete, speedMultiplier));
        }
        
        /// <summary>
        /// 사망 애니메이션 재생 (한 번만, 루프 없음)
        /// </summary>
        public void PlayDeathAnimation(System.Action onComplete = null, float speedMultiplier = 1.0f)
        {
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);
            }
            
            currentAnimationCoroutine = StartCoroutine(PlayOneShotAnimation(PlayerAnimationState.Death, onComplete, speedMultiplier));
        }
        
        /// <summary>
        /// 피격 애니메이션 재생 (한 번만)
        /// </summary>
        public void PlayHitAnimation(System.Action onComplete = null, float speedMultiplier = 1.0f)
        {
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);
            }

            currentAnimationCoroutine = StartCoroutine(PlayOneShotAnimation(PlayerAnimationState.Hit, onComplete, speedMultiplier));
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
        private IEnumerator PlayOneShotAnimation(PlayerAnimationState state, System.Action onComplete = null, float speedMultiplier = 1.0f)
        {
            var sprites = GetSpritesForState(state);
            var frameRate = GetFrameRateForState(state);
            
            if (sprites == null || sprites.Length == 0)
            {
                Debug.LogWarning($"🎬 PlayerSpriteAnimator: No sprites for state {state}");
                onComplete?.Invoke();
                
                // 사망 애니메이션이 아니면 Idle로 복귀
                if (state != PlayerAnimationState.Death)
                {
                    PlayAnimation(PlayerAnimationState.Idle);
                }
                yield break;
            }
            
            isPlaying = true;
            currentState = state; // 현재 상태 설정
            
            // 공격 속도에 따라 애니메이션 속도 조정
            float adjustedFrameRate = frameRate * speedMultiplier;
            float frameTime = 1f / adjustedFrameRate;
            
            // 한 번만 재생
            for (int i = 0; i < sprites.Length; i++)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = sprites[i];
                }
                
                yield return new WaitForSeconds(frameTime);
            }
            
            onComplete?.Invoke();
            
            // 사망 애니메이션이 아니면 Idle 상태로 복귀
            if (currentState != PlayerAnimationState.Death)
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
        /// 데미지 프레임 콜백이 있는 한 번만 재생하는 애니메이션 코루틴
        /// </summary>
        private IEnumerator PlayOneShotAnimationWithDamage(PlayerAnimationState state, System.Action onComplete = null, float speedMultiplier = 1.0f, System.Action onDamageFrame = null)
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
            currentState = state; // 현재 상태 설정
            
            // 공격 속도에 따라 애니메이션 속도 조정
            float adjustedFrameRate = frameRate * speedMultiplier;
            float frameTime = 1f / adjustedFrameRate;
            
            // 데미지 적용 프레임 가져오기 (공격 애니메이션일 때만)
            int damageFrame = -1;
            if (state == PlayerAnimationState.Attack)
            {
                if (currentCombinationData != null)
                {
                    damageFrame = currentCombinationData.AttackDamageFrame;
                }
                else if (currentRaceData != null)
                {
                    damageFrame = currentRaceData.AttackDamageFrame;
                }
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
        /// 상태에 따른 스프라이트 배열 반환 (새로운 조합 데이터 우선)
        /// </summary>
        private Sprite[] GetSpritesForState(PlayerAnimationState state)
        {
            // 새로운 종족-무기군 데이터 우선 사용
            if (currentCombinationData != null)
            {
                return state switch
                {
                    PlayerAnimationState.Idle => currentCombinationData.IdleSprites,
                    PlayerAnimationState.Walk => currentCombinationData.WalkSprites,
                    PlayerAnimationState.Hit => currentCombinationData.HitSprites,
                    PlayerAnimationState.Attack => currentCombinationData.AttackSprites,
                    PlayerAnimationState.Casting => currentCombinationData.CastingSprites,
                    PlayerAnimationState.Death => currentCombinationData.DeathSprites,
                    _ => currentCombinationData.IdleSprites
                };
            }
            
            // 하위 호환성: 기존 RaceData 사용
            if (currentRaceData != null)
            {
                return state switch
                {
                    PlayerAnimationState.Idle => currentRaceData.IdleSprites,
                    PlayerAnimationState.Walk => currentRaceData.WalkSprites,
                    PlayerAnimationState.Hit => currentRaceData.HitSprites,
                    PlayerAnimationState.Attack => currentRaceData.AttackSprites,
                    PlayerAnimationState.Casting => currentRaceData.CastingSprites,
                    PlayerAnimationState.Death => currentRaceData.DeathSprites,
                    _ => currentRaceData.IdleSprites
                };
            }
            
            return null;
        }
        
        /// <summary>
        /// 상태에 따른 프레임 레이트 반환 (새로운 조합 데이터 우선)
        /// </summary>
        private float GetFrameRateForState(PlayerAnimationState state)
        {
            // 새로운 종족-무기군 데이터 우선 사용
            if (currentCombinationData != null)
            {
                return state switch
                {
                    PlayerAnimationState.Idle => currentCombinationData.IdleFrameRate,
                    PlayerAnimationState.Walk => currentCombinationData.WalkFrameRate,
                    PlayerAnimationState.Hit => currentCombinationData.HitFrameRate,
                    PlayerAnimationState.Attack => currentCombinationData.AttackFrameRate,
                    PlayerAnimationState.Casting => currentCombinationData.CastingFrameRate,
                    PlayerAnimationState.Death => currentCombinationData.DeathFrameRate,
                    _ => currentCombinationData.IdleFrameRate
                };
            }
            
            // 하위 호환성: 기존 RaceData 사용
            if (currentRaceData != null)
            {
                return state switch
                {
                    PlayerAnimationState.Idle => currentRaceData.IdleFrameRate,
                    PlayerAnimationState.Walk => currentRaceData.WalkFrameRate,
                    PlayerAnimationState.Hit => currentRaceData.HitFrameRate,
                    PlayerAnimationState.Attack => currentRaceData.AttackFrameRate,
                    PlayerAnimationState.Casting => currentRaceData.CastingFrameRate,
                    PlayerAnimationState.Death => currentRaceData.DeathFrameRate,
                    _ => currentRaceData.IdleFrameRate
                };
            }
            
            return 6f; // 기본값
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
        /// 공격 애니메이션이 재생 중인지 확인
        /// </summary>
        public bool IsAttackAnimationPlaying()
        {
            return isPlaying && currentState == PlayerAnimationState.Attack;
        }

        /// <summary>
        /// 사망 애니메이션이 재생 중인지 확인
        /// </summary>
        public bool IsMovingOrIdleAnimationPlaying()
        {
            return isPlaying && (currentState == PlayerAnimationState.Idle || currentState == PlayerAnimationState.Walk);
        }
        /// <summary>
        /// 무기군 변경 (장비 변경 시 사용)
        /// </summary>
        public void ChangeWeaponGroup(WeaponGroup newWeaponGroup)
        {
            if (currentWeaponGroup != newWeaponGroup)
            {
                StopAllAnimations();
                SetupAnimations(currentRace, newWeaponGroup);
            }
        }
        
        /// <summary>
        /// RaceData 변경 (종족 변경 시 사용 - 하위 호환성)
        /// </summary>
        public void ChangeRaceData(RaceData newRaceData)
        {
            StopAllAnimations();
            SetupAnimations(newRaceData);
        }
        
        /// <summary>
        /// 현재 종족-무기군 조합 반환
        /// </summary>
        public (Race race, WeaponGroup weaponGroup) GetCurrentCombination()
        {
            return (currentRace, currentWeaponGroup);
        }
        
        /// <summary>
        /// 현재 사용 중인 데이터 타입 확인
        /// </summary>
        public bool IsUsingCombinationData()
        {
            return currentCombinationData != null;
        }
        
    }
}