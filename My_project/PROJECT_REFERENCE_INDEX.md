# 프로젝트 전체 참조 관계 완전 인덱스

Unity Template Multiplayer NGO Runtime 프로젝트의 모든 .cs 파일에 대한 완전한 참조 관계 분석

분석 일시: 2025-08-18
총 .cs 파일 개수: 85개
네임스페이스: Unity.Template.Multiplayer.NGO.Runtime

---

## 파일별 상세 내용

### AssemblyInfo.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime (내부)
**Using 구문**: System.Runtime.CompilerServices

#### 정의된 타입들
- 어셈블리 속성: `[assembly: InternalsVisibleTo("Unity.Template.Multiplayer-NGO.Tests.Runtime")]`

### Core/Element.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: UnityEngine

#### 정의된 타입들
##### 클래스: Element<T>
- 상속: Element  
- 제약: T : BaseApplication
- 프로퍼티:
  - new public T App { get }

##### 클래스: Element
- 상속: MonoBehaviour
- 프로퍼티:
  - public BaseApplication App => m_app
- 필드:
  - BaseApplication m_app
- 메서드:
  - internal T Find<T>(T p_var, bool searchGlobally = false) where T : Object
  - T FindInParent<T>(T p_var) where T : Object
  - internal void Broadcast(AppEvent evt)
- 사용하는 타입들: [BaseApplication, AppEvent, MonoBehaviour, Object]

### Core/BaseApplication.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: 없음

#### 정의된 타입들
##### 클래스: BaseApplication<M, V, C>
- 상속: BaseApplication
- 제약: M : Element, V : Element, C : Element
- 프로퍼티:
  - new internal BaseApplication<M, V, C> Instance => (BaseApplication<M, V, C>)(object)base.Instance
  - new public M Model => (M)(object)base.Model
  - new public V View => (V)(object)base.View
  - new public C Controller => (C)(object)base.Controller

##### 클래스: BaseApplication
- 상속: Element
- 필드:
  - internal BaseApplication Instance { get; private set; }
  - internal EventManager EventManager
  - Model m_model
  - View m_view
  - Controller m_controller
- 프로퍼티:
  - internal Model Model => m_model = Find<Model>(m_model)
  - internal View View => m_view = Find<View>(m_view)
  - internal Controller Controller => m_controller = Find<Controller>(m_controller)
- 메서드:
  - protected virtual void Awake()
  - new internal void Broadcast(AppEvent evt)
- 사용하는 타입들: [Element, EventManager, Model, View, Controller, AppEvent]

### Core/Controller.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: System

#### 정의된 타입들
##### 클래스: Controller
- 상속: Element

##### 추상 클래스: Controller<T>
- 상속: Controller
- 제약: T : BaseApplication
- 프로퍼티:
  - new public T App => (T)base.App
- 메서드:
  - internal void AddListener<E>(Action<E> evt) where E : AppEvent
  - internal void RemoveListener<E>(Action<E> evt) where E : AppEvent
  - internal abstract void RemoveListeners()
- 사용하는 타입들: [Element, BaseApplication, Action, AppEvent]

### Core/EventManager.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: System, System.Collections.Generic

#### 정의된 타입들
##### 클래스: AppEvent

##### 클래스: EventManager
- 필드:
  - readonly Dictionary<Type, Action<AppEvent>> m_Events
  - readonly Dictionary<Delegate, Action<AppEvent>> m_EventLookups
- 메서드:
  - internal void AddListener<T>(Action<T> evt) where T : AppEvent
  - internal void RemoveListener<T>(Action<T> evt) where T : AppEvent
  - internal void Broadcast(AppEvent evt)
  - internal void Clear()
- 사용하는 타입들: [Type, Action, Delegate, Dictionary, AppEvent]

### Core/Model.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: 없음

#### 정의된 타입들
##### 클래스: Model
- 상속: Element

##### 클래스: Model<T>
- 상속: Model
- 제약: T : BaseApplication
- 프로퍼티:
  - new public T App => (T)base.App
- 사용하는 타입들: [Element, BaseApplication]

### Core/View.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: 없음

#### 정의된 타입들
##### 클래스: View
- 상속: Element

##### 클래스: View<T>
- 상속: View
- 제약: T : BaseApplication
- 프로퍼티:
  - new public T App => (T)base.App
- 메서드:
  - internal void Show()
  - internal void Hide()
- 사용하는 타입들: [Element, BaseApplication]

### Core/ResourceLoader.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: UnityEngine, System.Collections.Generic

#### 정의된 타입들
##### 클래스: ResourceLoader (static)
- 필드:
  - private static Dictionary<string, Sprite> cachedSprites
  - private static Dictionary<string, Sprite[]> cachedSpriteSheets
- 메서드:
  - public static Sprite GetPlayerSprite(Race race, PlayerAnimationType animType = PlayerAnimationType.Idle, int direction = 0)
  - public static Sprite GetMonsterSprite(MonsterType monsterType, MonsterAnimationType animType = MonsterAnimationType.Idle)
  - public static Sprite[] GetSpriteSheet(string path)
  - public static Sprite GetWeaponSprite(WeaponType weaponType)
  - public static void ClearCache()
  - public static Sprite GetEnvironmentSprite(string spriteName)
  - public static Sprite GetItemSprite(string itemName)

##### 열거형: PlayerAnimationType
- 값: Idle, Walk, Run, Attack_Slice, Attack_Pierce, Hit, Death, Collect, Carry_Idle, Carry_Walk, Carry_Run

##### 열거형: MonsterType
- 값: Orc, OrcWarrior, OrcRogue, OrcShaman, Skeleton, SkeletonWarrior, SkeletonRogue, SkeletonMage

##### 열거형: MonsterAnimationType
- 값: Idle, Run, Death

- 사용하는 타입들: [Dictionary, Sprite, Race, PlayerAnimationType, MonsterType, MonsterAnimationType, WeaponType, Debug]

