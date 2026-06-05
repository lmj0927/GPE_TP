# GPE_TP 코드 리팩토링 가이드라인

> 작성 기준: `Assets/1. Scripts` 전체 + `Assets/3. Scenes` 씬/빌드 설정 교차 분석 (2026-06-05)  
> 최종 갱신: 2026-06-05 — **Phase 1 우선: 미사용 코드·폴더 정리** (Test 전면 삭제, Manager·Data 분리)  
> 목적: 안 쓰는 코드 정리, 폴더/책임 재배치, 이후 중복 제거의 **우선순위와 규칙**을 정의한다.

---

## 0. 진행 순서 (확정)

```
Phase 1  미사용 코드 삭제 + 폴더 1차 정리     ← 지금 여기
Phase 2  에이전트 공통 추출·StageReferences
Phase 3  UI·네이밍·품질
Phase 4  맵·스테이지 (콘텐츠 요구 시)
```

**Phase 1에서 하지 않는 것:** 에이전트 Base 추출, UI 통합, rename — 동작·계약 변경 없이 **삭제·이동만**.

---

## 1. 씬·빌드 현황 (사실 관계)

### 1.1 빌드에 포함된 씬 (`EditorBuildSettings`)

| 씬 | 역할 | 주요 스크립트 |
|---|---|---|
| `IntroStory` | 오프닝 스토리 | `IntroStoryController` |
| `MainMenu` | 타이틀·레벨 선택 | `MenuController` |
| `Level1` / `Level2` / `Level3` | 실제 플레이 | `GameManager`, `GameAgent`, `PlayerObstacleSpawner`, `PlayerObstacleInput`, `ObstaclePool`, `MapGenerator`, `RisingLava`, `FollowCamera`, `StoryUI`, `GameEndUI` |

### 1.2 빌드에 **없는** 씬

| 씬 | 역할 | 주요 스크립트 |
|---|---|---|
| `Prototype_Map_Dynamic` | ML 학습/프로토타입 | `EnemyAgentVer2`, `HeuristicSpawnerBot`, `MapGenerator`, `ObstaclePool` 등 |

→ **런타임(출시) 경로**와 **학습 경로**가 씬 단위로 분리되어 있다. 리팩토링 시 이 경계를 유지할 것.

---

## 2. 스크립트 사용 여부 매트릭스

### 2.1 Phase 1 삭제 대상 (빌드 씬·프리팹 미참조)

| 대상 | 상태 | 근거 | Phase 1 조치 |
|---|---|---|---|
| `Test/TestEnemy.cs` | **삭제** | `TestEnemy.prefab`에만 존재, 빌드 씬 없음. Heuristic 검증은 `GameAgent` + `BehaviorType.HeuristicOnly`로 대체 | `.cs` + `.meta` 삭제. 프리팹 `TestEnemy.prefab`은 Unity에서 수동 삭제 |
| `Test/ObstacleSpawnTest.cs` | **삭제** | 씬/프리팹 미참조 | `.cs` + `.meta` 삭제 |
| `Test/` 폴더 | **삭제** | 위 두 파일만 포함 | 폴더 전체 제거 |
| `Runtime/Climber/EnemyAgent.cs` | **레거시 삭제** | 어떤 씬/프리팹에도 미부착. `GameAgent` + `EnemyAgentVer2`로 대체됨 | `.cs` + `.meta` 삭제. ML yaml에 구 Behavior Name 없음 확인 |
| `Runtime/Climber/Config/ClimberRewardWeightConfig.cs` | **레거시 삭제** | `EnemyAgent` 전용 | SO 스크립트 + orphan asset `ClimberRewardWeightConfig.asset` 삭제 |
| `ObstacleBase.IgnoreEnemyCollisions()` | **데드 코드 삭제** | 정의만 있고 호출처 없음 | 메서드 제거 |
| `PlayerObstacleSpawner` 주석의 `ObstacleSpawnTest` 참조 | **정리** | 삭제된 타입 `@see` | 주석에서 제거 |

**Test 코드 정책:** `Dev/` 격리·Assembly Definition **하지 않음**. 전부 삭제하고 Git 이력으로만 보존.

