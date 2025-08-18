using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 캐릭터 삭제 시스템 - 하드코어 데스 페널티
    /// 사망 시 캐릭터 완전 삭제, 복구 불가
    /// </summary>
    public class CharacterDeletion : NetworkBehaviour
    {
        [Header("Deletion Settings")]
        [SerializeField] private bool enableDeletion = true;
        [SerializeField] private float deletionDelay = 1.0f;
        
        // 컴포넌트 참조
        private PlayerStatsManager statsManager;
        private DeathManager deathManager;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            statsManager = GetComponent<PlayerStatsManager>();
            deathManager = GetComponent<DeathManager>();
        }
        
        /// <summary>
        /// 캐릭터 삭제 실행
        /// </summary>
        public void DeleteCharacter()
        {
            if (!enableDeletion)
            {
                Debug.LogWarning("Character deletion is disabled!");
                return;
            }
            
            if (!IsServer)
            {
                Debug.LogError("Character deletion must be called on server!");
                return;
            }
            
            Debug.Log($"🗑️ Deleting character: {GetCharacterName()}");
            
            // 캐릭터 데이터 삭제
            DeleteCharacterData();
            
            // 클라이언트에 삭제 알림
            NotifyCharacterDeletionClientRpc();
        }
        
        /// <summary>
        /// 캐릭터 데이터 삭제
        /// </summary>
        private void DeleteCharacterData()
        {
            string characterId = GetCharacterName();
            
            if (string.IsNullOrEmpty(characterId))
            {
                Debug.LogError("Cannot delete character: Invalid character ID");
                return;
            }
            
            // 캐릭터 세이브 데이터 삭제
            DeleteSaveData(characterId);
            
            // 캐릭터 슬롯 정리
            ClearCharacterSlot(characterId);
            
            // 임시 아이템 데이터 삭제 (추후 인벤토리 시스템 연동)
            ClearInventoryData(characterId);
            
            Debug.Log($"✅ Character data deleted: {characterId}");
        }
        
        /// <summary>
        /// 세이브 데이터 삭제
        /// </summary>
        private void DeleteSaveData(string characterId)
        {
            // PlayerPrefs에서 캐릭터 관련 데이터 모두 삭제
            var keysToDelete = new[]
            {
                $"Character_{characterId}_Level",
                $"Character_{characterId}_Experience",
                $"Character_{characterId}_Gold",
                $"Character_{characterId}_Stats",
                $"Character_{characterId}_Skills",
                $"Character_{characterId}_Inventory",
                $"Character_{characterId}_Equipment",
                $"Character_{characterId}_Position",
                $"Character_{characterId}_LastSave"
            };
            
            foreach (string key in keysToDelete)
            {
                if (PlayerPrefs.HasKey(key))
                {
                    PlayerPrefs.DeleteKey(key);
                    Debug.Log($"🗑️ Deleted save key: {key}");
                }
            }
            
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// 캐릭터 슬롯 정리
        /// </summary>
        private void ClearCharacterSlot(string characterId)
        {
            // 캐릭터 슬롯 정보 가져오기
            for (int slot = 0; slot < 3; slot++) // 최대 3개 슬롯
            {
                string slotKey = $"CharacterSlot_{slot}";
                string slotCharacterId = PlayerPrefs.GetString(slotKey, "");
                
                if (slotCharacterId == characterId)
                {
                    // 해당 슬롯 비우기
                    PlayerPrefs.DeleteKey(slotKey);
                    PlayerPrefs.DeleteKey($"CharacterSlot_{slot}_Name");
                    PlayerPrefs.DeleteKey($"CharacterSlot_{slot}_Level");
                    PlayerPrefs.DeleteKey($"CharacterSlot_{slot}_Race");
                    PlayerPrefs.DeleteKey($"CharacterSlot_{slot}_CreateTime");
                    
                    Debug.Log($"🔓 Character slot {slot} cleared");
                    break;
                }
            }
            
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// 인벤토리 데이터 삭제
        /// </summary>
        private void ClearInventoryData(string characterId)
        {
            // 인벤토리 관련 데이터 삭제 (추후 인벤토리 시스템과 연동)
            var inventoryKeys = new[]
            {
                $"Inventory_{characterId}_Items",
                $"Inventory_{characterId}_Equipment",
                $"Inventory_{characterId}_Size",
                $"Equipment_{characterId}_Weapon",
                $"Equipment_{characterId}_Armor",
                $"Equipment_{characterId}_Accessory"
            };
            
            foreach (string key in inventoryKeys)
            {
                if (PlayerPrefs.HasKey(key))
                {
                    PlayerPrefs.DeleteKey(key);
                }
            }
        }
        
        /// <summary>
        /// 캐릭터 이름 가져오기
        /// </summary>
        private string GetCharacterName()
        {
            if (statsManager?.CurrentStats != null)
            {
                return statsManager.CurrentStats.CharacterName;
            }
            
            // 대체값으로 오브젝트 이름 사용
            return gameObject.name.Replace("(Clone)", "");
        }
        
        /// <summary>
        /// 캐릭터 삭제 알림
        /// </summary>
        [ClientRpc]
        private void NotifyCharacterDeletionClientRpc()
        {
            if (IsOwner)
            {
                HandleLocalCharacterDeletion();
            }
            
            Debug.Log($"📢 Character {GetCharacterName()} has been permanently deleted");
        }
        
        /// <summary>
        /// 로컬 캐릭터 삭제 처리
        /// </summary>
        private void HandleLocalCharacterDeletion()
        {
            // 로컬 플레이어의 캐릭터가 삭제됨을 알림
            Debug.Log("💀 Your character has been permanently deleted!");
            
            // UI 메시지 표시 (추후 UI 시스템에서 구현)
            ShowDeletionMessage();
            
            // 게임 통계 업데이트
            UpdateGameStatistics();
        }
        
        /// <summary>
        /// 삭제 메시지 표시
        /// </summary>
        private void ShowDeletionMessage()
        {
            // 추후 UI 시스템에서 구현
            // 예: "캐릭터가 영구적으로 삭제되었습니다. 새로운 캐릭터를 생성하세요."
            Debug.Log("UI: Character permanently deleted. Create a new character.");
        }
        
        /// <summary>
        /// 게임 통계 업데이트
        /// </summary>
        private void UpdateGameStatistics()
        {
            // 총 사망 횟수 증가
            int totalDeaths = PlayerPrefs.GetInt("TotalDeaths", 0);
            PlayerPrefs.SetInt("TotalDeaths", totalDeaths + 1);
            
            // 최고 레벨 기록 (이번 캐릭터가 기록을 갱신했는지 확인)
            if (statsManager?.CurrentStats != null)
            {
                int currentLevel = statsManager.CurrentStats.CurrentLevel;
                int bestLevel = PlayerPrefs.GetInt("BestLevel", 0);
                
                if (currentLevel > bestLevel)
                {
                    PlayerPrefs.SetInt("BestLevel", currentLevel);
                    PlayerPrefs.SetString("BestCharacterName", statsManager.CurrentStats.CharacterName);
                    Debug.Log($"🏆 New level record: {currentLevel}");
                }
            }
            
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// 강제 캐릭터 삭제 (디버그용)
        /// </summary>
        [ContextMenu("Force Delete Character")]
        public void ForceDeleteCharacter()
        {
            if (Application.isPlaying && IsServer)
            {
                DeleteCharacter();
            }
        }
        
        /// <summary>
        /// 삭제 기능 활성화/비활성화
        /// </summary>
        public void SetDeletionEnabled(bool enabled)
        {
            enableDeletion = enabled;
            Debug.Log($"Character deletion {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// 캐릭터 복구 불가능 여부 확인
        /// </summary>
        public bool IsRestorable => false; // 하드코어 게임이므로 항상 false
    }
}