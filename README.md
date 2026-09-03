# 🖥️ Block Racing Server

> C# / .NET 기반 2인 온라인 블록 레이싱 Dedicated Server

블록 레이싱의 게임 상태와 판정을 담당하는 **Server Authoritative Game Server**입니다.

Client는 입력을 Server에 전달하고, Server는 고정된 Tick을 기준으로 게임 Simulation을 수행한 뒤 Snapshot을 Client에 전달합니다.

이를 통해 Client의 실행 환경과 관계없이 **Server를 기준으로 일관된 게임 상태를 유지**하도록 설계했습니다.

---

## 📌 Overview

Server는 다음과 같은 역할을 담당합니다.

* TCP 기반 Client 연결 및 Session 관리
* Packet 수신 및 Handler 처리
* Matchmaking 및 Room 관리
* Server Authoritative Game Simulation
* Game Result 판정
* Snapshot 기반 State Synchronization
* Heartbeat 기반 Disconnect Detection
* Game Balance 및 Logging 관리

```text
Client
  │
  │ TCP
  ▼
TcpServer
  │
  ▼
SessionManager
  │
  ▼
PlayerSession
  │
  ▼
PacketManager
  │
  ▼
GameManager
  │
  ├── MatchMaker
  │      ↓
  │   RoomManager
  │      ↓
  │     Room
  │      ↓
  │ GameSimulation
  │
  ▼
Snapshot
  │
  ▼
Client
```

---

# 🏗️ Architecture

Server는 **Network Layer**와 **Game Layer**를 중심으로 구성했습니다.

```text
Server
│
├── Network
│   ├── TcpServer
│   ├── SessionManager
│   ├── PlayerSession
│   ├── PacketManager
│   └── Handlers
│
├── Game
│   ├── GameManager
│   ├── MatchMaker
│   ├── RoomManager
│   ├── Room
│   ├── Players
│   ├── Rules
│   ├── Simulations
│   └── Snapshots
│
└── Data
    └── Game Balance
```

| 영역              | 역할                        | 구현                                                                            |
| --------------- | ------------------------- | ----------------------------------------------------------------------------- |
| TCP Server      | Client 연결 및 Game Loop     | [`TcpServer.cs`](block-racing-server/Network/TcpServer.cs)                    |
| Session         | 연결된 Client 관리 및 Heartbeat | [`SessionManager.cs`](block-racing-server/Network/SessionManager.cs)          |
| Player Session  | 개별 Client 송수신             | [`PlayerSession.cs`](block-racing-server/Network/PlayerSession.cs)            |
| Packet Manager  | Packet 분배                 | [`PacketManager.cs`](block-racing-server/Network/PacketManager.cs)            |
| Game Manager    | Matchmaking / Room Update | [`GameManager.cs`](block-racing-server/Game/GameManager.cs)                   |
| Match Maker     | Player 매칭                 | [`MatchMaker.cs`](block-racing-server/Game/Matchs/MatchMaker.cs)              |
| Room Manager    | Room 생성 및 관리              | [`RoomManager.cs`](block-racing-server/Game/Rooms/RoomManager.cs)             |
| Room            | 게임 Lifecycle 관리           | [`Room.cs`](block-racing-server/Game/Rooms/Room.cs)                           |
| Game Simulation | 게임 상태 및 규칙 실행             | [`GameSimulation.cs`](block-racing-server/Game/Simulations/GameSimulation.cs) |

### Related Code

* [`Network`](block-racing-server/Network)
* [`Game`](block-racing-server/Game)
* [`Matchs`](block-racing-server/Game/Matchs)
* [`Rooms`](block-racing-server/Game/Rooms)
* [`Simulations`](block-racing-server/Game/Simulations)
* [`Snapshots`](block-racing-server/Game/Snapshots)

---

# 🌐 Network

## TCP & Packet Processing

[`TcpServer`](block-racing-server/Network/TcpServer.cs)가 Client 연결을 수락하고 `PlayerSession`을 생성합니다.

