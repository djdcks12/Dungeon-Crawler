using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 종족-무기군 조합 데이터 관리자
    /// Resources에서 RaceWeaponGroupData들을 로드하고 캐싱하여 제공
    /// </summary>
    public static class RaceWeaponGroupManager
    {
        // 캐싱된 조합 데이터들
        private static Dictionary<string, RaceWeaponGroupData> cachedCombinations = new Dictionary<string, RaceWeaponGroupData>();
        private static bool isInitialized = false;
        
        /// <summary>
        /// 초기화 - Resources에서 모든 RaceWeaponGroupData 로드
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized) return;
            
            cachedCombinations.Clear();
            
            // Resources/RaceWeaponGroups 폴더에서 모든 데이터 로드
            RaceWeaponGroupData[] allCombinations = Resources.LoadAll<RaceWeaponGroupData>("RaceWeaponGroups");
            
            foreach (var combination in allCombinations)
            {
                if (combination == null) continue;
                if (combination.IsValidCombination())
                {
                    string key = combination.GetCombinationKey();
                    cachedCombinations[key] = combination;
                    Debug.Log($"🎭 Loaded race-weapon combination: {key}");
                }
                else
                {
                    Debug.LogWarning($"❌ Invalid combination found: {combination.GetCombinationKey()}");
                }
            }
            
            isInitialized = true;
            Debug.Log($"🎭 RaceWeaponGroupManager initialized with {cachedCombinations.Count} combinations");
        }
        
        /// <summary>
        /// 종족-무기군 조합 데이터 가져오기
        /// </summary>
        public static RaceWeaponGroupData GetCombinationData(Race race, WeaponGroup weaponGroup)
        {
            if (!isInitialized)
            {
                Initialize();
            }
            
            string key = $"{race}_{weaponGroup}";
            
            if (cachedCombinations.TryGetValue(key, out RaceWeaponGroupData data))
            {
                return data;
            }
            
            // 조합이 없으면 폴백 시도
            return GetFallbackCombination(race, weaponGroup);
        }
        
        /// <summary>
        /// 폴백 조합 찾기
        /// 1. 같은 종족의 Fist 무기군
        /// 2. Human의 같은 무기군
        /// 3. Human의 Fist 무기군
        /// </summary>
        private static RaceWeaponGroupData GetFallbackCombination(Race race, WeaponGroup weaponGroup)
        {
            // 1. 같은 종족의 Fist 무기군 시도
            if (weaponGroup != WeaponGroup.Fist)
            {
                string fallbackKey1 = $"{race}_{WeaponGroup.Fist}";
                if (cachedCombinations.TryGetValue(fallbackKey1, out RaceWeaponGroupData fallback1))
                {
                    Debug.LogWarning($"Using fallback combination: {fallbackKey1} for {race}_{weaponGroup}");
                    return fallback1;
                }
            }
            
            // 2. Human의 같은 무기군 시도
            if (race != Race.Human)
            {
                string fallbackKey2 = $"{Race.Human}_{weaponGroup}";
                if (cachedCombinations.TryGetValue(fallbackKey2, out RaceWeaponGroupData fallback2))
                {
                    Debug.LogWarning($"Using fallback combination: {fallbackKey2} for {race}_{weaponGroup}");
                    return fallback2;
                }
            }
            
            // 3. Human의 Fist 무기군 시도 (최종 폴백)
            string ultimateFallback = $"{Race.Human}_{WeaponGroup.Fist}";
            if (cachedCombinations.TryGetValue(ultimateFallback, out RaceWeaponGroupData ultimateData))
            {
                Debug.LogWarning($"Using ultimate fallback: {ultimateFallback} for {race}_{weaponGroup}");
                return ultimateData;
            }
            
            // 그래도 없으면 null 반환
            Debug.LogError($"No fallback combination available for {race}_{weaponGroup}");
            return null;
        }
        
        /// <summary>
        /// 특정 종족이 사용 가능한 모든 조합 데이터 반환
        /// </summary>
        public static RaceWeaponGroupData[] GetCombinationsForRace(Race race)
        {
            if (!isInitialized)
            {
                Initialize();
            }
            
            return cachedCombinations.Values
                .Where(combination => combination.race == race)
                .ToArray();
        }
        
        /// <summary>
        /// 특정 무기군을 사용하는 모든 조합 데이터 반환
        /// </summary>
        public static RaceWeaponGroupData[] GetCombinationsForWeaponGroup(WeaponGroup weaponGroup)
        {
            if (!isInitialized)
            {
                Initialize();
            }
            
            return cachedCombinations.Values
                .Where(combination => combination.weaponGroup == weaponGroup)
                .ToArray();
        }
        
        /// <summary>
        /// 조합이 존재하는지 확인
        /// </summary>
        public static bool HasCombination(Race race, WeaponGroup weaponGroup)
        {
            if (!isInitialized)
            {
                Initialize();
            }
            
            string key = $"{race}_{weaponGroup}";
            return cachedCombinations.ContainsKey(key);
        }
        
        /// <summary>
        /// 모든 로드된 조합 목록 반환
        /// </summary>
        public static RaceWeaponGroupData[] GetAllCombinations()
        {
            if (!isInitialized)
            {
                Initialize();
            }
            
            return cachedCombinations.Values.ToArray();
        }
        
        /// <summary>
        /// 캐시 초기화 (에디터에서 리로드 시 사용)
        /// </summary>
        public static void ClearCache()
        {
            cachedCombinations.Clear();
            isInitialized = false;
            Debug.Log("RaceWeaponGroupManager cache cleared");
        }
        
        /// <summary>
        /// 디버그: 로드된 모든 조합 출력
        /// </summary>
        public static void LogAllCombinations()
        {
            if (!isInitialized)
            {
                Initialize();
            }
            
            Debug.Log($"=== All Loaded Race-Weapon Combinations ({cachedCombinations.Count}) ===");
            
            foreach (var kvp in cachedCombinations)
            {
                var data = kvp.Value;
                string animInfo = $"Idle:{data.HasValidIdleAnimation} Walk:{data.HasValidWalkAnimation} Attack:{data.HasValidAttackAnimation}";
                Debug.Log($"  {kvp.Key}: {animInfo}");
            }
        }
        
        /// <summary>
        /// 에디터용: 누락된 조합 찾기
        /// </summary>
        public static void FindMissingCombinations()
        {
            if (!isInitialized)
            {
                Initialize();
            }
            
            Debug.Log("=== Missing Race-Weapon Combinations ===");
            
            // 각 종족별로 가능한 무기군 확인
            foreach (Race race in System.Enum.GetValues(typeof(Race)))
            {
                if (race == Race.None || race == Race.Machina) continue; // 제외할 종족
                
                WeaponGroup[] availableGroups = WeaponTypeMapper.GetAvailableWeaponGroups(race);
                
                foreach (var weaponGroup in availableGroups)
                {
                    string key = $"{race}_{weaponGroup}";
                    if (!cachedCombinations.ContainsKey(key))
                    {
                        Debug.LogWarning($"  Missing: {key}");
                    }
                }
            }
        }
    }
}