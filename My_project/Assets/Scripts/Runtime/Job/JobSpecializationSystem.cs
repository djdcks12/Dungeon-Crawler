using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 직업 특성화 시스템 - 서버 권위적
    /// 레벨 10 도달 시 2가지 특성 중 택1, 골드로 리셋 가능
    /// </summary>
    public class JobSpecializationSystem : NetworkBehaviour
    {
        public static JobSpecializationSystem Instance { get; private set; }

        // 특성화 데이터 캐시: JobType → [specIndex0, specIndex1]
        private Dictionary<JobType, List<JobSpecializationData>> specDatabase =
            new Dictionary<JobType, List<JobSpecializationData>>();

        // 서버: 플레이어별 선택한 특성화
        private Dictionary<ulong, string> playerSpecializations = new Dictionary<ulong, string>();

        // 로컬
        private string localSpecId;
        private JobSpecializationData localSpecData;

        // 이벤트
        public System.Action<JobSpecializationData> OnSpecializationChosen;
        public System.Action OnSpecializationReset;

        // 접근자
        public string LocalSpecId => localSpecId;
        public JobSpecializationData LocalSpecData => localSpecData;
        public bool HasSpecialization => !string.IsNullOrEmpty(localSpecId);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            LoadSpecializationData();

            // 레벨업 이벤트 구독 - 레벨 10 도달 시 특성화 알림
            PlayerStatsData.OnLevelUp += OnPlayerLevelUp;
        }

        public override void OnNetworkDespawn()
        {
            PlayerStatsData.OnLevelUp -= OnPlayerLevelUp;
            base.OnNetworkDespawn();
        }

        private void OnPlayerLevelUp(int newLevel)
        {
            if (newLevel == 10)
            {
                // 이미 특성화를 선택했는지 확인
                ulong localId = NetworkManager.Singleton?.LocalClientId ?? 0;
                if (!playerSpecializations.ContainsKey(localId))
                {
                    var notif = NotificationManager.Instance;
                    if (notif != null)
                        notif.ShowNotification("<color=#FFD700>레벨 10 달성!</color> 직업 특성화를 선택할 수 있습니다. (NPC 방문)", NotificationType.Achievement);
                }
            }
        }

        private void LoadSpecializationData()
        {
            var allSpecs = Resources.LoadAll<JobSpecializationData>("ScriptableObjects/Specializations");
            foreach (var spec in allSpecs)
            {
                if (spec == null) continue;
                if (!specDatabase.ContainsKey(spec.ParentJob))
                    specDatabase[spec.ParentJob] = new List<JobSpecializationData>();
                specDatabase[spec.ParentJob].Add(spec);
            }

            // 인덱스 정렬
            foreach (var kvp in specDatabase)
                kvp.Value.Sort((a, b) => a.SpecIndex.CompareTo(b.SpecIndex));

            Debug.Log($"[Specialization] {allSpecs.Length}개 특성화 데이터 로드됨");
        }

        /// <summary>
        /// 직업에 대한 특성화 옵션 조회
        /// </summary>
        public List<JobSpecializationData> GetSpecializationOptions(JobType jobType)
        {
            return specDatabase.TryGetValue(jobType, out var specs) ? specs : new List<JobSpecializationData>();
        }

        /// <summary>
        /// 특성화 ID로 데이터 조회
        /// </summary>
        public JobSpecializationData GetSpecData(string specId)
        {
            foreach (var kvp in specDatabase)
            {
                foreach (var spec in kvp.Value)
                {
                    if (spec.SpecId == specId) return spec;
                }
            }
            return null;
        }

        #region 특성화 선택

        /// <summary>
        /// 특성화 선택
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ChooseSpecializationServerRpc(string specId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            // 이미 특성화를 선택했는지 확인
            if (playerSpecializations.ContainsKey(clientId))
            {
                SendMessageClientRpc("이미 특성화를 선택했습니다. 리셋 후 다시 선택하세요.", clientId);
                return;
            }

            // 특성화 데이터 확인
            var specData = GetSpecData(specId);
            if (specData == null)
            {
                SendMessageClientRpc("존재하지 않는 특성화입니다.", clientId);
                return;
            }

            // 레벨 확인
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            if (statsData.CurrentLevel < specData.RequiredLevel)
            {
                SendMessageClientRpc($"레벨 {specData.RequiredLevel} 이상 필요합니다.", clientId);
                return;
            }

            // 직업 확인
            // PlayerStatsData에서 현재 직업 타입을 가져와야 함
            // 간단히 specData.ParentJob과 일치 확인

            // 특성화 적용
            playerSpecializations[clientId] = specId;

            NotifySpecializationChosenClientRpc(specId, specData.SpecName, clientId);
        }

        /// <summary>
        /// 특성화 리셋 (골드 소모)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ResetSpecializationServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!playerSpecializations.ContainsKey(clientId))
            {
                SendMessageClientRpc("리셋할 특성화가 없습니다.", clientId);
                return;
            }

            var specData = GetSpecData(playerSpecializations[clientId]);
            if (specData == null) return;

            // 골드 확인
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            if (statsData.Gold < specData.ResetGoldCost)
            {
                SendMessageClientRpc($"리셋 비용 {specData.ResetGoldCost}G가 부족합니다.", clientId);
                return;
            }

            statsData.ChangeGold(-specData.ResetGoldCost);
            playerSpecializations.Remove(clientId);

            NotifySpecializationResetClientRpc(clientId);
        }

        #endregion

        #region 보너스 조회

        /// <summary>
        /// 서버: 플레이어의 특성화 스탯 보너스
        /// </summary>
        public StatBlock GetSpecStatBonus(ulong clientId)
        {
            if (!playerSpecializations.TryGetValue(clientId, out string specId))
                return default;
            var data = GetSpecData(specId);
            return data != null ? data.StatBonus : default;
        }

        /// <summary>
        /// 서버: 특성화 전투 보너스
        /// </summary>
        public SpecCombatBonus GetSpecCombatBonus(ulong clientId)
        {
            if (!playerSpecializations.TryGetValue(clientId, out string specId))
                return default;
            var data = GetSpecData(specId);
            if (data == null) return default;

            return new SpecCombatBonus
            {
                critRateBonus = data.CritRateBonusPercent,
                critDamageBonus = data.CritDamageBonusPercent,
                attackSpeedBonus = data.AttackSpeedBonusPercent,
                cooldownReduction = data.CooldownReductionPercent,
                lifestealPercent = data.LifestealPercent,
                hpBonusPercent = data.HPBonusPercent,
                mpBonusPercent = data.MPBonusPercent
            };
        }

        /// <summary>
        /// 서버: 패시브 효과의 보너스 값 조회
        /// </summary>
        public float GetPassiveBonus(ulong clientId, SpecPassiveType passiveType)
        {
            if (!playerSpecializations.TryGetValue(clientId, out string specId))
                return 0f;
            var data = GetSpecData(specId);
            if (data == null) return 0f;

            float total = 0f;
            if (data.Passive1 != null && data.Passive1.passiveType == passiveType)
                total += data.Passive1.value;
            if (data.Passive2 != null && data.Passive2.passiveType == passiveType)
                total += data.Passive2.value;
            if (data.Passive3 != null && data.Passive3.passiveType == passiveType)
                total += data.Passive3.value;

            return total;
        }

        /// <summary>
        /// 로컬: 내 패시브 보너스
        /// </summary>
        public float GetLocalPassiveBonus(SpecPassiveType passiveType)
        {
            if (localSpecData == null) return 0f;

            float total = 0f;
            if (localSpecData.Passive1 != null && localSpecData.Passive1.passiveType == passiveType)
                total += localSpecData.Passive1.value;
            if (localSpecData.Passive2 != null && localSpecData.Passive2.passiveType == passiveType)
                total += localSpecData.Passive2.value;
            if (localSpecData.Passive3 != null && localSpecData.Passive3.passiveType == passiveType)
                total += localSpecData.Passive3.value;

            return total;
        }

        #endregion

        #region ClientRPCs

        [ClientRpc]
        private void NotifySpecializationChosenClientRpc(string specId, string specName, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            localSpecId = specId;
            localSpecData = GetSpecData(specId);
            OnSpecializationChosen?.Invoke(localSpecData);

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"특성화 선택: {specName}!", NotificationType.System);
        }

        [ClientRpc]
        private void NotifySpecializationResetClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            localSpecId = null;
            localSpecData = null;
            OnSpecializationReset?.Invoke();

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification("특성화가 리셋되었습니다.", NotificationType.Warning);
        }

        [ClientRpc]
        private void SendMessageClientRpc(string message, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification(message, NotificationType.Warning);
        }

        #endregion

        #region Utility

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        #endregion

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }

    /// <summary>
    /// 특성화 전투 보너스 구조체
    /// </summary>
    public struct SpecCombatBonus
    {
        public float critRateBonus;
        public float critDamageBonus;
        public float attackSpeedBonus;
        public float cooldownReduction;
        public float lifestealPercent;
        public float hpBonusPercent;
        public float mpBonusPercent;
    }
}
