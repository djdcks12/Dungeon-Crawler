# 🌐 던전 크롤러 - 네트워크 아키텍처 분석 & 설계

## 📊 **현재 코드베이스 분석**

### **기존 구현된 시스템들:**
- ✅ Unity Netcode for GameObjects (NGO)
- ✅ Unity Services (Authentication, Relay, Matchmaker)
- ✅ CustomNetworkManager (고급 네트워크 관리)
- ✅ UnityServicesAuthenticator (익명 인증)
- ✅ P2P/Dedicated Server 지원

### **발견된 구조:**
```
Unity Game Services (UGS)
├── Authentication Service (익명/계정 인증)
├── Relay Service (P2P 연결 중계)
├── Matchmaker Service (매치메이킹)
└── Multiplay (전용 서버 호스팅)
```

## 🎯 **추천 네트워크 아키텍처**

### **하이브리드 구조 (권장)**
```
메인 서버 (Unity Game Services)
├── 사용자 인증 & 계정 관리
├── 캐릭터 데이터 저장
├── 매치메이킹
└── 글로벌 순위/통계

게임 세션 (P2P + Host Migration)
├── 실시간 던전 게임플레이
├── 몬스터 AI & 전투
├── 아이템 드롭 & 거래
└── 파티 시스템
```

## 🔧 **구현 전략**

### **Phase 1: 로컬 테스트 (현재 단계)**
```
Local Host-Client 구조
- 한 PC에서 Host + Client 동시 실행
- 네트워크 로직 테스트
- UI/게임플레이 검증
```

### **Phase 2: P2P 온라인 (다음 단계)**
```
Unity Relay 사용한 P2P
- 플레이어 중 한 명이 Host
- Unity Relay로 NAT 통과 해결
- 최대 4명 협동 플레이
```

### **Phase 3: 전용 서버 (최종 단계)**
```
Dedicated Server
- Unity Multiplay 사용
- 확장성 있는 서버 구조
- 대규모 플레이어 지원
```

## 🎮 **게임 세션 플로우**

### **1. 로그인 & 인증**
```
플레이어 → Unity Authentication → 익명 계정 생성
└── PlayerId 발급 → 캐릭터 데이터 로드
```

### **2. 캐릭터 선택/생성**
```
캐릭터 목록 조회 → 새 캐릭터 생성 or 기존 선택
└── 캐릭터 데이터 (레벨, 장비, 인벤토리) 로드
```

### **3. 매치메이킹/파티**
```
방법 A: 친구 초대 → 방 생성 → 초대 코드 공유
방법 B: 자동 매칭 → 레벨 기반 매칭
└── 최대 4명 파티 구성
```

### **4. 던전 입장**
```
Host 플레이어 선정 → 던전 세션 생성
└── 모든 플레이어 동기화 → 게임 시작
```

### **5. 게임플레이**
```
실시간 동기화: 플레이어 위치, 체력, 스킬 사용
Host 관리: 몬스터 AI, 아이템 드롭, 던전 진행
클라이언트 예측: 이동, 공격 입력
NetworkVariable 동기화: HP/MP, 레벨, 스탯 정보
```

### **6. 세션 종료**
```
던전 클리어/실패 → 결과 계산 → 보상 지급
└── 캐릭터 데이터 저장 → 로비로 복귀
```

## 💾 **데이터 저장 전략**

### **로컬 저장 (즉시 반영)**
- 플레이어 설정
- UI 상태
- 임시 게임 데이터

### **클라우드 저장 (세션 종료 시)**
- 캐릭터 레벨, 경험치
- 인벤토리, 장비
- 던전 진행 상황
- 업적, 통계

## 🛠️ **개발 단계별 구현 계획**

### **현재 우선순위: 로컬 테스트**

#### **필요한 작업:**
1. **TestGameManager 완성**
   - Host/Client 로컬 테스트 환경
   - 치트 코드로 빠른 테스트

2. **기본 UI 구성**
   - 네트워크 연결 UI
   - 캐릭터 생성 UI
   - 인게임 HUD

3. **게임 세션 관리**
   - 플레이어 스폰
   - 던전 입장/퇴장
   - 기본 게임플레이 루프

#### **테스트 시나리오:**
```
1. Host 시작 → 로컬에서 서버 역할
2. Client 연결 → 같은 PC에서 클라이언트 접속
3. 캐릭터 생성 → 기본 스탯으로 생성
4. 던전 입장 → 간단한 맵에서 테스트
5. 전투 테스트 → 몬스터 스폰 & 전투
6. 아이템 테스트 → 드롭 & 획득
7. 레벨업 테스트 → 경험치 & 스탯 증가
```

## 🚀 **즉시 구현할 기능들**

### **1. SimpleAuthManager.cs**
```csharp
// 간단한 익명 인증
// 개발 단계에서는 PlayerPrefs 사용
// 나중에 Unity Authentication으로 교체
```

### **2. LocalGameSession.cs**
```csharp
// 로컬 게임 세션 관리
// Host/Client 역할 분담
// 캐릭터 데이터 동기화
```

### **3. CharacterCreationUI.cs**
```csharp
// 캐릭터 생성 화면
// 종족 선택 (Human, Elf, Beast, Machina)
// 기본 스탯 표시
```

## 📋 **현재 해야 할 작업 순서**