### Shared/Events.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: 없음

#### 정의된 타입들
##### 클래스: MatchEnteredEvent
- 상속: AppEvent
- 사용하는 타입들: [AppEvent]

### Race/RaceData.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: UnityEngine, Unity.Netcode

#### 정의된 타입들
##### 클래스: RaceData
- 상속: ScriptableObject
- 어트리뷰트: [CreateAssetMenu(fileName = "New Race Data", menuName = "Dungeon Crawler/Race Data")]
- 필드:
  - public Race raceType
  - public string raceName
  - public string description
  - public Sprite raceIcon
  - [SerializeField] private StatBlock baseStats
  - [SerializeField] private StatGrowth statGrowth
  - [SerializeField] private ElementalStats elementalAffinity
  - [SerializeField] private RaceSpecialty[] specialties
- 프로퍼티:
  - public StatBlock BaseStats => baseStats
  - public StatGrowth StatGrowth => statGrowth
  - public ElementalStats ElementalAffinity => elementalAffinity
  - public RaceSpecialty[] Specialties => specialties
- 메서드:
  - public StatBlock CalculateStatsAtLevel(int level)
  - public bool HasSpecialty(RaceSpecialtyType specialtyType)
  - public float GetSpecialtyValue(RaceSpecialtyType specialtyType)
  - public string GetPlayStyleInfo()
  - public void LogRaceInfo()

##### 열거형: Race
- 값: Human, Elf, Beast, Machina

##### 구조체: DamageRange
- 구현: [System.Serializable]
- 필드:
  - public float minDamage
  - public float maxDamage
  - public float stability
- 메서드:
  - public DamageRange(float min, float max, float stab = 0f)
  - public DamageRange GetStabilizedRange(float stabilityBonus)
  - public float GetRandomDamage()

##### 구조체: ElementalDamageRange
- 구현: [System.Serializable]
- 필드:
  - public DamageRange fire, ice, lightning, poison, dark, holy

##### 구조체: CombatStats
- 구현: [System.Serializable]
- 필드:
  - public DamageRange physicalDamage
  - public DamageRange magicalDamage
  - public ElementalDamageRange elementalDamage
  - public float criticalChance, criticalMultiplier, stability

##### 구조체: StatBlock
- 구현: INetworkSerializable, [System.Serializable]
- 필드:
  - public float strength, agility, vitality, intelligence, defense, magicDefense, luck, stability
- 메서드:
  - public StatBlock(float str, float agi, float vit, float inte, float def, float mdef, float luk, float stab = 0f)
  - public static StatBlock operator +(StatBlock a, StatBlock b)
  - public static StatBlock operator *(StatBlock stats, float multiplier)
  - public bool HasAnyStats()
  - public string GetStatsText()
  - public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter

##### 구조체: StatGrowth
- 구현: [System.Serializable]
- 필드:
  - public float strengthGrowth, agilityGrowth, vitalityGrowth, intelligenceGrowth, defenseGrowth, magicDefenseGrowth, luckGrowth, stabilityGrowth

##### 구조체: ElementalStats
- 구현: [System.Serializable]
- 필드:
  - public float fireAttack, fireResist, iceAttack, iceResist, lightningAttack, lightningResist, poisonAttack, poisonResist, darkAttack, darkResist, holyAttack, holyResist

##### 구조체: RaceSpecialty
- 구현: [System.Serializable]
- 필드:
  - public RaceSpecialtyType specialtyType
  - public float value
  - public string description

##### 열거형: RaceSpecialtyType
- 값: MagicMastery, PhysicalMastery, TechnicalMastery, DropRateBonus, ExpBonus, ElementalResistance, CriticalBonus, MovementBonus

- 사용하는 타입들: [ScriptableObject, Sprite, Random, Mathf, Debug, Unity.Netcode.INetworkSerializable, Unity.Netcode.BufferSerializer, Unity.Netcode.IReaderWriter]

### Equipment/EquipmentTypes.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: UnityEngine, Unity.Netcode, System.Collections.Generic

#### 정의된 타입들
##### 열거형: EquipmentSlot
- 값: None, Head, Chest, Legs, Feet, Hands, MainHand, OffHand, TwoHand, Ring1, Ring2, Necklace

##### 클래스: EquipmentSaveData
- 구현: [System.Serializable]
- 필드:
  - public Dictionary<EquipmentSlot, ItemInstance> equippedItems

##### 구조체: EquipmentSlotData
- 구현: INetworkSerializable, [System.Serializable]
- 필드:
  - public EquipmentSlot slot
  - public ItemInstance item
- 메서드:
  - public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter

- 사용하는 타입들: [Dictionary, ItemInstance, Unity.Netcode.INetworkSerializable]

### Items/ItemData.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: UnityEngine, System

#### 정의된 타입들
##### 클래스: ItemData
- 상속: ScriptableObject
- 어트리뷰트: [CreateAssetMenu(fileName = "New Item", menuName = "Dungeon Crawler/Item Data")]
- 필드:
  - [SerializeField] private string itemId, itemName, description
  - [SerializeField] private Sprite itemIcon
  - [SerializeField] private ItemType itemType
  - [SerializeField] private ItemGrade grade
  - [SerializeField] private EquipmentSlot equipmentSlot
  - [SerializeField] private WeaponCategory weaponCategory
  - [SerializeField] private int stackSize, durability, maxDurability
  - [SerializeField] private long sellPrice
  - [SerializeField] private bool isDroppable, isDestroyable
  - [SerializeField] private StatBlock statBonuses
  - [SerializeField] private DamageRange weaponDamageRange
  - [SerializeField] private float criticalBonus, healAmount, manaAmount
  - [SerializeField] private DamageType weaponDamageType
  - [SerializeField] private StatusEffect[] consumableEffects
  - [SerializeField] private Color gradeColor
