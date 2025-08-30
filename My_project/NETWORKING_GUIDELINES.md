# Unity Netcode RPC 패턴 가이드라인

## 개요

이 문서는 프로젝트의 Unity Netcode for GameObjects(NGO) RPC 패턴에 대한 표준 가이드라인을 정의합니다. 모든 네트워크 기능은 일관된 패턴을 따라 구현되어야 합니다.

## 핵심 원칙

### 1. 단일 진입점 원칙
- 클라이언트와 서버 구분 없이 호출 가능한 **public 메서드** 하나만 제공
- 내부에서 `IsServer` 체크로 분기 처리

### 2. 명확한 책임 분리
- **진입점**: 유효성 검사 + 클라이언트/서버 분기
- **ServerRpc**: 클라이언트 요청을 서버로 전달만
- **서버 처리**: 실제 비즈니스 로직 실행

### 3. 순수 서버 로직
- 서버 처리 메서드는 **RPC 호출 없이** 직접 제어만
- `IsServer` 재검사 금지 (상위에서 이미 검증됨)
- ClientRpc만 허용 (결과 알림용)

### 4. 중복 검사 제거
- 서버 처리 메서드 내부에서 `IsServer` 체크 금지
- 불필요한 유효성 검사 중복 제거

## 표준 패턴

### 기본 구조

```csharp
/// <summary>
/// [기능명] (클라이언트/서버 공통 진입점)
/// </summary>
public [ReturnType] DoSomething(parameters)
{
    // 1. 기본 유효성 검사
    if (!IsValidRequest()) return [DefaultValue];
    
    // 2. 서버/클라이언트 분기
    if (!IsServer)
    {
        DoSomethingServerRpc(parameters);
        return [ClientReturnValue]; // 클라이언트는 요청만 전송
    }
    
    // 3. 서버에서 직접 처리
    return ProcessSomething(parameters);
}

/// <summary>
/// [기능명] ServerRpc (클라이언트에서 호출)
/// </summary>
[ServerRpc]
private void DoSomethingServerRpc(parameters)
{
    ProcessSomething(parameters);
}

/// <summary>
/// 서버에서 실제 [기능명] 처리
/// </summary>
private [ReturnType] ProcessSomething(parameters)
{
    // 실제 비즈니스 로직 (RPC 호출 없음)
    // ClientRpc만 허용 (결과 알림용)
    
    NotifyClientsClientRpc(result);
    return result;
}
```

## 실제 예시

### ✅ 올바른 패턴: MonsterEntity.TakeDamage

```csharp
/// <summary>
/// 데미지 받기 (클라이언트/서버 공통 진입점)
/// </summary>
public void TakeDamage(float damage, DamageType damageType, ulong attackerClientId = 0)
{   
    Debug.Log($"🩸 TakeDamage: damage={damage}, isDead={IsDead}");

    if (IsDead) return; // 기본 유효성 검사

    if (!NetworkManager.Singleton.IsServer)
    {
        // 클라이언트: ServerRpc 호출
        TakeDamageServerRPC(damage, damageType, attackerClientId);
    }
    else
    {
        // 서버: 직접 처리
        ProcessDamage(damage, damageType, attackerClientId);
    }
}

[ServerRpc(RequireOwnership = false)]
public void TakeDamageServerRPC(float damage, DamageType damageType, ulong attackerClientId = 0)
{   
    // 서버 처리 메서드로 위임만
    ProcessDamage(damage, damageType, attackerClientId);
}

/// <summary>
/// 서버에서 실제 데미지 처리
/// </summary>
private void ProcessDamage(float damage, DamageType damageType, ulong attackerClientId)
{
    // IsServer 체크 없음 (이미 상위에서 보장됨)
    // 실제 비즈니스 로직만 수행
    
    float finalDamage = CalculateFinalDamage(damage, damageType);
    networkCurrentHP.Value = Mathf.Max(0f, networkCurrentHP.Value - finalDamage);
    
    // ClientRpc로 결과 알림은 허용
    if (networkCurrentHP.Value <= 0f)
    {
        NotifyDeathClientRpc();
    }
}
```

### ❌ 잘못된 패턴들

#### 1. 직접 RPC 호출 (분기 없음)
```csharp
// 잘못된 예시
public bool LearnSkill(string skillId)
{
    // 분기 없이 바로 RPC 호출
    LearnSkillServerRpc(skillId);
    return true;
}
```

#### 2. 서버 처리 메서드 내 IsServer 재검사
```csharp
// 잘못된 예시
private void ProcessSomething()
{
    if (!IsServer) return; // ❌ 불필요한 재검사
    
    // 비즈니스 로직...
}
```

#### 3. 서버 처리 메서드 내 다른 RPC 호출
```csharp
// 잘못된 예시
private void ProcessSomething()
{
    // 비즈니스 로직...
    
    // ❌ 서버 처리 중 다른 ServerRpc 호출
    OtherSystemServerRpc(data);
}
```

## 개선 전후 비교

### SkillManager 개선 예시

#### Before (❌)
```csharp
public bool LearnSkill(string skillId)
{
    if (!enableSkillSystem) return false;
    
    // 바로 ServerRpc 호출 (분기 없음)
    LearnSkillServerRpc(skillId);
    return true;
}
```

#### After (✅)
```csharp
public bool LearnSkill(string skillId)
{
    if (!enableSkillSystem) return false;
    
    // 서버/클라이언트 분기
    if (!IsServer)
    {
        LearnSkillServerRpc(skillId);
        return true; // 클라이언트는 요청만 전송
    }
    
    // 서버에서 직접 처리
    return ProcessSkillLearning(skillId);
}
```

## 적용 체크리스트

새로운 네트워크 기능을 구현할 때 다음 사항을 확인하세요:

- [ ] 단일 public 진입점 메서드 존재
- [ ] 진입점에서 `IsServer` 체크로 분기 처리
- [ ] ServerRpc는 서버 처리 메서드로 위임만
- [ ] 서버 처리 메서드에 `IsServer` 재검사 없음
- [ ] 서버 처리 메서드에서 다른 ServerRpc 호출 없음
- [ ] ClientRpc는 결과 알림용으로만 사용
- [ ] 적절한 주석과 문서화

## 기존 코드 개선 가이드

### 1. 진입점 추가
```csharp
// 기존 코드에 진입점 메서드 추가
public void DoSomething() {
    if (!IsServer) {
        DoSomethingServerRpc();
        return;
    }
    ProcessSomething();
}
```

### 2. ServerRpc 간소화
```csharp
// 기존 복잡한 ServerRpc를 간소화
[ServerRpc]
private void DoSomethingServerRpc() {
    ProcessSomething(); // 위임만
}
```

### 3. 서버 로직 분리
```csharp
// 실제 처리 로직을 별도 메서드로 분리
private void ProcessSomething() {
    // 순수 서버 로직만
    // IsServer 체크 없음
    // 다른 ServerRpc 호출 없음
}
```

## 문의 및 개선

이 가이드라인에 대한 문의사항이나 개선사항이 있다면 팀과 논의해주세요.

---

*이 문서는 MonsterEntity.TakeDamage 패턴을 기준으로 작성되었습니다.*