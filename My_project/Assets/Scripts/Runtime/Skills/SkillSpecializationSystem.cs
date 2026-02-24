using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class SkillSpecializationSystem : NetworkBehaviour
    {
        public static SkillSpecializationSystem Instance { get; private set; }
        [SerializeField] private int maxSpecializedSkills = 5;
        [SerializeField] private int maxSpecLevel = 20;
        [SerializeField] private float baseExpPerUse = 10f;
        private Dictionary<string, SkillSpecData> specData = new Dictionary<string, SkillSpecData>();
        private List<string> specializedSkillIds = new List<string>();
        public System.Action<string, string, int> OnNodeLevelUp;
        public System.Action<string> OnSkillSpecialized;
        public System.Action<string> OnSkillUnspecialized;
        public int MaxSpecializedSkills => maxSpecializedSkills;
        public int SpecializedCount => specializedSkillIds.Count;
        private void Awake() { if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this; }
        [ServerRpc(RequireOwnership = false)]
        public void SpecializeSkillServerRpc(string skillId, ServerRpcParams rpcParams = default)
        { ulong cid = rpcParams.Receive.SenderClientId; if (specializedSkillIds.Count >= maxSpecializedSkills || specializedSkillIds.Contains(skillId)) return; NotifySpecClientRpc(cid, skillId); }
        [ServerRpc(RequireOwnership = false)]
        public void UnspecializeSkillServerRpc(string skillId, ServerRpcParams rpcParams = default)
        { ulong cid = rpcParams.Receive.SenderClientId; if (!specializedSkillIds.Contains(skillId)) return; NotifyUnspecClientRpc(cid, skillId); }
        public bool IsSpecialized(string sid) => specializedSkillIds.Contains(sid);
        public List<string> GetSpecializedSkills() => new List<string>(specializedSkillIds);
        public void GainSpecExp(string sid, float amt = 0)
        {
            if (!specializedSkillIds.Contains(sid)) return;
            if (!specData.TryGetValue(sid, out var d)) { d = new SkillSpecData { skillId = sid }; specData[sid] = d; }
            if (d.specLevel >= maxSpecLevel) return;
            float gain = amt > 0 ? amt : baseExpPerUse; d.specExp += gain;
            float need = 100f + d.specLevel * d.specLevel * 20f;
            while (d.specExp >= need && d.specLevel < maxSpecLevel) { d.specExp -= need; d.specLevel++; d.availablePoints++; need = 100f + d.specLevel * d.specLevel * 20f; }
        }
        public SkillSpecData GetSpecData(string sid) { specData.TryGetValue(sid, out var d); return d; }
        [ServerRpc(RequireOwnership = false)]
        public void InvestNodeServerRpc(string sid, string nid, ServerRpcParams rpcParams = default)
        { NotifyInvestClientRpc(rpcParams.Receive.SenderClientId, sid, nid); }
        [ServerRpc(RequireOwnership = false)]
        public void ResetNodesServerRpc(string sid, ServerRpcParams rpcParams = default)
        { ulong cid = rpcParams.Receive.SenderClientId; var s = GetStats(cid); if (s == null || s.Gold < 5000) return; s.ChangeGold(-5000); NotifyResetClientRpc(cid, sid); }
        public int GetNodeLevel(string sid, string nid) { if (!specData.TryGetValue(sid, out var d)) return 0; return d.nodeInvestments.TryGetValue(nid, out int lv) ? lv : 0; }
        public SpecBonus GetSpecBonus(string sid)
        {
            var b = new SpecBonus(); if (!specData.TryGetValue(sid, out var d)) return b;
            foreach (var kv in d.nodeInvestments) { float v = kv.Value; switch(kv.Key) { case "dmg_up": b.damagePercent += v * 5f; break; case "cooldown_down": b.cooldownReduction += v * 3f; break; case "aoe_up": b.aoePercent += v * 8f; break; case "mana_down": b.manaCostReduction += v * 4f; break; } }
            return b;
        }
        [ClientRpc] private void NotifySpecClientRpc(ulong tid, string sid)
        { if (NetworkManager.Singleton.LocalClientId != tid) return; if (!specializedSkillIds.Contains(sid)) { specializedSkillIds.Add(sid); if (!specData.ContainsKey(sid)) specData[sid] = new SkillSpecData { skillId = sid }; } OnSkillSpecialized?.Invoke(sid); }
        [ClientRpc] private void NotifyUnspecClientRpc(ulong tid, string sid)
        { if (NetworkManager.Singleton.LocalClientId != tid) return; specializedSkillIds.Remove(sid); OnSkillUnspecialized?.Invoke(sid); }
        [ClientRpc] private void NotifyInvestClientRpc(ulong tid, string sid, string nid)
        { if (NetworkManager.Singleton.LocalClientId != tid) return; if (!specData.TryGetValue(sid, out var d) || d.availablePoints <= 0) return; if (!d.nodeInvestments.ContainsKey(nid)) d.nodeInvestments[nid] = 0; d.nodeInvestments[nid]++; d.availablePoints--; OnNodeLevelUp?.Invoke(sid, nid, d.nodeInvestments[nid]); }
        [ClientRpc] private void NotifyResetClientRpc(ulong tid, string sid)
        { if (NetworkManager.Singleton.LocalClientId != tid) return; if (!specData.TryGetValue(sid, out var d)) return; int pts = 0; foreach (var kv in d.nodeInvestments) pts += kv.Value; d.nodeInvestments.Clear(); d.availablePoints += pts; d.selectedMutation = -1; }
        private PlayerStatsData GetStats(ulong cid) { return NetworkManager.Singleton.ConnectedClients.TryGetValue(cid, out var c) ? c.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats : null; }
        public override void OnDestroy() { if (Instance == this) { OnNodeLevelUp = null; OnSkillSpecialized = null; OnSkillUnspecialized = null; Instance = null; } base.OnDestroy(); }
    }
    public class SkillSpecData { public string skillId; public int specLevel; public float specExp; public int availablePoints; public int selectedMutation = -1; public Dictionary<string, int> nodeInvestments = new Dictionary<string, int>(); }
    public struct SpecBonus { public float damagePercent; public float cooldownReduction; public float aoePercent; public float manaCostReduction; }
}