### 2.2 빌드에서 활성 / 학습에서만 활성

| 파일 | 빌드 | 학습 씬 | 비고 |
|---|---|---|---|
| `GameAgent.cs` | ✅ Level1–3 | ❌ | ONNX 추론용 플레이 에이전트 |
| `EnemyAgentVer2.cs` | ❌ | ✅ Prototype + `EnemyResource.prefab` | 학습 전용 |
| `HeuristicSpawnerBot.cs` | ❌ | ✅ Prototype + `Spawner.prefab` | 학습 스포너 |
| `ClimberVer2RewardWeightConfig` | ❌ | ✅ | Ver2 보상 가중치 |
| `SpawnerPatternConfig` | ❌ | ✅ | 스포너 패턴 SO |

### 2.3 공통·인프라 (양 경로 모두 사용)

| 영역 | 파일 |
|---|---|
| FSM | `ClimberStateMachine`, `IClimberState`, `*ClimberState`, `ClimberStateContext`, `ClimberMoveInput` |
| 모터/지면 | `ClimberMotor`, `GroundChecker`, `ClimberMovementConfig` |
| 장애물 | `ObstaclePool`, `ObstacleBase`, `Faller/Bouncer/RollerObstacle`, `PlayerObstacleSpawner`, `PlayerObstacleInput`, `ObstacleTuningConfig` |
| 스테이지 | `MapGenerator`, `RisingLava`, `ClimberGoalTrigger`, `FollowCamera` |
| Manager | `GameManager`, `GameManagerEditor` |
| Data | `UserData`, `UserDataStore`, `UserDataPlayerPrefsEditorWindow` |
| UI | `MenuController`, `StoryUI`, `GameEndUI`, `IntroStoryController`, `UI_TextDialog` |
| 공용 | `Singleton`, `ObjectPool<T>`, `IPoolable`, `IClimberAgent` |

---

## 3. 목표 폴더 구조

### 3.1 Phase 1 적용 구조 (삭제·이동만)

현재 `Manager/`·`Test/`를 정리하고, **Manager**와 **Data**를 최상위로 분리한다.

```
Assets/1. Scripts/
├── Manager/                         ← 게임 흐름·세션 (추가 Manager 클래스 대비 독립 유지)
│   ├── GameManager.cs
│   └── Editor/
│       └── GameManagerEditor.cs
├── Data/                            ← 영속 데이터·저장소 (UserData 및 향후 Data 클래스)
│   ├── UserData.cs                  ← LevelProgress, UserDataStore, UserDataPrefsKeys 포함
│   └── Editor/
│       └── UserDataPlayerPrefsEditorWindow.cs
├── Common/
│   ├── Singleton.cs
│   └── ObjectPooling/
├── Runtime/                         ← Phase 1에서는 내부 구조 유지, Phase 2에서 세분화
│   ├── Climber/
│   ├── Obstacles/
│   └── Stage/
├── UI/
│   └── Editor/
└── (Test/ 삭제됨)
```

**Manager vs Data 분리 원칙**

| 폴더 | 넣을 것 | 넣지 않을 것 |
|---|---|---|
| `Manager/` | `MonoBehaviour` 매니저, 씬/세션 오케스트레이션 (`GameManager`, 향후 `AudioManager` 등) | PlayerPrefs·직렬화 DTO |
| `Data/` | 저장 모델, Store, Prefs 키, Data 전용 Editor | `Update()`/`Start()`가 있는 런타임 드라이버 |

**이동 매핑 (Phase 1)**

| 현재 | 이동 후 |
|---|---|
| `Manager/GameManager.cs` | `Manager/GameManager.cs` (유지) |
| `Manager/Editor/GameManagerEditor.cs` | `Manager/Editor/GameManagerEditor.cs` (유지) |
| `Manager/UserData.cs` | `Data/UserData.cs` |
| `Manager/Editor/UserDataPlayerPrefsEditorWindow.cs` | `Data/Editor/UserDataPlayerPrefsEditorWindow.cs` |

