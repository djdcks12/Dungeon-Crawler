using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    #region Data Structures

    [Serializable]
    public struct TemperingAffix
    {
        public string Name;
        public float MinValue;
        public float MaxValue;
        public bool IsPercentage;

        public TemperingAffix(string name, float minValue, float maxValue, bool isPercentage)
        {
            Name = name;
            MinValue = minValue;
            MaxValue = maxValue;
            IsPercentage = isPercentage;
        }
    }

    [Serializable]
    public struct TemperingRecipe
    {
        public int Id;
        public string Name;
        public string Category;
        public List<TemperingAffix> PossibleAffixes;

        public TemperingRecipe(int id, string name, string category, List<TemperingAffix> possibleAffixes)
        {
            Id = id;
            Name = name;
            Category = category;
            PossibleAffixes = possibleAffixes;
        }
    }

    [Serializable]
    public struct TemperingSlotData
    {
        public string AffixName;
        public float AffixValue;
        public bool IsPercentage;
        public int RemainingAttempts;
        public bool IsLocked;
    }

    [Serializable]
    public struct ItemTemperingData
    {
        public string ItemId;
        public TemperingSlotData[] Slots;

        public ItemTemperingData(string itemId)
        {
            ItemId = itemId;
            Slots = new TemperingSlotData[TemperingSystem.MaxSlots];
            for (int i = 0; i < TemperingSystem.MaxSlots; i++)
            {
                Slots[i] = new TemperingSlotData
                {
                    AffixName = string.Empty,
                    AffixValue = 0f,
                    IsPercentage = false,
                    RemainingAttempts = TemperingSystem.MaxAttemptsPerSlot,
                    IsLocked = false
                };
            }
        }
    }

    #endregion

    /// <summary>
    /// 템퍼링 시스템 - 장비에 접사를 부여/재굴림
    /// D4 템퍼링 참고: 슬롯 2개, 시도 횟수 제한, 접사 랜덤 값
    /// </summary>
    public class TemperingSystem : NetworkBehaviour
    {
        public static TemperingSystem Instance { get; private set; }

        public const int MaxSlots = 2;
        public const int MaxAttemptsPerSlot = 5;

        [Header("템퍼링 비용")]
        [SerializeField] private long baseCost = 5000;
        [SerializeField] private float costMultPerAttempt = 1.5f;

        // 서버: 아이템별 템퍼링 데이터
        private Dictionary<string, ItemTemperingData> temperingData = new Dictionary<string, ItemTemperingData>();

        // 레시피 데이터베이스
        private readonly TemperingRecipe[] recipes = new TemperingRecipe[]
        {
            new TemperingRecipe(0, "공격 강화", "무기", new List<TemperingAffix>
            {
                new TemperingAffix("물리 데미지", 5f, 25f, true),
                new TemperingAffix("크리티컬 확률", 2f, 8f, true),
                new TemperingAffix("크리티컬 데미지", 5f, 20f, true),
                new TemperingAffix("공격 속도", 3f, 12f, true)
            }),
            new TemperingRecipe(1, "방어 강화", "방어구", new List<TemperingAffix>
            {
                new TemperingAffix("방어력", 10f, 50f, false),
                new TemperingAffix("마법 저항", 10f, 50f, false),
                new TemperingAffix("최대 HP", 3f, 15f, true),
                new TemperingAffix("피해 감소", 2f, 8f, true)
            }),
            new TemperingRecipe(2, "유틸리티", "모든 장비", new List<TemperingAffix>
            {
                new TemperingAffix("골드 획득", 5f, 20f, true),
                new TemperingAffix("경험치 보너스", 3f, 12f, true),
                new TemperingAffix("이동 속도", 2f, 8f, true),
                new TemperingAffix("쿨다운 감소", 2f, 10f, true)
            }),
            new TemperingRecipe(3, "원소 부여", "무기", new List<TemperingAffix>
            {
                new TemperingAffix("화염 데미지", 10f, 40f, false),
                new TemperingAffix("냉기 데미지", 10f, 40f, false),
                new TemperingAffix("번개 데미지", 10f, 40f, false),
                new TemperingAffix("독 데미지", 8f, 30f, false)
            }),
            new TemperingRecipe(4, "생존", "방어구", new List<TemperingAffix>
            {
                new TemperingAffix("HP 재생", 1f, 5f, true),
                new TemperingAffix("흡혈", 1f, 4f, true),
                new TemperingAffix("회피율", 2f, 6f, true),
                new TemperingAffix("블록율", 2f, 6f, true)
            })
        };

        // 이벤트
        public System.Action<string, int, TemperingSlotData> OnTemperingSuccess; // itemId, slotIdx, result
        public System.Action<string, int, string> OnTemperingFailed; // itemId, slotIdx, reason

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        #region 레시피 조회

        public TemperingRecipe[] GetAllRecipes() => recipes;

        public TemperingRecipe? GetRecipe(int recipeId)
        {
            if (recipeId < 0 || recipeId >= recipes.Length) return null;
            return recipes[recipeId];
        }

        #endregion

        #region 템퍼링 데이터

        public ItemTemperingData GetTemperingData(string itemId)
        {
            if (!temperingData.ContainsKey(itemId))
                temperingData[itemId] = new ItemTemperingData(itemId);
            return temperingData[itemId];
        }

        #endregion

        #region 템퍼링 실행

        [ServerRpc(RequireOwnership = false)]
        public void TemperItemServerRpc(string itemId, int slotIndex, int recipeId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            // 검증
            if (slotIndex < 0 || slotIndex >= MaxSlots)
            {
                NotifyFailClientRpc(clientId, itemId, slotIndex, "잘못된 슬롯");
                return;
            }

            var recipe = GetRecipe(recipeId);
            if (recipe == null)
            {
                NotifyFailClientRpc(clientId, itemId, slotIndex, "잘못된 레시피");
                return;
            }

            var data = GetTemperingData(itemId);
            var slot = data.Slots[slotIndex];

            if (slot.IsLocked)
            {
                NotifyFailClientRpc(clientId, itemId, slotIndex, "잠긴 슬롯");
                return;
            }

            if (slot.RemainingAttempts <= 0)
            {
                NotifyFailClientRpc(clientId, itemId, slotIndex, "시도 횟수 소진");
                return;
            }

            // 비용 계산
            int usedAttempts = MaxAttemptsPerSlot - slot.RemainingAttempts;
            long cost = (long)(baseCost * Mathf.Pow(costMultPerAttempt, usedAttempts));

            if (statsData.Gold < cost)
            {
                NotifyFailClientRpc(clientId, itemId, slotIndex, $"골드 부족 ({cost:N0}G)");
                return;
            }

            statsData.ChangeGold(-cost);

            // 랜덤 접사 선택
            var r = recipe.Value;
            var affixes = r.PossibleAffixes;
            var chosen = affixes[UnityEngine.Random.Range(0, affixes.Count)];
            float value = UnityEngine.Random.Range(chosen.MinValue, chosen.MaxValue);
            value = Mathf.Round(value * 10f) / 10f; // 소수점 1자리

            // 슬롯 갱신
            slot.AffixName = chosen.Name;
            slot.AffixValue = value;
            slot.IsPercentage = chosen.IsPercentage;
            slot.RemainingAttempts--;
            data.Slots[slotIndex] = slot;
            temperingData[itemId] = data;

            NotifySuccessClientRpc(clientId, itemId, slotIndex, chosen.Name, value, chosen.IsPercentage, slot.RemainingAttempts);

            // 시스템 알림: 제작 타입 진행
            if (ProphecySystem.Instance != null)
                ProphecySystem.Instance.ReportProgress(clientId, ProphecyType.Craft, "any", 1);
            if (CodexSystem.Instance != null)
                CodexSystem.Instance.ReportProgress(clientId, CodexUnlockCondition.CraftingMilestone, "", 1);
        }

        [ServerRpc(RequireOwnership = false)]
        public void LockSlotServerRpc(string itemId, int slotIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (slotIndex < 0 || slotIndex >= MaxSlots) return;

            var data = GetTemperingData(itemId);
            var slot = data.Slots[slotIndex];

            if (string.IsNullOrEmpty(slot.AffixName))
            {
                NotifyFailClientRpc(clientId, itemId, slotIndex, "빈 슬롯은 잠글 수 없음");
                return;
            }

            slot.IsLocked = true;
            data.Slots[slotIndex] = slot;
            temperingData[itemId] = data;

            NotifyLockClientRpc(clientId, itemId, slotIndex, true);
        }

        #endregion

        #region ClientRPCs

        [ClientRpc]
        private void NotifySuccessClientRpc(ulong targetClientId, string itemId, int slotIdx, string affixName, float value, bool isPercent, int remaining)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            var slotData = new TemperingSlotData
            {
                AffixName = affixName,
                AffixValue = value,
                IsPercentage = isPercent,
                RemainingAttempts = remaining
            };

            OnTemperingSuccess?.Invoke(itemId, slotIdx, slotData);

            string valueStr = isPercent ? $"{value:F1}%" : $"{value:F1}";
            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"<color=#00AAFF>템퍼링 성공!</color> {affixName} +{valueStr} (남은 시도: {remaining})", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyFailClientRpc(ulong targetClientId, string itemId, int slotIdx, string reason)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            OnTemperingFailed?.Invoke(itemId, slotIdx, reason);

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"템퍼링 실패: {reason}", NotificationType.Warning);
        }

        [ClientRpc]
        private void NotifyLockClientRpc(ulong targetClientId, string itemId, int slotIdx, bool locked)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"슬롯 {slotIdx + 1} 잠금 완료", NotificationType.System);
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
                OnTemperingSuccess = null;
                OnTemperingFailed = null;
                Instance = null;
            }
            base.OnDestroy();
        }
    }
}
