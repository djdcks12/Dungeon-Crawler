using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 채팅 명령어 처리 시스템
    /// ChatUI의 ProcessCommand에서 호출, 확장 명령어 처리
    /// </summary>
    public class ChatCommandSystem : MonoBehaviour
    {
        public static ChatCommandSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 명령어 처리 시도. 처리 성공 시 true 반환
        /// </summary>
        public bool TryProcessCommand(string fullCommand, ulong senderClientId)
        {
            var parts = fullCommand.Split(' ');
            if (parts.Length == 0) return false;

            string cmd = parts[0].ToLower();

            switch (cmd)
            {
                case "/party":
                case "/p":
                    return HandlePartyCommand(parts, senderClientId);

                case "/invite":
                    return HandleInviteCommand(parts, senderClientId);

                case "/kick":
                    return HandleKickCommand(parts, senderClientId);

                case "/leave":
                    return HandleLeaveCommand(senderClientId);

                case "/trade":
                    return HandleTradeCommand(parts, senderClientId);

                case "/stats":
                    return HandleStatsCommand(senderClientId);

                case "/pos":
                case "/position":
                    return HandlePositionCommand(senderClientId);

                case "/time":
                    return HandleTimeCommand();

                case "/roll":
                    return HandleRollCommand(parts);

                case "/help":
                    return HandleHelpCommand();

                default:
                    return false;
            }
        }

        private bool HandlePartyCommand(string[] parts, ulong clientId)
        {
            if (parts.Length < 2)
            {
                ShowSystemMessage("/party create - 파티 생성\n/party info - 파티 정보\n/invite [이름] - 초대\n/leave - 파티 탈퇴");
                return true;
            }

            string subCmd = parts[1].ToLower();
            var partyManager = FindFirstObjectByType<PartyManager>();
            if (partyManager == null)
            {
                ShowSystemMessage("파티 시스템이 비활성화되어 있습니다.");
                return true;
            }

            switch (subCmd)
            {
                case "create":
                    partyManager.CreatePartyServerRpc("Party", 4, true);
                    ShowSystemMessage("파티 생성 요청...");
                    return true;

                case "info":
                    if (partyManager.HasParty(clientId))
                    {
                        var members = partyManager.GetPlayerPartyMembers(clientId);
                        string info = $"파티 인원: {members.Count}명\n";
                        foreach (var m in members)
                        {
                            string name = GetPlayerName(m.clientId);
                            info += $"  - {name} (ID: {m.clientId})\n";
                        }
                        ShowSystemMessage(info);
                    }
                    else
                    {
                        ShowSystemMessage("파티에 소속되어 있지 않습니다.");
                    }
                    return true;

                default:
                    ShowSystemMessage("알 수 없는 파티 명령어입니다. /party 로 도움말 확인");
                    return true;
            }
        }

        private bool HandleInviteCommand(string[] parts, ulong clientId)
        {
            if (parts.Length < 2)
            {
                ShowSystemMessage("사용법: /invite [플레이어명]");
                return true;
            }

            string targetName = parts[1];
            ulong? targetId = FindPlayerByName(targetName);

            if (targetId == null)
            {
                ShowSystemMessage($"플레이어 '{targetName}'을(를) 찾을 수 없습니다.");
                return true;
            }

            var partyManager = FindFirstObjectByType<PartyManager>();
            if (partyManager == null) return true;

            partyManager.InvitePlayerServerRpc(targetId.Value);
            ShowSystemMessage($"{targetName}에게 파티 초대를 보냈습니다.");
            return true;
        }

        private bool HandleKickCommand(string[] parts, ulong clientId)
        {
            if (parts.Length < 2)
            {
                ShowSystemMessage("사용법: /kick [플레이어명]");
                return true;
            }

            string targetName = parts[1];
            ulong? targetId = FindPlayerByName(targetName);

            if (targetId == null)
            {
                ShowSystemMessage($"플레이어 '{targetName}'을(를) 찾을 수 없습니다.");
                return true;
            }

            var partyManager = FindFirstObjectByType<PartyManager>();
            if (partyManager == null) return true;

            // 파티 추방은 해산 후 재생성으로 처리 (전용 API 없음)
            ShowSystemMessage("파티 추방 기능은 파티장만 /party disband 후 재초대로 처리됩니다.");
            return true;
        }

        private bool HandleLeaveCommand(ulong clientId)
        {
            var partyManager = FindFirstObjectByType<PartyManager>();
            if (partyManager == null) return true;

            if (!partyManager.HasParty(clientId))
            {
                ShowSystemMessage("파티에 소속되어 있지 않습니다.");
                return true;
            }

            partyManager.LeavePartyServerRpc();
            ShowSystemMessage("파티를 떠났습니다.");
            return true;
        }

        private bool HandleTradeCommand(string[] parts, ulong clientId)
        {
            if (parts.Length < 2)
            {
                ShowSystemMessage("사용법: /trade [플레이어명] 또는 /trade accept 또는 /trade cancel");
                return true;
            }

            string subCmd = parts[1].ToLower();

            if (subCmd == "accept")
            {
                if (TradeSystem.Instance != null)
                {
                    TradeSystem.Instance.AcceptTradeServerRpc();
                    ShowSystemMessage("거래를 수락했습니다.");
                }
                return true;
            }

            if (subCmd == "cancel")
            {
                if (TradeSystem.Instance != null)
                {
                    TradeSystem.Instance.CancelTradeServerRpc();
                }
                return true;
            }

            if (subCmd == "confirm")
            {
                if (TradeSystem.Instance != null)
                {
                    TradeSystem.Instance.ConfirmTradeServerRpc();
                    ShowSystemMessage("거래를 확인했습니다.");
                }
                return true;
            }

            // 플레이어 이름으로 거래 요청
            string targetName = parts[1];
            ulong? targetId = FindPlayerByName(targetName);

            if (targetId == null)
            {
                ShowSystemMessage($"플레이어 '{targetName}'을(를) 찾을 수 없습니다.");
                return true;
            }

            if (TradeSystem.Instance != null)
            {
                TradeSystem.Instance.RequestTradeServerRpc(targetId.Value);
                ShowSystemMessage($"{targetName}에게 거래 요청을 보냈습니다.");
            }
            else
            {
                ShowSystemMessage("거래 시스템이 비활성화되어 있습니다.");
            }
            return true;
        }

        private bool HandleStatsCommand(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return true;
            if (client.PlayerObject == null) return true;

            var statsManager = client.PlayerObject.GetComponent<PlayerStatsManager>();
            if (statsManager?.CurrentStats == null) return true;

            var s = statsManager.CurrentStats;
            string info = $"=== {s.CharacterName} ===\n";
            info += $"Lv.{s.CurrentLevel} {s.CharacterRace} {s.CurrentJobType}\n";
            info += $"HP: {s.CurrentHP:F0}/{s.MaxHP:F0} | MP: {s.CurrentMP:F0}/{s.MaxMP:F0}\n";
            info += $"STR:{s.TotalSTR:F0} AGI:{s.TotalAGI:F0} VIT:{s.TotalVIT:F0} INT:{s.TotalINT:F0}\n";
            info += $"DEF:{s.TotalDEF:F0} MDEF:{s.TotalMDEF:F0} LUK:{s.TotalLUK:F0} STAB:{s.TotalSTAB:F0}\n";
            info += $"Gold: {s.Gold:N0}";

            ShowSystemMessage(info);
            return true;
        }

        private bool HandlePositionCommand(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return true;
            if (client.PlayerObject == null) return true;

            var pos = client.PlayerObject.transform.position;
            ShowSystemMessage($"현재 위치: ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})");
            return true;
        }

        private bool HandleTimeCommand()
        {
            ShowSystemMessage($"서버 시간: {System.DateTime.Now:HH:mm:ss}");
            return true;
        }

        private bool HandleRollCommand(string[] parts)
        {
            int max = 100;
            if (parts.Length > 1 && int.TryParse(parts[1], out int customMax))
            {
                max = Mathf.Clamp(customMax, 1, 10000);
            }

            int result = Random.Range(1, max + 1);
            ShowSystemMessage($"주사위 결과: {result} (1-{max})");
            return true;
        }

        private bool HandleHelpCommand()
        {
            string help = "=== 명령어 목록 ===\n";
            help += "/help - 도움말\n";
            help += "/w [이름] [메시지] - 귓속말\n";
            help += "/party create - 파티 생성\n";
            help += "/party info - 파티 정보\n";
            help += "/invite [이름] - 파티 초대\n";
            help += "/kick [이름] - 파티 추방\n";
            help += "/leave - 파티 탈퇴\n";
            help += "/trade [이름] - 거래 요청\n";
            help += "/stats - 내 스탯 확인\n";
            help += "/pos - 현재 위치\n";
            help += "/time - 서버 시간\n";
            help += "/roll [최대값] - 주사위\n";
            help += "/clear - 채팅 초기화";

            ShowSystemMessage(help);
            return true;
        }

        // === 유틸리티 ===

        private void ShowSystemMessage(string message)
        {
            var chatUI = FindFirstObjectByType<ChatUI>();
            if (chatUI != null)
            {
                chatUI.AddSystemMessage(message);
            }
            else
            {
                Debug.Log($"[Chat] {message}");
            }
        }

        private string GetPlayerName(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return $"Player_{clientId}";
            if (client.PlayerObject == null)
                return $"Player_{clientId}";

            var statsManager = client.PlayerObject.GetComponent<PlayerStatsManager>();
            return statsManager?.CurrentStats?.CharacterName ?? $"Player_{clientId}";
        }

        private ulong? FindPlayerByName(string name)
        {
            foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            {
                if (kvp.Value.PlayerObject == null) continue;

                var statsManager = kvp.Value.PlayerObject.GetComponent<PlayerStatsManager>();
                if (statsManager?.CurrentStats?.CharacterName == name)
                    return kvp.Key;
            }
            return null;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
