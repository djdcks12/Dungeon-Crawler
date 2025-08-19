using System.Collections.Generic;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 던전 시스템의 문자열 해시 레지스트리
    /// NetworkList에서 문자열을 직접 사용할 수 없으므로 해시값으로 변환하여 저장하고,
    /// 필요할 때 다시 문자열로 변환하는 시스템
    /// </summary>
    public static class DungeonNameRegistry
    {
        // 해시값 -> 문자열 매핑
        private static readonly Dictionary<int, string> hashToName = new Dictionary<int, string>();
        
        // 문자열 -> 해시값 매핑 (성능 최적화용)
        private static readonly Dictionary<string, int> nameToHash = new Dictionary<string, int>();
        
        /// <summary>
        /// 문자열을 해시값으로 변환하고 레지스트리에 등록
        /// </summary>
        /// <param name="name">등록할 문자열</param>
        /// <returns>해당 문자열의 해시값</returns>
        public static int RegisterName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return 0;
            }
            
            // 이미 등록된 이름인지 확인
            if (nameToHash.TryGetValue(name, out int existingHash))
            {
                return existingHash;
            }
            
            // 새로운 해시값 생성
            int hash = name.GetHashCode();
            
            // 해시 충돌 방지를 위한 체크 및 조정
            while (hashToName.ContainsKey(hash))
            {
                hash += 1; // 간단한 선형 프로빙
            }
            
            // 양방향 매핑에 등록
            hashToName[hash] = name;
            nameToHash[name] = hash;
            
            return hash;
        }
        
        /// <summary>
        /// 해시값에서 문자열 반환
        /// </summary>
        /// <param name="hash">찾을 해시값</param>
        /// <returns>해당하는 문자열, 없으면 빈 문자열</returns>
        public static string GetNameFromHash(int hash)
        {
            if (hash == 0)
            {
                return string.Empty;
            }
            
            return hashToName.TryGetValue(hash, out string name) ? name : $"Unknown_{hash}";
        }
        
        /// <summary>
        /// 문자열에서 해시값 반환 (등록하지 않고 조회만)
        /// </summary>
        /// <param name="name">찾을 문자열</param>
        /// <returns>해당하는 해시값, 없으면 0</returns>
        public static int GetHashFromName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return 0;
            }
            
            return nameToHash.TryGetValue(name, out int hash) ? hash : 0;
        }
        
        /// <summary>
        /// 레지스트리에 등록된 이름인지 확인
        /// </summary>
        /// <param name="name">확인할 문자열</param>
        /// <returns>등록된 경우 true</returns>
        public static bool IsRegistered(string name)
        {
            return !string.IsNullOrEmpty(name) && nameToHash.ContainsKey(name);
        }
        
        /// <summary>
        /// 해시값이 등록된 것인지 확인
        /// </summary>
        /// <param name="hash">확인할 해시값</param>
        /// <returns>등록된 경우 true</returns>
        public static bool IsRegistered(int hash)
        {
            return hash != 0 && hashToName.ContainsKey(hash);
        }
        
        /// <summary>
        /// 레지스트리 초기화 (테스트 용도)
        /// </summary>
        public static void Clear()
        {
            hashToName.Clear();
            nameToHash.Clear();
        }
        
        /// <summary>
        /// 현재 등록된 모든 이름 목록 반환 (디버깅 용도)
        /// </summary>
        /// <returns>등록된 이름들의 리스트</returns>
        public static List<string> GetAllRegisteredNames()
        {
            return new List<string>(nameToHash.Keys);
        }
        
        /// <summary>
        /// 레지스트리 상태 정보 출력 (디버깅 용도)
        /// </summary>
        public static void LogRegistryInfo()
        {
            Debug.Log($"DungeonNameRegistry - Registered Names: {nameToHash.Count}");
            foreach (var kvp in nameToHash)
            {
                Debug.Log($"  '{kvp.Key}' -> {kvp.Value}");
            }
        }
        
        /// <summary>
        /// 자주 사용되는 던전 관련 이름들을 미리 등록
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void PreregisterCommonNames()
        {
            // 기본 던전 이름들
            RegisterName("새로운 던전");
            RegisterName("초보자 던전");
            RegisterName("어둠의 던전");
            RegisterName("화염의 던전");
            RegisterName("얼음의 던전");
            RegisterName("최종 던전");
            
            // 기본 층 이름들
            for (int i = 1; i <= 10; i++)
            {
                RegisterName($"{i}층");
                RegisterName($"Floor {i}");
            }
            
            // 기본 플레이어 이름들
            for (int i = 1; i <= 16; i++)
            {
                RegisterName($"Player_{i}");
                RegisterName($"플레이어{i}");
            }
            
            Debug.Log($"DungeonNameRegistry initialized with {nameToHash.Count} preregistered names");
        }
    }
}