- 프로퍼티:
  - 모든 필드에 대한 읽기 전용 프로퍼티들
  - public bool IsEquippable, IsWeapon, IsConsumable, CanStack 등 계산된 프로퍼티들
- 메서드:
  - private void OnValidate()
  - public static Color GetGradeColor(ItemGrade grade)
  - public static float GetGradeDropRate(ItemGrade grade)
  - public static float GetGradePriceMultiplier(ItemGrade grade)
  - public long GetTotalValue()
  - public void DecreaseDurability(int amount = 1)
  - public void RepairDurability(int amount)
  - public bool CanUse()
  - public DamageRange CalculateWeaponDamage(float strength, float stability)
  - public string GetInfoText()

##### 열거형: ItemType
- 값: Equipment, Consumable, Material, Quest, Other

##### 열거형: ItemGrade
- 값: Common = 1, Uncommon = 2, Rare = 3, Epic = 4, Legendary = 5

- 사용하는 타입들: [ScriptableObject, Sprite, Color, StatBlock, DamageRange, DamageType, StatusEffect, EquipmentSlot, WeaponCategory, Mathf, Debug, ColorUtility]

### Stats/PlayerStats.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: UnityEngine, Unity.Netcode, System

#### 정의된 타입들
##### 클래스: PlayerStats
- 상속: ScriptableObject
- 어트리뷰트: [CreateAssetMenu(fileName = "New Player Stats", menuName = "Dungeon Crawler/Player Stats")]
- 필드:
  - [SerializeField] private Race characterRace
  - [SerializeField] private RaceData raceData
  - [SerializeField] private StatBlock currentStats
  - [SerializeField] private int currentLevel, maxLevel
  - [SerializeField] private long currentExp, expToNextLevel
  - [SerializeField] private StatBlock soulBonusStats, equipmentBonusStats
  - [SerializeField] private float maxHP, currentHP, maxMP, currentMP
  - [SerializeField] private float attackDamage, magicDamage, moveSpeed, attackSpeed
  - [SerializeField] private float criticalChance, criticalDamage
  - [SerializeField] private long gold
  - [SerializeField] private CombatStats combatStats
  - [SerializeField] private WeaponData equippedWeapon
- 이벤트:
  - public static event Action<PlayerStats> OnStatsChanged
  - public static event Action<int> OnLevelUp
  - public static event Action<float, float> OnHPChanged, OnMPChanged
- 프로퍼티:
  - 모든 스탯에 대한 읽기 전용 프로퍼티들 (Race, Level, Stats 등)
  - 총합 스탯 프로퍼티들 (TotalSTR, TotalAGI 등)
- 메서드:
  - public void SetRace(Race race, RaceData data)
  - public void AddSoulBonusStats(StatBlock bonusStats)
  - public void SetEquipmentBonusStats(StatBlock bonusStats)
  - public void AddExperience(long amount)
  - private void LevelUp()
  - public void RecalculateStats()
  - private void RecalculateCombatStats()
  - public void ChangeHP(float amount), ChangeMP(float amount), ChangeGold(long amount)
  - public float CalculateAttackDamage(DamageType attackType = DamageType.Physical)
  - public float CalculateSkillDamage(float minDamagePercent, float maxDamagePercent, DamageType skillType = DamageType.Physical)
  - public void EquipWeapon(WeaponData weapon), UnequipWeapon()
  - public float TakeDamage(float damage, DamageType damageType = DamageType.Physical)
  - public bool IsDead()
  - public void Initialize()
  - public void LogStats()

##### 열거형: StatType
- 값: STR, AGI, VIT, INT, DEF, MDEF, LUK, STAB

##### 열거형: DamageType
- 값: Physical, Magical, True

- 사용하는 타입들: [ScriptableObject, Race, RaceData, StatBlock, CombatStats, WeaponData, DamageRange, Action, Mathf, Random, Debug]

### Player/PlayerController.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: Unity.Netcode, UnityEngine

#### 정의된 타입들
##### 클래스: PlayerController
- 상속: NetworkBehaviour
- 필드:
  - [SerializeField] private float baseMoveSpeed, rotationSpeed, baseAttackCooldown, attackRange
  - 컴포넌트 참조: PlayerInput, PlayerNetwork, PlayerStatsManager, CombatSystem 등
  - 계산된 값들: currentMoveSpeed, currentAttackCooldown
- 메서드:
  - public override void OnNetworkSpawn(), OnNetworkDespawn()
  - private void Update(), FixedUpdate()
  - private void HandleMovement(), HandleRotation(), HandleAttack(), HandleSkill()
  - private bool CanAttack(), void PerformAttack(), void ActivateSkill()
  - private void SetupDeathSystem()
  - private void InitializeStats(), OnStatsUpdated(), ApplyStatsFromManager()
  - public void SetMoveSpeed(float speed)
  - public void TakeDamage(float damage, DamageType damageType = DamageType.Physical)
  - public PlayerStats GetCurrentStats()
  - private void OnPlayerDeath()
  - public void GainExperience(long amount), Heal(float amount)
  - public float GetHealthPercentage(), GetAttackDamage()
  - public float GetSkillDamage(float minPercent, float maxPercent, DamageType skillType = DamageType.Physical)
  - private void OnDrawGizmosSelected()

- 사용하는 타입들: [NetworkBehaviour, Rigidbody2D, PlayerInput, PlayerNetwork, PlayerStatsManager, CombatSystem, PlayerVisualManager, Animator, DeathManager, SkillManager, CharacterDeletion, ItemScatter, SoulPreservation, SoulDropSystem, ItemDropSystem, InventoryManager, PlayerStats, DamageType, Camera, Vector2, Vector3, Quaternion, Mathf, Time, Debug, Gizmos]

### Shared/WeaponSystem.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: UnityEngine, Unity.Netcode

