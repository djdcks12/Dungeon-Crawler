using UnityEngine;
using UnityEngine.UI;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 던전 입구 포탈 NPC - F키로 상호작용하여 던전 입장 UI 표시
    /// </summary>
    public class DungeonPortalNPC : MonoBehaviour
    {
        [Header("포탈 설정")]
        [SerializeField] private string portalName = "던전 입구";
        [SerializeField] private string dungeonId = "";
        [SerializeField] private int requiredLevel = 1;

        [Header("시각 효과")]
        [SerializeField] private GameObject interactionPrompt;

        private bool playerInRange = false;
        private PlayerController nearbyPlayer;

        // 포탈 이펙트
        private float pulseTimer = 0f;
        private SpriteRenderer spriteRenderer;

        private void Start()
        {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);

            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            // 포탈 펄스 이펙트
            PulseEffect();

            if (!playerInRange || nearbyPlayer == null) return;

            if (Input.GetKeyDown(KeyCode.F))
            {
                TryEnterDungeon();
            }
        }

        private void PulseEffect()
        {
            if (spriteRenderer == null) return;

            pulseTimer += Time.deltaTime * 2f;
            float alpha = 0.6f + Mathf.Sin(pulseTimer) * 0.3f;
            var c = spriteRenderer.color;
            spriteRenderer.color = new Color(c.r, c.g, c.b, alpha);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null && player.IsOwner)
            {
                playerInRange = true;
                nearbyPlayer = player;
                if (interactionPrompt != null)
                    interactionPrompt.SetActive(true);
                ShowMessage($"[F] {portalName} (권장 Lv.{requiredLevel}+)");
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null && player.IsOwner)
            {
                playerInRange = false;
                nearbyPlayer = null;
                if (interactionPrompt != null)
                    interactionPrompt.SetActive(false);
            }
        }

        private void TryEnterDungeon()
        {
            // 레벨 체크
            var statsManager = nearbyPlayer.GetComponent<PlayerStatsManager>();
            if (statsManager != null && statsManager.CurrentStats != null)
            {
                int playerLevel = statsManager.CurrentStats.CurrentLevel;
                if (playerLevel < requiredLevel)
                {
                    ShowMessage($"레벨이 부족합니다! (현재 Lv.{playerLevel}, 권장 Lv.{requiredLevel})");
                    return;
                }
            }

            // 던전 입장 UI 열기
            var dungeonEntryUI = FindFirstObjectByType<DungeonEntryUI>();
            if (dungeonEntryUI == null)
            {
                // UIManager를 통해 로드
                var uiManager = UIManager.Instance;
                if (uiManager != null)
                {
                    dungeonEntryUI = uiManager.LoadDungeonEntryUI();
                }
            }

            if (dungeonEntryUI != null)
            {
                dungeonEntryUI.Show();
                ShowMessage($"{portalName}에 진입합니다...");
            }
            else
            {
                ShowMessage("던전 입장 UI를 로드할 수 없습니다.");
            }
        }

        private void ShowMessage(string message)
        {
            var chatUI = FindFirstObjectByType<ChatUI>();
            if (chatUI != null)
                chatUI.AddSystemMessage(message);
            else
                Debug.Log($"[DungeonPortal] {message}");
        }

        public string PortalName => portalName;
        public string DungeonId => dungeonId;
        public int RequiredLevel => requiredLevel;
    }
}
