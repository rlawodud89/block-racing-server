# Snapshot Synchronization

Block Racing은 서버의 게임 상태를 직접 네트워크로 노출하지 않고, 클라이언트에 필요한 상태만 `Snapshot`으로 변환하여 전달합니다.

Snapshot은 **게임 로직의 결과를 표현하는 네트워크 전용 데이터 구조**이며, 실제 게임 상태의 계산 과정은 [`game-loop.md`](./game-loop.md)에서 다룹니다.

---

## Snapshot Structure

```text
GameStateSnapshot
├── Tick
├── TargetDistance
└── Players[]
    ├── PlayerSnapshot
    │   ├── Id
    │   ├── CarX
    │   ├── Distance
    │   ├── Speed
    │   ├── IsStunned
    │   ├── Mode
    │   ├── CurrentPiece
    │   ├── ShootCooldown
    │   └── LaneSnapshot
    │
    └── ...
```

### GameStateSnapshot

```text
Tick
TargetDistance
Players[]
```

현재 게임 Tick과 목표 거리를 포함하고, 각 플레이어의 상태를 `PlayerSnapshot`으로 보유합니다.

### PlayerSnapshot

```text
Id
CarX
Distance
Speed
IsStunned
Mode
CurrentPieceType?
CurrentPieceRotation?
ShootCooldownRemaining
Lane
```

클라이언트에서 차량과 플레이어 상태를 표현하는 데 필요한 정보만 포함합니다.

### LaneSnapshot

```text
Blocks[]
FlyingBlocks[]
```

Lane의 블록 상태를 **정착된 Block**과 **이동 중인 FlyingBlock**으로 구분합니다.

정착된 Block은 전체 Grid를 1차원 `byte[]`로 변환합니다.

```text
0 = Empty
1 = Block
```

`FlyingBlock`은 현재 위치와 Piece 정보를 별도로 전달합니다.

```text
OwnerId
X
Y
Type
Rotation
```

---

## Snapshot Creation

Snapshot 생성은 Builder를 통해 서버의 게임 객체에서 네트워크 데이터 구조로 변환하는 방식으로 구현했습니다.

```text
GameState
   ↓
GameStateSnapshotBuilder
   ↓
PlayerSnapshotBuilder
   ↓
LaneSnapshotBuilder
   ↓
FlyingBlockSnapshotBuilder
   ↓
GameStateSnapshot
```

`GameSimulation`은 Snapshot 생성 요청만 담당합니다.

```csharp
public GameStateSnapshot CreateSnapshot()
{
    return GameStateSnapshotBuilder.Create(_gameState);
}
```

### GameState → Snapshot

`GameStateSnapshotBuilder`는 현재 Tick과 플레이어 목록을 Snapshot으로 변환합니다.

```csharp
foreach (Player player in gameState.PlayerDictionary.Values)
{
    players.Add(
        PlayerSnapshotBuilder.Create(player));
}
```

### Player → Snapshot

`PlayerSnapshotBuilder`는 차량, 플레이 모드, 현재 Piece, Cooldown 등의 상태를 추출합니다.

```csharp
return new PlayerSnapshot(
    player.Id,
    player.Car.X,
    player.Car.Distance,
    player.Car.CurrentSpeed,
    player.Car.IsStunned,
    player.Mode,
    player.CurrentPiece?.Type,
    player.CurrentPiece?.Rotation,
    player.PieceCooldown,
    LaneSnapshotBuilder.Create(player.Lane)
);
```

### Lane → Snapshot

`LaneSnapshotBuilder`는 Grid와 FlyingBlock을 각각 Snapshot으로 변환합니다.

```text
Lane
├── Grid
│   └── Block → 0 / 1
│
└── FlyingBlocks
    └── FlyingBlock → FlyingBlockSnapshot
```

이 구조를 통해 서버의 `Player`, `Lane`, `Block` 등의 내부 객체와 네트워크 데이터 구조를 분리했습니다.

---

## Packet Serialization

생성된 `GameStateSnapshot`은 `S_GameStatePacket`을 통해 직렬화됩니다.

```text
GameStateSnapshot
      ↓
S_GameStatePacket
      ↓
PacketWriter
      ↓
byte[]
```

Packet에는 Snapshot의 필드를 순서대로 기록합니다.

```text
S_GameStatePacket
├── Tick
├── TargetDistance
├── PlayerCount
└── Players[]
    ├── Player State
    └── Lane
        ├── Block Count
        ├── Blocks[]
        ├── FlyingBlock Count
        └── FlyingBlocks[]
```

각 컬렉션의 크기를 먼저 기록한 후 요소를 순차적으로 직렬화하기 때문에 클라이언트는 동일한 순서로 데이터를 읽을 수 있습니다.

---

## Snapshot Data Design

Snapshot은 서버의 전체 게임 객체를 복제하는 것이 아니라 **클라이언트가 상태를 표현하는 데 필요한 데이터만 포함**합니다.

예를 들어 `FlyingBlockSnapshot`은 실제 `FlyingBlock` 객체 전체가 아닌 다음 값만 전달합니다.

```text
OwnerId
X
Y
Type
Rotation
```

이를 통해 다음과 같은 구조적 분리가 이루어집니다.

```text
Server Game Objects
        │
        │  Snapshot Builder
        ▼
Network DTO
        │
        │  PacketWriter
        ▼
Serialized Packet
```

따라서 서버 내부 게임 로직의 구현이 변경되더라도, 클라이언트에 필요한 Snapshot 계약을 유지할 수 있습니다.

---

## Server Authoritative Snapshot

게임의 실제 상태는 서버의 `GameState`가 관리하며, Snapshot은 그 상태를 클라이언트에 전달하기 위한 표현입니다.

```text
Server GameState
       ↓
Snapshot
       ↓
Client
```

클라이언트는 Snapshot을 기준으로 게임 화면을 갱신하고, 게임 결과와 같은 핵심 상태는 서버가 결정합니다.

게임 상태의 계산 순서와 Tick 처리에 대한 내용은 [`game-loop.md`](./game-loop.md)를 참고합니다.