→ `Manager/` 아래에는 **UserData 관련 파일이 없어야** 한다.

### 3.2 Phase 2 이후 목표 구조 (참고)

```
Assets/1. Scripts/
├── Manager/
├── Data/
├── Core/                            ← Common/ rename (선택)
├── Climber/
│   ├── Agents/
│   ├── Fsm/
│   ├── Motor/
│   └── Config/
├── Obstacles/
├── Stage/
├── UI/
└── Editor/                          ← 프로젝트 전역 Editor (필요 시)
```

`Runtime/` 래퍼는 Phase 2에서 `Climber/`·`Obstacles/`·`Stage/`로 풀어 낸다.

---

## 4. 핵심 구조적 문제 (Phase 2+ 동기)

### 4.1 에이전트 2종 — 최대 중복 (TestEnemy 삭제 후)

```
EnemyAgentVer2 (학습, 8 obs, 체력/데미지/보상)   ← Phase 1에서 EnemyAgent 삭제
GameAgent       (플레이, 8 obs, GameManager 연동/애니메이션)
```

**공통으로 반복되는 블록** (각 파일 400~650줄):

- `Initialize` / 스테이지 루트·Goal·Lava 탐색
- `CollectObservations` (거의 동일)
- `Heuristic` / `ReadKeyboardInput` / `ToMoveInput` / `ToHorizontalAction`
- `WriteDiscreteActionMask` (Ver2·GameAgent)
- 애니메이터 동기화 (Ver2·GameAgent)
- `RefreshStageSpan`, `ResolveStageRoot`, `ResolveGoalPoint`

**Phase 2 목표:**

```
ClimberAgentBase (공통 로직)
  ├── TrainingClimberAgent : Agent   ← EnemyAgentVer2
  └── PlayClimberAgent : Agent       ← GameAgent
```

### 4.2 런타임 탐색(`Find`) 의존 — Phase 2

| 위치 | 패턴 | 문제 |
|---|---|---|
| `GameManager` | `FindFirstObjectByType` 폴백 | Inspector 미연결 시 조용히 동작 |
| `GameAgent` / `EnemyAgentVer2` | `GameObject.Find("StageRoot")` | 씬 이름 하드코딩 |
| `Singleton<T>` | 없으면 `new GameObject()` | `GameManager` 자동 생성 위험 |

### 4.3 보상 Config — Phase 2

`ClimberRewardWeightConfig`(레거시) 삭제 후 `ClimberVer2RewardWeightConfig` 단일 SO로 통일.

### 4.4 UI·스토리 이중 구현 — Phase 3

`IntroStoryController` / `StoryUI`+`UI_TextDialog` / `GameEndUI` 타이핑 로직 통합 검토.

### 4.5 `HeuristicSpawnerBot` vs 설계 규칙 — Phase 2

코드(최장 쿨다운) vs 규칙(랜덤) 불일치 — SSOT 정합 필요.

### 4.6 네이밍·도메인 — Phase 3

| 심볼 | 제안 |
|---|---|
| `IsFirstEntry` | `IntroShown` 등 (의미와 이름 반대) |
| `MenuController.isIntroDone` | `GameSessionState` 또는 PlayerPrefs |
| `Ver2` | `Training/` 네이밍 |

---

## 5. 리팩토링 원칙 (작업 시 준수)

1. **Phase 1 = 삭제·이동만** — 로직·관측·액션 계약 변경 금지.
2. **플레이 feel 우선** — Phase 2부터 `GameAgent`·히트 판정 경로 보호.
3. **학습/플레이 분리 유지** — `CommunicatorFactory.Enabled = false` (GameManager) 유지.
4. **SO·Inspector 우선** — C# 이동 후 씬/프리팹 YAML은 Unity가 GUID로 추적. 깨진 참조는 Inspector에서 재할당.
5. **데드 코드는 삭제** — Test는 격리하지 않고 제거.
6. **Manager/Data 경계** — 새 저장 로직은 `Data/`, 새 씬 매니저는 `Manager/`.

---

## 6. 단계별 실행 계획

### Phase 0 — 안전망 (선택)

