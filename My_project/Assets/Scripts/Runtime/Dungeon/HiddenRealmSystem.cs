using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class HiddenRealmSystem : NetworkBehaviour
    {
        public static HiddenRealmSystem Instance { get; private set; }

        private Dictionary<ulong, HashSet<int>> discoveredRealms = new Dictionary<ulong, HashSet<int>>();
        private Dictionary<ulong, Dictionary<int, bool>> realmFirstClears = new Dictionary<ulong, Dictionary<int, bool>>();

        public System.Action OnRealmDiscovered;
        public System.Action OnRealmStateChanged;

        private NetworkVariable<int> activeRealmIndex = new NetworkVariable<int>(-1);
        private NetworkVariable<bool> isRealmActive = new NetworkVariable<bool>(false);

        private static readonly HiddenRealmData[] hiddenRealms = new HiddenRealmData[]
        {
            new HiddenRealmData(0, "Void Between Worlds", "Combine 3 Void Fragments at the Dark Altar",
                70, 300000f, "void_walker_aspect", 20000, 100000),
            new HiddenRealmData(1, "Celestial Sanctum", "Defeat 5 Pinnacle Bosses without dying",
                65, 250000f, "celestial_aspect", 15000, 80000),
            new HiddenRealmData(2, "Abyssal Depths", "Enter the hidden portal at Rift level 100+",
                75, 400000f, "abyssal_aspect", 25000, 120000),
            new HiddenRealmData(3, "Forgotten Paradise", "Collect all 8 Eden Fragments from event dungeons",
                60, 200000f, "paradise_aspect", 12000, 60000),
            new HiddenRealmData(4, "Shattered Timeline", "Use the Chrono Crystal at midnight in-game time",
                80, 500000f, "timeline_aspect", 30000, 150000),
        };

        private static readonly SecretMerchantItem[] secretShopItems = new SecretMerchantItem[]
        {
            new SecretMerchantItem("Void Essence", 50000, "Unique crafting material"),
            new SecretMerchantItem("Celestial Shard", 75000, "Upgrade legendary aspects"),
            new SecretMerchantItem("Abyssal Core", 100000, "Max-tier gem material"),
            new SecretMerchantItem("Eden Seed", 30000, "Grows into random rare material"),
            new SecretMerchantItem("Chrono Dust", 60000, "Reset weekly lockouts"),
        };

        public override void OnNetworkSpawn()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (IsClient)
            {
                isRealmActive.OnValueChanged += OnIsRealmActiveChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsClient)
            {
                isRealmActive.OnValueChanged -= OnIsRealmActiveChanged;
            }
            if (Instance == this)
            {
                OnRealmDiscovered = null;
                OnRealmStateChanged = null;
                Instance = null;
            }
            base.OnNetworkDespawn();
        }

        private void OnIsRealmActiveChanged(bool prev, bool next) => OnRealmStateChanged?.Invoke();

        [ServerRpc(RequireOwnership = false)]
        public void TryDiscoverRealmServerRpc(int realmIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (realmIndex < 0 || realmIndex >= hiddenRealms.Length) return;

            var discovered = GetOrCreateDiscovered(clientId);
            if (discovered.Contains(realmIndex))
            {
                NotifyClientRpc("Realm already discovered", clientId);
                return;
            }

            discovered.Add(realmIndex);
            var realm = hiddenRealms[realmIndex];

            RealmDiscoveredClientRpc(realmIndex, realm.name, clientId);

            if (AchievementSystem.Instance != null)
                AchievementSystem.Instance.NotifyEvent(AchievementEvent.DungeonClear, 1);

            // Collection system integration (discovery tracked via achievement)
        }

        [ServerRpc(RequireOwnership = false)]
        public void EnterHiddenRealmServerRpc(int realmIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (isRealmActive.Value) return;
            if (realmIndex < 0 || realmIndex >= hiddenRealms.Length) return;

            var discovered = GetOrCreateDiscovered(clientId);
            if (!discovered.Contains(realmIndex))
            {
                NotifyClientRpc("Realm not yet discovered", clientId);
                return;
            }

            var realm = hiddenRealms[realmIndex];
            var stats = GetPlayerStatsData(clientId);
            if (stats == null || stats.CurrentLevel < realm.requiredLevel)
            {
                NotifyClientRpc("Level " + realm.requiredLevel + " required", clientId);
                return;
            }

            activeRealmIndex.Value = realmIndex;
            isRealmActive.Value = true;

            RealmEnteredClientRpc(realmIndex, realm.name);
        }

        public void OnRealmBossDefeated(ulong clientId)
        {
            if (!IsServer || !isRealmActive.Value) return;

            int realmIdx = activeRealmIndex.Value;
            var realm = hiddenRealms[realmIdx];

            var clears = GetOrCreateClears(clientId);
            bool isFirstClear = !clears.ContainsKey(realmIdx) || !clears[realmIdx];

            var stats = GetPlayerStatsData(clientId);
            if (stats != null)
            {
                long goldMult = isFirstClear ? 3 : 1;
                long expMult = isFirstClear ? 3 : 1;
                stats.ChangeGold(realm.goldReward * goldMult);
                stats.AddExperience(realm.expReward * expMult);
            }

            if (isFirstClear)
            {
                clears[realmIdx] = true;
                if (LegendaryAspectSystem.Instance != null)
                {
                    FirstClearRewardClientRpc(realmIdx, realm.name, realm.uniqueAspectId, clientId);
                }
            }

            RealmCompleteClientRpc(realmIdx, realm.name, realm.goldReward, realm.expReward, clientId);

            isRealmActive.Value = false;
            activeRealmIndex.Value = -1;
        }

        [ServerRpc(RequireOwnership = false)]
        public void PurchaseSecretItemServerRpc(int shopIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (shopIndex < 0 || shopIndex >= secretShopItems.Length) return;

            var item = secretShopItems[shopIndex];
            var stats = GetPlayerStatsData(clientId);
            if (stats == null || stats.Gold < item.cost)
            {
                NotifyClientRpc("Not enough gold (" + item.cost + "G)", clientId);
                return;
            }

            stats.ChangeGold(-item.cost);
            SecretPurchaseClientRpc(shopIndex, item.name, item.cost, clientId);
        }

        public bool IsRealmDiscovered(ulong clientId, int realmIndex)
        {
            var discovered = GetOrCreateDiscovered(clientId);
            return discovered.Contains(realmIndex);
        }

        public bool IsRealmActive() => isRealmActive.Value;
        public int GetActiveRealmIndex() => activeRealmIndex.Value;

        public static HiddenRealmData GetRealmData(int index)
        {
            if (index < 0 || index >= hiddenRealms.Length) return null;
            return hiddenRealms[index];
        }

        public static int GetRealmCount() => hiddenRealms.Length;
        public static SecretMerchantItem[] GetSecretShop() => secretShopItems;

        #region ClientRPCs

        [ClientRpc]
        private void RealmDiscoveredClientRpc(int idx, string name, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification("Hidden Realm Discovered: " + name, NotificationType.System);
            OnRealmDiscovered?.Invoke();
        }

        [ClientRpc]
        private void RealmEnteredClientRpc(int idx, string name)
        {
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification("Entering: " + name, NotificationType.Warning);
            OnRealmStateChanged?.Invoke();
        }

        [ClientRpc]
        private void RealmCompleteClientRpc(int idx, string name, long gold, long exp, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification(name + " cleared! +" + gold + "G, +" + exp + " EXP", NotificationType.System);
            OnRealmStateChanged?.Invoke();
        }

        [ClientRpc]
        private void FirstClearRewardClientRpc(int idx, string name, string aspectId, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification("First Clear! Unique aspect: " + aspectId, NotificationType.System);
        }

        [ClientRpc]
        private void SecretPurchaseClientRpc(int idx, string itemName, long cost, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification("Purchased: " + itemName + " (-" + cost + "G)", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyClientRpc(string message, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification(message, NotificationType.Warning);
        }

        #endregion

        #region Utility

        private HashSet<int> GetOrCreateDiscovered(ulong clientId)
        {
            if (!discoveredRealms.ContainsKey(clientId))
                discoveredRealms[clientId] = new HashSet<int>();
            return discoveredRealms[clientId];
        }

        private Dictionary<int, bool> GetOrCreateClears(ulong clientId)
        {
            if (!realmFirstClears.ContainsKey(clientId))
                realmFirstClears[clientId] = new Dictionary<int, bool>();
            return realmFirstClears[clientId];
        }

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        #endregion

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }

    public class HiddenRealmData
    {
        public int id;
        public string name;
        public string discoveryHint;
        public int requiredLevel;
        public float bossHP;
        public string uniqueAspectId;
        public long expReward;
        public long goldReward;

        public HiddenRealmData(int id, string name, string hint, int lvl, float hp, string aspect, long exp, long gold)
        {
            this.id = id; this.name = name; this.discoveryHint = hint;
            this.requiredLevel = lvl; this.bossHP = hp; this.uniqueAspectId = aspect;
            this.expReward = exp; this.goldReward = gold;
        }
    }

    public class SecretMerchantItem
    {
        public string name;
        public long cost;
        public string description;

        public SecretMerchantItem(string name, long cost, string desc)
        {
            this.name = name; this.cost = cost; this.description = desc;
        }
    }
}
