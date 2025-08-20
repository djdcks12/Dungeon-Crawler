# 하드코어 던전 크롤러 - 완전한 프로젝트 레퍼런스

**버전**: 3.0  
**최종 업데이트**: 2025-08-19  
**엔진**: Unity 6 LTS + Unity Netcode for GameObjects

이 문서는 하드코어 던전 크롤러 프로젝트의 모든 클래스, 함수, 구조체, 열거형에 대한 완전한 레퍼런스 가이드입니다.

---

# 📚 레퍼런스 목차 (라인 번호 기반)

## 🏗️ 프로젝트 구조 (라인 73-104)
- 네임스페이스 구조 (73-76)
- 폴더 구조 (77-104)

## 🎮 핵심 플레이어 시스템 (라인 106-242)
- PlayerController.cs (106-182)
- PlayerStatsManager.cs (183-242)

## 🌐 네트워크 기반 시스템 (라인 243-382)
- NetworkBehaviour 패턴 (243-282)
- NetworkList IEquatable 구현 (283-342)
- RPC 패턴 (343-382)

## ⚔️ 전투 시스템 (라인 383-542)
- CombatSystem.cs (383-452)
- MonsterAI 시스템 (453-512)
- MonsterSpawner (513-542)

## 🎒 아이템 관련 시스템 (라인 543-762)
- ItemData/ItemInstance (543-602)
- InventoryManager (603-662)
- EquipmentManager (663-722)
- 인챈트 시스템 (723-762)

## 🏰 던전 시스템 (라인 763-962)
- DungeonManager (763-862)
- DungeonScheduler (863-912)
- 파티 시스템 (913-962)

## 🧙 캐릭터 시스템 (라인 963-1162)
- 종족 시스템 (963-1022)
- 스킬 시스템 (1023-1082)
- 스탯 시스템 (1083-1142)
- 영혼 시스템 (1143-1162)

## 💀 데스 페널티 시스템 (라인 1163-1262)
- DeathManager (1163-1202)
- ItemScatter (1203-1242)
- CharacterDeletion (1243-1262)

## 🎨 UI 시스템 (라인 1263-1462)
- InventoryUI (1263-1312)
- EquipmentUI (1313-1362)
- StatsUI (1363-1412)
- PartyUI (1413-1462)

## 📊 데이터 구조 (라인 1463-1662)
- 열거형들 (1463-1562)
- 핵심 구조체들 (1563-1662)

## 🔧 유틸리티 (라인 1663-1762)
- 확장 메서드 (1663-1712)
- 헬퍼 클래스 (1713-1762)

---

**📋 사용법**: 필요한 섹션의 라인 번호를 확인하고 `Read(file_path, offset: 시작라인, limit: 줄수)` 형태로 해당 부분만 읽으세요.

---

# 📚 네임스페이스 및 전체 구조

## 📂 메인 네임스페이스
- **`Unity.Template.Multiplayer.NGO.Runtime`**: 모든 게임 클래스가 포함된 메인 네임스페이스

## 📁 프로젝트 폴더 구조
```
Assets/Scripts/Runtime/
├── AI/                  # 몬스터 AI 시스템
├── Character/           # 캐릭터 생성 및 관리
├── Combat/              # 전투 시스템
├── Core/                # 핵심 시스템 및 아키텍처
├── Death/               # 데스 페널티 시스템
├── Dungeon/             # 던전 시스템
├── Enchant/             # 인챈트 시스템
├── Equipment/           # 장비 시스템
├── Game/                # 게임 모델/뷰/컨트롤러
├── Inventory/           # 인벤토리 시스템
├── Items/               # 아이템 시스템
├── Metagame/            # 메타게임 (메뉴 등)
├── Party/               # 파티 시스템
├── Player/              # 플레이어 시스템
├── Race/                # 종족 시스템
├── Shared/              # 공통 시스템
├── Skills/              # 스킬 시스템
├── Soul/                # 영혼 시스템
├── Stats/               # 스탯 시스템
├── UI/                  # UI 시스템
└── UnityGameServices/   # Unity 게임 서비스
```

---

# 🎮 핵심 플레이어 시스템

## PlayerController.cs
**위치**: `Assets/Scripts/Runtime/Player/PlayerController.cs`  
**상속**: `NetworkBehaviour`  
**역할**: 플레이어 조작 및 시스템 통합 허브

### 🔗 주요 컴포넌트 의존성
- `PlayerInput` - 입력 처리
- `PlayerNetwork` - 네트워크 동기화  
- `PlayerStatsManager` - 스탯 관리
- `CombatSystem` - 전투 시스템
- `PlayerVisualManager` - 비주얼 관리
- `DeathManager` - 사망 처리
- `SkillManager` - 스킬 시스템

### 🎯 핵심 메서드

#### `OnNetworkSpawn()`
```csharp
public override void OnNetworkSpawn()
```
- **역할**: 네트워크 스폰 시 모든 컴포넌트 초기화
- **호출**: Unity Netcode 시스템에서 자동 호출
- **기능**: SetupDeathSystem(), InitializeStats() 호출

#### `HandleMovement()`
```csharp
private void HandleMovement()
```
- **역할**: WASD 이동 처리
- **호출**: FixedUpdate()에서 매 프레임 호출 (IsLocalPlayer만)
- **기능**: 대각선 이동 정규화, 스탯 기반 이동속도 적용

#### `HandleRotation()`
```csharp
private void HandleRotation()
```
- **역할**: 마우스 방향 기반 캐릭터 회전
- **호출**: Update()에서 매 프레임 호출
- **기능**: 부드러운 회전, 네트워크 동기화

#### `PerformAttack()`
```csharp
private void PerformAttack()
```
- **역할**: 기본 공격 실행
- **호출**: HandleAttack()에서 좌클릭 시 호출
- **기능**: CombatSystem과 연동, 애니메이션 트리거

#### `ActivateSkill()`
```csharp
private void ActivateSkill()
```
- **역할**: 스킬 사용
- **호출**: HandleSkill()에서 우클릭 시 호출
- **기능**: SkillManager를 통한 첫 번째 학습 스킬 사용

#### `TakeDamage(float damage, DamageType damageType)`
```csharp
public void TakeDamage(float damage, DamageType damageType = DamageType.Physical)
```
- **역할**: 데미지 받기
- **매개변수**: damage (데미지량), damageType (데미지 타입)
- **반환**: void
- **기능**: PlayerStatsManager를 통한 데미지 계산 및 사망 처리

#### `GetAttackDamage()`
```csharp
public float GetAttackDamage()
```
- **역할**: 현재 공격력 계산
- **반환**: float (민댐/맥댐 시스템 기반 데미지)
- **기능**: 새로운 민댐/맥댐 시스템으로 데미지 계산

#### `GetSkillDamage(float minPercent, float maxPercent, DamageType skillType)`
```csharp
public float GetSkillDamage(float minPercent, float maxPercent, DamageType skillType = DamageType.Physical)
```
- **역할**: 스킬 데미지 계산
- **매개변수**: minPercent (민댐 배율), maxPercent (맥댐 배율), skillType (스킬 타입)
- **반환**: float (계산된 스킬 데미지)

### 🔧 시스템 설정 메서드

#### `SetupDeathSystem()`
```csharp
private void SetupDeathSystem()
```
- **역할**: 데스 시스템 관련 컴포넌트들 자동 추가
- **추가 컴포넌트들**:
  - DeathManager
  - CharacterDeletion
  - ItemScatter
  - SoulDropSystem
  - EquipmentManager
  - SkillManager
  - ItemDropSystem
  - InventoryManager
  - EnchantManager

---

## PlayerStatsManager.cs
**위치**: `Assets/Scripts/Runtime/Stats/PlayerStatsManager.cs`  
**상속**: `NetworkBehaviour`  
**역할**: 플레이어 스탯의 네트워크 동기화 관리

### 📊 네트워크 변수
```csharp
private NetworkVariable<int> networkLevel = new NetworkVariable<int>(1);
private NetworkVariable<float> networkCurrentHP = new NetworkVariable<float>(100f);
private NetworkVariable<float> networkMaxHP = new NetworkVariable<float>(100f);
private NetworkVariable<float> networkCurrentMP = new NetworkVariable<float>(50f);
private NetworkVariable<float> networkMaxMP = new NetworkVariable<float>(50f);
private NetworkVariable<long> networkGold = new NetworkVariable<long>(1000);
```

### 🎯 핵심 메서드

#### `InitializeFromCharacterData(CharacterData characterData)`
```csharp
public void InitializeFromCharacterData(CharacterData characterData)
```
- **역할**: 캐릭터 데이터로 초기화
- **매개변수**: characterData (캐릭터 생성 데이터)
- **호출**: 캐릭터 생성 시점

#### `AddExperienceServerRpc(long amount)`
```csharp
[ServerRpc]
public void AddExperienceServerRpc(long amount)
```
- **역할**: 서버에서 경험치 추가
- **매개변수**: amount (추가할 경험치)
- **기능**: 레벨업 처리, 네트워크 동기화

#### `TakeDamage(float damage, DamageType damageType)`
```csharp
public float TakeDamage(float damage, DamageType damageType)
```
- **역할**: 데미지 처리 및 방어력 적용
- **매개변수**: damage (받을 데미지), damageType (데미지 타입)
- **반환**: float (실제 받은 데미지)

#### `ChangeGold(long amount)`
```csharp
public void ChangeGold(long amount)
```
- **역할**: 골드 변경
- **매개변수**: amount (변경할 골드량, 음수 가능)

### 🔔 이벤트 시스템
```csharp
public System.Action<PlayerStats> OnStatsUpdated;
public System.Action OnPlayerDied;
```

---

## PlayerStats.cs
**위치**: `Assets/Scripts/Runtime/Stats/PlayerStats.cs`  
**상속**: `ScriptableObject`  
**역할**: 플레이어 스탯 데이터 및 계산

