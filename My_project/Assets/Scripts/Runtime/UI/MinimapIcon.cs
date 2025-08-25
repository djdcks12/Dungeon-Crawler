using UnityEngine;
using UnityEngine.UI;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 미니맵에 표시되는 개별 아이콘 컴포넌트
    /// </summary>
    public class MinimapIcon : MonoBehaviour
    {
        [Header("Icon Components")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Text labelText;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Header("Animation Settings")]
        [SerializeField] private bool enablePulse = false;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseMinScale = 0.8f;
        [SerializeField] private float pulseMaxScale = 1.2f;
        
        // 상태
        private Color originalColor;
        private bool isVisible = true;
        private float pulseTimer = 0f;
        private Vector3 originalScale;
        
        // 레이블 정보
        private string iconLabel;
        
        private void Awake()
        {
            // 컴포넌트 자동 찾기
            if (iconImage == null)
                iconImage = GetComponent<Image>();
                
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
                
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            originalScale = transform.localScale;
        }
        
        private void Update()
        {
            if (enablePulse && isVisible)
            {
                UpdatePulseAnimation();
            }
        }
        
        /// <summary>
        /// 아이콘 초기화
        /// </summary>
        public void Initialize(Color color, string label = "")
        {
            originalColor = color;
            iconLabel = label;
            
            // 아이콘 색상 설정
            if (iconImage != null)
            {
                iconImage.color = color;
            }
            
            // 레이블 설정
            if (labelText != null)
            {
                labelText.text = label;
                labelText.gameObject.SetActive(!string.IsNullOrEmpty(label));
            }
            
            SetVisible(true);
        }
        
        /// <summary>
        /// 아이콘 색상 변경
        /// </summary>
        public void SetColor(Color color)
        {
            originalColor = color;
            if (iconImage != null)
            {
                iconImage.color = color;
            }
        }
        
        /// <summary>
        /// 레이블 변경
        /// </summary>
        public void SetLabel(string label)
        {
            iconLabel = label;
            if (labelText != null)
            {
                labelText.text = label;
                labelText.gameObject.SetActive(!string.IsNullOrEmpty(label));
            }
        }
        
        /// <summary>
        /// 표시/숨김 설정
        /// </summary>
        public void SetVisible(bool visible)
        {
            isVisible = visible;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }
        }
        
        /// <summary>
        /// 펄스 애니메이션 활성화/비활성화
        /// </summary>
        public void SetPulseEnabled(bool enabled)
        {
            enablePulse = enabled;
            if (!enabled)
            {
                // 원래 크기로 복원
                transform.localScale = originalScale;
            }
        }
        
        /// <summary>
        /// 펄스 애니메이션 업데이트
        /// </summary>
        private void UpdatePulseAnimation()
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            
            float pulseValue = Mathf.Sin(pulseTimer) * 0.5f + 0.5f; // 0~1 범위
            float scale = Mathf.Lerp(pulseMinScale, pulseMaxScale, pulseValue);
            
            transform.localScale = originalScale * scale;
        }
        
        /// <summary>
        /// 아이콘 깜빡이기 효과
        /// </summary>
        public void Blink(float duration = 1f, int blinkCount = 3)
        {
            StartCoroutine(BlinkCoroutine(duration, blinkCount));
        }
        
        /// <summary>
        /// 깜빡이기 코루틴
        /// </summary>
        private System.Collections.IEnumerator BlinkCoroutine(float duration, int blinkCount)
        {
            float blinkInterval = duration / (blinkCount * 2);
            
            for (int i = 0; i < blinkCount; i++)
            {
                // 숨김
                SetVisible(false);
                yield return new WaitForSeconds(blinkInterval);
                
                // 표시
                SetVisible(true);
                yield return new WaitForSeconds(blinkInterval);
            }
        }
        
        /// <summary>
        /// 회전 애니메이션
        /// </summary>
        public void StartRotation(float speed = 90f)
        {
            StartCoroutine(RotationCoroutine(speed));
        }
        
        /// <summary>
        /// 회전 코루틴
        /// </summary>
        private System.Collections.IEnumerator RotationCoroutine(float speed)
        {
            while (gameObject.activeInHierarchy)
            {
                transform.Rotate(0, 0, speed * Time.deltaTime);
                yield return null;
            }
        }
        
        /// <summary>
        /// 아이콘 정보 반환
        /// </summary>
        public string GetLabel()
        {
            return iconLabel;
        }
        
        /// <summary>
        /// 표시 상태 반환
        /// </summary>
        public bool IsVisible()
        {
            return isVisible;
        }
    }
}