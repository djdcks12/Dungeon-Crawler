using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class SalvageSystem : NetworkBehaviour
    {
        public static SalvageSystem Instance { get; private set; }

        private Dictionary<ulong, Dictionary<SalvageMaterial, int>> playerMaterials = new Dictionary<ulong, Dictionary<SalvageMaterial, int>>();

        private static readonly int[][] materialYields = new int[][]
        {
            new int[] { 3, 0, 0, 0 },
            new int[] { 2, 1, 0, 0 },
            new int[] { 1, 2, 1, 0 },
            new int[] { 0, 1, 2, 1 },
            new int[] { 0, 0, 1, 3 }
        };

        private static readonly long[] recoveryCosts = new long[] { 0, 500, 2000, 5000, 15000 };

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
        public void SalvageItemServerRpc(string itemId, int itemGrade, bool recoverExtras, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (itemGrade < 0 || itemGrade > 4) return;

            if (recoverExtras)
            {
                if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var salvClient)) return;
                var playerObj = salvClient.PlayerObject;
                if (playerObj != null)
                {
                    var stats = playerObj.GetComponent<PlayerStatsManager>()?.CurrentStats;
                    if (stats != null && stats.Gold >= recoveryCosts[itemGrade])
                        stats.ChangeGold(-recoveryCosts[itemGrade]);
                    else
                        recoverExtras = false;
                }
            }

            EnsureMaterials(clientId);
            var mats = playerMaterials[clientId];
            int[] yields = materialYields[itemGrade];

            if (yields[0] > 0) AddMaterial(mats, SalvageMaterial.CommonScrap, yields[0]);
            if (yields[1] > 0) AddMaterial(mats, SalvageMaterial.UncommonEssence, yields[1]);
            if (yields[2] > 0) AddMaterial(mats, SalvageMaterial.RareFragment, yields[2]);
            if (yields[3] > 0) AddMaterial(mats, SalvageMaterial.EpicCrystal, yields[3]);

            NotifySalvageClientRpc(itemGrade, recoverExtras, clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void BulkSalvageServerRpc(int maxGrade, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (maxGrade < 0 || maxGrade > 4) return;

            EnsureMaterials(clientId);
            int totalSalvaged = 0;

            for (int grade = 0; grade <= maxGrade; grade++)
            {
                var mats = playerMaterials[clientId];
                int[] yields = materialYields[grade];
                if (yields[0] > 0) AddMaterial(mats, SalvageMaterial.CommonScrap, yields[0]);
                if (yields[1] > 0) AddMaterial(mats, SalvageMaterial.UncommonEssence, yields[1]);
                if (yields[2] > 0) AddMaterial(mats, SalvageMaterial.RareFragment, yields[2]);
                if (yields[3] > 0) AddMaterial(mats, SalvageMaterial.EpicCrystal, yields[3]);
                totalSalvaged++;
            }

            NotifyBulkSalvageClientRpc(totalSalvaged, clientId);
        }

        private void AddMaterial(Dictionary<SalvageMaterial, int> mats, SalvageMaterial type, int amount)
        {
            if (!mats.ContainsKey(type)) mats[type] = 0;
            mats[type] += amount;
        }

        private void EnsureMaterials(ulong clientId)
        {
            if (!playerMaterials.ContainsKey(clientId))
                playerMaterials[clientId] = new Dictionary<SalvageMaterial, int>();
        }

        public int GetMaterialCount(ulong clientId, SalvageMaterial type)
        {
            if (!playerMaterials.ContainsKey(clientId)) return 0;
            if (!playerMaterials[clientId].ContainsKey(type)) return 0;
            return playerMaterials[clientId][type];
        }

        public bool ConsumeMaterial(ulong clientId, SalvageMaterial type, int amount)
        {
            if (GetMaterialCount(clientId, type) < amount) return false;
            playerMaterials[clientId][type] -= amount;
            return true;
        }

        public Dictionary<SalvageMaterial, int> GetAllMaterials(ulong clientId)
        {
            if (!playerMaterials.ContainsKey(clientId)) return new Dictionary<SalvageMaterial, int>();
            return new Dictionary<SalvageMaterial, int>(playerMaterials[clientId]);
        }

        [ClientRpc]
        private void NotifySalvageClientRpc(int grade, bool recovered, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            string extra = recovered ? " (extras recovered)" : "";
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(
                    "Item salvaged! Materials gained." + extra, NotificationType.System);
        }

        [ClientRpc]
        private void NotifyBulkSalvageClientRpc(int count, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(
                    count + " items salvaged!", NotificationType.System);
        }
    }

    public enum SalvageMaterial
    {
        CommonScrap, UncommonEssence, RareFragment, EpicCrystal
    }
}
