using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 악몽 던전 시스템 - 무한 스케일링 엔드게임
    /// 키스톤으로 기존 던전을 강화, 랜덤 접사, 티어별 보상 스케일링
    /// </summary>
    public class NightmareDungeonSystem : NetworkBehaviour
    {
        public static NightmareDungeonSystem Instance { get; private set; }

        [Header("악몽 던전 설정")]
        [SerializeField] private int requiredLevel = 10;
        [SerializeField] private float hpScalePerTier = 0.15f;
        [SerializeField] private float dmgScalePerTier = 0.10f;
        [SerializeField] private float rewardScalePerTier = 0.12f;
        [SerializeField] private int maxAffixes = 4;

        // 접사 풀
        private readonly NightmareAffix[] affixPool = new NightmareAffix[]
        {
            // 몬스터 강화
            new NightmareAffix { id = "fortified", name = "강화된", desc = "몬스터 HP +30%", category = AffixCategory.MonsterBuff, hpMult = 0.30f },
            new NightmareAffix { id = "empowered", name = "강력한", desc = "몬스터 데미지 +25%", category = AffixCategory.MonsterBuff, dmgMult = 0.25f },
            new NightmareAffix { id = "shielded", name = "보호받는", desc = "몬스터 방어 +40%", category = AffixCategory.MonsterBuff, defMult = 0.40f },
            new NightmareAffix { id = "swift", name = "신속한", desc = "몬스터 이동속도 +30%", category = AffixCategory.MonsterBuff, speedMult = 0.30f },
            new NightmareAffix { id = "regenerating", name = "재생하는", desc = "몬스터 HP 재생 1%/초", category = AffixCategory.MonsterBuff, regenPercent = 0.01f },

            // 원소 강화
            new NightmareAffix { id = "fire_empowered", name = "화염 주입", desc = "몬스터 화염 데미지 +50%", category = AffixCategory.Elemental, elementType = "Fire", elementMult = 0.50f },
            new NightmareAffix { id = "ice_empowered", name = "빙결 주입", desc = "몬스터 빙결 데미지 +50%", category = AffixCategory.Elemental, elementType = "Ice", elementMult = 0.50f },
            new NightmareAffix { id = "lightning_empowered", name = "뇌전 주입", desc = "몬스터 번개 데미지 +50%", category = AffixCategory.Elemental, elementType = "Lightning", elementMult = 0.50f },
            new NightmareAffix { id = "poison_empowered", name = "독성 주입", desc = "몬스터 독 데미지 +50%", category = AffixCategory.Elemental, elementType = "Poison", elementMult = 0.50f },

            // 플레이어 약화
            new NightmareAffix { id = "cursed_ground", name = "저주받은 땅", desc = "플레이어 힐링 -30%", category = AffixCategory.PlayerDebuff, healReduction = 0.30f },
            new NightmareAffix { id = "dark_zone", name = "어둠의 영역", desc = "플레이어 시야 감소", category = AffixCategory.PlayerDebuff },
            new NightmareAffix { id = "mana_drain", name = "마나 고갈", desc = "마나 소비 +50%", category = AffixCategory.PlayerDebuff, manaCostMult = 0.50f },
            new NightmareAffix { id = "armor_break", name = "갑옷 파괴", desc = "플레이어 방어 -20%", category = AffixCategory.PlayerDebuff, defReduction = 0.20f },

            // 시간/구조
            new NightmareAffix { id = "timed", name = "시간 제한", desc = "각 층 5분 시간 제한", category = AffixCategory.Structural, timeLimit = 300f },
            new NightmareAffix { id = "no_retreat", name = "퇴각 불가", desc = "이전 층 이동 불가", category = AffixCategory.Structural },
            new NightmareAffix { id = "extra_elite", name = "엘리트 증가", desc = "엘리트 몬스터 2배", category = AffixCategory.Structural, eliteMult = 2f },
            new NightmareAffix { id = "death_penalty", name = "사망 페널티", desc = "사망 시 층 진행도 초기화", category = AffixCategory.Structural },

            // 보상 증가 (긍정)
            new NightmareAffix { id = "bountiful", name = "풍요로운", desc = "골드 드롭 +50%", category = AffixCategory.Reward, goldBonus = 0.50f },
            new NightmareAffix { id = "lucky", name = "행운의", desc = "아이템 드롭률 +30%", category = AffixCategory.Reward, dropBonus = 0.30f },
            new NightmareAffix { id = "enlightened", name = "깨달음의", desc = "경험치 +40%", category = AffixCategory.Reward, expBonus = 0.40f },
        };

        // 서버 데이터
        private Dictionary<ulong, NightmareRunData> activeRuns = new Dictionary<ulong, NightmareRunData>();

        // 로컬
        private NightmareRunData localRun;
        private int localHighestTier;

        // 이벤트
        public System.Action<NightmareRunData> OnRunStarted;
        public System.Action<int, bool> OnRunEnded; // tier, success
        public System.Action<int> OnFloorCleared; // floor number

        // 접근자
        public bool IsInNightmareRun => localRun != null && localRun.isActive;
        public NightmareRunData CurrentRun => localRun;
        public int HighestTierCleared => localHighestTier;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            localHighestTier = PlayerPrefs.GetInt("Nightmare_HighestTier", 0);
        }

        #region 키스톤 & 시작

        /// <summary>
        /// 악몽 키스톤 생성 (티어 + 랜덤 접사)
        /// </summary>
        public NightmareKeystone GenerateKeystone(int tier)
        {
            var keystone = new NightmareKeystone();
            keystone.tier = tier;
            keystone.dungeonIndex = Random.Range(0, 5); // 5개 던전 중 랜덤

            // 접사 수: 티어에 비례 (1~maxAffixes)
            int affixCount = Mathf.Min(1 + tier / 5, maxAffixes);
            keystone.affixes = new List<string>();

            var usedCategories = new HashSet<string>();
            int attempts = 0;
            while (keystone.affixes.Count < affixCount && attempts < 50)
            {
                var affix = affixPool[Random.Range(0, affixPool.Length)];
                if (!keystone.affixes.Contains(affix.id))
                {
                    keystone.affixes.Add(affix.id);
                }
                attempts++;
            }

            return keystone;
        }

        /// <summary>
        /// 악몽 던전 시작
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void StartNightmareRunServerRpc(int tier, int dungeonIndex, string affixCSV, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            if (statsData.CurrentLevel < requiredLevel)
            {
                SendMessageClientRpc($"레벨 {requiredLevel} 이상 필요합니다.", clientId);
                return;
            }

            if (activeRuns.ContainsKey(clientId) && activeRuns[clientId].isActive)
            {
                SendMessageClientRpc("이미 악몽 던전 진행 중입니다.", clientId);
                return;
            }

            // 입장료 (티어 × 5000G)
            long entryCost = tier * 5000L;
            if (statsData.Gold < entryCost)
            {
                SendMessageClientRpc($"골드 부족 (입장료: {entryCost:N0}G)", clientId);
                return;
            }
            statsData.ChangeGold(-entryCost);

            // 런 데이터 생성
            var run = new NightmareRunData();
            run.isActive = true;
            run.tier = tier;
            run.dungeonIndex = dungeonIndex;
            run.currentFloor = 1;
            run.maxFloor = 10;
            run.startTime = Time.time;

            // 접사 파싱
            run.activeAffixes = new List<string>();
            if (!string.IsNullOrEmpty(affixCSV))
            {
                foreach (var a in affixCSV.Split(','))
                    if (!string.IsNullOrEmpty(a)) run.activeAffixes.Add(a);
            }

            // 스케일링 계산
            run.monsterHPScale = 1f + hpScalePerTier * tier;
            run.monsterDMGScale = 1f + dmgScalePerTier * tier;
            run.rewardScale = 1f + rewardScalePerTier * tier;

            // 접사 효과 적용
            foreach (var affixId in run.activeAffixes)
            {
                var affix = GetAffix(affixId);
                if (affix == null) continue;
                run.monsterHPScale += affix.hpMult;
                run.monsterDMGScale += affix.dmgMult;
                run.rewardScale += affix.goldBonus + affix.expBonus + affix.dropBonus;
            }

            activeRuns[clientId] = run;

            string affixNames = GetAffixNamesCSV(run.activeAffixes);
            NotifyRunStartClientRpc(tier, dungeonIndex, affixNames, run.monsterHPScale, run.monsterDMGScale, run.rewardScale, clientId);
        }

        /// <summary>
        /// 층 클리어 보고
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ReportFloorClearServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!activeRuns.TryGetValue(clientId, out var run) || !run.isActive)
                return;

            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            // 층별 보상
            long floorGold = (long)(2000 * run.tier * run.rewardScale);
            long floorExp = (long)(1000 * run.tier * run.rewardScale);
            statsData.ChangeGold(floorGold);
            statsData.AddExperience(floorExp);

            run.currentFloor++;
            NotifyFloorClearClientRpc(run.currentFloor - 1, floorGold, floorExp, clientId);

            // 모든 층 클리어 확인
            if (run.currentFloor > run.maxFloor)
            {
                CompleteRun(clientId, run, true);
            }
        }

        /// <summary>
        /// 런 실패 (사망/포기)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void FailRunServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!activeRuns.TryGetValue(clientId, out var run) || !run.isActive)
                return;
            CompleteRun(clientId, run, false);
        }

        private void CompleteRun(ulong clientId, NightmareRunData run, bool success)
        {
            run.isActive = false;

            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            if (success)
            {
                // 완주 보상
                long completionGold = 10000L * run.tier * (long)run.rewardScale;
                long completionExp = 5000L * run.tier * (long)run.rewardScale;
                statsData.ChangeGold(completionGold);
                statsData.AddExperience(completionExp);

                // 다음 티어 키스톤 부여 (성공 시 +1~2 티어)
                int nextTier = run.tier + Random.Range(1, 3);

                // 시즌패스
                if (SeasonPassSystem.Instance != null)
                    SeasonPassSystem.Instance.AddSeasonExp(clientId, run.tier * 30, "nightmare_clear");

                // 업적
                if (AchievementSystem.Instance != null)
                    AchievementSystem.Instance.NotifyEvent(AchievementEvent.DungeonClear, run.tier);

                NotifyRunCompleteClientRpc(run.tier, true, completionGold, completionExp, nextTier, clientId);
            }
            else
            {
                // 실패 보상 (30% 위로금)
                long consolationGold = (long)(3000 * run.tier * run.rewardScale * 0.3f);
                statsData.ChangeGold(consolationGold);

                NotifyRunCompleteClientRpc(run.tier, false, consolationGold, 0, 0, clientId);
            }

            activeRuns.Remove(clientId);
        }

        #endregion

        #region 정보 조회

        /// <summary>
        /// 접사 정보 조회
        /// </summary>
        public NightmareAffix GetAffix(string affixId)
        {
            foreach (var a in affixPool)
                if (a.id == affixId) return a;
            return null;
        }

        /// <summary>
        /// 모든 접사 목록
        /// </summary>
        public NightmareAffix[] GetAllAffixes() => affixPool;

        /// <summary>
        /// 티어별 난이도 배율 조회
        /// </summary>
        public float GetTierHPScale(int tier) => 1f + hpScalePerTier * tier;
        public float GetTierDMGScale(int tier) => 1f + dmgScalePerTier * tier;
        public float GetTierRewardScale(int tier) => 1f + rewardScalePerTier * tier;

        private string GetAffixNamesCSV(List<string> affixIds)
        {
            var names = new List<string>();
            foreach (var id in affixIds)
            {
                var affix = GetAffix(id);
                if (affix != null) names.Add(affix.name);
            }
            return string.Join(", ", names);
        }

        #endregion

        #region ClientRPCs

        [ClientRpc]
        private void NotifyRunStartClientRpc(int tier, int dungeonIndex, string affixNames, float hpScale, float dmgScale, float rewardScale, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            localRun = new NightmareRunData
            {
                isActive = true,
                tier = tier,
                dungeonIndex = dungeonIndex,
                currentFloor = 1,
                maxFloor = 10,
                monsterHPScale = hpScale,
                monsterDMGScale = dmgScale,
                rewardScale = rewardScale,
                startTime = Time.time
            };

            OnRunStarted?.Invoke(localRun);

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"<color=#FF4444>악몽 던전 티어 {tier}</color> 시작! [{affixNames}]", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyFloorClearClientRpc(int floor, long gold, long exp, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            if (localRun != null) localRun.currentFloor = floor + 1;
            OnFloorCleared?.Invoke(floor);

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"층 {floor} 클리어! +{gold:N0}G +{exp:N0}EXP", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyRunCompleteClientRpc(int tier, bool success, long gold, long exp, int nextTier, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            if (localRun != null) localRun.isActive = false;

            if (success && tier > localHighestTier)
            {
                localHighestTier = tier;
                PlayerPrefs.SetInt("Nightmare_HighestTier", localHighestTier);
                PlayerPrefs.Save();
            }

            OnRunEnded?.Invoke(tier, success);

            var notif = NotificationManager.Instance;
            if (notif != null)
            {
                if (success)
                    notif.ShowNotification($"<color=#FFD700>악몽 티어 {tier} 완주!</color> +{gold:N0}G +{exp:N0}EXP (다음: 티어 {nextTier})", NotificationType.Achievement);
                else
                    notif.ShowNotification($"악몽 티어 {tier} 실패... (위로금: {gold:N0}G)", NotificationType.Warning);
            }
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

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }

    #region 데이터 구조체

    public enum AffixCategory { MonsterBuff, Elemental, PlayerDebuff, Structural, Reward }

    [System.Serializable]
    public class NightmareAffix
    {
        public string id;
        public string name;
        public string desc;
        public AffixCategory category;
        // 몬스터 강화
        public float hpMult;
        public float dmgMult;
        public float defMult;
        public float speedMult;
        public float regenPercent;
        // 원소
        public string elementType;
        public float elementMult;
        // 플레이어 약화
        public float healReduction;
        public float manaCostMult;
        public float defReduction;
        // 구조
        public float timeLimit;
        public float eliteMult;
        // 보상
        public float goldBonus;
        public float dropBonus;
        public float expBonus;
    }

    [System.Serializable]
    public class NightmareKeystone
    {
        public int tier;
        public int dungeonIndex;
        public List<string> affixes = new List<string>();
    }

    public class NightmareRunData
    {
        public bool isActive;
        public int tier;
        public int dungeonIndex;
        public int currentFloor;
        public int maxFloor;
        public float startTime;
        public float monsterHPScale;
        public float monsterDMGScale;
        public float rewardScale;
        public List<string> activeAffixes = new List<string>();
    }

    #endregion
}
