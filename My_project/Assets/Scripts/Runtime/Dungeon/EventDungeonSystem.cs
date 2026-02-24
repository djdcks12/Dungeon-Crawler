using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class EventDungeonSystem : NetworkBehaviour
    {
        public static EventDungeonSystem Instance { get; private set; }

        [Header("Event Settings")]
        [SerializeField] private float eventDuration = 1800f;
        [SerializeField] private float cyclePeriod = 7200f;

        private NetworkVariable<int> activeEventIndex = new NetworkVariable<int>(-1);
        private NetworkVariable<float> eventTimer = new NetworkVariable<float>(0f);
        private NetworkVariable<float> cycleTimer = new NetworkVariable<float>(0f);

        private Dictionary<ulong, int> playerEventTokens = new Dictionary<ulong, int>();

        private static readonly EventDungeonData[] events = new EventDungeonData[]
        {
            new EventDungeonData("helltide", "Helltide", "Rivers of fire flood the dungeon. Increased fire damage and fire-themed loot.", 1.5f, 2.0f, 10),
            new EventDungeonData("treasure_cave", "Treasure Cavern", "A hidden cave filled with treasure chests. Double gold and item drops.", 1.0f, 3.0f, 15),
            new EventDungeonData("trial_tower", "Tower of Trials", "An endless tower of escalating challenges. Bonus XP per floor.", 2.0f, 1.5f, 8),
            new EventDungeonData("spirit_grove", "Spirit Grove", "A magical forest with spirit enemies. Bonus soul stone drops.", 1.3f, 1.8f, 12)
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

        private void Update()
        {
            if (!IsServer) return;

            if (activeEventIndex.Value >= 0)
            {
                eventTimer.Value -= Time.deltaTime;
                if (eventTimer.Value <= 0f)
                {
                    EndEvent();
                }
            }
            else
            {
                cycleTimer.Value -= Time.deltaTime;
                if (cycleTimer.Value <= 0f)
                {
                    StartNextEvent();
                }
            }
        }

        private void StartNextEvent()
        {
            int nextIdx = (activeEventIndex.Value + 1) % events.Length;
            if (nextIdx < 0) nextIdx = 0;
            activeEventIndex.Value = nextIdx;
            eventTimer.Value = eventDuration;
            NotifyEventStartClientRpc(nextIdx);
        }

        private void EndEvent()
        {
            int endedIdx = activeEventIndex.Value;
            activeEventIndex.Value = -1;
            cycleTimer.Value = cyclePeriod;
            NotifyEventEndClientRpc(endedIdx);
        }

        [ServerRpc(RequireOwnership = false)]
        public void EnterEventDungeonServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (activeEventIndex.Value < 0)
            {
                NotifyEventFailClientRpc("No event dungeon is currently active!", clientId);
                return;
            }
            NotifyEventEntryClientRpc(activeEventIndex.Value, clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ClaimEventRewardServerRpc(int tokenCost, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!playerEventTokens.ContainsKey(clientId)) playerEventTokens[clientId] = 0;
            if (playerEventTokens[clientId] < tokenCost)
            {
                NotifyEventFailClientRpc("Not enough event tokens!", clientId);
                return;
            }
            playerEventTokens[clientId] -= tokenCost;
            NotifyRewardClaimedClientRpc(tokenCost, clientId);
        }

        public void AwardEventTokens(ulong clientId, int amount)
        {
            if (!IsServer) return;
            if (!playerEventTokens.ContainsKey(clientId)) playerEventTokens[clientId] = 0;
            playerEventTokens[clientId] += amount;
        }

        public bool IsEventActive => activeEventIndex.Value >= 0;
        public int ActiveEventIndex => activeEventIndex.Value;
        public float EventTimeRemaining => eventTimer.Value;
        public float NextEventTime => cycleTimer.Value;

        public EventDungeonData GetActiveEvent()
        {
            if (activeEventIndex.Value < 0) return null;
            return events[activeEventIndex.Value];
        }

        public EventDungeonData[] GetAllEvents() => events;

        public int GetTokenCount(ulong clientId)
        {
            if (!playerEventTokens.ContainsKey(clientId)) return 0;
            return playerEventTokens[clientId];
        }

        [ClientRpc]
        private void NotifyEventStartClientRpc(int eventIdx)
        {
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(
                    "Event Dungeon opened: " + events[eventIdx].eventName + "!", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyEventEndClientRpc(int eventIdx)
        {
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(
                    events[eventIdx].eventName + " has closed.", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyEventEntryClientRpc(int eventIdx, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(
                    "Entering " + events[eventIdx].eventName + "!", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyEventFailClientRpc(string message, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(message, NotificationType.Warning);
        }

        [ClientRpc]
        private void NotifyRewardClaimedClientRpc(int tokens, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(
                    "Event reward claimed! " + tokens + " tokens spent.", NotificationType.System);
        }
    }

    [System.Serializable]
    public class EventDungeonData
    {
        public string eventId;
        public string eventName;
        public string description;
        public float difficultyMult;
        public float rewardMult;
        public int tokenReward;

        public EventDungeonData(string id, string name, string desc, float diff, float reward, int tokens)
        {
            eventId = id;
            eventName = name;
            description = desc;
            difficultyMult = diff;
            rewardMult = reward;
            tokenReward = tokens;
        }
    }
}
