using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 게임 설정 관리자
    /// AccountSettings를 런타임에 적용하고 PlayerPrefs에 캐싱
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        // 현재 설정
        private AccountSettings currentSettings;

        // PlayerPrefs 키
        private const string KEY_MASTER_VOLUME = "Settings_MasterVolume";
        private const string KEY_MUSIC_VOLUME = "Settings_MusicVolume";
        private const string KEY_SFX_VOLUME = "Settings_SFXVolume";
        private const string KEY_VSYNC = "Settings_VSync";
        private const string KEY_TARGET_FPS = "Settings_TargetFPS";
        private const string KEY_SHOW_DAMAGE = "Settings_ShowDamage";
        private const string KEY_SCREEN_SHAKE = "Settings_ScreenShake";
        private const string KEY_LANGUAGE = "Settings_Language";

        // 이벤트
        public System.Action OnSettingsChanged;
        public System.Action<float> OnMasterVolumeChanged;
        public System.Action<float> OnMusicVolumeChanged;
        public System.Action<float> OnSFXVolumeChanged;

        public AccountSettings CurrentSettings => currentSettings;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadSettings();
            ApplySettings();
        }

        /// <summary>
        /// 설정 로드 (PlayerPrefs 우선, 없으면 기본값)
        /// </summary>
        public void LoadSettings()
        {
            currentSettings = new AccountSettings
            {
                masterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOLUME, 1f),
                musicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOLUME, 0.7f),
                sfxVolume = PlayerPrefs.GetFloat(KEY_SFX_VOLUME, 0.8f),
                enableVSync = PlayerPrefs.GetInt(KEY_VSYNC, 1) == 1,
                targetFrameRate = PlayerPrefs.GetInt(KEY_TARGET_FPS, 60),
                showDamageNumbers = PlayerPrefs.GetInt(KEY_SHOW_DAMAGE, 1) == 1,
                enableScreenShake = PlayerPrefs.GetInt(KEY_SCREEN_SHAKE, 1) == 1,
                preferredLanguage = PlayerPrefs.GetString(KEY_LANGUAGE, "Korean")
            };

            // AccountData에서 설정 병합
            if (SaveSystem.Instance != null)
            {
                var accountData = SaveSystem.Instance.LoadAccountData();
                if (accountData?.settings != null)
                {
                    // AccountData 설정이 PlayerPrefs에 없으면 사용
                    if (!PlayerPrefs.HasKey(KEY_MASTER_VOLUME))
                        currentSettings.masterVolume = accountData.settings.masterVolume;
                    if (!PlayerPrefs.HasKey(KEY_MUSIC_VOLUME))
                        currentSettings.musicVolume = accountData.settings.musicVolume;
                    if (!PlayerPrefs.HasKey(KEY_SFX_VOLUME))
                        currentSettings.sfxVolume = accountData.settings.sfxVolume;
                }
            }

            Debug.Log("[Settings] Settings loaded");
        }

        /// <summary>
        /// 설정 저장
        /// </summary>
        public void SaveSettings()
        {
            PlayerPrefs.SetFloat(KEY_MASTER_VOLUME, currentSettings.masterVolume);
            PlayerPrefs.SetFloat(KEY_MUSIC_VOLUME, currentSettings.musicVolume);
            PlayerPrefs.SetFloat(KEY_SFX_VOLUME, currentSettings.sfxVolume);
            PlayerPrefs.SetInt(KEY_VSYNC, currentSettings.enableVSync ? 1 : 0);
            PlayerPrefs.SetInt(KEY_TARGET_FPS, currentSettings.targetFrameRate);
            PlayerPrefs.SetInt(KEY_SHOW_DAMAGE, currentSettings.showDamageNumbers ? 1 : 0);
            PlayerPrefs.SetInt(KEY_SCREEN_SHAKE, currentSettings.enableScreenShake ? 1 : 0);
            PlayerPrefs.SetString(KEY_LANGUAGE, currentSettings.preferredLanguage);
            PlayerPrefs.Save();

            // AccountData에도 동기화
            if (SaveSystem.Instance != null)
            {
                if (SaveSystem.Instance != null)
                {
                    var accountData = SaveSystem.Instance.LoadAccountData();
                    if (accountData != null)
                    {
                        accountData.settings = currentSettings;
                        SaveSystem.Instance.SaveAccountData(accountData);
                    }
                }
            }

            OnSettingsChanged?.Invoke();
            Debug.Log("[Settings] Settings saved");
        }

        /// <summary>
        /// 설정을 런타임에 적용
        /// </summary>
        public void ApplySettings()
        {
            // VSync
            QualitySettings.vSyncCount = currentSettings.enableVSync ? 1 : 0;

            // 프레임 제한
            if (!currentSettings.enableVSync)
            {
                Application.targetFrameRate = currentSettings.targetFrameRate;
            }

            // 오디오 볼륨
            AudioListener.volume = currentSettings.masterVolume;

            Debug.Log($"[Settings] Applied: VSync={currentSettings.enableVSync}, FPS={currentSettings.targetFrameRate}, Volume={currentSettings.masterVolume:F1}");
        }

        // === 개별 설정 변경 메서드 ===

        public void SetMasterVolume(float volume)
        {
            currentSettings.masterVolume = Mathf.Clamp01(volume);
            AudioListener.volume = currentSettings.masterVolume;
            OnMasterVolumeChanged?.Invoke(currentSettings.masterVolume);
            SaveSettings();
        }

        public void SetMusicVolume(float volume)
        {
            currentSettings.musicVolume = Mathf.Clamp01(volume);
            OnMusicVolumeChanged?.Invoke(currentSettings.musicVolume);
            SaveSettings();
        }

        public void SetSFXVolume(float volume)
        {
            currentSettings.sfxVolume = Mathf.Clamp01(volume);
            OnSFXVolumeChanged?.Invoke(currentSettings.sfxVolume);
            SaveSettings();
        }

        public void SetVSync(bool enabled)
        {
            currentSettings.enableVSync = enabled;
            QualitySettings.vSyncCount = enabled ? 1 : 0;
            if (!enabled)
                Application.targetFrameRate = currentSettings.targetFrameRate;
            SaveSettings();
        }

        public void SetTargetFrameRate(int fps)
        {
            currentSettings.targetFrameRate = Mathf.Clamp(fps, 30, 240);
            if (!currentSettings.enableVSync)
                Application.targetFrameRate = currentSettings.targetFrameRate;
            SaveSettings();
        }

        public void SetShowDamageNumbers(bool show)
        {
            currentSettings.showDamageNumbers = show;
            SaveSettings();
        }

        public void SetScreenShake(bool enabled)
        {
            currentSettings.enableScreenShake = enabled;
            SaveSettings();
        }

        /// <summary>
        /// 설정 초기화
        /// </summary>
        public void ResetToDefaults()
        {
            currentSettings = new AccountSettings();
            ApplySettings();
            SaveSettings();
            Debug.Log("[Settings] Reset to defaults");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                OnSettingsChanged = null;
                OnMasterVolumeChanged = null;
                OnMusicVolumeChanged = null;
                OnSFXVolumeChanged = null;
                Instance = null;
            }
        }
    }
}
