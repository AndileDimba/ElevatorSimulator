using ElevatorSim.Application;
using ElevatorSim.Domain;
using ElevatorSim.Infrastructure.Dispatch;
using ElevatorSim.Infrastructure.Elevators;

namespace ElevatorSim.ConsoleApp;

public static class Program
{
    private static bool _autoTick = false;
    private static CancellationTokenSource? _autoTickCts;

    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var configure = new ConfigureBuildingUseCase();
        var strategy = new NearestAvailableDispatch();
        var elevators = new List<IElevator>
        {
            new PassengerElevator("E1", startFloor: 0),
            new PassengerElevator("E2", startFloor: 5)
        };

        var building = configure.CreateDefault(floors: 12, strategy, elevators);

        Console.WriteLine("TEAMX Elevator Challenge - Console (Day 2)");
        Console.WriteLine("Commands: status | call <floor> <up/down> <count> | tick | auto on|off | quit");

        while (true)
        {
            Console.Write("> ");
            var line = Console.ReadLine();
            if (line is null) break;

            var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0) continue;

            var cmd = parts[0].ToLowerInvariant();
            switch (cmd)
            {
                case "quit":
                case "exit":
                    StopAutoTick();
                    Console.WriteLine("Goodbye.");
                    return;

                case "status":
                    RenderStatus(building);
                    break;

                case "tick":
                    building.TickAll();
                    foreach (var ev in building.DrainEvents()) Console.WriteLine(ev);
                    Console.WriteLine("Tick.");
                    break;

                case "auto":
                    {
                        var arg = parts.Length >= 2 ? parts[1].ToLowerInvariant() : "";
                        if (arg == "on")
                            StartAutoTick(building);
                        else if (arg == "off")
                            StopAutoTick();
                        else
                            Console.WriteLine("Usage: auto on|off");
                        break;
                    }

                case "call":
                    if (parts.Length >= 4 &&
                        int.TryParse(parts[1], out int floor) &&
                        TryParseDirection(parts[2], out var dir) &&
                        int.TryParse(parts[3], out int count) &&
                        count > 0)
                    {
                        try
                        {
                            var req = new Request(floor, dir, count);
                            building.SubmitCall(req);
                            Console.WriteLine($"Registered call: floor {floor}, {dir}, {count} pax.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Usage: call <floor:int> <up|down> <count:int>");
                    }
                    break;

                default:
                    Console.WriteLine("Unknown command. Try: status | call <floor> <up/down> <count> | tick | auto on|off | quit");
                    break;
            }
        }
    }

    private static bool TryParseDirection(string s, out Direction dir)
    {
        switch (s.ToLowerInvariant())
        {
            case "up": dir = Direction.Up; return true;
            case "down": dir = Direction.Down; return true;
            default: dir = Direction.Idle; return false;
        }
    }

    private static void RenderStatus(Building b)
    {
        Console.WriteLine($"Floors: {b.Floors}");
        foreach (var e in b.Elevators)
        {
            Console.WriteLine($"{e.Id} | F:{e.CurrentFloor} | Dir:{e.Direction} | State:{e.State} | Moving:{e.IsMoving} | Pax:{e.PassengerCount}/{e.Capacity} | Targets:[{string.Join(",", e.Targets)}]");
        }
    }

    private static string GetState(IElevator e)
    {
        // IElevator doesn't expose State; cast if base type (safe in our setup)
        return e is ElevatorBase eb ? ebState(eb) : (e.IsMoving ? "Moving" : "Idle");
        static string ebState(ElevatorBase eb) => eb.GetType().GetProperty("State", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public) is { } ? "OK" : "N/A";
    }

    private static void StartAutoTick(Building building, int count = -1)
    {
        if (_autoTick) return;
        _autoTick = true;
        _autoTickCts = new CancellationTokenSource();
        var ct = _autoTickCts.Token;

        _ = Task.Run(async () =>
        {
            var tickCount = 0;
            while (!ct.IsCancellationRequested)
            {
                building.TickAll();

                // Print Day 3 events
                var events = building.DrainEvents();
                foreach (var ev in events) Console.WriteLine(ev);

                tickCount++;
                if (tickCount % 5 == 0) Console.WriteLine($"(auto) tick x{tickCount}");

                if (count > 0 && tickCount >= count)
                {
                    StopAutoTick();
                    break;
                }

                await Task.Delay(300, ct);
            }
        }, ct);

        Console.WriteLine(count > 0 ? $"Auto-tick: ON (300ms) for {count} ticks" : "Auto-tick: ON (300ms)");
    }

    private static void StopAutoTick()
    {
        if (!_autoTick) return;
        _autoTick = false;
        _autoTickCts?.Cancel();
        _autoTickCts?.Dispose();
        _autoTickCts = null;
        Console.WriteLine("Auto-tick: OFF");
    }
}