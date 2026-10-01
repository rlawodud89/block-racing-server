# Architecture & Design Decisions

Block Racing을 개발하면서 선택한 주요 기술과 구조, 그리고 실제 개발 과정에서 발생한 문제를 해결하며 변경한 설계 결정을 정리합니다.

---

## Network

### TCP를 선택한 이유

Block Racing은 TCP 기반으로 구현했습니다.

게임 특성상 FPS처럼 매우 빈번한 이동 동기화가 필요한 게임이 아니며, 하나의 블록 설치와 같은 개별 입력이 게임 결과에 미치는 영향이 큽니다.

특히 블록 설치 패킷이 손실되면 이후 게임 상태 자체가 달라질 수 있기 때문에 **패킷의 신뢰성과 순서 보장**이 중요하다고 판단했습니다.

또한 서버는 지속적으로 Snapshot을 전송하여 게임 상태를 동기화하기 때문에 Snapshot 패킷의 순서가 변경될 경우 클라이언트가 과거 상태를 다시 렌더링하는 문제가 발생할 수 있습니다.

따라서 별도의 신뢰성 및 순서 보장 계층을 구현하기보다 TCP의 특성을 활용했습니다.

---

### Custom Packet Protocol

JSON이나 범용 직렬화 라이브러리 대신 직접 Packet Protocol을 구현했습니다.

주된 목적은 패킷의 구조와 직렬화 과정을 직접 구현하면서 C# 기반 네트워크 서버의 동작을 학습하는 것이었습니다.

패킷은 다음과 같은 구조로 구성했습니다.

```text
Length | PacketId | Body
```

`Length`를 가장 앞에 배치하여 TCP Stream에서 하나의 Packet이 완성되었는지 판단할 수 있도록 했습니다.

```text
TCP Stream
    ↓
Length 확인
    ↓
Length만큼 Buffer에서 추출
    ↓
Packet 완성
    ↓
PacketId 확인
    ↓
Handler 실행
```

이를 통해 TCP의 Stream 특성으로 발생하는 Message Framing 문제를 직접 처리하고, `PacketId`를 기준으로 적절한 Handler를 실행하도록 구성했습니다.

---

### async/await 네트워크 처리

네트워크 처리는 `async/await` 기반으로 구현했습니다.

연결된 사용자마다 별도의 Thread를 사용하는 구조는 사용자 수가 증가할수록 Thread 관리 비용이 증가할 수 있습니다.

반면 비동기 I/O를 사용하면 네트워크 응답을 기다리는 동안 Thread를 점유하지 않아 많은 연결을 효율적으로 처리할 수 있습니다.

---

## Server Architecture

### Dedicated Server

게임 규칙 계산을 서버에 일임하기 위해 Dedicated Server 구조를 선택했습니다.

Block Racing은 퍼즐 게임으로, 블록 배치와 공격, 충돌, Line Clear 등 여러 게임 규칙을 서버에서 계산해야 합니다.

또한 두 플레이어가 경쟁하지만 FPS처럼 서로의 위치에 즉각적으로 반응하는 구조가 아니기 때문에, 게임 상태를 중앙 서버에서 관리하는 구조가 적합하다고 판단했습니다.

---

### Server Authoritative

게임 상태의 일관성을 유지하기 위해 서버가 모든 게임 상태를 최종적으로 결정하도록 구성했습니다.

클라이언트마다 성능이나 네트워크 환경이 다르기 때문에 각 클라이언트가 게임 상태를 직접 계산하면 서로 다른 결과가 발생할 수 있습니다.

따라서:

```text
Client Input
     ↓
Server
     ↓
Game State Calculation
     ↓
Snapshot
     ↓
Client
```

모든 입력과 게임 규칙 계산을 서버에서 처리하고 결과를 클라이언트에 전달합니다.

게임 진행 중의 차량 위치, 보유 블록, Lane 상태뿐 아니라 Login, Matchmaking과 같은 게임 시작 전 상태 역시 서버가 관리합니다.

---

### Room 기반 게임 관리

하나의 서버에서 여러 게임을 독립적으로 실행하기 위해 Room 단위로 게임을 관리했습니다.

