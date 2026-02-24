using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 대화 시스템 - 클라이언트 로컬 (UI 표시)
    /// NPC별 조건부 대화 선택, 분기 대화 진행
    /// </summary>
    public class DialogueSystem : MonoBehaviour
    {
        public static DialogueSystem Instance { get; private set; }

        // NPC ID → 대화 목록 (우선순위 정렬)
        private Dictionary<string, List<DialogueData>> npcDialogues = new Dictionary<string, List<DialogueData>>();
        // Dialogue ID → DialogueData 캐시 (Resources.LoadAll 반복 방지)
        private Dictionary<string, DialogueData> dialogueByIdCache = new Dictionary<string, DialogueData>();

        // 현재 대화 상태
        private DialogueData currentDialogue;
        private DialogueNode currentNode;
        private bool isInDialogue;

        // 대화 플래그 (진행도 기록)
        private HashSet<string> dialogueFlags = new HashSet<string>();

        // 이벤트
        public System.Action<DialogueNode> OnNodeDisplayed;
        public System.Action OnDialogueStarted;
        public System.Action OnDialogueEnded;
        public System.Action<DialogueNodeEffect> OnEffectTriggered;

        // 접근자
        public bool IsInDialogue => isInDialogue;
        public DialogueData CurrentDialogue => currentDialogue;
        public DialogueNode CurrentNode => currentNode;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            LoadAllDialogues();
        }

        private void LoadAllDialogues()
        {
            var allDialogues = Resources.LoadAll<DialogueData>("ScriptableObjects/Dialogues");
            foreach (var dialogue in allDialogues)
            {
                if (string.IsNullOrEmpty(dialogue.DialogueId)) continue;
                var parts = dialogue.DialogueId.Split('_');
                string npcId = parts.Length > 0 ? parts[0] : dialogue.DialogueId;
                if (!npcDialogues.ContainsKey(npcId))
                    npcDialogues[npcId] = new List<DialogueData>();
                npcDialogues[npcId].Add(dialogue);
                dialogueByIdCache[dialogue.DialogueId] = dialogue;
            }

            // 우선순위 정렬
            foreach (var kvp in npcDialogues)
                kvp.Value.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            Debug.Log($"[Dialogue] {allDialogues.Length}개 대화 데이터 로드됨 ({npcDialogues.Count} NPC)");
        }

        /// <summary>
        /// NPC와 대화 시작
        /// </summary>
        public bool StartDialogue(string npcId)
        {
            if (isInDialogue) return false;

            var dialogue = FindBestDialogue(npcId);
            if (dialogue == null || dialogue.Nodes.Count == 0)
            {
                Debug.Log($"[Dialogue] NPC '{npcId}'에 대한 대화를 찾을 수 없습니다.");
                return false;
            }

            currentDialogue = dialogue;
            currentNode = dialogue.Nodes[0];
            isInDialogue = true;

            OnDialogueStarted?.Invoke();
            OnNodeDisplayed?.Invoke(currentNode);

            return true;
        }

        /// <summary>
        /// 특정 대화 ID로 대화 시작
        /// </summary>
        public bool StartDialogueById(string dialogueId)
        {
            dialogueByIdCache.TryGetValue(dialogueId, out var dialogue);
            if (dialogue == null || dialogue.Nodes.Count == 0) return false;

            currentDialogue = dialogue;
            currentNode = dialogue.Nodes[0];
            isInDialogue = true;

            OnDialogueStarted?.Invoke();
            OnNodeDisplayed?.Invoke(currentNode);
            return true;
        }

        /// <summary>
        /// 다음 노드로 진행 (선택지 없을 때)
        /// </summary>
        public void AdvanceDialogue()
        {
            if (!isInDialogue || currentNode == null) return;

            // 현재 노드 효과 실행
            if (currentNode.effect != null && currentNode.effect.effectType != DialogueEffectType.None)
                ExecuteEffect(currentNode.effect);

            // 다음 노드
            if (string.IsNullOrEmpty(currentNode.nextNodeId))
            {
                EndDialogue();
                return;
            }

            var nextNode = currentDialogue.Nodes.Find(n => n.nodeId == currentNode.nextNodeId);
            if (nextNode == null)
            {
                EndDialogue();
                return;
            }

            currentNode = nextNode;
            OnNodeDisplayed?.Invoke(currentNode);
        }

        /// <summary>
        /// 선택지 선택
        /// </summary>
        public void SelectChoice(int choiceIndex)
        {
            if (!isInDialogue || currentNode == null) return;
            if (currentNode.choices == null || choiceIndex >= currentNode.choices.Count) return;

            var choice = currentNode.choices[choiceIndex];

            // 선택 효과 실행
            if (choice.effect != null && choice.effect.effectType != DialogueEffectType.None)
                ExecuteEffect(choice.effect);

            // 다음 노드
            if (string.IsNullOrEmpty(choice.nextNodeId))
            {
                EndDialogue();
                return;
            }

            var nextNode = currentDialogue.Nodes.Find(n => n.nodeId == choice.nextNodeId);
            if (nextNode == null)
            {
                EndDialogue();
                return;
            }

            currentNode = nextNode;
            OnNodeDisplayed?.Invoke(currentNode);
        }

        /// <summary>
        /// 대화 종료
        /// </summary>
        public void EndDialogue()
        {
            isInDialogue = false;
            currentDialogue = null;
            currentNode = null;
            OnDialogueEnded?.Invoke();
        }

        /// <summary>
        /// 대화 강제 스킵
        /// </summary>
        public void SkipDialogue()
        {
            if (!isInDialogue) return;
            EndDialogue();
        }

        #region 조건 확인

        private DialogueData FindBestDialogue(string npcId)
        {
            if (!npcDialogues.ContainsKey(npcId)) return null;

            foreach (var dialogue in npcDialogues[npcId])
            {
                if (CheckCondition(dialogue.Condition))
                    return dialogue;
            }

            return null;
        }

        private bool CheckCondition(DialogueCondition condition)
        {
            if (condition == null || condition.conditionType == DialogueConditionType.None)
                return true;

            // 로컬 플레이어 정보 기반 체크
            var localPlayer = Unity.Netcode.NetworkManager.Singleton?.LocalClient?.PlayerObject;
            if (localPlayer == null) return true;

            var statsData = localPlayer.GetComponent<PlayerStatsManager>()?.CurrentStats;

            switch (condition.conditionType)
            {
                case DialogueConditionType.MinLevel:
                    return statsData != null && statsData.CurrentLevel >= condition.requiredValue;

                case DialogueConditionType.MaxLevel:
                    return statsData != null && statsData.CurrentLevel <= condition.requiredValue;

                case DialogueConditionType.GoldMin:
                    return statsData != null && statsData.Gold >= condition.requiredValue;

                case DialogueConditionType.RaceIs:
                    return statsData != null && statsData.CharacterRace == condition.requiredRace;

                case DialogueConditionType.HasQuest:
                    return QuestManager.Instance != null &&
                        QuestManager.Instance.GetQuestProgress(condition.requiredStringValue) != null;

                case DialogueConditionType.QuestComplete:
                    {
                        var progress = QuestManager.Instance?.GetQuestProgress(condition.requiredStringValue);
                        return progress != null && (progress.status == QuestStatus.Completed || progress.status == QuestStatus.Rewarded);
                    }

                case DialogueConditionType.HasSpecialization:
                    return JobSpecializationSystem.Instance != null &&
                        JobSpecializationSystem.Instance.HasSpecialization;

                default:
                    return true;
            }
        }

        /// <summary>
        /// 선택지 조건 확인 (UI에서 호출)
        /// </summary>
        public bool IsChoiceAvailable(DialogueChoice choice)
        {
            if (choice.condition == null) return true;
            return CheckCondition(choice.condition);
        }

        #endregion

        #region 효과 실행

        private void ExecuteEffect(DialogueNodeEffect effect)
        {
            OnEffectTriggered?.Invoke(effect);

            switch (effect.effectType)
            {
                case DialogueEffectType.GiveGold:
                    // 서버에 요청 필요 (간소화: 로컬에서 처리)
                    var notif = NotificationManager.Instance;
                    if (notif != null)
                        notif.ShowNotification($"{effect.intValue}G 획득!", NotificationType.System);
                    break;

                case DialogueEffectType.GiveExp:
                    if (NotificationManager.Instance != null)
                        NotificationManager.Instance.ShowNotification(
                            $"경험치 {effect.intValue} 획득!", NotificationType.System);
                    break;

                case DialogueEffectType.AcceptQuest:
                    if (QuestManager.Instance != null)
                        QuestManager.Instance.AcceptQuestServerRpc(effect.stringValue);
                    break;

                case DialogueEffectType.HealPlayer:
                    if (NotificationManager.Instance != null)
                        NotificationManager.Instance.ShowNotification("HP/MP가 회복되었습니다.", NotificationType.System);
                    break;

                case DialogueEffectType.SetFlag:
                    if (!string.IsNullOrEmpty(effect.stringValue))
                        dialogueFlags.Add(effect.stringValue);
                    break;
            }
        }

        #endregion

        #region 플래그

        public bool HasFlag(string flag) => dialogueFlags.Contains(flag);
        public void SetFlag(string flag) => dialogueFlags.Add(flag);
        public void RemoveFlag(string flag) => dialogueFlags.Remove(flag);

        #endregion

        private void OnDestroy()
        {
            if (Instance == this)
            {
                OnNodeDisplayed = null;
                OnDialogueStarted = null;
                OnDialogueEnded = null;
                OnEffectTriggered = null;
                Instance = null;
            }
        }
    }
}
