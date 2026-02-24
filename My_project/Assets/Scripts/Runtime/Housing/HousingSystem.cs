using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 플레이어 하우징 시스템
    /// 가구 배치/제거, 설비 효과 (경험치 버프, 골드 버프, 쿨다운 감소)
    /// 방문자 시스템, 골드로 가구 구매/집 확장
    /// </summary>
    public class HousingSystem : NetworkBehaviour
    {
        public static HousingSystem Instance { get; private set; }

        [Header("하우징 설정")]
        [SerializeField] private int baseSlots = 10;
        [SerializeField] private int maxSlots = 50;
        [SerializeField] private int slotsPerUpgrade = 5;
        [SerializeField] private long baseUpgradeCost = 10000;
        [SerializeField] private float upgradeCostMultiplier = 1.5f;

        // 가구 데이터베이스
        private readonly FurnitureInfo[] furnitureDatabase = new FurnitureInfo[]
        {
            // === 기본 가구 (장식) ===
            new FurnitureInfo { id = "f_table", name = "나무 테이블", category = FurnitureCategory.Decoration, cost = 500, comfort = 2 },
            new FurnitureInfo { id = "f_chair", name = "나무 의자", category = FurnitureCategory.Decoration, cost = 300, comfort = 1 },
            new FurnitureInfo { id = "f_bed", name = "침대", category = FurnitureCategory.Decoration, cost = 1500, comfort = 5 },
            new FurnitureInfo { id = "f_bookshelf", name = "책장", category = FurnitureCategory.Decoration, cost = 800, comfort = 3 },
            new FurnitureInfo { id = "f_carpet", name = "카펫", category = FurnitureCategory.Decoration, cost = 600, comfort = 2 },
            new FurnitureInfo { id = "f_fireplace", name = "벽난로", category = FurnitureCategory.Decoration, cost = 3000, comfort = 8 },
            new FurnitureInfo { id = "f_painting", name = "그림", category = FurnitureCategory.Decoration, cost = 1000, comfort = 3 },
            new FurnitureInfo { id = "f_trophy", name = "트로피 진열장", category = FurnitureCategory.Decoration, cost = 5000, comfort = 10 },
            new FurnitureInfo { id = "f_aquarium", name = "수족관", category = FurnitureCategory.Decoration, cost = 4000, comfort = 7 },
            new FurnitureInfo { id = "f_garden", name = "실내 정원", category = FurnitureCategory.Decoration, cost = 6000, comfort = 12 },

            // === 설비 (버프) ===
            new FurnitureInfo { id = "f_training_dummy", name = "훈련용 허수아비", category = FurnitureCategory.Facility,
                cost = 15000, comfort = 0, buffType = HousingBuffType.ExpBonus, buffValue = 5f,
                desc = "경험치 획득 +5%" },
            new FurnitureInfo { id = "f_enchant_table", name = "마법 부여대", category = FurnitureCategory.Facility,
                cost = 25000, comfort = 0, buffType = HousingBuffType.ExpBonus, buffValue = 10f,
                desc = "경험치 획득 +10%" },
            new FurnitureInfo { id = "f_gold_chest", name = "황금 금고", category = FurnitureCategory.Facility,
                cost = 20000, comfort = 0, buffType = HousingBuffType.GoldBonus, buffValue = 5f,
                desc = "골드 획득 +5%" },
            new FurnitureInfo { id = "f_merchant_stall", name = "상인 부스", category = FurnitureCategory.Facility,
                cost = 30000, comfort = 0, buffType = HousingBuffType.GoldBonus, buffValue = 10f,
                desc = "골드 획득 +10%" },
            new FurnitureInfo { id = "f_meditation_mat", name = "명상 매트", category = FurnitureCategory.Facility,
                cost = 18000, comfort = 0, buffType = HousingBuffType.CooldownReduction, buffValue = 3f,
                desc = "스킬 쿨다운 -3%" },
            new FurnitureInfo { id = "f_arcane_circle", name = "마법진", category = FurnitureCategory.Facility,
                cost = 35000, comfort = 0, buffType = HousingBuffType.CooldownReduction, buffValue = 7f,
                desc = "스킬 쿨다운 -7%" },
            new FurnitureInfo { id = "f_forge", name = "개인 대장간", category = FurnitureCategory.Facility,
                cost = 40000, comfort = 0, buffType = HousingBuffType.EnhanceBonus, buffValue = 5f,
                desc = "강화 성공률 +5%" },
            new FurnitureInfo { id = "f_herb_garden", name = "약초 정원", category = FurnitureCategory.Facility,
                cost = 22000, comfort = 0, buffType = HousingBuffType.HPRegen, buffValue = 1f,
                desc = "HP 자연회복 +1%/초" },
            new FurnitureInfo { id = "f_mana_well", name = "마나 우물", category = FurnitureCategory.Facility,
                cost = 22000, comfort = 0, buffType = HousingBuffType.MPRegen, buffValue = 1f,
                desc = "MP 자연회복 +1%/초" },
            new FurnitureInfo { id = "f_portal", name = "던전 포탈", category = FurnitureCategory.Facility,
                cost = 50000, comfort = 0, buffType = HousingBuffType.DropRateBonus, buffValue = 5f,
                desc = "아이템 드롭률 +5%" },

            // === 프리미엄 (고가 장식) ===
            new FurnitureInfo { id = "f_dragon_statue", name = "드래곤 석상", category = FurnitureCategory.Premium,
                cost = 100000, comfort = 25, buffType = HousingBuffType.ExpBonus, buffValue = 3f,
                desc = "경험치 +3%, 위엄 +25" },
            new FurnitureInfo { id = "f_crystal_throne", name = "수정 왕좌", category = FurnitureCategory.Premium,
                cost = 200000, comfort = 50, buffType = HousingBuffType.AllBonus, buffValue = 2f,
                desc = "모든 버프 +2%, 위엄 +50" },
            new FurnitureInfo { id = "f_world_tree", name = "세계수 묘목", category = FurnitureCategory.Premium,
                cost = 500000, comfort = 100, buffType = HousingBuffType.AllBonus, buffValue = 5f,
                desc = "모든 버프 +5%, 위엄 +100" }
        };

        // 서버: 플레이어별 하우징 데이터
        private Dictionary<ulong, PlayerHousingData> playerHousing = new Dictionary<ulong, PlayerHousingData>();

        // 로컬 캐시
        private PlayerHousingData localHousing;

        // 이벤트
        public System.Action OnHousingUpdated;
        public System.Action<string> OnFurniturePlaced;
        public System.Action<string> OnFurnitureRemoved;

        // 접근자
        public PlayerHousingData LocalHousing => localHousing;
        public FurnitureInfo[] AllFurniture => furnitureDatabase;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (localHousing == null)
            {
                localHousing = new PlayerHousingData();
                localHousing.maxSlots = baseSlots;
                LoadLocalHousing();
            }
        }

        #region 정보 조회

        /// <summary>
        /// 가구 정보 조회
        /// </summary>
        public FurnitureInfo GetFurnitureInfo(string furnitureId)
        {
            foreach (var f in furnitureDatabase)
                if (f.id == furnitureId) return f;
            return null;
        }

        /// <summary>
        /// 카테고리별 가구 목록
        /// </summary>
        public FurnitureInfo[] GetFurnitureByCategory(FurnitureCategory category)
        {
            return furnitureDatabase.Where(f => f.category == category).ToArray();
        }

        /// <summary>
        /// 현재 총 위엄 (comfort) 점수
        /// </summary>
        public int GetTotalComfort()
        {
            if (localHousing == null) return 0;
            int total = 0;
            foreach (var fId in localHousing.placedFurniture)
            {
                var info = GetFurnitureInfo(fId);
                if (info != null) total += info.comfort;
            }
            return total;
        }

        /// <summary>
        /// 하우징 버프 총합
        /// </summary>
        public HousingBuffSummary GetBuffSummary()
        {
            var summary = new HousingBuffSummary();
            if (localHousing == null) return summary;

            foreach (var fId in localHousing.placedFurniture)
            {
                var info = GetFurnitureInfo(fId);
                if (info == null || info.buffType == HousingBuffType.None) continue;

                switch (info.buffType)
                {
                    case HousingBuffType.ExpBonus:
                        summary.expBonusPercent += info.buffValue;
                        break;
                    case HousingBuffType.GoldBonus:
                        summary.goldBonusPercent += info.buffValue;
                        break;
                    case HousingBuffType.CooldownReduction:
                        summary.cooldownReductionPercent += info.buffValue;
                        break;
                    case HousingBuffType.EnhanceBonus:
                        summary.enhanceBonusPercent += info.buffValue;
                        break;
                    case HousingBuffType.HPRegen:
                        summary.hpRegenPercent += info.buffValue;
                        break;
                    case HousingBuffType.MPRegen:
                        summary.mpRegenPercent += info.buffValue;
                        break;
                    case HousingBuffType.DropRateBonus:
                        summary.dropRateBonusPercent += info.buffValue;
                        break;
                    case HousingBuffType.AllBonus:
                        summary.expBonusPercent += info.buffValue;
                        summary.goldBonusPercent += info.buffValue;
                        summary.cooldownReductionPercent += info.buffValue;
                        summary.dropRateBonusPercent += info.buffValue;
                        break;
                }
            }

            return summary;
        }

        /// <summary>
        /// 집 확장 비용
        /// </summary>
        public long GetUpgradeCost()
        {
            if (localHousing == null) return baseUpgradeCost;
            int upgradesDone = (localHousing.maxSlots - baseSlots) / slotsPerUpgrade;
            return (long)(baseUpgradeCost * Mathf.Pow(upgradeCostMultiplier, upgradesDone));
        }

        #endregion

        #region 가구 배치/제거

        /// <summary>
        /// 가구 구매 & 배치
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void PlaceFurnitureServerRpc(string furnitureId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            var info = GetFurnitureInfo(furnitureId);
            if (info == null)
            {
                SendMessageClientRpc("존재하지 않는 가구입니다.", clientId);
                return;
            }

            // 하우징 데이터 초기화
            if (!playerHousing.ContainsKey(clientId))
                playerHousing[clientId] = new PlayerHousingData { maxSlots = baseSlots };

            var housing = playerHousing[clientId];

            // 슬롯 확인
            if (housing.placedFurniture.Count >= housing.maxSlots)
            {
                SendMessageClientRpc("배치 공간이 부족합니다. 집을 확장하세요.", clientId);
                return;
            }

            // 골드 확인
            if (statsData.Gold < info.cost)
            {
                SendMessageClientRpc($"골드 부족 (필요: {info.cost:N0}G)", clientId);
                return;
            }

            // 중복 설비 확인 (같은 버프타입 2개 불가)
            if (info.category == FurnitureCategory.Facility && info.buffType != HousingBuffType.None)
            {
                foreach (var existingId in housing.placedFurniture)
                {
                    var existing = GetFurnitureInfo(existingId);
                    if (existing != null && existing.buffType == info.buffType && existing.category == FurnitureCategory.Facility)
                    {
                        SendMessageClientRpc($"같은 종류의 설비는 1개만 배치 가능합니다.", clientId);
                        return;
                    }
                }
            }

            // 구매 & 배치
            statsData.ChangeGold(-info.cost);
            housing.placedFurniture.Add(furnitureId);

            NotifyFurniturePlacedClientRpc(furnitureId, info.name, clientId);
        }

        /// <summary>
        /// 가구 제거 (50% 환불)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RemoveFurnitureServerRpc(int slotIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            if (!playerHousing.ContainsKey(clientId)) return;
            var housing = playerHousing[clientId];

            if (slotIndex < 0 || slotIndex >= housing.placedFurniture.Count)
            {
                SendMessageClientRpc("잘못된 슬롯입니다.", clientId);
                return;
            }

            string fId = housing.placedFurniture[slotIndex];
            var info = GetFurnitureInfo(fId);
            housing.placedFurniture.RemoveAt(slotIndex);

            // 50% 환불
            long refund = info != null ? info.cost / 2 : 0;
            if (refund > 0) statsData.ChangeGold(refund);

            NotifyFurnitureRemovedClientRpc(fId, refund, clientId);
        }

        /// <summary>
        /// 집 확장
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void UpgradeHouseServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            if (!playerHousing.ContainsKey(clientId))
                playerHousing[clientId] = new PlayerHousingData { maxSlots = baseSlots };

            var housing = playerHousing[clientId];

            if (housing.maxSlots >= maxSlots)
            {
                SendMessageClientRpc("최대 확장 완료입니다.", clientId);
                return;
            }

            int upgradesDone = (housing.maxSlots - baseSlots) / slotsPerUpgrade;
            long cost = (long)(baseUpgradeCost * Mathf.Pow(upgradeCostMultiplier, upgradesDone));

            if (statsData.Gold < cost)
            {
                SendMessageClientRpc($"골드 부족 (필요: {cost:N0}G)", clientId);
                return;
            }

            statsData.ChangeGold(-cost);
            housing.maxSlots += slotsPerUpgrade;

            NotifyHouseUpgradedClientRpc(housing.maxSlots, cost, clientId);
        }

        /// <summary>
        /// 동기화 요청
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestSyncServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!playerHousing.ContainsKey(clientId))
                playerHousing[clientId] = new PlayerHousingData { maxSlots = baseSlots };

            var housing = playerHousing[clientId];
            string furnitureList = string.Join(",", housing.placedFurniture);
            SyncHousingDataClientRpc(furnitureList, housing.maxSlots, clientId);
        }

        #endregion

        #region ClientRPCs

        [ClientRpc]
        private void NotifyFurniturePlacedClientRpc(string furnitureId, string name, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            if (localHousing == null) localHousing = new PlayerHousingData { maxSlots = baseSlots };
            localHousing.placedFurniture.Add(furnitureId);
            OnFurniturePlaced?.Invoke(furnitureId);
            OnHousingUpdated?.Invoke();
            SaveLocalHousing();

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"가구 배치: {name}", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyFurnitureRemovedClientRpc(string furnitureId, long refund, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            if (localHousing != null)
                localHousing.placedFurniture.Remove(furnitureId);
            OnFurnitureRemoved?.Invoke(furnitureId);
            OnHousingUpdated?.Invoke();
            SaveLocalHousing();

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"가구 제거 (환불: {refund:N0}G)", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyHouseUpgradedClientRpc(int newMaxSlots, long cost, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            if (localHousing != null) localHousing.maxSlots = newMaxSlots;
            OnHousingUpdated?.Invoke();
            SaveLocalHousing();

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"집 확장! ({newMaxSlots}칸, {cost:N0}G 소모)", NotificationType.Achievement);
        }

        [ClientRpc]
        private void SyncHousingDataClientRpc(string furnitureCSV, int slots, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            if (localHousing == null) localHousing = new PlayerHousingData();
            localHousing.maxSlots = slots;
            localHousing.placedFurniture.Clear();
            if (!string.IsNullOrEmpty(furnitureCSV))
            {
                foreach (var f in furnitureCSV.Split(','))
                    if (!string.IsNullOrEmpty(f)) localHousing.placedFurniture.Add(f);
            }
            OnHousingUpdated?.Invoke();
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

        #region 로컬 저장

        private void SaveLocalHousing()
        {
            if (localHousing == null) return;
            string json = JsonUtility.ToJson(new HousingSaveData
            {
                maxSlots = localHousing.maxSlots,
                furniture = localHousing.placedFurniture.ToArray()
            });
            PlayerPrefs.SetString("Housing_Data", json);
            PlayerPrefs.Save();
        }

        private void LoadLocalHousing()
        {
            string json = PlayerPrefs.GetString("Housing_Data", "");
            if (!string.IsNullOrEmpty(json))
            {
                var save = JsonUtility.FromJson<HousingSaveData>(json);
                if (save != null)
                {
                    localHousing.maxSlots = save.maxSlots;
                    localHousing.placedFurniture.Clear();
                    if (save.furniture != null)
                        localHousing.placedFurniture.AddRange(save.furniture);
                }
            }
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

    #region 데이터 구조체

    public enum FurnitureCategory { Decoration, Facility, Premium }
    public enum HousingBuffType { None, ExpBonus, GoldBonus, CooldownReduction, EnhanceBonus, HPRegen, MPRegen, DropRateBonus, AllBonus }

    [System.Serializable]
    public class FurnitureInfo
    {
        public string id;
        public string name;
        public FurnitureCategory category;
        public long cost;
        public int comfort;
        public HousingBuffType buffType;
        public float buffValue;
        public string desc;
    }

    public class PlayerHousingData
    {
        public int maxSlots = 10;
        public List<string> placedFurniture = new List<string>();
    }

    public struct HousingBuffSummary
    {
        public float expBonusPercent;
        public float goldBonusPercent;
        public float cooldownReductionPercent;
        public float enhanceBonusPercent;
        public float hpRegenPercent;
        public float mpRegenPercent;
        public float dropRateBonusPercent;
    }

    [System.Serializable]
    public class HousingSaveData
    {
        public int maxSlots;
        public string[] furniture;
    }

    #endregion
}
