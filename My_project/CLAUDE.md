# 하드코어 던전 크롤러 - 프로젝트 개발 인덱스

## 📋 프로젝트 개요
- **게임명**: 하드코어 던전 크롤러  
- **장르**: 2D 탑뷰 로그라이크 액션 RPG
- **엔진**: Unity 6 LTS (Small Size Multi Complete 템플릿)
- **네트워킹**: Unity Netcode for GameObjects
- **최대 인원**: 16명 멀티플레이어

## 🎯 핵심 하드코어 특징
- **완전한 데스 페널티**: 죽으면 캐릭터 삭제, 모든 진행도 소실
- **영혼 시스템**: 유일한 영구 진행도 (0.1% 드롭률)
- **PvP 시스템**: 던전 내 언제든 플레이어 간 전투 가능
- **골드 기반 스킬**: 스킬 포인트 없음, 골드로 스킬 구매
- **극악의 드롭률**: 영혼 0.1%, 인챈트 1%

---

# 📚 시스템별 클래스 인덱스

## 📦 아이템 시스템

### ItemData
- **파일**: `Assets/Scripts/Runtime/Items/ItemData.cs`
- **상속**: `ScriptableObject`
- **핵심 기능**: 5등급 아이템 시스템의 핵심 데이터
- **주요 프로퍼티**:
  - `ItemGrade Grade` - 아이템 등급 (Common~Legendary)
  - `ItemCategory Category` - 아이템 카테고리
  - `WeaponCategory WeaponCategory` - 무기 카테고리
  - `EquipmentSlot EquipmentSlot` - 장착 슬롯
  - `StatBlock StatBonuses` - 스탯 보너스
- **주요 메서드**:
  - `GetTotalValue()` - 아이템 총 가치 계산
  - `GetGradeDropRate(ItemGrade)` - 등급별 드롭률 (1%~60%)
  - `CanPlayerEquip(Race)` - 종족별 착용 가능 여부
  - `GetDamageRange(float, float)` - 무기 데미지 범위 계산

### ItemInstance
- **파일**: `Assets/Scripts/Runtime/Items/ItemInstance.cs`
- **상속**: `INetworkSerializable`
- **핵심 기능**: 개별 아이템 인스턴스 관리
- **주요 프로퍼티**:
  - `string ItemId` - 아이템 ID
  - `int Quantity` - 개수 (스택 가능 아이템)
  - `int CurrentDurability` - 현재 내구도
  - `string[] Enchantments` - 인챈트 목록
- **주요 메서드**:
  - `CanStackWith(ItemInstance)` - 스택 가능 여부
  - `SplitStack(int)` - 스택 분할
  - `RepairItem(int)` - 아이템 수리
  - `AddEnchantment(string)` - 인챈트 추가

### ItemDatabase
- **파일**: `Assets/Scripts/Runtime/Items/ItemDatabase.cs`
- **타입**: `Static Class`
- **핵심 기능**: 아이템 데이터베이스 관리
- **주요 메서드**:
  - `LoadAllItems()` - 모든 아이템 로드
  - `GetItemById(string)` - ID로 아이템 검색
  - `GetItemsByGrade(ItemGrade)` - 등급별 아이템 목록
  - `GetRandomItemDrop(ItemGrade)` - 랜덤 아이템 드롭 생성
- **아이템 인덱싱**: Grade, Category, Slot별 효율적 검색

### ItemDropSystem
- **파일**: `Assets/Scripts/Runtime/Items/ItemDropSystem.cs`
- **상속**: `MonoBehaviour`
- **핵심 기능**: 몬스터 처치 시 아이템 드롭 시스템
- **주요 메서드**:
  - `CheckItemDrop(Vector3, int, string, PlayerController)` - 아이템 드롭 체크
  - `CalculateFinalDropRate(PlayerController)` - LUK 기반 드롭률 계산
  - `SpawnItemDrop(ItemInstance, Vector3)` - 아이템 드롭 생성
  - `PickupItem(DroppedItem, PlayerController)` - 아이템 픽업 처리
- **LUK 보너스**: LUK * 0.1% 드롭률 증가

### DroppedItem
- **파일**: `Assets/Scripts/Runtime/Items/DroppedItem.cs`
- **상속**: `NetworkBehaviour`
- **핵심 기능**: 바닥 아이템 물리적 표현
- **주요 메서드**:
  - `Initialize(ItemInstance, ulong?)` - 아이템으로 초기화
  - `CheckForPlayerPickup()` - 자동 픽업 체크
  - `ManualPickup(PlayerController)` - 수동 픽업
- **시각적 효과**: 등급별 색상, 발광 효과, 회전/부유 애니메이션

---

## ⚔️ 장비 시스템

### EquipmentManager
- **파일**: `Assets/Scripts/Runtime/Equipment/EquipmentManager.cs`
- **상속**: `NetworkBehaviour`
- **핵심 기능**: 11개 장비 슬롯 관리 및 인벤토리 연동
- **주요 메서드**:
  - `TryEquipItem(ItemInstance, bool)` - 아이템 착용 시도
  - `UnequipItem(EquipmentSlot, bool)` - 장비 해제
  - `GetAllEquippedItems()` - 착용 중인 모든 장비 반환
  - `RecalculateEquipmentStats()` - 장비 스탯 재계산
- **연동 시스템**: InventoryManager, PlayerStatsManager, ItemScatter

### EquipmentData
- **파일**: `Assets/Scripts/Runtime/Equipment/EquipmentData.cs`
- **상속**: `INetworkSerializable`
- **핵심 기능**: 장비 착용 데이터 저장 및 네트워크 동기화
- **주요 메서드**:
  - `SetEquippedItem(EquipmentSlot, ItemInstance)` - 장비 착용
  - `GetEquippedItem(EquipmentSlot)` - 착용 장비 조회
  - `CalculateTotalStatBonus()` - 장비 스탯 보너스 계산
  - `IsItemEquipped(ItemInstance)` - 아이템 착용 여부 확인

### EquipmentTypes
- **파일**: `Assets/Scripts/Runtime/Equipment/EquipmentTypes.cs`
- **핵심 기능**: 장비 슬롯 및 데이터 타입 정의
- **장비 슬롯**: Head, Chest, Legs, Feet, Hands, MainHand, OffHand, TwoHand, Ring1, Ring2, Necklace
- **네트워크 지원**: EquipmentSlotData 구조체로 직렬화

### Equipment System 특징
- **11개 장비 슬롯**: 머리, 가슴, 다리, 발, 손, 주무기, 보조무기, 양손무기, 반지1, 반지2, 목걸이
- **지능형 슬롯 배치**: 아이템명과 카테고리 기반 자동 슬롯 결정
- **인벤토리 연동**: 장비 교체 시 자동 인벤토리 이동
- **스탯 적용**: 장비 변경 시 PlayerStatsManager 자동 업데이트
- **종족 제한**: 종족별 장비 착용 제한 지원
- **네트워크 동기화**: 멀티플레이어 장비 상태 실시간 동기화
- **ItemScatter 연동**: 사망 시 착용 장비 자동 드롭

---

## 🤖 몬스터 AI 시스템

### MonsterAI
- **파일**: `Assets/Scripts/Runtime/AI/MonsterAI.cs`
- **상속**: `NetworkBehaviour`
- **핵심 기능**: 상태 기반 몬스터 AI 시스템
- **AI 상태**: Idle, Patrol, Chase, Attack, Return, Dead
- **AI 타입**: Passive, Defensive, Aggressive, Territorial
- **주요 메서드**:
  - `UpdateAI()` - AI 메인 업데이트 루프
  - `FindNearestPlayer()` - 가장 가까운 플레이어 탐지
  - `ChangeState(MonsterAIState)` - 상태 변경
  - `PerformAttack()` - 공격 실행
  - `SetAIType(MonsterAIType)` - AI 타입 설정
- **네트워크 동기화**: 상태 및 위치 동기화

