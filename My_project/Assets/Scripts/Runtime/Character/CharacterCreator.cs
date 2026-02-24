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
            
            // 마지막 선택 영혼이 있는지 확인
            CheckAndShowSoulInheritanceOption();
        }
        
        /// <summary>
        /// 영혼 상속 옵션 표시
        /// </summary>
        private void CheckAndShowSoulInheritanceOption()
        {
            // SoulInheritance 시스템에서 보존된 영혼 확인
            var soulInheritance = FindFirstObjectByType<SoulInheritance>();
            if (soulInheritance == null)
            {
                Debug.LogError("SoulInheritance system not found! Creating fresh character.");
                ShowRaceSelection();
                return;
            }
            
            var preservedSoul = soulInheritance.GetPreservedSoul();
            if (preservedSoul == null)
            {
                Debug.Log("🆕 No preserved soul found. Creating fresh character.");
                ShowRaceSelection();
                return;
            }
            
            Debug.Log($"🔮 Preserved soul found: {preservedSoul.Value.soulName}");
            ShowSoulInheritanceUI(preservedSoul.Value);
        }
        
        /// <summary>
        /// 영혼 상속 UI 표시
        /// </summary>
        private void ShowSoulInheritanceUI(SoulData availableSoul)
        {
            var soulInheritanceUI = FindFirstObjectByType<SoulInheritanceUI>();
            if (soulInheritanceUI == null)
            {
                Debug.LogError("SoulInheritanceUI not found! Creating fallback decision...");
                // 폴백: 사용자에게 콘솔로 선택 요청
                Debug.Log($"💫 Soul '{availableSoul.soulName}' is available for inheritance.");
                Debug.Log("Creating character with soul inheritance (fallback behavior)");
                CreateCharacterWithSoul(availableSoul);
                return;
            }
            
            Debug.Log($"🔮 Showing soul inheritance UI for: {availableSoul.soulName}");
            
            soulInheritanceUI.ShowInheritanceOption(
                availableSoul,
                (inheritDecision) => OnSoulInheritanceDecision(inheritDecision, availableSoul)
            );
        }

        /// <summary>
        /// 영혼 상속 결정 처리
        /// </summary>
        private void OnSoulInheritanceDecision(bool inheritSoul, SoulData availableSoul)
        {
            if (inheritSoul)
            {
                Debug.Log($"✅ User chose to inherit soul: {availableSoul.soulName}");
                CreateCharacterWithSoul(availableSoul);
            }
            else
            {
                Debug.Log($"❌ User declined soul inheritance: {availableSoul.soulName}");
                // 영혼을 거부하면 완전히 삭제
                var soulInheritance = FindFirstObjectByType<SoulInheritance>();
                soulInheritance?.ConsumePreservedSoul();
                
                ShowRaceSelection();
            }
        }
        
        /// <summary>
        /// 영혼과 함께 캐릭터 생성
        /// </summary>
        private void CreateCharacterWithSoul(SoulData inheritedSoul)
        {
            Debug.Log($"✨ Creating character with inherited soul: {inheritedSoul.soulName}");
            
            // 영혼 상속 시에도 종족 선택 허용 (영혼 출처와 무관하게)
            ShowRaceSelectionWithSoul(inheritedSoul);
        }

        /// <summary>
        /// 영혼 상속과 함께 종족 선택 UI 표시
        /// </summary>
        private void ShowRaceSelectionWithSoul(SoulData inheritedSoul)
        {
            Debug.Log("🎭 Select your race (with soul inheritance):");
            Debug.Log("1. Human - Balanced stats");
            Debug.Log("2. Elf - Magic focused");
            Debug.Log("3. Beast - Physical focused");
            Debug.Log("4. Machina - Defense focused");
            Debug.Log($"✨ You will inherit: {inheritedSoul.soulName} (+{GetSoulBonusText(inheritedSoul.statBonus)})");
            
            // 기본값으로 인간 선택하여 캐릭터 생성
            CreateCharacterWithInheritedSoul(Race.Human, "Soul Inheritor", inheritedSoul);
        }

        /// <summary>
        /// 영혼 상속과 함께 캐릭터 생성 실행
        /// </summary>
        private void CreateCharacterWithInheritedSoul(Race selectedRace, string characterName, SoulData inheritedSoul)
        {
            Debug.Log($"🎭 Creating {selectedRace} character with inherited soul: {inheritedSoul.soulName}");
            
            if (IsServer)
            {
                // 서버에서 직접 생성
                CreateCharacterWithSoulInternal(characterName, selectedRace, inheritedSoul);
            }
            else
            {
                // 클라이언트에서 서버로 요청
                string soulJson = JsonUtility.ToJson(inheritedSoul);
                CreateCharacterWithSoulServerRpc(characterName, selectedRace, soulJson);
            }
        }
        
        /// <summary>
        /// 영혼 설명에서 종족 추출
        /// </summary>
        private Race GetRaceFromSoulDescription(string description)
        {
            if (description.Contains("Human")) return Race.Human;
            if (description.Contains("Elf")) return Race.Elf;
            if (description.Contains("Beast")) return Race.Beast;
            if (description.Contains("Machina")) return Race.Machina;
            
            return Race.Human; // 기본값
        }
        
        /// <summary>
        /// 종족 선택 UI 표시 (영혼 없이 새 캐릭터)
        /// </summary>
        private void ShowRaceSelection()
        {
            Debug.Log("🎭 Select your race:");
            Debug.Log("1. Human - Balanced stats");
            Debug.Log("2. Elf - Magic focused");
            Debug.Log("3. Beast - Physical focused");
            Debug.Log("4. Machina - Defense focused");
            
            // 임시로 인간 자동 선택
            CreateCharacter(Race.Human, "New Adventurer", null);
        }
        
        /// <summary>
        /// 캐릭터 생성 (로컬 메서드)
        /// </summary>
        private void CreateCharacter(Race race, string characterName, SoulData? inheritedSoul)
        {
            Debug.Log($"🎭 Creating character: {characterName} ({race}) with soul: {inheritedSoul?.soulName ?? "None"}");
            
            if (IsServer)
            {
                // 서버에서 직접 생성
                CreateCharacterInternal(characterName, race, inheritedSoul ?? default(SoulData));
            }
            else
            {
                // 클라이언트에서 서버로 요청
                CreateCharacterWithSoulServerRpc(characterName, race, inheritedSoul?.ToJson() ?? "");
            }
        }
        
        /// <summary>
        /// 영혼 상속 캐릭터 생성 요청 (서버 RPC)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CreateCharacterWithSoulServerRpc(string characterName, Race selectedRace, string inheritedSoulJson, ServerRpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            
            SoulData inheritedSoul = default(SoulData);
            if (!string.IsNullOrEmpty(inheritedSoulJson))
            {
                try
                {
                    inheritedSoul = JsonUtility.FromJson<SoulData>(inheritedSoulJson);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to parse inherited soul: {e.Message}");
                }
            }
            
            CreateCharacterInternal(characterName, selectedRace, inheritedSoul, clientId);
        }
        
        /// <summary>
        /// 캐릭터 생성 요청 (구 서버 RPC - 영혼 없이)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CreateCharacterServerRpc(string characterName, Race selectedRace, ServerRpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            CreateCharacterInternal(characterName, selectedRace, default(SoulData), clientId);
        }
        
        /// <summary>
        /// 영혼 상속 캐릭터 생성 내부 메서드
        /// </summary>
        private void CreateCharacterWithSoulInternal(string characterName, Race selectedRace, SoulData inheritedSoul, ulong? clientId = null)
        {
            // 캐릭터 이름 중복 확인
            if (IsCharacterNameTaken(characterName))
            {
                if (clientId.HasValue)
                {
                    CreateCharacterFailedClientRpc("이미 사용중인 캐릭터 이름입니다.", new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId.Value } }
                    });
                }
                return;
            }
            
            // 슬롯 확인
            if (characterSlots.GetAvailableSlots() <= 0)
            {
                if (clientId.HasValue)
                {
                    CreateCharacterFailedClientRpc("캐릭터 슬롯이 모두 찼습니다.", new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId.Value } }
                    });
                }
                return;
            }
            
            // 영혼 상속과 함께 캐릭터 데이터 생성
            var characterData = CreateNewCharacterData(characterName, selectedRace, clientId ?? 0, inheritedSoul);
            
            // SoulInheritance 시스템에 영혼 적용
            var soulInheritance = FindFirstObjectByType<SoulInheritance>();
            if (soulInheritance != null && inheritedSoul.soulId != 0)
            {
                soulInheritance.ApplyPreservedSoulToCharacter(characterData.characterId, inheritedSoul);
                soulInheritance.ConsumePreservedSoul(); // 보존된 영혼 삭제
            }
            
            // 캐릭터 슬롯에 저장
            int slotIndex = characterSlots.AssignCharacterToSlot(clientId ?? 0, characterData);
            
            if (slotIndex >= 0)
            {
                CreateCharacterSuccessClientRpc(characterData, slotIndex, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId ?? 0 } }
                });
                
                Debug.Log($"✨ Character '{characterName}' created with soul inheritance for client {clientId} in slot {slotIndex}");
            }
            else
            {
                CreateCharacterFailedClientRpc("캐릭터 생성에 실패했습니다.", new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId ?? 0 } }
                });
            }
        }

        /// <summary>
        /// 캐릭터 생성 내부 메서드 (영혼 없이)
        /// </summary>
        private void CreateCharacterInternal(string characterName, Race selectedRace, SoulData inheritedSoul, ulong? clientId = null)
        {
            // 영혼이 있으면 영혼 포함 생성으로 리다이렉트
            if (inheritedSoul.soulId != 0)
            {
                CreateCharacterWithSoulInternal(characterName, selectedRace, inheritedSoul, clientId);
                return;
            }

            // 캐릭터 이름 중복 확인
            if (IsCharacterNameTaken(characterName))
            {
                if (clientId.HasValue)
                {
                    CreateCharacterFailedClientRpc("이미 사용중인 캐릭터 이름입니다.", new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId.Value } }
                    });
                }
                return;
            }
            
            // 슬롯 확인
            if (characterSlots.GetAvailableSlots() <= 0)
            {
                if (clientId.HasValue)
                {
                    CreateCharacterFailedClientRpc("캐릭터 슬롯이 모두 찼습니다.", new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId.Value } }
                    });
                }
                return;
            }
            
            // 캐릭터 데이터 생성 (영혼 없이)
            var characterData = CreateNewCharacterData(characterName, selectedRace, clientId ?? 0);
            
            // 캐릭터 슬롯에 저장
            int slotIndex = characterSlots.AssignCharacterToSlot(clientId ?? 0, characterData);
            
            if (slotIndex >= 0)
            {
                CreateCharacterSuccessClientRpc(characterData, slotIndex, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId ?? 0 } }
                });
                
                Debug.Log($"Character '{characterName}' created for client {clientId} in slot {slotIndex}");
            }
            else
            {
                CreateCharacterFailedClientRpc("캐릭터 생성에 실패했습니다.", new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId ?? 0 } }
                });
            }
        }

        /// <summary>
        /// 영혼 보너스 텍스트 반환
        /// </summary>
        private string GetSoulBonusText(StatBlock statBonus)
        {
            var bonuses = new System.Collections.Generic.List<string>();
            
            if (statBonus.strength > 0) bonuses.Add($"STR+{statBonus.strength}");
            if (statBonus.agility > 0) bonuses.Add($"AGI+{statBonus.agility}");
            if (statBonus.vitality > 0) bonuses.Add($"VIT+{statBonus.vitality}");
            if (statBonus.intelligence > 0) bonuses.Add($"INT+{statBonus.intelligence}");
            
            return bonuses.Count > 0 ? string.Join(", ", bonuses) : "No bonuses";
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
        /// 새 캐릭터 데이터 생성 (영혼 상속 포함)
        /// </summary>
        private CharacterData CreateNewCharacterData(string characterName, Race selectedRace, ulong clientId, SoulData inheritedSoul = default)
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
            
            // 영혼 상속 처리
            if (inheritedSoul.soulId != 0)
            {
                // 상속된 영혼 적용
                characterData.equippedSoulIds = new ulong[] { inheritedSoul.soulId };
                characterData.soulBonusStats = inheritedSoul.statBonus;
                Debug.Log($"✨ Character created with inherited soul: {inheritedSoul.soulName}");
            }
            else
            {
                // 영혼 없이 시작
                characterData.equippedSoulIds = new ulong[0];
                characterData.soulBonusStats = new StatBlock();
                Debug.Log($"🆕 Character created without soul inheritance");
            }
            
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
            var allCharacters = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
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

        public override void OnDestroy()
        {
            OnCharacterCreated = null;
            OnCharacterCreationFailed = null;
            base.OnDestroy();
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