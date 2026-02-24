using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// Corruption outcome types from applying a Corruption Orb.
    /// </summary>
    public enum CorruptionResult
    {
        Empower,    // Gain a corruption-exclusive affix
        Transform,  // One existing affix replaced with a random affix
        Weaken,     // One stat reduced by 20-50%
        Destroy     // Item is destroyed, returns salvage materials
    }

    /// <summary>
    /// Data container for a single corruption-exclusive affix.
    /// </summary>
    [Serializable]
    public class CorruptionAffix
    {
        public string AffixId;
        public string Name;
        public string Description;
        public float Value;

        public CorruptionAffix(string affixId, string name, string description, float value)
        {
            AffixId = affixId;
            Name = name;
            Description = description;
            Value = value;
        }
    }

    /// <summary>
    /// Tracks corruption state for a single item.
    /// </summary>
    [Serializable]
    public class CorruptionData
    {
        public bool IsCorrupted;
        public CorruptionResult Result;
        public string AppliedAffixId;
        public float WeakenPercent;

        public CorruptionData()
        {
            IsCorrupted = false;
            Result = CorruptionResult.Empower;
            AppliedAffixId = string.Empty;
            WeakenPercent = 0f;
        }
    }

    /// <summary>
    /// Irreversible item corruption system inspired by PoE2 Vaal Orb.
    /// Corruption Orbs drop from Pinnacle bosses and hidden realm encounters.
    /// Applying a Corruption Orb produces one of four equally-weighted outcomes:
    /// Empower (exclusive affix), Transform (affix swap), Weaken (stat reduction), or Destroy.
    /// Already-corrupted items cannot be corrupted again.
    /// </summary>
    public class CorruptionSystem : NetworkBehaviour
    {
        public static CorruptionSystem Instance { get; private set; }

        /// <summary>
        /// Fired after a corruption attempt completes on the server.
        /// Parameters: clientId, itemId, result, affixName (empty if not Empower).
        /// </summary>
        public event Action<ulong, string, CorruptionResult, string> OnCorruptionComplete;

        // ─── Static Corruption-Exclusive Affixes ───────────────────────────
        private static readonly List<CorruptionAffix> s_CorruptionAffixes = new List<CorruptionAffix>
        {
            new CorruptionAffix("corrupt_piercing",          "Piercing",          "+20% armor penetration",                20f),
            new CorruptionAffix("corrupt_vampiric",          "Vampiric",          "+5% life steal",                         5f),
            new CorruptionAffix("corrupt_extra_socket",      "Extra Socket",      "+1 gem socket",                          1f),
            new CorruptionAffix("corrupt_skill_level",       "Skill Level",       "+1 to all skill levels",                 1f),
            new CorruptionAffix("corrupt_crit_multiplier",   "Crit Multiplier",   "+30% critical damage",                  30f),
            new CorruptionAffix("corrupt_spell_echo",        "Spell Echo",        "15% chance to cast twice",              15f),
            new CorruptionAffix("corrupt_phase_shift",       "Phase Shift",       "10% chance to dodge all damage",        10f),
            new CorruptionAffix("corrupt_soul_harvest",      "Soul Harvest",      "Kills grant +2% damage for 10s",         2f),
            new CorruptionAffix("corrupt_elemental_mastery", "Elemental Mastery", "+15% all elemental damage",             15f),
            new CorruptionAffix("corrupt_fortified_mind",    "Fortified Mind",    "+20% crowd control resistance",         20f)
        };

        // ─── Server-Side State ─────────────────────────────────────────────
        private readonly Dictionary<string, CorruptionData> m_CorruptedItems = new Dictionary<string, CorruptionData>();

        // ─── Lifecycle ─────────────────────────────────────────────────────
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

        // ─── Public Queries ────────────────────────────────────────────────

        /// <summary>Returns true if the item has already been corrupted.</summary>
        public bool IsItemCorrupted(string itemId)
        {
            return m_CorruptedItems.ContainsKey(itemId) && m_CorruptedItems[itemId].IsCorrupted;
        }

        /// <summary>Returns the corruption data for an item, or null.</summary>
        public CorruptionData GetCorruptionData(string itemId)
        {
            m_CorruptedItems.TryGetValue(itemId, out var data);
            return data;
        }

        /// <summary>Returns a read-only copy of all available corruption affixes.</summary>
        public static IReadOnlyList<CorruptionAffix> GetAllCorruptionAffixes()
        {
            return s_CorruptionAffixes;
        }

        // ─── Server RPC ────────────────────────────────────────────────────

        /// <summary>
        /// Client requests corruption of an item. Confirmation is handled client-side
        /// before this RPC is sent. The server rolls the outcome and notifies the client.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CorruptItemServerRpc(string itemId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            // Validate player exists
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
            {
                Debug.LogWarning($"[CorruptionSystem] Unknown client {clientId} attempted corruption.");
                return;
            }

            var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            if (playerObject == null)
            {
                Debug.LogWarning($"[CorruptionSystem] Client {clientId} has no player object.");
                return;
            }

            var statsData = playerObject.GetComponent<PlayerStatsManager>()?.CurrentStats;
            if (statsData == null)
            {
                Debug.LogWarning($"[CorruptionSystem] Client {clientId} missing PlayerStatsData.");
                return;
            }

            // Already corrupted check
            if (IsItemCorrupted(itemId))
            {
                NotifyCorruptionResultClientRpc(
                    clientId,
                    itemId,
                    -1,
                    string.Empty,
                    "This item is already corrupted and cannot be modified further."
                );
                return;
            }

            // Roll outcome (25% each)
            int roll = UnityEngine.Random.Range(0, 4);
            CorruptionResult result = (CorruptionResult)roll;

            // Create corruption data
            var corruptionData = new CorruptionData
            {
                IsCorrupted = true,
                Result = result
            };

            string affixName = string.Empty;
            string resultMessage = string.Empty;

            switch (result)
            {
                case CorruptionResult.Empower:
                    var affix = RollRandomCorruptionAffix();
                    corruptionData.AppliedAffixId = affix.AffixId;
                    affixName = affix.Name;
                    resultMessage = $"Corruption empowered the item with {affix.Name}: {affix.Description}";
                    Debug.Log($"[CorruptionSystem] Empower: {itemId} gained {affix.Name} for client {clientId}.");
                    break;

                case CorruptionResult.Transform:
                    resultMessage = "Corruption transformed one of the item's affixes into a new random affix.";
                    Debug.Log($"[CorruptionSystem] Transform: {itemId} affix swapped for client {clientId}.");
                    break;

                case CorruptionResult.Weaken:
                    float weakenPercent = UnityEngine.Random.Range(20f, 50f);
                    corruptionData.WeakenPercent = weakenPercent;
                    resultMessage = $"Corruption weakened the item. One stat reduced by {weakenPercent:F1}%.";
                    Debug.Log($"[CorruptionSystem] Weaken: {itemId} stat reduced by {weakenPercent:F1}% for client {clientId}.");
                    break;

                case CorruptionResult.Destroy:
                    resultMessage = "Corruption consumed the item. Salvage materials have been recovered.";
                    Debug.Log($"[CorruptionSystem] Destroy: {itemId} destroyed for client {clientId}.");
                    break;
            }

            // Store corruption state
            m_CorruptedItems[itemId] = corruptionData;

            // Notify client of the result
            NotifyCorruptionResultClientRpc(
                clientId,
                itemId,
                (int)result,
                affixName,
                resultMessage
            );

            // Fire server-side event
            OnCorruptionComplete?.Invoke(clientId, itemId, result, affixName);
        }

        // ─── Client RPC ────────────────────────────────────────────────────

        /// <summary>
        /// Notifies the requesting client about the corruption outcome.
        /// A resultCode of -1 indicates an error (item already corrupted, etc.).
        /// </summary>
        [ClientRpc]
        private void NotifyCorruptionResultClientRpc(
            ulong targetClientId,
            string itemId,
            int resultCode,
            string affixName,
            string message)
        {
            // Only process on the targeted client
            if (NetworkManager.Singleton.LocalClientId != targetClientId)
            {
                return;
            }

            // Error case (already corrupted, validation failure)
            if (resultCode < 0)
            {
                Debug.LogWarning($"[CorruptionSystem] Corruption failed for {itemId}: {message}");
                ShowNotification(message, NotificationType.Warning);
                return;
            }

            CorruptionResult result = (CorruptionResult)resultCode;

            switch (result)
            {
                case CorruptionResult.Empower:
                    ShowNotification($"Item empowered with {affixName}.", NotificationType.System);
                    break;

                case CorruptionResult.Transform:
                    ShowNotification("An affix has been transformed by corruption.", NotificationType.System);
                    break;

                case CorruptionResult.Weaken:
                    ShowNotification("Corruption has weakened the item.", NotificationType.Warning);
                    break;

                case CorruptionResult.Destroy:
                    ShowNotification("The item was consumed by corruption. Salvage materials recovered.", NotificationType.Warning);
                    break;
            }

            Debug.Log($"[CorruptionSystem] Client received corruption result: {result} for item {itemId}. {message}");
        }

        // ─── Internal Helpers ──────────────────────────────────────────────

        /// <summary>Picks a random corruption-exclusive affix.</summary>
        private CorruptionAffix RollRandomCorruptionAffix()
        {
            int index = UnityEngine.Random.Range(0, s_CorruptionAffixes.Count);
            return s_CorruptionAffixes[index];
        }

        /// <summary>
        /// Returns a CSV string of all corruption-exclusive affix names
        /// suitable for sending via ClientRpc.
        /// </summary>
        public static string GetAffixNamesCsv()
        {
            var names = new List<string>();
            foreach (var affix in s_CorruptionAffixes)
            {
                names.Add(affix.Name);
            }
            return string.Join(",", names);
        }

        /// <summary>
        /// Looks up a corruption affix by its ID.
        /// Returns null if the affix ID is not found.
        /// </summary>
        public static CorruptionAffix GetAffixById(string affixId)
        {
            foreach (var affix in s_CorruptionAffixes)
            {
                if (affix.AffixId == affixId)
                {
                    return affix;
                }
            }
            return null;
        }

        /// <summary>
        /// Removes corruption tracking for an item (e.g. when the item is
        /// destroyed or dropped permanently). Server only.
        /// </summary>
        public void ClearCorruptionData(string itemId)
        {
            if (!IsServer) return;
            m_CorruptedItems.Remove(itemId);
        }

        /// <summary>
        /// Returns the total number of items that have been corrupted this session.
        /// </summary>
        public int GetCorruptedItemCount()
        {
            return m_CorruptedItems.Count;
        }

        /// <summary>
        /// Displays an in-game notification to the local player.
        /// Falls back to Debug.Log if NotificationManager is unavailable.
        /// </summary>
        private void ShowNotification(string message, NotificationType type)
        {
            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ShowNotification(message, type);
            }
            else
            {
                Debug.Log($"[CorruptionSystem Notification] ({type}) {message}");
            }
        }
    }
}
