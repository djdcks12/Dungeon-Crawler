using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 영혼 보존 시스템 - 하드코어 게임의 유일한 영구 진행도
    /// 캐릭터 사망 시에도 영혼만은 계정에 보존됨
    /// </summary>
    public class SoulPreservation : NetworkBehaviour
    {
        [Header("Soul Settings")]
        [SerializeField] private int maxSoulSlots = 15; // 레벨당 1개, 최대 15개
        [SerializeField] private bool enableSoulPreservation = true;
        
        // 컴포넌트 참조
        private PlayerStatsManager statsManager;
        
        // 영혼 데이터
        private List<SoulData> currentSouls = new List<SoulData>();
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            statsManager = GetComponent<PlayerStatsManager>();
            
            // 현재 소유한 영혼들 로드
            LoadCurrentSouls();
        }
        
        /// <summary>
        /// 영혼 보존 실행 (사망 시 호출)
        /// </summary>
        public void PreserveSouls()
        {
            if (!enableSoulPreservation)
            {
                Debug.LogWarning("Soul preservation is disabled!");
                return;
            }
            
            Debug.Log($"👻 Preserving souls for {GetCharacterName()}...");
            
            // 현재 장착된 영혼들 저장
            SaveSoulsToAccount();
            
            // 보존 완료 로그
            Debug.Log($"✅ {currentSouls.Count} souls preserved to account");
        }
        
        /// <summary>
        /// 계정에 영혼 저장
        /// </summary>
        private void SaveSoulsToAccount()
        {
            if (currentSouls.Count == 0)
            {
                Debug.Log("No souls to preserve");
                return;
            }
            
            // 계정 ID 가져오기 (추후 계정 시스템과 연동)
            string accountId = GetAccountId();
            
            // 기존에 저장된 영혼들과 합치기
            var existingSouls = LoadSoulsFromAccount(accountId);
            var allSouls = new List<SoulData>(existingSouls);
            
            // 현재 영혼들 추가 (중복 제거)
            foreach (var soul in currentSouls)
            {
                if (!ContainsSoul(allSouls, soul))
                {
                    allSouls.Add(soul);
                    Debug.Log($"👻 Preserved soul: {soul.soulName} (+{soul.statBonus.strength} STR, +{soul.statBonus.agility} AGI, etc.)");
                }
            }
            
            // 최대 슬롯 수 제한
            if (allSouls.Count > maxSoulSlots)
            {
                // 가장 약한 영혼부터 제거 (추후 더 정교한 로직 구현)
                allSouls.Sort((a, b) => GetSoulPower(a).CompareTo(GetSoulPower(b)));
                allSouls.RemoveRange(0, allSouls.Count - maxSoulSlots);
            }
            
            // 계정에 저장
            SaveSoulsData(accountId, allSouls);
            
            Debug.Log($"💾 Total souls in account: {allSouls.Count}/{maxSoulSlots}");
        }
        
        /// <summary>
        /// 계정에서 영혼 로드
        /// </summary>
        private List<SoulData> LoadSoulsFromAccount(string accountId)
        {
            string soulsJson = PlayerPrefs.GetString($"Account_{accountId}_Souls", "");
            
            if (string.IsNullOrEmpty(soulsJson))
            {
                return new List<SoulData>();
            }
            
            try
            {
                var wrapper = JsonUtility.FromJson<SoulDataWrapper>(soulsJson);
                return new List<SoulData>(wrapper.souls);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load souls: {e.Message}");
                return new List<SoulData>();
            }
        }
        
        /// <summary>
        /// 영혼 데이터 저장
        /// </summary>
        private void SaveSoulsData(string accountId, List<SoulData> souls)
        {
            try
            {
                var wrapper = new SoulDataWrapper { souls = souls.ToArray() };
                string soulsJson = JsonUtility.ToJson(wrapper);
                PlayerPrefs.SetString($"Account_{accountId}_Souls", soulsJson);
                PlayerPrefs.Save();
                
                Debug.Log($"💾 Saved {souls.Count} souls to account {accountId}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save souls: {e.Message}");
            }
        }
        
        /// <summary>
        /// 현재 캐릭터의 영혼들 로드
        /// </summary>
        private void LoadCurrentSouls()
        {
            if (statsManager?.CurrentStats == null) return;
            
            // 현재 장착된 영혼 ID들 가져오기
            // TODO: EquippedSoulIds 프로퍼티가 PlayerStats에 구현되지 않음 - 임시로 빈 배열 사용
            var equippedSoulIds = new System.Collections.Generic.List<ulong>();
            
            if (equippedSoulIds == null || equippedSoulIds.Count == 0)
            {
                Debug.Log("No souls equipped on this character");
                return;
            }
            
            // 계정에서 영혼 데이터 로드
            string accountId = GetAccountId();
            var accountSouls = LoadSoulsFromAccount(accountId);
            
            // 장착된 영혼들만 필터링
            currentSouls.Clear();
            foreach (ulong soulId in equippedSoulIds)
            {
                var soul = accountSouls.Find(s => s.soulId == soulId);
                if (soul.soulId != 0) // 기본값이 아닌 경우
                {
                    currentSouls.Add(soul);
                }
            }
            
            Debug.Log($"📖 Loaded {currentSouls.Count} souls for current character");
        }
        
        /// <summary>
        /// 영혼 목록에 특정 영혼이 포함되어 있는지 확인
        /// </summary>
        private bool ContainsSoul(List<SoulData> souls, SoulData targetSoul)
        {
            return souls.Exists(s => s.soulId == targetSoul.soulId);
        }
        
        /// <summary>
        /// 영혼의 총 파워 계산 (정렬용)
        /// </summary>
        private float GetSoulPower(SoulData soul)
        {
            return soul.statBonus.strength + soul.statBonus.agility + soul.statBonus.vitality + 
                   soul.statBonus.intelligence + soul.statBonus.defense + soul.statBonus.magicDefense + 
                   soul.statBonus.luck + soul.statBonus.stability;
        }
        
        /// <summary>
        /// 계정 ID 가져오기 (임시 구현)
        /// </summary>
        private string GetAccountId()
        {
            // 추후 실제 계정 시스템과 연동
            return PlayerPrefs.GetString("AccountId", "DefaultAccount");
        }
        
        /// <summary>
        /// 캐릭터 이름 가져오기
        /// </summary>
        private string GetCharacterName()
        {
            if (statsManager?.CurrentStats != null)
            {
                // TODO: CharacterName 프로퍼티가 PlayerStats에 구현되지 않음 - 임시로 게임 오브젝트 이름 사용
                return statsManager.gameObject.name;
            }
            
            return gameObject.name.Replace("(Clone)", "");
        }
        
        /// <summary>
        /// 새 캐릭터에 영혼 적용 (캐릭터 생성 시 호출)
        /// </summary>
        public void ApplyAccountSoulsToNewCharacter()
        {
            string accountId = GetAccountId();
            var accountSouls = LoadSoulsFromAccount(accountId);
            
            if (accountSouls.Count == 0)
            {
                Debug.Log("No account souls to apply to new character");
                return;
            }
            
            Debug.Log($"🔄 Applying {accountSouls.Count} account souls to new character");
            
            // 영혼들을 새 캐릭터에 적용하는 로직은
            // 추후 캐릭터 생성 시스템에서 구현
        }
        
        /// <summary>
        /// 계정의 영혼 목록 가져오기 (UI용)
        /// </summary>
        public List<SoulData> GetAccountSouls()
        {
            string accountId = GetAccountId();
            return LoadSoulsFromAccount(accountId);
        }
        
        /// <summary>
        /// 영혼 슬롯 사용량 확인
        /// </summary>
        public (int used, int max) GetSoulSlotUsage()
        {
            string accountId = GetAccountId();
            var accountSouls = LoadSoulsFromAccount(accountId);
            return (accountSouls.Count, maxSoulSlots);
        }
        
        /// <summary>
        /// 계정 영혼 초기화 (디버그용)
        /// </summary>
        [ContextMenu("Clear Account Souls")]
        public void ClearAccountSouls()
        {
            if (Application.isPlaying)
            {
                string accountId = GetAccountId();
                PlayerPrefs.DeleteKey($"Account_{accountId}_Souls");
                PlayerPrefs.Save();
                Debug.Log("🧹 Cleared all account souls");
            }
        }
        
        /// <summary>
        /// 영혼 보존 테스트 (디버그용)
        /// </summary>
        [ContextMenu("Test Soul Preservation")]
        public void TestSoulPreservation()
        {
            if (Application.isPlaying)
            {
                // 테스트용 영혼 생성
                var testSoul = new SoulData
                {
                    soulId = (ulong)Random.Range(1000, 9999),
                    soulName = "Test Soul",
                    statBonus = new StatBlock
                    {
                        strength = Random.Range(1, 5),
                        agility = Random.Range(1, 5),
                        vitality = Random.Range(1, 5)
                    },
                    // TODO: sourceLevel과 sourceRace 필드가 SoulData에 정의되지 않음
                    floorFound = Random.Range(1, 15),
                    acquiredTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                
                currentSouls.Add(testSoul);
                PreserveSouls();
            }
        }
    }
    
    // SoulData는 SoulInheritance.cs에서 정의됨 (중복 제거)
    
    /// <summary>
    /// 영혼 데이터 래퍼 (JSON 직렬화용)
    /// </summary>
    [System.Serializable]
    public class SoulDataWrapper
    {
        public SoulData[] souls;
    }
}