1. **✅ 테스트 매니저 생성 (완료)**
2. **🔄 네트워크 테스트 UI 완성 (진행중)**
3. **❌ 캐릭터 생성 시스템**
4. **❌ 게임 세션 매니저**
5. **❌ 기본 HUD 연결**
6. **❌ 던전 입장 시스템**

---

## 🔗 **NetworkVariable 기반 데이터 동기화 아키텍처**

### **설계 원칙**
```
권한 분리 (Authority Separation):
├── Owner (Client): 로컬 데이터 관리 및 입력 처리
├── Server (Host): 게임 로직 처리 및 권한 있는 상태 관리
└── All Clients: NetworkVariable을 통한 상태 동기화
```

### **PlayerStatsManager 구조**

#### **NetworkVariable 정의**
```csharp
// 기본 스탯 동기화
private NetworkVariable<int> networkLevel
private NetworkVariable<float> networkCurrentHP
private NetworkVariable<float> networkMaxHP
private NetworkVariable<float> networkCurrentMP
private NetworkVariable<float> networkMaxMP

// 전투 계산용 스탯 동기화
private NetworkVariable<float> networkDefense
private NetworkVariable<float> networkMagicDefense
private NetworkVariable<float> networkAgility
```

#### **권한 및 역할 분담**

**Owner (Client) 역할:**
```csharp
- PlayerStats currentStats 객체 관리
- 로컬 상태 변경 (레벨업, 골드 변경 등)
- Server에 NetworkVariable 업데이트 요청
- UI 표시를 위한 상세 정보 접근
```

**Server (Host) 역할:**
```csharp
- NetworkVariable 값 업데이트 권한
- 데미지 계산 및 HP/MP 변경
- 전투 로직 처리 (TakeDamage, Heal 등)
- 게임 규칙 적용 및 검증
```

**All Clients 역할:**
```csharp
- NetworkVariable 값 읽기
- UI 업데이트 (DebugUI에서 HP/MP 표시)
- 시각적 효과 동기화
```

### **데이터 흐름 (Data Flow)**

#### **데미지 처리 흐름**
```
1. Monster (Server) → PlayerStatsManager.TakeDamage()
2. Server에서 NetworkVariable 기반 데미지 계산
3. networkCurrentHP.Value 업데이트 (Server Only)
4. NetworkVariable 자동 동기화 → All Clients
5. Client DebugUI에서 실시간 HP 표시 업데이트
```

#### **힐링 처리 흐름**
```
1. Client → PlayerStatsManager.Heal() 호출
2. Client가 Server가 아니면 → HealServerRpc() 호출
3. Server에서 networkCurrentHP.Value 업데이트
4. NetworkVariable 자동 동기화 → All Clients
5. Client에서 즉시 HP 회복 확인
```

#### **스탯 업데이트 흐름**
```
1. Owner → PlayerStats.currentStats 변경
2. Owner → UpdateNetworkVariables() 호출
3. Server가 모든 NetworkVariable 업데이트
4. 자동 동기화 → All Clients
5. Client UI에서 실시간 스탯 표시
```

### **메서드 구조**

#### **Server 전용 메서드 (NetworkVariable 기반)**
```csharp
public float TakeDamage(float damage, DamageType damageType)
- Server에서만 실행
- NetworkVariable 직접 조작
- 데미지 계산 로직 포함

public void Heal(float amount)
- Client → ServerRpc → Server 실행
- networkCurrentHP 직접 업데이트
```

#### **Owner 전용 메서드 (PlayerStats 기반)**
```csharp
private void UpdateNetworkVariables()
- Owner에서 currentStats → NetworkVariable 동기화
- Server 권한으로 모든 NetworkVariable 업데이트
```

#### **공용 접근 프로퍼티**
```csharp
public float NetworkCurrentHP => networkCurrentHP.Value;
public float NetworkMaxHP => networkMaxHP.Value;
- 모든 Client에서 NetworkVariable 읽기 가능
- UI 표시용으로 사용
```

### **장점**

1. **메모리 효율성**
   - currentStats는 Owner에서만 생성
   - Server는 NetworkVariable만 관리

2. **권한 명확성**
   - Server: 게임 로직 및 규칙 적용
   - Owner: 로컬 데이터 관리
   - Client: 표시 및 시각적 효과

3. **확장성**
   - 플레이어 수가 늘어도 메모리 사용량 선형 증가
   - NetworkVariable 자동 최적화

4. **일관성**
   - NetworkVariable이 단일 진실 소스(Single Source of Truth)
   - 동기화 지연이나 불일치 방지

### **구현 완료 시스템**

- ✅ **데미지 처리**: Server 전용 NetworkVariable 기반
- ✅ **힐링 시스템**: ServerRpc를 통한 안전한 처리
- ✅ **DebugUI 표시**: NetworkVariable에서 직접 읽기
- ✅ **스탯 동기화**: Owner → Server → All Clients
- ✅ **권한 분리**: 명확한 역할 분담 구조

---

**결론:** 현재 프로젝트는 Unity Game Services를 활용한 하이브리드 구조로 설계되어 있으며, **NetworkVariable 기반의 효율적인 데이터 동기화 시스템**을 구축했습니다. 이 아키텍처는 확장성과 메모리 효율성을 보장하며, 명확한 권한 분리를 통해 네트워크 일관성을 유지합니다. 개발 단계에서는 로컬 Host-Client 테스트부터 시작하여, 점진적으로 온라인 P2P, 최종적으로 전용 서버로 확장하는 것이 최적의 전략입니다.