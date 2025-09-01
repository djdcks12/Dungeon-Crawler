# 하드코어 던전 크롤러 - 완전한 개발 수칙

**버전**: 3.0  
**최종 업데이트**: 2025-08-19  
**적용 범위**: Unity 6 LTS + Unity Netcode for GameObjects

이 문서는 하드코어 던전 크롤러 프로젝트의 모든 개발 작업에서 반드시 따라야 할 원칙, 규칙, 패턴을 정의합니다.

---

# 🚨 핵심 개발 원칙 (절대 준수)

## 1. 근본 원인 해결 우선 원칙

### 1.1 회피 금지 (CRITICAL)
- **절대 금지**: 문제를 임시방편으로 우회하지 않고 근본 원인을 찾아 해결
- **전체 시스템 고려**: 개별 수정이 다른 부분에 미치는 영향 반드시 고려
- **완전한 참조 관계 관리**: 모든 의존성과 참조 관계를 명확히 파악

### 1.2 코드 수정 전 필수 검증 절차 (MANDATORY)
**모든 코드 수정 전 반드시 수행**:

1. **사용처 전체 검색 단계**
   ```bash
   # 메서드/클래스/필드 사용하는 모든 파일 찾기
   grep -r "메서드명\|클래스명\|필드명" 프로젝트경로
   # 또는 Grep 도구 사용하여 전체 프로젝트 검색
   ```

2. **영향도 분석 단계**
   - 찾은 모든 사용처에서 수정이 어떤 영향을 줄지 분석
   - 컴파일 에러, 런타임 에러, 기능 변화 가능성 점검
   - 네트워크 동기화에 미치는 영향 확인

3. **연결 수정 계획 단계**
   - 수정할 코드와 연결된 모든 부분의 수정 계획 수립
   - 레거시 지원 필요성 검토 (단, 중복 기능은 제거)
   - 기존 사용처가 올바르게 연결되도록 수정 방안 준비

4. **단계별 구현**
   - 필요한 새 메서드/클래스부터 구현
   - 기존 사용처를 새로운 구현으로 연결
   - 불필요한 레거시 코드 제거
   - 각 단계마다 컴파일 확인

5. **최종 검증**
   - 모든 사용처가 정상 작동하는지 확인
   - 컴파일 에러 없음 확인
   - 기능 테스트 완료

### 1.2 작업 방향 사전 승인 (MANDATORY)
**모든 작업 시작 전 반드시 사용자와 구체적인 방향 협의**:

1. **현재 상황 분석**
   - 현재 시스템의 구조와 문제점 상세히 설명
   - 수정이 필요한 파일들과 그 이유 나열
   - 기존 코드에서 삭제할 부분들 명시

2. **구현 방향 제안**
   - 새로 만들 클래스/메서드들의 구조 설계 제시
   - 시스템 간 의존성과 호환성 고려사항 설명
   - 네트워크 동기화 영향도 분석

3. **세부 구현 계획**
   - 단계별 작업 순서 상세히 설명
   - 각 단계에서 수정할 파일들과 변경 내용
   - 예상되는 부작용과 대응 방안

4. **사용자 승인 대기**
   - 모든 계획을 디테일하게 설명 후 사용자 확인 요청
   - 사용자가 승인하기 전까지 실제 코드 수정 금지
   - 필요시 계획 수정 및 재승인 과정 진행

### 1.3 계획적 작업 (MANDATORY)
**사용자 승인 후 작업 시 다음 단계를 거쳐야 함**:

### 1.3 표준 작업 순서
1. 에러 발생 위치와 원인 정확히 파악
2. 프로젝트 전체에서 유사한 패턴 검색
3. 아키텍처적으로 올바른 해결방법 설계
4. 단계별 구현 및 테스트
5. 관련 문서 업데이트 (PROJECT_REFERENCE.md 포함)

---

# 🌐 Unity Netcode 필수 패턴

## 2. NetworkList IEquatable<T> 패턴 (CRITICAL)

### 2.1 문제 상황
```csharp
// 이 에러가 발생하면 즉시 IEquatable<T> 구현 필요
error CS0315: The type 'YourStruct' cannot be used as type parameter 'T' 
in the generic type or method 'NetworkList<T>'. 
There is no boxing conversion from 'YourStruct' to 'System.IEquatable<YourStruct>'.
```

### 2.2 필수 구현 패턴 (절대 준수)
```csharp
[System.Serializable]
public struct YourNetworkStruct : INetworkSerializable, System.IEquatable<YourNetworkStruct>
{
    // 모든 필드 정의
    public int someId;
    public float someFloat;
    public Vector3 someVector;
    public bool someBool;
    
    // 1. NetworkSerialize 구현 (필수)
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref someId);
        serializer.SerializeValue(ref someFloat);
        serializer.SerializeValue(ref someVector);
        serializer.SerializeValue(ref someBool);
    }
    
    // 2. IEquatable<T> 구현 (필수) - 모든 필드 비교
    public bool Equals(YourNetworkStruct other)
    {
        return someId == other.someId &&
               Mathf.Approximately(someFloat, other.someFloat) &&  // float는 Approximately 사용
               someVector.Equals(other.someVector) &&              // Unity 타입은 .Equals()
               someBool == other.someBool;
    }
    
    // 3. Object.Equals 오버라이드 (필수)
    public override bool Equals(object obj)
    {
        return obj is YourNetworkStruct other && Equals(other);
    }
    
    // 4. GetHashCode 오버라이드 (필수) - 모든 필드 포함
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + someId.GetHashCode();
            hash = hash * 23 + someFloat.GetHashCode();
            hash = hash * 23 + someVector.GetHashCode();
            hash = hash * 23 + someBool.GetHashCode();
            return hash;
        }
    }
}
```

