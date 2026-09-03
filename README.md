# 🧱 Block Racing

> **2인 실시간 대전 레이싱 PC 게임**
>
> Unity Client와 C# Dedicated Server를 기반으로 구현한 실시간 멀티플레이 게임 프로젝트입니다.

🚧 **현재 개발 진행 중**

---

## 🎮 Overview

**블록 레이싱**은 두 플레이어가 각각 자신의 Lane에서 블록을 쌓으며 경쟁하는 2인 온라인 멀티플레이 게임입니다.

일반적인 블록 퍼즐 게임에 **상대방에게 블록을 보내는 공격 시스템**과 **차량 이동 및 레이싱 요소**를 결합했습니다.

게임의 핵심 상태와 판정은 **Dedicated Server에서 authoritative하게 처리**하고, Client는 입력을 전달하고 Server의 게임 상태를 받아 화면에 반영하는 구조로 설계했습니다.

### 주요 목표

* 실시간 2인 멀티플레이
* Server Authoritative Game Simulation
* Matchmaking → Room → Game Lifecycle
* Client / Server 간 State Synchronization
* Block / Attack / Line Clear 시스템
* 네트워크 환경에서 일관된 게임 상태 유지

---

# 🏗️ Project Architecture

프로젝트는 **Client / Server / Common**을 각각 독립적인 Repository로 관리합니다.

Common은 Client와 Server에서 사용하는 Packet, Snapshot, Enum 등의 공통 데이터를 관리하며 **Git Submodule**로 양쪽 프로젝트에 포함되어 있습니다.

```text
                         ┌──────────────────────┐
                         │       Common         │
                         │    Shared Module     │
                         │                      │
                         │ Packets / Snapshots  │
                         │ Enums / Shared Types │
                         └───────┬──────┬───────┘
                                 │      │
                             포함│      │포함
                                 │      │
                    ┌────────────▼─┐  ┌─▼───────────────┐
                    │ Unity Client │  │ Dedicated Server│
                    │              │  │                 │
                    │ Input        │  │ Session         │
                    │ Rendering    │  │ Matchmaking     │
                    │ UI / Scene   │  │ Room            │
                    │ State Sync   │  │ Game Simulation │
                    └───────┬──────┘  └────────┬────────┘
                            │                  │
                            └────── TCP ───────┘
```

---

## 🔐 Server Authoritative

게임의 핵심 상태와 판정은 Server에서 수행합니다.

```text
Client
  │
  │ Input
  ▼
Server
  │
  ├── Game Simulation
  ├── Collision
  ├── Attack
  ├── Line Clear
  ├── Lane Scroll
  └── Game Result
  │
  │ Snapshot
  ▼
Client
  │
  └── Rendering
```

Client의 입력이나 화면 상태가 게임의 최종 결과를 결정하지 않고, **Server가 게임 상태를 계산한 뒤 그 결과를 Client에 전달**합니다.

이를 통해 Client의 프레임이나 실행 환경과 관계없이 게임의 기준 상태를 Server에서 일관되게 유지합니다.

---

# 📦 Repositories

각 영역을 독립적인 Repository로 관리하고 있습니다.

| Repository     | 역할                                    |
| -------------- | ------------------------------------- |
| 🎮 **Client**  | Unity 기반 게임 Client                    |
| 🖥️ **Server** | C# / .NET 기반 Dedicated Server         |
| 📦 **Common**  | Client / Server 공통 Packet 및 Game Data |

```text
Block Racing
│
├── block-racing
│   └── Project Showcase / Documentation
│
├── block-racing-client
│   └── Unity Client
│
├── block-racing-server
│   └── Dedicated Server
│
└── block-racing-common
    └── Shared Module
```

### 🎮 Client

Unity 기반 게임 Client입니다.

* TCP Network Client
* Input 처리
* Server Snapshot 기반 State Synchronization
* Matchmaking / Room UI
* Game / Result Scene
* Game State Rendering
* Connection / Reconnect 처리

