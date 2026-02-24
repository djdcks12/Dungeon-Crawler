using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class WaystoneCraftingSystem : NetworkBehaviour
    {
        public static WaystoneCraftingSystem Instance { get; private set; }
        [SerializeField] private int maxAffixSlots = 3;
        private static readonly long[] AffixCosts = new long[] { 5000, 15000, 50000 };
        private static readonly WaystoneAffix[] Affixes = new WaystoneAffix[]
        {
            new WaystoneAffix("monster_hp", "Monster HP +30%", 1.3f, 1.2f),
            new WaystoneAffix("monster_dmg", "Monster DMG +25%", 1.25f, 1.15f),
            new WaystoneAffix("monster_speed", "Monster Speed +20%", 1.2f, 1.1f),
            new WaystoneAffix("extra_elite", "Extra Elites", 1.4f, 1.3f),
            new WaystoneAffix("no_regen", "No Regeneration", 1.3f, 1.25f),
            new WaystoneAffix("time_limit", "Time Limit -30%", 1.35f, 1.3f),
            new WaystoneAffix("boss_hp", "Boss HP +50%", 1.5f, 1.4f),
            new WaystoneAffix("thorns", "Thorns 15%", 1.15f, 1.1f),
            new WaystoneAffix("resist_down", "Resistance -20%", 1.2f, 1.15f),
            new WaystoneAffix("double_boss", "Double Boss", 1.8f, 1.6f),
            new WaystoneAffix("cursed_ground", "Cursed Ground", 1.25f, 1.2f),
            new WaystoneAffix("darkness", "Darkness", 1.15f, 1.1f),
        };
        private Dictionary<ulong, List<WaystoneInstance>> playerWaystones = new Dictionary<ulong, List<WaystoneInstance>>();
        private void Awake() { if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this; }
        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this)
                Instance = null;
        }
        public WaystoneAffix GetAffix(string id) { foreach(var a in Affixes) if(a.id==id) return a; return null; }
        public WaystoneAffix[] GetAllAffixes() => Affixes;
        [ServerRpc(RequireOwnership = false)]
        public void CreateWaystoneServerRpc(string dungeonId, ServerRpcParams rpcParams = default)
        {
            ulong cid = rpcParams.Receive.SenderClientId;
            EnsureData(cid);
            var ws = new WaystoneInstance { dungeonId = dungeonId, affixIds = new List<string>() };
            playerWaystones[cid].Add(ws);
            Notify(cid, "Waystone created for: " + dungeonId);
            Save(cid);
        }
        [ServerRpc(RequireOwnership = false)]
        public void AddAffixServerRpc(int waystoneIdx, string affixId, ServerRpcParams rpcParams = default)
        {
            ulong cid = rpcParams.Receive.SenderClientId;
            EnsureData(cid);
            if(waystoneIdx<0||waystoneIdx>=playerWaystones[cid].Count){Notify(cid,"Invalid waystone.");return;}
            var ws = playerWaystones[cid][waystoneIdx];
            if(ws.affixIds.Count>=maxAffixSlots){Notify(cid,"Max affixes reached.");return;}
            var affix = GetAffix(affixId);
            if(affix==null){Notify(cid,"Invalid affix.");return;}
            long cost = AffixCosts[ws.affixIds.Count];
            var stats = GetPlayerStats(cid);
            if(stats!=null && stats.Gold<cost){Notify(cid,"Not enough gold. Need: "+cost+"G");return;}
            if(stats!=null) stats.ChangeGold(-cost);
            ws.affixIds.Add(affixId);
            playerWaystones[cid][waystoneIdx] = ws;
            Notify(cid, "Affix added: " + affix.affixName + " (-" + cost + "G)");
            Save(cid);
        }
        public float GetDifficultyMultiplier(ulong cid, int wsIdx) {
            EnsureData(cid); if(wsIdx<0||wsIdx>=playerWaystones[cid].Count) return 1f;
            float m=1f; foreach(var aid in playerWaystones[cid][wsIdx].affixIds){var ax=GetAffix(aid);if(ax!=null)m*=ax.difficultyMult;} return m; }
        public float GetRewardMultiplier(ulong cid, int wsIdx) {
            EnsureData(cid); if(wsIdx<0||wsIdx>=playerWaystones[cid].Count) return 1f;
            float m=1f; foreach(var aid in playerWaystones[cid][wsIdx].affixIds){var ax=GetAffix(aid);if(ax!=null)m*=ax.rewardMult;} return m; }
        public string GetGrade(ulong cid, int wsIdx) {
            EnsureData(cid); if(wsIdx<0||wsIdx>=playerWaystones[cid].Count) return "None";
            int c=playerWaystones[cid][wsIdx].affixIds.Count;
            if(c>=3) return "Legendary"; if(c>=2) return "Rare"; if(c>=1) return "Magic"; return "Normal"; }
        public List<WaystoneInstance> GetWaystones(ulong cid) { EnsureData(cid); return new List<WaystoneInstance>(playerWaystones[cid]); }
        public void ConsumeWaystone(ulong cid, int wsIdx) {
            EnsureData(cid); if(wsIdx>=0&&wsIdx<playerWaystones[cid].Count) playerWaystones[cid].RemoveAt(wsIdx); Save(cid); }
        private void EnsureData(ulong cid) { if(!playerWaystones.ContainsKey(cid)){playerWaystones[cid]=new List<WaystoneInstance>();Load(cid);} }
        private PlayerStatsData GetPlayerStats(ulong cid) {
            if(NetworkManager.Singleton==null) return null;
            foreach(var c in NetworkManager.Singleton.ConnectedClientsList)
                if(c.ClientId==cid) return c.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
            return null; }
        private void Notify(ulong cid, string msg) { NotifyClientRpc(cid, msg); }
        [ClientRpc]
        private void NotifyClientRpc(ulong target, string msg)
        { if(NetworkManager.Singleton.LocalClientId!=target) return; NotificationManager.Instance?.ShowNotification(msg, NotificationType.System); }
        private void Save(ulong cid) {
            if(!playerWaystones.ContainsKey(cid)) return;
            var sd=new WaystoneSaveData{entries=new List<WaystoneSaveEntry>()};
            foreach(var ws in playerWaystones[cid])
                sd.entries.Add(new WaystoneSaveEntry{dungeonId=ws.dungeonId,affixes=string.Join(",",ws.affixIds)});
            PlayerPrefs.SetString("Waystone_"+cid, JsonUtility.ToJson(sd)); PlayerPrefs.Save(); }
        private void Load(ulong cid) {
            string key="Waystone_"+cid; if(!PlayerPrefs.HasKey(key)) return;
            var sd=JsonUtility.FromJson<WaystoneSaveData>(PlayerPrefs.GetString(key));
            if(sd?.entries==null) return;
            foreach(var e in sd.entries) {
                var ws=new WaystoneInstance{dungeonId=e.dungeonId,affixIds=new List<string>()};
                if(!string.IsNullOrEmpty(e.affixes))
                    ws.affixIds=new List<string>(e.affixes.Split(new char[]{(char)44},System.StringSplitOptions.RemoveEmptyEntries));
                playerWaystones[cid].Add(ws); } }
        [System.Serializable] public class WaystoneAffix {
            public string id, affixName; public float difficultyMult, rewardMult;
            public WaystoneAffix(){}
            public WaystoneAffix(string id,string nm,float diff,float rew){this.id=id;affixName=nm;difficultyMult=diff;rewardMult=rew;} }
        [System.Serializable] public struct WaystoneInstance {
            public string dungeonId; public List<string> affixIds; }
        [System.Serializable] private class WaystoneSaveData { public List<WaystoneSaveEntry> entries; }
        [System.Serializable] private class WaystoneSaveEntry { public string dungeonId, affixes; }
    
        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }
}
}