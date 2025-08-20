using UnityEngine;
using Unity.Netcode;
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 하드코어 던전 크롤러 세이브 시스템
    /// 영혼 컬렉션만 영구 저장, 캐릭터 데이터는 사망 시 완전 삭제
    /// </summary>
    public class SaveSystem : NetworkBehaviour
    {
        [Header("저장 설정")]
        [SerializeField] private bool enableAutoSave = true;
        [SerializeField] private float autoSaveInterval = 300f; // 5분마다 자동 저장
        [SerializeField] private string saveDirectory = "DungeonCrawlerSaves";
        [SerializeField] private bool enableCloudSync = false;
        
        [Header("백업 설정")]
        [SerializeField] private bool enableBackup = true;
        [SerializeField] private int maxBackupFiles = 3;
        
        // 저장 경로
        private string savePath;
        private string backupPath;
        
        // 자동 저장
        private float lastSaveTime = 0f;
        
        // 이벤트
        public static System.Action<bool> OnSaveCompleted; // success
        public static System.Action<AccountData> OnAccountLoaded;
        public static System.Action OnAccountReset;
        
        // 싱글톤 패턴
        private static SaveSystem instance;
        public static SaveSystem Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<SaveSystem>();
                }
                return instance;
            }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSaveSystem();
            }
        }
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Update()
        {
            // 자동 저장 처리
            if (enableAutoSave && Time.time > lastSaveTime + autoSaveInterval)
            {
                AutoSaveAccountData();
            }
        }
        
        /// <summary>
        /// 저장 시스템 초기화
        /// </summary>
        private void InitializeSaveSystem()
        {
            // 저장 경로 설정
            savePath = Path.Combine(Application.persistentDataPath, saveDirectory);
            backupPath = Path.Combine(savePath, "Backup");
            
            // 디렉토리 생성
            CreateSaveDirectories();
            
            Debug.Log($"💾 Save System initialized. Save path: {savePath}");
        }
        
        /// <summary>
        /// 저장 디렉토리 생성
        /// </summary>
        private void CreateSaveDirectories()
        {
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
            
            if (enableBackup && !Directory.Exists(backupPath))
            {
                Directory.CreateDirectory(backupPath);
            }
        }
        
        /// <summary>
        /// 계정 데이터 저장
        /// </summary>
        public void SaveAccountData(AccountData accountData)
        {
            try
            {
                string accountFilePath = Path.Combine(savePath, "account.json");
                
                // 백업 생성
                if (enableBackup && File.Exists(accountFilePath))
                {
                    CreateBackup(accountFilePath);
                }
                
                // JSON 직렬화
                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                
                string jsonData = JsonConvert.SerializeObject(accountData, settings);
                
                // 파일 저장
                File.WriteAllText(accountFilePath, jsonData);
                
                // 검증
                if (ValidateSaveFile(accountFilePath))
                {
                    lastSaveTime = Time.time;
                    OnSaveCompleted?.Invoke(true);
                    
                    Debug.Log($"✅ Account data saved successfully. Souls: {accountData.soulCollection.Count}");
                }
                else
                {
                    Debug.LogError("❌ Save file validation failed");
                    OnSaveCompleted?.Invoke(false);
                }
                
                // 클라우드 동기화 (옵션)
                if (enableCloudSync)
                {
                    SyncToCloud(accountData);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to save account data: {e.Message}");
                OnSaveCompleted?.Invoke(false);
            }
        }
        
        /// <summary>
        /// 계정 데이터 로드
        /// </summary>
        public AccountData LoadAccountData()
        {
            try
            {
                string accountFilePath = Path.Combine(savePath, "account.json");
                
                if (!File.Exists(accountFilePath))
                {
                    Debug.Log("💾 No existing account data found. Creating new account.");
                    return CreateNewAccount();
                }
                
                // 파일 검증
                if (!ValidateSaveFile(accountFilePath))
                {
                    Debug.LogWarning("⚠️ Save file corrupted. Attempting to restore from backup.");
                    return RestoreFromBackup() ?? CreateNewAccount();
                }
                
                // JSON 역직렬화
                string jsonData = File.ReadAllText(accountFilePath);
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                
                AccountData accountData = JsonConvert.DeserializeObject<AccountData>(jsonData, settings);
                
                // 데이터 검증
                if (ValidateAccountData(accountData))
                {
                    OnAccountLoaded?.Invoke(accountData);
                    Debug.Log($"✅ Account data loaded. Souls: {accountData.soulCollection.Count}, Created: {accountData.accountCreationDate}");
                    return accountData;
                }
                else
                {
                    Debug.LogError("❌ Loaded account data is invalid");
                    return CreateNewAccount();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to load account data: {e.Message}");
                return CreateNewAccount();
            }
        }
        
        /// <summary>
        /// 새 계정 생성
        /// </summary>
        private AccountData CreateNewAccount()
        {
            var newAccount = new AccountData
            {
                accountId = Guid.NewGuid().ToString(),
                accountCreationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                lastPlayDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                totalPlayTime = 0f,
                soulCollection = new List<SoulData>(),
                statistics = new AccountStatistics(),
                settings = new AccountSettings(),
                version = Application.version
            };
            
            // 초기 저장
            SaveAccountData(newAccount);
            
            Debug.Log($"🆕 New account created: {newAccount.accountId}");
            return newAccount;
        }
        
        /// <summary>
        /// 영혼 데이터 저장 (사망 시 호출)
        /// </summary>
        public void SaveSoulData(SoulData soulData)
        {
            var accountData = LoadAccountData();
            
            // 영혼 추가
            accountData.soulCollection.Add(soulData);
            
            // 통계 업데이트
            accountData.statistics.totalCharactersCreated++;
            accountData.statistics.totalDeaths++;
            
            // 저장
            SaveAccountData(accountData);
            
            Debug.Log($"👻 Soul saved: {soulData.soulName} (Level {soulData.characterLevel})");
        }
        
        /// <summary>
        /// 캐릭터 사망 처리 (완전 삭제)
        /// </summary>
        public void ProcessCharacterDeath(ulong playerId)
        {
            // 하드코어 게임: 캐릭터 데이터는 저장하지 않고 완전히 삭제
            // 오직 영혼만 보존됨
            
            var accountData = LoadAccountData();
            accountData.statistics.totalDeaths++;
            accountData.lastPlayDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            SaveAccountData(accountData);
            
            Debug.Log($"💀 Character permanently deleted for player {playerId}");
        }
        
        /// <summary>
        /// 자동 저장
        /// </summary>
        private void AutoSaveAccountData()
        {
            if (IsServer) // 서버에서만 자동 저장
            {
                var accountData = LoadAccountData();
                
                // 플레이 시간 업데이트
                accountData.totalPlayTime += autoSaveInterval;
                accountData.lastPlayDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                
                SaveAccountData(accountData);
                
                Debug.Log($"💾 Auto-save completed. Play time: {accountData.totalPlayTime / 3600f:F1}h");
            }
        }
        
        /// <summary>
        /// 백업 생성
        /// </summary>
        private void CreateBackup(string sourceFile)
        {
            try
            {
                if (!enableBackup) return;
                
                string fileName = Path.GetFileName(sourceFile);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{timestamp}.json";
                string backupFilePath = Path.Combine(backupPath, backupFileName);
                
                File.Copy(sourceFile, backupFilePath);
                
                // 오래된 백업 정리
                CleanupOldBackups();
                
                Debug.Log($"💾 Backup created: {backupFileName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to create backup: {e.Message}");
            }
        }
        
        /// <summary>
        /// 백업에서 복원
        /// </summary>
        private AccountData RestoreFromBackup()
        {
            try
            {
                if (!enableBackup || !Directory.Exists(backupPath))
                {
                    return null;
                }
                
                var backupFiles = Directory.GetFiles(backupPath, "account_*.json");
                if (backupFiles.Length == 0)
                {
                    return null;
                }
                
                // 가장 최근 백업 파일 찾기
                Array.Sort(backupFiles);
                string latestBackup = backupFiles[backupFiles.Length - 1];
                
                // 백업 파일 검증 및 로드
                if (ValidateSaveFile(latestBackup))
                {
                    string jsonData = File.ReadAllText(latestBackup);
                    var accountData = JsonConvert.DeserializeObject<AccountData>(jsonData);
                    
                    if (ValidateAccountData(accountData))
                    {
                        // 복원된 데이터를 메인 파일로 저장
                        string mainFilePath = Path.Combine(savePath, "account.json");
                        File.Copy(latestBackup, mainFilePath, true);
                        
                        Debug.Log($"🔄 Account data restored from backup: {Path.GetFileName(latestBackup)}");
                        return accountData;
                    }
                }
                
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to restore from backup: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 오래된 백업 파일 정리
        /// </summary>
        private void CleanupOldBackups()
        {
            try
            {
                var backupFiles = Directory.GetFiles(backupPath, "account_*.json");
                
                if (backupFiles.Length > maxBackupFiles)
                {
                    Array.Sort(backupFiles);
                    
                    int filesToDelete = backupFiles.Length - maxBackupFiles;
                    for (int i = 0; i < filesToDelete; i++)
                    {
                        File.Delete(backupFiles[i]);
                        Debug.Log($"🗑️ Deleted old backup: {Path.GetFileName(backupFiles[i])}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to cleanup old backups: {e.Message}");
            }
        }
        
        /// <summary>
        /// 저장 파일 검증
        /// </summary>
        private bool ValidateSaveFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return false;
                }
                
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    return false;
                }
                
                // JSON 형식 검증
                string jsonData = File.ReadAllText(filePath);
                JsonConvert.DeserializeObject(jsonData);
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 계정 데이터 검증
        /// </summary>
        private bool ValidateAccountData(AccountData accountData)
        {
            if (accountData == null) return false;
            if (string.IsNullOrEmpty(accountData.accountId)) return false;
            if (string.IsNullOrEmpty(accountData.accountCreationDate)) return false;
            if (accountData.soulCollection == null) return false;
            if (accountData.statistics == null) return false;
            if (accountData.settings == null) return false;
            
            return true;
        }
        
        /// <summary>
        /// 클라우드 동기화 (플레이스홀더)
        /// </summary>
        private void SyncToCloud(AccountData accountData)
        {
            // 실제 구현에서는 Steam Cloud, Google Play Games 등과 연동
            Debug.Log($"☁️ Cloud sync: {accountData.accountId}");
        }
        
        /// <summary>
        /// 계정 초기화 (주의: 모든 데이터 삭제)
        /// </summary>
        public void ResetAccount()
        {
            try
            {
                string accountFilePath = Path.Combine(savePath, "account.json");
                
                if (File.Exists(accountFilePath))
                {
                    // 마지막 백업 생성
                    CreateBackup(accountFilePath);
                    
                    // 계정 파일 삭제
                    File.Delete(accountFilePath);
                }
                
                OnAccountReset?.Invoke();
                
                Debug.Log("🗑️ Account reset completed. All data deleted.");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to reset account: {e.Message}");
            }
        }
        
        /// <summary>
        /// 저장 시스템 통계
        /// </summary>
        public SaveSystemStats GetSaveSystemStats()
        {
            try
            {
                var stats = new SaveSystemStats();
                
                string accountFilePath = Path.Combine(savePath, "account.json");
                if (File.Exists(accountFilePath))
                {
                    var fileInfo = new FileInfo(accountFilePath);
                    stats.accountFileSize = fileInfo.Length;
                    stats.lastSaveTime = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                }
                
                if (Directory.Exists(backupPath))
                {
                    stats.backupCount = Directory.GetFiles(backupPath, "*.json").Length;
                }
                
                stats.autoSaveEnabled = enableAutoSave;
                stats.autoSaveInterval = autoSaveInterval;
                stats.backupEnabled = enableBackup;
                
                return stats;
            }
            catch
            {
                return new SaveSystemStats();
            }
        }
        
        /// <summary>
        /// 세이브 파일 경로 가져오기
        /// </summary>
        public string GetSaveFilePath()
        {
            return Path.Combine(savePath, "account.json");
        }
        
        /// <summary>
        /// 디버그 정보
        /// </summary>
        [ContextMenu("Show Save System Debug Info")]
        private void ShowDebugInfo()
        {
            var stats = GetSaveSystemStats();
            
            Debug.Log("=== Save System Debug ===");
            Debug.Log($"Save Path: {savePath}");
            Debug.Log($"Account File Size: {stats.accountFileSize} bytes");
            Debug.Log($"Last Save: {stats.lastSaveTime}");
            Debug.Log($"Backup Count: {stats.backupCount}");
            Debug.Log($"Auto Save: {stats.autoSaveEnabled} ({stats.autoSaveInterval}s)");
            Debug.Log($"Backup Enabled: {stats.backupEnabled}");
        }
    }
    
    /// <summary>
    /// 계정 데이터 구조
    /// </summary>
    [System.Serializable]
    public class AccountData
    {
        public string accountId;
        public string accountCreationDate;
        public string lastPlayDate;
        public float totalPlayTime;
        public List<SoulData> soulCollection;
        public AccountStatistics statistics;
        public AccountSettings settings;
        public string version;
    }
    
    
    /// <summary>
    /// 계정 통계
    /// </summary>
    [System.Serializable]
    public class AccountStatistics
    {
        public int totalCharactersCreated;
        public int totalDeaths;
        public int maxFloorReached;
        public float longestSurvivalTime;
        public int totalMonstersKilled;
        public int totalPvpKills;
        public int bossesKilled;
        public int hiddenFloorsReached;
        public long totalGoldEarned;
        public long totalExpGained;
    }
    
    /// <summary>
    /// 계정 설정
    /// </summary>
    [System.Serializable]
    public class AccountSettings
    {
        public float masterVolume = 1f;
        public float musicVolume = 0.7f;
        public float sfxVolume = 0.8f;
        public bool enableVSync = true;
        public int targetFrameRate = 60;
        public bool showDamageNumbers = true;
        public bool enableScreenShake = true;
        public string preferredLanguage = "Korean";
    }
    
    /// <summary>
    /// 저장 시스템 통계
    /// </summary>
    [System.Serializable]
    public class SaveSystemStats
    {
        public long accountFileSize;
        public string lastSaveTime;
        public int backupCount;
        public bool autoSaveEnabled;
        public float autoSaveInterval;
        public bool backupEnabled;
    }
}