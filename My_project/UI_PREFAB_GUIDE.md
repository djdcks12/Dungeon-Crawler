# UI 프리팹 구성 가이드

**버전**: 3.0  
**최종 업데이트**: 2025-08-20  
**목적**: UI 클래스별 프리팹 생성 및 구성 가이드

---

# 📚 목차

## 🔧 자동 프리팹 생성 (라인 25-45)
## 📁 프리팹 경로 구조 (라인 46-70)
## PlayerHUD 프리팹 구성 (라인 71-150)
## StatsUI 프리팹 구성 (라인 151-230)
## DungeonUI 프리팹 구성 (라인 231-310)
## InventoryUI 프리팹 구성 (라인 311-390)

---

# 🔧 자동 프리팹 생성

## Unity 에디터 도구 사용
1. Unity 에디터에서 `Tools > UI > Generate UI Prefabs` 메뉴 선택
2. UI Prefab Generator 창이 열림
3. `Generate All UI Prefabs` 버튼 클릭
4. Resources/UI 폴더에 기본 프리팹들이 자동 생성됨

## 수동 커스터마이징
- 생성된 프리팹들은 기본 구조만 포함
- 각 프리팹을 열어서 UI 요소들을 원하는 대로 커스터마이징
- 스크립트 컴포넌트의 SerializeField들이 자동으로 연결됨

---

# 📁 프리팹 경로 구조

## Resources 폴더 구조
```
Assets/
└── Resources/
    └── UI/
        ├── PlayerHUD.prefab        # 메인 HUD (항상 표시)
        ├── StatsUI.prefab          # 스탯 창 (C키)
        ├── InventoryUI.prefab      # 인벤토리 (I키)
        ├── EquipmentUI.prefab      # 장비 창 (E키)
        ├── PartyUI.prefab          # 파티 창 (P키)
        ├── DungeonUI.prefab        # 던전 UI (던전 내에서만)
        └── DeathUI.prefab          # 사망 UI (사망 시에만)
```

## UIManager 로드 경로
- `UIManager.cs`에서 각 UI의 Resources 경로가 정의됨
- 경로 변경 시 UIManager의 prefabPath 변수들 수정 필요

```csharp
[SerializeField] private string playerHUDPrefabPath = "UI/PlayerHUD";
[SerializeField] private string statsUIPrefabPath = "UI/StatsUI";
// ... 기타 경로들
```

---

# 🎮 PlayerHUD 프리팹 구성

## 📁 프리팹 위치
- **경로**: `Assets/Prefabs/UI/PlayerHUD.prefab`
- **씬 배치**: 모든 게임플레이 씬의 Canvas 하위

## 🏗️ 계층 구조
```
PlayerHUD (GameObject + PlayerHUD.cs)
├── MainHUDPanel (GameObject)
│   ├── HealthPanel (GameObject)
│   │   ├── HealthSlider (Slider)
│   │   │   ├── Background (Image)
│   │   │   ├── Fill Area (GameObject)
│   │   │   │   └── Fill (Image) - 색상: 초록색 #00FF00
│   │   │   └── Handle Slide Area (GameObject)
│   │   └── HealthText (Text) - "100 / 100"
│   │   
│   ├── ManaPanel (GameObject)
│   │   ├── ManaSlider (Slider)
│   │   │   ├── Background (Image)
│   │   │   ├── Fill Area (GameObject)
│   │   │   │   └── Fill (Image) - 색상: 파란색 #0000FF
│   │   │   └── Handle Slide Area (GameObject)
│   │   └── ManaText (Text) - "50 / 50"
│   │   
│   ├── ExperiencePanel (GameObject)
│   │   ├── ExperienceSlider (Slider)
│   │   │   ├── Background (Image)
│   │   │   ├── Fill Area (GameObject)
│   │   │   │   └── Fill (Image) - 색상: 노란색 #FFFF00
│   │   │   └── Handle Slide Area (GameObject)
│   │   ├── LevelText (Text) - "Lv.1"
│   │   └── ExpText (Text) - "0 / 100"
│   │   
│   ├── ResourcePanel (GameObject)
│   │   ├── GoldPanel (GameObject)
│   │   │   ├── GoldIcon (Image) - 골드 아이콘 스프라이트
│   │   │   └── GoldText (Text) - "1000"
│   │   └── RaceText (Text) - "인간"
│   │   
│   └── StatusEffectsParent (GameObject) - 상태 효과 아이콘 배치용
```

## ⚙️ 컴포넌트 설정

### PlayerHUD.cs 스크립트 연결
```
MainHUDPanel: MainHUDPanel GameObject
HealthPanel: HealthPanel GameObject
ResourcePanel: ResourcePanel GameObject
HealthSlider: HealthPanel/HealthSlider
ManaSlider: ManaPanel/ManaSlider
HealthText: HealthPanel/HealthText
ManaText: ManaPanel/ManaText
ExperienceSlider: ExperiencePanel/ExperienceSlider
LevelText: ExperiencePanel/LevelText
ExpText: ExperiencePanel/ExpText
GoldText: ResourcePanel/GoldPanel/GoldText
GoldIcon: ResourcePanel/GoldPanel/GoldIcon
RaceText: ResourcePanel/RaceText
StatusEffectsParent: StatusEffectsParent
```

