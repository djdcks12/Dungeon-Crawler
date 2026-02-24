using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 던전 UI 시스템
    /// 던전 진행 상황, 플레이어 상태, 시간 등을 표시
    /// </summary>
    public class DungeonUI : MonoBehaviour
    {
        [Header("UI 패널들")]
        [SerializeField] private GameObject dungeonPanel;
        [SerializeField] private GameObject dungeonStatusPanel;
        [SerializeField] private GameObject playerListPanel;
        [SerializeField] private GameObject rewardPanel;
        
        [Header("던전 정보 UI")]
        [SerializeField] private Text dungeonNameText;
        [SerializeField] private Text currentFloorText;
        [SerializeField] private Text remainingTimeText;
        [SerializeField] private Text dungeonStateText;
        [SerializeField] private Slider timeProgressSlider;
        
        [Header("플레이어 정보 UI")]
        [SerializeField] private Text alivePlayersText;
        [SerializeField] private Transform playerListContent;
        [SerializeField] private GameObject playerListItemPrefab;
        
        [Header("진행 상황 UI")]
        [SerializeField] private Text monstersRemainingText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Slider floorProgressSlider;
        
        [Header("보상 UI")]
        [SerializeField] private Text expRewardText;
        [SerializeField] private Text goldRewardText;
        [SerializeField] private Transform itemRewardContent;
        
        // 던전 매니저 참조
        private DungeonManager dungeonManager;
        private bool isDungeonActive = false;
        
        // UI 상태
        private float originalTimeLimit;
        private int totalFloors;
        
        private void Awake()
        {
            // 기본적으로 던전 UI 숨김
            SetPanelActive(dungeonPanel, false);
            SetPanelActive(rewardPanel, false);
        }
        
        private void Start()
        {
            // 던전 매니저 찾기
            dungeonManager = FindFirstObjectByType<DungeonManager>();
            if (dungeonManager != null)
            {
                // 이벤트 구독
                dungeonManager.OnDungeonStarted += OnDungeonStarted;
                dungeonManager.OnDungeonStateChanged += OnDungeonStateChanged;
                dungeonManager.OnFloorChanged += OnFloorChanged;
                dungeonManager.OnDungeonCompleted += OnDungeonCompleted;
            }
        }
        
        private void OnDestroy()
        {
            CancelInvoke();
            // 이벤트 구독 해제
            if (dungeonManager != null)
            {
                dungeonManager.OnDungeonStarted -= OnDungeonStarted;
                dungeonManager.OnDungeonStateChanged -= OnDungeonStateChanged;
                dungeonManager.OnFloorChanged -= OnFloorChanged;
                dungeonManager.OnDungeonCompleted -= OnDungeonCompleted;
            }
        }
        
        private void Update()
        {
            if (isDungeonActive && dungeonManager != null)
            {
                UpdateDungeonInfo();
                UpdatePlayerList();
                UpdateProgress();
            }
        }
        
        /// <summary>
        /// 던전 시작 이벤트 처리
        /// </summary>
        private void OnDungeonStarted(DungeonInfo dungeonInfo)
        {
            isDungeonActive = true;
            originalTimeLimit = dungeonInfo.timeLimit;
            totalFloors = dungeonInfo.maxFloors;
            
            // UI 표시
            SetPanelActive(dungeonPanel, true);
            SetPanelActive(rewardPanel, false);
            
            // 던전 정보 설정
            SetText(dungeonNameText, dungeonInfo.GetDungeonName());
            SetText(dungeonStateText, "진행 중");
            
            Debug.Log($"🏰 Dungeon UI activated: {dungeonInfo.GetDungeonName()}");
        }
        
        /// <summary>
        /// 던전 상태 변경 이벤트 처리
        /// </summary>
        private void OnDungeonStateChanged(DungeonState newState)
        {
            string stateText = GetStateDisplayText(newState);
            SetText(dungeonStateText, stateText);
            
            switch (newState)
            {
                case DungeonState.Completed:
                    SetText(dungeonStateText, "완료!");
                    break;
                    
                case DungeonState.Failed:
                    SetText(dungeonStateText, "실패");
                    break;
                    
                case DungeonState.Abandoned:
                    SetText(dungeonStateText, "포기");
                    break;
            }
            
            // 던전이 종료되면 UI 숨김
            if (newState == DungeonState.Completed || newState == DungeonState.Failed || newState == DungeonState.Abandoned)
            {
                isDungeonActive = false;
                Invoke(nameof(HideDungeonUI), 5.0f); // 5초 후 숨김
            }
        }
        
        /// <summary>
        /// 층 변경 이벤트 처리
        /// </summary>
        private void OnFloorChanged(int newFloor)
        {
            SetText(currentFloorText, $"{newFloor} / {totalFloors}");
            SetText(objectiveText, $"{newFloor}층 - 출구를 찾으세요 (몬스터 처치 선택)");
            
            // 플로어 진행도 업데이트
            if (floorProgressSlider != null)
            {
                floorProgressSlider.value = totalFloors > 0 ? (float)newFloor / totalFloors : 0f;
            }
        }
        
        /// <summary>
        /// 던전 완료 이벤트 처리
        /// </summary>
        private void OnDungeonCompleted(DungeonReward reward)
        {
            // 보상 UI 표시
            ShowRewardUI(reward);
        }
        
        /// <summary>
        /// 던전 정보 업데이트
        /// </summary>
        private void UpdateDungeonInfo()
        {
            if (dungeonManager == null) return;
            
            // 층별 시간 업데이트 (현재 층 시간 / 총 남은 시간)
            float floorTime = dungeonManager.CurrentFloorRemainingTime;
            float totalTime = dungeonManager.TotalRemainingTime;
            
            int floorMinutes = Mathf.FloorToInt(floorTime / 60);
            int floorSeconds = Mathf.FloorToInt(floorTime % 60);
            int totalMinutes = Mathf.FloorToInt(totalTime / 60);
            int totalSeconds = Mathf.FloorToInt(totalTime % 60);
            
            SetText(remainingTimeText, $"현재층: {floorMinutes:00}:{floorSeconds:00} | 총: {totalMinutes:00}:{totalSeconds:00}");
            
            // 시간 진행 바 업데이트 (층별 시간 기준)
            if (timeProgressSlider != null && originalTimeLimit > 0)
            {
                timeProgressSlider.value = totalTime / originalTimeLimit;
            }
            
            // 현재 층 정보 업데이트
            SetText(currentFloorText, $"{dungeonManager.CurrentFloor} / {totalFloors}");
        }
        
        /// <summary>
        /// 플레이어 리스트 업데이트
        /// </summary>
        private void UpdatePlayerList()
        {
            if (dungeonManager == null) return;
            
            var players = dungeonManager.Players;
            int aliveCount = 0;
            
            foreach (var player in players)
            {
                if (player.isAlive) aliveCount++;
            }
            
            SetText(alivePlayersText, $"생존자: {aliveCount} / {players.Count}");
            
            // 플레이어 리스트 상세 정보는 필요시 구현
        }
        
        /// <summary>
        /// 진행 상황 업데이트
        /// </summary>
        private void UpdateProgress()
        {
            // 몬스터 수 정보는 실시간으로 업데이트하기 어려우므로
            // 기본적인 목표 텍스트만 표시
            SetText(objectiveText, "모든 몬스터를 처치하고 출구를 찾으세요");
        }
        
        /// <summary>
        /// 보상 UI 표시
        /// </summary>
        private void ShowRewardUI(DungeonReward reward)
        {
            SetPanelActive(rewardPanel, true);
            
            // 보상 정보 설정
            SetText(expRewardText, $"경험치: +{reward.expReward:N0}");
            SetText(goldRewardText, $"골드: +{reward.goldReward:N0}");
            
            // 아이템 보상 표시 (간단히 개수만)
            if (reward.itemRewards != null && reward.itemRewards.Count > 0)
            {
                Debug.Log($"Received {reward.itemRewards.Count} item rewards!");
            }
            
            // 완료 시간 표시
            int minutes = Mathf.FloorToInt(reward.completionTime / 60);
            int seconds = Mathf.FloorToInt(reward.completionTime % 60);
            Debug.Log($"Dungeon completed in {minutes}:{seconds:00}");
        }
        
        /// <summary>
        /// 던전 UI 숨기기
        /// </summary>
        private void HideDungeonUI()
        {
            SetPanelActive(dungeonPanel, false);
            SetPanelActive(rewardPanel, false);
            isDungeonActive = false;
        }
        
        /// <summary>
        /// 수동으로 던전 UI 토글 (D키 등)
        /// </summary>
        public void ToggleDungeonUI()
        {
            if (dungeonPanel != null)
            {
                bool isActive = dungeonPanel.activeInHierarchy;
                SetPanelActive(dungeonPanel, !isActive);
            }
        }
        
        /// <summary>
        /// 던전 포기 버튼
        /// </summary>
        public void OnAbandonDungeonClicked()
        {
            // 확인 다이얼로그 후 던전 포기 처리
            Debug.Log("Abandon dungeon requested by player");
            
            // 실제로는 DungeonManager에 포기 요청을 보내야 함
            if (dungeonManager != null)
            {
                // dungeonManager.AbandonDungeonServerRpc(); // 구현 필요
            }
        }
        
        /// <summary>
        /// 상태 표시 텍스트 변환
        /// </summary>
        private string GetStateDisplayText(DungeonState state)
        {
            switch (state)
            {
                case DungeonState.Waiting: return "대기 중";
                case DungeonState.Active: return "진행 중";
                case DungeonState.Completed: return "완료";
                case DungeonState.Failed: return "실패";
                case DungeonState.Abandoned: return "포기";
                default: return "알 수 없음";
            }
        }
        
        /// <summary>
        /// 안전한 텍스트 설정
        /// </summary>
        private void SetText(Text textComponent, string text)
        {
            if (textComponent != null)
            {
                textComponent.text = text;
            }
        }
        
        /// <summary>
        /// 안전한 패널 활성화
        /// </summary>
        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }
        
        /// <summary>
        /// 디버그용 던전 정보 표시
        /// </summary>
        [ContextMenu("Show Debug Info")]
        private void ShowDebugInfo()
        {
            if (dungeonManager != null)
            {
                Debug.Log($"=== Dungeon Debug Info ===");
                Debug.Log($"State: {dungeonManager.State}");
                Debug.Log($"Floor: {dungeonManager.CurrentFloor}");
                Debug.Log($"Time: {dungeonManager.RemainingTime}");
                Debug.Log($"Players: {dungeonManager.Players.Count}");
            }
        }
    }
}