using UnityEngine;
using Unity.Netcode;

using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 칭호 시스템 - 업적/던전/PvP/이벤트 기반 칭호 획득 및 표시
    /// </summary>
    public class TitleSystem : NetworkBehaviour
    {
        public static TitleSystem Instance { get; private set; }

        // 서버: 플레이어별 해금된 칭호
        private Dictionary<ulong, HashSet<string>> playerUnlockedTitles = new Dictionary<ulong, HashSet<string>>();

        // 서버: 플레이어별 장착된 칭호
        private Dictionary<ulong, string> playerEquippedTitles = new Dictionary<ulong, string>();

        // 로컬
        private HashSet<string> localUnlockedTitles = new HashSet<string>();
        private string localEquippedTitle = "";

        // 칭호 데이터베이스
        private Dictionary<string, TitleInfo> titleDatabase = new Dictionary<string, TitleInfo>();

        // 이벤트
        public System.Action OnTitlesUpdated;
        public System.Action<string> OnTitleUnlocked; // titleId

        // 접근자
        public IReadOnlyCollection<string> LocalUnlockedTitles => localUnlockedTitles;
        public string LocalEquippedTitle => localEquippedTitle;
        public int UnlockedCount => localUnlockedTitles.Count;
        public int TotalCount => titleDatabase.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeTitleDatabase();
        }

        /// <summary>
        /// 칭호 정보 조회
        /// </summary>
        public TitleInfo GetTitleInfo(string titleId)
        {
            titleDatabase.TryGetValue(titleId, out var info);
            return info;
        }

        /// <summary>
        /// 장착 중인 칭호의 표시 텍스트
        /// </summary>
        public string GetEquippedTitleDisplay()
        {
            if (string.IsNullOrEmpty(localEquippedTitle)) return "";
            if (titleDatabase.TryGetValue(localEquippedTitle, out var info))
                return $"<color={info.colorHex}>[{info.displayName}]</color>";
            return "";
        }

        /// <summary>
        /// 특정 플레이어의 장착 칭호 (서버에서 호출)
        /// </summary>
        public string GetPlayerEquippedTitle(ulong clientId)
        {
            if (playerEquippedTitles.TryGetValue(clientId, out var titleId))
            {
                if (titleDatabase.TryGetValue(titleId, out var info))
                    return info.displayName;
            }
            return "";
        }

        /// <summary>
        /// 칭호 해금 (서버에서 호출)
        /// </summary>
        public void UnlockTitle(ulong clientId, string titleId)
        {
            if (!IsServer) return;
            if (!titleDatabase.ContainsKey(titleId)) return;

            var titles = GetOrCreateUnlocked(clientId);
            if (titles.Add(titleId))
            {
                var info = titleDatabase[titleId];
                TitleUnlockedClientRpc(titleId,
                    info.displayName,
                    info.colorHex, clientId);
            }
        }

        /// <summary>
        /// 칭호 장착 요청
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void EquipTitleServerRpc(string titleId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            string id = titleId;

            if (!string.IsNullOrEmpty(id))
            {
                var titles = GetOrCreateUnlocked(clientId);
                if (!titles.Contains(id))
                {
                    SendMessageClientRpc("해금되지 않은 칭호입니다.", clientId);
                    return;
                }
            }

            playerEquippedTitles[clientId] = id;
            TitleEquippedClientRpc(titleId, clientId);
        }

        /// <summary>
        /// 칭호 해제 요청
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void UnequipTitleServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            playerEquippedTitles[clientId] = "";
            TitleEquippedClientRpc("", clientId);
        }

        /// <summary>
        /// 동기화 요청
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestSyncServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var titles = GetOrCreateUnlocked(clientId);
            string equipped = playerEquippedTitles.ContainsKey(clientId) ? playerEquippedTitles[clientId] : "";

            ClearLocalTitlesClientRpc(clientId);
            foreach (var titleId in titles)
            {
                SyncTitleClientRpc(titleId, clientId);
            }
            SyncEquippedClientRpc(equipped, titles.Count, clientId);
        }

        /// <summary>
        /// 모든 칭호 목록 반환
        /// </summary>
        public List<TitleInfo> GetAllTitles()
        {
            return new List<TitleInfo>(titleDatabase.Values);
        }

        /// <summary>
        /// 해금 여부 확인
        /// </summary>
        public bool IsUnlocked(string titleId)
        {
            return localUnlockedTitles.Contains(titleId);
        }

        #region ClientRPCs

        [ClientRpc]
        private void TitleUnlockedClientRpc(string titleId, string displayName,
            string colorHex, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localUnlockedTitles.Add(titleId);
            OnTitleUnlocked?.Invoke(titleId);
            OnTitlesUpdated?.Invoke();

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"칭호 해금: <color={colorHex}>[{displayName}]</color>", NotificationType.Achievement);
        }

        [ClientRpc]
        private void TitleEquippedClientRpc(string titleId, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localEquippedTitle = titleId;
            OnTitlesUpdated?.Invoke();

            string display = string.IsNullOrEmpty(localEquippedTitle) ? "없음" : GetEquippedTitleDisplay();
            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"칭호 변경: {display}", NotificationType.System);
        }

        [ClientRpc]
        private void ClearLocalTitlesClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localUnlockedTitles.Clear();
            localEquippedTitle = "";
        }

        [ClientRpc]
        private void SyncTitleClientRpc(string titleId, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localUnlockedTitles.Add(titleId);
        }

        [ClientRpc]
        private void SyncEquippedClientRpc(string equippedId, int totalCount, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localEquippedTitle = equippedId;
            OnTitlesUpdated?.Invoke();
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

        private HashSet<string> GetOrCreateUnlocked(ulong clientId)
        {
            if (!playerUnlockedTitles.ContainsKey(clientId))
                playerUnlockedTitles[clientId] = new HashSet<string>();
            return playerUnlockedTitles[clientId];
        }

        #endregion

        #region 칭호 데이터베이스 초기화 (40개)

        private void InitializeTitleDatabase()
        {
            // === 전투 칭호 (8개) ===
            AddTitle("first_blood", "첫 번째 피", "첫 몬스터 처치", "#FF4444", TitleCategory.Combat);
            AddTitle("slayer_100", "백인 학살자", "몬스터 100마리 처치", "#FF6666", TitleCategory.Combat);
            AddTitle("slayer_1000", "천인 학살자", "몬스터 1,000마리 처치", "#FF2222", TitleCategory.Combat);
            AddTitle("slayer_10000", "만인 학살자", "몬스터 10,000마리 처치", "#CC0000", TitleCategory.Combat);
            AddTitle("boss_hunter", "보스 사냥꾼", "보스 10마리 처치", "#FF8800", TitleCategory.Combat);
            AddTitle("boss_slayer", "보스 학살자", "보스 50마리 처치", "#FF4400", TitleCategory.Combat);
            AddTitle("critical_master", "치명타 달인", "크리티컬 히트 500회", "#FFAA00", TitleCategory.Combat);
            AddTitle("combo_king", "콤보의 왕", "10연속 콤보 달성", "#FF6600", TitleCategory.Combat);

            // === 레벨/성장 칭호 (6개) ===
            AddTitle("level_10", "성장하는 모험가", "레벨 10 달성", "#88FF88", TitleCategory.Level);
            AddTitle("level_25", "숙련된 모험가", "레벨 25 달성", "#44FF44", TitleCategory.Level);
            AddTitle("level_50", "베테랑 모험가", "레벨 50 달성", "#00FF00", TitleCategory.Level);
            AddTitle("level_max", "전설의 영웅", "최대 레벨 달성", "#00CC00", TitleCategory.Level);
            AddTitle("all_skills", "만능 전사", "모든 직업 스킬 학습", "#44CC44", TitleCategory.Level);
            AddTitle("spec_master", "특성 마스터", "특성화 선택 완료", "#22AA22", TitleCategory.Level);

            // === 던전 칭호 (6개) ===
            AddTitle("dungeon_first", "던전 탐험가", "첫 던전 클리어", "#4488FF", TitleCategory.Dungeon);
            AddTitle("dungeon_all", "던전 정복자", "모든 던전 클리어", "#2266FF", TitleCategory.Dungeon);
            AddTitle("dungeon_speed", "속도의 질주자", "던전 최속 클리어", "#0044FF", TitleCategory.Dungeon);
            AddTitle("dungeon_deathless", "불사의 탐험가", "무사망 던전 클리어", "#6688FF", TitleCategory.Dungeon);
            AddTitle("dungeon_nightmare", "악몽의 정복자", "악몽 난이도 클리어", "#AA44FF", TitleCategory.Dungeon);
            AddTitle("floor_master", "최하층 도달자", "던전 최하층 도달", "#8844CC", TitleCategory.Dungeon);

            // === PvP 칭호 (6개) ===
            AddTitle("pvp_first", "결투사", "첫 PvP 승리", "#FF44FF", TitleCategory.PvP);
            AddTitle("pvp_10", "투기장의 전사", "PvP 10승", "#FF22FF", TitleCategory.PvP);
            AddTitle("pvp_100", "투기장의 영웅", "PvP 100승", "#FF00FF", TitleCategory.PvP);
            AddTitle("pvp_streak_10", "무패의 전사", "PvP 10연승", "#CC00CC", TitleCategory.PvP);
            AddTitle("arena_gold", "골드 랭커", "아레나 골드 랭크 달성", "#FFD700", TitleCategory.PvP);
            AddTitle("arena_grand", "그랜드마스터", "아레나 최고 랭크 달성", "#FF4488", TitleCategory.PvP);

            // === 경제 칭호 (4개) ===
            AddTitle("rich_1m", "백만장자", "골드 1,000,000 보유", "#FFD700", TitleCategory.Economy);
            AddTitle("trader", "노련한 상인", "거래 50회 완료", "#FFAA44", TitleCategory.Economy);
            AddTitle("auction_king", "경매왕", "경매 100건 낙찰", "#FF8844", TitleCategory.Economy);
            AddTitle("crafter", "장인의 손", "아이템 50개 제작", "#DDAA44", TitleCategory.Economy);

            // === 수집 칭호 (4개) ===
            AddTitle("collector_50", "수집가", "아이템 50종 수집", "#88CCFF", TitleCategory.Collection);
            AddTitle("collector_200", "대수집가", "아이템 200종 수집", "#44AAFF", TitleCategory.Collection);
            AddTitle("pet_lover", "동물 애호가", "펫 5마리 보유", "#AAFFAA", TitleCategory.Collection);
            AddTitle("mount_collector", "마운트 수집가", "마운트 5종 보유", "#CCFFCC", TitleCategory.Collection);

            // === 길드 칭호 (3개) ===
            AddTitle("guild_founder", "길드 창립자", "길드 생성", "#FFCC44", TitleCategory.Guild);
            AddTitle("guild_elite", "길드 엘리트", "길드 엘리트 등급 달성", "#FFAA22", TitleCategory.Guild);
            AddTitle("guild_master", "길드 마스터", "길드장", "#FF8800", TitleCategory.Guild);

            // === 특수 칭호 (3개) ===
            AddTitle("world_boss", "세계의 수호자", "월드보스 처치 참여", "#FF44AA", TitleCategory.Special);
            AddTitle("explorer", "세계 탐험가", "모든 지역 방문", "#44FFAA", TitleCategory.Special);
            AddTitle("tutorial_complete", "신입 졸업", "튜토리얼 완료", "#AAAAAA", TitleCategory.Special);
        }

        private void AddTitle(string id, string displayName, string description, string colorHex, TitleCategory category)
        {
            titleDatabase[id] = new TitleInfo
            {
                titleId = id,
                displayName = displayName,
                description = description,
                colorHex = colorHex,
                category = category
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
    /// 칭호 정보
    /// </summary>
    public class TitleInfo
    {
        public string titleId;
        public string displayName;
        public string description;
        public string colorHex;
        public TitleCategory category;
    }

    /// <summary>
    /// 칭호 카테고리
    /// </summary>
    public enum TitleCategory
    {
        Combat,
        Level,
        Dungeon,
        PvP,
        Economy,
        Collection,
        Guild,
        Special
    }
}
