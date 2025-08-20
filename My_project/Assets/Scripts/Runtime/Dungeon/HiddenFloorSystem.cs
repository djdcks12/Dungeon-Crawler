using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 히든 11층 시스템
    /// 10층 클리어 시에만 진입 가능한 특별한 층 관리
    /// </summary>
    public class HiddenFloorSystem : NetworkBehaviour
    {
        [Header("히든 층 설정")]
        [SerializeField] private bool enableHiddenFloor = true;
        [SerializeField] private float hiddenFloorUnlockDelay = 10f; // 10층 클리어 후 10초 대기
        [SerializeField] private float hiddenFloorTimeLimit = 1800f; // 30분 제한
        
        [Header("진입 조건")]
        [SerializeField] private int requiredFloorClear = 10; // 클리어 필요 층수
        [SerializeField] private bool requireFullPartyAlive = true; // 파티 전원 생존 필요
        [SerializeField] private float minimumClearTime = 60f; // 최소 클리어 시간 (속공 방지)
        
        [Header("특별 보상")]
        [SerializeField] private float hiddenFloorExpMultiplier = 5f; // 경험치 5배
        [SerializeField] private float hiddenFloorGoldMultiplier = 10f; // 골드 10배
        [SerializeField] private float legendaryDropBonus = 0.1f; // 전설 장비 드롭률 10% 추가
        
        [Header("히든 층 특수 규칙")]
        [SerializeField] private bool disableReturnOnHiddenFloor = true; // 귀환 불가
        [SerializeField] private bool enablePermaDeathMode = true; // 영구 사망 모드
        [SerializeField] private float hiddenFloorDifficultyMultiplier = 3f; // 난이도 3배
        
        // 네트워크 변수
        private NetworkVariable<bool> hiddenFloorUnlocked = new NetworkVariable<bool>(false);
        private NetworkVariable<float> hiddenFloorRemainingTime = new NetworkVariable<float>(0f);
        private NetworkVariable<bool> hiddenFloorActive = new NetworkVariable<bool>(false);
        
        // 상태 관리
        private Dictionary<ulong, bool> playerEligibility = new Dictionary<ulong, bool>();
        private List<ulong> hiddenFloorParticipants = new List<ulong>();
        private float hiddenFloorStartTime;
        private bool hiddenFloorUnlockInProgress = false;
        
        // 진입 통계
        private Dictionary<ulong, HiddenFloorStats> playerHiddenStats = new Dictionary<ulong, HiddenFloorStats>();
        
        // 이벤트
        public System.Action OnHiddenFloorUnlocked;
        public System.Action<List<ulong>> OnHiddenFloorEntered;
        public System.Action<HiddenFloorResult> OnHiddenFloorCompleted;
        
        // 싱글톤 패턴
        private static HiddenFloorSystem instance;
        public static HiddenFloorSystem Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<HiddenFloorSystem>();
                }
                return instance;
            }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (instance == null)
            {
                instance = this;
            }
            
            if (IsServer)
            {
                // DungeonManager 이벤트 구독
                if (DungeonManager.Instance != null)
                {
                    DungeonManager.Instance.OnDungeonCompleted += OnDungeonCompleted;
                    DungeonManager.Instance.OnFloorChanged += OnFloorChanged;
                }
            }
            
            // 네트워크 변수 이벤트 구독
            hiddenFloorUnlocked.OnValueChanged += OnHiddenFloorUnlockedChanged;
            hiddenFloorActive.OnValueChanged += OnHiddenFloorActiveChanged;
        }
        
        public override void OnNetworkDespawn()
        {
            if (IsServer && DungeonManager.Instance != null)
            {
                DungeonManager.Instance.OnDungeonCompleted -= OnDungeonCompleted;
                DungeonManager.Instance.OnFloorChanged -= OnFloorChanged;
            }
            
            hiddenFloorUnlocked.OnValueChanged -= OnHiddenFloorUnlockedChanged;
            hiddenFloorActive.OnValueChanged -= OnHiddenFloorActiveChanged;
            
            base.OnNetworkDespawn();
        }
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Update()
        {
            if (IsServer && hiddenFloorActive.Value)
            {
                UpdateHiddenFloorTimer();
            }
        }
        
        /// <summary>
        /// 던전 완료 이벤트 처리
        /// </summary>
        private void OnDungeonCompleted(DungeonReward reward)
        {
            if (!IsServer || !enableHiddenFloor) return;
            
            // 10층 클리어 체크
            if (DungeonManager.Instance.CurrentFloor >= requiredFloorClear)
            {
                CheckHiddenFloorEligibility();
            }
        }
        
        /// <summary>
        /// 층 변경 이벤트 처리
        /// </summary>
        private void OnFloorChanged(int newFloor)
        {
            if (!IsServer) return;
            
            // 11층 진입 시 히든 층 활성화
            if (newFloor == 11 && hiddenFloorUnlocked.Value)
            {
                ActivateHiddenFloor();
            }
        }
        
        /// <summary>
        /// 히든 층 진입 자격 확인
        /// </summary>
        private void CheckHiddenFloorEligibility()
        {
            if (!IsServer || hiddenFloorUnlockInProgress) return;
            
            var dungeonPlayers = DungeonManager.Instance.Players;
            playerEligibility.Clear();
            
            int eligiblePlayerCount = 0;
            
            foreach (var player in dungeonPlayers)
            {
                bool isEligible = CheckPlayerEligibility(player);
                playerEligibility[player.clientId] = isEligible;
                
                if (isEligible)
                {
                    eligiblePlayerCount++;
                }
            }
            
            // 자격을 갖춘 플레이어가 있으면 히든 층 언락
            if (eligiblePlayerCount > 0)
            {
                StartHiddenFloorUnlock();
            }
            else
            {
                Debug.Log("❌ No players eligible for hidden floor access");
            }
        }
        
        /// <summary>
        /// 개별 플레이어 자격 확인
        /// </summary>
        private bool CheckPlayerEligibility(DungeonPlayer player)
        {
            // 살아있는지 확인
            if (requireFullPartyAlive)
            {
                var playerObject = NetworkManager.Singleton.ConnectedClients[player.clientId].PlayerObject;
                if (playerObject != null)
                {
                    var statsManager = playerObject.GetComponent<PlayerStatsManager>();
                    if (statsManager != null && statsManager.IsDead)
                    {
                        Debug.Log($"Player {player.clientId} is dead - not eligible for hidden floor");
                        return false;
                    }
                }
            }
            
            // 클리어 시간 확인 (속공 방지)
            float clearTime = Time.time - DungeonManager.Instance.CurrentDungeon.startTime;
            if (clearTime < minimumClearTime)
            {
                Debug.Log($"Clear time too fast ({clearTime:F1}s) - hidden floor locked");
                return false;
            }
            
            // 추가 조건들을 여기에 구현 가능
            // 예: 특정 아이템 소지, 특정 퀘스트 완료 등
            
            return true;
        }
        
        /// <summary>
        /// 히든 층 언락 시작
        /// </summary>
        private void StartHiddenFloorUnlock()
        {
            if (hiddenFloorUnlockInProgress) return;
            
            hiddenFloorUnlockInProgress = true;
            
            // 플레이어들에게 히든 층 언락 예고 알림
            NotifyHiddenFloorUnlockingClientRpc(hiddenFloorUnlockDelay);
            
            // 딜레이 후 언락
            Invoke(nameof(UnlockHiddenFloor), hiddenFloorUnlockDelay);
            
            Debug.Log($"🔓 Hidden floor unlock starting... {hiddenFloorUnlockDelay}s delay");
        }
        
        /// <summary>
        /// 히든 층 언락 실행
        /// </summary>
        private void UnlockHiddenFloor()
        {
            if (!IsServer) return;
            
            hiddenFloorUnlocked.Value = true;
            hiddenFloorUnlockInProgress = false;
            
            // 자격을 갖춘 플레이어들에게 진입 기회 제공
            List<ulong> eligiblePlayers = new List<ulong>();
            foreach (var kvp in playerEligibility)
            {
                if (kvp.Value)
                {
                    eligiblePlayers.Add(kvp.Key);
                }
            }
            
            // 히든 층 포탈 생성
            CreateHiddenFloorPortal();
            
            // 클라이언트에게 알림
            NotifyHiddenFloorUnlockedClientRpc(eligiblePlayers.ToArray());
            
            Debug.Log($"🌟 Hidden floor unlocked! {eligiblePlayers.Count} eligible players");
        }
        
        /// <summary>
        /// 히든 층 포탈 생성
        /// </summary>
        private void CreateHiddenFloorPortal()
        {
            // 던전 중앙에 특별한 포탈 생성
            Vector3 portalPosition = Vector3.zero; // 던전 중심
            
            // 포탈 프리팹이 있다면 사용, 없으면 기본 오브젝트
            GameObject portal = new GameObject("HiddenFloorPortal");
            portal.transform.position = portalPosition;
            
            // 포탈에 콜라이더와 스크립트 추가
            var collider = portal.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 2f;
            
            var portalScript = portal.AddComponent<HiddenFloorPortal>();
            portalScript.Initialize(this);
            
            // 네트워크 오브젝트로 만들기
            var networkObject = portal.AddComponent<NetworkObject>();
            networkObject.Spawn();
            
            Debug.Log($"🌀 Hidden floor portal created at {portalPosition}");
        }
        
        /// <summary>
        /// 히든 층 활성화
        /// </summary>
        private void ActivateHiddenFloor()
        {
            if (!IsServer) return;
            
            hiddenFloorActive.Value = true;
            hiddenFloorStartTime = Time.time;
            hiddenFloorRemainingTime.Value = hiddenFloorTimeLimit;
            
            // 참가자 목록 업데이트
            hiddenFloorParticipants.Clear();
            foreach (var kvp in playerEligibility)
            {
                if (kvp.Value)
                {
                    hiddenFloorParticipants.Add(kvp.Key);
                    
                    // 플레이어 히든 층 통계 초기화
                    playerHiddenStats[kvp.Key] = new HiddenFloorStats
                    {
                        entryTime = Time.time,
                        startLevel = GetPlayerLevel(kvp.Key),
                        startGold = GetPlayerGold(kvp.Key)
                    };
                }
            }
            
            // 히든 층 특수 규칙 적용
            ApplyHiddenFloorRules();
            
            // 클라이언트에게 알림
            NotifyHiddenFloorActivatedClientRpc(hiddenFloorParticipants.ToArray());
            
            Debug.Log($"🏴‍☠️ Hidden floor activated with {hiddenFloorParticipants.Count} participants");
        }
        
        /// <summary>
        /// 히든 층 특수 규칙 적용
        /// </summary>
        private void ApplyHiddenFloorRules()
        {
            // 귀환 시스템 비활성화
            if (disableReturnOnHiddenFloor && EconomySystem.Instance != null)
            {
                // EconomySystem에 히든 층 모드 설정 (구현 필요)
            }
            
            // 몬스터 난이도 증가
            var spawners = FindObjectsOfType<MonsterSpawner>();
            foreach (var spawner in spawners)
            {
                spawner.SetDifficultyMultiplier(hiddenFloorDifficultyMultiplier);
            }
            
            // PvP 강제 활성화 (영구 사망 모드)
            if (enablePermaDeathMode && PvPBalanceSystem.Instance != null)
            {
                // PvP 시스템에 영구 사망 모드 설정 (구현 필요)
            }
            
            Debug.Log("⚠️ Hidden floor special rules applied");
        }
        
        /// <summary>
        /// 히든 층 타이머 업데이트
        /// </summary>
        private void UpdateHiddenFloorTimer()
        {
            float elapsed = Time.time - hiddenFloorStartTime;
            float remaining = hiddenFloorTimeLimit - elapsed;
            
            hiddenFloorRemainingTime.Value = Mathf.Max(0f, remaining);
            
            // 시간 종료 시 강제 완료
            if (remaining <= 0f)
            {
                ForceCompleteHiddenFloor();
            }
        }
        
        /// <summary>
        /// 히든 층 강제 완료
        /// </summary>
        private void ForceCompleteHiddenFloor()
        {
            if (!IsServer) return;
            
            CompleteHiddenFloor(HiddenFloorResult.TimeUp);
        }
        
        /// <summary>
        /// 히든 층 완료 처리
        /// </summary>
        private void CompleteHiddenFloor(HiddenFloorResult result)
        {
            if (!IsServer) return;
            
            hiddenFloorActive.Value = false;
            
            // 참가자들에게 특별 보상 지급
            foreach (ulong participantId in hiddenFloorParticipants)
            {
                GrantHiddenFloorRewards(participantId, result);
            }
            
            // 결과 통계 생성
            var completionStats = GenerateHiddenFloorStats(result);
            
            // 클라이언트에게 완료 알림
            NotifyHiddenFloorCompletedClientRpc(result, completionStats);
            
            // 이벤트 호출
            OnHiddenFloorCompleted?.Invoke(result);
            
            Debug.Log($"🏆 Hidden floor completed: {result}");
        }
        
        /// <summary>
        /// 히든 층 보상 지급
        /// </summary>
        private void GrantHiddenFloorRewards(ulong playerId, HiddenFloorResult result)
        {
            var playerObject = NetworkManager.Singleton.ConnectedClients[playerId].PlayerObject;
            if (playerObject == null) return;
            
            var statsManager = playerObject.GetComponent<PlayerStatsManager>();
            if (statsManager == null) return;
            
            // 기본 보상 계산
            long baseExpReward = 10000; // 기본 1만 경험치
            long baseGoldReward = 50000; // 기본 5만 골드
            
            // 결과에 따른 배율
            float resultMultiplier = result switch
            {
                HiddenFloorResult.Victory => 1.0f,
                HiddenFloorResult.Survival => 0.7f,
                HiddenFloorResult.TimeUp => 0.5f,
                HiddenFloorResult.Defeat => 0.3f,
                _ => 0.5f
            };
            
            // 최종 보상 계산
            long finalExpReward = (long)(baseExpReward * hiddenFloorExpMultiplier * resultMultiplier);
            long finalGoldReward = (long)(baseGoldReward * hiddenFloorGoldMultiplier * resultMultiplier);
            
            // 보상 지급
            statsManager.AddExperience(finalExpReward);
            statsManager.ChangeGold(finalGoldReward);
            
            // 특별 아이템 드롭 (전설 장비 보너스)
            if (Random.value < legendaryDropBonus * resultMultiplier)
            {
                GrantLegendaryItem(playerId);
            }
            
            Debug.Log($"🎁 Hidden floor rewards: Player {playerId} received {finalExpReward} EXP, {finalGoldReward} Gold");
        }
        
        /// <summary>
        /// 전설 아이템 지급
        /// </summary>
        private void GrantLegendaryItem(ulong playerId)
        {
            // ItemDatabase에서 전설 등급 아이템 랜덤 선택
            {
                var legendaryItems = ItemDatabase.GetItemsByGrade(ItemGrade.Legendary);
                if (legendaryItems.Count > 0)
                {
                    var selectedItem = legendaryItems[Random.Range(0, legendaryItems.Count)];
                    
                    var playerObject = NetworkManager.Singleton.ConnectedClients[playerId].PlayerObject;
                    var inventoryManager = playerObject?.GetComponent<InventoryManager>();
                    
                    if (inventoryManager != null)
                    {
                        inventoryManager.AddItemServerRpc(selectedItem.ItemId, 1);
                        Debug.Log($"⭐ Legendary item granted: {selectedItem.ItemName} to player {playerId}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 플레이어 레벨 가져오기
        /// </summary>
        private int GetPlayerLevel(ulong playerId)
        {
            var playerObject = NetworkManager.Singleton.ConnectedClients[playerId].PlayerObject;
            var statsManager = playerObject?.GetComponent<PlayerStatsManager>();
            return statsManager?.CurrentStats?.CurrentLevel ?? 1;
        }
        
        /// <summary>
        /// 플레이어 골드 가져오기
        /// </summary>
        private long GetPlayerGold(ulong playerId)
        {
            var playerObject = NetworkManager.Singleton.ConnectedClients[playerId].PlayerObject;
            var statsManager = playerObject?.GetComponent<PlayerStatsManager>();
            return statsManager?.CurrentStats?.Gold ?? 0;
        }
        
        /// <summary>
        /// 히든 층 통계 생성
        /// </summary>
        private HiddenFloorCompletionStats GenerateHiddenFloorStats(HiddenFloorResult result)
        {
            return new HiddenFloorCompletionStats
            {
                result = result,
                participantCount = hiddenFloorParticipants.Count,
                completionTime = Time.time - hiddenFloorStartTime,
                survivorCount = CountSurvivors()
            };
        }
        
        /// <summary>
        /// 생존자 수 계산
        /// </summary>
        private int CountSurvivors()
        {
            int survivors = 0;
            foreach (ulong participantId in hiddenFloorParticipants)
            {
                var playerObject = NetworkManager.Singleton.ConnectedClients[participantId].PlayerObject;
                var statsManager = playerObject?.GetComponent<PlayerStatsManager>();
                
                if (statsManager != null && !statsManager.IsDead)
                {
                    survivors++;
                }
            }
            return survivors;
        }
        
        /// <summary>
        /// 히든 층 진입 (외부에서 호출)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void EnterHiddenFloorServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong playerId = rpcParams.Receive.SenderClientId;
            
            if (!hiddenFloorUnlocked.Value)
            {
                Debug.LogWarning($"Player {playerId} tried to enter locked hidden floor");
                return;
            }
            
            if (!playerEligibility.ContainsKey(playerId) || !playerEligibility[playerId])
            {
                Debug.LogWarning($"Player {playerId} is not eligible for hidden floor");
                return;
            }
            
            // 11층으로 이동
            var playerObject = NetworkManager.Singleton.ConnectedClients[playerId].PlayerObject;
            if (playerObject != null)
            {
                // 플레이어를 11층으로 텔레포트
                Vector3 hiddenFloorSpawn = new Vector3(0, 0, 0); // 히든 층 스폰 위치
                playerObject.transform.position = hiddenFloorSpawn;
                
                Debug.Log($"🌟 Player {playerId} entered hidden floor");
            }
        }
        
        // 네트워크 이벤트 처리
        private void OnHiddenFloorUnlockedChanged(bool previousValue, bool newValue)
        {
            if (newValue)
            {
                OnHiddenFloorUnlocked?.Invoke();
            }
        }
        
        private void OnHiddenFloorActiveChanged(bool previousValue, bool newValue)
        {
            if (newValue)
            {
                OnHiddenFloorEntered?.Invoke(hiddenFloorParticipants);
            }
        }
        
        // ClientRpc 메서드들
        [ClientRpc]
        private void NotifyHiddenFloorUnlockingClientRpc(float delay)
        {
            Debug.Log($"🔓 히든 층이 {delay}초 후에 해제됩니다!");
        }
        
        [ClientRpc]
        private void NotifyHiddenFloorUnlockedClientRpc(ulong[] eligiblePlayers)
        {
            Debug.Log($"🌟 히든 11층이 해제되었습니다! {eligiblePlayers.Length}명이 진입 가능합니다!");
        }
        
        [ClientRpc]
        private void NotifyHiddenFloorActivatedClientRpc(ulong[] participants)
        {
            Debug.Log($"🏴‍☠️ 히든 층 활성화! {participants.Length}명이 참가합니다!");
        }
        
        [ClientRpc]
        private void NotifyHiddenFloorCompletedClientRpc(HiddenFloorResult result, HiddenFloorCompletionStats stats)
        {
            string resultText = result switch
            {
                HiddenFloorResult.Victory => "승리!",
                HiddenFloorResult.Survival => "생존!",
                HiddenFloorResult.TimeUp => "시간 종료",
                HiddenFloorResult.Defeat => "패배",
                _ => "완료"
            };
            
            Debug.Log($"🏆 히든 층 완료: {resultText} (생존자: {stats.survivorCount}/{stats.participantCount})");
        }
        
        /// <summary>
        /// 디버그 정보
        /// </summary>
        [ContextMenu("Show Hidden Floor Debug Info")]
        private void ShowDebugInfo()
        {
            Debug.Log("=== Hidden Floor System Debug ===");
            Debug.Log($"Enabled: {enableHiddenFloor}");
            Debug.Log($"Unlocked: {hiddenFloorUnlocked.Value}");
            Debug.Log($"Active: {hiddenFloorActive.Value}");
            Debug.Log($"Eligible Players: {playerEligibility.Count}");
            Debug.Log($"Participants: {hiddenFloorParticipants.Count}");
            Debug.Log($"Remaining Time: {hiddenFloorRemainingTime.Value:F1}s");
        }
    }
    
    /// <summary>
    /// 히든 층 포탈 스크립트
    /// </summary>
    public class HiddenFloorPortal : MonoBehaviour
    {
        private HiddenFloorSystem hiddenFloorSystem;
        
        public void Initialize(HiddenFloorSystem system)
        {
            hiddenFloorSystem = system;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null && player.IsOwner)
            {
                hiddenFloorSystem.EnterHiddenFloorServerRpc();
            }
        }
    }
    
    /// <summary>
    /// 히든 층 결과 타입
    /// </summary>
    public enum HiddenFloorResult
    {
        Victory,    // 승리 (보스 처치)
        Survival,   // 생존 (시간 내 생존)
        TimeUp,     // 시간 종료
        Defeat      // 패배 (전멸)
    }
    
    /// <summary>
    /// 히든 층 플레이어 통계
    /// </summary>
    [System.Serializable]
    public struct HiddenFloorStats
    {
        public float entryTime;
        public int startLevel;
        public long startGold;
        public int monstersKilled;
        public int pvpKills;
        public float survivalTime;
    }
    
    /// <summary>
    /// 히든 층 완료 통계
    /// </summary>
    [System.Serializable]
    public struct HiddenFloorCompletionStats : INetworkSerializable
    {
        public HiddenFloorResult result;
        public int participantCount;
        public int survivorCount;
        public float completionTime;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref result);
            serializer.SerializeValue(ref participantCount);
            serializer.SerializeValue(ref survivorCount);
            serializer.SerializeValue(ref completionTime);
        }
    }
}