## 📐 레이아웃 설정

### Canvas 설정
- **Canvas Scaler**: Scale With Screen Size
- **Reference Resolution**: 1920 x 1080
- **Screen Match Mode**: Match Width Or Height (0.5)

### Anchor 설정
- **HealthPanel**: 좌상단 (0, 1) - Pos X: 50, Pos Y: -50
- **ManaPanel**: 좌상단 (0, 1) - Pos X: 50, Pos Y: -120
- **ExperiencePanel**: 하단 중앙 (0.5, 0) - Pos Y: 50
- **ResourcePanel**: 우상단 (1, 1) - Pos X: -50, Pos Y: -50

### 크기 설정
- **HealthSlider**: Width: 200, Height: 20
- **ManaSlider**: Width: 200, Height: 20
- **ExperienceSlider**: Width: 400, Height: 15
- **Text 요소들**: Auto Size 또는 고정 크기

---

# 📊 StatsUI 프리팹 구성

## 📁 프리팹 위치
- **경로**: `Assets/Prefabs/UI/StatsUI.prefab`
- **씬 배치**: 모든 게임플레이 씬의 Canvas 하위

## 🏗️ 계층 구조
```
StatsUI (GameObject + StatsUI.cs)
├── StatsPanel (GameObject + Image) - 반투명 검은색 배경
│   ├── Header (GameObject)
│   │   ├── TitleText (Text) - "캐릭터 정보"
│   │   ├── CloseButton (Button)
│   │   │   └── ButtonText (Text) - "X"
│   │   
│   ├── PlayerInfoSection (GameObject)
│   │   ├── PlayerNameText (Text) - "Player_12345"
│   │   ├── LevelText (Text) - "Lv.1"
│   │   ├── ExpSlider (Slider)
│   │   ├── ExpText (Text) - "0 / 100"
│   │   └── AvailablePointsText (Text) - "사용 가능 포인트: 0"
│   │   
│   ├── HealthManaSection (GameObject)
│   │   ├── HealthSlider (Slider)
│   │   ├── HealthText (Text) - "100 / 100"
│   │   ├── ManaSlider (Slider)
│   │   └── ManaText (Text) - "50 / 50"
│   │   
│   ├── PrimaryStatsSection (GameObject)
│   │   ├── StrStatElement (GameObject + StatUIElement.cs)
│   │   │   ├── StatNameText (Text) - "힘"
│   │   │   ├── StatValueText (Text) - "10"
│   │   │   ├── PlusButton (Button) - "+"
│   │   │   └── MinusButton (Button) - "-"
│   │   ├── AgiStatElement (GameObject + StatUIElement.cs)
│   │   │   └── [동일한 구조]
│   │   ├── VitStatElement (GameObject + StatUIElement.cs)
│   │   ├── IntStatElement (GameObject + StatUIElement.cs)
│   │   ├── DefStatElement (GameObject + StatUIElement.cs)
│   │   ├── MdefStatElement (GameObject + StatUIElement.cs)
│   │   └── LukStatElement (GameObject + StatUIElement.cs)
│   │   
│   └── DerivedStatsSection (GameObject)
│       ├── AttackDamageText (Text) - "물리 공격력: 20"
│       ├── MagicDamageText (Text) - "마법 공격력: 20"
│       ├── MoveSpeedText (Text) - "이동속도: 5.0"
│       ├── AttackSpeedText (Text) - "공격속도: 1.0"
│       ├── CritChanceText (Text) - "크리티컬 확률: 0%"
│       └── CritDamageText (Text) - "크리티컬 데미지: 150%"
```

## ⚙️ StatUIElement.cs 컴포넌트 설정
각 스탯 요소마다 개별적으로 연결:
```
StatNameText: StatNameText 컴포넌트
StatValueText: StatValueText 컴포넌트  
PlusButton: PlusButton 컴포넌트
MinusButton: MinusButton 컴포넌트
```

---

# 🏰 DungeonUI 프리팹 구성

## 📁 프리팹 위치
- **경로**: `Assets/Prefabs/UI/DungeonUI.prefab`
- **씬 배치**: 던전 씬의 Canvas 하위

