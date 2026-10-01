# 🎮 Game Loop

Block Racing의 게임 로직은 **Server Tick 기반의 Fixed Update** 방식으로 동작합니다.

Room이 게임을 시작하면 `GameSimulation`을 생성하고, 이후 매 Tick마다 입력 처리부터 게임 상태 업데이트, 충돌 검사, 종료 조건 확인, Snapshot 생성까지 수행합니다.

```text
Room.Update()
     │
     ▼
GameSimulation.Update()
     │
     ├── ProcessInput()
     ├── UpdatePlayers()
     ├── AttackSystem.Update()
     ├── UpdateBlockSystem()
     ├── LineClearSystem
     ├── LaneScrollSystem
     ├── CollisionSystem.Update()
     ├── GameState.UpdateTick()
     └── GameEndSystem.Update()
             │
             ▼
       GameEndResult?
             │
             ├── Game End
             │
             └── Snapshot Sync
```

현재 서버 Tick은 **50ms(20 Tick/s)** 로 동작합니다.

---

# 🔄 GameSimulation

`GameSimulation`은 실제 게임 규칙과 상태 변화를 담당하는 핵심 클래스입니다.

Room은 게임의 생명주기와 네트워크 전송을 담당하고, `GameSimulation`은 게임 내부 상태의 계산을 담당하도록 역할을 분리했습니다.

```csharp
public GameEndResult? Update(float deltaTime)
{
    ProcessInput();

    UpdatePlayers(deltaTime);

    _attackSystem.Update(_gameState);

    UpdateBlockSystem(deltaTime);

    UpdateLineClear();

    UpdateLaneScroll(deltaTime);

    _collisionSystem.Update(Players);

    _gameState.UpdateTick(deltaTime);

    GameEndResult? result = _gameEndSystem.Update(_gameState);

    if (result == null)
        return null;

    _gameState.EndGame();
    return result;
}
```

한 Tick에서 각 시스템을 정해진 순서대로 실행하여 동일한 게임 상태를 기준으로 다음 Tick을 계산합니다.

### Update 순서

| 순서 | 처리                         | 역할                   |
| -: | -------------------------- | -------------------- |
|  1 | `ProcessInput()`           | 입력 큐의 Player 입력 처리   |
|  2 | `UpdatePlayers()`          | Player 및 Car 상태 업데이트 |
|  3 | `AttackSystem.Update()`    | 예약된 공격 블록 처리         |
|  4 | `UpdateBlockSystem()`      | FlyingBlock 이동 및 배치  |
|  5 | `UpdateLineClear()`        | 완성된 Line 제거          |
|  6 | `UpdateLaneScroll()`       | Lane Scroll 처리       |
|  7 | `CollisionSystem.Update()` | 차량과 블록 충돌 검사         |
|  8 | `GameState.UpdateTick()`   | 게임 Tick 및 시간 상태 갱신   |
|  9 | `GameEndSystem.Update()`   | 게임 종료 조건 검사          |

게임 종료 조건이 만족되면 `GameEndResult`를 반환하고 `GameState`를 종료 상태로 변경합니다.

---

# 👤 Player

`Player`는 게임에 참가한 한 명의 플레이어를 표현하며, 입력과 차량, 자신의 Lane 및 현재 소유한 블록을 관리합니다.

### 주요 클래스

| 클래스             | 역할                 |
| --------------- | ------------------ |
| `Player`        | 플레이어 전체 게임 데이터 관리  |
| `PlayerInput`   | 입력 상태 저장           |
| `Car`           | 플레이어 차량 및 이동 상태    |
| `Lane`          | 플레이어 전용 게임 영역      |
| `BlockPiece`    | 현재 사용 가능한 블록       |
| `PlayerSession` | Player와 네트워크 연결 관리 |

### Player 데이터

| 필드             | 역할         |
| -------------- | ---------- |
| `Id`           | Player 식별자 |
| `Session`      | 네트워크 연결    |
| `Car`          | 차량 상태      |
| `Lane`         | 자신의 게임 영역  |
| `CurrentPiece` | 현재 소유한 블록  |
| `Mode`         | 공격 / 수비 상태 |

