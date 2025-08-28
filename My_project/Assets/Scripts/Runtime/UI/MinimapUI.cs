using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 미니맵 UI 시스템
    /// 플레이어와 몬스터, 아이템 위치를 실시간으로 표시
    /// </summary>
    public class MinimapUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RawImage minimapImage;
        [SerializeField] private Transform minimapCenter;
        [SerializeField] private Transform iconContainer;
        [SerializeField] private Button toggleButton;
        [SerializeField] private Slider zoomSlider;
        
        [Header("Minimap Settings")]
        [SerializeField] private Camera minimapCamera;
        [SerializeField] private RenderTexture minimapTexture;
        [SerializeField] private float minimapSize = 200f;
        [SerializeField] private float zoomMin = 5f;
        [SerializeField] private float zoomMax = 50f;
        [SerializeField] private float defaultZoom = 20f;
        [SerializeField] private KeyCode toggleKey = KeyCode.M;
        
        [Header("Icon Prefabs")]
        [SerializeField] private GameObject playerIconPrefab;
        [SerializeField] private GameObject monsterIconPrefab;
        [SerializeField] private GameObject itemIconPrefab;
        [SerializeField] private GameObject waypointIconPrefab;
        
        [Header("Icon Colors")]
        [SerializeField] private Color localPlayerColor = Color.blue;
        [SerializeField] private Color otherPlayerColor = Color.green;
        [SerializeField] private Color monsterColor = Color.red;
        [SerializeField] private Color itemColor = Color.yellow;
        [SerializeField] private Color waypointColor = Color.white;
        
        // 상태
        private bool isVisible = true;
        private float currentZoom;
        private Transform playerTransform;
        
        // 아이콘 관리
        private Dictionary<int, MinimapIcon> playerIcons = new Dictionary<int, MinimapIcon>();
        private Dictionary<int, MinimapIcon> monsterIcons = new Dictionary<int, MinimapIcon>();
        private Dictionary<int, MinimapIcon> itemIcons = new Dictionary<int, MinimapIcon>();
        private List<MinimapIcon> waypointIcons = new List<MinimapIcon>();
        
        // 업데이트 주기
        private float updateInterval = 0.1f;
        private float lastUpdateTime;
        
        // 이벤트
        public System.Action<bool> OnMinimapToggled;
        
        private void Start()
        {
            InitializeMinimap();
            SetupEventListeners();
        }
        
        private void Update()
        {
            HandleInput();
            UpdateMinimap();
        }
        
        /// <summary>
        /// 미니맵 초기화
        /// </summary>
        private void InitializeMinimap()
        {
            // 렌더 텍스처 생성
            if (minimapTexture == null)
            {
                minimapTexture = new RenderTexture(512, 512, 16);
                minimapTexture.Create();
            }
            
            // 미니맵 카메라 설정
            if (minimapCamera != null)
            {
                minimapCamera.targetTexture = minimapTexture;
                minimapCamera.orthographic = true;
                minimapCamera.orthographicSize = defaultZoom;
            }
            
            // 미니맵 이미지 설정
            if (minimapImage != null)
            {
                minimapImage.texture = minimapTexture;
            }
            
            // 줌 슬라이더 설정
            if (zoomSlider != null)
            {
                zoomSlider.minValue = zoomMin;
                zoomSlider.maxValue = zoomMax;
                zoomSlider.value = defaultZoom;
            }
            
            currentZoom = defaultZoom;
            
            // 로컬 플레이어 찾기
            FindLocalPlayer();
        }
        
        /// <summary>
        /// 이벤트 리스너 설정
        /// </summary>
        private void SetupEventListeners()
        {
            if (toggleButton != null)
                toggleButton.onClick.AddListener(ToggleMinimap);
                
            if (zoomSlider != null)
                zoomSlider.onValueChanged.AddListener(OnZoomChanged);
        }
        
        /// <summary>
        /// 입력 처리
        /// </summary>
        private void HandleInput()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleMinimap();
            }
        }
        
        /// <summary>
        /// 미니맵 업데이트
        /// </summary>
        private void UpdateMinimap()
        {
            if (Time.time - lastUpdateTime < updateInterval) return;
            lastUpdateTime = Time.time;
            
            if (!isVisible || playerTransform == null) return;
            
            // 카메라 위치 업데이트
            UpdateCameraPosition();
            
            // 아이콘들 업데이트
            UpdatePlayerIcons();
            UpdateMonsterIcons();
            UpdateItemIcons();
        }
        
        /// <summary>
        /// 카메라 위치 업데이트
        /// </summary>
        private void UpdateCameraPosition()
        {
            if (minimapCamera != null && playerTransform != null)
            {
                var cameraPos = minimapCamera.transform.position;
                cameraPos.x = playerTransform.position.x;
                cameraPos.z = playerTransform.position.z;
                minimapCamera.transform.position = cameraPos;
                
                minimapCamera.orthographicSize = currentZoom;
            }
        }
        
        /// <summary>
        /// 플레이어 아이콘 업데이트
        /// </summary>
        private void UpdatePlayerIcons()
        {
            var players = FindObjectsOfType<PlayerController>();
            
            // 현재 플레이어들 추적
            var currentPlayerIds = new HashSet<int>();
            
            foreach (var player in players)
            {
                if (player.NetworkObject == null) continue;
                
                int playerId = (int)player.NetworkObject.NetworkObjectId;
                currentPlayerIds.Add(playerId);
                
                if (!playerIcons.ContainsKey(playerId))
                {
                    // 새 플레이어 아이콘 생성
                    CreatePlayerIcon(player, playerId);
                }
                else
                {
                    // 기존 아이콘 위치 업데이트
                    UpdateIconPosition(playerIcons[playerId], player.transform);
                }
            }
            
            // 제거된 플레이어 아이콘 정리
            var toRemove = new List<int>();
            foreach (var kvp in playerIcons)
            {
                if (!currentPlayerIds.Contains(kvp.Key))
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (var id in toRemove)
            {
                RemovePlayerIcon(id);
            }
        }
        
        /// <summary>
        /// 몬스터 아이콘 업데이트
        /// </summary>
        private void UpdateMonsterIcons()
        {
            var monsters = FindObjectsOfType<MonsterEntity>();
            
            var currentMonsterIds = new HashSet<int>();
            
            foreach (var monster in monsters)
            {
                var networkObject = monster.GetComponent<NetworkObject>();
                if (networkObject == null || monster.CurrentHP <= 0) continue;
                
                int monsterId = (int)networkObject.NetworkObjectId;
                currentMonsterIds.Add(monsterId);
                
                if (!monsterIcons.ContainsKey(monsterId))
                {
                    CreateMonsterIcon(monster, monsterId);
                }
                else
                {
                    UpdateIconPosition(monsterIcons[monsterId], monster.transform);
                }
            }
            
            // 제거된 몬스터 아이콘 정리
            var toRemove = new List<int>();
            foreach (var kvp in monsterIcons)
            {
                if (!currentMonsterIds.Contains(kvp.Key))
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (var id in toRemove)
            {
                RemoveMonsterIcon(id);
            }
        }
        
        /// <summary>
        /// 아이템 아이콘 업데이트
        /// </summary>
        private void UpdateItemIcons()
        {
            var items = FindObjectsOfType<DroppedItem>();
            
            var currentItemIds = new HashSet<int>();
            
            foreach (var item in items)
            {
                if (item.NetworkObject == null) continue;
                
                int itemId = (int)item.NetworkObject.NetworkObjectId;
                currentItemIds.Add(itemId);
                
                if (!itemIcons.ContainsKey(itemId))
                {
                    CreateItemIcon(item, itemId);
                }
                else
                {
                    UpdateIconPosition(itemIcons[itemId], item.transform);
                }
            }
            
            // 제거된 아이템 아이콘 정리
            var toRemove = new List<int>();
            foreach (var kvp in itemIcons)
            {
                if (!currentItemIds.Contains(kvp.Key))
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (var id in toRemove)
            {
                RemoveItemIcon(id);
            }
        }
        
        /// <summary>
        /// 플레이어 아이콘 생성
        /// </summary>
        private void CreatePlayerIcon(PlayerController player, int playerId)
        {
            if (playerIconPrefab == null || iconContainer == null) return;
            
            var iconObj = Instantiate(playerIconPrefab, iconContainer);
            var minimapIcon = iconObj.GetComponent<MinimapIcon>();
            
            if (minimapIcon == null)
                minimapIcon = iconObj.AddComponent<MinimapIcon>();
            
            // 로컬 플레이어인지 확인
            bool isLocalPlayer = player.IsLocalPlayer;
            Color iconColor = isLocalPlayer ? localPlayerColor : otherPlayerColor;
            
            minimapIcon.Initialize(iconColor, player.name);
            playerIcons[playerId] = minimapIcon;
            
            UpdateIconPosition(minimapIcon, player.transform);
        }
        
        /// <summary>
        /// 몬스터 아이콘 생성
        /// </summary>
        private void CreateMonsterIcon(MonsterEntity monster, int monsterId)
        {
            if (monsterIconPrefab == null || iconContainer == null) return;
            
            var iconObj = Instantiate(monsterIconPrefab, iconContainer);
            var minimapIcon = iconObj.GetComponent<MinimapIcon>();
            
            if (minimapIcon == null)
                minimapIcon = iconObj.AddComponent<MinimapIcon>();
            
            minimapIcon.Initialize(monsterColor, monster.name);
            monsterIcons[monsterId] = minimapIcon;
            
            UpdateIconPosition(minimapIcon, monster.transform);
        }
        
        /// <summary>
        /// 아이템 아이콘 생성
        /// </summary>
        private void CreateItemIcon(DroppedItem item, int itemId)
        {
            if (itemIconPrefab == null || iconContainer == null) return;
            
            var iconObj = Instantiate(itemIconPrefab, iconContainer);
            var minimapIcon = iconObj.GetComponent<MinimapIcon>();
            
            if (minimapIcon == null)
                minimapIcon = iconObj.AddComponent<MinimapIcon>();
            
            minimapIcon.Initialize(itemColor, item.name);
            itemIcons[itemId] = minimapIcon;
            
            UpdateIconPosition(minimapIcon, item.transform);
        }
        
        /// <summary>
        /// 아이콘 위치 업데이트
        /// </summary>
        private void UpdateIconPosition(MinimapIcon icon, Transform worldTransform)
        {
            if (icon == null || worldTransform == null || playerTransform == null) return;
            
            // 월드 좌표를 미니맵 좌표로 변환
            Vector3 relativePos = worldTransform.position - playerTransform.position;
            Vector2 minimapPos = new Vector2(relativePos.x, relativePos.z);
            
            // 미니맵 크기에 맞게 스케일링
            float scale = minimapSize / (currentZoom * 2f);
            minimapPos *= scale;
            
            // 미니맵 범위 내에 있는지 확인
            bool isInRange = minimapPos.magnitude <= minimapSize * 0.5f;
            icon.SetVisible(isInRange);
            
            if (isInRange)
            {
                icon.transform.localPosition = minimapPos;
            }
        }
        
        /// <summary>
        /// 웨이포인트 추가
        /// </summary>
        public void AddWaypoint(Vector3 worldPosition, string label = "")
        {
            if (waypointIconPrefab == null || iconContainer == null) return;
            
            var iconObj = Instantiate(waypointIconPrefab, iconContainer);
            var minimapIcon = iconObj.GetComponent<MinimapIcon>();
            
            if (minimapIcon == null)
                minimapIcon = iconObj.AddComponent<MinimapIcon>();
            
            minimapIcon.Initialize(waypointColor, label);
            waypointIcons.Add(minimapIcon);
            
            // 위치 계산을 위한 임시 트랜스폼
            var tempTransform = new GameObject("TempWaypoint").transform;
            tempTransform.position = worldPosition;
            
            UpdateIconPosition(minimapIcon, tempTransform);
            
            Destroy(tempTransform.gameObject);
        }
        
        /// <summary>
        /// 모든 웨이포인트 제거
        /// </summary>
        public void ClearWaypoints()
        {
            foreach (var waypoint in waypointIcons)
            {
                if (waypoint != null)
                    Destroy(waypoint.gameObject);
            }
            waypointIcons.Clear();
        }
        
        /// <summary>
        /// 미니맵 토글
        /// </summary>
        public void ToggleMinimap()
        {
            isVisible = !isVisible;
            gameObject.SetActive(isVisible);
            OnMinimapToggled?.Invoke(isVisible);
        }
        
        /// <summary>
        /// 줌 변경 처리
        /// </summary>
        private void OnZoomChanged(float value)
        {
            currentZoom = value;
        }
        
        /// <summary>
        /// 로컬 플레이어 찾기
        /// </summary>
        private void FindLocalPlayer()
        {
            var players = FindObjectsOfType<PlayerController>();
            foreach (var player in players)
            {
                if (player.IsLocalPlayer)
                {
                    playerTransform = player.transform;
                    break;
                }
            }
        }
        
        /// <summary>
        /// 플레이어 아이콘 제거
        /// </summary>
        private void RemovePlayerIcon(int playerId)
        {
            if (playerIcons.TryGetValue(playerId, out var icon))
            {
                if (icon != null)
                    Destroy(icon.gameObject);
                playerIcons.Remove(playerId);
            }
        }
        
        /// <summary>
        /// 몬스터 아이콘 제거
        /// </summary>
        private void RemoveMonsterIcon(int monsterId)
        {
            if (monsterIcons.TryGetValue(monsterId, out var icon))
            {
                if (icon != null)
                    Destroy(icon.gameObject);
                monsterIcons.Remove(monsterId);
            }
        }
        
        /// <summary>
        /// 아이템 아이콘 제거
        /// </summary>
        private void RemoveItemIcon(int itemId)
        {
            if (itemIcons.TryGetValue(itemId, out var icon))
            {
                if (icon != null)
                    Destroy(icon.gameObject);
                itemIcons.Remove(itemId);
            }
        }
        
        /// <summary>
        /// 정리
        /// </summary>
        private void OnDestroy()
        {
            if (minimapTexture != null)
            {
                minimapTexture.Release();
            }
        }
    }
}