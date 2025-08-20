using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 던전 보물상자 시스템
    /// 플레이어가 열어서 아이템을 획득할 수 있는 보물상자
    /// </summary>
    public class TreasureChest : NetworkBehaviour
    {
        [Header("보물상자 설정")]
        [SerializeField] private ChestType chestType = ChestType.Common;
        [SerializeField] private int itemCount = 1;
        [SerializeField] private bool requiresKey = false;
        [SerializeField] private float openRadius = 2f;
        
        [Header("시각 효과")]
        [SerializeField] private GameObject closedChestPrefab;
        [SerializeField] private GameObject openChestPrefab;
        [SerializeField] private ParticleSystem openEffect;
        [SerializeField] private AudioSource chestAudioSource;
        [SerializeField] private AudioClip openSound;
        
        // 상자 상태
        private NetworkVariable<bool> isOpened = new NetworkVariable<bool>(false);
        private Collider2D chestCollider;
        private DungeonEnvironment dungeonEnvironment;
        
        // 보물상자 데이터
        private int floorLevel = 1;
        private List<ItemInstance> chestContents = new List<ItemInstance>();
        
        // 프로퍼티
        public ChestType ChestType => chestType;
        public bool IsOpened => isOpened.Value;
        public bool RequiresKey => requiresKey;
        
        private void Awake()
        {
            chestCollider = GetComponent<Collider2D>();
            if (chestCollider == null)
            {
                chestCollider = gameObject.AddComponent<BoxCollider2D>();
                chestCollider.isTrigger = true;
            }
            
            if (chestAudioSource == null)
            {
                chestAudioSource = gameObject.AddComponent<AudioSource>();
                chestAudioSource.playOnAwake = false;
            }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            isOpened.OnValueChanged += OnChestStateChanged;
            
            SetupChestVisual();
            GenerateChestContents();
        }
        
        public override void OnNetworkDespawn()
        {
            isOpened.OnValueChanged -= OnChestStateChanged;
            
            base.OnNetworkDespawn();
        }
        
        /// <summary>
        /// 보물상자 초기화
        /// </summary>
        public void Initialize(ChestType type, int floor, DungeonEnvironment environment)
        {
            chestType = type;
            floorLevel = floor;
            dungeonEnvironment = environment;
            
            ConfigureChestByType();
        }
        
        /// <summary>
        /// 상자 타입별 설정
        /// </summary>
        private void ConfigureChestByType()
        {
            switch (chestType)
            {
                case ChestType.Common:
                    itemCount = Random.Range(1, 3);
                    requiresKey = false;
                    break;
                    
                case ChestType.Rare:
                    itemCount = Random.Range(2, 4);
                    requiresKey = false;
                    break;
                    
                case ChestType.Epic:
                    itemCount = Random.Range(3, 5);
                    requiresKey = Random.value < 0.3f; // 30% 확률로 키 필요
                    break;
                    
                case ChestType.Legendary:
                    itemCount = Random.Range(4, 7);
                    requiresKey = Random.value < 0.6f; // 60% 확률로 키 필요
                    break;
            }
        }
        
        /// <summary>
        /// 보물상자 시각적 표현 설정
        /// </summary>
        private void SetupChestVisual()
        {
            if (closedChestPrefab != null && !isOpened.Value)
            {
                var visual = Instantiate(closedChestPrefab, transform);
                visual.transform.localPosition = Vector3.zero;
            }
            
            // 상자 타입별 색상 설정
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = GetChestColor();
            }
        }
        
        /// <summary>
        /// 상자 타입별 색상
        /// </summary>
        private Color GetChestColor()
        {
            return chestType switch
            {
                ChestType.Common => Color.white,
                ChestType.Rare => Color.blue,
                ChestType.Epic => Color.magenta,
                ChestType.Legendary => Color.yellow,
                _ => Color.white
            };
        }
        
        /// <summary>
        /// 상자 내용물 생성
        /// </summary>
        private void GenerateChestContents()
        {
            if (!IsServer) return;
            
            chestContents.Clear();
            
            for (int i = 0; i < itemCount; i++)
            {
                ItemInstance item = GenerateRandomItem();
                if (item != null)
                {
                    chestContents.Add(item);
                }
            }
            
            // 골드 추가
            int goldAmount = CalculateGoldReward();
            if (goldAmount > 0)
            {
                // 골드 아이템 생성 (추후 구현)
                Debug.Log($"💰 Chest contains {goldAmount} gold");
            }
        }
        
        /// <summary>
        /// 랜덤 아이템 생성
        /// </summary>
        private ItemInstance GenerateRandomItem()
        {
            ItemGrade targetGrade = DetermineItemGrade();
            
            // ItemDatabase에서 해당 등급의 아이템 가져오기 (추후 구현)
            // 현재는 더미 아이템 반환
            return null;
        }
        
        /// <summary>
        /// 아이템 등급 결정
        /// </summary>
        private ItemGrade DetermineItemGrade()
        {
            float random = Random.value;
            
            // 상자 타입별 등급 확률 조정
            float legendaryChance = chestType switch
            {
                ChestType.Legendary => 0.2f,
                ChestType.Epic => 0.05f,
                _ => 0.01f
            };
            
            float epicChance = chestType switch
            {
                ChestType.Legendary => 0.4f,
                ChestType.Epic => 0.3f,
                ChestType.Rare => 0.1f,
                _ => 0.02f
            };
            
            float rareChance = chestType switch
            {
                ChestType.Legendary => 0.3f,
                ChestType.Epic => 0.4f,
                ChestType.Rare => 0.6f,
                _ => 0.15f
            };
            
            if (random < legendaryChance)
                return ItemGrade.Legendary;
            if (random < legendaryChance + epicChance)
                return ItemGrade.Epic;
            if (random < legendaryChance + epicChance + rareChance)
                return ItemGrade.Rare;
            
            return ItemGrade.Common;
        }
        
        /// <summary>
        /// 골드 보상 계산
        /// </summary>
        private int CalculateGoldReward()
        {
            int baseGold = chestType switch
            {
                ChestType.Common => Random.Range(10, 50),
                ChestType.Rare => Random.Range(50, 150),
                ChestType.Epic => Random.Range(150, 500),
                ChestType.Legendary => Random.Range(500, 1500),
                _ => 0
            };
            
            return (int)(baseGold * (1f + floorLevel * 0.2f));
        }
        
        private void OnTriggerStay2D(Collider2D other)
        {
            if (isOpened.Value) return;
            
            var playerController = other.GetComponent<PlayerController>();
            if (playerController == null) return;
            
            // F키 입력 체크 (클라이언트에서 처리 후 ServerRpc 호출)
            if (Input.GetKeyDown(KeyCode.F) && NetworkManager.Singleton.LocalClientId == playerController.NetworkObjectId)
            {
                TryOpenChestServerRpc(playerController.NetworkObjectId);
            }
        }
        
        /// <summary>
        /// 보물상자 열기 시도
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void TryOpenChestServerRpc(ulong playerId)
        {
            if (!IsServer || isOpened.Value) return;
            
            var playerObject = NetworkManager.Singleton.ConnectedClients[playerId].PlayerObject;
            if (playerObject == null) return;
            
            var playerController = playerObject.GetComponent<PlayerController>();
            if (playerController == null) return;
            
            // 거리 체크
            float distance = Vector3.Distance(transform.position, playerController.transform.position);
            if (distance > openRadius) return;
            
            // 키 필요 여부 체크
            if (requiresKey)
            {
                if (!HasRequiredKey(playerController))
                {
                    ShowKeyRequiredMessageClientRpc(playerId);
                    return;
                }
            }
            
            OpenChest(playerController);
        }
        
        /// <summary>
        /// 필요한 키 보유 여부 체크
        /// </summary>
        private bool HasRequiredKey(PlayerController player)
        {
            // 인벤토리에서 키 아이템 확인 (추후 구현)
            return true; // 임시로 항상 true 반환
        }
        
        /// <summary>
        /// 보물상자 열기
        /// </summary>
        private void OpenChest(PlayerController player)
        {
            if (!IsServer) return;
            
            isOpened.Value = true;
            
            // 보상 지급
            GiveRewards(player);
            
            // 환경 매니저에 알림
            dungeonEnvironment?.OnChestOpenedByPlayer(this, player);
            
            // 클라이언트에 열림 효과 표시
            ShowChestOpenEffectClientRpc();
        }
        
        /// <summary>
        /// 보상 지급
        /// </summary>
        public void GiveRewards(PlayerController player)
        {
            if (!IsServer) return;
            
            var inventoryManager = player.GetComponent<InventoryManager>();
            if (inventoryManager == null) return;
            
            // 아이템 지급
            foreach (var item in chestContents)
            {
                if (item != null)
                {
                    inventoryManager.AddItem(item);
                }
            }
            
            // 골드 지급
            int goldReward = CalculateGoldReward();
            var statsManager = player.GetComponent<PlayerStatsManager>();
            if (statsManager != null)
            {
                statsManager.AddGold(goldReward);
            }
            
            // 클라이언트에 보상 알림
            ShowRewardMessageClientRpc(player.NetworkObjectId, chestContents.Count, goldReward);
        }
        
        /// <summary>
        /// 상자 상태 변경 이벤트
        /// </summary>
        private void OnChestStateChanged(bool previousValue, bool newValue)
        {
            if (newValue)
            {
                PlayOpenEffect();
                UpdateChestVisual();
            }
        }
        
        /// <summary>
        /// 열림 효과 재생
        /// </summary>
        private void PlayOpenEffect()
        {
            if (openEffect != null)
            {
                openEffect.Play();
            }
            
            if (chestAudioSource != null && openSound != null)
            {
                chestAudioSource.PlayOneShot(openSound);
            }
        }
        
        /// <summary>
        /// 상자 시각 업데이트
        /// </summary>
        private void UpdateChestVisual()
        {
            // 닫힌 상자 모델 제거
            Transform closedModel = transform.Find("ClosedChest");
            if (closedModel != null)
            {
                Destroy(closedModel.gameObject);
            }
            
            // 열린 상자 모델 생성
            if (openChestPrefab != null)
            {
                var openModel = Instantiate(openChestPrefab, transform);
                openModel.transform.localPosition = Vector3.zero;
            }
            
            // 콜라이더 비활성화
            chestCollider.enabled = false;
        }
        
        // ClientRpc 메서드들
        [ClientRpc]
        private void ShowKeyRequiredMessageClientRpc(ulong targetPlayerId)
        {
            if (NetworkManager.Singleton.LocalClientId == targetPlayerId)
            {
                Debug.Log("🔑 이 보물상자를 열려면 키가 필요합니다!");
            }
        }
        
        [ClientRpc]
        private void ShowChestOpenEffectClientRpc()
        {
            PlayOpenEffect();
            UpdateChestVisual();
        }
        
        [ClientRpc]
        private void ShowRewardMessageClientRpc(ulong targetPlayerId, int itemCount, int goldAmount)
        {
            if (NetworkManager.Singleton.LocalClientId == targetPlayerId)
            {
                string chestTypeName = chestType switch
                {
                    ChestType.Common => "일반",
                    ChestType.Rare => "희귀",
                    ChestType.Epic => "영웅",
                    ChestType.Legendary => "전설",
                    _ => "신비"
                };
                
                Debug.Log($"💰 {chestTypeName} 보물상자를 열었습니다! 아이템 {itemCount}개, 골드 {goldAmount} 획득!");
            }
        }
    }
    
    /// <summary>
    /// 보물상자 타입 열거형
    /// </summary>
    public enum ChestType
    {
        Common,     // 일반 상자
        Rare,       // 희귀 상자
        Epic,       // 영웅 상자
        Legendary   // 전설 상자
    }
}