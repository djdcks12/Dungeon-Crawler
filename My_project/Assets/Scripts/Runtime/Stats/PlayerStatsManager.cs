using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 플레이어 스탯 관리자
    /// PlayerStats와 게임 오브젝트 간의 연결 담당
    /// </summary>
    public class PlayerStatsManager : NetworkBehaviour
    {
        [Header("Stats Configuration")]
        [SerializeField] private PlayerStats defaultStats;
        [SerializeField] private PlayerStats currentStats;
        
        [Header("Stats Synchronization")]
        private NetworkVariable<int> networkLevel = new NetworkVariable<int>(1, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Owner);
        private NetworkVariable<float> networkCurrentHP = new NetworkVariable<float>(100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);
        private NetworkVariable<float> networkMaxHP = new NetworkVariable<float>(100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);
        
        // 컴포넌트 참조
        private PlayerController playerController;
        
        // 스탯 변경 이벤트
        public System.Action<PlayerStats> OnStatsUpdated;
        public System.Action<float, float> OnHealthChanged;
        public System.Action OnPlayerDeath; // 사망 이벤트
        public System.Action<int> OnLevelChanged;
        
        public PlayerStats CurrentStats => currentStats;
        public bool IsDead => currentStats != null && currentStats.IsDead();
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            playerController = GetComponent<PlayerController>();
            
            if (IsOwner)
            {
                InitializeStats();
            }
            
            // 네트워크 변수 변경 이벤트 구독
            networkLevel.OnValueChanged += OnNetworkLevelChanged;
            networkCurrentHP.OnValueChanged += OnNetworkHPChanged;
            networkMaxHP.OnValueChanged += OnNetworkMaxHPChanged;
            
            // 스탯 이벤트 구독
            if (currentStats != null)
            {
                PlayerStats.OnStatsChanged += OnStatsChanged;
                PlayerStats.OnLevelUp += OnLevelUp;
                PlayerStats.OnHPChanged += OnHPChanged;
                PlayerStats.OnMPChanged += OnMPChanged;
            }
        }
        
        public override void OnNetworkDespawn()
        {
            // 이벤트 구독 해제
            networkLevel.OnValueChanged -= OnNetworkLevelChanged;
            networkCurrentHP.OnValueChanged -= OnNetworkHPChanged;
            networkMaxHP.OnValueChanged -= OnNetworkMaxHPChanged;
            
            if (currentStats != null)
            {
                PlayerStats.OnStatsChanged -= OnStatsChanged;
                PlayerStats.OnLevelUp -= OnLevelUp;
                PlayerStats.OnHPChanged -= OnHPChanged;
                PlayerStats.OnMPChanged -= OnMPChanged;
            }
            
            base.OnNetworkDespawn();
        }
        
        private void InitializeStats()
        {
            // ScriptableObject 기반이 아닌 직접 데이터 초기화
            currentStats = ScriptableObject.CreateInstance<PlayerStats>();
            
            // 기본 종족 설정 (추후 캐릭터 생성 시 올바른 종족으로 설정)
            var humanRaceData = RaceDataCreator.CreateHumanRaceData();
            currentStats.SetRace(Race.Human, humanRaceData);
            currentStats.Initialize();
            
            // 네트워크 변수 초기화
            UpdateNetworkVariables();
            
            // PlayerController에 스탯 반영
            ApplyStatsToController();
            
            Debug.Log($"PlayerStatsManager initialized for {gameObject.name}");
        }
        
        /// <summary>
        /// 캐릭터 데이터로 스탯 설정 (캐릭터 생성/로드 시 사용)
        /// </summary>
        public void InitializeFromCharacterData(CharacterData characterData)
        {
            if (currentStats == null)
            {
                currentStats = ScriptableObject.CreateInstance<PlayerStats>();
            }
            
            // 종족 데이터 가져오기
            RaceData raceData = GetRaceDataByType(characterData.race);
            if (raceData != null)
            {
                currentStats.SetRace(characterData.race, raceData);
            }
            
            // 캐릭터 데이터로 초기화
            SetLevel(characterData.level);
            SetExperience(characterData.experience);
            
            // 영혼 보너스 스탯 적용
            currentStats.AddSoulBonusStats(characterData.soulBonusStats);
            
            // 네트워크 변수 업데이트
            if (IsOwner)
            {
                UpdateNetworkVariables();
            }
            
            ApplyStatsToController();
            
            Debug.Log($"PlayerStatsManager initialized from character data: {characterData.characterName}");
        }
        
        /// <summary>
        /// 종족 타입으로 RaceData 가져오기
        /// </summary>
        private RaceData GetRaceDataByType(Race raceType)
        {
            switch (raceType)
            {
                case Race.Human:
                    return RaceDataCreator.CreateHumanRaceData();
                case Race.Elf:
                    return RaceDataCreator.CreateElfRaceData();
                case Race.Beast:
                    return RaceDataCreator.CreateBeastRaceData();
                case Race.Machina:
                    return RaceDataCreator.CreateMachinaRaceData();
                default:
                    return RaceDataCreator.CreateHumanRaceData();
            }
        }
        
        /// <summary>
        /// 레벨 직접 설정 (로드 시 사용)
        /// </summary>
        private void SetLevel(int level)
        {
            if (currentStats == null) return;
            
            // 리플렉션을 사용하여 private 필드 설정
            var levelField = typeof(PlayerStats).GetField("currentLevel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            levelField?.SetValue(currentStats, level);
            
            currentStats.RecalculateStats();
        }
        
        /// <summary>
        /// 경험치 직접 설정 (로드 시 사용)
        /// </summary>
        private void SetExperience(long experience)
        {
            if (currentStats == null) return;
            
            // 리플렉션을 사용하여 private 필드 설정
            var expField = typeof(PlayerStats).GetField("currentExp", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            expField?.SetValue(currentStats, experience);
        }
        
        // 경험치 추가 (로컬)
        public void AddExperience(long amount)
        {
            if (!IsOwner || currentStats == null) return;
            
            currentStats.AddExperience(amount);
            ApplyStatsToController();
            UpdateNetworkVariables();
        }
        
        // 경험치 추가 (서버에서만)
        [ServerRpc]
        public void AddExperienceServerRpc(long amount)
        {
            if (currentStats == null) return;
            
            currentStats.AddExperience(amount);
            UpdateNetworkVariables();
        }
        
        // 데미지 받기
        public float TakeDamage(float damage, DamageType damageType = DamageType.Physical)
        {
            if (currentStats == null) return 0f;
            
            float actualDamage = currentStats.TakeDamage(damage, damageType);
            
            if (IsOwner)
            {
                UpdateNetworkVariables();
            }
            
            // 죽음 처리
            if (currentStats.IsDead())
            {
                OnPlayerDeath?.Invoke();
            }
            
            return actualDamage;
        }
        
        // 힐링
        public void Heal(float amount)
        {
            if (currentStats == null) return;
            
            currentStats.ChangeHP(amount);
            
            if (IsOwner)
            {
                UpdateNetworkVariables();
            }
        }
        
        // MP 회복
        public void RestoreMP(float amount)
        {
            if (currentStats == null) return;
            
            currentStats.ChangeMP(amount);
        }
        
        // MP 소모
        public void ChangeMP(float amount)
        {
            if (currentStats == null) return;
            
            currentStats.ChangeMP(amount);
            
            if (IsOwner)
            {
                UpdateNetworkVariables();
            }
        }
        
        // 골드 변경
        public void ChangeGold(long amount)
        {
            if (currentStats == null) return;
            
            currentStats.ChangeGold(amount);
            
            Debug.Log($"Gold changed by {amount}. Current gold: {currentStats?.Gold ?? 0}");
        }
        
        // 영혼 보너스 스탯 추가
        public void AddSoulBonusStats(StatBlock bonusStats)
        {
            if (currentStats == null) return;
            
            currentStats.AddSoulBonusStats(bonusStats);
            ApplyStatsToController();
            
            if (IsOwner)
            {
                UpdateNetworkVariables();
            }
        }
        
        // 스탯을 PlayerController에 반영
        private void ApplyStatsToController()
        {
            if (playerController == null || currentStats == null) return;
            
            // 이동속도는 PlayerController에서 직접 CurrentStats를 참조하도록 변경
            // playerController.SetMoveSpeed(currentStats.MoveSpeed);
            
            // 기타 스탯들도 필요에 따라 반영
            // 예: 공격속도, 공격력 등
        }
        
        // 네트워크 변수 업데이트
        private void UpdateNetworkVariables()
        {
            if (!IsOwner || currentStats == null) return;
            
            networkLevel.Value = currentStats.CurrentLevel;
            networkCurrentHP.Value = currentStats.CurrentHP;
            networkMaxHP.Value = currentStats.MaxHP;
        }
        
        // 네트워크 이벤트 콜백들
        private void OnNetworkLevelChanged(int previousValue, int newValue)
        {
            OnLevelChanged?.Invoke(newValue);
        }
        
        private void OnNetworkHPChanged(float previousValue, float newValue)
        {
            OnHealthChanged?.Invoke(newValue, networkMaxHP.Value);
        }
        
        private void OnNetworkMaxHPChanged(float previousValue, float newValue)
        {
            OnHealthChanged?.Invoke(networkCurrentHP.Value, newValue);
        }
        
        // 스탯 이벤트 콜백들
        private void OnStatsChanged(PlayerStats stats)
        {
            if (stats == currentStats)
            {
                ApplyStatsToController();
                OnStatsUpdated?.Invoke(stats);
            }
        }
        
        private void OnLevelUp(int newLevel)
        {
            Debug.Log($"Player {gameObject.name} leveled up to {newLevel}!");
            
            // 레벨업 이펙트나 사운드 재생
            PlayLevelUpEffect();
        }
        
        private void OnHPChanged(float currentHP, float maxHP)
        {
            OnHealthChanged?.Invoke(currentHP, maxHP);
        }
        
        private void OnMPChanged(float currentMP, float maxMP)
        {
            // MP 변경 처리
        }
        
        // 플레이어 죽음 처리
        private void HandlePlayerDeathInternal()
        {
            Debug.Log($"Player {gameObject.name} died!");
            
            // 죽음 처리 로직
            if (IsOwner)
            {
                HandlePlayerDeathServerRpc();
            }
        }
        
        [ServerRpc]
        private void HandlePlayerDeathServerRpc()
        {
            // 서버에서 죽음 처리
            PlayDeathEffectClientRpc();
        }
        
        [ClientRpc]
        private void PlayDeathEffectClientRpc()
        {
            // 죽음 이펙트 재생
        }
        
        // 레벨업 이펙트
        private void PlayLevelUpEffect()
        {
            // 레벨업 파티클이나 사운드 재생
            Debug.Log("Level up effect played!");
        }
        
        // 스탯 정보 가져오기
        public StatInfo GetStatInfo(StatType statType)
        {
            if (currentStats == null) return new StatInfo();
            
            switch (statType)
            {
                case StatType.STR:
                    return new StatInfo { baseValue = currentStats.TotalSTR, finalValue = currentStats.TotalSTR };
                case StatType.AGI:
                    return new StatInfo { baseValue = currentStats.TotalAGI, finalValue = currentStats.TotalAGI };
                case StatType.VIT:
                    return new StatInfo { baseValue = currentStats.TotalVIT, finalValue = currentStats.TotalVIT };
                case StatType.INT:
                    return new StatInfo { baseValue = currentStats.TotalINT, finalValue = currentStats.TotalINT };
                case StatType.DEF:
                    return new StatInfo { baseValue = currentStats.TotalDEF, finalValue = currentStats.TotalDEF };
                case StatType.MDEF:
                    return new StatInfo { baseValue = currentStats.TotalMDEF, finalValue = currentStats.TotalMDEF };
                case StatType.LUK:
                    return new StatInfo { baseValue = currentStats.TotalLUK, finalValue = currentStats.TotalLUK };
                default:
                    return new StatInfo();
            }
        }
        
        // 레벨업 가능 여부 확인
        public bool CanLevelUp()
        {
            return currentStats != null && currentStats.CurrentExp >= currentStats.ExpToNextLevel;
        }
        
        // 영혼 보너스 스탯 리셋
        public void ResetSoulBonusStats()
        {
            if (!IsOwner || currentStats == null) return;
            
            currentStats.ResetSoulBonusStats();
            ApplyStatsToController();
            UpdateNetworkVariables();
        }
        
        // 디버그 정보
        public void LogCurrentStats()
        {
            if (currentStats != null)
            {
                currentStats.LogStats();
            }
        }
        
        // 현재 체력 비율
        public float GetHealthPercentage()
        {
            if (currentStats == null) return 0f;
            return currentStats.CurrentHP / currentStats.MaxHP;
        }
        
        // 현재 MP 비율
        public float GetManaPercentage()
        {
            if (currentStats == null) return 0f;
            return currentStats.CurrentMP / currentStats.MaxMP;
        }
    }
    
    // 스탯 정보 구조체
    [System.Serializable]
    public struct StatInfo
    {
        public float baseValue;
        public float bonusValue;
        public float finalValue;
        
        public StatInfo(float baseVal = 0f, float bonusVal = 0f)
        {
            baseValue = baseVal;
            bonusValue = bonusVal;
            finalValue = baseVal + bonusVal;
        }
    }
}