## 🏗️ 계층 구조
```
DungeonUI (GameObject + DungeonUI.cs)
├── DungeonPanel (GameObject + Image) - 반투명 어두운 배경
│   ├── DungeonStatusPanel (GameObject)
│   │   ├── DungeonNameText (Text) - "초급자 던전"
│   │   ├── CurrentFloorText (Text) - "1 / 10"
│   │   ├── RemainingTimeText (Text) - "현재층: 09:30 | 총: 45:20"
│   │   ├── DungeonStateText (Text) - "진행 중"
│   │   └── TimeProgressSlider (Slider) - 시간 진행도
│   │   
│   ├── PlayerListPanel (GameObject)
│   │   ├── AlivePlayersText (Text) - "생존자: 8 / 10"
│   │   ├── PlayerListScrollView (ScrollRect)
│   │   │   └── PlayerListContent (GameObject) - Vertical Layout Group
│   │   │       └── PlayerListItems... (동적 생성)
│   │   
│   ├── ProgressPanel (GameObject)
│   │   ├── MonstersRemainingText (Text) - "남은 몬스터: 5"
│   │   ├── ObjectiveText (Text) - "모든 몬스터를 처치하고 출구를 찾으세요"
│   │   └── FloorProgressSlider (Slider) - 층 진행도
│   │   
│   └── ActionButtons (GameObject)
│       └── AbandonButton (Button) - "던전 포기"
│       
└── RewardPanel (GameObject + Image) - 보상 표시용
    ├── RewardTitle (Text) - "던전 완료!"
    ├── ExpRewardText (Text) - "경험치: +1500"
    ├── GoldRewardText (Text) - "골드: +2000"
    ├── ItemRewardScrollView (ScrollRect)
    │   └── ItemRewardContent (GameObject)
    └── CloseRewardButton (Button) - "확인"
```

---

# 🎒 InventoryUI 프리팹 구성

## 📁 프리팹 위치
- **경로**: `Assets/Prefabs/UI/InventoryUI.prefab`
- **씬 배치**: 모든 게임플레이 씬의 Canvas 하위

## 🏗️ 계층 구조
```
InventoryUI (GameObject + InventoryUI.cs)
└── InventoryPanel (GameObject + Image) - 인벤토리 배경
    ├── Header (GameObject)
    │   ├── TitleText (Text) - "인벤토리"
    │   └── CloseButton (Button) - "X"
    │   
    ├── InventoryGrid (GameObject + Grid Layout Group)
    │   ├── Slot00 (GameObject + InventorySlot.cs)
    │   │   ├── SlotBackground (Image) - 슬롯 배경
    │   │   ├── ItemIcon (Image) - 아이템 아이콘
    │   │   └── StackText (Text) - 스택 개수
    │   ├── Slot01 (GameObject + InventorySlot.cs)
    │   │   └── [동일한 구조]
    │   └── ... (총 30개 슬롯)
    │   
    └── ActionButtons (GameObject)
        ├── SortButton (Button) - "정렬"
        ├── AutoLootButton (Button) - "자동 수집"
        └── DropAllButton (Button) - "모두 버리기"
```

## ⚙️ Grid Layout Group 설정
- **Cell Size**: 64 x 64
- **Spacing**: 2 x 2
- **Constraint**: Fixed Column Count (6)
- **Child Alignment**: Upper Left

---

# 🛠️ 프리팹 생성 단계별 가이드

## 1단계: Canvas 설정
1. Hierarchy에서 우클릭 → UI → Canvas
2. Canvas Scaler 컴포넌트 추가 및 설정
3. Graphic Raycaster 컴포넌트 확인

## 2단계: 기본 UI 구조 생성
1. 각 UI별 루트 GameObject 생성
2. 해당 UI 스크립트 컴포넌트 추가
3. 계층 구조에 따라 하위 GameObject들 생성

## 3단계: 이미지 및 버튼 설정
1. UI → Image로 배경 패널들 생성
2. UI → Button으로 버튼들 생성
3. UI → Text로 텍스트 요소들 생성
4. UI → Slider로 슬라이더들 생성

## 4단계: 앵커 및 위치 설정
1. Rect Transform의 Anchor 설정
2. Position, Size 조정
3. Layout Group 컴포넌트 추가 (필요시)

## 5단계: 스크립트 연결
1. 각 UI 스크립트의 SerializeField들 연결
2. 버튼 onClick 이벤트 연결
3. 슬라이더 onValueChanged 이벤트 연결 (필요시)

## 6단계: 프리팹 저장
1. Assets/Prefabs/UI/ 폴더에 저장
2. 씬에서 테스트 후 Apply 버튼으로 프리팹 업데이트

---

# 🎨 UI 스타일 가이드

## 색상 팔레트
- **배경**: #2D2D30 (어두운 회색)
- **UI 패널**: #3E3E42 (중간 회색)
- **텍스트**: #FFFFFF (흰색)
- **액센트**: #007ACC (파란색)
- **위험**: #F44747 (빨간색)
- **성공**: #4EC9B0 (청록색)

## 폰트 설정
- **기본 폰트 크기**: 14px
- **제목 폰트 크기**: 18px
- **작은 텍스트**: 12px

## 간격 및 크기
- **기본 패딩**: 10px
- **요소 간격**: 5px
- **버튼 크기**: 100 x 30px
- **아이콘 크기**: 32 x 32px

---

이 가이드를 따라 프리팹을 생성하면 각 UI 클래스가 올바르게 작동합니다.