### 📊 주요 프로퍼티

#### 종족 및 기본 정보
```csharp
public Race CharacterRace => characterRace;
public RaceData RaceData => raceData;
public StatBlock CurrentStats => currentStats;
public string CharacterName => characterName;
```

#### 총 스탯 (종족 + 영혼 + 장비 + 인챈트)
```csharp
public float TotalSTR => currentStats.strength + soulBonusStats.strength + equipmentBonusStats.strength + enchantBonusStats.strength;
public float TotalAGI => currentStats.agility + soulBonusStats.agility + equipmentBonusStats.agility + enchantBonusStats.agility;
public float TotalVIT => currentStats.vitality + soulBonusStats.vitality + equipmentBonusStats.vitality + enchantBonusStats.vitality;
public float TotalINT => currentStats.intelligence + soulBonusStats.intelligence + equipmentBonusStats.intelligence + enchantBonusStats.intelligence;
public float TotalDEF => currentStats.defense + soulBonusStats.defense + equipmentBonusStats.defense + enchantBonusStats.defense;
public float TotalMDEF => currentStats.magicDefense + soulBonusStats.magicDefense + equipmentBonusStats.magicDefense + enchantBonusStats.magicDefense;
public float TotalLUK => currentStats.luck + soulBonusStats.luck + equipmentBonusStats.luck + enchantBonusStats.luck;
public float TotalSTAB => currentStats.stability + soulBonusStats.stability + equipmentBonusStats.stability + enchantBonusStats.stability;
```

### 🎯 핵심 메서드

#### `SetRace(Race race, RaceData data)`
```csharp
public void SetRace(Race race, RaceData data)
```
- **역할**: 종족 설정 (캐릭터 생성 시에만)
- **매개변수**: race (종족), data (종족 데이터)

#### `RecalculateStats()`
```csharp
public void RecalculateStats()
```
- **역할**: 모든 능력치 재계산
- **호출**: 스탯 변경 시마다 자동 호출
- **기능**: 새로운 공식에 따른 HP/MP/공격력/이동속도 등 계산

#### `CalculateAttackDamage(DamageType attackType)`
```csharp
public float CalculateAttackDamage(DamageType attackType = DamageType.Physical)
```
- **역할**: 민댐/맥댐 시스템으로 공격 데미지 계산
- **매개변수**: attackType (공격 타입)
- **반환**: float (계산된 공격 데미지)
- **기능**: 치명타 판정 포함

#### `CalculateSkillDamage(float minDamagePercent, float maxDamagePercent, DamageType skillType)`
```csharp
public float CalculateSkillDamage(float minDamagePercent, float maxDamagePercent, DamageType skillType = DamageType.Physical)
```
- **역할**: 스킬 데미지 계산 (민댐/맥댐 배율 적용)
- **매개변수**: 
  - minDamagePercent (최소 데미지 배율)
  - maxDamagePercent (최대 데미지 배율)
  - skillType (스킬 타입)
- **반환**: float (계산된 스킬 데미지)

#### `TakeDamage(float damage, DamageType damageType)`
```csharp
public float TakeDamage(float damage, DamageType damageType = DamageType.Physical)
```
- **역할**: 방어력 적용한 데미지 계산
- **매개변수**: damage (받을 데미지), damageType (데미지 타입)
- **반환**: float (최종 데미지)
- **기능**: 회피 확률, 방어력 계산 포함

### 📈 스탯 계산 공식

#### 기본 능력치
```csharp
// HP = 100 + (VIT * 10)
maxHP = 100f + (TotalVIT * 10f);

// MP = 50 + (INT * 5)  
maxMP = 50f + (TotalINT * 5f);

// 물리 공격력 = STR * 2
attackDamage = TotalSTR * 2f;

// 마법 공격력 = INT * 2
magicDamage = TotalINT * 2f;

// 이동속도 = 5.0 + (AGI * 0.1)
moveSpeed = 5.0f + (TotalAGI * 0.1f);

// 공격속도 = 1.0 + (AGI * 0.01)
attackSpeed = 1.0f + (TotalAGI * 0.01f);
```

#### 방어 공식
```csharp
// 물리 방어: DEF / (DEF + 100) * 100% 감소
float physicalReduction = TotalDEF / (TotalDEF + 100f);

// 마법 방어: MDEF / (MDEF + 100) * 100% 감소  
float magicalReduction = TotalMDEF / (TotalMDEF + 100f);
```

#### 확률 시스템
```csharp
// 회피율 = AGI * 0.1%
float dodgeRate = TotalAGI * 0.001f;

// 크리티컬 확률 = LUK * 0.05%
criticalChance = TotalLUK * 0.0005f;

// 드롭률 증가 = LUK * 0.01%
float dropRateBonus = TotalLUK * 0.0001f;
```

---

# 🛡️ 종족 시스템

## RaceData.cs
**위치**: `Assets/Scripts/Runtime/Race/RaceData.cs`  
**상속**: `ScriptableObject`  
**역할**: 종족별 스탯 성장 테이블

### 🎯 핵심 메서드

#### `CalculateStatsAtLevel(int level)`
```csharp
public StatBlock CalculateStatsAtLevel(int level)
```
- **역할**: 특정 레벨에서의 스탯 계산
- **매개변수**: level (계산할 레벨)
- **반환**: StatBlock (계산된 스탯)
- **공식**: 기본스탯 + (성장스탯 * (레벨-1))

#### `GetBaseStats()`
```csharp
public StatBlock GetBaseStats()
```
- **역할**: 1레벨 기본 스탯 반환
- **반환**: StatBlock (기본 스탯)

#### `GetGrowthPerLevel()`
```csharp
public StatBlock GetGrowthPerLevel()
```
- **역할**: 레벨당 성장 스탯 반환
- **반환**: StatBlock (성장 스탯)

## RaceDataCreator.cs
**위치**: `Assets/Scripts/Runtime/Race/RaceDataCreator.cs`  
**타입**: `Static Class`  
**역할**: 종족별 RaceData 동적 생성

### 🏭 팩토리 메서드

#### `CreateHumanRaceData()`
```csharp
public static RaceData CreateHumanRaceData()
```
- **반환**: RaceData (인간 종족 데이터)
- **특성**: 균형형 (모든 스탯 10, +1 성장)

#### `CreateElfRaceData()`
```csharp
public static RaceData CreateElfRaceData()
```
- **반환**: RaceData (엘프 종족 데이터)
- **특성**: 마법 특화 (INT 15, INT +2 성장)

#### `CreateBeastRaceData()`
```csharp
public static RaceData CreateBeastRaceData()
```
- **반환**: RaceData (수인 종족 데이터)
- **특성**: 물리 특화 (STR 15, STR +2 성장)

#### `CreateMachinaRaceData()`
```csharp
public static RaceData CreateMachinaRaceData()
```
- **반환**: RaceData (기계족 종족 데이터)
- **특성**: 방어 특화 (VIT/DEF 15, +2 성장)

### 🔢 종족별 특성 수치
```csharp
// 인간 (Human) - 균형형
기본 스탯: 모든 스탯 10
성장: 모든 스탯 +1/레벨

// 엘프 (Elf) - 마법형
기본 스탯: INT 15, 나머지 8  
성장: INT +2/레벨, 나머지 +1/레벨

// 수인 (Beast) - 물리형
기본 스탯: STR 15, 나머지 8
성장: STR +2/레벨, 나머지 +1/레벨

// 기계족 (Machina) - 방어형
기본 스탯: VIT/DEF 15, 나머지 8
성장: VIT/DEF +2/레벨, 나머지 +1/레벨
```

---

# ⚔️ 전투 시스템

## CombatSystem.cs
**위치**: `Assets/Scripts/Runtime/Combat/CombatSystem.cs`  
**상속**: `NetworkBehaviour`  
**역할**: 실제 공격 판정 및 데미지 적용

### 🎯 핵심 메서드

#### `PerformBasicAttack()`
```csharp
public void PerformBasicAttack()
```
- **역할**: 기본 공격 실행
- **호출**: PlayerController.PerformAttack()에서 호출
- **기능**: 서버에서 공격 처리, 범위 내 타겟 탐지

#### `PerformAttackServerRpc()`
```csharp
[ServerRpc]
private void PerformAttackServerRpc()
```
- **역할**: 서버에서 공격 처리
- **기능**: 타겟 탐지, 데미지 계산, 적용

#### `ApplyDamageToTarget(Collider2D target, float damage)`
```csharp
private void ApplyDamageToTarget(Collider2D target, float damage)
```
- **역할**: 타겟에 데미지 적용
- **매개변수**: target (타겟 콜라이더), damage (데미지량)
- **기능**: 몬스터/플레이어 구분하여 데미지 적용

#### `CalculateActualDamage()`
```csharp
private float CalculateActualDamage()
```
- **역할**: 실제 데미지 계산 (민댐/맥댐)
- **반환**: float (계산된 데미지)
- **기능**: PlayerStats의 민댐/맥댐 시스템 사용

### 🎯 공격 범위 및 설정
```csharp
[SerializeField] private float attackRange = 2.0f;      // 공격 사거리
[SerializeField] private float attackAngle = 60f;       // 공격 각도
[SerializeField] private LayerMask targetLayers;        // 타겟 레이어
```

---

# 🤖 몬스터 AI 시스템

## MonsterAI.cs
**위치**: `Assets/Scripts/Runtime/AI/MonsterAI.cs`  
**상속**: `NetworkBehaviour`  
**역할**: 상태 기반 몬스터 AI 시스템

