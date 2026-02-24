using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public enum RunewordRune
    {
        El = 0, Eld, Tir, Nef, Eth, Ith, Tal, Ral, Ort, Thul,
        Amn, Sol, Shael, Dol, Zod
    }

    [System.Serializable]
    public class RunewordRuneInfo
    {
        public RunewordRune type;
        public string runeName;
        public int grade;
        public string description;
        public float statBonus;

        public RunewordRuneInfo(RunewordRune type, string name, int grade, string desc)
        {
            this.type = type;
            this.runeName = name;
            this.grade = grade;
            this.description = desc;
            this.statBonus = grade * 2.5f;
        }
    }

    [System.Serializable]
    public struct RunewordBonus
    {
        public string statName;
        public float value;
        public string description;

        public RunewordBonus(string stat, float val, string desc)
        {
            statName = stat;
            value = val;
            description = desc;
        }
    }

    [System.Serializable]
    public class RunewordRecipe
    {
        public string runewordId;
        public string runewordName;
        public RunewordRune[] requiredRunes;
        public int minSockets;
        public RunewordBonus[] bonuses;
        public string lore;

        public RunewordRecipe(string id, string name, RunewordRune[] runes, RunewordBonus[] bonuses, string lore)
        {
            runewordId = id;
            runewordName = name;
            requiredRunes = runes;
            minSockets = runes.Length;
            this.bonuses = bonuses;
            this.lore = lore;
        }
    }

    public class RunewordSystem : NetworkBehaviour
    {
        public static RunewordSystem Instance { get; private set; }

        public event Action OnRuneInventoryUpdated;
        public event Action<string> OnRunewordDiscovered;

        // Server state
        private Dictionary<ulong, List<RunewordRune>> playerRunes = new Dictionary<ulong, List<RunewordRune>>();
        private Dictionary<ulong, HashSet<string>> discoveredRunewords = new Dictionary<ulong, HashSet<string>>();
        private Dictionary<ulong, List<string>> activeRunewordsByPlayer = new Dictionary<ulong, List<string>>();

        // Local client state
        public Dictionary<RunewordRune, int> localRuneCounts { get; private set; } = new Dictionary<RunewordRune, int>();
        public HashSet<string> localDiscoveredRunewords { get; private set; } = new HashSet<string>();

        // Static data
        private Dictionary<RunewordRune, RunewordRuneInfo> runeDatabase = new Dictionary<RunewordRune, RunewordRuneInfo>();
        private List<RunewordRecipe> runewordRecipes = new List<RunewordRecipe>();

        private static readonly float[] RuneDropRates = new float[]
        {
            0.02f, 0.015f, 0.01f, 0.008f, 0.005f,
            0.003f, 0.002f, 0.001f, 0.0008f, 0.0005f,
            0.0003f, 0.0002f, 0.0001f, 0.00005f, 0.00001f
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeRuneDatabase();
            InitializeRunewordRecipes();
        }

        public override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        private void InitializeRuneDatabase()
        {
            runeDatabase[RunewordRune.El]    = new RunewordRuneInfo(RunewordRune.El,    "El",    1,  "Light within darkness");
            runeDatabase[RunewordRune.Eld]   = new RunewordRuneInfo(RunewordRune.Eld,   "Eld",   2,  "Wisdom of the ancients");
            runeDatabase[RunewordRune.Tir]   = new RunewordRuneInfo(RunewordRune.Tir,   "Tir",   3,  "Mana from slain foes");
            runeDatabase[RunewordRune.Nef]   = new RunewordRuneInfo(RunewordRune.Nef,   "Nef",   4,  "Knockback force");
            runeDatabase[RunewordRune.Eth]   = new RunewordRuneInfo(RunewordRune.Eth,   "Eth",   5,  "Lower target defense");
            runeDatabase[RunewordRune.Ith]   = new RunewordRuneInfo(RunewordRune.Ith,   "Ith",   6,  "Maximum damage");
            runeDatabase[RunewordRune.Tal]   = new RunewordRuneInfo(RunewordRune.Tal,   "Tal",   7,  "Poison resistance");
            runeDatabase[RunewordRune.Ral]   = new RunewordRuneInfo(RunewordRune.Ral,   "Ral",   8,  "Fire damage");
            runeDatabase[RunewordRune.Ort]   = new RunewordRuneInfo(RunewordRune.Ort,   "Ort",   9,  "Lightning damage");
            runeDatabase[RunewordRune.Thul]  = new RunewordRuneInfo(RunewordRune.Thul,  "Thul",  10, "Cold damage");
            runeDatabase[RunewordRune.Amn]   = new RunewordRuneInfo(RunewordRune.Amn,   "Amn",   11, "Life stolen per hit");
            runeDatabase[RunewordRune.Sol]   = new RunewordRuneInfo(RunewordRune.Sol,   "Sol",   12, "Damage reduction");
            runeDatabase[RunewordRune.Shael] = new RunewordRuneInfo(RunewordRune.Shael, "Shael", 13, "Faster attack speed");
            runeDatabase[RunewordRune.Dol]   = new RunewordRuneInfo(RunewordRune.Dol,   "Dol",   14, "Hit causes monster flee");
            runeDatabase[RunewordRune.Zod]   = new RunewordRuneInfo(RunewordRune.Zod,   "Zod",   15, "Indestructible");
        }

        private void InitializeRunewordRecipes()
        {
            runewordRecipes.Add(new RunewordRecipe("steel", "Steel",
                new[] { RunewordRune.Tir, RunewordRune.El },
                new[] { new RunewordBonus("ATK", 20f, "ATK+20%"), new RunewordBonus("Speed", 10f, "Speed+10%") },
                "Forged in relentless fury"));

            runewordRecipes.Add(new RunewordRecipe("zephyr", "Zephyr",
                new[] { RunewordRune.Ort, RunewordRune.Eth },
                new[] { new RunewordBonus("AGI", 15f, "AGI+15"), new RunewordBonus("MoveSpeed", 15f, "MoveSpeed+15%") },
                "Swift as the eastern wind"));

            runewordRecipes.Add(new RunewordRecipe("stealth", "Stealth",
                new[] { RunewordRune.Tal, RunewordRune.Eth },
                new[] { new RunewordBonus("CritRate", 15f, "CritRate+15%"), new RunewordBonus("Evasion", 25f, "Evasion+25%") },
                "Unseen, unheard, unstoppable"));

            runewordRecipes.Add(new RunewordRecipe("lore", "Lore",
                new[] { RunewordRune.Ort, RunewordRune.Sol },
                new[] { new RunewordBonus("INT", 10f, "INT+10"), new RunewordBonus("EXP", 15f, "EXP+15%") },
                "Knowledge is the greatest weapon"));

            runewordRecipes.Add(new RunewordRecipe("spirit", "Spirit",
                new[] { RunewordRune.Tal, RunewordRune.Thul, RunewordRune.Ort, RunewordRune.Amn },
                new[] { new RunewordBonus("AllStats", 5f, "AllStats+5"), new RunewordBonus("CastSpeed", 25f, "CastSpeed+25%"), new RunewordBonus("Mana", 50f, "Mana+50") },
                "The spirit realm fuels your power"));

            runewordRecipes.Add(new RunewordRecipe("insight", "Insight",
                new[] { RunewordRune.Ral, RunewordRune.Tir, RunewordRune.Tal, RunewordRune.Sol },
                new[] { new RunewordBonus("ManaRegen", 400f, "ManaRegen+400%"), new RunewordBonus("INT", 15f, "INT+15") },
                "Meditation grants infinite wisdom"));

            runewordRecipes.Add(new RunewordRecipe("enigma", "Enigma",
                new[] { RunewordRune.Ith, RunewordRune.Sol, RunewordRune.Zod },
                new[] { new RunewordBonus("Teleport", 1f, "Grants Teleport"), new RunewordBonus("MagicFind", 20f, "MagicFind+20%"), new RunewordBonus("DEF", 50f, "DEF+50") },
                "A mystery wrapped in ancient power"));

            runewordRecipes.Add(new RunewordRecipe("grief", "Grief",
                new[] { RunewordRune.Eth, RunewordRune.Tir, RunewordRune.Ral, RunewordRune.Eld, RunewordRune.Zod },
                new[] { new RunewordBonus("FlatDamage", 400f, "FlatDamage+400"), new RunewordBonus("ATKSpeed", 40f, "ATKSpeed+40%") },
                "Pain beyond mortal comprehension"));

            runewordRecipes.Add(new RunewordRecipe("fortitude", "Fortitude",
                new[] { RunewordRune.El, RunewordRune.Sol, RunewordRune.Dol, RunewordRune.Zod },
                new[] { new RunewordBonus("DEF", 200f, "DEF+200%"), new RunewordBonus("Life", 15f, "Life+15"), new RunewordBonus("AllResist", 25f, "AllResist+25") },
                "An unyielding bastion of protection"));

            runewordRecipes.Add(new RunewordRecipe("infinity", "Infinity",
                new[] { RunewordRune.Eld, RunewordRune.Amn, RunewordRune.Shael, RunewordRune.Zod },
                new[] { new RunewordBonus("ConvictionAura", 1f, "Grants Conviction Aura"), new RunewordBonus("LightningDmg", 50f, "LightningDmg+50%") },
                "Boundless destruction without end"));
        }

        #region Server API

        public void AddRuneToPlayer(ulong clientId, RunewordRune rune)
        {
            if (!IsServer) return;
            if (!playerRunes.ContainsKey(clientId))
                playerRunes[clientId] = new List<RunewordRune>();
            playerRunes[clientId].Add(rune);

            var data = runeDatabase.ContainsKey(rune) ? runeDatabase[rune] : null;
            string runeName = data != null ? data.runeName : rune.ToString();
            int grade = data != null ? data.grade : 0;
            NotifyRuneDropClientRpc(runeName, grade, clientId);
            SyncRuneInventoryToClient(clientId);
        }

        public List<RunewordBonus> GetActiveRunewordBonuses(ulong clientId)
        {
            var bonuses = new List<RunewordBonus>();
            if (!activeRunewordsByPlayer.ContainsKey(clientId)) return bonuses;

            foreach (string rwId in activeRunewordsByPlayer[clientId])
            {
                var recipe = runewordRecipes.Find(r => r.runewordId == rwId);
                if (recipe != null)
                    bonuses.AddRange(recipe.bonuses);
            }
            return bonuses;
        }

        public float GetRuneDropChance(int monsterLevel, RunewordRune runeType)
        {
            int index = (int)runeType;
            if (index < 0 || index >= RuneDropRates.Length) return 0f;
            float baseRate = RuneDropRates[index];
            float levelBonus = Mathf.Clamp(monsterLevel * 0.001f, 0f, 0.01f);
            return baseRate + levelBonus;
        }

        public RunewordRuneInfo GetRunewordRuneInfo(RunewordRune type)
        {
            return runeDatabase.ContainsKey(type) ? runeDatabase[type] : null;
        }

        public List<RunewordRecipe> GetAllRecipes() => new List<RunewordRecipe>(runewordRecipes);

        #endregion

        #region ServerRpc

        [ServerRpc(RequireOwnership = false)]
        public void InsertRuneServerRpc(string itemInstanceId, int socketIndex, int runeTypeInt, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!System.Enum.IsDefined(typeof(RunewordRune), runeTypeInt)) return;
            RunewordRune rune = (RunewordRune)runeTypeInt;

            if (!playerRunes.ContainsKey(clientId) || !playerRunes[clientId].Contains(rune))
            {
                Debug.LogWarning($"[RunewordSystem] Player {clientId} does not own rune {rune}");
                return;
            }

            playerRunes[clientId].Remove(rune);
            Debug.Log($"[RunewordSystem] Player {clientId} inserted {rune} into item {itemInstanceId} socket {socketIndex}");
            SyncRuneInventoryToClient(clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ActivateRunewordServerRpc(string itemInstanceId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            RunewordRecipe matched = FindMatchingRuneword(itemInstanceId);
            if (matched == null)
            {
                Debug.LogWarning($"[RunewordSystem] No matching runeword for item {itemInstanceId}");
                return;
            }

            if (!activeRunewordsByPlayer.ContainsKey(clientId))
                activeRunewordsByPlayer[clientId] = new List<string>();
            activeRunewordsByPlayer[clientId].Add(matched.runewordId);

            if (!discoveredRunewords.ContainsKey(clientId))
                discoveredRunewords[clientId] = new HashSet<string>();
            discoveredRunewords[clientId].Add(matched.runewordId);

            ApplyRunewordBonuses(clientId, matched);
            NotifyRunewordActivatedClientRpc(matched.runewordName, clientId);
            Debug.Log($"[RunewordSystem] Player {clientId} activated runeword: {matched.runewordName}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void DestroyRunewordServerRpc(string itemInstanceId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            RunewordRecipe matched = FindMatchingRuneword(itemInstanceId);
            if (matched == null) return;

            if (activeRunewordsByPlayer.ContainsKey(clientId))
                activeRunewordsByPlayer[clientId].Remove(matched.runewordId);

            if (!playerRunes.ContainsKey(clientId))
                playerRunes[clientId] = new List<RunewordRune>();

            foreach (RunewordRune rune in matched.requiredRunes)
            {
                if (UnityEngine.Random.value <= 0.5f)
                {
                    playerRunes[clientId].Add(rune);
                    Debug.Log($"[RunewordSystem] Recovered rune {rune} for player {clientId}");
                }
            }

            SyncRuneInventoryToClient(clientId);
            Debug.Log($"[RunewordSystem] Player {clientId} destroyed runeword: {matched.runewordName}");
        }

        #endregion

        #region ClientRpc

        [ClientRpc]
        public void NotifyRuneDropClientRpc(string runeName, int grade, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            string rarity = grade >= 12 ? "Legendary" : grade >= 8 ? "Rare" : grade >= 4 ? "Uncommon" : "Common";
            string msg = $"Rune Drop: {runeName} (Grade {grade}, {rarity})";
            NotificationManager.Instance?.ShowNotification(msg, NotificationType.System);
            Debug.Log($"[RunewordSystem] {msg}");
        }

        [ClientRpc]
        public void NotifyRunewordActivatedClientRpc(string runewordName, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localDiscoveredRunewords.Add(runewordName);
            string msg = $"Runeword Activated: {runewordName}!";
            NotificationManager.Instance?.ShowNotification(msg, NotificationType.Warning);
            OnRunewordDiscovered?.Invoke(runewordName);
            Debug.Log($"[RunewordSystem] {msg}");
        }

        [ClientRpc]
        public void SyncRuneInventoryClientRpc(int[] runeTypes, int[] runeCounts, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localRuneCounts.Clear();
            int count = Mathf.Min(runeTypes.Length, runeCounts.Length);
            for (int i = 0; i < count; i++)
            {
                if (System.Enum.IsDefined(typeof(RunewordRune), runeTypes[i]))
                    localRuneCounts[(RunewordRune)runeTypes[i]] = runeCounts[i];
            }
            OnRuneInventoryUpdated?.Invoke();
        }

        #endregion

        #region Private Helpers

        private void SyncRuneInventoryToClient(ulong clientId)
        {
            if (!playerRunes.ContainsKey(clientId)) return;

            var counts = new Dictionary<RunewordRune, int>();
            foreach (RunewordRune r in playerRunes[clientId])
            {
                if (!counts.ContainsKey(r)) counts[r] = 0;
                counts[r]++;
            }

            int[] types = counts.Keys.Select(k => (int)k).ToArray();
            int[] vals = counts.Values.ToArray();
            SyncRuneInventoryClientRpc(types, vals, clientId);
        }

        private RunewordRecipe FindMatchingRuneword(string itemInstanceId)
        {
            // TODO: integrate with actual item socket data
            // For now, returns null - requires item system integration
            Debug.Log($"[RunewordSystem] FindMatchingRuneword called for item {itemInstanceId}");
            return null;
        }

        private void ApplyRunewordBonuses(ulong clientId, RunewordRecipe recipe)
        {
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null)
            {
                Debug.LogWarning($"[RunewordSystem] PlayerStatsData not found for client {clientId}");
                return;
            }

            foreach (var bonus in recipe.bonuses)
            {
                Debug.Log($"[RunewordSystem] Applying {bonus.statName} +{bonus.value} to player {clientId}");
            }
        }

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (NetworkManager.Singleton == null) return null;
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) return null;
            var client = NetworkManager.Singleton.ConnectedClients[clientId];
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        #endregion
    }
}
