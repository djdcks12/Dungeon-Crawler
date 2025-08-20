# 🎮 Unity TestScene 구성 단계별 가이드

## 🎯 **목표**
완전히 동작하는 멀티플레이어 테스트 환경을 Unity 에디터에서 구축

## 📁 **1단계: 새 씬 생성**

### **씬 파일 생성**
1. Unity Project 창에서 `Assets/Scenes/` 폴더 우클릭
2. `Create > Scene` 선택
3. 씬 이름: `TestScene` 입력
4. 생성된 씬을 더블클릭해서 열기

### **기본 환경 정리**
1. Hierarchy에서 기존 `Main Camera` 삭제 (NetworkManager가 카메라 관리)
2. `Directional Light`는 유지

## 🌐 **2단계: 네트워크 시스템 설정**

### **NetworkManager 추가**
1. `Assets/Prefabs/Shared/NetworkManager.prefab`을 Hierarchy로 드래그
2. NetworkManager 오브젝트 선택
3. Inspector에서 `Network Manager` 컴포넌트 확인:
   ```
   Transport: Unity Transport (UTP)
   Player Prefab: Assets/Prefabs/Game/Player/DungeonPlayer.prefab (설정)
   ```

### **CustomNetworkManager 추가**
1. NetworkManager 오브젝트에 `CustomNetworkManager` 스크립트 추가
2. 필요한 설정값 확인

### **UnityServicesManager 추가**
1. `Assets/Prefabs/Shared/UnityServicesManager.prefab`을 Hierarchy로 드래그

## 🎮 **3단계: 테스트 매니저들 설정**

### **TestGameManager 오브젝트 생성**
1. Hierarchy 우클릭 > `Create Empty`
2. 이름: `TestGameManager`
3. `TestGameManager.cs` 스크립트 추가
4. Inspector 설정:
   ```
   Enable Cheat Codes: ✓
   Cheat Menu Key: F1
   Player Prefab: Assets/Prefabs/Game/Player/DungeonPlayer.prefab
   ```

### **SimpleAuthManager 오브젝트 생성**
1. Hierarchy 우클릭 > `Create Empty`  
2. 이름: `AuthManager`
3. `SimpleAuthManager.cs` 스크립트 추가
4. Inspector 설정:
   ```
   Use Unity Authentication: ✗ (테스트용)
   Auto Sign In: ✓
   Development Mode: ✓
   Test Player Name: "TestPlayer"
   ```

## 🗺️ **4단계: 게임 환경 구축**

### **Ground (바닥) 생성**
1. Hierarchy 우클릭 > `3D Object > Plane`
2. 이름: `Ground`
3. Transform 설정:
   ```
   Position: (0, 0, 0)
   Scale: (5, 1, 5)  // 50x50 크기
   ```
4. Material 적용 (선택사항):
   - `Assets/Prefabs/New Material.mat` 적용

### **Walls (벽) 생성**
1. Hierarchy 우클릭 > `3D Object > Cube`
2. 이름: `Wall_North`  
3. Transform:
   ```
   Position: (0, 1, 25)
   Scale: (50, 2, 1)
   ```
4. 같은 방식으로 4개 벽 생성:
   - `Wall_South`: (0, 1, -25), Scale(50, 2, 1)
   - `Wall_East`: (25, 1, 0), Scale(1, 2, 50)  
   - `Wall_West`: (-25, 1, 0), Scale(1, 2, 50)

### **SpawnPoints 생성**
1. Hierarchy 우클릭 > `Create Empty`
2. 이름: `SpawnPoint_1`
3. Transform: Position(-5, 1, 0)
4. 추가 스폰포인트들:
   - `SpawnPoint_2`: (5, 1, 0)
   - `SpawnPoint_3`: (0, 1, -5)
   - `SpawnPoint_4`: (0, 1, 5)

## 🎨 **5단계: UI 시스템 구성**