#### 정의된 타입들
##### 클래스: WeaponData
- 상속: ScriptableObject
- 어트리뷰트: [CreateAssetMenu(fileName = "New Weapon", menuName = "Dungeon Crawler/Weapon")]
- 필드:
  - public string weaponName
  - public WeaponType weaponType
  - public WeaponCategory category
  - public ItemGrade rarity
  - public string description
  - public Sprite weaponIcon
  - [SerializeField] private float baseDamage, minDamagePercent, maxDamagePercent
  - [SerializeField] private StatBlock statBonuses
  - [SerializeField] private float attackSpeed, attackRange, criticalBonus, stabilityBonus
  - [SerializeField] private ElementalDamageRange elementalDamage
  - [SerializeField] private WeaponEffect[] specialEffects
- 프로퍼티:
  - 모든 필드에 대한 읽기 전용 프로퍼티들
- 메서드:
  - public DamageRange CalculateDamageRange(float playerSTR, float playerStability = 0f)
  - public DamageRange CalculateMagicDamageRange(float playerINT, float playerStability = 0f)
  - private void ApplyRarityModifiers(ref float minDamage, ref float maxDamage)
  - public static void SetWeaponTypeDefaults(WeaponData weapon)

##### 열거형: WeaponType
- 값: Longsword, Rapier, Broadsword, Mace, Warhammer, Dagger, CurvedDagger, Longbow, Crossbow, OakStaff, CrystalStaff, Fists, Shield

##### 열거형: WeaponCategory
- 값: None, Sword, Blunt, Dagger, Bow, Staff, Wand, Shield, Fists

##### 구조체: WeaponEffect
- 구현: [System.Serializable]
- 필드:
  - public WeaponEffectType effectType
  - public float value, chance
  - public string description

##### 열거형: WeaponEffectType
- 값: LifeSteal, ManaSteal, ElementalBurst, Stun, Poison, Freeze, Burn, CriticalBoost, DamageBoost, SpeedBoost

- 사용하는 타입들: [ScriptableObject, Sprite, StatBlock, ElementalDamageRange, DamageRange, ItemGrade, Random, Mathf]

### Skills/SkillData.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: UnityEngine

#### 정의된 타입들
##### 클래스: SkillData
- 상속: ScriptableObject
- 어트리뷰트: [CreateAssetMenu(fileName = "New Skill", menuName = "Dungeon Crawler/Skill Data")]
- 필드:
  - public string skillName, skillId, description
  - public Sprite skillIcon
  - public int requiredLevel, skillTier
  - public long goldCost
  - public Race requiredRace
  - public SkillCategory category
  - public SkillData[] prerequisiteSkills
  - public SkillType skillType
  - public DamageType damageType
  - public float cooldown, manaCost, castTime, range
  - public float baseDamage, damageScaling, minDamagePercent, maxDamagePercent
  - public StatBlock statBonus
  - public float healthBonus, manaBonus, moveSpeedBonus, attackSpeedBonus
  - public StatusEffect[] statusEffects
  - public float statusDuration, statusChance
  - public GameObject castEffectPrefab, hitEffectPrefab, buffEffectPrefab
  - public AudioClip castSound, hitSound
- 메서드:
  - public bool CanLearn(PlayerStats playerStats, System.Collections.Generic.List<string> learnedSkills)
  - public float CalculateDamage(PlayerStats playerStats)
  - public float CalculateHealing(PlayerStats playerStats)
  - public float GetManaCost(int playerLevel)
  - public float GetCooldown(PlayerStats playerStats)
  - public string GetDetailedDescription(PlayerStats playerStats = null)

##### 열거형: SkillCategory
- 값: Warrior, Paladin, Rogue, Archer, ElementalMage, PureMage, NatureMage, PsychicMage, Berserker, Hunter, Assassin, Beast, HeavyArmor, Engineer, Artillery, Nanotech

##### 열거형: SkillType
- 값: Active, Passive, Toggle, Triggered

##### 구조체: StatusEffect
- 구현: [System.Serializable]
- 필드:
  - public StatusType type
  - public float value, duration, tickInterval
  - public bool stackable

##### 열거형: StatusType
- 값: Poison, Burn, Freeze, Stun, Slow, Weakness, Strength, Speed, Regeneration, Shield, Blessing, Berserk

- 사용하는 타입들: [ScriptableObject, Sprite, Race, SkillCategory, SkillType, DamageType, StatBlock, StatusEffect, GameObject, AudioClip, PlayerStats, Random, Mathf]

### Combat/CombatSystem.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: UnityEngine, Unity.Netcode, System.Collections.Generic

#### 정의된 타입들
##### 클래스: CombatSystem
- 상속: NetworkBehaviour
- 필드:
  - [SerializeField] private LayerMask enemyLayerMask, playerLayerMask
  - [SerializeField] private bool enablePvP
  - [SerializeField] private GameObject hitEffectPrefab, criticalHitEffectPrefab, missEffectPrefab
  - 컴포넌트 참조들
  - 공격 상태 필드들
- 메서드:
  - public override void OnNetworkSpawn()
  - public void PerformBasicAttack()
  - [ServerRpc] private void PerformAttackServerRpc(Vector2 attackPosition, Vector2 attackDirection, ServerRpcParams rpcParams = default)
  - private List<Collider2D> DetectTargetsInRange(Vector2 attackPosition, Vector2 attackDirection)
  - private bool IsValidTarget(Collider2D target, ulong attackerClientId)
  - private void ProcessAttackOnTarget(Collider2D target, Vector2 attackPosition)
  - private void ApplyDamageToPlayer(), ApplyDamageToMonster()
  - [ClientRpc] private void PlayAttackEffectClientRpc(), ShowDamageEffectClientRpc()
  - public void PerformSkillAttack(string skillId, Vector2 targetPosition)
  - [ServerRpc] private void PerformSkillAttackServerRpc(string skillId, Vector2 targetPosition)
  - private void OnDrawGizmosSelected()