```text
GameManager
    ↓
RoomManager
    ↓
Room
    ↓
GameSimulation
```

`RoomManager`가 모든 Room을 관리하고, 서버의 Fixed Tick에서 각 Room의 상태를 확인합니다.

각 Room은 자신의 Player와 `GameSimulation`을 가지고 독립적으로 게임을 진행할 수 있습니다.

---

### Session과 Player 분리

TCP 연결 자체와 게임 참여자를 동일한 개념으로 취급하지 않았습니다.

```text
Session
└── SessionId
    └── TCP Connection

Player
└── PlayerId
    └── Login 이후 게임 참여자
```

TCP 연결은 존재하지만 아직 Login하지 않은 Session도 존재할 수 있기 때문에 둘을 하나의 객체로 관리하면 연결 상태와 게임 상태를 구분하기 어렵습니다.

따라서 연결을 나타내는 `Session`과 실제 게임 참여자를 나타내는 `Player`를 분리했습니다.

---

## Game Loop

### Fixed Tick

서버 게임 루프는 `50ms / 20 Tick/s`의 Fixed Tick 기반으로 동작합니다.

FPS처럼 매우 빠른 입력 반응이 필요한 게임이 아니기 때문에 일정한 주기로 게임 상태를 계산하는 방식을 선택했습니다.

클라이언트의 Unity `Update()`를 기준으로 게임 상태를 계산할 경우 PC 성능과 FPS에 따라 게임 상태 갱신 횟수가 달라질 수 있습니다.

서버가 동일한 Tick 기준으로 게임을 계산함으로써 모든 Player가 동일한 게임 시간 기준을 공유하도록 했습니다.

---

### GameSimulation 분리

`Room`은 Player 입장/퇴장, Ready, Session 처리 등 Room 관리에 집중하고 실제 게임 규칙은 `GameSimulation`이 담당합니다.

Room에 게임 로직까지 모두 포함하면 하나의 클래스가 너무 많은 책임을 가지게 되기 때문에 역할을 분리했습니다.

```text
Room
├── Player / Session 관리
├── Room State 관리
└── GameSimulation
     └── Game Rule 처리
```

---

### Input Queue

클라이언트의 입력은 즉시 게임 상태에 적용하지 않고 `ConcurrentQueue<PlayerInputCommand>`에 저장한 뒤 Tick에서 처리합니다.

```text
C_InputPacket
      ↓
Input Queue
      ↓
GameSimulation Tick
      ↓
ProcessInput()
      ↓
Game State 변경
```

네트워크 Session에서 입력을 직접 게임 상태에 적용하면 여러 요청이 동시에 상태를 변경할 가능성이 있습니다.

Queue를 통해 입력을 보류한 뒤 게임 루프에서 순차적으로 처리하여 게임 상태 변경을 하나의 흐름으로 통제했습니다.

---

## Snapshot

### GameState와 Snapshot 분리

서버의 `GameState`, `Player`, `Lane` 등의 객체를 직접 클라이언트에 전달하지 않고 별도의 Snapshot을 생성합니다.

서버 객체에는 게임 로직을 수행하기 위한 상태와 코드가 포함되어 있으며, 클라이언트에는 현재 상태를 표현하는 데이터만 필요합니다.

따라서:

```text
Server Game Object
        ↓
Snapshot Builder
        ↓
Network DTO
```

형태로 필요한 데이터만 추출하여 전달합니다.

Snapshot에 대한 구체적인 구조는 [`snapshot.md`](./snapshot.md)에서 다룹니다.

---

### Snapshot Builder

Snapshot은 정해진 구조로 생성되어야 클라이언트가 동일한 데이터 형식을 기준으로 렌더링할 수 있습니다.

이를 위해 Snapshot 생성 책임을 Builder로 분리했습니다.

```text
GameStateSnapshotBuilder
        ↓
PlayerSnapshotBuilder
        ↓
LaneSnapshotBuilder
        ↓
FlyingBlockSnapshotBuilder
```

