using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public enum ProphecyType
    {
        Kill,       // Kill specific monsters
        Discover,   // Explore specific zone
        Enhance,    // Upgrade equipment to +N
        Craft,      // Use a specific recipe
        Collect,    // Gather N materials
        Boss        // Defeat a specific boss
    }

    public enum ProphecyGrade { Normal, Rare, Legendary }

    [Serializable]
    public class ProphecyDefinition
    {
        public int id;
        public string name;
        public string description;
        public ProphecyType type;
        public ProphecyGrade grade;
        public string targetKey;     // monster race, zone name, boss id, etc.
        public int requiredCount;
        public long goldReward;
        public long expReward;
        public string itemReward;    // item id or empty
        public int chainNext;        // -1 if not chained, else next prophecy id

        public ProphecyDefinition(int id, string name, string desc, ProphecyType type, ProphecyGrade grade,
            string targetKey, int count, long gold, long exp, string item = "", int chainNext = -1)
        {
            this.id = id;
            this.name = name;
            this.description = desc;
            this.type = type;
            this.grade = grade;
            this.targetKey = targetKey;
            this.requiredCount = count;
            this.goldReward = gold;
            this.expReward = exp;
            this.itemReward = item;
            this.chainNext = chainNext;
        }
    }

    [Serializable]
    public class ActiveProphecy
    {
        public int prophecyId;
        public int currentProgress;
        public float activatedTime;
        public bool isCompleted;
    }

    public class ProphecySystem : NetworkBehaviour
    {
        public static ProphecySystem Instance { get; private set; }

        public const int MaxActiveProphecies = 5;
        public const int DailyProphecyLimit = 3;
        public const long DiscardCost = 500;

        public Action<int> OnProphecyCompleted;
        public Action<int, int> OnProphecyProgress; // prophecyId, currentProgress

        // All prophecy definitions
        private static readonly ProphecyDefinition[] AllProphecies = new ProphecyDefinition[]
        {
            // Kill prophecies
            new ProphecyDefinition(0, "고블린 사냥꾼", "고블린을 5마리 처치하세요", ProphecyType.Kill, ProphecyGrade.Normal, "Goblin", 5, 1000, 500),
            new ProphecyDefinition(1, "오크 토벌", "오크를 5마리 처치하세요", ProphecyType.Kill, ProphecyGrade.Normal, "Orc", 5, 1200, 600),
            new ProphecyDefinition(2, "언데드 퇴치", "언데드를 8마리 처치하세요", ProphecyType.Kill, ProphecyGrade.Rare, "Undead", 8, 2500, 1200),
            new ProphecyDefinition(3, "야수 포식자", "야수를 10마리 처치하세요", ProphecyType.Kill, ProphecyGrade.Rare, "Beast", 10, 3000, 1500),

            // Discover prophecies
            new ProphecyDefinition(4, "숲의 탐험가", "어둠의 숲을 탐험하세요", ProphecyType.Discover, ProphecyGrade.Normal, "DarkForest", 1, 800, 400),
            new ProphecyDefinition(5, "용기의 증명", "화산 균열에 진입하세요", ProphecyType.Discover, ProphecyGrade.Rare, "VolcanicRift", 1, 2000, 1000),

            // Enhance prophecies
            new ProphecyDefinition(6, "장인의 길", "장비를 +5까지 강화하세요", ProphecyType.Enhance, ProphecyGrade.Normal, "5", 1, 1500, 700),
            new ProphecyDefinition(7, "전설의 대장장이", "장비를 +7까지 강화하세요", ProphecyType.Enhance, ProphecyGrade.Rare, "7", 1, 4000, 2000),

            // Craft prophecies
            new ProphecyDefinition(8, "제작 입문", "아이템을 1회 제작하세요", ProphecyType.Craft, ProphecyGrade.Normal, "any", 1, 600, 300),
            new ProphecyDefinition(9, "숙련 제작자", "아이템을 5회 제작하세요", ProphecyType.Craft, ProphecyGrade.Rare, "any", 5, 2000, 1000),

            // Collect prophecies
            new ProphecyDefinition(10, "자원 수집가", "재료를 10개 수집하세요", ProphecyType.Collect, ProphecyGrade.Normal, "material", 10, 1000, 500),

            // Boss prophecies
            new ProphecyDefinition(11, "고블린 왕 토벌", "고블린 보스를 처치하세요", ProphecyType.Boss, ProphecyGrade.Rare, "GoblinBoss", 1, 5000, 2500),

            // Legendary chain: 3-step prophecy
            new ProphecyDefinition(12, "예언자의 시험 I", "어둠의 숲에서 야수 5마리 처치", ProphecyType.Kill, ProphecyGrade.Legendary, "Beast", 5, 2000, 1000, "", 13),
            new ProphecyDefinition(13, "예언자의 시험 II", "사막에서 악마 8마리 처치", ProphecyType.Kill, ProphecyGrade.Legendary, "Demon", 8, 3000, 1500, "", 14),
            new ProphecyDefinition(14, "예언자의 시험 III", "심연의 보스를 처치하세요", ProphecyType.Boss, ProphecyGrade.Legendary, "AbyssBoss", 1, 10000, 5000, "consumable_resurrectionscroll")
        };

        // Per-player active prophecies
        private Dictionary<ulong, List<ActiveProphecy>> playerProphecies = new Dictionary<ulong, List<ActiveProphecy>>();
        private Dictionary<ulong, int> dailyProphecyCount = new Dictionary<ulong, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        #region Public API

        public static ProphecyDefinition GetProphecyDef(int id)
        {
            if (id < 0 || id >= AllProphecies.Length) return null;
            return AllProphecies[id];
        }

        public List<ActiveProphecy> GetActiveProphecies(ulong clientId)
        {
            if (!playerProphecies.TryGetValue(clientId, out var list))
                return new List<ActiveProphecy>();
            return list.Where(p => !p.isCompleted).ToList();
        }

        /// <summary>
        /// Called by external systems when a relevant event occurs (monster killed, zone entered, etc.)
        /// </summary>
        public void ReportProgress(ulong clientId, ProphecyType type, string targetKey, int amount = 1)
        {
            if (!IsServer) return;
            if (!playerProphecies.TryGetValue(clientId, out var prophecies)) return;

            foreach (var active in prophecies)
            {
                if (active.isCompleted) continue;
                var def = GetProphecyDef(active.prophecyId);
                if (def == null || def.type != type) continue;

                if (def.targetKey == "any" || def.targetKey == targetKey)
                {
                    active.currentProgress = Mathf.Min(active.currentProgress + amount, def.requiredCount);
                    NotifyProgressClientRpc(clientId, active.prophecyId, active.currentProgress, def.requiredCount);
                    OnProphecyProgress?.Invoke(active.prophecyId, active.currentProgress);

                    if (active.currentProgress >= def.requiredCount)
                    {
                        CompleteProphecy(clientId, active, def);
                    }
                }
            }
        }

        #endregion

        #region ServerRpc

        [ServerRpc(RequireOwnership = false)]
        public void RequestProphecyServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!dailyProphecyCount.ContainsKey(clientId))
                dailyProphecyCount[clientId] = 0;

            if (dailyProphecyCount[clientId] >= DailyProphecyLimit)
            {
                NotifyMessageClientRpc(clientId, "오늘의 예언 한도에 도달했습니다.");
                return;
            }

            if (!playerProphecies.ContainsKey(clientId))
                playerProphecies[clientId] = new List<ActiveProphecy>();

            int activeCount = playerProphecies[clientId].Count(p => !p.isCompleted);
            if (activeCount >= MaxActiveProphecies)
            {
                NotifyMessageClientRpc(clientId, $"활성 예언이 가득합니다 ({MaxActiveProphecies}/{MaxActiveProphecies}).");
                return;
            }

            // Pick a random non-active prophecy (exclude chain mid-steps)
            var activeIds = new HashSet<int>(playerProphecies[clientId].Select(p => p.prophecyId));
            var available = AllProphecies
                .Where(p => !activeIds.Contains(p.id) && p.id != 13 && p.id != 14) // exclude chain steps 2,3
                .ToList();

            if (available.Count == 0)
            {
                NotifyMessageClientRpc(clientId, "더 이상 받을 예언이 없습니다.");
                return;
            }

            var chosen = available[UnityEngine.Random.Range(0, available.Count)];
            var active = new ActiveProphecy
            {
                prophecyId = chosen.id,
                currentProgress = 0,
                activatedTime = Time.time,
                isCompleted = false
            };

            playerProphecies[clientId].Add(active);
            dailyProphecyCount[clientId]++;

            string gradeColor = chosen.grade switch
            {
                ProphecyGrade.Rare => "#4444FF",
                ProphecyGrade.Legendary => "#FF8800",
                _ => "#AAAAAA"
            };

            NotifyProphecyReceivedClientRpc(clientId, chosen.id, chosen.name, chosen.description,
                (int)chosen.grade, chosen.requiredCount);

            Debug.Log($"[ProphecySystem] Client {clientId} received prophecy: {chosen.name}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void DiscardProphecyServerRpc(int prophecyId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            if (!playerProphecies.TryGetValue(clientId, out var prophecies)) return;

            var target = prophecies.FirstOrDefault(p => p.prophecyId == prophecyId && !p.isCompleted);
            if (target == null)
            {
                NotifyMessageClientRpc(clientId, "해당 예언을 찾을 수 없습니다.");
                return;
            }

            if (statsData.Gold < DiscardCost)
            {
                NotifyMessageClientRpc(clientId, $"예언 파기 비용 부족 ({DiscardCost}G 필요)");
                return;
            }

            statsData.ChangeGold(-DiscardCost);
            prophecies.Remove(target);

            NotifyMessageClientRpc(clientId, $"예언 '{GetProphecyDef(prophecyId)?.name}' 파기 완료 (-{DiscardCost}G)");
            Debug.Log($"[ProphecySystem] Client {clientId} discarded prophecy {prophecyId}");
        }

        #endregion

        #region Completion

        private void CompleteProphecy(ulong clientId, ActiveProphecy active, ProphecyDefinition def)
        {
            active.isCompleted = true;

            var statsData = GetPlayerStatsData(clientId);
            if (statsData != null)
            {
                if (def.goldReward > 0) statsData.ChangeGold(def.goldReward);
                if (def.expReward > 0) statsData.AddExperience(def.expReward);
            }

            NotifyProphecyCompletedClientRpc(clientId, def.id, def.name, (int)def.grade,
                def.goldReward, def.expReward, def.itemReward);

            OnProphecyCompleted?.Invoke(def.id);

            // Chain prophecy: auto-activate next step
            if (def.chainNext >= 0)
            {
                var nextDef = GetProphecyDef(def.chainNext);
                if (nextDef != null)
                {
                    var nextActive = new ActiveProphecy
                    {
                        prophecyId = nextDef.id,
                        currentProgress = 0,
                        activatedTime = Time.time,
                        isCompleted = false
                    };
                    playerProphecies[clientId].Add(nextActive);

                    NotifyProphecyReceivedClientRpc(clientId, nextDef.id, nextDef.name, nextDef.description,
                        (int)nextDef.grade, nextDef.requiredCount);
                }
            }

            Debug.Log($"[ProphecySystem] Prophecy completed: {def.name} for client {clientId}");
        }

        #endregion

        #region ClientRpc

        [ClientRpc]
        private void NotifyProphecyReceivedClientRpc(ulong targetClientId, int prophecyId, string name,
            string description, int grade, int requiredCount)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            string gradeName = ((ProphecyGrade)grade).ToString();
            NotificationManager.Instance?.ShowNotification(
                $"<color=#FFAA00>예언 수령:</color> [{gradeName}] {name} - {description}",
                NotificationType.System);
        }

        [ClientRpc]
        private void NotifyProphecyCompletedClientRpc(ulong targetClientId, int prophecyId, string name,
            int grade, long gold, long exp, string itemReward)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            string rewards = $"Gold +{gold:N0}, EXP +{exp:N0}";
            if (!string.IsNullOrEmpty(itemReward)) rewards += $", Item: {itemReward}";
            NotificationManager.Instance?.ShowNotification(
                $"<color=#FFD700>예언 달성!</color> {name} ({rewards})",
                NotificationType.System);
        }

        [ClientRpc]
        private void NotifyProgressClientRpc(ulong targetClientId, int prophecyId, int current, int required)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            var def = GetProphecyDef(prophecyId);
            if (def == null) return;
            NotificationManager.Instance?.ShowNotification(
                $"예언 진행: {def.name} ({current}/{required})",
                NotificationType.System);
        }

        [ClientRpc]
        private void NotifyMessageClientRpc(ulong targetClientId, string message)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager.Instance?.ShowNotification(message, NotificationType.Warning);
        }

        #endregion

        #region Helpers

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (NetworkManager.Singleton == null) return null;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        #endregion
    }
}