### 🔄 AI 상태 시스템
```csharp
public enum MonsterAIState
{
    Idle,       // 대기
    Patrol,     // 순찰
    Chase,      // 추격
    Attack,     // 공격
    Return,     // 복귀
    Dead        // 사망
}

public enum MonsterAIType
{
    Passive,    // 소극적 (공격받아야 반응)
    Defensive,  // 방어적 (영역 침입 시 반응)
    Aggressive, // 공격적 (시야에 들어오면 즉시 공격)
    Territorial // 영역형 (정해진 영역 벗어나면 복귀)
}
```

### 🎯 핵심 메서드

#### `UpdateAI()`
```csharp
private void UpdateAI()
```
- **역할**: AI 메인 업데이트 루프
- **호출**: Update()에서 매 프레임 호출 (서버만)
- **기능**: 현재 상태에 따른 행동 실행

#### `FindNearestPlayer()`
```csharp
private PlayerController FindNearestPlayer()
```
- **역할**: 가장 가까운 플레이어 탐지
- **반환**: PlayerController (가장 가까운 플레이어)
- **기능**: 탐지 반경 내 살아있는 플레이어 검색

#### `ChangeState(MonsterAIState newState)`
```csharp
public void ChangeState(MonsterAIState newState)
```
- **역할**: AI 상태 변경
- **매개변수**: newState (새로운 상태)
- **기능**: 상태 전환 및 네트워크 동기화

#### `PerformAttack()`
```csharp
private void PerformAttack()
```
- **역할**: 몬스터 공격 실행
- **기능**: 플레이어에게 데미지 적용, 쿨다운 관리

#### `SetAIType(MonsterAIType aiType)`
```csharp
public void SetAIType(MonsterAIType aiType)
```
- **역할**: AI 타입 설정
- **매개변수**: aiType (AI 타입)
- **기능**: 행동 패턴 변경

### 📊 AI 설정 변수
```csharp
[SerializeField] private float detectionRange = 5f;     // 탐지 범위
[SerializeField] private float attackRange = 1.5f;     // 공격 범위
[SerializeField] private float moveSpeed = 3f;         // 이동 속도
[SerializeField] private float attackCooldown = 2f;    // 공격 쿨다운
[SerializeField] private float patrolRadius = 10f;     // 순찰 반경
[SerializeField] private float returnDistance = 15f;   // 복귀 거리
```

## MonsterSpawner.cs
**위치**: `Assets/Scripts/Runtime/AI/MonsterSpawner.cs`  
**상속**: `NetworkBehaviour`  
**역할**: 몬스터 동적 스폰 관리

### 🎯 핵심 메서드

#### `SpawnRandomMonster()`
```csharp
public GameObject SpawnRandomMonster()
```
- **역할**: 랜덤 몬스터 생성
- **반환**: GameObject (생성된 몬스터)
- **기능**: 몬스터 프리팹 랜덤 선택 및 스폰

#### `CalculateMonsterLevel()`
```csharp
private int CalculateMonsterLevel()
```
- **역할**: 플레이어 레벨 기반 몬스터 레벨 조정
- **반환**: int (조정된 몬스터 레벨)
- **공식**: 근처 플레이어 평균 레벨 ± variance

#### `SetupMonster(GameObject monster, int level)`
```csharp
private void SetupMonster(GameObject monster, int level)
```
- **역할**: 몬스터 설정
- **매개변수**: monster (몬스터 오브젝트), level (레벨)
- **기능**: 레벨에 따른 스탯 조정

#### `CleanupDeadMonsters()`
```csharp
private void CleanupDeadMonsters()
```
- **역할**: 죽은 몬스터 정리
- **기능**: 시체 제거, 메모리 정리

### 📊 스폰 설정
```csharp
[SerializeField] private int maxMonsters = 10;           // 최대 몬스터 수
[SerializeField] private float spawnRadius = 20f;       // 스폰 반경
[SerializeField] private float spawnCooldown = 30f;     // 스폰 쿨다운
[SerializeField] private int levelVariance = 2;         // 레벨 편차
```

---

# 🎒 아이템 및 인벤토리 시스템

## ItemData.cs
**위치**: `Assets/Scripts/Runtime/Items/ItemData.cs`  
**상속**: `ScriptableObject`  
**역할**: 5등급 아이템 시스템의 기본 아이템 데이터

### 🏷️ 아이템 분류 시스템
```csharp
public enum ItemType
{
    Equipment,  // 장비 (무기, 방어구)
    Consumable, // 소모품 (포션, 스크롤)
    Material,   // 재료 (제작 재료)
    Quest,      // 퀘스트 아이템
    Other       // 기타
}

public enum ItemGrade
{
    Common = 1,     // 1등급 - 일반 (회색)
    Uncommon = 2,   // 2등급 - 고급 (초록)
    Rare = 3,       // 3등급 - 희귀 (파랑)
    Epic = 4,       // 4등급 - 영웅 (보라)
    Legendary = 5   // 5등급 - 전설 (주황)
}
```

### 🎯 핵심 프로퍼티
```csharp
public string ItemId => itemId;                    // 고유 ID
public string ItemName => itemName;                // 아이템 이름
public ItemGrade Grade => grade;                   // 등급
public ItemType ItemType => itemType;              // 타입
public EquipmentSlot EquipmentSlot => equipmentSlot; // 장착 슬롯
public WeaponCategory WeaponCategory => weaponCategory; // 무기 카테고리
public StatBlock StatBonuses => statBonuses;       // 스탯 보너스
public DamageRange WeaponDamageRange => weaponDamageRange; // 무기 데미지 범위
```

### 🎯 핵심 메서드

#### `GetGradeColor(ItemGrade grade)`
```csharp
public static Color GetGradeColor(ItemGrade grade)
```
- **역할**: 등급별 기본 색상 반환
- **매개변수**: grade (아이템 등급)
- **반환**: Color (등급별 색상)

#### `GetGradeDropRate(ItemGrade grade)`
```csharp
public static float GetGradeDropRate(ItemGrade grade)
```
- **역할**: 등급별 드롭 확률 반환
- **매개변수**: grade (아이템 등급)
- **반환**: float (드롭 확률)
- **확률**: Common 60% → Legendary 1%

#### `GetTotalValue()`
```csharp
public long GetTotalValue()
```
- **역할**: 아이템의 총 가치 계산
- **반환**: long (기본가 × 등급 배수)

#### `CalculateWeaponDamage(float strength, float stability)`
```csharp
public DamageRange CalculateWeaponDamage(float strength, float stability)
```
- **역할**: 무기 데미지 계산 (STR과 안정성 적용)
- **매개변수**: strength (힘 스탯), stability (안정성 스탯)
- **반환**: DamageRange (계산된 데미지 범위)

#### `CanPlayerEquip(Race playerRace)`
```csharp
public bool CanPlayerEquip(Race playerRace)
```
- **역할**: 플레이어가 이 아이템을 착용할 수 있는지 확인
- **매개변수**: playerRace (플레이어 종족)
- **반환**: bool (착용 가능 여부)

### 📊 등급별 드롭률 및 가격 배수
```csharp
Common (일반):     60% 드롭률, 1배 가격
Uncommon (고급):   25% 드롭률, 3배 가격
Rare (희귀):       10% 드롭률, 10배 가격
Epic (영웅):       4% 드롭률, 30배 가격
Legendary (전설):  1% 드롭률, 100배 가격
```

## ItemInstance.cs
**위치**: `Assets/Scripts/Runtime/Items/ItemInstance.cs`  
**상속**: `INetworkSerializable`  
**역할**: 개별 아이템 인스턴스 관리

### 🎯 핵심 프로퍼티
```csharp
public string ItemId { get; set; }              // 아이템 ID
public int Quantity { get; set; }               // 개수
public int CurrentDurability { get; set; }      // 현재 내구도
public string[] Enchantments { get; set; }      // 인챈트 목록
```

### 🎯 핵심 메서드

#### `CanStackWith(ItemInstance other)`
```csharp
public bool CanStackWith(ItemInstance other)
```
- **역할**: 다른 인스턴스와 스택 가능 여부 확인
- **매개변수**: other (다른 아이템 인스턴스)
- **반환**: bool (스택 가능 여부)

#### `SplitStack(int amount)`
```csharp
public ItemInstance SplitStack(int amount)
```
- **역할**: 스택 분할
- **매개변수**: amount (분할할 개수)
- **반환**: ItemInstance (분할된 새 인스턴스)

#### `RepairItem(int amount)`
```csharp
public void RepairItem(int amount)
```
- **역할**: 아이템 수리
- **매개변수**: amount (수리할 내구도)

#### `AddEnchantment(string enchantId)`
```csharp
public bool AddEnchantment(string enchantId)
```
- **역할**: 인챈트 추가
- **매개변수**: enchantId (인챈트 ID)
- **반환**: bool (성공 여부)

## InventoryManager.cs
**위치**: `Assets/Scripts/Runtime/Inventory/InventoryManager.cs`  
**상속**: `NetworkBehaviour`  
**역할**: 플레이어 인벤토리 네트워크 관리

### 🎯 핵심 메서드

#### `AddItemServerRpc(string itemId, int quantity)`
```csharp
[ServerRpc]
public void AddItemServerRpc(string itemId, int quantity)
```
- **역할**: 서버에서 아이템 추가
- **매개변수**: itemId (아이템 ID), quantity (개수)

#### `TryPickupItem(DroppedItem droppedItem)`
```csharp
public bool TryPickupItem(DroppedItem droppedItem)
```
- **역할**: 드롭된 아이템 픽업 시도
- **매개변수**: droppedItem (드롭된 아이템)
- **반환**: bool (픽업 성공 여부)

#### `UseItem(int slotIndex)`
```csharp
public void UseItem(int slotIndex)
```
- **역할**: 아이템 사용 (소모품/장비)
- **매개변수**: slotIndex (슬롯 인덱스)

#### `DropItem(int slotIndex, int quantity)`
```csharp
public void DropItem(int slotIndex, int quantity)
```
- **역할**: 아이템 바닥에 드롭
- **매개변수**: slotIndex (슬롯 인덱스), quantity (드롭할 개수)

