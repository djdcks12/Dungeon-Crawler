using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 새로운 캐릭터 생성 시스템 매니저
    /// 3단계 선택: 종족 → 무기 타입 → 직업
    /// </summary>
    public class NewCharacterCreationManager : NetworkBehaviour
    {
        [Header("Available Data")]
        [SerializeField] private RaceData[] availableRaces; // 3개 종족 (인간, 엘프, 수인)
        [SerializeField] private JobData[] availableJobs;   // 16개 직업
        
        [Header("Creation Settings")]
        [SerializeField] private int startingGold = 100;
        [SerializeField] private int startingLevel = 1;
        
        // 생성 과정의 단계별 선택 상태
        private CharacterCreationState creationState = new CharacterCreationState();
        
        // 컴포넌트 참조
        private SoulInheritance soulInheritance;
        private CharacterSlots characterSlots;
        
        // 이벤트
        public System.Action<Race[]> OnRaceOptionsUpdated;
        public System.Action<WeaponGroup[]> OnWeaponGroupOptionsUpdated;
        public System.Action<JobData[]> OnJobOptionsUpdated;
        public System.Action<CharacterCreationData> OnCharacterCreated;
        public System.Action<string> OnCreationError;
        
        private void Awake()
        {
            soulInheritance = GetComponent<SoulInheritance>();
            characterSlots = GetComponent<CharacterSlots>();
            
            // 데이터 검증 및 로드
            ValidateCreationData();
        }
        
        /// <summary>
        /// 캐릭터 생성 시작 - 1단계: 종족 선택
        /// </summary>
        public void StartCharacterCreation()
        {
            if (characterSlots.GetAvailableSlots() <= 0)
            {
                OnCreationError?.Invoke("캐릭터 슬롯이 모두 찼습니다.");
                return;
            }
            
            // 생성 상태 초기화
            creationState.Reset();
            
            // 사용 가능한 종족 목록 제공 (기계족 제외, 3종족만)
            Race[] availableRaceTypes = availableRaces
                .Where(race => race.raceType != Race.Machina)
                .Select(race => race.raceType)
                .ToArray();
            
            OnRaceOptionsUpdated?.Invoke(availableRaceTypes);
            
            Debug.Log("캐릭터 생성 시작 - 종족을 선택하세요.");
        }
        
        /// <summary>
        /// 1단계 완료: 종족 선택 → 2단계: 무기 타입 선택
        /// </summary>
        public void SelectRace(Race selectedRace)
        {
            // 선택된 종족 유효성 검증
            var raceData = availableRaces.FirstOrDefault(r => r.raceType == selectedRace);
            if (raceData == null)
            {
                OnCreationError?.Invoke("유효하지 않은 종족입니다.");
                return;
            }
            
            creationState.selectedRace = selectedRace;
            creationState.currentStep = CharacterCreationStep.WeaponGroupSelection;
            
            // 선택된 종족에 따른 무기군 옵션 계산
            WeaponGroup[] availableWeaponGroups = WeaponTypeMapper.GetAvailableWeaponGroups(selectedRace);
            OnWeaponGroupOptionsUpdated?.Invoke(availableWeaponGroups);
            
            Debug.Log($"종족 선택 완료: {selectedRace} - 무기 타입을 선택하세요.");
        }
        
        /// <summary>
        /// 2단계 완료: 무기 타입 선택 → 3단계: 직업 선택
        /// </summary>
        public void SelectWeaponGroup(WeaponGroup selectedWeaponGroup)
        {
            // 선택한 종족이 해당 무기군을 사용할 수 있는지 확인
            if (!WeaponTypeMapper.CanRaceUseWeaponGroup(creationState.selectedRace, selectedWeaponGroup))
            {
                OnCreationError?.Invoke($"{creationState.selectedRace} 종족은 {selectedWeaponGroup} 무기군을 사용할 수 없습니다.");
                return;
            }
            
            creationState.selectedWeaponGroup = selectedWeaponGroup;
            creationState.currentStep = CharacterCreationStep.JobSelection;
            
            // 종족 + 무기군 조합에 따른 직업 옵션 계산
            JobType[] availableJobTypes = WeaponTypeMapper.GetAvailableJobTypes(
                creationState.selectedRace, 
                selectedWeaponGroup);
            
            if (availableJobTypes.Length == 0)
            {
                OnCreationError?.Invoke("해당 조합에 선택 가능한 직업이 없습니다.");
                return;
            }
            
            // JobType을 JobData로 변환 (실제 구현에서는 JobDatabase나 Resources에서 로드)
            JobData[] availableJobsForCombo = availableJobTypes
                .Select(jobType => Resources.Load<JobData>($"Jobs/{jobType}"))
                .Where(jobData => jobData != null)
                .ToArray();
            
            OnJobOptionsUpdated?.Invoke(availableJobsForCombo);
            
            Debug.Log($"무기군 선택 완료: {selectedWeaponGroup} - 선택 가능한 직업: {string.Join(", ", availableJobTypes)}");
        }
        
        /// <summary>
        /// 3단계 완료: 직업 선택 → 캐릭터 생성
        /// </summary>
        public void SelectJobAndCreateCharacter(JobData selectedJob)
        {
            if (selectedJob == null)
            {
                OnCreationError?.Invoke("유효하지 않은 직업입니다.");
                return;
            }
            
            creationState.selectedJob = selectedJob;
            
            // 최종 검증
            if (!ValidateCharacterCreation(creationState))
            {
                OnCreationError?.Invoke("캐릭터 생성 조건이 맞지 않습니다.");
                return;
            }
            
            // 캐릭터 생성 실행
            CreateCharacterWithSelection();
        }
        
        /// <summary>
        /// 캐릭터 생성 조건 검증
        /// </summary>
        private bool ValidateCharacterCreation(CharacterCreationState state)
        {
            // 모든 선택이 완료되었는지 확인
            if (state.selectedJob == null || 
                state.currentStep != CharacterCreationStep.JobSelection)
            {
                return false;
            }
            
            // 선택된 조합이 유효한지 확인 (WeaponTypeMapper 사용)
            JobType[] availableJobTypes = WeaponTypeMapper.GetAvailableJobTypes(
                state.selectedRace, state.selectedWeaponGroup);
            
            return availableJobTypes.Contains(state.selectedJob.jobType);
        }
        
        /// <summary>
        /// 최종 캐릭터 생성 실행
        /// </summary>
        private void CreateCharacterWithSelection()
        {
            var creationData = new CharacterCreationData
            {
                race = creationState.selectedRace,
                jobType = creationState.selectedJob.jobType,
                weaponGroup = creationState.selectedWeaponGroup,
                startingLevel = startingLevel,
                startingGold = startingGold,
                inheritedSouls = new SoulData[0]
            };
            
            // 캐릭터 데이터 생성 완료 이벤트
            OnCharacterCreated?.Invoke(creationData);
            
            Debug.Log($"캐릭터 생성 완료: {creationData.race} {creationData.jobType} ({creationData.weaponGroup})");
        }
        
        /// <summary>
        /// 데이터 유효성 검증
        /// </summary>
        private void ValidateCreationData()
        {
            if (availableRaces == null || availableRaces.Length == 0)
            {
                Debug.LogError("사용 가능한 종족 데이터가 없습니다!");
            }
            
            if (availableJobs == null || availableJobs.Length != 16)
            {
                Debug.LogError($"16개 직업이 설정되지 않았습니다. 현재: {availableJobs?.Length ?? 0}개");
            }
        }
        
        /// <summary>
        /// 현재 생성 상태 반환 (디버그용)
        /// </summary>
        public CharacterCreationState GetCurrentCreationState()
        {
            return creationState;
        }
    }
    
    /// <summary>
    /// 캐릭터 생성 단계
    /// </summary>
    public enum CharacterCreationStep
    {
        RaceSelection = 0,
        WeaponGroupSelection = 1,
        JobSelection = 2,
        Completed = 3
    }
    
    /// <summary>
    /// 캐릭터 생성 진행 상태
    /// </summary>
    [System.Serializable]
    public class CharacterCreationState
    {
        public CharacterCreationStep currentStep = CharacterCreationStep.RaceSelection;
        public Race selectedRace = Race.None;
        public WeaponGroup selectedWeaponGroup = WeaponGroup.SwordShield;
        public JobData selectedJob = null;
        
        public void Reset()
        {
            currentStep = CharacterCreationStep.RaceSelection;
            selectedRace = Race.None;
            selectedWeaponGroup = WeaponGroup.SwordShield;
            selectedJob = null;
        }
    }
    
    /// <summary>
    /// 최종 캐릭터 생성 데이터
    /// </summary>
    [System.Serializable]
    public class CharacterCreationData
    {
        public Race race;
        public JobType jobType;
        public WeaponGroup weaponGroup;
        public int startingLevel;
        public long startingGold;
        public SoulData[] inheritedSouls;
    }
}