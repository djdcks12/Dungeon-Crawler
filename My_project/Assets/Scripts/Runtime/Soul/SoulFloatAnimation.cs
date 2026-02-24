using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 영혼 부유 애니메이션 - 영혼이 공중에 떠서 부드럽게 움직이는 효과
    /// </summary>
    public class SoulFloatAnimation : MonoBehaviour
    {
        [Header("Float Settings")]
        [SerializeField] private float floatHeight = 0.5f;
        [SerializeField] private float floatSpeed = 2.0f;
        [SerializeField] private bool randomizePhase = true;
        
        [Header("Drift Settings")]
        [SerializeField] private bool enableDrift = true;
        [SerializeField] private float driftRadius = 1.0f;
        [SerializeField] private float driftSpeed = 0.8f;
        
        [Header("Rotation Settings")]
        [SerializeField] private bool enableRotation = true;
        [SerializeField] private float rotationSpeed = 30f;
        [SerializeField] private Vector3 rotationAxis = Vector3.forward;
        
        // 애니메이션 변수
        private Vector3 originalPosition;
        private float floatTimer = 0f;
        private float driftTimer = 0f;
        private float phaseOffset = 0f;
        
        // 드리프트 중심점
        private Vector3 driftCenter;
        
        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private void Start()
        {
            InitializeAnimation();
        }
        
        private void Update()
        {
            UpdateFloatAnimation();
        }
        
        /// <summary>
        /// 애니메이션 초기화
        /// </summary>
        private void InitializeAnimation()
        {
            originalPosition = transform.position;
            driftCenter = originalPosition;
            
            // 랜덤 페이즈 오프셋 (여러 영혼이 다른 타이밍에 움직이도록)
            if (randomizePhase)
            {
                phaseOffset = Random.Range(0f, Mathf.PI * 2f);
            }
        }
        
        /// <summary>
        /// 부유 애니메이션 업데이트
        /// </summary>
        private void UpdateFloatAnimation()
        {
            floatTimer += Time.deltaTime * floatSpeed;
            driftTimer += Time.deltaTime * driftSpeed;
            
            Vector3 newPosition = CalculateFloatPosition();
            
            if (enableDrift)
            {
                newPosition += CalculateDriftOffset();
            }
            
            transform.position = newPosition;
            
            if (enableRotation)
            {
                UpdateRotation();
            }
        }
        
        /// <summary>
        /// 부유 위치 계산
        /// </summary>
        private Vector3 CalculateFloatPosition()
        {
            float floatOffset = Mathf.Sin(floatTimer + phaseOffset) * floatHeight;
            return originalPosition + Vector3.up * floatOffset;
        }
        
        /// <summary>
        /// 드리프트 오프셋 계산
        /// </summary>
        private Vector3 CalculateDriftOffset()
        {
            float driftX = Mathf.Sin(driftTimer + phaseOffset) * driftRadius;
            float driftY = Mathf.Cos(driftTimer * 0.7f + phaseOffset) * (driftRadius * 0.3f);
            
            return new Vector3(driftX, driftY, 0f);
        }
        
        /// <summary>
        /// 회전 업데이트
        /// </summary>
        private void UpdateRotation()
        {
            transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
        }
        
        /// <summary>
        /// 부유 애니메이션 시작
        /// </summary>
        public void StartFloating()
        {
            enabled = true;
            InitializeAnimation();
        }
        
        /// <summary>
        /// 부유 애니메이션 정지
        /// </summary>
        public void StopFloating()
        {
            enabled = false;
            transform.position = originalPosition;
        }
        
        /// <summary>
        /// 부유 높이 설정
        /// </summary>
        public void SetFloatHeight(float height)
        {
            floatHeight = height;
        }
        
        /// <summary>
        /// 부유 속도 설정
        /// </summary>
        public void SetFloatSpeed(float speed)
        {
            floatSpeed = speed;
        }
        
        /// <summary>
        /// 드리프트 활성화/비활성화
        /// </summary>
        public void SetDriftEnabled(bool enabled)
        {
            enableDrift = enabled;
        }
        
        /// <summary>
        /// 드리프트 반경 설정
        /// </summary>
        public void SetDriftRadius(float radius)
        {
            driftRadius = radius;
        }
        
        /// <summary>
        /// 회전 활성화/비활성화
        /// </summary>
        public void SetRotationEnabled(bool enabled)
        {
            enableRotation = enabled;
        }
        
        /// <summary>
        /// 회전 속도 설정
        /// </summary>
        public void SetRotationSpeed(float speed)
        {
            rotationSpeed = speed;
        }
        
        /// <summary>
        /// 원래 위치로 부드럽게 이동
        /// </summary>
        public void ReturnToOriginalPosition(float duration = 1f)
        {
            StartCoroutine(ReturnToPositionCoroutine(duration));
        }
        
        /// <summary>
        /// 원래 위치로 이동하는 코루틴
        /// </summary>
        private System.Collections.IEnumerator ReturnToPositionCoroutine(float duration)
        {
            Vector3 startPosition = transform.position;
            float elapsed = 0f;
            
            // 애니메이션 임시 정지
            bool wasEnabled = enabled;
            enabled = false;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // 부드러운 이동 (Ease-out)
                t = 1f - (1f - t) * (1f - t);
                
                transform.position = Vector3.Lerp(startPosition, originalPosition, t);
                
                yield return null;
            }
            
            transform.position = originalPosition;
            
            // 애니메이션 재시작
            enabled = wasEnabled;
        }
        
        /// <summary>
        /// 새로운 중심점 설정
        /// </summary>
        public void SetNewCenter(Vector3 newCenter)
        {
            originalPosition = newCenter;
            driftCenter = newCenter;
        }
        
        /// <summary>
        /// 애니메이션 페이즈 리셋
        /// </summary>
        public void ResetPhase()
        {
            floatTimer = 0f;
            driftTimer = 0f;
            
            if (randomizePhase)
            {
                phaseOffset = Random.Range(0f, Mathf.PI * 2f);
            }
        }
        
        /// <summary>
        /// 영혼이 플레이어에게 끌려가는 효과
        /// </summary>
        public void AttractToPlayer(Transform playerTransform, float attractionSpeed = 5f)
        {
            StartCoroutine(AttractionCoroutine(playerTransform, attractionSpeed));
        }
        
        /// <summary>
        /// 플레이어로 끌려가는 코루틴
        /// </summary>
        private System.Collections.IEnumerator AttractionCoroutine(Transform target, float speed)
        {
            // 부유 애니메이션 정지
            enabled = false;
            
            while (target != null && Vector3.Distance(transform.position, target.position) > 0.1f)
            {
                Vector3 direction = (target.position - transform.position).normalized;
                transform.position += direction * speed * Time.deltaTime;
                
                // 회전하면서 끌려가기
                if (enableRotation)
                {
                    transform.Rotate(rotationAxis * rotationSpeed * 2f * Time.deltaTime);
                }
                
                yield return null;
            }
            
            // 목표 지점에 도달하면 사라지는 효과 등을 여기서 처리
        }
    }
}