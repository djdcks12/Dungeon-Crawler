using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 장비 각성 시스템 - +10 장비의 최종 강화
    /// 각성 단계 1~5, 에픽+ 장비만 가능
    /// 각성 재료 + 골드 소비, 특수 발동 효과
    /// </summary>
    public class EquipmentAwakeningSystem : NetworkBehaviour
    {
        public static EquipmentAwakeningSystem Instance { get; private set; }

        [Header("각성 설정")]
        [SerializeField] private int requiredEnhanceLevel = 10;
        [SerializeField] private int maxAwakeningLevel = 5;
        [SerializeField] private ItemGrade minGradeRequired = ItemGrade.Epic;

        // 각성 단계별 데이터
        private readonly AwakeningTierData[] tierData = new AwakeningTierData[]
        {
            new AwakeningTierData { level = 1, goldCost = 50000, materialCount = 5, successRate = 0.80f,
                damageBonus = 0.10f, defenseBonus = 10f, effectName = "기본 각성", effectDesc = "데미지 +10%, 방어력 +10" },
            new AwakeningTierData { level = 2, goldCost = 100000, materialCount = 10, successRate = 0.60f,
                damageBonus = 0.20f, defenseBonus = 25f, effectName = "강화 각성", effectDesc = "데미지 +20%, 방어력 +25, 크리율 +3%" },
            new AwakeningTierData { level = 3, goldCost = 200000, materialCount = 20, successRate = 0.40f,
                damageBonus = 0.35f, defenseBonus = 45f, effectName = "상위 각성", effectDesc = "데미지 +35%, 방어력 +45, 크리율 +5%, 크리뎀 +10%" },
            new AwakeningTierData { level = 4, goldCost = 500000, materialCount = 40, successRate = 0.25f,
                damageBonus = 0.55f, defenseBonus = 70f, effectName = "초월 각성", effectDesc = "데미지 +55%, 방어력 +70, 크리율 +8%, 크리뎀 +20%, HP +10%" },
            new AwakeningTierData { level = 5, goldCost = 1000000, materialCount = 80, successRate = 0.10f,
                damageBonus = 0.80f, defenseBonus = 100f, effectName = "신성 각성", effectDesc = "데미지 +80%, 방어력 +100, 크리율 +12%, 크리뎀 +30%, HP +20%, 특수 발동" }
        };

        // 각성 발동 효과 (5각성 전용)
        private readonly AwakeningProcEffect[] procEffects = new AwakeningProcEffect[]
        {
            new AwakeningProcEffect { name = "화염 폭발", chance = 0.10f, damagePercent = 0.50f, description = "10% 확률로 화염 폭발 (공격력 50% 추가 데미지)" },
            new AwakeningProcEffect { name = "빙결 찬스", chance = 0.08f, duration = 2f, description = "8% 확률로 적 2초 동결" },
            new AwakeningProcEffect { name = "생명력 흡수", chance = 0.15f, healPercent = 0.05f, description = "15% 확률로 데미지의 5% 회복" },
            new AwakeningProcEffect { name = "방어 강화", chance = 0.12f, duration = 5f, description = "12% 확률로 5초간 방어력 2배" },
            new AwakeningProcEffect { name = "연쇄 번개", chance = 0.07f, damagePercent = 0.30f, description = "7% 확률로 주변 적에게 번개 (공격력 30%)" }
        };

        // 서버: 플레이어별 장비 각성 데이터 (inventorySlot → awakeningLevel)
        private Dictionary<ulong, Dictionary<string, int>> playerAwakenings = new Dictionary<ulong, Dictionary<string, int>>();

        // 이벤트
        public System.Action<string, int, bool> OnAwakeningResult; // instanceId, newLevel, success

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        #region 각성 정보 조회

        /// <summary>
        /// 아이템의 현재 각성 단계 조회
        /// </summary>
        public int GetAwakeningLevel(string instanceId)
        {
            ulong localId = NetworkManager.Singleton?.LocalClientId ?? 0;
            if (playerAwakenings.TryGetValue(localId, out var dict))
            {
                if (dict.TryGetValue(instanceId, out int level))
                    return level;
            }
            return 0;
        }

        /// <summary>
        /// 각성 가능 여부 확인
        /// </summary>
        public bool CanAwaken(ItemInstance item)
        {
            if (item == null || item.ItemData == null) return false;
            if (item.ItemData.ItemType != ItemType.Equipment) return false;
            if ((int)item.ItemData.Grade < (int)minGradeRequired) return false;
            if (item.EnhanceLevel < requiredEnhanceLevel) return false;

            int currentLevel = GetAwakeningLevel(item.InstanceId);
            return currentLevel < maxAwakeningLevel;
        }

        /// <summary>
        /// 다음 각성 단계 정보
        /// </summary>
        public AwakeningTierData GetNextTierData(string instanceId)
        {
            int currentLevel = GetAwakeningLevel(instanceId);
            if (currentLevel >= maxAwakeningLevel) return null;
            return tierData[currentLevel];
        }

        /// <summary>
        /// 각성 보너스 스탯 계산
        /// </summary>
        public AwakeningBonus GetAwakeningBonus(string instanceId)
        {
            int level = GetAwakeningLevel(instanceId);
            if (level <= 0) return default;

            var bonus = new AwakeningBonus();
            for (int i = 0; i < level && i < tierData.Length; i++)
            {
                bonus.damageMultiplier += tierData[i].damageBonus;
                bonus.defenseFlat += tierData[i].defenseBonus;
            }

            // 2각성 이상: 크리율
            if (level >= 2) bonus.critRateBonus = 3f + (level - 2) * 2.5f;
            // 3각성 이상: 크리뎀
            if (level >= 3) bonus.critDamageBonus = 10f + (level - 3) * 10f;
            // 4각성 이상: HP 보너스
            if (level >= 4) bonus.hpBonusPercent = 10f + (level - 4) * 10f;
            // 5각성: 발동 효과
            if (level >= 5) bonus.hasProcEffect = true;

            return bonus;
        }

        /// <summary>
        /// 5각성 발동 효과 (무기 타입에 따라 다름)
        /// </summary>
        public AwakeningProcEffect GetProcEffect(ItemInstance item)
        {
            if (item == null || item.ItemData == null) return null;
            int level = GetAwakeningLevel(item.InstanceId);
            if (level < 5) return null;

            // 무기 타입에 따라 발동 효과 배정
            var wType = item.ItemData.WeaponDamageType;
            int idx = wType == DamageType.Magical ? 4 : // 마법 → 연쇄 번개
                      wType == DamageType.Physical ? 0 : // 물리 → 화염 폭발
                      2; // 기타 → 생명력 흡수
            return procEffects[idx];
        }

        /// <summary>
        /// 각성 단계에 따른 이름 접두사
        /// </summary>
        public static string GetAwakeningPrefix(int level)
        {
            return level switch
            {
                1 => "<color=#88AAFF>★</color>",
                2 => "<color=#55FF55>★★</color>",
                3 => "<color=#FFAA00>★★★</color>",
                4 => "<color=#FF55FF>★★★★</color>",
                5 => "<color=#FF0000>★★★★★</color>",
                _ => ""
            };
        }

        #endregion

        #region 각성 실행

        /// <summary>
        /// 각성 시도 (ServerRpc)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void TryAwakeningServerRpc(int inventorySlot, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return;

            var playerObj = client.PlayerObject;
            if (playerObj == null) return;

            var statsData = playerObj.GetComponent<PlayerStatsManager>()?.CurrentStats;
            var inventory = playerObj.GetComponent<InventoryManager>();
            if (statsData == null || inventory == null) return;

            // 아이템 확인
            var item = inventory.GetItemAtSlot(inventorySlot);
            if (item == null || item.ItemData == null)
            {
                NotifyAwakeningResultClientRpc(clientId, false, "아이템을 찾을 수 없습니다.", "", 0);
                return;
            }

            // 장비 확인
            if (item.ItemData.ItemType != ItemType.Equipment)
            {
                NotifyAwakeningResultClientRpc(clientId, false, "장비 아이템만 각성 가능합니다.", "", 0);
                return;
            }

            // 등급 확인
            if ((int)item.ItemData.Grade < (int)minGradeRequired)
            {
                NotifyAwakeningResultClientRpc(clientId, false, $"{minGradeRequired} 등급 이상만 각성 가능합니다.", "", 0);
                return;
            }

            // 강화 레벨 확인
            if (item.EnhanceLevel < requiredEnhanceLevel)
            {
                NotifyAwakeningResultClientRpc(clientId, false, $"+{requiredEnhanceLevel} 이상만 각성 가능합니다.", "", 0);
                return;
            }

            // 현재 각성 단계 확인
            if (!playerAwakenings.ContainsKey(clientId))
                playerAwakenings[clientId] = new Dictionary<string, int>();

            int currentLevel = 0;
            if (playerAwakenings[clientId].ContainsKey(item.InstanceId))
                currentLevel = playerAwakenings[clientId][item.InstanceId];

            if (currentLevel >= maxAwakeningLevel)
            {
                NotifyAwakeningResultClientRpc(clientId, false, "최대 각성 단계입니다.", item.InstanceId, currentLevel);
                return;
            }

            var tier = tierData[currentLevel];

            // 골드 확인
            if (statsData.Gold < tier.goldCost)
            {
                NotifyAwakeningResultClientRpc(clientId, false, $"골드 부족 (필요: {tier.goldCost:N0}G)", item.InstanceId, currentLevel);
                return;
            }

            // 재료 확인 (각성석 아이템 소모)
            string materialId = "awakening_stone";
            int materialCount = CountMaterial(inventory, materialId);
            if (materialCount < tier.materialCount)
            {
                NotifyAwakeningResultClientRpc(clientId, false, $"각성석 부족 ({materialCount}/{tier.materialCount})", item.InstanceId, currentLevel);
                return;
            }

            // 골드 차감
            statsData.ChangeGold(-tier.goldCost);

            // 재료 소모
            ConsumeMaterial(inventory, materialId, tier.materialCount);

            // 성공 판정
            float roll = Random.value;
            if (roll <= tier.successRate)
            {
                // 성공
                currentLevel++;
                playerAwakenings[clientId][item.InstanceId] = currentLevel;

                NotifyAwakeningResultClientRpc(clientId, true,
                    $"각성 성공! {tier.effectName} (단계 {currentLevel})", item.InstanceId, currentLevel);

                if (currentLevel == maxAwakeningLevel)
                {
                    var notif = NotificationManager.Instance;
                    if (notif != null)
                        notif.ShowNotification($"<color=#FF0000>최종 각성 달성!</color> {item.ItemData.ItemName}", NotificationType.Achievement);
                }
            }
            else
            {
                // 실패 (단계 하락 없음, 재료+골드만 소모)
                NotifyAwakeningResultClientRpc(clientId, false,
                    $"각성 실패... (재료와 골드가 소모되었습니다)", item.InstanceId, currentLevel);
            }
        }

        private int CountMaterial(InventoryManager inventory, string materialId)
        {
            int count = 0;
            for (int i = 0; i < 40; i++)
            {
                var item = inventory.GetItemAtSlot(i);
                if (item != null && item.ItemId == materialId)
                    count += item.Quantity;
            }
            return count;
        }

        private void ConsumeMaterial(InventoryManager inventory, string materialId, int amount)
        {
            int remaining = amount;
            for (int i = 0; i < 40 && remaining > 0; i++)
            {
                var item = inventory.GetItemAtSlot(i);
                if (item != null && item.ItemId == materialId)
                {
                    int take = Mathf.Min(item.Quantity, remaining);
                    inventory.RemoveItem(i, take);
                    remaining -= take;
                }
            }
        }

        #endregion

        #region ClientRPC

        [ClientRpc]
        private void NotifyAwakeningResultClientRpc(ulong targetClientId, bool success, string message, string instanceId, int newLevel)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            // 로컬 캐시 업데이트
            ulong localId = NetworkManager.Singleton.LocalClientId;
            if (!playerAwakenings.ContainsKey(localId))
                playerAwakenings[localId] = new Dictionary<string, int>();

            if (!string.IsNullOrEmpty(instanceId) && newLevel > 0)
                playerAwakenings[localId][instanceId] = newLevel;

            OnAwakeningResult?.Invoke(instanceId, newLevel, success);

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification(message, success ? NotificationType.System : NotificationType.Warning);
        }

        #endregion

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }

    #region 데이터 구조체

    [System.Serializable]
    public class AwakeningTierData
    {
        public int level;
        public long goldCost;
        public int materialCount;
        public float successRate;
        public float damageBonus;
        public float defenseBonus;
        public string effectName;
        public string effectDesc;
    }

    public struct AwakeningBonus
    {
        public float damageMultiplier;
        public float defenseFlat;
        public float critRateBonus;
        public float critDamageBonus;
        public float hpBonusPercent;
        public bool hasProcEffect;
    }

    [System.Serializable]
    public class AwakeningProcEffect
    {
        public string name;
        public float chance;
        public float damagePercent;
        public float healPercent;
        public float duration;
        public string description;
    }

    #endregion
}
