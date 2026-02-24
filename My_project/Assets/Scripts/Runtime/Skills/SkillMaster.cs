using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 스킬 마스터 NPC - 골드로 스킬을 가르쳐주는 NPC
    /// 마을에서만 스킬 학습 가능
    /// </summary>
    public class SkillMaster : NetworkBehaviour
    {
        [Header("NPC Settings")]
        [SerializeField] private string masterName = "스킬 마스터";
        [SerializeField] private Race supportedRace = Race.Human;
        [SerializeField] private float interactionRange = 2f;
        [SerializeField] private bool enableInteraction = true;
        
        [Header("Visual Settings")]
        [SerializeField] private GameObject interactionIndicator;
        [SerializeField] private SpriteRenderer npcRenderer;
        [SerializeField] private Sprite masterSprite;
        
        [Header("Skill Master Type")]
        [SerializeField] private SkillMasterType masterType = SkillMasterType.General;
        [SerializeField] private SkillCategory[] teachableCategories;
        
        // 상호작용 상태
        private Dictionary<ulong, bool> playerInteractions = new Dictionary<ulong, bool>();

        // GC 최적화: 재사용 버퍼
        private static readonly Collider2D[] s_OverlapBuffer = new Collider2D[8];
        private PlayerController currentInteractingPlayer;
        
        // UI 관련 (추후 UI 시스템에서 구현)
        public System.Action<PlayerController, SkillMaster> OnPlayerInteract;
        public System.Action<PlayerController> OnPlayerLeaveInteraction;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            SetupVisuals();
            
            // 서버에서만 상호작용 관리
            if (IsServer)
            {
                SetupInteractionArea();
            }
        }
        
        private void Update()
        {
            if (!IsServer || !enableInteraction) return;
            
            CheckPlayerInteractions();
        }
        
        /// <summary>
        /// 비주얼 설정
        /// </summary>
        private void SetupVisuals()
        {
            if (npcRenderer == null)
            {
                npcRenderer = GetComponent<SpriteRenderer>();
                if (npcRenderer == null)
                {
                    var rendererObj = new GameObject("NPC_Renderer");
                    rendererObj.transform.SetParent(transform);
                    rendererObj.transform.localPosition = Vector3.zero;
                    npcRenderer = rendererObj.AddComponent<SpriteRenderer>();
                }
            }
            
            if (masterSprite != null)
            {
                npcRenderer.sprite = masterSprite;
            }
            else
            {
                // 기본 스프라이트 생성 (단색 사각형)
                CreateDefaultSprite();
            }
            
            npcRenderer.sortingLayerName = "Characters";
            npcRenderer.sortingOrder = 1;
            
            // 상호작용 인디케이터 설정
            SetupInteractionIndicator();
        }
        
        /// <summary>
        /// 기본 스프라이트 생성
        /// </summary>
        private void CreateDefaultSprite()
        {
            Texture2D texture = new Texture2D(32, 48);
            Color masterColor = GetMasterColor();
            
            for (int y = 0; y < 48; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    texture.SetPixel(x, y, masterColor);
                }
            }
            
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 32, 48), new Vector2(0.5f, 0f));
            npcRenderer.sprite = sprite;
        }
        
        /// <summary>
        /// 마스터 타입별 색상
        /// </summary>
        private Color GetMasterColor()
        {
            return supportedRace switch
            {
                Race.Human => Color.blue,
                Race.Elf => Color.green,
                Race.Beast => Color.red,
                Race.Machina => Color.gray,
                _ => Color.white
            };
        }
        
        /// <summary>
        /// 상호작용 인디케이터 설정
        /// </summary>
        private void SetupInteractionIndicator()
        {
            if (interactionIndicator == null)
            {
                var indicatorObj = new GameObject("InteractionIndicator");
                indicatorObj.transform.SetParent(transform);
                indicatorObj.transform.localPosition = Vector3.up * 2f;
                
                var renderer = indicatorObj.AddComponent<SpriteRenderer>();
                renderer.sprite = CreateIndicatorSprite();
                renderer.sortingLayerName = "UI";
                renderer.sortingOrder = 10;
                
                interactionIndicator = indicatorObj;
            }
            
            interactionIndicator.SetActive(false);
        }
        
        /// <summary>
        /// 인디케이터 스프라이트 생성
        /// </summary>
        private Sprite CreateIndicatorSprite()
        {
            Texture2D texture = new Texture2D(16, 16);
            
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(8, 8));
                    Color color = distance < 6 ? Color.yellow : Color.clear;
                    texture.SetPixel(x, y, color);
                }
            }
            
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        }
        
        /// <summary>
        /// 상호작용 영역 설정
        /// </summary>
        private void SetupInteractionArea()
        {
            var collider = GetComponent<Collider2D>();
            if (collider == null)
            {
                var circleCollider = gameObject.AddComponent<CircleCollider2D>();
                circleCollider.isTrigger = true;
                circleCollider.radius = interactionRange;
            }
        }
        
        /// <summary>
        /// 플레이어 상호작용 체크
        /// </summary>
        private void CheckPlayerInteractions()
        {
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, interactionRange, s_OverlapBuffer);
            var currentPlayerIds = new HashSet<ulong>();

            for (int i = 0; i < count; i++)
            {
                var player = s_OverlapBuffer[i].GetComponent<PlayerController>();
                if (player != null && player.IsOwner)
                {
                    var playerId = player.OwnerClientId;
                    currentPlayerIds.Add(playerId);

                    // 새로운 플레이어 상호작용
                    if (!playerInteractions.ContainsKey(playerId))
                    {
                        OnPlayerEnterRange(player);
                    }

                    playerInteractions[playerId] = true;
                }
            }
            
            // 범위를 벗어난 플레이어들 처리
            var playersToRemove = new List<ulong>();
            foreach (var kvp in playerInteractions)
            {
                if (!currentPlayerIds.Contains(kvp.Key))
                {
                    playersToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var playerId in playersToRemove)
            {
                OnPlayerLeaveRange(playerId);
                playerInteractions.Remove(playerId);
            }
        }
        
        /// <summary>
        /// 플레이어가 범위에 들어옴
        /// </summary>
        private void OnPlayerEnterRange(PlayerController player)
        {
            var statsManager = player.GetComponent<PlayerStatsManager>();
            if (statsManager?.CurrentStats == null) return;
            
            // 종족 확인
            if (statsManager.CurrentStats.CharacterRace != supportedRace)
            {
                ShowMessageClientRpc(player.OwnerClientId, $"저는 {GetRaceName(supportedRace)} 전용 스킬 마스터입니다.");
                return;
            }
            
            currentInteractingPlayer = player;
            
            // 상호작용 인디케이터 표시
            ShowInteractionIndicatorClientRpc(player.OwnerClientId, true);
            
            // 환영 메시지
            ShowMessageClientRpc(player.OwnerClientId, $"안녕하세요! {masterName}입니다. 스킬을 배우고 싶으시면 말씀하세요.");
            
            // UI 이벤트 발생
            OnPlayerInteract?.Invoke(player, this);
            
            Debug.Log($"Player {player.name} can interact with {masterName}");
        }
        
        /// <summary>
        /// 플레이어가 범위를 벗어남
        /// </summary>
        private void OnPlayerLeaveRange(ulong playerId)
        {
            // 상호작용 인디케이터 숨김
            ShowInteractionIndicatorClientRpc(playerId, false);
            
            // 작별 메시지
            ShowMessageClientRpc(playerId, "또 오세요!");
            
            if (currentInteractingPlayer != null && currentInteractingPlayer.OwnerClientId == playerId)
            {
                OnPlayerLeaveInteraction?.Invoke(currentInteractingPlayer);
                currentInteractingPlayer = null;
            }
        }
        
        /// <summary>
        /// 스킬 학습 처리
        /// </summary>
        public bool TeachSkill(PlayerController player, string skillId)
        {
            if (!IsServer || currentInteractingPlayer != player) return false;
            
            var skillManager = player.GetComponent<SkillManager>();
            var statsManager = player.GetComponent<PlayerStatsManager>();
            
            if (skillManager == null || statsManager == null) return false;
            
            // 스킬 데이터 확인
            var skillData = skillManager.GetSkillData(skillId);
            if (skillData == null)
            {
                ShowMessageClientRpc(player.OwnerClientId, "그런 스킬은 모르겠습니다.");
                return false;
            }
            
            // 종족 확인
            if (skillData.requiredRace != supportedRace)
            {
                ShowMessageClientRpc(player.OwnerClientId, "저는 그 스킬을 가르칠 수 없습니다.");
                return false;
            }
            
            // 카테고리 확인 (특정 마스터인 경우)
            if (teachableCategories != null && teachableCategories.Length > 0)
            {
                if (!teachableCategories.Contains(skillData.category))
                {
                    ShowMessageClientRpc(player.OwnerClientId, "저는 그 계열의 스킬을 가르치지 않습니다.");
                    return false;
                }
            }
            
            // 스킬 학습 시도
            bool success = skillManager.LearnSkill(skillId);
            
            if (success)
            {
                ShowMessageClientRpc(player.OwnerClientId, $"{skillData.skillName}을(를) 배우셨습니다! 잘 활용하세요.");
            }
            else
            {
                // 실패 이유 확인
                string reason = GetLearnFailureReason(skillData, statsManager.CurrentStats, skillManager.GetLearnedSkills());
                ShowMessageClientRpc(player.OwnerClientId, reason);
            }
            
            return success;
        }
        
        /// <summary>
        /// 학습 실패 이유 메시지
        /// </summary>
        private string GetLearnFailureReason(SkillData skillData, PlayerStatsData playerStats, IReadOnlyList<string> learnedSkills)
        {
            if (playerStats.CurrentLevel < skillData.requiredLevel)
            {
                return $"{skillData.skillName}을(를) 배우려면 {skillData.requiredLevel}레벨이 필요합니다.";
            }
            
            if (playerStats.Gold < skillData.goldCost)
            {
                return $"{skillData.skillName}을(를) 배우려면 {skillData.goldCost} 골드가 필요합니다.";
            }
            
            if (learnedSkills.Contains(skillData.skillId))
            {
                return "이미 배운 스킬입니다.";
            }
            
            if (skillData.prerequisiteSkills != null && skillData.prerequisiteSkills.Length > 0)
            {
                foreach (var prereq in skillData.prerequisiteSkills)
                {
                    if (prereq != null && !learnedSkills.Contains(prereq.skillId))
                    {
                        return $"{skillData.skillName}을(를) 배우려면 먼저 {prereq.skillName}을(를) 배워야 합니다.";
                    }
                }
            }
            
            return "스킬을 배울 수 없습니다.";
        }
        
        /// <summary>
        /// 학습 가능한 스킬 목록 가져오기
        /// </summary>
        public List<SkillData> GetTeachableSkills(PlayerController player)
        {
            var skillManager = player.GetComponent<SkillManager>();
            if (skillManager == null) return new List<SkillData>();
            
            var learnableSkills = skillManager.GetLearnableSkills();
            
            // 지원하는 종족 필터링
            learnableSkills = learnableSkills.Where(s => s.requiredRace == supportedRace).ToList();
            
            // 카테고리 필터링 (특정 마스터인 경우)
            if (teachableCategories != null && teachableCategories.Length > 0)
            {
                learnableSkills = learnableSkills.Where(s => teachableCategories.Contains(s.category)).ToList();
            }
            
            return learnableSkills;
        }
        
        /// <summary>
        /// 종족 이름 가져오기
        /// </summary>
        private string GetRaceName(Race race)
        {
            return race switch
            {
                Race.Human => "인간",
                Race.Elf => "엘프",
                Race.Beast => "수인",
                Race.Machina => "기계족",
                _ => "알 수 없음"
            };
        }
        
        /// <summary>
        /// 상호작용 인디케이터 표시/숨김
        /// </summary>
        [ClientRpc]
        private void ShowInteractionIndicatorClientRpc(ulong targetClientId, bool show)
        {
            if (NetworkManager.Singleton.LocalClientId == targetClientId)
            {
                if (interactionIndicator != null)
                {
                    interactionIndicator.SetActive(show);
                }
            }
        }
        
        /// <summary>
        /// 메시지 표시
        /// </summary>
        [ClientRpc]
        private void ShowMessageClientRpc(ulong targetClientId, string message)
        {
            if (NetworkManager.Singleton.LocalClientId == targetClientId)
            {
                Debug.Log($"[{masterName}] {message}");
                // 추후 UI 시스템에서 말풍선이나 채팅으로 표시
            }
        }
        
        /// <summary>
        /// 기즈모 그리기 (에디터용)
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // 상호작용 범위 시각화
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
            
            // 스킬 마스터 타입 표시
            Gizmos.color = GetMasterColor();
            Gizmos.DrawWireCube(transform.position + Vector3.up * 3f, Vector3.one * 0.5f);
        }
        
        /// <summary>
        /// 마스터 정보 가져오기
        /// </summary>
        public string GetMasterInfo()
        {
            string info = $"이름: {masterName}\n";
            info += $"종족: {GetRaceName(supportedRace)}\n";
            info += $"타입: {masterType}\n";
            
            if (teachableCategories != null && teachableCategories.Length > 0)
            {
                info += "전문 분야: ";
                for (int i = 0; i < teachableCategories.Length; i++)
                {
                    info += teachableCategories[i].ToString();
                    if (i < teachableCategories.Length - 1) info += ", ";
                }
            }
            
            return info;
        }
    }
    
    /// <summary>
    /// 스킬 마스터 타입
    /// </summary>
    public enum SkillMasterType
    {
        General,    // 모든 계열 가능
        Specialist, // 특정 계열만
        Advanced,   // 고급 스킬만
        Master      // 마스터급 스킬만
    }
}