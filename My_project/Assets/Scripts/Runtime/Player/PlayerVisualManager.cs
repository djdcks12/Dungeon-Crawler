using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 플레이어 비주얼 매니저 - 종족-무기군 조합별 애니메이션 관리
    /// PlayerSpriteAnimator를 통해 실제 애니메이션 처리
    /// </summary>
    public class PlayerVisualManager : NetworkBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer characterRenderer;
        [SerializeField] private Animator characterAnimator;

        [Header("Visual Settings")]
        [SerializeField] private bool autoSetupOnSpawn = true;
        [SerializeField] private float characterScale = 1f;
        [SerializeField] private Color characterTint = Color.white;
        
        // 네트워크 동기화된 비주얼 상태
        private NetworkVariable<Race> networkRace = new NetworkVariable<Race>(Race.Human,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);
        private NetworkVariable<int> networkDirection = new NetworkVariable<int>(0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);
        private NetworkVariable<PlayerAnimationState> networkAnimationState = new NetworkVariable<PlayerAnimationState>(PlayerAnimationState.Idle,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);
            
        // 현재 상태
        private Race currentRace = Race.Human;
        private WeaponGroup currentWeaponGroup = WeaponGroup.Fist;
        private PlayerAnimationState currentAnimationState = PlayerAnimationState.Idle;
        
        // 컴포넌트 참조
        private PlayerController playerController;
        private PlayerStatsManager statsManager;
        private PlayerSpriteAnimator spriteAnimator;
        private EquipmentManager equipmentManager;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // 컴포넌트 초기화
            SetupComponents();
            
            // 네트워크 변수 이벤트 구독
            networkRace.OnValueChanged += OnRaceChanged;
            networkDirection.OnValueChanged += OnDirectionChanged;
            networkAnimationState.OnValueChanged += OnAnimationStateChanged;
            
            // 초기 비주얼 설정
            if (autoSetupOnSpawn)
            {
                SetupInitialVisuals();
            }
        }
        
        public override void OnNetworkDespawn()
        {
            // 이벤트 구독 해제
            networkRace.OnValueChanged -= OnRaceChanged;
            networkDirection.OnValueChanged -= OnDirectionChanged;
            networkAnimationState.OnValueChanged -= OnAnimationStateChanged;
            
            if (equipmentManager != null)
            {
                equipmentManager.OnEquipmentChanged -= OnEquipmentChanged;
            }
            
            base.OnNetworkDespawn();
        }
        
        /// <summary>
        /// 컴포넌트 설정
        /// </summary>
        private void SetupComponents()
        {
            playerController = GetComponent<PlayerController>();
            statsManager = GetComponent<PlayerStatsManager>();
            equipmentManager = GetComponent<EquipmentManager>();
            
            // PlayerSpriteAnimator 가져오기 (실제 애니메이션 처리를 담당)
            spriteAnimator = GetComponent<PlayerSpriteAnimator>();
            if (spriteAnimator == null)
            {
                spriteAnimator = gameObject.AddComponent<PlayerSpriteAnimator>();
            }
            
            // 장비 변경 이벤트 구독
            if (equipmentManager != null)
            {
                equipmentManager.OnEquipmentChanged += OnEquipmentChanged;
            }
            
            // characterRenderer는 더 이상 사용하지 않음 (PlayerSpriteAnimator가 처리)
            Debug.Log("🎭 PlayerVisualManager: Using PlayerSpriteAnimator for rendering");
        }
        
        /// <summary>
        /// 초기 비주얼 설정
        /// </summary>
        private void SetupInitialVisuals()
        {
            // 스탯 매니저에서 종족 정보 가져오기
            if (statsManager?.CurrentStats != null)
            {
                currentRace = statsManager.CurrentStats.CharacterRace;
            }
            else
            {
                currentRace = Race.Human;
            }
            
            // 장비 매니저에서 현재 무기군 정보 가져오기
            currentWeaponGroup = GetCurrentWeaponGroup();
            
            // PlayerSpriteAnimator에 종족-무기군 설정
            if (spriteAnimator != null)
            {
                spriteAnimator.SetupAnimations(currentRace, currentWeaponGroup);
            }
            
            Debug.Log($"🎭 Initial visuals setup: {currentRace}_{currentWeaponGroup}");
        }
        
        /// <summary>
        /// 종족 설정
        /// </summary>
        public void SetRace(Race race)
        {
            if (currentRace != race)
            {
                currentRace = race;
                
                if (IsOwner)
                {
                    networkRace.Value = race;
                }
                
                // 종족 변경 시 PlayerSpriteAnimator 업데이트
                if (spriteAnimator != null)
                {
                    spriteAnimator.SetupAnimations(currentRace, currentWeaponGroup);
                }
            }
        }

        /// <summary>
        /// 애니메이션 상태 설정 - PlayerSpriteAnimator로 위임
        /// </summary>
        public void SetAnimation(PlayerAnimationState animationState)
        {
            currentAnimationState = animationState;
            
            if (IsOwner)
            {
                networkAnimationState.Value = animationState;
            }
            
            // PlayerSpriteAnimator로 애니메이션 전환
            if (spriteAnimator != null)
            {
                spriteAnimator.PlayAnimation(animationState);
            }
        }
        
        /// <summary>
        /// 방향 설정 (사용 안함 - 레거시)
        /// </summary>
        public void SetDirection(Vector2 moveDirection)
        {
            // 더 이상 사용하지 않음 - 종족-무기군 시스템에서는 방향 개념 없음
        }
        
        /// <summary>
        /// 마우스 방향 설정 (사용 안함 - 레거시)
        /// </summary>
        public void SetDirectionFromMouse(Vector2 mouseDirection)
        {
            // 더 이상 사용하지 않음 - 종족-무기군 시스템에서는 방향 개념 없음
        }
        
        /// <summary>
        /// 캐릭터 스프라이트 업데이트 - 레거시 코드 제거됨
        /// 실제 애니메이션은 PlayerSpriteAnimator가 처리
        /// </summary>
        private void UpdateCharacterSprite()
        {
            // 더 이상 사용되지 않음 - PlayerSpriteAnimator가 직접 RaceData로 처리
            Debug.Log($"PlayerVisualManager.UpdateCharacterSprite() deprecated - use PlayerSpriteAnimator instead");
        }

        /// <summary>
        /// 공격 애니메이션 트리거 - PlayerSpriteAnimator로 위임
        /// </summary>
        public void TriggerAttackAnimation()
        {
            if (spriteAnimator != null)
            {
                spriteAnimator.PlayAttackAnimation(() => {
                    // 공격 완료 후 Idle로 복귀
                    currentAnimationState = PlayerAnimationState.Idle;
                });
            }
        }
        
        /// <summary>
        /// 피격 이펙트 - PlayerSpriteAnimator로 위임
        /// </summary>
        public void PlayHitEffect()
        {
            if (spriteAnimator != null)
            {
                spriteAnimator.PlayHitAnimation(() => {
                    // 피격 완료 후 Idle로 복귀
                    currentAnimationState = PlayerAnimationState.Idle;
                });
            }
        }
        
        /// <summary>
        /// 네트워크 이벤트 - 종족 변경
        /// </summary>
        private void OnRaceChanged(Race previousValue, Race newValue)
        {
            currentRace = newValue;
            if (spriteAnimator != null)
            {
                spriteAnimator.SetupAnimations(currentRace, currentWeaponGroup);
            }
        }
        
        /// <summary>
        /// 네트워크 이벤트 - 방향 변경 (사용 안함)
        /// </summary>
        private void OnDirectionChanged(int previousValue, int newValue)
        {
            // 더 이상 사용하지 않음
        }
        
        /// <summary>
        /// 네트워크 이벤트 - 애니메이션 상태 변경
        /// </summary>
        private void OnAnimationStateChanged(PlayerAnimationState previousValue, PlayerAnimationState newValue)
        {
            currentAnimationState = newValue;
            if (spriteAnimator != null)
            {
                spriteAnimator.PlayAnimation(newValue);
            }
        }
        
        /// <summary>
        /// 현재 장착한 무기의 WeaponGroup 가져오기
        /// </summary>
        private WeaponGroup GetCurrentWeaponGroup()
        {
            if (equipmentManager != null)
            {
                var mainWeapon = equipmentManager.Equipment.GetEquippedItem(EquipmentSlot.MainHand);
                if (mainWeapon?.ItemData?.IsWeapon == true)
                {
                    return mainWeapon.ItemData.WeaponGroup;
                }
                
                var twoHandWeapon = equipmentManager.Equipment.GetEquippedItem(EquipmentSlot.TwoHand);
                if (twoHandWeapon?.ItemData?.IsWeapon == true)
                {
                    return twoHandWeapon.ItemData.WeaponGroup;
                }
            }
            
            return WeaponGroup.Fist; // 기본값
        }
        
        
        /// <summary>
        /// 장비 변경 이벤트 핸들러
        /// </summary>
        private void OnEquipmentChanged(EquipmentSlot slot, ItemInstance item)
        {
            // 무기 슬롯이 변경된 경우만 처리
            if (slot == EquipmentSlot.MainHand || slot == EquipmentSlot.TwoHand)
            {
                WeaponGroup newWeaponGroup = GetCurrentWeaponGroup();
                
                if (currentWeaponGroup != newWeaponGroup)
                {
                    currentWeaponGroup = newWeaponGroup;
                    
                    // PlayerSpriteAnimator에 무기군 변경 알림
                    if (spriteAnimator != null)
                    {
                        spriteAnimator.ChangeWeaponGroup(newWeaponGroup);
                    }
                    
                    Debug.Log($"🛡️ Weapon group changed to: {newWeaponGroup}");
                }
            }
        }
        
        /// <summary>
        /// 수동 스프라이트 업데이트 (에디터 테스트용)
        /// </summary>
        [ContextMenu("Update Visuals")]
        public void UpdateVisuals()
        {
            SetupComponents();
            SetupInitialVisuals();
        }
        
        /// <summary>
        /// 현재 종족-무기군 조합 반환
        /// </summary>
        public (Race race, WeaponGroup weaponGroup) GetCurrentCombination()
        {
            return (currentRace, currentWeaponGroup);
        }
    }
}