### MonsterSpawner
- **파일**: `Assets/Scripts/Runtime/AI/MonsterSpawner.cs`
- **상속**: `NetworkBehaviour`
- **핵심 기능**: 몬스터 동적 스폰 관리
- **주요 메서드**:
  - `SpawnRandomMonster()` - 랜덤 몬스터 생성
  - `CalculateMonsterLevel()` - 플레이어 레벨 기반 몬스터 레벨 조정
  - `SetupMonster(GameObject, int)` - 몬스터 설정
  - `CleanupDeadMonsters()` - 죽은 몬스터 정리
- **스폰 조건**: 플레이어 근접 시, 최대 몬스터 수 제한
- **레벨 조정**: 근처 플레이어 평균 레벨 ± variance

### MonsterPrefabCreator
- **파일**: `Assets/Scripts/Runtime/AI/MonsterPrefabCreator.cs`
- **상속**: `MonoBehaviour`
- **핵심 기능**: 몬스터 프리팹 생성 도우미
- **주요 메서드**:
  - `SetupMonsterPrefab()` - 몬스터 프리팹 완전 설정
  - `ApplyGoblinPreset()` - 고블린 프리셋 적용
  - `ApplyOrcPreset()` - 오크 프리셋 적용
  - `ApplySlimePreset()` - 슬라임 프리셋 적용
- **프리셋 지원**: Goblin, Orc, Slime, Skeleton

### MonsterHealth (CombatSystem.cs 내)
- **파일**: `Assets/Scripts/Runtime/Combat/CombatSystem.cs`
- **상속**: `MonoBehaviour`
- **핵심 기능**: 몬스터 체력 및 보상 관리
- **주요 메서드**:
  - `TakeDamage(float, PlayerController)` - 데미지 처리 및 공격자 추적
  - `Die()` - 사망 처리 및 보상 지급
  - `GiveExperienceReward()` - 경험치 보상
  - `TriggerItemDrop()` - 아이템 드롭 트리거
  - `SetMonsterInfo(string, int, string, float, long)` - 몬스터 정보 설정

---

## 🎒 인벤토리 시스템

### InventoryData
- **파일**: `Assets/Scripts/Runtime/Inventory/InventoryData.cs`
- **타입**: `Serializable Class`
- **핵심 기능**: 인벤토리 데이터 관리 및 네트워크 직렬화
- **주요 메서드**:
  - `TryAddItem(ItemInstance, out int)` - 아이템 추가 시도
  - `RemoveItem(int, int)` - 아이템 제거
  - `MoveItem(int, int)` - 아이템 이동
  - `SortInventory()` - 인벤토리 정렬
  - `GetItemCount(string)` - 특정 아이템 개수 확인
- **스택 시스템**: 같은 아이템 자동 스택, 최대 스택 크기 지원

### InventoryManager
- **파일**: `Assets/Scripts/Runtime/Inventory/InventoryManager.cs`
- **상속**: `NetworkBehaviour`
- **핵심 기능**: 플레이어 인벤토리 네트워크 관리
- **주요 메서드**:
  - `AddItemServerRpc(string, int)` - 서버에서 아이템 추가
  - `UseItem(int)` - 아이템 사용 (소모품/장비)
  - `DropItem(int, int)` - 아이템 바닥에 드롭
  - `TryPickupItem(DroppedItem)` - 드롭된 아이템 픽업
- **자동 픽업**: 설정 가능한 픽업 범위 내 아이템 자동 수집
- **네트워크 동기화**: 인벤토리 상태 실시간 동기화

### InventoryUI
- **파일**: `Assets/Scripts/Runtime/Inventory/InventoryUI.cs`
- **상속**: `MonoBehaviour`
- **핵심 기능**: 인벤토리 UI 표시 및 상호작용
- **주요 메서드**:
  - `ToggleInventory()` - 인벤토리 열기/닫기 (I키)
  - `CreateSlots()` - 슬롯 UI 동적 생성
  - `StartDrag(InventorySlotUI)` - 드래그&드롭 시작
  - `EndDrag(InventorySlotUI)` - 드래그&드롭 종료
- **UI 기능**: 그리드 레이아웃, 드래그&드롭, 툴팁, 정렬

### InventorySlotUI
- **파일**: `Assets/Scripts/Runtime/Inventory/InventorySlotUI.cs`
- **상속**: `MonoBehaviour`
- **핵심 기능**: 개별 슬롯 UI 및 이벤트 처리
- **이벤트 인터페이스**: IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
- **주요 기능**:
  - 좌클릭/우클릭 구분 처리
  - 드래그&드롭 비주얼 피드백
  - 등급별 색상 프레임
  - 수량 표시, 내구도 상태 표시
- **시각적 효과**: 하이라이트, 드래그 오버, 등급별 프레임

---

## 🎮 플레이어 시스템

### PlayerController
- **파일**: `Assets/Scripts/Runtime/Player/PlayerController.cs`
- **상속**: `NetworkBehaviour`
- **핵심 기능**: WASD 이동, 마우스 회전, 좌클릭 공격, 우클릭 스킬
- **주요 메서드**:
  - `HandleMovement()` - WASD 이동 처리
  - `HandleRotation()` - 마우스 기반 회전
  - `PerformAttack()` - 기본 공격 실행
  - `ActivateSkill()` - 스킬 시스템 활성화
  - `TakeDamage(float, DamageType)` - 데미지 받기
  - `SetupDeathSystem()` - Death 시스템 컴포넌트 자동 추가
- **연동 컴포넌트**: PlayerInput, PlayerNetwork, PlayerStatsManager, CombatSystem, PlayerVisualManager, DeathManager, SkillManager

### PlayerStatsManager
- **파일**: `Assets/Scripts/Runtime/Stats/PlayerStatsManager.cs`
- **상속**: `NetworkBehaviour`
- **핵심 기능**: 플레이어 스탯 관리, 네트워크 동기화
- **주요 메서드**:
  - `InitializeFromCharacterData(CharacterData)` - 캐릭터 데이터로 초기화
  - `AddExperience(long)` - 경험치 추가
  - `TakeDamage(float, DamageType)` - 데미지 처리
  - `ChangeGold(long)` - 골드 변경
  - `AddSoulBonusStats(StatBlock)` - 영혼 보너스 스탯 추가
- **네트워크 변수**:
  - `NetworkVariable<int> networkLevel` - 레벨 동기화
  - `NetworkVariable<float> networkCurrentHP` - HP 동기화
  - `NetworkVariable<float> networkMaxHP` - 최대 HP 동기화

### PlayerStats
- **파일**: `Assets/Scripts/Runtime/Stats/PlayerStats.cs`
- **상속**: `ScriptableObject`
- **핵심 기능**: 종족별 고정 성장, 스탯 계산
- **주요 프로퍼티**:
  - `Race CharacterRace` - 캐릭터 종족
  - `long Gold` - 보유 골드
  - `float TotalSTR/AGI/VIT/INT/DEF/MDEF/LUK/STAB` - 총 스탯 (기본+영혼+장비)
- **주요 메서드**:
  - `SetRace(Race, RaceData)` - 종족 설정
  - `AddExperience(long)` - 경험치 추가 및 레벨업
  - `CalculateAttackDamage(DamageType)` - 민댐/맥댐 공격 데미지 계산
  - `CalculateSkillDamage(float, float, DamageType)` - 스킬 데미지 계산
  - `TakeDamage(float, DamageType)` - 방어력 적용한 데미지 계산
  - `ChangeGold(long)` - 골드 변경
  - `ChangeHP/MP(float)` - HP/MP 변경

### PlayerVisualManager
- **파일**: `Assets/Scripts/Runtime/Player/PlayerVisualManager.cs`
- **상속**: `MonoBehaviour`
- **핵심 기능**: 종족별 스프라이트, 애니메이션 관리
- **주요 메서드**:
  - `SetRace(Race)` - 종족별 스프라이트 설정
  - `SetAnimation(PlayerAnimationType)` - 애니메이션 상태 변경
  - `SetDirectionFromMouse(Vector2)` - 마우스 방향에 따른 스프라이트 설정
  - `TriggerAttackAnimation()` - 공격 애니메이션 트리거

---

## ⚔️ 전투 시스템