### 2.3 필드별 비교 규칙 (필수 암기)
| 타입 | 비교 방법 | 예시 |
|------|-----------|------|
| **기본 타입** | `==` 사용 | `id == other.id` |
| **float/double** | `Mathf.Approximately()` | `Mathf.Approximately(value, other.value)` |
| **Unity Vector/Quaternion** | `.Equals()` 사용 | `position.Equals(other.position)` |
| **string 해시** | `==` 사용 | `nameHash == other.nameHash` |
| **enum** | `==` 사용 | `type == other.type` |

### 2.4 GetHashCode 표준 패턴 (절대 준수)
```csharp
public override int GetHashCode()
{
    unchecked  // 오버플로 허용 (성능상 필요)
    {
        int hash = 17;  // 소수로 시작
        hash = hash * 23 + field1.GetHashCode();  // 소수(23) 곱셈
        hash = hash * 23 + field2.GetHashCode();
        hash = hash * 23 + field3.GetHashCode();
        // 모든 필드에 대해 동일한 패턴 적용
        return hash;
    }
}
```

## 3. 네트워크 직렬화 패턴

### 3.1 string[] 배열 직렬화 (Unity Netcode 미지원)
```csharp
public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
{
    if (serializer.IsReader)
    {
        int arrayLength = 0;
        serializer.SerializeValue(ref arrayLength);
        stringArray = new string[arrayLength];
        for (int i = 0; i < arrayLength; i++)
        {
            serializer.SerializeValue(ref stringArray[i]);
        }
    }
    else
    {
        int arrayLength = stringArray?.Length ?? 0;
        serializer.SerializeValue(ref arrayLength);
        if (stringArray != null)
        {
            for (int i = 0; i < arrayLength; i++)
            {
                serializer.SerializeValue(ref stringArray[i]);
            }
        }
    }
}
```

### 3.2 복잡한 객체 직렬화 (List<ItemInstance> 등)
```csharp
// List<ItemInstance> 직렬화 예시
if (serializer.IsReader)
{
    int itemCount = 0;
    serializer.SerializeValue(ref itemCount);
    itemRewards = new List<ItemInstance>();
    
    for (int i = 0; i < itemCount; i++)
    {
        var item = new ItemInstance();
        item.NetworkSerialize(serializer);
        itemRewards.Add(item);
    }
}
else
{
    int itemCount = itemRewards?.Count ?? 0;
    serializer.SerializeValue(ref itemCount);
    
    if (itemRewards != null)
    {
        foreach (var item in itemRewards)
        {
            var itemCopy = item;
            itemCopy.NetworkSerialize(serializer);
        }
    }
}
```

### 3.3 NetworkBehaviour vs MonoBehaviour 선택
- **NetworkBehaviour**: 네트워크 동기화가 필요한 컴포넌트 (플레이어, 몬스터, 아이템 등)
- **MonoBehaviour**: 로컬 전용 컴포넌트 (UI, 이펙트, 사운드 등)

---

# 🏗️ 타입 시스템 일관성 규칙

## 4. 명명 규칙 (절대 준수)

### 4.1 StatBlock 필드 명명
```csharp
public struct StatBlock
{
    // ✅ 올바른 필드명: 소문자로 시작하는 full name
    public float strength;      // ❌ STR 사용 금지
    public float agility;       // ❌ AGI 사용 금지  
    public float vitality;      // ❌ VIT 사용 금지
    public float intelligence;  // ❌ INT 사용 금지
    public float defense;       // ❌ DEF 사용 금지
    public float magicDefense;  // ❌ MDEF 사용 금지
    public float luck;          // ❌ LUK 사용 금지
    public float stability;     // ❌ STAB 사용 금지
}
```

### 4.2 프로퍼티 접근 패턴
```csharp
public class ItemData : ScriptableObject
{
    [SerializeField] private string itemName;  // private 필드
    public string ItemName => itemName;        // public 프로퍼티
}

// 사용 시
item.ItemData.ItemName  // ✅ 올바름
item.ItemData.itemName  // ❌ 컴파일 에러
```

### 4.3 클래스 및 메서드 명명
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

// 이벤트: On + 동사 + 명사
public event Action<PlayerStats> OnStatsChanged;
```

## 5. 연산자 오버로딩 우선 (Performance Critical)

### 5.1 StatBlock 연산 (필수 구현)
```csharp
public static StatBlock operator +(StatBlock a, StatBlock b)
{
    return new StatBlock
    {
        strength = a.strength + b.strength,
        agility = a.agility + b.agility,
        vitality = a.vitality + b.vitality,
        intelligence = a.intelligence + b.intelligence,
        defense = a.defense + b.defense,
        magicDefense = a.magicDefense + b.magicDefense,
        luck = a.luck + b.luck,
        stability = a.stability + b.stability
    };
}

public static StatBlock operator -(StatBlock a, StatBlock b)
{
    return new StatBlock
    {
        strength = a.strength - b.strength,
        agility = a.agility - b.agility,
        vitality = a.vitality - b.vitality,
        intelligence = a.intelligence - b.intelligence,
        defense = a.defense - b.defense,
        magicDefense = a.magicDefense - b.magicDefense,
        luck = a.luck - b.luck,
        stability = a.stability - b.stability
    };
}