### 📊 인벤토리 설정
```csharp
private const int INVENTORY_SIZE = 30;          // 30슬롯
private float autoPickupRange = 2f;             // 자동 픽업 범위
```

## InventoryData.cs
**위치**: `Assets/Scripts/Runtime/Inventory/InventoryData.cs`  
**타입**: `Serializable Class`  
**역할**: 인벤토리 데이터 관리 및 네트워크 직렬화

### 🎯 핵심 메서드

#### `TryAddItem(ItemInstance item, out int remaining)`
```csharp
public bool TryAddItem(ItemInstance item, out int remaining)
```
- **역할**: 아이템 추가 시도
- **매개변수**: item (추가할 아이템), remaining (out: 남은 개수)
- **반환**: bool (추가 성공 여부)

#### `RemoveItem(int slotIndex, int quantity)`
```csharp
public bool RemoveItem(int slotIndex, int quantity)
```
- **역할**: 아이템 제거
- **매개변수**: slotIndex (슬롯 인덱스), quantity (제거할 개수)
- **반환**: bool (제거 성공 여부)

#### `MoveItem(int fromSlot, int toSlot)`
```csharp
public bool MoveItem(int fromSlot, int toSlot)
```
- **역할**: 아이템 이동
- **매개변수**: fromSlot (출발 슬롯), toSlot (도착 슬롯)
- **반환**: bool (이동 성공 여부)

#### `SortInventory()`
```csharp
public void SortInventory()
```
- **역할**: 인벤토리 정렬
- **기능**: 등급별, 타입별 자동 정렬

#### `GetItemCount(string itemId)`
```csharp
public int GetItemCount(string itemId)
```
- **역할**: 특정 아이템 개수 확인
- **매개변수**: itemId (아이템 ID)
- **반환**: int (해당 아이템의 총 개수)

---

# ⚔️ 장비 시스템

## EquipmentManager.cs
**위치**: `Assets/Scripts/Runtime/Equipment/EquipmentManager.cs`  
**상속**: `NetworkBehaviour`  
**역할**: 장비 착용/해제 관리 시스템

### 🛡️ 장비 슬롯 시스템
```csharp
public enum EquipmentSlot
{
    None,
    Head,        // 머리 - 투구, 모자
    Chest,       // 가슴 - 갑옷, 상의
    Legs,        // 다리 - 하의, 바지
    Feet,        // 발 - 신발, 부츠
    Hands,       // 손 - 장갑
    MainHand,    // 주무기 - 검, 둔기, 단검, 지팡이
    OffHand,     // 보조 - 방패, 보조무기
    TwoHand,     // 양손무기 - 활, 대형무기
    Ring1,       // 반지1 - 악세서리
    Ring2,       // 반지2 - 악세서리
    Necklace     // 목걸이 - 악세서리
}
```

### 🎯 핵심 메서드

#### `TryEquipItem(ItemInstance item)`
```csharp
public bool TryEquipItem(ItemInstance item)
```
- **역할**: 아이템 착용 시도
- **매개변수**: item (착용할 아이템)
- **반환**: bool (착용 성공 여부)
- **기능**: 자동 슬롯 탐지, 기존 장비 교체

#### `UnequipItem(EquipmentSlot slot)`
```csharp
public bool UnequipItem(EquipmentSlot slot)
```
- **역할**: 장비 해제
- **매개변수**: slot (해제할 슬롯)
- **반환**: bool (해제 성공 여부)

#### `GetAllEquippedItems()`
```csharp
public List<ItemInstance> GetAllEquippedItems()
```
- **역할**: 모든 착용 장비 반환 (ItemScatter 연동)
- **반환**: List<ItemInstance> (착용 중인 모든 장비)

#### `RecalculateEquipmentStats()`
```csharp
private void RecalculateEquipmentStats()
```
- **역할**: 장비 스탯 보너스 재계산
- **기능**: 모든 장비의 스탯 보너스 합산

#### `GetEquippedItem(EquipmentSlot slot)`
```csharp
public ItemInstance GetEquippedItem(EquipmentSlot slot)
```
- **역할**: 특정 슬롯의 착용 장비 반환
- **매개변수**: slot (조회할 슬롯)
- **반환**: ItemInstance (착용된 장비, 없으면 null)

### 🔧 지능형 착용 시스템
- **자동 슬롯 탐지**: 아이템 타입에 따라 적절한 슬롯 자동 선택
- **기존 장비 교체**: 인벤토리 공간 확인 후 안전한 교체
- **호환성 검사**: 무기 카테고리별 올바른 슬롯 배치
- **중복 착용 방지**: 동일 아이템 중복 착용 차단

## EquipmentData.cs
**위치**: `Assets/Scripts/Runtime/Equipment/EquipmentData.cs`  
**타입**: `Serializable Class`  
**역할**: 장비 데이터 저장 및 네트워크 동기화

### 🎯 핵심 메서드

#### `EquipItem(EquipmentSlot slot, ItemInstance item)`
```csharp
public bool EquipItem(EquipmentSlot slot, ItemInstance item)
```
- **역할**: 특정 슬롯에 아이템 착용
- **매개변수**: slot (착용 슬롯), item (착용할 아이템)
- **반환**: bool (착용 성공 여부)

#### `UnequipItem(EquipmentSlot slot)`
```csharp
public ItemInstance UnequipItem(EquipmentSlot slot)
```
- **역할**: 특정 슬롯의 장비 해제
- **매개변수**: slot (해제할 슬롯)
- **반환**: ItemInstance (해제된 장비)

#### `IsItemEquipped(string itemId)`
```csharp
public bool IsItemEquipped(string itemId)
```
- **역할**: 특정 아이템이 착용 중인지 확인 (중복 착용 방지)
- **매개변수**: itemId (확인할 아이템 ID)
- **반환**: bool (착용 중 여부)

#### `CalculateTotalStatBonus()`
```csharp
public StatBlock CalculateTotalStatBonus()
```
- **역할**: 총 스탯 보너스 계산
- **반환**: StatBlock (모든 장비의 스탯 보너스 합계)

---

# 🔮 스킬 시스템

## SkillManager.cs
**위치**: `Assets/Scripts/Runtime/Skills/SkillManager.cs`  
**상속**: `NetworkBehaviour`  
**역할**: 플레이어의 스킬 학습, 사용, 관리를 담당

### 🎯 핵심 메서드

#### `LearnSkill(string skillId)`
```csharp
public bool LearnSkill(string skillId)
```
- **역할**: 스킬 학습
- **매개변수**: skillId (학습할 스킬 ID)
- **반환**: bool (학습 성공 여부)
- **기능**: 골드 차감, 전제 조건 확인

#### `UseSkill(string skillId, Vector3 targetPosition)`
```csharp
public bool UseSkill(string skillId, Vector3 targetPosition = default)
```
- **역할**: 스킬 사용
- **매개변수**: skillId (사용할 스킬 ID), targetPosition (대상 위치)
- **반환**: bool (사용 성공 여부)
- **기능**: 쿨다운, 마나 확인

#### `GetLearnableSkills()`
```csharp
public List<SkillData> GetLearnableSkills()
```
- **역할**: 학습 가능한 스킬 목록 가져오기
- **반환**: List<SkillData> (학습 가능한 스킬들)
- **조건**: 종족 일치, 레벨 조건, 전제 스킬

#### `GetLearnedSkills()`
```csharp
public List<string> GetLearnedSkills()
```
- **역할**: 학습한 스킬 목록 가져오기
- **반환**: List<string> (학습한 스킬 ID 목록)

#### `IsSkillOnCooldown(string skillId)`
```csharp
public bool IsSkillOnCooldown(string skillId)
```
- **역할**: 스킬이 쿨다운 중인지 확인
- **매개변수**: skillId (확인할 스킬 ID)
- **반환**: bool (쿨다운 중 여부)

#### `ExecuteActiveSkill(SkillData skillData, Vector3 targetPosition)`
```csharp
private void ExecuteActiveSkill(SkillData skillData, Vector3 targetPosition)
```
- **역할**: 액티브 스킬 실행
- **매개변수**: skillData (스킬 데이터), targetPosition (대상 위치)
- **기능**: 데미지/힐링/버프 효과 적용

### 🔄 스킬 타입 시스템
```csharp
public enum SkillType
{
    Active,     // 액티브 스킬 (즉시 사용)
    Passive,    // 패시브 스킬 (영구 효과)
    Toggle      // 토글 스킬 (on/off 전환)
}
```

### 📊 네트워크 동기화
```csharp
private NetworkVariable<SkillListWrapper> networkLearnedSkills = 
    new NetworkVariable<SkillListWrapper>(new SkillListWrapper(), 
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
```

## SkillData.cs
**위치**: `Assets/Scripts/Runtime/Skills/SkillData.cs`  
**상속**: `ScriptableObject`  
**역할**: 스킬 정의 및 설정

### 🎯 핵심 프로퍼티
```csharp
public string skillId;                  // 스킬 고유 ID
public string skillName;                // 스킬 이름
public string description;              // 설명
public SkillType skillType;             // 스킬 타입
public SkillCategory category;          // 스킬 카테고리
public Race requiredRace;               // 필요 종족
public int requiredLevel;               // 필요 레벨
public int skillTier;                   // 스킬 티어 (1-5)
public long goldCost;                   // 골드 비용
public float manaCost;                  // 마나 비용
public float cooldown;                  // 쿨다운 시간
public float range;                     // 사거리
public float baseDamage;                // 기본 데미지
public float minDamagePercent;          // 최소 데미지 배율
public float maxDamagePercent;          // 최대 데미지 배율
public DamageType damageType;           // 데미지 타입
public StatBlock statBonus;             // 스탯 보너스 (패시브)
public string[] prerequisiteSkills;     // 전제 조건 스킬들
```