### CombatSystem
- **파일**: `Assets/Scripts/Runtime/Combat/CombatSystem.cs`
- **상속**: `NetworkBehaviour`
- **핵심 기능**: 실제 공격 판정 및 데미지 적용
- **주요 메서드**:
  - `PerformBasicAttack()` - 기본 공격 실행
  - `PerformAttackServerRpc()` - 서버에서 공격 처리
  - `ApplyDamageToTarget(Collider2D, float)` - 타겟에 데미지 적용
  - `CalculateActualDamage()` - 실제 데미지 계산 (민댐/맥댐)

### DamageRange
- **파일**: `Assets/Scripts/Runtime/Shared/WeaponSystem.cs`
- **타입**: `Struct`
- **핵심 기능**: 민댐/맥댐 시스템의 핵심 구조체
- **주요 메서드**:
  - `GetRandomDamage()` - 범위 내 랜덤 데미지 반환
  - `GetStabilizedRange(float stability)` - 안정성 적용한 범위 조정
- **프로퍼티**: `minDamage`, `maxDamage`, `stability`

### WeaponData
- **파일**: `Assets/Scripts/Runtime/Shared/WeaponSystem.cs`
- **상속**: `ScriptableObject`
- **핵심 기능**: 무기별 데미지 범위 및 속성 정의
- **주요 메서드**:
  - `CalculateDamageRange(float str, float stab)` - STR과 안정성 기반 데미지 계산
  - `CalculateMagicDamageRange(float int, float stab)` - 마법 데미지 계산
- **프로퍼티**: `WeaponCategory`, `ItemGrade`, `StatBlock StatBonuses`

---

## 🧬 종족 시스템

### RaceData
- **파일**: `Assets/Scripts/Runtime/Race/RaceData.cs`
- **상속**: `ScriptableObject`
- **핵심 기능**: 종족별 스탯 성장 테이블
- **주요 메서드**:
  - `CalculateStatsAtLevel(int level)` - 레벨별 스탯 계산
  - `GetBaseStats()` - 기본 스탯 반환
  - `GetGrowthPerLevel()` - 레벨당 성장치 반환

### RaceDataCreator
- **파일**: `Assets/Scripts/Runtime/Race/RaceDataCreator.cs`
- **타입**: `Static Class`
- **핵심 기능**: 종족별 RaceData 생성
- **주요 메서드**:
  - `CreateHumanRaceData()` - 인간 종족 데이터 생성
  - `CreateElfRaceData()` - 엘프 종족 데이터 생성
  - `CreateBeastRaceData()` - 수인 종족 데이터 생성
  - `CreateMachinaRaceData()` - 기계족 종족 데이터 생성

### 종족별 특성 요약
- **인간**: 균형형 (모든 스탯 10, +1 성장)
- **엘프**: 마법 특화 (INT 15, INT +2 성장)
- **수인**: 물리 특화 (STR 15, STR +2 성장)  
- **기계족**: 방어 특화 (VIT/DEF 15, +2 성장)

---

## 💀 데스 페널티 시스템

### DeathManager
- **파일**: `Assets/Scripts/Runtime/Death/DeathManager.cs`
- **상속**: `NetworkBehaviour`
- **핵심 기능**: 플레이어 사망 처리 총괄
- **주요 메서드**:
  - `ProcessDeathSequence()` - 사망 시퀀스 실행
  - `DisablePlayerControl()` - 플레이어 조작 비활성화
  - `TriggerDeathServerRpc()` - 서버에서 사망 처리
  - `PlayDeathEffectClientRpc(Vector3)` - 사망 이펙트 재생

### CharacterDeletion
- **파일**: `Assets/Scripts/Runtime/Death/CharacterDeletion.cs`
- **상속**: `MonoBehaviour`
- **핵심 기능**: 캐릭터 영구 삭제 처리
- **주요 메서드**:
  - `DeleteCharacterFromAccount()` - 계정에서 캐릭터 삭제
  - `RemoveCharacterSaveData()` - 세이브 데이터 삭제
  - `UpdateGameStatistics()` - 게임 통계 업데이트

### ItemScatter
- **파일**: `Assets/Scripts/Runtime/Death/ItemScatter.cs`
- **상속**: `MonoBehaviour`
- **핵심 기능**: 사망 시 아이템 흩뿌리기
- **주요 메서드**:
  - `ScatterAllItems(Vector3, float)` - 모든 아이템 드롭
  - `CreateItemDrop(Vector3, ItemData)` - 개별 아이템 드롭 생성
  - `CalculateScatterDistance(ItemGrade)` - 등급별 흩어짐 거리 계산

### SoulPreservation
- **파일**: `Assets/Scripts/Runtime/Death/SoulPreservation.cs`
- **상속**: `MonoBehaviour`
- **핵심 기능**: 계정 영혼 보존 관리
- **주요 메서드**:
  - `PreserveSouls()` - 영혼을 계정에 보존
  - `CalculateSoulBonus(PlayerStats)` - 영혼 보너스 계산
  - `SaveSoulToAccount(SoulData)` - 영혼 데이터 저장

---

## 👻 영혼 시스템

### SoulDropSystem
- **파일**: `Assets/Scripts/Runtime/Soul/SoulDropSystem.cs`
- **상속**: `MonoBehaviour`
- **핵심 기능**: 0.1% 확률 영혼 드롭 시스템
- **주요 메서드**:
  - `CheckSoulDrop(Vector3, int, string)` - 영혼 드롭 확률 체크
  - `CreateSoulDrop(Vector3, int, string)` - 영혼 드롭 생성
  - `CreatePlayerSoulDrop(Vector3, PlayerStats)` - 플레이어 사망 시 영혼 드롭
- **드롭 확률**: 몬스터 0.1%, 플레이어 100%

### SoulPickup
- **파일**: `Assets/Scripts/Runtime/Soul/SoulPickup.cs`
- **상속**: `MonoBehaviour`
- **핵심 기능**: 영혼 수집 및 계정 저장
- **주요 메서드**:
  - `CollectSoul(PlayerController)` - 영혼 수집 처리
  - `SaveSoulToAccount(string)` - 영혼을 계정에 저장
  - `OnTriggerEnter2D(Collider2D)` - 자동 수집 트리거

### SoulGlow & SoulFloatAnimation
- **파일**: `Assets/Scripts/Runtime/Soul/SoulGlow.cs`, `SoulFloatAnimation.cs`
- **상속**: `MonoBehaviour`
- **핵심 기능**: 영혼 시각적 효과
- **SoulGlow 메서드**:
  - `UpdateGlowEffect()` - 발광 효과 업데이트
  - `SetGlowColor(Color)` - 발광 색상 설정
- **SoulFloatAnimation 메서드**:
  - `UpdateFloatMotion()` - 부유 애니메이션
  - `StartCollectionAnimation()` - 수집 애니메이션

---

## 🎯 스킬 시스템

### SkillManager
- **파일**: `Assets/Scripts/Runtime/Skills/SkillManager.cs`
- **상속**: `NetworkBehaviour`
- **핵심 기능**: 플레이어 스킬 학습, 사용, 관리
- **주요 메서드**:
  - `LearnSkill(string skillId)` - 스킬 학습
  - `UseSkill(string skillId, Vector3 targetPosition)` - 스킬 사용
  - `GetLearnableSkills()` - 학습 가능한 스킬 목록
  - `LoadAvailableSkills()` - 종족별 스킬 로드
  - `ExecuteActiveSkill(SkillData, Vector3)` - 액티브 스킬 실행
- **네트워크**: `NetworkVariable<SkillListWrapper>` - 학습한 스킬 동기화

### SkillData
- **파일**: `Assets/Scripts/Runtime/Skills/SkillData.cs`
- **상속**: `ScriptableObject`
- **핵심 기능**: 스킬 정의 및 설정
- **주요 프로퍼티**:
  - `string skillId` - 스킬 고유 ID
  - `long goldCost` - 골드 비용
  - `Race requiredRace` - 필요 종족
  - `SkillCategory category` - 스킬 카테고리
  - `int skillTier` - 스킬 티어 (1-5)
- **주요 메서드**:
  - `CanLearn(PlayerStats, List<string>)` - 학습 가능 여부 체크