##### 클래스: MonsterHealth
- 상속: MonoBehaviour
- 필드:
  - [SerializeField] private string monsterName, monsterType
  - [SerializeField] private int monsterLevel
  - [SerializeField] private float maxHealth, currentHealth
  - [SerializeField] private long expReward
  - private PlayerController lastAttacker
- 프로퍼티:
  - public float MaxHealth, CurrentHealth
  - public bool IsDead
- 메서드:
  - private void Start()
  - public void TakeDamage(float damage, PlayerController attacker = null)
  - private void Die()
  - private void GiveExperienceReward(), TriggerItemDrop(), TriggerSoulDrop()
  - public void SetMonsterInfo(string name, int level, string type, float health, long exp)
  - public void Heal(float amount)
  - public float GetHealthPercentage()

- 사용하는 타입들: [NetworkBehaviour, LayerMask, GameObject, PlayerController, PlayerStatsManager, MonsterHealth, Collider2D, Physics2D, Vector2, Vector3, DamageType, Color, Debug, Gizmos, ItemDropSystem, SoulDropSystem, Mathf]

### Items/ItemInstance.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: UnityEngine, Unity.Netcode, System

#### 정의된 타입들
##### 클래스: ItemInstance
- 구현: INetworkSerializable, [System.Serializable]
- 필드:
  - [SerializeField] private string itemId, instanceId
  - [SerializeField] private int quantity, currentDurability
  - [SerializeField] private long acquisitionTime
  - [SerializeField] private string[] enchantments
  - private ItemData cachedItemData
- 프로퍼티:
  - 모든 필드에 대한 읽기 전용 프로퍼티들
  - public ItemData ItemData (캐시된 참조)
- 메서드:
  - public ItemInstance() (빈 생성자)
  - public ItemInstance(ItemData itemData, int quantity = 1)
  - public ItemInstance(ItemInstance other) (복사 생성자)
  - public bool IsValid(), CanUse()
  - public bool CanStackWith(ItemInstance other)
  - public bool TryStackWith(ItemInstance other, out int remainingQuantity)
  - public ItemInstance SplitStack(int splitQuantity)
  - public void DecreaseDurability(), RepairDurability()
  - public void Initialize(), ChangeQuantity(), SetQuantity(), AddQuantity()
  - public ItemInstance Clone()
  - public float GetDurabilityPercentage()
  - public bool AddEnchantment(string enchantmentId)
  - public DamageRange GetCurrentWeaponDamage(float strength, float stability)
  - public StatBlock GetCurrentStatBonuses()
  - public string GetDetailedInfoText()
  - public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
  - public override string ToString()

- 사용하는 타입들: [ItemData, ItemDatabase, DamageRange, StatBlock, System.Guid, System.DateTimeOffset, DateTime, Mathf, Unity.Netcode.INetworkSerializable]

### Inventory/InventoryData.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: UnityEngine, Unity.Netcode, System.Collections.Generic, System.Linq

#### 정의된 타입들
##### 클래스: InventoryData
- 구현: INetworkSerializable, [System.Serializable]
- 필드:
  - [SerializeField] private int maxSlots
  - [SerializeField] private bool allowStacking, autoSort
  - private List<InventorySlot> slots
- 이벤트:
  - public System.Action<int, ItemInstance> OnItemAdded, OnItemRemoved
  - public System.Action<int, int> OnItemMoved
  - public System.Action OnInventoryChanged
- 프로퍼티:
  - public int MaxSlots, UsedSlots, EmptySlots
  - public bool IsFull
  - public List<InventorySlot> Slots
- 메서드:
  - public void Initialize(int slotCount = 30)
  - public bool TryAddItem(ItemInstance item, out int slotIndex)
  - public bool RemoveItem(int slotIndex, int quantity = 1)
  - public bool MoveItem(int fromSlot, int toSlot)
  - public int GetItemCount(string itemId)
  - public bool HasItem(string itemId, int requiredQuantity = 1)
  - public int RemoveAllItems(string itemId, int maxRemove = int.MaxValue)
  - public void SortInventory()
  - public InventorySlot GetSlot(int index)
  - public ItemInstance GetItem(int slotIndex)
  - public void LogInventoryInfo()
  - public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter

##### 클래스: InventorySlot
- 구현: [System.Serializable]
- 필드:
  - [SerializeField] private int slotIndex
  - [SerializeField] private ItemInstance item
- 프로퍼티:
  - public int SlotIndex, ItemInstance Item
  - public bool IsEmpty
- 메서드:
  - public InventorySlot(int index)
  - public void SetItem(), Clear()

- 사용하는 타입들: [List, Action, ItemInstance, Debug, Mathf, Linq operations, Unity.Netcode.INetworkSerializable]

### Equipment/EquipmentData.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: UnityEngine, Unity.Netcode, System.Collections.Generic, System.Linq

#### 정의된 타입들
##### 클래스: EquipmentData
- 구현: INetworkSerializable, [System.Serializable]
- 필드:
  - private Dictionary<EquipmentSlot, ItemInstance> equippedItems
  - [SerializeField] private EquipmentSlotData[] equipmentSlots
- 메서드:
  - public void Initialize()
  - public void SetEquippedItem(), GetEquippedItem()
  - public List<ItemInstance> GetAllEquippedItems()
  - public Dictionary<EquipmentSlot, ItemInstance> GetAllEquippedItemsForSave()
  - public void LoadFromSaveData()
  - public bool IsSlotEmpty(), IsItemEquipped()
  - public EquipmentSlot FindItemSlot()
  - public void ClearAllEquipment()
  - public int GetEquippedItemCount()
  - public bool HasWeaponOfCategory()
  - public StatBlock CalculateTotalStatBonus()
  - public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
  - public void LogEquipmentInfo()

- 사용하는 타입들: [Dictionary, List, EquipmentSlot, ItemInstance, EquipmentSlotData, WeaponCategory, StatBlock, System.Enum, Debug, Unity.Netcode.INetworkSerializable]

### AI/MonsterAI.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: UnityEngine, Unity.Netcode, System.Collections.Generic

