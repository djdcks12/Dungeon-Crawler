using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    [Serializable]
    public class InfusionSlotData
    {
        public string affixName;
        public float affixValue;
        public bool isPercentage;
        public string sourceItemId;

        public InfusionSlotData(string affixName, float affixValue, bool isPercentage, string sourceItemId)
        {
            this.affixName = affixName;
            this.affixValue = affixValue;
            this.isPercentage = isPercentage;
            this.sourceItemId = sourceItemId;
        }
    }

    [Serializable]
    public class ItemInfusionData
    {
        public string itemId;
        public List<InfusionSlotData> infusedAffixes = new List<InfusionSlotData>();
        public int infusionCount;

        public ItemInfusionData(string itemId)
        {
            this.itemId = itemId;
            infusionCount = 0;
        }
    }

    /// <summary>
    /// Infusion System - Extract an affix from a source item and inject it into a target.
    /// Source item is destroyed. Success rate depends on grade difference.
    /// Max 2 infusions per target item.
    /// </summary>
    public class InfusionSystem : NetworkBehaviour
    {
        public static InfusionSystem Instance { get; private set; }

        public const int MaxInfusionsPerItem = 2;

        [Header("Infusion Cost")]
        [SerializeField] private long baseGoldCost = 10000;

        // Success rates based on grade difference
        private static readonly float[] SuccessRateByGradeDiff = { 0.80f, 0.50f, 0.25f, 0.10f, 0.05f };

        // Available affixes that can be extracted
        private static readonly string[] ExtractableAffixes =
        {
            "물리 데미지", "마법 데미지", "크리티컬 확률", "크리티컬 데미지",
            "공격 속도", "방어력", "마법 저항", "최대 HP",
            "피해 감소", "흡혈", "회피율", "이동 속도",
            "쿨다운 감소", "골드 획득", "경험치 보너스", "HP 재생"
        };

        // Server-side infusion data per item
        private Dictionary<string, ItemInfusionData> infusionDataMap = new Dictionary<string, ItemInfusionData>();

        // Events
        public Action<string, InfusionSlotData> OnInfusionSuccess;
        public Action<string, string> OnInfusionFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        #region Public API

        public ItemInfusionData GetInfusionData(string itemId)
        {
            if (!infusionDataMap.TryGetValue(itemId, out var data))
            {
                data = new ItemInfusionData(itemId);
                infusionDataMap[itemId] = data;
            }
            return data;
        }

        public float GetSuccessRate(int sourceGrade, int targetGrade)
        {
            int diff = Mathf.Abs(sourceGrade - targetGrade);
            if (diff >= SuccessRateByGradeDiff.Length) return 0.05f;
            return SuccessRateByGradeDiff[diff];
        }

        public long GetInfusionCost(int targetGrade)
        {
            return baseGoldCost * (1 + targetGrade);
        }

        #endregion

        #region ServerRpc

        [ServerRpc(RequireOwnership = false)]
        public void InfuseItemServerRpc(string sourceItemId, string targetItemId, int sourceGrade, int targetGrade,
            ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            // Validate infusion count
            var targetData = GetInfusionData(targetItemId);
            if (targetData.infusionCount >= MaxInfusionsPerItem)
            {
                NotifyInfusionResultClientRpc(clientId, targetItemId, false,
                    $"주입 횟수 초과 ({MaxInfusionsPerItem}/{MaxInfusionsPerItem})", "", 0f, false);
                return;
            }

            // Check gold cost
            long cost = GetInfusionCost(targetGrade);
            if (statsData.Gold < cost)
            {
                NotifyInfusionResultClientRpc(clientId, targetItemId, false,
                    $"골드 부족 (필요: {cost:N0}G)", "", 0f, false);
                return;
            }

            // Deduct cost
            statsData.ChangeGold(-cost);

            // Calculate success
            float successRate = GetSuccessRate(sourceGrade, targetGrade);
            bool success = UnityEngine.Random.value <= successRate;

            if (!success)
            {
                // Source item still destroyed on failure
                NotifyInfusionResultClientRpc(clientId, targetItemId, false,
                    $"주입 실패! (성공률: {successRate:P0}) 소스 아이템 소멸", "", 0f, false);
                NotifySourceDestroyedClientRpc(clientId, sourceItemId);
                OnInfusionFailed?.Invoke(targetItemId, "주입 실패");
                Debug.Log($"[InfusionSystem] Infusion failed for client {clientId}. Rate: {successRate:P0}");
                return;
            }

            // Pick random affix to transfer
            string affixName = ExtractableAffixes[UnityEngine.Random.Range(0, ExtractableAffixes.Length)];
            float affixValue;
            bool isPercent;

            // Generate value based on source grade
            switch (affixName)
            {
                case "물리 데미지":
                case "마법 데미지":
                    affixValue = UnityEngine.Random.Range(5f + sourceGrade * 3f, 15f + sourceGrade * 5f);
                    isPercent = true;
                    break;
                case "크리티컬 확률":
                    affixValue = UnityEngine.Random.Range(2f + sourceGrade, 6f + sourceGrade * 2f);
                    isPercent = true;
                    break;
                case "크리티컬 데미지":
                    affixValue = UnityEngine.Random.Range(5f + sourceGrade * 2f, 15f + sourceGrade * 4f);
                    isPercent = true;
                    break;
                case "방어력":
                case "마법 저항":
                    affixValue = UnityEngine.Random.Range(5f + sourceGrade * 5f, 15f + sourceGrade * 10f);
                    isPercent = false;
                    break;
                case "최대 HP":
                    affixValue = UnityEngine.Random.Range(3f + sourceGrade * 2f, 10f + sourceGrade * 4f);
                    isPercent = true;
                    break;
                default:
                    affixValue = UnityEngine.Random.Range(2f + sourceGrade, 8f + sourceGrade * 2f);
                    isPercent = true;
                    break;
            }

            affixValue = Mathf.Round(affixValue * 10f) / 10f;

            // Apply infusion
            var slot = new InfusionSlotData(affixName, affixValue, isPercent, sourceItemId);
            targetData.infusedAffixes.Add(slot);
            targetData.infusionCount++;
            infusionDataMap[targetItemId] = targetData;

            NotifyInfusionResultClientRpc(clientId, targetItemId, true,
                "주입 성공!", affixName, affixValue, isPercent);
            NotifySourceDestroyedClientRpc(clientId, sourceItemId);
            OnInfusionSuccess?.Invoke(targetItemId, slot);

            // 시스템 알림: 제작 타입 진행
            if (ProphecySystem.Instance != null)
                ProphecySystem.Instance.ReportProgress(clientId, ProphecyType.Craft, "any", 1);
            if (CodexSystem.Instance != null)
                CodexSystem.Instance.ReportProgress(clientId, CodexUnlockCondition.CraftingMilestone, "", 1);

            Debug.Log($"[InfusionSystem] Infusion success: {affixName} +{affixValue} → {targetItemId}");
        }

        #endregion

        #region ClientRpc

        [ClientRpc]
        private void NotifyInfusionResultClientRpc(ulong targetClientId, string itemId, bool success,
            string message, string affixName, float affixValue, bool isPercent)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            if (success)
            {
                string valueStr = isPercent ? $"{affixValue:F1}%" : $"{affixValue:F1}";
                NotificationManager.Instance?.ShowNotification(
                    $"<color=#AA00FF>[주입] 성공!</color> {affixName} +{valueStr}",
                    NotificationType.System);
            }
            else
            {
                NotificationManager.Instance?.ShowNotification(message, NotificationType.Warning);
            }
        }

        [ClientRpc]
        private void NotifySourceDestroyedClientRpc(ulong targetClientId, string sourceItemId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager.Instance?.ShowNotification(
                "소스 장비가 소멸되었습니다.", NotificationType.Warning);
        }

        #endregion

        #region Helpers

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (NetworkManager.Singleton == null) return null;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        #endregion
    }
}
