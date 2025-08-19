# 컴파일 에러 해결 원칙 및 아키텍처 패턴

**작성일**: 2025-08-18  
**버전**: 1.0  
**적용 범위**: Unity 6 LTS + Unity Netcode for GameObjects

## 1. 기본 해결 원칙

### 1.1 근본 원인 해결 우선
- **회피 금지**: 문제를 임시방편으로 우회하지 않고 근본 원인을 찾아 해결
- **전체 시스템 고려**: 개별 에러 수정이 다른 부분에 미치는 영향 고려
- **완전한 참조 관계 관리**: 모든 의존성과 참조 관계를 명확히 파악

### 1.2 작업 순서
1. 에러 발생 위치와 원인 정확히 파악
2. 프로젝트 전체에서 유사한 패턴 검색
3. 아키텍처적으로 올바른 해결방법 설계
4. 단계별 구현 및 테스트
5. 관련 문서 업데이트

## 2. Unity Netcode 관련 패턴

### 2.1 네트워크 직렬화 문제 해결

#### string[] 배열 직렬화
```csharp
// 문제: Unity Netcode는 string[] 배열 직렬화를 지원하지 않음
// 해결: 수동 직렬화 구현

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

#### NetworkBehaviour vs MonoBehaviour
- **원칙**: 네트워크 동기화가 필요한 컴포넌트는 NetworkBehaviour 상속
- **컴포넌트 탐색**: 타입 안전성을 위해 GetComponents<NetworkBehaviour>() 사용 후 타입 이름으로 필터링

### 2.2 ScriptableObject 네트워크 처리
- **문제**: ScriptableObject는 네트워크 동기화 불가
- **해결**: 필요한 데이터만 별도 구조체로 추출하여 동기화

## 3. 타입 시스템 일관성

### 3.1 필드 명명 규칙
```csharp
// StatBlock 구조체 필드 명명
public struct StatBlock
{
    // 올바른 필드명: 소문자로 시작하는 full name
    public float strength;      // STR 아님
    public float agility;       // AGI 아님  
    public float vitality;      // VIT 아님
    public float intelligence;  // INT 아님
    public float defense;       // DEF 아님
    public float magicDefense;  // MDEF 아님
    public float luck;          // LUK 아님
    public float stability;     // STAB 아님
}
```

### 3.2 프로퍼티 접근 패턴
```csharp
// ItemData 접근 패턴
public class ItemData : ScriptableObject
{
    [SerializeField] private string itemName;  // private 필드
    public string ItemName => itemName;        // public 프로퍼티
}

// 사용 시
item.ItemData.ItemName  // 올바름
item.ItemData.itemName  // 컴파일 에러
```

### 3.3 연산자 오버로딩 우선
```csharp
// StatBlock 연산
public static StatBlock operator +(StatBlock a, StatBlock b)
{
    return new StatBlock(
        a.strength + b.strength,
        a.agility + b.agility,
        // ...
    );
}

// 사용
totalStats = baseStats + bonusStats;  // 올바름
totalStats = baseStats.Add(bonusStats);  // 메서드 대신 연산자 사용
```

## 4. 열거형 확장 원칙

### 4.1 종족별 스킬 카테고리 확장
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
    
    // 기타 (3개)
    Archery, Stealth, Spirit
}
```

### 4.2 상태 효과 확장
```csharp
public enum StatusType
{
    // 디버프
    Poison, Burn, Freeze, Stun, Slow, Weakness, Root,
    
    // 버프
    Strength, Speed, Regeneration, Shield, Blessing, Berserk,
    Enhancement, Invisibility
}
```

### 4.3 무기 카테고리 세분화
```csharp
public enum WeaponCategory
{
    None, Sword, Blunt, Dagger, 
    Axe, Mace,  // 추가됨
    Bow, Staff, Wand, Shield, Fists
}
```

## 5. 컴포넌트 접근 패턴

### 5.1 타입 안전한 컴포넌트 검색
```csharp
// 리플렉션을 사용한 안전한 컴포넌트 접근
var allComponents = GetComponents<NetworkBehaviour>();
foreach (var component in allComponents)
{
    if (component.GetType().Name == "EquipmentManager")
    {
        var method = component.GetType().GetMethod("GetAllEquippedItems");
        if (method != null)
        {
            var result = method.Invoke(component, null);
            // 처리...
        }
        break;
    }
}
```

### 5.2 ScriptableObject 컨텍스트 처리
```csharp
// ScriptableObject에서 GameObject 참조가 필요한 경우
public class PlayerStats : ScriptableObject
{
    [SerializeField] private string characterName = "Unknown";
    public string CharacterName => characterName;
    
    // gameObject.name 대신 직렬화된 필드 사용
    public void SetCharacterName(string name)
    {
        characterName = !string.IsNullOrEmpty(name) ? name.Replace("(Clone)", "") : "Unknown";
    }
}
```

## 6. 수치 타입 변환 규칙

### 6.1 Math vs Mathf 사용
```csharp
// float 연산
float result = Mathf.Max(0f, value);

// long 연산 (특히 금화 시스템)
long gold = Math.Max(0L, gold + amount);  // Math.Max 사용
```

### 6.2 형변환 안전성
```csharp
// 안전한 형변환
if (result is List<ItemInstance> items)
{
    equippedItems.AddRange(items);
}
```

## 7. 레거시 C# 문법 대응

### 7.1 제네릭 Enum 메서드
```csharp
// Unity 2022.3 LTS 호환
var values = System.Enum.GetValues(typeof(ItemType));  // 올바름
var values = Enum.GetValues<ItemType>();               // 최신 C#만 지원
```

### 7.2 using 문 정리
```csharp
// 필요한 using만 명시적으로 추가
using System.Reflection;  // 리플렉션 사용 시
using System.Collections.Generic;
using Unity.Netcode;
```

## 8. 디버깅 및 로깅 패턴

### 8.1 정보성 로그
```csharp
Debug.Log($"💎 Scattering all items from {gameObject.name} at {deathPosition}");
Debug.Log($"⚔️ Equipped {item.ItemData.ItemName} to {targetSlot}");
Debug.Log($"✅ Item scattering completed. {droppedItems.Count} items dropped.");
```

### 8.2 경고 및 에러 로그
```csharp
Debug.LogWarning($"Cannot equip {item.ItemData.ItemName}: No suitable equipment slot");
Debug.LogError("ItemScatter must be called on server!");
```

## 9. 미래 개발 가이드라인

### 9.1 확장성 고려사항
- 새로운 열거형 값 추가 시 기존 switch 문 영향도 체크
- 네트워크 직렬화 호환성 유지
- ScriptableObject 기반 데이터 구조 일관성 유지

### 9.2 성능 최적화
- 리플렉션 사용 최소화 (캐싱 고려)
- 네트워크 동기화 빈도 최적화
- 불필요한 GameObject.Find() 사용 금지

### 9.3 유지보수성
- 명확한 인터페이스 설계
- 의존성 주입 패턴 고려
- 단위 테스트 가능한 구조 설계

---

**이 문서는 실제 컴파일 에러 해결 과정에서 발견된 패턴들을 기반으로 작성되었으며, 하드코어 던전 크롤러 프로젝트의 아키텍처 무결성을 유지하기 위한 가이드라인입니다.**