### SkillMaster
- **파일**: `Assets/Scripts/Runtime/Skills/SkillMaster.cs`
- **상속**: `NetworkBehaviour`
- **핵심 기능**: 스킬 가르치는 NPC
- **주요 메서드**:
  - `TeachSkill(PlayerController, string)` - 스킬 가르치기
  - `GetTeachableSkills(PlayerController)` - 가르칠 수 있는 스킬 목록
  - `OnPlayerEnterRange(PlayerController)` - 플레이어 상호작용 시작
- **설정 가능**: `Race supportedRace`, `SkillCategory[] teachableCategories`

### 종족별 스킬 생성기
- **HumanSkills**: `Assets/Scripts/Runtime/Skills/RaceSkills/HumanSkills.cs`
  - 카테고리: Warrior, Paladin, Rogue, Archer
  - `CreateAllHumanSkills()` - 모든 인간 스킬 생성
- **ElfSkills**: `Assets/Scripts/Runtime/Skills/RaceSkills/ElfSkills.cs`
  - 카테고리: Nature, Archery, Stealth, Spirit
- **BeastSkills**: `Assets/Scripts/Runtime/Skills/RaceSkills/BeastSkills.cs`
  - 카테고리: Wild, ShapeShift, Hunt, Combat
- **MachinaSkills**: `Assets/Scripts/Runtime/Skills/RaceSkills/MachinaSkills.cs`
  - 카테고리: Engineering, Energy, Defense, Hacking

---

## 🎨 비주얼 시스템

### ResourceLoader
- **파일**: `Assets/Scripts/Runtime/Core/ResourceLoader.cs`
- **타입**: `Static Class`
- **핵심 기능**: Resources 폴더 스프라이트 자동 로드
- **주요 메서드**:
  - `LoadCharacterSprites(Race)` - 종족별 캐릭터 스프라이트 로드
  - `LoadMonsterSprites(MonsterType)` - 몬스터 스프라이트 로드
  - `LoadWeaponSprites(WeaponCategory)` - 무기 스프라이트 로드
  - `GetSpriteByDirection(Dictionary, Direction)` - 방향별 스프라이트 반환

### CharacterSprite 경로 규칙
```
Resources/Characters/Body_A/{Race}/
├── {Race}_Up.png
├── {Race}_Down.png  
├── {Race}_Side.png
└── {Race}_Idle.png
```

### MonsterSprite 경로 규칙
```
Resources/Entities/Mobs/{MonsterType}/
├── {Monster}_Up.png
├── {Monster}_Down.png
└── {Monster}_Side.png
```

---

## ⚔️ ItemScatter 시스템 통합 완료 (2025-01-18)

### 주요 변경 사항
- **ItemScatter.cs**: 실제 `ItemInstance` 객체와 연동하도록 완전 리팩토링
- **WeaponSystem.cs**: `ItemRarity` → `ItemGrade` 통합, `WeaponCategory.None` 추가
- **ItemDrop.cs**: 새로 생성 - 드롭된 아이템의 완전한 관리 시스템
- **GoldDrop.cs**: 새로 생성 - 골드 드롭의 자석 효과 및 픽업 시스템

### 통합된 시스템 구조
```
ItemScatter System (사망 시 아이템 흩뿌리기)
├── ScatterAllItems() - 모든 아이템 흩뿌리기 진입점
├── ScatterGold() - 골드를 여러 뭉치로 나누어 드롭
├── ScatterEquippedItems() - EquipmentManager와 연동 (향후 구현)
├── ScatterInventoryItems() - InventoryManager와 실제 연동
└── 등급별 흩뿌리기 반경 적용 (전설 아이템 = 3배 반경)

ItemDrop System
├── SetItemInstance() - 실제 ItemInstance 객체 사용
├── 등급별 색상 오버레이 (Common=흰색, Legendary=노란색)
├── 자동 픽업 감지 (근접 시)
└── 떠다니는 애니메이션

GoldDrop System
├── 골드량에 따른 크기/색상 변화
├── 자석 효과 (5m 범위에서 플레이어에게 이동)
├── 네트워크 동기화된 골드량
└── 즉시 픽업 시 PlayerStatsManager.ChangeGold() 호출
```

### 핵심 연동 포인트
1. **InventoryManager**: `GetInventoryItems()` - 실제 인벤토리 슬롯에서 ItemInstance 가져오기
2. **EquipmentManager**: `GetAllEquippedItems()` - 향후 장비 시스템과 연동
3. **ItemGrade**: 모든 시스템에서 통합된 아이템 등급 사용
4. **Network Spawning**: 드롭된 아이템들이 모든 클라이언트에서 동기화

### 제거된 중복 코드
- **SoulPreservation.cs**: 중복 `SoulData` 구조체 제거
- **WeaponSystem.cs**: `ItemRarity` enum 제거, `ItemGrade` 사용
- **ItemScatter.cs**: 가짜 데이터 생성 로직 제거, 실제 시스템 연동

---

## ⚔️ Equipment System 구현 완료 (2025-01-18)

### 새로 구현된 Equipment System
- **EquipmentManager.cs**: 장비 착용/해제 관리 시스템
- **EquipmentData.cs**: 장비 데이터 저장 및 네트워크 동기화
- **EquipmentUI.cs**: 장비 창 UI 시스템 (E키로 토글)
- **EquipmentSlotUI.cs**: 개별 장비 슬롯 UI 및 드래그앤드롭

### Equipment System 구조
```
EquipmentManager (플레이어당 1개)
├── TryEquipItem() - 아이템 착용 시도
├── UnequipItem() - 장비 해제
├── GetAllEquippedItems() - 모든 착용 장비 반환 (ItemScatter 연동)
└── RecalculateEquipmentStats() - 장비 스탯 보너스 재계산

EquipmentData (장비 정보 저장)
├── 11개 장비 슬롯 관리
├── 네트워크 직렬화 지원
├── IsItemEquipped() - 중복 착용 방지
└── CalculateTotalStatBonus() - 총 스탯 보너스 계산

EquipmentUI (장비창 인터페이스)
├── E키로 장비창 토글
├── 실시간 스탯 표시
├── 드래그앤드롭 지원
└── 장비 슬롯별 시각적 피드백
```

### 장비 슬롯 시스템
```
11개 장비 슬롯 지원:
├── Head (머리) - 투구, 모자
├── Chest (가슴) - 갑옷, 상의
├── Legs (다리) - 하의, 바지
├── Feet (발) - 신발, 부츠
├── Hands (손) - 장갑
├── MainHand (주무기) - 검, 둔기, 단검, 지팡이
├── OffHand (보조) - 방패, 보조무기
├── TwoHand (양손무기) - 활, 대형무기
├── Ring1/Ring2 (반지) - 악세서리
└── Necklace (목걸이) - 악세서리
```

### 핵심 연동 기능
1. **InventoryManager 연동**: 인벤토리 ↔ 장비창 아이템 이동
2. **ItemScatter 연동**: 사망 시 착용 장비 드롭
3. **PlayerStatsManager 연동**: 장비 스탯 보너스 자동 적용
4. **드래그앤드롭**: 인벤토리에서 장비창으로 직관적 착용
5. **네트워크 동기화**: 모든 클라이언트에서 장비 상태 동기화

### 지능형 착용 시스템
- **자동 슬롯 탐지**: 아이템 타입에 따라 적절한 슬롯 자동 선택
- **기존 장비 교체**: 인벤토리 공간 확인 후 안전한 교체
- **호환성 검사**: 무기 카테고리별 올바른 슬롯 배치
- **중복 착용 방지**: 동일 아이템 중복 착용 차단

---

## 🔮 Enchant System 구현 완료 (2025-08-19)

### 새로 구현된 Enchant System
- **EnchantTypes.cs**: 인챈트 타입, 희귀도, 데이터 구조 정의
- **EnchantDropSystem.cs**: 1% 확률 인챈트 북 드롭 시스템
- **EnchantManager.cs**: 아이템 인챈트 적용 및 효과 관리

