using UnityEngine;
// using UnityEngine.InputSystem; // Not available in current setup

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 플레이어 입력 처리 시스템
    /// WASD 이동, 마우스 회전, 좌클릭 공격, 우클릭 스킬
    /// </summary>
    public class PlayerInput : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private KeyCode attackKey = KeyCode.Mouse0;
        [SerializeField] private KeyCode skillKey = KeyCode.Mouse1;
        [SerializeField] private bool debugInput = true;
        
        private Vector2 moveInput;
        private Vector2 mousePosition;
        private bool attackPressed;
        private bool skillPressed;
        private bool attackHeld;
        private bool skillHeld;
        
        private void Update()
        {
            // PlayerInput Update 호출 확인 (2초마다 한 번)
            if (Time.frameCount % 120 == 0)
            {
                Debug.Log($"🎯 PlayerInput Update called for {gameObject.name}");
            }
            
            HandleInput();
        }
        
        private void HandleInput()
        {
            // WASD 이동 입력
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            moveInput = new Vector2(horizontal, vertical);
            
            // Input.GetAxis 동작 확인 (한 번만 로그)
            if (Time.frameCount % 240 == 0) // 4초마다 한 번
            {
                Debug.Log($"🔍 Input.GetAxis values - Horizontal: {horizontal:F2}, Vertical: {vertical:F2}");
            }
            
            // 이동 입력 디버그 (0이 아닐 때만)
            if (debugInput && moveInput.magnitude > 0.1f)
            {
                Debug.Log($"🎮 PlayerInput: Move input detected - H:{horizontal:F2}, V:{vertical:F2}");
            }
            
            // 마우스 위치
            mousePosition = Input.mousePosition;
            
            // 공격 입력
            if (Input.GetKeyDown(attackKey))
            {
                attackPressed = true;
                if (debugInput)
                {
                    Debug.Log($"🔥 PlayerInput: Attack key ({attackKey}) pressed!");
                }
            }
            attackHeld = Input.GetKey(attackKey);
            
            // 스킬 입력
            if (Input.GetKeyDown(skillKey))
            {
                skillPressed = true;
                if (debugInput)
                {
                    Debug.Log($"✨ PlayerInput: Skill key ({skillKey}) pressed!");
                }
            }
            skillHeld = Input.GetKey(skillKey);
        }
        
        // 공용 접근 메서드들
        public Vector2 GetMoveInput()
        {
            return moveInput;
        }
        
        public Vector2 GetMousePosition()
        {
            return mousePosition;
        }
        
        public bool GetAttackInput()
        {
            bool result = attackPressed;
            attackPressed = false; // 한 번만 처리되도록
            return result;
        }
        
        public bool GetSkillInput()
        {
            bool result = skillPressed;
            skillPressed = false; // 한 번만 처리되도록
            return result;
        }
        
        // 입력 상태 확인 (연속 입력용)
        public bool IsAttackHeld()
        {
            return attackHeld;
        }
        
        public bool IsSkillHeld()
        {
            return skillHeld;
        }
        
        // 디버그용 정보
        public void LogInputState()
        {
            Debug.Log($"Move: {moveInput}, Mouse: {mousePosition}, Attack: {attackPressed}, Skill: {skillPressed}");
        }
    }
}