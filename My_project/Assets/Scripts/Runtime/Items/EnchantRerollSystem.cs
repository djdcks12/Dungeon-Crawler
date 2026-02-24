using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class EnchantRerollSystem : NetworkBehaviour
    {
        public static EnchantRerollSystem Instance { get; private set; }

        private Dictionary<ulong, Dictionary<string, int>> lockedAffixes = new Dictionary<ulong, Dictionary<string, int>>();

        public System.Action OnRerollComplete;

        private static readonly long[] rerollCostByGrade = { 1000, 3000, 10000, 25000, 50000 };
        private static readonly long[] lockCostByGrade = { 500, 1500, 5000, 12000, 25000 };
        private static readonly long[] blessedOrbCost = { 2000, 5000, 15000, 35000, 75000 };

        private static readonly string[][] affixPoolBySlot = new string[][]
        {
            new string[] { "STR", "AGI", "VIT", "INT", "DEF", "MDEF", "CritRate", "CritDmg", "AttackSpeed", "MoveSpeed" },
            new string[] { "HP", "MP", "HPRegen", "MPRegen", "LifeSteal", "DodgeRate", "BlockRate", "Thorns" },
            new string[] { "FireRes", "IceRes", "LightningRes", "PoisonRes", "AllRes", "ExpBonus", "GoldFind", "MagicFind" },
        };

        private static readonly float[][] affixValueRange = new float[][]
        {
            new float[] { 1f, 5f },
            new float[] { 3f, 8f },
            new float[] { 5f, 12f },
            new float[] { 8f, 18f },
            new float[] { 12f, 30f },
        };

        public override void OnNetworkSpawn()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RerollAffixServerRpc(string itemId, int affixSlot, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (affixSlot < 0 || affixSlot > 2) return;

            int gradeIndex = GetItemGradeIndex(itemId);
            if (gradeIndex < 0 || gradeIndex >= rerollCostByGrade.Length) return;

            long cost = rerollCostByGrade[gradeIndex];

            var locks = GetOrCreateLocks(clientId);
            string lockKey = itemId + "_" + affixSlot;
            if (locks.ContainsKey(lockKey) && locks[lockKey] > 0)
            {
                cost += lockCostByGrade[gradeIndex];
            }

            if (!TrySpendGold(clientId, cost))
            {
                NotifyClientRpc("Not enough gold for reroll (" + cost + "G)", clientId);
                return;
            }

            string[] pool = affixPoolBySlot[affixSlot % affixPoolBySlot.Length];
            string newAffix = pool[Random.Range(0, pool.Length)];
            float minVal = affixValueRange[gradeIndex][0];
            float maxVal = affixValueRange[gradeIndex][1];
            float newValue = Mathf.Round(Random.Range(minVal, maxVal) * 10f) / 10f;

            RerollResultClientRpc(itemId, affixSlot, newAffix, newValue, cost, clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void BlessedRerollServerRpc(string itemId, int affixSlot, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (affixSlot < 0 || affixSlot > 2) return;

            int gradeIndex = GetItemGradeIndex(itemId);
            if (gradeIndex < 0 || gradeIndex >= blessedOrbCost.Length) return;

            long cost = blessedOrbCost[gradeIndex];

            if (!TrySpendGold(clientId, cost))
            {
                NotifyClientRpc("Not enough gold for blessed reroll (" + cost + "G)", clientId);
                return;
            }

            float minVal = affixValueRange[gradeIndex][0];
            float maxVal = affixValueRange[gradeIndex][1];
            float newValue = Mathf.Round(Random.Range(minVal, maxVal) * 10f) / 10f;

            BlessedRerollResultClientRpc(itemId, affixSlot, newValue, cost, clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void LockAffixServerRpc(string itemId, int affixSlot, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var locks = GetOrCreateLocks(clientId);
            string lockKey = itemId + "_" + affixSlot;

            if (locks.ContainsKey(lockKey) && locks[lockKey] > 0)
            {
                locks[lockKey] = 0;
                AffixLockChangedClientRpc(itemId, affixSlot, false, clientId);
            }
            else
            {
                locks[lockKey] = 1;
                AffixLockChangedClientRpc(itemId, affixSlot, true, clientId);
            }
        }

        public long GetRerollCost(int gradeIndex, string itemId, int affixSlot, ulong clientId)
        {
            if (gradeIndex < 0 || gradeIndex >= rerollCostByGrade.Length) return 0;
            long cost = rerollCostByGrade[gradeIndex];
            return cost;
        }

        public long GetBlessedCost(int gradeIndex)
        {
            if (gradeIndex < 0 || gradeIndex >= blessedOrbCost.Length) return 0;
            return blessedOrbCost[gradeIndex];
        }

        #region ClientRPCs

        [ClientRpc]
        private void RerollResultClientRpc(string itemId, int slot, string newAffix, float newValue, long cost, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification("Reroll: " + newAffix + " +" + newValue + " (-" + cost + "G)", NotificationType.System);
            OnRerollComplete?.Invoke();
        }

        [ClientRpc]
        private void BlessedRerollResultClientRpc(string itemId, int slot, float newValue, long cost, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification("Blessed Reroll: value -> " + newValue + " (-" + cost + "G)", NotificationType.System);
            OnRerollComplete?.Invoke();
        }

        [ClientRpc]
        private void AffixLockChangedClientRpc(string itemId, int slot, bool locked, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification("Affix slot " + slot + (locked ? " locked" : " unlocked"), NotificationType.System);
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

        private int GetItemGradeIndex(string itemId)
        {
            return Mathf.Clamp(itemId.GetHashCode() % 5, 0, 4);
        }

        private bool TrySpendGold(ulong clientId, long amount)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return false;
            var stats = client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
            if (stats == null || stats.Gold < amount) return false;
            stats.ChangeGold(-amount);
            return true;
        }

        private Dictionary<string, int> GetOrCreateLocks(ulong clientId)
        {
            if (!lockedAffixes.ContainsKey(clientId))
                lockedAffixes[clientId] = new Dictionary<string, int>();
            return lockedAffixes[clientId];
        }

        #endregion

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }
}
