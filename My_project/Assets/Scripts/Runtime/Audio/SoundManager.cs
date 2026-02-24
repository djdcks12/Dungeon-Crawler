using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 사운드 매니저 - BGM/SFX 관리
    /// 전투/마을/던전별 BGM 전환, 효과음 재생
    /// SettingsManager 볼륨 설정 연동
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("오디오 소스")]
        [SerializeField] private int sfxPoolSize = 10;

        // BGM
        private AudioSource bgmSource;
        private AudioSource bgmCrossfadeSource;

        // SFX 풀
        private List<AudioSource> sfxSources = new List<AudioSource>();
        private int currentSfxIndex = 0;

        // 앰비언스 (환경음)
        private AudioSource ambientSource;

        // 현재 상태
        private BGMType currentBGM = BGMType.None;
        private float masterVolume = 1f;
        private float musicVolume = 0.7f;
        private float sfxVolume = 0.8f;
        private bool isCrossfading = false;

        // Resources.Load 캐시 (반복 로드 방지)
        private readonly Dictionary<string, AudioClip> audioClipCache = new Dictionary<string, AudioClip>();

        // BGM 데이터 (리소스 경로)
        private readonly Dictionary<BGMType, string> bgmPaths = new Dictionary<BGMType, string>
        {
            { BGMType.Title, "Audio/BGM/Title" },
            { BGMType.Town, "Audio/BGM/Town" },
            { BGMType.Field, "Audio/BGM/Field" },
            { BGMType.Dungeon_Easy, "Audio/BGM/Dungeon_Easy" },
            { BGMType.Dungeon_Normal, "Audio/BGM/Dungeon_Normal" },
            { BGMType.Dungeon_Hard, "Audio/BGM/Dungeon_Hard" },
            { BGMType.Dungeon_Nightmare, "Audio/BGM/Dungeon_Nightmare" },
            { BGMType.Boss, "Audio/BGM/Boss" },
            { BGMType.Battle, "Audio/BGM/Battle" },
            { BGMType.Victory, "Audio/BGM/Victory" },
            { BGMType.Defeat, "Audio/BGM/Defeat" },
            { BGMType.Shop, "Audio/BGM/Shop" },
        };

        // SFX 캐시
        private Dictionary<string, AudioClip> sfxCache = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupAudioSources();
            LoadVolumeSettings();
        }

        private void SetupAudioSources()
        {
            // BGM 소스
            var bgmObj = new GameObject("BGM");
            bgmObj.transform.SetParent(transform);
            bgmSource = bgmObj.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.priority = 0;

            // BGM 크로스페이드용
            var crossObj = new GameObject("BGM_Crossfade");
            crossObj.transform.SetParent(transform);
            bgmCrossfadeSource = crossObj.AddComponent<AudioSource>();
            bgmCrossfadeSource.loop = true;
            bgmCrossfadeSource.playOnAwake = false;
            bgmCrossfadeSource.priority = 0;

            // 앰비언스 소스
            var ambObj = new GameObject("Ambient");
            ambObj.transform.SetParent(transform);
            ambientSource = ambObj.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.playOnAwake = false;
            ambientSource.priority = 10;

            // SFX 풀
            for (int i = 0; i < sfxPoolSize; i++)
            {
                var sfxObj = new GameObject($"SFX_{i}");
                sfxObj.transform.SetParent(transform);
                var src = sfxObj.AddComponent<AudioSource>();
                src.loop = false;
                src.playOnAwake = false;
                src.priority = 128;
                sfxSources.Add(src);
            }
        }

        private void LoadVolumeSettings()
        {
            if (SettingsManager.Instance != null)
            {
                var settings = SettingsManager.Instance.CurrentSettings;
                masterVolume = settings.masterVolume;
                musicVolume = settings.musicVolume;
                sfxVolume = settings.sfxVolume;
            }

            ApplyVolumes();
        }

        // === BGM ===

        /// <summary>
        /// BGM 재생 (크로스페이드)
        /// </summary>
        public void PlayBGM(BGMType type, float fadeTime = 1.5f)
        {
            if (type == currentBGM && bgmSource.isPlaying) return;
            if (type == BGMType.None)
            {
                StopBGM(fadeTime);
                return;
            }

            currentBGM = type;

            if (!bgmPaths.TryGetValue(type, out string path)) return;

            var clip = LoadAudioClipCached(path);
            if (clip == null)
            {
                // 클립이 없으면 로그만 출력 (리소스 없는 경우 정상 동작)
                Debug.Log($"[Sound] BGM not found: {path} (resource may not exist yet)");
                return;
            }

            if (fadeTime > 0 && bgmSource.isPlaying)
            {
                StartCoroutine(CrossfadeBGM(clip, fadeTime));
            }
            else
            {
                bgmSource.clip = clip;
                bgmSource.volume = musicVolume * masterVolume;
                bgmSource.Play();
            }
        }

        /// <summary>
        /// BGM 정지
        /// </summary>
        public void StopBGM(float fadeTime = 1f)
        {
            currentBGM = BGMType.None;

            if (fadeTime > 0)
                StartCoroutine(FadeOutBGM(fadeTime));
            else
                bgmSource.Stop();
        }

        private IEnumerator CrossfadeBGM(AudioClip newClip, float duration)
        {
            if (isCrossfading) yield break;
            isCrossfading = true;

            // 새 트랙 시작
            bgmCrossfadeSource.clip = newClip;
            bgmCrossfadeSource.volume = 0f;
            bgmCrossfadeSource.Play();

            float startVol = bgmSource.volume;
            float targetVol = musicVolume * masterVolume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, t);
                bgmCrossfadeSource.volume = Mathf.Lerp(0f, targetVol, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 스왑
            bgmSource.Stop();
            bgmSource.clip = newClip;
            bgmSource.volume = targetVol;
            bgmSource.Play();

            bgmCrossfadeSource.Stop();
            bgmCrossfadeSource.volume = 0f;

            isCrossfading = false;
        }

        private IEnumerator FadeOutBGM(float duration)
        {
            float startVol = bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            bgmSource.Stop();
            bgmSource.volume = 0f;
        }

        // === SFX ===

        /// <summary>
        /// 효과음 재생 (리소스 경로)
        /// </summary>
        public void PlaySFX(string clipName, float volumeScale = 1f)
        {
            string path = $"Audio/SFX/{clipName}";
            AudioClip clip = GetOrLoadClip(path);
            if (clip == null) return;

            PlaySFXClip(clip, volumeScale);
        }

        /// <summary>
        /// 효과음 직접 재생 (AudioClip)
        /// </summary>
        public void PlaySFXClip(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;

            var source = GetNextSFXSource();
            source.clip = clip;
            source.volume = sfxVolume * masterVolume * volumeScale;
            source.pitch = 1f;
            source.Play();
        }

        /// <summary>
        /// 랜덤 피치 효과음 (연속 타격감)
        /// </summary>
        public void PlaySFXRandomPitch(string clipName, float pitchMin = 0.9f, float pitchMax = 1.1f, float volumeScale = 1f)
        {
            string path = $"Audio/SFX/{clipName}";
            AudioClip clip = GetOrLoadClip(path);
            if (clip == null) return;

            var source = GetNextSFXSource();
            source.clip = clip;
            source.volume = sfxVolume * masterVolume * volumeScale;
            source.pitch = Random.Range(pitchMin, pitchMax);
            source.Play();
        }

        /// <summary>
        /// 위치 기반 효과음 (3D)
        /// </summary>
        public void PlaySFXAtPosition(string clipName, Vector3 position, float volumeScale = 1f)
        {
            string path = $"Audio/SFX/{clipName}";
            AudioClip clip = GetOrLoadClip(path);
            if (clip == null) return;

            AudioSource.PlayClipAtPoint(clip, position, sfxVolume * masterVolume * volumeScale);
        }

        // === 미리 정의된 SFX ===

        public void PlayAttackSFX() => PlaySFXRandomPitch("Attack_Hit", 0.85f, 1.15f);
        public void PlayCriticalSFX() => PlaySFX("Critical_Hit", 1.2f);
        public void PlaySkillSFX() => PlaySFX("Skill_Cast");
        public void PlayLevelUpSFX() => PlaySFX("LevelUp", 1.3f);
        public void PlayItemPickupSFX() => PlaySFX("Item_Pickup");
        public void PlayGoldPickupSFX() => PlaySFX("Gold_Pickup");
        public void PlayMenuClickSFX() => PlaySFX("Menu_Click", 0.7f);
        public void PlayMenuOpenSFX() => PlaySFX("Menu_Open");
        public void PlayMenuCloseSFX() => PlaySFX("Menu_Close");
        public void PlayQuestCompleteSFX() => PlaySFX("Quest_Complete", 1.2f);
        public void PlayAchievementSFX() => PlaySFX("Achievement", 1.3f);
        public void PlayEnhanceSuccessSFX() => PlaySFX("Enhance_Success");
        public void PlayEnhanceFailSFX() => PlaySFX("Enhance_Fail");
        public void PlayDeathSFX() => PlaySFX("Player_Death");
        public void PlayBossAppearSFX() => PlaySFX("Boss_Appear", 1.5f);
        public void PlayTradeSFX() => PlaySFX("Trade_Complete");
        public void PlayErrorSFX() => PlaySFX("Error", 0.8f);
        public void PlayHealSFX() => PlaySFX("Heal");
        public void PlayDodgeSFX() => PlaySFX("Dodge");
        public void PlayShieldBlockSFX() => PlaySFX("Shield_Block");

        // === 앰비언스 ===

        /// <summary>
        /// 환경음 재생
        /// </summary>
        public void PlayAmbient(string clipName, float fadeTime = 2f)
        {
            string path = $"Audio/Ambient/{clipName}";
            var clip = LoadAudioClipCached(path);
            if (clip == null) return;

            if (fadeTime > 0 && ambientSource.isPlaying)
            {
                StartCoroutine(CrossfadeAmbient(clip, fadeTime));
            }
            else
            {
                ambientSource.clip = clip;
                ambientSource.volume = musicVolume * masterVolume * 0.5f;
                ambientSource.Play();
            }
        }

        /// <summary>
        /// 환경음 정지
        /// </summary>
        public void StopAmbient(float fadeTime = 1f)
        {
            if (fadeTime > 0)
                StartCoroutine(FadeOutAmbient(fadeTime));
            else
                ambientSource.Stop();
        }

        private IEnumerator CrossfadeAmbient(AudioClip newClip, float duration)
        {
            float startVol = ambientSource.volume;
            float targetVol = musicVolume * masterVolume * 0.5f;
            float elapsed = 0f;
            float halfDur = duration * 0.5f;

            // 페이드 아웃
            while (elapsed < halfDur)
            {
                ambientSource.volume = Mathf.Lerp(startVol, 0f, elapsed / halfDur);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 새 클립
            ambientSource.clip = newClip;
            ambientSource.Play();

            // 페이드 인
            elapsed = 0f;
            while (elapsed < halfDur)
            {
                ambientSource.volume = Mathf.Lerp(0f, targetVol, elapsed / halfDur);
                elapsed += Time.deltaTime;
                yield return null;
            }
            ambientSource.volume = targetVol;
        }

        private IEnumerator FadeOutAmbient(float duration)
        {
            float startVol = ambientSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                ambientSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            ambientSource.Stop();
        }

        // === 씬 전환 BGM ===

        /// <summary>
        /// 던전 BGM 자동 선택
        /// </summary>
        public void PlayDungeonBGM(DungeonDifficulty difficulty)
        {
            switch (difficulty)
            {
                case DungeonDifficulty.Easy:
                    PlayBGM(BGMType.Dungeon_Easy);
                    break;
                case DungeonDifficulty.Normal:
                    PlayBGM(BGMType.Dungeon_Normal);
                    break;
                case DungeonDifficulty.Hard:
                    PlayBGM(BGMType.Dungeon_Hard);
                    break;
                case DungeonDifficulty.Nightmare:
                    PlayBGM(BGMType.Dungeon_Nightmare);
                    break;
                default:
                    PlayBGM(BGMType.Dungeon_Normal);
                    break;
            }
        }

        // === 볼륨 설정 ===

        /// <summary>
        /// 볼륨 설정 적용 (SettingsManager 연동)
        /// </summary>
        public void UpdateVolumes(float master, float music, float sfx)
        {
            masterVolume = master;
            musicVolume = music;
            sfxVolume = sfx;
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            float bgmVol = musicVolume * masterVolume;
            if (bgmSource != null && !isCrossfading)
                bgmSource.volume = bgmVol;

            if (ambientSource != null)
                ambientSource.volume = bgmVol * 0.5f;

            AudioListener.volume = masterVolume;
        }

        // === 유틸 ===

        private AudioSource GetNextSFXSource()
        {
            var source = sfxSources[currentSfxIndex];
            currentSfxIndex = (currentSfxIndex + 1) % sfxSources.Count;
            return source;
        }

        private AudioClip GetOrLoadClip(string path)
        {
            if (sfxCache.TryGetValue(path, out var cached))
                return cached;

            var clip = LoadAudioClipCached(path);
            if (clip != null)
                sfxCache[path] = clip;

            return clip;
        }

        /// <summary>
        /// SFX 캐시 초기화
        /// </summary>
        public void ClearSFXCache()
        {
            sfxCache.Clear();
        }

        /// <summary>
        /// 캐시된 AudioClip 로드 (Resources.Load 반복 호출 방지)
        /// </summary>
        private AudioClip LoadAudioClipCached(string path)
        {
            if (audioClipCache.TryGetValue(path, out var cached))
                return cached;

            var clip = Resources.Load<AudioClip>(path);
            if (clip != null)
                audioClipCache[path] = clip;
            return clip;
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            audioClipCache.Clear();

            if (Instance == this)
                Instance = null;
        }
    }

    public enum BGMType
    {
        None,
        Title,
        Town,
        Field,
        Dungeon_Easy,
        Dungeon_Normal,
        Dungeon_Hard,
        Dungeon_Nightmare,
        Boss,
        Battle,
        Victory,
        Defeat,
        Shop
    }
}