### 🎯 핵심 메서드

#### `CanLearn(PlayerStats playerStats, List<string> learnedSkills)`
```csharp
public bool CanLearn(PlayerStats playerStats, List<string> learnedSkills)
```
- **역할**: 학습 가능 여부 체크
- **매개변수**: playerStats (플레이어 스탯), learnedSkills (학습한 스킬 목록)
- **반환**: bool (학습 가능 여부)
- **조건 확인**: 종족, 레벨, 골드, 전제 스킬

### 🏷️ 스킬 카테고리 시스템
```csharp
public enum SkillCategory
{
    // 인간 스킬 (4개)
    Warrior, Paladin, Rogue, Archer,
    
    // 엘프 스킬 (5개)
    ElementalMage, PureMage, NatureMage, PsychicMage, Nature,
    
    // 수인 스킬 (8개)
    Berserker, Hunter, Assassin, Beast, Wild, ShapeShift, Hunt, Combat,
    
    // 기계족 스킬 (8개)
    HeavyArmor, Engineer, Artillery, Nanotech, Engineering, Energy, Defense, Hacking,
    
    // 상태이상 관련
    Enhancement, Root, Invisibility
}
```

### 💰 스킬 비용 시스템
```csharp
1티어 (3레벨): 100-200 골드
2티어 (6레벨): 500-800 골드
3티어 (9레벨): 2000-3000 골드
4티어 (12레벨): 8000-15000 골드
5티어 (15레벨): 50000-100000 골드
```

## 종족별 스킬 생성기

### HumanSkills.cs
**위치**: `Assets/Scripts/Runtime/Skills/RaceSkills/HumanSkills.cs`  
**타입**: `Static Class`  
**역할**: 인간 종족 스킬 생성

#### `CreateAllHumanSkills()`
```csharp
public static SkillData[] CreateAllHumanSkills()
```
- **반환**: SkillData[] (모든 인간 스킬)
- **카테고리**: Warrior, Paladin, Rogue, Archer (4개)

### ElfSkills.cs
**위치**: `Assets/Scripts/Runtime/Skills/RaceSkills/ElfSkills.cs`  
**카테고리**: ElementalMage, PureMage, NatureMage, PsychicMage, Nature (5개)

### BeastSkills.cs
**위치**: `Assets/Scripts/Runtime/Skills/RaceSkills/BeastSkills.cs`  
**카테고리**: Berserker, Hunter, Assassin, Beast, Wild, ShapeShift, Hunt, Combat (8개)

### MachinaSkills.cs
**위치**: `Assets/Scripts/Runtime/Skills/RaceSkills/MachinaSkills.cs`  
**카테고리**: HeavyArmor, Engineer, Artillery, Nanotech, Engineering, Energy, Defense, Hacking (8개)

---

# 💀 데스 페널티 시스템

## DeathManager.cs
**위치**: `Assets/Scripts/Runtime/Death/DeathManager.cs`  
**상속**: `NetworkBehaviour`  
**역할**: 플레이어 사망 처리 총괄

### 🎯 핵심 메서드

#### `ProcessDeathSequence()`
```csharp
public void ProcessDeathSequence()
```
- **역할**: 사망 시퀀스 실행
- **호출**: PlayerStatsManager에서 사망 시 호출
- **기능**: 모든 사망 처리 시스템 순차 실행

#### `DisablePlayerControl()`
```csharp
private void DisablePlayerControl()
```
- **역할**: 플레이어 조작 비활성화
- **기능**: 입력 차단, 이동 정지

#### `TriggerDeathServerRpc()`
```csharp
[ServerRpc]
private void TriggerDeathServerRpc()
```
- **역할**: 서버에서 사망 처리
- **기능**: 모든 클라이언트에 사망 알림

#### `PlayDeathEffectClientRpc(Vector3 deathPosition)`
```csharp
[ClientRpc]
private void PlayDeathEffectClientRpc(Vector3 deathPosition)
```
- **역할**: 사망 이펙트 재생
- **매개변수**: deathPosition (사망 위치)

### ⏱️ 사망 시퀀스
```
1. 플레이어 조작 비활성화 (즉시)
2. 사망 애니메이션 재생 (0.5초)
3. 아이템 흩뿌리기 (1초)
4. 영혼 드롭 처리 (1.5초)
5. 캐릭터 삭제 (3초)
6. 캐릭터 생성 화면 전환 (5초)
```

## CharacterDeletion.cs
**위치**: `Assets/Scripts/Runtime/Death/CharacterDeletion.cs`  
**상속**: `MonoBehaviour`  
**역할**: 캐릭터 영구 삭제 처리

### 🎯 핵심 메서드

#### `DeleteCharacterFromAccount()`
```csharp
public void DeleteCharacterFromAccount()
```
- **역할**: 계정에서 캐릭터 삭제
- **기능**: 복구 불가능한 영구 삭제

#### `RemoveCharacterSaveData()`
```csharp
private void RemoveCharacterSaveData()
```
- **역할**: 세이브 데이터 삭제
- **기능**: PlayerPrefs에서 모든 캐릭터 데이터 제거

#### `UpdateGameStatistics()`
```csharp
private void UpdateGameStatistics()
```
- **역할**: 게임 통계 업데이트
- **기능**: 사망 횟수, 생존 시간 등 기록

## ItemScatter.cs
**위치**: `Assets/Scripts/Runtime/Death/ItemScatter.cs`  
**상속**: `MonoBehaviour`  
**역할**: 사망 시 아이템 흩뿌리기

### 🎯 핵심 메서드

#### `ScatterAllItems(Vector3 deathPosition, float scatterRadius)`
```csharp
public void ScatterAllItems(Vector3 deathPosition, float scatterRadius = 5f)
```
- **역할**: 모든 아이템 흩뿌리기 진입점
- **매개변수**: deathPosition (사망 위치), scatterRadius (흩뿌리기 반경)

#### `ScatterGold(Vector3 position, long totalGold)`
```csharp
private void ScatterGold(Vector3 position, long totalGold)
```
- **역할**: 골드를 여러 뭉치로 나누어 드롭
- **매개변수**: position (위치), totalGold (총 골드량)

#### `ScatterInventoryItems(Vector3 position)`
```csharp
private void ScatterInventoryItems(Vector3 position)
```
- **역할**: InventoryManager와 실제 연동하여 인벤토리 아이템 드롭
- **매개변수**: position (위치)

#### `ScatterEquippedItems(Vector3 position)`
```csharp
private void ScatterEquippedItems(Vector3 position)
```
- **역할**: EquipmentManager와 연동하여 착용 장비 드롭
- **매개변수**: position (위치)

#### `CalculateScatterDistance(ItemGrade grade)`
```csharp
private float CalculateScatterDistance(ItemGrade grade)
```
- **역할**: 등급별 흩어짐 거리 계산
- **매개변수**: grade (아이템 등급)
- **반환**: float (흩어짐 거리)
- **공식**: 전설 아이템 = 3배 반경

### 📊 등급별 흩어짐 거리
```csharp
Common:     기본 반경 × 1.0
Uncommon:   기본 반경 × 1.2
Rare:       기본 반경 × 1.5
Epic:       기본 반경 × 2.0
Legendary:  기본 반경 × 3.0
```

## SoulPreservation.cs
**위치**: `Assets/Scripts/Runtime/Death/SoulPreservation.cs`  
**상속**: `MonoBehaviour`  
**역할**: 계정 영혼 보존 관리

### 🎯 핵심 메서드

#### `PreserveSouls()`
```csharp
public void PreserveSouls()
```
- **역할**: 영혼을 계정에 보존
- **기능**: 플레이어가 소유한 모든 영혼을 계정에 저장

#### `CalculateSoulBonus(PlayerStats playerStats)`
```csharp
private StatBlock CalculateSoulBonus(PlayerStats playerStats)
```
- **역할**: 영혼 보너스 계산
- **매개변수**: playerStats (플레이어 스탯)
- **반환**: StatBlock (영혼 보너스 스탯)

#### `SaveSoulToAccount(SoulData soulData)`
```csharp
private void SaveSoulToAccount(SoulData soulData)
```
- **역할**: 영혼 데이터를 계정에 저장
- **매개변수**: soulData (저장할 영혼 데이터)

---

# 👻 영혼 시스템

## SoulDropSystem.cs
**위치**: `Assets/Scripts/Runtime/Soul/SoulDropSystem.cs`  
**상속**: `MonoBehaviour`  
**역할**: 0.1% 확률 영혼 드롭 시스템

### 🎯 핵심 메서드

#### `CheckSoulDrop(Vector3 position, int monsterLevel, string monsterName)`
```csharp
public void CheckSoulDrop(Vector3 position, int monsterLevel, string monsterName)
```
- **역할**: 영혼 드롭 확률 체크
- **매개변수**: position (위치), monsterLevel (몬스터 레벨), monsterName (몬스터 이름)
- **확률**: 0.1% (LUK 보정 적용)

#### `CreateSoulDrop(Vector3 position, int soulLevel, string soulName)`
```csharp
private GameObject CreateSoulDrop(Vector3 position, int soulLevel, string soulName)
```
- **역할**: 영혼 드롭 생성
- **매개변수**: position (생성 위치), soulLevel (영혼 레벨), soulName (영혼 이름)
- **반환**: GameObject (생성된 영혼 오브젝트)

#### `CreatePlayerSoulDrop(Vector3 position, PlayerStats playerStats)`
```csharp
public GameObject CreatePlayerSoulDrop(Vector3 position, PlayerStats playerStats)
```
- **역할**: 플레이어 사망 시 영혼 드롭
- **매개변수**: position (위치), playerStats (플레이어 스탯)
- **반환**: GameObject (플레이어 영혼 오브젝트)
- **확률**: 100% (플레이어는 항상 영혼 드롭)