public static StatBlock operator *(StatBlock a, float multiplier)
{
    return new StatBlock
    {
        strength = a.strength * multiplier,
        agility = a.agility * multiplier,
        vitality = a.vitality * multiplier,
        intelligence = a.intelligence * multiplier,
        defense = a.defense * multiplier,
        magicDefense = a.magicDefense * multiplier,
        luck = a.luck * multiplier,
        stability = a.stability * multiplier
    };
}
```

### 5.2 사용 패턴 (필수)
```csharp
// ✅ 올바름 - 연산자 사용
totalStats = baseStats + bonusStats;
enhancedStats = baseStats * 1.5f;

// ❌ 금지 - 메서드 사용
totalStats = baseStats.Add(bonusStats);
```

---

# 📋 열거형 확장 원칙

## 6. 종족별 스킬 카테고리 (확장 완료)

### 6.1 SkillCategory 표준 (수정 금지)
```csharp
public enum SkillCategory
{
    // 인간 (4개)
    Warrior, Paladin, Rogue, Archer,
    
    // 엘프 (5개) 
    ElementalMage, PureMage, NatureMage, PsychicMage, Nature,
    
    // 수인 (8개)
    Berserker, Hunter, Assassin, Beast,
    Wild, ShapeShift, Hunt, Combat,
    
    // 기계족 (8개)
    HeavyArmor, Engineer, Artillery, Nanotech,
    Engineering, Energy, Defense, Hacking,
    
    // 공통 (3개)
    Archery, Stealth, Spirit,
    
    // 상태이상 관련 (3개)
    Enhancement, Root, Invisibility
}
```

### 6.2 StatusType 확장 규칙
```csharp
public enum StatusType
{
    // 디버프 (부정적 효과)
    Poison, Burn, Freeze, Stun, Slow, Weakness, Root,
    
    // 버프 (긍정적 효과)
    Strength, Speed, Regeneration, Shield, Blessing, Berserk,
    Enhancement, Invisibility
}
```

### 6.3 WeaponCategory 세분화 (확장 완료)
```csharp
public enum WeaponCategory
{
    None,           // 무기 없음
    Sword,          // 검류 (균형형: 80-120%)
    Axe,            // 도끼류 (고댐 불안정)
    Bow,            // 활류 (원거리)
    Staff,          // 지팡이류 (마법형: 50-150%)
    Dagger,         // 단검류 (안정형: 90-110%)
    Mace,           // 둔기류 (도박형: 40-160%)
    Wand            // 완드류 (마법 보조)
}
```

## 7. 열거형 확장 시 주의사항

### 7.1 확장 전 체크리스트
- [ ] 기존 switch 문에 새 값 추가
- [ ] 네트워크 직렬화 호환성 확인
- [ ] UI 시스템에서 새 값 처리 추가
- [ ] 데이터베이스/ScriptableObject 업데이트

### 7.2 switch 문 완전성 (CRITICAL)
```csharp
// ✅ 올바름 - 모든 케이스 처리
switch (skillCategory)
{
    case SkillCategory.Warrior:
        return "전사";
    case SkillCategory.Paladin:
        return "성기사";
    // ... 모든 케이스
    default:
        Debug.LogError($"Unknown skill category: {skillCategory}");
        return "알 수 없음";
}

// ❌ 금지 - default 없이 일부만 처리
switch (skillCategory)
{
    case SkillCategory.Warrior:
        return "전사";
    // 다른 케이스 누락
}
```

---

# 🔍 컴포넌트 접근 패턴

## 8. 타입 안전한 컴포넌트 검색

### 8.1 리플렉션 기반 접근 (권장)
```csharp
// 타입 안전한 컴포넌트 접근 패턴
var allComponents = GetComponents<NetworkBehaviour>();
foreach (var component in allComponents)
{
    if (component.GetType().Name == "EquipmentManager")
    {
        var method = component.GetType().GetMethod("GetAllEquippedItems");
        if (method != null)
        {
            var result = method.Invoke(component, null);
            if (result is List<ItemInstance> items)
            {
                // 안전한 타입 캐스팅 후 사용
                ProcessEquippedItems(items);
            }
        }
        break;
    }
}
```

### 8.2 직접 참조 패턴 (최적)
```csharp
// 가능한 경우 직접 참조 사용
[SerializeField] private EquipmentManager equipmentManager;
[SerializeField] private InventoryManager inventoryManager;

// OnNetworkSpawn에서 초기화
public override void OnNetworkSpawn()
{
    equipmentManager = GetComponent<EquipmentManager>();
    inventoryManager = GetComponent<InventoryManager>();
    
    // null 체크 필수
    if (equipmentManager == null)
    {
        Debug.LogError($"EquipmentManager not found on {gameObject.name}");
    }
}
```

### 8.3 컴포넌트 자동 추가 패턴
```csharp
// SetupDeathSystem() 패턴 준수
private void SetupRequiredComponents()
{
    // 필요한 컴포넌트들 자동 추가
    if (GetComponent<DeathManager>() == null)
        gameObject.AddComponent<DeathManager>();
        
    if (GetComponent<InventoryManager>() == null)
        gameObject.AddComponent<InventoryManager>();
        
    // ... 다른 필수 컴포넌트들
}
```

## 9. ScriptableObject 컨텍스트 처리

### 9.1 ScriptableObject에서 GameObject 참조 금지
```csharp
// ❌ 금지 - ScriptableObject에서 gameObject 참조
public class PlayerStats : ScriptableObject
{
    public void SomeMethod()
    {
        string name = gameObject.name;  // 컴파일 에러
    }
}

