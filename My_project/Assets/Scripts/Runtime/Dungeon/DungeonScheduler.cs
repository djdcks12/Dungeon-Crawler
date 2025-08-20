using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 던전 스케줄 관리자
    /// 정해진 시간에 모든 플레이어가 일괄 입장하는 시스템
    /// </summary>
    public class DungeonScheduler : NetworkBehaviour
    {
        [Header("던전 스케줄 설정")]
        [SerializeField] private float dungeonEntryInterval = 300f; // 5분마다 입장
        [SerializeField] private float preparationTime = 30f;       // 입장 준비 시간 30초
        [SerializeField] private int maxPlayersPerSession = 16;     // 세션당 최대 플레이어 수
        [SerializeField] private List<DungeonData> availableDungeons = new List<DungeonData>();
        
        // 네트워크 변수들
        private NetworkVariable<float> nextEntryTime = new NetworkVariable<float>();
        private NetworkVariable<bool> isPreparationPhase = new NetworkVariable<bool>();
        private NetworkVariable<int> currentSessionId = new NetworkVariable<int>();
        private NetworkVariable<int> selectedDungeonIndex = new NetworkVariable<int>();
        
        // 참가자 관리
        private NetworkList<ulong> registeredPlayers;
        private NetworkList<PartySpawnGroup> partySpawnGroups;
        
        // 컴포넌트 참조
        private DungeonManager dungeonManager;
        private PartyManager partyManager;
        
        // 상태 관리
        private int sessionIdCounter = 1;
        private Dictionary<ulong, bool> playerReadyStatus = new Dictionary<ulong, bool>();
        
        // 이벤트
        public System.Action<float> OnNextEntryTimeChanged;
        public System.Action<bool> OnPreparationPhaseChanged;
        public System.Action<List<ulong>> OnPlayersRegistered;
        public System.Action<int> OnDungeonSessionStarted;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // NetworkList 초기화
            registeredPlayers = new NetworkList<ulong>();
            partySpawnGroups = new NetworkList<PartySpawnGroup>();
            
            // 컴포넌트 참조
            dungeonManager = FindFirstObjectByType<DungeonManager>();
            partyManager = FindFirstObjectByType<PartyManager>();
            
            if (IsServer)
            {
                // 서버에서 첫 번째 입장 시간 설정
                SetNextEntryTime();
                selectedDungeonIndex.Value = SelectRandomDungeon();
                
                // 이벤트 구독
                nextEntryTime.OnValueChanged += OnNextEntryTimeChangedServer;
                isPreparationPhase.OnValueChanged += OnPreparationPhaseChangedServer;
            }
            
            // 클라이언트 이벤트
            nextEntryTime.OnValueChanged += OnNextEntryTimeChangedClient;
            isPreparationPhase.OnValueChanged += OnPreparationPhaseChangedClient;
            
            Debug.Log($"🕒 DungeonScheduler spawned (IsServer: {IsServer})");
        }
        
        private void Update()
        {
            if (!IsServer) return;
            
            float currentTime = Time.time;
            
            // 준비 단계가 아니고 입장 시간이 된 경우
            if (!isPreparationPhase.Value && currentTime >= nextEntryTime.Value - preparationTime)
            {
                StartPreparationPhase();
            }
            
            // 준비 단계이고 입장 시간이 된 경우
            if (isPreparationPhase.Value && currentTime >= nextEntryTime.Value)
            {
                StartDungeonSession();
            }
        }
        
        /// <summary>
        /// 다음 입장 시간 설정
        /// </summary>
        private void SetNextEntryTime()
        {
            nextEntryTime.Value = Time.time + dungeonEntryInterval;
            Debug.Log($"🕒 Next dungeon entry scheduled at: {nextEntryTime.Value:F1}s (in {dungeonEntryInterval}s)");
        }
        
        /// <summary>
        /// 준비 단계 시작
        /// </summary>
        private void StartPreparationPhase()
        {
            if (isPreparationPhase.Value) return;
            
            isPreparationPhase.Value = true;
            currentSessionId.Value = sessionIdCounter++;
            
            // 현재 접속한 모든 플레이어를 등록
            RegisterAllConnectedPlayers();
            
            // 파티 스폰 그룹 생성
            GeneratePartySpawnGroups();
            
            Debug.Log($"🚪 Preparation phase started for session {currentSessionId.Value}. {registeredPlayers.Count} players registered.");
            
            // 클라이언트에 알림
            NotifyPreparationStartedClientRpc(currentSessionId.Value, preparationTime);
        }
        
        /// <summary>
        /// 던전 세션 시작
        /// </summary>
        private void StartDungeonSession()
        {
            if (!isPreparationPhase.Value) return;
            
            Debug.Log($"🏰 Starting dungeon session {currentSessionId.Value} with {registeredPlayers.Count} players");
            
            // 선택된 던전으로 시작 (파티 스폰 그룹과 함께)
            if (dungeonManager != null && selectedDungeonIndex.Value >= 0 && selectedDungeonIndex.Value < availableDungeons.Count)
            {
                var spawnGroups = new List<PartySpawnGroup>();
                for (int i = 0; i < partySpawnGroups.Count; i++)
                {
                    spawnGroups.Add(partySpawnGroups[i]);
                }
                
                dungeonManager.StartDungeonWithSpawnGroups(selectedDungeonIndex.Value, spawnGroups);
            }
            
            // 플레이어들을 던전으로 이동
            TeleportPlayersToCarabiner();
            
            // 세션 정리
            EndPreparationPhase();
            
            // 다음 세션 예약
            SetNextEntryTime();
            selectedDungeonIndex.Value = SelectRandomDungeon();
            
            // 이벤트 알림
            OnDungeonSessionStarted?.Invoke(currentSessionId.Value);
        }
        
        /// <summary>
        /// 준비 단계 종료
        /// </summary>
        private void EndPreparationPhase()
        {
            isPreparationPhase.Value = false;
            registeredPlayers.Clear();
            partySpawnGroups.Clear();
            playerReadyStatus.Clear();
            
            Debug.Log($"✅ Preparation phase ended for session {currentSessionId.Value}");
        }
        
        /// <summary>
        /// 접속한 모든 플레이어 등록
        /// </summary>
        private void RegisterAllConnectedPlayers()
        {
            registeredPlayers.Clear();
            
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                var playerObject = client.Value.PlayerObject;
                if (playerObject != null)
                {
                    registeredPlayers.Add(client.Key);
                    playerReadyStatus[client.Key] = false; // 기본적으로 준비 안됨
                }
            }
            
            Debug.Log($"📝 Registered {registeredPlayers.Count} players for dungeon session");
        }
        
        /// <summary>
        /// 파티 스폰 그룹 생성
        /// </summary>
        private void GeneratePartySpawnGroups()
        {
            partySpawnGroups.Clear();
            var processedPlayers = new HashSet<ulong>();
            var partyGroups = new List<PartySpawnGroup>(); // 변수 범위 확장
            
            if (partyManager != null)
            {
                // 파티 그룹 처리
                partyGroups = partyManager.GeneratePartySpawnGroups();
                foreach (var group in partyGroups)
                {
                    partySpawnGroups.Add(group);
                    
                    // 처리된 플레이어 추가
                    for (int i = 0; i < group.memberCount; i++)
                    {
                        var clientId = group.GetMemberAtIndex(i);
                        processedPlayers.Add(clientId);
                    }
                }
            }
            
            // 파티에 속하지 않은 솔로 플레이어들 처리
            var soloPlayers = new List<ulong>();
            for (int i = 0; i < registeredPlayers.Count; i++)
            {
                var clientId = registeredPlayers[i];
                if (!processedPlayers.Contains(clientId))
                {
                    soloPlayers.Add(clientId);
                }
            }
            
            // 솔로 플레이어들을 개별 그룹으로 생성
            foreach (var soloPlayer in soloPlayers)
            {
                var soloGroup = new PartySpawnGroup(-1, new List<ulong> { soloPlayer });
                partySpawnGroups.Add(soloGroup);
            }
            
            Debug.Log($"🎯 Generated {partySpawnGroups.Count} spawn groups ({partyGroups.Count} parties, {soloPlayers.Count} solo players)");
        }
        
        /// <summary>
        /// 플레이어들을 동심원 존에 배치
        /// </summary>
        private void TeleportPlayersToCarabiner()
        {
            var dungeonZones = CalculateDungeonZones(partySpawnGroups.Count);
            
            for (int i = 0; i < partySpawnGroups.Count; i++)
            {
                var group = partySpawnGroups[i];
                var zone = dungeonZones[i % dungeonZones.Count];
                
                // 존별 스폰 위치 계산
                var spawnCenter = CalculateZoneSpawnCenter(zone);
                var spawnRadius = zone == 0 ? 5f : (zone == 1 ? 10f : 15f); // 중앙/내층/외층
                
                // 그룹 멤버들을 해당 존에 스폰
                var memberIds = new ulong[group.memberCount];
                for (int j = 0; j < group.memberCount; j++)
                {
                    memberIds[j] = group.GetMemberAtIndex(j);
                }
                TeleportGroupToZoneClientRpc(memberIds, spawnCenter, spawnRadius, zone);
            }
        }
        
        /// <summary>
        /// 던전 존 계산 (동심원 배치)
        /// </summary>
        private List<int> CalculateDungeonZones(int groupCount)
        {
            var zones = new List<int>();
            
            // 중앙 → 내층 → 외층 순서로 배치
            int centerCount = Mathf.Min(2, groupCount);      // 최대 2그룹 중앙
            int innerCount = Mathf.Min(4, groupCount - centerCount); // 최대 4그룹 내층
            int outerCount = groupCount - centerCount - innerCount;   // 나머지 외층
            
            // 존 배정
            for (int i = 0; i < centerCount; i++) zones.Add(0); // 중앙
            for (int i = 0; i < innerCount; i++) zones.Add(1);  // 내층  
            for (int i = 0; i < outerCount; i++) zones.Add(2);  // 외층
            
            return zones;
        }
        
        /// <summary>
        /// 존별 스폰 중심점 계산
        /// </summary>
        private Vector3 CalculateZoneSpawnCenter(int zone)
        {
            switch (zone)
            {
                case 0: // 중앙
                    return Vector3.zero;
                    
                case 1: // 내층 (반지름 20m)
                    var innerAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    var innerRadius = Random.Range(15f, 25f);
                    return new Vector3(Mathf.Cos(innerAngle) * innerRadius, Mathf.Sin(innerAngle) * innerRadius, 0);
                    
                case 2: // 외층 (반지름 40m)
                default:
                    var outerAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    var outerRadius = Random.Range(35f, 45f);
                    return new Vector3(Mathf.Cos(outerAngle) * outerRadius, Mathf.Sin(outerAngle) * outerRadius, 0);
            }
        }
        
        /// <summary>
        /// 랜덤 던전 선택
        /// </summary>
        private int SelectRandomDungeon()
        {
            if (availableDungeons == null || availableDungeons.Count == 0)
            {
                Debug.LogWarning("No available dungeons configured!");
                return -1;
            }
            
            return Random.Range(0, availableDungeons.Count);
        }
        
        /// <summary>
        /// 플레이어 던전 참가 신청
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RegisterForDungeonServerRpc(ServerRpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            
            if (!isPreparationPhase.Value)
            {
                NotifyRegistrationResultClientRpc(clientId, false, "던전 준비 단계가 아닙니다.");
                return;
            }
            
            if (!registeredPlayers.Contains(clientId))
            {
                NotifyRegistrationResultClientRpc(clientId, false, "이미 등록된 플레이어입니다.");
                return;
            }
            
            // 플레이어 준비 상태 설정
            playerReadyStatus[clientId] = true;
            NotifyRegistrationResultClientRpc(clientId, true, "던전 입장 준비 완료!");
        }
        
        // 이벤트 핸들러들
        private void OnNextEntryTimeChangedServer(float previous, float current)
        {
            Debug.Log($"⏰ Next entry time updated: {current:F1}s");
        }
        
        private void OnPreparationPhaseChangedServer(bool previous, bool current)
        {
            Debug.Log($"🚪 Preparation phase: {(current ? "Started" : "Ended")}");
        }
        
        private void OnNextEntryTimeChangedClient(float previous, float current)
        {
            OnNextEntryTimeChanged?.Invoke(current);
        }
        
        private void OnPreparationPhaseChangedClient(bool previous, bool current)
        {
            OnPreparationPhaseChanged?.Invoke(current);
        }
        
        // ClientRpc 메서드들
        [ClientRpc]
        private void NotifyPreparationStartedClientRpc(int sessionId, float remainingTime)
        {
            Debug.Log($"🚪 Dungeon preparation started! Session {sessionId}, {remainingTime:F0}s remaining");
            // 실제로는 UI에 카운트다운 표시
        }
        
        [ClientRpc]
        private void NotifyRegistrationResultClientRpc(ulong targetClientId, bool success, string message)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            
            Debug.Log($"📝 Registration result: {message} (Success: {success})");
            // 실제로는 UI에 결과 표시
        }
        
        [ClientRpc]
        private void TeleportGroupToZoneClientRpc(ulong[] memberClientIds, Vector3 spawnCenter, float spawnRadius, int zone)
        {
            var localClientId = NetworkManager.Singleton.LocalClientId;
            
            // 로컬 플레이어가 이 그룹에 속하는지 확인
            bool isInGroup = System.Array.Exists(memberClientIds, id => id == localClientId);
            if (!isInGroup) return;
            
            // 그룹 내에서의 개별 스폰 위치 계산
            var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (localPlayer != null)
            {
                // 그룹 내 랜덤 위치
                var angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                var distance = Random.Range(0f, spawnRadius);
                var spawnPosition = spawnCenter + new Vector3(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance,
                    0
                );
                
                localPlayer.transform.position = spawnPosition;
                
                Debug.Log($"🎯 Teleported to zone {zone} at {spawnPosition}");
            }
        }
        
        // 공개 API
        public float GetNextEntryTime() => nextEntryTime.Value;
        public bool IsPreparationPhase() => isPreparationPhase.Value;
        public int GetCurrentSessionId() => currentSessionId.Value;
        public float GetTimeUntilNextEntry() => Mathf.Max(0f, nextEntryTime.Value - Time.time);
        public float GetPreparationTimeRemaining() => isPreparationPhase.Value ? Mathf.Max(0f, nextEntryTime.Value - Time.time) : 0f;
        
        public string GetScheduleInfo()
        {
            if (isPreparationPhase.Value)
            {
                float remaining = GetPreparationTimeRemaining();
                return $"던전 입장 준비: {remaining:F0}초 남음";
            }
            else
            {
                float untilNext = GetTimeUntilNextEntry();
                int minutes = Mathf.FloorToInt(untilNext / 60);
                int seconds = Mathf.FloorToInt(untilNext % 60);
                return $"다음 던전 입장: {minutes:00}:{seconds:00}";
            }
        }
    }
}