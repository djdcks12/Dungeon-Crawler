using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class LoadoutSystem : NetworkBehaviour
    {
        public static LoadoutSystem Instance { get; private set; }
        [SerializeField] private int maxLoadouts = 3;
        [SerializeField] private float switchCastTime = 5f;
        private LoadoutProfile[] profiles;
        private int activeProfileIndex = 0;
        private bool isSwitching = false;
        private float switchTimer = 0;
        public System.Action<int> OnLoadoutSaved;
        public System.Action<int> OnLoadoutSwitched;
        public System.Action<float> OnSwitchProgress;
        public System.Action OnSwitchCancelled;
        public int ActiveProfileIndex => activeProfileIndex;
        public int MaxLoadouts => maxLoadouts;
        public bool IsSwitching => isSwitching;
        public LoadoutProfile ActiveProfile => profiles != null && activeProfileIndex < profiles.Length ? profiles[activeProfileIndex] : null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            profiles = new LoadoutProfile[maxLoadouts];
            for (int i = 0; i < maxLoadouts; i++)
                profiles[i] = new LoadoutProfile { name = i == 0 ? "Combat" : i == 1 ? "Farm" : "Boss", skillSlots = new string[5], equipmentSlots = new string[8], paragonAllocations = new Dictionary<string, int>(), isEmpty = true };
            LoadProfiles();
        }

        public void SaveCurrentToProfile(int idx)
        {
            if (idx < 0 || idx >= maxLoadouts) return;
            var p = profiles[idx];
            if (LootFilterSystem.Instance != null) p.lootFilterPreset = LootFilterSystem.Instance.ActivePresetIndex;
            if (MercenarySystem.Instance != null) p.activeMercenary = MercenarySystem.Instance.ActiveMercenaryId ?? "";
            p.isEmpty = false;
            SaveProfiles();
            OnLoadoutSaved?.Invoke(idx);
            NotificationManager.Instance?.ShowNotification($"Loadout '{p.name}' saved", NotificationType.System);
        }

        public void RenameProfile(int idx, string name) { if (idx >= 0 && idx < maxLoadouts) { profiles[idx].name = name; SaveProfiles(); } }
        public void ClearProfile(int idx) { if (idx >= 0 && idx < maxLoadouts) { profiles[idx].skillSlots = new string[5]; profiles[idx].equipmentSlots = new string[8]; profiles[idx].paragonAllocations.Clear(); profiles[idx].isEmpty = true; SaveProfiles(); } }

        [ServerRpc(RequireOwnership = false)]
        public void SwitchLoadoutServerRpc(int profileIndex, ServerRpcParams rpcParams = default)
        { ulong cid = rpcParams.Receive.SenderClientId; if (profileIndex < 0 || profileIndex >= maxLoadouts) return; NotifySwitchClientRpc(cid, profileIndex); }

        public void CancelSwitch() { if (!isSwitching) return; isSwitching = false; switchTimer = 0; OnSwitchCancelled?.Invoke(); }

        private void Update()
        {
            if (!isSwitching) return;
            switchTimer += Time.deltaTime;
            OnSwitchProgress?.Invoke(Mathf.Clamp01(switchTimer / switchCastTime));
            if (switchTimer >= switchCastTime) { isSwitching = false; switchTimer = 0; ApplyProfile(); }
        }

        private void ApplyProfile()
        {
            var p = ActiveProfile; if (p == null || p.isEmpty) return;
            if (LootFilterSystem.Instance != null) LootFilterSystem.Instance.SetActivePreset(p.lootFilterPreset);
            OnLoadoutSwitched?.Invoke(activeProfileIndex);
            NotificationManager.Instance?.ShowNotification($"Loadout '{p.name}' applied!", NotificationType.Achievement);
        }

        public LoadoutProfile GetProfile(int idx) => (idx >= 0 && idx < maxLoadouts) ? profiles[idx] : null;
        public string[] GetProfileNames() { var n = new string[maxLoadouts]; for (int i = 0; i < maxLoadouts; i++) n[i] = profiles[i]?.name ?? $"Profile {i+1}"; return n; }
        public bool IsProfileEmpty(int idx) => idx < 0 || idx >= maxLoadouts || profiles[idx].isEmpty;

        [ClientRpc] private void NotifySwitchClientRpc(ulong tid, int idx)
        { if (NetworkManager.Singleton.LocalClientId != tid) return; activeProfileIndex = idx; isSwitching = true; switchTimer = 0; }

        private void SaveProfiles()
        {
            for (int i = 0; i < maxLoadouts; i++)
            {
                var p = profiles[i]; string pfx = $"Loadout_{i}_";
                PlayerPrefs.SetString(pfx + "Name", p.name); PlayerPrefs.SetInt(pfx + "Empty", p.isEmpty ? 1 : 0);
                PlayerPrefs.SetInt(pfx + "LootFilter", p.lootFilterPreset); PlayerPrefs.SetString(pfx + "Merc", p.activeMercenary ?? "");
                for (int s = 0; s < 5; s++) PlayerPrefs.SetString(pfx + $"Skill_{s}", p.skillSlots[s] ?? "");
                for (int e = 0; e < 8; e++) PlayerPrefs.SetString(pfx + $"Equip_{e}", p.equipmentSlots[e] ?? "");
                var pl = new List<string>(); foreach (var kv in p.paragonAllocations) pl.Add($"{kv.Key}:{kv.Value}");
                PlayerPrefs.SetString(pfx + "Paragon", string.Join(",", pl));
            }
            PlayerPrefs.SetInt("Loadout_Active", activeProfileIndex); PlayerPrefs.Save();
        }

        private void LoadProfiles()
        {
            activeProfileIndex = PlayerPrefs.GetInt("Loadout_Active", 0);
            for (int i = 0; i < maxLoadouts; i++)
            {
                string pfx = $"Loadout_{i}_";
                if (!PlayerPrefs.HasKey(pfx + "Name")) continue;
                profiles[i].name = PlayerPrefs.GetString(pfx + "Name", profiles[i].name);
                profiles[i].isEmpty = PlayerPrefs.GetInt(pfx + "Empty", 1) == 1;
                profiles[i].lootFilterPreset = PlayerPrefs.GetInt(pfx + "LootFilter", 0);
                profiles[i].activeMercenary = PlayerPrefs.GetString(pfx + "Merc", "");
                for (int s = 0; s < 5; s++) profiles[i].skillSlots[s] = PlayerPrefs.GetString(pfx + $"Skill_{s}", "");
                for (int e = 0; e < 8; e++) profiles[i].equipmentSlots[e] = PlayerPrefs.GetString(pfx + $"Equip_{e}", "");
                string ps = PlayerPrefs.GetString(pfx + "Paragon", "");
                if (!string.IsNullOrEmpty(ps)) foreach (string en in ps.Split(',')) { var parts = en.Split(':'); if (parts.Length == 2 && int.TryParse(parts[1], out int val)) profiles[i].paragonAllocations[parts[0]] = val; }
            }
        }

        public override void OnDestroy() { if (Instance == this) { OnLoadoutSaved = null; OnLoadoutSwitched = null; OnSwitchProgress = null; OnSwitchCancelled = null; Instance = null; } base.OnDestroy(); }
    }

    [System.Serializable]
    public class LoadoutProfile
    {
        public string name;
        public string[] skillSlots;
        public string[] equipmentSlots;
        public Dictionary<string, int> paragonAllocations = new Dictionary<string, int>();
        public int lootFilterPreset;
        public string activeMercenary;
        public bool isEmpty;
    }
}