// ✅ 올바름 - 직렬화된 필드 사용
public class PlayerStats : ScriptableObject
{
    [SerializeField] private string characterName = "Unknown";
    public string CharacterName => characterName;
    
    public void SetCharacterName(string name)
    {
        characterName = !string.IsNullOrEmpty(name) ? name.Replace("(Clone)", "") : "Unknown";
    }
}
```

### 9.2 ScriptableObject 네트워크 처리
```csharp
// ❌ 금지 - ScriptableObject 직접 네트워크 동기화
public class PlayerStats : ScriptableObject, INetworkSerializable  // 불가능

// ✅ 올바름 - 필요한 데이터만 구조체로 추출
[System.Serializable]
public struct PlayerStatsData : INetworkSerializable
{
    public int level;
    public float currentHP;
    public long gold;
    // ScriptableObject에서 필요한 데이터만 추출
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref level);
        serializer.SerializeValue(ref currentHP);
        serializer.SerializeValue(ref gold);
    }
}
```

---

# 🔢 수치 타입 및 변환 규칙

## 10. Math vs Mathf 사용 규칙

### 10.1 타입별 사용 원칙 (절대 준수)
```csharp
// float 연산 - Mathf 사용
float result = Mathf.Max(0f, value);
float distance = Mathf.Sqrt(x * x + y * y);
bool isEqual = Mathf.Approximately(a, b);

// int/long 연산 - Math 사용
long gold = Math.Max(0L, gold + amount);
int level = Math.Min(maxLevel, currentLevel);

// double 연산 - Math 사용 (하지만 게임에서는 float 권장)
double preciseValue = Math.Pow(base, exponent);
```

### 10.2 형변환 안전성 (CRITICAL)
```csharp
// ✅ 올바름 - 안전한 형변환
if (result is List<ItemInstance> items)
{
    equippedItems.AddRange(items);
}

// ✅ 올바름 - 명시적 캐스팅
var itemList = (List<ItemInstance>)result;

// ❌ 금지 - 직접 캐스팅 (런타임 에러 위험)
var items = result as List<ItemInstance>;
items.AddRange(...);  // items가 null일 수 있음
```

### 10.3 부동소수점 비교 (CRITICAL)
```csharp
// ✅ 올바름 - Mathf.Approximately 사용
if (Mathf.Approximately(currentHP, maxHP))
{
    // HP가 최대치와 같음
}

// ❌ 금지 - 직접 비교
if (currentHP == maxHP)  // 부동소수점 오차로 false일 수 있음
```

---

# 🐛 디버깅 및 로깅 패턴

## 11. 로그 레벨 및 형식

### 11.1 정보성 로그 (Info)
```csharp
// 시스템 작동 상태 (이모지 사용으로 시각적 구분)
Debug.Log($"💎 Scattering all items from {gameObject.name} at {deathPosition}");
Debug.Log($"⚔️ Equipped {item.ItemData.ItemName} to {targetSlot}");
Debug.Log($"✅ Item scattering completed. {droppedItems.Count} items dropped.");
Debug.Log($"🌟 SKILL LEARNED! {skillData.skillName}");
Debug.Log($"💰 Gold changed: {oldGold} → {newGold}");
```

### 11.2 경고 로그 (Warning)
```csharp
// 예상치 못한 상황이지만 치명적이지 않음
Debug.LogWarning($"Cannot equip {item.ItemData.ItemName}: No suitable equipment slot");
Debug.LogWarning($"Skill on cooldown: {skillData.skillName}");
Debug.LogWarning($"Index {index} out of range for inventory");
```

### 11.3 에러 로그 (Error)
```csharp
// 시스템 오작동, 즉시 수정 필요
Debug.LogError("ItemScatter must be called on server!");
Debug.LogError($"Component not found on {gameObject.name}");
Debug.LogError($"Failed to parse save data: {e.Message}");
```

### 11.4 조건부 로그 (개발/디버그 전용)
```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
Debug.Log($"🔧 Debug: Monster AI state changed from {oldState} to {newState}");
Debug.Log($"📊 Performance: NetworkList operation took {elapsedTime}ms");
#endif
```

## 12. 에러 처리 패턴

### 12.1 방어적 프로그래밍 (MANDATORY)
```csharp
// Null 체크 패턴
if (component == null)
{
    Debug.LogError($"Component not found on {gameObject.name}");
    return;  // 조기 반환으로 추가 에러 방지
}

// 범위 체크 패턴
if (index < 0 || index >= array.Length)
{
    Debug.LogWarning($"Index {index} out of range (0-{array.Length - 1})");
    return false;
}

// 권한 체크 패턴 (네트워크)
if (!IsOwner)
{
    Debug.LogWarning("Only owner can perform this action");
    return;
}

// 서버 체크 패턴 (네트워크)
if (!IsServer)
{
    Debug.LogError("This method must be called on server!");
    return;
}
```

### 12.2 예외 처리 패턴
```csharp
// JSON 파싱 등 외부 데이터 처리
try
{
    var data = JsonUtility.FromJson<SaveData>(jsonString);
    return data;
}
catch (System.Exception e)
{
    Debug.LogError($"Failed to parse save data: {e.Message}");
    return new SaveData();  // 기본값 반환
}