- [ ] Level1 플레이 스모크: 스폰 3종, 히트, 승/패 UI, 별점 저장
- [ ] `git tag pre-refactor-phase1` (선택)

### Phase 1 — 미사용 코드 삭제 + 폴더 정리 ⬅ **현재 작업**

#### 1-A. 코드·폴더 삭제

| # | 작업 | 검증 |
|---|---|---|
| 1 | `Test/TestEnemy.cs`, `Test/ObstacleSpawnTest.cs` + meta 삭제 | ✅ |
| 2 | `Test/` 폴더 삭제 | ✅ |
| 3 | `EnemyAgent.cs` + meta 삭제 | ✅ |
| 4 | `ClimberRewardWeightConfig.cs` + meta 삭제 | ✅ |
| 5 | `ObstacleBase.IgnoreEnemyCollisions()` 삭제 | ✅ |
| 6 | `PlayerObstacleSpawner` `@see ObstacleSpawnTest` 주석 정리 | ✅ |

#### 1-B. 폴더 이동

| # | 작업 | 검증 |
|---|---|---|
| 7 | `Manager/UserData.cs` → `Data/UserData.cs` | ✅ |
| 8 | `Manager/Editor/UserDataPlayerPrefsEditorWindow.cs` → `Data/Editor/` | ✅ |
| 9 | `Manager/`에 UserData 잔존 파일 없음 확인 | ✅ |

#### 1-C. Unity 에셋 (코드 외 — 수동)

| # | 작업 | 담당 |
|---|---|---|
| 10 | `Assets/5. Data/Climber/ClimberRewardWeightConfig.asset` 삭제 | Unity Project 창 |
| 11 | `Assets/2. Prefabs/Enemys/TestEnemy.prefab` 삭제 | Unity Project 창 |
| 12 | `_Recovery/` 등 TestEnemy 참조 씬 정리 (있을 경우) | 선택 |

#### Phase 1 완료 기준

- [x] `Test/` 폴더 없음
- [x] `EnemyAgent` / `ClimberRewardWeightConfig` 없음
- [x] `Data/UserData.cs` 존재, `Manager/UserData.cs` 없음
- [ ] Level1 컴파일·플레이 스모크 통과 (Unity에서 확인)
- [ ] (선택) Prototype 학습 씬 컴파일 통과

**예상 감소:** ~650줄 (Test ~150 + EnemyAgent ~450 + Config ~25 + 데드 코드)

### Phase 2 — 공통 추출 (중간 리스크)

| 작업 | 설명 |
|---|---|
| `ClimberAgentBase` | GameAgent/Ver2 공통 로직 |
| `StageReferences` | `Find("StageRoot")` 제거 |
| `GameEndData` 분리 | `Manager/GameEndData.cs` 또는 `Data/` |
| `ClimberVer2RewardWeightConfig` → 단일 Reward SO | |
| `HeuristicSpawnerBot` 랜덤 선택 | 설계 규칙 정합 |
| `Runtime/` 하위 → `Climber/`·`Obstacles/`·`Stage/` | 폴더 2차 정리 |

### Phase 3 — UI·네이밍·품질

| 작업 | 설명 |
|---|---|
| 다이얼로그 UI 통합 | `UI_TextDialog` 중심 |
| `GameEndUI` 점수 → SO | |
| `MenuController` 필드 캡슐화 | |
| Agent rename / namespace | |

### Phase 4 — 맵·스테이지 (콘텐츠 요구 시)

`MapGenerator` SO화, `FollowCamera` 개선 등.

---

## 7. 파일별 체크리스트

### Phase 1 — 삭제

| 파일 | 조치 |
|---|---|
| `Test/TestEnemy.cs` | 삭제 |
| `Test/ObstacleSpawnTest.cs` | 삭제 |
| `Runtime/Climber/EnemyAgent.cs` | 삭제 |
| `Runtime/Climber/Config/ClimberRewardWeightConfig.cs` | 삭제 |
| `ObstacleBase.IgnoreEnemyCollisions` | 삭제 |

### Phase 1 — 이동