### **Canvas 생성**
1. Hierarchy 우클릭 > `UI > Canvas`
2. 이름: `DebugCanvas`
3. Canvas 설정:
   ```
   Render Mode: Screen Space - Overlay
   Canvas Scaler: Scale With Screen Size
   Reference Resolution: 1920x1080
   ```

### **NetworkTestUI 패널**
1. DebugCanvas 우클릭 > `UI > Panel`
2. 이름: `NetworkTestPanel`
3. RectTransform:
   ```
   Anchors: Top-Left
   Position: (200, -100, 0)
   Size: (350, 200)
   ```

#### **NetworkTestUI 버튼들 추가:**

**Host 버튼:**
1. NetworkTestPanel 우클릭 > `UI > Button`
2. 이름: `HostButton` 
3. Text: "Start Host"
4. Position: (0, 50, 0)

**Client 버튼:**
1. NetworkTestPanel 우클릭 > `UI > Button`
2. 이름: `ClientButton`
3. Text: "Start Client"  
4. Position: (0, 0, 0)

**Shutdown 버튼:**
1. NetworkTestPanel 우클릭 > `UI > Button`
2. 이름: `ShutdownButton`
3. Text: "Shutdown"
4. Position: (0, -50, 0)

### **Status Text 추가**
1. NetworkTestPanel 우클릭 > `UI > Text`
2. 이름: `StatusText`
3. Text: "Network Status: Not Connected"
4. Position: (0, 100, 0)

### **NetworkTestUI 스크립트 연결**
1. NetworkTestPanel에 `NetworkTestUI.cs` 추가
2. Inspector에서 UI 요소들 연결:
   ```
   Host Button: HostButton
   Client Button: ClientButton  
   Shutdown Button: ShutdownButton
   Status Text: StatusText
   IP Address Input: (추가 생성 필요)
   Port Input: (추가 생성 필요)
   ```

### **DebugUI 패널 생성**
1. DebugCanvas 우클릭 > `UI > Panel`
2. 이름: `DebugPanel`
3. Position: 우측 상단 (1600, -100, 0)
4. Size: (300, 400)

#### **Debug 정보 텍스트들:**

**FPS Text:**
1. DebugPanel 우클릭 > `UI > Text`
2. 이름: `FPSText`
3. Text: "FPS: 60"
4. Color: Green

**Player Stats Text:**  
1. DebugPanel 우클릭 > `UI > Text`
2. 이름: `PlayerStatsText`
3. Text: "Player Stats:\\nLevel: 1\\nHP: 100/100"

**System Info Text:**
1. DebugPanel 우클릭 > `UI > Text`  
2. 이름: `SystemInfoText`
3. Text: "System Info:\\nMemory: 100MB"

### **DebugUI 스크립트 연결**
1. DebugPanel에 `DebugUI.cs` 추가
2. Inspector에서 연결:
   ```
   Debug Canvas: DebugCanvas
   Network Status Text: StatusText (NetworkTestPanel의)
   Player Stats Text: PlayerStatsText  
   System Info Text: SystemInfoText
   FPS Text: FPSText
   ```

### **CheatMenu 패널 생성**
1. DebugCanvas 우클릭 > `UI > Panel`
2. 이름: `CheatMenuPanel`  
3. 기본적으로 비활성화: Active ✗
4. Position: 중앙 (0, 0, 0)
5. Size: (400, 500)

#### **Cheat 버튼들:**
각각 UI > Button으로 생성:
- `LevelUpButton`: "Level Up (L)"
- `AddGoldButton`: "Add Gold (G)"
- `AddItemButton`: "Add Item (I)"  
- `SpawnMonsterButton`: "Spawn Monster (M)"
- `HealPlayerButton`: "Heal Player (H)"

## ⚙️ **6단계: 게임 매니저들 설정**