→ **[Block Racing Client Repository](https://github.com/rlawodud89/block-racing-client)**

### 🖥️ Server

C# / .NET 기반 Dedicated Server입니다.

* TCP Server
* Session Management
* Matchmaking
* Room Lifecycle
* Fixed Tick Game Simulation
* Collision / Attack / Line Clear
* Lane Scroll
* Game Result
* Heartbeat / Disconnect Detection

→ **[Block Racing Server Repository](https://github.com/rlawodud89/block-racing-server)**

### 📦 Common

Client와 Server가 공유하는 Network / Game Data 모듈입니다.

* Packet
* Snapshot
* Enum
* Shared Data Types
* Packet Serialization / Deserialization

→ **[Block Racing Common Repository](https://github.com/rlawodud89/block-racing-common)**

---

# ⚙️ Tech Stack

| Category         | Technology                              |
| ---------------- | --------------------------------------- |
| Client           | Unity 2022.3.62f3                       |
| Client Language  | C#                                      |
| Server           | .NET 9                                  |
| Server Language  | C#                                      |
| Network          | TCP Socket                              |
| Architecture     | Dedicated Server / Server Authoritative |
| Shared Module    | Git Submodule                           |
| Logging          | Serilog                                 |
| IDE              | Visual Studio 2022                      |
| Platform         | Windows                                 |

---

# 🎯 Core Features

## 1. Login & Session

Client가 Server에 접속하면 TCP Session을 생성하고 Login Packet을 통해 Player를 등록합니다.

```text
Client
  │
  │ Login
  ▼
TCP Session
  │
  ▼
Player
```

---

## 2. Matchmaking

Server의 `MatchMaker`가 대기 중인 Player를 관리하고 두 Player를 하나의 Room으로 연결합니다.

```text
Player A ──┐
           │
           ▼
       MatchMaker
           │
           ▼
Player B ──┘
           │
           ▼
        Room 생성
```

Player의 Match State를 통해 매칭 과정의 상태를 관리합니다.

```text
None
  ↓
Queued
  ↓
Matching
  ↓
InRoom
```

---

## 3. Room Lifecycle

Matchmaking 이후 Room이 게임의 전체 Lifecycle을 관리합니다.

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

Room에서는 다음과 같은 게임 흐름을 관리합니다.

* Player 입장 / 퇴장
* Ready
* Game Start
* Game End
* Opponent Disconnect
* Rematch
* Room Cleanup

---

## 4. Fixed Tick Game Simulation

Server는 고정된 Tick을 기준으로 게임 Simulation을 실행합니다.

```text
Server Game Loop
       │
       ▼
   GameManager
       │
       ▼
     Room
       │
       ▼
GameSimulation
       │
       ├── Input
       ├── Player
       ├── Attack
       ├── Block
       ├── Line Clear
       ├── Lane Scroll
       └── Collision
```

현재 Server는 **20 Tick/s (50ms)** 기준으로 Game Simulation을 진행합니다.

이를 통해 Client의 Frame Rate와 관계없이 Server 기준으로 게임 상태를 업데이트합니다.

---

## 5. Block & Attack

게임의 핵심 플레이 요소는 Block과 Attack입니다.

```text
Player Input
     │
     ▼
   Shoot
     │
 ┌───┴────┐
 ▼        ▼
Defense   Attack
 │         │
 ▼         ▼
Flying    Attack
Block     Piece
 │         │
 ▼         ▼
Lane      Opponent Lane
```

수비 모드에서는 자신의 Lane에 Block을 생성하고, 공격 모드에서는 상대방 Lane에 Block을 생성하기 위한 Attack 정보를 전달합니다.

---

## 6. State Synchronization

Server는 게임 상태를 Snapshot으로 구성하여 Client에 전달합니다.

```text
Server Game State
       │
       ▼
GameStateSnapshot
       │
       ├── Tick
       │
       └── Players
             │
             ├── PlayerSnapshot
             │
             └── LaneSnapshot
                    ├── Blocks
                    └── FlyingBlocks
       │
       ▼
     Client
       │
       ▼
   Rendering
```

Client는 전달받은 Snapshot을 기준으로 게임 화면을 갱신합니다.

---

## 7. Network & Error Handling

실시간 TCP 통신 환경에서 발생할 수 있는 연결 문제를 처리하기 위해 Session 및 Heartbeat 구조를 사용합니다.

```text
TCP Connection
      │
      ▼
Session
      │
      ▼
Heartbeat
      │
      ├── Normal
      │
      └── Timeout
            │
            ▼
        Disconnect
            │
            ▼
      Game / UI Cleanup
```

이를 통해 게임 도중 발생하는 Client / Server 연결 종료에도 게임 상태와 UI가 비정상적으로 남지 않도록 처리합니다.

---

# 📚 Detailed Documentation

프로젝트의 세부 구현과 설계 내용은 각 Repository의 README 및 향후 `docs`에서 관리할 예정입니다.

### Client

→ **[Client README](https://github.com/rlawodud89/block-racing-client)**

Client Network, State Synchronization, Game State Rendering 등의 구현 내용을 확인할 수 있습니다.

### Server

→ **[Server README](https://github.com/rlawodud89/block-racing-server)**

Session, Matchmaking, Room, GameSimulation, Snapshot Sync 등의 서버 구조를 확인할 수 있습니다.

### Common

→ **[Common README](https://github.com/rlawodud89/block-racing-common)**

Packet, Snapshot, Enum 및 Client / Server 간 Shared Contract를 확인할 수 있습니다.

---

# 💡 Development Focus

이 프로젝트의 핵심 목표는 단순한 멀티플레이 기능 구현이 아니라,

> **실시간 멀티플레이 환경에서 일관된 게임 상태를 유지할 수 있는 서버 구조를 직접 설계하고 구현하는 것**

입니다.

주요 개발 관심사는 다음과 같습니다.

* **Server Authoritative Architecture**
* **Fixed Tick Game Simulation**
* **Real-time TCP Communication**
* **Session & Connection Lifecycle**
* **Matchmaking State Management**
* **Room Lifecycle Management**
* **Snapshot-based State Synchronization**
* **Client / Server Shared Data**
* **Multiplayer Error Handling**
* **Logging & Performance Analysis**

Client → Server로 입력을 전달하고, Server → Client로 확정된 게임 상태를 전달하는 구조를 통해 **Network / Game Logic / Rendering의 책임을 분리**하는 것을 목표로 개발하고 있습니다.

---

# 🚧 Development Status

현재 다음 기능을 중심으로 개발 및 테스트를 진행하고 있습니다.

* [x] TCP Client / Server 통신
* [x] Login / Session
* [x] Matchmaking
* [x] Room Management
* [x] Server Authoritative Game Simulation
* [x] Block / Attack System
* [x] Line Clear
* [x] Lane Scroll
* [x] Collision / Stun
* [x] Snapshot State Synchronization
* [x] Heartbeat / Disconnect Handling
* [ ] Performance Optimization
* [ ] Concurrency / Stress Test 고도화
* [ ] Deployment / 운영 환경 검증
