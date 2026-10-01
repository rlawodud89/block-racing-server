# Concurrency Test & Race Condition

다수의 Unity Client를 직접 실행하는 방식에는 동시 접속자 수를 늘리는 데 한계가 있기 때문에, 별도의 **Console 기반 테스트 클라이언트**를 제작하여 다수의 TCP Client를 자동으로 생성하고 동시성 테스트를 수행했습니다.

테스트 클라이언트는 별도 Repository로 관리하며, Connection, Login, MatchMaking, Game Start, Input, Disconnect 등의 테스트 시나리오를 실행할 수 있도록 구성했습니다.

**[Block Racing Test Repository](https://github.com/rlawodud89/block-racing-test)**

---

## 테스트 결과

테스트 규모를 단계적으로 증가시키면서 Connection, Login, MatchMaking, Game Start, Input, Disconnect 단계의 성공/실패 및 Timeout을 확인했습니다.

### Connection / Login

300, 350, 500, 1,000 Client 규모로 동시 연결 및 Login 테스트를 수행했습니다.

다수의 Client가 동시에 연결되는 상황에서 Connection 및 Login 처리와 Session 등록 상태를 검증했습니다.

### MatchMaking

50, 100, 200, 최대 1,000 Client 규모로 동시 MatchMaking 테스트를 수행했습니다.

각 Client가 정상적으로 Queue에 등록되고 2명씩 Room에 배정되는지 확인하였으며, 중복 매칭 및 `MatchState` 전이를 검증했습니다.

### Game Start / Room

동시에 여러 Room을 생성하고 각 Room에서 Game Start Sequence가 정상적으로 진행되는지 확인했습니다.

Game Start 중복 호출, Room별 Player 수, Room 간 게임 상태 간섭 및 State 전이를 검증했습니다.

### Game Input

게임 중 여러 Client가 지속적으로 Input Packet을 전송하여 서버의 Input 처리와 Game Tick을 검증했습니다.

| Clients | Input Success | Input Fail |
| ------: | ------------: | ---------: |
|      50 |       101,178 |          0 |
|     100 |       208,158 |          0 |
|     200 |       380,723 |          0 |

모든 테스트에서 Input Fail은 발생하지 않았으며, `GameSimulation`과 Tick 처리도 정상적으로 유지되는 것을 확인했습니다.

---

# Race Condition 발견

동시 Disconnect 테스트에서 500개의 Client를 동시에 종료시키던 중 다음 예외가 발생했습니다.

```text
System.ArgumentOutOfRangeException:

Index was out of range.

at System.Collections.Generic.List`1.RemoveAt(Int32 index)
at System.Collections.Generic.List`1.Remove(T item)
at Room.RemovePlayerAsync(Player player)
at GameManager.UnregisterPlayer(Player player)
at PlayerSession.DisconnectAsync()
```

문제가 발생한 호출 흐름은 다음과 같았다.

```text
PlayerSession.DisconnectAsync()
        ↓
GameManager.UnregisterPlayer()
        ↓
Room.RemovePlayerAsync()
        ↓
_players.Remove(player)
```

여러 Session Task가 동시에 Disconnect를 수행하면서 동일한 `Room._players`에 접근할 수 있었고, `_players`가 `List<Player>`로 관리되고 있어 동시 수정 과정에서 Race Condition이 발생했습니다.

## 원인 분석

Game Loop는 하나의 Loop에서 순차적으로 실행됩니다.

```text
Game Loop
    ↓
GameManager.Update()
    ↓
Room.Update()
```

하지만 각 `PlayerSession`은 별도의 Task에서 동작합니다.

```text
Session A ──→ Task
Session B ──→ Task
Session C ──→ Task
                ↓
          DisconnectAsync()
                ↓
          Room 상태 변경
```

따라서 Game Loop가 `Room`을 순차적으로 처리하더라도 Session에서 발생하는 상태 변경은 Game Loop와 동시에 실행될 수 있었다.

특히 다음 상태가 여러 Session에서 공유되고 있었다.

```text
_players
_readyMap
_rematchMap
State
```

또한 다음 메서드가 서로 다른 Session Task에서 동시에 호출될 수 있었다.

```text
AddPlayer()

RemovePlayerAsync()

SetReady()

EnqueueInput()

RequestRematch()
```

즉, **Game Loop 자체는 단일 실행이지만 Room의 모든 상태 접근이 단일 실행을 보장하는 구조는 아니었다.**

---

# 동시성 제어

## 전체 Game Loop Lock은 사용하지 않음

처음에는 `Room.Update()` 전체를 Lock으로 보호하는 방법을 고려했습니다.

하지만 `Update()` 내부에는 GameSimulation뿐만 아니라 Snapshot 생성과 Socket Send도 포함되어 있습니다.

```text
Room.Update()
    ↓
Simulation Update
    ↓
Sync()
    ↓
Socket Send
```

전체를 Lock으로 보호하면 게임 로직과 네트워크 전송까지 불필요하게 직렬화될 수 있습니다.

따라서 **Game Loop 전체가 아닌 외부 Session에서 Room 상태를 변경하는 작업만 직렬화**하도록 설계를 변경했습니다.

## SemaphoreSlim 적용

`Room`에 `SemaphoreSlim` 기반의 `_stateLock`을 추가했습니다.

```text
PlayerSession
      ↓
Room 상태 변경 요청
      ↓
   _stateLock
      ↓
   상태 변경
      ↓
   Lock 해제
```

다음 상태 변경 메서드를 보호했습니다.

```text
AddPlayer()

RemovePlayerAsync()

SetReady()

EnqueueInput()

RequestRematch()
```

이를 통해 여러 Session Task가 동시에 Room 상태를 변경하더라도 한 번에 하나의 작업만 공유 상태를 수정하도록 보장했습니다.

---

# 비동기 상태 변경 Race 처리

Lock 내부에서 장시간 실행되는 작업을 수행하지 않도록 **상태 변경과 실제 비동기 작업을 분리**했습니다.

```text
SetReady()
    ↓
State 변경
    ↓
Lock 해제
    ↓
StartGameSync()
```

```text
RequestRematch()
    ↓
State 변경
    ↓
Lock 해제
    ↓
RestartRoomAsync()
```

Lock을 해제한 이후 Disconnect가 먼저 발생할 수 있기 때문에 실제 게임 시작 및 재시작 직전에 Room 상태를 다시 확인하도록 변경했습니다.

```text
StartGameSync()
    ↓
State == Starting?
PlayerCount == 2?
    ↓
게임 시작
```

```text
RestartRoomAsync()
    ↓
State == Ready?
PlayerCount == 2?
    ↓
Room 재시작
```

이를 통해 상태 변경과 후속 작업 사이에 발생하는 Race Condition도 방어했습니다.

---

# Game End / Disconnect Race 처리

게임 종료 처리와 Disconnect가 동시에 발생하는 경우도 고려했습니다.

```text
Room.Update()
    ↓
EndGame()
    ↓
Game End 처리 대기
          ↑
          │
     Disconnect
          ↓
       Closing
```

Disconnect로 인해 Room이 `Closing` 상태가 된 이후 Game End 처리에서 다시 `Result` 상태로 변경되면 이미 종료된 Room의 상태가 되돌아가는 문제가 발생할 수 있습니다.

따라서 Game End 처리 시 `Closing` 상태를 먼저 확인하도록 변경했습니다.

```text
HandleGameEndAsync()
        ↓
State == Closing?
   ├─ Yes → 상태 변경하지 않음
   └─ No  → Result
```

이를 통해 Disconnect에 의해 종료된 Room이 다시 게임 결과 상태로 변경되는 것을 방지했습니다.

---

# 해결 결과

수정 전에는 500명의 Client를 동시에 Disconnect할 경우 다음과 같은 문제가 발생했습니다.

```text
500 Clients Disconnect
        ↓
Multiple Session Tasks
        ↓
Room._players 동시 수정
        ↓
ArgumentOutOfRangeException
        ↓
Session 정리 중단
```

`SemaphoreSlim`을 적용하여 Room의 공유 상태 변경을 직렬화한 이후 동일한 테스트를 다시 수행했습니다.

```text
500 Clients Disconnect
        ↓
Room 상태 변경 직렬화
        ↓
Player 정상 제거
        ↓
Room Closing
        ↓
Room 제거
```

기존에 발생하던 `ArgumentOutOfRangeException`이 더 이상 발생하지 않았으며, Disconnect 이후 Session / Player / Room 상태가 정상적으로 정리되는 것을 확인했습니다.

## 최종 결과

**문제**

> 여러 Session Task가 동일한 Room 상태를 동시에 수정하면서 Race Condition 발생

**원인**

> Game Loop는 단일 실행이지만 Session에서 발생하는 Room 상태 변경은 별도의 Task에서 동시에 실행됨

**해결**

> `SemaphoreSlim`을 이용하여 외부 Session에서 수행되는 Room 상태 변경을 직렬화하고, Lock 외부의 비동기 작업에서도 Room 상태와 Player 수를 재검증

**검증**

> 500 Client 동시 Disconnect 테스트에서 기존 `ArgumentOutOfRangeException`이 재현되지 않고 정상적인 Session / Player / Room 정리를 확인
