using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class SkillMutationSystem : NetworkBehaviour
    {
        public static SkillMutationSystem Instance { get; private set; }

        private Dictionary<ulong, Dictionary<string, MutationSlotData>> playerMutations = new Dictionary<ulong, Dictionary<string, MutationSlotData>>();
        private Dictionary<ulong, Dictionary<MutationType, int>> playerMutationStones = new Dictionary<ulong, Dictionary<MutationType, int>>();

        public System.Action OnMutationsUpdated;
        private Dictionary<string, MutationSlotData> localMutations = new Dictionary<string, MutationSlotData>();

        private static readonly Dictionary<MutationType, MutationTemplate> mutationTemplates = new Dictionary<MutationType, MutationTemplate>
        {
            { MutationType.FireConvert, new MutationTemplate("Fire Convert", "Converts to fire, +5% dmg", MutationCategory.Elemental, 0.05f, 0f, DamageType.Physical) },
            { MutationType.IceConvert, new MutationTemplate("Ice Convert", "Converts to ice, 10% slow", MutationCategory.Elemental, 0f, 0.10f, DamageType.Physical) },
            { MutationType.LightningConvert, new MutationTemplate("Lightning Convert", "To lightning, +8% crit", MutationCategory.Elemental, 0.08f, 0f, DamageType.Physical) },
            { MutationType.PoisonConvert, new MutationTemplate("Poison Convert", "Converts to poison, 3s DoT", MutationCategory.Elemental, 0f, 0f, DamageType.Physical) },
            { MutationType.HolyConvert, new MutationTemplate("Holy Convert", "Converts to holy, +5% heal", MutationCategory.Elemental, 0f, 0.05f, DamageType.Magical) },
            { MutationType.AreaExpand, new MutationTemplate("Area Expand", "Range +40%, damage -10%", MutationCategory.Behavior, -0.10f, 0.40f, DamageType.Physical) },
            { MutationType.Chain, new MutationTemplate("Chain", "Chain to 2 nearby at 50% dmg", MutationCategory.Behavior, 0f, 0f, DamageType.Physical) },
            { MutationType.Pierce, new MutationTemplate("Pierce", "Pierce enemies at 70% dmg", MutationCategory.Behavior, 0f, 0f, DamageType.Physical) },
            { MutationType.Split, new MutationTemplate("Split", "Split into 3 at 60% each", MutationCategory.Behavior, -0.40f, 0f, DamageType.Physical) },
            { MutationType.Focus, new MutationTemplate("Focus", "Range -50%, damage +30%", MutationCategory.Behavior, 0.30f, -0.50f, DamageType.Physical) },
        };

        private static readonly MutationSynergy[] synergies = new MutationSynergy[]
        {
            new MutationSynergy(MutationType.FireConvert, MutationType.Chain, "Fire Chain", "Chain dmg +20%", 0.20f),
            new MutationSynergy(MutationType.IceConvert, MutationType.AreaExpand, "Glacier Storm", "Slow 2x, 10% freeze", 0.15f),
            new MutationSynergy(MutationType.LightningConvert, MutationType.Split, "Lightning Scatter", "5 splits, 5% shock", 0.10f),
            new MutationSynergy(MutationType.PoisonConvert, MutationType.Pierce, "Venom Pierce", "DoT 2x, unlimited pierce", 0.25f),
            new MutationSynergy(MutationType.HolyConvert, MutationType.Focus, "Divine Focus", "Heal +15%, crit heals", 0.20f),
        };

        private static readonly int[] mutationStoneCost = { 1, 2 };

        public override void OnNetworkSpawn()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (IsClient) RequestSyncServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ApplyMutationServerRpc(string skillId, int slotIndex, int mutationTypeInt, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (slotIndex < 0 || slotIndex > 1) return;
            if (!System.Enum.IsDefined(typeof(MutationType), mutationTypeInt)) return;

            var mutationType = (MutationType)mutationTypeInt;
            int stoneCost = mutationStoneCost[slotIndex];

            var stones = GetOrCreateStones(clientId);
            if (!stones.ContainsKey(mutationType)) stones[mutationType] = 0;
            if (stones[mutationType] < stoneCost)
            {
                NotifyClientRpc("Mutation stone insufficient", clientId);
                return;
            }

            var mutations = GetOrCreateMutations(clientId);
            if (mutations.TryGetValue(skillId, out var existing))
            {
                if (slotIndex == 0 && existing.mutation1 == mutationType) return;
                if (slotIndex == 1 && existing.mutation2 == mutationType) return;
                if (slotIndex == 0 && existing.mutation2 == mutationType) return;
                if (slotIndex == 1 && existing.mutation1 == mutationType) return;
            }

            stones[mutationType] -= stoneCost;

            if (!mutations.ContainsKey(skillId))
                mutations[skillId] = new MutationSlotData();

            var slotData = mutations[skillId];
            if (slotIndex == 0) slotData.mutation1 = mutationType;
            else slotData.mutation2 = mutationType;
            mutations[skillId] = slotData;

            string synergyName = "";
            if (slotData.mutation1 != MutationType.None && slotData.mutation2 != MutationType.None)
            {
                foreach (var syn in synergies)
                {
                    if ((syn.mutation1 == slotData.mutation1 && syn.mutation2 == slotData.mutation2) ||
                        (syn.mutation1 == slotData.mutation2 && syn.mutation2 == slotData.mutation1))
                    {
                        synergyName = syn.name;
                        break;
                    }
                }
            }
            MutationAppliedClientRpc(skillId, slotIndex, mutationTypeInt, synergyName, clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemoveMutationServerRpc(string skillId, int slotIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (slotIndex < 0 || slotIndex > 1) return;
            var mutations = GetOrCreateMutations(clientId);
            if (!mutations.ContainsKey(skillId)) return;
            var slotData = mutations[skillId];
            if (slotIndex == 0) slotData.mutation1 = MutationType.None;
            else slotData.mutation2 = MutationType.None;
            mutations[skillId] = slotData;
            MutationRemovedClientRpc(skillId, slotIndex, clientId);
        }

        public void AddMutationStone(ulong clientId, MutationType type, int amount)
        {
            if (!IsServer) return;
            var stones = GetOrCreateStones(clientId);
            if (!stones.ContainsKey(type)) stones[type] = 0;
            stones[type] += amount;
            StoneAddedClientRpc((int)type, amount, stones[type], clientId);
        }

        public MutationEffect CalculateMutationEffect(string skillId)
        {
            var effect = new MutationEffect();
            if (!localMutations.TryGetValue(skillId, out var slotData)) return effect;

            if (slotData.mutation1 != MutationType.None && mutationTemplates.TryGetValue(slotData.mutation1, out var tmpl1))
            {
                effect.damageMultiplier += tmpl1.damageModifier;
                effect.rangeMultiplier += tmpl1.rangeModifier;
                effect.mutations.Add(slotData.mutation1);
                if (tmpl1.category == MutationCategory.Elemental)
                    effect.convertedElement = slotData.mutation1;
            }

            if (slotData.mutation2 != MutationType.None && mutationTemplates.TryGetValue(slotData.mutation2, out var tmpl2))
            {
                effect.damageMultiplier += tmpl2.damageModifier;
                effect.rangeMultiplier += tmpl2.rangeModifier;
                effect.mutations.Add(slotData.mutation2);
                if (tmpl2.category == MutationCategory.Elemental && effect.convertedElement == MutationType.None)
                    effect.convertedElement = slotData.mutation2;
            }

            if (slotData.mutation1 != MutationType.None && slotData.mutation2 != MutationType.None)
            {
                foreach (var syn in synergies)
                {
                    if ((syn.mutation1 == slotData.mutation1 && syn.mutation2 == slotData.mutation2) ||
                        (syn.mutation1 == slotData.mutation2 && syn.mutation2 == slotData.mutation1))
                    {
                        effect.synergyBonus = syn.bonusMult;
                        effect.synergyName = syn.name;
                        effect.damageMultiplier += syn.bonusMult;
                        break;
                    }
                }
            }
            return effect;
        }

        public MutationSlotData GetSkillMutations(string skillId)
        {
            return localMutations.TryGetValue(skillId, out var data) ? data : new MutationSlotData();
        }

        public static MutationTemplate GetTemplate(MutationType type)
        {
            return mutationTemplates.TryGetValue(type, out var t) ? t : null;
        }

        public static MutationSynergy[] GetAllSynergies() => synergies;

        #region Sync

        [ServerRpc(RequireOwnership = false)]
        private void RequestSyncServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var mutations = GetOrCreateMutations(clientId);
            ClearLocalClientRpc(clientId);
            foreach (var kvp in mutations)
                SyncMutationClientRpc(kvp.Key, (int)kvp.Value.mutation1, (int)kvp.Value.mutation2, clientId);
            SyncCompleteClientRpc(clientId);
        }

        [ClientRpc]
        private void ClearLocalClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localMutations.Clear();
        }

        [ClientRpc]
        private void SyncMutationClientRpc(string skillId, int m1, int m2, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localMutations[skillId] = new MutationSlotData { mutation1 = (MutationType)m1, mutation2 = (MutationType)m2 };
        }

        [ClientRpc]
        private void SyncCompleteClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            OnMutationsUpdated?.Invoke();
        }

        [ClientRpc]
        private void MutationAppliedClientRpc(string skillId, int slot, int mutTypeInt, string synergyName, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            if (!localMutations.ContainsKey(skillId))
                localMutations[skillId] = new MutationSlotData();
            var data = localMutations[skillId];
            if (slot == 0) data.mutation1 = (MutationType)mutTypeInt;
            else data.mutation2 = (MutationType)mutTypeInt;
            localMutations[skillId] = data;
            OnMutationsUpdated?.Invoke();
        }

        [ClientRpc]
        private void MutationRemovedClientRpc(string skillId, int slot, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            if (!localMutations.ContainsKey(skillId)) return;
            var data = localMutations[skillId];
            if (slot == 0) data.mutation1 = MutationType.None;
            else data.mutation2 = MutationType.None;
            localMutations[skillId] = data;
            OnMutationsUpdated?.Invoke();
        }

        [ClientRpc]
        private void StoneAddedClientRpc(int typeInt, int amount, int total, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            var tmpl = mutationTemplates.TryGetValue((MutationType)typeInt, out var t) ? t : null;
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification((tmpl != null ? tmpl.name : "Unknown") + " stone +" + amount, NotificationType.System);
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

        private Dictionary<string, MutationSlotData> GetOrCreateMutations(ulong clientId)
        {
            if (!playerMutations.ContainsKey(clientId))
                playerMutations[clientId] = new Dictionary<string, MutationSlotData>();
            return playerMutations[clientId];
        }

        private Dictionary<MutationType, int> GetOrCreateStones(ulong clientId)
        {
            if (!playerMutationStones.ContainsKey(clientId))
                playerMutationStones[clientId] = new Dictionary<MutationType, int>();
            return playerMutationStones[clientId];
        }

        #endregion

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }

    public enum MutationType
    {
        None = 0, FireConvert = 1, IceConvert = 2, LightningConvert = 3,
        PoisonConvert = 4, HolyConvert = 5, AreaExpand = 6, Chain = 7,
        Pierce = 8, Split = 9, Focus = 10
    }

    public enum MutationCategory { Elemental, Behavior }

    public struct MutationSlotData
    {
        public MutationType mutation1;
        public MutationType mutation2;
    }

    public class MutationTemplate
    {
        public string name;
        public string description;
        public MutationCategory category;
        public float damageModifier;
        public float rangeModifier;
        public DamageType baseDamageType;

        public MutationTemplate(string name, string desc, MutationCategory cat, float dmgMod, float rngMod, DamageType dmgType)
        {
            this.name = name; this.description = desc; this.category = cat;
            this.damageModifier = dmgMod; this.rangeModifier = rngMod; this.baseDamageType = dmgType;
        }
    }

    public class MutationSynergy
    {
        public MutationType mutation1;
        public MutationType mutation2;
        public string name;
        public string description;
        public float bonusMult;

        public MutationSynergy(MutationType m1, MutationType m2, string name, string desc, float bonus)
        {
            this.mutation1 = m1; this.mutation2 = m2; this.name = name;
            this.description = desc; this.bonusMult = bonus;
        }
    }

    public class MutationEffect
    {
        public float damageMultiplier = 0f;
        public float rangeMultiplier = 0f;
        public MutationType convertedElement = MutationType.None;
        public float synergyBonus = 0f;
        public string synergyName = "";
        public List<MutationType> mutations = new List<MutationType>();
    }
}