### Enchant System 구조
```
EnchantDropSystem (몬스터 처치 시 드롭)
├── CheckEnchantDrop() - 1% 기본 드롭률 + LUK 보너스
├── GenerateRandomEnchant() - 몬스터 레벨 기반 인챈트 생성
├── CreateEnchantBookItem() - 인챈트 북 아이템 생성
└── 희귀도별 드롭률: 전설 1%, 영웅 9%, 희귀 30%, 일반 60%

EnchantManager (플레이어당 1개)
├── ApplyEnchantToItem() - 인챈트 북으로 장비에 인챈트 적용
├── RecalculateEnchantEffects() - 착용 장비의 인챈트 효과 계산
├── GetEnchantEffect() - 특정 인챈트 효과 값 조회
└── 80% 성공률, 아이템당 최대 3개 인챈트

EnchantTypes (인챈트 정의)
├── 10가지 인챈트 타입 지원
├── 4단계 희귀도 시스템
├── 네트워크 직렬화 지원
└── 인챈트별 색상 및 설명 시스템
```

### 인챈트 타입 시스템 (10종)
```
공격 관련:
├── Sharpness (예리함) - 공격력 +5% per level
├── CriticalHit (치명타) - 치명타 확률 +2% per level
└── LifeSteal (흡혈) - 데미지의 3% 체력 흡수 per level

방어 관련:
├── Protection (보호) - 방어력 +4% per level
├── Thorns (가시) - 반격 데미지 10% per level
└── Regeneration (재생) - 체력 재생 +2/초 per level

유틸리티:
├── Fortune (행운) - 드롭률 +8% per level
├── Speed (신속) - 이동속도 +6% per level
├── MagicBoost (마력 증폭) - 마법 공격력 +7% per level
└── Durability (내구성) - 아이템 내구도 +15% per level
```

### 인챈트 희귀도 시스템
```
희귀도별 특성:
├── Common (일반) - 60% 확률, 1-2레벨, 1.0배 효과
├── Rare (희귀) - 30% 확률, 2-3레벨, 1.3배 효과
├── Epic (영웅) - 9% 확률, 3-4레벨, 1.6배 효과
└── Legendary (전설) - 1% 확률, 4-5레벨, 2.0배 효과
```

### 핵심 연동 기능
1. **전투 시스템 연동**: 실시간 인챈트 효과 적용 (공격력, 치명타, 흡혈)
2. **장비 시스템 연동**: 착용/해제 시 인챈트 효과 자동 재계산
3. **스탯 시스템 연동**: 인챈트 보너스가 총 스탯에 반영
4. **몬스터 AI 연동**: 몬스터 처치 시 자동 인챈트 북 드롭 체크
5. **아이템 시스템 연동**: 인챈트 북 아이템 생성 및 관리
6. **네트워크 동기화**: 모든 인챈트 효과가 멀티플레이어에서 동기화

### 인챈트 적용 규칙
- **호환성 검사**: 무기 전용, 방어구 전용, 범용 인챈트 구분
- **최대 인챈트 수**: 아이템당 최대 3개까지 적용 가능
- **중복 방지**: 동일한 인챈트 타입 중복 적용 차단
- **성공률**: 80% 기본 성공률 (실패 시 인챈트 북만 소모)

---

## 🏰 던전 시스템 (고급 시간 관리 완료)

### DungeonManager
- **파일**: `Assets/Scripts/Runtime/Dungeon/DungeonManager.cs`
- **상속**: `NetworkBehaviour`
- **핵심 기능**: 던전 시스템 총괄 관리자 - 층별 제한시간, 강제 방출, 몬스터 관리
- **주요 메서드**:
  - `StartDungeonServerRpc(int dungeonDataIndex)` - 던전 시작 및 시간 할당
  - `AdvanceToNextFloorServerRpc()` - 다음 층으로 이동 (시간 이월 포함)
  - `ForceEjectAllPlayersServerRpc(string reason)` - 모든 플레이어 강제 방출
  - `ForceEjectPlayerServerRpc(ulong clientId, string reason)` - 개별 플레이어 방출
  - `CalculateFloorTimeAllocations(DungeonData)` - 층별 시간 할당 계산
  - `LoadFloor(int floorNumber, DungeonData)` - 층 로드 및 몬스터 스폰
- **네트워크 동기화**:
  - `NetworkVariable<DungeonInfo>` - 현재 던전 정보
  - `NetworkVariable<DungeonState>` - 던전 상태
  - `NetworkVariable<float> currentFloorRemainingTime` - 현재 층 남은 시간
  - `NetworkVariable<float> totalRemainingTime` - 총 남은 시간
  - `NetworkList<DungeonPlayer>` - 던전 참가자 목록

### DungeonData
- **파일**: `Assets/Scripts/Runtime/Dungeon/DungeonData.cs`
- **상속**: `ScriptableObject`
- **핵심 기능**: 던전 정의 및 설정, 고급 시간 관리 시스템
- **시간 관리 기능**:
  - `CalculateFloorTimeLimit(int floorNumber)` - 층별 제한시간 계산
  - `CalculateTotalEstimatedTime()` - 모든 층의 총 예상 시간
  - `GetTimeManagementInfo()` - 시간 관리 정보 텍스트
  - `DungeonTimeMode TimeMode` - 시간 관리 모드 (PerFloor/Total/Custom)
- **주요 프로퍼티**:
  - `float baseFloorTime` - 기본 층별 시간 (기본 10분)
  - `float timeIncreasePerFloor` - 층당 시간 증가량 (기본 2분)
  - `AnimationCurve floorTimeCurve` - 층별 시간 배율 곡선

### DungeonTypes
- **파일**: `Assets/Scripts/Runtime/Dungeon/DungeonTypes.cs`
- **핵심 기능**: 던전 시스템 데이터 구조, 해시 기반 문자열 저장
- **네트워크 호환 구조체들**:
  - `DungeonInfo` - 던전 기본 정보 (dungeonNameHash 사용)
  - `DungeonPlayer` - 플레이어 정보 (playerNameHash 사용)
  - `DungeonFloor` - 층 정보 (floorNameHash 사용)
- **해시 헬퍼 메서드들**:
  - `GetDungeonName()` - dungeonNameHash → 던전명 변환
  - `GetPlayerName()` - playerNameHash → 플레이어명 변환
  - `GetFloorName()` - floorNameHash → 층명 변환

### DungeonNameRegistry
- **파일**: `Assets/Scripts/Runtime/Dungeon/DungeonNameRegistry.cs`
- **타입**: `Static Class`
- **핵심 기능**: 네트워크 직렬화를 위한 해시 기반 문자열 관리 시스템
- **주요 메서드**:
  - `RegisterName(string name)` - 문자열을 해시값으로 등록
  - `GetNameFromHash(int hash)` - 해시값에서 문자열 조회
  - `PreregisterCommonNames()` - 자주 사용되는 이름들 미리 등록
- **해결한 문제**: NetworkList<T> 요구사항 (non-nullable value types)

### 던전 시간 관리 시스템 특징
```
3가지 시간 관리 모드:
├── PerFloor (층별 개별) - 기본시간 + 층당 증가량 × 곡선배율
├── Total (총 제한시간) - 총 시간을 층수로 균등 분할
└── Custom (커스텀) - FloorConfiguration에서 층별 개별 설정

시간 이월 시스템:
├── 현재 층에서 남은 시간을 다음 층으로 이월
├── 빠른 클리어 시 더 많은 시간 확보 가능
└── 총 던전 시간과 층별 시간 모두 체크

강제 방출 시스템:
├── 층별 시간 초과 시 모든 플레이어 강제 방출
├── 총 던전 시간 초과 시 강제 방출
├── 개별 플레이어 방출 기능 (관리자)
└── 방출 시 마을 스폰 위치로 자동 이동
```

### 던전 진입 및 나가는 방법
- **진입**: F키로 던전 입구 상호작용
- **퇴장 방법** (2가지만):
  1. **시간 초과**: 층별 또는 총 제한시간 초과로 강제 방출
  2. **사망**: 모든 플레이어가 사망하면 던전 실패
- **긴급 탈출 없음**: 던전에 갇히면 위 2가지 방법으로만 나갈 수 있음

