using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 하드코어 던전 크롤러 캐릭터 생성 시스템
    /// 영혼 기반 새 캐릭터 시스템
    /// </summary>
    public class CharacterCreator : NetworkBehaviour
    {
        [Header("Character Creation Settings")]
        [SerializeField] private RaceData[] availableRaces;
        [SerializeField] private int maxCharacterSlots = 3;
        [SerializeField] private int maxSoulSlots = 15;
        
        [Header("Starting Equipment")]
        [SerializeField] private int startingGold = 100;
        [SerializeField] private int startingPotions = 5;
        
        private SoulInheritance soulInheritance;
        private CharacterSlots characterSlots;
        
        // 캐릭터 생성 이벤트
        public System.Action<CharacterData> OnCharacterCreated;
        public System.Action<string> OnCharacterCreationFailed;
        
        private void Awake()
        {
            soulInheritance = GetComponent<SoulInheritance>();
            characterSlots = GetComponent<CharacterSlots>();
            
            // 기본 종족 데이터 로드
            if (availableRaces == null || availableRaces.Length == 0)
            {
                LoadDefaultRaceData();
            }
        }
        
        /// <summary>
        /// 새 캐릭터 생성 시작
        /// </summary>
        public void StartCharacterCreation()
        {
            if (characterSlots.GetAvailableSlots() <= 0)
            {
                OnCharacterCreationFailed?.Invoke("캐릭터 슬롯이 모두 찼습니다.");
                return;
            }
            
            Debug.Log("Character creation started");
            // UI 활성화는 별도 UI 컨트롤러에서 처리
        }
        
        /// <summary>
        /// 캐릭터 생성 요청 (서버 RPC)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CreateCharacterServerRpc(string characterName, Race selectedRace, ServerRpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            
            // 캐릭터 이름 중복 확인
            if (IsCharacterNameTaken(characterName))
            {
                CreateCharacterFailedClientRpc("이미 사용중인 캐릭터 이름입니다.", new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
                });
                return;
            }
            
            // 슬롯 확인
            if (characterSlots.GetAvailableSlots() <= 0)
            {
                CreateCharacterFailedClientRpc("캐릭터 슬롯이 모두 찼습니다.", new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
                });
                return;
            }
            
            // 새 캐릭터는 영혼 없이 시작 (영혼은 캐릭터 귀속)
            
            // 캐릭터 데이터 생성
            var characterData = CreateNewCharacterData(characterName, selectedRace, clientId);
            
            // 캐릭터 슬롯에 저장
            int slotIndex = characterSlots.AssignCharacterToSlot(clientId, characterData);
            
            if (slotIndex >= 0)
            {
                CreateCharacterSuccessClientRpc(characterData, slotIndex, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
                });
                
                Debug.Log($"Character '{characterName}' created for client {clientId} in slot {slotIndex}");
            }
            else
            {
                CreateCharacterFailedClientRpc("캐릭터 생성에 실패했습니다.", new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
                });
            }
        }
        
        /// <summary>
        /// 캐릭터 생성 성공 알림
        /// </summary>
        [ClientRpc]
        private void CreateCharacterSuccessClientRpc(CharacterData characterData, int slotIndex, ClientRpcParams rpcParams = default)
        {
            OnCharacterCreated?.Invoke(characterData);
            Debug.Log($"Character creation successful: {characterData.characterName} in slot {slotIndex}");
        }
        
        /// <summary>
        /// 캐릭터 생성 실패 알림
        /// </summary>
        [ClientRpc]
        private void CreateCharacterFailedClientRpc(string errorMessage, ClientRpcParams rpcParams = default)
        {
            OnCharacterCreationFailed?.Invoke(errorMessage);
            Debug.LogWarning($"Character creation failed: {errorMessage}");
        }
        
        /// <summary>
        /// 새 캐릭터 데이터 생성
        /// </summary>
        private CharacterData CreateNewCharacterData(string characterName, Race selectedRace, ulong clientId)
        {
            var characterData = new CharacterData
            {
                characterId = GenerateCharacterId(),
                characterName = characterName,
                race = selectedRace,
                level = 1,
                experience = 0,
                gold = startingGold,
                creationTime = System.DateTime.Now.ToBinary(),
                lastPlayTime = System.DateTime.Now.ToBinary(),
                ownerId = clientId
            };
            
            // 종족별 기본 스탯 설정
            var raceData = GetRaceData(selectedRace);
            if (raceData != null)
            {
                characterData.baseStats = raceData.CalculateStatsAtLevel(1);
            }
            
            // 새 캐릭터는 영혼 없이 시작
            characterData.equippedSoulIds = new ulong[0];
            characterData.soulBonusStats = new StatBlock();
            
            // 기본 장비 지급
            characterData.startingItems = CreateStartingItems(selectedRace);
            
            return characterData;
        }
        
        /// <summary>
        /// 캐릭터 이름 중복 확인
        /// </summary>
        private bool IsCharacterNameTaken(string characterName)
        {
            // 실제로는 데이터베이스나 서버 저장소에서 확인
            // 지금은 간단히 현재 접속한 플레이어들만 확인
            var allCharacters = FindObjectsOfType<PlayerController>();
            foreach (var character in allCharacters)
            {
                if (character.name == characterName)
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 종족 데이터 가져오기
        /// </summary>
        private RaceData GetRaceData(Race race)
        {
            foreach (var raceData in availableRaces)
            {
                if (raceData.raceType == race)
                {
                    return raceData;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 시작 아이템 생성
        /// </summary>
        private StartingItem[] CreateStartingItems(Race race)
        {
            var items = new List<StartingItem>();
            
            // 기본 무기 (종족별)
            switch (race)
            {
                case Race.Human:
                    items.Add(new StartingItem { itemId = "basic_sword", quantity = 1 });
                    items.Add(new StartingItem { itemId = "basic_shield", quantity = 1 });
                    break;
                case Race.Elf:
                    items.Add(new StartingItem { itemId = "basic_staff", quantity = 1 });
                    items.Add(new StartingItem { itemId = "basic_robe", quantity = 1 });
                    break;
                case Race.Beast:
                    items.Add(new StartingItem { itemId = "basic_claw", quantity = 1 });
                    items.Add(new StartingItem { itemId = "basic_leather", quantity = 1 });
                    break;
                case Race.Machina:
                    items.Add(new StartingItem { itemId = "basic_hammer", quantity = 1 });
                    items.Add(new StartingItem { itemId = "basic_plate", quantity = 1 });
                    break;
            }
            
            // 기본 포션
            items.Add(new StartingItem { itemId = "health_potion", quantity = startingPotions });
            
            return items.ToArray();
        }
        
        /// <summary>
        /// 고유 캐릭터 ID 생성
        /// </summary>
        private ulong GenerateCharacterId()
        {
            // 실제로는 서버에서 고유 ID 생성
            return (ulong)(System.DateTime.Now.Ticks + Random.Range(1000, 9999));
        }
        
        /// <summary>
        /// 기본 종족 데이터 로드
        /// </summary>
        private void LoadDefaultRaceData()
        {
            availableRaces = new RaceData[]
            {
                RaceDataCreator.CreateHumanRaceData(),
                RaceDataCreator.CreateElfRaceData(),
                RaceDataCreator.CreateBeastRaceData(),
                RaceDataCreator.CreateMachinaRaceData()
            };
        }
        
        /// <summary>
        /// 사용 가능한 종족 리스트 반환
        /// </summary>
        public RaceData[] GetAvailableRaces()
        {
            return availableRaces;
        }
        
        /// <summary>
        /// 캐릭터 삭제 (사망 시)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void DeleteCharacterServerRpc(ulong characterId, ServerRpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            
            // 캐릭터 슬롯에서 제거
            characterSlots.RemoveCharacterFromSlot(clientId, characterId);
            
            Debug.Log($"Character {characterId} deleted for client {clientId}");
        }
        
        /// <summary>
        /// 디버그용 모든 종족 정보 출력
        /// </summary>
        [ContextMenu("Log All Race Info")]
        public void LogAllRaceInfo()
        {
            RaceDataCreator.LogAllRaceData();
        }
    }
    
    /// <summary>
    /// 캐릭터 데이터 구조체
    /// </summary>
    [System.Serializable]
    public struct CharacterData : INetworkSerializable
    {
        public ulong characterId;
        public string characterName;
        public Race race;
        public int level;
        public long experience;
        public int gold;
        public StatBlock baseStats;
        public StatBlock soulBonusStats;
        public ulong[] equippedSoulIds;
        public StartingItem[] startingItems;
        public long creationTime;
        public long lastPlayTime;
        public ulong ownerId;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref characterId);
            serializer.SerializeValue(ref characterName);
            serializer.SerializeValue(ref race);
            serializer.SerializeValue(ref level);
            serializer.SerializeValue(ref experience);
            serializer.SerializeValue(ref gold);
            serializer.SerializeValue(ref baseStats);
            serializer.SerializeValue(ref soulBonusStats);
            serializer.SerializeValue(ref equippedSoulIds);
            serializer.SerializeValue(ref startingItems);
            serializer.SerializeValue(ref creationTime);
            serializer.SerializeValue(ref lastPlayTime);
            serializer.SerializeValue(ref ownerId);
        }
        
        public System.DateTime GetCreationDateTime()
        {
            return System.DateTime.FromBinary(creationTime);
        }
        
        public System.DateTime GetLastPlayDateTime()
        {
            return System.DateTime.FromBinary(lastPlayTime);
        }
    }
    
    /// <summary>
    /// 시작 아이템 구조체
    /// </summary>
    [System.Serializable]
    public struct StartingItem : INetworkSerializable
    {
        public string itemId;
        public int quantity;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref itemId);
            serializer.SerializeValue(ref quantity);
        }
    }
}