---

# 🎮 Input Processing

클라이언트의 입력은 네트워크 수신 시점에 즉시 게임 상태에 적용하지 않고 `PlayerInputCommand`로 변환하여 입력 큐에 저장합니다.

```text
Client
  │
  │ C_InputPacket
  ▼
PlayerSession
  │
  ▼
PlayerInputCommand
  │
  ▼
Input Queue
  │
  │ Server Tick
  ▼
GameSimulation.ProcessInput()
  │
  ▼
Player State Update
```

### 처리 과정

1. 클라이언트가 `C_InputPacket` 전송
2. 서버가 입력 데이터를 `PlayerInputCommand`로 변환
3. `GameSimulation`의 Input Queue에 추가
4. Tick마다 `ProcessInput()` 실행
5. 해당 Player의 상태 변경

이 구조를 통해 네트워크 패킷이 도착한 시점과 게임 상태가 변경되는 시점을 분리하고, 모든 게임 로직을 Server Tick 기준으로 처리합니다.

---

# 🧱 Block System

Block System은 블록의 생성, 이동, 배치 및 공격 처리를 담당합니다.

주요 블록 객체는 역할에 따라 분리되어 있습니다.

| 클래스               | 역할                 |
| ----------------- | ------------------ |
| `BlockPiece`      | 플레이어가 현재 소유한 블록 모양 |
| `FlyingBlock`     | Lane에서 이동 중인 블록    |
| `Block`           | Grid에 정착된 블록       |
| `Lane`            | 블록을 저장하는 게임 공간     |
| `GameSimulation`  | 블록 시스템 업데이트        |
| `CollisionSystem` | 차량 충돌 처리           |

`FlyingBlock`과 `Block`을 분리하여 **이동 중인 블록과 Grid에 정착된 블록을 서로 다른 상태로 관리**합니다.

```text
BlockPiece
    │
    │ 사용
    ▼
FlyingBlock
    │
    │ 이동
    ├── 다른 Block과 충돌
    │
    ├── Line Clear
    │
    └── Lane 최상단 도달
            │
            ▼
          Block
            │
            ▼
          Grid
```

---

## 🛡️ Defensive Block

수비 모드에서는 Player가 현재 소유한 `BlockPiece`를 자신의 Lane 앞으로 발사합니다.

### 처리 과정

1. Player가 Shoot 입력
2. 현재 `BlockPiece`를 가져옴
3. `FlyingBlock` 생성
4. Tick마다 이동 위치 계산
5. 이동 위치에서 다른 Block과 충돌 검사
6. 충돌하면 이전 위치에 Block으로 정착
7. Line Clear 조건에 해당하면 제거
8. 충돌하지 않고 Lane 최상단에 도달하면 Grid에 정착

즉, 블록은 처음부터 Grid 내부에서 이동하지 않고 `FlyingBlock`으로 독립적으로 이동하다가 특정 조건을 만족하면 `Block`으로 변환되어 Grid에 저장됩니다.

---

# ⚔️ Attack System

공격 모드에서는 자신의 블록을 상대 Lane에 일정 Tick 이후 생성합니다.

공격 예약 정보는 `AttackPiece`로 관리하고, 상대 Lane의 `PendingAttacks` Queue에 저장합니다.

```text
Player Input
    │
    ▼
AttackPiece
    │
    ├── BlockPiece
    ├── X Position
    ├── SpawnTick
    └── SenderId
    │
    ▼
Target Lane.PendingAttacks
    │
    │ SpawnTick 도달
    ▼
AttackSystem
    │
    ▼
Target Lane 최상단에 Block 생성
```

```csharp
private void SendAttack(Player sender, BlockPiece piece)
{
    Player target =
        Players.Values.First(
            p => p.Id != sender.Id
        );

    target.Lane.PendingAttacks.Enqueue(
        new AttackPiece(
            piece,
            sender.Car.X,
            gameState.Tick + 30,
            sender.Id
        )
    );
}
```

