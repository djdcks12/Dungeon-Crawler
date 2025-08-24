# 🎯 몬스터 시스템 완전 가이드

새로운 몬스터 시스템에서 종족 데이터와 개체 데이터를 만드는 완전한 가이드입니다.

## 📖 목차
1. [시스템 개요](#시스템-개요)
2. [스킬 데이터 생성](#1단계-몬스터-스킬-데이터-생성)
3. [종족 데이터 생성](#2단계-몬스터-종족-데이터-생성)
4. [개체 데이터 생성](#3단계-몬스터-개체-데이터-생성)
5. [스포너 설정](#4단계-몬스터-스포너-설정)
6. [실전 예시](#5단계-실전-예시---완전한-몬스터-생성)
7. [고급 설정](#6단계-고급-설정-팁)

---

## 📋 시스템 개요

### **새로운 등급 시스템 (80-120)**
- **80등급**: 약한 개체 (기본 능력치의 80%)
- **100등급**: 표준 개체 (기본 능력치의 100%)
- **120등급**: 강력한 개체 (기본 능력치의 120%)
- **연속적 스케일링**: 모든 능력치가 등급에 따라 부드럽게 증가

### **계층적 구조**
```
MonsterRaceData (종족)
├── 기본 스탯 정의
├── 종족 공통 스킬
└── 종족 공통 드롭

MonsterVariantData (개체)
├── 종족 데이터 참조
├── 스탯 편차 적용
├── 개체별 추가 스킬
└── 개체별 특별 드롭
```

---

## 📋 1단계: 몬스터 스킬 데이터 생성

### **1.1 스킬 데이터 생성**
1. **Unity 에디터**에서 `Assets` 폴더 우클릭
2. `Create > Dungeon Crawler > Monster > Skill Data` 선택
3. 파일명: `GoblinRage` (예시)

### **1.2 스킬 설정 예시 - "고블린 분노"**
```yaml
# Basic Info
Skill Name: "Goblin Rage"
Description: "고블린의 야생적 분노가 폭발하여 힘과 체력이 증가합니다"
Skill Icon: [스킬 아이콘 이미지]

# Skill Type
Skill Type: Passive
Category: DamageBonus

# Skill Effects (Range-based)
Strength Bonus:
  Min Value: 8.0    # 80등급에서 +8
  Max Value: 15.0   # 120등급에서 +15

Vitality Bonus:
  Min Value: 5.0    # 80등급에서 +5  
  Max Value: 12.0   # 120등급에서 +12

Damage Multiplier Range:
  Min Value: 1.1    # 80등급에서 110% 데미지
  Max Value: 1.3    # 120등급에서 130% 데미지

# Activation Conditions
Cooldown: 0 (패시브 스킬)
Mana Cost: 0
Range: 0
Trigger: Manual
```

### **1.3 스킬 유형별 설정 가이드**

#### **패시브 스킬 (항상 적용)**
```yaml
Skill Type: Passive
# 스탯 보너스 중심으로 설정
Strength Bonus: Min 5.0, Max 12.0
Agility Bonus: Min 3.0, Max 8.0
```

#### **액티브 스킬 (조건부 발동)**
```yaml
Skill Type: Active
Trigger: OnLowHealth  # 체력 30% 이하에서 발동
Cooldown: 15.0        # 15초 쿨다운
Damage Multiplier Range: Min 1.5, Max 2.0  # 강력한 효과
Duration Range: Min 3.0, Max 5.0  # 지속 시간
```

---

## 📋 2단계: 몬스터 종족 데이터 생성

### **2.1 종족 데이터 생성**
1. **Unity 에디터**에서 `Assets` 폴더 우클릭
2. `Create > Dungeon Crawler > Monster > Race Data` 선택  
3. 파일명: `GoblinRaceData`

### **2.2 종족 설정 예시 - 고블린족**
```yaml
# Race Information
Race Type: Goblin
Race Name: "고블린족"
Description: "민첩하고 교활한 소형 인간형 몬스터. 무리를 지어 다니며 빠른 공격을 선호한다."
Race Icon: [종족 아이콘 이미지]

# Base Stats (80등급 기준값)
Base Stats:
  Strength: 12.0
  Agility: 18.0      # 고블린은 민첩형 특화
  Vitality: 10.0
  Intelligence: 8.0
  Defense: 6.0
  Magic Defense: 4.0
  Luck: 12.0
  Stability: 10.0

# Stat Growth Per Grade (등급당 성장률)
Grade Growth:
  Strength Growth: 0.8
  Agility Growth: 1.2    # 민첩이 가장 빠르게 성장
  Vitality Growth: 0.6
  Intelligence Growth: 0.4
  Defense Growth: 0.5
  Magic Defense Growth: 0.3
  Luck Growth: 0.7
  Stability Growth: 0.6

# Race Mandatory Skills (모든 고블린이 보유)
Mandatory Skills:
  - Skill Data: GoblinRage
    Min Grade: 80.0
    Max Grade: 120.0
  - Skill Data: QuickStep
    Min Grade: 85.0
    Max Grade: 120.0

# Available Optional Skills (등급에 따라 선택적 획득)
Available Skills:
  - Skill Data: PoisonBlade
    Min Grade: 90.0
    Max Grade: 120.0
  - Skill Data: SneakAttack
    Min Grade: 95.0
    Max Grade: 120.0
  - Skill Data: PackHunter
    Min Grade: 105.0
    Max Grade: 120.0

# Experience & Drops
Base Experience: 45
Base Gold: 8
Soul Drop Rate: 0.0008 (0.08%)

# Common Item Drops (모든 고블린이 드롭 가능)
Common Drops:
  - Item: 고블린 이빨
    Drop Rate: 0.3
    Min Quantity: 1
    Max Quantity: 2
    Min Level: 80
    Max Level: 120

  - Item: 낡은 천
    Drop Rate: 0.2
    Min Quantity: 1
    Max Quantity: 1
    Min Level: 80
    Max Level: 100

# Rare Item Drops (낮은 확률로 드롭)
Rare Drops:
  - Item: 고블린 독주머니
    Drop Rate: 0.05
    Min Quantity: 1
    Max Quantity: 1
    Min Level: 100
    Max Level: 120

  - Item: 날카로운 발톱
    Drop Rate: 0.02
    Min Quantity: 1
    Max Quantity: 1
    Min Level: 110
    Max Level: 120
```

### **2.3 종족별 특화 가이드**

#### **민첩형 종족 (고블린, 코볼트 등)**
```yaml
특징: 높은 Agility, 낮은 Vitality
강점: 빠른 이동, 높은 회피율, 기습 공격
약점: 낮은 체력, 약한 방어력
추천 스킬: 이동속도 증가, 회피율 증가, 기습 공격
```

#### **힘형 종족 (오크, 트롤 등)**
```yaml
특징: 높은 Strength, 높은 Vitality
강점: 강한 물리 공격, 높은 체력
약점: 느린 이동, 낮은 회피율
추천 스킬: 공격력 증가, 체력 증가, 광폭화
```

#### **마법형 종족 (언데드, 정령 등)**
```yaml
특징: 높은 Intelligence, 높은 Magic Defense
강점: 강한 마법 공격, 마법 저항
약점: 낮은 물리 방어력, 제한적 마나
추천 스킬: 마법 공격, 마나 회복, 원소 저항
```

---

## 📋 3단계: 몬스터 개체 데이터 생성

### **3.1 개체 데이터 생성**
1. **Unity 에디터**에서 `Assets` 폴더 우클릭
2. `Create > Dungeon Crawler > Monster > Variant Data` 선택
3. 파일명: `GoblinWarriorVariant`

### **3.2 개체 설정 예시 - 고블린 워리어**
```yaml
# Variant Information
Variant Name: "Goblin Warrior"
Description: "무기를 든 고블린 전사. 일반 고블린보다 강하고 공격적이다."
Variant Icon: [개체 아이콘 이미지]
Prefab: [몬스터 GameObject 프리팹]

# Race Reference
Base Race: GoblinRaceData

# Stat Variations (종족 스탯에서의 편차)
Stat Min Variance:
  Strength: 2.0     # 최소 +2 (무기 착용 효과)
  Agility: -1.0     # 최소 -1 (무거운 장비)
  Vitality: 3.0     # 최소 +3 (단련된 체력)
  Intelligence: 0.0 # 변화 없음
  Defense: 4.0      # 최소 +4 (갑옷 착용)
  Magic Defense: 0.0
  Luck: 0.0
  Stability: 1.0    # 최소 +1

Stat Max Variance:
  Strength: 8.0     # 최대 +8
  Agility: 1.0      # 최대 +1
  Vitality: 7.0     # 최대 +7
  Intelligence: 0.0
  Defense: 10.0     # 최대 +10
  Magic Defense: 2.0 # 최대 +2
  Luck: 2.0         # 최대 +2
  Stability: 4.0    # 최대 +4

# Variant Mandatory Skills (워리어 전용 필수 스킬)
Variant Mandatory Skills:
  - Skill Data: ShieldBlock
    Min Grade: 80.0
    Max Grade: 120.0
  - Skill Data: WarriorStance
    Min Grade: 85.0
    Max Grade: 120.0

# Variant Available Skills (워리어 전용 선택 스킬)
Variant Available Skills:
  - Skill Data: PowerStrike
    Min Grade: 90.0
    Max Grade: 120.0
  - Skill Data: DefensiveFormation
    Min Grade: 95.0
    Max Grade: 120.0
  - Skill Data: WeaponMastery
    Min Grade: 110.0
    Max Grade: 120.0

# Spawn Settings
Spawn Weight: 1.0
Min Floor: 1
Max Floor: 15

# AI Behavior
Preferred AI Type: Aggressive
Aggression Multiplier: 1.2

# Variant Drops (워리어만의 특별 드롭)
Variant Drops:
  - Item: 고블린 전사의 투구
    Drop Rate: 0.01
    Min Quantity: 1
    Max Quantity: 1
    Min Level: 105
    Max Level: 120

  - Item: 낡은 검
    Drop Rate: 0.03
    Min Quantity: 1
    Max Quantity: 1
    Min Level: 90
    Max Level: 120

  - Item: 가죽 갑옷 조각
    Drop Rate: 0.08
    Min Quantity: 1
    Max Quantity: 3
    Min Level: 80
    Max Level: 120
```

### **3.3 개체 역할별 설정 가이드**

#### **전사형 개체 (Warrior)**
```yaml
스탯 특화: Strength↑, Defense↑, Vitality↑
필수 스킬: 방어 기술, 무기 숙련
선택 스킬: 강화 공격, 방어 자세, 도발
AI 타입: Aggressive
드롭: 무기, 방어구 위주
```

#### **궁수형 개체 (Archer)**
```yaml
스탯 특화: Agility↑, Luck↑, Intelligence(원거리 조준)↑
필수 스킬: 정확한 사격, 거리 유지
선택 스킬: 연사, 관통사격, 독화살
AI 타입: Defensive
드롭: 화살, 활, 원거리 장비
```

#### **마법사형 개체 (Shaman)**
```yaml
스탯 특화: Intelligence↑, Magic Defense↑, Stability↑
필수 스킬: 마법 시전, 마나 관리
선택 스킬: 원소 공격, 치유, 버프/디버프
AI 타입: Balanced
드롭: 마법서, 스태프, 마법 재료
```

---

## 📋 4단계: 몬스터 스포너 설정

### **4.1 스포너 컴포넌트 설정**
1. **Hierarchy**에 빈 GameObject 생성 → `GoblinSpawner`
2. `MonsterEntitySpawner` 컴포넌트 추가

### **4.2 스포너 설정**
```yaml
# Spawn Configuration
Spawn Data Sets:
  Element 0:
    Race Data: GoblinRaceData
    Variant Data: GoblinWarriorVariant
    Base Prefab: [몬스터 GameObject 프리팹]
    Spawn Weight: 1.0
    Description: "Goblin Warrior"

  Element 1:
    Race Data: GoblinRaceData  
    Variant Data: GoblinArcherVariant
    Base Prefab: [궁수 GameObject 프리팹]
    Spawn Weight: 0.7
    Description: "Goblin Archer"

# Spawn Points
Spawn Points: [빈 GameObject들을 생성해서 스폰 위치로 설정]

# Spawn Limits
Max Active Monsters: 10
Spawn Interval: 15.0 (초)
Auto Spawn: true

# Spawn Conditions
Player Detection Range: 20.0
Min Players Nearby: 1
Spawn Only When Players Near: true

# Grade Range (80-120 시스템)
Min Grade: 85.0
Max Grade: 115.0
Average Grade: 100.0        # 평균값 (정규분포 중심)
Grade Standard Deviation: 8.0  # 표준편차 (분포 폭)

# Floor-based Scaling
Current Floor: 1
Floor Difficulty Multiplier: 1.0
```

### **4.3 다중 스포너 설정 예시**
```yaml
# 초급 지역 스포너
Min Grade: 80.0
Max Grade: 95.0
Average Grade: 87.0
Standard Deviation: 4.0

# 중급 지역 스포너  
Min Grade: 95.0
Max Grade: 110.0
Average Grade: 102.0
Standard Deviation: 6.0

# 고급 지역 스포너
Min Grade: 110.0
Max Grade: 120.0
Average Grade: 115.0
Standard Deviation: 3.0
```

---

## 📋 5단계: 실전 예시 - 완전한 몬스터 생성

### **5.1 다중 스탯 영향 스킬 - "고블린 민첩성"**
```yaml
Skill Name: "Goblin Agility"
Description: "고블린의 타고난 민첩성이 이동속도와 민첩성을 동시에 향상시킵니다"

# 여러 스탯에 동시 영향
Skill Effects:
  Agility Bonus:
    Min Value: 3.0
    Max Value: 7.0

  Speed Multiplier Range:
    Min Value: 1.05      # 5% 이동속도 증가
    Max Value: 1.15      # 15% 이동속도 증가

  Luck Bonus:
    Min Value: 2.0       # 회피율 증가를 위한 운 보너스
    Max Value: 5.0

Skill Type: Passive
Category: MovementSpeed
```

### **5.2 조건부 발동 스킬 - "광전사의 분노"**
```yaml
Skill Name: "Berserker Rage"
Description: "체력이 30% 이하로 떨어지면 광폭한 분노가 폭발합니다"

Skill Effects:
  Damage Multiplier Range:
    Min Value: 1.8       # 180% 데미지
    Max Value: 2.5       # 250% 데미지

  Speed Multiplier Range:
    Min Value: 1.3       # 30% 공격속도 증가
    Max Value: 1.6       # 60% 공격속도 증가

  Duration Range:
    Min Value: 8.0       # 8초 지속
    Max Value: 12.0      # 12초 지속

Skill Type: Active
Trigger: OnLowHealth
Cooldown: 45.0
Category: SpecialAbility
```

### **5.3 완전한 종족 설계 - 오크족**
```yaml
# === OrcRaceData ===
Race Type: Orc
Race Name: "오크족"
Description: "강인한 체격을 가진 거대한 인간형 몬스터. 압도적인 힘과 높은 체력이 특징."

Base Stats:
  Strength: 20.0       # 힘 특화
  Agility: 8.0         # 둔함
  Vitality: 18.0       # 높은 체력
  Intelligence: 6.0    # 낮은 지능
  Defense: 15.0        # 높은 방어력
  Magic Defense: 4.0   # 낮은 마법 저항
  Luck: 5.0           # 낮은 운
  Stability: 12.0     # 안정적

Mandatory Skills:
  - Orcish Strength (오크의 힘): 기본 힘 증가
  - Thick Skin (두꺼운 피부): 기본 방어력 증가

Available Skills:
  - Rage (분노): 공격력 일시 증가
  - Intimidation (위협): 적 능력치 감소  
  - Weapon Throw (무기 투척): 원거리 공격
  - Battle Cry (전투 함성): 주변 아군 강화
```

### **5.4 개체 다양성 설계**
```yaml
# === 오크 전사 (Orc Warrior) ===
특징: 근접 전투 특화, 높은 공격력과 방어력
스탯 편차: STR+5~12, DEF+3~8, VIT+4~10
전용 스킬: Shield Slam, Cleave Attack
드롭: 오크 도끼, 철 갑옷

# === 오크 버서커 (Orc Berserker) ===  
특징: 극한 공격력, 낮은 방어력
스탯 편차: STR+8~15, AGI+2~5, DEF-2~+2
전용 스킬: Frenzy, Reckless Charge
드롭: 거대 도끼, 분노의 물약

# === 오크 샤먼 (Orc Shaman) ===
특징: 마법 지원, 치유 및 버프
스탯 편차: INT+6~12, M.DEF+4~8, STR-1~+3  
전용 스킬: Healing Wave, Earth Spike
드롭: 샤먼 스태프, 마법 토템
```

---

## 📋 6단계: 고급 설정 팁

### **6.1 등급별 균형 조정 가이드**

#### **등급 구간별 특성**
```yaml
80-90등급 (하급):
  선택 스킬: 1-2개
  특징: 기본 능력치, 단순한 패턴
  설계 목적: 초보자용, 기본 학습

90-100등급 (중급):
  선택 스킬: 2-3개  
  특징: 약간의 능력치 증가, 중간 복잡도
  설계 목적: 표준 도전, 균형잡힌 전투

100-110등급 (상급):
  선택 스킬: 3-4개
  특징: 눈에 띄는 강화, 복합적 패턴
  설계 목적: 숙련자용, 전략적 접근 필요

110-120등급 (최상급):
  선택 스킬: 4-5개
  특징: 엘리트 수준, 복잡한 메커니즘
  설계 목적: 전문가용, 완벽한 조합 필요
```

### **6.2 드롭 아이템 설계 철학**

#### **드롭 계층 구조**
```yaml
종족 공통 드롭:
  Common (20-50%): 기본 재료, 소모품
  Rare (2-8%): 고급 재료, 특수 소모품
  
개체별 특별 드롭:
  Equipment (1-5%): 해당 개체 테마의 장비
  Unique (0.1-1%): 고유 아이템, 수집품

등급별 드롭 확률:
  80등급: 기본 확률
  100등급: 1.0배 확률 (기준)
  120등급: 1.2배 확률 (희귀템은 1.5배)
```

#### **드롭 테마 일관성**
```yaml
고블린족:
  - 테마: 날카로움, 민첩성, 독
  - 드롭: 날카로운 발톱, 독주머니, 가벼운 장비

오크족:
  - 테마: 힘, 무력, 전투
  - 드롭: 거대한 무기, 두꺼운 갑옷, 전투 물약

언데드족:
  - 테마: 죽음, 어둠, 마법
  - 드롭: 뼈 조각, 어둠의 정수, 네크로만시 재료
```

### **6.3 AI 행동 패턴 최적화**

#### **AI 타입별 설정**
```yaml
Aggressive (공격적):
  - 플레이어 발견 즉시 돌진
  - 근접 전투 선호
  - 체력이 낮아져도 계속 공격
  - 적합한 몬스터: 전사, 버서커

Defensive (수비적):
  - 일정 거리 유지 시도
  - 원거리 공격 선호
  - 체력이 낮으면 도망 시도
  - 적합한 몬스터: 궁수, 마법사

Balanced (균형):
  - 상황에 따라 전략 변경
  - 근거리/원거리 공격 혼용
  - 체력에 따라 전술 조정
  - 적합한 몬스터: 팔라딘, 하이브리드
```

### **6.4 스킬 밸런싱 기법**

#### **패시브 vs 액티브 밸런스**
```yaml
패시브 스킬:
  - 지속 효과: 약-중간 수준
  - 범위: 단일 또는 소수 스탯
  - 예시: STR +5~10, 항상 적용

액티브 스킬:
  - 순간 효과: 강력-매우 강력
  - 범위: 광범위한 영향
  - 제약: 쿨다운, 발동 조건
  - 예시: 데미지 200% 증가, 10초간
```

#### **다중 효과 스킬 설계**
```yaml
단일 효과:
  - 명확한 목적
  - 예측 가능한 결과
  - 예시: "힘 +10"

다중 효과:
  - 복합적 강화
  - 시너지 효과
  - 예시: "힘 +5, 공격속도 +10%, 치명타 확률 +3%"

트레이드오프 효과:
  - 장점과 단점 동시 존재
  - 전략적 선택 유도
  - 예시: "공격력 +50%, 방어력 -20%"
```

---

## 🔧 7단계: 실무 체크리스트

### **7.1 스킬 생성 체크리스트**
- [ ] 스킬명이 명확하고 테마에 맞는가?
- [ ] Min/Max 값이 적절한 범위 내에 있는가?
- [ ] 80등급과 120등급 효과 차이가 적당한가?
- [ ] 패시브/액티브 분류가 올바른가?
- [ ] 다른 스킬과 시너지가 있는가?

### **7.2 종족 생성 체크리스트**
- [ ] 종족 컨셉이 명확한가?
- [ ] 기본 스탯 배분이 테마에 맞는가?
- [ ] 필수 스킬이 종족 특성을 잘 나타내는가?
- [ ] 드롭 아이템이 테마와 일치하는가?
- [ ] 다른 종족과 차별화되는가?

### **7.3 개체 생성 체크리스트**
- [ ] 개체명이 역할을 잘 나타내는가?
- [ ] 스탯 편차가 적절한가?
- [ ] 개체별 스킬이 역할에 맞는가?
- [ ] 스폰 층수 설정이 적당한가?
- [ ] AI 타입이 개체 특성에 맞는가?

### **7.4 밸런스 테스트 가이드**
```yaml
단계 1: 단일 개체 테스트
- 80/100/120등급 각각 생성
- 스탯, 스킬, 드롭 확인
- 플레이어와의 전투 균형 체크

단계 2: 그룹 전투 테스트  
- 여러 개체 동시 스폰
- 다양한 등급 조합 테스트
- 전투 지속 시간 측정

단계 3: 진행도별 테스트
- 플레이어 레벨별 적절성 확인
- 보상과 난이도의 균형
- 장기적 게임플레이 영향
```

---

## 📊 8단계: 참고 수치표

### **8.1 권장 스탯 범위**
| 종족 타입 | STR | AGI | VIT | INT | DEF | M.DEF | LUK | STAB |
|----------|-----|-----|-----|-----|-----|--------|-----|------|
| 전사형   | 15-25 | 8-12 | 15-20 | 5-10 | 12-18 | 4-8 | 8-12 | 10-15 |
| 민첩형   | 10-15 | 18-25 | 8-12 | 8-15 | 5-10 | 4-8 | 15-20 | 8-12 |
| 마법형   | 8-12 | 10-15 | 10-15 | 18-25 | 6-10 | 15-22 | 10-15 | 12-18 |
| 균형형   | 12-16 | 12-16 | 12-16 | 12-16 | 10-14 | 10-14 | 10-14 | 12-16 |

### **8.2 권장 드롭 확률**
| 드롭 등급 | 확률 범위 | 용도 |
|----------|-----------|------|
| 매우 흔함 | 40-70% | 기본 재료, 소모품 |
| 흔함     | 15-30% | 일반 재료, 저급 장비 |
| 보통     | 5-15%  | 중급 재료, 일반 장비 |
| 희귀     | 1-5%   | 고급 재료, 희귀 장비 |
| 매우 희귀 | 0.1-1% | 특수 재료, 유니크 장비 |

### **8.3 등급별 스킬 개수 권장**
| 등급 범위 | 필수 스킬 | 선택 스킬 | 총 스킬 |
|----------|-----------|-----------|---------|
| 80-90    | 1-2개     | 0-1개     | 1-3개   |
| 90-100   | 1-2개     | 1-2개     | 2-4개   |
| 100-110  | 2-3개     | 2-3개     | 4-6개   |
| 110-120  | 2-3개     | 3-4개     | 5-7개   |

---

## 💡 마지막 팁

### **자주하는 실수들**
1. **과도한 스탯 편차**: 편차는 기본값의 ±50% 이내 권장
2. **스킬 효과 과다**: 한 스킬이 너무 많은 효과를 가지지 않도록
3. **드롭 확률 과다**: 총 드롭 확률이 100%를 넘지 않도록 주의
4. **테마 불일치**: 스킬, 스탯, 드롭이 몬스터 컨셉과 맞는지 확인

### **성공적인 몬스터 설계를 위한 핵심**
- **명확한 컨셉**: 각 몬스터가 고유한 역할과 특징을 가져야 함
- **점진적 난이도**: 등급별로 자연스러운 난이도 상승 곡선
- **보상 균형**: 난이도에 맞는 적절한 보상 제공
- **다양성 확보**: 같은 종족 내에서도 다양한 플레이 경험

---

이 가이드를 따라 다양하고 균형잡힌 몬스터들을 만들어보세요! 
각 몬스터는 고유한 특성과 전략을 가지게 될 것입니다.

**Happy Monster Creating! 🎮**