### 던전 출구 발견 시스템
- **몬스터 처치 선택사항**: 모든 몬스터를 처치하지 않아도 출구 발견 시 다음 층 이동 가능
- **출구 활성화**: 몬스터 처치와 관계없이 출구는 항상 상호작용 가능
- **F키 상호작용**: 출구 근처에서 F키로 다음 층 이동

### DungeonExit
- **파일**: `Assets/Scripts/Runtime/Dungeon/DungeonExit.cs`
- **상속**: `NetworkBehaviour`
- **핵심 기능**: 던전 층 출구 시스템 - 플레이어 감지 및 층 이동
- **주요 메서드**:
  - `Initialize(DungeonManager)` - 던전 매니저 연결
  - `UseExitServerRpc()` - 출구 사용 (다음 층 이동)
  - `CheckPlayersInRange()` - 플레이어 범위 내 체크
  - `CheckExitActivationConditions()` - 출구 활성화 조건 확인
- **활성화 조건**: 모든 몬스터 처치 완료 + 플레이어 근접

### DungeonUI
- **파일**: `Assets/Scripts/Runtime/Dungeon/DungeonUI.cs`
- **상속**: `MonoBehaviour`
- **핵심 기능**: 던전 진행 상황 UI 표시
- **UI 요소들**:
  - 던전 이름, 현재 층수, 남은 시간 표시
  - 생존 플레이어 수, 진행 상황 표시
  - 던전 완료 시 보상 UI 표시
- **주요 메서드**:
  - `OnDungeonStarted(DungeonInfo)` - 던전 시작 UI 활성화
  - `OnFloorChanged(int)` - 층 변경 UI 업데이트
  - `ShowRewardUI(DungeonReward)` - 완료 보상 표시

### DungeonController
- **파일**: `Assets/Scripts/Runtime/Dungeon/DungeonController.cs`
- **상속**: `NetworkBehaviour`
- **핵심 기능**: 던전 시스템 통합 컨트롤러 - 플레이어 상호작용 및 시스템 연결
- **주요 기능**:
  - 던전 입구 근접 감지 (F키로 입장)
  - UI 토글 기능 (Tab키)
  - 테스트 모드 지원
- **키 바인딩**:
  - `F키`: 던전 입장
  - `Tab키`: 던전 UI 토글
  - `T키`: 테스트 던전 시작 (개발자 모드)

### DungeonCreator
- **파일**: `Assets/Scripts/Runtime/Dungeon/DungeonCreator.cs`
- **타입**: `Static Class`
- **핵심 기능**: 프로그래매틱 던전 생성 유틸리티
- **주요 메서드**:
  - `CreateBeginnerDungeon()` - 초보자 동굴 (3층, 쉬움)
  - `CreateIntermediateDungeon()` - 어둠의 숲 (5층, 보통)
  - `CreateAdvancedDungeon()` - 고대의 유적 (7층, 어려움)
  - `CreateNightmareDungeon()` - 지옥의 문 (10층, 악몽)
  - `CreatePvPDungeon()` - 투기장 (5층, PvP)

### DungeonTypes
- **파일**: `Assets/Scripts/Runtime/Dungeon/DungeonTypes.cs`
- **핵심 기능**: 던전 시스템 전용 데이터 구조체 및 열거형
- **주요 구조체들**:
  - `DungeonInfo` - 네트워크 직렬화 가능한 던전 정보
  - `DungeonPlayer` - 던전 참가 플레이어 정보
  - `DungeonReward` - 던전 완료 보상 정보
  - `FloorConfiguration` - 층별 구성 정보

---

## 🏰 던전 시스템 구조

### 던전 타입 시스템
```
DungeonType:
├── Normal - 일반 던전 (기본 몬스터, 안전한 난이도)
├── Elite - 엘리트 던전 (강화된 몬스터, 높은 보상)
├── Boss - 보스 던전 (강력한 보스 몬스터)
├── Challenge - 도전 던전 (특수 규칙, 극한 난이도)
└── PvP - PvP 던전 (플레이어 간 전투 허용)

DungeonDifficulty:
├── Easy (1-3층 추천) - 1.0x 보상
├── Normal (4-6층 추천) - 1.3x 보상  
├── Hard (7-9층 추천) - 1.6x 보상
└── Nightmare (10층 추천) - 2.0x 보상
```

### 던전 진행 플로우
```
1. 던전 입장 (DungeonController F키)
   ↓
2. 던전 시작 (DungeonManager.StartDungeon)
   ↓
3. 층별 진행 (1층 → 10층)
   ├── 몬스터 스폰 (MonsterSpawner 연동)
   ├── 플레이어 전투 (CombatSystem 연동)
   ├── 층 클리어 조건 체크
   └── 출구 활성화 (DungeonExit)
   ↓
4. 던전 완료 또는 실패
   ├── 보상 계산 및 지급
   ├── 경험치/골드 지급 (PlayerStatsManager 연동)
   └── 마을로 복귀
```

### 층별 동적 스케일링
```
각 층별 난이도 증가:
├── 몬스터 수: 기본 + (층수 × 2)
├── 엘리트 몬스터: 1 + (층수 / 3)
├── 보스 등장: 5층, 10층 (또는 마지막 층)
├── 경험치 보상: 기본 × (1.2 ^ 층수)
└── 골드 보상: 기본 × (1.1 ^ 층수)
```

### 네트워크 동기화
```
서버 관리 요소:
├── 던전 상태 (대기/진행/완료/실패)
├── 현재 층 정보 및 몬스터 상태
├── 참가자 생존 여부 및 위치
├── 시간 제한 및 남은 시간
└── 보상 계산 및 분배

클라이언트 동기화:
├── UI 업데이트 (층수, 시간, 생존자)
├── 시각 효과 (출구 활성화, 층 이동)
├── 플레이어 위치 이동
└── 보상 수령 알림
```

---

## 🛠️ 코어 시스템

### CharacterCreator
- **파일**: `Assets/Scripts/Runtime/Character/CharacterCreator.cs`
- **상속**: `MonoBehaviour`
- **핵심 기능**: 캐릭터 생성 시스템
- **주요 메서드**:
  - `CreateCharacter(Race, string)` - 새 캐릭터 생성
  - `LoadCharacterData(string)` - 캐릭터 데이터 로드
  - `DeleteCharacter(string)` - 캐릭터 삭제
- **제한**: 계정당 최대 3개 캐릭터 슬롯

### 열거형 정의들
- **Race**: `Human, Elf, Beast, Machina`
- **DamageType**: `Physical, Magical, True, Holy`
- **SkillCategory**: `Warrior, Paladin, Rogue, Archer, Nature, Spirit, etc.`
- **SkillType**: `Active, Passive, Toggle`
- **ItemGrade**: `Common, Uncommon, Rare, Epic, Legendary`
- **WeaponCategory**: `Sword, Axe, Bow, Staff, Dagger, Mace`

---

# 📖 공식 및 밸런싱

## 스탯 계산 공식
```csharp
// 기본 능력치
HP = 100 + (VIT * 10)
MP = 50 + (INT * 5)
물리 공격력 = STR * 2
마법 공격력 = INT * 2
이동속도 = 5.0 + (AGI * 0.1)
공격속도 = 1.0 + (AGI * 0.01)

// 방어 공식
물리 방어 = DEF / (DEF + 100) * 100% 감소
마법 방어 = MDEF / (MDEF + 100) * 100% 감소

// 확률 계산
회피율 = AGI * 0.1%
크리티컬 확률 = LUK * 0.05%
드롭률 증가 = LUK * 0.01%
```

## 민댐/맥댐 시스템
```csharp
// 무기별 편차 (기본 80-120%)
검류: 80-120%, 둔기류: 40-160%, 창류: 70-130%
활: 85-115%, 지팡이: 75-125%, 단검: 90-130%

// 등급별 편차 조정
Common: ±10%, Rare: ±20%, Epic: ±30%, Legendary: ±40%, Mythic: ±50%

// 안정성 적용
최종편차 = 기본편차 * (1 - STAB * 0.01)
```

## 스킬 비용 시스템
```csharp
// 티어별 기본 비용
1티어(3레벨): 100-200 골드
2티어(6레벨): 500-800 골드  
3티어(9레벨): 2000-3000 골드
4티어(12레벨): 8000-15000 골드
5티어(15레벨): 50000-100000 골드
```

