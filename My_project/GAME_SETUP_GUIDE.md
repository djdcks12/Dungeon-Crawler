# 🎮 던전 크롤러 게임 실행 환경 구축 가이드

## 📋 현재 상황 분석
- ✅ 코드베이스: 완전한 시스템들 구현됨
- ✅ 기본 프리팹: NetworkManager, Player, GameApplication 존재
- ❌ 실행 가능한 씬 구성: 부재
- ❌ UI 연결: 부재
- ❌ 게임플레이 플로우: 테스트 안됨

## 🚀 단계별 구축 계획

### **Phase 1: 기본 네트워크 환경 (우선순위 1)**
```
씬: TestNetworkScene.unity
목적: 네트워크 연결 및 기본 플레이어 스폰 테스트
```

**필요한 GameObject들:**
1. **NetworkManager** (이미 존재하는 프리팹 사용)
2. **GameApplication** (게임 매니저 역할)
3. **Basic Player Spawner** 
4. **Test UI Canvas**

### **Phase 2: 플레이어 시스템 (우선순위 2)**
```
목적: 캐릭터 생성, 스탯, 인벤토리 시스템 테스트
```

**테스트할 시스템들:**
- PlayerController (이동, 입력)
- PlayerStatsManager (레벨, 경험치, 골드)
- InventoryManager (아이템 추가/제거)
- EquipmentManager (장비 착용)

### **Phase 3: 전투 시스템 (우선순위 3)**
```
목적: 몬스터 스폰, AI, 전투 메커니즘 테스트
```

**테스트할 시스템들:**
- MonsterSpawner
- MonsterAI / BossMonsterAI
- CombatSystem
- MonsterHealth

### **Phase 4: 던전 시스템 (우선순위 4)**
```
목적: 던전 입장, 층 이동, 보상 시스템 테스트
```

**테스트할 시스템들:**
- DungeonManager
- DungeonController
- HiddenFloorSystem
- 층 전환 시스템

## 🛠️ 즉시 시작할 작업들

### **1. 테스트 씬 생성 (TestScene.unity)**

#### **필수 GameObject 구성:**
```
TestScene
├── NetworkManager (프리팹)
├── GameApplication (프리팹)  
├── TestEnvironment
│   ├── Ground (Plane)
│   ├── Walls (Cube들)
│   └── SpawnPoints (Empty GameObjects)
├── UI Canvas
│   ├── NetworkStatus (Text)
│   ├── PlayerStats (Text)
│   ├── TestButtons (Button들)
│   └── Debug Info (Text)
└── Managers
    ├── InventoryManager
    ├── UIManager
    └── MonsterSpawner
```

### **2. 기본 테스트 시나리오**

#### **테스트 시나리오 1: 네트워크 연결**
- [ ] Host 시작
- [ ] Client 접속
- [ ] 플레이어 스폰 확인

#### **테스트 시나리오 2: 플레이어 기본 기능**
- [ ] 캐릭터 이동 (WASD)
- [ ] 스탯 표시 (레벨, HP, MP, 골드)
- [ ] 인벤토리 열기/닫기

#### **테스트 시나리오 3: 전투 시스템**
- [ ] 몬스터 스폰
- [ ] 플레이어 공격
- [ ] 데미지 계산
- [ ] 경험치 획득

#### **테스트 시나리오 4: 아이템 시스템**
- [ ] 아이템 드롭
- [ ] 아이템 획득
- [ ] 장비 착용/해제
- [ ] 스탯 변화 확인

## ⚙️ 구현해야 할 주요 컴포넌트들

### **1. TestGameManager.cs** (새로 생성 필요)
```csharp
// 테스트 환경에서 게임 상태 관리
// 치트 코드, 테스트 명령어 제공
```

### **2. DebugUI.cs** (새로 생성 필요)
```csharp
// 실시간 디버그 정보 표시
// 테스트 버튼들 (아이템 생성, 레벨업, 몬스터 스폰)
```

### **3. NetworkTestUI.cs** (새로 생성 필요)
```csharp
// 네트워크 상태 표시
// Host/Client 버튼
```

## 🎯 단계별 검증 포인트

### **Phase 1 완료 기준:**
- ✅ Host/Client 연결 성공
- ✅ 플레이어 프리팹 스폰 확인
- ✅ 기본 이동 동작 확인

### **Phase 2 완료 기준:**
- ✅ 플레이어 스탯 UI 표시
- ✅ 인벤토리 열기/닫기
- ✅ 아이템 추가/제거 동작

### **Phase 3 완료 기준:**
- ✅ 몬스터 AI 동작
- ✅ 전투 데미지 계산
- ✅ 경험치/골드 획득

### **Phase 4 완료 기준:**
- ✅ 던전 입장/퇴장
- ✅ 층 전환 시스템
- ✅ 보상 시스템 동작

## 🚦 다음 단계

1. **TestScene.unity 생성**
2. **NetworkManager 설정**
3. **기본 UI 구성**
4. **테스트 매니저 스크립트 작성**
5. **단계별 테스트 실행**

이 가이드를 따라 진행하면 체계적으로 게임의 모든 시스템을 검증할 수 있습니다.