### **ItemDatabase 초기화**
1. Hierarchy 우클릭 > `Create Empty`
2. 이름: `ItemDatabaseManager`
3. 스크립트 생성 `ItemDatabaseInitializer.cs`:
```csharp
using UnityEngine;
namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class ItemDatabaseInitializer : MonoBehaviour
    {
        void Start()
        {
            ItemDatabase.Initialize();
            Debug.Log("ItemDatabase initialized");
        }
    }
}
```

### **MonsterSpawner 설정**
1. Hierarchy 우클릭 > `Create Empty`
2. 이름: `MonsterSpawner`
3. `MonsterSpawner.cs` 스크립트 추가
4. Inspector 설정:
   ```
   Enable Auto Spawn: ✗ (수동 테스트용)
   Max Monsters: 5
   Spawn Radius: 10
   ```

## 🔧 **7단계: 최종 연결 및 테스트**

### **TestGameManager 설정 완료**
TestGameManager Inspector에서:
```
Player Prefab: DungeonPlayer.prefab
Spawn Points: SpawnPoint_1, SpawnPoint_2, SpawnPoint_3, SpawnPoint_4
Debug UI: DebugPanel의 DebugUI 컴포넌트
```

### **씬 저장 및 빌드 설정**
1. `Ctrl+S`로 씬 저장
2. `File > Build Settings`
3. `Add Open Scenes` 클릭하여 TestScene 추가
4. TestScene을 인덱스 0으로 설정 (첫 번째 씬)

## 🚀 **8단계: 테스트 실행**

### **Unity 에디터에서 Host 테스트**
1. TestScene이 열린 상태에서 Play 버튼
2. "Start Host" 버튼 클릭
3. Status Text가 "🔸 Host (1 players)"로 변경 확인

### **빌드에서 Client 테스트** 
1. `File > Build and Run`
2. 빌드 완료 후 실행
3. "Start Client" 버튼 클릭
4. Unity 에디터의 Status가 "🔸 Host (2 players)"로 변경
5. 빌드의 Status가 "🔸 Client Connected"로 변경

### **첫 번째 테스트**
1. F1 키로 치트 메뉴 열기
2. L 키로 레벨업 테스트
3. 두 화면에서 모두 레벨 변화 확인
4. WASD로 이동 테스트
5. 상대방 화면에서 이동 동기화 확인

## 📋 **완성된 Hierarchy 구조**
```
TestScene
├── Directional Light
├── NetworkManager (prefab)
├── UnityServicesManager (prefab)
├── TestGameManager
├── AuthManager
├── Environment
│   ├── Ground (Plane)
│   ├── Wall_North (Cube)
│   ├── Wall_South (Cube)
│   ├── Wall_East (Cube)
│   └── Wall_West (Cube)
├── SpawnPoints
│   ├── SpawnPoint_1
│   ├── SpawnPoint_2
│   ├── SpawnPoint_3
│   └── SpawnPoint_4
├── Managers
│   ├── ItemDatabaseManager
│   └── MonsterSpawner
└── DebugCanvas
    ├── NetworkTestPanel
    │   ├── HostButton
    │   ├── ClientButton
    │   ├── ShutdownButton
    │   └── StatusText
    ├── DebugPanel
    │   ├── FPSText
    │   ├── PlayerStatsText
    │   └── SystemInfoText
    └── CheatMenuPanel (inactive)
        ├── LevelUpButton
        ├── AddGoldButton
        ├── AddItemButton
        ├── SpawnMonsterButton
        └── HealPlayerButton
```

## ✅ **구성 완료 체크리스트**
- [ ] TestScene 생성 및 저장
- [ ] NetworkManager 프리팹 추가
- [ ] TestGameManager 설정
- [ ] SimpleAuthManager 설정
- [ ] 기본 게임 환경 (Ground, Walls, SpawnPoints)
- [ ] UI Canvas 및 패널들
- [ ] 모든 버튼 및 텍스트 컴포넌트
- [ ] 스크립트들 연결 완료
- [ ] Build Settings에 씬 추가

**🎯 다음 단계**: 이제 CharacterCreationUI를 만들어서 전체 게임 플로우를 완성하겠습니다!