Session은 [`SessionManager`](block-racing-server/Network/SessionManager.cs)가 관리하며, 수신된 Packet은 [`PacketManager`](block-racing-server/Network/PacketManager.cs)를 통해 적절한 Handler로 전달됩니다.

```text
TCP Stream
    ↓
Receive
    ↓
Packet Parsing
    ↓
Packet ID
    ↓
PacketManager
    ↓
Packet Handler
    ↓
Game / Session
```

Network Layer는 통신과 Packet 전달에 집중하고, 실제 게임 상태 변경은 Game Layer에서 처리하도록 책임을 분리했습니다.

---

# ❤️ Session & Heartbeat

실시간 게임에서는 TCP 연결이 즉시 종료되지 않는 상황을 고려해야 합니다.

[`SessionManager`](block-racing-server/Network/SessionManager.cs)는 Heartbeat를 통해 Session의 연결 상태를 확인합니다.

```text
Heartbeat Check
      │
      ├── Normal
      │
      └── Timeout
             ↓
         Disconnect
             ↓
        Player 처리
             ↓
         Room 처리
```

Timeout이 발생하면 Player의 연결 종료를 처리하고 게임 중이었다면 Room의 상태에도 반영합니다.

---

# 🎯 Matchmaking & Room

[`MatchMaker`](block-racing-server/Game/Matchs/MatchMaker.cs)는 Match State를 기준으로 대기 중인 Player를 관리하고 두 Player를 하나의 Room으로 연결합니다.

```text
None
 ↓
Queued
 ↓
Match
 ↓
InRoom
```

```text
Player A ──┐
           ├── MatchMaker ── RoomManager ── Room
Player B ──┘
```

각 Room은 독립적인 Game Simulation을 가지며 다음 Lifecycle을 관리합니다.

```text
Waiting
  ↓
Ready
  ↓
Starting
  ↓
Playing
  ↓
Ended
  ↓
Closing
```

관련 구현:

* [`MatchMaker.cs`](block-racing-server/Game/Matchs/MatchMaker.cs)
* [`RoomManager.cs`](block-racing-server/Game/Rooms/RoomManager.cs)
* [`Room.cs`](block-racing-server/Game/Rooms/Room.cs)

---

# 🎮 Game Simulation

게임의 실제 상태와 판정은 [`GameSimulation`](block-racing-server/Game/Simulations/GameSimulation.cs)이 담당합니다.

Server는 **고정 Tick 기반**으로 Simulation을 실행하며, 한 Tick 안의 처리 순서를 명확하게 유지합니다.

```text
Process Input
     ↓
Update Players
     ↓
Attack
     ↓
Block
     ↓
Line Clear
     ↓
Lane Scroll
     ↓
Collision
     ↓
Game End Check
```

게임 로직을 명확한 Pipeline으로 구성하여 **시스템 간 처리 순서와 상태 의존성을 관리**했습니다.

주요 Simulation 구현:

* [`Simulations`](block-racing-server/Game/Simulations)
* [`Blocks`](block-racing-server/Game/Simulations/Blocks)
* [`Lanes`](block-racing-server/Game/Simulations/Lanes)

---

# 🔄 State Synchronization

Server는 게임 상태를 Snapshot으로 변환하여 Client에 전달합니다.

```text
GameSimulation
      ↓
Game State
      ↓
GameStateSnapshot
      │
      ├── Tick
      └── Players
             ├── PlayerSnapshot
             └── LaneSnapshot
                    ├── Blocks
                    └── FlyingBlocks
      ↓
Client
```

Snapshot을 통해 **게임의 실제 상태(Server)**와 **화면 표현(Client)**을 분리했습니다.

관련 구현:

* [`Snapshots`](block-racing-server/Game/Snapshots)

---

# ⚖️ Game Balance

게임 밸런스 값은 코드와 분리하여 설정 파일로 관리합니다.

```text
Config/game_balance.json
          ↓
GameBalanceLoader
          ↓
GameBalance
          ↓
Game Simulation
```

주요 설정:

* Car Speed
* Max Speed
* Speed Bonus / Penalty
* Stun Duration
* Scroll Speed
* Piece Cooldown
* Target Distance

