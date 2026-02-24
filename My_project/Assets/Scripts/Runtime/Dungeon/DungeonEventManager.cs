using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 던전 이벤트 매니저 - 던전 내 랜덤 이벤트 관리
    /// 서버에서 이벤트 결정, 클라이언트에서 UI/이펙트 표시
    /// </summary>
    public class DungeonEventManager : NetworkBehaviour
    {
        public static DungeonEventManager Instance { get; private set; }

        [Header("이벤트 설정")]
        [SerializeField] private float eventCheckInterval = 5f;      // 이벤트 체크 간격
        [SerializeField] private int maxEventsPerFloor = 3;          // 층당 최대 이벤트 수
        [SerializeField] private float eventSpawnRadius = 8f;        // 이벤트 스폰 반경

        // 로드된 이벤트 데이터
        private List<DungeonEventData> allEvents = new List<DungeonEventData>();

        // 현재 던전 상태
        private List<string> usedEventIds = new List<string>();      // 이번 던전에서 사용된 이벤트
        private int eventsOnCurrentFloor = 0;
        private int currentFloor = 0;
        private DungeonDifficulty currentDifficulty = DungeonDifficulty.Easy;
        private float eventTimer = 0f;

        // 활성 이벤트 버프
        private List<ActiveEventBuff> activeBuffs = new List<ActiveEventBuff>();

        // 이벤트
        public System.Action<DungeonEventData> OnEventTriggered;
        public System.Action<EventOutcome> OnEventOutcome;
        public System.Action<string> OnEventMessage;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadEventData();
        }

        private void LoadEventData()
        {
            var loaded = Resources.LoadAll<DungeonEventData>("ScriptableObjects/DungeonEvents");
            allEvents.AddRange(loaded);
            Debug.Log($"[DungeonEvent] Loaded {allEvents.Count} event data");
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        private void Update()
        {
            // 활성 버프 업데이트
            UpdateActiveBuffs();

            if (!IsServer) return;

            // 이벤트 체크 (서버만)
            eventTimer += Time.deltaTime;
            if (eventTimer >= eventCheckInterval)
            {
                eventTimer = 0f;
                CheckRandomEvent();
            }
        }

        /// <summary>
        /// 새 던전 시작 시 초기화
        /// </summary>
        public void InitializeDungeon(DungeonDifficulty difficulty)
        {
            currentDifficulty = difficulty;
            currentFloor = 1;
            eventsOnCurrentFloor = 0;
            usedEventIds.Clear();
            activeBuffs.Clear();
        }

        /// <summary>
        /// 층 이동 시 호출
        /// </summary>
        public void OnFloorChanged(int newFloor)
        {
            currentFloor = newFloor;
            eventsOnCurrentFloor = 0;

            if (IsServer)
            {
                // 보장 이벤트 체크
                CheckGuaranteedEvents();
            }
        }

        // === 이벤트 체크 ===

        private void CheckRandomEvent()
        {
            if (eventsOnCurrentFloor >= maxEventsPerFloor) return;

            var candidates = allEvents.Where(e =>
                e.TriggerType == DungeonEventTrigger.ChanceBased &&
                e.MinFloor <= currentFloor &&
                e.MaxFloor >= currentFloor &&
                e.MinDifficulty <= currentDifficulty &&
                (!e.OncePerDungeon || !usedEventIds.Contains(e.EventId))
            ).ToList();

            foreach (var evt in candidates)
            {
                if (Random.value < evt.SpawnChance * (eventCheckInterval / 60f))
                {
                    TriggerEvent(evt);
                    break;
                }
            }
        }

        private void CheckGuaranteedEvents()
        {
            var guaranteed = allEvents.Where(e =>
                e.TriggerType == DungeonEventTrigger.FloorGuaranteed &&
                e.MinFloor <= currentFloor &&
                e.MaxFloor >= currentFloor &&
                e.MinDifficulty <= currentDifficulty &&
                (!e.OncePerDungeon || !usedEventIds.Contains(e.EventId))
            ).ToList();

            foreach (var evt in guaranteed)
            {
                TriggerEvent(evt);
            }
        }

        /// <summary>
        /// 조건 기반 이벤트 체크 (외부 호출)
        /// </summary>
        public void CheckConditionEvents(EventConditionType condition, float value = 0)
        {
            if (!IsServer) return;

            var candidates = allEvents.Where(e =>
                e.TriggerType == DungeonEventTrigger.ConditionBased &&
                e.MinFloor <= currentFloor &&
                e.MaxFloor >= currentFloor &&
                (!e.OncePerDungeon || !usedEventIds.Contains(e.EventId))
            ).ToList();

            foreach (var evt in candidates)
            {
                if (Random.value < evt.SpawnChance)
                {
                    TriggerEvent(evt);
                    break;
                }
            }
        }

        // === 이벤트 실행 ===

        private void TriggerEvent(DungeonEventData eventData)
        {
            if (eventData == null) return;

            eventsOnCurrentFloor++;
            usedEventIds.Add(eventData.EventId);

            // 모든 클라이언트에 알림
            TriggerEventClientRpc(eventData.EventId);
        }

        [ClientRpc]
        private void TriggerEventClientRpc(string eventId)
        {
            var eventData = allEvents.Find(e => e.EventId == eventId);
            if (eventData == null) return;

            OnEventTriggered?.Invoke(eventData);
            ShowEventNotification(eventData);

            // 즉시 효과 이벤트 (제단, 샘물 등)
            if (!eventData.RequiresChoice && eventData.BareHandOutcomes.Count > 0)
            {
                ProcessOutcomes(eventData.BareHandOutcomes);
            }
        }

        /// <summary>
        /// 이벤트 상호작용 (플레이어가 F키로 상호작용)
        /// </summary>
        public void InteractWithEvent(DungeonEventData eventData, int choiceIndex = -1)
        {
            if (eventData == null) return;

            if (eventData.RequiresChoice && choiceIndex >= 0 && choiceIndex < eventData.Choices.Count)
            {
                // 선택지 결과
                var choice = eventData.Choices[choiceIndex];
                OnEventMessage?.Invoke(choice.resultText);
                ProcessOutcomes(choice.outcomes);
            }
            else if (eventData.BareHandOutcomes.Count > 0)
            {
                // 맨손 상호작용
                ProcessOutcomes(eventData.BareHandOutcomes);
            }

            // 전투 이벤트
            if (eventData.CombatWaves.Count > 0)
            {
                StartCombatEvent(eventData);
            }
        }

        /// <summary>
        /// 아이템으로 상호작용 (안전한 선택)
        /// </summary>
        public void InteractWithItem(DungeonEventData eventData, string itemName)
        {
            if (eventData == null) return;

            var interaction = eventData.ItemInteractions.Find(i => i.requiredItemName == itemName);
            if (interaction != null)
            {
                OnEventMessage?.Invoke(interaction.resultDescription);
                if (interaction.guaranteedOutcome != null)
                {
                    ApplyOutcome(interaction.guaranteedOutcome);
                }
            }
        }

        // === 결과 처리 ===

        private void ProcessOutcomes(List<EventOutcome> outcomes)
        {
            if (outcomes == null || outcomes.Count == 0) return;

            // 확률 기반 결과 선택
            float totalChance = outcomes.Sum(o => o.chance);
            float roll = Random.Range(0f, totalChance);
            float cumulative = 0f;

            foreach (var outcome in outcomes)
            {
                cumulative += outcome.chance;
                if (roll <= cumulative)
                {
                    ApplyOutcome(outcome);
                    break;
                }
            }
        }

        private void ApplyOutcome(EventOutcome outcome)
        {
            if (outcome == null) return;

            OnEventOutcome?.Invoke(outcome);
            OnEventMessage?.Invoke(outcome.description);

            var player = FindLocalPlayer();
            if (player == null) return;

            switch (outcome.effectType)
            {
                case EventEffectType.HealHP:
                    player.CurrentStats?.ChangeHP((int)outcome.effectValue);
                    NotifyEffect($"HP +{(int)outcome.effectValue}", false);
                    break;

                case EventEffectType.HealMP:
                    player.CurrentStats?.ChangeMP((int)outcome.effectValue);
                    NotifyEffect($"MP +{(int)outcome.effectValue}", false);
                    break;

                case EventEffectType.HealPercent:
                    if (player.CurrentStats != null)
                    {
                        int healAmount = Mathf.RoundToInt(player.CurrentStats.MaxHP * outcome.effectValue / 100f);
                        player.CurrentStats.ChangeHP(healAmount);
                        NotifyEffect($"HP +{healAmount} ({outcome.effectValue}%)", false);
                    }
                    break;

                case EventEffectType.DamageHP:
                    player.CurrentStats?.ChangeHP(-(int)outcome.effectValue);
                    NotifyEffect($"HP -{(int)outcome.effectValue}", true);
                    break;

                case EventEffectType.GainGold:
                    player.CurrentStats?.ChangeGold((long)outcome.effectValue);
                    NotifyEffect($"골드 +{(int)outcome.effectValue}", false);
                    break;

                case EventEffectType.LoseGold:
                    player.CurrentStats?.ChangeGold(-(long)outcome.effectValue);
                    NotifyEffect($"골드 -{(int)outcome.effectValue}", true);
                    break;

                case EventEffectType.GainExp:
                    player.CurrentStats?.AddExperience((long)outcome.effectValue);
                    NotifyEffect($"경험치 +{(int)outcome.effectValue}", false);
                    break;

                case EventEffectType.BuffStat:
                    AddEventBuff(outcome);
                    NotifyEffect($"버프: {outcome.description} ({outcome.duration}초)", false);
                    break;

                case EventEffectType.DebuffStat:
                    AddEventBuff(outcome);
                    NotifyEffect($"디버프: {outcome.description} ({outcome.duration}초)", true);
                    break;

                case EventEffectType.IncreaseDamage:
                    AddEventBuff(outcome);
                    NotifyEffect($"데미지 +{(int)outcome.effectValue}% ({outcome.duration}초)", false);
                    break;

                case EventEffectType.ReduceDamage:
                    AddEventBuff(outcome);
                    NotifyEffect($"피해감소 +{(int)outcome.effectValue}% ({outcome.duration}초)", false);
                    break;

                case EventEffectType.IncreaseSpeed:
                    AddEventBuff(outcome);
                    NotifyEffect($"이동속도 +{(int)outcome.effectValue}% ({outcome.duration}초)", false);
                    break;

                case EventEffectType.CooldownReset:
                    NotifyEffect("모든 스킬 쿨다운 초기화!", false);
                    break;

                case EventEffectType.FullRestore:
                    if (player.CurrentStats != null)
                    {
                        player.CurrentStats.ChangeHP(player.CurrentStats.MaxHP);
                        player.CurrentStats.ChangeMP(player.CurrentStats.MaxMP);
                    }
                    NotifyEffect("HP/MP 전체 회복!", false);
                    break;

                case EventEffectType.ApplyStatus:
                    NotifyEffect($"상태이상: {outcome.statusEffect}", outcome.isNegative);
                    break;

                case EventEffectType.RemoveStatus:
                    NotifyEffect("모든 상태이상 해제!", false);
                    break;

                case EventEffectType.RandomBuff:
                    ApplyRandomBuff(outcome.duration);
                    break;

                case EventEffectType.CurseAndReward:
                    AddEventBuff(outcome);
                    NotifyEffect($"저주: 받는 피해 +{(int)outcome.effectValue}%, 획득 보상 2배!", true);
                    break;

                default:
                    NotifyEffect(outcome.description, outcome.isNegative);
                    break;
            }

            // 시각 이펙트
            if (CameraEffects.Instance != null)
            {
                if (outcome.isNegative)
                    CameraEffects.Instance.FlashRed(0.3f);
                else
                    CameraEffects.Instance.FlashGold(0.3f);
            }
        }

        private void ApplyRandomBuff(float duration)
        {
            string[] buffNames = { "힘 증가", "민첩 증가", "지능 증가", "방어력 증가", "행운 증가" };
            int idx = Random.Range(0, buffNames.Length);
            var randomOutcome = new EventOutcome
            {
                description = buffNames[idx],
                effectType = EventEffectType.BuffStat,
                effectValue = Random.Range(3f, 8f),
                duration = duration,
                isNegative = false
            };
            AddEventBuff(randomOutcome);
            NotifyEffect($"랜덤 버프: {buffNames[idx]} +{(int)randomOutcome.effectValue} ({duration}초)", false);
        }

        // === 전투 이벤트 ===

        private void StartCombatEvent(DungeonEventData eventData)
        {
            OnEventMessage?.Invoke("적이 습격합니다!");

            // MonsterEntitySpawner를 통해 실제 몬스터 스폰
            var spawner = FindFirstObjectByType<MonsterEntitySpawner>();
            if (spawner != null && eventData.CombatWaves != null)
            {
                int waveCount = eventData.CombatWaves.Count;
                int monstersToSpawn = Mathf.Min(waveCount * 3, 12); // 웨이브당 3마리, 최대 12
                for (int i = 0; i < monstersToSpawn; i++)
                {
                    if (spawner.CanSpawn)
                        spawner.SpawnRandomMonsterEntity();
                }
                Debug.Log($"[DungeonEvent] Combat event: spawned {monstersToSpawn} monsters from {waveCount} waves");
            }
            else
            {
                Debug.Log($"[DungeonEvent] Combat event: no spawner found or no waves defined");
            }
        }

        // === 버프 관리 ===

        private void AddEventBuff(EventOutcome outcome)
        {
            activeBuffs.Add(new ActiveEventBuff
            {
                description = outcome.description,
                effectType = outcome.effectType,
                value = outcome.effectValue,
                remainingTime = outcome.duration,
                isNegative = outcome.isNegative
            });
        }

        private void UpdateActiveBuffs()
        {
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                var buff = activeBuffs[i];
                buff.remainingTime -= Time.deltaTime;

                if (buff.remainingTime <= 0f)
                {
                    OnEventMessage?.Invoke($"효과 종료: {buff.description}");
                    activeBuffs.RemoveAt(i);
                }
                else
                {
                    activeBuffs[i] = buff;
                }
            }
        }

        /// <summary>
        /// 현재 이벤트 데미지 보정 (전투 시스템 연동용)
        /// </summary>
        public float GetDamageMultiplier()
        {
            float mult = 1f;
            foreach (var buff in activeBuffs)
            {
                if (buff.effectType == EventEffectType.IncreaseDamage)
                    mult += buff.value / 100f;
                else if (buff.effectType == EventEffectType.CurseAndReward)
                    mult -= buff.value / 200f; // 저주는 절반만 데미지에 영향
            }
            return mult;
        }

        /// <summary>
        /// 현재 이벤트 피해감소 보정
        /// </summary>
        public float GetDamageReductionMultiplier()
        {
            float mult = 1f;
            foreach (var buff in activeBuffs)
            {
                if (buff.effectType == EventEffectType.ReduceDamage)
                    mult -= buff.value / 100f;
                else if (buff.effectType == EventEffectType.CurseAndReward)
                    mult += buff.value / 100f; // 저주: 받는 피해 증가
            }
            return Mathf.Max(0.1f, mult);
        }

        /// <summary>
        /// 현재 이벤트 보상 배율
        /// </summary>
        public float GetRewardMultiplier()
        {
            float mult = 1f;
            foreach (var buff in activeBuffs)
            {
                if (buff.effectType == EventEffectType.CurseAndReward)
                    mult += 1f; // 저주 시 보상 2배
            }
            return mult;
        }

        /// <summary>
        /// 활성 버프 목록
        /// </summary>
        public List<ActiveEventBuff> GetActiveBuffs()
        {
            return activeBuffs;
        }

        // === 유틸 ===

        private void ShowEventNotification(DungeonEventData eventData)
        {
            string rarityColor = eventData.Rarity switch
            {
                DungeonEventRarity.Common => "#AAAAAA",
                DungeonEventRarity.Uncommon => "#55FF55",
                DungeonEventRarity.Rare => "#5555FF",
                DungeonEventRarity.Epic => "#AA55FF",
                DungeonEventRarity.Legendary => "#FFAA00",
                _ => "#FFFFFF"
            };

            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ShowNotification(
                    $"이벤트 발견: {eventData.EventName}",
                    NotificationType.System
                );
            }
        }

        private void NotifyEffect(string message, bool isNegative)
        {
            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ShowNotification(
                    message,
                    isNegative ? NotificationType.Warning : NotificationType.System
                );
            }
        }

        private PlayerStatsManager FindLocalPlayer()
        {
            var players = FindObjectsByType<PlayerStatsManager>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p.IsOwner) return p;
            }
            return null;
        }

        /// <summary>
        /// 특정 타입의 이벤트 목록
        /// </summary>
        public List<DungeonEventData> GetEventsByType(DungeonEventType type)
        {
            return allEvents.Where(e => e.EventType == type).ToList();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this)
                Instance = null;
        }
    }

    [System.Serializable]
    public struct ActiveEventBuff
    {
        public string description;
        public EventEffectType effectType;
        public float value;
        public float remainingTime;
        public bool isNegative;
    }
}
