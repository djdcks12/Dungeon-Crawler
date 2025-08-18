using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 플레이어 비주얼 매니저 - 종족별 스프라이트 및 애니메이션 관리
    /// </summary>
    public class PlayerVisualManager : NetworkBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer characterRenderer;
        [SerializeField] private SpriteRenderer weaponRenderer;
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
        private NetworkVariable<PlayerAnimationType> networkAnimationType = new NetworkVariable<PlayerAnimationType>(PlayerAnimationType.Idle,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);
            
        // 현재 상태
        private Race currentRace = Race.Human;
        private int currentDirection = 0; // 0=Down, 1=Side, 2=Up
        private PlayerAnimationType currentAnimationType = PlayerAnimationType.Idle;
        private WeaponType currentWeaponType = WeaponType.Fists;
        
        // 컴포넌트 참조
        private PlayerController playerController;
        private PlayerStatsManager statsManager;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // 컴포넌트 초기화
            SetupComponents();
            
            // 네트워크 변수 이벤트 구독
            networkRace.OnValueChanged += OnRaceChanged;
            networkDirection.OnValueChanged += OnDirectionChanged;
            networkAnimationType.OnValueChanged += OnAnimationTypeChanged;
            
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
            networkAnimationType.OnValueChanged -= OnAnimationTypeChanged;
            
            base.OnNetworkDespawn();
        }
        
        /// <summary>
        /// 컴포넌트 설정
        /// </summary>
        private void SetupComponents()
        {
            playerController = GetComponent<PlayerController>();
            statsManager = GetComponent<PlayerStatsManager>();
            
            // SpriteRenderer가 없으면 생성
            if (characterRenderer == null)
            {
                characterRenderer = GetComponent<SpriteRenderer>();
                if (characterRenderer == null)
                {
                    var rendererObject = new GameObject("CharacterSprite");
                    rendererObject.transform.SetParent(transform);
                    rendererObject.transform.localPosition = Vector3.zero;
                    characterRenderer = rendererObject.AddComponent<SpriteRenderer>();
                }
            }
            
            // 무기 렌더러 설정
            if (weaponRenderer == null)
            {
                var weaponObject = new GameObject("WeaponSprite");
                weaponObject.transform.SetParent(transform);
                weaponObject.transform.localPosition = Vector3.zero;
                weaponRenderer = weaponObject.AddComponent<SpriteRenderer>();
                weaponRenderer.sortingOrder = 1; // 캐릭터보다 앞에
            }
            
            // 기본 설정
            if (characterRenderer != null)
            {
                characterRenderer.sortingLayerName = "Characters";
                characterRenderer.sortingOrder = 0;
                characterRenderer.color = characterTint;
                
                // 스케일 적용
                transform.localScale = Vector3.one * characterScale;
            }
        }
        
        /// <summary>
        /// 초기 비주얼 설정
        /// </summary>
        private void SetupInitialVisuals()
        {
            // 스탯 매니저에서 종족 정보 가져오기
            if (statsManager?.CurrentStats != null)
            {
                SetRace(statsManager.CurrentStats.CharacterRace);
            }
            else
            {
                // 기본값 설정
                SetRace(Race.Human);
            }
            
            // 기본 무기 설정
            SetWeapon(WeaponType.Fists);
            
            // 기본 애니메이션 설정
            SetAnimation(PlayerAnimationType.Idle);
        }
        
        /// <summary>
        /// 종족 설정
        /// </summary>
        public void SetRace(Race race)
        {
            currentRace = race;
            
            if (IsOwner)
            {
                networkRace.Value = race;
            }
            
            UpdateCharacterSprite();
        }
        
        /// <summary>
        /// 무기 설정
        /// </summary>
        public void SetWeapon(WeaponType weaponType)
        {
            currentWeaponType = weaponType;
            UpdateWeaponSprite();
        }
        
        /// <summary>
        /// 애니메이션 타입 설정
        /// </summary>
        public void SetAnimation(PlayerAnimationType animationType)
        {
            currentAnimationType = animationType;
            
            if (IsOwner)
            {
                networkAnimationType.Value = animationType;
            }
            
            UpdateCharacterSprite();
        }
        
        /// <summary>
        /// 방향 설정 (이동 방향에 따라) - 사용 안함
        /// </summary>
        public void SetDirection(Vector2 moveDirection)
        {
            // 이동 방향으로는 더 이상 캐릭터 방향을 설정하지 않음
            // 마우스 방향으로만 설정
        }
        
        /// <summary>
        /// 마우스 방향에 따른 캐릭터 바라보는 방향 설정
        /// </summary>
        public void SetDirectionFromMouse(Vector2 mouseDirection)
        {
            int newDirection = CalculateDirectionFromMouse(mouseDirection);
            
            if (newDirection != currentDirection)
            {
                currentDirection = newDirection;
                
                if (IsOwner)
                {
                    networkDirection.Value = newDirection;
                }
                
                UpdateCharacterSprite();
                UpdateSpriteFlipFromMouse(mouseDirection);
            }
        }
        
        /// <summary>
        /// 이동 방향으로부터 스프라이트 방향 계산 (사용 안함)
        /// </summary>
        private int CalculateDirection(Vector2 moveDirection)
        {
            if (moveDirection.magnitude < 0.1f) return currentDirection; // 정지 시 기존 방향 유지
            
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            
            // 8방향을 4방향으로 매핑
            if (angle >= -45f && angle < 45f) return 1; // Right (Side)
            else if (angle >= 45f && angle < 135f) return 2; // Up
            else if (angle >= -135f && angle < -45f) return 0; // Down
            else return 1; // Left (Side, 플립 적용)
        }
        
        /// <summary>
        /// 마우스 방향으로부터 스프라이트 방향 계산
        /// </summary>
        private int CalculateDirectionFromMouse(Vector2 mouseDirection)
        {
            if (mouseDirection.magnitude < 0.1f) return currentDirection; // 변화가 없으면 기존 방향 유지
            
            float angle = Mathf.Atan2(mouseDirection.y, mouseDirection.x) * Mathf.Rad2Deg;
            
            // 8방향을 4방향으로 매핑
            if (angle >= -45f && angle < 45f) return 1; // Right (Side)
            else if (angle >= 45f && angle < 135f) return 2; // Up
            else if (angle >= -135f && angle < -45f) return 0; // Down
            else return 1; // Left (Side, 플립 적용)
        }
        
        /// <summary>
        /// 스프라이트 플립 적용 (이동 방향 - 사용 안함)
        /// </summary>
        private void UpdateSpriteFlip(Vector2 moveDirection)
        {
            // 이동 방향으로는 더 이상 플립하지 않음
        }
        
        /// <summary>
        /// 마우스 방향에 따른 스프라이트 플립 적용
        /// </summary>
        private void UpdateSpriteFlipFromMouse(Vector2 mouseDirection)
        {
            if (characterRenderer == null) return;
            
            // 마우스 방향에 따른 좌우 플립
            if (Mathf.Abs(mouseDirection.x) > 0.1f)
            {
                characterRenderer.flipX = mouseDirection.x < 0;
                if (weaponRenderer != null)
                {
                    weaponRenderer.flipX = mouseDirection.x < 0;
                }
            }
        }
        
        /// <summary>
        /// 캐릭터 스프라이트 업데이트
        /// </summary>
        private void UpdateCharacterSprite()
        {
            if (characterRenderer == null) return;
            
            Sprite characterSprite = ResourceLoader.GetPlayerSprite(currentRace, currentAnimationType, currentDirection);
            
            if (characterSprite != null)
            {
                characterRenderer.sprite = characterSprite;
                Debug.Log($"Updated character sprite: {currentRace} - {currentAnimationType} - {GetDirectionName(currentDirection)}");
            }
            else
            {
                // 기본 스프라이트 로드 시도
                characterSprite = ResourceLoader.GetPlayerSprite(Race.Human, PlayerAnimationType.Idle, 0);
                if (characterSprite != null)
                {
                    characterRenderer.sprite = characterSprite;
                    Debug.LogWarning($"Using fallback sprite for {currentRace}");
                }
            }
        }
        
        /// <summary>
        /// 무기 스프라이트 업데이트
        /// </summary>
        private void UpdateWeaponSprite()
        {
            if (weaponRenderer == null) return;
            
            // 맨손이면 무기 숨기기
            if (currentWeaponType == WeaponType.Fists)
            {
                weaponRenderer.sprite = null;
                return;
            }
            
            Sprite weaponSprite = ResourceLoader.GetWeaponSprite(currentWeaponType);
            weaponRenderer.sprite = weaponSprite;
            
            if (weaponSprite != null)
            {
                Debug.Log($"Updated weapon sprite: {currentWeaponType}");
            }
        }
        
        /// <summary>
        /// 네트워크 이벤트 - 종족 변경
        /// </summary>
        private void OnRaceChanged(Race previousValue, Race newValue)
        {
            currentRace = newValue;
            UpdateCharacterSprite();
        }
        
        /// <summary>
        /// 네트워크 이벤트 - 방향 변경
        /// </summary>
        private void OnDirectionChanged(int previousValue, int newValue)
        {
            currentDirection = newValue;
            UpdateCharacterSprite();
        }
        
        /// <summary>
        /// 네트워크 이벤트 - 애니메이션 타입 변경
        /// </summary>
        private void OnAnimationTypeChanged(PlayerAnimationType previousValue, PlayerAnimationType newValue)
        {
            currentAnimationType = newValue;
            UpdateCharacterSprite();
        }
        
        /// <summary>
        /// 공격 애니메이션 트리거
        /// </summary>
        public void TriggerAttackAnimation()
        {
            // 무기 타입에 따른 공격 애니메이션 선택
            PlayerAnimationType attackAnim = GetAttackAnimationType(currentWeaponType);
            
            SetAnimation(attackAnim);
            
            // 잠시 후 Idle로 복귀
            Invoke(nameof(ReturnToIdle), 0.5f);
        }
        
        /// <summary>
        /// 무기 타입에 따른 공격 애니메이션 가져오기
        /// </summary>
        private PlayerAnimationType GetAttackAnimationType(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Longsword:
                case WeaponType.Rapier:
                case WeaponType.Broadsword:
                    return PlayerAnimationType.Attack_Slice;
                    
                case WeaponType.Dagger:
                case WeaponType.CurvedDagger:
                    return PlayerAnimationType.Attack_Pierce;
                    
                default:
                    return PlayerAnimationType.Attack_Slice;
            }
        }
        
        /// <summary>
        /// Idle 애니메이션으로 복귀
        /// </summary>
        private void ReturnToIdle()
        {
            SetAnimation(PlayerAnimationType.Idle);
        }
        
        /// <summary>
        /// 피격 이펙트
        /// </summary>
        public void PlayHitEffect()
        {
            SetAnimation(PlayerAnimationType.Hit);
            Invoke(nameof(ReturnToIdle), 0.3f);
            
            // 빨간색 플래시 효과
            if (characterRenderer != null)
            {
                StartCoroutine(HitFlashCoroutine());
            }
        }
        
        /// <summary>
        /// 피격 플래시 효과
        /// </summary>
        private System.Collections.IEnumerator HitFlashCoroutine()
        {
            Color originalColor = characterRenderer.color;
            characterRenderer.color = Color.red;
            
            yield return new WaitForSeconds(0.1f);
            
            characterRenderer.color = originalColor;
        }
        
        /// <summary>
        /// 방향명 가져오기 (디버그용)
        /// </summary>
        private string GetDirectionName(int direction)
        {
            switch (direction)
            {
                case 0: return "Down";
                case 1: return "Side";
                case 2: return "Up";
                default: return "Unknown";
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
        /// 종족 변경 테스트 (에디터용)
        /// </summary>
        [ContextMenu("Test Race Change")]
        public void TestRaceChange()
        {
            Race nextRace = (Race)(((int)currentRace + 1) % 4);
            SetRace(nextRace);
            Debug.Log($"Changed race to: {nextRace}");
        }
    }
}