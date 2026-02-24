using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    [System.Serializable]
    public class BloodOath
    {
        public string oathId;
        public List<ulong> members = new List<ulong>();
        public float formationTime;
        public int oathLevel = 1;
        public float oathExp;
        public float expToNextLevel = 100f;

        public BloodOath(string id, ulong founderId)
        {
            oathId = id;
            members.Add(founderId);
            formationTime = Time.time;
            oathLevel = 1;
            oathExp = 0f;
        }
    }

    public class BloodOathSystem : NetworkBehaviour
    {
        public static BloodOathSystem Instance { get; private set; }

        public event Action OnOathUpdated;
        public event Action<int> OnOathLevelUp;

        private const int MaxOathMembers = 4;
        private const int MaxOathLevel = 5;
        private const float LeaveCooldownSeconds = 86400f;
        private const float BonusPerMemberPerLevel = 0.02f;

        private Dictionary<string, BloodOath> activeOaths = new Dictionary<string, BloodOath>();
        private Dictionary<ulong, string> playerOathMap = new Dictionary<ulong, string>();
        private Dictionary<ulong, float> leaveCooldowns = new Dictionary<ulong, float>();
        private Dictionary<ulong, string> pendingInvites = new Dictionary<ulong, string>();

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
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        #region Public API

        public float GetOathBonuses(ulong clientId)
        {
            if (!playerOathMap.TryGetValue(clientId, out string oathId)) return 0f;
            if (!activeOaths.TryGetValue(oathId, out BloodOath oath)) return 0f;
            return oath.members.Count * BonusPerMemberPerLevel * oath.oathLevel;
        }

        public bool IsInOath(ulong clientId)
        {
            return playerOathMap.ContainsKey(clientId);
        }

        public List<ulong> GetOathMembers(ulong clientId)
        {
            if (!playerOathMap.TryGetValue(clientId, out string oathId)) return new List<ulong>();
            if (!activeOaths.TryGetValue(oathId, out BloodOath oath)) return new List<ulong>();
            return new List<ulong>(oath.members);
        }

        public void AddOathExp(ulong clientId, float exp)
        {
            if (!IsServer) return;
            if (!playerOathMap.TryGetValue(clientId, out string oathId)) return;
            if (!activeOaths.TryGetValue(oathId, out BloodOath oath)) return;

            oath.oathExp += exp;
            while (oath.oathExp >= oath.expToNextLevel && oath.oathLevel < MaxOathLevel)
            {
                oath.oathExp -= oath.expToNextLevel;
                oath.oathLevel++;
                oath.expToNextLevel *= 1.5f;
                foreach (ulong memberId in oath.members)
                {
                    NotifyOathLevelUpClientRpc(oath.oathLevel, memberId);
                }
            }
            SyncOathToMembers(oath);
        }

        #endregion

        #region Oath Combo Skills

        public void ActivateBloodLink(ulong casterId)
        {
            if (!IsServer) return;
            List<ulong> members = GetOathMembers(casterId);
            if (members.Count < 2) return;

            float totalHp = 0f;
            foreach (ulong id in members)
            {
                var stats = GetPlayerStatsData(id);
                if (stats != null) totalHp += stats.CurrentHP;
            }
            float sharedHp = totalHp / members.Count;
            foreach (ulong id in members)
            {
                var stats = GetPlayerStatsData(id);
                if (stats != null) stats.SetCurrentHP(sharedHp);
            }
            Debug.Log("[BloodOathSystem] BloodLink activated. HP shared equally among members.");
        }

        public void ActivateOathShield(ulong casterId, float barrierAmount)
        {
            if (!IsServer) return;
            List<ulong> members = GetOathMembers(casterId);
            if (members.Count < 2) return;

            foreach (ulong id in members)
            {
                var stats = GetPlayerStatsData(id);
                if (stats != null)
                {
                    float shield = barrierAmount * (1f + GetOathBonuses(casterId));
                    stats.SetCurrentHP(Mathf.Min(stats.CurrentHP + shield, stats.MaxHP));
                }
            }
            Debug.Log("[BloodOathSystem] OathShield activated. Barrier applied to all members.");
        }

        public float GetVengeanceStrikeBonus(ulong attackerId)
        {
            if (!playerOathMap.TryGetValue(attackerId, out string oathId)) return 0f;
            if (!activeOaths.TryGetValue(oathId, out BloodOath oath)) return 0f;

            int injuredAllies = 0;
            foreach (ulong id in oath.members)
            {
                if (id == attackerId) continue;
                var stats = GetPlayerStatsData(id);
                if (stats != null && stats.CurrentHP < stats.MaxHP * 0.5f)
                    injuredAllies++;
            }
            return injuredAllies * 0.15f * oath.oathLevel;
        }

        #endregion

        #region ServerRpcs

        [ServerRpc(RequireOwnership = false)]
        public void CreateOathServerRpc(ulong targetPlayerId, ServerRpcParams rpcParams = default)
        {
            ulong senderId = rpcParams.Receive.SenderClientId;
            if (IsInOath(senderId))
            {
                NotifyOathMessageClientRpc("You are already in a blood oath.", senderId);
                return;
            }
            if (IsOnCooldown(senderId))
            {
                NotifyOathMessageClientRpc("You must wait before forming a new oath.", senderId);
                return;
            }
            if (IsInOath(targetPlayerId))
            {
                NotifyOathMessageClientRpc("Target player is already in an oath.", senderId);
                return;
            }

            string oathId = Guid.NewGuid().ToString();
            BloodOath oath = new BloodOath(oathId, senderId);
            activeOaths[oathId] = oath;
            playerOathMap[senderId] = oathId;
            pendingInvites[targetPlayerId] = oathId;

            NotifyOathInviteClientRpc(oathId, senderId, targetPlayerId);
            Debug.Log($"[BloodOathSystem] Oath {oathId} created by {senderId}. Invite sent to {targetPlayerId}.");
        }

        [ServerRpc(RequireOwnership = false)]
        public void AcceptOathServerRpc(string oathId, ServerRpcParams rpcParams = default)
        {
            ulong accepterId = rpcParams.Receive.SenderClientId;
            if (!pendingInvites.TryGetValue(accepterId, out string pendingOathId) || pendingOathId != oathId)
            {
                NotifyOathMessageClientRpc("No valid oath invitation found.", accepterId);
                return;
            }
            if (!activeOaths.TryGetValue(oathId, out BloodOath oath))
            {
                NotifyOathMessageClientRpc("The oath no longer exists.", accepterId);
                pendingInvites.Remove(accepterId);
                return;
            }
            if (oath.members.Count >= MaxOathMembers)
            {
                NotifyOathMessageClientRpc("The oath is already full.", accepterId);
                pendingInvites.Remove(accepterId);
                return;
            }

            oath.members.Add(accepterId);
            playerOathMap[accepterId] = oathId;
            pendingInvites.Remove(accepterId);

            foreach (ulong memberId in oath.members)
            {
                NotifyOathFormedClientRpc(oath.members.Count, oath.oathLevel, memberId);
            }
            SyncOathToMembers(oath);
            Debug.Log($"[BloodOathSystem] Player {accepterId} joined oath {oathId}. Members: {oath.members.Count}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void LeaveOathServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong leaverId = rpcParams.Receive.SenderClientId;
            if (!playerOathMap.TryGetValue(leaverId, out string oathId))
            {
                NotifyOathMessageClientRpc("You are not in an oath.", leaverId);
                return;
            }
            if (!activeOaths.TryGetValue(oathId, out BloodOath oath)) return;

            oath.members.Remove(leaverId);
            playerOathMap.Remove(leaverId);
            leaveCooldowns[leaverId] = Time.time;

            NotifyOathMessageClientRpc("You have left the blood oath. 24h cooldown applied.", leaverId);

            if (oath.members.Count == 0)
            {
                activeOaths.Remove(oathId);
                Debug.Log($"[BloodOathSystem] Oath {oathId} disbanded. No members remain.");
            }
            else
            {
                SyncOathToMembers(oath);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void InviteToOathServerRpc(ulong targetPlayerId, ServerRpcParams rpcParams = default)
        {
            ulong inviterId = rpcParams.Receive.SenderClientId;
            if (!playerOathMap.TryGetValue(inviterId, out string oathId))
            {
                NotifyOathMessageClientRpc("You are not in an oath.", inviterId);
                return;
            }
            if (!activeOaths.TryGetValue(oathId, out BloodOath oath)) return;
            if (oath.members.Count >= MaxOathMembers)
            {
                NotifyOathMessageClientRpc("The oath is already full.", inviterId);
                return;
            }
            if (IsInOath(targetPlayerId))
            {
                NotifyOathMessageClientRpc("Target player is already in an oath.", inviterId);
                return;
            }

            pendingInvites[targetPlayerId] = oathId;
            NotifyOathInviteClientRpc(oathId, inviterId, targetPlayerId);
            Debug.Log($"[BloodOathSystem] Player {inviterId} invited {targetPlayerId} to oath {oathId}.");
        }

        #endregion

        #region ClientRpcs

        [ClientRpc]
        private void SyncOathDataClientRpc(int memberCount, int oathLevel, float oathExp, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            OnOathUpdated?.Invoke();
        }

        [ClientRpc]
        private void NotifyOathFormedClientRpc(int memberCount, int oathLevel, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            string msg = $"Blood Oath formed! Members: {memberCount}, Level: {oathLevel}";
            NotificationManager.Instance?.ShowNotification(msg, NotificationType.System);
            OnOathUpdated?.Invoke();
        }

        [ClientRpc]
        private void NotifyOathLevelUpClientRpc(int newLevel, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            string msg = $"Blood Oath leveled up to {newLevel}!";
            NotificationManager.Instance?.ShowNotification(msg, NotificationType.System);
            OnOathLevelUp?.Invoke(newLevel);
        }

        [ClientRpc]
        private void NotifyOathInviteClientRpc(string oathId, ulong inviterId, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            string msg = $"Player {inviterId} invites you to a Blood Oath. Use AcceptOath to join.";
            NotificationManager.Instance?.ShowNotification(msg, NotificationType.Warning);
        }

        [ClientRpc]
        private void NotifyOathMessageClientRpc(string message, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager.Instance?.ShowNotification(message, NotificationType.Warning);
        }

        #endregion

        #region Helpers

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (NetworkManager.Singleton == null) return null;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        private bool IsOnCooldown(ulong clientId)
        {
            if (!leaveCooldowns.TryGetValue(clientId, out float leaveTime)) return false;
            if (Time.time - leaveTime < LeaveCooldownSeconds) return true;
            leaveCooldowns.Remove(clientId);
            return false;
        }

        private void SyncOathToMembers(BloodOath oath)
        {
            foreach (ulong memberId in oath.members)
            {
                SyncOathDataClientRpc(oath.members.Count, oath.oathLevel, oath.oathExp, memberId);
            }
        }

        #endregion
    }
}
