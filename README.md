### TEAMX Elevator Challenge (C# / .NET 8)

Clean Architecture solution with Domain / Application / Infrastructure / Console layers.  
Day 2 implements a tick‑based elevator movement state machine, initial dispatch strategy, and console commands.

#### Project structure
- src/
  - ElevatorSim.Domain
  - ElevatorSim.Application
  - ElevatorSim.Infrastructure
    - Dispatch (`NearestAvailableDispatch`)
    - Elevators (`ElevatorBase`, `PassengerElevator`)
  - ElevatorSim.Console
- tests/
  - ElevatorSim.Tests

#### How to run
- `dotnet build`
- `dotnet test`
- `dotnet run --project src/ElevatorSim.Console`

#### Console commands
- `status`  
  Show current state of each elevator.
- `call <floor> <up|down> <count>`  
  Submit a floor call with direction and passenger count.
- `tick`  
  Advance the simulation by one tick.
- `auto on`  
  Start continuous ticks (300 ms interval).
- `auto on <count>`  
  Run exactly N ticks, then stop.
- `auto off`  
  Stop auto ticking.
- `quit` / `exit`  
  Exit the console app.

#### Day 2
- Domain
  - Enums: `Direction`, `ElevatorState`, `ElevatorType`
  - Interfaces: `IElevator` (now includes `State`), `IDispatchStrategy`
  - `Building` with request submission and ticking
- Infrastructure
  - `ElevatorBase`: tick‑based movement and door cycle  
    `DoorsOpening → DoorsOpen → DoorsClosing → DoorsClosed`
  - `PassengerElevator` derived from `ElevatorBase`
  - `NearestAvailableDispatch`: distance + direction alignment + simple load penalty
- Console
  - Commands for calls, manual tick, auto tick, and status rendering
  - Status shows: Floor, Direction, State, Moving, Pax load, Targets
- Tests
  - Basic movement and arrival/door transition tests
- CI
  - Build before test to avoid “test dll not found”

#### Example session
- status
  - E1 | F:0 | Dir:Idle | State:DoorsClosed | Moving:False | Pax:0/10 | Targets:[]
  - E2 | F:5 | Dir:Idle | State:DoorsClosed | Moving:False | Pax:0/10 | Targets:[]

- call 7 up 3
  - Registered call: floor 7, Up, 3 pax.

- auto on 20
  - (auto) tick x5
  - (auto) tick x10
  - (auto) tick x15
  - (auto) tick x20

- status
  - E2 | F:7 | Dir:Idle | State:DoorsClosed | Moving:False | Pax:0/10 | Targets:[]

#### Architecture notes
- Clean Architecture: dependencies point inward
  - Domain: business rules, no framework deps
  - Application: use‑cases orchestrating domain
  - Infrastructure: implementations (e.g., dispatch, elevators, persistence if added)
  - Presentation: console UI
- SOLID
  - Strategy pattern for dispatch
  - LSP via elevator types (`PassengerElevator`, future: `HighSpeed`, `Freight`, `Glass`)
  - DIP via `IElevator` and `IDispatchStrategy` abstractions

#### Day 3
- Domain
  - `IElevator` extended:
    - `Passengers` (read-only)
    - `AvailableCapacity`
    - `UnloadAtCurrentFloor()`, `BoardPassengers(IEnumerable<Passenger>)`
    - `IElevatorObserver? Observer`
  - `Building` implements `IElevatorObserver`
    - Per-floor `FloorQueue` (Up/Down)
    - On doors open: unload first, then board up to capacity
    - After ticks: re-dispatch floors that still have waiting passengers
  - `Request` supports passenger count (`People` with `Count` alias)
- Infrastructure
  - `ElevatorBase`
    - Internal passenger list and capacity calculation
    - `AddTarget(int)` with direction-aware routing
    - Stabilized door cycle (correct tick budgeting; no re-entry loop)
    - Fires observer events: Arrived, DoorsOpened, DoorsClosed
  - `PassengerElevator` inherits new behavior
- Console
  - Same commands as Day 2: `status`, `call <floor> <up|down> <count>`, `tick`, `auto on|off`, `quit`
  - Functional flow: submit calls; on doors open, building unloads/loads; overflow remains queued