공격 블록은 공격자의 현재 차량 X 위치와 생성 예정 Tick을 함께 저장합니다.

현재 `SpawnTick`은 현재 Tick에서 `30`을 더해 설정합니다.

서버 Tick이 50ms이므로 약 **1.5초 후** 상대 Lane에 생성됩니다.

생성 위치에 다른 블록이 이미 존재하는 경우에는 공격을 즉시 삭제하지 않고 Queue에 유지하여 다음 Tick에 다시 생성 가능 여부를 확인합니다.

---

# 🛣️ Lane

각 Player는 자신의 게임 공간인 `Lane`을 하나씩 가지고 있습니다.

```text
Y
19  □ □ □ □ □
18  □ □ □ □ □
17  □ □ □ □ □
...
 1  □ □ □ □ □
 0  □ □ □ □ □
   -----------
   0 1 2 3 4   X
```

Lane은 Grid를 기반으로 블록 상태를 관리합니다.

```text
Cell
 └── Block
      ├── null  → 빈 공간
      └── Block → 블록 존재
```

### 주요 클래스

| 클래스                | 역할            |
| ------------------ | ------------- |
| `Lane`             | Player의 게임 영역 |
| `Cell`             | Lane 내부 하나의 칸 |
| `Block`            | Grid에 정착된 블록  |
| `LaneScrollSystem` | Lane 이동 처리    |
| `LineClearSystem`  | 완성 Line 제거    |

---

# ⬇️ Lane Scroll

Block Racing의 Lane은 일반적인 테트리스처럼 블록이 위에서 아래로 낙하하는 방식이 아닙니다.

대신 **Lane 자체가 아래 방향으로 이동**합니다.

```text
시간 경과
   │
   ▼
Lane Scroll
   │
   ▼
Grid 전체가 아래로 1칸 이동
   │
   ▼
최상단 Cell은 비어있는 상태로 생성
```

`LaneScrollSystem`은 일정한 Scroll Interval마다 Lane 전체를 한 칸씩 이동시킵니다.

현재 Scroll Interval은 **0.5초**입니다.

이를 통해 차량은 고정된 위치에 있고, Lane이 아래로 이동하면서 차량이 앞으로 전진하는 것처럼 보이도록 구성했습니다.

```text
┌───────────────┐
│               │
│   █ █         │
│ █ █ █         │
│ █   █ █       │
│               │
│       🚗      │  ← 차량은 정착
│_______________│
       ↓
   Lane Scroll
       ↓

        
```

이 구조는 게임의 레이싱 진행을 표현하면서 동시에 블록을 차량 앞으로 이동시키는 역할을 합니다.

---

# 🧹 Line Clear

완성된 Line은 `LineClearSystem`에서 검사하고 제거합니다.

| 클래스                 | 역할          |
| ------------------- | ----------- |
| `LineClearSystem`   | 완성된 Line 검사 |
| `Lane.RemoveLine()` | 해당 Line 제거  |

일반적인 테트리스와 달리 Line이 제거된 이후 **위쪽 블록을 아래로 이동시키지 않습니다.**

```text
일반 Tetris

█████  ← Line Clear
█████
█████
 ↓
█████
█████
```

Block Racing에서는 Lane이 지속적으로 Scroll되기 때문에 Line Clear 이후 빈 공간을 채우기 위해 블록을 재배치할 필요가 없습니다.

```text
Line Clear
     │
     ▼
해당 Line 제거
     │
     ▼
기존 Grid 구조 유지
     │
     ▼
Lane Scroll에 따라 차량 앞으로 이동
```

이는 낙하형 퍼즐이 아니라 **전진하는 레이싱 장애물 구조**에 맞춘 설계입니다.

---

# 💥 Collision

`CollisionSystem`은 Player의 차량과 Lane의 블록 충돌을 검사합니다.

### 주요 클래스

