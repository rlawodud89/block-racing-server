# Server Architecture

Block Racing Server는 **TCP 기반 커스텀 패킷 시스템**과 **Authoritative Server 기반 Room Tick 아키텍처**로 구성되어 있습니다.

서버는 네트워크 계층과 게임 로직을 분리하고, 각 게임을 `Room` 단위로 독립적으로 관리합니다. 클라이언트는 입력과 요청만 전달하며, 실제 게임 상태 변경과 판정은 서버에서 수행합니다.

---

## Architecture Overview

```text
Client
  │
  │ TCP
  ▼
PlayerSession
  │
  ▼
ReceiveBuffer
  │
  ▼
PacketReader
  │
  ▼
PacketManager
  │
  ▼
Packet Handler
  │
  ▼
GameManager
  │
  ├───────────────┐
  ▼               ▼
MatchMaker     RoomManager
  │               │
  └───────┬───────┘
          ▼
        Room
          │
          ▼
      Game Tick
          │
    ┌─────┼──────────────┐
    ▼     ▼              ▼
  Input  Game State    State Sync
    │     Update          │
    └─────┴───────────────┘
          │
          ▼
        Client
```

서버는 크게 **Network Layer**와 **Game Layer**로 구분됩니다.

### Network Layer

클라이언트의 TCP 연결을 관리하고, 바이트 스트림을 패킷으로 변환한 뒤 적절한 Handler로 전달합니다.

```text
Client
  │
  │ TCP
  ▼
PlayerSession
  │
  ▼
ReceiveBuffer
  │
  ▼
PacketReader
  │
  ▼
PacketManager
  │
  ▼
Packet Handler
```

### Game Layer

패킷 Handler에서 전달받은 요청을 기반으로 Player와 Room의 상태를 변경하고, 일정한 Tick 주기로 게임을 진행합니다.

```text
Packet Handler
      │
      ▼
GameManager
      │
 ┌────┴────┐
 ▼         ▼
MatchMaker RoomManager
             │
             ▼
            Room
             │
             ▼
          Game Tick
```

---

패킷은 별도의 Reflection 기반 직렬화 대신 `PacketWriter`와 `PacketReader`를 사용하여 명시적으로 데이터를 변환합니다.

각 패킷은 `IPacket`을 구현하며 자신의 데이터 구조에 맞는 `Read()`와 `Write()`를 정의합니다.

```text
Packet Object
     │
     │ Write()
     ▼
PacketWriter
     │
     ▼
byte[]
```

반대로 서버가 패킷을 수신하면:

```text
byte[]
  │
  ▼
PacketReader
  │
  │ Read()
  ▼
Packet Object
```

이 구조를 통해 패킷마다 필요한 데이터만 명시적으로 처리하고, 패킷의 데이터 구조를 쉽게 확장할 수 있도록 구성했습니다.

---

# 1. Authoritative Server

Block Racing은 **Server Authoritative** 구조를 사용합니다.

서버가 게임 상태에 대한 최종 권한을 가지고 있으며, 클라이언트는 자신의 의도와 입력만 서버에 전달합니다.

```text
Client
  │
  │ Input
  ▼
Server
  │
  ├── Input Validation
  ├── Game Logic
  ├── Collision
  ├── Game Rule
  └── State Update
          │
          ▼
       Snapshot
          │
          ▼
       Client
```

클라이언트에서 계산한 게임 상태를 그대로 신뢰하지 않고, 실제 게임 로직과 판정을 서버에서 수행합니다.

이를 통해 게임 로직의 일관성을 유지하고, 여러 클라이언트가 동일한 서버 상태를 기준으로 게임을 진행할 수 있도록 했습니다.

---

# 2. Room 기반 Game Architecture

하나의 Dedicated Server에서 여러 게임이 동시에 진행될 수 있기 때문에 각각의 게임을 `Room` 단위로 분리했습니다.

```text
GameManager
  │
  ├── MatchMaker
  │
  └── RoomManager
        │
        ├── Room 1
        │    ├── Player
        │    └── Game State
        │
        ├── Room 2
        │    ├── Player
        │    └── Game State
        │
        └── Room N
             ├── Player
             └── Game State
```

각 Room은 자신의 Player, Game State, Input 및 게임 진행 상태를 독립적으로 관리합니다.

