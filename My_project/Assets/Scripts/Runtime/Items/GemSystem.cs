using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class GemSystem : NetworkBehaviour
    {
        public static GemSystem Instance { get; private set; }

        private Dictionary<ulong, List<GemInstance>> playerGems = new Dictionary<ulong, List<GemInstance>>();

        private static readonly GemTemplate[] gemTemplates = new GemTemplate[]
        {
            new GemTemplate("ruby", "Ruby", GemType.Ruby, "Increases Strength"),
            new GemTemplate("sapphire", "Sapphire", GemType.Sapphire, "Increases Intelligence"),
            new GemTemplate("emerald", "Emerald", GemType.Emerald, "Increases Agility"),
            new GemTemplate("topaz", "Topaz", GemType.Topaz, "Increases Vitality"),
            new GemTemplate("diamond", "Diamond", GemType.Diamond, "Increases All Stats")
        };

        private static readonly float[][] gemValues = new float[][]
        {
            new float[] { 2f, 5f, 10f, 18f, 30f },
            new float[] { 2f, 5f, 10f, 18f, 30f },
            new float[] { 2f, 5f, 10f, 18f, 30f },
            new float[] { 2f, 5f, 10f, 18f, 30f },
            new float[] { 1f, 2f, 4f, 8f, 15f }
        };

        private static readonly long[] combineCosts = new long[] { 500, 2000, 8000, 30000, 0 };

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

        [ServerRpc(RequireOwnership = false)]
        public void AddGemServerRpc(int gemTypeIndex, int grade, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (gemTypeIndex < 0 || gemTypeIndex >= gemTemplates.Length) return;
            if (grade < 0 || grade > 4) return;

            if (!playerGems.ContainsKey(clientId))
                playerGems[clientId] = new List<GemInstance>();

            var gem = new GemInstance
            {
                gemType = (GemType)gemTypeIndex,
                grade = (GemGrade)grade,
                isSocketed = false,
                socketedItemId = ""
            };
            playerGems[clientId].Add(gem);

            NotifyGemAddedClientRpc(gemTypeIndex, grade, clientId);
        }

        [ClientRpc]
        private void NotifyGemAddedClientRpc(int typeIndex, int grade, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            string[] gradeNames = { "Chipped", "Plain", "Perfect", "Royal", "Imperial" };
            if (grade < 0 || grade >= gradeNames.Length) grade = Mathf.Clamp(grade, 0, gradeNames.Length - 1);
            if (typeIndex < 0 || typeIndex >= gemTemplates.Length) return;
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(
                    gradeNames[grade] + " " + gemTemplates[typeIndex].gemName + " acquired!", NotificationType.System);
        }

        [ServerRpc(RequireOwnership = false)]
        public void CombineGemsServerRpc(int gemTypeIndex, int currentGrade, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (gemTypeIndex < 0 || gemTypeIndex >= gemTemplates.Length) return;
            if (currentGrade < 0 || currentGrade >= 4) return;

            if (!playerGems.ContainsKey(clientId)) return;
            var gems = playerGems[clientId];

            int count = 0;
            for (int i = 0; i < gems.Count; i++)
            {
                if ((int)gems[i].gemType == gemTypeIndex && (int)gems[i].grade == currentGrade && !gems[i].isSocketed)
                    count++;
            }

            if (count < 3) { NotifyCombineFailClientRpc(clientId); return; }

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var combineClient)) return;
            var playerObj = combineClient.PlayerObject;
            if (playerObj == null) return;
            var stats = playerObj.GetComponent<PlayerStatsManager>()?.CurrentStats;
            if (stats == null) return;

            long cost = combineCosts[currentGrade];
            if (stats.Gold < cost) { NotifyCombineFailClientRpc(clientId); return; }

            stats.ChangeGold(-cost);

            int removed = 0;
            for (int i = gems.Count - 1; i >= 0 && removed < 3; i--)
            {
                if ((int)gems[i].gemType == gemTypeIndex && (int)gems[i].grade == currentGrade && !gems[i].isSocketed)
                {
                    gems.RemoveAt(i);
                    removed++;
                }
            }

            int newGrade = currentGrade + 1;
            gems.Add(new GemInstance
            {
                gemType = (GemType)gemTypeIndex,
                grade = (GemGrade)newGrade,
                isSocketed = false,
                socketedItemId = ""
            });

            NotifyCombineSuccessClientRpc(gemTypeIndex, newGrade, clientId);
        }

        [ClientRpc]
        private void NotifyCombineFailClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(
                    "Gem combination failed! Need 3 gems of same type/grade and enough gold.", NotificationType.Warning);
        }

        [ClientRpc]
        private void NotifyCombineSuccessClientRpc(int typeIndex, int newGrade, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            string[] gradeNames = { "Chipped", "Plain", "Perfect", "Royal", "Imperial" };
            if (newGrade < 0 || newGrade >= gradeNames.Length) newGrade = Mathf.Clamp(newGrade, 0, gradeNames.Length - 1);
            if (typeIndex < 0 || typeIndex >= gemTemplates.Length) return;
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(
                    gradeNames[newGrade] + " " + gemTemplates[typeIndex].gemName + " created!", NotificationType.System);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SocketGemServerRpc(int gemIndex, string itemId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!playerGems.ContainsKey(clientId)) return;
            var gems = playerGems[clientId];

            if (gemIndex < 0 || gemIndex >= gems.Count) return;
            if (gems[gemIndex].isSocketed) return;

            var gem = gems[gemIndex];
            gem.isSocketed = true;
            gem.socketedItemId = itemId;
            gems[gemIndex] = gem;

            NotifySocketClientRpc(gemIndex, true, clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void UnsocketGemServerRpc(int gemIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!playerGems.ContainsKey(clientId)) return;
            var gems = playerGems[clientId];

            if (gemIndex < 0 || gemIndex >= gems.Count) return;
            if (!gems[gemIndex].isSocketed) return;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var unsocketClient)) return;
            var playerObj = unsocketClient.PlayerObject;
            if (playerObj == null) return;
            var stats = playerObj.GetComponent<PlayerStatsManager>()?.CurrentStats;
            if (stats == null) return;

            long unsocketCost = 1000 * ((int)gems[gemIndex].grade + 1);
            if (stats.Gold < unsocketCost) return;
            stats.ChangeGold(-unsocketCost);

            var gem = gems[gemIndex];
            gem.isSocketed = false;
            gem.socketedItemId = "";
            gems[gemIndex] = gem;

            NotifySocketClientRpc(gemIndex, false, clientId);
        }

        [ClientRpc]
        private void NotifySocketClientRpc(int gemIndex, bool socketed, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            string msg = socketed ? "Gem socketed!" : "Gem removed from socket.";
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(msg, NotificationType.System);
        }

        public GemBonusResult CalculateGemBonuses(ulong clientId)
        {
            var result = new GemBonusResult();
            if (!playerGems.ContainsKey(clientId)) return result;

            foreach (var gem in playerGems[clientId])
            {
                if (!gem.isSocketed) continue;
                int typeIdx = (int)gem.gemType;
                int gradeIdx = (int)gem.grade;
                float value = gemValues[typeIdx][gradeIdx];

                switch (gem.gemType)
                {
                    case GemType.Ruby: result.bonusSTR += value; break;
                    case GemType.Sapphire: result.bonusINT += value; break;
                    case GemType.Emerald: result.bonusAGI += value; break;
                    case GemType.Topaz: result.bonusVIT += value; break;
                    case GemType.Diamond:
                        result.bonusSTR += value;
                        result.bonusINT += value;
                        result.bonusAGI += value;
                        result.bonusVIT += value;
                        break;
                }
            }

            return result;
        }

        public List<GemInstance> GetPlayerGems(ulong clientId)
        {
            if (!playerGems.ContainsKey(clientId)) return new List<GemInstance>();
            return new List<GemInstance>(playerGems[clientId]);
        }

        public int GetGemCount(ulong clientId, GemType type, GemGrade grade)
        {
            if (!playerGems.ContainsKey(clientId)) return 0;
            int count = 0;
            foreach (var gem in playerGems[clientId])
            {
                if (gem.gemType == type && gem.grade == grade && !gem.isSocketed)
                    count++;
            }
            return count;
        }

        public float GetGemValue(GemType type, GemGrade grade)
        {
            return gemValues[(int)type][(int)grade];
        }

        public long GetCombineCost(GemGrade currentGrade)
        {
            return combineCosts[(int)currentGrade];
        }

        public GemTemplate GetTemplate(GemType type)
        {
            return gemTemplates[(int)type];
        }
    }

    public enum GemType { Ruby, Sapphire, Emerald, Topaz, Diamond }
    public enum GemGrade { Chipped, Plain, Perfect, Royal, Imperial }

    [System.Serializable]
    public struct GemInstance
    {
        public GemType gemType;
        public GemGrade grade;
        public bool isSocketed;
        public string socketedItemId;
    }

    [System.Serializable]
    public class GemTemplate
    {
        public string gemId;
        public string gemName;
        public GemType gemType;
        public string description;

        public GemTemplate(string id, string name, GemType type, string desc)
        {
            gemId = id;
            gemName = name;
            gemType = type;
            description = desc;
        }
    }

    [System.Serializable]
    public struct GemBonusResult
    {
        public float bonusSTR;
        public float bonusINT;
        public float bonusAGI;
        public float bonusVIT;
    }
}
