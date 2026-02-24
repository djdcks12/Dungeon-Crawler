using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 전체 게임 플로우 관리
    /// 인증 → 캐릭터 생성 → 네트워크 연결 → 게임 시작
    /// </summary>
    public class GameFlowManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CharacterCreationUI characterCreationUI;
        [SerializeField] private NetworkTestUI networkTestUI;
        [SerializeField] private DebugUI debugUI;
        
        [Header("Flow Settings")]
        [SerializeField] private bool autoStartFlow = true;
        [SerializeField] private bool skipCharacterCreation = false; // 테스트용
        [SerializeField] private bool autoStartHost = false; // 테스트용
        
        [Header("Test Character Data")]
        [SerializeField] private string testCharacterName = "TestHero";
        [SerializeField] private Race testCharacterRace = Race.Human;
        
        // 상태 변수
        public enum GameFlowState
        {
            Initializing,
            Authentication,
            CharacterCreation,
            NetworkConnection,
            GameReady,
            InGame
        }
        
        private GameFlowState currentState = GameFlowState.Initializing;
        private CharacterData? currentCharacter;
        
        public static GameFlowManager Instance { get; private set; }
        
        // 이벤트
        public System.Action<GameFlowState> OnStateChanged;
        public System.Action<CharacterData> OnCharacterReady;
        public System.Action OnGameReady;
        
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
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private async void Start()
        {
            try
            {
                if (autoStartFlow)
                {
                    await StartGameFlow();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameFlowManager] Start failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// 게임 플로우 시작
        /// </summary>
        public async System.Threading.Tasks.Task StartGameFlow()
        {
            ChangeState(GameFlowState.Initializing);
            
            // 1단계: 인증
            await StartAuthenticationPhase();
            
            // 2단계: 캐릭터 생성
            await StartCharacterCreationPhase();
            
            // 3단계: 네트워크 연결 (수동)
            StartNetworkConnectionPhase();
        }
        
        /// <summary>
        /// 상태 변경
        /// </summary>
        private void ChangeState(GameFlowState newState)
        {
            var previousState = currentState;
            currentState = newState;
            
            Debug.Log($"🔄 Game Flow: {previousState} → {newState}");
            OnStateChanged?.Invoke(newState);
        }
        
        /// <summary>
        /// 인증 단계 시작
        /// </summary>
        private async System.Threading.Tasks.Task StartAuthenticationPhase()
        {
            ChangeState(GameFlowState.Authentication);
            
            // SimpleAuthManager를 통한 인증
            if (SimpleAuthManager.Instance != null)
            {
                bool authSuccess = await SimpleAuthManager.Instance.InitializeAuth();
                if (authSuccess)
                {
                    Debug.Log($"✅ Authentication successful: {SimpleAuthManager.Instance.GetAuthStatusInfo()}");
                }
                else
                {
                    Debug.LogError("❌ Authentication failed!");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ SimpleAuthManager not found, proceeding without authentication");
            }
        }
        
        /// <summary>
        /// 캐릭터 생성 단계 시작
        /// </summary>
        private async System.Threading.Tasks.Task StartCharacterCreationPhase()
        {
            ChangeState(GameFlowState.CharacterCreation);
            
            // 기존 세이브 데이터 확인
            var savedCharacter = LoadCharacterData();
            if (savedCharacter.HasValue)
            {
                currentCharacter = savedCharacter;
                OnCharacterCreated(currentCharacter.Value);
                Debug.Log($"Loaded saved character: {currentCharacter.Value.characterName}");
                return;
            }

            if (skipCharacterCreation)
            {
                // 테스트용 캐릭터 자동 생성
                currentCharacter = CreateTestCharacter();
                OnCharacterCreated(currentCharacter.Value);
                return;
            }
            
            // 캐릭터 생성 UI 표시
            if (characterCreationUI != null)
            {
                var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                
                CharacterCreationUI.StartCharacterCreation(
                    onCreated: (creationData) =>
                    {
                        currentCharacter = ConvertToCharacterData(creationData);
                        OnCharacterCreated(currentCharacter.Value);
                        tcs.SetResult(true);
                    },
                    onCancelled: () =>
                    {
                        Debug.Log("Character creation cancelled");
                        tcs.SetResult(false);
                    }
                );
                
                await tcs.Task;
            }
            else
            {
                Debug.LogWarning("⚠️ CharacterCreationUI not found, creating test character");
                currentCharacter = CreateTestCharacter();
                OnCharacterCreated(currentCharacter.Value);
            }
        }
        
        /// <summary>
        /// 네트워크 연결 단계 시작
        /// </summary>
        private void StartNetworkConnectionPhase()
        {
            ChangeState(GameFlowState.NetworkConnection);
            
            if (autoStartHost)
            {
                // 자동으로 Host 시작
                StartAsHost();
            }
            else
            {
                // 사용자가 Host/Client 선택할 수 있도록 UI 활성화
                if (networkTestUI != null)
                {
                    Debug.Log("🎮 Please select Host or Client to start networking");
                }
            }
        }
        
        /// <summary>
        /// 캐릭터 생성 완료 처리
        /// </summary>
        private void OnCharacterCreated(CharacterData character)
        {
            Debug.Log($"👤 Character created: {character.characterName} ({character.race})");
            
            OnCharacterReady?.Invoke(character);
            
            // 캐릭터 데이터 저장 (로컬)
            SaveCharacterData(character);
        }
        
        /// <summary>
        /// CharacterCreationData → CharacterData 변환
        /// </summary>
        private CharacterData ConvertToCharacterData(CharacterCreationData creationData)
        {
            return new CharacterData
            {
                characterName = creationData.characterName ?? "Unnamed",
                race = creationData.race,
                level = creationData.startingLevel,
                experience = 0,
                gold = (int)creationData.startingGold,
                soulBonusStats = new StatBlock(),
                creationTime = System.DateTime.Now.ToBinary()
            };
        }

        /// <summary>
        /// 테스트용 캐릭터 생성
        /// </summary>
        private CharacterData CreateTestCharacter()
        {
            var character = new CharacterData
            {
                characterName = testCharacterName,
                race = testCharacterRace,
                level = 1,
                experience = 0,
                soulBonusStats = new StatBlock(),
                creationTime = System.DateTime.Now.ToBinary()
            };
            
            Debug.Log($"🧪 Test character created: {character.characterName} ({character.race})");
            return character;
        }
        
        /// <summary>
        /// Host로 시작
        /// </summary>
        public void StartAsHost()
        {
            if (NetworkManager.Singleton != null)
            {
                if (NetworkManager.Singleton.StartHost())
                {
                    Debug.Log("🔸 Started as Host");
                    OnNetworkStarted();
                }
                else
                {
                    Debug.LogError("❌ Failed to start Host");
                }
            }
        }
        
        /// <summary>
        /// Client로 시작
        /// </summary>
        public void StartAsClient()
        {
            if (NetworkManager.Singleton != null)
            {
                if (NetworkManager.Singleton.StartClient())
                {
                    Debug.Log("🔸 Started as Client");
                    OnNetworkStarted();
                }
                else
                {
                    Debug.LogError("❌ Failed to start Client");
                }
            }
        }
        
        /// <summary>
        /// 네트워크 시작 후 처리
        /// </summary>
        private void OnNetworkStarted()
        {
            ChangeState(GameFlowState.GameReady);

            // 플레이어 스폰 시 캐릭터 데이터 적용
            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            }

            OnGameReady?.Invoke();

            // 게임 준비 완료 후 InGame 상태로 전환
            TransitionToInGame();
        }

        /// <summary>
        /// InGame 상태로 전환 - 타운 UI 활성화
        /// </summary>
        private void TransitionToInGame()
        {
            ChangeState(GameFlowState.InGame);

            Debug.Log("Game is now InGame state - Town hub active");
        }
        
        /// <summary>
        /// 클라이언트 연결 시 처리
        /// </summary>
        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"🔗 Client {clientId} connected");
            
            // 플레이어 스폰 후 캐릭터 데이터 적용
            var playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
            if (playerObject != null)
            {
                ApplyCharacterDataToPlayer(playerObject.gameObject, currentCharacter.Value);
            }
        }
        
        /// <summary>
        /// 플레이어에게 캐릭터 데이터 적용 (서버측 검증 포함)
        /// </summary>
        private void ApplyCharacterDataToPlayer(GameObject playerObject, CharacterData characterData)
        {
            if (playerObject == null) return;

            // 서버측 기본 검증
            if (!ValidateCharacterDataOnServer(characterData))
            {
                Debug.LogWarning($"Invalid character data rejected: {characterData.characterName}");
                // 기본값으로 대체
                characterData = CreateTestCharacter();
            }

            var statsManager = playerObject.GetComponent<PlayerStatsManager>();
            if (statsManager != null)
            {
                statsManager.InitializeFromCharacterData(characterData);
                Debug.Log($"Applied character data to player: {characterData.characterName}");
            }
        }

        /// <summary>
        /// 서버측 캐릭터 데이터 검증
        /// </summary>
        private bool ValidateCharacterDataOnServer(CharacterData characterData)
        {
            // 이름 검증
            if (string.IsNullOrEmpty(characterData.characterName) || characterData.characterName.Length > 20)
                return false;

            // 종족 검증
            if (characterData.race == Race.None)
                return false;

            // 레벨 범위 검증
            if (characterData.level < 1 || characterData.level > 100)
                return false;

            return true;
        }
        
        /// <summary>
        /// 캐릭터 데이터 저장
        /// </summary>
        private void SaveCharacterData(CharacterData character)
        {
            if (SimpleAuthManager.Instance != null)
            {
                string dataKey = SimpleAuthManager.Instance.GetCharacterDataKey("main");
                string jsonData = JsonUtility.ToJson(character);
                
                PlayerPrefs.SetString(dataKey, jsonData);
                PlayerPrefs.Save();
                
                Debug.Log($"💾 Character data saved: {character.characterName}");
            }
        }
        
        /// <summary>
        /// 캐릭터 데이터 로드
        /// </summary>
        public CharacterData? LoadCharacterData()
        {
            if (SimpleAuthManager.Instance != null)
            {
                string dataKey = SimpleAuthManager.Instance.GetCharacterDataKey("main");
                string jsonData = PlayerPrefs.GetString(dataKey, "");
                
                if (!string.IsNullOrEmpty(jsonData))
                {
                    try
                    {
                        var character = JsonUtility.FromJson<CharacterData>(jsonData);
                        Debug.Log($"📂 Character data loaded: {character.characterName}");
                        return character;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"❌ Failed to load character data: {e.Message}");
                    }
                }
            }
            
            return default(CharacterData?);
        }
        
        /// <summary>
        /// 게임 종료
        /// </summary>
        public void QuitGame()
        {
            // 네트워크 정리
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
            
            // 데이터 저장
            if (currentCharacter.HasValue)
            {
                SaveCharacterData(currentCharacter.Value);
            }
            
            Debug.Log("👋 Game quit");
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        
        /// <summary>
        /// 현재 상태 정보
        /// </summary>
        public string GetStateInfo()
        {
            string stateText = $"State: {currentState}";
            
            if (currentCharacter.HasValue)
            {
                stateText += $"\\nCharacter: {currentCharacter.Value.characterName} ({currentCharacter.Value.race})";
            }
            
            if (SimpleAuthManager.Instance != null)
            {
                stateText += $"\\n{SimpleAuthManager.Instance.GetAuthStatusInfo()}";
            }
            
            return stateText;
        }
        
        // 개발용 함수들
        [ContextMenu("Skip to Network Phase")]
        public void SkipToNetworkPhase()
        {
            if (!currentCharacter.HasValue)
            {
                currentCharacter = CreateTestCharacter();
            }
            StartNetworkConnectionPhase();
        }
        
        [ContextMenu("Reset Game Flow")]
        public async void ResetGameFlow()
        {
            try
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                {
                    NetworkManager.Singleton.Shutdown();
                }

                currentCharacter = null;
                await StartGameFlow();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameFlowManager] ResetGameFlow failed: {e.Message}");
            }
        }
    }
}