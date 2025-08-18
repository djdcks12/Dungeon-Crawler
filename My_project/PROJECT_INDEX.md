# 프로젝트 완전 참조 인덱스

> 클래스별, 함수별 완전 분석 - 단계적 구축

## 📋 작업 진행 상황
- [ ] SkillData.cs 완전 분석
- [ ] MachinaSkills.cs 완전 분석  
- [ ] BeastSkills.cs 완전 분석
- [ ] (추가 파일들은 작업 중 확장)

## 🎯 현재 컴파일 에러 분석
### 첫 번째 에러: SkillCategory.Engineering 누락
**에러 위치**: MachinaSkills.cs:41,43
**원인**: SkillData.cs의 SkillCategory enum에 Engineering 값이 없음
**영향받는 파일들**: MachinaSkills.cs, BeastSkills.cs

---

## 파일별 완전 분석

### SkillData.cs ✅ 완료
**파일 위치**: `Assets/Scripts/Runtime/Skills/SkillData.cs`
**네임스페이스**: `Unity.Template.Multiplayer.NGO.Runtime`
**상속**: `ScriptableObject`

#### 📋 클래스: SkillData
**기능**: 골드 기반 스킬 시스템의 데이터 클래스

##### 필드들
```csharp
[Header("Basic Info")]
public string skillName;           // 스킬 이름
public string skillId;            // 고유 식별자
public string description;        // 설명 (TextArea)
public Sprite skillIcon;         // 아이콘

[Header("Requirements")]
public int requiredLevel = 3;           // 필요 레벨
public long goldCost = 100;            // 골드 비용
public Race requiredRace = Race.Human;  // 필요 종족
public SkillCategory category = SkillCategory.Warrior; // 카테고리
public int skillTier = 1;              // 1-5티어

[Header("Prerequisites")]
public SkillData[] prerequisiteSkills;  // 선행 스킬들

[Header("Skill Effects")]
public SkillType skillType = SkillType.Active;
public DamageType damageType = DamageType.Physical;
public float cooldown = 5f;
public float manaCost = 10f;
public float castTime = 1f;
public float range = 2f;

[Header("Damage/Healing")]
public float baseDamage = 10f;
public float damageScaling = 1f;       // STR/INT 스케일링
public float minDamagePercent = 80f;   // 민댐 %
public float maxDamagePercent = 120f;  // 맥댐 %

[Header("Stat Bonuses (Passive Skills)")]
public StatBlock statBonus;
public float healthBonus = 0f;
public float manaBonus = 0f;
public float moveSpeedBonus = 0f;
public float attackSpeedBonus = 0f;

[Header("Special Effects")]
public StatusEffect[] statusEffects;
public float statusDuration = 5f;
public float statusChance = 100f;

[Header("Visual Effects")]
public GameObject castEffectPrefab;
public GameObject hitEffectPrefab;
public GameObject buffEffectPrefab;
public AudioClip castSound;
public AudioClip hitSound;
```

##### 메서드들
**1. CanLearn(PlayerStats playerStats, List<string> learnedSkills): bool**
- **기능**: 스킬 학습 가능 여부 확인
- **로직**: 레벨, 종족, 골드, 중복 학습, 선행 스킬 확인
- **호출위치**: SkillManager.cs에서 호출됨

**2. CalculateDamage(PlayerStats playerStats): float**
- **기능**: 스킬 데미지 계산
- **로직**: 물리/마법 판별 → STR/INT 기반 스케일링 → 민댐/맥댐 적용
- **호출위치**: CombatSystem.cs에서 사용

#### 📋 열거형: SkillCategory
**현재 정의된 값들**:
```csharp
// 인간
Warrior, Paladin, Rogue, Archer,
// 엘프  
ElementalMage, PureMage, NatureMage, PsychicMage,
// 수인
Berserker, Hunter, Assassin, Beast,
// 기계족
HeavyArmor, Engineer, Artillery, Nanotech
```


