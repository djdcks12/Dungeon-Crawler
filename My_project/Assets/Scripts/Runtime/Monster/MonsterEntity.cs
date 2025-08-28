using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 통합된 몬스터 엔티티 시스템
    /// 플레이어와 동일한 스탯 계산 및 전투 시스템 사용
    /// </summary>
    public class MonsterEntity : NetworkBehaviour
    {
        [Header("Monster Configuration")]
        [SerializeField] private MonsterRaceData raceData;
        [SerializeField] private MonsterVariantData variantData;
        
        [Header("Runtime Status (Read Only)")]
        [SerializeField] private string currentRaceName = "Not Generated";
        [SerializeField] private string currentVariantName = "Not Generated";
        [SerializeField] private float currentGrade = 0f;
        
        [Header("Generated Properties")]
        [SerializeField] private float grade = 100f; // 80~120 범위
        [SerializeField] private StatBlock finalStats;
        [SerializeField] private List<MonsterSkillInstance> activeSkills = new List<MonsterSkillInstance>();
        
        [Header("Combat Stats")]
        [SerializeField] private CombatStats combatStats;
        
        // 네트워크 동기화 스탯 정보
        private NetworkVariable<float> networkCurrentHP = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> networkMaxHP = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> networkIsDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        // 서버 전용 변수들
        private float currentMP = 50f;
        private float maxMP = 50f;
        private bool itemsDropped = false; // 아이템 드롭 중복 방지
        
        // 프로퍼티로 접근
        public float CurrentHP => networkCurrentHP.Value;
        public float MaxHP => networkMaxHP.Value;
        public bool IsDead => networkIsDead.Value;
        
        // 컴포넌트 참조
        private MonsterAI monsterAI;
        private MonsterSkillSystem skillSystem;
        private SpriteRenderer spriteRenderer;
        private Rigidbody2D rb;
        
        // 공격 참여자 추적
        private HashSet<ulong> participatingPlayers = new HashSet<ulong>();
        private Dictionary<ulong, float> playerDamageContribution = new Dictionary<ulong, float>();
        
        // 이벤트
        public System.Action<float> OnDamageTaken;
        public System.Action OnDeath;
        public System.Action<MonsterEntity> OnEntityGenerated;
        
        // 프로퍼티들
        public MonsterRaceData RaceData => raceData;
        public MonsterVariantData VariantData => variantData;
        public float Grade => grade;
        public StatBlock FinalStats => finalStats;
        public List<MonsterSkillInstance> ActiveSkills => activeSkills;
        public CombatStats CombatStats => combatStats;
        
        public float CurrentMP => currentMP;
        public float MaxMP => maxMP;
        
        private void Start()
        {
            Debug.Log($"🌐 MonsterEntity Start: name={name}");
            Debug.Log($"🌐 Initial Data: raceData={raceData?.raceName ?? "NULL"}, variantData={variantData?.variantName ?? "NULL"}");
            
            // 컴포넌트 참조
            monsterAI = GetComponent<MonsterAI>();
            skillSystem = GetComponent<MonsterSkillSystem>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
            
            // 몬스터 생성 로직 (서버 체크는 GenerateMonster 내부에서)
            if (raceData != null && variantData != null)
            {
                Debug.Log($"🌐 Start: Calling GenerateMonster with existing data");
                GenerateMonster(raceData, variantData);
            }
            else
            {
                Debug.LogWarning($"🌐 Start: raceData or variantData is null, waiting for external call");
            }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Debug.Log($"🌟 MonsterEntity {name} OnNetworkSpawn called! IsSpawned={IsSpawned}, IsServer={IsServer}");
        }
        
        /// <summary>
        /// 몬스터 생성 및 초기화 (서버에서만)
        /// </summary>
        public void GenerateMonster(MonsterRaceData race = null, MonsterVariantData variant = null, float? forceGrade = null)
        {
            bool isServer = NetworkManager.Singleton != null ? NetworkManager.Singleton.IsServer : true; // 네트워크가 없으면 로컬로 처리
            
            Debug.Log($"🔧 GenerateMonster called: IsServer={isServer}, race={race?.raceName}, variant={variant?.variantName}");
            
            if (!isServer) 
            {
                Debug.LogWarning($"❌ GenerateMonster skipped - not running on server (IsServer={isServer})");
                return;
            }
            
            if (race != null) raceData = race;
            if (variant != null) variantData = variant;
            
            if (raceData == null || variantData == null)
            {
                Debug.LogError($"MonsterEntity requires both RaceData and VariantData!");
                return;
            }
            
            // 등급 결정 (강제 지정되지 않은 경우 확률적)
            grade = forceGrade ?? DetermineRandomGrade();
            
            // 스탯 계산
            CalculateFinalStats();
            
            // 스킬 생성
            GenerateSkills();
            
            // 전투 스탯 계산
            CalculateCombatStats();
            
            // HP/MP 초기화
            InitializeVitals();
            
            // AI 설정
            ConfigureAI();
            
            // 클라이언트에 동기화
            OnEntityGenerated?.Invoke(this);
            
            // Inspector 표시용 업데이트
            currentRaceName = raceData?.raceName ?? "Unknown Race";
            currentVariantName = variantData?.variantName ?? "Unknown Variant";
            currentGrade = grade;
            
            Debug.Log($"✨ Generated {variantData.variantName} ({raceData.raceName}) - Grade: {grade}");
            Debug.Log($"🔍 DEBUG: RaceData={raceData?.raceName ?? "NULL"}, VariantData={variantData?.variantName ?? "NULL"}");
            Debug.Log($"📊 Final Stats: STR {finalStats.strength:F1}, HP {MaxHP:F0}");
        }
        
        /// <summary>
        /// 랜덤 등급 결정
        /// </summary>
        private float DetermineRandomGrade()
        {
            // 80~120 범위에서 정규분포에 가까운 형태로 생성
            // 100을 중심으로 대부분의 값이 분포하되, 극값도 나올 수 있게
            
            float roll1 = Random.Range(0f, 1f);
            float roll2 = Random.Range(0f, 1f);
            
            // Box-Muller 변환으로 정규분포 생성
            float gaussianRandom = Mathf.Sqrt(-2.0f * Mathf.Log(roll1)) * Mathf.Cos(2.0f * Mathf.PI * roll2);
            
            // 평균 100, 표준편차 7 정도로 설정하여 80~120 범위에 대부분 포함되도록
            float grade = 100f + gaussianRandom * 7f;
            
            // 80~120 범위로 클램프
            return Mathf.Clamp(grade, 80f, 120f);
        }
        
        /// <summary>
        /// 최종 스탯 계산 (플레이어와 동일한 방식)
        /// </summary>
        private void CalculateFinalStats()
        {
            // 1. 종족 기본 스탯 (등급별 조정)
            StatBlock raceStats = raceData.CalculateStatsForGrade(grade);
            
            // 2. 개체별 편차 적용
            StatBlock variantStats = variantData.ApplyVarianceToStats(raceStats);
            
            // 3. 스킬 보너스는 나중에 적용 (스킬 생성 후)
            finalStats = variantStats;
        }
        
        /// <summary>
        /// 스킬 생성 (필수 + 선택)
        /// </summary>
        private void GenerateSkills()
        {
            activeSkills.Clear();
            
            // 1. 필수 스킬 추가
            var mandatorySkills = variantData.GetAllMandatorySkills(grade);
            foreach (var skillRef in mandatorySkills)
            {
                var skillInstance = CreateSkillInstance(skillRef.skillData, grade);
                activeSkills.Add(skillInstance);
            }
            
            // 2. 선택 스킬 추가
            var availableSkills = variantData.GetAllAvailableSkills(grade);
            int optionalCount = variantData.CalculateOptionalSkillCount(grade);
            
            // 중복 제거를 위해 셔플
            var shuffledSkills = new List<MonsterSkillReference>(availableSkills);
            for (int i = 0; i < shuffledSkills.Count; i++)
            {
                var temp = shuffledSkills[i];
                int randomIndex = Random.Range(i, shuffledSkills.Count);
                shuffledSkills[i] = shuffledSkills[randomIndex];
                shuffledSkills[randomIndex] = temp;
            }
            
            // 필요한 개수만큼 선택
            for (int i = 0; i < Mathf.Min(optionalCount, shuffledSkills.Count); i++)
            {
                var skillInstance = CreateSkillInstance(shuffledSkills[i].skillData, grade);
                activeSkills.Add(skillInstance);
            }
            
            // 스킬 보너스를 최종 스탯에 적용
            ApplySkillBonusesToStats();
        }
        
        /// <summary>
        /// 스킬 인스턴스 생성
        /// </summary>
        private MonsterSkillInstance CreateSkillInstance(MonsterSkillData skillData, float skillGrade)
        {
            return new MonsterSkillInstance
            {
                skillData = skillData,
                effectGrade = skillGrade,
                lastUsedTime = -999f, // 즉시 사용 가능
                isActive = true
            };
        }
        
        /// <summary>
        /// 스킬 보너스를 스탯에 적용
        /// </summary>
        private void ApplySkillBonusesToStats()
        {
            foreach (var skill in activeSkills)
            {
                if (skill.skillData.IsPassive)
                {
                    var effect = skill.skillData.GetSkillEffect();
                    var statBonus = effect.GetStatBlockForGrade(skill.effectGrade);
                    finalStats = finalStats + statBonus;
                }
            }
        }
        
        /// <summary>
        /// 전투 스탯 계산 (플레이어와 동일한 방식)
        /// </summary>
        private void CalculateCombatStats()
        {
            // 물리 데미지 범위 계산
            float minPhysDamage = finalStats.strength * 1.5f;
            float maxPhysDamage = finalStats.strength * 2.5f;
            var physicalDamage = new DamageRange(minPhysDamage, maxPhysDamage, finalStats.stability);
            
            // 마법 데미지 범위 계산
            float minMagDamage = finalStats.intelligence * 1.2f;
            float maxMagDamage = finalStats.intelligence * 2.0f;
            var magicalDamage = new DamageRange(minMagDamage, maxMagDamage, finalStats.stability);
            
            // 치명타 확률 계산
            float critChance = finalStats.luck * 0.0005f; // 0.05% per LUK
            
            combatStats = new CombatStats(physicalDamage, magicalDamage, critChance, 2.0f, finalStats.stability);
        }
        
        /// <summary>
        /// HP/MP 초기화
        /// </summary>
        private void InitializeVitals()
        {
            // 플레이어와 동일한 계산식
            float maxHealth = 100f + (finalStats.vitality * 10f);
            float maxMana = 50f + (finalStats.intelligence * 5f);
            
            // 서버에서만 NetworkVariable 값 설정
            if (IsServer)
            {
                networkMaxHP.Value = maxHealth;
                networkCurrentHP.Value = maxHealth;
                networkIsDead.Value = false;
            }
            
            maxMP = maxMana;
            currentMP = maxMana;
            itemsDropped = false; // 아이템 드롭 플래그 초기화
        }
        
        /// <summary>
        /// AI 설정
        /// </summary>
        private void ConfigureAI()
        {
            if (monsterAI != null)
            {
                monsterAI.SetAIType(variantData.PreferredAIType);
                
                // AI 기본 설정
                monsterAI.SetAttackDamage(combatStats.physicalDamage.maxDamage);
            }
        }
        
        /// <summary>
        /// 데미지 받기 - ServerRpc로 호출
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(float damage, DamageType damageType, ulong attackerClientId = 0)
        {
            Debug.Log($"🩸 TakeDamageServerRpc: damage={damage}, attackerClientId={attackerClientId}, isDead={IsDead}, IsServer={IsServer}");
            
            if (!IsServer)
            {
                Debug.LogWarning($"🩸 TakeDamageServerRpc called on non-server, ignoring");
                return;
            }
            
            if (IsDead) 
            {
                Debug.LogWarning($"🩸 Monster already dead, ignoring damage");
                return;
            }
            
            // 서버에서만 실제 데미지 처리
            ProcessDamage(damage, damageType, attackerClientId);
        }
        
        /// <summary>
        /// 서버에서 실제 데미지 처리
        /// </summary>
        private void ProcessDamage(float damage, DamageType damageType, ulong attackerClientId)
        {
            float finalDamage = damage;
            
            // 방어력 적용
            if (damageType == DamageType.Physical)
            {
                float defenseRate = finalStats.defense / (finalStats.defense + 100f);
                finalDamage *= (1f - defenseRate);
            }
            else if (damageType == DamageType.Magical)
            {
                float magicDefenseRate = finalStats.magicDefense / (finalStats.magicDefense + 100f);
                finalDamage *= (1f - magicDefenseRate);
            }
            
            // 회피 체크
            float dodgeChance = finalStats.agility * 0.001f; // 0.1% per AGI
            if (Random.value < dodgeChance)
            {
                Debug.Log($"{variantData.variantName} dodged the attack!");
                return;
            }
            
            // 최소 1 데미지
            finalDamage = Mathf.Max(1f, finalDamage);
            
            // HP 감소 (NetworkVariable 업데이트)
            float oldHP = networkCurrentHP.Value;
            float newHP = Mathf.Max(0f, oldHP - finalDamage);
            networkCurrentHP.Value = newHP;
            
            Debug.Log($"🩸 HP change: {oldHP:F1} → {newHP:F1} (damage: {finalDamage:F1})");
            
            // 공격자를 참여자로 추가 (데미지가 실제로 들어갔을 때만)
            if (attackerClientId != 0 && finalDamage > 0f)
            {
                participatingPlayers.Add(attackerClientId);
                
                // 데미지 기여도 추적
                if (playerDamageContribution.ContainsKey(attackerClientId))
                {
                    playerDamageContribution[attackerClientId] += finalDamage;
                }
                else
                {
                    playerDamageContribution[attackerClientId] = finalDamage;
                }
            }
            
            OnDamageTaken?.Invoke(finalDamage);
            
            // 사망 처리
            Debug.Log($"🩸 Death check: newHP={newHP:F1}, isDead={IsDead}, shouldDie={newHP <= 0f && !IsDead}");
            
            if (newHP <= 0f && !IsDead)
            {
                Debug.Log($"☠️ Monster dying: {variantData?.variantName ?? "Unknown"}");
                Die(attackerClientId);
            }
        }
        
        /// <summary>
        /// 사망 처리 (서버에서만)
        /// </summary>
        private void Die(ulong killerClientId = 0)
        {
            Debug.Log($"💀 Die() called: killerClientId={killerClientId}, alreadyDead={IsDead}");
            
            if (IsDead) 
            {
                Debug.LogWarning($"💀 Die() skipped - already dead");
                return;
            }
            
            Debug.Log($"💀 Setting networkIsDead to true...");
            networkIsDead.Value = true;
            Debug.Log($"💀 networkIsDead successfully set to: {IsDead}");
            
            // 즉시 콜라이더와 AI 비활성화 (더 이상 공격받지 않도록)
            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = false;
                Debug.Log($"💀 Collider disabled for {name}");
            }
            
            var monsterAI = GetComponent<MonsterAI>();
            if (monsterAI != null)
            {
                monsterAI.enabled = false;
                Debug.Log($"💀 MonsterAI disabled for {name}");
            }
            
            // 시각적 표시 (투명하게)
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                var color = spriteRenderer.color;
                color.a = 0.3f; // 30% 투명도
                spriteRenderer.color = color;
            }
            
            Debug.Log($"💀 Invoking OnDeath event...");
            OnDeath?.Invoke();
            
            // 보상 지급
            Debug.Log($"💀 Giving rewards to nearby players...");
            GiveRewardsToNearbyPlayers(killerClientId);
            
            // 아이템 드롭 (ClientRpc로 모든 클라이언트에 알림)
            Debug.Log($"🎁 Dropping items...");
            DropItemsForAllClients();
            
            Debug.Log($"☠️ {variantData.variantName} has died!");
        }
        
        /// <summary>
        /// 보상 지급 (공격에 참여한 플레이어들에게만)
        /// </summary>
        private void GiveRewardsToNearbyPlayers(ulong killerClientId = 0)
        {
            bool isServer = NetworkManager.Singleton != null ? NetworkManager.Singleton.IsServer : true;
            
            Debug.Log($"🎁 GiveRewardsToNearbyPlayers: IsServer={isServer}, participatingPlayers={participatingPlayers.Count}");
            
            if (!isServer) 
            {
                Debug.LogWarning($"🎁 GiveRewardsToNearbyPlayers skipped - not server");
                return;
            }
            
            // 경험치와 골드 계산
            long expReward = raceData.CalculateExperienceForGrade(grade);
            long goldReward = raceData.CalculateGoldForGrade(grade);
            
            var monsterNetworkObject = GetComponent<NetworkObject>();
            ulong monsterId = monsterNetworkObject != null ? monsterNetworkObject.NetworkObjectId : 0;
            int playersRewarded = 0;
            
            // 공격에 참여한 플레이어들에게만 경험치 지급
            foreach (ulong playerId in participatingPlayers)
            {
                // NetworkManager에서 해당 플레이어의 NetworkObject 찾기
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerNetObj))
                {
                    var player = playerNetObj.GetComponent<PlayerController>();
                    if (player != null)
                    {
                        var statsManager = player.GetComponent<PlayerStatsManager>();
                        if (statsManager != null && !statsManager.IsDead)
                        {
                            // 중복 방지 경험치 획득 시도
                            if (statsManager.TryGainExperienceFromMonster(monsterId, expReward))
                            {
                                playersRewarded++;
                            }
                            // 골드는 나중에 추가 구현
                        }
                    }
                }
            }
            
            Debug.Log($"🎯 {variantData.variantName} defeated! {playersRewarded}/{participatingPlayers.Count} players rewarded with {expReward} EXP");
            
            // 아이템 드롭
            Debug.Log($"🎁 About to call TryDropItems...");
            TryDropItems();
            
            // 영혼 드롭 (스킬 포함)
            Debug.Log($"🎁 About to call TryDropSoul...");
            TryDropSoul();
        }
        
        /// <summary>
        /// 아이템 드롭 시도
        /// </summary>
        private void TryDropItems()
        {
            Debug.Log($"🎲 TryDropItems called for {variantData?.variantName ?? "NULL"}");
            
            // 이미 아이템을 드롭했는지 체크
            if (itemsDropped)
            {
                Debug.LogWarning($"🎲 Items already dropped for {variantData?.variantName}, skipping...");
                return;
            }
            Debug.Log($"🎲 Grade: {grade}, VariantData: {variantData != null}");
            
            if (variantData == null)
            {
                Debug.LogError($"🎲 VariantData is null! Cannot drop items.");
                return;
            }
            
            // 개체별 전체 드롭 계산 (종족 + 개체)
            var droppedItems = variantData.CalculateAllItemDrops(grade);
            Debug.Log($"🎲 CalculateAllItemDrops returned {droppedItems?.Count ?? -1} items");
            
            if (droppedItems != null && droppedItems.Count > 0)
            {
                Vector3 dropPosition = transform.position;
                Debug.Log($"🎲 Dropping {droppedItems.Count} items at position {dropPosition}");
                
                foreach (var item in droppedItems)
                {
                    if (item != null)
                    {
                        Debug.Log($"🎲 Spawning item: {item.ItemName} (Grade: {item.Grade})");
                        // 드롭된 아이템을 월드에 생성
                        SpawnDroppedItem(item, dropPosition);
                    }
                    else
                    {
                        Debug.LogWarning($"🎲 Null item in dropped items list");
                    }
                }
                
                Debug.Log($"💰 {variantData.variantName} dropped {droppedItems.Count} items!");
            }
            else
            {
                Debug.Log($"🎲 No items to drop for {variantData.variantName} (grade: {grade})");
            }
            
            // 드롭 완료 플래그 설정
            itemsDropped = true;
            Debug.Log($"🎲 itemsDropped flag set to true for {variantData?.variantName}");
        }
        
        /// <summary>
        /// 드롭된 아이템을 월드에 스폰
        /// </summary>
        private void SpawnDroppedItem(ItemData itemData, Vector3 position)
        {
            Debug.Log($"🎁 SpawnDroppedItem called: item={itemData?.ItemName ?? "NULL"}, position={position}");
            
            // 서버 체크
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            {
                Debug.LogWarning($"🎁 Not server, ignoring item drop for {itemData?.ItemName}");
                return;
            }
            
            // 간단한 아이템 드롭 생성 (프리팹 없이)
            Vector3 randomOffset = new Vector3(
                Random.Range(-1f, 1f), 
                Random.Range(-1f, 1f), 
                0f
            );
            Vector3 spawnPosition = position + randomOffset;
            
            Debug.Log($"🎁 Creating ItemDrop GameObject at {spawnPosition}...");
            GameObject droppedItemObj = new GameObject($"DroppedItem_{itemData.ItemName}");
            droppedItemObj.transform.position = spawnPosition;
            
            // ItemDrop 컴포넌트 추가
            var itemDrop = droppedItemObj.AddComponent<ItemDrop>();
            var itemInstance = new ItemInstance(itemData, 1);
            itemDrop.SetItemInstance(itemInstance);
            itemDrop.SetDropPosition(spawnPosition);
            
            Debug.Log($"🎁 ItemDrop created successfully with {itemData.ItemName}");
        }
        
        /// <summary>
        /// 모든 클라이언트에서 아이템 드롭 생성
        /// </summary>
        private void DropItemsForAllClients()
        {
            // 드롭할 아이템들 계산
            var droppedItems = variantData?.CalculateAllItemDrops(grade);
            if (droppedItems != null && droppedItems.Count > 0)
            {
                foreach (var itemData in droppedItems)
                {
                    if (itemData != null)
                    {
                        // 모든 클라이언트에 아이템 드롭 알림
                        SpawnItemClientRpc(transform.position, itemData.ItemId, itemData.ItemName, itemData.GradeColor);
                    }
                }
            }
        }
        
        /// <summary>
        /// 클라이언트에서 아이템 생성
        /// </summary>
        [ClientRpc]
        private void SpawnItemClientRpc(Vector3 position, string itemId, string itemName, Color gradeColor)
        {
            Debug.Log($"🎁 [ClientRpc] Spawning item: {itemName} at {position}");
            
            // 랜덤 오프셋 적용
            Vector3 randomOffset = new Vector3(
                Random.Range(-1f, 1f), 
                Random.Range(-1f, 1f), 
                0f
            );
            Vector3 spawnPosition = position + randomOffset;
            
            // 아이템 GameObject 생성
            GameObject itemObject = new GameObject($"DroppedItem_{itemName}");
            itemObject.transform.position = spawnPosition;
            
            // ItemDrop 컴포넌트 추가
            var itemDrop = itemObject.AddComponent<ItemDrop>();
            var itemData = ItemDatabase.GetItem(itemId);
            if (itemData != null)
            {
                var itemInstance = new ItemInstance(itemData, 1);
                itemDrop.SetItemInstance(itemInstance);
                itemDrop.SetDropPosition(spawnPosition);
                
                Debug.Log($"🎁 Item created successfully: {itemName}");
            }
            else
            {
                Debug.LogError($"❌ ItemData not found for {itemId}");
                Destroy(itemObject);
            }
        }
        
        /// <summary>
        /// 영혼 드롭 시도
        /// </summary>
        private void TryDropSoul()
        {
            float dropRate = raceData.CalculateSoulDropRateForGrade(grade);
            
            if (Random.value < dropRate)
            {
                // 몬스터가 가진 스킬들을 포함한 영혼 생성
                CreateSoulWithSkills();
            }
        }
        
        /// <summary>
        /// 스킬이 포함된 영혼 생성
        /// </summary>
        private void CreateSoulWithSkills()
        {
            // MonsterSoulDropSystem을 통해 스킬이 포함된 영혼 드롭
            var soulDropSystem = GetComponent<MonsterSoulDropSystem>();
            if (soulDropSystem == null)
            {
                // 컴포넌트가 없으면 추가
                soulDropSystem = gameObject.AddComponent<MonsterSoulDropSystem>();
            }
            
            // 영혼 드롭 체크는 MonsterEntitySpawner의 OnMonsterEntityDeath에서 처리됨
            Debug.Log($"💎 {variantData.variantName} soul drop will be handled by MonsterSoulDropSystem!");
        }
        
        
        /// <summary>
        /// 디버그 정보
        /// </summary>
        [ContextMenu("Show Monster Info")]
        public void ShowMonsterInfo()
        {
            Debug.Log($"=== {variantData?.variantName} ({raceData?.raceName}) ===");
            Debug.Log($"Grade: {grade}");
            Debug.Log($"Final Stats: STR {finalStats.strength:F1}, AGI {finalStats.agility:F1}, VIT {finalStats.vitality:F1}, INT {finalStats.intelligence:F1}");
            Debug.Log($"Combat: {combatStats.physicalDamage.minDamage:F1}-{combatStats.physicalDamage.maxDamage:F1} DMG, {combatStats.criticalChance:P1} CRIT");
            Debug.Log($"Skills: {activeSkills.Count} active");
            Debug.Log($"HP: {CurrentHP:F0}/{MaxHP:F0}, MP: {CurrentMP:F0}/{MaxMP:F0}");
        }
        
        /// <summary>
        /// MonsterHealth 호환성을 위한 인터페이스들
        /// </summary>
        public int MaxHealth => Mathf.RoundToInt(MaxHP);
        public int CurrentHealth => Mathf.RoundToInt(CurrentHP);
        public float HealthPercentage => MaxHP > 0 ? CurrentHP / MaxHP : 0f;
        
        /// <summary>
        /// 몬스터 정보 설정 (기존 MonsterHealth.SetMonsterInfo 호환)
        /// </summary>
        public void SetMonsterInfo(string monsterName, int level, string origin, float health, long expReward)
        {
            // MonsterEntity는 이미 GenerateMonster로 초기화되므로 추가 설정 필요 시 여기서 처리
            Debug.Log($"MonsterEntity {monsterName} info set (compatibility mode)");
        }
        
        /// <summary>
        /// 기존 MonsterHealth.TakeDamage 호환 메서드
        /// </summary>
        public float TakeDamage(float damage, DamageType damageType, PlayerController attacker = null)
        {
            ulong attackerClientId = attacker?.OwnerClientId ?? 0;
            TakeDamageServerRpc(damage, damageType, attackerClientId);
            return damage; // 실제 데미지 반환
        }
        
        /// <summary>
        /// 기존 MonsterHealth.TakeDamageServerRpc 호환 (로컬 호출로 변경)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageCompatServerRpc(float damage, DamageType damageType = DamageType.Physical)
        {
            TakeDamageServerRpc(damage, damageType, 0);
        }
    }
    
    /// <summary>
    /// 몬스터 스킬 인스턴스
    /// </summary>
    [System.Serializable]
    public struct MonsterSkillInstance
    {
        public MonsterSkillData skillData;
        public float effectGrade;
        public float lastUsedTime;
        public bool isActive;
        
        public bool CanUse => Time.time >= lastUsedTime + skillData.Cooldown;
        
        public MonsterSkillEffect GetCurrentEffect()
        {
            return skillData.GetSkillEffect();
        }
        
        /// <summary>
        /// 등급에 따른 실제 StatBlock 가져오기
        /// </summary>
        public StatBlock GetActualStatBlock()
        {
            return skillData.GetSkillEffect().GetStatBlockForGrade(effectGrade);
        }
    }
}