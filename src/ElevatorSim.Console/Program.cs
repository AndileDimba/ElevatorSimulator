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
            new PassengerElevator("E2", startFloor: 5),
            new HighSpeedElevator("E2", startFloor: 5),
            new FreightElevator("F1"),
        };

        var building = configure.CreateDefault(floors: 12, strategy, elevators);
        building.AddElevator(new PassengerElevator("E1"));
        building.AddElevator(new HighSpeedElevator("E2", startFloor: 5));
        building.AddElevator(new FreightElevator("F1"));

        Console.WriteLine("TEAMX Elevator Challenge - Console (Day 5)");
        Console.WriteLine("Commands: status | status waiting | call <floor> <up/down> <count> | press <elevatorId> <floor> | tick | auto on|off | quit");

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
                    {
                        if (parts.Length > 1 && parts[1].Equals("waiting", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine("Waiting (non-zero only):");
                            for (int f = 0; f < building.Floors; f++)
                            {
                                var up = building.GetWaitingCount(f, Direction.Up);
                                var down = building.GetWaitingCount(f, Direction.Down);
                                if (up > 0 || down > 0)
                                    Console.WriteLine($"F:{f} | Up:{up} Down:{down}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Floors: {building.Floors}");
                            foreach (var e in building.Elevators
                                                      .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                                                      .Select(g => g.First())
                                                      .OrderBy(x => x.Id))
                            {
                                Console.WriteLine($"{e.Id} | F:{e.CurrentFloor} | Dir:{e.Direction} | State:{e.State} | Moving:{e.IsMoving} | Pax:{e.PassengerCount}/{e.Capacity}");
                            }
                        }
                        break;
                    }

                case "help":
                    {
                        Console.WriteLine("Commands:");
                        Console.WriteLine("  status");
                        Console.WriteLine("  status waiting");
                        Console.WriteLine("  call <floor> <up/down> <count>");
                        Console.WriteLine("  press <elevatorId> <floor>");
                        Console.WriteLine("  tick [N]");
                        Console.WriteLine("  auto on|off");
                        Console.WriteLine("  oos <elevatorId> <on|off>");
                        Console.WriteLine("  events [N]");
                        Console.WriteLine("  quit");
                        break;
                    }

                case "tick":
                    {
                        int repeat = 1;
                        if (parts.Length >= 2 && int.TryParse(parts[1], out var n) && n > 0)
                            repeat = n;

                        for (int i = 0; i < repeat; i++)
                            building.TickAll();

                        Console.WriteLine($"(manual) tick x{repeat}");
                        break;
                    }

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

                case "press":
                    {
                        if (parts.Length < 3) { Console.WriteLine("Usage: press <elevatorId> <floor>"); break; }
                        var id = parts[1];
                        if (!int.TryParse(parts[2], out var f)) { Console.WriteLine("Invalid floor"); break; }
                        var elev = building.Elevators.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
                        if (elev == null) { Console.WriteLine($"No elevator '{id}'"); break; }
                        elev.PressButton(f);
                        Console.WriteLine($"Pressed {f} in {id}");
                        break;
                    }

                case "call":
                    {
                        try
                        {
                            // call <floor:int> <up|down> <count:int>
                            if (parts.Length < 4)
                            {
                                Console.WriteLine("Usage: call <floor:int> <up|down> <count:int>");
                                break;
                            }

                            var floor = int.Parse(parts[1]);

                            if (!TryParseDirection(parts[2], out var dir))
                            {
                                Console.WriteLine("Direction must be 'up' or 'down'.");
                                break;
                            }

                            var count = int.Parse(parts[3]);

                            var req = new Request(floor, dir, count);
                            building.SubmitCall(req);
                            Console.WriteLine($"Registered call: floor {floor}, {dir}, {count} pax.");
                        }
                        catch (FormatException)
                        {
                            Console.WriteLine("Usage: call <floor:int> <up|down> <count:int>");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                        }
                        break;
                    }

                case "events":
                    {
                        int take = 20;
                        if (parts.Length >= 2 && int.TryParse(parts[1], out var n) && n > 0)
                            take = n;

                        foreach (var ev in building.GetRecentEvents(take))
                            Console.WriteLine(ev);
                        break;
                    }

                case "oos":
                    {
                        if (parts.Length < 3)
                        {
                            Console.WriteLine("Usage: oos <elevatorId> <on|off>");
                            break;
                        }
                        var id = parts[1];
                        var onOff = parts[2].Equals("on", StringComparison.OrdinalIgnoreCase);

                        var ok = building.SetOutOfService(id, onOff);
                        if (!ok) Console.WriteLine($"No elevator with Id '{id}'");
                        else Console.WriteLine($"Out-of-service {(onOff ? "enabled" : "disabled")} for {id}");
                        break;
                    }

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