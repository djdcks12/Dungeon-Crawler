using UnityEngine;
using Unity.Netcode;

using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    #region Enums & Structs

    public enum GuildRank
    {
        Member = 0,
        Elite = 1,
        ViceMaster = 2,
        Master = 3
    }

    public enum GuildPermission
    {
        Invite = 0,
        Kick = 1,
        Promote = 2,
        EditNotice = 3,
        UseGuildBank = 4
    }

    [System.Serializable]
    public struct GuildMemberInfo : INetworkSerializable, System.IEquatable<GuildMemberInfo>
    {
        public ulong clientId;
        public string playerName;
        public int playerLevel;
        public GuildRank rank;
        public int contributionPoints;
        public float lastOnlineTime;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref playerName);
            serializer.SerializeValue(ref playerLevel);
            serializer.SerializeValue(ref rank);
            serializer.SerializeValue(ref contributionPoints);
            serializer.SerializeValue(ref lastOnlineTime);
        }

        public bool Equals(GuildMemberInfo other) => clientId == other.clientId;
    }

    [System.Serializable]
    public struct GuildInfo : INetworkSerializable, System.IEquatable<GuildInfo>
    {
        public int guildId;
        public string guildName;
        public string notice;
        public ulong masterClientId;
        public int memberCount;
        public int maxMembers;
        public int guildLevel;
        public int guildExp;
        public int guildGold;
        public float createdTime;

        // 길드 버프
        public float expBonusPercent;
        public float goldBonusPercent;
        public float dropBonusPercent;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref guildId);
            serializer.SerializeValue(ref guildName);
            serializer.SerializeValue(ref notice);
            serializer.SerializeValue(ref masterClientId);
            serializer.SerializeValue(ref memberCount);
            serializer.SerializeValue(ref maxMembers);
            serializer.SerializeValue(ref guildLevel);
            serializer.SerializeValue(ref guildExp);
            serializer.SerializeValue(ref guildGold);
            serializer.SerializeValue(ref createdTime);
            serializer.SerializeValue(ref expBonusPercent);
            serializer.SerializeValue(ref goldBonusPercent);
            serializer.SerializeValue(ref dropBonusPercent);
        }

        public bool Equals(GuildInfo other) => guildId == other.guildId;
    }

    #endregion

    /// <summary>
    /// 길드 시스템 - 서버 권위적
    /// 길드 생성/해산, 가입/탈퇴, 직급 관리, 길드 레벨, 길드 버프
    /// </summary>
    public class GuildSystem : NetworkBehaviour
    {
        public static GuildSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int createCost = 5000;
        [SerializeField] private int baseMaxMembers = 20;
        [SerializeField] private int membersPerLevel = 5;
        [SerializeField] private int maxGuildLevel = 10;
        [SerializeField] private int minLevelToCreate = 5;
        [SerializeField] private float inviteTimeoutSeconds = 60f;

        // 길드 레벨별 필요 경험치
        private readonly int[] levelExpRequirements = { 0, 1000, 3000, 6000, 10000, 15000, 22000, 30000, 40000, 55000 };

        // 길드 레벨별 버프
        private readonly float[] expBonusByLevel = { 0f, 2f, 4f, 6f, 8f, 10f, 13f, 16f, 19f, 22f, 25f };
        private readonly float[] goldBonusByLevel = { 0f, 1f, 2f, 3f, 4f, 5f, 7f, 9f, 11f, 13f, 15f };
        private readonly float[] dropBonusByLevel = { 0f, 0.5f, 1f, 1.5f, 2f, 3f, 4f, 5f, 6f, 7f, 8f };

        // 서버 데이터
        private Dictionary<int, GuildInfo> guilds = new Dictionary<int, GuildInfo>();
        private Dictionary<int, List<GuildMemberInfo>> guildMembers = new Dictionary<int, List<GuildMemberInfo>>();
        private Dictionary<ulong, int> playerGuildMap = new Dictionary<ulong, int>();
        private Dictionary<ulong, (int guildId, float timestamp)> pendingInvites = new Dictionary<ulong, (int, float)>();
        private int nextGuildId = 1;

        // 로컬 데이터
        private GuildInfo localGuildInfo;
        private List<GuildMemberInfo> localMembers = new List<GuildMemberInfo>();
        private GuildRank localRank = GuildRank.Member;
        private bool isInGuild;

        // 이벤트
        public System.Action<GuildInfo> OnGuildJoined;
        public System.Action OnGuildLeft;
        public System.Action<GuildInfo> OnGuildInfoUpdated;
        public System.Action<List<GuildMemberInfo>> OnMembersUpdated;
        public System.Action<int, string> OnGuildInviteReceived; // guildId, guildName
        public System.Action<string> OnGuildMessage;

        // 접근자
        public bool IsInGuild => isInGuild;
        public GuildInfo LocalGuildInfo => localGuildInfo;
        public IReadOnlyList<GuildMemberInfo> LocalMembers => localMembers;
        public GuildRank LocalRank => localRank;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (!IsServer) return;
            CleanExpiredInvites();
        }

        private void CleanExpiredInvites()
        {
            var expired = new List<ulong>();
            foreach (var kvp in pendingInvites)
            {
                if (Time.time - kvp.Value.timestamp > inviteTimeoutSeconds)
                    expired.Add(kvp.Key);
            }
            foreach (var clientId in expired)
                pendingInvites.Remove(clientId);
        }

        #region 길드 생성/해산

        [ServerRpc(RequireOwnership = false)]
        public void CreateGuildServerRpc(string guildName, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            // 이미 길드에 가입되어 있으면 거부
            if (playerGuildMap.ContainsKey(clientId))
            {
                SendMessageClientRpc("이미 길드에 가입되어 있습니다.", clientId);
                return;
            }

            // 이름 검증
            if (string.IsNullOrEmpty(guildName) || guildName.Length < 2 || guildName.Length > 16)
            {
                SendMessageClientRpc("길드 이름은 2~16자 이내여야 합니다.", clientId);
                return;
            }

            // 이름 중복 체크
            foreach (var g in guilds.Values)
            {
                if (g.guildName == guildName)
                {
                    SendMessageClientRpc("이미 존재하는 길드 이름입니다.", clientId);
                    return;
                }
            }

            // 레벨/골드 체크
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            if (statsData.CurrentLevel < minLevelToCreate)
            {
                SendMessageClientRpc($"레벨 {minLevelToCreate} 이상이어야 길드를 만들 수 있습니다.", clientId);
                return;
            }

            if (statsData.Gold < createCost)
            {
                SendMessageClientRpc($"길드 생성에 {createCost}G가 필요합니다.", clientId);
                return;
            }

            // 골드 차감
            statsData.ChangeGold(-createCost);

            // 길드 생성
            int guildId = nextGuildId++;
            var guild = new GuildInfo
            {
                guildId = guildId,
                guildName = guildName,
                notice = "길드에 오신 것을 환영합니다!",
                masterClientId = clientId,
                memberCount = 1,
                maxMembers = baseMaxMembers,
                guildLevel = 1,
                guildExp = 0,
                guildGold = 0,
                createdTime = Time.time,
                expBonusPercent = expBonusByLevel[1],
                goldBonusPercent = goldBonusByLevel[1],
                dropBonusPercent = dropBonusByLevel[1]
            };

            guilds[guildId] = guild;

            // 멤버 추가
            var member = new GuildMemberInfo
            {
                clientId = clientId,
                playerName = statsData.CharacterName ?? "Unknown",
                playerLevel = statsData.CurrentLevel,
                rank = GuildRank.Master,
                contributionPoints = 0,
                lastOnlineTime = Time.time
            };

            guildMembers[guildId] = new List<GuildMemberInfo> { member };
            playerGuildMap[clientId] = guildId;

            // 클라이언트에 알림
            NotifyGuildJoinedClientRpc(guild, clientId);
            SyncMembersClientRpc(guildId, new GuildMemberInfo[] { member }, clientId);

            Debug.Log($"[Guild] '{guildName}' created by client {clientId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void DisbandGuildServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!playerGuildMap.TryGetValue(clientId, out int guildId))
            {
                SendMessageClientRpc("길드에 가입되어 있지 않습니다.", clientId);
                return;
            }

            if (!guilds.TryGetValue(guildId, out var guild))
                return;

            if (guild.masterClientId != clientId)
            {
                SendMessageClientRpc("길드 마스터만 해산할 수 있습니다.", clientId);
                return;
            }

            // 모든 멤버에게 알림
            if (guildMembers.TryGetValue(guildId, out var members))
            {
                foreach (var m in members)
                {
                    playerGuildMap.Remove(m.clientId);
                    NotifyGuildLeftClientRpc("길드가 해산되었습니다.", m.clientId);
                }
            }

            guilds.Remove(guildId);
            guildMembers.Remove(guildId);
        }

        #endregion

        #region 가입/탈퇴/초대

        [ServerRpc(RequireOwnership = false)]
        public void InvitePlayerServerRpc(ulong targetClientId, ServerRpcParams rpcParams = default)
        {
            ulong inviterClientId = rpcParams.Receive.SenderClientId;

            if (!playerGuildMap.TryGetValue(inviterClientId, out int guildId))
            {
                SendMessageClientRpc("길드에 가입되어 있지 않습니다.", inviterClientId);
                return;
            }

            // 권한 체크 (엘리트 이상 초대 가능)
            if (!HasPermission(inviterClientId, guildId, GuildPermission.Invite))
            {
                SendMessageClientRpc("초대 권한이 없습니다.", inviterClientId);
                return;
            }

            // 대상 체크
            if (playerGuildMap.ContainsKey(targetClientId))
            {
                SendMessageClientRpc("해당 플레이어는 이미 길드에 가입되어 있습니다.", inviterClientId);
                return;
            }

            if (pendingInvites.ContainsKey(targetClientId))
            {
                SendMessageClientRpc("해당 플레이어에게 이미 초대를 보냈습니다.", inviterClientId);
                return;
            }

            var guild = guilds[guildId];
            if (guild.memberCount >= guild.maxMembers)
            {
                SendMessageClientRpc("길드 인원이 가득 찼습니다.", inviterClientId);
                return;
            }

            // 초대 등록
            pendingInvites[targetClientId] = (guildId, Time.time);
            NotifyInviteClientRpc(guildId, guild.guildName, targetClientId);
            SendMessageClientRpc("초대를 보냈습니다.", inviterClientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void AcceptInviteServerRpc(int guildId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!pendingInvites.TryGetValue(clientId, out var invite) || invite.guildId != guildId)
            {
                SendMessageClientRpc("유효한 초대가 없습니다.", clientId);
                return;
            }

            pendingInvites.Remove(clientId);

            if (playerGuildMap.ContainsKey(clientId))
            {
                SendMessageClientRpc("이미 길드에 가입되어 있습니다.", clientId);
                return;
            }

            if (!guilds.TryGetValue(guildId, out var guild))
            {
                SendMessageClientRpc("존재하지 않는 길드입니다.", clientId);
                return;
            }

            if (guild.memberCount >= guild.maxMembers)
            {
                SendMessageClientRpc("길드 인원이 가득 찼습니다.", clientId);
                return;
            }

            AddMemberToGuild(clientId, guildId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void DeclineInviteServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            pendingInvites.Remove(clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void LeaveGuildServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!playerGuildMap.TryGetValue(clientId, out int guildId))
            {
                SendMessageClientRpc("길드에 가입되어 있지 않습니다.", clientId);
                return;
            }

            var guild = guilds[guildId];
            if (guild.masterClientId == clientId)
            {
                SendMessageClientRpc("길드 마스터는 탈퇴할 수 없습니다. 양도 후 탈퇴하세요.", clientId);
                return;
            }

            RemoveMemberFromGuild(clientId, guildId, "길드를 탈퇴했습니다.");
        }

        [ServerRpc(RequireOwnership = false)]
        public void KickMemberServerRpc(ulong targetClientId, ServerRpcParams rpcParams = default)
        {
            ulong kickerClientId = rpcParams.Receive.SenderClientId;

            if (!playerGuildMap.TryGetValue(kickerClientId, out int guildId))
                return;

            if (!HasPermission(kickerClientId, guildId, GuildPermission.Kick))
            {
                SendMessageClientRpc("추방 권한이 없습니다.", kickerClientId);
                return;
            }

            if (!playerGuildMap.TryGetValue(targetClientId, out int targetGuildId) || targetGuildId != guildId)
            {
                SendMessageClientRpc("같은 길드 멤버가 아닙니다.", kickerClientId);
                return;
            }

            // 마스터는 추방 불가
            var guild = guilds[guildId];
            if (guild.masterClientId == targetClientId)
            {
                SendMessageClientRpc("길드 마스터는 추방할 수 없습니다.", kickerClientId);
                return;
            }

            // 같은 직급 이상은 추방 불가
            var kickerRank = GetMemberRank(kickerClientId, guildId);
            var targetRank = GetMemberRank(targetClientId, guildId);
            if (targetRank >= kickerRank)
            {
                SendMessageClientRpc("같은 직급 이상의 멤버는 추방할 수 없습니다.", kickerClientId);
                return;
            }

            RemoveMemberFromGuild(targetClientId, guildId, "길드에서 추방되었습니다.");
        }

        #endregion

        #region 직급 관리

        [ServerRpc(RequireOwnership = false)]
        public void PromoteMemberServerRpc(ulong targetClientId, ServerRpcParams rpcParams = default)
        {
            ulong promoterClientId = rpcParams.Receive.SenderClientId;

            if (!playerGuildMap.TryGetValue(promoterClientId, out int guildId))
                return;

            if (!HasPermission(promoterClientId, guildId, GuildPermission.Promote))
            {
                SendMessageClientRpc("직급 변경 권한이 없습니다.", promoterClientId);
                return;
            }

            if (!guildMembers.TryGetValue(guildId, out var members))
                return;

            int targetIdx = members.FindIndex(m => m.clientId == targetClientId);
            if (targetIdx < 0)
            {
                SendMessageClientRpc("해당 멤버를 찾을 수 없습니다.", promoterClientId);
                return;
            }

            var target = members[targetIdx];
            if (target.rank >= GuildRank.ViceMaster)
            {
                SendMessageClientRpc("더 이상 승급할 수 없습니다.", promoterClientId);
                return;
            }

            target.rank = (GuildRank)((int)target.rank + 1);
            members[targetIdx] = target;
            BroadcastMemberUpdate(guildId);
            SendMessageClientRpc($"{target.playerName} 승급 완료.", promoterClientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void DemoteMemberServerRpc(ulong targetClientId, ServerRpcParams rpcParams = default)
        {
            ulong demoterClientId = rpcParams.Receive.SenderClientId;

            if (!playerGuildMap.TryGetValue(demoterClientId, out int guildId))
                return;

            if (!HasPermission(demoterClientId, guildId, GuildPermission.Promote))
            {
                SendMessageClientRpc("직급 변경 권한이 없습니다.", demoterClientId);
                return;
            }

            if (!guildMembers.TryGetValue(guildId, out var members))
                return;

            int targetIdx = members.FindIndex(m => m.clientId == targetClientId);
            if (targetIdx < 0) return;

            var target = members[targetIdx];
            if (target.rank <= GuildRank.Member)
            {
                SendMessageClientRpc("더 이상 강등할 수 없습니다.", demoterClientId);
                return;
            }

            if (target.rank == GuildRank.Master)
            {
                SendMessageClientRpc("길드 마스터는 강등할 수 없습니다.", demoterClientId);
                return;
            }

            target.rank = (GuildRank)((int)target.rank - 1);
            members[targetIdx] = target;
            BroadcastMemberUpdate(guildId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void TransferMasterServerRpc(ulong targetClientId, ServerRpcParams rpcParams = default)
        {
            ulong masterClientId = rpcParams.Receive.SenderClientId;

            if (!playerGuildMap.TryGetValue(masterClientId, out int guildId))
                return;

            if (!guilds.TryGetValue(guildId, out var guild) || guild.masterClientId != masterClientId)
            {
                SendMessageClientRpc("길드 마스터만 양도할 수 있습니다.", masterClientId);
                return;
            }

            if (!guildMembers.TryGetValue(guildId, out var members))
                return;

            int oldMasterIdx = members.FindIndex(m => m.clientId == masterClientId);
            int newMasterIdx = members.FindIndex(m => m.clientId == targetClientId);
            if (newMasterIdx < 0)
            {
                SendMessageClientRpc("해당 멤버를 찾을 수 없습니다.", masterClientId);
                return;
            }

            // 직급 교환
            if (oldMasterIdx >= 0)
            {
                var old = members[oldMasterIdx];
                old.rank = GuildRank.ViceMaster;
                members[oldMasterIdx] = old;
            }

            var newMaster = members[newMasterIdx];
            newMaster.rank = GuildRank.Master;
            members[newMasterIdx] = newMaster;

            guild.masterClientId = targetClientId;
            guilds[guildId] = guild;

            BroadcastGuildInfo(guildId);
            BroadcastMemberUpdate(guildId);
        }

        #endregion

        #region 길드 공지 & 기여

        [ServerRpc(RequireOwnership = false)]
        public void SetNoticeServerRpc(string notice, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!playerGuildMap.TryGetValue(clientId, out int guildId))
                return;

            if (!HasPermission(clientId, guildId, GuildPermission.EditNotice))
            {
                SendMessageClientRpc("공지 수정 권한이 없습니다.", clientId);
                return;
            }

            if (notice != null && notice.Length > 120)
                notice = notice.Substring(0, 120);

            var guild = guilds[guildId];
            guild.notice = notice ?? "";
            guilds[guildId] = guild;

            BroadcastGuildInfo(guildId);
        }

        /// <summary>
        /// 서버에서 호출 - 길드 경험치/기여도 추가
        /// </summary>
        public void AddGuildContribution(ulong clientId, int exp, int gold = 0)
        {
            if (!IsServer) return;
            if (!playerGuildMap.TryGetValue(clientId, out int guildId)) return;
            if (!guilds.TryGetValue(guildId, out var guild)) return;

            // 기여도 추가
            if (guildMembers.TryGetValue(guildId, out var members))
            {
                int idx = members.FindIndex(m => m.clientId == clientId);
                if (idx >= 0)
                {
                    var m = members[idx];
                    m.contributionPoints += exp;
                    members[idx] = m;
                }
            }

            // 길드 경험치 추가
            guild.guildExp += exp;
            guild.guildGold += gold;

            // 레벨업 체크
            while (guild.guildLevel < maxGuildLevel &&
                   guild.guildLevel < levelExpRequirements.Length &&
                   guild.guildExp >= levelExpRequirements[guild.guildLevel])
            {
                guild.guildExp -= levelExpRequirements[guild.guildLevel];
                guild.guildLevel++;
                guild.maxMembers = baseMaxMembers + (guild.guildLevel - 1) * membersPerLevel;
                guild.expBonusPercent = guild.guildLevel < expBonusByLevel.Length ? expBonusByLevel[guild.guildLevel] : 25f;
                guild.goldBonusPercent = guild.guildLevel < goldBonusByLevel.Length ? goldBonusByLevel[guild.guildLevel] : 15f;
                guild.dropBonusPercent = guild.guildLevel < dropBonusByLevel.Length ? dropBonusByLevel[guild.guildLevel] : 8f;

                BroadcastGuildMessageToMembers(guildId, $"길드 레벨이 {guild.guildLevel}로 상승했습니다!");
            }

            guilds[guildId] = guild;
            BroadcastGuildInfo(guildId);
        }

        /// <summary>
        /// 골드 기부
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void DonateGoldServerRpc(int amount, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (amount <= 0) return;
            if (!playerGuildMap.TryGetValue(clientId, out int guildId)) return;

            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null || statsData.Gold < amount)
            {
                SendMessageClientRpc("골드가 부족합니다.", clientId);
                return;
            }

            statsData.ChangeGold(-amount);
            AddGuildContribution(clientId, amount / 10, amount);
            SendMessageClientRpc($"{amount}G를 기부했습니다. 기여도 +{amount / 10}", clientId);
        }

        #endregion

        #region 길드 버프 (외부 참조용)

        /// <summary>
        /// 해당 플레이어의 길드 EXP 보너스%
        /// </summary>
        public float GetGuildExpBonus(ulong clientId)
        {
            if (!playerGuildMap.TryGetValue(clientId, out int guildId)) return 0f;
            if (!guilds.TryGetValue(guildId, out var guild)) return 0f;
            return guild.expBonusPercent;
        }

        /// <summary>
        /// 해당 플레이어의 길드 골드 보너스%
        /// </summary>
        public float GetGuildGoldBonus(ulong clientId)
        {
            if (!playerGuildMap.TryGetValue(clientId, out int guildId)) return 0f;
            if (!guilds.TryGetValue(guildId, out var guild)) return 0f;
            return guild.goldBonusPercent;
        }

        /// <summary>
        /// 해당 플레이어의 길드 드롭률 보너스%
        /// </summary>
        public float GetGuildDropBonus(ulong clientId)
        {
            if (!playerGuildMap.TryGetValue(clientId, out int guildId)) return 0f;
            if (!guilds.TryGetValue(guildId, out var guild)) return 0f;
            return guild.dropBonusPercent;
        }

        /// <summary>
        /// 해당 플레이어가 길드에 소속되어 있는지 확인
        /// </summary>
        public bool IsPlayerInGuild(ulong clientId) => playerGuildMap.ContainsKey(clientId);

        /// <summary>
        /// 플레이어의 길드 ID 문자열 반환 (미소속시 빈 문자열)
        /// </summary>
        public string GetPlayerGuildId(ulong clientId)
        {
            if (playerGuildMap.TryGetValue(clientId, out int guildId))
                return guildId.ToString();
            return "";
        }

        /// <summary>
        /// 길드 목록 (가입 UI용)
        /// </summary>
        public List<GuildInfo> GetAllGuilds() => guilds.Values.ToList();

        #endregion

        #region Internal

        private void AddMemberToGuild(ulong clientId, int guildId)
        {
            if (!guilds.TryGetValue(guildId, out var guild)) return;

            var statsData = GetPlayerStatsData(clientId);
            string playerName = statsData != null ? (statsData.CharacterName ?? "Unknown") : "Unknown";
            int playerLevel = statsData != null ? statsData.CurrentLevel : 1;

            var member = new GuildMemberInfo
            {
                clientId = clientId,
                playerName = playerName,
                playerLevel = playerLevel,
                rank = GuildRank.Member,
                contributionPoints = 0,
                lastOnlineTime = Time.time
            };

            if (!guildMembers.ContainsKey(guildId))
                guildMembers[guildId] = new List<GuildMemberInfo>();

            guildMembers[guildId].Add(member);
            playerGuildMap[clientId] = guildId;

            guild.memberCount = guildMembers[guildId].Count;
            guilds[guildId] = guild;

            // 가입자에게 길드 정보 전송
            NotifyGuildJoinedClientRpc(guild, clientId);
            SyncMembersClientRpc(guildId, guildMembers[guildId].ToArray(), clientId);

            // 기존 멤버에게 알림
            BroadcastGuildMessageToMembers(guildId, $"{playerName}님이 길드에 가입했습니다.");
            BroadcastMemberUpdate(guildId);
            BroadcastGuildInfo(guildId);
        }

        private void RemoveMemberFromGuild(ulong clientId, int guildId, string reason)
        {
            if (!guildMembers.TryGetValue(guildId, out var members)) return;

            string playerName = "Unknown";
            int idx = members.FindIndex(m => m.clientId == clientId);
            if (idx >= 0)
            {
                playerName = members[idx].playerName;
                members.RemoveAt(idx);
            }

            playerGuildMap.Remove(clientId);

            if (guilds.TryGetValue(guildId, out var guild))
            {
                guild.memberCount = members.Count;
                guilds[guildId] = guild;
            }

            NotifyGuildLeftClientRpc(reason, clientId);
            BroadcastGuildMessageToMembers(guildId, $"{playerName}님이 길드를 떠났습니다.");
            BroadcastMemberUpdate(guildId);
            BroadcastGuildInfo(guildId);
        }

        private bool HasPermission(ulong clientId, int guildId, GuildPermission permission)
        {
            var rank = GetMemberRank(clientId, guildId);
            switch (permission)
            {
                case GuildPermission.Invite: return rank >= GuildRank.Elite;
                case GuildPermission.Kick: return rank >= GuildRank.ViceMaster;
                case GuildPermission.Promote: return rank >= GuildRank.Master;
                case GuildPermission.EditNotice: return rank >= GuildRank.ViceMaster;
                case GuildPermission.UseGuildBank: return rank >= GuildRank.Elite;
                default: return false;
            }
        }

        private GuildRank GetMemberRank(ulong clientId, int guildId)
        {
            if (!guildMembers.TryGetValue(guildId, out var members)) return GuildRank.Member;
            var m = members.FirstOrDefault(x => x.clientId == clientId);
            return m.clientId == clientId ? m.rank : GuildRank.Member;
        }

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            if (client.PlayerObject == null) return null;
            return client.PlayerObject.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        private void BroadcastGuildInfo(int guildId)
        {
            if (!guilds.TryGetValue(guildId, out var guild)) return;
            if (!guildMembers.TryGetValue(guildId, out var members)) return;

            foreach (var m in members)
                NotifyGuildInfoUpdatedClientRpc(guild, m.clientId);
        }

        private void BroadcastMemberUpdate(int guildId)
        {
            if (!guildMembers.TryGetValue(guildId, out var members)) return;

            var arr = members.ToArray();
            foreach (var m in members)
                SyncMembersClientRpc(guildId, arr, m.clientId);
        }

        private void BroadcastGuildMessageToMembers(int guildId, string message)
        {
            if (!guildMembers.TryGetValue(guildId, out var members)) return;
            foreach (var m in members)
                NotifyGuildMessageClientRpc(message, m.clientId);
        }

        #endregion

        #region ClientRPCs

        [ClientRpc]
        private void NotifyGuildJoinedClientRpc(GuildInfo guild, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localGuildInfo = guild;
            isInGuild = true;
            localRank = guild.masterClientId == targetClientId ? GuildRank.Master : GuildRank.Member;
            OnGuildJoined?.Invoke(guild);

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"길드 '{guild.guildName}' 에 가입했습니다!", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyGuildLeftClientRpc(string reason, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localGuildInfo = default;
            localMembers.Clear();
            isInGuild = false;
            localRank = GuildRank.Member;
            OnGuildLeft?.Invoke();

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification(reason, NotificationType.Warning);
        }

        [ClientRpc]
        private void NotifyGuildInfoUpdatedClientRpc(GuildInfo guild, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localGuildInfo = guild;
            OnGuildInfoUpdated?.Invoke(guild);
        }

        [ClientRpc]
        private void SyncMembersClientRpc(int guildId, GuildMemberInfo[] members, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localMembers.Clear();
            localMembers.AddRange(members);

            // 로컬 랭크 업데이트
            foreach (var m in members)
            {
                if (m.clientId == NetworkManager.Singleton.LocalClientId)
                {
                    localRank = m.rank;
                    break;
                }
            }

            OnMembersUpdated?.Invoke(localMembers);
        }

        [ClientRpc]
        private void NotifyInviteClientRpc(int guildId, string guildName, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            OnGuildInviteReceived?.Invoke(guildId, guildName);

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"길드 '{guildName}' 초대가 도착했습니다.", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyGuildMessageClientRpc(string message, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            OnGuildMessage?.Invoke(message);
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

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                OnGuildJoined = null;
                OnGuildLeft = null;
                OnGuildInfoUpdated = null;
                OnMembersUpdated = null;
                OnGuildInviteReceived = null;
                OnGuildMessage = null;
                Instance = null;
            }
            base.OnDestroy();
        }
    }
}
