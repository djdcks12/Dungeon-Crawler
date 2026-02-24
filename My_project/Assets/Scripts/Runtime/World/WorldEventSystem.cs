using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 월드 랜덤 이벤트 시스템
    /// 던전 밖 필드에서 랜덤 이벤트를 관리하고 실행
    /// 트레저 고블린, 방랑 상인, 미니보스, 몬스터 습격 등
    /// </summary>
    public class WorldEventSystem : NetworkBehaviour
    {
        public static WorldEventSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float baseCheckInterval = 30f;
        [SerializeField] private int maxActiveEvents = 3;
        [SerializeField] private float eventSpawnDistance = 15f;

        // 모든 이벤트 데이터
        private WorldEventData[] allEvents;

        // 활성 이벤트
        private List<ActiveWorldEvent> activeEvents = new List<ActiveWorldEvent>();

        // 쿨다운 (이벤트 ID → 마지막 발생 시간)
        private Dictionary<string, float> eventCooldowns = new Dictionary<string, float>();

        // 타이머
        private float checkTimer;

        // 이벤트
        public System.Action<WorldEventData> OnEventStarted;
        public System.Action<WorldEventData, bool> OnEventCompleted; // data, success

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            LoadEventData();
        }

        private void LoadEventData()
        {
            allEvents = Resources.LoadAll<WorldEventData>("ScriptableObjects/WorldEvents");
            Debug.Log($"[WorldEvent] {allEvents.Length}개 월드 이벤트 로드됨");
        }

        private void Update()
        {
            if (!IsServer) return;

            checkTimer += Time.deltaTime;
            if (checkTimer >= baseCheckInterval)
            {
                checkTimer = 0;
                CheckRandomEvents();
            }

            UpdateActiveEvents();
        }

        /// <summary>
        /// 랜덤 이벤트 발생 체크
        /// </summary>
        private void CheckRandomEvents()
        {
            if (activeEvents.Count >= maxActiveEvents) return;
            if (allEvents == null || allEvents.Length == 0) return;

            // 현재 시간대, 날씨 확인
            bool isNight = false;
            WeatherCondition currentWeather = WeatherCondition.Any;

            var weatherSystem = FindFirstObjectByType<WeatherSystem>();
            if (weatherSystem != null)
            {
                isNight = weatherSystem.GetCurrentWeather() == WeatherType.Clear;
                // WeatherType → WeatherCondition 변환
                currentWeather = ConvertWeather(weatherSystem.GetCurrentWeather());
            }

            // 플레이어 레벨 확인
            int playerLevel = 1;
            var players = FindObjectsByType<PlayerStatsManager>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.IsOwner)
                {
                    playerLevel = player.GetComponent<PlayerStatsManager>()?.CurrentStats?.CurrentLevel ?? 1;
                    break;
                }
            }

            // 이벤트 후보 수집
            var candidates = new List<WorldEventData>();
            foreach (var eventData in allEvents)
            {
                if (!CanSpawnEvent(eventData, playerLevel, isNight, currentWeather))
                    continue;
                candidates.Add(eventData);
            }

            if (candidates.Count == 0) return;

            // 희귀도 기반 가중 랜덤 선택
            float totalWeight = 0;
            foreach (var c in candidates)
            {
                totalWeight += GetRarityWeight(c.Rarity) * c.SpawnChance;
            }

            float roll = Random.Range(0, totalWeight);
            float cumulative = 0;
            foreach (var c in candidates)
            {
                cumulative += GetRarityWeight(c.Rarity) * c.SpawnChance;
                if (roll <= cumulative)
                {
                    TriggerEvent(c);
                    break;
                }
            }
        }

        /// <summary>
        /// 이벤트 발생 가능 여부 확인
        /// </summary>
        private bool CanSpawnEvent(WorldEventData eventData, int playerLevel, bool isNight, WeatherCondition weather)
        {
            // 레벨 체크
            if (playerLevel < eventData.MinPlayerLevel || playerLevel > eventData.MaxPlayerLevel)
                return false;

            // 시간대 체크
            if (eventData.RequiresNight && !isNight) return false;
            if (eventData.RequiresDay && isNight) return false;

            // 날씨 체크
            if (eventData.RequiredWeather != WeatherCondition.Any && eventData.RequiredWeather != weather)
                return false;

            // 쿨다운 체크 (같은 이벤트 연속 방지)
            if (eventCooldowns.TryGetValue(eventData.EventId, out float lastTime))
            {
                if (Time.time - lastTime < eventData.CheckInterval * 3f)
                    return false;
            }

            // 이미 활성 체크
            foreach (var active in activeEvents)
            {
                if (active.eventData.EventId == eventData.EventId)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 이벤트 발생
        /// </summary>
        private void TriggerEvent(WorldEventData eventData)
        {
            // 플레이어 근처에서 발생 위치 결정
            Vector3 spawnPos = Vector3.zero;
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            if (players.Length > 0)
            {
                var randomPlayer = players[Random.Range(0, players.Length)];
                Vector2 offset = Random.insideUnitCircle.normalized * eventSpawnDistance;
                spawnPos = randomPlayer.transform.position + new Vector3(offset.x, offset.y, 0);
            }

            var activeEvent = new ActiveWorldEvent
            {
                eventData = eventData,
                startTime = Time.time,
                position = spawnPos,
                isCompleted = false
            };

            activeEvents.Add(activeEvent);
            eventCooldowns[eventData.EventId] = Time.time;

            // 전체 공지
            if (eventData.AnnounceToAll)
            {
                AnnounceEventClientRpc(eventData.EventName, eventData.AnnounceMessage);
            }
            else
            {
                // 근처 플레이어에게만 알림
                NotifyNearbyPlayersClientRpc(eventData.EventName, spawnPos);
            }

            // 몬스터 스폰 (몬스터 이벤트인 경우)
            if (eventData.EventMonsters != null && eventData.EventMonsters.Length > 0)
            {
                SpawnEventMonsters(eventData, spawnPos);
            }

            OnEventStarted?.Invoke(eventData);
            Debug.Log($"[WorldEvent] 이벤트 발생: {eventData.EventName} at {spawnPos}");
        }

        /// <summary>
        /// 이벤트 몬스터 스폰
        /// </summary>
        private void SpawnEventMonsters(WorldEventData eventData, Vector3 center)
        {
            var spawner = FindFirstObjectByType<MonsterEntitySpawner>();
            if (spawner == null)
            {
                Debug.LogWarning("[WorldEvent] MonsterEntitySpawner를 찾을 수 없습니다");
                return;
            }

            foreach (var monster in eventData.EventMonsters)
            {
                for (int i = 0; i < monster.count; i++)
                {
                    Vector2 offset = Random.insideUnitCircle * eventData.SpawnRadius;
                    Vector3 spawnPos = center + new Vector3(offset.x, offset.y, 0);

                    // MonsterEntitySpawner를 통한 스폰
                    // 직접 호출이 어려우면 위치에 마커를 남겨 Spawner가 처리하도록
                    Debug.Log($"[WorldEvent] 몬스터 스폰: {monster.monsterVariantId} at {spawnPos}");
                }
            }
        }

        /// <summary>
        /// 활성 이벤트 업데이트
        /// </summary>
        private void UpdateActiveEvents()
        {
            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                var active = activeEvents[i];

                // 시간 만료 체크
                if (!active.eventData.IsPermanentUntilCompleted)
                {
                    if (Time.time - active.startTime > active.eventData.Duration)
                    {
                        CompleteEvent(i, false);
                        continue;
                    }
                }

                // 완료 여부 체크
                if (active.isCompleted)
                {
                    CompleteEvent(i, true);
                }
            }
        }

        /// <summary>
        /// 이벤트 완료 처리
        /// </summary>
        private void CompleteEvent(int index, bool success)
        {
            var active = activeEvents[index];
            var eventData = active.eventData;

            if (success && eventData.Rewards != null)
            {
                DistributeRewards(eventData, active.position);
            }

            OnEventCompleted?.Invoke(eventData, success);
            activeEvents.RemoveAt(index);

            Debug.Log($"[WorldEvent] 이벤트 {(success ? "완료" : "실패/만료")}: {eventData.EventName}");
        }

        /// <summary>
        /// 보상 분배
        /// </summary>
        private void DistributeRewards(WorldEventData eventData, Vector3 position)
        {
            var players = FindObjectsByType<PlayerStatsManager>(FindObjectsSortMode.None);

            foreach (var reward in eventData.Rewards)
            {
                if (Random.value > reward.dropChance) continue;

                foreach (var player in players)
                {
                    float dist = Vector3.Distance(player.transform.position, position);
                    if (dist > eventData.SpawnRadius * 2f) continue;

                    var statsData = player.GetComponent<PlayerStatsManager>()?.CurrentStats;
                    if (statsData == null) continue;

                    switch (reward.rewardType)
                    {
                        case WorldRewardType.Gold:
                            statsData.ChangeGold(reward.amount);
                            break;
                        case WorldRewardType.Experience:
                            statsData.AddExperience(reward.amount);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 이벤트를 외부에서 완료 처리 (예: 트레저 고블린 처치)
        /// </summary>
        public void MarkEventCompleted(string eventId)
        {
            for (int i = 0; i < activeEvents.Count; i++)
            {
                if (activeEvents[i].eventData.EventId == eventId)
                {
                    var evt = activeEvents[i];
                    evt.isCompleted = true;
                    activeEvents[i] = evt;
                    break;
                }
            }
        }

        /// <summary>
        /// 이벤트 선택지 처리
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ProcessChoiceServerRpc(string eventId, int choiceIndex, ServerRpcParams rpcParams = default)
        {
            WorldEventData eventData = null;
            foreach (var active in activeEvents)
            {
                if (active.eventData.EventId == eventId)
                {
                    eventData = active.eventData;
                    break;
                }
            }

            if (eventData == null || eventData.Choices == null) return;
            if (choiceIndex < 0 || choiceIndex >= eventData.Choices.Length) return;

            var choice = eventData.Choices[choiceIndex];
            ulong clientId = rpcParams.Receive.SenderClientId;

            // 위험 체크
            bool isRisky = Random.value < choice.riskChance;

            if (isRisky && choice.riskDamage > 0)
            {
                // 위험 결과 - 데미지
                var player = GetPlayerByClientId(clientId);
                if (player != null)
                {
                    player.GetComponent<PlayerStatsManager>()?.CurrentStats?.ChangeHP(-choice.riskDamage);
                }
                NotifyChoiceResultClientRpc(eventId, choiceIndex, false, clientId);
            }
            else
            {
                // 성공 결과 - 보상
                var player = GetPlayerByClientId(clientId);
                if (player != null)
                {
                    var statsData = player.GetComponent<PlayerStatsManager>()?.CurrentStats;
                    if (statsData != null)
                    {
                        switch (choice.rewardType)
                        {
                            case WorldRewardType.Gold:
                                statsData.ChangeGold(choice.rewardAmount);
                                break;
                            case WorldRewardType.Experience:
                                statsData.AddExperience(choice.rewardAmount);
                                break;
                        }
                    }
                }
                NotifyChoiceResultClientRpc(eventId, choiceIndex, true, clientId);
            }
        }

        #region Network RPCs

        [ClientRpc]
        private void AnnounceEventClientRpc(string eventName, string message)
        {
            var notif = NotificationManager.Instance;
            if (notif != null)
            {
                string text = string.IsNullOrEmpty(message) ? $"월드 이벤트: {eventName}!" : message;
                notif.ShowNotification(text, NotificationType.System);
            }

            var cam = CameraEffects.Instance;
            if (cam != null)
            {
                cam.ShakeLight();
            }
        }

        [ClientRpc]
        private void NotifyNearbyPlayersClientRpc(string eventName, Vector3 position)
        {
            // 자기 위치와 이벤트 위치 거리 체크
            var localPlayer = FindLocalPlayer();
            if (localPlayer == null) return;

            float dist = Vector3.Distance(localPlayer.transform.position, position);
            if (dist < 30f) // 30유닛 이내면 알림
            {
                var notif = NotificationManager.Instance;
                if (notif != null)
                {
                    notif.ShowNotification($"근처에서 이벤트 발생: {eventName}", NotificationType.System);
                }
            }
        }

        [ClientRpc]
        private void NotifyChoiceResultClientRpc(string eventId, int choiceIndex, bool success, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            var notif = NotificationManager.Instance;
            if (notif != null)
            {
                string result = success ? "성공! 보상을 획득했습니다." : "실패! 위험에 빠졌습니다.";
                notif.ShowNotification(result, success ? NotificationType.ItemAcquire : NotificationType.Warning);
            }
        }

        #endregion

        #region Utility

        private PlayerStatsManager FindLocalPlayer()
        {
            var players = FindObjectsByType<PlayerStatsManager>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p.IsOwner) return p;
            }
            return null;
        }

        private PlayerStatsManager GetPlayerByClientId(ulong clientId)
        {
            var players = FindObjectsByType<PlayerStatsManager>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p.OwnerClientId == clientId) return p;
            }
            return null;
        }

        private float GetRarityWeight(WorldEventRarity rarity)
        {
            switch (rarity)
            {
                case WorldEventRarity.Common: return 50f;
                case WorldEventRarity.Uncommon: return 30f;
                case WorldEventRarity.Rare: return 15f;
                case WorldEventRarity.Epic: return 4f;
                case WorldEventRarity.Legendary: return 1f;
                default: return 10f;
            }
        }

        private WeatherCondition ConvertWeather(WeatherType weatherType)
        {
            switch (weatherType)
            {
                case WeatherType.Clear: return WeatherCondition.Clear;
                case WeatherType.Rain: return WeatherCondition.Rain;
                case WeatherType.Fog: return WeatherCondition.Any;
                case WeatherType.Snow: return WeatherCondition.Snow;
                default: return WeatherCondition.Any;
            }
        }

        /// <summary>
        /// 활성 이벤트 목록 (외부 UI용)
        /// </summary>
        public IReadOnlyList<ActiveWorldEvent> ActiveEvents => activeEvents;

        #endregion

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                OnEventStarted = null;
                OnEventCompleted = null;
                Instance = null;
            }
            base.OnDestroy();
        }
    }

    /// <summary>
    /// 활성 월드 이벤트
    /// </summary>
    public struct ActiveWorldEvent
    {
        public WorldEventData eventData;
        public float startTime;
        public Vector3 position;
        public bool isCompleted;

        public float RemainingTime => eventData.Duration - (Time.time - startTime);
        public float Progress => (Time.time - startTime) / eventData.Duration;
    }
}
