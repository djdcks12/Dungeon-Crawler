using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    [System.Serializable]
    public class SetBonusStat
    {
        public string statName;
        public float value;

        public SetBonusStat(string statName, float value)
        {
            this.statName = statName;
            this.value = value;
        }
    }

    [System.Serializable]
    public class SetBonus
    {
        public int requiredPieces;
        public string bonusName;
        public string description;
        public SetBonusStat[] stats;

        public SetBonus(int requiredPieces, string bonusName, string description, SetBonusStat[] stats)
        {
            this.requiredPieces = requiredPieces;
            this.bonusName = bonusName;
            this.description = description;
            this.stats = stats;
        }
    }

    [System.Serializable]
    public class ItemSetDefinition
    {
        public string setId;
        public string setName;
        public string[] pieceItemIds;
        public SetBonus[] bonuses;

        public ItemSetDefinition(string setId, string setName, string[] pieceItemIds, SetBonus[] bonuses)
        {
            this.setId = setId;
            this.setName = setName;
            this.pieceItemIds = pieceItemIds;
            this.bonuses = bonuses;
        }
    }

    [System.Serializable]
    public class ActiveSetBonus
    {
        public string setId;
        public string setName;
        public int equippedPieces;
        public int totalPieces;
        public List<SetBonusStat> activeStats;

        public ActiveSetBonus(string setId, string setName, int equippedPieces, int totalPieces)
        {
            this.setId = setId;
            this.setName = setName;
            this.equippedPieces = equippedPieces;
            this.totalPieces = totalPieces;
            this.activeStats = new List<SetBonusStat>();
        }
    }

    public class ItemSetSystem : NetworkBehaviour
    {
        public static ItemSetSystem Instance { get; private set; }

        public event System.Action OnSetBonusesChanged;

        private Dictionary<string, ItemSetDefinition> setDefinitions = new Dictionary<string, ItemSetDefinition>();
        private Dictionary<ulong, HashSet<string>> playerEquippedSetPieces = new Dictionary<ulong, HashSet<string>>();
        private List<ActiveSetBonus> localActiveSetBonuses = new List<ActiveSetBonus>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeSetDefinitions();
        }

        public override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        private void InitializeSetDefinitions()
        {
            RegisterSet(new ItemSetDefinition("BerserkerFury", "Berserker's Fury",
                new[] { "berserker_helm", "berserker_armor", "berserker_gauntlets" },
                new[] {
                    new SetBonus(2, "Berserker Might", "Raw strength surges through you.",
                        new[] { new SetBonusStat("STR", 10f), new SetBonusStat("ATK%", 5f) }),
                    new SetBonus(3, "Berserker Wrath", "Critical strikes devastate your foes.",
                        new[] { new SetBonusStat("CritDmg%", 25f) })
                }));

            RegisterSet(new ItemSetDefinition("ShadowVeil", "Shadow Veil",
                new[] { "shadow_hood", "shadow_cloak", "shadow_boots" },
                new[] {
                    new SetBonus(2, "Shadow Step", "Move like a whisper in the dark.",
                        new[] { new SetBonusStat("AGI", 10f), new SetBonusStat("Evasion%", 10f) }),
                    new SetBonus(3, "Shadow Strike", "Strike from the shadows with lethal precision.",
                        new[] { new SetBonusStat("CritRate%", 15f) })
                }));

            RegisterSet(new ItemSetDefinition("IronFortress", "Iron Fortress",
                new[] { "iron_helm", "iron_plate", "iron_shield" },
                new[] {
                    new SetBonus(2, "Iron Skin", "Your armor becomes an extension of your will.",
                        new[] { new SetBonusStat("DEF", 15f), new SetBonusStat("VIT", 10f) }),
                    new SetBonus(3, "Fortress Wall", "Stand unyielding against all attacks.",
                        new[] { new SetBonusStat("DmgReduction%", 20f) })
                }));

            RegisterSet(new ItemSetDefinition("ArcaneWisdom", "Arcane Wisdom",
                new[] { "arcane_hat", "arcane_robe", "arcane_staff", "arcane_orb" },
                new[] {
                    new SetBonus(2, "Arcane Mind", "Mana flows more freely through you.",
                        new[] { new SetBonusStat("INT", 10f), new SetBonusStat("ManaRegen%", 20f) }),
                    new SetBonus(4, "Arcane Mastery", "Your spells carry devastating force.",
                        new[] { new SetBonusStat("SpellDmg%", 30f) })
                }));

            RegisterSet(new ItemSetDefinition("NaturesBlessing", "Nature's Blessing",
                new[] { "nature_crown", "nature_vest", "nature_ring" },
                new[] {
                    new SetBonus(2, "Nature's Vitality", "The forest lends you its resilience.",
                        new[] { new SetBonusStat("VIT", 15f), new SetBonusStat("HpRegen%", 10f) }),
                    new SetBonus(3, "Nature's Ward", "All elements bend around your shield.",
                        new[] { new SetBonusStat("AllResist", 15f) })
                }));

            RegisterSet(new ItemSetDefinition("DragonSlayer", "Dragon Slayer",
                new[] { "dragon_helm", "dragon_armor", "dragon_greaves", "dragon_blade" },
                new[] {
                    new SetBonus(2, "Dragon Hunter", "Born to slay the mightiest beasts.",
                        new[] { new SetBonusStat("ATK%", 10f), new SetBonusStat("BossDmg%", 15f) }),
                    new SetBonus(4, "Dragon's Bane", "No dragon can withstand your onslaught.",
                        new[] { new SetBonusStat("CritRate%", 20f), new SetBonusStat("CritDmg%", 40f) })
                }));

            RegisterSet(new ItemSetDefinition("TreasureHunter", "Treasure Hunter",
                new[] { "treasure_hat", "treasure_vest", "treasure_gloves" },
                new[] {
                    new SetBonus(2, "Lucky Find", "Fortune favors the bold.",
                        new[] { new SetBonusStat("LUK", 10f), new SetBonusStat("GoldFind%", 15f) }),
                    new SetBonus(3, "Treasure Sense", "Rare items gravitate toward you.",
                        new[] { new SetBonusStat("MagicFind%", 20f) })
                }));

            RegisterSet(new ItemSetDefinition("ElementalMastery", "Elemental Mastery",
                new[] { "elemental_crown", "elemental_robe", "elemental_ring", "elemental_staff" },
                new[] {
                    new SetBonus(2, "Elemental Attunement", "The elements heed your call.",
                        new[] { new SetBonusStat("AllElemental%", 10f) }),
                    new SetBonus(3, "Elemental Shield", "Elements form a protective barrier.",
                        new[] { new SetBonusStat("ElementalResist%", 20f) }),
                    new SetBonus(4, "Elemental Overlord", "Command all elements with absolute authority.",
                        new[] { new SetBonusStat("ElementalDmg%", 35f) })
                }));
        }

        private void RegisterSet(ItemSetDefinition definition)
        {
            setDefinitions[definition.setId] = definition;
        }

        public void UpdatePlayerEquipment(ulong clientId, string[] equippedItemIds)
        {
            if (!IsServer) return;

            if (!playerEquippedSetPieces.ContainsKey(clientId))
                playerEquippedSetPieces[clientId] = new HashSet<string>();

            var previousBonuses = GetActiveSetBonuses(clientId);
            playerEquippedSetPieces[clientId] = new HashSet<string>(equippedItemIds ?? System.Array.Empty<string>());
            var currentBonuses = GetActiveSetBonuses(clientId);

            foreach (var current in currentBonuses)
            {
                var previous = previousBonuses.Find(b => b.setId == current.setId);
                if (previous == null || current.equippedPieces > previous.equippedPieces)
                {
                    NotifySetBonusActivatedClientRpc(current.setName, current.equippedPieces, clientId);
                }
            }

            SyncSetBonusesToClient(clientId, currentBonuses);
        }

        public List<ActiveSetBonus> GetActiveSetBonuses(ulong clientId)
        {
            var result = new List<ActiveSetBonus>();
            if (!playerEquippedSetPieces.ContainsKey(clientId)) return result;

            var equipped = playerEquippedSetPieces[clientId];

            foreach (var setDef in setDefinitions.Values)
            {
                int matchCount = setDef.pieceItemIds.Count(id => equipped.Contains(id));
                if (matchCount < 2) continue;

                var activeSet = new ActiveSetBonus(setDef.setId, setDef.setName, matchCount, setDef.pieceItemIds.Length);

                foreach (var bonus in setDef.bonuses)
                {
                    if (matchCount >= bonus.requiredPieces)
                    {
                        foreach (var stat in bonus.stats)
                            activeSet.activeStats.Add(new SetBonusStat(stat.statName, stat.value));
                    }
                }

                if (activeSet.activeStats.Count > 0)
                    result.Add(activeSet);
            }

            return result;
        }

        public Dictionary<string, float> GetTotalSetStatBonuses(ulong clientId)
        {
            var totals = new Dictionary<string, float>();
            var activeBonuses = GetActiveSetBonuses(clientId);

            foreach (var setBonus in activeBonuses)
            {
                foreach (var stat in setBonus.activeStats)
                {
                    if (totals.ContainsKey(stat.statName))
                        totals[stat.statName] += stat.value;
                    else
                        totals[stat.statName] = stat.value;
                }
            }

            return totals;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestSetInfoServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var activeBonuses = GetActiveSetBonuses(clientId);
            SyncSetBonusesToClient(clientId, activeBonuses);
        }

        private void SyncSetBonusesToClient(ulong clientId, List<ActiveSetBonus> bonuses)
        {
            string[] setIds = bonuses.Select(b => b.setId).ToArray();
            int[] pieces = bonuses.Select(b => b.equippedPieces).ToArray();
            int[] totals = bonuses.Select(b => b.totalPieces).ToArray();

            var target = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            };

            SyncSetBonusesClientRpc(string.Join(",", setIds), string.Join(",", pieces), string.Join(",", totals), target);
        }

        [ClientRpc]
        public void SyncSetBonusesClientRpc(string setIdsCsv, string equippedCountsCsv, string totalCountsCsv,
            ClientRpcParams clientRpcParams = default)
        {
            localActiveSetBonuses.Clear();
            if (string.IsNullOrEmpty(setIdsCsv)) { OnSetBonusesChanged?.Invoke(); return; }
            var setIds = setIdsCsv.Split(',');
            var eqParts = equippedCountsCsv.Split(',');
            var toParts = totalCountsCsv.Split(',');

            for (int i = 0; i < setIds.Length; i++)
            {
                if (!setDefinitions.ContainsKey(setIds[i])) continue;
                var setDef = setDefinitions[setIds[i]];
                int eq = i < eqParts.Length && int.TryParse(eqParts[i], out int e) ? e : 0;
                int to = i < toParts.Length && int.TryParse(toParts[i], out int t) ? t : 0;
                var activeSet = new ActiveSetBonus(setIds[i], setDef.setName, eq, to);

                foreach (var bonus in setDef.bonuses)
                {
                    if (eq >= bonus.requiredPieces)
                    {
                        foreach (var stat in bonus.stats)
                            activeSet.activeStats.Add(new SetBonusStat(stat.statName, stat.value));
                    }
                }

                localActiveSetBonuses.Add(activeSet);
            }

            OnSetBonusesChanged?.Invoke();
        }

        [ClientRpc]
        public void NotifySetBonusActivatedClientRpc(string setName, int pieces, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            string msg = $"Set Bonus Activated: {setName} ({pieces}pc)";
            UnityEngine.Debug.Log(msg);
            NotificationManager.Instance?.ShowNotification(msg, NotificationType.System);
        }

        public List<ActiveSetBonus> GetLocalActiveSetBonuses()
        {
            return new List<ActiveSetBonus>(localActiveSetBonuses);
        }

        public ItemSetDefinition GetSetDefinition(string setId)
        {
            return setDefinitions.TryGetValue(setId, out var def) ? def : null;
        }

        public List<ItemSetDefinition> GetAllSetDefinitions()
        {
            return new List<ItemSetDefinition>(setDefinitions.Values);
        }
    }
}
