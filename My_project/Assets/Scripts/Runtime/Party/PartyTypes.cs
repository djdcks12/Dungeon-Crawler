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
    public struct PartyInfo : INetworkSerializable
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
    }

    /// <summary>
    /// 파티 초대 정보
    /// </summary>
    [System.Serializable]
    public struct PartyInvitation : INetworkSerializable
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
    }

    /// <summary>
    /// 파티 스폰 그룹 정보 (던전 입장 시 사용)
    /// </summary>
    [System.Serializable]
    public struct PartySpawnGroup
    {
        public int partyId;
        public List<ulong> memberClientIds;
        public Vector3 spawnCenter;     // 파티 스폰 중심점
        public float spawnRadius;       // 파티원 스폰 반경
        public int assignedZone;        // 할당된 던전 존 (0=중앙, 1=내층, 2=외층)
        
        public PartySpawnGroup(int partyId, List<ulong> memberIds)
        {
            this.partyId = partyId;
            this.memberClientIds = memberIds ?? new List<ulong>();
            this.spawnCenter = Vector3.zero;
            this.spawnRadius = 5f; // 기본 5미터 반경
            this.assignedZone = 0; // 기본 중앙 존
        }
    }
}