| 클래스               | 역할            |
| ----------------- | ------------- |
| `CollisionSystem` | 차량과 블록의 충돌 검사 |
| `Car`             | 충돌 결과 및 상태 처리 |

### 충돌 처리

```text
GameSimulation.Update()
        │
        ▼
Lane Scroll
        │
        ▼
CollisionSystem.Update()
        │
        ▼
Car 위치 확인
        │
        ▼
Lane Grid 검사
        │
        ▼
Block 발견
        │
        ▼
Car.OnCollision()
        │
        ├── Stun
        ├── Speed 감소
        └── Invincible
```

충돌 검사에서는 현재 Player의 `Car` 위치를 확인하고 해당 위치의 Lane Cell에 Block이 존재하는지 검사합니다.

충돌이 발생하면 `Car.OnCollision()`을 통해 차량의 상태를 변경합니다.

* Stun 처리
* 이동 속도 감소
* 일정 시간 무적 처리

이후 일정 Tick이 지나면 Stun과 무적 상태가 종료됩니다.

### Update 순서

충돌 판정은 `LaneScrollSystem` 이후에 수행합니다.

```text
Lane Scroll
    │
    ▼
Collision Check
```

Lane Scroll에 의해 실제 차량 위치와 장애물 위치 관계가 변경된 이후 충돌을 검사해야 서버의 게임 상태와 클라이언트 화면의 충돌 시점이 일치하기 때문입니다.

---

# 🏁 Game End

게임 종료 조건은 `GameEndSystem`에서 판단합니다.

| 클래스             | 역할       |
| --------------- | -------- |
| `GameEndSystem` | 승패 조건 판단 |
| `GameEndResult` | 게임 결과 전달 |

현재 게임의 종료 조건은 **목표 거리에 먼저 도달하는 Player가 승리하는 것**입니다.

```text
Car.Distance >= TargetDistance
        │
        ▼
GameEndSystem
        │
        ├── 도착 Player → Win
        └── 상대 Player → Lose
```

매 Tick마다 모든 Player의 `Car.Distance`를 검사합니다.

종료 조건을 만족하면:

1. `GameEndResult` 생성
2. `GameState` 종료 상태 변경
3. `GameSimulation.Update()`에서 결과 반환
4. Room에서 게임 종료 처리
5. `S_GameEndPacket` 전송

---

# 🔁 Complete Game Loop

전체적인 게임 실행 흐름은 다음과 같습니다.

```text
┌───────────────────────────────┐
│         Room.Update()         │
└───────────────┬───────────────┘
                │
                ▼
┌───────────────────────────────┐
│     GameSimulation.Update()   │
└───────────────┬───────────────┘
                │
                ▼
        ┌───────────────┐
        │ ProcessInput  │
        └───────┬───────┘
                ▼
        ┌───────────────┐
        │ UpdatePlayers │
        └───────┬───────┘
                ▼
        ┌───────────────┐
        │ AttackSystem  │
        └───────┬───────┘
                ▼
        ┌───────────────┐
        │ Block System  │
        └───────┬───────┘
                ▼
        ┌───────────────┐
        │ Line Clear    │
        └───────┬───────┘
                ▼
        ┌───────────────┐
        │ Lane Scroll   │
        └───────┬───────┘
                ▼
        ┌───────────────┐
        │ Collision     │
        └───────┬───────┘
                ▼
        ┌───────────────┐
        │ Update Tick   │
        └───────┬───────┘
                ▼
        ┌───────────────┐
        │ Game End Check│
        └───────┬───────┘
                │
        ┌───────┴────────┐
        │                │
      End             Continue
        │                │
        ▼                ▼
 GameEndResult     CreateSnapshot
        │                │
        ▼                ▼
 Game End Packet   GameStatePacket
                         │
                         ▼
                  Client Rendering
```

이 구조를 통해 **입력 수신 → 게임 규칙 계산 → 상태 변경 → 종료 판단 → 상태 동기화**의 전체 흐름을 하나의 고정된 Server Tick 안에서 처리합니다.
