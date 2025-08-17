using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 캐릭터 슬롯 관리 시스템
    /// 계정당 최대 3개 캐릭터 슬롯
    /// </summary>
    public class CharacterSlots : NetworkBehaviour
    {
        [Header("Slot Settings")]
        [SerializeField] private int maxSlotsPerAccount = 3;
        
        // 계정별 캐릭터 슬롯 (서버에서 관리)
        private Dictionary<ulong, CharacterSlot[]> accountSlots = new Dictionary<ulong, CharacterSlot[]>();
        
        // 슬롯 관련 이벤트
        public System.Action<CharacterSlot[]> OnSlotsUpdated;
        public System.Action<int, CharacterData> OnCharacterAssigned;
        public System.Action<int> OnSlotFreed;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsServer)
            {
                LoadAllAccountSlotData();
            }
        }
        
        /// <summary>
        /// 계정의 캐릭터 슬롯 정보 요청
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void GetCharacterSlotsServerRpc(ServerRpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            
            if (!accountSlots.ContainsKey(clientId))
            {
                InitializeAccountSlots(clientId);
            }
            
            UpdateCharacterSlotsClientRpc(accountSlots[clientId], new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
            });
        }
        
        /// <summary>
        /// 캐릭터 슬롯 정보 업데이트 (클라이언트)
        /// </summary>
        [ClientRpc]
        private void UpdateCharacterSlotsClientRpc(CharacterSlot[] slots, ClientRpcParams rpcParams = default)
        {
            OnSlotsUpdated?.Invoke(slots);
        }
        
        /// <summary>
        /// 빈 슬롯에 캐릭터 할당
        /// </summary>
        public int AssignCharacterToSlot(ulong clientId, CharacterData characterData)
        {
            if (!accountSlots.ContainsKey(clientId))
            {
                InitializeAccountSlots(clientId);
            }
            
            var slots = accountSlots[clientId];
            
            // 빈 슬롯 찾기
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].isOccupied)
                {
                    slots[i].isOccupied = true;
                    slots[i].characterData = characterData;
                    slots[i].lastAccessTime = System.DateTime.Now.ToBinary();
                    
                    // 슬롯 데이터 저장
                    SaveAccountSlots(clientId);
                    
                    // 클라이언트에 알림
                    CharacterAssignedClientRpc(i, characterData, new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
                    });
                    
                    return i;
                }
            }
            
            return -1; // 빈 슬롯 없음
        }
        
        /// <summary>
        /// 캐릭터 할당 알림 (클라이언트)
        /// </summary>
        [ClientRpc]
        private void CharacterAssignedClientRpc(int slotIndex, CharacterData characterData, ClientRpcParams rpcParams = default)
        {
            OnCharacterAssigned?.Invoke(slotIndex, characterData);
        }
        
        /// <summary>
        /// 슬롯에서 캐릭터 제거 (사망 시)
        /// </summary>
        public bool RemoveCharacterFromSlot(ulong clientId, ulong characterId)
        {
            if (!accountSlots.ContainsKey(clientId))
            {
                return false;
            }
            
            var slots = accountSlots[clientId];
            
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].isOccupied && slots[i].characterData.characterId == characterId)
                {
                    slots[i].isOccupied = false;
                    slots[i].characterData = new CharacterData();
                    slots[i].lastAccessTime = 0;
                    
                    // 슬롯 데이터 저장
                    SaveAccountSlots(clientId);
                    
                    // 클라이언트에 알림
                    SlotFreedClientRpc(i, new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
                    });
                    
                    Debug.Log($"Character {characterId} removed from slot {i} for client {clientId}");
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 슬롯 해제 알림 (클라이언트)
        /// </summary>
        [ClientRpc]
        private void SlotFreedClientRpc(int slotIndex, ClientRpcParams rpcParams = default)
        {
            OnSlotFreed?.Invoke(slotIndex);
        }
        
        /// <summary>
        /// 캐릭터 선택
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SelectCharacterServerRpc(int slotIndex, ServerRpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            
            if (!accountSlots.ContainsKey(clientId))
            {
                return;
            }
            
            var slots = accountSlots[clientId];
            
            if (slotIndex < 0 || slotIndex >= slots.Length || !slots[slotIndex].isOccupied)
            {
                CharacterSelectionFailedClientRpc("유효하지 않은 캐릭터 슬롯입니다.", new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
                });
                return;
            }
            
            // 마지막 접근 시간 업데이트
            slots[slotIndex].lastAccessTime = System.DateTime.Now.ToBinary();
            SaveAccountSlots(clientId);
            
            // 캐릭터 로드 성공
            CharacterSelectedClientRpc(slots[slotIndex].characterData, new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
            });
        }
        
        /// <summary>
        /// 캐릭터 선택 성공 알림
        /// </summary>
        [ClientRpc]
        private void CharacterSelectedClientRpc(CharacterData characterData, ClientRpcParams rpcParams = default)
        {
            Debug.Log($"Character selected: {characterData.characterName} (Level {characterData.level})");
            // 실제 게임으로 전환하는 로직은 별도 GameManager에서 처리
        }
        
        /// <summary>
        /// 캐릭터 선택 실패 알림
        /// </summary>
        [ClientRpc]
        private void CharacterSelectionFailedClientRpc(string errorMessage, ClientRpcParams rpcParams = default)
        {
            Debug.LogWarning($"Character selection failed: {errorMessage}");
        }
        
        /// <summary>
        /// 사용 가능한 슬롯 수 반환
        /// </summary>
        public int GetAvailableSlots()
        {
            var clientId = NetworkManager.Singleton.LocalClientId;
            
            if (!accountSlots.ContainsKey(clientId))
            {
                return maxSlotsPerAccount;
            }
            
            return accountSlots[clientId].Count(slot => !slot.isOccupied);
        }
        
        /// <summary>
        /// 계정 슬롯 초기화
        /// </summary>
        private void InitializeAccountSlots(ulong clientId)
        {
            var slots = new CharacterSlot[maxSlotsPerAccount];
            
            for (int i = 0; i < maxSlotsPerAccount; i++)
            {
                slots[i] = new CharacterSlot
                {
                    slotIndex = i,
                    isOccupied = false,
                    characterData = new CharacterData(),
                    lastAccessTime = 0
                };
            }
            
            accountSlots[clientId] = slots;
        }
        
        /// <summary>
        /// 계정 슬롯 데이터 로드
        /// </summary>
        private void LoadAllAccountSlotData()
        {
            // 실제로는 데이터베이스에서 로드
            Debug.Log("Loading all account slot data...");
        }
        
        /// <summary>
        /// 계정 슬롯 데이터 저장
        /// </summary>
        private void SaveAccountSlots(ulong clientId)
        {
            // 실제로는 데이터베이스에 저장
            Debug.Log($"Saving slot data for client {clientId}");
        }
        
        /// <summary>
        /// 계정의 모든 캐릭터 정보 반환
        /// </summary>
        public CharacterSlot[] GetAccountSlots(ulong clientId)
        {
            if (!accountSlots.ContainsKey(clientId))
            {
                InitializeAccountSlots(clientId);
            }
            
            return accountSlots[clientId];
        }
        
        /// <summary>
        /// 캐릭터 슬롯 통계
        /// </summary>
        public SlotStatistics GetSlotStatistics(ulong clientId)
        {
            if (!accountSlots.ContainsKey(clientId))
            {
                return new SlotStatistics
                {
                    totalSlots = maxSlotsPerAccount,
                    occupiedSlots = 0,
                    availableSlots = maxSlotsPerAccount
                };
            }
            
            var slots = accountSlots[clientId];
            int occupied = slots.Count(slot => slot.isOccupied);
            
            return new SlotStatistics
            {
                totalSlots = maxSlotsPerAccount,
                occupiedSlots = occupied,
                availableSlots = maxSlotsPerAccount - occupied,
                totalPlayTime = CalculateTotalPlayTime(slots),
                highestLevel = GetHighestLevel(slots)
            };
        }
        
        /// <summary>
        /// 총 플레이 시간 계산
        /// </summary>
        private long CalculateTotalPlayTime(CharacterSlot[] slots)
        {
            long totalTime = 0;
            foreach (var slot in slots)
            {
                if (slot.isOccupied)
                {
                    totalTime += slot.lastAccessTime - slot.characterData.creationTime;
                }
            }
            return totalTime;
        }
        
        /// <summary>
        /// 최고 레벨 캐릭터 찾기
        /// </summary>
        private int GetHighestLevel(CharacterSlot[] slots)
        {
            int maxLevel = 0;
            foreach (var slot in slots)
            {
                if (slot.isOccupied)
                {
                    maxLevel = Mathf.Max(maxLevel, slot.characterData.level);
                }
            }
            return maxLevel;
        }
    }
    
    /// <summary>
    /// 캐릭터 슬롯 구조체
    /// </summary>
    [System.Serializable]
    public struct CharacterSlot : INetworkSerializable
    {
        public int slotIndex;
        public bool isOccupied;
        public CharacterData characterData;
        public long lastAccessTime;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref slotIndex);
            serializer.SerializeValue(ref isOccupied);
            serializer.SerializeValue(ref characterData);
            serializer.SerializeValue(ref lastAccessTime);
        }
        
        public System.DateTime GetLastAccessDateTime()
        {
            return System.DateTime.FromBinary(lastAccessTime);
        }
    }
    
    /// <summary>
    /// 슬롯 통계 구조체
    /// </summary>
    [System.Serializable]
    public struct SlotStatistics
    {
        public int totalSlots;
        public int occupiedSlots;
        public int availableSlots;
        public long totalPlayTime;
        public int highestLevel;
    }
}