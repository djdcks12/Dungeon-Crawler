using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public enum FishRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }

    [System.Serializable]
    public class FishData
    {
        public string fishId;
        public string fishName;
        public FishRarity rarity;
        public int sellPrice;
        public string cookResultId;
        public string buffDescription;
        public float buffDuration;
    }

    [System.Serializable]
    public class FishingSpot
    {
        public string spotId;
        public string spotName;
        public string[] availableFishIds;
        public float[] fishWeights;
    }

    public class FishingState
    {
        public string spotId;
        public float startTime;
        public float biteTime;
        public bool hasBite;
        public string selectedFishId;
    }

    public class FishingSystem : NetworkBehaviour
    {
        public static FishingSystem Instance { get; private set; }

        // Server state
        private Dictionary<ulong, FishingState> activeFishers = new Dictionary<ulong, FishingState>();
        private Dictionary<ulong, int> playerProficiency = new Dictionary<ulong, int>();

        // Local client state
        public bool localIsFishing { get; private set; }
        public bool localHasBite { get; private set; }
        public int localProficiency { get; private set; }
        public List<string> localCaughtFish { get; private set; } = new List<string>();

        // Events
        public System.Action OnFishBite;
        public System.Action<string> OnFishCaught;
        public System.Action OnFishEscaped;

        // Data
        private Dictionary<string, FishData> allFish = new Dictionary<string, FishData>();
        private Dictionary<string, FishingSpot> fishingSpots = new Dictionary<string, FishingSpot>();

        private const float BITE_WINDOW = 3f;
        private const float MIN_WAIT = 5f;
        private const float MAX_WAIT = 15f;
        private const int MAX_PROFICIENCY = 100;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeFishData();
            InitializeFishingSpots();
        }

        public override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        private void InitializeFishData()
        {
            // Riverside fish (6)
            AddFish("rv_01", "Mudfish", FishRarity.Common, 10, "food_mudfish", "ATK+5", 300f);
            AddFish("rv_02", "Creek Trout", FishRarity.Common, 15, "food_trout", "DEF+5", 300f);
            AddFish("rv_03", "River Bass", FishRarity.Uncommon, 30, "food_bass", "ATK+10", 600f);
            AddFish("rv_04", "Golden Carp", FishRarity.Uncommon, 45, "food_carp", "DEF+10", 600f);
            AddFish("rv_05", "Silver Salmon", FishRarity.Rare, 100, "food_salmon", "ATK+15, Speed+10%", 900f);
            AddFish("rv_06", "River Dragon", FishRarity.Legendary, 300, "food_dragon_rv", "ATK+20, DEF+10, Speed+20%", 1800f);

            // Lake fish (6)
            AddFish("lk_01", "Bluegill", FishRarity.Common, 12, "food_bluegill", "DEF+5", 300f);
            AddFish("lk_02", "Perch", FishRarity.Common, 14, "food_perch", "ATK+5", 300f);
            AddFish("lk_03", "Lake Pike", FishRarity.Uncommon, 35, "food_pike", "Speed+15%", 600f);
            AddFish("lk_04", "Mirror Carp", FishRarity.Uncommon, 40, "food_mirror_carp", "DEF+10", 600f);
            AddFish("lk_05", "Jade Sturgeon", FishRarity.Rare, 120, "food_sturgeon", "DEF+15, ExpBonus+5%", 900f);
            AddFish("lk_06", "Lake Leviathan", FishRarity.Legendary, 350, "food_leviathan_lk", "DEF+20, ATK+10, ExpBonus+10%", 1800f);

            // Ocean fish (6)
            AddFish("oc_01", "Sardine", FishRarity.Common, 8, "food_sardine", "Speed+10%", 300f);
            AddFish("oc_02", "Mackerel", FishRarity.Common, 16, "food_mackerel", "ATK+5", 300f);
            AddFish("oc_03", "Tuna", FishRarity.Uncommon, 50, "food_tuna", "ATK+10, Speed+10%", 600f);
            AddFish("oc_04", "Swordfish", FishRarity.Uncommon, 55, "food_swordfish", "ATK+15", 600f);
            AddFish("oc_05", "Abyssal Eel", FishRarity.Rare, 150, "food_abyssal_eel", "ATK+15, Speed+20%", 900f);
            AddFish("oc_06", "Sea Serpent", FishRarity.Legendary, 400, "food_serpent_oc", "ATK+20, Speed+30%, ExpBonus+10%", 1800f);

            // Underground fish (6)
            AddFish("ug_01", "Cave Loach", FishRarity.Common, 11, "food_loach", "DEF+5", 300f);
            AddFish("ug_02", "Blind Catfish", FishRarity.Common, 18, "food_catfish", "DEF+8", 300f);
            AddFish("ug_03", "Crystal Tetra", FishRarity.Uncommon, 40, "food_tetra", "DEF+12, ExpBonus+5%", 600f);
            AddFish("ug_04", "Shadow Bass", FishRarity.Uncommon, 48, "food_shadow_bass", "ATK+10, DEF+8", 600f);
            AddFish("ug_05", "Magma Crab", FishRarity.Rare, 130, "food_magma_crab", "DEF+20, ATK+10", 900f);
            AddFish("ug_06", "Abyssal Wyrm", FishRarity.Legendary, 380, "food_wyrm_ug", "DEF+20, ATK+15, ExpBonus+15%", 1800f);

            // Mystic fish (6)
            AddFish("my_01", "Spirit Minnow", FishRarity.Common, 20, "food_minnow_sp", "ExpBonus+5%", 300f);
            AddFish("my_02", "Fae Guppy", FishRarity.Common, 22, "food_guppy_fae", "Speed+10%", 300f);
            AddFish("my_03", "Moonfish", FishRarity.Uncommon, 60, "food_moonfish", "ExpBonus+10%, Speed+15%", 600f);
            AddFish("my_04", "Starlight Koi", FishRarity.Uncommon, 65, "food_starlight_koi", "ATK+12, DEF+12", 600f);
            AddFish("my_05", "Void Anglerfish", FishRarity.Rare, 180, "food_void_angler", "ATK+18, ExpBonus+10%", 900f);
            AddFish("my_06", "Celestial Dragon", FishRarity.Legendary, 500, "food_celestial", "ATK+20, DEF+20, Speed+30%, ExpBonus+15%", 1800f);
        }

        private void AddFish(string id, string name, FishRarity rarity, int price, string cookId, string buffDesc, float buffDur)
        {
            allFish[id] = new FishData
            {
                fishId = id,
                fishName = name,
                rarity = rarity,
                sellPrice = price,
                cookResultId = cookId,
                buffDescription = buffDesc,
                buffDuration = buffDur
            };
        }

        private void InitializeFishingSpots()
        {
            fishingSpots["riverside"] = new FishingSpot
            {
                spotId = "riverside", spotName = "Riverside",
                availableFishIds = new[] { "rv_01", "rv_02", "rv_03", "rv_04", "rv_05", "rv_06" },
                fishWeights = new[] { 30f, 30f, 20f, 12f, 6f, 2f }
            };
            fishingSpots["lake"] = new FishingSpot
            {
                spotId = "lake", spotName = "Lake",
                availableFishIds = new[] { "lk_01", "lk_02", "lk_03", "lk_04", "lk_05", "lk_06" },
                fishWeights = new[] { 30f, 30f, 20f, 12f, 6f, 2f }
            };
            fishingSpots["ocean"] = new FishingSpot
            {
                spotId = "ocean", spotName = "Ocean",
                availableFishIds = new[] { "oc_01", "oc_02", "oc_03", "oc_04", "oc_05", "oc_06" },
                fishWeights = new[] { 30f, 30f, 20f, 12f, 6f, 2f }
            };
            fishingSpots["underground"] = new FishingSpot
            {
                spotId = "underground", spotName = "Underground",
                availableFishIds = new[] { "ug_01", "ug_02", "ug_03", "ug_04", "ug_05", "ug_06" },
                fishWeights = new[] { 30f, 30f, 20f, 12f, 6f, 2f }
            };
            fishingSpots["mystic"] = new FishingSpot
            {
                spotId = "mystic", spotName = "Mystic",
                availableFishIds = new[] { "my_01", "my_02", "my_03", "my_04", "my_05", "my_06" },
                fishWeights = new[] { 30f, 30f, 20f, 12f, 6f, 2f }
            };
        }

        private void Update()
        {
            if (!IsServer) return;

            float currentTime = Time.time;
            var clientsToNotify = new List<ulong>();

            foreach (var kvp in activeFishers)
            {
                ulong clientId = kvp.Key;
                FishingState state = kvp.Value;

                if (!state.hasBite && currentTime >= state.biteTime)
                {
                    state.hasBite = true;
                    clientsToNotify.Add(clientId);
                }
                else if (state.hasBite && currentTime >= state.biteTime + BITE_WINDOW)
                {
                    NotifyFishEscapedClientRpc(clientId);
                    state.hasBite = false;
                    float waitTime = UnityEngine.Random.Range(MIN_WAIT, MAX_WAIT);
                    state.biteTime = currentTime + waitTime;
                    state.selectedFishId = SelectRandomFish(state.spotId, clientId);
                }
            }

            foreach (ulong clientId in clientsToNotify)
            {
                NotifyBiteClientRpc(clientId);
            }
        }

        private string SelectRandomFish(string spotId, ulong clientId)
        {
            if (!fishingSpots.ContainsKey(spotId)) return null;
            FishingSpot spot = fishingSpots[spotId];

            int proficiency = playerProficiency.ContainsKey(clientId) ? playerProficiency[clientId] : 0;
            float rarityBonus = proficiency / (float)MAX_PROFICIENCY;

            float[] adjustedWeights = new float[spot.fishWeights.Length];
            for (int i = 0; i < adjustedWeights.Length; i++)
            {
                adjustedWeights[i] = spot.fishWeights[i];
                string fishId = spot.availableFishIds[i];
                if (allFish.ContainsKey(fishId))
                {
                    FishRarity rarity = allFish[fishId].rarity;
                    if (rarity == FishRarity.Rare) adjustedWeights[i] += rarityBonus * 8f;
                    else if (rarity == FishRarity.Legendary) adjustedWeights[i] += rarityBonus * 4f;
                }
            }

            float totalWeight = 0f;
            foreach (float w in adjustedWeights) totalWeight += w;

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < adjustedWeights.Length; i++)
            {
                cumulative += adjustedWeights[i];
                if (roll <= cumulative) return spot.availableFishIds[i];
            }

            return spot.availableFishIds[spot.availableFishIds.Length - 1];
        }

        private int GetProficiency(ulong clientId)
        {
            return playerProficiency.ContainsKey(clientId) ? playerProficiency[clientId] : 0;
        }

        private void AddProficiency(ulong clientId, int amount)
        {
            if (!playerProficiency.ContainsKey(clientId)) playerProficiency[clientId] = 0;
            playerProficiency[clientId] = Mathf.Min(playerProficiency[clientId] + amount, MAX_PROFICIENCY);
        }

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (NetworkManager.Singleton == null) return null;
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) return null;
            var client = NetworkManager.Singleton.ConnectedClients[clientId];
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        // ===================== ServerRpcs =====================

        [ServerRpc(RequireOwnership = false)]
        public void StartFishingServerRpc(string spotId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (activeFishers.ContainsKey(clientId))
            {
                Debug.LogWarning($"[FishingSystem] Client {clientId} is already fishing.");
                return;
            }

            if (!fishingSpots.ContainsKey(spotId))
            {
                Debug.LogWarning($"[FishingSystem] Invalid spot: {spotId}");
                return;
            }

            float waitTime = UnityEngine.Random.Range(MIN_WAIT, MAX_WAIT);
            string selectedFish = SelectRandomFish(spotId, clientId);

            activeFishers[clientId] = new FishingState
            {
                spotId = spotId,
                startTime = Time.time,
                biteTime = Time.time + waitTime,
                hasBite = false,
                selectedFishId = selectedFish
            };

            Debug.Log($"[FishingSystem] Client {clientId} started fishing at {spotId}. Bite in {waitTime:F1}s.");
        }

        [ServerRpc(RequireOwnership = false)]
        public void ReelInServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!activeFishers.ContainsKey(clientId))
            {
                Debug.LogWarning($"[FishingSystem] Client {clientId} is not fishing.");
                return;
            }

            FishingState state = activeFishers[clientId];

            if (!state.hasBite)
            {
                NotifyFishEscapedClientRpc(clientId);
                activeFishers.Remove(clientId);
                return;
            }

            string fishId = state.selectedFishId;
            if (fishId == null || !allFish.ContainsKey(fishId))
            {
                NotifyFishEscapedClientRpc(clientId);
                activeFishers.Remove(clientId);
                return;
            }

            FishData fish = allFish[fishId];
            int profGain = (fish.rarity >= FishRarity.Rare) ? 3 : 1;
            AddProficiency(clientId, profGain);

            NotifyCatchClientRpc(fish.fishName, (int)fish.rarity, clientId);
            Debug.Log($"[FishingSystem] Client {clientId} caught {fish.fishName} ({fish.rarity}). Prof +{profGain}");

            activeFishers.Remove(clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void CookFishServerRpc(string fishId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!allFish.ContainsKey(fishId))
            {
                Debug.LogWarning($"[FishingSystem] Invalid fish ID for cooking: {fishId}");
                return;
            }

            FishData fish = allFish[fishId];
            string foodName = fish.cookResultId;
            string buffDesc = fish.buffDescription;

            Debug.Log($"[FishingSystem] Client {clientId} cooked {fish.fishName} into {foodName}. Buff: {buffDesc} for {fish.buffDuration}s");
            NotifyCookCompleteClientRpc(foodName, buffDesc, clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void StopFishingServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (activeFishers.ContainsKey(clientId))
            {
                activeFishers.Remove(clientId);
                Debug.Log($"[FishingSystem] Client {clientId} stopped fishing.");
            }
        }

        // ===================== ClientRpcs =====================

        [ClientRpc]
        private void NotifyBiteClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localHasBite = true;
            OnFishBite?.Invoke();
            NotificationManager.Instance?.ShowNotification("Something is biting!", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyCatchClientRpc(string fishName, int rarity, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localIsFishing = false;
            localHasBite = false;
            localCaughtFish.Add(fishName);
            localProficiency = Mathf.Min(localProficiency + ((FishRarity)rarity >= FishRarity.Rare ? 3 : 1), MAX_PROFICIENCY);
            OnFishCaught?.Invoke(fishName);
            string rarityName = ((FishRarity)rarity).ToString();
            NotificationManager.Instance?.ShowNotification($"Caught a {rarityName} {fishName}!", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyFishEscapedClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localIsFishing = false;
            localHasBite = false;
            OnFishEscaped?.Invoke();
            NotificationManager.Instance?.ShowNotification("The fish escaped...", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyCookCompleteClientRpc(string foodName, string buffDesc, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager.Instance?.ShowNotification($"Cooked {foodName}! Buff: {buffDesc}", NotificationType.System);
        }

        // ===================== Public Helpers =====================

        public void RequestStartFishing(string spotId)
        {
            if (localIsFishing) return;
            localIsFishing = true;
            localHasBite = false;
            StartFishingServerRpc(spotId);
        }

        public void RequestReelIn()
        {
            if (!localIsFishing) return;
            ReelInServerRpc();
        }

        public void RequestCookFish(string fishId)
        {
            CookFishServerRpc(fishId);
        }

        public FishData GetFishData(string fishId)
        {
            return allFish.ContainsKey(fishId) ? allFish[fishId] : null;
        }

        public FishingSpot GetFishingSpot(string spotId)
        {
            return fishingSpots.ContainsKey(spotId) ? fishingSpots[spotId] : null;
        }

        public List<FishData> GetAllFishForSpot(string spotId)
        {
            if (!fishingSpots.ContainsKey(spotId)) return new List<FishData>();
            FishingSpot spot = fishingSpots[spotId];
            return spot.availableFishIds
                .Where(id => allFish.ContainsKey(id))
                .Select(id => allFish[id])
                .ToList();
        }

        public int GetLocalProficiency() => localProficiency;
        public bool IsLocalFishing() => localIsFishing;
        public bool HasLocalBite() => localHasBite;
    }
}
