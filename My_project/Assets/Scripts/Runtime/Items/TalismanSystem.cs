using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class TalismanSystem : NetworkBehaviour
    {
        public static TalismanSystem Instance { get; private set; }
        [SerializeField] private int totalSlotCapacity = 6;
        private static readonly TalismanTemplate[] Templates = new TalismanTemplate[]
        {
            new TalismanTemplate("force", "Force Charm", 1, "power", "STR", 3f),
            new TalismanTemplate("wind", "Wind Charm", 1, "swift", "AGI", 3f),
            new TalismanTemplate("life", "Life Charm", 1, "endurance", "VIT", 3f),
            new TalismanTemplate("mind", "Mind Charm", 1, "arcane", "INT", 3f),
            new TalismanTemplate("luck", "Luck Charm", 1, "shadow", "LUK", 3f),
            new TalismanTemplate("assault", "Assault Talisman", 2, "power", "ATK_PCT", 5f),
            new TalismanTemplate("guard", "Guard Talisman", 2, "endurance", "DEF_PCT", 5f),
            new TalismanTemplate("vitality", "Vitality Talisman", 2, "endurance", "HP_PCT", 8f),
            new TalismanTemplate("arcane_t", "Arcane Talisman", 2, "arcane", "MP_PCT", 8f),
            new TalismanTemplate("precision", "Precision Talisman", 2, "shadow", "CRIT_RATE", 3f),
            new TalismanTemplate("primal", "Primal Relic", 3, "power", "ALL_STAT", 5f),
            new TalismanTemplate("aegis", "Aegis Relic", 3, "endurance", "DMG_REDUCE", 10f),
            new TalismanTemplate("wisdom", "Wisdom Relic", 3, "arcane", "EXP_BONUS", 15f),
            new TalismanTemplate("fortune", "Fortune Relic", 3, "swift", "GOLD_BONUS", 15f),
            new TalismanTemplate("eldritch", "Eldritch Relic", 3, "shadow", "SKILL_DMG", 12f),
        };
        private static readonly TalismanSet[] Sets = new TalismanSet[]
        {
            new TalismanSet("power", "Power Set", new SetBonus("STR", 5f), new SetBonus("DMG_PCT", 10f), new SetBonus("CRIT_DMG", 25f)),
            new TalismanSet("endurance", "Endurance Set", new SetBonus("VIT", 5f), new SetBonus("DEF", 10f), new SetBonus("DMG_REDUCE", 15f)),
            new TalismanSet("arcane", "Arcane Set", new SetBonus("INT", 5f), new SetBonus("MDEF", 10f), new SetBonus("SKILL_CD", 20f)),
            new TalismanSet("swift", "Swift Set", new SetBonus("AGI", 5f), new SetBonus("DODGE", 8f), new SetBonus("ATK_SPD", 15f)),
            new TalismanSet("shadow", "Shadow Set", new SetBonus("LUK", 5f), new SetBonus("CRIT_RATE", 5f), new SetBonus("LIFESTEAL", 8f)),
        };
        private Dictionary<ulong, List<string>> equippedTalismans = new Dictionary<ulong, List<string>>();
        private void Awake() { if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this; }
        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this)
                Instance = null;
        }
        public TalismanTemplate GetTemplate(string id) { foreach (var t in Templates) if (t.id == id) return t; return null; }
        public TalismanTemplate[] GetAllTemplates() => Templates;
        public TalismanSet GetSet(string sid) { foreach (var s in Sets) if (s.setId == sid) return s; return null; }
        [ServerRpc(RequireOwnership = false)]
        public void EquipTalismanServerRpc(string talId, ServerRpcParams rpcParams = default)
        {
            ulong cid = rpcParams.Receive.SenderClientId;
            EnsureData(cid);
            var template = GetTemplate(talId);
            if (template == null) { Notify(cid, "Invalid talisman."); return; }
            int used = GetUsedSlots(cid);
            if (used + template.size > totalSlotCapacity)
            { Notify(cid, "Not enough slots."); return; }
            equippedTalismans[cid].Add(talId);
            Notify(cid, "Talisman equipped: " + template.talismanName);
            Save(cid);
        }
        [ServerRpc(RequireOwnership = false)]
        public void UnequipTalismanServerRpc(string talId, ServerRpcParams rpcParams = default)
        {
            ulong cid = rpcParams.Receive.SenderClientId;
            EnsureData(cid);
            if (!equippedTalismans[cid].Contains(talId)) { Notify(cid, "Not equipped."); return; }
            equippedTalismans[cid].Remove(talId);
            Notify(cid, "Talisman removed: " + talId); Save(cid);
        }
        public TalismanBonusResult CalculateBonuses(ulong cid)
        {
            var result = new TalismanBonusResult(); EnsureData(cid);
            foreach (var tid in equippedTalismans[cid])
            { var t = GetTemplate(tid); if (t != null) result.AddStat(t.statType, t.statValue); }
            var sc = GetSetCounts(cid);
            foreach (var kvp in sc) {
                var set = GetSet(kvp.Key); if (set == null) continue;
                if (kvp.Value >= 2) result.AddStat(set.bonus2.statType, set.bonus2.value);
                if (kvp.Value >= 4) result.AddStat(set.bonus4.statType, set.bonus4.value);
                if (kvp.Value >= 6) result.AddStat(set.bonus6.statType, set.bonus6.value);
            } return result;
        }
        public int GetUsedSlots(ulong cid) { EnsureData(cid); int t=0; foreach(var tid in equippedTalismans[cid]){var x=GetTemplate(tid);if(x!=null)t+=x.size;} return t; }
        public List<string> GetEquipped(ulong cid) { EnsureData(cid); return new List<string>(equippedTalismans[cid]); }
        public Dictionary<string, int> GetSetCounts(ulong cid) {
            EnsureData(cid); var c=new Dictionary<string,int>();
            foreach(var tid in equippedTalismans[cid]){var t=GetTemplate(tid);if(t==null)continue;if(!c.ContainsKey(t.setId))c[t.setId]=0;c[t.setId]++;}return c;}
        private void EnsureData(ulong cid) { if(!equippedTalismans.ContainsKey(cid)){equippedTalismans[cid]=new List<string>();Load(cid);} }
        private void Notify(ulong cid, string msg) { NotifyClientRpc(cid, msg); }
        [ClientRpc]
        private void NotifyClientRpc(ulong target, string msg)
        { if (NetworkManager.Singleton.LocalClientId != target) return; NotificationManager.Instance?.ShowNotification(msg, NotificationType.System); }
        private void Save(ulong cid) {
            if(!equippedTalismans.ContainsKey(cid))return;
            PlayerPrefs.SetString("Talisman_"+cid, string.Join(",", equippedTalismans[cid])); PlayerPrefs.Save(); }
        private void Load(ulong cid) {
            string key="Talisman_"+cid; if(!PlayerPrefs.HasKey(key))return;
            string d=PlayerPrefs.GetString(key); if(string.IsNullOrEmpty(d))return;
            equippedTalismans[cid]=new List<string>(d.Split(new char[]{(char)44},System.StringSplitOptions.RemoveEmptyEntries)); }
        [System.Serializable] public class TalismanTemplate {
            public string id,talismanName,setId,statType; public int size; public float statValue;
            public TalismanTemplate(){}
            public TalismanTemplate(string id,string nm,int sz,string set,string st,float v){this.id=id;talismanName=nm;size=sz;setId=set;statType=st;statValue=v;} }
        [System.Serializable] public class TalismanSet {
            public string setId,setName; public SetBonus bonus2,bonus4,bonus6;
            public TalismanSet(){}
            public TalismanSet(string id,string nm,SetBonus b2,SetBonus b4,SetBonus b6){setId=id;setName=nm;bonus2=b2;bonus4=b4;bonus6=b6;} }
        [System.Serializable] public class SetBonus {
            public string statType; public float value; public SetBonus(){}
            public SetBonus(string st,float v){statType=st;value=v;} }
        public class TalismanBonusResult {
            public Dictionary<string,float> stats=new Dictionary<string,float>();
            public void AddStat(string t,float v){if(!stats.ContainsKey(t))stats[t]=0f;stats[t]+=v;}
            public float GetStat(string t)=>stats.ContainsKey(t)?stats[t]:0f; }
    }
}