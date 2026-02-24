using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 아이템 강화 시스템 - 골드 소모하여 장비 강화 (+1~+10)
    /// </summary>
    public class ItemEnhanceSystem : NetworkBehaviour
    {
        public static ItemEnhanceSystem Instance { get; private set; }

        [Header("강화 설정")]
        [SerializeField] private int maxEnhanceLevel = 10;
        [SerializeField] private long baseEnhanceCost = 100;
        [SerializeField] private float costMultiplierPerLevel = 1.8f;

        [Header("강화 확률")]
        [SerializeField] private float[] successRates = new float[]
        {
            1.0f,   // +0 → +1: 100%
            1.0f,   // +1 → +2: 100%
            1.0f,   // +2 → +3: 100%
            0.95f,  // +3 → +4: 95%
            0.90f,  // +4 → +5: 90%
            0.80f,  // +5 → +6: 80%
            0.70f,  // +6 → +7: 70%
            0.50f,  // +7 → +8: 50%
            0.35f,  // +8 → +9: 35%
            0.20f   // +9 → +10: 20%
        };

        [Header("파괴 확률")]
        [SerializeField] private int destroyStartLevel = 9;
        [SerializeField] private float destroyChanceAtMax = 0.10f;

        [Header("스탯 보너스")]
        [SerializeField] private float damagePerLevel = 0.05f;
        [SerializeField] private float defensePerLevel = 2f;

        // 이벤트
        public System.Action<ItemInstance, int, bool> OnEnhanceResult;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 강화 비용 계산
        /// </summary>
        public long CalculateEnhanceCost(int currentLevel)
        {
            if (currentLevel >= maxEnhanceLevel) return -1;
            return (long)(baseEnhanceCost * Mathf.Pow(costMultiplierPerLevel, currentLevel));
        }

        /// <summary>
        /// 강화 성공 확률 가져오기
        /// </summary>
        public float GetSuccessRate(int currentLevel)
        {
            if (currentLevel >= maxEnhanceLevel) return 0f;
            if (currentLevel < 0 || currentLevel >= successRates.Length) return 0f;
            return successRates[currentLevel];
        }

        /// <summary>
        /// 파괴 확률 가져오기
        /// </summary>
        public float GetDestroyChance(int currentLevel)
        {
            if (currentLevel < destroyStartLevel) return 0f;
            return destroyChanceAtMax;
        }

        /// <summary>
        /// 강화 시도 (ServerRpc)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void TryEnhanceItemServerRpc(int inventorySlot, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return;

            var playerObject = client.PlayerObject;
            if (playerObject == null) return;

            var statsManager = playerObject.GetComponent<PlayerStatsManager>();
            var inventoryManager = playerObject.GetComponent<InventoryManager>();
            if (statsManager == null || inventoryManager == null) return;

            // 아이템 가져오기
            var item = inventoryManager.GetItemAtSlot(inventorySlot);
            if (item == null || item.ItemData == null)
            {
                NotifyEnhanceResultClientRpc(clientId, false, "아이템을 찾을 수 없습니다.", 0);
                return;
            }

            // 장비 아이템인지 확인
            if (item.ItemData.ItemType != ItemType.Equipment)
            {
                NotifyEnhanceResultClientRpc(clientId, false, "장비 아이템만 강화 가능합니다.", 0);
                return;
            }

            int currentLevel = item.EnhanceLevel;
            if (currentLevel >= maxEnhanceLevel)
            {
                NotifyEnhanceResultClientRpc(clientId, false, "최대 강화 단계입니다.", currentLevel);
                return;
            }

            // 비용 확인
            long cost = CalculateEnhanceCost(currentLevel);
            if (statsManager.CurrentStats.Gold < cost)
            {
                NotifyEnhanceResultClientRpc(clientId, false, $"골드 부족 (필요: {cost}G)", currentLevel);
                return;
            }

            // 골드 차감
            statsManager.ChangeGold(-cost);

            // 강화 성공 판정
            float roll = Random.value;
            float successRate = GetSuccessRate(currentLevel);

            if (roll <= successRate)
            {
                // 성공
                item.EnhanceLevel++;
                NotifyEnhanceResultClientRpc(clientId, true,
                    $"강화 성공! +{item.EnhanceLevel} (비용: {cost}G)", item.EnhanceLevel);

                if (CombatLogUI.Instance != null)
                    CombatLogUI.Instance.LogSystem($"{item.ItemData.ItemName} +{item.EnhanceLevel} 강화 성공!");

                // 시스템 알림: 강화 성공
                if (ProphecySystem.Instance != null)
                    ProphecySystem.Instance.ReportProgress(clientId, ProphecyType.Enhance, item.EnhanceLevel.ToString(), 1);
                if (CodexSystem.Instance != null)
                    CodexSystem.Instance.ReportProgress(clientId, CodexUnlockCondition.EnhanceMilestone, "", 1);
            }
            else
            {
                // 실패
                float destroyChance = GetDestroyChance(currentLevel);
                if (destroyChance > 0 && Random.value < destroyChance)
                {
                    // 파괴
                    inventoryManager.RemoveItem(inventorySlot);
                    NotifyEnhanceResultClientRpc(clientId, false,
                        $"강화 실패... 아이템이 파괴되었습니다! (비용: {cost}G)", -1);

                    if (CombatLogUI.Instance != null)
                        CombatLogUI.Instance.LogSystem($"{item.ItemData.ItemName} 강화 실패 - 아이템 파괴!");
                }
                else
                {
                    // 실패 (단계 유지)
                    NotifyEnhanceResultClientRpc(clientId, false,
                        $"강화 실패 (비용: {cost}G)", currentLevel);

                    if (CombatLogUI.Instance != null)
                        CombatLogUI.Instance.LogSystem($"{item.ItemData.ItemName} +{currentLevel} 강화 실패");
                }
            }
        }

        /// <summary>
        /// 강화 보너스 데미지 배율 계산
        /// </summary>
        public static float GetEnhanceDamageMultiplier(int enhanceLevel)
        {
            if (enhanceLevel <= 0) return 1f;
            return 1f + (enhanceLevel * 0.05f);
        }

        /// <summary>
        /// 강화 보너스 방어력 계산
        /// </summary>
        public static float GetEnhanceDefenseBonus(int enhanceLevel)
        {
            if (enhanceLevel <= 0) return 0f;
            return enhanceLevel * 2f;
        }

        /// <summary>
        /// 강화 결과 알림 ClientRpc
        /// </summary>
        [ClientRpc]
        private void NotifyEnhanceResultClientRpc(ulong targetClientId, bool success, string message, int newLevel)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            Debug.Log($"{(success ? "✅" : "❌")} Enhance: {message}");
            OnEnhanceResult?.Invoke(null, newLevel, success);
        }

        /// <summary>
        /// 강화 정보 텍스트 생성
        /// </summary>
        public string GetEnhanceInfoText(int currentLevel)
        {
            if (currentLevel >= maxEnhanceLevel)
                return "최대 강화 단계에 도달했습니다.";

            long cost = CalculateEnhanceCost(currentLevel);
            float rate = GetSuccessRate(currentLevel) * 100f;
            float destroy = GetDestroyChance(currentLevel) * 100f;

            string info = $"+{currentLevel} → +{currentLevel + 1}\n";
            info += $"비용: {cost:N0}G\n";
            info += $"성공률: {rate:F0}%\n";
            if (destroy > 0)
                info += $"파괴 확률: {destroy:F0}%\n";

            return info;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this)
                Instance = null;
        }
    }
}