관련 구현:

* [`game_balance.json`](block-racing-server/Config/game_balance.json)
* [`GameBalance.cs`](block-racing-server/Data/GameBalance.cs)
* [`GameBalanceLoader.cs`](block-racing-server/Data/GameBalanceLoader.cs)

---

# 📝 Logging

Server 상태 추적을 위해 Logging 시스템을 사용합니다.

주요 기록 대상:

* Server Start / Shutdown
* Client Connect / Disconnect
* Session 상태 변경
* Matchmaking
* Room 생성 / 종료
* Game Start / End
* Heartbeat Timeout
* Exception

반복적인 Game Tick 로그는 최소화하고 **상태 변화와 예외 상황을 중심으로 기록**합니다.

---

# 📁 Project Structure

```text
block-racing-server/
│
├── Common/                    # Shared Module
│
├── block-racing-server/
│   ├── Config/
│   │   └── game_balance.json
│   │
│   ├── Data/
│   │   ├── GameBalance.cs
│   │   └── GameBalanceLoader.cs
│   │
│   ├── Game/
│   │   ├── Matchs/
│   │   ├── Players/
│   │   ├── Rooms/
│   │   ├── Rules/
│   │   ├── Simulations/
│   │   └── Snapshots/
│   │
│   ├── Network/
│   │   ├── Handlers/
│   │   ├── PacketManager.cs
│   │   ├── PlayerSession.cs
│   │   ├── SessionManager.cs
│   │   └── TcpServer.cs
│   │
│   └── Program.cs
│
└── README.md
```

### 🔍 주요 코드

* [`TcpServer.cs`](block-racing-server/Network/TcpServer.cs)
* [`SessionManager.cs`](block-racing-server/Network/SessionManager.cs)
* [`PlayerSession.cs`](block-racing-server/Network/PlayerSession.cs)
* [`PacketManager.cs`](block-racing-server/Network/PacketManager.cs)
* [`GameManager.cs`](block-racing-server/Game/GameManager.cs)
* [`MatchMaker.cs`](block-racing-server/Game/Matchs/MatchMaker.cs)
* [`RoomManager.cs`](block-racing-server/Game/Rooms/RoomManager.cs)
* [`Room.cs`](block-racing-server/Game/Rooms/Room.cs)
* [`GameSimulation.cs`](block-racing-server/Game/Simulations/GameSimulation.cs)

---

# 🛠️ Tech Stack

| Category      | Technology                              |
| ------------- | --------------------------------------- |
| Language      | C#                                      |
| Runtime       | .NET 9                                  |
| Network       | TCP Socket                              |
| Architecture  | Dedicated Server / Server Authoritative |
| Logging       | Serilog                                 |
| Shared Module | Git Submodule                           |
| Repository    | Git / GitHub                            |

---

# 🔗 Related Repositories

* [Block Racing](https://github.com/rlawodud89/block-racing) — 전체 프로젝트
* [Block Racing Client](https://github.com/rlawodud89/block-racing-client) — Unity Client
* [Block Racing Common](https://github.com/rlawodud89/block-racing-common) — Client / Server Shared Module

---

# 🎯 Development Focus

Block Racing Server의 핵심 목표는 **실시간 멀티플레이 환경에서 일관된 게임 상태를 유지할 수 있는 서버 구조를 직접 설계하는 것**입니다.

주요 구현 및 학습 영역:

* **Server Authoritative Architecture**
* **Fixed Tick 기반 Game Simulation**
* **TCP 기반 실시간 통신**
* **Session & Connection Lifecycle Management**
* **Heartbeat 기반 Disconnect Detection**
* **Matchmaking State Management**
* **Room Lifecycle Management**
* **Snapshot 기반 State Synchronization**
* **Multiplayer Error Handling**
* **Logging 및 성능 분석**

이를 통해 네트워크 통신부터 Session, Matchmaking, Room, Game Simulation, State Synchronization까지 **멀티플레이 게임 서버의 전체 흐름을 직접 구현하고 문제를 해결하는 경험**을 목표로 개발했습니다.
