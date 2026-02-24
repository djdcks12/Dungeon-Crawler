using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class LegendaryAspectSystem : NetworkBehaviour
    {
        public static LegendaryAspectSystem Instance { get; private set; }

        private Dictionary<ulong, HashSet<string>> unlockedAspects = new Dictionary<ulong, HashSet<string>>();
        private Dictionary<ulong, Dictionary<string, string>> equippedAspects = new Dictionary<ulong, Dictionary<string, string>>();

        private static readonly AspectTemplate[] aspects = new AspectTemplate[]
        {
            new AspectTemplate("aspect_berserker", "Berserker Aspect", AspectCategory.Offensive, "Critical hits grant +15% attack speed for 3s", 0.15f),
            new AspectTemplate("aspect_rapid", "Rapid Fire Aspect", AspectCategory.Offensive, "Attack speed increased by 10%", 0.10f),
            new AspectTemplate("aspect_piercing", "Piercing Aspect", AspectCategory.Offensive, "Attacks ignore 20% of enemy defense", 0.20f),
            new AspectTemplate("aspect_executioner", "Executioner Aspect", AspectCategory.Offensive, "Deal 30% more damage to enemies below 30% HP", 0.30f),
            new AspectTemplate("aspect_nova", "Nova Aspect", AspectCategory.Offensive, "Skills have 15% chance to trigger an elemental nova", 0.15f),
            new AspectTemplate("aspect_fortress", "Fortress Aspect", AspectCategory.Defensive, "While above 50% HP, gain 10% damage reduction", 0.10f),
            new AspectTemplate("aspect_thorns", "Thorns Aspect", AspectCategory.Defensive, "Reflect 20% of received damage to attacker", 0.20f),
            new AspectTemplate("aspect_barrier", "Barrier Aspect", AspectCategory.Defensive, "Generate a barrier for 5% max HP every 5s", 0.05f),
            new AspectTemplate("aspect_lifesteal", "Lifesteal Aspect", AspectCategory.Defensive, "Heal for 3% of damage dealt", 0.03f),
            new AspectTemplate("aspect_resolve", "Iron Resolve Aspect", AspectCategory.Defensive, "Reduce crowd control duration by 25%", 0.25f),
            new AspectTemplate("aspect_wind", "Wind Walker Aspect", AspectCategory.Mobility, "Movement speed increased by 8%", 0.08f),
            new AspectTemplate("aspect_blink", "Blink Aspect", AspectCategory.Mobility, "Dodge rolls travel 30% further", 0.30f),
            new AspectTemplate("aspect_pursuit", "Pursuit Aspect", AspectCategory.Mobility, "Move 15% faster toward enemies", 0.15f),
            new AspectTemplate("aspect_evasion", "Evasion Aspect", AspectCategory.Mobility, "5% chance to completely dodge attacks", 0.05f),
            new AspectTemplate("aspect_dash", "Dash Aspect", AspectCategory.Mobility, "After killing an enemy, gain 20% move speed for 2s", 0.20f),
            new AspectTemplate("aspect_greed", "Greed Aspect", AspectCategory.Utility, "Gold drops increased by 15%", 0.15f),
            new AspectTemplate("aspect_fortune", "Fortune Aspect", AspectCategory.Utility, "Magic find increased by 10%", 0.10f),
            new AspectTemplate("aspect_wisdom", "Wisdom Aspect", AspectCategory.Utility, "Experience gained increased by 8%", 0.08f),
            new AspectTemplate("aspect_harvest", "Harvest Aspect", AspectCategory.Utility, "20% chance for double loot from bosses", 0.20f),
            new AspectTemplate("aspect_endurance", "Endurance Aspect", AspectCategory.Utility, "Potion effectiveness increased by 25%", 0.25f)
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
        public void ExtractAspectServerRpc(string aspectId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (FindAspect(aspectId) == null) return;

            if (!unlockedAspects.ContainsKey(clientId))
                unlockedAspects[clientId] = new HashSet<string>();

            if (unlockedAspects[clientId].Contains(aspectId)) return;

            unlockedAspects[clientId].Add(aspectId);
            NotifyAspectUnlockedClientRpc(aspectId, clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ImprintAspectServerRpc(string aspectId, string itemId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!unlockedAspects.ContainsKey(clientId) || !unlockedAspects[clientId].Contains(aspectId))
            {
                NotifyAspectFailClientRpc("Aspect not unlocked!", clientId);
                return;
            }

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var clientData)) return;
            var playerObj = clientData.PlayerObject;
            if (playerObj == null) return;
            var stats = playerObj.GetComponent<PlayerStatsManager>()?.CurrentStats;
            if (stats == null) return;

            long cost = 5000;
            if (stats.Gold < cost)
            {
                NotifyAspectFailClientRpc("Not enough gold for imprinting!", clientId);
                return;
            }

            stats.ChangeGold(-cost);

            if (!equippedAspects.ContainsKey(clientId))
                equippedAspects[clientId] = new Dictionary<string, string>();

            equippedAspects[clientId][itemId] = aspectId;
            NotifyAspectImprintedClientRpc(aspectId, itemId, clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemoveAspectServerRpc(string itemId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!equippedAspects.ContainsKey(clientId)) return;
            if (!equippedAspects[clientId].ContainsKey(itemId)) return;

            equippedAspects[clientId].Remove(itemId);
            NotifyAspectRemovedClientRpc(itemId, clientId);
        }

        public AspectTemplate FindAspect(string aspectId)
        {
            foreach (var a in aspects)
            {
                if (a.aspectId == aspectId) return a;
            }
            return null;
        }

        public bool IsAspectUnlocked(ulong clientId, string aspectId)
        {
            if (!unlockedAspects.ContainsKey(clientId)) return false;
            return unlockedAspects[clientId].Contains(aspectId);
        }

        public string GetItemAspect(ulong clientId, string itemId)
        {
            if (!equippedAspects.ContainsKey(clientId)) return null;
            if (!equippedAspects[clientId].ContainsKey(itemId)) return null;
            return equippedAspects[clientId][itemId];
        }

        public List<string> GetUnlockedAspects(ulong clientId)
        {
            if (!unlockedAspects.ContainsKey(clientId)) return new List<string>();
            return new List<string>(unlockedAspects[clientId]);
        }

        public float GetAspectValue(string aspectId)
        {
            var aspect = FindAspect(aspectId);
            return aspect != null ? aspect.value : 0f;
        }

        public AspectTemplate[] GetAllAspects() => aspects;
        public int TotalAspects => aspects.Length;

        public int GetUnlockedCount(ulong clientId)
        {
            if (!unlockedAspects.ContainsKey(clientId)) return 0;
            return unlockedAspects[clientId].Count;
        }

        [ClientRpc]
        private void NotifyAspectUnlockedClientRpc(string aspectId, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            var aspect = FindAspect(aspectId);
            if (aspect != null && NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(
                    "Aspect unlocked: " + aspect.aspectName, NotificationType.System);
        }

        [ClientRpc]
        private void NotifyAspectImprintedClientRpc(string aspectId, string itemId, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            var aspect = FindAspect(aspectId);
            if (aspect != null && NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(
                    aspect.aspectName + " imprinted on item!", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyAspectRemovedClientRpc(string itemId, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(
                    "Aspect removed from item.", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyAspectFailClientRpc(string message, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(message, NotificationType.Warning);
        }
    }

    public enum AspectCategory { Offensive, Defensive, Mobility, Utility }

    [System.Serializable]
    public class AspectTemplate
    {
        public string aspectId;
        public string aspectName;
        public AspectCategory category;
        public string description;
        public float value;

        public AspectTemplate(string id, string name, AspectCategory cat, string desc, float val)
        {
            aspectId = id;
            aspectName = name;
            category = cat;
            description = desc;
            value = val;
        }
    }
}
