using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 던전 시스템 총괄 관리자
    /// 던전 생성, 플레이어 관리, 층 이동, 완료 처리 등을 담당
    /// </summary>
    public class DungeonManager : NetworkBehaviour
    {
        [Header("던전 설정")]
        [SerializeField] private List<DungeonData> availableDungeons = new List<DungeonData>();
        [SerializeField] private Transform dungeonRoot;
        [SerializeField] private GameObject dungeonExitPrefab;
        [SerializeField] private float floorTransitionTime = 5.0f;
        
        // 네트워크 변수들
        private NetworkVariable<DungeonInfo> currentDungeon = new NetworkVariable<DungeonInfo>();
        private NetworkVariable<DungeonState> dungeonState = new NetworkVariable<DungeonState>(DungeonState.Waiting);
        private NetworkVariable<float> remainingTime = new NetworkVariable<float>();
        private NetworkVariable<int> currentFloor = new NetworkVariable<int>(1);
        
        // 층별 시간 관리
        private NetworkVariable<float> currentFloorRemainingTime = new NetworkVariable<float>();
        private NetworkVariable<float> totalRemainingTime = new NetworkVariable<float>();
        
        // 던전 참가자 관리
        private NetworkList<DungeonPlayer> dungeonPlayers;
        
        // 컴포넌트 참조
        private List<MonsterSpawner> activeSpawners = new List<MonsterSpawner>();
        private List<GameObject> currentFloorObjects = new List<GameObject>();
        
        // 던전 진행 상태
        private float dungeonStartTime;
        private float currentFloorStartTime;
        private int totalMonstersKilled;
        private Dictionary<ulong, DungeonPlayer> playerStats = new Dictionary<ulong, DungeonPlayer>();
        private Dictionary<int, float> floorTimeAllocations = new Dictionary<int, float>(); // 층별 제한시간
        
        // 이벤트
        public System.Action<DungeonInfo> OnDungeonStarted;
        public System.Action<int> OnFloorChanged;
        public System.Action<DungeonReward> OnDungeonCompleted;
        public System.Action<DungeonState> OnDungeonStateChanged;
        
        // 프로퍼티
        public DungeonInfo CurrentDungeon => currentDungeon.Value;
        public DungeonState State => dungeonState.Value;
        public int CurrentFloor => currentFloor.Value;
        public float RemainingTime => remainingTime.Value;
        public float CurrentFloorRemainingTime => currentFloorRemainingTime.Value;
        public float TotalRemainingTime => totalRemainingTime.Value;
        public bool IsActive => dungeonState.Value == DungeonState.Active;
        public List<DungeonPlayer> Players 
        { 
            get 
            { 
                var players = new List<DungeonPlayer>();
                if (dungeonPlayers != null)
                {
                    for (int i = 0; i < dungeonPlayers.Count; i++)
                    {
                        players.Add(dungeonPlayers[i]);
                    }
                }
                return players;
            } 
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // NetworkList 초기화
            dungeonPlayers = new NetworkList<DungeonPlayer>();
            
            // 서버에서만 기본 던전 데이터 초기화
            if (IsServer)
            {
                InitializeDefaultDungeons();
                dungeonState.OnValueChanged += OnStateChangedServer;
                currentFloor.OnValueChanged += OnFloorChangedServer;
            }
            
            dungeonState.OnValueChanged += OnStateChangedClient;
            currentFloor.OnValueChanged += OnFloorChangedClient;
            
            Debug.Log($"DungeonManager spawned (IsServer: {IsServer})");
        }
        
        public override void OnNetworkDespawn()
        {
            // 이벤트 구독 해제
            if (IsServer)
            {
                dungeonState.OnValueChanged -= OnStateChangedServer;
                currentFloor.OnValueChanged -= OnFloorChangedServer;
            }
            
            dungeonState.OnValueChanged -= OnStateChangedClient;
            currentFloor.OnValueChanged -= OnFloorChangedClient;
            
            base.OnNetworkDespawn();
        }
        
        private void Update()
        {
            if (!IsServer) return;
            
            // 던전 시간 관리
            if (dungeonState.Value == DungeonState.Active)
            {
                // 층별 시간 감소
                currentFloorRemainingTime.Value -= Time.deltaTime;
                totalRemainingTime.Value -= Time.deltaTime;
                
                // 총 던전 시간 초과 체크
                if (totalRemainingTime.Value <= 0)
                {
                    ForceEjectAllPlayersServerRpc("던전 총 제한시간 초과");
                    return;
                }
                
                // 현재 층 시간 초과 체크
                if (currentFloorRemainingTime.Value <= 0)
                {
                    ForceEjectAllPlayersServerRpc($"{currentFloor.Value}층 제한시간 초과");
                    return;
                }
                
                // 기존 remainingTime은 현재 층 시간으로 동기화
                remainingTime.Value = currentFloorRemainingTime.Value;
            }
        }
        
        /// <summary>
        /// 기본 던전 데이터들 초기화
        /// </summary>
        private void InitializeDefaultDungeons()
        {
            if (availableDungeons == null)
            {
                availableDungeons = new List<DungeonData>();
            }
            
            // 기본 던전들이 없으면 생성
            if (availableDungeons.Count == 0)
            {
                Debug.Log("🏗️ Creating default dungeons...");
                
                // 기본 던전들 생성
                availableDungeons.Add(DungeonCreator.CreateBeginnerDungeon());
                availableDungeons.Add(DungeonCreator.CreateIntermediateDungeon());
                availableDungeons.Add(DungeonCreator.CreateAdvancedDungeon());
                availableDungeons.Add(DungeonCreator.CreateNightmareDungeon());
                availableDungeons.Add(DungeonCreator.CreatePvPDungeon());
                
                Debug.Log($"✅ Created {availableDungeons.Count} default dungeons");
                
                // 던전 정보 출력
                for (int i = 0; i < availableDungeons.Count; i++)
                {
                    var dungeon = availableDungeons[i];
                    Debug.Log($"  [{i}] {dungeon.DungeonName} - {dungeon.MaxFloors}층, {dungeon.Difficulty} 난이도");
                }
            }
        }
        
        /// <summary>
        /// 던전 시작 (서버 전용)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void StartDungeonServerRpc(int dungeonDataIndex)
        {
            if (!IsServer) return;
            
            if (dungeonDataIndex < 0 || dungeonDataIndex >= availableDungeons.Count)
            {
                Debug.LogError($"Invalid dungeon index: {dungeonDataIndex}");
                return;
            }
            
            var dungeonData = availableDungeons[dungeonDataIndex];
            if (dungeonData == null)
            {
                Debug.LogError("Dungeon data is null!");
                return;
            }
            
            // 던전 정보 설정
            currentDungeon.Value = dungeonData.ToDungeonInfo();
            currentFloor.Value = 1;
            dungeonStartTime = Time.time;
            currentFloorStartTime = Time.time;
            totalMonstersKilled = 0;
            
            // 층별 시간 할당 계산
            CalculateFloorTimeAllocations(dungeonData);
            
            // 초기 시간 설정
            totalRemainingTime.Value = dungeonData.TimeLimit;
            currentFloorRemainingTime.Value = floorTimeAllocations[1];
            remainingTime.Value = currentFloorRemainingTime.Value;
            
            // 플레이어 정보 수집
            CollectPlayerInformation();
            
            // 첫 번째 층 로드
            LoadFloor(1, dungeonData);
            
            // 던전 상태 변경
            dungeonState.Value = DungeonState.Active;
            
            Debug.Log($"🏰 Dungeon '{dungeonData.DungeonName}' started with {dungeonPlayers.Count} players");
        }
        
        /// <summary>
        /// 다음 층으로 이동
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void AdvanceToNextFloorServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer || dungeonState.Value != DungeonState.Active) return;
            
            var dungeonData = GetCurrentDungeonData();
            if (dungeonData == null) return;
            
            int nextFloor = currentFloor.Value + 1;
            
            if (nextFloor > dungeonData.MaxFloors)
            {
                // 던전 완료
                CompleteDungeon();
                return;
            }
            
            // 현재 층에서 남은 시간을 다음 층에 추가
            float remainingFloorTime = currentFloorRemainingTime.Value;
            float nextFloorAllocatedTime = floorTimeAllocations.ContainsKey(nextFloor) 
                ? floorTimeAllocations[nextFloor] 
                : 600f; // 기본 10분
            
            // 다음 층으로 이동
            currentFloor.Value = nextFloor;
            currentFloorStartTime = Time.time;
            
            // 다음 층 시간 = 할당된 시간 + 이전 층에서 남은 시간
            currentFloorRemainingTime.Value = nextFloorAllocatedTime + remainingFloorTime;
            remainingTime.Value = currentFloorRemainingTime.Value;
            
            LoadFloor(nextFloor, dungeonData);
            
            Debug.Log($"🔺 Advanced to floor {nextFloor} with {currentFloorRemainingTime.Value:F1}s remaining");
        }
        
        /// <summary>
        /// 층 로드
        /// </summary>
        private void LoadFloor(int floorNumber, DungeonData dungeonData)
        {
            if (!IsServer) return;
            
            // 기존 층 정리
            ClearCurrentFloor();
            
            // 층 구성 가져오기
            var floorConfig = dungeonData.GetFloorConfig(floorNumber);
            
            // 몬스터 스폰
            SpawnMonstersForFloor(floorNumber, floorConfig, dungeonData);
            
            // 출구 생성 (마지막 층이거나 출구가 있는 층)
            if (floorConfig.hasExit || floorNumber == dungeonData.MaxFloors)
            {
                SpawnFloorExit(floorConfig.exitPoint);
            }
            
            // 클라이언트에 층 변경 알림
            NotifyFloorChangedClientRpc(floorNumber, floorConfig.floorName);
        }
        
        /// <summary>
        /// 현재 층 정리
        /// </summary>
        private void ClearCurrentFloor()
        {
            // 기존 몬스터들 제거
            foreach (var spawner in activeSpawners)
            {
                if (spawner != null)
                {
                    spawner.ClearAllMonsters();
                }
            }
            activeSpawners.Clear();
            
            // 기존 층 오브젝트들 제거
            foreach (var obj in currentFloorObjects)
            {
                if (obj != null)
                {
                    NetworkObject networkObj = obj.GetComponent<NetworkObject>();
                    if (networkObj != null && networkObj.IsSpawned)
                    {
                        networkObj.Despawn();
                    }
                    else
                    {
                        Destroy(obj);
                    }
                }
            }
            currentFloorObjects.Clear();
        }
        
        /// <summary>
        /// 층별 몬스터 스폰
        /// </summary>
        private void SpawnMonstersForFloor(int floorNumber, FloorConfiguration floorConfig, DungeonData dungeonData)
        {
            // 스폰 가능한 몬스터 목록
            var spawnableMonsters = dungeonData.GetSpawnableMonsters(floorNumber);
            if (spawnableMonsters.Count == 0)
            {
                Debug.LogWarning($"No spawnable monsters for floor {floorNumber}");
                return;
            }
            
            // 몬스터 스포너 생성 및 몬스터 스폰
            var spawnerObject = new GameObject($"Monster Spawner - Floor {floorNumber}");
            spawnerObject.transform.SetParent(dungeonRoot);
            
            var spawner = spawnerObject.AddComponent<MonsterSpawner>();
            var networkObject = spawnerObject.AddComponent<NetworkObject>();
            
            // 네트워크 스폰
            networkObject.Spawn();
            
            // 일반 몬스터 스폰
            for (int i = 0; i < floorConfig.monsterCount; i++)
            {
                var monsterInfo = SelectRandomMonster(spawnableMonsters, false);
                if (monsterInfo != null && monsterInfo.monsterPrefab != null)
                {
                    Vector3 spawnPos = GetRandomSpawnPosition(floorConfig.floorSize);
                    int monsterLevel = CalculateMonsterLevel(floorNumber, monsterInfo);
                    
                    spawner.SpawnSpecificMonster(monsterInfo.monsterPrefab, spawnPos, monsterLevel);
                }
            }
            
            // 엘리트 몬스터 스폰
            for (int i = 0; i < floorConfig.eliteCount; i++)
            {
                var monsterInfo = SelectRandomMonster(spawnableMonsters, true);
                if (monsterInfo != null && monsterInfo.monsterPrefab != null)
                {
                    Vector3 spawnPos = GetRandomSpawnPosition(floorConfig.floorSize);
                    int monsterLevel = CalculateMonsterLevel(floorNumber, monsterInfo) + 2; // 엘리트는 +2 레벨
                    
                    spawner.SpawnSpecificMonster(monsterInfo.monsterPrefab, spawnPos, monsterLevel);
                }
            }
            
            // 보스 스폰
            if (floorConfig.hasBoss)
            {
                var bossInfo = dungeonData.GetBossForFloor(floorNumber);
                if (bossInfo != null && bossInfo.bossPrefab != null)
                {
                    Vector3 bossSpawnPos = GetBossSpawnPosition(floorConfig.floorSize);
                    spawner.SpawnSpecificMonster(bossInfo.bossPrefab, bossSpawnPos, bossInfo.level);
                }
            }
            
            activeSpawners.Add(spawner);
            currentFloorObjects.Add(spawnerObject);
        }
        
        /// <summary>
        /// 층 출구 생성
        /// </summary>
        private void SpawnFloorExit(Vector2 exitPosition)
        {
            if (dungeonExitPrefab == null) return;
            
            Vector3 spawnPos = new Vector3(exitPosition.x, exitPosition.y, 0);
            var exitObject = Instantiate(dungeonExitPrefab, spawnPos, Quaternion.identity, dungeonRoot);
            
            var networkObject = exitObject.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
            }
            
            // 출구 상호작용 설정
            var exitTrigger = exitObject.GetComponent<DungeonExit>();
            if (exitTrigger != null)
            {
                exitTrigger.Initialize(this);
            }
            
            currentFloorObjects.Add(exitObject);
        }
        
        /// <summary>
        /// 랜덤 몬스터 선택
        /// </summary>
        private MonsterSpawnInfo SelectRandomMonster(List<MonsterSpawnInfo> monsters, bool eliteOnly)
        {
            var validMonsters = monsters.Where(m => eliteOnly ? m.isElite : !m.isElite).ToList();
            if (validMonsters.Count == 0)
            {
                validMonsters = monsters; // 엘리트가 없으면 일반 몬스터라도
            }
            
            // 가중치 기반 선택
            float totalWeight = validMonsters.Sum(m => m.spawnWeight);
            float randomValue = Random.Range(0f, totalWeight);
            
            float currentWeight = 0f;
            foreach (var monster in validMonsters)
            {
                currentWeight += monster.spawnWeight;
                if (randomValue <= currentWeight)
                {
                    return monster;
                }
            }
            
            return validMonsters[0]; // 안전장치
        }
        
        /// <summary>
        /// 몬스터 레벨 계산
        /// </summary>
        private int CalculateMonsterLevel(int floorNumber, MonsterSpawnInfo monsterInfo)
        {
            int baseLevel = Mathf.Clamp(floorNumber, monsterInfo.minLevel, monsterInfo.maxLevel);
            return baseLevel;
        }
        
        /// <summary>
        /// 랜덤 스폰 위치 생성
        /// </summary>
        private Vector3 GetRandomSpawnPosition(Vector2 floorSize)
        {
            float x = Random.Range(-floorSize.x / 2, floorSize.x / 2);
            float y = Random.Range(-floorSize.y / 2, floorSize.y / 2);
            return new Vector3(x, y, 0);
        }
        
        /// <summary>
        /// 보스 스폰 위치 생성 (중앙 부근)
        /// </summary>
        private Vector3 GetBossSpawnPosition(Vector2 floorSize)
        {
            // 층의 중앙 부근에 스폰
            float x = Random.Range(-floorSize.x * 0.2f, floorSize.x * 0.2f);
            float y = Random.Range(-floorSize.y * 0.2f, floorSize.y * 0.2f);
            return new Vector3(x, y, 0);
        }
        
        /// <summary>
        /// 던전 완료 처리
        /// </summary>
        private void CompleteDungeon()
        {
            if (!IsServer) return;
            
            float completionTime = Time.time - dungeonStartTime;
            var dungeonData = GetCurrentDungeonData();
            
            if (dungeonData != null)
            {
                var reward = dungeonData.CalculateCompletionReward(completionTime, totalMonstersKilled);
                
                // 보상 지급
                DistributeRewards(reward);
                
                // 던전 완료 알림
                NotifyDungeonCompletedClientRpc(reward);
            }
            
            dungeonState.Value = DungeonState.Completed;
        }
        
        /// <summary>
        /// 던전 종료 (실패)
        /// </summary>
        private void EndDungeon(bool success, string reason)
        {
            if (!IsServer) return;
            
            if (success)
            {
                CompleteDungeon();
            }
            else
            {
                dungeonState.Value = DungeonState.Failed;
                NotifyDungeonFailedClientRpc(reason);
            }
            
            // 층 정리
            ClearCurrentFloor();
            
            Debug.Log($"Dungeon ended: {(success ? "Success" : "Failed")} - {reason}");
        }
        
        /// <summary>
        /// 보상 분배
        /// </summary>
        private void DistributeRewards(DungeonReward reward)
        {
            foreach (var player in dungeonPlayers)
            {
                if (player.isAlive)
                {
                    DistributePlayerRewardClientRpc(player.clientId, reward);
                }
            }
        }
        
        /// <summary>
        /// 플레이어 정보 수집
        /// </summary>
        private void CollectPlayerInformation()
        {
            dungeonPlayers.Clear();
            
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                var playerObject = client.Value.PlayerObject;
                if (playerObject != null)
                {
                    var statsManager = playerObject.GetComponent<PlayerStatsManager>();
                    if (statsManager != null)
                    {
                        var dungeonPlayer = new DungeonPlayer
                        {
                            clientId = client.Key,
                            playerNameHash = DungeonNameRegistry.RegisterName($"Player_{client.Key}"),
                            playerLevel = statsManager.CurrentStats?.CurrentLevel ?? 1,
                            playerRace = statsManager.CurrentStats?.CharacterRace ?? Race.Human,
                            isAlive = true,
                            isReady = true,
                            spawnPosition = playerObject.transform.position
                        };
                        
                        dungeonPlayers.Add(dungeonPlayer);
                        playerStats[client.Key] = dungeonPlayer;
                    }
                }
            }
        }
        
        /// <summary>
        /// 현재 던전 데이터 가져오기
        /// </summary>
        private DungeonData GetCurrentDungeonData()
        {
            var current = currentDungeon.Value;
            return availableDungeons.FirstOrDefault(d => d.GetInstanceID() == current.dungeonId);
        }
        
        /// <summary>
        /// 몬스터 처치 알림 처리
        /// </summary>
        public void OnMonsterKilled(ulong killerClientId)
        {
            if (!IsServer) return;
            
            totalMonstersKilled++;
            
            // 층 클리어 조건 체크
            CheckFloorCompletion();
        }
        
        /// <summary>
        /// 층 클리어 조건 확인
        /// </summary>
        private void CheckFloorCompletion()
        {
            // 모든 몬스터가 죽었는지 확인
            bool allMonstersKilled = true;
            
            foreach (var spawner in activeSpawners)
            {
                if (spawner != null && spawner.HasActiveMonsters())
                {
                    allMonstersKilled = false;
                    break;
                }
            }
            
            if (allMonstersKilled)
            {
                // 층 클리어!
                NotifyFloorClearedClientRpc(currentFloor.Value);
            }
        }
        
        /// <summary>
        /// 플레이어 사망 처리
        /// </summary>
        public void OnPlayerDied(ulong clientId)
        {
            if (!IsServer) return;
            
            // 플레이어 상태 업데이트
            for (int i = 0; i < dungeonPlayers.Count; i++)
            {
                var player = dungeonPlayers[i];
                if (player.clientId == clientId)
                {
                    player.isAlive = false;
                    dungeonPlayers[i] = player;
                    break;
                }
            }
            
            // 모든 플레이어가 죽었으면 던전 실패
            bool anyPlayerAlive = false;
            foreach (var player in dungeonPlayers)
            {
                if (player.isAlive)
                {
                    anyPlayerAlive = true;
                    break;
                }
            }
            
            if (!anyPlayerAlive)
            {
                EndDungeon(false, "모든 플레이어 사망");
            }
        }
        
        // 이벤트 핸들러들
        private void OnStateChangedServer(DungeonState previousValue, DungeonState newValue)
        {
            Debug.Log($"Dungeon state changed: {previousValue} → {newValue}");
        }
        
        private void OnFloorChangedServer(int previousValue, int newValue)
        {
            Debug.Log($"Floor changed: {previousValue} → {newValue}");
        }
        
        private void OnStateChangedClient(DungeonState previousValue, DungeonState newValue)
        {
            OnDungeonStateChanged?.Invoke(newValue);
        }
        
        private void OnFloorChangedClient(int previousValue, int newValue)
        {
            OnFloorChanged?.Invoke(newValue);
        }
        
        // ClientRpc 메서드들
        [ClientRpc]
        private void NotifyFloorChangedClientRpc(int floorNumber, string floorName)
        {
            Debug.Log($"🔺 Entered floor {floorNumber}: {floorName}");
        }
        
        [ClientRpc]
        private void NotifyFloorClearedClientRpc(int floorNumber)
        {
            Debug.Log($"✅ Floor {floorNumber} cleared! Exit is now available.");
        }
        
        [ClientRpc]
        private void NotifyDungeonCompletedClientRpc(DungeonReward reward)
        {
            Debug.Log($"🎉 Dungeon completed! Exp: {reward.expReward}, Gold: {reward.goldReward}");
            OnDungeonCompleted?.Invoke(reward);
        }
        
        [ClientRpc]
        private void NotifyDungeonFailedClientRpc(string reason)
        {
            Debug.Log($"💀 Dungeon failed: {reason}");
        }
        
        [ClientRpc]
        private void DistributePlayerRewardClientRpc(ulong targetClientId, DungeonReward reward)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            
            // 로컬 플레이어에게 보상 지급
            var playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (playerObject != null)
            {
                var statsManager = playerObject.GetComponent<PlayerStatsManager>();
                if (statsManager != null)
                {
                    statsManager.AddExperience(reward.expReward);
                    statsManager.ChangeGold(reward.goldReward);
                }
                
                var inventoryManager = playerObject.GetComponent<InventoryManager>();
                if (inventoryManager != null && reward.itemRewards != null)
                {
                    foreach (var item in reward.itemRewards)
                    {
                        inventoryManager.AddItemToInventory(item);
                    }
                }
            }
        }
        
        /// <summary>
        /// 층별 시간 할당 계산
        /// </summary>
        private void CalculateFloorTimeAllocations(DungeonData dungeonData)
        {
            floorTimeAllocations.Clear();
            
            // DungeonData의 시간 관리 시스템 사용
            for (int floor = 1; floor <= dungeonData.MaxFloors; floor++)
            {
                float floorTime = dungeonData.CalculateFloorTimeLimit(floor);
                floorTimeAllocations[floor] = floorTime;
                
                Debug.Log($"Floor {floor}: {floorTime:F1}s allocated ({floorTime/60:F1} minutes) - Mode: {dungeonData.TimeMode}");
            }
            
            // 시간 관리 정보 출력
            Debug.Log($"Dungeon Time Management Info:\n{dungeonData.GetTimeManagementInfo()}");
        }
        
        /// <summary>
        /// 모든 플레이어 강제 방출
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ForceEjectAllPlayersServerRpc(string reason)
        {
            if (!IsServer) return;
            
            Debug.LogWarning($"⚠️ Force ejecting all players: {reason}");
            
            // 모든 플레이어를 마을로 이동
            EjectAllPlayersToTownClientRpc(reason);
            
            // 던전 상태를 실패로 변경
            dungeonState.Value = DungeonState.Failed;
            
            // 던전 정리
            ClearCurrentFloor();
            
            NotifyDungeonFailedClientRpc(reason);
        }
        
        /// <summary>
        /// 모든 플레이어를 마을로 이동 (클라이언트)
        /// </summary>
        [ClientRpc]
        private void EjectAllPlayersToTownClientRpc(string reason)
        {
            Debug.LogWarning($"💨 Ejected from dungeon: {reason}");
            
            // 로컬 플레이어를 마을 위치로 이동
            var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (localPlayer != null)
            {
                // 마을 스폰 위치로 이동 (0, 0, 0을 기본으로 사용)
                Vector3 townPosition = Vector3.zero;
                localPlayer.transform.position = townPosition;
                
                // 플레이어 컨트롤 재활성화
                var playerController = localPlayer.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    // 플레이어 상태 복구
                }
            }
        }
        
        /// <summary>
        /// 개별 플레이어 강제 방출
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ForceEjectPlayerServerRpc(ulong clientId, string reason, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            
            Debug.LogWarning($"⚠️ Force ejecting player {clientId}: {reason}");
            
            // 특정 플레이어만 마을로 이동
            EjectPlayerToTownClientRpc(clientId, reason);
            
            // 던전 플레이어 목록에서 제거
            for (int i = dungeonPlayers.Count - 1; i >= 0; i--)
            {
                if (dungeonPlayers[i].clientId == clientId)
                {
                    dungeonPlayers.RemoveAt(i);
                    break;
                }
            }
            
            // 던전에 플레이어가 없으면 던전 종료
            if (dungeonPlayers.Count == 0)
            {
                EndDungeon(false, "모든 플레이어 방출");
            }
        }
        
        /// <summary>
        /// 개별 플레이어를 마을로 이동 (클라이언트)
        /// </summary>
        [ClientRpc]
        private void EjectPlayerToTownClientRpc(ulong targetClientId, string reason)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            
            Debug.LogWarning($"💨 You were ejected from dungeon: {reason}");
            
            var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (localPlayer != null)
            {
                Vector3 townPosition = Vector3.zero;
                localPlayer.transform.position = townPosition;
            }
        }
        
        /// <summary>
        /// 현재 층의 남은 시간 정보
        /// </summary>
        public string GetFloorTimeInfo()
        {
            if (!IsActive) return "던전이 활성화되지 않음";
            
            int minutes = Mathf.FloorToInt(currentFloorRemainingTime.Value / 60);
            int seconds = Mathf.FloorToInt(currentFloorRemainingTime.Value % 60);
            int totalMinutes = Mathf.FloorToInt(totalRemainingTime.Value / 60);
            int totalSeconds = Mathf.FloorToInt(totalRemainingTime.Value % 60);
            
            return $"현재 층: {minutes:00}:{seconds:00} | 총 시간: {totalMinutes:00}:{totalSeconds:00}";
        }
    }
}