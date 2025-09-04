using ElevatorSim.Domain;

namespace ElevatorSim.Application;

public sealed class ConfigureBuildingUseCase
{
    public Building CreateDefault(int floors, IDispatchStrategy strategy, IEnumerable<IElevator> elevators)
    {
        var building = new Building(floors, strategy);
        foreach (var e in elevators)
            building.AddElevator(e);
        return building;
    }
}

public interface ITimeProvider
{
    Task DelayAsync(TimeSpan delay, CancellationToken ct = default);
}

public sealed class RealTimeProvider : ITimeProvider
{
    public Task DelayAsync(TimeSpan delay, CancellationToken ct = default) => Task.Delay(delay, ct);
}