### 📊 영혼 드롭 확률
```csharp
몬스터 처치: 0.1% (기본) + (LUK × 0.01%)
플레이어 사망: 100% (항상 드롭)
```

## SoulPickup.cs
**위치**: `Assets/Scripts/Runtime/Soul/SoulPickup.cs`  
**상속**: `MonoBehaviour`  
**역할**: 영혼 수집 및 계정 저장

### 🎯 핵심 메서드

#### `CollectSoul(PlayerController player)`
```csharp
public void CollectSoul(PlayerController player)
```
- **역할**: 영혼 수집 처리
- **매개변수**: player (수집하는 플레이어)
- **기능**: 계정에 영혼 추가, 보너스 적용

#### `SaveSoulToAccount(string accountId)`
```csharp
private void SaveSoulToAccount(string accountId)
```
- **역할**: 영혼을 계정에 저장
- **매개변수**: accountId (계정 ID)

#### `OnTriggerEnter2D(Collider2D other)`
```csharp
private void OnTriggerEnter2D(Collider2D other)
```
- **역할**: 자동 수집 트리거
- **매개변수**: other (충돌한 콜라이더)

## SoulGlow.cs & SoulFloatAnimation.cs
**위치**: `Assets/Scripts/Runtime/Soul/SoulGlow.cs`, `SoulFloatAnimation.cs`  
**상속**: `MonoBehaviour`  
**역할**: 영혼 시각적 효과

### SoulGlow 메서드
#### `UpdateGlowEffect()`
```csharp
private void UpdateGlowEffect()
```
- **역할**: 발광 효과 업데이트
- **기능**: 주기적인 발광 강도 변화

#### `SetGlowColor(Color color)`
```csharp
public void SetGlowColor(Color color)
```
- **역할**: 발광 색상 설정
- **매개변수**: color (발광 색상)

### SoulFloatAnimation 메서드
#### `UpdateFloatMotion()`
```csharp
private void UpdateFloatMotion()
```
- **역할**: 부유 애니메이션
- **기능**: 부드러운 상하 움직임

#### `StartCollectionAnimation()`
```csharp
public void StartCollectionAnimation()
```
- **역할**: 수집 애니메이션
- **기능**: 플레이어에게 빨려들어가는 효과

---

# 🏰 던전 시스템

## DungeonTypes.cs
**위치**: `Assets/Scripts/Runtime/Dungeon/DungeonTypes.cs`  
**역할**: 던전 시스템 관련 모든 데이터 구조체 및 열거형 정의

### 🏷️ 던전 관련 열거형

#### `DungeonType`
```csharp
public enum DungeonType
{
    Normal,         // 일반 던전
    Elite,          // 엘리트 던전 (강화된 몬스터)
    Boss,           // 보스 던전 (보스 몬스터 등장)
    Challenge,      // 도전 던전 (특수 규칙)
    PvP             // PvP 던전 (플레이어 대전)
}
```

#### `DungeonDifficulty`
```csharp
public enum DungeonDifficulty
{
    Easy = 1,       // 쉬움 (1-3층 추천)
    Normal = 2,     // 보통 (4-6층 추천) 
    Hard = 3,       // 어려움 (7-9층 추천)
    Nightmare = 4   // 악몽 (10층 추천)
}
```

#### `DungeonState`
```csharp
public enum DungeonState
{
    Waiting,        // 대기 중 (플레이어 입장 대기)
    Active,         // 진행 중
    Completed,      // 완료
    Failed,         // 실패
    Abandoned       // 포기
}
```

### 📊 핵심 데이터 구조체

#### `DungeonInfo` 구조체
```csharp
public struct DungeonInfo : INetworkSerializable, System.IEquatable<DungeonInfo>
{
    public int dungeonId;              // 던전 고유 ID
    public int dungeonNameHash;        // 던전 이름 해시 (네트워크 최적화)
    public DungeonType dungeonType;    // 던전 타입
    public DungeonDifficulty difficulty; // 난이도
    public int currentFloor;           // 현재 층
    public int maxFloors;              // 최대 층수
    public int recommendedLevel;       // 추천 레벨
    public int maxPlayers;             // 최대 플레이어 수
    public float timeLimit;            // 제한 시간 (초)
    public long baseExpReward;         // 기본 경험치 보상
    public long baseGoldReward;        // 기본 골드 보상
}
```

#### `DungeonPlayer` 구조체
```csharp
public struct DungeonPlayer : INetworkSerializable, System.IEquatable<DungeonPlayer>
{
    public ulong clientId;             // 클라이언트 ID
    public int playerNameHash;         // 플레이어 이름 해시
    public int playerLevel;            // 플레이어 레벨
    public Race playerRace;            // 플레이어 종족
    public bool isAlive;               // 생존 여부
    public bool isReady;               // 준비 상태
    public Vector3 spawnPosition;      // 스폰 위치
    public long currentExp;            // 현재 경험치
    public long currentGold;           // 현재 골드
}
```

#### `DungeonFloor` 구조체
```csharp
public struct DungeonFloor : INetworkSerializable, System.IEquatable<DungeonFloor>
{
    public int floorNumber;            // 층 번호
    public int floorNameHash;          // 층 이름 해시
    public Vector2 floorSize;          // 던전 크기 (가로, 세로)
    public int monsterCount;           // 몬스터 수
    public int eliteCount;             // 엘리트 몬스터 수
    public bool hasBoss;               // 보스 존재 여부
    public bool hasExit;               // 출구 존재 여부
    public float completionBonus;      // 완주 보너스 배율
    public Vector3 playerSpawnPoint;   // 플레이어 스폰 지점
    public Vector3 exitPoint;          // 출구 위치
}
```

#### `DungeonReward` 구조체
```csharp
public struct DungeonReward : INetworkSerializable
{
    public long expReward;             // 경험치 보상
    public long goldReward;            // 골드 보상
    public List<ItemInstance> itemRewards; // 아이템 보상
    public float completionTime;       // 완료 시간
    public int floorReached;           // 도달한 층수
    public int monstersKilled;         // 처치한 몬스터 수
    public float survivalRate;         // 생존율
}
```

### 🔧 네트워크 최적화 기법
- **이름 해시화**: string 대신 int 해시값 사용으로 네트워크 트래픽 감소
- **IEquatable 구현**: NetworkList 호환성을 위한 필수 구현
- **효율적 직렬화**: 복잡한 데이터 구조의 최적화된 네트워크 직렬화

---

# 🎉 파티 시스템

## PartyManager.cs
**위치**: `Assets/Scripts/Runtime/Party/PartyManager.cs`  
**상속**: `NetworkBehaviour`  
**역할**: 16명 파티 시스템 관리

### 🎯 핵심 메서드 (구현 예정)

#### `CreateParty(string partyName)`
```csharp
public bool CreateParty(string partyName)
```
- **역할**: 새 파티 생성
- **매개변수**: partyName (파티 이름)
- **반환**: bool (생성 성공 여부)

#### `JoinParty(string partyId)`
```csharp
public bool JoinParty(string partyId)
```
- **역할**: 파티 참가
- **매개변수**: partyId (파티 ID)
- **반환**: bool (참가 성공 여부)

#### `LeaveParty()`
```csharp
public void LeaveParty()
```
- **역할**: 파티 탈퇴

#### `GetPartyMembers()`
```csharp
public List<PartyMember> GetPartyMembers()
```
- **역할**: 파티원 목록 반환
- **반환**: List<PartyMember> (파티원 정보)

### 📊 파티 설정
```csharp
private const int MAX_PARTY_SIZE = 16;          // 최대 16명
private const float PARTY_SPAWN_RADIUS = 10f;   // 파티 스폰 반경
```

---

# 🔮 인챈트 시스템

## EnchantManager.cs
**위치**: `Assets/Scripts/Runtime/Enchant/EnchantManager.cs`  
**상속**: `NetworkBehaviour`  
**역할**: 인챈트 시스템 관리

### 🎯 핵심 메서드 (구현 완료)

#### `ApplyEnchantToWeapon(ItemInstance weapon, EnchantData enchant)`
```csharp
public bool ApplyEnchantToWeapon(ItemInstance weapon, EnchantData enchant)
```
- **역할**: 무기에 인챈트 적용
- **매개변수**: weapon (대상 무기), enchant (인챈트 데이터)
- **반환**: bool (적용 성공 여부)
- **제한**: 최대 3개 인챈트 적용 가능

#### `RemoveEnchantFromWeapon(ItemInstance weapon, int enchantIndex)`
```csharp
public bool RemoveEnchantFromWeapon(ItemInstance weapon, int enchantIndex)
```
- **역할**: 무기에서 인챈트 제거
- **매개변수**: weapon (대상 무기), enchantIndex (제거할 인챈트 인덱스)
- **반환**: bool (제거 성공 여부)

### 🌟 인챈트 타입 시스템
```csharp
public enum EnchantType
{
    Fire,       // 화염 (데미지 증가 + 화상)
    Ice,        // 냉기 (데미지 증가 + 둔화)
    Lightning,  // 번개 (데미지 증가 + 감전)
    Poison,     // 독 (데미지 증가 + 중독)
    Shadow,     // 암흑 (데미지 증가 + 실명)
    Holy,       // 신성 (언데드에게 추가 데미지)
    Critical,   // 치명타 (치명타 확률 증가)
    Stability,  // 안정성 (데미지 안정성 증가)
    Durability, // 내구성 (내구도 감소 방지)
    Lifesteal   // 흡혈 (데미지의 일정 비율 체력 회복)
}
```

## EnchantDropSystem.cs
**위치**: `Assets/Scripts/Runtime/Enchant/EnchantDropSystem.cs`  
**상속**: `MonoBehaviour`  
**역할**: 1% 드롭률 인챈트 드롭 시스템

### 🎯 핵심 메서드