---

# 🎯 개발 상태 현황

## ✅ 완료된 시스템들
1. **Player Controller 시스템** - WASD 이동, 마우스 회전, 공격
2. **Stats System** - 종족별 고정 성장, 민댐/맥댐
3. **Race System** - 4종족 시스템 (인간/엘프/수인/기계족)
4. **Combat System** - 실제 전투, PvP/PvE 지원
5. **Visual System** - 종족별 스프라이트, 애니메이션
6. **Death Penalty System** - 완전한 하드코어 데스 페널티
7. **Soul System** - 0.1% 드롭률 영혼 시스템
8. **Skill System** - 골드 기반 스킬 학습/사용

## ✅ 추가 완료된 시스템들 (2025-08-18 세션 3)
9. **Item System** - 5등급 아이템 시스템 (Common~Legendary)
10. **Monster AI System** - 상태 기반 몬스터 AI
11. **Monster Spawner System** - 동적 몬스터 스폰 관리
12. **Inventory System** - 완전한 인벤토리 관리 시스템

## ✅ 추가 완료된 시스템들 (2025-08-19 세션 4)
13. **Soul Inheritance System** - 하드코어 영혼 상속 시스템
14. **Equipment System** - 11슬롯 장비 착용/해제 시스템
15. **Enchant System** - 1% 드롭률 인챈트 시스템 (10종 인챈트, 4단계 희귀도)
16. **Dungeon System** - 10층 던전 시스템 (완전한 던전 관리)
17. **Advanced Dungeon Features** - 층별 제한시간, 강제 방출, 출구 발견 시스템 ✅

## ✅ 추가 완료된 시스템들 (2025-08-19 계속 세션)
18. **Dungeon Time Management System** - 3가지 시간 관리 모드 (PerFloor/Total/Custom)
19. **Dungeon Forced Ejection System** - 시간 초과 시 강제 마을 이동
20. **Network String Hash System** - DungeonNameRegistry를 통한 NetworkList 호환성

## 🔴 미구현 시스템들 (우선순위순)
1. **PvP System** - 던전 내 PvP
2. **Party System** - 16명 파티
3. **UI System** - 게임 UI 확장
4. **Economy System** - 골드 경제
5. **Save System** - 데이터 저장

---

# 🔧 네트워크 아키텍처

## NetworkBehaviour 컴포넌트들
- **PlayerController** - 플레이어 조작 및 이동
- **PlayerStatsManager** - 스탯 및 레벨 동기화  
- **CombatSystem** - 공격 및 데미지 처리
- **DeathManager** - 사망 처리
- **SkillManager** - 스킬 시스템
- **SkillMaster** - NPC 상호작용

## 네트워크 동기화 데이터
```csharp
NetworkVariable<int> networkLevel
NetworkVariable<float> networkCurrentHP
NetworkVariable<float> networkMaxHP
NetworkVariable<SkillListWrapper> networkLearnedSkills
```

## ServerRpc/ClientRpc 메서드들
- **서버 처리**: `*ServerRpc` - 공격, 스킬 사용, 사망 등
- **클라이언트 알림**: `*ClientRpc` - 이펙트, 사운드, UI 업데이트

---

# 📁 파일 구조 요약

```
Assets/Scripts/Runtime/
├── Player/              # 플레이어 관련
│   ├── PlayerController.cs
│   └── PlayerVisualManager.cs
├── Stats/               # 스탯 시스템
│   ├── PlayerStats.cs
│   └── PlayerStatsManager.cs
├── Race/                # 종족 시스템
│   ├── RaceData.cs
│   └── RaceDataCreator.cs
├── Combat/              # 전투 시스템
│   └── CombatSystem.cs
├── Items/               # 아이템 시스템
│   ├── ItemData.cs
│   ├── ItemInstance.cs
│   ├── ItemDatabase.cs
│   ├── ItemDropSystem.cs
│   └── DroppedItem.cs
├── Inventory/           # 인벤토리 시스템
│   ├── InventoryData.cs
│   ├── InventoryManager.cs
│   ├── InventoryUI.cs
│   └── InventorySlotUI.cs
├── Equipment/           # 장비 시스템
│   ├── EquipmentData.cs
│   ├── EquipmentManager.cs
│   ├── EquipmentUI.cs
│   └── EquipmentSlotUI.cs
├── Enchant/             # 인챈트 시스템
│   ├── EnchantData.cs
│   ├── EnchantManager.cs
│   ├── EnchantDropSystem.cs
│   └── EnchantTypes.cs
├── Dungeon/             # 던전 시스템
│   ├── DungeonManager.cs
│   ├── DungeonData.cs
│   ├── DungeonTypes.cs
│   ├── DungeonExit.cs
│   ├── DungeonUI.cs
│   ├── DungeonController.cs
│   └── DungeonCreator.cs
├── AI/                  # 몬스터 AI 시스템
│   ├── MonsterAI.cs
│   ├── MonsterSpawner.cs
│   └── MonsterPrefabCreator.cs
├── Death/               # 데스 페널티
│   ├── DeathManager.cs
│   ├── CharacterDeletion.cs
│   ├── ItemScatter.cs
│   └── SoulPreservation.cs
├── Soul/                # 영혼 시스템
│   ├── SoulDropSystem.cs
│   ├── SoulPickup.cs
│   ├── SoulGlow.cs
│   └── SoulFloatAnimation.cs
├── Skills/              # 스킬 시스템
│   ├── SkillData.cs
│   ├── SkillManager.cs
│   ├── SkillMaster.cs
│   └── RaceSkills/
│       ├── HumanSkills.cs
│       ├── ElfSkills.cs
│       ├── BeastSkills.cs
│       └── MachinaSkills.cs
├── Character/           # 캐릭터 생성
│   └── CharacterCreator.cs
├── Core/                # 코어 시스템
│   └── ResourceLoader.cs
└── Shared/              # 공유 시스템
    └── WeaponSystem.cs
```

---

# 🎮 게임 플레이 플로우

## 캐릭터 생성
1. **CharacterCreator** → 종족 선택 → **RaceData** 적용
2. **PlayerStats** 초기화 → **PlayerStatsManager** 연결
3. **PlayerVisualManager** → 종족별 스프라이트 설정

## 전투 플로우  
1. **PlayerController** → 좌클릭 → **CombatSystem** 활성화
2. **CombatSystem** → 타겟 탐지 → **PlayerStats** 데미지 계산
3. **DamageRange** 시스템 → 민댐/맥댐 적용 → 최종 데미지

## 사망 플로우
1. HP 0 → **DeathManager** 활성화 → **PlayerStatsManager** 사망 이벤트
2. **SoulDropSystem** → 영혼 드롭 생성 → **SoulPreservation** 계정 저장
3. **ItemScatter** → 아이템 드롭 → **CharacterDeletion** 캐릭터 삭제

## 스킬 학습 플로우
1. **SkillMaster** NPC 상호작용 → **SkillManager** 학습 가능 스킬 조회
2. 골드 소모 → **PlayerStats** 골드 차감 → **SkillManager** 스킬 등록
3. 우클릭 → **PlayerController** → **SkillManager** 스킬 사용

---

# 🚨 컴파일 에러 수정 기록

## 중복 정의 문제 해결

### 1. ItemType enum 중복 제거
**문제**: 3개 파일에서 서로 다른 정의
- `ItemScatter.cs`: Weapon, Armor, Accessory, Consumable, Material, Quest
- `ItemData.cs`: Equipment, Consumable, Material, Quest, Other ✅ (유지)
- `WeaponSystem.cs`: 없음

**해결**: ItemScatter.cs의 중복 정의 제거, ItemData.cs의 통합 정의 사용

### 2. ItemRarity/ItemGrade enum 중복 제거
**문제**: 2개 파일에서 서로 다른 희귀도 정의
- `ItemScatter.cs`: Common, Uncommon, Rare, Epic, Legendary (제거됨)
- `WeaponSystem.cs`: Common, Rare, Epic, Legendary, Mythic (제거됨)
- `ItemData.cs`: ItemGrade enum ✅ (유지)

