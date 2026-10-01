# Performance Optimization

## 테스트 환경

| 항목          | 환경                                          |
| ----------- | ------------------------------------------- |
| OS          | Windows                                     |
| Server      | .NET                                        |
| Test Client | Console 기반 다중 클라이언트                         |
| 측정 도구       | `dotnet-counters`, `dotnet-trace`, PerfView |
| 데이터 분석      | Excel                                       |

모든 성능 테스트는 **Windows 로컬 환경**에서 진행했습니다.

다수의 Client를 이용한 테스트는 별도의 Console 기반 테스트 클라이언트를 사용했습니다.

**[Block Racing Test Repository](https://github.com/rlawodud89/block-racing-test)**

---

# 테스트 조건

성능 테스트는 **Baseline 측정**과 **최적화 단계별 측정**으로 나누어 진행했습니다.

### Baseline

최적화 전 서버의 성능 특성을 확인하기 위해 동시 접속자 수를 단계적으로 증가시켰습니다.

```text
100
 ↓
200
 ↓
300
 ↓
400
 ↓
500
```

동시 접속자 증가에 따른 CPU, Memory, Allocation, GC 등의 변화를 확인했습니다.

### 최적화 단계

Baseline에서 성능 문제가 확인된 이후에는 **500명 동시 접속 환경을 고정**하여 각 최적화 단계의 결과를 비교했습니다.

```text
500 Players
     ↓
Measurement
     ↓
Allocation Analysis
     ↓
Optimization
     ↓
Measurement
     ↓
Next Hotspot Analysis
     ↓
...
     ↓
Final
```

동일한 부하 조건을 유지하여 각 최적화가 성능 지표에 미치는 영향을 비교했습니다.

---

# 측정 방법

## dotnet-counters

`dotnet-counters`를 이용하여 CPU, Memory, Allocation, GC, ThreadPool Queue 등의 Runtime 성능 지표를 1초 간격으로 수집했습니다.

```bash
dotnet-counters ps

dotnet-counters collect \
  --process-id 6080 \
  --refresh-interval 1 \
  --format csv \
  -o "performance_100.csv"
```

## dotnet-trace / PerfView

`dotnet-counters`를 통해 Allocation과 GC 관련 지표의 증가를 확인한 이후, 구체적인 allocation 발생 위치를 분석하기 위해 `dotnet-trace`와 PerfView를 사용했습니다.

```bash
dotnet-trace collect \
  -p 12345 \
  --duration 00:03:00 \
  --profile gc-verbose \
  -o "gc_optimization_1.nettrace"
```

`dotnet-counters`는 **성능 지표의 변화 확인**, `dotnet-trace`와 PerfView는 **allocation 및 GC 발생 원인 분석**에 사용했습니다.

---

# 측정 지표

| 지표             | 확인 내용                   |
| -------------- | ----------------------- |
| Players        | 동시 접속자 수                |
| CPU User       | 사용자 코드 CPU 사용량          |
| CPU System     | 시스템/네트워크 처리 CPU 사용량     |
| Memory Avg     | 평균 Working Set          |
| Memory Max     | 최대 Working Set          |
| Allocation Avg | 평균 Managed Allocation   |
| Allocation Max | 최대 Allocation           |
| Gen0 GC        | Gen0 GC 발생 횟수           |
| Gen1 GC        | Gen1 GC 발생 횟수           |
| Gen2 GC        | Gen2 GC 발생 횟수           |
| GC Pause       | GC에 의한 중단 시간            |
| Queue Max      | ThreadPool Queue 최대 적체량 |

주요 Runtime 지표는 `dotnet-counters`의 CSV를 Excel에서 집계하여 테스트별 결과를 비교했습니다.

---

# 문제 발견

Baseline 측정 결과 동시 접속자 증가에 따라 **Managed Allocation과 GC 관련 지표가 증가**하는 현상을 확인했습니다.

특히 GC Pause 증가가 확인되어 반복적으로 실행되는 게임 Tick 및 네트워크 동기화 과정의 allocation을 주요 분석 대상으로 선정했습니다.

```text
Baseline
   ↓
Allocation 증가
   ↓
GC 증가
   ↓
GC Pause 증가
   ↓
dotnet-trace 수집
   ↓
PerfView 분석
   ↓
Allocation Hotspot 확인
```

500명 동시 접속 상태에서 GC Trace를 분석한 결과 **Gen1 GC가 높은 비중을 차지하고 있었으며**, 반복적인 객체 및 `byte[]` allocation이 GC Pause 증가에 영향을 주고 있음을 확인했습니다.

Allocation Stack을 추적한 결과 `S_GameStatePacket`을 `PacketWriter`로 직렬화하는 과정에서 많은 `byte[]` allocation이 발생하고 있었다.

따라서 다음 영역을 우선적으로 최적화했습니다.

* 반복적인 Packet Serialization
* 게임 Tick에서 반복되는 Enumerator 및 LINQ allocation

---

# GC 최적화

최적화는 메모리 풀링을 먼저 적용하기보다 **실제 allocation hotspot을 측정하고 현재 코드 구조에서 제거할 수 있는 allocation부터 줄이는 방식**으로 진행했습니다.

```text
Allocation 측정
      ↓
Hotspot 확인
      ↓
코드 원인 분석
      ↓
Allocation 제거
      ↓
동일 조건 재측정
      ↓
다음 Hotspot 분석
```

## 1. PacketWriter 개선

기존 `PacketWriter`는 `Write()` 과정에서 중간 데이터를 생성하고 `List<byte>`에 데이터를 추가하는 구조였다.

이를 미리 `byte[]` 버퍼를 확보하고 `_position`을 이용하여 직접 기록하는 방식으로 변경했습니다.

```text
Before

Write()
  ↓
Temporary Data
  ↓
List<byte>
  ↓
Dynamic Expansion
```

```text
After

Write()
  ↓
Preallocated byte[]
  ↓
Direct Write
```

**결과**

* 임시 데이터 allocation 감소
* `List<byte>` 확장에 따른 allocation 감소
* Packet Serialization allocation 감소

---

## 2. Room.Sync() PacketWriter 재사용

`Room.Sync()`는 게임 Tick마다 실행되는 hot path입니다.

기존에는 Sync 호출마다 `PacketWriter`를 새로 생성하였으나, `Room`에서 하나의 `PacketWriter`를 보유하고 `Reset()`하여 재사용하도록 변경했습니다.

```text
Room
 └── PacketWriter

Sync()
 ├── Reset()
 ├── Serialize
 └── ToArray()
```

**결과**

* 매 Tick `PacketWriter` 생성 제거
* 반복적인 패킷 직렬화 allocation 감소

---

## 3. Enumerator 제거

PerfView 분석에서 `FlyingBlockPosition` Enumerator가 높은 비중의 allocation을 차지하는 것을 확인했습니다.

기존 `IReadOnlyList<T>`의 `foreach` 기반 순회를 `List<T>`와 인덱스 기반 `for` 순회로 변경했습니다.

```text
IReadOnlyList<T>
      ↓
List<T>
      ↓
for + index
```

이를 통해 인터페이스를 통한 Enumerator boxing 가능성을 제거했습니다.

또한 Player Dictionary에 대한 반복적인 열거 과정도 실제 Dictionary에 직접 접근하도록 변경했습니다.

초기 측정에서 Player Enumerator는 약 **163MB / 9.6%** 수준의 allocation을 차지했으며, 이후 최적화를 통해 약 **42MB / 2.9%** 수준까지 감소했습니다.

→ 해당 allocation 약 **74% 감소**

---

## 4. LINQ 제거

게임 Tick에서 반복적으로 실행되는 LINQ 연산을 직접 순회 방식으로 변경했습니다.

주요 대상은 다음과 같다.

```text
Where()
Select()
ToList()
ToDictionary()
DefaultIfEmpty()
Max()
```

예를 들어 `UpdateBlockSystem()`에서 다음과 같은 LINQ를 직접 순회 방식으로 변경했습니다.

```csharp
lane.FlyingBlocks
    .Where(b => !b.IsFinished)
    .ToList();
```

↓

```csharp
List<FlyingBlock> activeBlocks = new();

foreach (FlyingBlock block in lane.FlyingBlocks)
{
    if (!block.IsFinished)
        activeBlocks.Add(block);
}
```

Snapshot 생성 과정의 `Select().ToList()` 역시 직접 순회하도록 변경했습니다.

```csharp
List<FlyingBlockSnapshot> snapshots =
    new(lane.FlyingBlocks.Count);

foreach (FlyingBlock block in lane.FlyingBlocks)
{
    snapshots.Add(
        FlyingBlockSnapshotBuilder.Create(block));
}
```

`GameEndSystem`의 Player 검색 과정과 `UpdateBlockSystem()`의 반복적인 LINQ 연산도 동일한 방식으로 변경했습니다.

**결과**

PerfView에서 확인되던 Iterator 및 LINQ 관련 allocation을 제거했습니다.

---

## 5. UpdateBlockSystem() 최적화

`UpdateBlockSystem()` 내부에서 반복적으로 실행되는 LINQ 연산을 추가로 분석했습니다.

특히 다음 연산들이 Player별로 매 Tick 실행되고 있었다.

```text
ToDictionary()
Select()
ToList()
DefaultIfEmpty()
Max()
```

게임 로직의 계산 결과는 유지하면서 반복문을 직접 사용하도록 변경했습니다.

예를 들어 현재 Grid 위치와 다음 Grid 위치를 계산하는 과정에서 직접 Dictionary를 구성하도록 변경했습니다.

```csharp
Dictionary<FlyingBlock, int> currentGridYs =
    new(activeBlocks.Count);

Dictionary<FlyingBlock, int> nextGridYs =
    new(activeBlocks.Count);

foreach (FlyingBlock block in activeBlocks)
{
    int currentGridY = block.GridY;
    int nextGridY =
        (int)MathF.Floor(
            block.Y + block.MoveSpeed * deltaTime);

    currentGridYs.Add(block, currentGridY);
    nextGridYs.Add(block, nextGridY);
}
```

이후 최대 이동량 역시 직접 순회하여 계산했습니다.

```csharp
int maxSteps = 0;

foreach (FlyingBlock block in activeBlocks)
{
    int steps =
        nextGridYs[block] - currentGridYs[block];

    if (steps > maxSteps)
        maxSteps = steps;
}
```

여기서는 기존 로직과 동작을 최대한 동일하게 유지하면서 Iterator를 생성하는 LINQ 연산을 우선 제거하는 방향으로 최적화했습니다.

---

# 단계별 검증

각 최적화 단계마다 동일하게 **500명 동시 접속 조건**을 적용하여 다시 측정했습니다.

```text
500 Players
     ↓
Measure
     ↓
Analyze Allocation
     ↓
Optimize
     ↓
Measure
     ↓
Analyze Next Hotspot
     ↓
...
```

각 단계의 Trace를 PerfView에서 분석하고 다음 allocation hotspot을 확인하는 과정을 반복했습니다.

이를 통해 특정 최적화가 실제 성능 지표에 미치는 영향을 단계별로 확인했습니다.

---

# 최종 결과

최적화 전후의 상세 결과는 `block-racing-performance-test.xlsx`에 정리했습니다.

| Metric                | Baseline 500 | Final 500 Avg |             변화 |
| --------------------- | -----------: | ------------: | -------------: |
| CPU User Avg (%)      |        0.228 |         0.108 | 약 **52.5% 감소** |
| CPU System Avg (%)    |        0.442 |         0.410 |  약 **7.3% 감소** |
| Memory Avg (MB)       |        81.83 |         80.25 |  약 **1.9% 감소** |
| Memory Max (MB)       |        97.07 |         91.15 |  약 **6.1% 감소** |
| Allocation Avg (MB/s) |        15.06 |          6.49 | 약 **56.9% 감소** |
| Allocation Max (MB/s) |        31.83 |         13.55 | 약 **57.4% 감소** |
| Gen0 GC               |            4 |          32.7 |              - |
| Gen1 GC               |          313 |          87.7 | 약 **72.0% 감소** |
| Gen2 GC               |            3 |           3.0 |          거의 동일 |
| GC Pause (ms)         |       3146.5 |         936.1 | 약 **70.3% 감소** |
| Queue Max             |          173 |          70.7 | 약 **59.1% 감소** |

최종 500명 동시 접속 테스트에서 주요 지표는 다음과 같이 변화했습니다.

* CPU User: **52.5% 감소**
* Managed Allocation: **56.9% 감소**
* Gen1 GC: **72.0% 감소**
* GC Pause: **70.3% 감소**
* ThreadPool Queue Max: **59.1% 감소**

GC Pause는 측정별 변동이 존재했지만 최종 평균 약 **936ms** 수준으로 확인되었습니다.

---

# 성능 분석 흐름

```text
Console 다중 클라이언트
          ↓
실제 게임 시나리오 수행
          ↓
Baseline 측정
100 → 200 → ... → 500
          ↓
dotnet-counters
CPU / Memory / Allocation / GC / Queue
          ↓
Allocation / GC 증가
          ↓
dotnet-trace / PerfView
          ↓
Allocation Hotspot 확인
          ↓
코드 최적화
PacketWriter / Enumerator / LINQ
          ↓
500명 고정 재측정
          ↓
다음 Hotspot 분석
          ↓
반복
          ↓
Final Measurement
Before / After 비교
```

---

# 결과 파일

성능 분석에 사용한 최종 결과 파일은 다음과 같습니다.

```text
performance-test-result/
├── baseline.nettrace
├── block-racing-performance-test.xlsx
├── gc_optimization_1.nettrace
├── gc_optimization_2.nettrace
├── gc_optimization_3.nettrace
├── gc_optimization_4_1.nettrace
├── gc_optimization_4_2.nettrace
├── gc_optimization_4_3.nettrace
├── gc_optimization_4_4.nettrace
├── gc_optimization_5_1.nettrace
├── gc_optimization_5_2.nettrace
└── gc_optimization_6.nettrace
```

CSV 원본과 기타 내부 분석 파일은 Repository에 포함하지 않고, **최종 결과 Excel과 최적화 단계별 Trace 파일만 Repository에 포함했습니다.**

---

# 결론

이번 성능 최적화에서는 **실제 게임 플레이 시나리오를 수행하는 다중 Client를 통해 지속적인 서버 부하를 발생시키고, 측정 결과를 기반으로 최적화 대상을 선정**했습니다.

`dotnet-counters`를 이용해 Runtime 지표를 수집하고, Allocation과 GC 증가가 확인된 이후 `dotnet-trace`와 PerfView를 이용하여 실제 allocation hotspot을 추적했습니다.

분석 결과 반복적으로 실행되는 패킷 직렬화, Enumerator, LINQ Iterator 등이 주요 allocation 대상으로 확인되었으며 다음과 같은 최적화를 단계적으로 적용했습니다.

1. `PacketWriter` 내부 버퍼 직접 기록
2. `Room.Sync()`의 `PacketWriter` 재사용
3. `FlyingBlockPosition` 및 `Player` Enumerator allocation 제거
4. `Player` Dictionary 직접 접근
5. `GameEndSystem` 및 게임 시스템의 LINQ 제거
6. `UpdateBlockSystem()`의 반복 LINQ 연산 제거

각 단계마다 500명 동시 접속 조건에서 동일한 테스트를 반복하여 최적화 효과를 검증했습니다.

그 결과 **Managed Allocation 56.9%, Gen1 GC 72.0%, GC Pause 70.3% 감소**를 확인하였으며, 최종 500명 동시 접속 환경에서 평균 GC Pause는 약 **936ms**로 측정되었습니다.
