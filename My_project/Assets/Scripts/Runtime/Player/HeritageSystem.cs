using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    [Serializable]
    public class HeritageEntry
    {
        public string characterName;
        public int race;
        public int job;
        public int level;
        public string registeredTime;
    }

    [Serializable]
    public class HeritageEntryList
    {
        public List<HeritageEntry> entries = new List<HeritageEntry>();
    }

    [Serializable]
    public struct HeritageBonusData
    {
        public float expBonus;
        public float goldBonus;
        public float moveSpeedBonus;
        public float damageBonus;
        public float hpBonus;
        public float allStatBonus;

        public static HeritageBonusData Zero => new HeritageBonusData
        {
            expBonus = 0f,
            goldBonus = 0f,
            moveSpeedBonus = 0f,
            damageBonus = 0f,
            hpBonus = 0f,
            allStatBonus = 0f
        };
    }

    public class HeritageSystem : NetworkBehaviour
    {
        public static HeritageSystem Instance { get; private set; }

        public event Action<HeritageEntry> OnHeritageRegistered;
        public event Action<HeritageBonusData> OnHeritageBonusChanged;

        private const int MaxHeritageSlots = 4;
        private const int MaxLevel = 15;
        private const int MaxUniqueJobs = 16;
        private const string HeritagePrefsKey = "HeritageData";
        private const string ChallengePrefsKey = "HeritageChallenges";

        private HeritageEntryList heritageData = new HeritageEntryList();
        private HashSet<string> completedChallenges = new HashSet<string>();
        private HeritageBonusData cachedBonuses;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LoadHeritageData();
            LoadChallengeData();
            RecalculateBonuses();
        }

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            base.OnDestroy();
        }

        #region Registration

        [ServerRpc(RequireOwnership = false)]
        public void RegisterHeritageServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
            {
                return;
            }

            var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            if (playerObject == null)
            {
                NotifyRegistrationResultClientRpc("Heritage registration failed: player not found.", false,
                    CreateClientRpcParams(clientId));
                return;
            }

            var statsData = playerObject.GetComponent<PlayerStatsManager>()?.CurrentStats;
            if (statsData == null)
            {
                NotifyRegistrationResultClientRpc("Heritage registration failed: stats not found.", false,
                    CreateClientRpcParams(clientId));
                return;
            }

            if (statsData.CurrentLevel < MaxLevel)
            {
                NotifyRegistrationResultClientRpc(
                    $"Character must be level {MaxLevel} to register as Heritage. Current level: {statsData.CurrentLevel}",
                    false, CreateClientRpcParams(clientId));
                return;
            }

            if (heritageData.entries.Count >= MaxHeritageSlots)
            {
                NotifyRegistrationResultClientRpc(
                    $"Heritage slots are full ({MaxHeritageSlots}/{MaxHeritageSlots}). Remove a heritage first.",
                    false, CreateClientRpcParams(clientId));
                return;
            }

            string charName = statsData.CharacterName;
            if (heritageData.entries.Any(e => e.characterName == charName))
            {
                NotifyRegistrationResultClientRpc(
                    "This character is already registered as Heritage.",
                    false, CreateClientRpcParams(clientId));
                return;
            }

            var entry = new HeritageEntry
            {
                characterName = charName,
                race = (int)statsData.CharacterRace,
                job = (int)statsData.CurrentJobType,
                level = statsData.CurrentLevel,
                registeredTime = DateTime.UtcNow.ToString("o")
            };

            heritageData.entries.Add(entry);
            SaveHeritageData();
            RecalculateBonuses();
            CheckHeritageChallenges();

            OnHeritageRegistered?.Invoke(entry);

            string bonusSummary = FormatBonusSummary(cachedBonuses);
            NotifyRegistrationResultClientRpc(
                $"Heritage registered: {charName} (Slot {heritageData.entries.Count}/{MaxHeritageSlots}). {bonusSummary}",
                true, CreateClientRpcParams(clientId));

            SyncHeritageBonusesClientRpc(
                cachedBonuses.expBonus, cachedBonuses.goldBonus, cachedBonuses.moveSpeedBonus,
                cachedBonuses.damageBonus, cachedBonuses.hpBonus, cachedBonuses.allStatBonus,
                CreateClientRpcParams(clientId));
        }

        #endregion

        #region Bonus Calculation

        public HeritageBonusData GetHeritageBonuses()
        {
            return cachedBonuses;
        }

        public float GetRaceBonus(int currentRace)
        {
            float bonus = 0f;

            foreach (var entry in heritageData.entries)
            {
                if (entry.race != currentRace) continue;

                // RaceType: Human=0, Elf=1, Beast=2, Machina=3
                switch (entry.race)
                {
                    case 0: // Human - skill cooldown reduction
                        bonus += 0.05f;
                        break;
                    case 1: // Elf - magic damage
                        bonus += 0.08f;
                        break;
                    case 2: // Beast - physical damage
                        bonus += 0.08f;
                        break;
                    case 3: // Machina - defense
                        bonus += 0.10f;
                        break;
                }
            }

            return bonus;
        }

        private void RecalculateBonuses()
        {
            var bonuses = HeritageBonusData.Zero;
            int count = heritageData.entries.Count;

            // Per-character bonuses: +10% EXP, +5% Gold, +3% Move Speed each
            bonuses.expBonus = count * 0.10f;
            bonuses.goldBonus = count * 0.05f;
            bonuses.moveSpeedBonus = count * 0.03f;

            // Milestone bonuses
            if (count >= 2)
            {
                bonuses.damageBonus += 0.05f;
            }
            if (count >= 3)
            {
                bonuses.hpBonus += 100f;
            }
            if (count >= 4)
            {
                bonuses.allStatBonus += 0.10f;
            }

            // Job Heritage: +1% all stats per unique job (up to 16%)
            var uniqueJobs = heritageData.entries.Select(e => e.job).Distinct().Count();
            bonuses.allStatBonus += Mathf.Min(uniqueJobs * 0.01f, MaxUniqueJobs * 0.01f);

            // Challenge permanent bonuses
            if (completedChallenges.Contains("Explorer"))
            {
                bonuses.moveSpeedBonus += 0.05f;
            }
            if (completedChallenges.Contains("MasterOfAll"))
            {
                bonuses.expBonus += 0.10f;
            }

            cachedBonuses = bonuses;
            OnHeritageBonusChanged?.Invoke(cachedBonuses);
        }

        public int GetRegisteredCount()
        {
            return heritageData.entries.Count;
        }

        public int GetUniqueJobCount()
        {
            return heritageData.entries.Select(e => e.job).Distinct().Count();
        }

        public List<HeritageEntry> GetHeritageEntries()
        {
            return new List<HeritageEntry>(heritageData.entries);
        }

        #endregion

        #region Challenges

        private void CheckHeritageChallenges()
        {
            bool changed = false;

            // Explorer: Register all 4 races
            if (!completedChallenges.Contains("Explorer"))
            {
                var uniqueRaces = heritageData.entries.Select(e => e.race).Distinct().Count();
                if (uniqueRaces >= 4)
                {
                    completedChallenges.Add("Explorer");
                    changed = true;
                    NotificationManager.Instance?.ShowNotification(
                        "Heritage Challenge Complete: Explorer - All 4 races registered. +5% move speed permanent",
                        NotificationType.System);
                }
            }

            // Master of All: Register all 16 jobs
            if (!completedChallenges.Contains("MasterOfAll"))
            {
                var uniqueJobCount = heritageData.entries.Select(e => e.job).Distinct().Count();
                if (uniqueJobCount >= MaxUniqueJobs)
                {
                    completedChallenges.Add("MasterOfAll");
                    changed = true;
                    NotificationManager.Instance?.ShowNotification(
                        "Heritage Challenge Complete: Master of All - All 16 jobs registered. +10% EXP permanent",
                        NotificationType.System);
                }
            }

            // Veteran: Register 4 max level characters
            if (!completedChallenges.Contains("Veteran"))
            {
                var maxLevelCount = heritageData.entries.Count(e => e.level >= MaxLevel);
                if (maxLevelCount >= MaxHeritageSlots)
                {
                    completedChallenges.Add("Veteran");
                    changed = true;
                    NotificationManager.Instance?.ShowNotification(
                        "Heritage Challenge Complete: Veteran - 4 max level characters. Title earned: Legend",
                        NotificationType.System);
                }
            }

            if (changed)
            {
                SaveChallengeData();
                RecalculateBonuses();
            }
        }

        public bool HasCompletedChallenge(string challengeName)
        {
            return completedChallenges.Contains(challengeName);
        }

        public string GetTitle()
        {
            if (completedChallenges.Contains("Veteran"))
            {
                return "Legend";
            }
            return string.Empty;
        }

        #endregion

        #region ClientRpc

        [ClientRpc]
        private void NotifyRegistrationResultClientRpc(string message, bool success,
            ClientRpcParams clientRpcParams = default)
        {
            var notifType = success ? NotificationType.System : NotificationType.Warning;
            NotificationManager.Instance?.ShowNotification(message, notifType);

            if (success)
            {
                Debug.Log($"[HeritageSystem] Heritage registered successfully: {message}");
            }
            else
            {
                Debug.LogWarning($"[HeritageSystem] Heritage registration failed: {message}");
            }
        }

        [ClientRpc]
        private void SyncHeritageBonusesClientRpc(float expBonus, float goldBonus, float moveSpeedBonus,
            float damageBonus, float hpBonus, float allStatBonus,
            ClientRpcParams clientRpcParams = default)
        {
            cachedBonuses = new HeritageBonusData
            {
                expBonus = expBonus,
                goldBonus = goldBonus,
                moveSpeedBonus = moveSpeedBonus,
                damageBonus = damageBonus,
                hpBonus = hpBonus,
                allStatBonus = allStatBonus
            };

            OnHeritageBonusChanged?.Invoke(cachedBonuses);
            Debug.Log($"[HeritageSystem] Bonuses synced - EXP:{expBonus:P0} Gold:{goldBonus:P0} Speed:{moveSpeedBonus:P0} DMG:{damageBonus:P0} HP:{hpBonus} Stats:{allStatBonus:P0}");
        }

        #endregion

        #region Persistence

        private void SaveHeritageData()
        {
            string json = JsonUtility.ToJson(heritageData);
            PlayerPrefs.SetString(HeritagePrefsKey, json);
            PlayerPrefs.Save();
            Debug.Log($"[HeritageSystem] Heritage data saved. {heritageData.entries.Count} entries.");
        }

        private void LoadHeritageData()
        {
            if (PlayerPrefs.HasKey(HeritagePrefsKey))
            {
                string json = PlayerPrefs.GetString(HeritagePrefsKey);
                try
                {
                    heritageData = JsonUtility.FromJson<HeritageEntryList>(json);
                    if (heritageData == null)
                    {
                        heritageData = new HeritageEntryList();
                    }
                    Debug.Log($"[HeritageSystem] Heritage data loaded. {heritageData.entries.Count} entries.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[HeritageSystem] Failed to load heritage data: {e.Message}");
                    heritageData = new HeritageEntryList();
                }
            }
            else
            {
                heritageData = new HeritageEntryList();
                Debug.Log("[HeritageSystem] No existing heritage data found. Starting fresh.");
            }
        }

        private void SaveChallengeData()
        {
            string csv = string.Join(",", completedChallenges);
            PlayerPrefs.SetString(ChallengePrefsKey, csv);
            PlayerPrefs.Save();
            Debug.Log($"[HeritageSystem] Challenge data saved: {csv}");
        }

        private void LoadChallengeData()
        {
            completedChallenges.Clear();
            if (PlayerPrefs.HasKey(ChallengePrefsKey))
            {
                string csv = PlayerPrefs.GetString(ChallengePrefsKey);
                if (!string.IsNullOrEmpty(csv))
                {
                    foreach (var challenge in csv.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        completedChallenges.Add(challenge.Trim());
                    }
                }
                Debug.Log($"[HeritageSystem] Challenge data loaded: {csv}");
            }
        }

        #endregion

        #region Utility

        private ClientRpcParams CreateClientRpcParams(ulong clientId)
        {
            return new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };
        }

        private string FormatBonusSummary(HeritageBonusData bonuses)
        {
            var parts = new List<string>();
            if (bonuses.expBonus > 0f) parts.Add($"EXP +{bonuses.expBonus:P0}");
            if (bonuses.goldBonus > 0f) parts.Add($"Gold +{bonuses.goldBonus:P0}");
            if (bonuses.moveSpeedBonus > 0f) parts.Add($"Speed +{bonuses.moveSpeedBonus:P0}");
            if (bonuses.damageBonus > 0f) parts.Add($"DMG +{bonuses.damageBonus:P0}");
            if (bonuses.hpBonus > 0f) parts.Add($"HP +{bonuses.hpBonus}");
            if (bonuses.allStatBonus > 0f) parts.Add($"All Stats +{bonuses.allStatBonus:P0}");
            return parts.Count > 0 ? "Bonuses: " + string.Join(", ", parts) : "No bonuses yet.";
        }

        public void RemoveHeritage(int index)
        {
            if (index < 0 || index >= heritageData.entries.Count)
            {
                Debug.LogWarning($"[HeritageSystem] Invalid heritage index: {index}");
                return;
            }

            var removed = heritageData.entries[index];
            heritageData.entries.RemoveAt(index);
            SaveHeritageData();
            RecalculateBonuses();
            Debug.Log($"[HeritageSystem] Heritage removed: {removed.characterName}");
        }

        public void ResetAllHeritageData()
        {
            heritageData = new HeritageEntryList();
            completedChallenges.Clear();
            cachedBonuses = HeritageBonusData.Zero;
            PlayerPrefs.DeleteKey(HeritagePrefsKey);
            PlayerPrefs.DeleteKey(ChallengePrefsKey);
            PlayerPrefs.Save();
            OnHeritageBonusChanged?.Invoke(cachedBonuses);
            Debug.Log("[HeritageSystem] All heritage data has been reset.");
        }

        #endregion
    }
}
