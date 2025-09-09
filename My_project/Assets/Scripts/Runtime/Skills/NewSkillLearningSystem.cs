using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 새로운 스킬 학습 시스템
    /// 골드 기반, NPC 상호작용, 레벨별 3선택지 시스템
    /// </summary>
    public class NewSkillLearningSystem : NetworkBehaviour
    {
        [Header("Skill Learning Settings")]
        [SerializeField] private int[] skillLearningLevels = { 1, 3, 5, 7, 10 };
        [SerializeField] private float npcInteractionRange = 3.0f;
        
        // 컴포넌트 참조
        private PlayerStatsManager statsManager;
        private EconomySystem economySystem;
        private JobData currentJobData;
        
        // 학습한 스킬 관리
        private Dictionary<int, SkillData> learnedSkills = new Dictionary<int, SkillData>();
        private Dictionary<int, int> skillChoices = new Dictionary<int, int>(); // level -> choice index (0,1,2)
        
        // 네트워크 동기화
        private NetworkVariable<LearnedSkillsData> networkLearnedSkills = new NetworkVariable<LearnedSkillsData>(
            new LearnedSkillsData(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        // 이벤트
        public System.Action<int, SkillChoice> OnSkillChoicesAvailable;        // 레벨, 선택지들
        public System.Action<int, SkillData> OnSkillLearned;                  // 레벨, 배운 스킬
        public System.Action<string> OnSkillLearningError;                    // 오류 메시지
        public System.Action OnSkillsUpdated;                                 // 스킬 목록 갱신
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            statsManager = GetComponent<PlayerStatsManager>();
            economySystem = GetComponent<EconomySystem>();
            
            // 네트워크 동기화 이벤트
            networkLearnedSkills.OnValueChanged += OnNetworkSkillsChanged;
            
            // 레벨업 이벤트 구독
            if (statsManager != null)
            {
                statsManager.OnLevelUp += OnPlayerLevelUp;
            }
            
            // 현재 직업 데이터 로드
            LoadCurrentJobData();
        }
        
        private void OnPlayerLevelUp(int newLevel)
        {
            // 스킬 학습 가능 레벨인지 확인
            if (skillLearningLevels.Contains(newLevel))
            {
                CheckForAvailableSkillLearning(newLevel);
            }
        }
        
        /// <summary>
        /// 특정 레벨에서 학습 가능한 스킬이 있는지 확인
        /// </summary>
        private void CheckForAvailableSkillLearning(int level)
        {
            if (currentJobData == null) return;
            
            // 이미 해당 레벨의 스킬을 배웠는지 확인
            if (learnedSkills.ContainsKey(level))
            {
                Debug.Log($"레벨 {level} 스킬은 이미 학습했습니다.");
                return;
            }
            
            // 레벨에 맞는 스킬 선택지 가져오기
            SkillChoice skillChoice = GetSkillChoiceForLevel(level);
            if (skillChoice == null)
            {
                Debug.LogWarning($"레벨 {level}에 해당하는 스킬 선택지가 없습니다.");
                return;
            }
            
            // UI에 스킬 선택지 표시
            OnSkillChoicesAvailable?.Invoke(level, skillChoice);
            
            Debug.Log($"레벨 {level} 도달! 스킬을 배울 수 있습니다. 직업 마스터 NPC를 찾아가세요.");
        }
        
        /// <summary>
        /// NPC와 상호작용하여 스킬 학습 시도
        /// </summary>
        public void AttemptSkillLearning(int level, int choiceIndex, SkillMasterNPC npc)
        {
            if (!IsOwner) return;
            
            // NPC 거리 확인
            float distance = Vector3.Distance(transform.position, npc.transform.position);
            if (distance > npcInteractionRange)
            {
                OnSkillLearningError?.Invoke("NPC에게 더 가까이 다가가세요.");
                return;
            }
            
            // 직업 일치 확인
            if (npc.NPCJobType != currentJobData.jobType)
            {
                OnSkillLearningError?.Invoke("다른 직업의 마스터입니다.");
                return;
            }
            
            // 레벨 확인
            if (statsManager.CurrentLevel < level)
            {
                OnSkillLearningError?.Invoke($"레벨 {level}이 되어야 이 스킬을 배울 수 있습니다.");
                return;
            }
            
            // 이미 배운 스킬 확인
            if (learnedSkills.ContainsKey(level))
            {
                OnSkillLearningError?.Invoke("이미 이 레벨의 스킬을 배웠습니다.");
                return;
            }
            
            // 스킬 선택지 가져오기 및 유효성 확인
            SkillChoice skillChoice = GetSkillChoiceForLevel(level);
            if (skillChoice == null || choiceIndex < 0 || choiceIndex > 2)
            {
                OnSkillLearningError?.Invoke("유효하지 않은 스킬 선택입니다.");
                return;
            }
            
            // 선택된 스킬과 비용 확인
            SkillData selectedSkill = GetSkillFromChoice(skillChoice, choiceIndex);
            long goldCost = GetGoldCostFromChoice(skillChoice, choiceIndex);
            
            if (selectedSkill == null)
            {
                OnSkillLearningError?.Invoke("스킬 데이터를 찾을 수 없습니다.");
                return;
            }
            
            // 골드 확인
            long currentGold = statsManager.CurrentStats.CurrentGold;
            if (currentGold < goldCost)
            {
                OnSkillLearningError?.Invoke($"골드가 부족합니다. 필요: {goldCost}, 보유: {currentGold}");
                return;
            }
            
            // 스킬 학습 실행
            LearnSkillServerRpc(level, choiceIndex, goldCost);
        }
        
        [ServerRpc]
        private void LearnSkillServerRpc(int level, int choiceIndex, long goldCost)
        {
            // 서버에서도 동일한 검증 수행
            if (learnedSkills.ContainsKey(level)) return;
            if (statsManager.CurrentStats.CurrentGold < goldCost) return;
            
            SkillChoice skillChoice = GetSkillChoiceForLevel(level);
            if (skillChoice == null) return;
            
            SkillData selectedSkill = GetSkillFromChoice(skillChoice, choiceIndex);
            if (selectedSkill == null) return;
            
            // 골드 차감
            statsManager.ChangeGold(-goldCost);
            
            // 스킬 학습
            learnedSkills[level] = selectedSkill;
            skillChoices[level] = choiceIndex;
            
            // 네트워크 동기화
            SyncSkillsToNetwork();
            
            // 클라이언트에 결과 전송
            OnSkillLearnedClientRpc(level, selectedSkill.skillId);
            
            Debug.Log($"스킬 학습 완료: {selectedSkill.skillName} (레벨 {level}, 선택 {choiceIndex})");
        }
        
        [ClientRpc]
        private void OnSkillLearnedClientRpc(int level, string skillId)
        {
            // 클라이언트에서 스킬 학습 완료 이벤트 발생
            if (learnedSkills.ContainsKey(level))
            {
                OnSkillLearned?.Invoke(level, learnedSkills[level]);
                OnSkillsUpdated?.Invoke();
            }
        }
        
        /// <summary>
        /// 레벨에 따른 스킬 선택지 반환
        /// </summary>
        private SkillChoice GetSkillChoiceForLevel(int level)
        {
            if (currentJobData?.skillSet == null) return null;
            
            return level switch
            {
                1 => currentJobData.skillSet.level1Skills,
                3 => currentJobData.skillSet.level3Skills,
                5 => currentJobData.skillSet.level5Skills,
                7 => currentJobData.skillSet.level7Skills,
                10 => new SkillChoice { choiceA = currentJobData.skillSet.ultimateSkill }, // 궁극기는 선택지 없음
                _ => null
            };
        }
        
        /// <summary>
        /// 선택지에서 특정 인덱스의 스킬 반환
        /// </summary>
        private SkillData GetSkillFromChoice(SkillChoice choice, int index)
        {
            return index switch
            {
                0 => choice.choiceA,
                1 => choice.choiceB,
                2 => choice.choiceC,
                _ => null
            };
        }
        
        /// <summary>
        /// 선택지에서 특정 인덱스의 골드 비용 반환
        /// </summary>
        private long GetGoldCostFromChoice(SkillChoice choice, int index)
        {
            return index switch
            {
                0 => choice.goldCostA,
                1 => choice.goldCostB,
                2 => choice.goldCostC,
                _ => 0
            };
        }
        
        /// <summary>
        /// 현재 직업 데이터 로드
        /// </summary>
        private void LoadCurrentJobData()
        {
            // PlayerStatsManager에서 현재 직업 정보 가져오기
            if (statsManager?.CurrentJobType != null)
            {
                currentJobData = Resources.Load<JobData>($"Jobs/{statsManager.CurrentJobType}");
                if (currentJobData == null)
                {
                    Debug.LogError($"직업 데이터를 찾을 수 없습니다: {statsManager.CurrentJobType}");
                }
            }
        }
        
        /// <summary>
        /// 네트워크 동기화
        /// </summary>
        private void SyncSkillsToNetwork()
        {
            if (!IsOwner) return;
            
            LearnedSkillsData data = new LearnedSkillsData();
            data.UpdateFromDictionaries(learnedSkills, skillChoices);
            networkLearnedSkills.Value = data;
        }
        
        private void OnNetworkSkillsChanged(LearnedSkillsData previousValue, LearnedSkillsData newValue)
        {
            if (IsOwner) return;
            
            // 네트워크에서 받은 데이터로 로컬 상태 업데이트
            newValue.UpdateLocalDictionaries(ref learnedSkills, ref skillChoices);
            OnSkillsUpdated?.Invoke();
        }
        
        /// <summary>
        /// 학습한 모든 스킬 반환
        /// </summary>
        public Dictionary<int, SkillData> GetLearnedSkills()
        {
            return new Dictionary<int, SkillData>(learnedSkills);
        }
        
        /// <summary>
        /// 특정 레벨의 스킬 반환
        /// </summary>
        public SkillData GetSkillForLevel(int level)
        {
            return learnedSkills.TryGetValue(level, out SkillData skill) ? skill : null;
        }
        
        /// <summary>
        /// 현재 학습 가능한 스킬 레벨들 반환
        /// </summary>
        public int[] GetAvailableSkillLevels()
        {
            int currentLevel = statsManager?.CurrentLevel ?? 1;
            return skillLearningLevels
                .Where(level => level <= currentLevel && !learnedSkills.ContainsKey(level))
                .ToArray();
        }
    }
    
    /// <summary>
    /// 네트워크 동기화용 학습 스킬 데이터
    /// </summary>
    [System.Serializable]
    public struct LearnedSkillsData : Unity.Netcode.INetworkSerializable
    {
        public int[] levels;
        public string[] skillIds;
        public int[] choices;
        
        public void UpdateFromDictionaries(Dictionary<int, SkillData> learnedSkills, Dictionary<int, int> skillChoices)
        {
            int count = learnedSkills.Count;
            levels = new int[count];
            skillIds = new string[count];
            choices = new int[count];
            
            int index = 0;
            foreach (var kvp in learnedSkills)
            {
                levels[index] = kvp.Key;
                skillIds[index] = kvp.Value.skillId;
                choices[index] = skillChoices.TryGetValue(kvp.Key, out int choice) ? choice : 0;
                index++;
            }
        }
        
        public void UpdateLocalDictionaries(ref Dictionary<int, SkillData> learnedSkills, ref Dictionary<int, int> skillChoices)
        {
            learnedSkills.Clear();
            skillChoices.Clear();
            
            if (levels != null && skillIds != null && choices != null)
            {
                for (int i = 0; i < levels.Length && i < skillIds.Length; i++)
                {
                    // Resources에서 스킬 데이터 로드 (실제 구현에서는 더 효율적인 방법 사용)
                    SkillData skillData = Resources.Load<SkillData>($"Skills/{skillIds[i]}");
                    if (skillData != null)
                    {
                        learnedSkills[levels[i]] = skillData;
                        if (i < choices.Length)
                        {
                            skillChoices[levels[i]] = choices[i];
                        }
                    }
                }
            }
        }
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            int levelCount = levels?.Length ?? 0;
            serializer.SerializeValue(ref levelCount);
            
            if (serializer.IsWriter && levels != null)
            {
                for (int i = 0; i < levels.Length; i++)
                {
                    int level = levels[i];
                    serializer.SerializeValue(ref level);
                }
            }
            else if (serializer.IsReader)
            {
                levels = new int[levelCount];
                for (int i = 0; i < levelCount; i++)
                {
                    serializer.SerializeValue(ref levels[i]);
                }
            }
            
            int skillIdCount = skillIds?.Length ?? 0;
            serializer.SerializeValue(ref skillIdCount);
            
            if (serializer.IsWriter && skillIds != null)
            {
                for (int i = 0; i < skillIds.Length; i++)
                {
                    var skillId = skillIds[i];
                    serializer.SerializeValue(ref skillId);
                }
            }
            else if (serializer.IsReader)
            {
                skillIds = new string[skillIdCount];
                for (int i = 0; i < skillIdCount; i++)
                {
                    serializer.SerializeValue(ref skillIds[i]);
                }
            }
            
            int choiceCount = choices?.Length ?? 0;
            serializer.SerializeValue(ref choiceCount);
            
            if (serializer.IsWriter && choices != null)
            {
                for (int i = 0; i < choices.Length; i++)
                {
                    int choice = choices[i];
                    serializer.SerializeValue(ref choice);
                }
            }
            else if (serializer.IsReader)
            {
                choices = new int[choiceCount];
                for (int i = 0; i < choiceCount; i++)
                {
                    serializer.SerializeValue(ref choices[i]);
                }
            }
        }
    }
}