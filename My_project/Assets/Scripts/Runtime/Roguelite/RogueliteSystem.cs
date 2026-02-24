using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 로그라이트 모드 - 단판 런 기반 랜덤 던전
    /// 랜덤 층 구성, 층 사이 버프 선택 (3지선다)
    /// 사망 시 런 종료, 메타 화폐 → 영구 시작 보너스
    /// 뮤테이터: 런별 랜덤 변형
    /// </summary>
    public class RogueliteSystem : NetworkBehaviour
    {
        public static RogueliteSystem Instance { get; private set; }

        [Header("런 설정")]
        [SerializeField] private int maxFloors = 20;
        [SerializeField] private int buffsPerChoice = 3;
        [SerializeField] private long entryCost = 5000;
        [SerializeField] private int requiredLevel = 5;

        [Header("난이도 스케일링")]
        [SerializeField] private float hpScalePerFloor = 0.20f;
        [SerializeField] private float dmgScalePerFloor = 0.12f;
        [SerializeField] private float rewardScalePerFloor = 0.15f;

        // 런 상태
        private NetworkVariable<bool> isRunActive = new NetworkVariable<bool>(false);
        private NetworkVariable<int> currentFloor = new NetworkVariable<int>(0);
        private NetworkVariable<int> activeMutatorId = new NetworkVariable<int>(-1);

        // 서버 데이터
        private Dictionary<ulong, RogueliteRunData> playerRunData = new Dictionary<ulong, RogueliteRunData>();

        // 로컬 데이터
        private int localMetaCurrency;
        private Dictionary<string, int> localPermanentUpgrades = new Dictionary<string, int>();
        private int localBestFloor;
        private long localBestScore;

        // 버프 풀 (층 사이 선택)
        private readonly RogueliteBuff[] buffPool = new RogueliteBuff[]
        {
            // 공격 버프 (10)
            new RogueliteBuff { id = "atk_10", name = "공격력 +10%", category = BuffCategory.Attack, statType = "ATK", value = 0.10f, rarity = 0 },
            new RogueliteBuff { id = "atk_20", name = "공격력 +20%", category = BuffCategory.Attack, statType = "ATK", value = 0.20f, rarity = 1 },
            new RogueliteBuff { id = "atk_35", name = "공격력 +35%", category = BuffCategory.Attack, statType = "ATK", value = 0.35f, rarity = 2 },
            new RogueliteBuff { id = "crit_5", name = "크리율 +5%", category = BuffCategory.Attack, statType = "CRIT", value = 5f, rarity = 0 },
            new RogueliteBuff { id = "crit_10", name = "크리율 +10%", category = BuffCategory.Attack, statType = "CRIT", value = 10f, rarity = 1 },
            new RogueliteBuff { id = "critdmg_15", name = "크리뎀 +15%", category = BuffCategory.Attack, statType = "CRITDMG", value = 15f, rarity = 0 },
            new RogueliteBuff { id = "critdmg_30", name = "크리뎀 +30%", category = BuffCategory.Attack, statType = "CRITDMG", value = 30f, rarity = 1 },
            new RogueliteBuff { id = "aspd_10", name = "공격속도 +10%", category = BuffCategory.Attack, statType = "ASPD", value = 0.10f, rarity = 0 },
            new RogueliteBuff { id = "pen_10", name = "방어 관통 +10%", category = BuffCategory.Attack, statType = "PEN", value = 0.10f, rarity = 1 },
            new RogueliteBuff { id = "double_hit", name = "이중 타격 15%", category = BuffCategory.Attack, statType = "DOUBLEHIT", value = 0.15f, rarity = 2 },

            // 방어 버프 (8)
            new RogueliteBuff { id = "hp_15", name = "최대 HP +15%", category = BuffCategory.Defense, statType = "HP", value = 0.15f, rarity = 0 },
            new RogueliteBuff { id = "hp_30", name = "최대 HP +30%", category = BuffCategory.Defense, statType = "HP", value = 0.30f, rarity = 1 },
            new RogueliteBuff { id = "def_20", name = "방어력 +20%", category = BuffCategory.Defense, statType = "DEF", value = 0.20f, rarity = 0 },
            new RogueliteBuff { id = "def_40", name = "방어력 +40%", category = BuffCategory.Defense, statType = "DEF", value = 0.40f, rarity = 1 },
            new RogueliteBuff { id = "mdef_20", name = "마방 +20%", category = BuffCategory.Defense, statType = "MDEF", value = 0.20f, rarity = 0 },
            new RogueliteBuff { id = "block_8", name = "블록 +8%", category = BuffCategory.Defense, statType = "BLOCK", value = 0.08f, rarity = 1 },
            new RogueliteBuff { id = "dodge_8", name = "회피 +8%", category = BuffCategory.Defense, statType = "DODGE", value = 0.08f, rarity = 1 },
            new RogueliteBuff { id = "thorns_20", name = "반사 20%", category = BuffCategory.Defense, statType = "THORNS", value = 0.20f, rarity = 2 },

            // 유틸리티 버프 (8)
            new RogueliteBuff { id = "gold_20", name = "골드 +20%", category = BuffCategory.Utility, statType = "GOLD", value = 0.20f, rarity = 0 },
            new RogueliteBuff { id = "exp_20", name = "경험치 +20%", category = BuffCategory.Utility, statType = "EXP", value = 0.20f, rarity = 0 },
            new RogueliteBuff { id = "drop_15", name = "드롭률 +15%", category = BuffCategory.Utility, statType = "DROP", value = 0.15f, rarity = 1 },
            new RogueliteBuff { id = "heal_floor", name = "층 클리어 시 HP 10% 회복", category = BuffCategory.Utility, statType = "FLOORHEAL", value = 0.10f, rarity = 0 },
            new RogueliteBuff { id = "heal_floor_25", name = "층 클리어 시 HP 25% 회복", category = BuffCategory.Utility, statType = "FLOORHEAL", value = 0.25f, rarity = 1 },
            new RogueliteBuff { id = "meta_20", name = "메타 화폐 +20%", category = BuffCategory.Utility, statType = "META", value = 0.20f, rarity = 1 },
            new RogueliteBuff { id = "revive", name = "부활 1회 (HP 30%)", category = BuffCategory.Utility, statType = "REVIVE", value = 1f, rarity = 2 },
            new RogueliteBuff { id = "shop_discount", name = "상점 할인 25%", category = BuffCategory.Utility, statType = "DISCOUNT", value = 0.25f, rarity = 1 },

            // 특수 버프 (6)
            new RogueliteBuff { id = "vampiric", name = "흡혈 5%", category = BuffCategory.Special, statType = "VAMPIRE", value = 0.05f, rarity = 1 },
            new RogueliteBuff { id = "vampiric_10", name = "흡혈 10%", category = BuffCategory.Special, statType = "VAMPIRE", value = 0.10f, rarity = 2 },
            new RogueliteBuff { id = "explosion", name = "킬 시 폭발 (30% 광역)", category = BuffCategory.Special, statType = "EXPLOSION", value = 0.30f, rarity = 2 },
            new RogueliteBuff { id = "shield", name = "층 시작 시 보호막 (HP 15%)", category = BuffCategory.Special, statType = "SHIELD", value = 0.15f, rarity = 1 },
            new RogueliteBuff { id = "berserk", name = "HP 낮을수록 공격력↑ (최대 +50%)", category = BuffCategory.Special, statType = "BERSERK", value = 0.50f, rarity = 2 },
            new RogueliteBuff { id = "lucky", name = "레어 버프 확률 +50%", category = BuffCategory.Special, statType = "LUCKY", value = 0.50f, rarity = 1 }
        };

        // 뮤테이터 풀 (런당 1개 랜덤 적용)
        private readonly RogueliteMutator[] mutatorPool = new RogueliteMutator[]
        {
            new RogueliteMutator { id = 0, name = "폭발적 죽음", desc = "적 사망 시 주변에 폭발 데미지", rewardMult = 1.15f },
            new RogueliteMutator { id = 1, name = "회복 불가", desc = "모든 힐링 효과 50% 감소", rewardMult = 1.30f },
            new RogueliteMutator { id = 2, name = "유리 대포", desc = "데미지 +50%, 받는 데미지 +50%", rewardMult = 1.25f },
            new RogueliteMutator { id = 3, name = "거인 슬레이어", desc = "적 HP 2배, 드롭 2배", rewardMult = 1.40f },
            new RogueliteMutator { id = 4, name = "스피드 런", desc = "이동속도 +30%, 층당 시간제한 120초", rewardMult = 1.20f },
            new RogueliteMutator { id = 5, name = "엘리트 범람", desc = "일반 몬스터 없음, 전부 엘리트", rewardMult = 1.50f },
            new RogueliteMutator { id = 6, name = "보물 사냥꾼", desc = "골드/아이템 +100%, 경험치 없음", rewardMult = 1.10f },
            new RogueliteMutator { id = 7, name = "균형", desc = "모든 스탯 +10%, 쿨다운 +20%", rewardMult = 1.15f },
            new RogueliteMutator { id = 8, name = "공허의 저주", desc = "층마다 최대HP 5% 감소, 보상 2배", rewardMult = 2.00f },
            new RogueliteMutator { id = 9, name = "평화로운 날", desc = "적 약화 30%, 보상 50% 감소", rewardMult = 0.50f }
        };

        // 영구 업그레이드
        private readonly MetaUpgrade[] metaUpgrades = new MetaUpgrade[]
        {
            new MetaUpgrade { id = "start_hp", name = "시작 HP 보너스", desc = "런 시작 시 HP +5%/레벨", maxLevel = 10, costPerLevel = 50, valuePerLevel = 0.05f },
            new MetaUpgrade { id = "start_atk", name = "시작 공격력 보너스", desc = "런 시작 시 공격력 +3%/레벨", maxLevel = 10, costPerLevel = 50, valuePerLevel = 0.03f },
            new MetaUpgrade { id = "start_def", name = "시작 방어력 보너스", desc = "런 시작 시 방어력 +3%/레벨", maxLevel = 10, costPerLevel = 50, valuePerLevel = 0.03f },
            new MetaUpgrade { id = "gold_find", name = "골드 획득 보너스", desc = "런 중 골드 +5%/레벨", maxLevel = 10, costPerLevel = 40, valuePerLevel = 0.05f },
            new MetaUpgrade { id = "meta_find", name = "메타 화폐 보너스", desc = "메타 화폐 획득 +8%/레벨", maxLevel = 10, costPerLevel = 60, valuePerLevel = 0.08f },
            new MetaUpgrade { id = "rare_buff", name = "레어 버프 확률", desc = "레어 버프 등장 +5%/레벨", maxLevel = 10, costPerLevel = 80, valuePerLevel = 0.05f },
            new MetaUpgrade { id = "revive_chance", name = "자동 부활", desc = "사망 시 30% 확률로 부활 (+3%/레벨)", maxLevel = 5, costPerLevel = 200, valuePerLevel = 0.03f },
            new MetaUpgrade { id = "starting_buff", name = "시작 버프", desc = "런 시작 시 랜덤 버프 1개 보유", maxLevel = 3, costPerLevel = 300, valuePerLevel = 1f }
        };

        // 접근자
        public bool IsRunActive => isRunActive.Value;
        public int CurrentFloor => currentFloor.Value;
        public int MetaCurrency => localMetaCurrency;
        public int BestFloor => localBestFloor;
        public long BestScore => localBestScore;

        // 이벤트
        public System.Action<int> OnFloorStarted;
        public System.Action<int, long> OnRunEnded; // floor, score
        public System.Action<RogueliteBuff[]> OnBuffChoiceAvailable;
        public System.Action<RogueliteBuff> OnBuffAcquired;
        public System.Action<string> OnMutatorApplied;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            LoadMetaData();
        }

        #region 메타 데이터 저장/로드

        private void LoadMetaData()
        {
            localMetaCurrency = PlayerPrefs.GetInt("Roguelite_Meta", 0);
            localBestFloor = PlayerPrefs.GetInt("Roguelite_BestFloor", 0);
            long.TryParse(PlayerPrefs.GetString("Roguelite_BestScore", "0"), out localBestScore);

            foreach (var upgrade in metaUpgrades)
            {
                int level = PlayerPrefs.GetInt($"Roguelite_Upgrade_{upgrade.id}", 0);
                if (level > 0)
                    localPermanentUpgrades[upgrade.id] = level;
            }
        }

        private void SaveMetaData()
        {
            PlayerPrefs.SetInt("Roguelite_Meta", localMetaCurrency);
            PlayerPrefs.SetInt("Roguelite_BestFloor", localBestFloor);
            PlayerPrefs.SetString("Roguelite_BestScore", localBestScore.ToString());

            foreach (var kvp in localPermanentUpgrades)
            {
                PlayerPrefs.SetInt($"Roguelite_Upgrade_{kvp.Key}", kvp.Value);
            }
            PlayerPrefs.Save();
        }

        #endregion

        #region 런 시작/종료

        /// <summary>
        /// 로그라이트 런 시작
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void StartRunServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (isRunActive.Value)
            {
                SendMessageClientRpc("이미 런이 진행 중입니다.", clientId);
                return;
            }

            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            if (statsData.CurrentLevel < requiredLevel)
            {
                SendMessageClientRpc($"레벨 {requiredLevel} 이상에서 참여 가능합니다.", clientId);
                return;
            }

            if (statsData.Gold < entryCost)
            {
                SendMessageClientRpc($"골드 부족 (입장료: {entryCost:N0}G)", clientId);
                return;
            }

            statsData.ChangeGold(-entryCost);

            // 초기화
            playerRunData.Clear();
            isRunActive.Value = true;
            currentFloor.Value = 0;

            // 뮤테이터 랜덤 선택
            int mutIdx = Random.Range(0, mutatorPool.Length);
            activeMutatorId.Value = mutIdx;

            // 플레이어 런 데이터 초기화
            foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            {
                playerRunData[kvp.Key] = new RogueliteRunData
                {
                    acquiredBuffs = new List<string>(),
                    totalKills = 0,
                    totalGold = 0,
                    metaEarned = 0,
                    revivesLeft = 0,
                    score = 0
                };
            }

            NotifyRunStartClientRpc(mutIdx);
            AdvanceToNextFloor();
        }

        /// <summary>
        /// 런 포기
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ForfeitRunServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!isRunActive.Value) return;
            EndRun("포기");
        }

        private void EndRun(string reason)
        {
            if (!IsServer || !isRunActive.Value) return;

            int finalFloor = currentFloor.Value;
            isRunActive.Value = false;

            // 보상 계산 및 분배
            foreach (var kvp in playerRunData)
            {
                DistributeRunRewards(kvp.Key, kvp.Value, finalFloor, reason);
            }

            NotifyRunEndClientRpc(finalFloor, reason);
        }

        #endregion

        #region 층 진행

        private void AdvanceToNextFloor()
        {
            if (!IsServer) return;

            currentFloor.Value++;
            int floor = currentFloor.Value;

            if (floor > maxFloors)
            {
                EndRun("완주!");
                return;
            }

            // 층 유형 결정
            FloorType floorType = DetermineFloorType(floor);

            // 몬스터 스케일링
            float hpMult = 1f + hpScalePerFloor * (floor - 1);
            float dmgMult = 1f + dmgScalePerFloor * (floor - 1);

            // 뮤테이터 적용
            if (activeMutatorId.Value >= 0 && activeMutatorId.Value < mutatorPool.Length)
            {
                var mut = mutatorPool[activeMutatorId.Value];
                if (mut.id == 3) hpMult *= 2f; // 거인 슬레이어
                if (mut.id == 2) dmgMult *= 1.5f; // 유리 대포
            }

            NotifyFloorStartClientRpc(floor, (int)floorType, hpMult, dmgMult);
        }

        private FloorType DetermineFloorType(int floor)
        {
            // 5층마다 보스
            if (floor % 5 == 0) return FloorType.Boss;
            // 10층마다 상점 (보스 직후)
            if (floor % 5 == 1 && floor > 1) return FloorType.Shop;
            // 3층마다 이벤트 가능
            if (floor % 3 == 0) return Random.value < 0.5f ? FloorType.Event : FloorType.Combat;
            return FloorType.Combat;
        }

        /// <summary>
        /// 층 클리어 보고 (서버에서 호출)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ReportFloorClearServerRpc(int kills, long goldEarned, ServerRpcParams rpcParams = default)
        {
            if (!isRunActive.Value) return;

            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!playerRunData.ContainsKey(clientId)) return;

            var data = playerRunData[clientId];
            data.totalKills += kills;
            data.totalGold += goldEarned;
            data.score += (long)(kills * 100 + goldEarned);

            // 메타 화폐 획득 (층당 기본 5 + 킬당 1)
            int metaBase = 5 + kills;
            float metaMult = 1f;
            if (activeMutatorId.Value >= 0)
                metaMult = mutatorPool[activeMutatorId.Value].rewardMult;

            int metaEarned = Mathf.RoundToInt(metaBase * metaMult);
            data.metaEarned += metaEarned;

            // 버프 선택지 제공 (보스/상점 층 제외)
            int nextFloor = currentFloor.Value + 1;
            if (nextFloor <= maxFloors)
            {
                OfferBuffChoice(clientId);
            }
            else
            {
                AdvanceToNextFloor();
            }
        }

        /// <summary>
        /// 플레이어 사망 보고
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ReportPlayerDeathServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!isRunActive.Value) return;

            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!playerRunData.ContainsKey(clientId)) return;

            var data = playerRunData[clientId];

            // 부활 체크
            if (data.revivesLeft > 0)
            {
                data.revivesLeft--;
                NotifyReviveClientRpc(clientId, data.revivesLeft);
                return;
            }

            // 자동 부활 메타 업그레이드 체크
            float reviveChance = GetMetaUpgradeValue("revive_chance");
            if (reviveChance > 0 && Random.value <= reviveChance)
            {
                NotifyReviveClientRpc(clientId, 0);
                return;
            }

            // 런 종료
            EndRun("사망");
        }

        #endregion

        #region 버프 선택

        private void OfferBuffChoice(ulong clientId)
        {
            // 3개 랜덤 버프 선택
            var choices = new List<int>();
            var availableIndices = new List<int>();

            for (int i = 0; i < buffPool.Length; i++)
                availableIndices.Add(i);

            // 레어 버프 확률 보정
            float rareMult = 1f + GetMetaUpgradeValue("rare_buff");

            int count = Mathf.Min(buffsPerChoice, availableIndices.Count);
            for (int i = 0; i < count; i++)
            {
                int idx;
                // 레어 확률 조정
                if (Random.value < 0.15f * rareMult)
                {
                    // 레어(rarity 2) 우선
                    var rareIndices = availableIndices.FindAll(x => buffPool[x].rarity == 2);
                    if (rareIndices.Count > 0)
                    {
                        idx = rareIndices[Random.Range(0, rareIndices.Count)];
                    }
                    else
                    {
                        idx = availableIndices[Random.Range(0, availableIndices.Count)];
                    }
                }
                else if (Random.value < 0.40f)
                {
                    // 언커먼(rarity 1)
                    var uncommonIndices = availableIndices.FindAll(x => buffPool[x].rarity == 1);
                    if (uncommonIndices.Count > 0)
                    {
                        idx = uncommonIndices[Random.Range(0, uncommonIndices.Count)];
                    }
                    else
                    {
                        idx = availableIndices[Random.Range(0, availableIndices.Count)];
                    }
                }
                else
                {
                    idx = availableIndices[Random.Range(0, availableIndices.Count)];
                }

                choices.Add(idx);
                availableIndices.Remove(idx);
            }

            // 배열로 변환하여 ClientRpc 전송
            int choice0 = choices.Count > 0 ? choices[0] : -1;
            int choice1 = choices.Count > 1 ? choices[1] : -1;
            int choice2 = choices.Count > 2 ? choices[2] : -1;

            OfferBuffChoiceClientRpc(clientId, choice0, choice1, choice2);
        }

        /// <summary>
        /// 버프 선택 (클라이언트 → 서버)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SelectBuffServerRpc(int buffIndex, ServerRpcParams rpcParams = default)
        {
            if (!isRunActive.Value) return;
            if (buffIndex < 0 || buffIndex >= buffPool.Length) return;

            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!playerRunData.ContainsKey(clientId)) return;

            var buff = buffPool[buffIndex];
            var data = playerRunData[clientId];
            data.acquiredBuffs.Add(buff.id);

            // 특수 버프 처리
            if (buff.statType == "REVIVE")
                data.revivesLeft += (int)buff.value;

            NotifyBuffAcquiredClientRpc(clientId, buffIndex);

            // 다음 층으로
            AdvanceToNextFloor();
        }

        #endregion

        #region 메타 업그레이드

        /// <summary>
        /// 메타 업그레이드 구매
        /// </summary>
        public bool PurchaseMetaUpgrade(string upgradeId)
        {
            MetaUpgrade upgrade = null;
            foreach (var u in metaUpgrades)
            {
                if (u.id == upgradeId) { upgrade = u; break; }
            }
            if (upgrade == null) return false;

            int currentLevel = 0;
            localPermanentUpgrades.TryGetValue(upgradeId, out currentLevel);

            if (currentLevel >= upgrade.maxLevel) return false;

            int cost = upgrade.costPerLevel * (currentLevel + 1);
            if (localMetaCurrency < cost) return false;

            localMetaCurrency -= cost;
            localPermanentUpgrades[upgradeId] = currentLevel + 1;
            SaveMetaData();
            return true;
        }

        /// <summary>
        /// 메타 업그레이드 값 조회
        /// </summary>
        public float GetMetaUpgradeValue(string upgradeId)
        {
            if (!localPermanentUpgrades.TryGetValue(upgradeId, out int level)) return 0;

            foreach (var u in metaUpgrades)
            {
                if (u.id == upgradeId)
                    return u.valuePerLevel * level;
            }
            return 0;
        }

        /// <summary>
        /// 메타 업그레이드 목록 조회
        /// </summary>
        public MetaUpgrade[] GetMetaUpgrades() => metaUpgrades;

        /// <summary>
        /// 메타 업그레이드 현재 레벨
        /// </summary>
        public int GetMetaUpgradeLevel(string upgradeId)
        {
            localPermanentUpgrades.TryGetValue(upgradeId, out int level);
            return level;
        }

        #endregion

        #region 보상

        private void DistributeRunRewards(ulong clientId, RogueliteRunData data, int finalFloor, string reason)
        {
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            // 보상 스케일링
            float rewardMult = 1f + rewardScalePerFloor * (finalFloor - 1);
            float mutMult = 1f;
            if (activeMutatorId.Value >= 0)
                mutMult = mutatorPool[activeMutatorId.Value].rewardMult;

            long goldReward = (long)(1000L * finalFloor * rewardMult * mutMult);
            long expReward = (long)(500L * finalFloor * rewardMult);

            // 완주 보너스
            if (reason == "완주!")
            {
                goldReward *= 3;
                expReward *= 3;
                data.metaEarned *= 2;
            }

            statsData.ChangeGold(goldReward);
            statsData.AddExperience(expReward);

            // 메타 화폐 지급 (클라이언트에서 저장)
            NotifyMetaCurrencyClientRpc(clientId, data.metaEarned);

            // 업적
            if (AchievementSystem.Instance != null)
                AchievementSystem.Instance.NotifyEvent(AchievementEvent.DungeonClear, finalFloor);

            // 시즌패스
            if (SeasonPassSystem.Instance != null)
                SeasonPassSystem.Instance.AddSeasonExp(clientId, finalFloor * 15, "roguelite");

            // 리더보드
            if (LeaderboardSystem.Instance != null)
            {
                string pName = statsData.CharacterName ?? "Unknown";
                LeaderboardSystem.Instance.UpdateScore(clientId, pName, LeaderboardCategory.DungeonClear, (long)finalFloor);
            }
        }

        #endregion

        #region 뮤테이터 정보

        /// <summary>
        /// 현재 뮤테이터 정보
        /// </summary>
        public RogueliteMutator GetActiveMutator()
        {
            int idx = activeMutatorId.Value;
            if (idx < 0 || idx >= mutatorPool.Length) return null;
            return mutatorPool[idx];
        }

        /// <summary>
        /// 런 데이터 조회 (로컬 플레이어)
        /// </summary>
        public RogueliteRunData GetLocalRunData()
        {
            ulong localId = NetworkManager.Singleton?.LocalClientId ?? 0;
            playerRunData.TryGetValue(localId, out var data);
            return data;
        }

        /// <summary>
        /// 플레이어의 획득 버프 목록
        /// </summary>
        public List<RogueliteBuff> GetAcquiredBuffs()
        {
            var data = GetLocalRunData();
            if (data == null) return new List<RogueliteBuff>();

            var result = new List<RogueliteBuff>();
            foreach (var buffId in data.acquiredBuffs)
            {
                foreach (var b in buffPool)
                {
                    if (b.id == buffId) { result.Add(b); break; }
                }
            }
            return result;
        }

        /// <summary>
        /// 특정 스탯에 대한 총 버프 값 계산
        /// </summary>
        public float GetTotalBuffValue(string statType)
        {
            var buffs = GetAcquiredBuffs();
            float total = 0;
            foreach (var b in buffs)
            {
                if (b.statType == statType)
                    total += b.value;
            }
            return total;
        }

        #endregion

        #region ClientRPCs

        [ClientRpc]
        private void NotifyRunStartClientRpc(int mutatorIdx)
        {
            string mutName = mutatorIdx >= 0 && mutatorIdx < mutatorPool.Length
                ? mutatorPool[mutatorIdx].name : "없음";

            OnMutatorApplied?.Invoke(mutName);

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"<color=#AA00FF>로그라이트 런 시작!</color> 뮤테이터: {mutName}", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyFloorStartClientRpc(int floor, int floorTypeInt, float hpMult, float dmgMult)
        {
            OnFloorStarted?.Invoke(floor);

            FloorType ft = (FloorType)floorTypeInt;
            string typeStr = ft switch
            {
                FloorType.Boss => "<color=#FF0000>보스</color>",
                FloorType.Shop => "<color=#00FF00>상점</color>",
                FloorType.Event => "<color=#FFAA00>이벤트</color>",
                _ => "전투"
            };

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"{floor}층: {typeStr} (HP×{hpMult:F1}, DMG×{dmgMult:F1})", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyRunEndClientRpc(int finalFloor, string reason)
        {
            // 로컬 최고 기록 갱신
            if (finalFloor > localBestFloor)
            {
                localBestFloor = finalFloor;
                SaveMetaData();
            }

            OnRunEnded?.Invoke(finalFloor, 0);

            var notif = NotificationManager.Instance;
            if (notif != null)
            {
                string color = reason == "완주!" ? "#FFD700" : "#FF4444";
                notif.ShowNotification($"<color={color}>로그라이트 종료!</color> {finalFloor}층 ({reason})", NotificationType.Achievement);
            }
        }

        [ClientRpc]
        private void OfferBuffChoiceClientRpc(ulong targetClientId, int idx0, int idx1, int idx2)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            var choices = new List<RogueliteBuff>();
            if (idx0 >= 0 && idx0 < buffPool.Length) choices.Add(buffPool[idx0]);
            if (idx1 >= 0 && idx1 < buffPool.Length) choices.Add(buffPool[idx1]);
            if (idx2 >= 0 && idx2 < buffPool.Length) choices.Add(buffPool[idx2]);

            OnBuffChoiceAvailable?.Invoke(choices.ToArray());
        }

        [ClientRpc]
        private void NotifyBuffAcquiredClientRpc(ulong targetClientId, int buffIdx)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            if (buffIdx >= 0 && buffIdx < buffPool.Length)
            {
                var buff = buffPool[buffIdx];

                // 로컬 런 데이터에도 추가
                ulong localId = NetworkManager.Singleton.LocalClientId;
                if (playerRunData.ContainsKey(localId))
                    playerRunData[localId].acquiredBuffs.Add(buff.id);

                OnBuffAcquired?.Invoke(buff);

                var notif = NotificationManager.Instance;
                if (notif != null)
                {
                    string rarityColor = buff.rarity switch
                    {
                        2 => "#FF8800",
                        1 => "#5555FF",
                        _ => "#AAAAAA"
                    };
                    notif.ShowNotification($"<color={rarityColor}>{buff.name}</color> 획득!", NotificationType.System);
                }
            }
        }

        [ClientRpc]
        private void NotifyReviveClientRpc(ulong targetClientId, int remainingRevives)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"<color=#00FF00>부활!</color> 남은 부활: {remainingRevives}", NotificationType.Achievement);
        }

        [ClientRpc]
        private void NotifyMetaCurrencyClientRpc(ulong targetClientId, int amount)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            localMetaCurrency += amount;
            SaveMetaData();

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"메타 화폐 +{amount} (총: {localMetaCurrency})", NotificationType.System);
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
            if (Instance == this)
            {
                OnFloorStarted = null;
                OnRunEnded = null;
                OnBuffChoiceAvailable = null;
                OnBuffAcquired = null;
                OnMutatorApplied = null;
                Instance = null;
            }
            base.OnDestroy();
        }
    }

    #region 데이터 구조체

    public enum FloorType
    {
        Combat,
        Boss,
        Shop,
        Event
    }

    public enum BuffCategory
    {
        Attack,
        Defense,
        Utility,
        Special
    }

    [System.Serializable]
    public class RogueliteBuff
    {
        public string id;
        public string name;
        public BuffCategory category;
        public string statType;
        public float value;
        public int rarity; // 0=Common, 1=Uncommon, 2=Rare
    }

    [System.Serializable]
    public class RogueliteMutator
    {
        public int id;
        public string name;
        public string desc;
        public float rewardMult;
    }

    public class RogueliteRunData
    {
        public List<string> acquiredBuffs;
        public int totalKills;
        public long totalGold;
        public int metaEarned;
        public int revivesLeft;
        public long score;
    }

    [System.Serializable]
    public class MetaUpgrade
    {
        public string id;
        public string name;
        public string desc;
        public int maxLevel;
        public int costPerLevel;
        public float valuePerLevel;
    }

    #endregion
}