각 Builder가 자신의 영역에 필요한 데이터만 변환하도록 하여 Snapshot 생성 구조를 명확하게 유지했습니다.

---

### Lane Block 데이터 최소화

현재 게임 규칙에서는 정착된 Block의 종류 자체가 필요하지 않기 때문에 Lane Grid를 `0/1 byte[]`로 표현했습니다.

```text
0 = Empty
1 = Block
```

클라이언트는 해당 위치에 Block이 존재하는지만 알면 렌더링할 수 있기 때문에 Block 객체의 전체 정보를 전달하지 않았습니다.

이를 통해 Snapshot의 크기를 줄이고 불필요한 네트워크 전송을 방지했습니다.

---

## Matchmaking & Room

### Auto Match와 Private Room 통합

자동 매칭과 Private Room은 입장 방식만 다르고 게임 시작 이후의 구조와 기능은 동일합니다.

따라서 별도의 게임 구조를 만들기보다 두 방식 모두 동일한 `Room`을 사용하도록 구성했습니다.

```text
Auto Match ───┐
              ├── Room
Private Room ─┘
```

---

### Room State Machine

Room은 다음 상태를 사용합니다.

```text
Waiting
   ↓
Ready
   ↓
Starting
   ↓
Playing
   ↓
Result
   ↓
Closing
```

상태에 따라 허용되는 요청과 처리를 구분하기 위해 State Machine 형태로 관리했습니다.

특히 `Result` 상태는 게임 종료와 Room 종료를 분리하기 위해 추가했습니다.

게임 종료 직후 Room을 제거하지 않고:

```text
Result
├── Rematch → Ready
└── Exit    → Closing
```

형태로 처리하여 기존 Player 관계를 유지하면서 재대결을 지원할 수 있도록 했습니다.

---

### Matchmaking 중복 방지

MatchMaker는 Player를 다음 상태로 관리합니다.

```text
None
 ↓
Queued
 ↓
Matching
 ↓
InRoom
```

매칭 대상 Player를 선택한 직후 `Matching` 상태로 변경하여 다른 매칭 요청에서 다시 선택되지 않도록 했습니다.

Room 입장이 실패하면 다시 `Queued` 상태로 복구합니다.

이를 통해 여러 매칭 작업이 동시에 실행되는 상황에서도 동일 Player가 중복으로 Room에 배정되는 것을 방지했습니다.

---

### Room 상태 재검증

MatchMaker나 RoomManager에서 확인한 상태만으로 모든 요청을 처리하지 않고, 실제 Room 내부에서도 상태를 다시 검증합니다.

예를 들어 Private Room Join 요청에서는 실제 `Room`에 접근한 시점에:

* Room이 `Waiting` 상태인지
* Player가 이미 Room에 존재하지 않는지
* Room이 가득 차지 않았는지

등을 다시 확인합니다.

이는 중복 Packet이나 동시에 들어오는 요청으로 인해 Room 상태가 잘못 변경되는 것을 방지하기 위한 것입니다.

---

## Concurrency

### Room 전체 Lock을 사용하지 않은 이유

Room의 모든 코드를 하나의 Lock으로 보호하지 않았습니다.

Room에는 두 종류의 상태 변경이 존재하기 때문입니다.

```text
Session Task
    ↓
외부 요청에 따른 상태 변경

Game Server Tick
    ↓
게임 상태 변경
```

게임 루프의 `Room.Update()`는 하나의 서버 Tick에서 단일 실행 흐름으로 처리되기 때문에 별도의 Lock이 필요하지 않습니다.

오히려 전체 `Room.Update()`를 Lock으로 감싸면 게임 로직뿐 아니라 네트워크 처리까지 불필요하게 직렬화할 수 있습니다.

---

### SemaphoreSlim을 이용한 상태 보호

여러 Session에서 동시에 접근할 수 있는 Room 상태 변경에는 `SemaphoreSlim`을 사용했습니다.

예를 들어 두 Player가 동시에 Ready를 요청하면 다음과 같은 상태 변경이 동시에 발생할 수 있습니다.

```text
Player A Ready ─┐
                ├── Room
Player B Ready ─┘
```

