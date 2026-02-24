using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 상태이상 효과 UI - HP바 하단에 활성 버프/디버프 아이콘 표시
    /// </summary>
    public class StatusEffectUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform iconContainer;
        [SerializeField] private GameObject iconPrefab; // 없으면 자동 생성

        [Header("Settings")]
        [SerializeField] private int maxIcons = 10;
        [SerializeField] private float iconSize = 32f;
        [SerializeField] private float iconSpacing = 4f;

        private SkillManager skillManager;
        private List<StatusIconSlot> iconSlots = new List<StatusIconSlot>();
        private bool isInitialized = false;

        private class StatusIconSlot
        {
            public GameObject root;
            public Image iconImage;
            public Image cooldownOverlay;
            public Text durationText;
            public StatusType activeType;
            public bool isInUse;
        }

        private void Start()
        {
            InitializeForLocalPlayer();
        }

        private void InitializeForLocalPlayer()
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null)
            {
                Invoke(nameof(InitializeForLocalPlayer), 0.5f);
                return;
            }

            var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (localPlayer == null)
            {
                Invoke(nameof(InitializeForLocalPlayer), 0.5f);
                return;
            }

            skillManager = localPlayer.GetComponent<SkillManager>();
            if (skillManager == null)
            {
                Invoke(nameof(InitializeForLocalPlayer), 0.5f);
                return;
            }

            // 아이콘 컨테이너 확인/생성
            if (iconContainer == null)
                iconContainer = transform;

            // 아이콘 슬롯 미리 생성
            CreateIconSlots();

            // 이벤트 구독
            skillManager.OnStatusEffectChanged += OnStatusEffectChanged;
            isInitialized = true;
        }

        private void CreateIconSlots()
        {
            for (int i = 0; i < maxIcons; i++)
            {
                var slot = new StatusIconSlot();

                var go = new GameObject($"StatusIcon_{i}");
                go.transform.SetParent(iconContainer, false);

                var rt = go.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(iconSize, iconSize);

                // 아이콘 배경
                slot.iconImage = go.AddComponent<Image>();
                slot.iconImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

                // 쿨다운 오버레이
                var overlayObj = new GameObject("Overlay");
                overlayObj.transform.SetParent(go.transform, false);
                var overlayRt = overlayObj.AddComponent<RectTransform>();
                overlayRt.anchorMin = Vector2.zero;
                overlayRt.anchorMax = Vector2.one;
                overlayRt.sizeDelta = Vector2.zero;
                slot.cooldownOverlay = overlayObj.AddComponent<Image>();
                slot.cooldownOverlay.color = new Color(0, 0, 0, 0.5f);
                slot.cooldownOverlay.type = Image.Type.Filled;
                slot.cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
                slot.cooldownOverlay.fillClockwise = false;

                // 시간 텍스트
                var textObj = new GameObject("Duration");
                textObj.transform.SetParent(go.transform, false);
                var textRt = textObj.AddComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.sizeDelta = Vector2.zero;
                slot.durationText = textObj.AddComponent<Text>();
                slot.durationText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                slot.durationText.fontSize = 10;
                slot.durationText.alignment = TextAnchor.LowerRight;
                slot.durationText.color = Color.white;
                var outline = textObj.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(1, -1);

                slot.root = go;
                slot.isInUse = false;
                go.SetActive(false);
                iconSlots.Add(slot);
            }
        }

        private void Update()
        {
            if (!isInitialized || skillManager == null) return;

            var effects = skillManager.ActiveEffects;

            // 모든 슬롯 비활성화
            foreach (var slot in iconSlots)
            {
                slot.isInUse = false;
            }

            // 활성 효과 표시
            int slotIndex = 0;
            for (int i = 0; i < effects.Count && slotIndex < maxIcons; i++)
            {
                var effect = effects[i];
                var slot = iconSlots[slotIndex];

                slot.isInUse = true;
                slot.activeType = effect.effect.type;
                slot.root.SetActive(true);

                // 아이콘 색상 (타입별)
                slot.iconImage.color = GetEffectColor(effect.effect.type);

                // 남은 시간 오버레이
                if (effect.effect.duration > 0)
                {
                    float fillAmount = effect.remainingDuration / effect.effect.duration;
                    slot.cooldownOverlay.fillAmount = 1f - fillAmount;
                    slot.durationText.text = $"{effect.remainingDuration:F0}";
                }
                else
                {
                    slot.cooldownOverlay.fillAmount = 0f;
                    slot.durationText.text = "";
                }

                slotIndex++;
            }

            // 사용되지 않는 슬롯 숨기기
            for (int i = slotIndex; i < iconSlots.Count; i++)
            {
                iconSlots[i].root.SetActive(false);
            }
        }

        private void OnStatusEffectChanged(StatusType type, bool isApplied)
        {
            // UI는 Update에서 자동 갱신되므로 여기서는 추가 처리 필요 없음
        }

        private Color GetEffectColor(StatusType type)
        {
            switch (type)
            {
                case StatusType.Poison: return new Color(0.4f, 0.8f, 0.2f);
                case StatusType.Burn: return new Color(1f, 0.3f, 0.1f);
                case StatusType.Stun: return new Color(1f, 1f, 0.3f);
                case StatusType.Slow: return new Color(0.3f, 0.5f, 1f);
                case StatusType.Regeneration: return new Color(0.3f, 1f, 0.5f);
                case StatusType.Strength: return new Color(1f, 0.5f, 0.3f);
                case StatusType.Speed: return new Color(0.5f, 1f, 1f);
                case StatusType.Blessing: return new Color(1f, 1f, 0.7f);
                case StatusType.Berserk: return new Color(0.9f, 0.2f, 0.2f);
                case StatusType.Enhancement: return new Color(0.7f, 0.5f, 1f);
                default: return new Color(0.5f, 0.5f, 0.5f);
            }
        }

        private void OnDestroy()
        {
            if (skillManager != null)
                skillManager.OnStatusEffectChanged -= OnStatusEffectChanged;
        }
    }
}
