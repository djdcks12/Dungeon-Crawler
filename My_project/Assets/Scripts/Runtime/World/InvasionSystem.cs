using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public enum InvasionType
    {
        DemonIncursion,
        UndeadRising,
        ElementalStorm,
        BeastSwarm,
        ShadowWave,
        DragonDescent
    }

    public enum InvasionPhase
    {
        Scouts,
        MainForce,
        Boss,
        Complete
    }

    [Serializable]
    public class InvasionData
    {
        public InvasionType Type;
        public string Name;
        public string Description;
        public string MonsterRace;
        public string ThemeEffect;
        public string Modifier;

        public InvasionData(InvasionType type, string name, string description, string monsterRace, string themeEffect, string modifier)
        {
            Type = type;
            Name = name;
            Description = description;
            MonsterRace = monsterRace;
            ThemeEffect = themeEffect;
            Modifier = modifier;
        }
    }

    [Serializable]
    public class InvasionShopItem
    {
        public string Name;
        public int Cost;
        public string Description;

        public InvasionShopItem(string name, int cost, string description)
        {
            Name = name;
            Cost = cost;
            Description = description;
        }
    }

    public struct ActiveInvasion
    {
        public InvasionType Type;
        public InvasionPhase Phase;
        public Vector3 Center;
        public float StartTime;
        public float PhaseStartTime;
        public int TotalKills;
        public int PhaseKills;
        public bool IsActive;
        public bool BossSpawned;
    }

    public class InvasionSystem : NetworkBehaviour
    {
        public static InvasionSystem Instance { get; private set; }

        public event Action<InvasionType> OnInvasionStarted;
        public event Action<InvasionPhase> OnInvasionPhaseChanged;
        public event Action<InvasionType, int> OnInvasionEnded;

        [Header("Invasion Settings")]
        [SerializeField] private float invasionRadius = 25f;
        [SerializeField] private float invasionDuration = 300f;
        [SerializeField] private float minInterval = 1200f;
        [SerializeField] private float maxInterval = 2400f;
        [SerializeField] private int phaseTransitionKills = 100;

        private ActiveInvasion currentInvasion;
        private float nextInvasionTime;
        private Dictionary<ulong, int> playerKills = new Dictionary<ulong, int>();
        private Dictionary<ulong, int> playerTokens = new Dictionary<ulong, int>();
        private Dictionary<ulong, int> playerParticipation = new Dictionary<ulong, int>();

        private static readonly List<InvasionData> InvasionDatabase = new List<InvasionData>
        {
            new InvasionData(InvasionType.DemonIncursion, "Demon Incursion",
                "Demons pour through a rift from the burning hells, scorching the battlefield with infernal fire.",
                "Demon", "Fire", "+FireDamage to all enemies"),
            new InvasionData(InvasionType.UndeadRising, "Undead Rising",
                "The dead claw their way from the earth, spreading necrotic corruption across the land.",
                "Undead", "Necrotic", "Healing reduced by 30%"),
            new InvasionData(InvasionType.ElementalStorm, "Elemental Storm",
                "A maelstrom of primal elements tears through the area, unleashing chaotic elemental fury.",
                "Elemental", "RandomElement", "Elemental damage +25%"),
            new InvasionData(InvasionType.BeastSwarm, "Beast Swarm",
                "A tidal wave of savage beasts stampedes across the field, overwhelming all in their path.",
                "Beast", "Numbers", "2x monster density"),
            new InvasionData(InvasionType.ShadowWave, "Shadow Wave",
                "Living shadows consume the light, cloaking deadly creatures in impenetrable darkness.",
                "Shadow", "Darkness", "Accuracy reduced by 20%"),
            new InvasionData(InvasionType.DragonDescent, "Dragon Descent",
                "Ancient dragons descend from the skies, unleashing devastating power upon all challengers.",
                "Dragon", "Devastating", "All enemy stats +30%")
        };

        private static readonly List<InvasionShopItem> ShopItems = new List<InvasionShopItem>
        {
            new InvasionShopItem("Invasion Sword", 200, "A blade forged in invasion fire. Deals bonus damage during invasions."),
            new InvasionShopItem("Invasion Shield", 150, "A shield tempered by invasion energy. Reduces invasion damage taken."),
            new InvasionShopItem("Invasion Ring", 300, "A ring pulsing with invasion power. Boosts all stats during invasions."),
            new InvasionShopItem("Invasion Potion", 20, "Restores health and mana instantly. Effective only during invasions."),
            new InvasionShopItem("Invasion Scroll", 50, "Teleports you to the nearest active invasion zone."),
            new InvasionShopItem("Invasion Gem", 100, "A gem that amplifies invasion token drop rate by 10%."),
            new InvasionShopItem("Invasion Mount", 1000, "A fearsome mount born from invasion chaos. +30% move speed."),
            new InvasionShopItem("Invasion Title", 500, "The prestigious Invasion Vanquisher title. Visible to all players."),
            new InvasionShopItem("Invasion Pet", 800, "A companion creature captured during an invasion. Picks up loot automatically."),
            new InvasionShopItem("Invasion Relic", 1500, "A legendary relic of immense power. Grants a unique invasion-themed ability.")
        };

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
            if (Instance == this)
            {
                Instance = null;
            }
            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                ScheduleNextInvasion();
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            if (currentInvasion.IsActive)
            {
                UpdateActiveInvasion();
            }
            else
            {
                if (Time.time >= nextInvasionTime)
                {
                    InvasionType randomType = (InvasionType)UnityEngine.Random.Range(0, 6);
                    Vector3 spawnCenter = GetRandomInvasionCenter();
                    StartInvasion(randomType, spawnCenter);
                }
            }
        }

        private void UpdateActiveInvasion()
        {
            float elapsed = Time.time - currentInvasion.StartTime;

            if (elapsed >= invasionDuration)
            {
                EndInvasion();
                return;
            }

            InvasionPhase expectedPhase = GetExpectedPhase(elapsed);
            if (expectedPhase != currentInvasion.Phase)
            {
                TransitionToPhase(expectedPhase);
            }
        }

        private InvasionPhase GetExpectedPhase(float elapsed)
        {
            if (elapsed < 120f)
                return InvasionPhase.Scouts;
            if (elapsed < 240f)
                return InvasionPhase.MainForce;
            return InvasionPhase.Boss;
        }

        private void TransitionToPhase(InvasionPhase newPhase)
        {
            currentInvasion.Phase = newPhase;
            currentInvasion.PhaseStartTime = Time.time;
            currentInvasion.PhaseKills = 0;

            if (newPhase == InvasionPhase.Boss)
            {
                currentInvasion.BossSpawned = true;
            }

            InvasionData data = GetInvasionData(currentInvasion.Type);
            string phaseName = newPhase.ToString();
            string message = data.Name + " - Phase: " + phaseName;

            AnnouncePhaseChangeClientRpc(message, (int)newPhase);
            OnInvasionPhaseChanged?.Invoke(newPhase);

            Debug.Log("[InvasionSystem] Phase transition: " + phaseName);
        }

        public void StartInvasion(InvasionType type, Vector3 center)
        {
            if (!IsServer) return;
            if (currentInvasion.IsActive) return;

            InvasionData data = GetInvasionData(type);
            if (data == null)
            {
                Debug.LogError("[InvasionSystem] Unknown invasion type: " + type);
                return;
            }

            currentInvasion = new ActiveInvasion
            {
                Type = type,
                Phase = InvasionPhase.Scouts,
                Center = center,
                StartTime = Time.time,
                PhaseStartTime = Time.time,
                TotalKills = 0,
                PhaseKills = 0,
                IsActive = true,
                BossSpawned = false
            };

            playerKills.Clear();

            string announcement = data.Name + " has begun near (" + center.x.ToString("F0") + ", " + center.z.ToString("F0") + ")! Modifier: " + data.Modifier;
            AnnounceInvasionClientRpc(announcement, (int)type, center);
            OnInvasionStarted?.Invoke(type);

            Debug.Log("[InvasionSystem] Started " + data.Name + " at " + center);
        }

        public void EndInvasion()
        {
            if (!IsServer) return;
            if (!currentInvasion.IsActive) return;

            currentInvasion.Phase = InvasionPhase.Complete;
            InvasionData data = GetInvasionData(currentInvasion.Type);
            int totalKills = currentInvasion.TotalKills;

            DistributeRewards();

            string summary = data.Name + " ended. Total kills: " + totalKills + ". Participants: " + playerKills.Count;
            AnnounceInvasionEndClientRpc(summary, totalKills);

            OnInvasionEnded?.Invoke(currentInvasion.Type, totalKills);

            currentInvasion.IsActive = false;
            ScheduleNextInvasion();

            Debug.Log("[InvasionSystem] Ended " + data.Name + " with " + totalKills + " total kills");
        }

        private void DistributeRewards()
        {
            foreach (var kvp in playerKills)
            {
                ulong clientId = kvp.Key;
                int kills = kvp.Value;

                if (kills <= 0) continue;

                int tokenReward = kills;
                long goldBonus = (long)(kills * 10 * 1.3f);
                long expBonus = (long)(kills * 25 * 1.5f);

                if (!playerTokens.ContainsKey(clientId))
                {
                    playerTokens[clientId] = 0;
                }
                playerTokens[clientId] += tokenReward;

                if (!playerParticipation.ContainsKey(clientId))
                {
                    playerParticipation[clientId] = 0;
                }
                playerParticipation[clientId]++;

                if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
                {
                    var playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
                    if (playerObj != null)
                    {
                        var statsData = playerObj.GetComponent<PlayerStatsManager>()?.CurrentStats;
                        if (statsData != null)
                        {
                            statsData.ChangeGold(goldBonus);
                            statsData.AddExperience(expBonus);
                        }
                    }
                }

                InvasionRewardClientRpc(tokenReward, goldBonus, expBonus, kills, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { clientId }
                    }
                });
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RecordKillServerRpc(int monsterType, ServerRpcParams rpcParams = default)
        {
            if (!currentInvasion.IsActive) return;

            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!playerKills.ContainsKey(clientId))
            {
                playerKills[clientId] = 0;
            }

            int tokenGain = 1;
            if (monsterType == 1)
            {
                tokenGain = 3;
            }
            else if (monsterType == 2)
            {
                tokenGain = 20;
            }

            playerKills[clientId] += tokenGain;
            currentInvasion.TotalKills++;
            currentInvasion.PhaseKills++;

            AchievementSystem.Instance?.NotifyEvent(AchievementEvent.MonsterKill, 1);

            float progress = (float)currentInvasion.PhaseKills / phaseTransitionKills;
            UpdateProgressClientRpc(currentInvasion.TotalKills, Mathf.Clamp01(progress), (int)currentInvasion.Phase);

            if (currentInvasion.PhaseKills >= phaseTransitionKills)
            {
                InvasionPhase nextPhase = GetNextPhase(currentInvasion.Phase);
                if (nextPhase != currentInvasion.Phase)
                {
                    TransitionToPhase(nextPhase);
                }
            }
        }

        private InvasionPhase GetNextPhase(InvasionPhase current)
        {
            switch (current)
            {
                case InvasionPhase.Scouts: return InvasionPhase.MainForce;
                case InvasionPhase.MainForce: return InvasionPhase.Boss;
                case InvasionPhase.Boss: return InvasionPhase.Complete;
                default: return current;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void PurchaseShopItemServerRpc(int itemIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (itemIndex < 0 || itemIndex >= ShopItems.Count)
            {
                Debug.LogWarning("[InvasionSystem] Invalid shop item index: " + itemIndex);
                return;
            }

            InvasionShopItem item = ShopItems[itemIndex];

            if (!playerTokens.ContainsKey(clientId) || playerTokens[clientId] < item.Cost)
            {
                NotifyPurchaseResultClientRpc(item.Name, false, "Not enough Invasion Tokens", new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { clientId }
                    }
                });
                return;
            }

            playerTokens[clientId] -= item.Cost;

            NotifyPurchaseResultClientRpc(item.Name, true, "Purchase successful. Remaining tokens: " + playerTokens[clientId], new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            });

            Debug.Log("[InvasionSystem] Player " + clientId + " purchased " + item.Name + " for " + item.Cost + " tokens");
        }

        [ClientRpc]
        private void AnnounceInvasionClientRpc(string message, int invasionTypeIndex, Vector3 center)
        {
            NotificationManager.Instance?.ShowNotification(message, NotificationType.System);
            CameraEffects.Instance?.ShakeMedium();
            Debug.Log("[InvasionSystem] Invasion announced: " + message);
        }

        [ClientRpc]
        private void AnnouncePhaseChangeClientRpc(string message, int phaseIndex)
        {
            NotificationManager.Instance?.ShowNotification(message, NotificationType.Warning);

            if (!System.Enum.IsDefined(typeof(InvasionPhase), phaseIndex)) return;
            InvasionPhase phase = (InvasionPhase)phaseIndex;
            if (phase == InvasionPhase.Boss)
            {
                CameraEffects.Instance?.ShakeHeavy();
            }
            else
            {
                CameraEffects.Instance?.ShakeLight();
            }

            Debug.Log("[InvasionSystem] Phase change: " + message);
        }

        [ClientRpc]
        private void AnnounceInvasionEndClientRpc(string summary, int totalKills)
        {
            NotificationManager.Instance?.ShowNotification(summary, NotificationType.System);
            Debug.Log("[InvasionSystem] Invasion ended: " + summary);
        }

        [ClientRpc]
        private void UpdateProgressClientRpc(int totalKills, float phaseProgress, int currentPhase)
        {
            Debug.Log("[InvasionSystem] Progress - Kills: " + totalKills + " Phase: " + (InvasionPhase)currentPhase + " Progress: " + (phaseProgress * 100f).ToString("F0") + "%");
        }

        [ClientRpc]
        private void InvasionRewardClientRpc(int tokens, long gold, long exp, int kills, ClientRpcParams rpcParams = default)
        {
            string rewardMsg = "Invasion Complete - Tokens: " + tokens + ", Gold: " + gold + ", EXP: " + exp + " (Kills: " + kills + ")";
            NotificationManager.Instance?.ShowNotification(rewardMsg, NotificationType.System);
            Debug.Log("[InvasionSystem] " + rewardMsg);
        }

        [ClientRpc]
        private void NotifyPurchaseResultClientRpc(string itemName, bool success, string message, ClientRpcParams rpcParams = default)
        {
            NotificationType notifType = success ? NotificationType.System : NotificationType.Warning;
            NotificationManager.Instance?.ShowNotification(message, notifType);
            Debug.Log("[InvasionSystem] Purchase result for " + itemName + ": " + message);
        }

        private void ScheduleNextInvasion()
        {
            nextInvasionTime = Time.time + UnityEngine.Random.Range(minInterval, maxInterval);
            Debug.Log("[InvasionSystem] Next invasion scheduled in " + (nextInvasionTime - Time.time).ToString("F0") + " seconds");
        }

        private Vector3 GetRandomInvasionCenter()
        {
            float x = UnityEngine.Random.Range(-100f, 100f);
            float z = UnityEngine.Random.Range(-100f, 100f);
            return new Vector3(x, 0f, z);
        }

        private InvasionData GetInvasionData(InvasionType type)
        {
            for (int i = 0; i < InvasionDatabase.Count; i++)
            {
                if (InvasionDatabase[i].Type == type)
                {
                    return InvasionDatabase[i];
                }
            }
            return null;
        }

        public bool IsInvasionActive()
        {
            return currentInvasion.IsActive;
        }

        public InvasionType GetCurrentInvasionType()
        {
            return currentInvasion.Type;
        }

        public InvasionPhase GetCurrentPhase()
        {
            return currentInvasion.Phase;
        }

        public Vector3 GetInvasionCenter()
        {
            return currentInvasion.Center;
        }

        public float GetInvasionRadius()
        {
            return invasionRadius;
        }

        public float GetRemainingTime()
        {
            if (!currentInvasion.IsActive) return 0f;
            float elapsed = Time.time - currentInvasion.StartTime;
            return Mathf.Max(0f, invasionDuration - elapsed);
        }

        public int GetPlayerTokens(ulong clientId)
        {
            return playerTokens.ContainsKey(clientId) ? playerTokens[clientId] : 0;
        }

        public int GetPlayerParticipation(ulong clientId)
        {
            return playerParticipation.ContainsKey(clientId) ? playerParticipation[clientId] : 0;
        }

        public int GetPlayerKillsThisInvasion(ulong clientId)
        {
            return playerKills.ContainsKey(clientId) ? playerKills[clientId] : 0;
        }

        public List<InvasionShopItem> GetShopItems()
        {
            return ShopItems;
        }

        public float GetExpBonus()
        {
            return currentInvasion.IsActive ? 0.5f : 0f;
        }

        public float GetGoldBonus()
        {
            return currentInvasion.IsActive ? 0.3f : 0f;
        }

        public bool IsInsideInvasionZone(Vector3 position)
        {
            if (!currentInvasion.IsActive) return false;
            float distance = Vector3.Distance(position, currentInvasion.Center);
            return distance <= invasionRadius;
        }

        public string GetInvasionModifier()
        {
            if (!currentInvasion.IsActive) return string.Empty;
            InvasionData data = GetInvasionData(currentInvasion.Type);
            return data != null ? data.Modifier : string.Empty;
        }

        public string GetShopItemsAsCSV()
        {
            string result = "";
            for (int i = 0; i < ShopItems.Count; i++)
            {
                if (i > 0) result += ",";
                result += ShopItems[i].Name + ":" + ShopItems[i].Cost;
            }
            return result;
        }
    }
}
