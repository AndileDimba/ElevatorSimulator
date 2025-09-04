using ElevatorSim.Application;
using ElevatorSim.Domain;
using ElevatorSim.Infrastructure.Dispatch;
using ElevatorSim.Infrastructure.Elevators;

namespace ElevatorSim.ConsoleApp;

public static class Program
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var configure = new ConfigureBuildingUseCase();
        var strategy = new NearestAvailableDispatch();
        var elevators = new List<IElevator>
        {
            new StubElevator("E1", startFloor: 0),
            new StubElevator("E2", startFloor: 5)
        };

        var building = configure.CreateDefault(floors: 12, strategy, elevators);

        System.Console.WriteLine("TEAMX Elevator Challenge - Console (Day 1)");
        System.Console.WriteLine("Type 'status' or 'quit'.");

        while (true)
        {
            System.Console.Write("> ");
            var line = System.Console.ReadLine();
            if (line is null) break;

            switch (line.Trim().ToLowerInvariant())
            {
                case "quit":
                case "exit":
                    System.Console.WriteLine("Goodbye.");
                    return;

                case "status":
                    RenderStatus(building);
                    break;

                default:
                    System.Console.WriteLine("Unknown command. Try 'status' or 'quit'.");
                    break;
            }
        }
    }

    private static void RenderStatus(Building b)
    {
        System.Console.WriteLine($"Floors: {b.Floors}");
        foreach (var e in b.Elevators)
        {
            System.Console.WriteLine($"{e.Id} | F:{e.CurrentFloor} | Dir:{e.Direction} | Moving:{e.IsMoving} | Pax:{e.PassengerCount}/{e.Capacity} | Targets:[{string.Join(",", e.Targets)}]");
        }
    }
}