#### 정의된 타입들
##### 클래스: MonsterAI
- 상속: NetworkBehaviour
- 필드:
  - [SerializeField] AI 설정 필드들 (범위, 속도, 데미지 등)
  - 상태 관련 필드들
  - 컴포넌트 참조들
  - NetworkVariable들
- 프로퍼티:
  - public MonsterAIState CurrentState
  - public PlayerController CurrentTarget
  - public bool HasTarget
- 메서드:
  - public override void OnNetworkSpawn(), OnNetworkDespawn()
  - private void Update()
  - private void UpdateAI()
  - 각 상태별 업데이트 메서드들 (UpdateIdleState, UpdatePatrolState 등)
  - private PlayerController FindNearestPlayer()
  - private void SetTarget(), ChangeState()
  - private void OnStateEnter()
  - private void SetNewPatrolTarget(), MoveTowards(), LookAt()
  - private void PerformAttack(), TriggerAttackAnimation()
  - private void ValidateTarget(), UpdateNetworkSync()
  - 네트워크 콜백 메서드들
  - public void ForceSetTarget(), SetAIType()
  - private void OnDrawGizmosSelected()

##### 열거형: MonsterAIState
- 값: Idle, Patrol, Chase, Attack, Return, Dead

##### 열거형: MonsterAIType
- 값: Passive, Defensive, Aggressive, Territorial

- 사용하는 타입들: [NetworkBehaviour, Rigidbody2D, MonsterHealth, SpriteRenderer, PlayerController, PlayerStatsManager, NetworkVariable, Physics2D, Collider2D, Vector2, Vector3, Color, Mathf, Time, Debug, Gizmos, System.Collections.IEnumerator, WaitForSeconds, DamageType]

### Death/DeathManager.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: UnityEngine, Unity.Netcode, System.Collections

#### 정의된 타입들
##### 클래스: DeathManager
- 상속: NetworkBehaviour
- 필드:
  - [SerializeField] private float deathProcessDelay, itemScatterRadius, itemDespawnTime
  - [SerializeField] private GameObject deathEffectPrefab, itemDropEffectPrefab
  - 컴포넌트 참조들
  - 사망 상태 필드들
- 프로퍼티:
  - public bool IsDead, IsProcessingDeath
- 메서드:
  - public override void OnNetworkSpawn(), OnNetworkDespawn()
  - private void HandlePlayerDeath()
  - [ServerRpc] private void RequestDeathProcessingServerRpc(), ProcessDeathServerRpc()
  - private IEnumerator ProcessDeathSequence()
  - private void DisablePlayerControl(), SaveDeathRecord()
  - [ClientRpc] private void PlayDeathEffectClientRpc(), NotifyDeathCompletedClientRpc()
  - private IEnumerator DeathScreenEffect()
  - private void HandleLocalPlayerDeath()
  - private IEnumerator CleanupDeadPlayer()
  - [ContextMenu("Force Death")] public void ForceDeath()

##### 구조체: DeathInfo
- 구현: [System.Serializable]
- 필드:
  - public string characterId, cause, killerName
  - public int level
  - public Race race
  - public long deathTime
  - public Vector3 deathPosition

- 사용하는 타입들: [NetworkBehaviour, PlayerStatsManager, PlayerController, CharacterDeletion, ItemScatter, SoulPreservation, SoulDropSystem, GameObject, Rigidbody2D, Collider2D, NetworkManager, System.DateTime, Vector3, Race, Debug, JsonUtility, PlayerPrefs, WaitForSeconds, IEnumerator]

### Player/PlayerInput.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: UnityEngine

#### 정의된 타입들
##### 클래스: PlayerInput
- 상속: MonoBehaviour
- 필드:
  - [SerializeField] private KeyCode attackKey, skillKey
  - [SerializeField] private bool debugInput
  - private Vector2 moveInput, mousePosition
  - private bool attackPressed, skillPressed, attackHeld, skillHeld
- 메서드:
  - private void Update()
  - private void HandleInput()
  - public Vector2 GetMoveInput(), GetMousePosition()
  - public bool GetAttackInput(), GetSkillInput()
  - public bool IsAttackHeld(), IsSkillHeld()
  - public void LogInputState()

- 사용하는 타입들: [MonoBehaviour, KeyCode, Vector2, Input, Debug]

### Player/PlayerNetwork.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: Unity.Netcode, UnityEngine

#### 정의된 타입들
##### 클래스: PlayerNetwork
- 상속: NetworkBehaviour
- 필드:
  - [SerializeField] private float positionSyncThreshold, rotationSyncThreshold, interpolationSpeed
  - NetworkVariable들 (위치, 회전, 공격상태, 이동상태)
  - 캐시 필드들
  - 컴포넌트 참조들
- 메서드:
  - public override void OnNetworkSpawn()
  - private void Update()
  - public void UpdatePosition(), UpdateRotation()
  - [ServerRpc] public void TriggerAttackServerRpc()
  - [ClientRpc] private void TriggerAttackClientRpc()
  - private void ResetAttackState(), CanPerformAttack()
  - 네트워크 변수 변경 콜백들
  - private void InterpolatePosition(), InterpolateRotation()
  - public void LogNetworkState()
  - public NetworkPlayerState GetNetworkState()

##### 구조체: NetworkPlayerState
- 구현: [System.Serializable]
- 필드:
  - public Vector2 position
  - public float rotation
  - public bool isAttacking, isMoving

- 사용하는 타입들: [NetworkBehaviour, NetworkVariable, NetworkVariableReadPermission, NetworkVariableWritePermission, Animator, Transform, Vector2, Vector3, Quaternion, Mathf, Time, Debug]

### Stats/PlayerStatsManager.cs
**네임스페이스**: Unity.Template.Multiplayer.NGO.Runtime
**Using 구문**: UnityEngine, Unity.Netcode, System.Collections.Generic