이러한 상태 변경을 직렬화하여 Player 목록, Ready 상태, Room State 등의 Race Condition을 방지했습니다.

---

### Concurrent Disconnect Race Condition

동시 접속 테스트에서 여러 Session이 동시에 Disconnect되면서 하나의 Room에서 `Player`를 동시에 제거하는 문제가 발생했습니다.

`Room` 내부의 `_players`는 `List<Player>`였기 때문에 여러 Session Task가 동시에 수정하면서 `ArgumentOutOfRangeException`이 발생했습니다.

```text
Multiple Session Tasks
        ↓
RemovePlayer()
        ↓
List<Player> 동시 수정
        ↓
ArgumentOutOfRangeException
```

상태 변경 구간을 `SemaphoreSlim`으로 직렬화하여 한 번에 하나의 Session만 Room 상태를 변경하도록 수정했습니다.

이후 동시 Disconnect 테스트에서 동일한 예외가 발생하지 않고 Player, Session, Room 정리가 정상적으로 수행되는 것을 확인했습니다.

---

## Performance

### 500 Player 기준 성능 측정

성능 테스트는 100 → 200 → 300 → 400 → 500 Player 순으로 증가시키면서 진행했습니다.

Player 수가 증가할 때 특정 지표가 비정상적으로 증가하는지 확인하고, 이후 500 Player 환경을 기준으로 최적화를 진행했습니다.

---

### GC / Allocation 최적화

500 Player 환경에서 3분간 테스트한 결과 약 `3000ms`의 GC Pause가 측정되었습니다.

반면 CPU 사용률은 동일 조건에서 User 약 `0.28%`, System 약 `0.58%` 수준으로 측정되어 CPU보다 Allocation과 GC가 우선적으로 개선할 대상이라고 판단했습니다.

---

### PacketWriter 최적화

`dotnet-trace`를 통해 Allocation이 많이 발생하는 위치를 확인한 결과 PacketWriter의 직렬화 과정에서 반복적인 할당이 발생하고 있었습니다.

기존에는 `BitConverter` 등의 사용으로 값이 직렬화될 때 임시 `byte[]`가 생성되었습니다.

이를 직접 관리하는 Preallocated `byte[]` Buffer 기반 구조로 변경했습니다.

또한 Room마다 PacketWriter를 재사용하고 `Reset()`을 통해 위치만 초기화하여 매 Snapshot마다 새로운 PacketWriter를 생성하는 비용을 줄였습니다.

---

### LINQ 제거

매 Tick마다 실행되는 Snapshot Sync 및 게임 로직에서 일부 LINQ가 반복적으로 실행되면서 Iterator와 컬렉션 등의 추가 할당이 발생했습니다.

`dotnet-trace` 측정 결과 Enumerator 관련 Allocation이 확인되어 반복 실행되는 부분을 `foreach`와 직접적인 Index 접근으로 변경했습니다.

이를 통해 매 Tick 발생하는 Allocation과 GC 부담을 줄였습니다.

---

## Shared Common Project

Client와 Server가 동일하게 사용해야 하는 Packet, Enum, Snapshot 등을 별도의 Common 프로젝트에 분리했습니다.

```text
Client ───┐
          ├── Common
Server ───┘
```

네트워크 Packet의 구조나 Enum 값, Snapshot 데이터 구조는 Client와 Server 사이의 공통 계약이므로 양쪽에서 동일한 정의를 사용하도록 구성했습니다.

이를 통해 Client와 Server가 서로 다른 데이터 형식을 사용하는 문제를 방지했습니다.

---

## Game Rule Systems

게임 규칙은 하나의 `GameSimulation`에 모두 구현하지 않고 각각의 System으로 분리했습니다.

```text
GameSimulation
├── AttackSystem
├── CollisionSystem
├── LineClearSystem
├── LaneScrollSystem
└── GameEndSystem
```

각 System이 특정 규칙을 담당하도록 하여 `GameSimulation`의 책임을 줄이고 개별 게임 규칙을 독립적으로 수정할 수 있도록 했습니다.

---

## Room Reuse & Rematch

