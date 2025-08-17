using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 영혼 관리 시스템
    /// 캐릭터 귀속 - 캐릭터 사망 시 모든 영혼 소실
    /// </summary>
    public class SoulInheritance : NetworkBehaviour
    {
        [Header("Soul Settings")]
        [SerializeField] private int maxSoulSlots = 15;
        [SerializeField] private float soulDropRate = 0.001f; // 0.1%
        
        // 캐릭터별 영혼 컬렉션 (서버에서 관리)
        private Dictionary<ulong, List<SoulData>> characterSoulCollections = new Dictionary<ulong, List<SoulData>>();
        
        // 영혼 관련 이벤트
        public System.Action<SoulData> OnSoulAcquired;
        public System.Action<ulong[]> OnSoulCollectionUpdated;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsServer)
            {
                LoadAllCharacterSoulData();
            }
        }
        
        /// <summary>
        /// 캐릭터의 영혼 컬렉션 가져오기
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void GetSoulCollectionServerRpc(ulong characterId, ServerRpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            
            if (characterSoulCollections.ContainsKey(characterId))
            {
                var soulIds = characterSoulCollections[characterId].Select(soul => soul.soulId).ToArray();
                UpdateSoulCollectionClientRpc(soulIds, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
                });
            }
            else
            {
                // 빈 컬렉션 전송
                UpdateSoulCollectionClientRpc(new ulong[0], new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
                });
            }
        }
        
        /// <summary>
        /// 영혼 컬렉션 업데이트 (클라이언트)
        /// </summary>
        [ClientRpc]
        private void UpdateSoulCollectionClientRpc(ulong[] soulIds, ClientRpcParams rpcParams = default)
        {
            OnSoulCollectionUpdated?.Invoke(soulIds);
        }
        
        /// <summary>
        /// 새 영혼 획득
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void AcquireSoulServerRpc(ulong characterId, SoulData newSoul, ServerRpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            
            if (!characterSoulCollections.ContainsKey(characterId))
            {
                characterSoulCollections[characterId] = new List<SoulData>();
            }
            
            // 중복 영혼 확인
            var existingSoul = characterSoulCollections[characterId].FirstOrDefault(soul => soul.soulId == newSoul.soulId);
            if (existingSoul.soulId != 0)
            {
                Debug.Log($"Soul {newSoul.soulName} already owned by character {characterId}");
                return;
            }
            
            // 영혼 추가
            characterSoulCollections[characterId].Add(newSoul);
            
            // 서버 저장
            SaveSoulCollection(characterId);
            
            // 클라이언트에 알림
            SoulAcquiredClientRpc(newSoul, new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
            });
            
            Debug.Log($"Soul '{newSoul.soulName}' acquired by character {characterId}");
        }
        
        /// <summary>
        /// 영혼 획득 알림 (클라이언트)
        /// </summary>
        [ClientRpc]
        private void SoulAcquiredClientRpc(SoulData acquiredSoul, ClientRpcParams rpcParams = default)
        {
            OnSoulAcquired?.Invoke(acquiredSoul);
            Debug.Log($"Soul acquired: {acquiredSoul.soulName} (+{acquiredSoul.statBonus.strength} STR, +{acquiredSoul.statBonus.agility} AGI, etc.)");
        }
        
        /// <summary>
        /// 영혼 소유권 검증
        /// </summary>
        public bool ValidateSoulOwnership(ulong characterId, ulong[] soulIds)
        {
            if (!characterSoulCollections.ContainsKey(characterId))
            {
                return soulIds.Length == 0; // 영혼이 없으면 빈 배열이어야 함
            }
            
            var ownedSoulIds = characterSoulCollections[characterId].Select(soul => soul.soulId).ToHashSet();
            
            foreach (var soulId in soulIds)
            {
                if (!ownedSoulIds.Contains(soulId))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 영혼 보너스 스탯 계산
        /// </summary>
        public StatBlock CalculateSoulBonusStats(ulong characterId, ulong[] equippedSoulIds)
        {
            var totalBonus = new StatBlock();
            
            if (characterSoulCollections.ContainsKey(characterId))
            {
                foreach (var soul in characterSoulCollections[characterId])
                {
                    if (equippedSoulIds.Contains(soul.soulId))
                    {
                        totalBonus = totalBonus + soul.statBonus;
                    }
                }
            }
            
            return totalBonus;
        }
        
        /// <summary>
        /// 영혼 드롭 확률 계산 (LUK 포함)
        /// </summary>
        public bool RollSoulDrop(float luckStat)
        {
            float adjustedDropRate = soulDropRate + (luckStat * 0.0001f); // LUK * 0.01%
            return Random.value < adjustedDropRate;
        }
        
        /// <summary>
        /// 랜덤 영혼 생성
        /// </summary>
        public SoulData GenerateRandomSoul(int dungeonFloor = 1)
        {
            var soulTypes = System.Enum.GetValues(typeof(SoulType)) as SoulType[];
            var rarities = System.Enum.GetValues(typeof(SoulRarity)) as SoulRarity[];
            
            // 던전 층수에 따른 희귀도 가중치
            var rarity = CalculateSoulRarity(dungeonFloor);
            var soulType = soulTypes[Random.Range(0, soulTypes.Length)];
            
            var newSoul = new SoulData
            {
                soulId = GenerateSoulId(),
                soulName = GenerateSoulName(soulType, rarity),
                soulType = soulType,
                rarity = rarity,
                statBonus = GenerateStatBonus(rarity),
                specialEffect = GenerateSpecialEffect(rarity),
                description = GenerateSoulDescription(soulType, rarity),
                floorFound = dungeonFloor,
                acquiredTime = System.DateTime.Now.ToBinary()
            };
            
            return newSoul;
        }
        
        /// <summary>
        /// 던전 층수에 따른 영혼 희귀도 계산
        /// </summary>
        private SoulRarity CalculateSoulRarity(int floor)
        {
            float random = Random.value;
            
            // 층수가 높을수록 좋은 영혼이 나올 확률 증가
            float floorBonus = floor * 0.05f;
            
            if (random < 0.05f + floorBonus) return SoulRarity.Legendary;
            if (random < 0.15f + floorBonus) return SoulRarity.Epic;
            if (random < 0.35f + floorBonus) return SoulRarity.Rare;
            return SoulRarity.Common;
        }
        
        /// <summary>
        /// 희귀도에 따른 스탯 보너스 생성
        /// </summary>
        private StatBlock GenerateStatBonus(SoulRarity rarity)
        {
            var statTypes = new[] { "STR", "AGI", "VIT", "INT", "DEF", "MDEF", "LUK" };
            var selectedStat = statTypes[Random.Range(0, statTypes.Length)];
            
            float bonusValue = rarity switch
            {
                SoulRarity.Common => 1f,
                SoulRarity.Rare => 2f,
                SoulRarity.Epic => 3f,
                SoulRarity.Legendary => Random.Range(3f, 5f),
                _ => 1f
            };
            
            var bonus = new StatBlock();
            switch (selectedStat)
            {
                case "STR": bonus.strength = bonusValue; break;
                case "AGI": bonus.agility = bonusValue; break;
                case "VIT": bonus.vitality = bonusValue; break;
                case "INT": bonus.intelligence = bonusValue; break;
                case "DEF": bonus.defense = bonusValue; break;
                case "MDEF": bonus.magicDefense = bonusValue; break;
                case "LUK": bonus.luck = bonusValue; break;
            }
            
            return bonus;
        }
        
        /// <summary>
        /// 특수 효과 생성 (전설 등급 이상)
        /// </summary>
        private string GenerateSpecialEffect(SoulRarity rarity)
        {
            if (rarity < SoulRarity.Legendary)
                return "";
            
            var effects = new[]
            {
                "드롭률 +5%",
                "경험치 획득 +10%",
                "치명타 확률 +3%",
                "모든 속성 저항 +5%",
                "이동속도 +10%",
                "공격속도 +5%"
            };
            
            return effects[Random.Range(0, effects.Length)];
        }
        
        /// <summary>
        /// 영혼 이름 생성
        /// </summary>
        private string GenerateSoulName(SoulType soulType, SoulRarity rarity)
        {
            string prefix = rarity switch
            {
                SoulRarity.Common => "",
                SoulRarity.Rare => "빛나는 ",
                SoulRarity.Epic => "찬란한 ",
                SoulRarity.Legendary => "전설의 ",
                _ => ""
            };
            
            string baseName = soulType switch
            {
                SoulType.Warrior => "전사의 영혼",
                SoulType.Mage => "마법사의 영혼",
                SoulType.Archer => "궁수의 영혼",
                SoulType.Priest => "성직자의 영혼",
                SoulType.Thief => "도적의 영혼",
                SoulType.Beast => "야수의 영혼",
                SoulType.Elemental => "정령의 영혼",
                SoulType.Ancient => "고대의 영혼",
                _ => "알 수 없는 영혼"
            };
            
            return prefix + baseName;
        }
        
        /// <summary>
        /// 영혼 설명 생성
        /// </summary>
        private string GenerateSoulDescription(SoulType soulType, SoulRarity rarity)
        {
            return $"{rarity} 등급의 {soulType} 영혼입니다. 착용 시 스탯 보너스를 제공합니다.";
        }
        
        /// <summary>
        /// 고유 영혼 ID 생성
        /// </summary>
        private ulong GenerateSoulId()
        {
            return (ulong)(System.DateTime.Now.Ticks + Random.Range(10000, 99999));
        }
        
        /// <summary>
        /// 캐릭터 사망 시 모든 영혼 삭제
        /// </summary>
        public void DeleteAllSoulsOnDeath(ulong characterId)
        {
            if (characterSoulCollections.ContainsKey(characterId))
            {
                characterSoulCollections.Remove(characterId);
                Debug.Log($"All souls deleted for character {characterId} due to death");
            }
        }
        
        /// <summary>
        /// 캐릭터 영혼 데이터 로드
        /// </summary>
        private void LoadAllCharacterSoulData()
        {
            // 실제로는 데이터베이스에서 로드
            Debug.Log("Loading all character soul data...");
        }
        
        /// <summary>
        /// 영혼 컬렉션 저장
        /// </summary>
        private void SaveSoulCollection(ulong characterId)
        {
            // 실제로는 데이터베이스에 저장
            Debug.Log($"Saving soul collection for character {characterId}");
        }
        
        /// <summary>
        /// 캐릭터의 총 영혼 개수 반환
        /// </summary>
        public int GetSoulCount(ulong characterId)
        {
            return characterSoulCollections.ContainsKey(characterId) ? characterSoulCollections[characterId].Count : 0;
        }
        
        /// <summary>
        /// 영혼 컬렉션 통계
        /// </summary>
        public SoulCollectionStats GetSoulCollectionStats(ulong characterId)
        {
            if (!characterSoulCollections.ContainsKey(characterId))
            {
                return new SoulCollectionStats();
            }
            
            var souls = characterSoulCollections[characterId];
            var stats = new SoulCollectionStats
            {
                totalSouls = souls.Count,
                commonSouls = souls.Count(s => s.rarity == SoulRarity.Common),
                rareSouls = souls.Count(s => s.rarity == SoulRarity.Rare),
                epicSouls = souls.Count(s => s.rarity == SoulRarity.Epic),
                legendarySouls = souls.Count(s => s.rarity == SoulRarity.Legendary)
            };
            
            return stats;
        }
    }
    
    /// <summary>
    /// 영혼 데이터 구조체
    /// </summary>
    [System.Serializable]
    public struct SoulData : INetworkSerializable
    {
        public ulong soulId;
        public string soulName;
        public SoulType soulType;
        public SoulRarity rarity;
        public StatBlock statBonus;
        public string specialEffect;
        public string description;
        public int floorFound;
        public long acquiredTime;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref soulId);
            serializer.SerializeValue(ref soulName);
            serializer.SerializeValue(ref soulType);
            serializer.SerializeValue(ref rarity);
            serializer.SerializeValue(ref statBonus);
            serializer.SerializeValue(ref specialEffect);
            serializer.SerializeValue(ref description);
            serializer.SerializeValue(ref floorFound);
            serializer.SerializeValue(ref acquiredTime);
        }
        
        public System.DateTime GetAcquiredDateTime()
        {
            return System.DateTime.FromBinary(acquiredTime);
        }
    }
    
    /// <summary>
    /// 영혼 타입
    /// </summary>
    public enum SoulType
    {
        Warrior,    // 전사
        Mage,       // 마법사
        Archer,     // 궁수
        Priest,     // 성직자
        Thief,      // 도적
        Beast,      // 야수
        Elemental,  // 정령
        Ancient     // 고대
    }
    
    /// <summary>
    /// 영혼 희귀도
    /// </summary>
    public enum SoulRarity
    {
        Common,     // 일반 (+1 스탯)
        Rare,       // 희귀 (+2 스탯)
        Epic,       // 영웅 (+3 스탯)
        Legendary   // 전설 (+3~5 스탯 + 특수 효과)
    }
    
    /// <summary>
    /// 영혼 컬렉션 통계
    /// </summary>
    [System.Serializable]
    public struct SoulCollectionStats
    {
        public int totalSouls;
        public int commonSouls;
        public int rareSouls;
        public int epicSouls;
        public int legendarySouls;
    }
}