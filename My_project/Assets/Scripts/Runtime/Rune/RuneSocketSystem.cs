using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 룬 소켓 시스템 - 서버 권위적
    /// 장비에 소켓 추가, 룬 장착/해제, 조합 효과 계산
    /// </summary>
    public class RuneSocketSystem : NetworkBehaviour
    {
        public static RuneSocketSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int socketCostBase = 500;          // 소켓 추가 기본 비용
        [SerializeField] private float socketCostMultiplier = 2.5f; // 소켓 수별 비용 배율
        [SerializeField] private int maxSockets = 3;                // 장비당 최대 소켓
        [SerializeField] private int removeRuneCost = 200;          // 룬 제거 비용

        // 룬 데이터 캐시
        private Dictionary<string, RuneData> runeDatabase = new Dictionary<string, RuneData>();

        // 이벤트
        public System.Action<string, string> OnRuneSocketed;    // instanceId, runeId
        public System.Action<string, int> OnRuneRemoved;        // instanceId, socketIndex
        public System.Action<string> OnSocketAdded;             // instanceId

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            LoadRuneData();
        }

        private void LoadRuneData()
        {
            var runes = Resources.LoadAll<RuneData>("ScriptableObjects/Runes");
            foreach (var rune in runes)
            {
                if (!string.IsNullOrEmpty(rune.RuneId))
                    runeDatabase[rune.RuneId] = rune;
            }
            Debug.Log($"[RuneSocket] {runeDatabase.Count}개 룬 데이터 로드됨");
        }

        /// <summary>
        /// 룬 데이터 조회
        /// </summary>
        public RuneData GetRuneData(string runeId)
        {
            return runeDatabase.TryGetValue(runeId, out var rune) ? rune : null;
        }

        #region 소켓 관리

        /// <summary>
        /// 장비에 소켓 추가 (골드 소비)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void AddSocketServerRpc(int inventorySlot, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return;
            if (client.PlayerObject == null) return;

            var inventoryMgr = client.PlayerObject.GetComponent<InventoryManager>();
            var statsData = client.PlayerObject.GetComponent<PlayerStatsManager>()?.CurrentStats;
            if (inventoryMgr == null || statsData == null) return;

            var item = inventoryMgr.GetItemAtSlot(inventorySlot);
            if (item == null || item.ItemData == null || !item.ItemData.IsEquippable)
            {
                SendMessageClientRpc("장비 아이템만 소켓을 추가할 수 있습니다.", clientId);
                return;
            }

            // 현재 소켓 수 확인
            int currentSockets = GetSocketCount(item);
            if (currentSockets >= maxSockets)
            {
                SendMessageClientRpc($"최대 소켓 수({maxSockets})에 도달했습니다.", clientId);
                return;
            }

            // 비용 계산
            int cost = (int)(socketCostBase * Mathf.Pow(socketCostMultiplier, currentSockets));
            if (statsData.Gold < cost)
            {
                SendMessageClientRpc($"소켓 추가에 {cost}G가 필요합니다.", clientId);
                return;
            }

            // 골드 차감 + 소켓 추가
            statsData.ChangeGold(-cost);
            SetSocketCount(item, currentSockets + 1);

            NotifySocketAddedClientRpc(item.InstanceId, currentSockets + 1, clientId);
        }

        /// <summary>
        /// 룬 장착
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SocketRuneServerRpc(int equipSlot, int socketIndex, string runeItemId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return;
            if (client.PlayerObject == null) return;

            var inventoryMgr = client.PlayerObject.GetComponent<InventoryManager>();
            if (inventoryMgr == null) return;

            var item = inventoryMgr.GetItemAtSlot(equipSlot);
            if (item == null || !item.ItemData.IsEquippable)
            {
                SendMessageClientRpc("유효하지 않은 장비입니다.", clientId);
                return;
            }

            // 소켓 수 체크
            int sockets = GetSocketCount(item);
            if (socketIndex < 0 || socketIndex >= sockets)
            {
                SendMessageClientRpc("유효하지 않은 소켓 인덱스입니다.", clientId);
                return;
            }

            // 이미 장착된 룬 체크
            string existingRune = GetRuneAtSocket(item, socketIndex);
            if (!string.IsNullOrEmpty(existingRune))
            {
                SendMessageClientRpc("이미 룬이 장착되어 있습니다. 먼저 제거하세요.", clientId);
                return;
            }

            // 룬 아이템 소유 체크
            if (!inventoryMgr.HasItem(runeItemId))
            {
                SendMessageClientRpc("룬을 보유하고 있지 않습니다.", clientId);
                return;
            }

            // 룬 데이터 확인
            var runeData = GetRuneData(runeItemId);
            if (runeData == null)
            {
                SendMessageClientRpc("유효하지 않은 룬입니다.", clientId);
                return;
            }

            // 소켓 색상 매칭 체크
            var socketColor = GetSocketColor(item, socketIndex);
            if (socketColor != SocketColor.White && runeData.SocketColor != socketColor)
            {
                SendMessageClientRpc("소켓 색상이 맞지 않습니다.", clientId);
                return;
            }

            // 룬 소비 & 장착
            inventoryMgr.Inventory.RemoveAllItems(runeItemId, 1);
            SetRuneAtSocket(item, socketIndex, runeItemId);

            NotifyRuneSocketedClientRpc(item.InstanceId, runeItemId, socketIndex, clientId);
        }

        /// <summary>
        /// 룬 제거 (골드 소비, 룬 반환)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RemoveRuneServerRpc(int equipSlot, int socketIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return;
            if (client.PlayerObject == null) return;

            var inventoryMgr = client.PlayerObject.GetComponent<InventoryManager>();
            var statsData = client.PlayerObject.GetComponent<PlayerStatsManager>()?.CurrentStats;
            if (inventoryMgr == null || statsData == null) return;

            var item = inventoryMgr.GetItemAtSlot(equipSlot);
            if (item == null) return;

            string runeId = GetRuneAtSocket(item, socketIndex);
            if (string.IsNullOrEmpty(runeId))
            {
                SendMessageClientRpc("해당 소켓에 룬이 없습니다.", clientId);
                return;
            }

            if (statsData.Gold < removeRuneCost)
            {
                SendMessageClientRpc($"룬 제거에 {removeRuneCost}G가 필요합니다.", clientId);
                return;
            }

            // 골드 차감, 룬 제거, 룬 아이템 반환
            statsData.ChangeGold(-removeRuneCost);
            ClearRuneAtSocket(item, socketIndex);

            // 룬을 아이템으로 반환
            var runeItemData = ItemDatabase.GetItem(runeId);
            if (runeItemData != null)
            {
                inventoryMgr.AddItem(new ItemInstance(runeItemData, 1));
            }

            NotifyRuneRemovedClientRpc(item.InstanceId, socketIndex, clientId);
        }

        #endregion

        #region 룬 보너스 계산

        /// <summary>
        /// 아이템에 장착된 모든 룬의 스탯 보너스 합산
        /// </summary>
        public RuneBonuses GetRuneBonuses(ItemInstance item)
        {
            var bonuses = new RuneBonuses();
            if (item == null) return bonuses;

            int sockets = GetSocketCount(item);
            var socketedRunes = new List<RuneData>();

            for (int i = 0; i < sockets; i++)
            {
                string runeId = GetRuneAtSocket(item, i);
                if (string.IsNullOrEmpty(runeId)) continue;

                var runeData = GetRuneData(runeId);
                if (runeData == null) continue;

                socketedRunes.Add(runeData);

                // 기본 보너스 합산
                bonuses.statBonus.strength += runeData.StatBonus.strength;
                bonuses.statBonus.agility += runeData.StatBonus.agility;
                bonuses.statBonus.vitality += runeData.StatBonus.vitality;
                bonuses.statBonus.intelligence += runeData.StatBonus.intelligence;
                bonuses.statBonus.defense += runeData.StatBonus.defense;
                bonuses.statBonus.magicDefense += runeData.StatBonus.magicDefense;
                bonuses.statBonus.luck += runeData.StatBonus.luck;
                bonuses.statBonus.stability += runeData.StatBonus.stability;

                bonuses.hpBonus += runeData.HPBonus;
                bonuses.mpBonus += runeData.MPBonus;
                bonuses.critChanceBonus += runeData.CritChanceBonus;
                bonuses.critDamageBonus += runeData.CritDamageBonus;
                bonuses.attackSpeedBonus += runeData.AttackSpeedBonus;
                bonuses.moveSpeedBonus += runeData.MoveSpeedBonus;
                bonuses.cooldownReduction += runeData.CooldownReduction;
                bonuses.lifestealPercent += runeData.LifestealPercent;
                bonuses.expBonusPercent += runeData.ExpBonusPercent;
                bonuses.goldBonusPercent += runeData.GoldBonusPercent;
                bonuses.elementalDamageBonus += runeData.ElementalDamageBonus;
            }

            // 조합 효과 체크
            for (int i = 0; i < socketedRunes.Count; i++)
            {
                if (string.IsNullOrEmpty(socketedRunes[i].ComboRuneId)) continue;

                for (int j = i + 1; j < socketedRunes.Count; j++)
                {
                    if (socketedRunes[j].RuneId == socketedRunes[i].ComboRuneId)
                    {
                        bonuses.comboMultiplier += socketedRunes[i].ComboBonusMultiplier;
                        bonuses.hasComboEffect = true;
                    }
                }
            }

            return bonuses;
        }

        /// <summary>
        /// 플레이어의 모든 장비 룬 보너스 합산
        /// </summary>
        public RuneBonuses GetTotalRuneBonuses(ulong clientId)
        {
            var total = new RuneBonuses();

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return total;
            if (client.PlayerObject == null) return total;

            var inventoryMgr = client.PlayerObject.GetComponent<InventoryManager>();
            if (inventoryMgr == null) return total;

            // 모든 장비 슬롯의 룬 보너스 합산
            for (int slot = 0; slot < 40; slot++)
            {
                var item = inventoryMgr.GetItemAtSlot(slot);
                if (item == null || item.ItemData == null || !item.ItemData.IsEquippable) continue;

                var bonuses = GetRuneBonuses(item);
                total.statBonus.strength += bonuses.statBonus.strength;
                total.statBonus.agility += bonuses.statBonus.agility;
                total.statBonus.vitality += bonuses.statBonus.vitality;
                total.statBonus.intelligence += bonuses.statBonus.intelligence;
                total.statBonus.defense += bonuses.statBonus.defense;
                total.statBonus.magicDefense += bonuses.statBonus.magicDefense;
                total.statBonus.luck += bonuses.statBonus.luck;
                total.statBonus.stability += bonuses.statBonus.stability;
                total.hpBonus += bonuses.hpBonus;
                total.mpBonus += bonuses.mpBonus;
                total.critChanceBonus += bonuses.critChanceBonus;
                total.critDamageBonus += bonuses.critDamageBonus;
                total.attackSpeedBonus += bonuses.attackSpeedBonus;
                total.moveSpeedBonus += bonuses.moveSpeedBonus;
                total.cooldownReduction += bonuses.cooldownReduction;
                total.lifestealPercent += bonuses.lifestealPercent;
                total.expBonusPercent += bonuses.expBonusPercent;
                total.goldBonusPercent += bonuses.goldBonusPercent;
                total.elementalDamageBonus += bonuses.elementalDamageBonus;
                if (bonuses.hasComboEffect)
                {
                    total.hasComboEffect = true;
                    total.comboMultiplier += bonuses.comboMultiplier;
                }
            }

            return total;
        }

        #endregion

        #region ItemInstance CustomData 헬퍼

        private const string KEY_SOCKET_COUNT = "rune_sockets";
        private const string KEY_RUNE_PREFIX = "rune_slot_";
        private const string KEY_SOCKET_COLOR_PREFIX = "socket_color_";

        public int GetSocketCount(ItemInstance item)
        {
            if (item.CustomData.TryGetValue(KEY_SOCKET_COUNT, out string val) && int.TryParse(val, out int count))
                return count;
            return 0;
        }

        private void SetSocketCount(ItemInstance item, int count)
        {
            item.CustomData[KEY_SOCKET_COUNT] = count.ToString();
        }

        public string GetRuneAtSocket(ItemInstance item, int socketIndex)
        {
            string key = KEY_RUNE_PREFIX + socketIndex;
            return item.CustomData.TryGetValue(key, out string val) ? val : null;
        }

        private void SetRuneAtSocket(ItemInstance item, int socketIndex, string runeId)
        {
            item.CustomData[KEY_RUNE_PREFIX + socketIndex] = runeId;
        }

        private void ClearRuneAtSocket(ItemInstance item, int socketIndex)
        {
            item.CustomData.Remove(KEY_RUNE_PREFIX + socketIndex);
        }

        public SocketColor GetSocketColor(ItemInstance item, int socketIndex)
        {
            string key = KEY_SOCKET_COLOR_PREFIX + socketIndex;
            if (item.CustomData.TryGetValue(key, out string val) && System.Enum.TryParse<SocketColor>(val, out var color))
                return color;
            return SocketColor.White; // 기본 만능 소켓
        }

        /// <summary>
        /// 아이템의 소켓/룬 정보 텍스트
        /// </summary>
        public string GetSocketInfoText(ItemInstance item)
        {
            int sockets = GetSocketCount(item);
            if (sockets == 0) return "";

            string info = $"\n소켓 ({sockets}):";
            for (int i = 0; i < sockets; i++)
            {
                string runeId = GetRuneAtSocket(item, i);
                if (string.IsNullOrEmpty(runeId))
                {
                    info += "\n  [빈 소켓]";
                }
                else
                {
                    var runeData = GetRuneData(runeId);
                    string runeName = runeData != null ? runeData.RuneName : runeId;
                    info += $"\n  [{runeName}]";
                }
            }
            return info;
        }

        #endregion

        #region ClientRPCs

        [ClientRpc]
        private void NotifySocketAddedClientRpc(string instanceId, int totalSockets, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            OnSocketAdded?.Invoke(instanceId);

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"소켓 추가 완료! (총 {totalSockets}개)", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyRuneSocketedClientRpc(string instanceId, string runeId, int socketIndex, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            OnRuneSocketed?.Invoke(instanceId, runeId);

            var runeData = GetRuneData(runeId);
            var notif = NotificationManager.Instance;
            if (notif != null && runeData != null)
                notif.ShowNotification($"룬 장착: {runeData.RuneName}", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyRuneRemovedClientRpc(string instanceId, int socketIndex, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            OnRuneRemoved?.Invoke(instanceId, socketIndex);

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification("룬이 제거되었습니다.", NotificationType.System);
        }

        [ClientRpc]
        private void SendMessageClientRpc(string message, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification(message, NotificationType.Warning);
        }

        #endregion

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }

    /// <summary>
    /// 룬 보너스 총합 구조체
    /// </summary>
    public struct RuneBonuses
    {
        public StatBlock statBonus;
        public float hpBonus;
        public float mpBonus;
        public float critChanceBonus;
        public float critDamageBonus;
        public float attackSpeedBonus;
        public float moveSpeedBonus;
        public float cooldownReduction;
        public float lifestealPercent;
        public float expBonusPercent;
        public float goldBonusPercent;
        public float elementalDamageBonus;
        public bool hasComboEffect;
        public float comboMultiplier;
    }
}