초기에는 게임이 종료되면 Room을 바로 `Closing` 상태로 변경하고 삭제했습니다.

하지만 Rematch를 구현하면서 문제가 발생했습니다.

Room을 삭제하면 이전 게임에서 어떤 Player가 서로 매칭되었는지에 대한 관계도 함께 사라지기 때문에, Rematch를 위해 각 Player가 상대방을 별도로 기억하거나 새로운 매칭 과정을 거쳐야 했습니다.

이를 해결하기 위해 게임 종료 후 Room을 `Result` 상태로 유지했습니다.

```text
Game End
   ↓
Result
 ├── Both Rematch → Ready
 └── Exit         → Closing
```

기존 Room과 Player 관계를 그대로 유지한 상태에서 두 Player의 Rematch 요청을 확인하고 다시 게임을 시작할 수 있도록 변경했습니다.

---

## Design Evolution

개발 과정에서 초기 설계가 실제 요구사항과 테스트 결과에 따라 변경되었습니다.

### Room Lifecycle

```text
초기
Playing
   ↓
Closing
   ↓
Room 삭제
```

Rematch 기능을 추가하면서:

```text
변경
Playing
   ↓
Result
 ├── Rematch → Ready
 └── Exit    → Closing
```

으로 변경했습니다.

### LINQ

초기에는 코드의 간결성을 위해 LINQ를 적극적으로 사용했습니다.

하지만 성능 측정 과정에서 Enumerator 관련 Allocation이 반복적으로 발생하는 것을 확인했고, 매 Tick 실행되는 부분을 직접적인 반복문과 Index 접근 방식으로 변경했습니다.

즉, 단순히 코드를 먼저 최적화하기보다 **측정을 통해 실제 Allocation이 발생하는 지점을 확인한 후 필요한 부분만 변경**했습니다.

---

## Network Disconnect & Heartbeat

### 문제

TCP 연결이 실제로 끊어진 경우에는 Disconnect를 감지할 수 있었지만, Wi-Fi 단절과 같이 네트워크가 끊겼어도 TCP 연결 자체가 즉시 종료되지 않는 상황에서는 이를 감지할 수 없었습니다.

이 문제는 로컬 환경에서는 쉽게 발견되지 않았고, 클라우드 서버에 실제로 배포한 후 확인했습니다.

```text
Client
  X  Network
  │
Server
  │
  └── TCP Connection은 아직 유지된 것으로 보임
```

서버 입장에서는 단순히 패킷이 오지 않는 상황으로 보이기 때문에 기존 Disconnect 처리만으로는 해결할 수 없었습니다.

### Heartbeat

서버가 주기적으로 Heartbeat를 전송하고 클라이언트가 응답하도록 구성했습니다.

```text
Server
  │
  ├── Heartbeat ──────→ Client
  │
  │←── Heartbeat ──────┤
  │
  └── Timeout 확인
```

서버에서 일정 시간 동안 응답을 받지 못하면 해당 Session을 정리하도록 했습니다.

이를 통해 실제 네트워크가 단절된 Session을 감지하고 서버의 게임 상태를 정리할 수 있도록 했습니다.

### Client-side Timeout

서버에서 Session을 정리하더라도 네트워크가 단절된 상태에서는 서버의 Disconnect 결과를 클라이언트가 전달받을 수 없다는 문제가 추가로 발견되었습니다.

따라서 클라이언트에서도 서버의 Heartbeat 수신 여부를 확인하고 Timeout이 발생하면 자체적으로 연결이 끊긴 것으로 판단하도록 했습니다.

```text
Server Heartbeat Timeout
        ↓
Server Session 정리

Client Heartbeat Timeout
        ↓
Client Session 종료 처리
        ↓
Login 전 화면으로 이동
```

이를 통해 서버와 클라이언트가 네트워크 단절 상태를 각각 감지하고 독립적으로 정리할 수 있도록 했습니다.

Block Racing은 별도의 계정 상태를 유지할 필요가 없기 때문에, 연결이 복구된 이후 새로운 Session으로 다시 시작하는 방식으로 처리했습니다.
