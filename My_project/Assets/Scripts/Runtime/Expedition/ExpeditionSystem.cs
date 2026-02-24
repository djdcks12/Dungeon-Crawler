using UnityEngine;
using Unity.Netcode;

using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 원정대 시스템 - AFK 보상
    /// 원정 파견 → 시간 경과 → 보상 수령
    /// </summary>
    public class ExpeditionSystem : NetworkBehaviour
    {
        public static ExpeditionSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int maxActiveExpeditions = 3;

        // 서버: 플레이어별 활성 원정
        private Dictionary<ulong, List<ActiveExpedition>> playerExpeditions = new Dictionary<ulong, List<ActiveExpedition>>();

        // 로컬
        private List<ActiveExpedition> localExpeditions = new List<ActiveExpedition>();

        // 원정 정의
        private ExpeditionTemplate[] templates;

        // 이벤트
        public System.Action OnExpeditionUpdated;

        // 접근자
        public IReadOnlyList<ActiveExpedition> LocalExpeditions => localExpeditions;
        public int MaxActiveExpeditions => maxActiveExpeditions;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeTemplates();
        }

        private void Update()
        {
            if (!IsServer) return;
            // 주기적 완료 체크 (1초마다)
            if (Time.frameCount % 60 == 0) CheckCompletedExpeditions();
        }

        /// <summary>
        /// 원정 시작
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void StartExpeditionServerRpc(int templateIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var expeditions = GetOrCreate(clientId);

            if (expeditions.Count >= maxActiveExpeditions)
            {
                SendMessageClientRpc("동시 진행 가능한 원정 수를 초과했습니다.", clientId);
                return;
            }

            if (templateIndex < 0 || templateIndex >= templates.Length)
            {
                SendMessageClientRpc("유효하지 않은 원정입니다.", clientId);
                return;
            }

            var template = templates[templateIndex];

            // 골드 비용
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            if (statsData.Gold < template.goldCost)
            {
                SendMessageClientRpc($"원정 비용 {template.goldCost}G가 부족합니다.", clientId);
                return;
            }

            // 레벨 체크
            if (statsData.CurrentLevel < template.requiredLevel)
            {
                SendMessageClientRpc($"레벨 {template.requiredLevel} 이상 필요합니다.", clientId);
                return;
            }

            statsData.ChangeGold(-template.goldCost);

            // 성공률 계산
            float successRate = CalculateSuccessRate(statsData.CurrentLevel, template);

            var expedition = new ActiveExpedition
            {
                expeditionId = System.Guid.NewGuid().ToString("N").Substring(0, 8),
                templateIndex = templateIndex,
                startTime = Time.time,
                duration = template.durationSeconds,
                successRate = successRate,
                isCompleted = false,
                isSuccess = false,
                isClaimed = false
            };

            expeditions.Add(expedition);

            ExpeditionStartedClientRpc(expedition.expeditionId, templateIndex,
                expedition.duration, successRate, template.goldCost, clientId);
        }

        /// <summary>
        /// 완료된 원정 보상 수령
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ClaimExpeditionServerRpc(string expeditionId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var expeditions = GetOrCreate(clientId);
            string id = expeditionId;

            ActiveExpedition target = default;
            int targetIndex = -1;
            for (int i = 0; i < expeditions.Count; i++)
            {
                if (expeditions[i].expeditionId == id)
                {
                    target = expeditions[i];
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex < 0)
            {
                SendMessageClientRpc("해당 원정을 찾을 수 없습니다.", clientId);
                return;
            }

            if (!target.isCompleted)
            {
                SendMessageClientRpc("원정이 아직 완료되지 않았습니다.", clientId);
                return;
            }

            if (target.isClaimed)
            {
                SendMessageClientRpc("이미 보상을 수령했습니다.", clientId);
                return;
            }

            target.isClaimed = true;
            expeditions[targetIndex] = target;

            var template = templates[target.templateIndex];

            if (target.isSuccess)
            {
                GrantRewards(clientId, template);
                ExpeditionClaimedClientRpc(expeditionId, true, template.rewardDescription, clientId);
            }
            else
            {
                // 실패 시 위로 보상 (30%)
                GrantConsolationRewards(clientId, template);
                ExpeditionClaimedClientRpc(expeditionId, false, "실패 위로 보상", clientId);
            }

            // 완료 후 목록에서 제거
            expeditions.RemoveAt(targetIndex);
        }

        /// <summary>
        /// 원정 즉시 완료 (골드 소비)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RushExpeditionServerRpc(string expeditionId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var expeditions = GetOrCreate(clientId);
            string id = expeditionId;

            for (int i = 0; i < expeditions.Count; i++)
            {
                if (expeditions[i].expeditionId == id && !expeditions[i].isCompleted)
                {
                    var exp = expeditions[i];
                    float remaining = exp.duration - (Time.time - exp.startTime);
                    int rushCost = Mathf.Max(100, Mathf.RoundToInt(remaining / 60f * 50)); // 분당 50G

                    var statsData = GetPlayerStatsData(clientId);
                    if (statsData == null) return;
                    if (statsData.Gold < rushCost)
                    {
                        SendMessageClientRpc($"즉시 완료 비용 {rushCost}G가 부족합니다.", clientId);
                        return;
                    }

                    statsData.ChangeGold(-rushCost);
                    CompleteExpedition(clientId, i);
                    ExpeditionRushedClientRpc(expeditionId, rushCost, clientId);
                    return;
                }
            }
        }

        /// <summary>
        /// 동기화 요청
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestSyncServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var expeditions = GetOrCreate(clientId);

            ClearLocalExpeditionsClientRpc(clientId);
            foreach (var exp in expeditions)
            {
                SyncExpeditionClientRpc(exp.expeditionId,
                    exp.templateIndex, exp.startTime, exp.duration,
                    exp.successRate, exp.isCompleted, exp.isSuccess, clientId);
            }
            SyncCompleteClientRpc(expeditions.Count, clientId);
        }

        /// <summary>
        /// 원정 템플릿 목록
        /// </summary>
        public ExpeditionTemplate[] GetTemplates() => templates;

        private void CheckCompletedExpeditions()
        {
            foreach (var kvp in playerExpeditions)
            {
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    var exp = kvp.Value[i];
                    if (!exp.isCompleted && Time.time - exp.startTime >= exp.duration)
                    {
                        CompleteExpedition(kvp.Key, i);
                    }
                }
            }
        }

        private void CompleteExpedition(ulong clientId, int index)
        {
            var expeditions = GetOrCreate(clientId);
            if (index >= expeditions.Count) return;

            var exp = expeditions[index];
            exp.isCompleted = true;
            exp.isSuccess = Random.Range(0f, 1f) <= exp.successRate;
            expeditions[index] = exp;

            ExpeditionCompletedClientRpc(exp.expeditionId,
                exp.isSuccess, clientId);
        }

        private float CalculateSuccessRate(int playerLevel, ExpeditionTemplate template)
        {
            float baseRate = template.baseSuccessRate;
            float levelBonus = Mathf.Max(0, (playerLevel - template.requiredLevel) * 0.02f);
            return Mathf.Clamp01(baseRate + levelBonus);
        }

        private void GrantRewards(ulong clientId, ExpeditionTemplate template)
        {
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            if (template.goldReward > 0) statsData.ChangeGold(template.goldReward);
            if (template.expReward > 0) statsData.AddExperience(template.expReward);

            // 아이템 보상
            if (!string.IsNullOrEmpty(template.guaranteedItemId))
            {
                var inventory = GetInventoryManager(clientId);
                if (inventory != null)
                {
                    var item = new ItemInstance();
                    item.Initialize(template.guaranteedItemId, 1);
                    if (!inventory.AddItem(item) && MailSystem.Instance != null)
                    {
                        var mailAttachment = new MailAttachment { itemId = template.guaranteedItemId, quantity = 1 };
                        MailSystem.Instance.SendSystemMail(clientId, "원정 보상",
                            "인벤토리 부족으로 우편 발송", MailType.SystemReward, mailAttachment, true);
                    }
                }
            }

            // 보너스 아이템 (확률)
            if (!string.IsNullOrEmpty(template.bonusItemId) && Random.Range(0f, 1f) <= template.bonusItemChance)
            {
                var inventory = GetInventoryManager(clientId);
                if (inventory != null)
                {
                    var item = new ItemInstance();
                    item.Initialize(template.bonusItemId, 1);
                    if (!inventory.AddItem(item) && MailSystem.Instance != null)
                    {
                        var mailAttachment = new MailAttachment { itemId = template.bonusItemId, quantity = 1 };
                        MailSystem.Instance.SendSystemMail(clientId, "원정 보너스",
                            "보너스 아이템 우편 발송", MailType.SystemReward, mailAttachment, true);
                    }
                }
            }

            // 시즌 경험치
            if (SeasonPassSystem.Instance != null)
                SeasonPassSystem.Instance.AddSeasonExp(clientId, template.seasonExp, "원정 완료");

            // 평판
            if (ReputationSystem.Instance != null && template.factionRepGain > 0)
                ReputationSystem.Instance.ChangeReputation(clientId, template.reputationFaction, template.factionRepGain, "원정 완료");
        }

        private void GrantConsolationRewards(ulong clientId, ExpeditionTemplate template)
        {
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            int gold = Mathf.RoundToInt(template.goldReward * 0.3f);
            int exp = Mathf.RoundToInt(template.expReward * 0.3f);
            if (gold > 0) statsData.ChangeGold(gold);
            if (exp > 0) statsData.AddExperience(exp);
        }

        #region ClientRPCs

        [ClientRpc]
        private void ExpeditionStartedClientRpc(string expId, int templateIndex,
            float duration, float successRate, int cost, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localExpeditions.Add(new ActiveExpedition
            {
                expeditionId = expId,
                templateIndex = templateIndex,
                startTime = Time.time,
                duration = duration,
                successRate = successRate,
                isCompleted = false,
                isSuccess = false,
                isClaimed = false
            });
            OnExpeditionUpdated?.Invoke();

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"원정 출발! ({templates[templateIndex].expeditionName}) -{cost}G", NotificationType.System);
        }

        [ClientRpc]
        private void ExpeditionCompletedClientRpc(string expId, bool success, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            for (int i = 0; i < localExpeditions.Count; i++)
            {
                if (localExpeditions[i].expeditionId == expId)
                {
                    var e = localExpeditions[i];
                    e.isCompleted = true;
                    e.isSuccess = success;
                    localExpeditions[i] = e;
                    break;
                }
            }
            OnExpeditionUpdated?.Invoke();

            string result = success ? "<color=#44FF44>성공!</color>" : "<color=#FF4444>실패</color>";
            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"원정 완료: {result} 보상을 수령하세요.", NotificationType.System);
        }

        [ClientRpc]
        private void ExpeditionClaimedClientRpc(string expId, bool success,
            string rewardDesc, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localExpeditions.RemoveAll(e => e.expeditionId == expId);
            OnExpeditionUpdated?.Invoke();

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"원정 보상 수령: {rewardDesc}", NotificationType.System);
        }

        [ClientRpc]
        private void ExpeditionRushedClientRpc(string expId, int cost, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"원정 즉시 완료! -{cost}G", NotificationType.System);
        }

        [ClientRpc]
        private void ClearLocalExpeditionsClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localExpeditions.Clear();
        }

        [ClientRpc]
        private void SyncExpeditionClientRpc(string expId, int templateIndex,
            float startTime, float duration, float successRate,
            bool isCompleted, bool isSuccess, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localExpeditions.Add(new ActiveExpedition
            {
                expeditionId = expId,
                templateIndex = templateIndex,
                startTime = startTime,
                duration = duration,
                successRate = successRate,
                isCompleted = isCompleted,
                isSuccess = isSuccess,
                isClaimed = false
            });
        }

        [ClientRpc]
        private void SyncCompleteClientRpc(int count, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            OnExpeditionUpdated?.Invoke();
        }

        [ClientRpc]
        private void SendMessageClientRpc(string message, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification(message, NotificationType.Warning);
        }

        #endregion

        #region Utility

        private List<ActiveExpedition> GetOrCreate(ulong clientId)
        {
            if (!playerExpeditions.ContainsKey(clientId))
                playerExpeditions[clientId] = new List<ActiveExpedition>();
            return playerExpeditions[clientId];
        }

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        private InventoryManager GetInventoryManager(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject?.GetComponent<InventoryManager>();
        }

        #endregion

        #region 원정 데이터 초기화

        private void InitializeTemplates()
        {
            templates = new ExpeditionTemplate[]
            {
                // 정찰 (1시간)
                new ExpeditionTemplate("정찰: 근교 숲", "마을 근처 숲을 정찰합니다.",
                    3600, 1, 100, 0.90f, 500, 200, 50,
                    "consumable_healthpotion_small", null, 0f,
                    Faction.TownGuard, 50, "골드 500 + 경험치 200"),
                new ExpeditionTemplate("정찰: 강가 탐색", "강가 주변을 탐색하여 자원을 찾습니다.",
                    3600, 1, 150, 0.85f, 600, 250, 50,
                    "consumable_manapotion_small", null, 0f,
                    Faction.AdventurerGuild, 50, "골드 600 + 경험치 250"),

                // 탐험 (4시간)
                new ExpeditionTemplate("탐험: 고블린 동굴 외곽", "고블린 동굴 외곽을 탐험합니다.",
                    14400, 5, 500, 0.75f, 2000, 800, 100,
                    "consumable_healthpotion_medium", "consumable_strengthscroll", 0.20f,
                    Faction.AdventurerGuild, 100, "골드 2000 + 경험치 800 + 포션"),
                new ExpeditionTemplate("탐험: 폐광산 조사", "오래된 광산을 조사합니다.",
                    14400, 5, 600, 0.70f, 2500, 1000, 100,
                    "consumable_manapotion_medium", "consumable_speedscroll", 0.15f,
                    Faction.MerchantGuild, 100, "골드 2500 + 경험치 1000 + 재료"),

                // 원정 (8시간)
                new ExpeditionTemplate("원정: 어둠의 숲 깊은 곳", "어둠의 숲 깊숙이 진입합니다.",
                    28800, 8, 1000, 0.65f, 5000, 2000, 200,
                    "consumable_healthpotion_large", "consumable_protectionscroll", 0.25f,
                    Faction.ShadowHunter, 200, "골드 5000 + 경험치 2000 + 희귀 아이템"),
                new ExpeditionTemplate("원정: 고대 유적 탐사", "고대 유적을 탐사합니다.",
                    28800, 8, 1200, 0.60f, 6000, 2500, 200,
                    "consumable_manapotion_large", "consumable_strengthscroll", 0.30f,
                    Faction.AncientGuardian, 200, "골드 6000 + 경험치 2500 + 유물"),

                // 대원정 (24시간)
                new ExpeditionTemplate("대원정: 드래곤의 영역", "드래곤이 사는 지역을 정복합니다.",
                    86400, 12, 3000, 0.50f, 15000, 5000, 500,
                    "consumable_healthpotion_max", "consumable_resurrectionscroll", 0.35f,
                    Faction.AncientGuardian, 500, "골드 15000 + 경험치 5000 + 전설급"),
                new ExpeditionTemplate("대원정: 심연의 문", "심연의 문 너머를 탐험합니다.",
                    86400, 15, 5000, 0.40f, 25000, 8000, 800,
                    "consumable_resurrectionscroll", "consumable_townportal", 0.40f,
                    Faction.ShadowHunter, 800, "골드 25000 + 경험치 8000 + 최고급 보상"),
            };
        }

        #endregion

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }

    /// <summary>
    /// 원정 템플릿
    /// </summary>
    [System.Serializable]
    public struct ExpeditionTemplate
    {
        public string expeditionName;
        public string description;
        public float durationSeconds;
        public int requiredLevel;
        public int goldCost;
        public float baseSuccessRate;
        public int goldReward;
        public int expReward;
        public int seasonExp;
        public string guaranteedItemId;
        public string bonusItemId;
        public float bonusItemChance;
        public Faction reputationFaction;
        public int factionRepGain;
        public string rewardDescription;

        public ExpeditionTemplate(string name, string desc, float duration, int reqLevel, int cost,
            float successRate, int gold, int exp, int sExp,
            string item, string bonus, float bonusChance,
            Faction faction, int repGain, string rewardDesc)
        {
            expeditionName = name;
            description = desc;
            durationSeconds = duration;
            requiredLevel = reqLevel;
            goldCost = cost;
            baseSuccessRate = successRate;
            goldReward = gold;
            expReward = exp;
            seasonExp = sExp;
            guaranteedItemId = item;
            bonusItemId = bonus;
            bonusItemChance = bonusChance;
            reputationFaction = faction;
            factionRepGain = repGain;
            rewardDescription = rewardDesc;
        }
    }

    /// <summary>
    /// 활성 원정 데이터
    /// </summary>
    [System.Serializable]
    public struct ActiveExpedition
    {
        public string expeditionId;
        public int templateIndex;
        public float startTime;
        public float duration;
        public float successRate;
        public bool isCompleted;
        public bool isSuccess;
        public bool isClaimed;

        public float RemainingTime => Mathf.Max(0, duration - (Time.time - startTime));
        public float Progress => Mathf.Clamp01((Time.time - startTime) / duration);
    }
}
