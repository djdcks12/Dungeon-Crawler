using UnityEngine;
using System.Collections;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 카메라 이펙트 시스템 - 화면 흔들림, 화면 플래시, 줌 효과
    /// SettingsManager의 screenShake 설정 연동
    /// </summary>
    public class CameraEffects : MonoBehaviour
    {
        public static CameraEffects Instance { get; private set; }

        [Header("화면 흔들림")]
        [SerializeField] private float defaultShakeDuration = 0.2f;
        [SerializeField] private float defaultShakeMagnitude = 0.1f;

        [Header("화면 플래시")]
        [SerializeField] private float flashDuration = 0.15f;

        private Camera mainCamera;
        private Vector3 originalCamPos;
        private bool isShaking = false;
        private Coroutine shakeCoroutine;

        // 화면 플래시용
        private Texture2D flashTexture;
        private float flashAlpha = 0f;
        private Color flashColor = Color.white;

        // 줌 효과용
        private float originalOrthoSize;
        private Coroutine zoomCoroutine;

        // 비네팅 효과용
        private Texture2D vignetteTexture;
        private float vignetteAlpha = 0f;
        private Color vignetteColor = Color.black;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CreateFlashTexture();
            CreateVignetteTexture();
        }

        private void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                originalCamPos = mainCamera.transform.localPosition;
                originalOrthoSize = mainCamera.orthographicSize;
            }
        }

        private void CreateFlashTexture()
        {
            flashTexture = new Texture2D(1, 1);
            flashTexture.SetPixel(0, 0, Color.white);
            flashTexture.Apply();
        }

        private void CreateVignetteTexture()
        {
            int size = 256;
            vignetteTexture = new Texture2D(size, size);
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                    float alpha = Mathf.Clamp01(dist * dist * 0.8f);
                    vignetteTexture.SetPixel(x, y, new Color(0, 0, 0, alpha));
                }
            }
            vignetteTexture.Apply();
        }

        // === 화면 흔들림 ===

        /// <summary>
        /// 기본 화면 흔들림 (데미지 받을 때)
        /// </summary>
        public void ShakeLight()
        {
            Shake(0.15f, 0.05f);
        }

        /// <summary>
        /// 중간 흔들림 (강한 공격)
        /// </summary>
        public void ShakeMedium()
        {
            Shake(0.25f, 0.12f);
        }

        /// <summary>
        /// 강한 흔들림 (보스 공격, 폭발)
        /// </summary>
        public void ShakeHeavy()
        {
            Shake(0.4f, 0.25f);
        }

        /// <summary>
        /// 커스텀 흔들림
        /// </summary>
        public void Shake(float duration, float magnitude)
        {
            // 설정에서 화면 흔들림 비활성화 확인
            if (SettingsManager.Instance != null && !SettingsManager.Instance.CurrentSettings.enableScreenShake)
                return;

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            if (shakeCoroutine != null)
                StopCoroutine(shakeCoroutine);

            shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, magnitude));
        }

        private IEnumerator ShakeCoroutine(float duration, float magnitude)
        {
            isShaking = true;
            originalCamPos = mainCamera.transform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float progress = elapsed / duration;
                float currentMagnitude = magnitude * (1f - progress); // 점점 약해짐

                float x = Random.Range(-1f, 1f) * currentMagnitude;
                float y = Random.Range(-1f, 1f) * currentMagnitude;

                mainCamera.transform.localPosition = originalCamPos + new Vector3(x, y, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            mainCamera.transform.localPosition = originalCamPos;
            isShaking = false;
            shakeCoroutine = null;
        }

        // === 화면 플래시 ===

        /// <summary>
        /// 흰색 플래시 (레벨업, 치명타)
        /// </summary>
        public void FlashWhite(float intensity = 0.6f)
        {
            Flash(Color.white, intensity, flashDuration);
        }

        /// <summary>
        /// 빨간 플래시 (큰 피해)
        /// </summary>
        public void FlashRed(float intensity = 0.4f)
        {
            Flash(new Color(1f, 0f, 0f, 1f), intensity, 0.2f);
        }

        /// <summary>
        /// 금색 플래시 (업적, 보상)
        /// </summary>
        public void FlashGold(float intensity = 0.5f)
        {
            Flash(new Color(1f, 0.84f, 0f, 1f), intensity, 0.3f);
        }

        /// <summary>
        /// 커스텀 플래시
        /// </summary>
        public void Flash(Color color, float intensity, float duration)
        {
            flashColor = color;
            StartCoroutine(FlashCoroutine(intensity, duration));
        }

        private IEnumerator FlashCoroutine(float intensity, float duration)
        {
            flashAlpha = intensity;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                flashAlpha = Mathf.Lerp(intensity, 0f, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            flashAlpha = 0f;
        }

        // === 줌 효과 ===

        /// <summary>
        /// 줌인 (보스 등장)
        /// </summary>
        public void ZoomIn(float targetSize, float duration)
        {
            if (mainCamera == null) return;
            if (zoomCoroutine != null)
                StopCoroutine(zoomCoroutine);
            zoomCoroutine = StartCoroutine(ZoomCoroutine(targetSize, duration));
        }

        /// <summary>
        /// 줌 복원
        /// </summary>
        public void ZoomReset(float duration = 0.5f)
        {
            if (mainCamera == null) return;
            if (zoomCoroutine != null)
                StopCoroutine(zoomCoroutine);
            zoomCoroutine = StartCoroutine(ZoomCoroutine(originalOrthoSize, duration));
        }

        /// <summary>
        /// 보스 등장 연출 (줌인 → 잠깐 대기 → 줌아웃)
        /// </summary>
        public void BossEntrance(float zoomSize = 3f, float holdTime = 1f)
        {
            StartCoroutine(BossEntranceCoroutine(zoomSize, holdTime));
        }

        private IEnumerator BossEntranceCoroutine(float zoomSize, float holdTime)
        {
            // 줌인
            yield return ZoomCoroutine(zoomSize, 0.5f);

            // 대기
            ShakeHeavy();
            FlashWhite(0.4f);
            yield return new WaitForSeconds(holdTime);

            // 줌아웃
            yield return ZoomCoroutine(originalOrthoSize, 0.8f);
        }

        private IEnumerator ZoomCoroutine(float targetSize, float duration)
        {
            float startSize = mainCamera.orthographicSize;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float eased = t * t * (3f - 2f * t); // smoothstep
                mainCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, eased);
                elapsed += Time.deltaTime;
                yield return null;
            }

            mainCamera.orthographicSize = targetSize;
            zoomCoroutine = null;
        }

        // === 레벨업 이펙트 ===

        /// <summary>
        /// 레벨업 연출 (플래시 + 흔들림 + 줌)
        /// </summary>
        public void LevelUpEffect()
        {
            FlashGold(0.5f);
            ShakeMedium();

            if (mainCamera != null)
            {
                float zoomTarget = mainCamera.orthographicSize * 0.85f;
                StartCoroutine(LevelUpZoomCoroutine(zoomTarget));
            }
        }

        private IEnumerator LevelUpZoomCoroutine(float zoomTarget)
        {
            yield return ZoomCoroutine(zoomTarget, 0.3f);
            yield return new WaitForSeconds(0.5f);
            yield return ZoomCoroutine(originalOrthoSize, 0.5f);
        }

        // === 사망 이펙트 ===

        /// <summary>
        /// 플레이어 사망 연출
        /// </summary>
        public void DeathEffect()
        {
            FlashRed(0.6f);
            ShakeHeavy();
            StartCoroutine(DeathVignetteCoroutine());
        }

        private IEnumerator DeathVignetteCoroutine()
        {
            vignetteColor = new Color(0.3f, 0f, 0f);
            float elapsed = 0f;
            float duration = 1.5f;

            // 비네팅 점점 강해짐
            while (elapsed < duration)
            {
                vignetteAlpha = Mathf.Lerp(0f, 0.7f, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            vignetteAlpha = 0.7f;

            // 유지
            yield return new WaitForSeconds(1f);

            // 사라짐
            elapsed = 0f;
            float fadeOut = 1f;
            while (elapsed < fadeOut)
            {
                vignetteAlpha = Mathf.Lerp(0.7f, 0f, elapsed / fadeOut);
                elapsed += Time.deltaTime;
                yield return null;
            }

            vignetteAlpha = 0f;
        }

        // === 크리티컬 히트 이펙트 ===

        /// <summary>
        /// 크리티컬 히트 타격감
        /// </summary>
        public void CriticalHitEffect()
        {
            FlashWhite(0.3f);
            ShakeLight();
            StartCoroutine(HitStopCoroutine(0.05f));
        }

        /// <summary>
        /// 히트 스탑 (잠깐 멈춤 효과)
        /// </summary>
        private IEnumerator HitStopCoroutine(float duration)
        {
            float originalTimeScale = Time.timeScale;
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = originalTimeScale;
        }

        // === 던전 입장 연출 ===

        /// <summary>
        /// 던전 입장 전환 효과
        /// </summary>
        public void DungeonEnterEffect()
        {
            StartCoroutine(DungeonTransitionCoroutine(true));
        }

        /// <summary>
        /// 던전 퇴장 전환 효과
        /// </summary>
        public void DungeonExitEffect()
        {
            StartCoroutine(DungeonTransitionCoroutine(false));
        }

        private IEnumerator DungeonTransitionCoroutine(bool entering)
        {
            flashColor = Color.black;

            // 페이드 아웃
            float elapsed = 0f;
            float fadeTime = 0.5f;
            while (elapsed < fadeTime)
            {
                flashAlpha = Mathf.Lerp(0f, 1f, elapsed / fadeTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
            flashAlpha = 1f;

            // 전환 시간
            yield return new WaitForSeconds(0.3f);

            // 페이드 인
            elapsed = 0f;
            while (elapsed < fadeTime)
            {
                flashAlpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
            flashAlpha = 0f;
        }

        // === OnGUI 렌더링 ===

        private void OnGUI()
        {
            // 화면 플래시
            if (flashAlpha > 0.01f)
            {
                Color c = flashColor;
                c.a = flashAlpha;
                GUI.color = c;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), flashTexture);
                GUI.color = Color.white;
            }

            // 비네팅
            if (vignetteAlpha > 0.01f)
            {
                Color c = vignetteColor;
                c.a = vignetteAlpha;
                GUI.color = c;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignetteTexture);
                GUI.color = Color.white;
            }
        }

        private void OnDestroy()
        {
            StopAllCoroutines();

            if (Instance == this)
                Instance = null;

            if (flashTexture != null) Destroy(flashTexture);
            if (vignetteTexture != null) Destroy(vignetteTexture);
        }
    }
}
