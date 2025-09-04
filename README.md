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

#### Current status (Day 2)
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

#### Next (Day 3)
- Capacity handling and passenger load/unload on door open
- Floor queues and re‑dispatch when elevator is full
- Improved dispatch tie‑breakers and starvation prevention
- Richer console feedback (loaded/left‑behind counts, ETAs)

#### Build/Run requirements
- Windows 11 (dev environment)
- .NET 8 SDK