- Tests
  - Initial smoke tests for boarding/unloading and capacity (to expand later)
- CI
  - No changes required from Day 2

#### Example sessions
- Basic boarding
  - Input
    - `call 0 up 6`
    - `tick`
    - `tick`
    - `status`
  - Output (example)
    - `Arrived: E1 at F:0`
    - `Stop F:0 | Unloaded:0 Boarded:6 RemainingUp:0 RemainingDown:0`
    - `E1 | F:0 | Dir:Idle | State:DoorsOpen | Pax:6/10 | Targets:[3]`

- Capacity edge case
  - Input
    - `call 0 up 20`
    - `auto on`
    - …let it run…
    - `auto off`
    - `status`
  - Output (example excerpts)
    - `Arrived: E1 at F:0`
    - `Stop F:0 | Unloaded:0 Boarded:10 RemainingUp:10 RemainingDown:0`
    - `Arrived: E1 at F:3`
    - `Stop F:3 | Unloaded:10 Boarded:0 RemainingUp:10 RemainingDown:0`
    - `E1 | F:3 | Dir:Idle | State:DoorsClosed | Pax:0/10 | Targets:[]`

#### Architecture notes (Day 3 deltas)
- Observer pattern: elevators raise events; `Building` performs unload/load and manages queues
- Clean Architecture preserved: Domain remains independent of Infrastructure (interaction via `IElevator`)
- Floor queues enable realistic batching and overflow handling; remaining passengers are re-dispatched

#### Known limitations / next steps
- Destination modeling: current heuristic; consider randomized or user-entered destinations
- Dispatch: explore load-aware scoring and starvation prevention
- Console: optionally show per-floor waiting counts in `status`
- Tests: broaden for multi-elevator routing and queue re-dispatch timing

#### Build/Run
- `dotnet build`
- `dotnet run --project src/ElevatorSim.Console`
- Try:
  - `call 0 up 6` → `tick` → `tick` → `status`
  - `call 0 up 20` → `auto on` → `auto off` → `status`

### Day 4: Inside-Car Buttons and Routing Discipline

Implemented core Day 4 features focusing on handling inside-elevator button presses (AddTarget) and a basic routing discipline to serve targets efficiently in the current direction. Passengers' destinations are now added as targets upon boarding, ensuring stops for unloading. Used SortedSet for up/down targets to serve nearest in direction (ascending for up). Tested capacity, unloading, and idle behavior with a scenario involving 10 passengers and multiple presses.

#### Key Changes
- Added `AddTarget(int floor)` to IElevator and ElevatorBase for button presses, classifying as up/down based on current floor.
- In Building's `OnDoorsOpened`, added loop to call `elevator.AddTarget(passenger.DestinationFloor)` for each boarded passenger.
- Updated `UnloadAtCurrentFloor` to remove passengers where `DestinationFloor == CurrentFloor` from the underlying `_passengers` list.
- Switched targets to SortedSet (ascending for up, descending for down) for optimized serving order without skips.
- Passenger creation in `SubmitCall` fixed destinations to 3 for test consistency (can be randomized later).
- Resolved constructor overloads in PassengerElevator and interface signatures for compilation/tests.

#### Test Run (Successful Scenario)
Ran the following commands to verify boarding, targeted movement, unloading at destinations, and idling:

> status
- Floors: 12
  - E1 | F:0 | Dir:Idle | State:DoorsClosed | Moving:False | Pax:0/10 | Targets:[]
  - E2 | F:5 | Dir:Idle | State:DoorsClosed | Moving:False | Pax:0/10 | Targets:[]

> call 0 up 10
- Registered call: floor 0, Up, 10 pax.

> press E1 6
- Pressed 6 in E1

> press E1 8
- Pressed 8 in E1

> press E1 4
- Pressed 4 in E1

> auto on
Auto-tick: ON (300ms)

