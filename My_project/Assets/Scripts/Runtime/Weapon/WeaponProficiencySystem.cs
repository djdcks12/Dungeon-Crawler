using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 무기 숙련도 시스템
    /// 각 WeaponType별로 0~100 숙련도를 관리하고 최소 데미지를 보정
    /// </summary>
    public class WeaponProficiencySystem : NetworkBehaviour
    {
        [Header("Proficiency Settings")]
        [SerializeField] private float expPerHit = 1.0f;
        [SerializeField] private float humanBonusMultiplier = 1.1f; // 인간 종족 보너스 10%
        
        // WeaponType별 숙련도 (0~100)
        private Dictionary<WeaponType, float> proficiencies = new Dictionary<WeaponType, float>();
        
        // 네트워크 동기화용 (WeaponType은 enum이므로 int로 변환해서 동기화)
        private NetworkVariable<ProficiencyData> networkProficiencies = new NetworkVariable<ProficiencyData>(
            new ProficiencyData(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        // 컴포넌트 참조
        private PlayerStatsManager statsManager;
        private EquipmentManager equipmentManager;
        
        // 이벤트
        public System.Action<WeaponType, float> OnProficiencyChanged;
        public System.Action<WeaponType, int> OnProficiencyLevelUp; // 레벨업 (10, 20, 30... 단위)
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            statsManager = GetComponent<PlayerStatsManager>();
            equipmentManager = GetComponent<EquipmentManager>();
            
            // 네트워크 변수 이벤트 구독
            networkProficiencies.OnValueChanged += OnNetworkProficiencyChanged;
            
            // 모든 무기군 초기화
            InitializeProficiencies();
        }
        
        private void InitializeProficiencies()
        {
            foreach (WeaponType weaponType in System.Enum.GetValues(typeof(WeaponType)))
            {
                proficiencies[weaponType] = 0f;
            }
        }
        
        /// <summary>
        /// 적 공격시 숙련도 경험치 획득
        /// </summary>
        public void GainProficiencyExp(WeaponType weaponType, float damage)
        {
            if (!IsOwner) return;
            
            float expGain = expPerHit;
            
            // 인간 종족 보너스
            if (statsManager.PlayerRace == Race.Human)
            {
                expGain *= humanBonusMultiplier;
            }
            
            // 데미지에 비례한 추가 경험치 (소수점)
            expGain += damage * 0.01f;
            
            float currentProf = proficiencies.ContainsKey(weaponType) ? proficiencies[weaponType] : 0f;
            float newProf = Mathf.Clamp(currentProf + expGain, 0f, 100f);
            
            if (newProf != currentProf)
            {
                proficiencies[weaponType] = newProf;
                OnProficiencyChanged?.Invoke(weaponType, newProf);
                
                // 10 단위로 레벨업 체크
                int oldLevel = Mathf.FloorToInt(currentProf / 10f);
                int newLevel = Mathf.FloorToInt(newProf / 10f);
                
                if (newLevel > oldLevel)
                {
                    OnProficiencyLevelUp?.Invoke(weaponType, newLevel);
                }
                
                // 네트워크 동기화
                SyncProficiencyToNetwork();
            }
        }
        
        /// <summary>
        /// 특정 무기 타입의 숙련도 반환 (0~100)
        /// </summary>
        public float GetProficiency(WeaponType weaponType)
        {
            return proficiencies.ContainsKey(weaponType) ? proficiencies[weaponType] : 0f;
        }
        
        /// <summary>
        /// 숙련도 계수 반환 (0.0~1.0)
        /// </summary>
        public float GetProficiencyRatio(WeaponType weaponType)
        {
            return GetProficiency(weaponType) / 100f;
        }
        
        /// <summary>
        /// 무기 데미지에 숙련도 보정 적용
        /// </summary>
        public float ApplyProficiencyToMinDamage(WeaponType weaponType, float minDamage, float maxDamage)
        {
            float proficiencyRatio = GetProficiencyRatio(weaponType);
            
            // 보정된 최소 데미지 = 기존 최소 데미지 + (최대 데미지 - 최소 데미지) * 숙련도 비율
            float effectiveMinDamage = minDamage + (maxDamage - minDamage) * proficiencyRatio;
            
            return effectiveMinDamage;
        }
        
        /// <summary>
        /// 현재 장착중인 무기의 WeaponType 반환
        /// </summary>
        public WeaponType GetCurrentWeaponType()
        {
            var mainWeapon = equipmentManager.Equipment.GetEquippedItem(EquipmentSlot.MainHand);
            if (mainWeapon != null && mainWeapon.ItemData != null && mainWeapon.ItemData.IsWeapon)
            {
                return mainWeapon.ItemData.WeaponType;
            }
            
            // 맨손일 경우 격투 무기로 처리
            return WeaponType.Fists;
        }
        
        /// <summary>
        /// 현재 장착중인 무기의 WeaponGroup 반환 (호환성용)
        /// [Deprecated] GetCurrentWeaponType() 사용 권장
        /// </summary>
        [System.Obsolete("Use GetCurrentWeaponType() instead")]
        public WeaponGroup GetCurrentWeaponGroup()
        {
            WeaponType currentType = GetCurrentWeaponType();
            return WeaponTypeMapper.GetWeaponGroup(currentType);
        }
        
        private void SyncProficiencyToNetwork()
        {
            if (!IsOwner) return;
            
            ProficiencyData data = new ProficiencyData();
            data.UpdateFromDictionary(proficiencies);
            networkProficiencies.Value = data;
        }
        
        private void OnNetworkProficiencyChanged(ProficiencyData previousValue, ProficiencyData newValue)
        {
            if (IsOwner) return; // Owner는 이미 로컬에서 업데이트됨
            
            proficiencies = newValue.ToDictionary();
            
            // 모든 무기군에 대해 변경 이벤트 발생
            foreach (var kvp in proficiencies)
            {
                OnProficiencyChanged?.Invoke(kvp.Key, kvp.Value);
            }
        }
        
        /// <summary>
        /// 디버그용 - 모든 숙련도 출력
        /// </summary>
        [ContextMenu("Debug Proficiencies")]
        public void DebugProficiencies()
        {
            Debug.Log("=== Weapon Proficiencies ===");
            foreach (var kvp in proficiencies)
            {
                Debug.Log($"{kvp.Key}: {kvp.Value:F1}/100 ({GetProficiencyRatio(kvp.Key):P1})");
            }
        }
    }
    
    /// <summary>
    /// 네트워크 동기화용 숙련도 데이터 - 동적 WeaponType 관리
    /// </summary>
    [System.Serializable]
    public struct ProficiencyData : Unity.Netcode.INetworkSerializable
    {
        public WeaponType[] weaponTypes;
        public float[] proficiencyValues;
        
        public void UpdateFromDictionary(Dictionary<WeaponType, float> proficiencies)
        {
            // 0보다 큰 값만 동기화 (사용하는 무기만)
            var activeProficiencies = proficiencies.Where(kvp => kvp.Value > 0f).ToArray();
            
            weaponTypes = activeProficiencies.Select(kvp => kvp.Key).ToArray();
            proficiencyValues = activeProficiencies.Select(kvp => kvp.Value).ToArray();
        }
        
        public Dictionary<WeaponType, float> ToDictionary()
        {
            var result = new Dictionary<WeaponType, float>();
            
            if (weaponTypes != null && proficiencyValues != null)
            {
                int length = Mathf.Min(weaponTypes.Length, proficiencyValues.Length);
                for (int i = 0; i < length; i++)
                {
                    result[weaponTypes[i]] = proficiencyValues[i];
                }
            }
            
            return result;
        }
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // 배열 길이 동기화
            int weaponCount = weaponTypes?.Length ?? 0;
            serializer.SerializeValue(ref weaponCount);
            
            // WeaponType 배열 동기화
            if (serializer.IsWriter && weaponTypes != null)
            {
                for (int i = 0; i < weaponTypes.Length; i++)
                {
                    int weaponTypeInt = (int)weaponTypes[i];
                    serializer.SerializeValue(ref weaponTypeInt);
                }
            }
            else if (serializer.IsReader)
            {
                weaponTypes = new WeaponType[weaponCount];
                for (int i = 0; i < weaponCount; i++)
                {
                    int weaponTypeInt = 0;
                    serializer.SerializeValue(ref weaponTypeInt);
                    weaponTypes[i] = (WeaponType)weaponTypeInt;
                }
            }
            
            // 숙련도 값 배열 동기화
            if (serializer.IsWriter && proficiencyValues != null)
            {
                for (int i = 0; i < proficiencyValues.Length; i++)
                {
                    float value = proficiencyValues[i];
                    serializer.SerializeValue(ref value);
                }
            }
            else if (serializer.IsReader)
            {
                proficiencyValues = new float[weaponCount];
                for (int i = 0; i < weaponCount; i++)
                {
                    serializer.SerializeValue(ref proficiencyValues[i]);
                }
            }
        }
    }
}