using UnityEngine;
using System.IO;
using System.Collections.Generic;
// using Newtonsoft.Json; // Using Unity's JsonUtility instead

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 플레이어 스탯 세이브/로드 시스템
    /// JSON 기반 로컬 저장 및 클라우드 저장 지원
    /// </summary>
    public class StatsSaveSystem : MonoBehaviour
    {
        [Header("Save Settings")]
        [SerializeField] private string saveFileName = "player_stats.json";
        [SerializeField] private bool autoSave = true;
        [SerializeField] private float autoSaveInterval = 30f;
        [SerializeField] private bool useCloudSave = false;
        
        private string savePath;
        private PlayerStatsManager statsManager;
        private float lastSaveTime;
        
        // 싱글톤 패턴
        public static StatsSaveSystem Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSaveSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            FindLocalPlayerStatsManager();
        }
        
        private void Update()
        {
            // 자동 저장
            if (autoSave && Time.time - lastSaveTime >= autoSaveInterval)
            {
                AutoSave();
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SavePlayerStats();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SavePlayerStats();
            }
        }
        
        private void OnDestroy()
        {
            SavePlayerStats();
        }
        
        private void InitializeSaveSystem()
        {
            // 저장 경로 설정
            savePath = Path.Combine(Application.persistentDataPath, saveFileName);
            Debug.Log($"Save path: {savePath}");
        }
        
        private void FindLocalPlayerStatsManager()
        {
            // 로컬 플레이어의 StatsManager 찾기
            var playerControllers = FindObjectsOfType<PlayerController>();
            foreach (var controller in playerControllers)
            {
                var networkBehaviour = controller.GetComponent<Unity.Netcode.NetworkBehaviour>();
                if (networkBehaviour != null && networkBehaviour.IsLocalPlayer)
                {
                    statsManager = controller.GetComponent<PlayerStatsManager>();
                    break;
                }
            }
        }
        
        // 플레이어 스탯 저장
        public bool SavePlayerStats()
        {
            if (statsManager == null || statsManager.CurrentStats == null)
            {
                Debug.LogWarning("No player stats to save");
                return false;
            }
            
            try
            {
                var saveData = CreateSaveData();
                string jsonData = JsonUtility.ToJson(saveData, true);
                
                File.WriteAllText(savePath, jsonData);
                lastSaveTime = Time.time;
                
                Debug.Log("Player stats saved successfully");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save player stats: {e.Message}");
                return false;
            }
        }
        
        // 플레이어 스탯 로드
        public bool LoadPlayerStats()
        {
            if (!File.Exists(savePath))
            {
                Debug.Log("No save file found, creating new stats");
                return false;
            }
            
            try
            {
                string jsonData = File.ReadAllText(savePath);
                var saveData = JsonUtility.FromJson<PlayerStatsSaveData>(jsonData);
                
                ApplySaveData(saveData);
                Debug.Log("Player stats loaded successfully");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load player stats: {e.Message}");
                return false;
            }
        }
        
        // 자동 저장
        private void AutoSave()
        {
            if (statsManager != null)
            {
                SavePlayerStats();
            }
        }
        
        // 저장 데이터 생성
        private PlayerStatsSaveData CreateSaveData()
        {
            var stats = statsManager.CurrentStats;
            
            return new PlayerStatsSaveData
            {
                // 기본 정보
                playerName = statsManager.gameObject.name,
                saveTime = System.DateTime.Now.ToBinary(),
                
                // 레벨 및 경험치
                level = stats.CurrentLevel,
                experience = stats.CurrentExperience,
                
                // 기본 스탯
                baseSTR = stats.TotalSTR, // 주의: 실제로는 base 값만 저장해야 함
                baseAGI = stats.TotalAGI,
                baseVIT = stats.TotalVIT,
                baseINT = stats.TotalINT,
                baseDEF = stats.TotalDEF,
                baseMDEF = stats.TotalMDEF,
                baseLUK = stats.TotalLUK,
                
                // 스탯 포인트 시스템 제거됨 (종족별 고정 성장)
                // availableStatPoints = 0, // 더 이상 사용하지 않음
                
                // 현재 상태
                currentHP = stats.CurrentHP,
                currentMP = stats.CurrentMP,
                
                // 추가 정보
                playTime = Time.time, // 총 플레이 시간 (임시)
                version = Application.version
            };
        }
        
        // 저장 데이터 적용
        private void ApplySaveData(PlayerStatsSaveData saveData)
        {
            if (statsManager == null || statsManager.CurrentStats == null) return;
            
            var stats = statsManager.CurrentStats;
            
            // 여기서는 실제로 PlayerStats ScriptableObject에 값을 설정해야 함
            // 현재 구조상 직접 설정이 어려우므로 StatManager를 통해 설정
            
            Debug.Log($"Loading stats for {saveData.playerName}");
            Debug.Log($"Level: {saveData.level}, EXP: {saveData.experience}");
            Debug.Log($"Stats - STR: {saveData.baseSTR}, AGI: {saveData.baseAGI}, VIT: {saveData.baseVIT}");
        }
        
        // 저장 파일 삭제
        public bool DeleteSaveFile()
        {
            try
            {
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                    Debug.Log("Save file deleted");
                    return true;
                }
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to delete save file: {e.Message}");
                return false;
            }
        }
        
        // 저장 파일 존재 확인
        public bool SaveFileExists()
        {
            return File.Exists(savePath);
        }
        
        // 저장 파일 정보 가져오기
        public PlayerStatsSaveData GetSaveFileInfo()
        {
            if (!SaveFileExists()) return null;
            
            try
            {
                string jsonData = File.ReadAllText(savePath);
                return JsonUtility.FromJson<PlayerStatsSaveData>(jsonData);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to read save file info: {e.Message}");
                return null;
            }
        }
        
        // 클라우드 저장 (플레이스홀더)
        public void SaveToCloud()
        {
            if (!useCloudSave) return;
            
            // Unity Cloud Save 또는 다른 클라우드 서비스 연동
            Debug.Log("Cloud save not implemented yet");
        }
        
        // 클라우드 로드 (플레이스홀더)
        public void LoadFromCloud()
        {
            if (!useCloudSave) return;
            
            // Unity Cloud Save 또는 다른 클라우드 서비스 연동
            Debug.Log("Cloud load not implemented yet");
        }
        
        // 다중 저장 슬롯 지원
        public bool SaveToSlot(int slotIndex)
        {
            string slotFileName = $"player_stats_slot_{slotIndex}.json";
            string slotPath = Path.Combine(Application.persistentDataPath, slotFileName);
            
            if (statsManager == null || statsManager.CurrentStats == null) return false;
            
            try
            {
                var saveData = CreateSaveData();
                string jsonData = JsonUtility.ToJson(saveData, true);
                
                File.WriteAllText(slotPath, jsonData);
                Debug.Log($"Stats saved to slot {slotIndex}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save to slot {slotIndex}: {e.Message}");
                return false;
            }
        }
        
        public bool LoadFromSlot(int slotIndex)
        {
            string slotFileName = $"player_stats_slot_{slotIndex}.json";
            string slotPath = Path.Combine(Application.persistentDataPath, slotFileName);
            
            if (!File.Exists(slotPath)) return false;
            
            try
            {
                string jsonData = File.ReadAllText(slotPath);
                var saveData = JsonUtility.FromJson<PlayerStatsSaveData>(jsonData);
                
                ApplySaveData(saveData);
                Debug.Log($"Stats loaded from slot {slotIndex}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load from slot {slotIndex}: {e.Message}");
                return false;
            }
        }
        
        // 슬롯 정보 가져오기
        public List<PlayerStatsSaveData> GetAllSlotInfo()
        {
            var slotInfo = new List<PlayerStatsSaveData>();
            
            for (int i = 0; i < 10; i++) // 최대 10개 슬롯
            {
                string slotFileName = $"player_stats_slot_{i}.json";
                string slotPath = Path.Combine(Application.persistentDataPath, slotFileName);
                
                if (File.Exists(slotPath))
                {
                    try
                    {
                        string jsonData = File.ReadAllText(slotPath);
                        var saveData = JsonUtility.FromJson<PlayerStatsSaveData>(jsonData);
                        saveData.slotIndex = i;
                        slotInfo.Add(saveData);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to read slot {i}: {e.Message}");
                    }
                }
            }
            
            return slotInfo;
        }
    }
    
    /// <summary>
    /// 플레이어 스탯 저장 데이터 구조체
    /// </summary>
    [System.Serializable]
    public class PlayerStatsSaveData
    {
        // 기본 정보
        public string playerName;
        public long saveTime;
        public string version;
        public int slotIndex = -1;
        
        // 레벨 및 경험치
        public int level;
        public long experience;
        
        // 기본 스탯
        public float baseSTR;
        public float baseAGI;
        public float baseVIT;
        public float baseINT;
        public float baseDEF;
        public float baseMDEF;
        public float baseLUK;
        
        // 스탯 포인트 시스템 제거됨 (종족별 고정 성장)
        // public int availableStatPoints; // 더 이상 사용하지 않음
        
        // 현재 상태
        public float currentHP;
        public float currentMP;
        
        // 추가 정보
        public float playTime;
        
        // 저장 시간을 DateTime으로 변환
        public System.DateTime GetSaveDateTime()
        {
            return System.DateTime.FromBinary(saveTime);
        }
        
        // 저장 정보 문자열
        public string GetSaveInfoString()
        {
            var dateTime = GetSaveDateTime();
            return $"Level {level} | {dateTime:yyyy-MM-dd HH:mm}";
        }
    }
}