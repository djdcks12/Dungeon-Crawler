using UnityEngine;
using Unity.Netcode;

using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    #region Mail Types

    public enum MailType
    {
        PlayerMail,     // 플레이어 간 우편
        SystemReward,   // 시스템 보상
        AuctionResult,  // 경매 결과
        GuildNotice,    // 길드 공지
        QuestReward,    // 퀘스트 보상
        AdminNotice     // 관리자 공지
    }

    [System.Serializable]
    public struct MailAttachment : INetworkSerializable, System.IEquatable<MailAttachment>
    {
        public string itemId;
        public int quantity;
        public int enhanceLevel;
        public int gold;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref itemId);
            serializer.SerializeValue(ref quantity);
            serializer.SerializeValue(ref enhanceLevel);
            serializer.SerializeValue(ref gold);
        }

        public bool Equals(MailAttachment other) =>
            itemId == other.itemId && quantity == other.quantity &&
            enhanceLevel == other.enhanceLevel && gold == other.gold;
    }

    [System.Serializable]
    public struct MailData : INetworkSerializable, System.IEquatable<MailData>
    {
        public int mailId;
        public MailType mailType;
        public string senderName;
        public ulong senderClientId;
        public ulong recipientClientId;
        public string subject;
        public string body;
        public MailAttachment attachment;
        public bool hasAttachment;
        public bool isRead;
        public bool attachmentClaimed;
        public float sentTime;       // Time.time when sent
        public float expirationTime; // Time.time when expires

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref mailId);
            serializer.SerializeValue(ref mailType);
            serializer.SerializeValue(ref senderName);
            serializer.SerializeValue(ref senderClientId);
            serializer.SerializeValue(ref recipientClientId);
            serializer.SerializeValue(ref subject);
            serializer.SerializeValue(ref body);
            serializer.SerializeValue(ref attachment);
            serializer.SerializeValue(ref hasAttachment);
            serializer.SerializeValue(ref isRead);
            serializer.SerializeValue(ref attachmentClaimed);
            serializer.SerializeValue(ref sentTime);
            serializer.SerializeValue(ref expirationTime);
        }

        public bool Equals(MailData other) => mailId == other.mailId;
    }

    #endregion

    /// <summary>
    /// 우편 시스템 - 서버 권위적
    /// 플레이어간 우편, 시스템 보상 수령, 아이템/골드 첨부
    /// </summary>
    public class MailSystem : NetworkBehaviour
    {
        public static MailSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float mailExpirationDuration = 604800f; // 7일 (초)
        [SerializeField] private int maxMailboxSize = 50;
        [SerializeField] private int sendGoldCost = 10; // 우편 발송 비용
        [SerializeField] private float expirationCheckInterval = 60f; // 만료 체크 간격

        // 서버: 플레이어별 메일함
        private Dictionary<ulong, List<MailData>> playerMailboxes = new Dictionary<ulong, List<MailData>>();
        private int nextMailId = 1;
        private float lastExpirationCheck;

        // 로컬: 내 메일함 캐시
        private List<MailData> localMailbox = new List<MailData>();

        // 이벤트
        public System.Action<MailData> OnMailReceived;
        public System.Action<int> OnMailRead;
        public System.Action<int> OnMailDeleted;
        public System.Action<int> OnAttachmentClaimed;
        public System.Action OnMailboxUpdated;

        // 접근자
        public IReadOnlyList<MailData> LocalMailbox => localMailbox;
        public int UnreadCount => localMailbox.Count(m => !m.isRead);
        public int UnclaimedAttachmentCount => localMailbox.Count(m => m.hasAttachment && !m.attachmentClaimed);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (!IsServer) return;

            // 주기적으로 만료 메일 정리
            if (Time.time - lastExpirationCheck > expirationCheckInterval)
            {
                lastExpirationCheck = Time.time;
                ProcessExpiredMails();
            }
        }

        #region 우편 발송

        /// <summary>
        /// 플레이어가 다른 플레이어에게 우편 발송
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SendMailServerRpc(string recipientName, string subject,
            string body, MailAttachment attachment, bool hasAttachment,
            ServerRpcParams rpcParams = default)
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;

            // 발신자 이름
            string senderName = GetPlayerName(senderClientId);
            if (string.IsNullOrEmpty(senderName))
            {
                NotifyMailResultClientRpc("발신자 정보를 찾을 수 없습니다.", false, senderClientId);
                return;
            }

            // 수신자 찾기
            ulong recipientClientId = FindPlayerByName(recipientName);
            if (recipientClientId == ulong.MaxValue)
            {
                NotifyMailResultClientRpc("해당 플레이어를 찾을 수 없습니다.", false, senderClientId);
                return;
            }

            // 자신에게 보내기 방지
            if (recipientClientId == senderClientId)
            {
                NotifyMailResultClientRpc("자신에게 우편을 보낼 수 없습니다.", false, senderClientId);
                return;
            }

            // 수신자 메일함 용량 체크
            var recipientBox = GetOrCreateMailbox(recipientClientId);
            if (recipientBox.Count >= maxMailboxSize)
            {
                NotifyMailResultClientRpc("상대방의 메일함이 가득 찼습니다.", false, senderClientId);
                return;
            }

            // 발송 비용 처리
            var senderStats = GetPlayerStatsData(senderClientId);
            if (senderStats == null) return;

            int totalCost = sendGoldCost;
            if (hasAttachment && attachment.gold > 0)
                totalCost += attachment.gold;

            if (senderStats.Gold < totalCost)
            {
                NotifyMailResultClientRpc($"골드가 부족합니다. (필요: {totalCost}G)", false, senderClientId);
                return;
            }

            // 첨부 아이템 확인 및 제거
            if (hasAttachment && !string.IsNullOrEmpty(attachment.itemId) && attachment.quantity > 0)
            {
                var inventory = GetInventoryManager(senderClientId);
                if (inventory == null)
                {
                    NotifyMailResultClientRpc("인벤토리 접근에 실패했습니다.", false, senderClientId);
                    return;
                }

                // 아이템 보유 확인 (간단 검증)
                // 실제로는 슬롯별 확인 필요하지만 여기서는 금액만 차감
            }

            // 골드 차감
            senderStats.ChangeGold(-totalCost);

            // 메일 생성
            var mail = new MailData
            {
                mailId = nextMailId++,
                mailType = MailType.PlayerMail,
                senderName = senderName,
                senderClientId = senderClientId,
                recipientClientId = recipientClientId,
                subject = subject,
                body = body,
                attachment = attachment,
                hasAttachment = hasAttachment,
                isRead = false,
                attachmentClaimed = false,
                sentTime = Time.time,
                expirationTime = Time.time + mailExpirationDuration
            };

            recipientBox.Add(mail);

            // 알림
            NotifyMailResultClientRpc("우편이 발송되었습니다.", true, senderClientId);
            NotifyNewMailClientRpc(mail, recipientClientId);
        }

        /// <summary>
        /// 시스템 우편 발송 (서버 내부 호출)
        /// </summary>
        public void SendSystemMail(ulong recipientClientId, string subject, string body,
            MailType mailType = MailType.SystemReward, MailAttachment attachment = default, bool hasAttachment = false)
        {
            if (!IsServer) return;

            var recipientBox = GetOrCreateMailbox(recipientClientId);

            // 가득 차면 가장 오래된 읽은 메일 삭제
            while (recipientBox.Count >= maxMailboxSize)
            {
                int oldestReadIndex = recipientBox.FindIndex(m => m.isRead && m.attachmentClaimed);
                if (oldestReadIndex >= 0)
                    recipientBox.RemoveAt(oldestReadIndex);
                else
                    break; // 삭제할 수 있는 메일 없으면 중단
            }

            if (recipientBox.Count >= maxMailboxSize) return; // 여전히 가득 차면 포기

            var mail = new MailData
            {
                mailId = nextMailId++,
                mailType = mailType,
                senderName = "시스템",
                senderClientId = ulong.MaxValue,
                recipientClientId = recipientClientId,
                subject = subject,
                body = body,
                attachment = attachment,
                hasAttachment = hasAttachment,
                isRead = false,
                attachmentClaimed = !hasAttachment, // 첨부 없으면 이미 수령한 것으로
                sentTime = Time.time,
                expirationTime = Time.time + mailExpirationDuration
            };

            recipientBox.Add(mail);
            NotifyNewMailClientRpc(mail, recipientClientId);
        }

        /// <summary>
        /// 보상 우편 발송 (골드 + 아이템)
        /// </summary>
        public void SendRewardMail(ulong recipientClientId, string subject, string body, int gold,
            string itemId = null, int itemQuantity = 0)
        {
            var attachment = new MailAttachment
            {
                gold = gold,
                itemId = string.IsNullOrEmpty(itemId) ? default : itemId,
                quantity = itemQuantity,
                enhanceLevel = 0
            };

            bool hasAttachment = gold > 0 || !string.IsNullOrEmpty(itemId);
            SendSystemMail(recipientClientId, subject, body, MailType.SystemReward, attachment, hasAttachment);
        }

        #endregion

        #region 우편 조작

        /// <summary>
        /// 메일 읽기
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ReadMailServerRpc(int mailId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var mailbox = GetOrCreateMailbox(clientId);

            int index = mailbox.FindIndex(m => m.mailId == mailId);
            if (index < 0) return;

            var mail = mailbox[index];
            if (!mail.isRead)
            {
                mail.isRead = true;
                mailbox[index] = mail;
            }

            MailReadClientRpc(mailId, clientId);
        }

        /// <summary>
        /// 첨부 아이템/골드 수령
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ClaimAttachmentServerRpc(int mailId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var mailbox = GetOrCreateMailbox(clientId);

            int index = mailbox.FindIndex(m => m.mailId == mailId);
            if (index < 0)
            {
                NotifyMailResultClientRpc("해당 우편을 찾을 수 없습니다.", false, clientId);
                return;
            }

            var mail = mailbox[index];
            if (!mail.hasAttachment)
            {
                NotifyMailResultClientRpc("첨부물이 없는 우편입니다.", false, clientId);
                return;
            }

            if (mail.attachmentClaimed)
            {
                NotifyMailResultClientRpc("이미 수령한 첨부물입니다.", false, clientId);
                return;
            }

            // 골드 지급
            if (mail.attachment.gold > 0)
            {
                var statsData = GetPlayerStatsData(clientId);
                if (statsData != null)
                    statsData.ChangeGold(mail.attachment.gold);
            }

            // 아이템 지급
            if (!string.IsNullOrEmpty(mail.attachment.itemId) && mail.attachment.quantity > 0)
            {
                var inventory = GetInventoryManager(clientId);
                if (inventory != null)
                {
                    // 아이템 추가 시도
                    string itemIdStr = mail.attachment.itemId;
                    for (int i = 0; i < mail.attachment.quantity; i++)
                    {
                        var itemInst = new ItemInstance();
                        itemInst.Initialize(itemIdStr, 1);
                        inventory.AddItem(itemInst);
                    }
                }
            }

            // 수령 완료 표시
            mail.attachmentClaimed = true;
            mail.isRead = true;
            mailbox[index] = mail;

            AttachmentClaimedClientRpc(mailId, mail.attachment, clientId);
        }

        /// <summary>
        /// 메일 삭제
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void DeleteMailServerRpc(int mailId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var mailbox = GetOrCreateMailbox(clientId);

            int index = mailbox.FindIndex(m => m.mailId == mailId);
            if (index < 0) return;

            var mail = mailbox[index];

            // 미수령 첨부물이 있으면 삭제 불가
            if (mail.hasAttachment && !mail.attachmentClaimed)
            {
                NotifyMailResultClientRpc("첨부물을 먼저 수령해주세요.", false, clientId);
                return;
            }

            mailbox.RemoveAt(index);
            MailDeletedClientRpc(mailId, clientId);
        }

        /// <summary>
        /// 읽은 메일 전체 삭제
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void DeleteAllReadMailsServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var mailbox = GetOrCreateMailbox(clientId);

            var deletedIds = new List<int>();
            for (int i = mailbox.Count - 1; i >= 0; i--)
            {
                var mail = mailbox[i];
                if (mail.isRead && (!mail.hasAttachment || mail.attachmentClaimed))
                {
                    deletedIds.Add(mail.mailId);
                    mailbox.RemoveAt(i);
                }
            }

            foreach (int id in deletedIds)
                MailDeletedClientRpc(id, clientId);
        }

        /// <summary>
        /// 모든 첨부물 일괄 수령
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ClaimAllAttachmentsServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var mailbox = GetOrCreateMailbox(clientId);

            int claimedCount = 0;
            int totalGold = 0;

            for (int i = 0; i < mailbox.Count; i++)
            {
                var mail = mailbox[i];
                if (!mail.hasAttachment || mail.attachmentClaimed) continue;

                // 골드 지급
                if (mail.attachment.gold > 0)
                    totalGold += mail.attachment.gold;

                // 아이템 지급
                if (!string.IsNullOrEmpty(mail.attachment.itemId) && mail.attachment.quantity > 0)
                {
                    var inventory = GetInventoryManager(clientId);
                    if (inventory != null)
                    {
                        string itemIdStr = mail.attachment.itemId;
                        for (int q = 0; q < mail.attachment.quantity; q++)
                        {
                            var itemInst = new ItemInstance();
                            itemInst.Initialize(itemIdStr, 1);
                            inventory.AddItem(itemInst);
                        }
                    }
                }

                mail.attachmentClaimed = true;
                mail.isRead = true;
                mailbox[i] = mail;
                claimedCount++;

                AttachmentClaimedClientRpc(mail.mailId, mail.attachment, clientId);
            }

            // 총 골드 한번에 지급
            if (totalGold > 0)
            {
                var statsData = GetPlayerStatsData(clientId);
                if (statsData != null)
                    statsData.ChangeGold(totalGold);
            }

            if (claimedCount > 0)
                NotifyMailResultClientRpc($"{claimedCount}개 첨부물을 수령했습니다.", true, clientId);
            else
                NotifyMailResultClientRpc("수령할 첨부물이 없습니다.", false, clientId);
        }

        /// <summary>
        /// 메일함 동기화 요청
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestMailboxSyncServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var mailbox = GetOrCreateMailbox(clientId);

            // 최신 50개 메일 전송
            int count = Mathf.Min(mailbox.Count, maxMailboxSize);
            for (int i = mailbox.Count - count; i < mailbox.Count; i++)
            {
                SyncMailClientRpc(mailbox[i], clientId);
            }
            SyncMailboxCompleteClientRpc(count, clientId);
        }

        #endregion

        #region 만료 처리

        private void ProcessExpiredMails()
        {
            float now = Time.time;

            foreach (var kvp in playerMailboxes)
            {
                var mailbox = kvp.Value;
                for (int i = mailbox.Count - 1; i >= 0; i--)
                {
                    var mail = mailbox[i];
                    if (mail.expirationTime > 0 && now > mail.expirationTime)
                    {
                        // 미수령 첨부물 반환 (플레이어 메일의 경우 발신자에게)
                        if (mail.hasAttachment && !mail.attachmentClaimed && mail.mailType == MailType.PlayerMail)
                        {
                            ReturnAttachmentToSender(mail);
                        }

                        mailbox.RemoveAt(i);
                        MailDeletedClientRpc(mail.mailId, kvp.Key);
                    }
                }
            }
        }

        private void ReturnAttachmentToSender(MailData mail)
        {
            if (mail.senderClientId == ulong.MaxValue) return; // 시스템 메일은 반환 불필요

            string subject = "반송 우편";
            string body = $"'{mail.subject}' 우편이 만료되어 첨부물이 반송됩니다.";
            SendSystemMail(mail.senderClientId, subject, body, MailType.SystemReward,
                mail.attachment, true);
        }

        #endregion

        #region ClientRPCs

        [ClientRpc]
        private void NotifyNewMailClientRpc(MailData mail, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            localMailbox.Add(mail);
            OnMailReceived?.Invoke(mail);
            OnMailboxUpdated?.Invoke();

            var notif = NotificationManager.Instance;
            if (notif != null)
            {
                string senderStr = mail.senderName;
                notif.ShowNotification($"새 우편: {senderStr}님으로부터", NotificationType.System);
            }
        }

        [ClientRpc]
        private void MailReadClientRpc(int mailId, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            int index = localMailbox.FindIndex(m => m.mailId == mailId);
            if (index >= 0)
            {
                var mail = localMailbox[index];
                mail.isRead = true;
                localMailbox[index] = mail;
            }

            OnMailRead?.Invoke(mailId);
            OnMailboxUpdated?.Invoke();
        }

        [ClientRpc]
        private void AttachmentClaimedClientRpc(int mailId, MailAttachment attachment, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            int index = localMailbox.FindIndex(m => m.mailId == mailId);
            if (index >= 0)
            {
                var mail = localMailbox[index];
                mail.attachmentClaimed = true;
                mail.isRead = true;
                localMailbox[index] = mail;
            }

            OnAttachmentClaimed?.Invoke(mailId);
            OnMailboxUpdated?.Invoke();

            var notif = NotificationManager.Instance;
            if (notif != null)
            {
                string msg = "";
                if (attachment.gold > 0)
                    msg += $"{attachment.gold}G ";
                if (!string.IsNullOrEmpty(attachment.itemId) && attachment.quantity > 0)
                    msg += $"아이템 {attachment.quantity}개 ";
                if (!string.IsNullOrEmpty(msg))
                    notif.ShowNotification($"수령: {msg.Trim()}", NotificationType.System);
            }
        }

        [ClientRpc]
        private void MailDeletedClientRpc(int mailId, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            localMailbox.RemoveAll(m => m.mailId == mailId);
            OnMailDeleted?.Invoke(mailId);
            OnMailboxUpdated?.Invoke();
        }

        [ClientRpc]
        private void NotifyMailResultClientRpc(string message, bool success, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification(message, success ? NotificationType.System : NotificationType.Warning);
        }

        [ClientRpc]
        private void SyncMailClientRpc(MailData mail, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            // 중복 방지
            if (!localMailbox.Any(m => m.mailId == mail.mailId))
                localMailbox.Add(mail);
        }

        [ClientRpc]
        private void SyncMailboxCompleteClientRpc(int totalCount, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            OnMailboxUpdated?.Invoke();
        }

        #endregion

        #region Utility

        private List<MailData> GetOrCreateMailbox(ulong clientId)
        {
            if (!playerMailboxes.ContainsKey(clientId))
                playerMailboxes[clientId] = new List<MailData>();
            return playerMailboxes[clientId];
        }

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        private InventoryManager GetInventoryManager(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject?.GetComponent<InventoryManager>();
        }

        private string GetPlayerName(ulong clientId)
        {
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return $"Player_{clientId}";
            return statsData.CharacterName;
        }

        private ulong FindPlayerByName(string playerName)
        {
            foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            {
                var stats = kvp.Value.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
                if (stats != null && stats.CharacterName == playerName)
                    return kvp.Key;
            }
            return ulong.MaxValue;
        }

        #endregion

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }
}
