using UnityEngine;
using UnityEngine.UI;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 대장장이 NPC - F키로 상호작용하여 아이템 강화 UI 열기
    /// </summary>
    public class BlacksmithNPC : MonoBehaviour
    {
        [Header("NPC 설정")]
        [SerializeField] private string npcName = "대장장이";

        [Header("시각 효과")]
        [SerializeField] private GameObject interactionPrompt;

        private bool playerInRange = false;
        private PlayerController nearbyPlayer;

        // 강화 UI
        private GameObject enhancePanel;
        private Text enhanceInfoText;
        private bool isUIOpen = false;
        private Button closeEnhanceButton;

        private void Start()
        {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
        }

        private void Update()
        {
            if (!playerInRange || nearbyPlayer == null) return;

            if (Input.GetKeyDown(KeyCode.F))
            {
                if (isUIOpen)
                    CloseEnhanceUI();
                else
                    OpenEnhanceUI();
            }

            if (isUIOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseEnhanceUI();
            }
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
                ShowMessage($"[F] {npcName}와 대화하기");
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

                if (isUIOpen)
                    CloseEnhanceUI();
            }
        }

        private void OpenEnhanceUI()
        {
            if (enhancePanel == null)
                CreateEnhanceUI();

            isUIOpen = true;
            enhancePanel.SetActive(true);
            RefreshEnhanceInfo();
            ShowMessage($"{npcName}: 어서 오게. 장비를 강화해주지.");
        }

        private void CloseEnhanceUI()
        {
            isUIOpen = false;
            if (enhancePanel != null)
                enhancePanel.SetActive(false);
        }

        private void RefreshEnhanceInfo()
        {
            if (enhanceInfoText == null) return;

            var enhanceSystem = ItemEnhanceSystem.Instance;
            if (enhanceSystem == null)
            {
                enhanceInfoText.text = "강화 시스템을 사용할 수 없습니다.";
                return;
            }

            string info = "<color=#FFD700><b>아이템 강화</b></color>\n\n";
            info += "인벤토리에서 장비를 선택하고\n강화 버튼을 눌러주세요.\n\n";
            info += "<color=#AADDFF>강화 단계별 성공률:</color>\n";
            info += "+1~+3: 100%  |  +4: 95%\n";
            info += "+5: 90%  |  +6: 80%  |  +7: 70%\n";
            info += "+8: 50%  |  +9: 35%  |  +10: 20%\n\n";
            info += "<color=#FF6666>주의: +9 이상 실패 시 파괴 확률 10%</color>";

            enhanceInfoText.text = info;
        }

        private void CreateEnhanceUI()
        {
            // Canvas
            var canvasObj = new GameObject("BlacksmithCanvas");
            canvasObj.transform.SetParent(transform, false);
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            // 반투명 배경
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.5f);
            var bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // 패널
            enhancePanel = new GameObject("EnhancePanel");
            enhancePanel.transform.SetParent(canvasObj.transform, false);
            var panelBg = enhancePanel.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.08f, 0.15f, 0.95f);
            var panelRt = enhancePanel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(400, 350);

            // 타이틀
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(enhancePanel.transform, false);
            var titleText = titleObj.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.UpperCenter;
            titleText.color = new Color(1f, 0.85f, 0.3f);
            titleText.text = npcName;
            var titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1);
            titleRt.sizeDelta = new Vector2(0, 40);
            titleRt.anchoredPosition = new Vector2(0, -10);

            // 정보 텍스트
            var infoObj = new GameObject("Info");
            infoObj.transform.SetParent(enhancePanel.transform, false);
            enhanceInfoText = infoObj.AddComponent<Text>();
            enhanceInfoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            enhanceInfoText.fontSize = 14;
            enhanceInfoText.alignment = TextAnchor.UpperLeft;
            enhanceInfoText.color = Color.white;
            enhanceInfoText.supportRichText = true;
            var infoRt = infoObj.GetComponent<RectTransform>();
            infoRt.anchorMin = Vector2.zero;
            infoRt.anchorMax = Vector2.one;
            infoRt.offsetMin = new Vector2(20, 50);
            infoRt.offsetMax = new Vector2(-20, -55);

            // 닫기 버튼
            var closeBtnObj = new GameObject("CloseBtn");
            closeBtnObj.transform.SetParent(enhancePanel.transform, false);
            var closeBtnImg = closeBtnObj.AddComponent<Image>();
            closeBtnImg.color = new Color(0.4f, 0.2f, 0.2f);
            closeEnhanceButton = closeBtnObj.AddComponent<Button>();
            closeEnhanceButton.onClick.AddListener(CloseEnhanceUI);
            var closeBtnRt = closeBtnObj.GetComponent<RectTransform>();
            closeBtnRt.anchorMin = new Vector2(1, 1);
            closeBtnRt.anchorMax = new Vector2(1, 1);
            closeBtnRt.pivot = new Vector2(1, 1);
            closeBtnRt.sizeDelta = new Vector2(30, 30);
            closeBtnRt.anchoredPosition = new Vector2(-5, -5);

            var closeTextObj = new GameObject("Text");
            closeTextObj.transform.SetParent(closeBtnObj.transform, false);
            var closeText = closeTextObj.AddComponent<Text>();
            closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeText.fontSize = 16;
            closeText.fontStyle = FontStyle.Bold;
            closeText.alignment = TextAnchor.MiddleCenter;
            closeText.color = Color.white;
            closeText.text = "X";
            var ctRt = closeTextObj.GetComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.offsetMin = Vector2.zero;
            ctRt.offsetMax = Vector2.zero;

            enhancePanel.SetActive(false);
        }

        private void ShowMessage(string message)
        {
            var chatUI = FindFirstObjectByType<ChatUI>();
            if (chatUI != null)
                chatUI.AddSystemMessage(message);
            else
                Debug.Log($"[Blacksmith] {message}");
        }

        private void OnDestroy()
        {
            if (closeEnhanceButton != null) closeEnhanceButton.onClick.RemoveAllListeners();
        }

        public string NPCName => npcName;
    }
}