// 파일 I/O 처리
try
{
    string saveData = System.IO.File.ReadAllText(savePath);
    return saveData;
}
catch (System.IO.FileNotFoundException)
{
    Debug.LogWarning($"Save file not found: {savePath}");
    return string.Empty;
}
catch (System.Exception e)
{
    Debug.LogError($"Failed to read save file: {e.Message}");
    return string.Empty;
}
```

---

# ⚡ 성능 최적화 규칙

## 13. Update vs FixedUpdate vs 이벤트

### 13.1 사용 원칙 (Performance Critical)
```csharp
// Update: 입력 처리, UI 업데이트
void Update()
{
    if (!IsLocalPlayer) return;  // 네트워크 최적화
    
    HandleInput();
    UpdateUI();
    UpdateCooldowns();
}

// FixedUpdate: 물리 처리, 이동
void FixedUpdate()
{
    if (!IsLocalPlayer) return;
    
    HandleMovement();
    HandlePhysics();
}

// 이벤트 기반: 상태 변화 처리 (최적)
public event Action<PlayerStats> OnStatsChanged;

private void OnStatsUpdated(PlayerStats stats)
{
    // 상태 변화 시에만 실행 (성능 최적)
    UpdateStatsUI(stats);
    RecalculateEquipmentBonus();
}
```

### 13.2 메모리 할당 최소화 (CRITICAL)
```csharp
// ❌ 금지 - 매 프레임 새 객체 생성
void Update()
{
    Vector3 direction = new Vector3(input.x, input.y, 0);  // GC 압박
    List<Enemy> nearbyEnemies = new List<Enemy>();          // GC 압박
}

// ✅ 올바름 - 객체 재사용
private Vector3 direction;  // 클래스 레벨에서 선언
private List<Enemy> nearbyEnemies = new List<Enemy>();

void Update()
{
    direction.Set(input.x, input.y, 0);  // 기존 객체 재사용
    nearbyEnemies.Clear();               // 리스트 재사용
    GetNearbyEnemies(nearbyEnemies);
}
```

### 13.3 GameObject.Find 사용 금지 (CRITICAL)
```csharp
// ❌ 금지 - 매번 씬 전체 검색
void Update()
{
    var player = GameObject.Find("Player");  // 매우 느림
}

// ✅ 올바름 - 캐싱된 참조 사용
private PlayerController playerController;

void Start()
{
    playerController = FindObjectOfType<PlayerController>();  // 한 번만 실행
}

void Update()
{
    if (playerController != null)  // 캐싱된 참조 사용
    {
        // 처리...
    }
}
```

## 14. 네트워크 최적화

### 14.1 NetworkVariable 사용 최적화
```csharp
// ✅ 올바름 - 필요한 데이터만 동기화
private NetworkVariable<int> networkLevel = new NetworkVariable<int>(1);
private NetworkVariable<float> networkCurrentHP = new NetworkVariable<float>(100f);

// ❌ 금지 - 과도한 동기화
private NetworkVariable<PlayerStats> networkStats;  // 전체 객체 동기화는 비효율
```

### 14.2 RPC 사용 최적화
```csharp
// ✅ 올바름 - 필요한 매개변수만 전송
[ServerRpc]
private void UseSkillServerRpc(string skillId, Vector3 targetPosition)

// ❌ 금지 - 큰 객체 전체 전송
[ServerRpc]
private void UseSkillServerRpc(SkillData entireSkillData)  // 비효율적
```

### 14.3 문자열 해시화 (네트워크 최적화)
```csharp
// ✅ 올바름 - string 대신 hash 사용
public struct DungeonInfo : INetworkSerializable
{
    public int dungeonNameHash;  // string 대신 해시값 사용
    
    public string GetDungeonName()
    {
        return DungeonNameRegistry.GetNameFromHash(dungeonNameHash);
    }
}

// ❌ 금지 - 직접 string 전송
public struct DungeonInfo : INetworkSerializable
{
    public string dungeonName;  // 네트워크 대역폭 낭비
}
```

---

# 🏗️ 아키텍처 패턴

## 15. 의존성 주입 패턴

### 15.1 컴포넌트 의존성 관리
```csharp
public class PlayerController : NetworkBehaviour
{
    // 의존성 주입을 위한 프로퍼티
    [Header("Dependencies")]
    [SerializeField] private PlayerStatsManager statsManager;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private EquipmentManager equipmentManager;
    
    // 자동 주입 패턴
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        InjectDependencies();
    }
    
    private void InjectDependencies()
    {
        // null 체크 후 자동 주입
        if (statsManager == null)
            statsManager = GetComponent<PlayerStatsManager>();
            
        if (inventoryManager == null)
            inventoryManager = GetComponent<InventoryManager>();
            
        if (equipmentManager == null)
            equipmentManager = GetComponent<EquipmentManager>();
            
        // 필수 의존성 검증
        ValidateDependencies();
    }
    
    private void ValidateDependencies()
    {
        if (statsManager == null)
            Debug.LogError($"PlayerStatsManager dependency missing on {gameObject.name}");
            
        if (inventoryManager == null)
            Debug.LogError($"InventoryManager dependency missing on {gameObject.name}");
    }
}
```

### 15.2 이벤트 기반 시스템 통신
```csharp
// 시스템 간 느슨한 결합을 위한 이벤트 패턴
public class PlayerStatsManager : NetworkBehaviour
{
    // 정적 이벤트로 시스템 간 통신
    public static event Action<PlayerStats> OnPlayerStatsChanged;
    public static event Action<ulong> OnPlayerDied;
    