> Arrived: E1 at F:0
- Stop F:0 | Unloaded:0 Boarded:10 RemainingUp:0 RemainingDown:0
- (auto) tick x5
- (auto) tick x10
- (auto) tick x15
- (auto) tick x20
- Arrived: E1 at F:3
- Stop F:3 | Unloaded:10 Boarded:0 RemainingUp:0 RemainingDown:0
- (auto) tick x25
- (auto) tick x30
- Arrived: E1 at F:4
- Stop F:4 | Unloaded:0 Boarded:0 RemainingUp:0 RemainingDown:0
- (auto) tick x35
- (auto) tick x40
- (auto) tick x45
- Arrived: E1 at F:6
- Stop F:6 | Unloaded:0 Boarded:0 RemainingUp:0 RemainingDown:0
- (auto) tick x50
- (auto) tick x55
- (auto) tick x60
- Arrived: E1 at F:8
- (auto) tick x65
- Stop F:8 | Unloaded:0 Boarded:0 RemainingUp:0 RemainingDown:0
- (auto) tick x70
- (auto) tick x75
- (auto) x80
> auto  off
- Auto-tick: OFF

> status
- Floors: 12
  - E1 | F:8 | Dir:Idle | State:DoorsClosed | Moving:False | Pax:0/10 | Targets:[]
  - E2 | F:5 | Dir:Idle | State:DoorsClosed | Moving:False | Pax:0/10 | Targets:[]


### Day 5 — Elevator types, dispatch polish, capacity and queue visibility

#### What’s new
- Added HighSpeedElevator and FreightElevator (inherits PassengerElevator; currently a tagged type).
- Dispatch uses ChooseElevator(Building, Request) with partial assignment to avoid all‑or‑nothing behavior.
- Re‑enabled TryDispatchQueues on each tick so waiting calls are actively assigned.
- Capacity‑aware boarding on door open; overflow remains queued.
- Console: added status waiting to show per‑floor waiting counts.
- Tests: expose SpeedTicksPerFloor and verify dispatch preference for the high‑speed car.

#### Commands
- status
- status waiting
- call <floor> <up|down> <count>
- press <elevatorId> <floor>
- tick
- auto on|off
- quit

#### Manual verification
- Dispatch preference
  1) status
  2) call 10 down 1
  3) auto on → expect E2 to be dispatched and arrive at F:10
  4) auto off

- Inside‑car buttons
  1) call 0 up 10 → auto on until boarding/unload → auto off
  2) press E1 4; press E1 6; press E1 8
  3) auto on → expect stops in order 4 → 6 → 8 → auto off

- Capacity and overflow
  1) call 0 up 12
  2) auto on → expect “Stop F:0 | … Boarded:10 RemainingUp:2 …”
  3) auto off
  4) status waiting → should show F:0 | Up:2 (if you don’t stop auto immediately, another idle car may pick up the remaining 2 right away)

- Validation
  - call 3 sideways 2 → “Direction must be 'up' or 'down'.”

#### Notes
- FreightElevator currently behaves like PassengerElevator (type tag only); can be specialized later.
- status output is simplified for portability; use status waiting to inspect floor queues.

### Day 6 — Operations polish and console UX

#### What’s new
- Out-of-service toggle per elevator; dispatch skips OOS units.
- tick N helper to step the simulation quickly.
- events [N] command to view recent events.
- help command summarizing available commands.

#### New/updated commands
- help
- oos <elevatorId> <on|off>
- tick [N]
- events [N]
- status
- status waiting
- call <floor> <up|down> <count>
- press <elevatorId> <floor>
- auto on|off
- quit

#### Verify quickly
- oos E2 on → call 10 down 1 → E2 is skipped by dispatch.
- tick 25 → advances 25 ticks and prints a summary.
- events 10 → shows arrivals, stops, and OOS toggles.

### Day 7 — Wait-time metrics

#### What’s new
- Wait-time metrics tracked from call to boarding.
- `metrics` console command to display served count, average wait, and max wait (ticks).

#### Commands
- metrics

#### Manual verification
- `call 0 up 12`
- `auto on` (let the first car board 10)
- `auto off`
- `metrics` → should show `Served: 10` and non-zero avg/max wait
- Turn auto back on to board the remaining passengers; `metrics` will update (e.g., `Served: 12`)

#### Build/Run requirements
- Windows 11 (dev environment)
- .NET 8 SDK