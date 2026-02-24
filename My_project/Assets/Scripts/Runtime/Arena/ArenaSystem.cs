using UnityEngine;
using Unity.Netcode;

using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    #region Arena Types

    public enum ArenaRank
    {
        Bronze = 0,
        Silver = 1,
        Gold = 2,
        Platinum = 3,
        Diamond = 4,
        Master = 5,
        Grandmaster = 6
    }

    [System.Serializable]
    public struct ArenaPlayerData : INetworkSerializable, System.IEquatable<ArenaPlayerData>
    {
        public ulong clientId;
        public string playerName;
        public int rating;
        public ArenaRank rank;
        public int wins;
        public int losses;
        public int winStreak;
        public int bestWinStreak;
        public int seasonWins;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref playerName);
            serializer.SerializeValue(ref rating);
            serializer.SerializeValue(ref rank);
            serializer.SerializeValue(ref wins);
            serializer.SerializeValue(ref losses);
            serializer.SerializeValue(ref winStreak);
            serializer.SerializeValue(ref bestWinStreak);
            serializer.SerializeValue(ref seasonWins);
        }

        public bool Equals(ArenaPlayerData other) => clientId == other.clientId;
    }

    public struct ArenaMatch
    {
        public ulong player1;
        public ulong player2;
        public float startTime;
        public int matchId;
    }

    #endregion

    /// <summary>
    /// 아레나 PvP 시스템 - 서버 권위적
    /// 1v1 레이팅 매칭, 시즌 랭킹, 시즌 보상
    /// </summary>
    public class ArenaSystem : NetworkBehaviour
    {
        public static ArenaSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int startingRating = 1000;
        [SerializeField] private int kFactor = 32;
        [SerializeField] private int matchmakingRange = 100;
        [SerializeField] private float matchmakingExpandRate = 10f; // 초당 범위 확장
        [SerializeField] private float matchTimeout = 300f; // 매칭 대기 최대 5분

        // 랭크 기준 레이팅
        private static readonly int[] RankThresholds = { 0, 1100, 1300, 1500, 1700, 1900, 2100 };

        // 서버: 플레이어 데이터
        private Dictionary<ulong, ArenaPlayerData> playerArenaData = new Dictionary<ulong, ArenaPlayerData>();

        // 서버: 매칭 큐
        private List<ulong> matchmakingQueue = new List<ulong>();
        private Dictionary<ulong, float> queueJoinTime = new Dictionary<ulong, float>();

        // 서버: 활성 매치
        private Dictionary<int, ArenaMatch> activeMatches = new Dictionary<int, ArenaMatch>();
        private Dictionary<ulong, int> playerMatchMap = new Dictionary<ulong, int>();
        private int nextMatchId = 1;

        // 로컬
        private ArenaPlayerData localArenaData;
        private bool localInQueue;
        private bool localInMatch;
        private float localQueueTime;

        // 이벤트
        public System.Action OnQueueJoined;
        public System.Action OnQueueLeft;
        public System.Action<ulong> OnMatchFound;
        public System.Action<bool, int> OnMatchResult; // win, ratingChange
        public System.Action<ArenaPlayerData> OnArenaDataUpdated;

        // 접근자
        public ArenaPlayerData LocalData => localArenaData;
        public bool InQueue => localInQueue;
        public bool InMatch => localInMatch;
        public float QueueTime => localInQueue ? Time.time - localQueueTime : 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (!IsServer) return;

            ProcessMatchmaking();
        }

        #region 매칭

        /// <summary>
        /// 매칭 큐 참가
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void JoinQueueServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (matchmakingQueue.Contains(clientId))
            {
                SendMessageClientRpc("이미 대기열에 있습니다.", clientId);
                return;
            }

            if (playerMatchMap.ContainsKey(clientId))
            {
                SendMessageClientRpc("이미 대전 중입니다.", clientId);
                return;
            }

            // 아레나 데이터 초기화 (첫 참가 시)
            if (!playerArenaData.ContainsKey(clientId))
            {
                string name = GetPlayerName(clientId);
                playerArenaData[clientId] = new ArenaPlayerData
                {
                    clientId = clientId,
                    playerName = name,
                    rating = startingRating,
                    rank = ArenaRank.Bronze,
                    wins = 0,
                    losses = 0,
                    winStreak = 0,
                    bestWinStreak = 0,
                    seasonWins = 0
                };
            }

            matchmakingQueue.Add(clientId);
            queueJoinTime[clientId] = Time.time;

            QueueJoinedClientRpc(clientId);
        }

        /// <summary>
        /// 매칭 큐 이탈
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void LeaveQueueServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            RemoveFromQueue(clientId);
            QueueLeftClientRpc(clientId);
        }

        private void RemoveFromQueue(ulong clientId)
        {
            matchmakingQueue.Remove(clientId);
            queueJoinTime.Remove(clientId);
        }

        private void ProcessMatchmaking()
        {
            if (matchmakingQueue.Count < 2) return;

            // 타임아웃 제거
            for (int i = matchmakingQueue.Count - 1; i >= 0; i--)
            {
                ulong pid = matchmakingQueue[i];
                if (queueJoinTime.TryGetValue(pid, out float joinTime) && Time.time - joinTime > matchTimeout)
                {
                    RemoveFromQueue(pid);
                    SendMessageClientRpc("매칭 시간이 초과되었습니다.", pid);
                    QueueLeftClientRpc(pid);
                }
            }

            // 매칭 시도
            var matched = new HashSet<ulong>();
            for (int i = 0; i < matchmakingQueue.Count; i++)
            {
                if (matched.Contains(matchmakingQueue[i])) continue;
                ulong p1 = matchmakingQueue[i];
                int p1Rating = GetRating(p1);
                if (!queueJoinTime.TryGetValue(p1, out float p1JoinTime)) continue;
                float p1Wait = Time.time - p1JoinTime;
                int expandedRange = matchmakingRange + Mathf.FloorToInt(p1Wait * matchmakingExpandRate);

                for (int j = i + 1; j < matchmakingQueue.Count; j++)
                {
                    if (matched.Contains(matchmakingQueue[j])) continue;
                    ulong p2 = matchmakingQueue[j];
                    int p2Rating = GetRating(p2);

                    if (Mathf.Abs(p1Rating - p2Rating) <= expandedRange)
                    {
                        // 매치 생성
                        matched.Add(p1);
                        matched.Add(p2);
                        CreateMatch(p1, p2);
                        break;
                    }
                }
            }

            // 매칭된 플레이어 큐에서 제거
            foreach (ulong pid in matched)
                RemoveFromQueue(pid);
        }

        private void CreateMatch(ulong p1, ulong p2)
        {
            int matchId = nextMatchId++;
            var match = new ArenaMatch
            {
                player1 = p1,
                player2 = p2,
                startTime = Time.time,
                matchId = matchId
            };

            activeMatches[matchId] = match;
            playerMatchMap[p1] = matchId;
            playerMatchMap[p2] = matchId;

            MatchFoundClientRpc(p2, p1);
            MatchFoundClientRpc(p1, p2);
        }

        #endregion

        #region 대전 결과

        /// <summary>
        /// 대전 결과 보고 (서버 내부 호출)
        /// </summary>
        public void ReportMatchResult(ulong winnerClientId, ulong loserClientId)
        {
            if (!IsServer) return;

            // 매치 확인
            if (!playerMatchMap.TryGetValue(winnerClientId, out int matchId)) return;
            if (!activeMatches.ContainsKey(matchId)) return;

            // 레이팅 계산 (Elo)
            int winnerRating = GetRating(winnerClientId);
            int loserRating = GetRating(loserClientId);

            float expectedWinner = 1f / (1f + Mathf.Pow(10f, (loserRating - winnerRating) / 400f));
            float expectedLoser = 1f - expectedWinner;

            int winnerChange = Mathf.RoundToInt(kFactor * (1f - expectedWinner));
            int loserChange = Mathf.RoundToInt(kFactor * (0f - expectedLoser));

            // 승리 연승 보너스
            if (!playerArenaData.TryGetValue(winnerClientId, out var winnerData) ||
                !playerArenaData.TryGetValue(loserClientId, out var loserData))
                return;

            winnerData.wins++;
            winnerData.seasonWins++;
            winnerData.winStreak++;
            if (winnerData.winStreak > winnerData.bestWinStreak)
                winnerData.bestWinStreak = winnerData.winStreak;

            // 연승 보너스 (+2 per streak, max +10)
            int streakBonus = Mathf.Min(winnerData.winStreak * 2, 10);
            winnerChange += streakBonus;

            winnerData.rating = Mathf.Max(0, winnerData.rating + winnerChange);
            winnerData.rank = CalculateRank(winnerData.rating);
            playerArenaData[winnerClientId] = winnerData;

            // 패배자
            loserData.losses++;
            loserData.winStreak = 0;
            loserData.rating = Mathf.Max(0, loserData.rating + loserChange);
            loserData.rank = CalculateRank(loserData.rating);
            playerArenaData[loserClientId] = loserData;

            // 매치 정리
            activeMatches.Remove(matchId);
            playerMatchMap.Remove(winnerClientId);
            playerMatchMap.Remove(loserClientId);

            // 보상 (승자)
            var winnerStats = GetPlayerStatsData(winnerClientId);
            if (winnerStats != null)
            {
                int goldReward = 100 + (int)winnerData.rank * 50;
                winnerStats.ChangeGold(goldReward);
            }

            // 결과 알림
            MatchResultClientRpc(true, winnerChange, winnerData, winnerClientId);
            MatchResultClientRpc(false, loserChange, loserData, loserClientId);
        }

        /// <summary>
        /// 대전 결과 보고 ServerRpc (외부 호출용)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ReportWinServerRpc(ulong loserClientId, ServerRpcParams rpcParams = default)
        {
            ulong winnerClientId = rpcParams.Receive.SenderClientId;
            ReportMatchResult(winnerClientId, loserClientId);
        }

        private ArenaRank CalculateRank(int rating)
        {
            for (int i = RankThresholds.Length - 1; i >= 0; i--)
            {
                if (rating >= RankThresholds[i])
                    return (ArenaRank)i;
            }
            return ArenaRank.Bronze;
        }

        private int GetRating(ulong clientId)
        {
            return playerArenaData.TryGetValue(clientId, out var data) ? data.rating : startingRating;
        }

        #endregion

        #region 랭킹 조회

        /// <summary>
        /// 랭킹 요청
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestRankingServerRpc(int topN, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            var ranked = playerArenaData.Values
                .OrderByDescending(d => d.rating)
                .Take(topN)
                .ToList();

            foreach (var data in ranked)
                SendRankingEntryClientRpc(data, clientId);

            // 내 정보
            if (playerArenaData.TryGetValue(clientId, out var myData))
                SendMyArenaDataClientRpc(myData, clientId);
        }

        /// <summary>
        /// 시즌 종료 보상 (서버에서 호출)
        /// </summary>
        public void EndSeason()
        {
            if (!IsServer) return;

            var ranked = playerArenaData.Values
                .OrderByDescending(d => d.rating)
                .ToList();

            for (int i = 0; i < ranked.Count; i++)
            {
                var data = ranked[i];
                int goldReward = CalculateSeasonReward(data.rank);

                if (MailSystem.Instance != null && goldReward > 0)
                {
                    var attachment = new MailAttachment { gold = goldReward };
                    MailSystem.Instance.SendSystemMail(data.clientId,
                        $"시즌 종료 보상 ({data.rank})",
                        $"시즌 최종 순위: {i + 1}위\n레이팅: {data.rating}\n승/패: {data.wins}/{data.losses}",
                        MailType.SystemReward, attachment, true);
                }
            }

            // 시즌 리셋
            var resetData = new Dictionary<ulong, ArenaPlayerData>();
            foreach (var kvp in playerArenaData)
            {
                var data = kvp.Value;
                // 소프트 리셋: 레이팅을 평균쪽으로 당기기
                data.rating = (data.rating + startingRating) / 2;
                data.rank = CalculateRank(data.rating);
                data.wins = 0;
                data.losses = 0;
                data.winStreak = 0;
                data.seasonWins = 0;
                resetData[kvp.Key] = data;
            }
            playerArenaData = resetData;

            Debug.Log("[Arena] 시즌 종료. 모든 레이팅 소프트 리셋 완료.");
        }

        private int CalculateSeasonReward(ArenaRank rank)
        {
            return rank switch
            {
                ArenaRank.Bronze => 500,
                ArenaRank.Silver => 1000,
                ArenaRank.Gold => 2000,
                ArenaRank.Platinum => 4000,
                ArenaRank.Diamond => 8000,
                ArenaRank.Master => 15000,
                ArenaRank.Grandmaster => 30000,
                _ => 0
            };
        }

        #endregion

        #region ClientRPCs

        [ClientRpc]
        private void QueueJoinedClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localInQueue = true;
            localQueueTime = Time.time;
            OnQueueJoined?.Invoke();

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification("아레나 매칭 대기 중...", NotificationType.System);
        }

        [ClientRpc]
        private void QueueLeftClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localInQueue = false;
            OnQueueLeft?.Invoke();
        }

        [ClientRpc]
        private void MatchFoundClientRpc(ulong opponentClientId, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localInQueue = false;
            localInMatch = true;
            OnMatchFound?.Invoke(opponentClientId);

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification("대전 상대를 찾았습니다!", NotificationType.System);
        }

        [ClientRpc]
        private void MatchResultClientRpc(bool isWin, int ratingChange, ArenaPlayerData updatedData,
            ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localInMatch = false;
            localArenaData = updatedData;
            OnMatchResult?.Invoke(isWin, ratingChange);
            OnArenaDataUpdated?.Invoke(updatedData);

            var notif = NotificationManager.Instance;
            if (notif != null)
            {
                string result = isWin ? "<color=#00FF00>승리</color>" : "<color=#FF0000>패배</color>";
                string change = ratingChange >= 0 ? $"+{ratingChange}" : $"{ratingChange}";
                notif.ShowNotification($"아레나 {result}! 레이팅: {change} ({updatedData.rating})",
                    isWin ? NotificationType.System : NotificationType.Warning);
            }
        }

        [ClientRpc]
        private void SendRankingEntryClientRpc(ArenaPlayerData data, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            // UI에서 이 데이터를 수집하여 표시
            OnArenaDataUpdated?.Invoke(data);
        }

        [ClientRpc]
        private void SendMyArenaDataClientRpc(ArenaPlayerData data, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localArenaData = data;
            OnArenaDataUpdated?.Invoke(data);
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

        private string GetPlayerName(ulong clientId)
        {
            var statsData = GetPlayerStatsData(clientId);
            return statsData != null ? statsData.CharacterName : $"Player_{clientId}";
        }

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        #endregion

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }
}
