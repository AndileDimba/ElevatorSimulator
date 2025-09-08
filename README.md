### TEAMX Elevator Challenge (C# / .NET 8)

Clean Architecture simulation of a multi‑elevator building with real‑time console controls. By Day 7 the system supports mixed elevator types, capacity‑aware boarding, efficient dispatch, per‑floor waiting visibility, out‑of‑service control, events logging, and wait‑time metrics.

#### Highlights
- Real‑time status and interactive console commands
- Multiple elevators and floors with clean separation of concerns
- Efficient dispatch (nearest available with direction/load awareness, partial assignment)
- Capacity handling with overflow queues
- Inside‑car buttons and routing discipline
- Wait‑time metrics (served, average wait, max wait)
- Operations controls: out‑of‑service, tick N, events

---

### Quick start

- Build and test
  - `dotnet build`
  - `dotnet test`
- Run console
  - `dotnet run --project src/ElevatorSim.Console`

#### Commands quick reference
- `help`
- `status`
- `status waiting`
- `call <floor> <up|down> <count>`
- `press <elevatorId> <floor>`
- `tick [N]`
- `auto on|off`
- `oos <elevatorId> <on|off>`
- `events [N]`
- `metrics`
- `quit`

---

### Features (mapping to challenge requirements)

1) Real‑time elevator status
- `status` shows: Id, floor, direction, door state, moving, load
- `status waiting` shows per‑floor Up/Down waiting counts

2) Interactive control
- `call <floor> <up|down> <count>` queues passengers
- `press <elevatorId> <floor>` simulates inside‑car button presses

3) Multiple floors and elevators
- Building supports many cars and floors (demo defaults to 12 floors)

4) Efficient dispatch
- Strategy: “NearestAvailableDispatch”
  - Scores by distance, direction alignment, and load
  - Partial assignment: assigns even if only some passengers can be served now

5) Passenger limits
- Capacity enforced at boarding on door open
- Overflow remains queued and is re‑dispatched

6) Different elevator types (extensibility)
- PassengerElevator base; HighSpeedElevator and FreightElevator derive from it (behavior can be specialized)

7) Real‑time operation
- Tick‑based movement and door cycle:
  - DoorsOpening → DoorsOpen → DoorsClosing → DoorsClosed
- Manual `tick` or `auto on` for continuous ticks

Operations and observability
- Out‑of‑service per elevator: `oos <id> on|off` (dispatch skips OOS cars)
- Events: `events [N]` shows recent stops, arrivals, toggles
- Metrics: `metrics` prints served count, average and max wait (ticks)

Validation and safety
- Input parsing validates direction/floors; errors are surfaced clearly
- Movement clamped within building bounds

---

### Demo scenarios with expected outcomes

Basic dispatch and boarding
- Commands:
  - `call 0 up 6`
  - `auto on` (a few ticks) → `auto off`
  - `status`
- Expected excerpts:
  - `Stop F:0 | Unloaded:0 Boarded:6 RemainingUp:0 RemainingDown:0`
  - Elevator shows Pax: 6 and a destination target

Capacity and overflow
- Commands:
  - `call 0 up 12`
  - `auto on` → observe stop at F:0 → `auto off`
  - `status waiting`
- Expected excerpts:
  - `Stop F:0 | ... Boarded:10 RemainingUp:2 ...`
  - `status waiting` prints `F:0 | Up:2` if you stop auto immediately
  - Note: if auto remains on, another idle car may pick up the remaining 2 shortly thereafter

Inside‑car buttons
- Commands:
  - `call 0 up 10` → `auto on` until boarded → `auto off`
  - `press E1 4` → `press E1 6` → `press E1 8`
  - `auto on` → expect stops at 4 → 6 → 8 → `auto off`

Out‑of‑service
- Commands:
  - `oos E2 on`
  - `call 10 down 1` → `auto on` → observe E2 skipped by dispatch → `auto off`
  - `oos E2 off`

Metrics
- Commands:
  - `call 0 up 12` → `auto on` to board 10 → `auto off`
  - `metrics`
- Expected:
  - `Served: 10 | Avg wait ticks: <non-zero> | Max wait ticks: <non-zero>`
  - Turn auto back on to board remaining; metrics update to Served: 12

---

### Architecture

Clean Architecture (dependencies point inward)

- Domain (core rules, no external deps)
  - Entities and value types: Passenger, Request
  - Abstractions: IElevator, IDispatchStrategy
  - Enums: Direction, ElevatorState, ElevatorType
  - Building: manages queues, tick orchestration, boarding/unloading

- Infrastructure (implementations)
  - Elevators: ElevatorBase, PassengerElevator (+ HighSpeed, Freight)
  - Dispatch strategies: NearestAvailableDispatch

- Presentation (console)
  - Command parser and rendering
  - Manual/auto ticking and operations commands

- Tests
  - Unit tests validating dispatch, capacity, boarding, metrics, and guardrails

Design decisions
- Event‑driven stop handling: Building invokes unload → load on door‑open transition
- Direction‑aware targets via sorted sets for predictable routing
- Partial assignment in dispatch to avoid “all or nothing”
- Real queues are the source of truth (no shadow counters)
- Metrics computed from per‑passenger enqueue stamps and boarding time

---

### Project 

- src/
- ElevatorSim.Domain/
- ElevatorSim.Application/
- ElevatorSim.Infrastructure/
- Dispatch/NearestAvailableDispatch.cs
- Elevators/
- ElevatorBase.cs
- PassengerElevator.cs
- HighSpeedElevator.cs
- FreightElevator.cs
- ElevatorSim.Console/
- tests/
- ElevatorSim.Tests/
- .github/workflows/
- dotnet.yml

---

### Testing and CI

- Tests
  - `dotnet test` runs core scenarios (capacity, queues, dispatch preference, metrics)
- CI
  - GitHub Actions builds and runs tests on push and PR
  - See workflow in `.github/workflows`

---

### Known behaviors and limitations

- If auto remains on, leftover passengers may be picked up by another idle car shortly after a large boarding.
- Freight and HighSpeed elevators currently share base behaviors; specialization can be added.
- Metrics are tick‑based and not persisted across runs.

Future enhancements
- Starvation prevention and prioritization windowing
- Idle parking strategy (e.g., lobby or mid‑building)
- Scenario scripting and reproducible seeding for tests/demos
- Visualization or web UI

---

### Milestones (development history)

- Day 2 — Core movement state machine, initial dispatch, console I/O
- Day 3 — Floor queues, unload‑then‑load, capacity handling, event wiring
- Day 4 — Inside‑car buttons; direction‑aware target routing
- Day 5 — Elevator types; dispatch polish; per‑floor waiting visibility
- Day 6 — Ops polish: out‑of‑service, tick N, events, help
- Day 7 — Wait‑time metrics and metrics command