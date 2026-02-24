using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public enum PetGrade { Common, Rare, Epic, Legendary }
    public enum PetRole { Attacker, Defender, Collector, Buffer }

    [Serializable]
    public class PetTemplate
    {
        public string petId;
        public string petName;
        public PetGrade grade;
        public PetRole role;
        public float baseAttack;
        public float baseDefense;
        public float baseSpeed;
        public string skillName;
        public string skillDescription;
        public float skillCooldown;
        public float skillValue;
    }

    [Serializable]
    public class PetInstance
    {
        public string instanceId;
        public string templateId;
        public int level;
        public int exp;
        public PetGrade grade;
        public string equippedAccessoryId;

        public PetInstance(string templateId, PetGrade grade)
        {
            this.instanceId = Guid.NewGuid().ToString("N").Substring(0, 8);
            this.templateId = templateId;
            this.level = 1;
            this.exp = 0;
            this.grade = grade;
            this.equippedAccessoryId = string.Empty;
        }
    }

    [Serializable]
    public class PetInstanceInfo
    {
        public string instanceId;
        public string templateId;
        public int level;
        public int exp;
    }

    public struct PetBonus
    {
        public float atkBonus;
        public float defBonus;
        public float speedBonus;
        public float critBonus;
        public float luckBonus;
        public float expBonus;
    }

    public class PetSystem : NetworkBehaviour
    {
        public static PetSystem Instance { get; private set; }

        public event Action OnPetsUpdated;
        public event Action<string> OnPetLevelUp;

        // Server state
        private Dictionary<ulong, List<PetInstance>> playerPets = new Dictionary<ulong, List<PetInstance>>();
        private Dictionary<ulong, string> activePetMap = new Dictionary<ulong, string>();

        // Local client state
        public List<PetInstanceInfo> localPets = new List<PetInstanceInfo>();
        public string localActivePetId;

        // Pet templates
        private Dictionary<string, PetTemplate> templates = new Dictionary<string, PetTemplate>();

        // Exp thresholds per level (index 0 = level 1->2, index 19 = level 20 cap)
        private static readonly int[] ExpThresholds = {
            100, 250, 500, 800, 1200, 1700, 2300, 3000, 3800, 4700,
            5700, 6800, 8000, 9300, 10700, 12200, 13800, 15500, 17300, 20000
        };

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeTemplates();
        }

        public override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        private void InitializeTemplates()
        {
            // Common (4)
            AddTemplate("FireSprite", "Fire Sprite", PetGrade.Common, PetRole.Attacker,
                10f, 3f, 8f, "Flame Bolt", "Shoots a fire bolt at enemies", 5f, 10f);
            AddTemplate("StoneGolem", "Stone Golem", PetGrade.Common, PetRole.Defender,
                3f, 15f, 4f, "Rock Shield", "Raises defense for owner", 10f, 15f);
            AddTemplate("PixieDust", "Pixie Dust", PetGrade.Common, PetRole.Collector,
                2f, 2f, 20f, "Gather", "Increases item pickup range", 0f, 20f);
            AddTemplate("HealFairy", "Heal Fairy", PetGrade.Common, PetRole.Buffer,
                1f, 5f, 10f, "Healing Light", "Heals owner for 5% max HP", 12f, 5f);

            // Rare (3)
            AddTemplate("ThunderHawk", "Thunder Hawk", PetGrade.Rare, PetRole.Attacker,
                20f, 8f, 15f, "Lightning Dive", "Strikes enemy with lightning", 6f, 20f);
            AddTemplate("IronTurtle", "Iron Turtle", PetGrade.Rare, PetRole.Defender,
                5f, 25f, 3f, "Iron Fortress", "Greatly raises defense", 15f, 25f);
            AddTemplate("GoldFerret", "Gold Ferret", PetGrade.Rare, PetRole.Collector,
                4f, 4f, 18f, "Gold Nose", "Increases luck by 10", 0f, 10f);

            // Epic (3)
            AddTemplate("PhoenixChick", "Phoenix Chick", PetGrade.Epic, PetRole.Attacker,
                35f, 12f, 14f, "Blazing Wing", "Burns enemies for extra damage", 8f, 35f);
            AddTemplate("CrystalDragon", "Crystal Dragon", PetGrade.Epic, PetRole.Defender,
                10f, 40f, 6f, "Prism Reflect", "Reflects damage back to attackers", 18f, 40f);
            AddTemplate("ShadowCat", "Shadow Cat", PetGrade.Epic, PetRole.Buffer,
                18f, 10f, 22f, "Shadow Cloak", "Increases critical hit chance by 15%", 10f, 15f);

            // Legendary (2)
            AddTemplate("CelestialWolf", "Celestial Wolf", PetGrade.Legendary, PetRole.Attacker,
                50f, 20f, 20f, "Astral Chain", "Chain attack hitting multiple enemies", 10f, 50f);
            AddTemplate("AncientSpirit", "Ancient Spirit", PetGrade.Legendary, PetRole.Buffer,
                25f, 25f, 18f, "Ancestral Blessing", "Boosts all stats by 10%", 20f, 10f);
        }

        private void AddTemplate(string id, string name, PetGrade grade, PetRole role,
            float atk, float def, float spd, string skillName, string skillDesc, float cooldown, float skillVal)
        {
            templates[id] = new PetTemplate
            {
                petId = id, petName = name, grade = grade, role = role,
                baseAttack = atk, baseDefense = def, baseSpeed = spd,
                skillName = skillName, skillDescription = skillDesc,
                skillCooldown = cooldown, skillValue = skillVal
            };
        }

        public PetTemplate GetTemplate(string templateId)
        {
            return templates.TryGetValue(templateId, out var t) ? t : null;
        }

        // --- Server: Grant pet to player ---
        public void AddPetToPlayer(ulong clientId, string petTemplateId)
        {
            if (!IsServer) return;
            var template = GetTemplate(petTemplateId);
            if (template == null) { Debug.LogWarning($"[PetSystem] Unknown template: {petTemplateId}"); return; }

            if (!playerPets.ContainsKey(clientId))
                playerPets[clientId] = new List<PetInstance>();

            var pet = new PetInstance(petTemplateId, template.grade);
            playerPets[clientId].Add(pet);

            NotifyPetObtainedClientRpc(template.petName, (int)template.grade,
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } });
            SyncPetDataClientRpc(pet.instanceId, pet.templateId, pet.level, pet.exp,
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } });
        }

        // --- Server: Active pet gains exp ---
        public void GainPetExp(ulong clientId, int exp)
        {
            if (!IsServer || exp <= 0) return;
            if (!activePetMap.TryGetValue(clientId, out var activePetId)) return;
            if (!playerPets.TryGetValue(clientId, out var pets)) return;

            var pet = pets.FirstOrDefault(p => p.instanceId == activePetId);
            if (pet == null || pet.level >= 20) return;

            pet.exp += exp;
            CheckLevelUp(clientId, pet);

            SyncPetDataClientRpc(pet.instanceId, pet.templateId, pet.level, pet.exp,
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } });
        }

        private void CheckLevelUp(ulong clientId, PetInstance pet)
        {
            while (pet.level < 20)
            {
                int threshold = ExpThresholds[pet.level - 1];
                if (pet.exp < threshold) break;

                pet.exp -= threshold;
                pet.level++;

                var template = GetTemplate(pet.templateId);
                string petName = template != null ? template.petName : pet.templateId;

                NotifyPetLevelUpClientRpc(petName, pet.level,
                    new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } });
            }
        }

        // --- Server: Compute pet bonus for a player ---
        public PetBonus GetActivePetBonus(ulong clientId)
        {
            var bonus = new PetBonus();
            if (!activePetMap.TryGetValue(clientId, out var activePetId)) return bonus;
            if (!playerPets.TryGetValue(clientId, out var pets)) return bonus;

            var pet = pets.FirstOrDefault(p => p.instanceId == activePetId);
            if (pet == null) return bonus;

            var template = GetTemplate(pet.templateId);
            if (template == null) return bonus;

            float levelScale = 1f + (pet.level - 1) * 0.08f;

            switch (template.role)
            {
                case PetRole.Attacker:
                    bonus.atkBonus = template.skillValue * levelScale;
                    break;
                case PetRole.Defender:
                    bonus.defBonus = template.skillValue * levelScale;
                    break;
                case PetRole.Collector:
                    if (template.petId == "GoldFerret") bonus.luckBonus = template.skillValue * levelScale;
                    else bonus.speedBonus = template.skillValue * levelScale;
                    break;
                case PetRole.Buffer:
                    if (template.petId == "ShadowCat") bonus.critBonus = template.skillValue * levelScale;
                    else if (template.petId == "AncientSpirit")
                    {
                        float val = template.skillValue * levelScale;
                        bonus.atkBonus = val; bonus.defBonus = val;
                        bonus.speedBonus = val; bonus.critBonus = val;
                        bonus.luckBonus = val; bonus.expBonus = val;
                    }
                    else bonus.expBonus = template.skillValue * levelScale;
                    break;
            }
            return bonus;
        }

        // --- ServerRpcs ---
        [ServerRpc(RequireOwnership = false)]
        public void SummonPetServerRpc(string petInstanceId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!playerPets.TryGetValue(clientId, out var pets)) return;

            var pet = pets.FirstOrDefault(p => p.instanceId == petInstanceId);
            if (pet == null) return;

            activePetMap[clientId] = petInstanceId;

            var template = GetTemplate(pet.templateId);
            string petName = template != null ? template.petName : pet.templateId;
            Debug.Log($"[PetSystem] Client {clientId} summoned {petName} (Lv.{pet.level})");
        }

        [ServerRpc(RequireOwnership = false)]
        public void DismissPetServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (activePetMap.ContainsKey(clientId))
            {
                activePetMap.Remove(clientId);
                Debug.Log($"[PetSystem] Client {clientId} dismissed pet");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void FeedPetServerRpc(string petInstanceId, int expAmount, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (expAmount <= 0) return;

            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            long cost = expAmount * 10;
            if (statsData.Gold < cost)
            {
                NotificationManager.Instance?.ShowNotification("Not enough gold to feed pet.", NotificationType.Warning);
                return;
            }

            if (!playerPets.TryGetValue(clientId, out var pets)) return;
            var pet = pets.FirstOrDefault(p => p.instanceId == petInstanceId);
            if (pet == null || pet.level >= 20) return;

            statsData.ChangeGold(-cost);
            pet.exp += expAmount;
            CheckLevelUp(clientId, pet);

            SyncPetDataClientRpc(pet.instanceId, pet.templateId, pet.level, pet.exp,
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } });
        }

        [ServerRpc(RequireOwnership = false)]
        public void EquipPetAccessoryServerRpc(string petInstanceId, string accessoryItemId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!playerPets.TryGetValue(clientId, out var pets)) return;

            var pet = pets.FirstOrDefault(p => p.instanceId == petInstanceId);
            if (pet == null) return;

            pet.equippedAccessoryId = accessoryItemId;
            Debug.Log($"[PetSystem] Pet {petInstanceId} equipped accessory {accessoryItemId}");

            SyncPetDataClientRpc(pet.instanceId, pet.templateId, pet.level, pet.exp,
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } });
        }

        // --- ClientRpcs ---
        [ClientRpc]
        private void SyncPetDataClientRpc(string petId, string templateId, int level, int exp,
            ClientRpcParams rpcParams = default)
        {
            var existing = localPets.FirstOrDefault(p => p.instanceId == petId);
            if (existing != null)
            {
                existing.level = level;
                existing.exp = exp;
            }
            else
            {
                localPets.Add(new PetInstanceInfo
                {
                    instanceId = petId,
                    templateId = templateId,
                    level = level,
                    exp = exp
                });
            }
            OnPetsUpdated?.Invoke();
        }

        [ClientRpc]
        private void NotifyPetLevelUpClientRpc(string petName, int newLevel,
            ClientRpcParams rpcParams = default)
        {
            NotificationManager.Instance?.ShowNotification(
                $"{petName} reached level {newLevel}!", NotificationType.System);
            OnPetLevelUp?.Invoke(petName);
        }

        [ClientRpc]
        private void NotifyPetObtainedClientRpc(string petName, int grade,
            ClientRpcParams rpcParams = default)
        {
            string gradeName = ((PetGrade)grade).ToString();
            NotificationManager.Instance?.ShowNotification(
                $"Obtained [{gradeName}] {petName}!", NotificationType.System);
            OnPetsUpdated?.Invoke();
        }

        // --- Helper ---
        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (NetworkManager.Singleton == null) return null;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }
    }
}
