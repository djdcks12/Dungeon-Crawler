using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 무기 숙련도 시스템
    /// 각 무기군별로 0~100 숙련도를 관리하고 최소 데미지를 보정
    /// </summary>
    public class WeaponProficiencySystem : NetworkBehaviour
    {
        [Header("Proficiency Settings")]
        [SerializeField] private float expPerHit = 1.0f;
        [SerializeField] private float humanBonusMultiplier = 1.1f; // 인간 종족 보너스 10%
        
        // 무기군별 숙련도 (0~100)
        private Dictionary<WeaponGroup, float> proficiencies = new Dictionary<WeaponGroup, float>();
        
        // 네트워크 동기화용 (WeaponGroup은 enum이므로 int로 변환해서 동기화)
        private NetworkVariable<ProficiencyData> networkProficiencies = new NetworkVariable<ProficiencyData>(
            new ProficiencyData(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        // 컴포넌트 참조
        private PlayerStatsManager statsManager;
        private EquipmentManager equipmentManager;
        
        // 이벤트
        public System.Action<WeaponGroup, float> OnProficiencyChanged;
        public System.Action<WeaponGroup, int> OnProficiencyLevelUp; // 레벨업 (10, 20, 30... 단위)
        
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
            foreach (WeaponGroup weaponGroup in System.Enum.GetValues(typeof(WeaponGroup)))
            {
                proficiencies[weaponGroup] = 0f;
            }
        }
        
        /// <summary>
        /// 적 공격시 숙련도 경험치 획득
        /// </summary>
        public void GainProficiencyExp(WeaponGroup weaponGroup, float damage)
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
            
            float currentProf = proficiencies[weaponGroup];
            float newProf = Mathf.Clamp(currentProf + expGain, 0f, 100f);
            
            if (newProf != currentProf)
            {
                proficiencies[weaponGroup] = newProf;
                OnProficiencyChanged?.Invoke(weaponGroup, newProf);
                
                // 10 단위로 레벨업 체크
                int oldLevel = Mathf.FloorToInt(currentProf / 10f);
                int newLevel = Mathf.FloorToInt(newProf / 10f);
                
                if (newLevel > oldLevel)
                {
                    OnProficiencyLevelUp?.Invoke(weaponGroup, newLevel);
                }
                
                // 네트워크 동기화
                SyncProficiencyToNetwork();
            }
        }
        
        /// <summary>
        /// 특정 무기군의 숙련도 반환 (0~100)
        /// </summary>
        public float GetProficiency(WeaponGroup weaponGroup)
        {
            return proficiencies.ContainsKey(weaponGroup) ? proficiencies[weaponGroup] : 0f;
        }
        
        /// <summary>
        /// 숙련도 계수 반환 (0.0~1.0)
        /// </summary>
        public float GetProficiencyRatio(WeaponGroup weaponGroup)
        {
            return GetProficiency(weaponGroup) / 100f;
        }
        
        /// <summary>
        /// 무기 데미지에 숙련도 보정 적용
        /// </summary>
        public float ApplyProficiencyToMinDamage(WeaponGroup weaponGroup, float minDamage, float maxDamage)
        {
            float proficiencyRatio = GetProficiencyRatio(weaponGroup);
            
            // 보정된 최소 데미지 = 기존 최소 데미지 + (최대 데미지 - 최소 데미지) * 숙련도 비율
            float effectiveMinDamage = minDamage + (maxDamage - minDamage) * proficiencyRatio;
            
            return effectiveMinDamage;
        }
        
        /// <summary>
        /// 현재 장착중인 무기의 무기군 반환
        /// </summary>
        public WeaponGroup GetCurrentWeaponGroup()
        {
            var mainWeapon = equipmentManager.Equipment.GetEquippedItem(EquipmentSlot.MainHand);
            if (mainWeapon != null && mainWeapon.itemData is WeaponData weaponData)
            {
                return weaponData.weaponGroup;
            }
            
            // 맨손일 경우 격투 무기로 처리
            return WeaponGroup.Fist;
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
    /// 네트워크 동기화용 숙련도 데이터
    /// </summary>
    [System.Serializable]
    public struct ProficiencyData : Unity.Netcode.INetworkSerializable
    {
        public float swordShield;
        public float twoHandedSword;
        public float twoHandedAxe;
        public float dagger;
        public float bow;
        public float staff;
        public float wand;
        public float fist;
        
        public void UpdateFromDictionary(Dictionary<WeaponGroup, float> proficiencies)
        {
            swordShield = proficiencies.GetValueOrDefault(WeaponGroup.SwordShield, 0f);
            twoHandedSword = proficiencies.GetValueOrDefault(WeaponGroup.TwoHandedSword, 0f);
            twoHandedAxe = proficiencies.GetValueOrDefault(WeaponGroup.TwoHandedAxe, 0f);
            dagger = proficiencies.GetValueOrDefault(WeaponGroup.Dagger, 0f);
            bow = proficiencies.GetValueOrDefault(WeaponGroup.Bow, 0f);
            staff = proficiencies.GetValueOrDefault(WeaponGroup.Staff, 0f);
            wand = proficiencies.GetValueOrDefault(WeaponGroup.Wand, 0f);
            fist = proficiencies.GetValueOrDefault(WeaponGroup.Fist, 0f);
        }
        
        public Dictionary<WeaponGroup, float> ToDictionary()
        {
            return new Dictionary<WeaponGroup, float>
            {
                { WeaponGroup.SwordShield, swordShield },
                { WeaponGroup.TwoHandedSword, twoHandedSword },
                { WeaponGroup.TwoHandedAxe, twoHandedAxe },
                { WeaponGroup.Dagger, dagger },
                { WeaponGroup.Bow, bow },
                { WeaponGroup.Staff, staff },
                { WeaponGroup.Wand, wand },
                { WeaponGroup.Fist, fist }
            };
        }
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref swordShield);
            serializer.SerializeValue(ref twoHandedSword);
            serializer.SerializeValue(ref twoHandedAxe);
            serializer.SerializeValue(ref dagger);
            serializer.SerializeValue(ref bow);
            serializer.SerializeValue(ref staff);
            serializer.SerializeValue(ref wand);
            serializer.SerializeValue(ref fist);
        }
    }
}