이를 통해 게임 간 상태를 분리하고, 하나의 서버에서 여러 게임을 동시에 관리할 수 있도록 구성했습니다.

---

# 3. Tick-based Game Loop

Room의 게임 로직은 이벤트 발생 즉시 수행하는 방식이 아니라 **고정된 주기의 Tick**을 기준으로 처리합니다.

현재 게임 Tick은 **50ms 주기**로 동작합니다.

```text
             Game Tick
                │
     ┌──────────┼──────────┐
     ▼          ▼          ▼
ProcessInput  Update     Sync
     │          │          │
     ▼          ▼          ▼
  Player     Game State  Snapshot
  Input       Update
```

각 Tick에서는 입력을 처리하고 게임 상태를 업데이트한 뒤 필요한 상태를 동기화합니다.

```text
Room.Update()
    │
    ├── Process Input
    ├── Update Game State
    └── Sync Snapshot
```

Tick 기반 구조를 사용함으로써 서버의 게임 로직을 일정한 시간 간격으로 실행하고, 네트워크 이벤트와 게임 시뮬레이션을 분리했습니다.

---

# 4. 주요 클래스

## Network

| 클래스                            | 역할                                |
| ------------------------------ | --------------------------------- |
| `Program`                      | TCP 서버 시작 및 서버 구성                 |
| `TcpServer`                    | 클라이언트 접속을 수락하고 `PlayerSession` 생성 |
| `SessionManager`               | 연결된 `PlayerSession`의 추가 및 삭제 관리   |
| `PlayerSession`                | 클라이언트의 TCP 연결과 데이터 송수신 관리         |
| `PacketManager`                | `PacketId`에 따라 패킷 Handler 호출      |

## Game

| 클래스                  | 역할                                |
| -------------------- | --------------------------------- |
| `GameManager`        | 게임 시스템의 최상위 관리자                   |
| `MatchMaker`         | 매칭 대기 Player를 관리하고 Room 생성        |
| `RoomManager`        | Room 생성 및 삭제 관리                   |
| `Room`               | 하나의 게임 인스턴스와 Tick, 상태, 입력, 동기화 관리 |
| `Player`             | 게임 내 Player 상태와 Session 정보 관리     |
| `PlayerInputCommand` | Tick에서 처리할 Player 입력 명령           |

---

네트워크 I/O에는 `async/await` 기반 비동기 처리를 사용합니다.

```text
TcpServer
    │
    ├── Client A
    │     └── PlayerSession
    │
    ├── Client B
    │     └── PlayerSession
    │
    └── Client C
          └── PlayerSession
```

각 Session의 네트워크 I/O가 대기하는 동안 별도의 스레드를 계속 점유하지 않도록 하여 다수의 연결을 효율적으로 처리할 수 있도록 구성했습니다.

또한 게임 로직과 네트워크 처리를 적절히 분리하여 비동기 코드의 범위를 네트워크 I/O 중심으로 제한하고, 게임 로직은 Tick 기반으로 명확하게 실행되도록 구성했습니다.

---

# Summary

Block Racing Server의 전체 구조는 다음과 같이 정리할 수 있습니다.

```text
                 Dedicated Server
                        │
          ┌─────────────┴─────────────┐
          │                           │
     Network Layer               Game Layer
          │                           │
      TCP Session                GameManager
          │                    ┌──────┴──────┐
      Packet System            │             │
          │                 MatchMaker   RoomManager
      Packet Handler                         │
          │                                  ▼
          └───────────────►                Room
                                             │
                                          Game Tick
                                             │
                                    ┌────────┼────────┐
                                    ▼        ▼        ▼
                                  Input    Update    Sync
```

핵심적으로 **Network Layer는 안정적인 패킷 전달을 담당하고, Game Layer는 서버 권한을 기반으로 게임 상태를 관리합니다.**

각 게임은 `Room` 단위로 독립적으로 실행되며, 고정된 Tick을 기준으로 입력 처리와 게임 상태 업데이트를 수행합니다. 이를 통해 네트워크 이벤트와 게임 시뮬레이션을 분리하고, 하나의 Dedicated Server에서 여러 게임을 동시에 관리할 수 있도록 구성했습니다.
