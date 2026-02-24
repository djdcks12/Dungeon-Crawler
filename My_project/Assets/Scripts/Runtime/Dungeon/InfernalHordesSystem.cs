using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class InfernalHordesSystem : NetworkBehaviour
    {
        public static InfernalHordesSystem Instance { get; private set; }

        private NetworkVariable<int> currentTier = new NetworkVariable<int>(0);
        private NetworkVariable<int> currentRound = new NetworkVariable<int>(0);
        private NetworkVariable<int> currentWave = new NetworkVariable<int>(0);
        private NetworkVariable<float> waveTimer = new NetworkVariable<float>(0f);
        private NetworkVariable<bool> isActive = new NetworkVariable<bool>(false);
        private NetworkVariable<int> killCount = new NetworkVariable<int>(0);

        private int totalRounds = 5;
        private int wavesPerRound = 3;
        private float waveTimeLimit = 120f;
        private int killsPerWave = 30;

        private Dictionary<ulong, List<TreasureChoice>> playerTreasures = new Dictionary<ulong, List<TreasureChoice>>();
        private Dictionary<ulong, int> burnAmulets = new Dictionary<ulong, int>();

        public System.Action OnHordesStateChanged;
        public System.Action<TreasureOption[]> OnTreasureChoice;

        private static readonly float[] tierHPMult = { 1f, 1.5f, 2.2f, 3.0f, 4.0f, 5.5f, 7.0f, 9.0f, 12.0f, 16.0f };
        private static readonly float[] tierDmgMult = { 1f, 1.3f, 1.7f, 2.2f, 2.8f, 3.5f, 4.5f, 5.5f, 7.0f, 9.0f };
        private static readonly float[] tierRewardMult = { 1f, 1.5f, 2.0f, 3.0f, 4.0f, 5.5f, 7.0f, 9.0f, 12.0f, 16.0f };

        private static readonly TreasureOption[] treasurePool = new TreasureOption[]
        {
            new TreasureOption("Gold Pile", TreasureType.Gold, 5000, "Large pile of gold"),
            new TreasureOption("Mega Gold", TreasureType.Gold, 15000, "Massive pile of gold"),
            new TreasureOption("EXP Tome", TreasureType.Experience, 3000, "Tome of experience"),
            new TreasureOption("Grand Tome", TreasureType.Experience, 10000, "Grand tome of experience"),
            new TreasureOption("Material Cache", TreasureType.Material, 10, "Crafting materials"),
            new TreasureOption("Rare Material", TreasureType.Material, 5, "Rare crafting materials"),
            new TreasureOption("Equipment Box", TreasureType.Equipment, 1, "Random equipment"),
            new TreasureOption("Rare Box", TreasureType.Equipment, 1, "Rare+ equipment guaranteed"),
            new TreasureOption("Gem Pouch", TreasureType.Gem, 3, "Random gems"),
            new TreasureOption("Mutation Stone", TreasureType.MutationStone, 2, "Random mutation stones"),
        };

        public override void OnNetworkSpawn()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (IsClient)
            {
                isActive.OnValueChanged += OnIsActiveChanged;
                currentRound.OnValueChanged += OnCurrentRoundChanged;
                currentWave.OnValueChanged += OnCurrentWaveChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsClient)
            {
                isActive.OnValueChanged -= OnIsActiveChanged;
                currentRound.OnValueChanged -= OnCurrentRoundChanged;
                currentWave.OnValueChanged -= OnCurrentWaveChanged;
            }
            if (Instance == this)
            {
                OnHordesStateChanged = null;
                OnTreasureChoice = null;
                Instance = null;
            }
            base.OnNetworkDespawn();
        }

        private void OnIsActiveChanged(bool prev, bool next) => OnHordesStateChanged?.Invoke();
        private void OnCurrentRoundChanged(int prev, int next) => OnHordesStateChanged?.Invoke();
        private void OnCurrentWaveChanged(int prev, int next) => OnHordesStateChanged?.Invoke();

        private void Update()
        {
            if (!IsServer || !isActive.Value) return;

            waveTimer.Value += Time.deltaTime;

            if (waveTimer.Value >= waveTimeLimit)
            {
                FailHordes();
                return;
            }

            if (killCount.Value >= killsPerWave)
            {
                AdvanceWave();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void StartHordesServerRpc(int tier, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (isActive.Value) return;
            if (tier < 0 || tier >= tierHPMult.Length) return;

            if (!burnAmulets.ContainsKey(clientId)) burnAmulets[clientId] = 0;
            if (burnAmulets[clientId] <= 0)
            {
                NotifyClientRpc("No burn amulets available", clientId);
                return;
            }

            burnAmulets[clientId]--;
            currentTier.Value = tier;
            currentRound.Value = 0;
            currentWave.Value = 0;
            waveTimer.Value = 0f;
            killCount.Value = 0;
            isActive.Value = true;

            playerTreasures[clientId] = new List<TreasureChoice>();

            HordesStartedClientRpc(tier);
        }

        public void OnMonsterKilledInHordes()
        {
            if (!IsServer || !isActive.Value) return;
            killCount.Value++;
        }

        private void AdvanceWave()
        {
            killCount.Value = 0;
            waveTimer.Value = 0f;

            if (currentWave.Value < wavesPerRound - 1)
            {
                currentWave.Value++;
                WaveStartedClientRpc(currentRound.Value, currentWave.Value);
            }
            else
            {
                if (currentRound.Value < totalRounds - 1)
                {
                    OfferTreasureChoice();
                }
                else
                {
                    CompleteHordes();
                }
            }
        }

        private void OfferTreasureChoice()
        {
            List<int> chosen = new List<int>();
            int[] options = new int[3];
            for (int i = 0; i < 3; i++)
            {
                int idx;
                int tries = 0;
                do { idx = Random.Range(0, treasurePool.Length); tries++; }
                while (chosen.Contains(idx) && tries < 50);
                chosen.Add(idx);
                options[i] = idx;
            }

            TreasureChoiceClientRpc(options[0], options[1], options[2]);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ChooseTreasureServerRpc(int optionIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (optionIndex < 0 || optionIndex >= treasurePool.Length) return;

            if (!playerTreasures.ContainsKey(clientId))
                playerTreasures[clientId] = new List<TreasureChoice>();

            var option = treasurePool[optionIndex];
            float mult = tierRewardMult[currentTier.Value];
            playerTreasures[clientId].Add(new TreasureChoice
            {
                type = option.type,
                amount = Mathf.RoundToInt(option.amount * mult)
            });

            currentRound.Value++;
            currentWave.Value = 0;
            waveTimer.Value = 0f;
            killCount.Value = 0;
            RoundStartedClientRpc(currentRound.Value);
        }

        private void CompleteHordes()
        {
            isActive.Value = false;

            foreach (var kvp in playerTreasures)
            {
                ulong clientId = kvp.Key;
                var stats = GetPlayerStatsData(clientId);
                if (stats == null) continue;

                long totalGold = 0;
                long totalExp = 0;
                int totalItems = 0;

                foreach (var treasure in kvp.Value)
                {
                    switch (treasure.type)
                    {
                        case TreasureType.Gold:
                            totalGold += treasure.amount;
                            break;
                        case TreasureType.Experience:
                            totalExp += treasure.amount;
                            break;
                        default:
                            totalItems += treasure.amount;
                            break;
                    }
                }

                if (totalGold > 0) stats.ChangeGold(totalGold);
                if (totalExp > 0) stats.AddExperience(totalExp);

                HordesCompleteClientRpc(totalGold, totalExp, totalItems, clientId);
            }

            playerTreasures.Clear();
        }

        private void FailHordes()
        {
            isActive.Value = false;
            HordesFailedClientRpc();
            playerTreasures.Clear();
        }

        public void AddBurnAmulet(ulong clientId, int amount)
        {
            if (!IsServer) return;
            if (!burnAmulets.ContainsKey(clientId)) burnAmulets[clientId] = 0;
            burnAmulets[clientId] += amount;
        }

        public float GetCurrentHPMult() => isActive.Value ? tierHPMult[currentTier.Value] : 1f;
        public float GetCurrentDmgMult() => isActive.Value ? tierDmgMult[currentTier.Value] : 1f;
        public bool IsHordesActive() => isActive.Value;
        public int GetCurrentRound() => currentRound.Value;
        public int GetCurrentWave() => currentWave.Value;
        public float GetWaveProgress() => killsPerWave > 0 ? (float)killCount.Value / killsPerWave : 0f;

        #region ClientRPCs

        [ClientRpc]
        private void HordesStartedClientRpc(int tier)
        {
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification("Infernal Hordes Tier " + (tier + 1) + " started!", NotificationType.Warning);
            OnHordesStateChanged?.Invoke();
        }

        [ClientRpc]
        private void WaveStartedClientRpc(int round, int wave)
        {
            OnHordesStateChanged?.Invoke();
        }

        [ClientRpc]
        private void RoundStartedClientRpc(int round)
        {
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification("Round " + (round + 1) + " starting!", NotificationType.System);
            OnHordesStateChanged?.Invoke();
        }

        [ClientRpc]
        private void TreasureChoiceClientRpc(int opt1, int opt2, int opt3)
        {
            OnTreasureChoice?.Invoke(new TreasureOption[] { treasurePool[opt1], treasurePool[opt2], treasurePool[opt3] });
        }

        [ClientRpc]
        private void HordesCompleteClientRpc(long gold, long exp, int items, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification("Hordes Complete! +" + gold + "G, +" + exp + " EXP, " + items + " items", NotificationType.System);
            OnHordesStateChanged?.Invoke();
        }

        [ClientRpc]
        private void HordesFailedClientRpc()
        {
            NotificationManager notif = FindFirstObjectByType<NotificationManager>();
            if (notif != null)
                notif.ShowNotification("Infernal Hordes failed - time expired", NotificationType.Warning);
            OnHordesStateChanged?.Invoke();
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

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }

    public enum TreasureType { Gold, Experience, Material, Equipment, Gem, MutationStone }

    public class TreasureOption
    {
        public string name;
        public TreasureType type;
        public int amount;
        public string description;

        public TreasureOption(string name, TreasureType type, int amount, string desc)
        {
            this.name = name; this.type = type; this.amount = amount; this.description = desc;
        }
    }

    public class TreasureChoice
    {
        public TreasureType type;
        public int amount;
    }
}
