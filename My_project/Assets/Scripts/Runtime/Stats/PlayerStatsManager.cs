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
            NetworkVariableWritePermission.Server);
        private NetworkVariable<float> networkCurrentHP = new NetworkVariable<float>(100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private NetworkVariable<float> networkMaxHP = new NetworkVariable<float>(100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private NetworkVariable<float> networkCurrentMP = new NetworkVariable<float>(100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private NetworkVariable<float> networkMaxMP = new NetworkVariable<float>(100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        
        // 스탯 계산용 추가 NetworkVariable들
        private NetworkVariable<float> networkDefense = new NetworkVariable<float>(0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private NetworkVariable<float> networkMagicDefense = new NetworkVariable<float>(0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private NetworkVariable<float> networkAgility = new NetworkVariable<float>(0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
            
        // 경험치 동기화용 NetworkVariable들
        private NetworkVariable<long> networkCurrentExp = new NetworkVariable<long>(0L,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private NetworkVariable<long> networkExpToNext = new NetworkVariable<long>(100L,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        
        // 컴포넌트 참조
        private PlayerController playerController;
        
        // 처치한 몬스터 추적 (경험치 중복 방지)
        private HashSet<ulong> defeatedMonsters = new HashSet<ulong>();
        
        // 스탯 변경 이벤트
        public System.Action<PlayerStats> OnStatsUpdated;
        public System.Action<float, float> OnHealthChanged;
        public System.Action<float, float> OnManaChanged;
        public System.Action OnExperienceChanged;
        public System.Action OnPlayerDeath; // 사망 이벤트
        public System.Action<int> OnLevelChanged;
        
        
        public PlayerStats CurrentStats => currentStats;
        public bool IsDead => networkCurrentHP.Value <= 0f;
        
        // NetworkVariable 접근용 프로퍼티들
        public int NetworkLevel => networkLevel.Value;
        public long NetworkCurrentExp => networkCurrentExp.Value;
        public long NetworkExpToNext => networkExpToNext.Value;
        public float NetworkCurrentHP => networkCurrentHP.Value;
        public float NetworkMaxHP => networkMaxHP.Value;
        public float NetworkCurrentMP => networkCurrentMP.Value;
        public float NetworkMaxMP => networkMaxMP.Value;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            playerController = GetComponent<PlayerController>();
            
            // Owner 또는 Server에서 스탯 초기화 (서버가 모든 플레이어 관리)
            if (IsOwner || IsServer)
            {
                InitializeStats();
            }
            
            // 네트워크 변수 변경 이벤트 구독
            networkLevel.OnValueChanged += OnNetworkLevelChanged;
            networkCurrentExp.OnValueChanged += OnNetworkExpChanged;
            networkExpToNext.OnValueChanged += OnNetworkExpToNextChanged;
            networkCurrentHP.OnValueChanged += OnNetworkHPChanged;
            networkMaxHP.OnValueChanged += OnNetworkMaxHPChanged;
            networkCurrentMP.OnValueChanged += OnNetworkMPChanged;
            networkMaxMP.OnValueChanged += OnNetworkMaxMPChanged;
            
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
            networkCurrentExp.OnValueChanged -= OnNetworkExpChanged;
            networkExpToNext.OnValueChanged -= OnNetworkExpToNextChanged;
            networkCurrentHP.OnValueChanged -= OnNetworkHPChanged;
            networkMaxHP.OnValueChanged -= OnNetworkMaxHPChanged;
            networkCurrentMP.OnValueChanged -= OnNetworkMPChanged;
            networkMaxMP.OnValueChanged -= OnNetworkMaxMPChanged;
            
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
        
        // 경험치 추가 (서버 권한으로만)
        public void AddExperience(long amount)
        {
            // 서버에서만 경험치 추가 가능 (모든 플레이어에 대해)
            if (!IsServer || currentStats == null) return;
            
            currentStats.AddExperience(amount);
            
            // 서버에서 NetworkVariable 업데이트 (모든 클라이언트에 동기화)
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
        
        // 데미지 받기 (Server 전용 - NetworkVariable 기반)
        public float TakeDamage(float damage, DamageType damageType = DamageType.Physical)
        {
            float finalDamage = CalculateDamage(damage, damageType);
            float oldHP = networkCurrentHP.Value;
            
            // 회피 체크
            if (CheckDodge())
            {
                Debug.Log($"💨 {name} dodged the attack!");
                return 0f;
            }
            
            // HP 감소 적용
            float newHP = Mathf.Max(0f, oldHP - finalDamage);
            networkCurrentHP.Value = newHP;
            
            Debug.Log($"💔 {name} TakeDamage - HP: {oldHP} → {newHP}, actualDamage: {finalDamage}, type: {damageType}");
            
            // 죽음 처리
            if (newHP <= 0f)
            {
                OnPlayerDeath?.Invoke();
            }
            
            return finalDamage;
        }
        
        // 데미지 계산 (NetworkVariable 기반)
        private float CalculateDamage(float damage, DamageType damageType)
        {
            float finalDamage = damage;
            
            switch (damageType)
            {
                case DamageType.Physical:
                    // 물리 방어: DEF / (DEF + 100) * 100% 감소
                    float physicalReduction = networkDefense.Value / (networkDefense.Value + 100f);
                    finalDamage = damage * (1f - physicalReduction);
                    break;
                case DamageType.Magical:
                    // 마법 방어: MDEF / (MDEF + 100) * 100% 감소
                    float magicalReduction = networkMagicDefense.Value / (networkMagicDefense.Value + 100f);
                    finalDamage = damage * (1f - magicalReduction);
                    break;
                case DamageType.True:
                    // 고정 데미지 (방어력 무시)
                    break;
            }
            
            // 최소 1 데미지는 받음
            return Mathf.Max(1f, finalDamage);
        }
        
        // 회피 체크 (NetworkVariable 기반)
        private bool CheckDodge()
        {
            float dodgeChance = networkAgility.Value * 0.001f; // AGI * 0.1%
            return UnityEngine.Random.value < dodgeChance;
        }
        
        // 힐링 (Server 전용 - NetworkVariable 기반)
        public void Heal(float amount)
        {
            // Server에서만 처리하도록 RPC로 전달
            if (!IsServer)
            {
                HealServerRpc(amount);
                return;
            }
            
            float oldHP = networkCurrentHP.Value;
            float newHP = Mathf.Min(networkMaxHP.Value, oldHP + amount);
            networkCurrentHP.Value = newHP;
            
            Debug.Log($"💚 {name} healed: {oldHP} → {newHP} (+{amount})");
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void HealServerRpc(float amount)
        {
            Heal(amount);
        }
        
        // MP 회복 (Server 전용 - NetworkVariable 기반)
        public void RestoreMP(float amount)
        {
            // Server에서만 처리하도록 RPC로 전달
            if (!IsServer)
            {
                RestoreMPServerRpc(amount);
                return;
            }
            
            float oldMP = networkCurrentMP.Value;
            float newMP = Mathf.Min(networkMaxMP.Value, oldMP + amount);
            networkCurrentMP.Value = newMP;
            
            Debug.Log($"💙 {name} MP restored: {oldMP} → {newMP} (+{amount})");
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void RestoreMPServerRpc(float amount)
        {
            RestoreMP(amount);
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
        
        // 골드 추가 (양수 값으로 골드 증가)
        public void AddGold(long amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"AddGold called with negative amount: {amount}. Use ChangeGold for negative values.");
                return;
            }
            
            ChangeGold(amount);
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
            if (currentStats == null) return;
            
            // Server 권한으로 NetworkVariable 업데이트
            if (IsServer)
            {
                networkLevel.Value = currentStats.CurrentLevel;
                networkCurrentExp.Value = currentStats.CurrentExperience;
                networkExpToNext.Value = currentStats.ExpToNextLevel;
                networkCurrentHP.Value = currentStats.CurrentHP;
                networkMaxHP.Value = currentStats.MaxHP;
                networkCurrentMP.Value = currentStats.CurrentMP;
                networkMaxMP.Value = currentStats.MaxMP;
                networkDefense.Value = currentStats.TotalDEF;
                networkMagicDefense.Value = currentStats.TotalMDEF;
                networkAgility.Value = currentStats.TotalAGI;
            }
        }
        
        // 네트워크 이벤트 콜백들
        private void OnNetworkLevelChanged(int previousValue, int newValue)
        {
            OnLevelChanged?.Invoke(newValue);
        }
        
        private void OnNetworkExpChanged(long previousValue, long newValue)
        {            
            // Client에서 NetworkVariable 변경을 currentStats에 반영
            if (!IsServer && currentStats != null)
            {
                SetExperience(newValue);
            }
            
            // 모든 클라이언트에서 경험치 변경 이벤트 호출
            OnExperienceChanged?.Invoke();
        }
        
        private void OnNetworkExpToNextChanged(long previousValue, long newValue)
        {
            // 레벨업이나 경험치 변경 시 ExpToNextLevel 동기화
            Debug.Log($"📈 ExpToNext updated: {newValue}");
        }
        
        private void OnNetworkHPChanged(float previousValue, float newValue)
        {
            // Client에서 NetworkVariable 변경을 currentStats에 반영
            if (!IsServer && currentStats != null)
                currentStats.SetCurrentHP(newValue);
            
            OnHealthChanged?.Invoke(newValue, networkMaxHP.Value);
        }
        
        private void OnNetworkMaxHPChanged(float previousValue, float newValue)
        {
            // Client에서 NetworkVariable 변경을 currentStats에 반영
            if (!IsServer && currentStats != null)
            {
                currentStats.SetMaxHP(newValue);
            }
            OnHealthChanged?.Invoke(networkCurrentHP.Value, newValue);
        }
        
        private void OnNetworkMPChanged(float previousValue, float newValue)
        {
            // Client에서 NetworkVariable 변경을 currentStats에 반영
            if (!IsServer && currentStats != null)
            {
                currentStats.SetCurrentMP(newValue);
            }
            
            // 모든 클라이언트에서 마나 변경 이벤트 호출
            OnManaChanged?.Invoke(newValue, networkMaxMP.Value);
        }
        
        private void OnNetworkMaxMPChanged(float previousValue, float newValue)
        {
            // Client에서 NetworkVariable 변경을 currentStats에 반영
            if (!IsServer && currentStats != null)
            {
                currentStats.SetMaxMP(newValue);
            }
            
            // 모든 클라이언트에서 마나 변경 이벤트 호출
            OnManaChanged?.Invoke(networkCurrentMP.Value, newValue);
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
            return currentStats != null && currentStats.CurrentExperience >= currentStats.ExpToNextLevel;
        }
        
        // 영혼 보너스 스탯 리셋
        public void ResetSoulBonusStats()
        {
            if (!IsOwner || currentStats == null) return;
            
            currentStats.ResetSoulBonusStats();
            ApplyStatsToController();
            UpdateNetworkVariables();
        }
        
        /// <summary>
        /// 몬스터 처치 여부 확인
        /// </summary>
        public bool HasDefeatedMonster(ulong monsterId)
        {
            return defeatedMonsters.Contains(monsterId);
        }
        
        /// <summary>
        /// 몬스터 처치 기록
        /// </summary>
        public void MarkMonsterAsDefeated(ulong monsterId)
        {
            defeatedMonsters.Add(monsterId);
        }
        
        /// <summary>
        /// 몬스터로부터 경험치 획득 (중복 방지)
        /// </summary>
        public bool TryGainExperienceFromMonster(ulong monsterId, long expAmount)
        {
            if (!IsServer) return false;
            
            // 이미 이 몬스터로부터 경험치를 받았는지 확인
            if (HasDefeatedMonster(monsterId))
            {
                return false; // 이미 받았음
            }
            
            // 몬스터 처치 기록 및 경험치 획득
            MarkMonsterAsDefeated(monsterId);
            AddExperience(expAmount);
            
            Debug.Log($"💰 Player {NetworkObject.NetworkObjectId} gained {expAmount} EXP from monster {monsterId}");
            return true; // 성공적으로 받음
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
        /// <summary>
        /// 장비 스탯 업데이트
        /// </summary>
        public void UpdateEquipmentStats(StatBlock equipmentStats)
        {
            if (currentStats == null) return;
            
            // 장비 스탯을 플레이어 스탯에 적용
            currentStats.SetEquipmentBonusStats(equipmentStats);
            
            // 네트워크 변수 업데이트
            if (IsOwner)
            {
                networkCurrentHP.Value = currentStats.CurrentHP;
                networkMaxHP.Value = currentStats.MaxHP;
            }
            
            // 스탯 변경 이벤트 발생
            OnStatsUpdated?.Invoke(currentStats);
            
            Debug.Log($"📊 Equipment stats updated");
        }
        
        /// <summary>
        /// 체력 완전 회복
        /// </summary>
        public void RestoreFullHealth()
        {
            if (!IsServer) return;
            
            networkCurrentHP.Value = networkMaxHP.Value;
            Debug.Log($"💚 Full health restored: {networkCurrentHP.Value}");
        }
        
        /// <summary>
        /// 마나 완전 회복
        /// </summary>
        public void RestoreFullMana()
        {
            if (!IsServer) return;
            
            networkCurrentMP.Value = networkMaxMP.Value;
            Debug.Log($"💙 Full mana restored: {networkCurrentMP.Value}");
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