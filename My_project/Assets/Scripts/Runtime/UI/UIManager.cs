using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// UI 프리팹 로드 및 관리 시스템
    /// 모든 UI를 프리팹으로 관리하고 동적으로 로드/언로드
    /// </summary>
    public class UIManager : NetworkBehaviour
    {
        [Header("UI 프리팹 경로")]
        [SerializeField] private string playerHUDPrefabPath = "UI/PlayerHUD";
        [SerializeField] private string monsterTargetHUDPrefabPath = "UI/MonsterTargetHUD";
        [SerializeField] private string statsUIPrefabPath = "UI/StatsUI";
        [SerializeField] private string inventoryUIPrefabPath = "UI/AdvancedInventoryUI";
        [SerializeField] private string equipmentUIPrefabPath = "UI/EquipmentUI";
        [SerializeField] private string partyUIPrefabPath = "UI/PartyUI";
        [SerializeField] private string dungeonUIPrefabPath = "UI/DungeonUI";
        [SerializeField] private string deathUIPrefabPath = "UI/DeathUI";
        [SerializeField] private string shopUIPrefabPath = "UI/ShopUI";
        [SerializeField] private string dungeonEntryUIPrefabPath = "UI/DungeonEntryUI";
        [SerializeField] private string skillLearningUIPrefabPath = "UI/SkillLearningUI";

        [Header("UI 설정")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private bool loadUIOnStart = true;
        
        // UI 인스턴스들
        private Dictionary<System.Type, GameObject> loadedUIs = new Dictionary<System.Type, GameObject>();
        private Dictionary<System.Type, MonoBehaviour> uiComponents = new Dictionary<System.Type, MonoBehaviour>();
        
        // 싱글톤 패턴
        private static UIManager instance;
        public static UIManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<UIManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("UIManager");
                        instance = go.AddComponent<UIManager>();
                    }
                }
                return instance;
            }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // 로컬 플레이어에서만 UI 로드
            if (IsOwner)
            {
                InitializeUI();
            }
        }
        
        private void Awake()
        {
            // 싱글톤 설정
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            // Canvas 찾기
            if (mainCanvas == null)
            {
                mainCanvas = FindFirstObjectByType<Canvas>();
                if (mainCanvas == null)
                {
                    CreateMainCanvas();
                }
            }
        }
        
        public override void OnDestroy()
        {
            base.OnDestroy();
            if (instance == this)
                instance = null;
        }

        private void Start()
        {
            if (loadUIOnStart && IsOwner)
            {
                InitializeUI();
            }
        }
        
        /// <summary>
        /// 메인 Canvas 생성
        /// </summary>
        private void CreateMainCanvas()
        {
            GameObject canvasGO = new GameObject("MainCanvas");
            canvasGO.layer = LayerMask.NameToLayer("UI");
            
            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 0;
            
            var canvasScaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            DontDestroyOnLoad(canvasGO);
            Debug.Log("✅ Main Canvas created");
        }
        
        /// <summary>
        /// UI 초기화 (필수 UI들 로드)
        /// </summary>
        private void InitializeUI()
        {
            // 필수 UI들 로드
            LoadUI<PlayerHUD>(playerHUDPrefabPath);
            LoadUI<MonsterTargetHUD>(monsterTargetHUDPrefabPath);
            LoadUI<StatsUI>(statsUIPrefabPath);
            LoadUI<UnifiedInventoryUI>(inventoryUIPrefabPath);
            LoadUI<PartyUI>(partyUIPrefabPath);

            // NPC 상호작용 UI 미리 로드 (패널은 숨김 상태)
            LoadUI<ShopUI>(shopUIPrefabPath);
            LoadUI<DungeonEntryUI>(dungeonEntryUIPrefabPath);
            LoadUI<SkillLearningUI>(skillLearningUIPrefabPath);
            LoadUI<DeathUI>(deathUIPrefabPath);

            Debug.Log("🎨 Core UI systems loaded");
        }
        
        /// <summary>
        /// UI 프리팹 로드
        /// </summary>
        public T LoadUI<T>(string prefabPath) where T : MonoBehaviour
        {
            var uiType = typeof(T);
            
            // 이미 로드된 경우 기존 인스턴스 반환
            if (uiComponents.ContainsKey(uiType))
            {
                return uiComponents[uiType] as T;
            }
            
            // Resources 폴더에서 프리팹 로드
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"❌ UI prefab not found: {prefabPath}. Make sure to generate UI prefabs first!");
                return null;
            }
            
            // Canvas 하위에 인스턴스 생성
            GameObject uiInstance = Instantiate(prefab, mainCanvas.transform);
            
            // 컴포넌트 가져오기
            T uiComponent = uiInstance.GetComponent<T>();
            if (uiComponent == null)
            {
                Debug.LogError($"❌ UI Component {uiType.Name} not found on prefab: {prefabPath}");
                Destroy(uiInstance);
                return null;
            }
            
            // 딕셔너리에 저장
            loadedUIs[uiType] = uiInstance;
            uiComponents[uiType] = uiComponent;
            
            Debug.Log($"✅ UI Loaded: {uiType.Name} from {prefabPath}");
            return uiComponent;
        }
        
        /// <summary>
        /// UI 언로드
        /// </summary>
        public void UnloadUI<T>() where T : MonoBehaviour
        {
            var uiType = typeof(T);
            
            if (loadedUIs.ContainsKey(uiType))
            {
                Destroy(loadedUIs[uiType]);
                loadedUIs.Remove(uiType);
                uiComponents.Remove(uiType);
                
                Debug.Log($"🗑️ UI Unloaded: {uiType.Name}");
            }
        }
        
        /// <summary>
        /// UI 컴포넌트 가져오기
        /// </summary>
        public T GetUI<T>() where T : MonoBehaviour
        {
            var uiType = typeof(T);
            
            if (uiComponents.ContainsKey(uiType))
            {
                return uiComponents[uiType] as T;
            }
            
            return null;
        }
        
        /// <summary>
        /// UI 표시/숨김
        /// </summary>
        public void ShowUI<T>(bool show = true) where T : MonoBehaviour
        {
            var uiType = typeof(T);
            
            if (loadedUIs.ContainsKey(uiType))
            {
                loadedUIs[uiType].SetActive(show);
            }
        }
        
        /// <summary>
        /// UI 토글
        /// </summary>
        public void ToggleUI<T>() where T : MonoBehaviour
        {
            var uiType = typeof(T);
            Debug.Log($"🔍 ToggleUI called for: {uiType.Name}");
            
            if (loadedUIs.ContainsKey(uiType))
            {
                var uiObject = loadedUIs[uiType];
                var uiComponent = uiComponents[uiType];
                bool currentState = uiObject.activeInHierarchy;
                
                Debug.Log($"🔍 UI Object: {uiObject.name}, Component: {uiComponent?.GetType().Name}");
                Debug.Log($"🔍 UI Object Scale: {uiObject.transform.localScale}");
                Debug.Log($"🔍 UI Object Position: {uiObject.transform.position}");
                
                uiObject.SetActive(!currentState);
                Debug.Log($"🔍 {uiType.Name} toggled from {currentState} to {!currentState}");
            }
            else
            {
                Debug.LogError($"❌ {uiType.Name} not found in loadedUIs! Attempting to load it now...");
            }
        }
        
        /// <summary>
        /// 던전 입장 UI 로드 (포탈 상호작용 시)
        /// </summary>
        public DungeonEntryUI LoadDungeonEntryUI()
        {
            return LoadUI<DungeonEntryUI>(dungeonEntryUIPrefabPath);
        }

        /// <summary>
        /// 던전 진입 시 던전 UI 로드
        /// </summary>
        public void LoadDungeonUI()
        {
            LoadUI<DungeonUI>(dungeonUIPrefabPath);
            ShowUI<DungeonUI>(true);
        }
        
        /// <summary>
        /// 던전 종료 시 던전 UI 언로드
        /// </summary>
        public void UnloadDungeonUI()
        {
            UnloadUI<DungeonUI>();
        }
        
        /// <summary>
        /// 사망 시 데스 UI 로드
        /// </summary>
        public void LoadDeathUI()
        {
            LoadUI<DeathUI>(deathUIPrefabPath);
            ShowUI<DeathUI>(true);
        }
        
        /// <summary>
        /// 부활 시 데스 UI 언로드
        /// </summary>
        public void UnloadDeathUI()
        {
            UnloadUI<DeathUI>();
        }
        
        /// <summary>
        /// 모든 UI 숨기기 (스크린샷용)
        /// </summary>
        public void HideAllUI()
        {
            if (mainCanvas != null)
            {
                mainCanvas.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// 모든 UI 다시 표시
        /// </summary>
        public void ShowAllUI()
        {
            if (mainCanvas != null)
            {
                mainCanvas.gameObject.SetActive(true);
            }
        }
        
        /// <summary>
        /// UI 입력 처리 (PlayerController에서 호출)
        /// </summary>
        public void HandleUIInput()
        {
            // P키 - 파티
            if (Input.GetKeyDown(KeyCode.P))
            {
                ToggleUI<PartyUI>();
            }
            
            
            // B키 - 상점
            if (Input.GetKeyDown(KeyCode.B))
            {
                ToggleUI<ShopUI>();
            }
            
            // ESC키 - 모든 UI 닫기
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseAllToggleableUI();
            }
        }
        
        /// <summary>
        /// 토글 가능한 UI들 모두 닫기
        /// </summary>
        private void CloseAllToggleableUI()
        {
            ShowUI<PartyUI>(false);
            ShowUI<StatsUI>(false);
            ShowUI<ShopUI>(false);
        }
        
        /// <summary>
        /// 메모리 정리
        /// </summary>
        public override void OnNetworkDespawn()
        {
            // 모든 UI 언로드
            foreach (var uiPair in loadedUIs)
            {
                if (uiPair.Value != null)
                {
                    Destroy(uiPair.Value);
                }
            }
            
            loadedUIs.Clear();
            uiComponents.Clear();
            
            base.OnNetworkDespawn();
        }
        
        /// <summary>
        /// 디버그 정보
        /// </summary>
        [ContextMenu("Show UI Debug Info")]
        private void ShowDebugInfo()
        {
            Debug.Log($"=== UIManager Debug Info ===");
            Debug.Log($"Loaded UIs: {loadedUIs.Count}");
            Debug.Log($"Main Canvas: {(mainCanvas != null ? "Found" : "Missing")}");
            
            foreach (var uiPair in loadedUIs)
            {
                var uiObject = uiPair.Value;
                Debug.Log($"- {uiPair.Key.Name}: {(uiObject.activeInHierarchy ? "Active" : "Inactive")}");
            }
        }
    }
}