#### `CheckEnchantDrop(Vector3 position, int monsterLevel)`
```csharp
public void CheckEnchantDrop(Vector3 position, int monsterLevel)
```
- **역할**: 인챈트 드롭 확률 체크
- **매개변수**: position (위치), monsterLevel (몬스터 레벨)
- **확률**: 1% (LUK 보정 적용)

#### `CreateEnchantDrop(Vector3 position, EnchantData enchantData)`
```csharp
private GameObject CreateEnchantDrop(Vector3 position, EnchantData enchantData)
```
- **역할**: 인챈트 아이템 드롭 생성
- **매개변수**: position (위치), enchantData (인챈트 데이터)
- **반환**: GameObject (생성된 인챈트 아이템)

### 📊 인챈트 드롭 확률
```csharp
기본 드롭률: 1%
LUK 보정: + (LUK × 0.01%)
층별 보정: 고층일수록 더 높은 등급 인챈트
```

---

# 🖥️ UI 시스템

## InventoryUI.cs
**위치**: `Assets/Scripts/Runtime/Inventory/InventoryUI.cs`  
**상속**: `MonoBehaviour`  
**역할**: 인벤토리 UI 표시 및 상호작용

### 🎯 핵심 메서드

#### `ToggleInventory()`
```csharp
public void ToggleInventory()
```
- **역할**: 인벤토리 열기/닫기 (I키)
- **기능**: UI 활성화/비활성화 토글

#### `CreateSlots()`
```csharp
private void CreateSlots()
```
- **역할**: 슬롯 UI 동적 생성
- **기능**: 30개 슬롯 GridLayout으로 생성

#### `StartDrag(InventorySlotUI slotUI)`
```csharp
public void StartDrag(InventorySlotUI slotUI)
```
- **역할**: 드래그&드롭 시작
- **매개변수**: slotUI (드래그 시작 슬롯)

#### `EndDrag(InventorySlotUI slotUI)`
```csharp
public void EndDrag(InventorySlotUI slotUI)
```
- **역할**: 드래그&드롭 종료
- **매개변수**: slotUI (드롭 대상 슬롯)

### 🖱️ UI 기능
- **드래그&드롭**: 아이템 이동 및 정렬
- **우클릭 메뉴**: 사용/버리기/정보 보기
- **자동 정렬**: 등급별/타입별 정렬
- **검색 기능**: 아이템 이름 검색

## EquipmentUI.cs
**위치**: `Assets/Scripts/Runtime/Equipment/EquipmentUI.cs`  
**상속**: `MonoBehaviour`  
**역할**: 장비창 UI 시스템 (E키로 토글)

### 🎯 핵심 메서드

#### `ToggleEquipment()`
```csharp
public void ToggleEquipment()
```
- **역할**: 장비창 열기/닫기 (E키)

#### `UpdateEquipmentDisplay()`
```csharp
private void UpdateEquipmentDisplay()
```
- **역할**: 장비 정보 실시간 표시
- **기능**: 착용 장비 아이콘, 스탯 보너스 표시

#### `OnSlotClicked(EquipmentSlot slot)`
```csharp
private void OnSlotClicked(EquipmentSlot slot)
```
- **역할**: 장비 슬롯 클릭 처리
- **매개변수**: slot (클릭된 슬롯)
- **기능**: 장비 해제 또는 정보 표시

### 🎨 시각적 기능
- **장비 슬롯별 시각적 피드백**
- **드래그&드롭으로 장착**
- **실시간 스탯 변화 표시**
- **장비 툴팁 정보**

## StatsUI.cs
**위치**: `Assets/Scripts/Runtime/Stats/StatsUI.cs`  
**상속**: `MonoBehaviour`  
**역할**: 플레이어 스탯 UI 표시

### 🎯 핵심 메서드

#### `UpdateStatsDisplay(PlayerStats stats)`
```csharp
public void UpdateStatsDisplay(PlayerStats stats)
```
- **역할**: 스탯 정보 업데이트
- **매개변수**: stats (플레이어 스탯)

#### `ShowDetailedStats()`
```csharp
public void ShowDetailedStats()
```
- **역할**: 상세 스탯 창 표시
- **기능**: 종족별/영혼별 스탯 구분 표시

### 📊 표시 정보
- **기본 스탯**: STR/AGI/VIT/INT/DEF/MDEF/LUK/STAB
- **능력치**: HP/MP/공격력/이동속도 등
- **보너스 구분**: 기본/영혼/장비/인챈트별 표시
- **계산 공식**: 마우스 오버 시 공식 표시

---

# 🔧 공통 시스템 및 구조체

## StatBlock.cs
**위치**: `Assets/Scripts/Runtime/Shared/WeaponSystem.cs` 내부  
**타입**: `Serializable Struct`  
**역할**: 8개 스탯의 표준 구조체

```csharp
[System.Serializable]
public struct StatBlock
{
    public float strength;      // STR - 힘
    public float agility;       // AGI - 민첩
    public float vitality;      // VIT - 체력
    public float intelligence;  // INT - 지능
    public float defense;       // DEF - 물리 방어력
    public float magicDefense;  // MDEF - 마법 방어력
    public float luck;          // LUK - 운
    public float stability;     // STAB - 안정성
}
```

### 🎯 핵심 메서드

#### `HasAnyStats()`
```csharp
public bool HasAnyStats()
```
- **역할**: 0이 아닌 스탯이 있는지 확인
- **반환**: bool (스탯 존재 여부)

#### `GetStatsText()`
```csharp
public string GetStatsText()
```
- **역할**: 스탯 정보를 텍스트로 변환
- **반환**: string (스탯 텍스트)

#### 연산자 오버로딩
```csharp
public static StatBlock operator +(StatBlock a, StatBlock b)
public static StatBlock operator -(StatBlock a, StatBlock b)
public static StatBlock operator *(StatBlock a, float multiplier)
```

## DamageRange.cs
**위치**: `Assets/Scripts/Runtime/Shared/WeaponSystem.cs` 내부  
**타입**: `Serializable Struct`  
**역할**: 민댐/맥댐 시스템의 핵심 구조체

```csharp
[System.Serializable]
public struct DamageRange
{
    public float minDamage;    // 최소 데미지
    public float maxDamage;    // 최대 데미지
    public float stability;    // 안정성 (편차 조절)
}
```

### 🎯 핵심 메서드

#### `GetRandomDamage()`
```csharp
public float GetRandomDamage()
```
- **역할**: 범위 내 랜덤 데미지 반환
- **반환**: float (민댐~맥댐 사이의 랜덤값)

#### `GetStabilizedRange(float stability)`
```csharp
public DamageRange GetStabilizedRange(float stability)
```
- **역할**: 안정성 적용한 범위 조정
- **매개변수**: stability (안정성 스탯)
- **반환**: DamageRange (조정된 데미지 범위)
- **공식**: 
  ```csharp
  실제 민댐 = 기본 민댐 + (STAB * 0.5)
  실제 맥댐 = 기본 맥댐 - (STAB * 0.3)
  ```

## CombatStats.cs
**위치**: `Assets/Scripts/Runtime/Shared/WeaponSystem.cs` 내부  
**타입**: `Serializable Struct`  
**역할**: 전투 관련 모든 스탯 통합

```csharp
[System.Serializable]
public struct CombatStats
{
    public DamageRange physicalDamage;     // 물리 데미지 범위
    public DamageRange magicalDamage;      // 마법 데미지 범위
    public float criticalChance;           // 치명타 확률
    public float criticalMultiplier;       // 치명타 배수
    public float stability;                // 안정성
}
```

## WeaponData.cs
**위치**: `Assets/Scripts/Runtime/Shared/WeaponSystem.cs`  
**상속**: `ScriptableObject`  
**역할**: 무기별 데미지 범위 및 속성 정의

### 🎯 핵심 메서드

#### `CalculateDamageRange(float str, float stab)`
```csharp
public DamageRange CalculateDamageRange(float str, float stab)
```
- **역할**: STR과 안정성 기반 물리 데미지 계산
- **매개변수**: str (힘 스탯), stab (안정성 스탯)
- **반환**: DamageRange (계산된 데미지 범위)

#### `CalculateMagicDamageRange(float int, float stab)`
```csharp
public DamageRange CalculateMagicDamageRange(float intel, float stab)
```
- **역할**: INT와 안정성 기반 마법 데미지 계산
- **매개변수**: intel (지능 스탯), stab (안정성 스탯)
- **반환**: DamageRange (계산된 마법 데미지 범위)

### 🗡️ 무기 카테고리 시스템
```csharp
public enum WeaponCategory
{
    None,       // 무기 없음
    Sword,      // 검류 (균형형: 80-120%)
    Axe,        // 도끼류 (고댐 불안정)
    Bow,        // 활류 (원거리)
    Staff,      // 지팡이류 (마법형: 50-150%)
    Dagger,     // 단검류 (안정형: 90-110%)
    Mace,       // 둔기류 (도박형: 40-160%)
    Wand        // 완드류 (마법 보조)
}
```

### 📊 무기별 민댐/맥댐 특성
```csharp
롱소드: 80-120% (안정적)
워해머: 40-160% (매우 큰 편차)
대거: 90-110% (낮지만 안정적)
롱보우: 70-130% (원거리)
오크 스태프: 75-125% (마법)
크리스탈 스태프: 50-150% (고위 마법)
```

---

# 🗂️ 열거형 및 상수 정의

## 주요 열거형 목록

### Race (종족)
```csharp
public enum Race
{
    Human,      // 인간 - 균형형
    Elf,        // 엘프 - 마법형
    Beast,      // 수인 - 물리형
    Machina     // 기계족 - 방어형
}
```

### DamageType (데미지 타입)
```csharp
public enum DamageType
{
    Physical,   // 물리 데미지
    Magical,    // 마법 데미지
    True,       // 고정 데미지 (방어력 무시)
    Holy        // 신성 데미지 (언데드에게 효과적)
}
```

