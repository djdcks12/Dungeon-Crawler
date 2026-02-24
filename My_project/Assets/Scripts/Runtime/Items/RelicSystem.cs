using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public enum RelicGrade
    {
        Epic,
        Legendary,
        Mythic
    }

    [System.Serializable]
    public class RelicTemplate
    {
        public string templateId;
        public string displayName;
        public Dictionary<string, float> baseBonuses;

        public RelicTemplate(string id, string name, Dictionary<string, float> bonuses)
        {
            templateId = id;
            displayName = name;
            baseBonuses = bonuses;
        }
    }

    [System.Serializable]
    public class RelicInstance
    {
        public string instanceId;
        public string templateId;
        public RelicGrade grade;
        public int enhanceLevel;

        public RelicInstance(string templateId, RelicGrade grade)
        {
            this.instanceId = System.Guid.NewGuid().ToString();
            this.templateId = templateId;
            this.grade = grade;
            this.enhanceLevel = 0;
        }

        public float GetEnhanceMultiplier()
        {
            return 1f + (enhanceLevel * 0.2f);
        }
    }

    public class RelicSystem : NetworkBehaviour
    {
        public static RelicSystem Instance { get; private set; }

        public event System.Action OnRelicsUpdated;
        public event System.Action<string> OnRelicEnhanced;

        private const int MaxEnhanceLevel = 5;
        private const int MaxRelicsPerPlayer = 20;

        private Dictionary<string, RelicTemplate> relicTemplates = new Dictionary<string, RelicTemplate>();
        private Dictionary<ulong, List<RelicInstance>> playerRelics = new Dictionary<ulong, List<RelicInstance>>();
        private Dictionary<ulong, string> equippedRelicMap = new Dictionary<ulong, string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeTemplates();
        }

        public override void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            base.OnDestroy();
        }

        private void InitializeTemplates()
        {
            relicTemplates.Clear();

            RegisterTemplate("HeartOfFire", "Heart of Fire", new Dictionary<string, float>
            {
                { "ATK", 0.30f }, { "FireDmg", 0.50f }
            });
            RegisterTemplate("ShieldOfEternity", "Shield of Eternity", new Dictionary<string, float>
            {
                { "DEF", 0.50f }, { "BlockRate", 0.25f }
            });
            RegisterTemplate("CrownOfShadows", "Crown of Shadows", new Dictionary<string, float>
            {
                { "CritRate", 0.25f }, { "CritDmg", 0.50f }
            });
            RegisterTemplate("StaffOfWisdom", "Staff of Wisdom", new Dictionary<string, float>
            {
                { "INT", 40f }, { "SpellDmg", 0.35f }
            });
            RegisterTemplate("BootsOfWind", "Boots of Wind", new Dictionary<string, float>
            {
                { "Speed", 0.50f }, { "Evasion", 0.20f }
            });
            RegisterTemplate("GauntletsOfTitan", "Gauntlets of Titan", new Dictionary<string, float>
            {
                { "STR", 30f }, { "MaxHP", 0.25f }
            });
            RegisterTemplate("AmuletOfFortune", "Amulet of Fortune", new Dictionary<string, float>
            {
                { "LUK", 20f }, { "MagicFind", 0.30f }
            });
            RegisterTemplate("RingOfEternity", "Ring of Eternity", new Dictionary<string, float>
            {
                { "AllStats", 15f }, { "ExpBonus", 0.20f }
            });
            RegisterTemplate("CloakOfNight", "Cloak of Night", new Dictionary<string, float>
            {
                { "Stealth", 1f }, { "CritDmg", 0.40f }
            });
            RegisterTemplate("HelmOfValor", "Helm of Valor", new Dictionary<string, float>
            {
                { "DEF", 0.30f }, { "AllResist", 20f }
            });
            RegisterTemplate("BeltOfGiants", "Belt of Giants", new Dictionary<string, float>
            {
                { "VIT", 30f }, { "HPRegen", 0.50f }
            });
            RegisterTemplate("OrbOfChaos", "Orb of Chaos", new Dictionary<string, float>
            {
                { "AllDmg", 0.25f }, { "RandomProc", 1f }
            });
        }

        private void RegisterTemplate(string id, string name, Dictionary<string, float> bonuses)
        {
            relicTemplates[id] = new RelicTemplate(id, name, bonuses);
        }

        public void AddRelicToPlayer(ulong clientId, string templateId, RelicGrade grade)
        {
            if (!IsServer) return;
            if (!relicTemplates.ContainsKey(templateId))
            {
                Debug.LogWarning($"[RelicSystem] Unknown template: {templateId}");
                return;
            }

            if (!playerRelics.ContainsKey(clientId))
                playerRelics[clientId] = new List<RelicInstance>();

            if (playerRelics[clientId].Count >= MaxRelicsPerPlayer)
            {
                NotifyClientRpc(clientId, "Relic inventory is full.", true);
                return;
            }

            var instance = new RelicInstance(templateId, grade);
            playerRelics[clientId].Add(instance);

            string gradeName = grade.ToString();
            string relicName = relicTemplates[templateId].displayName;
            NotifyClientRpc(clientId, $"Acquired {gradeName} relic: {relicName}", false);
            OnRelicsUpdated?.Invoke();
        }

        public Dictionary<string, float> GetEquippedRelicBonuses(ulong clientId)
        {
            var bonuses = new Dictionary<string, float>();

            if (!equippedRelicMap.TryGetValue(clientId, out string equippedId))
                return bonuses;

            if (!playerRelics.ContainsKey(clientId))
                return bonuses;

            var relic = playerRelics[clientId].FirstOrDefault(r => r.instanceId == equippedId);
            if (relic == null)
                return bonuses;

            if (!relicTemplates.TryGetValue(relic.templateId, out var template))
                return bonuses;

            float multiplier = relic.GetEnhanceMultiplier();
            float gradeMultiplier = GetGradeMultiplier(relic.grade);

            foreach (var kvp in template.baseBonuses)
            {
                bonuses[kvp.Key] = kvp.Value * multiplier * gradeMultiplier;
            }

            return bonuses;
        }

        private float GetGradeMultiplier(RelicGrade grade)
        {
            switch (grade)
            {
                case RelicGrade.Epic: return 1.0f;
                case RelicGrade.Legendary: return 1.3f;
                case RelicGrade.Mythic: return 1.6f;
                default: return 1.0f;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void EquipRelicServerRpc(string relicInstanceId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!playerRelics.ContainsKey(clientId))
            {
                NotifyClientRpc(clientId, "No relics found.", true);
                return;
            }

            var relic = playerRelics[clientId].FirstOrDefault(r => r.instanceId == relicInstanceId);
            if (relic == null)
            {
                NotifyClientRpc(clientId, "Relic not found in inventory.", true);
                return;
            }

            equippedRelicMap[clientId] = relicInstanceId;

            string relicName = relicTemplates.ContainsKey(relic.templateId)
                ? relicTemplates[relic.templateId].displayName
                : relic.templateId;
            NotifyClientRpc(clientId, $"Equipped relic: {relicName}", false);
            OnRelicsUpdated?.Invoke();
        }

        [ServerRpc(RequireOwnership = false)]
        public void UnequipRelicServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!equippedRelicMap.ContainsKey(clientId))
            {
                NotifyClientRpc(clientId, "No relic is equipped.", true);
                return;
            }

            equippedRelicMap.Remove(clientId);
            NotifyClientRpc(clientId, "Relic unequipped.", false);
            OnRelicsUpdated?.Invoke();
        }

        [ServerRpc(RequireOwnership = false)]
        public void EnhanceRelicServerRpc(string baseId, string materialId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!playerRelics.ContainsKey(clientId))
            {
                NotifyClientRpc(clientId, "No relics found.", true);
                return;
            }

            var relics = playerRelics[clientId];
            var baseRelic = relics.FirstOrDefault(r => r.instanceId == baseId);
            var materialRelic = relics.FirstOrDefault(r => r.instanceId == materialId);

            if (baseRelic == null || materialRelic == null)
            {
                NotifyClientRpc(clientId, "Required relics not found.", true);
                return;
            }

            if (baseRelic.templateId != materialRelic.templateId)
            {
                NotifyClientRpc(clientId, "Both relics must be the same type to enhance.", true);
                return;
            }

            if (baseRelic.enhanceLevel >= MaxEnhanceLevel)
            {
                NotifyClientRpc(clientId, "Relic is already at maximum enhancement level.", true);
                return;
            }

            // Remove material relic and unequip if it was equipped
            if (equippedRelicMap.TryGetValue(clientId, out string equippedId) && equippedId == materialId)
            {
                equippedRelicMap.Remove(clientId);
            }
            relics.Remove(materialRelic);

            baseRelic.enhanceLevel++;

            string relicName = relicTemplates.ContainsKey(baseRelic.templateId)
                ? relicTemplates[baseRelic.templateId].displayName
                : baseRelic.templateId;
            NotifyClientRpc(clientId, $"{relicName} enhanced to +{baseRelic.enhanceLevel}", false);
            OnRelicEnhanced?.Invoke(baseRelic.instanceId);
            OnRelicsUpdated?.Invoke();
        }

        public List<RelicInstance> GetPlayerRelics(ulong clientId)
        {
            if (playerRelics.TryGetValue(clientId, out var relics))
                return new List<RelicInstance>(relics);
            return new List<RelicInstance>();
        }

        public string GetEquippedRelicId(ulong clientId)
        {
            equippedRelicMap.TryGetValue(clientId, out string equippedId);
            return equippedId;
        }

        public RelicTemplate GetTemplate(string templateId)
        {
            relicTemplates.TryGetValue(templateId, out var template);
            return template;
        }

        [ClientRpc]
        private void NotifyClientRpc(ulong targetClientId, string message, bool isWarning,
            ClientRpcParams clientRpcParams = default)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId)
                return;

            if (isWarning)
                NotificationManager.Instance?.ShowNotification(message, NotificationType.Warning);
            else
                NotificationManager.Instance?.ShowNotification(message, NotificationType.System);

            Debug.Log($"[RelicSystem] {message}");
        }

        public void RemovePlayerData(ulong clientId)
        {
            if (!IsServer) return;
            playerRelics.Remove(clientId);
            equippedRelicMap.Remove(clientId);
            OnRelicsUpdated?.Invoke();
        }
    }
}
