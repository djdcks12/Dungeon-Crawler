using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 대화 UI - 대화 진행, 선택지, NPC 초상화
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        private GameObject dialoguePanel;
        private Text speakerNameText;
        private Text dialogueText;
        private Image portraitImage;
        private Transform choiceContainer;
        private Button continueButton;
        private Button skipButton;

        private List<GameObject> choiceButtons = new List<GameObject>();
        private bool isInitialized;

        // 타이핑 효과
        private string fullText;
        private float typingSpeed = 0.03f;
        private float typingTimer;
        private int typingIndex;
        private bool isTyping;

        private void Start()
        {
            CreateUI();
            isInitialized = true;
            dialoguePanel.SetActive(false);

            if (DialogueSystem.Instance != null)
            {
                DialogueSystem.Instance.OnNodeDisplayed += ShowNode;
                DialogueSystem.Instance.OnDialogueStarted += OnDialogueStarted;
                DialogueSystem.Instance.OnDialogueEnded += OnDialogueEnded;
            }
        }

        private void Update()
        {
            if (!isInitialized || !dialoguePanel.activeSelf) return;

            // 타이핑 효과
            if (isTyping)
            {
                typingTimer += Time.deltaTime;
                if (typingTimer >= typingSpeed)
                {
                    typingTimer = 0f;
                    typingIndex++;
                    if (typingIndex >= fullText.Length)
                    {
                        dialogueText.text = fullText;
                        isTyping = false;
                    }
                    else
                    {
                        dialogueText.text = fullText.Substring(0, typingIndex);
                    }
                }
            }

            // 클릭/스페이스바로 대화 진행
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                if (isTyping)
                {
                    // 타이핑 중이면 즉시 완성
                    isTyping = false;
                    dialogueText.text = fullText;
                }
                else if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsInDialogue)
                {
                    var node = DialogueSystem.Instance.CurrentNode;
                    if (node != null && (node.choices == null || node.choices.Count == 0))
                    {
                        DialogueSystem.Instance.AdvanceDialogue();
                    }
                }
            }

            // ESC로 스킵
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (DialogueSystem.Instance != null)
                    DialogueSystem.Instance.SkipDialogue();
            }
        }

        private void OnDialogueStarted()
        {
            dialoguePanel.SetActive(true);
        }

        private void OnDialogueEnded()
        {
            dialoguePanel.SetActive(false);
            ClearChoices();
        }

        private void ShowNode(DialogueNode node)
        {
            if (node == null) return;

            // 화자 이름
            string speaker = !string.IsNullOrEmpty(node.speakerName)
                ? node.speakerName
                : (DialogueSystem.Instance?.CurrentDialogue?.NPCName ?? "???");
            speakerNameText.text = $"<color=#FFD700>{speaker}</color>";

            // 초상화
            if (DialogueSystem.Instance?.CurrentDialogue?.NPCPortrait != null)
            {
                portraitImage.sprite = DialogueSystem.Instance.CurrentDialogue.NPCPortrait;
                portraitImage.gameObject.SetActive(true);
            }
            else
            {
                portraitImage.gameObject.SetActive(false);
            }

            // 텍스트 (타이핑 효과)
            fullText = node.text;
            typingIndex = 0;
            typingTimer = 0f;
            isTyping = true;
            dialogueText.text = "";

            // 선택지
            ClearChoices();
            if (node.choices != null && node.choices.Count > 0)
            {
                continueButton.gameObject.SetActive(false);
                ShowChoices(node.choices);
            }
            else
            {
                continueButton.gameObject.SetActive(true);
            }
        }

        private void ShowChoices(List<DialogueChoice> choices)
        {
            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                bool available = DialogueSystem.Instance != null && DialogueSystem.Instance.IsChoiceAvailable(choice);

                var btn = CreateChoiceButton(i, choice.choiceText, available);
                choiceButtons.Add(btn);
            }
        }

        private void ClearChoices()
        {
            foreach (var btn in choiceButtons)
                if (btn != null) Destroy(btn);
            choiceButtons.Clear();
        }

        #region UI 생성

        private void CreateUI()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // 최상위
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            // 대화 패널 (하단)
            dialoguePanel = new GameObject("DialoguePanel");
            dialoguePanel.transform.SetParent(transform, false);
            var panelRT = dialoguePanel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0, 0);
            panelRT.anchorMax = new Vector2(1, 0);
            panelRT.pivot = new Vector2(0.5f, 0);
            panelRT.anchoredPosition = new Vector2(0, 10);
            panelRT.sizeDelta = new Vector2(-40, 200);
            dialoguePanel.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.92f);

            // 초상화
            var portraitObj = new GameObject("Portrait");
            portraitObj.transform.SetParent(dialoguePanel.transform, false);
            var prtRT = portraitObj.AddComponent<RectTransform>();
            prtRT.anchoredPosition = new Vector2(-320, 20);
            prtRT.sizeDelta = new Vector2(120, 120);
            portraitImage = portraitObj.AddComponent<Image>();
            portraitImage.color = Color.white;

            // 화자 이름
            var nameObj = CreateText(dialoguePanel.transform, "SpeakerName", "", 18, TextAnchor.MiddleLeft,
                new Vector2(60, 70), new Vector2(400, 30));
            speakerNameText = nameObj.GetComponent<Text>();

            // 대화 텍스트
            var textObj = CreateText(dialoguePanel.transform, "DialogueText", "", 15, TextAnchor.UpperLeft,
                new Vector2(60, 10), new Vector2(500, 100));
            dialogueText = textObj.GetComponent<Text>();

            // 계속 버튼
            continueButton = CreateButton(dialoguePanel.transform, "ContinueBtn", "▼ 계속",
                new Vector2(250, -70), new Vector2(100, 30));
            continueButton.onClick.AddListener(() =>
            {
                if (DialogueSystem.Instance != null)
                    DialogueSystem.Instance.AdvanceDialogue();
            });

            // 스킵 버튼
            skipButton = CreateButton(dialoguePanel.transform, "SkipBtn", "스킵",
                new Vector2(320, 70), new Vector2(70, 25));
            skipButton.onClick.AddListener(() =>
            {
                if (DialogueSystem.Instance != null)
                    DialogueSystem.Instance.SkipDialogue();
            });
            skipButton.GetComponent<Image>().color = new Color(0.3f, 0.2f, 0.2f, 0.8f);

            // 선택지 컨테이너
            var choiceObj = new GameObject("ChoiceContainer");
            choiceObj.transform.SetParent(dialoguePanel.transform, false);
            var choiceRT = choiceObj.AddComponent<RectTransform>();
            choiceRT.anchoredPosition = new Vector2(60, -30);
            choiceRT.sizeDelta = new Vector2(500, 100);
            var layout = choiceObj.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            choiceContainer = choiceObj.transform;
        }

        private GameObject CreateChoiceButton(int index, string text, bool available)
        {
            var obj = new GameObject($"Choice_{index}");
            obj.transform.SetParent(choiceContainer, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 35);
            var img = obj.AddComponent<Image>();
            img.color = available ? new Color(0.2f, 0.25f, 0.35f, 0.9f) : new Color(0.2f, 0.2f, 0.2f, 0.5f);

            var btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = available;

            int choiceIndex = index;
            btn.onClick.AddListener(() =>
            {
                if (DialogueSystem.Instance != null)
                    DialogueSystem.Instance.SelectChoice(choiceIndex);
            });

            string prefix = available ? $"<color=#FFD700>{index + 1}.</color>" : $"<color=#666666>{index + 1}.</color>";
            string color = available ? "#FFFFFF" : "#666666";
            CreateText(obj.transform, "Text", $"{prefix} <color={color}>{text}</color>",
                14, TextAnchor.MiddleLeft, new Vector2(10, 0), new Vector2(480, 30));

            return obj;
        }

        private GameObject CreateText(Transform parent, string name, string text, int fontSize,
            TextAnchor alignment, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = Color.white;
            txt.supportRichText = true;
            return obj;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var img = obj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.3f, 0.4f, 0.9f);
            var btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(obj.transform, false);
            var txtRT = txtObj.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.sizeDelta = Vector2.zero;
            var txt = txtObj.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 14;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            return btn;
        }

        #endregion

        private void OnDestroy()
        {
            if (DialogueSystem.Instance != null)
            {
                DialogueSystem.Instance.OnNodeDisplayed -= ShowNode;
                DialogueSystem.Instance.OnDialogueStarted -= OnDialogueStarted;
                DialogueSystem.Instance.OnDialogueEnded -= OnDialogueEnded;
            }
        }
    }
}