#### 정의된 타입들
##### 클래스: PlayerStatsManager
- 상속: NetworkBehaviour
- 필드:
  - [SerializeField] private PlayerStats defaultStats, currentStats
  - NetworkVariable들 (레벨, HP)
  - 컴포넌트 참조들
- 이벤트:
  - public System.Action<PlayerStats> OnStatsUpdated
  - public System.Action<float, float> OnHealthChanged
  - public System.Action OnPlayerDeath
  - public System.Action<int> OnLevelChanged
- 프로퍼티:
  - public PlayerStats CurrentStats
  - public bool IsDead
- 메서드:
  - public override void OnNetworkSpawn(), OnNetworkDespawn()
  - private void InitializeStats()
  - public void InitializeFromCharacterData(CharacterData characterData)
  - private RaceData GetRaceDataByType(), SetLevel(), SetExperience()
  - public void AddExperience()
  - [ServerRpc] public void AddExperienceServerRpc()
  - public float TakeDamage(), void Heal(), RestoreMP(), ChangeMP(), ChangeGold()
  - public void AddSoulBonusStats()
  - private void ApplyStatsToController(), UpdateNetworkVariables()
  - 네트워크 및 스탯 이벤트 콜백들
  - public StatInfo GetStatInfo()
  - public bool CanLevelUp()
  - public void ResetSoulBonusStats(), LogCurrentStats()
  - public float GetHealthPercentage(), GetManaPercentage()

##### 구조체: StatInfo
- 구현: [System.Serializable]
- 필드:
  - public float baseValue, bonusValue, finalValue
- 메서드:
  - public StatInfo(float baseVal = 0f, float bonusVal = 0f)

- 사용하는 타입들: [NetworkBehaviour, PlayerStats, PlayerController, NetworkVariable, Action, RaceData, Race, CharacterData, RaceDataCreator, StatType, StatBlock, DamageType, System.Reflection, Debug, ScriptableObject]

---

## 타입별 역참조 관계

### 기본 타입들

