using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public enum MountGrade { Common, Rare, Epic, Legendary }

    [System.Serializable]
    public class MountTemplate
    {
        public string mountId;
        public string mountName;
        public MountGrade grade;
        public float speedBonus;
        public string skillName;
        public string skillDescription;
        public float skillCooldown;
        public float skillValue;
    }

    [System.Serializable]
    public class MountInstance
    {
        public string instanceId;
        public string templateId;
        public MountGrade grade;
        public string equippedSaddleId;
        public string equippedArmorId;
    }

    [System.Serializable]
    public class MountInstanceInfo
    {
        public string instanceId;
        public string templateId;
        public int grade;
    }
    public class MountSystem : NetworkBehaviour
    {
        public static MountSystem Instance { get; private set; }

        public event System.Action OnMountsUpdated;
        public event System.Action<bool> OnMountStateChanged;

        // Local client state
        public List<MountInstanceInfo> localMounts = new List<MountInstanceInfo>();
        public string localActiveMountId;
        public bool localIsMounted;

        // Server state
        private Dictionary<ulong, List<MountInstance>> playerMounts = new Dictionary<ulong, List<MountInstance>>();
        private Dictionary<ulong, string> activeMountMap = new Dictionary<ulong, string>();
        private Dictionary<ulong, float> mountSkillCooldowns = new Dictionary<ulong, float>();
        private Dictionary<ulong, bool> mountingInProgress = new Dictionary<ulong, bool>();

        private const float MountCastTime = 2f;

        private static readonly Dictionary<string, MountTemplate> mountTemplates = new Dictionary<string, MountTemplate>
        {
            { "WarHorse", new MountTemplate { mountId = "WarHorse", mountName = "War Horse", grade = MountGrade.Common, speedBonus = 30f, skillName = "Charge", skillDescription = "Charges forward knocking enemies aside", skillCooldown = 15f, skillValue = 20f } },
            { "DesertCamel", new MountTemplate { mountId = "DesertCamel", mountName = "Desert Camel", grade = MountGrade.Common, speedBonus = 25f, skillName = "Endurance", skillDescription = "Increases stamina regeneration by 20%", skillCooldown = 30f, skillValue = 20f } },
            { "ForestWolf", new MountTemplate { mountId = "ForestWolf", mountName = "Forest Wolf", grade = MountGrade.Common, speedBonus = 35f, skillName = "Howl", skillDescription = "Frightens nearby enemies reducing their attack", skillCooldown = 20f, skillValue = 15f } },
            { "ThunderStag", new MountTemplate { mountId = "ThunderStag", mountName = "Thunder Stag", grade = MountGrade.Rare, speedBonus = 50f, skillName = "Lightning Dash", skillDescription = "Dashes forward leaving a lightning trail", skillCooldown = 12f, skillValue = 35f } },
            { "ShadowPanther", new MountTemplate { mountId = "ShadowPanther", mountName = "Shadow Panther", grade = MountGrade.Rare, speedBonus = 60f, skillName = "Stealth", skillDescription = "Becomes invisible for a short duration", skillCooldown = 25f, skillValue = 5f } },
            { "IceWyvern", new MountTemplate { mountId = "IceWyvern", mountName = "Ice Wyvern", grade = MountGrade.Rare, speedBonus = 45f, skillName = "Ice Trail", skillDescription = "Leaves a freezing trail that slows enemies", skillCooldown = 18f, skillValue = 40f } },
            { "PhoenixMount", new MountTemplate { mountId = "PhoenixMount", mountName = "Phoenix", grade = MountGrade.Epic, speedBonus = 70f, skillName = "Fire Charge", skillDescription = "Engulfs in flames and charges dealing fire damage", skillCooldown = 15f, skillValue = 60f } },
            { "StormGriffin", new MountTemplate { mountId = "StormGriffin", mountName = "Storm Griffin", grade = MountGrade.Epic, speedBonus = 80f, skillName = "Wind Dash", skillDescription = "Leaps into the air dashing over obstacles", skillCooldown = 10f, skillValue = 50f } },
            { "DragonMount", new MountTemplate { mountId = "DragonMount", mountName = "Ancient Dragon", grade = MountGrade.Legendary, speedBonus = 100f, skillName = "Fly Over", skillDescription = "Takes flight ignoring all terrain for a duration", skillCooldown = 30f, skillValue = 8f } },
            { "CelestialUnicorn", new MountTemplate { mountId = "CelestialUnicorn", mountName = "Celestial Unicorn", grade = MountGrade.Legendary, speedBonus = 90f, skillName = "Teleport", skillDescription = "Instantly teleports to a target location", skillCooldown = 45f, skillValue = 30f } }
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
            StopAllCoroutines();
            base.OnDestroy();
            if (Instance == this)
                Instance = null;
        }

        // Server API

        public void AddMountToPlayer(ulong clientId, string mountTemplateId)
        {
            if (!IsServer) return;
            if (!mountTemplates.ContainsKey(mountTemplateId)) return;

            if (!playerMounts.ContainsKey(clientId))
                playerMounts[clientId] = new List<MountInstance>();

            var template = mountTemplates[mountTemplateId];
            var instance = new MountInstance
            {
                instanceId = System.Guid.NewGuid().ToString(),
                templateId = mountTemplateId,
                grade = template.grade,
                equippedSaddleId = string.Empty,
                equippedArmorId = string.Empty
            };
            playerMounts[clientId].Add(instance);

            SyncMountDataClientRpc(instance.instanceId, instance.templateId, (int)instance.grade, clientId);
            NotifyMountObtainedClientRpc(template.mountName, (int)template.grade, clientId);
        }

        public float GetMountSpeedBonus(ulong clientId)
        {
            if (!activeMountMap.ContainsKey(clientId)) return 0f;
            var inst = FindMountInstance(clientId, activeMountMap[clientId]);
            if (inst == null || !mountTemplates.ContainsKey(inst.templateId)) return 0f;
            return mountTemplates[inst.templateId].speedBonus;
        }

        public bool IsPlayerMounted(ulong clientId)
        {
            return activeMountMap.ContainsKey(clientId);
        }

        public static MountTemplate GetTemplate(string templateId)
        {
            mountTemplates.TryGetValue(templateId, out var data);
            return data;
        }

        // ServerRpcs

        [ServerRpc(RequireOwnership = false)]
        public void MountUpServerRpc(string mountInstanceId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (activeMountMap.ContainsKey(clientId)) return;
            if (mountingInProgress.ContainsKey(clientId) && mountingInProgress[clientId]) return;

            var inst = FindMountInstance(clientId, mountInstanceId);
            if (inst == null || !mountTemplates.ContainsKey(inst.templateId)) return;

            mountingInProgress[clientId] = true;
            StartCoroutine(MountCastRoutine(clientId, inst));
        }

        private System.Collections.IEnumerator MountCastRoutine(ulong clientId, MountInstance inst)
        {
            yield return new WaitForSeconds(MountCastTime);
            if (!IsSpawned) yield break;

            mountingInProgress[clientId] = false;

            // 클라이언트가 아직 연결되어 있는지 확인
            if (NetworkManager.Singleton == null ||
                !NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
                yield break;

            if (activeMountMap.ContainsKey(clientId)) yield break;

            if (!mountTemplates.ContainsKey(inst.templateId)) yield break;
            var template = mountTemplates[inst.templateId];
            activeMountMap[clientId] = inst.instanceId;
            NotifyMountedClientRpc(template.mountName, template.speedBonus, clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void DismountServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            PerformDismount(clientId);
        }

        public void ForceDismountOnCombat(ulong clientId)
        {
            if (!IsServer) return;
            PerformDismount(clientId);
        }

        private void PerformDismount(ulong clientId)
        {
            if (!activeMountMap.ContainsKey(clientId)) return;
            activeMountMap.Remove(clientId);
            mountSkillCooldowns.Remove(clientId);
            NotifyDismountedClientRpc(clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void UseMountSkillServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!activeMountMap.ContainsKey(clientId)) return;

            var inst = FindMountInstance(clientId, activeMountMap[clientId]);
            if (inst == null || !mountTemplates.ContainsKey(inst.templateId)) return;

            var template = mountTemplates[inst.templateId];
            if (mountSkillCooldowns.ContainsKey(clientId) && Time.time < mountSkillCooldowns[clientId]) return;

            mountSkillCooldowns[clientId] = Time.time + template.skillCooldown;
            Debug.Log($"[MountSystem] Player {clientId} used mount skill: {template.skillName} (value={template.skillValue})");
        }

        [ServerRpc(RequireOwnership = false)]
        public void EquipMountGearServerRpc(string mountInstanceId, string gearItemId, int slot, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var inst = FindMountInstance(clientId, mountInstanceId);
            if (inst == null) return;

            if (slot == 0) inst.equippedSaddleId = gearItemId;
            else if (slot == 1) inst.equippedArmorId = gearItemId;

            SyncMountDataClientRpc(inst.instanceId, inst.templateId, (int)inst.grade, clientId);
        }

        // ClientRpcs

        [ClientRpc]
        private void SyncMountDataClientRpc(string mountId, string templateId, int grade, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            var existing = localMounts.FirstOrDefault(m => m.instanceId == mountId);
            if (existing != null)
            {
                existing.templateId = templateId;
                existing.grade = grade;
            }
            else
            {
                localMounts.Add(new MountInstanceInfo { instanceId = mountId, templateId = templateId, grade = grade });
            }
            OnMountsUpdated?.Invoke();
        }

        [ClientRpc]
        private void NotifyMountObtainedClientRpc(string mountName, int grade, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            string gradeName = ((MountGrade)grade).ToString();
            string msg = $"Obtained [{gradeName}] {mountName}!";
            NotificationManager.Instance?.ShowNotification(msg, NotificationType.System);
        }

        [ClientRpc]
        private void NotifyMountedClientRpc(string mountName, float speedBonus, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localIsMounted = true;
            string msg = $"Mounted {mountName} (Speed +{speedBonus}%)";
            NotificationManager.Instance?.ShowNotification(msg, NotificationType.System);
            OnMountStateChanged?.Invoke(true);
        }

        [ClientRpc]
        private void NotifyDismountedClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localIsMounted = false;
            localActiveMountId = null;
            NotificationManager.Instance?.ShowNotification("Dismounted.", NotificationType.System);
            OnMountStateChanged?.Invoke(false);
        }

        // Helpers

        private MountInstance FindMountInstance(ulong clientId, string instanceId)
        {
            if (!playerMounts.ContainsKey(clientId)) return null;
            return playerMounts[clientId].FirstOrDefault(m => m.instanceId == instanceId);
        }

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
            return null;
        }
    }
}
