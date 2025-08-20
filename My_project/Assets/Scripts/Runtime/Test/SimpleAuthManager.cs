using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 간단한 인증 관리자
    /// 개발 단계에서 Unity Authentication 또는 로컬 인증 처리
    /// </summary>
    public class SimpleAuthManager : MonoBehaviour
    {
        [Header("Authentication Settings")]
        [SerializeField] private bool useUnityAuthentication = true;
        [SerializeField] private bool autoSignIn = true;
        [SerializeField] private string testPlayerName = "TestPlayer";
        
        [Header("Development Settings")]
        [SerializeField] private bool developmentMode = true;
        [SerializeField] private string developmentPlayerId = "";
        
        public static SimpleAuthManager Instance { get; private set; }
        
        // 이벤트
        public System.Action<bool> OnAuthenticationComplete;
        public System.Action<string> OnAuthenticationError;
        
        // 프로퍼티
        public bool IsAuthenticated { get; private set; }
        public string PlayerId { get; private set; } = "";
        public string PlayerName { get; private set; } = "";
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        
        private async void Start()
        {
            if (autoSignIn)
            {
                await InitializeAuth();
            }
        }
        
        /// <summary>
        /// 인증 초기화
        /// </summary>
        public async Task<bool> InitializeAuth()
        {
            try
            {
                if (developmentMode)
                {
                    return InitializeDevelopmentAuth();
                }
                
                if (useUnityAuthentication)
                {
                    return await InitializeUnityAuth();
                }
                else
                {
                    return InitializeLocalAuth();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Authentication failed: {e.Message}");
                OnAuthenticationError?.Invoke(e.Message);
                return false;
            }
        }
        
        /// <summary>
        /// 개발용 인증 (빠른 테스트)
        /// </summary>
        private bool InitializeDevelopmentAuth()
        {
            PlayerId = string.IsNullOrEmpty(developmentPlayerId) ? 
                       $"dev_player_{System.DateTime.Now.Ticks}" : 
                       developmentPlayerId;
            PlayerName = testPlayerName;
            IsAuthenticated = true;
            
            Debug.Log($"🧪 Development Auth: {PlayerName} ({PlayerId})");
            OnAuthenticationComplete?.Invoke(true);
            return true;
        }
        
        /// <summary>
        /// Unity Authentication 사용
        /// </summary>
        private async Task<bool> InitializeUnityAuth()
        {
            try
            {
                // Unity Services 초기화
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    await UnityServices.InitializeAsync();
                }
                
                // 이미 로그인되어 있다면 스킵
                if (AuthenticationService.Instance.IsSignedIn)
                {
                    PlayerId = AuthenticationService.Instance.PlayerId;
                    PlayerName = GetStoredPlayerName();
                    IsAuthenticated = true;
                    
                    Debug.Log($"🔐 Already signed in: {PlayerName} ({PlayerId})");
                    OnAuthenticationComplete?.Invoke(true);
                    return true;
                }
                
                // 익명 로그인
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                
                PlayerId = AuthenticationService.Instance.PlayerId;
                PlayerName = GetOrCreatePlayerName();
                IsAuthenticated = true;
                
                Debug.Log($"🔐 Unity Auth successful: {PlayerName} ({PlayerId})");
                OnAuthenticationComplete?.Invoke(true);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Unity Authentication failed: {e.Message}");
                
                // Unity Auth 실패시 로컬 인증으로 폴백
                Debug.Log("🔄 Falling back to local authentication...");
                return InitializeLocalAuth();
            }
        }
        
        /// <summary>
        /// 로컬 인증 (PlayerPrefs 사용)
        /// </summary>
        private bool InitializeLocalAuth()
        {
            PlayerId = PlayerPrefs.GetString("LocalPlayerId", "");
            
            if (string.IsNullOrEmpty(PlayerId))
            {
                PlayerId = $"local_{System.Guid.NewGuid().ToString("N")[0..8]}";
                PlayerPrefs.SetString("LocalPlayerId", PlayerId);
            }
            
            PlayerName = PlayerPrefs.GetString("LocalPlayerName", testPlayerName);
            IsAuthenticated = true;
            
            Debug.Log($"💾 Local Auth: {PlayerName} ({PlayerId})");
            OnAuthenticationComplete?.Invoke(true);
            return true;
        }
        
        /// <summary>
        /// 저장된 플레이어 이름 가져오기
        /// </summary>
        private string GetStoredPlayerName()
        {
            return PlayerPrefs.GetString($"PlayerName_{PlayerId}", $"Player_{PlayerId[0..4]}");
        }
        
        /// <summary>
        /// 플레이어 이름 생성 또는 가져오기
        /// </summary>
        private string GetOrCreatePlayerName()
        {
            string storedName = GetStoredPlayerName();
            if (!string.IsNullOrEmpty(storedName) && storedName != testPlayerName)
            {
                return storedName;
            }
            
            // 새로운 이름 생성
            string newName = $"{testPlayerName}_{PlayerId[0..4]}";
            SetPlayerName(newName);
            return newName;
        }
        
        /// <summary>
        /// 플레이어 이름 설정
        /// </summary>
        public void SetPlayerName(string newName)
        {
            if (string.IsNullOrEmpty(newName)) return;
            
            PlayerName = newName;
            PlayerPrefs.SetString($"PlayerName_{PlayerId}", PlayerName);
            PlayerPrefs.Save();
            
            Debug.Log($"✏️ Player name updated: {PlayerName}");
        }
        
        /// <summary>
        /// 로그아웃
        /// </summary>
        public async Task<bool> SignOut()
        {
            try
            {
                if (useUnityAuthentication && AuthenticationService.Instance.IsSignedIn)
                {
                    AuthenticationService.Instance.SignOut();
                }
                
                IsAuthenticated = false;
                PlayerId = "";
                PlayerName = "";
                
                Debug.Log("🚪 Signed out successfully");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Sign out failed: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 인증 상태 정보 가져오기
        /// </summary>
        public string GetAuthStatusInfo()
        {
            if (!IsAuthenticated)
                return "❌ Not Authenticated";
                
            string authType = developmentMode ? "DEV" : 
                             useUnityAuthentication ? "UNITY" : "LOCAL";
                             
            return $"✅ {authType}: {PlayerName} ({PlayerId[0..8]}...)";
        }
        
        /// <summary>
        /// 캐릭터 데이터 키 생성
        /// </summary>
        public string GetCharacterDataKey(string suffix = "")
        {
            string key = $"CharacterData_{PlayerId}";
            if (!string.IsNullOrEmpty(suffix))
                key += $"_{suffix}";
            return key;
        }
        
        /// <summary>
        /// 개발용 함수들
        /// </summary>
        [ContextMenu("Reset Authentication")]
        public async void ResetAuthentication()
        {
            await SignOut();
            
            // 로컬 데이터 삭제
            PlayerPrefs.DeleteKey("LocalPlayerId");
            PlayerPrefs.DeleteKey($"PlayerName_{PlayerId}");
            
            // 재인증
            await InitializeAuth();
        }
        
        [ContextMenu("Generate New Development ID")]
        public void GenerateNewDevelopmentId()
        {
            if (developmentMode)
            {
                developmentPlayerId = $"dev_player_{System.DateTime.Now.Ticks}";
                Debug.Log($"🔄 New development ID: {developmentPlayerId}");
            }
        }
        
        private void OnValidate()
        {
            // Inspector에서 설정 변경 시 개발용 ID 자동 생성
            if (developmentMode && string.IsNullOrEmpty(developmentPlayerId))
            {
                developmentPlayerId = $"dev_player_{System.DateTime.Now.Ticks % 100000}";
            }
        }
    }
}