| 파일 | 이동 |
|---|---|
| `Manager/UserData.cs` | `Data/UserData.cs` |
| `Manager/Editor/UserDataPlayerPrefsEditorWindow.cs` | `Data/Editor/` |

### Phase 1 — 유지 (변경 없음)

| 파일 | 비고 |
|---|---|
| `Manager/GameManager.cs` | Phase 2에서 `GameEndData` 분리 |
| `Manager/Editor/GameManagerEditor.cs` | |
| `GameAgent.cs`, `EnemyAgentVer2.cs` | Phase 2 Base 추출 |

### Phase 2+ — 핫패스

| 파일 | 리팩토링 포인트 |
|---|---|
| `GameAgent.cs` | Base 추출, `StageReferences` |
| `PlayerObstacleInput.cs` | Bouncer 조준선 분리 검토 |
| `GameManager.cs` | `Find` 폴백 정책 |
| `EnemyAgentVer2.cs` | Base 추출, SO 통합 |

---

## 8. 하지 말 것

- Phase 1에서 **에이전트 로직·관측·액션** 변경
- Test 코드를 `Dev/`로 **이동만** 하기 (삭제가 SSOT)
- `UserData`를 `Manager/`에 두기 (Data 폴더로 분리)
- **`EnemyAgent` 삭제 전** ML-Agents config 참조 확인 생략
- Phase 1 PR에 씬·프리팹 대량 수정 (C# + 수동 에셋 가이드 분리)

---

## 9. PR 단위 템플릿 (Phase 1)

```markdown
## Refactor Phase 1 — Cleanup & folder layout

### 삭제
- Test/, EnemyAgent, ClimberRewardWeightConfig, dead code

### 이동
- UserData → Data/

### Unity 수동 작업
- [ ] ClimberRewardWeightConfig.asset 삭제
- [ ] TestEnemy.prefab 삭제

### 검증
- [ ] 컴파일 에러 없음
- [ ] Level1 승리/패배 스모크
- [ ] MainMenu 레벨 잠금/해제 (UserDataStore)

### 계약
- [ ] VectorObservationCount unchanged
- [ ] Action branches unchanged
- [ ] 동작 변경 없음 (삭제·이동만)
```

---

## 10. 우선순위 요약

| 순위 | 작업 | Phase | 리스크 |
|:---:|---|:---:|:---:|
| 1 | `Test/` 전면 삭제 | 1 | 낮음 |
| 2 | `EnemyAgent` + `ClimberRewardWeightConfig` 삭제 | 1 | 낮음 |
| 3 | `UserData` → `Data/` 이동, Manager 분리 | 1 | 낮음 |
| 4 | `ObstacleBase` 데드 코드 삭제 | 1 | 낮음 |
| 5 | `ClimberAgentBase` + `StageReferences` | 2 | 중간 |
| 6 | 보상 SO 통합 | 2 | 중간 |
| 7 | UI 다이얼로그 통합 | 3 | 높음 |

---

## 부록 A — 씬별 컴포넌트 빠른 참조

### Level1 / 2 / 3

`GameManager` · `GameAgent` · `PlayerObstacleSpawner` · `PlayerObstacleInput` · `ObstaclePool` · `MapGenerator` · `RisingLava` · `FollowCamera` · `StoryUI` · `GameEndUI`

### MainMenu

`MenuController` (+ `UserDataStore` via `Data/`)

### IntroStory

`IntroStoryController`

### Prototype_Map_Dynamic (비빌드)

`EnemyAgentVer2` · `HeuristicSpawnerBot` · `MapGenerator` · `ObstaclePool` · `RisingLava` · `PlayerObstacleSpawner`

---

## 부록 B — 관련 프로젝트 규칙

- `game-concept.mdc` — 플레이어/클라이머 역할
- `climber-ai.mdc` — 액션 공간, FSM
- `training-design.mdc` — 스포너·체크포인트
- `code-style.mdc` — C# 스타일
- `obstacle-catalog.mdc` — 3종 장애물
- `unity-yaml-assets.mdc` — C#만 수정, Inspector 가이드

---

*Phase 1 완료 후 이 문서의 체크리스트를 갱신한다.*
