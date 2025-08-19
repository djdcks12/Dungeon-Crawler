using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 던전 타입 열거형
    /// </summary>
    public enum DungeonType
    {
        Normal,         // 일반 던전
        Elite,          // 엘리트 던전 (강화된 몬스터)
        Boss,           // 보스 던전 (보스 몬스터 등장)
        Challenge,      // 도전 던전 (특수 규칙)
        PvP             // PvP 던전 (플레이어 대전)
    }
    
    /// <summary>
    /// 던전 난이도
    /// </summary>
    public enum DungeonDifficulty
    {
        Easy = 1,       // 쉬움 (1-3층 추천)
        Normal = 2,     // 보통 (4-6층 추천) 
        Hard = 3,       // 어려움 (7-9층 추천)
        Nightmare = 4   // 악몽 (10층 추천)
    }
    
    /// <summary>
    /// 던전 상태
    /// </summary>
    public enum DungeonState
    {
        Waiting,        // 대기 중 (플레이어 입장 대기)
        Active,         // 진행 중
        Completed,      // 완료
        Failed,         // 실패
        Abandoned       // 포기
    }
    
    /// <summary>
    /// 던전 시간 관리 모드
    /// </summary>
    public enum DungeonTimeMode
    {
        PerFloor,       // 층별 개별 제한시간 (남은 시간 이월 가능)
        Total,          // 던전 전체 제한시간 (총 시간 고정)
        Custom          // FloorConfiguration에서 개별 설정
    }
    
    /// <summary>
    /// 던전 정보 데이터
    /// </summary>
    [System.Serializable]
    public struct DungeonInfo : INetworkSerializable
    {
        public int dungeonId;
        public int dungeonNameHash; // string 대신 해시값 사용
        public DungeonType dungeonType;
        public DungeonDifficulty difficulty;
        public int currentFloor;
        public int maxFloors;
        public int recommendedLevel;
        public int maxPlayers;
        public float timeLimit; // 제한 시간 (초)
        public long baseExpReward;
        public long baseGoldReward;
        
        // 해시에서 실제 이름을 가져오는 헬퍼 메서드
        public string GetDungeonName()
        {
            return DungeonNameRegistry.GetNameFromHash(dungeonNameHash);
        }
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref dungeonId);
            serializer.SerializeValue(ref dungeonNameHash);
            serializer.SerializeValue(ref dungeonType);
            serializer.SerializeValue(ref difficulty);
            serializer.SerializeValue(ref currentFloor);
            serializer.SerializeValue(ref maxFloors);
            serializer.SerializeValue(ref recommendedLevel);
            serializer.SerializeValue(ref maxPlayers);
            serializer.SerializeValue(ref timeLimit);
            serializer.SerializeValue(ref baseExpReward);
            serializer.SerializeValue(ref baseGoldReward);
        }
    }
    
    /// <summary>
    /// 던전 플레이어 정보
    /// </summary>
    [System.Serializable]
    public struct DungeonPlayer : INetworkSerializable, System.IEquatable<DungeonPlayer>
    {
        public ulong clientId;
        public int playerNameHash; // string 대신 해시값 사용
        public int playerLevel;
        public Race playerRace;
        public bool isAlive;
        public bool isReady;
        public Vector3 spawnPosition;
        public long currentExp;
        public long currentGold;
        
        // 해시에서 실제 이름을 가져오는 헬퍼 메서드
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
            serializer.SerializeValue(ref isAlive);
            serializer.SerializeValue(ref isReady);
            serializer.SerializeValue(ref spawnPosition);
            serializer.SerializeValue(ref currentExp);
            serializer.SerializeValue(ref currentGold);
        }
        
        /// <summary>
        /// IEquatable 구현 - NetworkList 호환성을 위해 필요
        /// </summary>
        public bool Equals(DungeonPlayer other)
        {
            return clientId == other.clientId &&
                   playerNameHash == other.playerNameHash &&
                   playerLevel == other.playerLevel &&
                   playerRace == other.playerRace &&
                   isAlive == other.isAlive &&
                   isReady == other.isReady &&
                   spawnPosition.Equals(other.spawnPosition) &&
                   currentExp == other.currentExp &&
                   currentGold == other.currentGold;
        }
        
        /// <summary>
        /// Object.Equals 오버라이드
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is DungeonPlayer other && Equals(other);
        }
        
        /// <summary>
        /// GetHashCode 오버라이드 - Equals와 함께 구현 필수
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + clientId.GetHashCode();
                hash = hash * 23 + playerNameHash.GetHashCode();
                hash = hash * 23 + playerLevel.GetHashCode();
                hash = hash * 23 + playerRace.GetHashCode();
                hash = hash * 23 + isAlive.GetHashCode();
                hash = hash * 23 + isReady.GetHashCode();
                hash = hash * 23 + spawnPosition.GetHashCode();
                hash = hash * 23 + currentExp.GetHashCode();
                hash = hash * 23 + currentGold.GetHashCode();
                return hash;
            }
        }
    }
    
    /// <summary>
    /// 던전 층 정보
    /// </summary>
    [System.Serializable]
    public struct DungeonFloor : INetworkSerializable
    {
        public int floorNumber;
        public int floorNameHash; // string 대신 해시값 사용
        public Vector2 floorSize; // 던전 크기 (가로, 세로)
        public int monsterCount;
        public int eliteCount;
        public bool hasBoss;
        public bool hasExit;
        public float completionBonus; // 완주 보너스 배율
        
        // 해시에서 실제 이름을 가져오는 헬퍼 메서드
        public string GetFloorName()
        {
            return DungeonNameRegistry.GetNameFromHash(floorNameHash);
        }
        
        // 스폰 포인트들
        public Vector3 playerSpawnPoint;
        public Vector3 exitPoint;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref floorNumber);
            serializer.SerializeValue(ref floorNameHash);
            serializer.SerializeValue(ref floorSize);
            serializer.SerializeValue(ref monsterCount);
            serializer.SerializeValue(ref eliteCount);
            serializer.SerializeValue(ref hasBoss);
            serializer.SerializeValue(ref hasExit);
            serializer.SerializeValue(ref completionBonus);
            serializer.SerializeValue(ref playerSpawnPoint);
            serializer.SerializeValue(ref exitPoint);
        }
    }
    
    /// <summary>
    /// 던전 보상 정보
    /// </summary>
    [System.Serializable]
    public struct DungeonReward : INetworkSerializable
    {
        public long expReward;
        public long goldReward;
        public List<ItemInstance> itemRewards;
        public float completionTime;
        public int floorReached;
        public int monstersKilled;
        public float survivalRate;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref expReward);
            serializer.SerializeValue(ref goldReward);
            serializer.SerializeValue(ref completionTime);
            serializer.SerializeValue(ref floorReached);
            serializer.SerializeValue(ref monstersKilled);
            serializer.SerializeValue(ref survivalRate);
            
            // 아이템 보상 직렬화 (복잡하므로 별도 처리)
            if (serializer.IsReader)
            {
                int itemCount = 0;
                serializer.SerializeValue(ref itemCount);
                itemRewards = new List<ItemInstance>();
                
                for (int i = 0; i < itemCount; i++)
                {
                    var item = new ItemInstance();
                    item.NetworkSerialize(serializer);
                    itemRewards.Add(item);
                }
            }
            else
            {
                int itemCount = itemRewards?.Count ?? 0;
                serializer.SerializeValue(ref itemCount);
                
                if (itemRewards != null)
                {
                    foreach (var item in itemRewards)
                    {
                        var itemCopy = item;
                        itemCopy.NetworkSerialize(serializer);
                    }
                }
            }
        }
    }
}