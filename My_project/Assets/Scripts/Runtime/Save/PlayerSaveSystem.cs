using UnityEngine;
using Unity.Netcode;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 플레이어 캐릭터 데이터 저장/로드 시스템
    /// 하드코어 모드: 살아있는 동안만 저장, 사망 시 완전 삭제
    /// </summary>
    public class PlayerSaveSystem : NetworkBehaviour
    {
        public static PlayerSaveSystem Instance { get; private set; }

        [Header("저장 설정")]
        [SerializeField] private float autoSaveInterval = 120f; // 2분마다
        [SerializeField] private bool enableAutoSave = true;

        private string savePath;
        private float lastSaveTime;

        // 이벤트
        public static event Action<bool> OnCharacterSaved;
        public static event Action<CharacterSaveData> OnCharacterLoaded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            savePath = Path.Combine(Application.persistentDataPath, "DungeonCrawlerSaves", "Characters");
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
        }

        private void Update()
        {
            if (!IsServer || !enableAutoSave) return;

            if (Time.time > lastSaveTime + autoSaveInterval)
            {
                AutoSaveAllPlayers();
                lastSaveTime = Time.time;
            }
        }

        /// <summary>
        /// 플레이어 캐릭터 데이터 저장
        /// </summary>
        public void SaveCharacter(ulong clientId)
        {
            if (!IsServer) return;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return;
            if (client.PlayerObject == null) return;

            var playerObj = client.PlayerObject;
            var saveData = CollectCharacterData(playerObj);
            if (saveData == null) return;

            try
            {
                string filePath = GetCharacterFilePath(clientId);
                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                };

                string json = JsonConvert.SerializeObject(saveData, settings);
                File.WriteAllText(filePath, json);

                OnCharacterSaved?.Invoke(true);
                Debug.Log($"[Save] Character saved for client {clientId}: Lv.{saveData.level} {saveData.characterName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Failed to save character for client {clientId}: {e.Message}");
                OnCharacterSaved?.Invoke(false);
            }
        }

        /// <summary>
        /// 플레이어 캐릭터 데이터 로드
        /// </summary>
        public CharacterSaveData LoadCharacter(ulong clientId)
        {
            string filePath = GetCharacterFilePath(clientId);
            if (!File.Exists(filePath))
            {
                Debug.Log($"[Save] No save file found for client {clientId}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                };

                var saveData = JsonConvert.DeserializeObject<CharacterSaveData>(json, settings);
                if (saveData != null && ValidateSaveData(saveData))
                {
                    OnCharacterLoaded?.Invoke(saveData);
                    Debug.Log($"[Save] Character loaded for client {clientId}: Lv.{saveData.level} {saveData.characterName}");
                    return saveData;
                }

                Debug.LogWarning($"[Save] Invalid save data for client {clientId}");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Failed to load character for client {clientId}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 캐릭터 세이브 삭제 (사망 시)
        /// </summary>
        public void DeleteCharacterSave(ulong clientId)
        {
            string filePath = GetCharacterFilePath(clientId);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"[Save] Character save deleted for client {clientId} (death)");
            }
        }

        /// <summary>
        /// 세이브 파일 존재 여부
        /// </summary>
        public bool HasCharacterSave(ulong clientId)
        {
            return File.Exists(GetCharacterFilePath(clientId));
        }

        /// <summary>
        /// 캐릭터 데이터 수집
        /// </summary>
        private CharacterSaveData CollectCharacterData(NetworkObject playerObj)
        {
            var statsManager = playerObj.GetComponent<PlayerStatsManager>();
            if (statsManager == null || statsManager.CurrentStats == null) return null;

            var stats = statsManager.CurrentStats;
            var saveData = new CharacterSaveData
            {
                saveVersion = 1,
                saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),

                // 기본 정보
                characterName = stats.CharacterName,
                race = stats.CharacterRace,
                jobType = stats.CurrentJobType,
                level = stats.CurrentLevel,
                currentExp = stats.CurrentExperience,

                // HP/MP
                currentHP = stats.CurrentHP,
                maxHP = stats.MaxHP,
                currentMP = stats.CurrentMP,
                maxMP = stats.MaxMP,

                // 재화
                gold = stats.Gold,

                // 위치
                positionX = playerObj.transform.position.x,
                positionY = playerObj.transform.position.y,
                positionZ = playerObj.transform.position.z
            };

            // 인벤토리
            var inventoryManager = playerObj.GetComponent<InventoryManager>();
            if (inventoryManager != null)
            {
                saveData.inventoryItems = CollectInventoryData(inventoryManager);
            }

            // 장비
            var equipmentManager = playerObj.GetComponent<EquipmentManager>();
            if (equipmentManager != null)
            {
                saveData.equippedItems = CollectEquipmentData(equipmentManager);
            }

            // 스킬
            var skillManager = playerObj.GetComponent<SkillManager>();
            if (skillManager != null)
            {
                var learnedSkills = skillManager.GetLearnedSkills();
                saveData.learnedSkillIds = learnedSkills != null ? learnedSkills.ToArray() : new string[0];
            }

            // 무기 숙련도
            var profSystem = playerObj.GetComponent<WeaponProficiencySystem>();
            if (profSystem != null)
            {
                saveData.weaponProficiencies = CollectProficiencyData(profSystem);
            }

            // 퀘스트
            if (QuestManager.Instance != null)
            {
                saveData.questProgress = CollectQuestData();
            }

            return saveData;
        }

        /// <summary>
        /// 인벤토리 데이터 수집
        /// </summary>
        private List<ItemSaveEntry> CollectInventoryData(InventoryManager inventoryManager)
        {
            var items = new List<ItemSaveEntry>();

            for (int i = 0; i < inventoryManager.MaxSlots; i++)
            {
                var item = inventoryManager.GetItemAtSlot(i);
                if (item != null && item.IsValid())
                {
                    items.Add(new ItemSaveEntry
                    {
                        slotIndex = i,
                        itemId = item.ItemId,
                        quantity = item.Quantity,
                        enhanceLevel = item.EnhanceLevel,
                        instanceId = item.InstanceId
                    });
                }
            }

            return items;
        }

        /// <summary>
        /// 장비 데이터 수집
        /// </summary>
        private List<EquipmentSaveEntry> CollectEquipmentData(EquipmentManager equipmentManager)
        {
            var entries = new List<EquipmentSaveEntry>();

            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                if (slot == EquipmentSlot.None) continue;

                var item = equipmentManager.GetEquippedItem(slot);
                if (item != null && item.IsValid())
                {
                    entries.Add(new EquipmentSaveEntry
                    {
                        slot = slot,
                        itemId = item.ItemId,
                        enhanceLevel = item.EnhanceLevel,
                        instanceId = item.InstanceId
                    });
                }
            }

            return entries;
        }

        /// <summary>
        /// 무기 숙련도 데이터 수집
        /// </summary>
        private List<ProficiencySaveEntry> CollectProficiencyData(WeaponProficiencySystem profSystem)
        {
            var entries = new List<ProficiencySaveEntry>();

            foreach (WeaponType weaponType in Enum.GetValues(typeof(WeaponType)))
            {
                float prof = profSystem.GetProficiency(weaponType);
                if (prof > 0f)
                {
                    entries.Add(new ProficiencySaveEntry
                    {
                        weaponType = weaponType,
                        proficiency = prof
                    });
                }
            }

            return entries;
        }

        /// <summary>
        /// 퀘스트 데이터 수집
        /// </summary>
        private List<QuestSaveEntry> CollectQuestData()
        {
            var entries = new List<QuestSaveEntry>();
            if (QuestManager.Instance == null) return entries;
            var activeQuests = QuestManager.Instance.GetActiveQuests();

            if (activeQuests != null)
            {
                foreach (var quest in activeQuests)
                {
                    entries.Add(new QuestSaveEntry
                    {
                        questId = quest.questId,
                        status = quest.status,
                        currentCounts = quest.currentCounts,
                        acceptedTime = quest.acceptedTime,
                        completedTime = quest.completedTime
                    });
                }
            }

            return entries;
        }

        /// <summary>
        /// 캐릭터 데이터 적용 (로드)
        /// </summary>
        public void ApplyCharacterData(ulong clientId, CharacterSaveData saveData)
        {
            if (!IsServer || saveData == null) return;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return;
            if (client.PlayerObject == null) return;

            var playerObj = client.PlayerObject;

            // 스탯 복원
            var statsManager = playerObj.GetComponent<PlayerStatsManager>();
            if (statsManager != null && statsManager.CurrentStats != null)
            {
                var stats = statsManager.CurrentStats;

                // 종족 & 직업 데이터 로드
                var raceData = LoadRaceData(saveData.race);
                if (raceData != null)
                {
                    stats.SetRace(saveData.race, raceData);
                }

                var jobData = LoadJobData(saveData.jobType);
                if (jobData != null)
                {
                    stats.SetJobData(saveData.jobType, jobData);
                }

                stats.SetCharacterName(saveData.characterName);

                // 경험치 복원 (레벨업은 AddExperience 내부에서 처리)
                if (saveData.level > 1)
                {
                    // 레벨에 맞는 경험치를 누적 추가
                    long totalExpNeeded = 0;
                    for (int lv = 2; lv <= saveData.level; lv++)
                    {
                        totalExpNeeded += (long)(100 * Mathf.Pow(lv, 1.5f));
                    }
                    stats.AddExperience(totalExpNeeded + saveData.currentExp);
                }

                // HP/MP 복원
                stats.SetCurrentHP(saveData.currentHP);
                stats.SetCurrentMP(saveData.currentMP);

                // 골드 복원
                stats.ChangeGold(saveData.gold - stats.Gold);
            }

            // 위치 복원
            playerObj.transform.position = new Vector3(saveData.positionX, saveData.positionY, saveData.positionZ);

            // 인벤토리 복원
            ApplyInventoryData(playerObj, saveData.inventoryItems);

            // 장비 복원
            ApplyEquipmentData(playerObj, saveData.equippedItems);

            // 스킬 복원
            ApplySkillData(playerObj, saveData.learnedSkillIds);

            // 무기 숙련도 복원
            ApplyProficiencyData(playerObj, saveData.weaponProficiencies);

            // 퀘스트 복원
            ApplyQuestData(clientId, saveData.questProgress);

            Debug.Log($"[Save] Character data applied for client {clientId}: Lv.{saveData.level} {saveData.characterName}");
        }

        /// <summary>
        /// 인벤토리 데이터 적용
        /// </summary>
        private void ApplyInventoryData(NetworkObject playerObj, List<ItemSaveEntry> items)
        {
            if (items == null || items.Count == 0) return;

            var inventoryManager = playerObj.GetComponent<InventoryManager>();
            if (inventoryManager == null) return;

            foreach (var entry in items)
            {
                var itemInstance = new ItemInstance();
                itemInstance.Initialize(entry.itemId, entry.quantity);
                itemInstance.EnhanceLevel = entry.enhanceLevel;
                inventoryManager.TryAddItemDirect(itemInstance, out _);
            }
        }

        /// <summary>
        /// 장비 데이터 적용
        /// </summary>
        private void ApplyEquipmentData(NetworkObject playerObj, List<EquipmentSaveEntry> equipment)
        {
            if (equipment == null || equipment.Count == 0) return;

            var equipmentManager = playerObj.GetComponent<EquipmentManager>();
            if (equipmentManager == null) return;

            foreach (var entry in equipment)
            {
                var itemInstance = new ItemInstance();
                itemInstance.Initialize(entry.itemId, 1);
                itemInstance.EnhanceLevel = entry.enhanceLevel;
                equipmentManager.TryEquipItem(itemInstance, false);
            }
        }

        /// <summary>
        /// 스킬 데이터 적용
        /// </summary>
        private void ApplySkillData(NetworkObject playerObj, string[] skillIds)
        {
            if (skillIds == null || skillIds.Length == 0) return;

            var skillManager = playerObj.GetComponent<SkillManager>();
            if (skillManager == null) return;

            foreach (string skillId in skillIds)
            {
                if (!string.IsNullOrEmpty(skillId))
                {
                    skillManager.LearnSkill(skillId);
                }
            }
        }

        /// <summary>
        /// 무기 숙련도 적용
        /// </summary>
        private void ApplyProficiencyData(NetworkObject playerObj, List<ProficiencySaveEntry> proficiencies)
        {
            if (proficiencies == null || proficiencies.Count == 0) return;

            var profSystem = playerObj.GetComponent<WeaponProficiencySystem>();
            if (profSystem == null) return;

            foreach (var entry in proficiencies)
            {
                profSystem.SetProficiency(entry.weaponType, entry.proficiency);
            }
        }

        /// <summary>
        /// 퀘스트 데이터 적용
        /// </summary>
        private void ApplyQuestData(ulong clientId, List<QuestSaveEntry> quests)
        {
            if (quests == null || quests.Count == 0) return;
            if (QuestManager.Instance == null) return;

            foreach (var entry in quests)
            {
                QuestManager.Instance.RestoreQuestProgress(clientId, entry.questId, entry.status, entry.currentCounts, entry.acceptedTime, entry.completedTime);
            }
        }

        /// <summary>
        /// RaceData 로드
        /// </summary>
        private RaceData LoadRaceData(Race race)
        {
            string raceName = race.ToString();
            var raceData = Resources.Load<RaceData>($"ScriptableObjects/PlayerData/PlayerRaceData/{raceName}_RaceData");
            if (raceData == null)
            {
                raceData = Resources.Load<RaceData>($"ScriptableObjects/PlayerData/PlayerRaceData/{raceName}RaceData");
            }
            return raceData;
        }

        /// <summary>
        /// JobData 로드
        /// </summary>
        private JobData LoadJobData(JobType jobType)
        {
            string jobName = jobType.ToString();
            return Resources.Load<JobData>($"Jobs/{jobName}");
        }

        /// <summary>
        /// 모든 플레이어 자동 저장
        /// </summary>
        private void AutoSaveAllPlayers()
        {
            if (!IsServer) return;

            foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            {
                if (kvp.Value.PlayerObject != null)
                {
                    SaveCharacter(kvp.Key);
                }
            }
        }

        /// <summary>
        /// 세이브 데이터 검증
        /// </summary>
        private bool ValidateSaveData(CharacterSaveData data)
        {
            if (data == null) return false;
            if (string.IsNullOrEmpty(data.characterName)) return false;
            if (data.level < 1 || data.level > 15) return false;
            return true;
        }

        /// <summary>
        /// 세이브 파일 경로
        /// </summary>
        private string GetCharacterFilePath(ulong clientId)
        {
            return Path.Combine(savePath, $"character_{clientId}.json");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this)
                Instance = null;
        }
    }

    /// <summary>
    /// 캐릭터 저장 데이터
    /// </summary>
    [System.Serializable]
    public class CharacterSaveData
    {
        public int saveVersion;
        public string saveTime;

        // 기본 정보
        public string characterName;
        public Race race;
        public JobType jobType;
        public int level;
        public long currentExp;

        // HP/MP
        public float currentHP;
        public float maxHP;
        public float currentMP;
        public float maxMP;

        // 재화
        public long gold;

        // 위치
        public float positionX;
        public float positionY;
        public float positionZ;

        // 인벤토리
        public List<ItemSaveEntry> inventoryItems;

        // 장비
        public List<EquipmentSaveEntry> equippedItems;

        // 스킬
        public string[] learnedSkillIds;

        // 무기 숙련도
        public List<ProficiencySaveEntry> weaponProficiencies;

        // 퀘스트
        public List<QuestSaveEntry> questProgress;
    }

    /// <summary>
    /// 아이템 저장 엔트리
    /// </summary>
    [System.Serializable]
    public class ItemSaveEntry
    {
        public int slotIndex;
        public string itemId;
        public int quantity;
        public int enhanceLevel;
        public string instanceId;
    }

    /// <summary>
    /// 장비 저장 엔트리
    /// </summary>
    [System.Serializable]
    public class EquipmentSaveEntry
    {
        public EquipmentSlot slot;
        public string itemId;
        public int enhanceLevel;
        public string instanceId;
    }

    /// <summary>
    /// 무기 숙련도 저장 엔트리
    /// </summary>
    [System.Serializable]
    public class ProficiencySaveEntry
    {
        public WeaponType weaponType;
        public float proficiency;
    }

    /// <summary>
    /// 퀘스트 저장 엔트리
    /// </summary>
    [System.Serializable]
    public class QuestSaveEntry
    {
        public string questId;
        public QuestStatus status;
        public int[] currentCounts;
        public long acceptedTime;
        public long completedTime;
    }
}
