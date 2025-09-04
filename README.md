### TEAMX Elevator Challenge (C#/.NET 8)

Clean Architecture scaffold with Domain/Application/Infrastructure/Console layers.

How to run:
- dotnet build
- dotnet test
- dotnet run --project src/ElevatorSim.Console

Current status (Day 1):
- Project structure ready
- Domain enums/interfaces and Building shell in place
- Stub dispatch and elevator to compile and run
- Console app shows status

Next:
- Implement ElevatorBase state machine (movement/doors)
- Dispatch scoring (nearest available with direction bias)
- Console commands: call <floor> <up/down> <count>, tick loop