#### Race 열거형
- 정의 위치: Race/RaceData.cs:123
- 사용되는 곳들:
  - RaceData.cs:14 (필드)
  - PlayerStats.cs:15 (필드)
  - Skills/SkillData.cs:21 (필드)
  - Death/DeathManager.cs:194 (필드)
  - Stats/PlayerStatsManager.cs:89, 115, 143-156 (메서드)
  - Character/* (캐릭터 시스템)
  - 값: Human, Elf, Beast, Machina

#### StatBlock 구조체
- 정의 위치: Race/RaceData.cs:214
- 사용되는 곳들:
  - RaceData.cs:21-24 (필드들)
  - PlayerStats.cs:19, 27, 28 (필드들)
  - Items/ItemData.cs:32 (필드)
  - Shared/WeaponSystem.cs:27 (필드)
  - Skills/SkillData.cs:42 (필드)
  - Equipment/EquipmentData.cs:178 (메서드)
  - 모든 스탯 관련 시스템에서 광범위하게 사용

#### DamageRange 구조체
- 정의 위치: Race/RaceData.cs:135
- 사용되는 곳들:
  - Race/RaceData.cs:192-194 (CombatStats 내부)
  - Items/ItemData.cs:37 (무기 데미지)
  - Shared/WeaponSystem.cs:54, 74 (데미지 계산)
  - Stats/PlayerStats.cs:219-244 (전투 스탯 계산)
  - Items/ItemInstance.cs:322 (현재 무기 데미지)

#### ItemInstance 클래스
- 정의 위치: Items/ItemInstance.cs:12
- 사용되는 곳들:
  - Equipment/EquipmentTypes.cs:32, 42 (장비 데이터)
  - Inventory/InventoryData.cs:24-26 (인벤토리 이벤트)
  - Equipment/EquipmentData.cs:15 (장비 딕셔너리)
  - 모든 아이템 관련 시스템

#### PlayerStats 클래스
- 정의 위치: Stats/PlayerStats.cs:12
- 사용되는 곳들:
  - Stats/PlayerStatsManager.cs:14-15 (필드)
  - Player/PlayerController.cs:380-447 (메서드들)
  - Skills/SkillData.cs:64, 108 (스킬 학습/데미지 계산)
  - Death/DeathManager.cs:191-198 (사망 정보)
  - AI/MonsterAI.cs:304 (몬스터 AI가 플레이어 상태 확인)

### 열거형들

#### ItemGrade 열거형
- 정의 위치: Items/ItemData.cs:316
- 사용되는 곳들:
  - Items/ItemData.cs:21 (필드)
  - Shared/WeaponSystem.cs:16 (필드)
  - Equipment/EquipmentData.cs:272 (디버그)
  - 값: Common=1, Uncommon=2, Rare=3, Epic=4, Legendary=5

#### WeaponType 열거형
- 정의 위치: Shared/WeaponSystem.cs:212
- 사용되는 곳들:
  - Shared/WeaponSystem.cs:14 (필드)
  - Core/ResourceLoader.cs:155, 168-187 (무기 스프라이트)
  - 값: 13개 무기 타입 (Longsword부터 Shield까지)

#### EquipmentSlot 열거형
- 정의 위치: Equipment/EquipmentTypes.cs:10
- 사용되는 곳들:
  - Equipment/EquipmentTypes.cs:32, 41 (데이터 구조체들)
  - Items/ItemData.cs:22 (필드)
  - Equipment/EquipmentData.cs:15, 47-281 (전체 장비 시스템)
  - 값: 12개 장비 슬롯 (None부터 Necklace까지)

#### DamageType 열거형
- 정의 위치: Stats/PlayerStats.cs:503
- 사용되는 곳들:
  - Stats/PlayerStats.cs:39, 302-436 (데미지 계산)
  - Items/ItemData.cs:39 (무기 데미지 타입)
  - Skills/SkillData.cs:30 (스킬 데미지 타입)
  - Combat/CombatSystem.cs:24, 147-225 (전투 시스템)
  - Player/PlayerController.cs:361, 432-449 (플레이어 데미지)
  - AI/MonsterAI.cs:24, 436 (몬스터 공격)
  - 값: Physical, Magical, True

### 네트워크 관련 타입들

#### NetworkBehaviour 상속 클래스들
- Player/PlayerController.cs:10
- Player/PlayerNetwork.cs:10
- Stats/PlayerStatsManager.cs:12
- Combat/CombatSystem.cs:11
- AI/MonsterAI.cs:11
- Death/DeathManager.cs:11

#### INetworkSerializable 구현 클래스들
- Race/RaceData.cs:214 (StatBlock)
- Equipment/EquipmentTypes.cs:39 (EquipmentSlotData)
- Items/ItemInstance.cs:12
- Inventory/InventoryData.cs:13
- Equipment/EquipmentData.cs:12

---

## 컴파일 에러 관련 분석

### 누락된 타입 참조들

#### 1. CharacterData 클래스
- 사용 위치: Stats/PlayerStatsManager.cs:106
- 정의 위치: 없음 (누락)
- 영향받는 메서드: InitializeFromCharacterData

#### 2. RaceDataCreator 클래스
- 사용 위치: Stats/PlayerStatsManager.cs:90, 146-155
- 정의 위치: Race/RaceDataCreator.cs (파일 존재하지만 내용 미확인)
- 영향받는 메서드: 모든 종족 데이터 생성 메서드들

#### 3. ItemDatabase 클래스
- 사용 위치: Items/ItemInstance.cs:43, 225
- 정의 위치: Items/ItemDatabase.cs (파일 존재하지만 내용 미확인)
- 영향받는 메서드: ItemData 캐싱 시스템

#### 4. StatusEffect 구조체
- 사용 위치: Items/ItemData.cs:44, Skills/SkillData.cs:50
- 정의 위치: Skills/SkillData.cs:254에 정의됨
- 상태: 정상

#### 5. WeaponData 클래스
- 사용 위치: Stats/PlayerStats.cs:50, 220-395
- 정의 위치: Shared/WeaponSystem.cs:10에 정의됨
- 상태: 정상

### 순환 참조 문제들

#### 1. PlayerStats ↔ PlayerStatsManager
- PlayerStats가 PlayerStatsManager를 직접적으로는 참조하지 않음
- PlayerStatsManager가 PlayerStats를 관리
- 이벤트 기반으로 통신하므로 순환 참조 없음

#### 2. ItemInstance ↔ ItemData
- ItemInstance가 ItemData를 참조 (정상적인 의존성)
- ItemData가 ItemInstance를 직접 참조하지 않음
- 문제 없음

#### 3. Equipment ↔ Inventory 시스템
- 둘 다 ItemInstance를 사용하지만 서로를 직접 참조하지 않음
- PlayerController가 두 시스템을 모두 사용
- 문제 없음

### 접근성 문제들

#### 1. private 필드 접근
- Stats/PlayerStatsManager.cs:166-183에서 리플렉션 사용
- PlayerStats의 private 필드에 접근
- 잠재적 런타임 오류 가능성

#### 2. internal 메서드 사용
- Core 시스템의 internal 메서드들이 적절히 사용됨
- AssemblyInfo.cs에서 테스트용 접근 허용 설정

#### 3. 네트워크 권한 설정
- NetworkVariable들의 읽기/쓰기 권한이 적절히 설정됨
- 대부분 Owner가 쓰고 Everyone이 읽는 구조

---

## 시스템별 의존성 분석

### Core MVC 시스템
- **의존성**: UnityEngine만 사용
- **피의존성**: 모든 게임플레이 시스템이 의존
- **안정성**: 높음

### 스탯 시스템
- **핵심 타입들**: Race, StatBlock, DamageRange, PlayerStats
- **의존성**: Core, Unity.Netcode, UnityEngine
- **피의존성**: 전투, 장비, 스킬, 플레이어 컨트롤러
- **안정성**: 높음

### 아이템/장비 시스템
- **핵심 타입들**: ItemData, ItemInstance, EquipmentSlot, ItemGrade
- **의존성**: 스탯 시스템, Unity.Netcode
- **피의존성**: 인벤토리, 플레이어, 드롭 시스템
- **안정성**: 중간 (ItemDatabase 누락)

### 전투 시스템
- **핵심 타입들**: CombatSystem, DamageType, MonsterHealth
- **의존성**: 플레이어, 스탯, 네트워크
- **피의존성**: AI, 스킬 시스템
- **안정성**: 높음

### 네트워크 시스템
- **핵심 타입들**: NetworkBehaviour, INetworkSerializable
- **의존성**: Unity.Netcode
- **피의존성**: 거의 모든 게임플레이 시스템
- **안정성**: 높음

---

## 추가 개발 시 고려사항

### 1. 확장성
- 새로운 종족 추가 시: RaceData, RaceDataCreator 수정 필요
- 새로운 아이템 타입 추가 시: ItemType, ItemData, ItemInstance 수정 필요
- 새로운 스킬 추가 시: SkillData, SkillCategory 확장 필요

### 2. 성능 최적화
- ResourceLoader의 캐시 시스템 최적화 필요
- 네트워크 동기화 빈도 조정 고려
- 아이템 인스턴스 풀링 시스템 도입 검토

### 3. 안정성 개선
- 누락된 클래스들(CharacterData, ItemDatabase 등) 구현 필요
- 리플렉션 사용 부분의 대안 모색
- 에러 처리 로직 강화

### 4. 코드 품질
- 일부 클래스의 책임 분리 필요 (특히 PlayerStats)
- 매직 넘버들을 상수화
- 문서화 개선

이 인덱스는 전체 프로젝트의 타입 관계와 의존성을 완전히 분석한 결과입니다. 향후 코드 수정 시 영향 범위를 빠르게 파악할 수 있도록 설계되었습니다.