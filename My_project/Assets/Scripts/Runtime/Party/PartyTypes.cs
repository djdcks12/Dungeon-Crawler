using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 파티 상태
    /// </summary>
    public enum PartyState
    {
        Forming,        // 파티 구성 중
        Ready,          // 던전 입장 준비 완료
        InDungeon,      // 던전 진행 중
        Disbanded       // 해산됨
    }

    /// <summary>
    /// 파티 멤버 역할
    /// </summary>
    public enum PartyRole
    {
        Leader,         // 파티장
        Member,         // 일반 멤버
        SubLeader       // 부파티장 (선택적)
    }

    /// <summary>
    /// 파티 멤버 정보 (네트워크 직렬화 가능)
    /// </summary>
    [System.Serializable]
    public struct PartyMember : INetworkSerializable, System.IEquatable<PartyMember>
    {
        public ulong clientId;
        public int playerNameHash;      // DungeonNameRegistry 사용
        public int playerLevel;
        public Race playerRace;
        public PartyRole role;
        public bool isReady;            // 던전 입장 준비 상태
        public bool isOnline;           // 온라인 상태
        public Vector3 lastKnownPosition; // 마지막 알려진 위치

        // DungeonNameRegistry 헬퍼 메서드
        public string GetPlayerName()
        {
            return DungeonNameRegistry.GetNameFromHash(playerNameHash);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref playerNameHash);
            serializer.SerializeValue(ref playerLevel);
            serializer.SerializeValue(ref playerRace);
            serializer.SerializeValue(ref role);
            serializer.SerializeValue(ref isReady);
            serializer.SerializeValue(ref isOnline);
            serializer.SerializeValue(ref lastKnownPosition);
        }

        // IEquatable 구현
        public bool Equals(PartyMember other)
        {
            return clientId == other.clientId &&
                   playerNameHash == other.playerNameHash &&
                   playerLevel == other.playerLevel &&
                   playerRace == other.playerRace &&
                   role == other.role &&
                   isReady == other.isReady &&
                   isOnline == other.isOnline &&
                   lastKnownPosition.Equals(other.lastKnownPosition);
        }

        public override bool Equals(object obj)
        {
            return obj is PartyMember other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + clientId.GetHashCode();
                hash = hash * 23 + playerNameHash.GetHashCode();
                hash = hash * 23 + playerLevel.GetHashCode();
                hash = hash * 23 + playerRace.GetHashCode();
                hash = hash * 23 + role.GetHashCode();
                hash = hash * 23 + isReady.GetHashCode();
                hash = hash * 23 + isOnline.GetHashCode();
                hash = hash * 23 + lastKnownPosition.GetHashCode();
                return hash;
            }
        }
    }

    /// <summary>
    /// 파티 정보 (네트워크 직렬화 가능)
    /// </summary>
    [System.Serializable]
    public struct PartyInfo : INetworkSerializable, System.IEquatable<PartyInfo>
    {
        public int partyId;
        public int partyNameHash;       // 파티명 해시
        public PartyState state;
        public int maxMembers;          // 최대 멤버 수 (기본 4명, 최대 16명)
        public bool isPublic;           // 공개 파티 여부
        public int targetDungeonIndex;  // 목표 던전
        public int minimumLevel;        // 최소 레벨 제한
        public bool allowPvP;           // 파티 내 PvP 허용 여부
        
        // 파티 설정
        public float lootShareRadius;   // 아이템 공유 반경 (0 = 개인, >0 = 파티 공유)
        public bool expShareEnabled;    // 경험치 공유 여부
        public bool autoAcceptMembers;  // 자동 멤버 승인

        // 파티명 헬퍼 메서드
        public string GetPartyName()
        {
            return DungeonNameRegistry.GetNameFromHash(partyNameHash);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref partyId);
            serializer.SerializeValue(ref partyNameHash);
            serializer.SerializeValue(ref state);
            serializer.SerializeValue(ref maxMembers);
            serializer.SerializeValue(ref isPublic);
            serializer.SerializeValue(ref targetDungeonIndex);
            serializer.SerializeValue(ref minimumLevel);
            serializer.SerializeValue(ref allowPvP);
            serializer.SerializeValue(ref lootShareRadius);
            serializer.SerializeValue(ref expShareEnabled);
            serializer.SerializeValue(ref autoAcceptMembers);
        }

        // IEquatable 구현 - NetworkList 호환성을 위해 필요
        public bool Equals(PartyInfo other)
        {
            return partyId == other.partyId &&
                   partyNameHash == other.partyNameHash &&
                   state == other.state &&
                   maxMembers == other.maxMembers &&
                   isPublic == other.isPublic &&
                   targetDungeonIndex == other.targetDungeonIndex &&
                   minimumLevel == other.minimumLevel &&
                   allowPvP == other.allowPvP &&
                   Mathf.Approximately(lootShareRadius, other.lootShareRadius) &&
                   expShareEnabled == other.expShareEnabled &&
                   autoAcceptMembers == other.autoAcceptMembers;
        }

        public override bool Equals(object obj)
        {
            return obj is PartyInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + partyId.GetHashCode();
                hash = hash * 23 + partyNameHash.GetHashCode();
                hash = hash * 23 + state.GetHashCode();
                hash = hash * 23 + maxMembers.GetHashCode();
                hash = hash * 23 + isPublic.GetHashCode();
                hash = hash * 23 + targetDungeonIndex.GetHashCode();
                hash = hash * 23 + minimumLevel.GetHashCode();
                hash = hash * 23 + allowPvP.GetHashCode();
                hash = hash * 23 + lootShareRadius.GetHashCode();
                hash = hash * 23 + expShareEnabled.GetHashCode();
                hash = hash * 23 + autoAcceptMembers.GetHashCode();
                return hash;
            }
        }
    }

    /// <summary>
    /// 파티 초대 정보
    /// </summary>
    [System.Serializable]
    public struct PartyInvitation : INetworkSerializable, System.IEquatable<PartyInvitation>
    {
        public int partyId;
        public ulong inviterClientId;
        public ulong inviteeClientId;
        public int inviterNameHash;
        public int partyNameHash;
        public float inviteTime;        // 초대 시간 (만료 체크용)
        public float expireTime;        // 만료 시간 (기본 30초)

        public string GetInviterName()
        {
            return DungeonNameRegistry.GetNameFromHash(inviterNameHash);
        }

        public string GetPartyName()
        {
            return DungeonNameRegistry.GetNameFromHash(partyNameHash);
        }

        public bool IsExpired(float currentTime)
        {
            return currentTime > expireTime;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref partyId);
            serializer.SerializeValue(ref inviterClientId);
            serializer.SerializeValue(ref inviteeClientId);
            serializer.SerializeValue(ref inviterNameHash);
            serializer.SerializeValue(ref partyNameHash);
            serializer.SerializeValue(ref inviteTime);
            serializer.SerializeValue(ref expireTime);
        }

        // IEquatable 구현 - NetworkList 호환성을 위해 필요
        public bool Equals(PartyInvitation other)
        {
            return partyId == other.partyId &&
                   inviterClientId == other.inviterClientId &&
                   inviteeClientId == other.inviteeClientId &&
                   inviterNameHash == other.inviterNameHash &&
                   partyNameHash == other.partyNameHash &&
                   Mathf.Approximately(inviteTime, other.inviteTime) &&
                   Mathf.Approximately(expireTime, other.expireTime);
        }

        public override bool Equals(object obj)
        {
            return obj is PartyInvitation other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + partyId.GetHashCode();
                hash = hash * 23 + inviterClientId.GetHashCode();
                hash = hash * 23 + inviteeClientId.GetHashCode();
                hash = hash * 23 + inviterNameHash.GetHashCode();
                hash = hash * 23 + partyNameHash.GetHashCode();
                hash = hash * 23 + inviteTime.GetHashCode();
                hash = hash * 23 + expireTime.GetHashCode();
                return hash;
            }
        }
    }

    /// <summary>
    /// 파티 스폰 그룹 정보 (던전 입장 시 사용)
    /// NetworkList 호환을 위해 고정 크기 struct 필드 사용
    /// </summary>
    [System.Serializable]
    public struct PartySpawnGroup : INetworkSerializable, System.IEquatable<PartySpawnGroup>
    {
        public int partyId;
        // 고정 크기 배열 대신 개별 필드 사용 (최대 16명)
        public ulong member0, member1, member2, member3;
        public ulong member4, member5, member6, member7;
        public ulong member8, member9, member10, member11;
        public ulong member12, member13, member14, member15;
        public int memberCount;         // 실제 멤버 수
        public Vector3 spawnCenter;     // 파티 스폰 중심점
        public float spawnRadius;       // 파티원 스폰 반경
        public int assignedZone;        // 할당된 던전 존 (0=중앙, 1=내층, 2=외층)
        
        public PartySpawnGroup(int partyId, List<ulong> memberIds)
        {
            // 모든 필드 먼저 초기화
            this.partyId = partyId;
            this.memberCount = Mathf.Min(memberIds?.Count ?? 0, 16);
            this.spawnCenter = Vector3.zero;
            this.spawnRadius = 5f; // 기본 5미터 반경
            this.assignedZone = 0; // 기본 중앙 존
            
            // 개별 멤버 필드 초기화
            member0 = member1 = member2 = member3 = 0;
            member4 = member5 = member6 = member7 = 0;
            member8 = member9 = member10 = member11 = 0;
            member12 = member13 = member14 = member15 = 0;
            
            // 실제 멤버 ID 할당
            if (memberIds != null)
            {
                for (int i = 0; i < this.memberCount; i++)
                {
                    switch (i)
                    {
                        case 0: member0 = memberIds[i]; break;
                        case 1: member1 = memberIds[i]; break;
                        case 2: member2 = memberIds[i]; break;
                        case 3: member3 = memberIds[i]; break;
                        case 4: member4 = memberIds[i]; break;
                        case 5: member5 = memberIds[i]; break;
                        case 6: member6 = memberIds[i]; break;
                        case 7: member7 = memberIds[i]; break;
                        case 8: member8 = memberIds[i]; break;
                        case 9: member9 = memberIds[i]; break;
                        case 10: member10 = memberIds[i]; break;
                        case 11: member11 = memberIds[i]; break;
                        case 12: member12 = memberIds[i]; break;
                        case 13: member13 = memberIds[i]; break;
                        case 14: member14 = memberIds[i]; break;
                        case 15: member15 = memberIds[i]; break;
                    }
                }
            }
        }
        
        // 인덱스로 멤버 설정
        private void SetMemberAtIndex(int index, ulong clientId)
        {
            switch (index)
            {
                case 0: member0 = clientId; break;
                case 1: member1 = clientId; break;
                case 2: member2 = clientId; break;
                case 3: member3 = clientId; break;
                case 4: member4 = clientId; break;
                case 5: member5 = clientId; break;
                case 6: member6 = clientId; break;
                case 7: member7 = clientId; break;
                case 8: member8 = clientId; break;
                case 9: member9 = clientId; break;
                case 10: member10 = clientId; break;
                case 11: member11 = clientId; break;
                case 12: member12 = clientId; break;
                case 13: member13 = clientId; break;
                case 14: member14 = clientId; break;
                case 15: member15 = clientId; break;
            }
        }
        
        // 인덱스로 멤버 조회
        public ulong GetMemberAtIndex(int index)
        {
            return index switch
            {
                0 => member0,
                1 => member1,
                2 => member2,
                3 => member3,
                4 => member4,
                5 => member5,
                6 => member6,
                7 => member7,
                8 => member8,
                9 => member9,
                10 => member10,
                11 => member11,
                12 => member12,
                13 => member13,
                14 => member14,
                15 => member15,
                _ => 0
            };
        }
        
        // 실제 멤버 ID들을 리스트로 반환하는 헬퍼 메서드
        public List<ulong> GetMemberClientIds()
        {
            var list = new List<ulong>();
            for (int i = 0; i < memberCount; i++)
            {
                list.Add(GetMemberAtIndex(i));
            }
            return list;
        }
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref partyId);
            serializer.SerializeValue(ref memberCount);
            serializer.SerializeValue(ref spawnCenter);
            serializer.SerializeValue(ref spawnRadius);
            serializer.SerializeValue(ref assignedZone);
            
            // 개별 멤버 필드 직렬화
            serializer.SerializeValue(ref member0);
            serializer.SerializeValue(ref member1);
            serializer.SerializeValue(ref member2);
            serializer.SerializeValue(ref member3);
            serializer.SerializeValue(ref member4);
            serializer.SerializeValue(ref member5);
            serializer.SerializeValue(ref member6);
            serializer.SerializeValue(ref member7);
            serializer.SerializeValue(ref member8);
            serializer.SerializeValue(ref member9);
            serializer.SerializeValue(ref member10);
            serializer.SerializeValue(ref member11);
            serializer.SerializeValue(ref member12);
            serializer.SerializeValue(ref member13);
            serializer.SerializeValue(ref member14);
            serializer.SerializeValue(ref member15);
        }
        
        // IEquatable<T> 구현 - NetworkList 호환성을 위해 필요
        public bool Equals(PartySpawnGroup other)
        {
            return partyId == other.partyId &&
                   memberCount == other.memberCount &&
                   spawnCenter.Equals(other.spawnCenter) &&
                   Mathf.Approximately(spawnRadius, other.spawnRadius) &&
                   assignedZone == other.assignedZone &&
                   member0 == other.member0 &&
                   member1 == other.member1 &&
                   member2 == other.member2 &&
                   member3 == other.member3 &&
                   member4 == other.member4 &&
                   member5 == other.member5 &&
                   member6 == other.member6 &&
                   member7 == other.member7 &&
                   member8 == other.member8 &&
                   member9 == other.member9 &&
                   member10 == other.member10 &&
                   member11 == other.member11 &&
                   member12 == other.member12 &&
                   member13 == other.member13 &&
                   member14 == other.member14 &&
                   member15 == other.member15;
        }
        
        public override bool Equals(object obj)
        {
            return obj is PartySpawnGroup other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + partyId.GetHashCode();
                hash = hash * 23 + memberCount.GetHashCode();
                hash = hash * 23 + spawnCenter.GetHashCode();
                hash = hash * 23 + spawnRadius.GetHashCode();
                hash = hash * 23 + assignedZone.GetHashCode();
                
                // 실제 사용 중인 멤버들만 해시에 포함
                for (int i = 0; i < memberCount && i < 16; i++)
                {
                    hash = hash * 23 + GetMemberAtIndex(i).GetHashCode();
                }
                
                return hash;
            }
        }
    }
}