    private void OnStatsUpdated()
    {
        OnPlayerStatsChanged?.Invoke(currentStats);
    }
    
    private void OnPlayerDeath()
    {
        OnPlayerDied?.Invoke(OwnerClientId);
    }
}

// 다른 시스템에서 구독
public class UIManager : MonoBehaviour
{
    private void OnEnable()
    {
        PlayerStatsManager.OnPlayerStatsChanged += UpdateStatsUI;
    }
    
    private void OnDisable()
    {
        PlayerStatsManager.OnPlayerStatsChanged -= UpdateStatsUI;
    }
}
```

## 16. ScriptableObject 아키텍처

### 16.1 데이터 중심 설계
```csharp
// 데이터 정의 (ScriptableObject)
[CreateAssetMenu(fileName = "New Item", menuName = "Dungeon Crawler/Item Data")]
public class ItemData : ScriptableObject
{
    // 데이터만 포함, 로직 최소화
    [SerializeField] private string itemName;
    [SerializeField] private ItemGrade grade;
    [SerializeField] private StatBlock statBonuses;
    
    // 프로퍼티로 읽기 전용 접근
    public string ItemName => itemName;
    public ItemGrade Grade => grade;
    public StatBlock StatBonuses => statBonuses;
}

// 로직 처리 (MonoBehaviour/NetworkBehaviour)
public class ItemManager : NetworkBehaviour
{
    // ScriptableObject 데이터를 사용하는 로직
    public bool TryUseItem(ItemData itemData)
    {
        // 실제 아이템 사용 로직
        switch (itemData.ItemType)
        {
            case ItemType.Consumable:
                return UseConsumable(itemData);
            case ItemType.Equipment:
                return EquipItem(itemData);
            default:
                return false;
        }
    }
}
```

### 16.2 팩토리 패턴 (데이터 생성)
```csharp
// 정적 팩토리 클래스
public static class RaceDataCreator
{
    // 런타임에 데이터 생성
    public static RaceData CreateHumanRaceData()
    {
        var data = ScriptableObject.CreateInstance<RaceData>();
        data.Initialize(/* 인간 스탯 */);
        return data;
    }
    
    public static RaceData CreateElfRaceData()
    {
        var data = ScriptableObject.CreateInstance<RaceData>();
        data.Initialize(/* 엘프 스탯 */);
        return data;
    }
}
```

---

# 🔒 보안 및 검증 패턴

## 17. 서버 권한 검증 (CRITICAL)

### 17.1 서버 사이드 검증 (모든 중요 로직)
```csharp
// ✅ 올바른 패턴 - 서버에서 검증
[ServerRpc]
public void UseItemServerRpc(int slotIndex)
{
    // 1. 서버에서 재검증
    if (!IsValidSlot(slotIndex))
    {
        Debug.LogWarning($"Invalid slot index: {slotIndex}");
        return;
    }
    
    // 2. 비즈니스 로직 검증
    var item = inventory.GetItem(slotIndex);
    if (item == null || !item.CanUse())
    {
        Debug.LogWarning("Cannot use this item");
        return;
    }
    
    // 3. 서버에서 실행
    ExecuteItemUse(item);
    
    // 4. 클라이언트에 결과 알림
    NotifyItemUsedClientRpc(slotIndex);
}

// ❌ 금지 - 클라이언트 신뢰
public void UseItem(int slotIndex)
{
    // 클라이언트에서 직접 실행 (치트 가능)
    var item = inventory.GetItem(slotIndex);
    item.Use();  // 서버 검증 없음
}
```

### 17.2 입력 검증 패턴
```csharp
// 모든 입력에 대한 범위/유효성 검증
private bool IsValidInput(float value, float min, float max)
{
    if (float.IsNaN(value) || float.IsInfinity(value))
    {
        Debug.LogWarning($"Invalid float value: {value}");
        return false;
    }
    
    if (value < min || value > max)
    {
        Debug.LogWarning($"Value {value} out of range ({min}-{max})");
        return false;
    }
    
    return true;
}