**해결**: 모든 곳에서 ItemData.cs의 ItemGrade enum 사용

### 3. ItemData struct/class 중복 제거
**문제**: ItemScatter.cs에서 간단한 struct 정의
**해결**: Items/ItemData.cs의 완전한 ScriptableObject 클래스 사용

### 4. SoulData struct 중복 제거
**문제**: 2개 파일에서 정의
- `SoulPreservation.cs`: 중복 정의 제거
- `SoulInheritance.cs`: 메인 정의 ✅ (유지)

**해결**: SoulPreservation.cs의 중복 제거, SoulInheritance.cs 참조 사용

## Unity 네임스페이스 충돌 해결

### Light2D 클래스명 충돌 해결
**문제**: Unity 2D Light 시스템과 이름 충돌
- `SoulGlow.cs`: `Light2D` → `SoulLight2D`로 변경
- 모든 참조 코드 업데이트

## TMP 의존성 제거

### UI 텍스트 시스템 통일
**수정된 파일들**:
- `InventoryUI.cs`: TextMeshProUGUI → Text
- `InventorySlotUI.cs`: TextMeshProUGUI → Text, TextAlignmentOptions → TextAnchor
- `StatsUI.cs`: 이미 Text 사용 (수정 불필요)

**추가 설정**:
```csharp
// 기본 Unity 폰트 사용
quantityText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
quantityText.alignment = TextAnchor.LowerRight;
```

## UnityEditor 런타임 사용 문제 해결

### MonsterPrefabCreator.cs 수정
**문제**: SerializedObject를 런타임에서 사용
**해결**: 직접 메서드 호출로 변경
```csharp
// 변경 전: SerializedObject 사용
var serializedObject = new UnityEditor.SerializedObject(monsterAI);

// 변경 후: 직접 메서드 호출
monsterAI.SetAIType(aiType);
monsterHealth.SetMonsterInfo(monsterName, baseLevel, "Prefab", maxHealth, expReward);
```

## 네임스페이스 정리

### 현재 네임스페이스 구조
모든 클래스가 동일한 네임스페이스 사용:
```csharp
namespace Unity.Template.Multiplayer.NGO.Runtime
```

### 향후 권장 구조 (선택사항)
```csharp
namespace Unity.Template.Multiplayer.NGO.Runtime.AI          // MonsterAI, MonsterSpawner
namespace Unity.Template.Multiplayer.NGO.Runtime.Items       // ItemData, ItemInstance
namespace Unity.Template.Multiplayer.NGO.Runtime.Inventory   // InventoryManager, InventoryUI
namespace Unity.Template.Multiplayer.NGO.Runtime.Combat      // CombatSystem, MonsterHealth
namespace Unity.Template.Multiplayer.NGO.Runtime.Player      // PlayerController
```

## 수정 효과

### ✅ 해결된 컴파일 에러들
1. **CS0579**: Duplicate 'System.Serializable' attribute ✅
2. **CS0101**: The namespace already contains a definition for 'SoulData' ✅  
3. **CS0101**: The namespace already contains a definition for 'ItemData' ✅
4. **UnityEditor**: Runtime에서 사용 불가 ✅
5. **TMPro**: 의존성 문제 ✅

### ✅ 개선된 코드 품질
- 중복 코드 제거
- 일관된 데이터 구조 사용
- Unity 표준 컴포넌트 사용
- 네임스페이스 충돌 방지

---

# 📋 클로드 작업 히스토리

## 2025-08-17 세션 ✅
1. 프로젝트 전체 분석 및 시스템 평가
2. 컴파일 에러 수정 (NetworkDiagnostic.cs)
3. PlayerStatsManager 연결 완성
4. Combat System 완전 구현
5. Visual System 구현 (종족별 스프라이트)

## 2025-08-18 세션 1 ✅
1. Player Control 개선 (WASD/마우스 분리)
2. 카메라 시스템 최적화
3. 테스트 코드 정리
4. Death Penalty System 구현
5. Soul System 구현 (0.1% 드롭률)

## 2025-08-18 세션 2 ✅
1. **Skill System 완전 구현**
   - SkillData, SkillManager, SkillMaster 생성
   - 종족별 스킬 시스템 (Human/Elf/Beast/Machina)
   - 골드 기반 학습 시스템
   - 5티어 스킬 진행 (레벨 3,6,9,12,15)
2. **PlayerStats 골드 시스템 추가**
3. **CLAUDE.md 체계적 인덱스 구성**

## 2025-08-18 세션 3 ✅
1. **Item System 완전 구현**
   - ItemData, ItemInstance, ItemDatabase 생성
   - 5등급 아이템 시스템 (Common~Legendary)
   - 등급별 드롭률 및 색상 시스템
   - LUK 기반 드롭률 보너스 시스템
2. **Monster AI System 완전 구현**
   - MonsterAI 상태 기반 AI (6개 상태)
   - MonsterSpawner 동적 스폰 시스템
   - MonsterPrefabCreator 프리팹 생성 도우미
   - 플레이어 레벨 기반 몬스터 레벨 조정
3. **Inventory System 완전 구현**
   - InventoryData, InventoryManager, InventoryUI 생성
   - 30슬롯 그리드 레이아웃 인벤토리
   - 드래그&드롭 아이템 이동
   - 자동 픽업 및 아이템 사용 시스템
4. **컴파일 에러 완전 해결**
   - 중복 정의 문제 수정 (ItemType, ItemRarity, ItemData, SoulData)
   - TMP 의존성 제거 (UI.Text 사용)
   - Unity 네임스페이스 충돌 해결 (Light2D → SoulLight2D)
   - UnityEditor 런타임 사용 문제 해결
5. **시스템 통합**
   - MonsterHealth와 아이템/경험치/영혼 드롭 연동
   - DroppedItem 물리적 아이템 표현 및 자동 픽업
   - 네트워크 동기화 및 서버 검증

## 2025-08-18 세션 계속 (컴파일 에러 해결) ✅
7. **대규모 컴파일 에러 수정 (30+ 개 에러)**
   - SkillCategory enum 확장: Engineering, Energy, Defense, Hacking, Wild, ShapeShift, Hunt, Combat, Nature, Archery, Stealth, Spirit 추가
   - StatusType enum 확장: Enhancement, Root, Invisibility 추가
   - StatBlock 필드 이름 수정: .STR/.AGI/.VIT → .strength/.agility/.vitality
   - Unity Netcode 호환성: string[] 네트워크 직렬화 문제 해결
   - 타입 접근 문제: ItemData.itemName → ItemName 프로퍼티 사용
   - WeaponCategory 확장: Axe, Mace 추가
   - DamageType 확장: Holy 추가
   - PlayerStats 확장: CharacterName, EquippedSoulIds 프로퍼티 추가
8. **프로젝트 문서화 업데이트**
   - COMPILE_ERROR_RESOLUTION_RULES.md 새롭 생성
   - PROJECT_REFERENCE_INDEX.md 업데이트
   - PROJECT_INDEX.md 업데이트
   - CLAUDE.md 업데이트

## 2025-08-19 세션 4 ✅
9. **Enchant System 완전 구현**
   - EnchantTypes.cs: 10종 인챈트 타입, 4단계 희귀도 시스템
   - EnchantDropSystem.cs: 1% 기본 드롭률 + LUK 보너스 시스템
   - EnchantManager.cs: 인챈트 적용, 효과 계산, 장비 연동
   - CombatSystem 연동: 실시간 인챈트 효과 적용 (예리함, 치명타, 흡혈)
   - PlayerStats 확장: 인챈트 보너스 스탯 지원
   - MonsterHealth 연동: 몬스터 처치 시 인챈트 북 드롭
   - ItemDatabase 확장: 인챈트 북 아이템 추가

## 다음 우선순위
1. **Dungeon System** - 10층 던전 시스템

---

이제 이 CLAUDE.md 파일이 프로젝트 전체의 완전한 인덱스 역할을 합니다. 어떤 기능을 구현하거나 수정할 때 이 파일을 참조하면 관련 클래스와 메서드를 쉽게 찾을 수 있습니다.