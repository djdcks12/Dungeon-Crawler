using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 퀘스트 관리 시스템
    /// 퀘스트 수락, 진행, 완료, 보상 처리
    /// </summary>
    public class QuestManager : NetworkBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Header("설정")]
        [SerializeField] private int maxActiveQuests = 10;
        [SerializeField] private int maxDailyQuests = 3;

        // 전체 퀘스트 DB (Resources에서 로드)
        private Dictionary<string, QuestData> questDatabase = new Dictionary<string, QuestData>();

        // 플레이어별 퀘스트 진행 (서버에서 관리)
        private Dictionary<ulong, List<QuestProgress>> playerQuests = new Dictionary<ulong, List<QuestProgress>>();
        private Dictionary<ulong, HashSet<string>> completedQuestIds = new Dictionary<ulong, HashSet<string>>();

        // 로컬 플레이어 퀘스트 (클라이언트 캐시)
        private List<QuestProgress> localQuests = new List<QuestProgress>();

        // 이벤트
        public System.Action<string, QuestStatus> OnQuestStatusChanged;
        public System.Action<string, int, int, int> OnQuestProgressUpdated; // questId, objectiveIndex, current, required
        public System.Action<QuestData> OnQuestCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LoadQuestDatabase();
        }

        /// <summary>
        /// Resources에서 퀘스트 데이터 로드
        /// </summary>
        private void LoadQuestDatabase()
        {
            questDatabase.Clear();
            var allQuests = Resources.LoadAll<QuestData>("");
            foreach (var quest in allQuests)
            {
                if (!string.IsNullOrEmpty(quest.QuestId))
                {
                    questDatabase[quest.QuestId] = quest;
                }
            }
            Debug.Log($"QuestManager: Loaded {questDatabase.Count} quests");
        }

        /// <summary>
        /// 퀘스트 데이터 가져오기
        /// </summary>
        public QuestData GetQuestData(string questId)
        {
            questDatabase.TryGetValue(questId, out var data);
            return data;
        }

        /// <summary>
        /// 전체 퀘스트 목록
        /// </summary>
        public List<QuestData> GetAllQuests()
        {
            return new List<QuestData>(questDatabase.Values);
        }

        /// <summary>
        /// 수락 가능한 퀘스트 목록
        /// </summary>
        public List<QuestData> GetAvailableQuests(int playerLevel)
        {
            var available = new List<QuestData>();
            var completed = GetLocalCompletedIds();

            foreach (var quest in questDatabase.Values)
            {
                if (quest.RequiredLevel > playerLevel) continue;

                // 이미 완료했고 반복 불가
                if (completed.Contains(quest.QuestId) && !quest.IsRepeatable) continue;

                // 이미 진행 중
                if (IsQuestActive(quest.QuestId)) continue;

                // 선행 퀘스트 미완료
                if (!string.IsNullOrEmpty(quest.PrerequisiteQuestId) &&
                    !completed.Contains(quest.PrerequisiteQuestId)) continue;

                available.Add(quest);
            }

            return available;
        }

        /// <summary>
        /// 퀘스트 수락
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void AcceptQuestServerRpc(string questId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!questDatabase.TryGetValue(questId, out var questData))
            {
                NotifyQuestResultClientRpc(clientId, questId, false, "퀘스트를 찾을 수 없습니다.");
                return;
            }

            var quests = GetPlayerQuests(clientId);

            // 활성 퀘스트 수 확인
            int activeCount = 0;
            foreach (var q in quests)
            {
                if (q.status == QuestStatus.Active || q.status == QuestStatus.Completed)
                    activeCount++;
            }
            if (activeCount >= maxActiveQuests)
            {
                NotifyQuestResultClientRpc(clientId, questId, false, "활성 퀘스트가 가득 찼습니다.");
                return;
            }

            // 이미 진행 중인지 확인
            foreach (var q in quests)
            {
                if (q.questId == questId && q.status == QuestStatus.Active)
                {
                    NotifyQuestResultClientRpc(clientId, questId, false, "이미 진행 중인 퀘스트입니다.");
                    return;
                }
            }

            // 완료 체크 (반복 불가)
            if (!questData.IsRepeatable && GetPlayerCompletedIds(clientId).Contains(questId))
            {
                NotifyQuestResultClientRpc(clientId, questId, false, "이미 완료한 퀘스트입니다.");
                return;
            }

            // 퀘스트 수락
            var progress = new QuestProgress(questData);
            quests.Add(progress);

            NotifyQuestResultClientRpc(clientId, questId, true, $"퀘스트 수락: {questData.QuestName}");
            SyncQuestProgressClientRpc(clientId, questId, QuestStatus.Active, new int[questData.Objectives.Length]);
        }

        /// <summary>
        /// 퀘스트 포기
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void AbandonQuestServerRpc(string questId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var quests = GetPlayerQuests(clientId);

            for (int i = quests.Count - 1; i >= 0; i--)
            {
                if (quests[i].questId == questId && quests[i].status == QuestStatus.Active)
                {
                    quests.RemoveAt(i);
                    NotifyQuestResultClientRpc(clientId, questId, true, "퀘스트를 포기했습니다.");
                    SyncQuestRemovedClientRpc(clientId, questId);
                    return;
                }
            }
        }

        /// <summary>
        /// 퀘스트 진행도 업데이트 (서버에서 호출)
        /// </summary>
        public void UpdateQuestProgress(ulong clientId, QuestType type, string targetId, int amount = 1)
        {
            if (!IsServer) return;

            var quests = GetPlayerQuests(clientId);

            foreach (var progress in quests)
            {
                if (progress.status != QuestStatus.Active) continue;

                var questData = GetQuestData(progress.questId);
                if (questData == null) continue;

                bool changed = false;
                for (int i = 0; i < questData.Objectives.Length; i++)
                {
                    var obj = questData.Objectives[i];
                    if (obj.objectiveType != type) continue;

                    // targetId 매칭 (빈 문자열이면 모든 대상)
                    if (!string.IsNullOrEmpty(obj.targetId) && obj.targetId != targetId) continue;

                    if (i < progress.currentCounts.Length && progress.currentCounts[i] < obj.requiredCount)
                    {
                        progress.currentCounts[i] = Mathf.Min(progress.currentCounts[i] + amount, obj.requiredCount);
                        changed = true;

                        // 클라이언트에 진행도 동기화
                        SyncQuestObjectiveClientRpc(clientId, progress.questId, i, progress.currentCounts[i], obj.requiredCount);
                    }
                }

                // 모든 목표 달성 체크
                if (changed && progress.IsAllObjectivesComplete(questData))
                {
                    progress.status = QuestStatus.Completed;
                    progress.completedTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    SyncQuestProgressClientRpc(clientId, progress.questId, QuestStatus.Completed, progress.currentCounts);
                    Debug.Log($"Quest completed: {questData.QuestName} for client {clientId}");
                }
            }
        }

        /// <summary>
        /// 보상 수령
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ClaimRewardServerRpc(string questId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var quests = GetPlayerQuests(clientId);

            for (int i = 0; i < quests.Count; i++)
            {
                if (quests[i].questId != questId || quests[i].status != QuestStatus.Completed)
                    continue;

                var questData = GetQuestData(questId);
                if (questData == null) continue;

                // 플레이어 찾기
                if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                    return;
                var playerObject = client.PlayerObject;
                if (playerObject == null) return;

                var statsManager = playerObject.GetComponent<PlayerStatsManager>();
                var inventoryManager = playerObject.GetComponent<InventoryManager>();

                // 보상 지급
                var reward = questData.Reward;
                if (statsManager != null)
                {
                    if (reward.experienceReward > 0)
                        statsManager.AddExperience(reward.experienceReward);
                    if (reward.goldReward > 0)
                        statsManager.ChangeGold(reward.goldReward);
                }

                if (inventoryManager != null && !string.IsNullOrEmpty(reward.itemRewardId) && reward.itemRewardCount > 0)
                {
                    var itemData = ItemDatabase.GetItem(reward.itemRewardId);
                    if (itemData != null)
                    {
                        var itemInstance = new ItemInstance(itemData, reward.itemRewardCount);
                        inventoryManager.TryAddItemDirect(itemInstance, out _);
                    }
                }

                // 상태 변경
                quests[i].status = QuestStatus.Rewarded;
                GetPlayerCompletedIds(clientId).Add(questId);

                NotifyQuestRewardClientRpc(clientId, questId, reward.experienceReward, reward.goldReward, reward.itemRewardId);
                SyncQuestProgressClientRpc(clientId, questId, QuestStatus.Rewarded, quests[i].currentCounts);

                Debug.Log($"Quest reward claimed: {questData.QuestName} for client {clientId}");
                return;
            }
        }

        /// <summary>
        /// 몬스터 처치 이벤트 처리
        /// </summary>
        public void OnMonsterKilled(ulong killerClientId, string monsterRaceId, bool isBoss)
        {
            if (!IsServer) return;
            UpdateQuestProgress(killerClientId, QuestType.Kill, monsterRaceId);
            if (isBoss)
                UpdateQuestProgress(killerClientId, QuestType.BossKill, monsterRaceId);
        }

        /// <summary>
        /// 아이템 획득 이벤트 처리
        /// </summary>
        public void OnItemCollected(ulong clientId, string itemId, int count)
        {
            if (!IsServer) return;
            UpdateQuestProgress(clientId, QuestType.Collect, itemId, count);
        }

        /// <summary>
        /// 던전 층 도달 이벤트 처리
        /// </summary>
        public void OnFloorReached(ulong clientId, string dungeonId, int floor)
        {
            if (!IsServer) return;
            UpdateQuestProgress(clientId, QuestType.Explore, dungeonId);
        }

        /// <summary>
        /// 레벨업 이벤트 처리
        /// </summary>
        public void OnPlayerLevelUp(ulong clientId, int newLevel)
        {
            if (!IsServer) return;
            UpdateQuestProgress(clientId, QuestType.LevelUp, "", 1);
        }

        // === 내부 헬퍼 ===

        private List<QuestProgress> GetPlayerQuests(ulong clientId)
        {
            if (!playerQuests.ContainsKey(clientId))
                playerQuests[clientId] = new List<QuestProgress>();
            return playerQuests[clientId];
        }

        private HashSet<string> GetPlayerCompletedIds(ulong clientId)
        {
            if (!completedQuestIds.ContainsKey(clientId))
                completedQuestIds[clientId] = new HashSet<string>();
            return completedQuestIds[clientId];
        }

        private HashSet<string> GetLocalCompletedIds()
        {
            if (!IsServer) return new HashSet<string>();
            ulong localId = NetworkManager.Singleton.LocalClientId;
            return GetPlayerCompletedIds(localId);
        }

        private bool IsQuestActive(string questId)
        {
            foreach (var q in localQuests)
            {
                if (q.questId == questId && (q.status == QuestStatus.Active || q.status == QuestStatus.Completed))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 로컬 활성 퀘스트 목록
        /// </summary>
        public List<QuestProgress> GetActiveQuests()
        {
            var result = new List<QuestProgress>();
            foreach (var q in localQuests)
            {
                if (q.status == QuestStatus.Active || q.status == QuestStatus.Completed)
                    result.Add(q);
            }
            return result;
        }

        /// <summary>
        /// 특정 퀘스트 진행 상태
        /// </summary>
        public QuestProgress GetQuestProgress(string questId)
        {
            foreach (var q in localQuests)
            {
                if (q.questId == questId) return q;
            }
            return null;
        }

        // === ClientRpc ===

        [ClientRpc]
        private void NotifyQuestResultClientRpc(ulong targetClientId, string questId, bool success, string message)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            Debug.Log($"{(success ? "O" : "X")} Quest: {message}");

            if (success && questDatabase.TryGetValue(questId, out var data))
            {
                OnQuestStatusChanged?.Invoke(questId, QuestStatus.Active);
            }
        }

        [ClientRpc]
        private void SyncQuestProgressClientRpc(ulong targetClientId, string questId, QuestStatus status, int[] counts)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            // 로컬 캐시 업데이트
            QuestProgress existing = null;
            foreach (var q in localQuests)
            {
                if (q.questId == questId) { existing = q; break; }
            }

            if (existing != null)
            {
                existing.status = status;
                existing.currentCounts = counts;
            }
            else
            {
                var newProgress = new QuestProgress(GetQuestData(questId) ?? ScriptableObject.CreateInstance<QuestData>());
                newProgress.questId = questId;
                newProgress.status = status;
                newProgress.currentCounts = counts;
                localQuests.Add(newProgress);
            }

            OnQuestStatusChanged?.Invoke(questId, status);

            if (status == QuestStatus.Completed && questDatabase.TryGetValue(questId, out var data))
            {
                OnQuestCompleted?.Invoke(data);
            }
        }

        [ClientRpc]
        private void SyncQuestObjectiveClientRpc(ulong targetClientId, string questId, int objectiveIndex, int current, int required)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            foreach (var q in localQuests)
            {
                if (q.questId == questId && objectiveIndex < q.currentCounts.Length)
                {
                    q.currentCounts[objectiveIndex] = current;
                    break;
                }
            }

            OnQuestProgressUpdated?.Invoke(questId, objectiveIndex, current, required);
        }

        [ClientRpc]
        private void SyncQuestRemovedClientRpc(ulong targetClientId, string questId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            for (int i = localQuests.Count - 1; i >= 0; i--)
            {
                if (localQuests[i].questId == questId)
                {
                    localQuests.RemoveAt(i);
                    break;
                }
            }

            OnQuestStatusChanged?.Invoke(questId, QuestStatus.Available);
        }

        [ClientRpc]
        private void NotifyQuestRewardClientRpc(ulong targetClientId, string questId, long exp, long gold, string itemId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            string msg = $"퀘스트 보상 수령!";
            if (exp > 0) msg += $" EXP+{exp}";
            if (gold > 0) msg += $" Gold+{gold}";
            if (!string.IsNullOrEmpty(itemId)) msg += $" 아이템: {itemId}";
            Debug.Log(msg);

            if (CombatLogUI.Instance != null)
                CombatLogUI.Instance.LogSystem(msg);
        }

        /// <summary>
        /// 퀘스트 진행 상태 복원 (세이브 로드용, 서버에서 호출)
        /// </summary>
        public void RestoreQuestProgress(ulong clientId, string questId, QuestStatus status, int[] currentCounts, long acceptedTime, long completedTime)
        {
            if (!IsServer) return;

            var questData = GetQuestData(questId);
            if (questData == null) return;

            var quests = GetPlayerQuests(clientId);

            // 이미 존재하면 업데이트
            for (int i = 0; i < quests.Count; i++)
            {
                if (quests[i].questId == questId)
                {
                    quests[i].status = status;
                    quests[i].currentCounts = currentCounts ?? new int[questData.Objectives.Length];
                    quests[i].acceptedTime = acceptedTime;
                    quests[i].completedTime = completedTime;
                    SyncQuestProgressClientRpc(clientId, questId, status, quests[i].currentCounts);
                    return;
                }
            }

            // 새로 추가
            var progress = new QuestProgress(questData);
            progress.status = status;
            progress.currentCounts = currentCounts ?? new int[questData.Objectives.Length];
            progress.acceptedTime = acceptedTime;
            progress.completedTime = completedTime;
            quests.Add(progress);

            if (status == QuestStatus.Rewarded)
            {
                GetPlayerCompletedIds(clientId).Add(questId);
            }

            SyncQuestProgressClientRpc(clientId, questId, status, progress.currentCounts);
        }

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                OnQuestStatusChanged = null;
                OnQuestProgressUpdated = null;
                OnQuestCompleted = null;
                Instance = null;
            }
            base.OnDestroy();
        }
    }
}
