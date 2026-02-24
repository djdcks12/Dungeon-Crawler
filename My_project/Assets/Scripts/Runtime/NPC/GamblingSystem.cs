using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class GamblingSystem : NetworkBehaviour
    {
        public static GamblingSystem Instance { get; private set; }

        [Header("Gambling Settings")]
        [SerializeField] private int dailyPurchaseLimit = 30;
        [SerializeField] private int pityCounter = 10;

        private Dictionary<ulong, GamblerData> playerData = new Dictionary<ulong, GamblerData>();

        private static readonly GambleSlot[] gambleSlots = new GambleSlot[]
        {
            new GambleSlot("weapon", "Mystery Weapon", 50, GambleItemCategory.Weapon),
            new GambleSlot("armor_head", "Mystery Helmet", 40, GambleItemCategory.ArmorHead),
            new GambleSlot("armor_chest", "Mystery Chest", 60, GambleItemCategory.ArmorChest),
            new GambleSlot("armor_legs", "Mystery Greaves", 45, GambleItemCategory.ArmorLegs),
            new GambleSlot("armor_feet", "Mystery Boots", 35, GambleItemCategory.ArmorFeet),
            new GambleSlot("accessory", "Mystery Accessory", 55, GambleItemCategory.Accessory),
            new GambleSlot("gem", "Mystery Gem", 30, GambleItemCategory.Gem)
        };

        private static readonly float[] gradeChances = new float[]
        {
            0.60f, 0.25f, 0.10f, 0.04f, 0.01f
        };

        public override void OnNetworkSpawn()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this) Instance = null;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this)
                Instance = null;
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddBloodShardsServerRpc(int amount, ServerRpcParams rpcParams = default)
        {
            if (amount <= 0 || amount > 100) return;
            ulong clientId = rpcParams.Receive.SenderClientId;
            EnsurePlayerData(clientId);
            playerData[clientId].bloodShards += amount;
        }

        [ServerRpc(RequireOwnership = false)]
        public void GambleServerRpc(int slotIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (slotIndex < 0 || slotIndex >= gambleSlots.Length) return;

            EnsurePlayerData(clientId);
            var data = playerData[clientId];

            if (data.dailyPurchases >= dailyPurchaseLimit)
            {
                NotifyGambleResultClientRpc("Daily purchase limit reached!", -1, clientId);
                return;
            }

            int cost = gambleSlots[slotIndex].shardCost;
            if (data.bloodShards < cost)
            {
                NotifyGambleResultClientRpc("Not enough Blood Shards!", -1, clientId);
                return;
            }

            data.bloodShards -= cost;
            data.dailyPurchases++;
            data.purchasesSincePity++;

            int grade = RollGrade(data.purchasesSincePity >= pityCounter);
            if (grade >= 2) data.purchasesSincePity = 0;

            data.totalGambles++;
            if (grade == 4) data.legendaryCount++;

            playerData[clientId] = data;

            string[] gradeNames = { "Common", "Uncommon", "Rare", "Epic", "Legendary" };
            if (grade < 0 || grade >= gradeNames.Length) grade = Mathf.Clamp(grade, 0, gradeNames.Length - 1);
            string resultMsg = gradeNames[grade] + " " + gambleSlots[slotIndex].slotName + " obtained!";
            NotifyGambleResultClientRpc(resultMsg, grade, clientId);

            if (AchievementSystem.Instance != null && grade == 4)
                AchievementSystem.Instance.NotifyEvent(AchievementEvent.EnhanceSuccess, 1);
        }

        private int RollGrade(bool pityGuarantee)
        {
            if (pityGuarantee) return 2;

            float roll = Random.value;
            float cumulative = 0f;
            for (int i = 0; i < gradeChances.Length; i++)
            {
                cumulative += gradeChances[i];
                if (roll <= cumulative) return i;
            }
            return 0;
        }

        private void EnsurePlayerData(ulong clientId)
        {
            if (!playerData.ContainsKey(clientId))
                playerData[clientId] = new GamblerData();
        }

        public int GetBloodShards(ulong clientId)
        {
            if (!playerData.ContainsKey(clientId)) return 0;
            return playerData[clientId].bloodShards;
        }

        public int GetDailyPurchasesRemaining(ulong clientId)
        {
            if (!playerData.ContainsKey(clientId)) return dailyPurchaseLimit;
            return dailyPurchaseLimit - playerData[clientId].dailyPurchases;
        }

        public int GetTotalGambles(ulong clientId)
        {
            if (!playerData.ContainsKey(clientId)) return 0;
            return playerData[clientId].totalGambles;
        }

        public int GetLegendaryCount(ulong clientId)
        {
            if (!playerData.ContainsKey(clientId)) return 0;
            return playerData[clientId].legendaryCount;
        }

        public GambleSlot[] GetSlots() => gambleSlots;
        public float[] GetGradeChances() => gradeChances;

        public void ResetDailyPurchases()
        {
            if (!IsServer) return;
            foreach (var kvp in playerData)
            {
                var data = kvp.Value;
                data.dailyPurchases = 0;
                playerData[kvp.Key] = data;
            }
        }

        [ClientRpc]
        private void NotifyGambleResultClientRpc(string message, int grade, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationType type = grade >= 3 ? NotificationType.System : NotificationType.Warning;
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(message, type);
        }
    }

    public enum GambleItemCategory
    {
        Weapon, ArmorHead, ArmorChest, ArmorLegs, ArmorFeet, Accessory, Gem
    }

    [System.Serializable]
    public class GambleSlot
    {
        public string slotId;
        public string slotName;
        public int shardCost;
        public GambleItemCategory category;

        public GambleSlot(string id, string name, int cost, GambleItemCategory cat)
        {
            slotId = id;
            slotName = name;
            shardCost = cost;
            category = cat;
        }
    }

    [System.Serializable]
    public class GamblerData
    {
        public int bloodShards;
        public int dailyPurchases;
        public int purchasesSincePity;
        public int totalGambles;
        public int legendaryCount;
    }
}