#### 📋 열거형: StatusType  
**현재 정의된 값들**:
```csharp
// 디버프
Poison, Burn, Freeze, Stun, Slow, Weakness,
// 버프
Strength, Speed, Regeneration, Shield, Blessing, Berserk, Enhancement
```


#### 📋 사용 관계
**이 클래스를 사용하는 곳들**:
- `SkillManager.cs` → 스킬 학습/관리
- `CombatSystem.cs` → 데미지 계산  
- `MachinaSkills.cs` → 마키나 종족 스킬 생성
- `BeastSkills.cs` → 수인 종족 스킬 생성
- `HumanSkills.cs` → 인간 종족 스킬 생성
- `ElfSkills.cs` → 엘프 종족 스킬 생성

**이 클래스가 의존하는 타입들**:
- `ScriptableObject` (상속)
- `Race` (종족 열거형)
- `PlayerStats` (플레이어 스탯)
- `StatBlock` (스탯 구조체)  
- `DamageType` (데미지 타입)
- `StatusEffect` (상태 효과 구조체)

---

### BeastSkills.cs ✅ 완료
**파일 위치**: `Assets/Scripts/Runtime/Skills/RaceSkills/BeastSkills.cs`
**네임스페이스**: `Unity.Template.Multiplayer.NGO.Runtime`
**타입**: `static class`

#### 📋 클래스: BeastSkills
**기능**: 수인 종족 스킬 생성기 - 야성, 변신, 사냥, 격투

##### 메서드들
**1. CreateAllBeastSkills(): SkillData[]**
- **기능**: 모든 수인 스킬 생성
- **로직**: 4가지 카테고리별 스킬들을 생성하여 배열로 반환
- **반환**: Wild, ShapeShift, Hunt, Combat 스킬 배열

**2. CreateWildSkills(): SkillData[] (private)**
- **기능**: 야성 스킬 생성 (SkillCategory.Wild)
- **스킬들**: 
  - "beast_wild_claw_attack" - 발톱 공격 (1티어, 3레벨)
  - "beast_wild_instinct" - 야생 본능 (1티어, Passive)
  - "beast_wild_frenzy" - 광폭화 (2티어, 6레벨)

**3. CreateShapeShiftSkills(): SkillData[] (private)**
- **기능**: 변신 스킬 생성 (SkillCategory.ShapeShift)
- **스킬들**:
  - "beast_shift_wolf_form" - 늑대 변신 (1티어, Toggle형)

**4. CreateHuntSkills(): SkillData[] (private)**
- **기능**: 사냥 스킬 생성 (SkillCategory.Hunt)
- **스킬들**:
  - "beast_hunt_track" - 추적 (1티어, 범위 15m)

**5. CreateCombatSkills(): SkillData[] (private)**
- **기능**: 격투 스킬 생성 (SkillCategory.Combat)
- **스킬들**:
  - "beast_combat_slam" - 강타 (1티어, 기절 효과)

**6. CreateSkill(...): SkillData (private)**
- **기능**: 스킬 생성 헬퍼 메서드
- **파라미터**: 15개 매개변수 (skillId, name, description 등)
- **특징**: Race.Beast로 고정, ScriptableObject 인스턴스 생성

#### 📋 사용하는 열거형 및 타입
- `SkillCategory.Wild`, `SkillCategory.ShapeShift`, `SkillCategory.Hunt`, `SkillCategory.Combat`
- `SkillType.Active`, `SkillType.Passive`, `SkillType.Toggle`
- `StatusType.Strength`, `StatusType.Speed`, `StatusType.Stun`
- `StatBlock` 구조체 (STR, AGI, VIT 보너스)
- `StatusEffect[]` 배열

#### 📋 스킬 특징 분석
- **종족**: Race.Beast 전용
- **티어 시스템**: 1티어(3레벨), 2티어(6레벨) 
- **골드 비용**: 100~500골드
- **데미지 스케일링**: 1.4~1.6 (높은 물리 계수)
- **민댐/맥댐**: 80%~140% (높은 변동성)
- **상태 효과**: 주로 버프 중심 (Strength, Speed, Stun)

