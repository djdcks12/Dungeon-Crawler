using UnityEngine;
using UnityEngine.UI;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 플로팅 데미지 넘버 - 데미지/힐 수치를 머리 위로 띄워서 표시
    /// 오브젝트 풀에서 재사용됨
    /// </summary>
    public class FloatingDamageNumber : MonoBehaviour
    {
        [SerializeField] private float floatSpeed = 1.5f;
        [SerializeField] private float lifetime = 1.0f;
        [SerializeField] private float fadeStartTime = 0.5f;
        [SerializeField] private float spreadRange = 0.3f;

        private Text damageText;
        private float elapsed;
        private Color baseColor;
        private Vector3 startPos;
        private Vector3 randomOffset;
        private float startScale;
        private bool isActive;

        private void Awake()
        {
            damageText = GetComponent<Text>();
        }

        public void Show(Vector3 worldPosition, float damage, bool isCritical, DamageType damageType, bool isHeal = false)
        {
            // 위치 설정 (약간의 랜덤 오프셋)
            randomOffset = new Vector3(
                Random.Range(-spreadRange, spreadRange),
                Random.Range(0f, spreadRange * 0.5f),
                0f
            );
            startPos = worldPosition + Vector3.up * 0.5f + randomOffset;
            transform.position = startPos;

            // 텍스트 설정
            string text = $"{damage:F0}";
            if (isCritical)
                text = $"{damage:F0}!";
            if (isHeal)
                text = $"+{damage:F0}";

            // 색상 결정
            if (isHeal)
            {
                baseColor = new Color(0.3f, 1f, 0.3f); // 초록
            }
            else if (isCritical)
            {
                baseColor = new Color(1f, 0.9f, 0.2f); // 노랑
            }
            else
            {
                baseColor = GetDamageColor(damageType);
            }

            // 크기 설정
            startScale = isCritical ? 1.4f : 1.0f;
            transform.localScale = Vector3.one * startScale;

            if (damageText != null)
            {
                damageText.text = text;
                damageText.color = baseColor;
                damageText.fontSize = isCritical ? 28 : 22;
            }

            elapsed = 0f;
            isActive = true;
            gameObject.SetActive(true);
        }

        private Color GetDamageColor(DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical: return Color.white;
                case DamageType.Fire: return new Color(1f, 0.4f, 0.1f);
                case DamageType.Ice: return new Color(0.5f, 0.8f, 1f);
                case DamageType.Poison: return new Color(0.6f, 0.2f, 0.8f);
                case DamageType.Holy: return new Color(1f, 1f, 0.6f);
                case DamageType.Dark: return new Color(0.5f, 0.2f, 0.5f);
                default: return Color.white;
            }
        }

        private void Update()
        {
            if (!isActive) return;

            elapsed += Time.deltaTime;

            if (elapsed >= lifetime)
            {
                isActive = false;
                gameObject.SetActive(false);
                return;
            }

            // 위로 이동
            float t = elapsed / lifetime;
            transform.position = startPos + Vector3.up * (floatSpeed * elapsed);

            // 페이드 아웃
            if (elapsed > fadeStartTime && damageText != null)
            {
                float fadeDuration = lifetime - fadeStartTime;
                float fadeT = fadeDuration > 0f ? (elapsed - fadeStartTime) / fadeDuration : 1f;
                Color c = baseColor;
                c.a = 1f - fadeT;
                damageText.color = c;
            }

            // 크기 축소
            float scaleT = Mathf.Lerp(startScale, startScale * 0.7f, t);
            transform.localScale = Vector3.one * scaleT;
        }
    }
}
