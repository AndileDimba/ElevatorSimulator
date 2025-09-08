using ElevatorSim.Domain;
using ElevatorSim.Infrastructure.Elevators;
using Xunit;

namespace ElevatorSim.Tests;

public class MovementTests
{
    [Fact]
    public void Elevator_Moves_Toward_Target()
    {
        var e = new PassengerElevator("E1", startFloor: 0, capacity: 10, speedTicksPerFloor: 1);
        e.Assign(new Request(3, Direction.Up, 1));
        for (int i = 0; i < 3; i++) e.Tick();
        Assert.Equal(3, e.CurrentFloor);
    }

    [Fact]
    public void Elevator_Stops_And_Opens_Doors_At_Target()
    {
        var e = new PassengerElevator("E1", startFloor: 0, capacity: 10, speedTicksPerFloor: 1, doorOpenTicks: 1, doorCloseTicks: 1);
        e.Assign(new Request(1, Direction.Up, 1));
        e.Tick();
        Assert.Equal(1, e.CurrentFloor);
        e.Tick();
        Assert.NotEqual(Direction.Idle, e.Direction);
        Assert.True(e.State == ElevatorState.DoorsOpening || e.State == ElevatorState.DoorsOpen || e.State == ElevatorState.DoorsClosing);
    }
}