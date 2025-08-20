using UnityEngine;
using UnityEngine.UI;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 플레이어 사망 시 표시되는 UI
    /// 부활 옵션, 스탯 손실 정보 등을 표시
    /// </summary>
    public class DeathUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private GameObject deathPanel;
        [SerializeField] private Text deathMessageText;
        [SerializeField] private Button respawnButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Text penaltyInfoText;
        
        [Header("Death Settings")]
        [SerializeField] private string deathMessage = "You have died...";
        [SerializeField] private float respawnDelay = 5f;
        
        private float respawnTimer = 0f;
        private bool isRespawnReady = false;
        
        private void Awake()
        {
            if (respawnButton != null)
            {
                respawnButton.onClick.AddListener(OnRespawnClicked);
            }
            
            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitClicked);
            }
            
            // 초기에는 비활성화
            if (deathPanel != null)
            {
                deathPanel.SetActive(false);
            }
        }
        
        private void Update()
        {
            if (!isRespawnReady && respawnTimer > 0f)
            {
                respawnTimer -= Time.deltaTime;
                
                if (respawnTimer <= 0f)
                {
                    isRespawnReady = true;
                    UpdateRespawnButton();
                }
                else
                {
                    UpdateRespawnTimer();
                }
            }
        }
        
        /// <summary>
        /// 사망 UI 표시
        /// </summary>
        public void ShowDeathUI()
        {
            if (deathPanel != null)
            {
                deathPanel.SetActive(true);
            }
            
            respawnTimer = respawnDelay;
            isRespawnReady = false;
            
            UpdateUI();
        }
        
        /// <summary>
        /// 사망 UI 숨기기
        /// </summary>
        public void HideDeathUI()
        {
            if (deathPanel != null)
            {
                deathPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// UI 정보 업데이트
        /// </summary>
        private void UpdateUI()
        {
            if (deathMessageText != null)
            {
                deathMessageText.text = deathMessage;
            }
            
            UpdatePenaltyInfo();
            UpdateRespawnButton();
        }
        
        /// <summary>
        /// 페널티 정보 업데이트
        /// </summary>
        private void UpdatePenaltyInfo()
        {
            if (penaltyInfoText != null)
            {
                penaltyInfoText.text = "Death Penalty: -10% EXP, -50% Gold";
            }
        }
        
        /// <summary>
        /// 부활 버튼 상태 업데이트
        /// </summary>
        private void UpdateRespawnButton()
        {
            if (respawnButton != null)
            {
                respawnButton.interactable = isRespawnReady;
                
                Text buttonText = respawnButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    if (isRespawnReady)
                    {
                        buttonText.text = "Respawn";
                    }
                    else
                    {
                        buttonText.text = $"Respawn ({respawnTimer:F0}s)";
                    }
                }
            }
        }
        
        /// <summary>
        /// 부활 타이머 업데이트
        /// </summary>
        private void UpdateRespawnTimer()
        {
            Text buttonText = respawnButton?.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = $"Respawn ({respawnTimer:F0}s)";
            }
        }
        
        /// <summary>
        /// 부활 버튼 클릭
        /// </summary>
        private void OnRespawnClicked()
        {
            if (!isRespawnReady) return;
            
            // DeathManager를 통해 부활 처리
            var deathManager = FindObjectOfType<DeathManager>();
            if (deathManager != null)
            {
                // TODO: 부활 로직 연동
                Debug.Log("Respawn requested");
            }
            
            HideDeathUI();
        }
        
        /// <summary>
        /// 게임 종료 버튼 클릭
        /// </summary>
        private void OnQuitClicked()
        {
            // 메인 메뉴로 돌아가기 또는 게임 종료
            Debug.Log("Quit to main menu requested");
            
            // TODO: 메인 메뉴 이동 로직
            HideDeathUI();
        }
        
        /// <summary>
        /// 사망 메시지 설정
        /// </summary>
        public void SetDeathMessage(string message)
        {
            deathMessage = message;
            
            if (deathMessageText != null)
            {
                deathMessageText.text = message;
            }
        }
    }
}