// 문자열 입력 검증
private bool IsValidString(string input, int maxLength)
{
    if (string.IsNullOrEmpty(input))
        return false;
        
    if (input.Length > maxLength)
    {
        Debug.LogWarning($"String too long: {input.Length} > {maxLength}");
        return false;
    }
    
    // 특수 문자 필터링
    if (input.Contains("\\") || input.Contains("/"))
    {
        Debug.LogWarning("Invalid characters in string");
        return false;
    }
    
    return true;
}
```

## 18. 안티 치트 패턴

### 18.1 중요 값 서버 동기화
```csharp
// 중요한 값들은 NetworkVariable로 서버 관리
public class PlayerStatsManager : NetworkBehaviour
{
    // 서버에서만 수정 가능
    private NetworkVariable<int> networkLevel = new NetworkVariable<int>(
        1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
    private NetworkVariable<long> networkGold = new NetworkVariable<long>(
        1000, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    // 클라이언트에서는 읽기만 가능
    public int CurrentLevel => networkLevel.Value;
    public long CurrentGold => networkGold.Value;
    
    // 서버에서만 값 변경
    [ServerRpc]
    public void ChangeGoldServerRpc(long amount)
    {
        if (!IsValidGoldChange(amount))
            return;
            
        networkGold.Value = Math.Max(0L, networkGold.Value + amount);
    }
}
```

### 18.2 비즈니스 로직 서버 검증
```csharp
[ServerRpc]
public void LearnSkillServerRpc(string skillId)
{
    // 1. 스킬 존재 검증
    if (!availableSkills.ContainsKey(skillId))
    {
        Debug.LogWarning($"Invalid skill: {skillId}");
        return;
    }
    
    // 2. 학습 조건 검증
    var skillData = availableSkills[skillId];
    if (!skillData.CanLearn(currentStats, learnedSkills))
    {
        Debug.LogWarning($"Cannot learn skill: {skillId}");
        return;
    }
    
    // 3. 비용 검증
    if (currentStats.Gold < skillData.goldCost)
    {
        Debug.LogWarning($"Not enough gold for skill: {skillId}");
        return;
    }
    
    // 4. 서버에서 실행
    ProcessSkillLearning(skillData);
}
```

---

# 📚 레거시 호환성 및 기타 규칙

## 19. Unity 2022.3 LTS 호환성

### 19.1 C# 버전 제약
```csharp
// ✅ 올바름 - Unity 2022.3 LTS 호환
var values = System.Enum.GetValues(typeof(ItemType));
string text = value.ToString();

// ❌ 최신 C#만 지원 (Unity 2022.3에서 컴파일 에러)
var values = Enum.GetValues<ItemType>();
string text = $"{value}";  // 일부 케이스에서 문제
```

### 19.2 using 문 정리
```csharp
// 필요한 using만 명시적으로 추가
using System;
using System.Collections.Generic;
using System.Reflection;  // 리플렉션 사용 시만
using Unity.Netcode;
using UnityEngine;

// ❌ 불필요한 using (빌드 크기 증가)
using System.Linq;  // 사용하지 않는 경우
using System.Text;  // 사용하지 않는 경우
```

## 20. 문서화 규칙

### 20.1 XML 문서 주석 (공개 API)
```csharp
/// <summary>
/// 플레이어의 스킬을 학습합니다.
/// </summary>
/// <param name="skillId">학습할 스킬의 고유 ID</param>
/// <returns>학습 성공 시 true, 실패 시 false</returns>
/// <exception cref="ArgumentNullException">skillId가 null인 경우</exception>
public bool LearnSkill(string skillId)
{
    if (string.IsNullOrEmpty(skillId))
        throw new ArgumentNullException(nameof(skillId));
        
    // 구현...
}
```

### 20.2 코드 주석 규칙
```csharp
// ✅ 의미 있는 주석 - 왜 이렇게 했는지 설명
// Unity Netcode는 string[] 직렬화를 지원하지 않으므로 수동 구현
if (serializer.IsReader)
{
    // 배열 길이를 먼저 읽고, 그 다음 각 문자열을 읽음
    int arrayLength = 0;
    serializer.SerializeValue(ref arrayLength);
    // ...
}

// ❌ 불필요한 주석 - 코드와 동일한 내용
// i를 0부터 배열 길이까지 증가시킴
for (int i = 0; i < array.Length; i++)
{
    // ...
}
```

### 20.3 TODO 주석 형식
```csharp
// TODO: [우선순위] 설명 - 담당자 (예상일정)
// TODO: [HIGH] 몬스터 AI 최적화 필요 - Developer (2025-09-01)
// TODO: [MEDIUM] UI 애니메이션 추가 - Designer (2025-09-15)
// TODO: [LOW] 코드 리팩토링 고려 - Developer (TBD)
```

---

# 🔄 코드 리뷰 체크리스트

## 21. 제출 전 필수 체크사항

### 21.1 컴파일 및 기본 검증
- [ ] 컴파일 에러 없음
- [ ] 컴파일 경고 최소화 (0개 목표)
- [ ] Unity Console 에러 없음
- [ ] 네트워크 기능 테스트 완료

### 21.2 아키텍처 준수 검증
- [ ] NetworkList 사용 시 IEquatable<T> 구현
- [ ] 네트워크 동기화 데이터 최소화
- [ ] 서버 사이드 검증 적용
- [ ] 방어적 프로그래밍 패턴 적용

### 21.3 성능 검증
- [ ] Update/FixedUpdate 최적화
- [ ] 메모리 할당 최소화
- [ ] GameObject.Find 사용 금지 준수
- [ ] 네트워크 RPC 최적화

### 21.4 코드 품질 검증
- [ ] 명명 규칙 준수
- [ ] 에러 처리 완비
- [ ] 로깅 적절성
- [ ] 문서 주석 완성

### 21.5 통합 테스트
- [ ] 멀티플레이어 환경에서 테스트
- [ ] 극한 상황 테스트 (연결 끊김, 높은 레이턴시)
- [ ] 메모리 누수 확인
- [ ] 프레임율 60fps 유지 확인

---

# 📋 마무리 원칙

## 22. 이 문서의 위상 (CRITICAL)

### 22.1 절대 우선순위
이 문서의 모든 규칙은 **절대적**입니다. 다른 문서나 기존 코드와 충돌하는 경우:

1. **DEVELOPMENT_RULES.md (이 문서)** - 최고 우선순위
2. **PROJECT_REFERENCE.md** - 구현 가이드
3. **PROJECT_ROADMAP.md** - 계획 및 진행상황
4. 기타 문서들 - 참고용

### 22.2 규칙 위반 시 조치
- **컴파일 에러**: 즉시 수정 필요 (배포 차단)
- **성능 문제**: 높은 우선순위로 수정
- **보안 문제**: 즉시 수정 필요 (보안 위험)
- **아키텍처 위반**: 리팩토링 계획 수립

### 22.3 문서 업데이트 책임
- 새로운 패턴 발견 시 이 문서에 즉시 추가
- 규칙 변경 시 전체 프로젝트에 일관성 있게 적용
- 모든 팀원이 새 규칙 숙지 후 개발 진행

## 23. 레퍼런스 문서 관리 규칙 (CRITICAL)

### 23.1 목차 기반 선택적 읽기 패턴 (필수)
- **절대 금지**: 큰 레퍼런스 문서 전체 읽기
- **반드시 준수**: 목차의 라인 번호를 이용한 부분별 읽기

### 23.2 목차 형식 (표준화)
```markdown
# 📚 레퍼런스 목차

## 🎮 핵심 플레이어 시스템 (라인 45-120)
- PlayerController.cs (45-80)
- PlayerStatsManager.cs (81-120)

## 🌐 네트워크 시스템 (라인 121-200)
- NetworkList 패턴 (121-160)
- RPC 패턴 (161-200)

## 📊 데이터 구조 (라인 201-280)
- 열거형들 (201-240)
- 구조체들 (241-280)
```

### 23.3 읽기 방법 (필수 패턴)
```csharp
// ✅ 올바름 - 필요한 부분만 읽기
Read(file_path, offset: 45, limit: 35)  // PlayerController 섹션만
Read(file_path, offset: 121, limit: 40) // NetworkList 패턴만

// ❌ 금지 - 전체 파일 읽기
Read(file_path) // 전체 파일 읽기 금지
```

### 23.4 목차 업데이트 규칙 (MANDATORY)

#### 23.4.1 강제 동시 업데이트 원칙 (CRITICAL)
- **절대 금지**: 내용만 수정하고 목차는 나중에 업데이트
- **반드시 준수**: 모든 내용 수정 시 목차 라인 번호 즉시 동시 업데이트
- **작업 순서**: 내용 수정 → 즉시 목차 업데이트 → 검증

#### 23.4.2 구체적 업데이트 시나리오
**내용 추가 시**:
1. 새 내용을 문서에 추가
2. 즉시 목차에서 해당 섹션 이후의 모든 라인 번호 재계산
3. 목차 업데이트 완료

**내용 삭제 시**:
1. 해당 내용을 문서에서 삭제
2. 즉시 목차에서 해당 항목 제거
3. 삭제된 라인 수만큼 이후 모든 라인 번호 감소 조정

**내용 이동 시**:
1. 내용을 새 위치로 이동
2. 즉시 목차에서 해당 항목의 라인 번호 변경
3. 영향받는 다른 섹션들의 라인 번호도 재조정

#### 23.4.3 라인 번호 계산 공식 (필수 암기)
```markdown
새로운_라인_번호 = 기존_라인_번호 + 추가된_라인_수
새로운_라인_번호 = 기존_라인_번호 - 삭제된_라인_수

범위 계산:
시작_라인: 해당_섹션_첫번째_라인
끝_라인: 다음_섹션_시작_라인 - 1
라인_수: 끝_라인 - 시작_라인 + 1
```

#### 23.4.4 작업 프로세스 (절대 준수)
```markdown
1. 내용 수정 전: 해당 섹션의 현재 라인 번호 확인
2. 내용 수정: Edit 도구로 실제 내용 변경
3. 라인 수 계산: 추가/삭제된 라인 수 정확히 계산
4. 목차 업데이트: 영향받는 모든 라인 번호 즉시 수정
5. 검증: Read 도구로 목차 라인 번호 정확성 확인
```

#### 23.4.5 에러 방지 규칙 (CRITICAL)
- **절대 금지**: "나중에 목차 업데이트하겠다"는 생각
- **반드시 실행**: 내용 수정과 목차 업데이트를 하나의 작업 단위로 처리
- **의무 검증**: 목차 업데이트 후 반드시 라인 번호 정확성 검증

### 23.5 목차 검증 패턴 (MANDATORY)

#### 23.5.1 필수 검증 체크리스트
```markdown
- [ ] 각 섹션의 라인 번호가 정확함
- [ ] 라인 범위가 겹치지 않음
- [ ] 모든 주요 섹션이 목차에 포함됨
- [ ] 목차 순서가 실제 문서 순서와 일치함
- [ ] 라인 범위 계산이 올바름 (시작~끝)
- [ ] 섹션 간 라인 번호 연속성 확인
```

#### 23.5.2 검증 방법
```markdown
1. Read(file_path, offset: 목차_시작라인, limit: 목차_라인수)로 목차 확인
2. 각 주요 섹션별로 Read(file_path, offset: 섹션_시작라인, limit: 10) 실행
3. 실제 내용과 목차 라인 번호 일치 확인
4. 불일치 발견 시 즉시 목차 수정
```

#### 23.5.3 목차 무결성 유지 규칙
- **매 수정마다**: 영향받는 모든 라인 번호 업데이트
- **의심스러우면**: 전체 목차 재검증
- **확신 없으면**: Read 도구로 실제 라인 확인 후 업데이트

---

**이 문서의 모든 규칙은 하드코어 던전 크롤러 프로젝트의 품질, 성능, 안정성을 보장하기 위한 필수 요구사항입니다. 예외는 없습니다.**

**마지막 업데이트**: 2025-08-19  
**다음 리뷰**: 주요 기능 추가 시마다  
**문의**: 이 문서의 내용에 대한 질문이나 제안사항이 있을 경우 프로젝트 담당자에게 문의