#### 📋 사용 관계
**이 클래스를 사용하는 곳들**:
- 추후 SkillManager.cs에서 수인 스킬 로드 시 호출
- 캐릭터 생성 시 종족별 스킬 초기화

**이 클래스가 의존하는 타입들**:
- `SkillData` (스킬 데이터 클래스)
- `Race.Beast` (수인 종족)
- `ScriptableObject` (인스턴스 생성)

---

### MachinaSkills.cs ✅ 완료
**파일 위치**: `Assets/Scripts/Runtime/Skills/RaceSkills/MachinaSkills.cs`
**네임스페이스**: `Unity.Template.Multiplayer.NGO.Runtime`
**타입**: `static class`

#### 📋 클래스: MachinaSkills
**기능**: 기계족 종족 스킬 생성기 - 공학, 에너지, 방어, 해킹

##### 메서드들
**1. CreateAllMachinaSkills(): SkillData[]**
- **기능**: 모든 기계족 스킬 생성
- **로직**: 4가지 카테고리별 스킬들을 생성하여 배열로 반환
- **반환**: Engineering, Energy, Defense, Hacking 스킬 배열

**2. CreateEngineeringSkills(): SkillData[] (private)**
- **기능**: 공학 스킬 생성 (SkillCategory.Engineering)
- **스킬들**:
  - "machina_eng_turret" - 터렛 설치 (1티어, 3레벨, 자동 공격)
  - "machina_eng_repair" - 자가 수리 (1티어, 체력 회복)
  - "machina_eng_upgrade" - 시스템 업그레이드 (2티어, 6레벨, Enhancement 버프)

**3. CreateEnergySkills(): SkillData[] (private)**
- **기능**: 에너지 스킬 생성 (SkillCategory.Energy)
- **스킬들**:
  - "machina_energy_blast" - 에너지 폭발 (1티어, 마법 데미지)

**4. CreateDefenseSkills(): SkillData[] (private)**
- **기능**: 방어 스킬 생성 (SkillCategory.Defense)
- **스킬들**:
  - "machina_def_barrier" - 에너지 방벽 (1티어, Shield 효과)

**5. CreateHackingSkills(): SkillData[] (private)**
- **기능**: 해킹 스킬 생성 (SkillCategory.Hacking)
- **스킬들**:
  - "machina_hack_disable" - 시스템 무력화 (1티어, Weakness 디버프)

**6. CreateSkill(...): SkillData (private)**
- **기능**: 스킬 생성 헬퍼 메서드
- **파라미터**: 16개 매개변수 (skillId~maxDamagePercent)
- **특징**: Race.Machina로 고정, ScriptableObject 인스턴스 생성

#### 📋 사용하는 열거형 및 타입
- `SkillCategory.Engineering`, `SkillCategory.Energy`, `SkillCategory.Defense`, `SkillCategory.Hacking`
- `SkillType.Active` (기본값)
- `StatusType.Enhancement`, `StatusType.Shield`, `StatusType.Weakness`
- `DamageType.Physical`, `DamageType.Magical`
- `StatusEffect[]` 배열

#### 📋 스킬 특징 분석
- **종족**: Race.Machina 전용
- **티어 시스템**: 1티어(3레벨), 2티어(6레벨)
- **골드 비용**: 120~600골드 (높은 편)
- **데미지 스케일링**: 1.0~1.5 (중간 수준)
- **특화**: 상태 효과 중심 (Shield, Enhancement, Weakness)
- **다양한 데미지 타입**: Physical과 Magical 혼합

#### 📋 사용 관계
**이 클래스를 사용하는 곳들**:
- 추후 SkillManager.cs에서 기계족 스킬 로드 시 호출
- 캐릭터 생성 시 종족별 스킬 초기화

**이 클래스가 의존하는 타입들**:
- `SkillData` (스킬 데이터 클래스)
- `Race.Machina` (기계족)
- `ScriptableObject` (인스턴스 생성)
