using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public enum ZoneType
    {
        SafeZone,
        Field,
        Dungeon,
        Raid
    }

    public enum ZoneEnvironmentEffect
    {
        None,
        Fog,        // -20% visibility
        Heat,       // HP drain 1/5s
        Cold,       // -10% attack speed
        Darkness,   // -15% accuracy
        Lava,       // fire DoT near edges
        Abyss       // all damage +10%
    }

    [Serializable]
    public class ZoneData
    {
        public int id;
        public string name;
        public string description;
        public int levelMin;
        public int levelMax;
        public string monsterRaces; // CSV format
        public ZoneType zoneType;
        public ZoneEnvironmentEffect environmentEffect;
        public string bgmKey;

        public ZoneData(int id, string name, string description, int levelMin, int levelMax,
            string monsterRaces, ZoneType zoneType, ZoneEnvironmentEffect environmentEffect, string bgmKey)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.levelMin = levelMin;
            this.levelMax = levelMax;
            this.monsterRaces = monsterRaces;
            this.zoneType = zoneType;
            this.environmentEffect = environmentEffect;
            this.bgmKey = bgmKey;
        }
    }

    [Serializable]
    public class WaypointData
    {
        public int id;
        public string zoneName;
        public Vector3 position;
        public string name;
        public bool isDiscovered;

        public WaypointData(int id, string zoneName, Vector3 position, string name)
        {
            this.id = id;
            this.zoneName = zoneName;
            this.position = position;
            this.name = name;
            this.isDiscovered = false;
        }
    }

    public class ZoneSystem : NetworkBehaviour
    {
        public static ZoneSystem Instance { get; private set; }

        public event Action<int, int> OnZoneChanged;          // oldZoneId, newZoneId
        public event Action<int> OnWaypointDiscovered;        // waypointId

        private static readonly List<ZoneData> s_AllZones = new List<ZoneData>
        {
            new ZoneData(0, "Town", "A peaceful safe haven with shops and NPCs. Rest and resupply here.",
                0, 0, "", ZoneType.SafeZone, ZoneEnvironmentEffect.None, "bgm_town"),
            new ZoneData(1, "Greenfields", "Lush grassy plains where novice adventurers begin their journey.",
                1, 3, "Beast,Goblin", ZoneType.Field, ZoneEnvironmentEffect.None, "bgm_greenfields"),
            new ZoneData(2, "DarkForest", "A foggy woodland teeming with dangerous creatures.",
                3, 5, "Beast,Orc", ZoneType.Field, ZoneEnvironmentEffect.Fog, "bgm_darkforest"),
            new ZoneData(3, "CrystalCaverns", "Underground caverns glittering with magical crystals.",
                5, 7, "Elemental,Construct", ZoneType.Dungeon, ZoneEnvironmentEffect.Darkness, "bgm_crystalcaverns"),
            new ZoneData(4, "ScorchDesert", "A blistering desert where heat saps the life from travelers.",
                7, 9, "Demon,Undead", ZoneType.Field, ZoneEnvironmentEffect.Heat, "bgm_scorchdesert"),
            new ZoneData(5, "FrozenPeaks", "Snow-covered mountains battered by freezing winds.",
                9, 11, "Dragon,Elemental", ZoneType.Field, ZoneEnvironmentEffect.Cold, "bgm_frozenpeaks"),
            new ZoneData(6, "VolcanicRift", "A molten rift where lava flows and demons roam.",
                11, 13, "Demon,Dragon", ZoneType.Dungeon, ZoneEnvironmentEffect.Lava, "bgm_volcanicrift"),
            new ZoneData(7, "AbyssalDepths", "The darkest depths where unspeakable horrors dwell.",
                13, 15, "Demon,Undead,Dragon", ZoneType.Raid, ZoneEnvironmentEffect.Abyss, "bgm_abyssaldepths")
        };

        private static readonly List<WaypointData> s_AllWaypoints = new List<WaypointData>
        {
            // Town (zone 0)
            new WaypointData(0,  "Town",           new Vector3(0, 0, 0),       "Town Gate"),
            new WaypointData(1,  "Town",           new Vector3(25, 0, 25),     "Town Square"),
            new WaypointData(2,  "Town",           new Vector3(50, 0, 50),     "Town Market"),
            // Greenfields (zone 1)
            new WaypointData(3,  "Greenfields",    new Vector3(100, 0, 0),     "Greenfields Entry"),
            new WaypointData(4,  "Greenfields",    new Vector3(150, 0, 50),    "Greenfields Crossroad"),
            new WaypointData(5,  "Greenfields",    new Vector3(200, 0, 100),   "Greenfields Exit"),
            // DarkForest (zone 2)
            new WaypointData(6,  "DarkForest",     new Vector3(250, 0, 100),   "Forest Entrance"),
            new WaypointData(7,  "DarkForest",     new Vector3(300, 0, 150),   "Foggy Clearing"),
            new WaypointData(8,  "DarkForest",     new Vector3(350, 0, 200),   "Forest Depths"),
            // CrystalCaverns (zone 3)
            new WaypointData(9,  "CrystalCaverns", new Vector3(400, -20, 200), "Cavern Mouth"),
            new WaypointData(10, "CrystalCaverns", new Vector3(450, -40, 250), "Crystal Chamber"),
            new WaypointData(11, "CrystalCaverns", new Vector3(500, -60, 300), "Deep Cavern"),
            // ScorchDesert (zone 4)
            new WaypointData(12, "ScorchDesert",   new Vector3(550, 0, 300),   "Desert Outpost"),
            new WaypointData(13, "ScorchDesert",   new Vector3(600, 0, 350),   "Scorching Dunes"),
            new WaypointData(14, "ScorchDesert",   new Vector3(650, 0, 400),   "Oasis Ruins"),
            // FrozenPeaks (zone 5)
            new WaypointData(15, "FrozenPeaks",    new Vector3(700, 50, 400),  "Mountain Base"),
            new WaypointData(16, "FrozenPeaks",    new Vector3(750, 100, 450), "Frozen Pass"),
            new WaypointData(17, "FrozenPeaks",    new Vector3(800, 150, 500), "Summit Camp"),
            // VolcanicRift (zone 6)
            new WaypointData(18, "VolcanicRift",   new Vector3(850, -10, 500), "Rift Entrance"),
            new WaypointData(19, "VolcanicRift",   new Vector3(900, -30, 550), "Magma Bridge"),
            new WaypointData(20, "VolcanicRift",   new Vector3(950, -50, 600), "Caldera Core"),
            // AbyssalDepths (zone 7)
            new WaypointData(21, "AbyssalDepths",  new Vector3(1000, -80, 600),  "Abyssal Gate"),
            new WaypointData(22, "AbyssalDepths",  new Vector3(1050, -120, 650), "Void Corridor"),
            new WaypointData(23, "AbyssalDepths",  new Vector3(1100, -160, 700), "Heart of Darkness")
        };

        // Per-player tracking
        private readonly Dictionary<ulong, HashSet<int>> m_DiscoveredZones = new Dictionary<ulong, HashSet<int>>();
        private readonly Dictionary<ulong, HashSet<int>> m_DiscoveredWaypoints = new Dictionary<ulong, HashSet<int>>();
        private readonly Dictionary<ulong, int> m_CurrentZone = new Dictionary<ulong, int>();

        private const int FAST_TRAVEL_GOLD_PER_UNIT = 2;
        private const string WAYPOINT_PREFS_PREFIX = "WP_Discovered_";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
            base.OnNetworkDespawn();
        }

        private void OnClientConnected(ulong clientId)
        {
            m_DiscoveredZones[clientId] = new HashSet<int> { 0 }; // Town always discovered
            m_DiscoveredWaypoints[clientId] = new HashSet<int> { 0 }; // Town Gate always discovered
            m_CurrentZone[clientId] = 0; // Start in Town

            // Restore saved waypoint discoveries from PlayerPrefs
            LoadWaypointDiscoveries(clientId);
        }

        private void OnClientDisconnected(ulong clientId)
        {
            m_DiscoveredZones.Remove(clientId);
            m_DiscoveredWaypoints.Remove(clientId);
            m_CurrentZone.Remove(clientId);
        }

        private void LoadWaypointDiscoveries(ulong clientId)
        {
            string key = WAYPOINT_PREFS_PREFIX + clientId;
            string saved = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(saved)) return;

            string[] ids = saved.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string idStr in ids)
            {
                if (int.TryParse(idStr, out int wpId))
                {
                    m_DiscoveredWaypoints[clientId].Add(wpId);

                    // Also mark the zone as discovered
                    WaypointData wp = GetWaypointData(wpId);
                    if (wp != null)
                    {
                        ZoneData zone = GetZoneByName(wp.zoneName);
                        if (zone != null)
                        {
                            m_DiscoveredZones[clientId].Add(zone.id);
                        }
                    }
                }
            }
        }

        private void SaveWaypointDiscoveries(ulong clientId)
        {
            if (!m_DiscoveredWaypoints.ContainsKey(clientId)) return;

            string key = WAYPOINT_PREFS_PREFIX + clientId;
            string csv = string.Join(",", m_DiscoveredWaypoints[clientId]);
            PlayerPrefs.SetString(key, csv);
            PlayerPrefs.Save();
        }

        // ===================== Public Getters =====================

        public static ZoneData GetZoneData(int zoneId)
        {
            return s_AllZones.FirstOrDefault(z => z.id == zoneId);
        }

        public static List<ZoneData> GetAllZones()
        {
            return new List<ZoneData>(s_AllZones);
        }

        public static WaypointData GetWaypointData(int waypointId)
        {
            return s_AllWaypoints.FirstOrDefault(w => w.id == waypointId);
        }

        public static List<WaypointData> GetWaypointsForZone(string zoneName)
        {
            return s_AllWaypoints.Where(w => w.zoneName == zoneName).ToList();
        }

        public static ZoneData GetZoneByName(string zoneName)
        {
            return s_AllZones.FirstOrDefault(z => z.name == zoneName);
        }

        public int GetCurrentZone(ulong clientId)
        {
            return m_CurrentZone.TryGetValue(clientId, out int zoneId) ? zoneId : 0;
        }

        public bool IsZoneDiscovered(ulong clientId, int zoneId)
        {
            return m_DiscoveredZones.TryGetValue(clientId, out var zones) && zones.Contains(zoneId);
        }

        public bool IsWaypointDiscovered(ulong clientId, int waypointId)
        {
            return m_DiscoveredWaypoints.TryGetValue(clientId, out var waypoints) && waypoints.Contains(waypointId);
        }

        public List<int> GetDiscoveredZones(ulong clientId)
        {
            if (m_DiscoveredZones.TryGetValue(clientId, out var zones))
            {
                return zones.ToList();
            }
            return new List<int> { 0 };
        }

        public List<int> GetDiscoveredWaypoints(ulong clientId)
        {
            if (m_DiscoveredWaypoints.TryGetValue(clientId, out var waypoints))
            {
                return waypoints.ToList();
            }
            return new List<int> { 0 };
        }

        // ===================== Zone Entry =====================

        [ServerRpc(RequireOwnership = false)]
        public void EnterZoneServerRpc(int zoneId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            ZoneData zone = GetZoneData(zoneId);

            if (zone == null)
            {
                Debug.LogWarning($"[ZoneSystem] Invalid zone id: {zoneId}");
                return;
            }

            // Check player level against zone requirements
            var playerStats = GetPlayerStats(clientId);
            if (playerStats != null && zone.levelMin > 0)
            {
                int playerLevel = playerStats.CurrentLevel;
                if (playerLevel < zone.levelMin)
                {
                    NotifyZoneEntryDeniedClientRpc(
                        $"Level {zone.levelMin} required to enter {zone.name}. Your level: {playerLevel}",
                        GetClientRpcParams(clientId));
                    return;
                }
            }

            int oldZoneId = GetCurrentZone(clientId);
            m_CurrentZone[clientId] = zoneId;

            // First-time zone discovery
            bool newDiscovery = false;
            if (!m_DiscoveredZones.ContainsKey(clientId))
            {
                m_DiscoveredZones[clientId] = new HashSet<int>();
            }
            if (m_DiscoveredZones[clientId].Add(zoneId))
            {
                newDiscovery = true;

                // Auto-discover the entry waypoint of this zone
                var entryWaypoints = s_AllWaypoints.Where(w => w.zoneName == zone.name).ToList();
                if (entryWaypoints.Count > 0)
                {
                    DiscoverWaypointInternal(clientId, entryWaypoints[0].id);
                }
            }

            // Build environment info string
            string envInfo = GetEnvironmentEffectDescription(zone.environmentEffect);

            // Notify the entering player
            NotifyZoneChangeClientRpc(oldZoneId, zoneId, zone.name, envInfo, newDiscovery,
                GetClientRpcParams(clientId));

            // Broadcast to nearby players in old zone
            if (oldZoneId != zoneId)
            {
                BroadcastZoneTransition(clientId, oldZoneId, zoneId);
            }

            OnZoneChanged?.Invoke(oldZoneId, zoneId);
            Debug.Log($"[ZoneSystem] Client {clientId} entered zone {zone.name} (id={zoneId})");
        }

        private void BroadcastZoneTransition(ulong travelerId, int oldZoneId, int newZoneId)
        {
            ZoneData newZone = GetZoneData(newZoneId);
            if (newZone == null) return;

            foreach (var kvp in m_CurrentZone)
            {
                if (kvp.Key == travelerId) continue;
                if (kvp.Value == oldZoneId || kvp.Value == newZoneId)
                {
                    NotifyPlayerTransitionClientRpc(
                        $"A player has entered {newZone.name}.",
                        GetClientRpcParams(kvp.Key));
                }
            }
        }

        // ===================== Waypoint Discovery =====================

        public void DiscoverWaypoint(int waypointId, ulong clientId)
        {
            if (!IsServer) return;
            DiscoverWaypointInternal(clientId, waypointId);
        }

        private void DiscoverWaypointInternal(ulong clientId, int waypointId)
        {
            WaypointData wp = GetWaypointData(waypointId);
            if (wp == null) return;

            if (!m_DiscoveredWaypoints.ContainsKey(clientId))
            {
                m_DiscoveredWaypoints[clientId] = new HashSet<int>();
            }

            if (m_DiscoveredWaypoints[clientId].Add(waypointId))
            {
                SaveWaypointDiscoveries(clientId);

                NotifyWaypointDiscoveredClientRpc(waypointId, wp.name, wp.zoneName,
                    GetClientRpcParams(clientId));

                OnWaypointDiscovered?.Invoke(waypointId);
                Debug.Log($"[ZoneSystem] Client {clientId} discovered waypoint: {wp.name} in {wp.zoneName}");
            }
        }

        // ===================== Fast Travel =====================

        [ServerRpc(RequireOwnership = false)]
        public void FastTravelServerRpc(int waypointId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            // Validate waypoint exists
            WaypointData targetWp = GetWaypointData(waypointId);
            if (targetWp == null)
            {
                Debug.LogWarning($"[ZoneSystem] Invalid waypoint id: {waypointId}");
                return;
            }

            // Check if waypoint is discovered
            if (!IsWaypointDiscovered(clientId, waypointId))
            {
                NotifyFastTravelDeniedClientRpc(
                    "You have not discovered this waypoint yet.",
                    GetClientRpcParams(clientId));
                return;
            }

            // Calculate travel cost based on distance
            int currentZoneId = GetCurrentZone(clientId);
            ZoneData currentZone = GetZoneData(currentZoneId);
            ZoneData targetZone = GetZoneByName(targetWp.zoneName);

            if (currentZone == null || targetZone == null) return;

            // Get current player position for distance calc
            var playerObject = GetPlayerObject(clientId);
            if (playerObject == null) return;

            float distance = Vector3.Distance(playerObject.transform.position, targetWp.position);
            int goldCost = Mathf.Max(10, Mathf.RoundToInt(distance * FAST_TRAVEL_GOLD_PER_UNIT));

            // Free travel within Town
            if (currentZone.zoneType == ZoneType.SafeZone && targetZone.zoneType == ZoneType.SafeZone)
            {
                goldCost = 0;
            }

            // Check player gold
            var playerStats = GetPlayerStats(clientId);
            if (playerStats == null) return;

            if (playerStats.Gold < goldCost)
            {
                NotifyFastTravelDeniedClientRpc(
                    $"Not enough gold. Required: {goldCost}G, You have: {playerStats.Gold}G",
                    GetClientRpcParams(clientId));
                return;
            }

            // Deduct gold and teleport
            if (goldCost > 0)
            {
                playerStats.ChangeGold(-goldCost);
            }

            // Move player to waypoint position
            playerObject.transform.position = targetWp.position;

            // Update zone if changed
            if (targetZone.id != currentZoneId)
            {
                m_CurrentZone[clientId] = targetZone.id;

                if (!m_DiscoveredZones.ContainsKey(clientId))
                {
                    m_DiscoveredZones[clientId] = new HashSet<int>();
                }
                m_DiscoveredZones[clientId].Add(targetZone.id);

                string envInfo = GetEnvironmentEffectDescription(targetZone.environmentEffect);
                NotifyZoneChangeClientRpc(currentZoneId, targetZone.id, targetZone.name, envInfo, false,
                    GetClientRpcParams(clientId));

                OnZoneChanged?.Invoke(currentZoneId, targetZone.id);
            }

            NotifyFastTravelCompleteClientRpc(targetWp.name, targetWp.zoneName, goldCost,
                GetClientRpcParams(clientId));

            Debug.Log($"[ZoneSystem] Client {clientId} fast traveled to {targetWp.name} ({targetWp.zoneName}) for {goldCost}G");
        }

        // ===================== ClientRpc Notifications =====================

        [ClientRpc]
        private void NotifyZoneChangeClientRpc(int oldZoneId, int newZoneId, string zoneName,
            string envEffect, bool isNewDiscovery, ClientRpcParams clientRpcParams = default)
        {
            string message = $"Entered {zoneName}";
            if (isNewDiscovery)
            {
                message += " - New zone discovered!";
            }
            if (!string.IsNullOrEmpty(envEffect))
            {
                message += $" [{envEffect}]";
            }

            NotificationManager.Instance?.ShowNotification(message, NotificationType.System);
            Debug.Log($"[ZoneSystem] {message}");
        }

        [ClientRpc]
        private void NotifyWaypointDiscoveredClientRpc(int waypointId, string waypointName,
            string zoneName, ClientRpcParams clientRpcParams = default)
        {
            string message = $"Waypoint discovered: {waypointName} ({zoneName})";
            NotificationManager.Instance?.ShowNotification(message, NotificationType.System);
            Debug.Log($"[ZoneSystem] {message}");
        }

        [ClientRpc]
        private void NotifyZoneEntryDeniedClientRpc(string reason, ClientRpcParams clientRpcParams = default)
        {
            NotificationManager.Instance?.ShowNotification(reason, NotificationType.Warning);
        }

        [ClientRpc]
        private void NotifyFastTravelDeniedClientRpc(string reason, ClientRpcParams clientRpcParams = default)
        {
            NotificationManager.Instance?.ShowNotification(reason, NotificationType.Warning);
        }

        [ClientRpc]
        private void NotifyFastTravelCompleteClientRpc(string waypointName, string zoneName,
            int goldCost, ClientRpcParams clientRpcParams = default)
        {
            string message = goldCost > 0
                ? $"Traveled to {waypointName} ({zoneName}) - {goldCost}G"
                : $"Traveled to {waypointName} ({zoneName})";
            NotificationManager.Instance?.ShowNotification(message, NotificationType.System);
        }

        [ClientRpc]
        private void NotifyPlayerTransitionClientRpc(string message, ClientRpcParams clientRpcParams = default)
        {
            NotificationManager.Instance?.ShowNotification(message, NotificationType.System);
        }

        // ===================== Helpers =====================

        private string GetEnvironmentEffectDescription(ZoneEnvironmentEffect effect)
        {
            switch (effect)
            {
                case ZoneEnvironmentEffect.Fog:      return "Fog: -20% visibility";
                case ZoneEnvironmentEffect.Heat:     return "Heat: HP drain 1/5s";
                case ZoneEnvironmentEffect.Cold:     return "Cold: -10% attack speed";
                case ZoneEnvironmentEffect.Darkness: return "Darkness: -15% accuracy";
                case ZoneEnvironmentEffect.Lava:     return "Lava: fire DoT near edges";
                case ZoneEnvironmentEffect.Abyss:    return "Abyss: all damage +10%";
                case ZoneEnvironmentEffect.None:
                default:                             return "";
            }
        }

        private PlayerStatsData GetPlayerStats(ulong clientId)
        {
            if (NetworkManager.Singleton == null) return null;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        private GameObject GetPlayerObject(ulong clientId)
        {
            if (NetworkManager.Singleton == null) return null;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
            return client.PlayerObject?.gameObject;
        }

        private ClientRpcParams GetClientRpcParams(ulong clientId)
        {
            return new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };
        }
    }
}