### PlayerAnimationType (플레이어 애니메이션)
```csharp
public enum PlayerAnimationType
{
    Idle,       // 대기
    Walk,       // 걷기
    Attack,     // 공격
    Skill,      // 스킬 사용
    Death       // 사망
}
```

### StatusType (상태이상)
```csharp
public enum StatusType
{
    Poison,     // 중독
    Burn,       // 화상
    Freeze,     // 동결
    Stun,       // 기절
    Slow,       // 둔화
    Enhancement, // 강화
    Root,       // 속박
    Invisibility // 은신
}
```

## 중요 상수들

### 게임 밸런스 상수
```csharp
// 최대 레벨
public const int MAX_LEVEL = 15;

// 최대 플레이어 수
public const int MAX_PLAYERS = 16;

// 인벤토리 크기
public const int INVENTORY_SIZE = 30;

// 영혼 드롭률
public const float SOUL_DROP_RATE = 0.001f;    // 0.1%

// 인챈트 드롭률
public const float ENCHANT_DROP_RATE = 0.01f;  // 1%

// 최대 인챈트 수
public const int MAX_ENCHANTS_PER_ITEM = 3;
```

### 네트워크 상수
```csharp
// 네트워크 틱율
public const int NETWORK_TICK_RATE = 20;

// 최대 네트워크 메시지 크기
public const int MAX_MESSAGE_SIZE = 1024;

// 서버 타임아웃
public const float SERVER_TIMEOUT = 30f;
```

---

# 🚀 성능 최적화 및 네트워크

## 네트워크 최적화 기법

### 1. 문자열 해시화
```csharp
// 네트워크 트래픽 감소를 위해 문자열 대신 해시값 사용
public int playerNameHash;  // string playerName 대신
public int dungeonNameHash; // string dungeonName 대신
```

### 2. INetworkSerializable 구현
모든 네트워크 전송 구조체는 효율적인 직렬화 구현:
```csharp
public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
{
    serializer.SerializeValue(ref field1);
    serializer.SerializeValue(ref field2);
    // 최소한의 데이터만 직렬화
}
```

### 3. NetworkList IEquatable 패턴
```csharp
public struct NetworkStruct : INetworkSerializable, IEquatable<NetworkStruct>
{
    // IEquatable 구현으로 NetworkList 호환성 확보
    public bool Equals(NetworkStruct other) { /* ... */ }
    public override int GetHashCode() { /* ... */ }
}
```

## 메모리 최적화

### 1. Object Pooling 시스템
```csharp
// 드롭된 아이템, 이펙트 등에 Object Pool 적용 (구현 예정)
public class ObjectPool<T> where T : MonoBehaviour
{
    private Queue<T> pool = new Queue<T>();
    public T Get() { /* ... */ }
    public void Return(T obj) { /* ... */ }
}
```

### 2. 리소스 캐싱
```csharp
// ResourceLoader에서 스프라이트 캐싱
private static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
```

### 3. 메모리 효율적 데이터 구조
- List 대신 Array 사용 (고정 크기)
- string 대신 hash 사용 (네트워크)
- 불필요한 참조 제거

---

# 📖 개발 가이드라인

## 1. 클래스 생성 규칙

### 네이밍 컨벤션
```csharp
// 클래스: PascalCase
public class PlayerController

// 메서드: PascalCase  
public void PerformAttack()

// 필드: camelCase
private float moveSpeed;

// 프로퍼티: PascalCase
public float MoveSpeed => moveSpeed;

// 상수: UPPER_CASE
public const int MAX_LEVEL = 15;
```

### 네트워크 클래스 구조
```csharp
public class MyNetworkClass : NetworkBehaviour
{
    // 1. 설정 변수 (SerializeField)
    [SerializeField] private float configValue;
    
    // 2. 네트워크 변수
    private NetworkVariable<int> networkValue = new NetworkVariable<int>();
    
    // 3. 컴포넌트 참조
    private PlayerController playerController;
    
    // 4. 생명주기 메서드
    public override void OnNetworkSpawn() { }
    
    // 5. 퍼블릭 메서드
    public void PublicMethod() { }
    
    // 6. 서버/클라이언트 RPC
    [ServerRpc]
    private void MyServerRpc() { }
    
    [ClientRpc]
    private void MyClientRpc() { }
    
    // 7. 프라이빗 메서드
    private void PrivateMethod() { }
}
```

## 2. 에러 처리 패턴

### 방어적 프로그래밍
```csharp
// Null 체크
if (component == null)
{
    Debug.LogError($"Component not found on {gameObject.name}");
    return;
}

// 범위 체크
if (index < 0 || index >= array.Length)
{
    Debug.LogWarning($"Index {index} out of range");
    return false;
}

// 상태 체크
if (!IsOwner)
{
    Debug.LogWarning("Only owner can perform this action");
    return;
}
```

### 예외 처리
```csharp
try
{
    var data = JsonUtility.FromJson<SaveData>(jsonString);
    return data;
}
catch (System.Exception e)
{
    Debug.LogError($"Failed to parse save data: {e.Message}");
    return new SaveData();
}
```

## 3. 성능 고려사항

### Update vs FixedUpdate vs 이벤트
```csharp
// 입력 처리: Update
void Update()
{
    if (!IsLocalPlayer) return;
    HandleInput();
}

// 물리 처리: FixedUpdate
void FixedUpdate()
{
    if (!IsLocalPlayer) return;
    HandleMovement();
}

// 상태 변화: 이벤트 기반
public event Action<PlayerStats> OnStatsChanged;
```

### 메모리 할당 최소화
```csharp
// ❌ 매 프레임 새 객체 생성
Vector3 direction = new Vector3(input.x, input.y, 0);

// ✅ 기존 객체 재사용
direction.Set(input.x, input.y, 0);
```

---

# 🔗 시스템 간 연동 맵

## 주요 시스템 의존성

```
PlayerController (허브)
├── PlayerStatsManager (스탯 관리)
│   ├── PlayerStats (데이터)
│   │   ├── RaceData (종족별 성장)
│   │   ├── StatBlock (스탯 구조)
│   │   └── DamageRange (민댐/맥댐)
│   └── CombatSystem (전투)
├── InventoryManager (인벤토리)
│   ├── InventoryData (데이터)
│   ├── ItemInstance (아이템 인스턴스)
│   └── ItemData (아이템 정의)
├── EquipmentManager (장비)
│   ├── EquipmentData (장비 데이터)
│   └── WeaponData (무기 정의)
├── SkillManager (스킬)
│   ├── SkillData (스킬 정의)
│   └── RaceSkills (종족별 스킬)
├── DeathManager (사망 처리)
│   ├── ItemScatter (아이템 흩뿌리기)
│   ├── SoulDropSystem (영혼 드롭)
│   ├── CharacterDeletion (캐릭터 삭제)
│   └── SoulPreservation (영혼 보존)
└── EnchantManager (인챈트)
    ├── EnchantData (인챈트 정의)
    └── EnchantDropSystem (인챈트 드롭)
```

## 데이터 플로우

### 캐릭터 생성 플로우
```
CharacterCreator
  ↓ Race 선택
RaceDataCreator.CreateRaceData()
  ↓ RaceData 생성
PlayerStats.SetRace()
  ↓ 초기 스탯 설정
PlayerStatsManager.InitializeFromCharacterData()
  ↓ 네트워크 동기화
PlayerController.OnNetworkSpawn()
```

### 전투 플로우
```
PlayerController.PerformAttack()
  ↓ 공격 명령
CombatSystem.PerformBasicAttack()
  ↓ 데미지 계산
PlayerStats.CalculateAttackDamage()
  ↓ 민댐/맥댐 적용
DamageRange.GetRandomDamage()
  ↓ 최종 데미지
타겟.TakeDamage()
```

### 아이템 획득 플로우
```
몬스터 처치
  ↓ 드롭 체크
ItemDropSystem.CheckItemDrop()
  ↓ 드롭 생성
DroppedItem.Initialize()
  ↓ 플레이어 접촉
InventoryManager.TryPickupItem()
  ↓ 인벤토리 추가
InventoryData.TryAddItem()
```

### 사망 플로우
```
PlayerStatsManager.TakeDamage() → HP ≤ 0
  ↓ 사망 이벤트
DeathManager.ProcessDeathSequence()
  ↓ 병렬 처리
├── ItemScatter.ScatterAllItems()      // 아이템 흩뿌리기
├── SoulDropSystem.CreatePlayerSoulDrop() // 영혼 드롭
├── SoulPreservation.PreserveSouls()    // 영혼 보존
└── CharacterDeletion.DeleteCharacterFromAccount() // 캐릭터 삭제
```

---

# 📝 이 문서 사용법

## 1. 클래스 찾기
- **Ctrl+F**로 클래스명 검색
- 각 클래스의 **위치**, **상속**, **역할** 확인
- **핵심 메서드** 섹션에서 주요 기능 파악

## 2. 시스템 이해하기
- 각 시스템별로 구성된 섹션 참조
- **의존성 맵**으로 시스템 간 연관관계 파악
- **데이터 플로우**로 처리 순서 이해

## 3. 개발 시 참조
- **개발 가이드라인** 섹션의 코딩 규칙 준수
- **네트워크 최적화** 기법 적용
- **성능 고려사항** 체크

## 4. 디버깅 도구
- 각 클래스의 디버그 메서드 활용
- **LogStats()**, **DrawGizmosSelected()** 등
- 네트워크 동기화 상태 확인

---

이 레퍼런스는 하드코어 던전 크롤러 프로젝트의 모든 구현 내용을 망라한 완전한 가이드입니다. 새로운 기능 개발이나 기존 시스템 수정 시 이 문서를 참조하여 일관성 있는 개발을 진행할 수 있습니다.