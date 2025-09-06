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
        [SerializeField] private KeyCode statsKey = KeyCode.C;
        [SerializeField] private bool debugInput = true;
        
        private Vector2 moveInput;
        private Vector2 mousePosition;
        private bool attackPressed;
        private bool skillPressed;
        private bool attackHeld;
        private bool skillHeld;
        
        private void Update()
        {        
            HandleInput();
        }
        
        private void HandleInput()
        {
            // WASD 이동 입력
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            moveInput = new Vector2(horizontal, vertical);
            
            // 마우스 위치
            mousePosition = Input.mousePosition;
            
            // 공격 입력
            if (Input.GetKeyDown(attackKey))
            {
                attackPressed = true;
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
            
            // 스탯창 토글 입력
            if (Input.GetKeyDown(statsKey))
            {
                ToggleStatsUI();
            }
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
        
        /// <summary>
        /// 스탯 UI 토글
        /// </summary>
        private void ToggleStatsUI()
        {
            var statsUI = FindObjectOfType<StatsUI>();
            if (statsUI != null)
            {
                statsUI.ToggleStatsPanel();
            }
        }
    }
}