using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 파티 시스템 관리자
    /// 파티 생성, 참가, 탈퇴, 초대, 준비 상태 등을 관리
    /// </summary>
    public class PartyManager : NetworkBehaviour
    {
        [Header("파티 설정")]
        [SerializeField] private int maxPartiesGlobal = 100; // 전체 최대 파티 수
        [SerializeField] private int defaultMaxMembers = 4;   // 기본 최대 멤버 수
        [SerializeField] private int absoluteMaxMembers = 16; // 절대 최대 멤버 수
        [SerializeField] private float inviteExpireTime = 30f; // 초대 만료 시간
        
        // 네트워크 변수들
        private NetworkList<PartyInfo> allParties = new NetworkList<PartyInfo>();
        private NetworkList<PartyMember> allPartyMembers = new NetworkList<PartyMember>();
        private NetworkList<PartyInvitation> pendingInvitations = new NetworkList<PartyInvitation>();
        
        // 로컬 캐시 (성능 최적화용)
        private Dictionary<int, List<PartyMember>> partyMemberCache = new Dictionary<int, List<PartyMember>>();
        private Dictionary<ulong, int> playerToPartyMap = new Dictionary<ulong, int>();
        
        // 이벤트
        public System.Action<PartyInfo> OnPartyCreated;
        public System.Action<PartyInfo> OnPartyDisbanded;
        public System.Action<int, PartyMember> OnMemberJoined;
        public System.Action<int, ulong> OnMemberLeft;
        public System.Action<PartyInvitation> OnInvitationReceived;
        public System.Action<int> OnInvitationExpired;
        public System.Action<int, PartyState> OnPartyStateChanged;
        
        // 프로퍼티
        public int TotalParties => allParties?.Count ?? 0;
        public bool HasParty(ulong clientId) => playerToPartyMap.ContainsKey(clientId);
        
        private int nextPartyId = 1; // 파티 ID 카운터
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // 서버/클라이언트 모두 이벤트 구독 (UI 업데이트를 위해 필수)
            allParties.OnListChanged += OnPartiesListChanged;
            allPartyMembers.OnListChanged += OnMembersListChanged;
            pendingInvitations.OnListChanged += OnInvitationsListChanged;
            
            Debug.Log($"PartyManager spawned (IsServer: {IsServer})");
        }
        
        private void Update()
        {
            if (IsServer)
            {
                CheckExpiredInvitations();
                UpdatePlayerPositions();
            }
        }
        
        /// <summary>
        /// 새 파티 생성
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CreatePartyServerRpc(string partyName, int maxMembers, bool isPublic, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            
            var leaderClientId = rpcParams.Receive.SenderClientId;
            
            // 이미 파티에 속해있으면 거절
            if (HasParty(leaderClientId))
            {
                NotifyPartyActionResultClientRpc(leaderClientId, false, "이미 파티에 속해있습니다.");
                return;
            }
            
            // 최대 파티 수 체크
            if (allParties.Count >= maxPartiesGlobal)
            {
                NotifyPartyActionResultClientRpc(leaderClientId, false, "서버 파티 수가 한계에 도달했습니다.");
                return;
            }
            
            // 멤버 수 유효성 검사
            maxMembers = Mathf.Clamp(maxMembers, 1, absoluteMaxMembers);
            
            // 새 파티 생성
            var partyInfo = new PartyInfo
            {
                partyId = nextPartyId++,
                partyNameHash = DungeonNameRegistry.RegisterName(partyName),
                state = PartyState.Forming,
                maxMembers = maxMembers,
                isPublic = isPublic,
                targetDungeonIndex = 0,
                minimumLevel = 1,
                allowPvP = true,
                lootShareRadius = 10f,
                expShareEnabled = true,
                autoAcceptMembers = false
            };
            
            // 파티장 멤버 생성
            var leaderMember = CreatePartyMemberFromClient(leaderClientId, PartyRole.Leader);
            
            // 네트워크 리스트에 추가
            allParties.Add(partyInfo);
            allPartyMembers.Add(leaderMember);
            
            // 캐시 업데이트
            RefreshPartyCache();
            
            Debug.Log($"🎉 Party created: '{partyName}' by client {leaderClientId}");
            NotifyPartyActionResultClientRpc(leaderClientId, true, $"파티 '{partyName}' 생성 완료!");
        }
        
        /// <summary>
        /// 파티 참가 요청
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void JoinPartyServerRpc(int partyId, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            
            var clientId = rpcParams.Receive.SenderClientId;
            
            // 파티 존재 여부 확인
            var party = GetPartyById(partyId);
            if (!party.HasValue)
            {
                NotifyPartyActionResultClientRpc(clientId, false, "존재하지 않는 파티입니다.");
                return;
            }
            
            // 이미 파티에 속해있으면 거절
            if (HasParty(clientId))
            {
                NotifyPartyActionResultClientRpc(clientId, false, "이미 파티에 속해있습니다.");
                return;
            }
            
            // 파티 정원 체크
            var currentMembers = GetPartyMembers(partyId);
            if (currentMembers.Count >= party.Value.maxMembers)
            {
                NotifyPartyActionResultClientRpc(clientId, false, "파티 정원이 가득찼습니다.");
                return;
            }
            
            // 레벨 제한 체크
            var playerLevel = GetPlayerLevel(clientId);
            if (playerLevel < party.Value.minimumLevel)
            {
                NotifyPartyActionResultClientRpc(clientId, false, $"최소 레벨 {party.Value.minimumLevel} 이상 필요합니다.");
                return;
            }
            
            // 자동 승인이거나 공개 파티면 바로 참가
            if (party.Value.autoAcceptMembers || party.Value.isPublic)
            {
                AddMemberToParty(partyId, clientId, PartyRole.Member);
                NotifyPartyActionResultClientRpc(clientId, true, $"파티 '{party.Value.GetPartyName()}' 참가 완료!");
            }
            else
            {
                // 파티장에게 초대 요청
                SendJoinRequestToLeader(partyId, clientId);
            }
        }
        
        /// <summary>
        /// 파티 탈퇴
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void LeavePartyServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            
            var clientId = rpcParams.Receive.SenderClientId;
            
            if (!HasParty(clientId))
            {
                NotifyPartyActionResultClientRpc(clientId, false, "파티에 속해있지 않습니다.");
                return;
            }
            
            var partyId = playerToPartyMap[clientId];
            var member = GetPartyMember(partyId, clientId);
            
            if (member.HasValue)
            {
                RemoveMemberFromParty(partyId, clientId);
                
                // 파티장이 나가면 파티 해산 또는 새 파티장 지정
                if (member.Value.role == PartyRole.Leader)
                {
                    HandleLeaderLeaving(partyId);
                }
                
                NotifyPartyActionResultClientRpc(clientId, true, "파티에서 탈퇴했습니다.");
            }
        }
        
        /// <summary>
        /// 플레이어 초대
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void InvitePlayerServerRpc(ulong targetClientId, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            
            var inviterClientId = rpcParams.Receive.SenderClientId;
            
            // 초대자가 파티에 속해있는지 확인
            if (!HasParty(inviterClientId))
            {
                NotifyPartyActionResultClientRpc(inviterClientId, false, "파티에 속해있지 않습니다.");
                return;
            }
            
            var partyId = playerToPartyMap[inviterClientId];
            var inviterMember = GetPartyMember(partyId, inviterClientId);
            
            // 파티장이나 부파티장만 초대 가능
            if (!inviterMember.HasValue || (inviterMember.Value.role != PartyRole.Leader && inviterMember.Value.role != PartyRole.SubLeader))
            {
                NotifyPartyActionResultClientRpc(inviterClientId, false, "파티장이나 부파티장만 초대할 수 있습니다.");
                return;
            }
            
            // 대상이 이미 파티에 속해있으면 거절
            if (HasParty(targetClientId))
            {
                NotifyPartyActionResultClientRpc(inviterClientId, false, "해당 플레이어는 이미 파티에 속해있습니다.");
                return;
            }
            
            // 파티 정원 체크
            var party = GetPartyById(partyId);
            var currentMembers = GetPartyMembers(partyId);
            if (currentMembers.Count >= party.Value.maxMembers)
            {
                NotifyPartyActionResultClientRpc(inviterClientId, false, "파티 정원이 가득찼습니다.");
                return;
            }
            
            // 초대 생성
            var invitation = new PartyInvitation
            {
                partyId = partyId,
                inviterClientId = inviterClientId,
                inviteeClientId = targetClientId,
                inviterNameHash = DungeonNameRegistry.RegisterName(GetPlayerName(inviterClientId)),
                partyNameHash = party.Value.partyNameHash,
                inviteTime = Time.time,
                expireTime = Time.time + inviteExpireTime
            };
            
            pendingInvitations.Add(invitation);
            
            Debug.Log($"📧 Party invitation sent from {inviterClientId} to {targetClientId}");
            NotifyPartyActionResultClientRpc(inviterClientId, true, "초대를 보냈습니다.");
        }
        
        /// <summary>
        /// 초대 응답
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RespondToInvitationServerRpc(int partyId, bool accept, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            
            var clientId = rpcParams.Receive.SenderClientId;
            
            // 초대 찾기
            var invitation = FindInvitation(partyId, clientId);
            if (!invitation.HasValue)
            {
                NotifyPartyActionResultClientRpc(clientId, false, "유효하지 않은 초대입니다.");
                return;
            }
            
            // 초대 제거
            RemoveInvitation(invitation.Value);
            
            if (accept)
            {
                // 파티 참가 처리
                AddMemberToParty(partyId, clientId, PartyRole.Member);
                NotifyPartyActionResultClientRpc(clientId, true, $"파티 '{invitation.Value.GetPartyName()}' 참가 완료!");
            }
            else
            {
                NotifyPartyActionResultClientRpc(clientId, true, "초대를 거절했습니다.");
            }
        }
        
        /// <summary>
        /// 파티 준비 상태 토글
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ToggleReadyStateServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            
            var clientId = rpcParams.Receive.SenderClientId;
            
            if (!HasParty(clientId))
            {
                NotifyPartyActionResultClientRpc(clientId, false, "파티에 속해있지 않습니다.");
                return;
            }
            
            var partyId = playerToPartyMap[clientId];
            UpdateMemberReadyState(partyId, clientId);
        }
        
        /// <summary>
        /// 파티 해산
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void DisbandPartyServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            
            var clientId = rpcParams.Receive.SenderClientId;
            
            if (!HasParty(clientId))
            {
                NotifyPartyActionResultClientRpc(clientId, false, "파티에 속해있지 않습니다.");
                return;
            }
            
            var partyId = playerToPartyMap[clientId];
            var member = GetPartyMember(partyId, clientId);
            
            // 파티장만 해산 가능
            if (!member.HasValue || member.Value.role != PartyRole.Leader)
            {
                NotifyPartyActionResultClientRpc(clientId, false, "파티장만 파티를 해산할 수 있습니다.");
                return;
            }
            
            DisbandParty(partyId);
            NotifyPartyActionResultClientRpc(clientId, true, "파티를 해산했습니다.");
        }
        
        // =========================
        // 헬퍼 메서드들
        // =========================
        
        private PartyMember CreatePartyMemberFromClient(ulong clientId, PartyRole role)
        {
            return new PartyMember
            {
                clientId = clientId,
                playerNameHash = DungeonNameRegistry.RegisterName(GetPlayerName(clientId)),
                playerLevel = GetPlayerLevel(clientId),
                playerRace = GetPlayerRace(clientId),
                role = role,
                isReady = false,
                isOnline = true,
                lastKnownPosition = GetPlayerPosition(clientId)
            };
        }
        
        private string GetPlayerName(ulong clientId)
        {
            // PlayerStatsManager에서 플레이어 이름 가져오기
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
            {
                var statsManager = client.PlayerObject.GetComponent<PlayerStatsManager>();
                if (statsManager?.CurrentStats != null)
                {
                    return statsManager.CurrentStats.CharacterName ?? $"Player_{clientId}";
                }
            }
            return $"Player_{clientId}";
        }
        
        private int GetPlayerLevel(ulong clientId)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
            {
                var statsManager = client.PlayerObject.GetComponent<PlayerStatsManager>();
                if (statsManager?.CurrentStats != null)
                {
                    return statsManager.CurrentStats.CurrentLevel;
                }
            }
            return 1;
        }
        
        private Race GetPlayerRace(ulong clientId)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
            {
                var statsManager = client.PlayerObject.GetComponent<PlayerStatsManager>();
                if (statsManager?.CurrentStats != null)
                {
                    return statsManager.CurrentStats.CharacterRace;
                }
            }
            return Race.Human;
        }
        
        private Vector3 GetPlayerPosition(ulong clientId)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
            {
                return client.PlayerObject.transform.position;
            }
            return Vector3.zero;
        }
        
        private PartyInfo? GetPartyById(int partyId)
        {
            for (int i = 0; i < allParties.Count; i++)
            {
                if (allParties[i].partyId == partyId)
                    return allParties[i];
            }
            return null;
        }
        
        private PartyMember? GetPartyMember(int partyId, ulong clientId)
        {
            for (int i = 0; i < allPartyMembers.Count; i++)
            {
                var member = allPartyMembers[i];
                if (GetMemberPartyId(member) == partyId && member.clientId == clientId)
                    return member;
            }
            return null;
        }
        
        private int GetMemberPartyId(PartyMember member)
        {
            // 파티 ID를 멤버에서 찾기 위해 캐시 사용
            return playerToPartyMap.GetValueOrDefault(member.clientId, -1);
        }
        
        private List<PartyMember> GetPartyMembers(int partyId)
        {
            if (partyMemberCache.TryGetValue(partyId, out var cachedMembers))
            {
                return cachedMembers;
            }
            
            var members = new List<PartyMember>();
            for (int i = 0; i < allPartyMembers.Count; i++)
            {
                var member = allPartyMembers[i];
                if (GetMemberPartyId(member) == partyId)
                {
                    members.Add(member);
                }
            }
            
            partyMemberCache[partyId] = members;
            return members;
        }
        
        private PartyInvitation? FindInvitation(int partyId, ulong inviteeClientId)
        {
            for (int i = 0; i < pendingInvitations.Count; i++)
            {
                var invitation = pendingInvitations[i];
                if (invitation.partyId == partyId && invitation.inviteeClientId == inviteeClientId)
                    return invitation;
            }
            return null;
        }
        
        private void AddMemberToParty(int partyId, ulong clientId, PartyRole role)
        {
            var newMember = CreatePartyMemberFromClient(clientId, role);
            allPartyMembers.Add(newMember);
            
            playerToPartyMap[clientId] = partyId;
            RefreshPartyCache();
            
            Debug.Log($"👥 Player {clientId} joined party {partyId}");
        }
        
        private void RemoveMemberFromParty(int partyId, ulong clientId)
        {
            for (int i = allPartyMembers.Count - 1; i >= 0; i--)
            {
                if (allPartyMembers[i].clientId == clientId && GetMemberPartyId(allPartyMembers[i]) == partyId)
                {
                    allPartyMembers.RemoveAt(i);
                    break;
                }
            }
            
            playerToPartyMap.Remove(clientId);
            RefreshPartyCache();
            
            Debug.Log($"👋 Player {clientId} left party {partyId}");
        }
        
        private void DisbandParty(int partyId)
        {
            // 모든 멤버 제거
            var members = GetPartyMembers(partyId);
            foreach (var member in members)
            {
                RemoveMemberFromParty(partyId, member.clientId);
            }
            
            // 파티 제거
            for (int i = allParties.Count - 1; i >= 0; i--)
            {
                if (allParties[i].partyId == partyId)
                {
                    allParties.RemoveAt(i);
                    break;
                }
            }
            
            // 관련 초대 모두 제거
            for (int i = pendingInvitations.Count - 1; i >= 0; i--)
            {
                if (pendingInvitations[i].partyId == partyId)
                {
                    pendingInvitations.RemoveAt(i);
                }
            }
            
            RefreshPartyCache();
            Debug.Log($"💥 Party {partyId} disbanded");
        }
        
        private void HandleLeaderLeaving(int partyId)
        {
            var members = GetPartyMembers(partyId);
            
            if (members.Count > 1)
            {
                // 가장 높은 레벨의 멤버를 새 파티장으로 지정
                var newLeader = members.Where(m => m.clientId != members.First(l => l.role == PartyRole.Leader).clientId)
                                     .OrderByDescending(m => m.playerLevel)
                                     .FirstOrDefault();
                
                if (newLeader.clientId != 0)
                {
                    UpdateMemberRole(partyId, newLeader.clientId, PartyRole.Leader);
                    Debug.Log($"👑 New party leader: {newLeader.clientId} for party {partyId}");
                }
                else
                {
                    DisbandParty(partyId);
                }
            }
            else
            {
                DisbandParty(partyId);
            }
        }
        
        private void UpdateMemberRole(int partyId, ulong clientId, PartyRole newRole)
        {
            for (int i = 0; i < allPartyMembers.Count; i++)
            {
                var member = allPartyMembers[i];
                if (member.clientId == clientId && GetMemberPartyId(member) == partyId)
                {
                    member.role = newRole;
                    allPartyMembers[i] = member;
                    break;
                }
            }
            RefreshPartyCache();
        }
        
        private void UpdateMemberReadyState(int partyId, ulong clientId)
        {
            for (int i = 0; i < allPartyMembers.Count; i++)
            {
                var member = allPartyMembers[i];
                if (member.clientId == clientId && GetMemberPartyId(member) == partyId)
                {
                    member.isReady = !member.isReady;
                    allPartyMembers[i] = member;
                    break;
                }
            }
            RefreshPartyCache();
        }
        
        private void RemoveInvitation(PartyInvitation invitation)
        {
            for (int i = pendingInvitations.Count - 1; i >= 0; i--)
            {
                var inv = pendingInvitations[i];
                if (inv.partyId == invitation.partyId && inv.inviteeClientId == invitation.inviteeClientId)
                {
                    pendingInvitations.RemoveAt(i);
                    break;
                }
            }
        }
        
        private void SendJoinRequestToLeader(int partyId, ulong requesterClientId)
        {
            var members = GetPartyMembers(partyId);
            var leader = members.FirstOrDefault(m => m.role == PartyRole.Leader);
            
            if (leader.clientId != 0)
            {
                NotifyJoinRequestClientRpc(leader.clientId, requesterClientId, GetPlayerName(requesterClientId));
            }
        }
        
        private void RefreshPartyCache()
        {
            partyMemberCache.Clear();
            playerToPartyMap.Clear();
            
            // 플레이어-파티 매핑 재구성
            for (int i = 0; i < allParties.Count; i++)
            {
                var party = allParties[i];
                var members = new List<PartyMember>();
                
                for (int j = 0; j < allPartyMembers.Count; j++)
                {
                    var member = allPartyMembers[j];
                    // 임시로 파티 ID 매칭 (실제로는 더 정교한 방법 필요)
                    if (IsPlayerInParty(member.clientId, party.partyId))
                    {
                        members.Add(member);
                        playerToPartyMap[member.clientId] = party.partyId;
                    }
                }
                
                partyMemberCache[party.partyId] = members;
            }
        }
        
        private bool IsPlayerInParty(ulong clientId, int partyId)
        {
            // playerToPartyMap을 사용한 정확한 매칭 로직
            if (playerToPartyMap.TryGetValue(clientId, out var playerPartyId))
            {
                return playerPartyId == partyId;
            }
            
            // 캐시가 없으면 직접 NetworkList에서 검색
            for (int i = 0; i < allPartyMembers.Count; i++)
            {
                var member = allPartyMembers[i];
                if (member.clientId == clientId)
                {
                    // 해당 멤버가 속한 파티 찾기
                    for (int j = 0; j < allParties.Count; j++)
                    {
                        var party = allParties[j];
                        if (party.partyId == partyId)
                        {
                            // 이 파티의 멤버인지 확인 (시간순 추정)
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }
        
        private void CheckExpiredInvitations()
        {
            float currentTime = Time.time;
            
            for (int i = pendingInvitations.Count - 1; i >= 0; i--)
            {
                var invitation = pendingInvitations[i];
                if (invitation.IsExpired(currentTime))
                {
                    pendingInvitations.RemoveAt(i);
                    NotifyInviteExpiredClientRpc(invitation.inviteeClientId, invitation.partyId);
                }
            }
        }
        
        private void UpdatePlayerPositions()
        {
            // 플레이어 위치 업데이트 (5초마다)
            if (Time.fixedTime % 5f < Time.fixedDeltaTime)
            {
                for (int i = 0; i < allPartyMembers.Count; i++)
                {
                    var member = allPartyMembers[i];
                    member.lastKnownPosition = GetPlayerPosition(member.clientId);
                    member.isOnline = NetworkManager.Singleton.ConnectedClients.ContainsKey(member.clientId);
                    allPartyMembers[i] = member;
                }
            }
        }
        
        // =========================
        // 이벤트 핸들러들
        // =========================
        
        private void OnPartiesListChanged(NetworkListEvent<PartyInfo> changeEvent)
        {
            RefreshPartyCache();
            
            switch (changeEvent.Type)
            {
                case NetworkListEvent<PartyInfo>.EventType.Add:
                    OnPartyCreated?.Invoke(changeEvent.Value);
                    break;
                case NetworkListEvent<PartyInfo>.EventType.Remove:
                    OnPartyDisbanded?.Invoke(changeEvent.Value);
                    break;
            }
        }
        
        private void OnMembersListChanged(NetworkListEvent<PartyMember> changeEvent)
        {
            RefreshPartyCache();
            
            switch (changeEvent.Type)
            {
                case NetworkListEvent<PartyMember>.EventType.Add:
                    var partyId = GetMemberPartyId(changeEvent.Value);
                    OnMemberJoined?.Invoke(partyId, changeEvent.Value);
                    break;
                case NetworkListEvent<PartyMember>.EventType.Remove:
                    OnMemberLeft?.Invoke(GetMemberPartyId(changeEvent.Value), changeEvent.Value.clientId);
                    break;
            }
        }
        
        private void OnInvitationsListChanged(NetworkListEvent<PartyInvitation> changeEvent)
        {
            switch (changeEvent.Type)
            {
                case NetworkListEvent<PartyInvitation>.EventType.Add:
                    OnInvitationReceived?.Invoke(changeEvent.Value);
                    break;
            }
        }
        
        // =========================
        // ClientRpc 메서드들
        // =========================
        
        [ClientRpc]
        private void NotifyPartyActionResultClientRpc(ulong targetClientId, bool success, string message)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            
            Debug.Log($"🎉 Party action result: {message} (Success: {success})");
            // 실제로는 UI에 메시지 표시
        }
        
        [ClientRpc]
        private void NotifyJoinRequestClientRpc(ulong leaderClientId, ulong requesterClientId, string requesterName)
        {
            if (NetworkManager.Singleton.LocalClientId != leaderClientId) return;
            
            Debug.Log($"📨 Join request from {requesterName}");
            // 실제로는 UI에 승인/거절 다이얼로그 표시
        }
        
        [ClientRpc]
        private void NotifyInviteExpiredClientRpc(ulong inviteeClientId, int partyId)
        {
            if (NetworkManager.Singleton.LocalClientId != inviteeClientId) return;
            
            OnInvitationExpired?.Invoke(partyId);
            Debug.Log($"⏰ Party invitation expired for party {partyId}");
        }
        
        // =========================
        // 공개 API 메서드들
        // =========================
        
        /// <summary>
        /// 플레이어의 파티 정보 가져오기
        /// </summary>
        public PartyInfo? GetPlayerParty(ulong clientId)
        {
            if (playerToPartyMap.TryGetValue(clientId, out var partyId))
            {
                return GetPartyById(partyId);
            }
            return null;
        }
        
        /// <summary>
        /// 플레이어의 파티 멤버들 가져오기
        /// </summary>
        public List<PartyMember> GetPlayerPartyMembers(ulong clientId)
        {
            if (playerToPartyMap.TryGetValue(clientId, out var partyId))
            {
                return GetPartyMembers(partyId);
            }
            return new List<PartyMember>();
        }
        
        /// <summary>
        /// 파티가 모든 멤버가 준비되었는지 확인
        /// </summary>
        public bool IsPartyAllReady(int partyId)
        {
            var members = GetPartyMembers(partyId);
            return members.Count > 0 && members.All(m => m.isReady);
        }
        
        /// <summary>
        /// 던전 입장을 위한 파티 스폰 그룹 생성
        /// </summary>
        public List<PartySpawnGroup> GeneratePartySpawnGroups()
        {
            var spawnGroups = new List<PartySpawnGroup>();
            
            for (int i = 0; i < allParties.Count; i++)
            {
                var party = allParties[i];
                if (party.state == PartyState.Ready)
                {
                    var members = GetPartyMembers(party.partyId);
                    var memberIds = members.Select(m => m.clientId).ToList();
                    
                    spawnGroups.Add(new PartySpawnGroup(party.partyId, memberIds));
                }
            }
            
            return spawnGroups;
        }
    }
}