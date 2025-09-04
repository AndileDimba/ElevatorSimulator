using ElevatorSim.Domain;
using ElevatorSim.Infrastructure.Elevators;
using Xunit;

namespace ElevatorSim.Tests;

public class MovementTests
{
    [Fact]
    public void Elevator_Moves_Toward_Target()
    {
        var e = new PassengerElevator("E1", startFloor: 0, speedTicksPerFloor: 1);
        e.Assign(new Request(3, Direction.Up, 1));
        // One tick per floor; door timings cause a stop at arrival
        for (int i = 0; i < 3; i++) e.Tick();
        Assert.Equal(3, e.CurrentFloor);
    }

    [Fact]
    public void Elevator_Stops_And_Opens_Doors_At_Target()
    {
        var e = new PassengerElevator("E1", startFloor: 0, speedTicksPerFloor: 1, doorOpenTicks: 1, doorCloseTicks: 1);
        e.Assign(new Request(1, Direction.Up, 1));
        e.Tick(); // move to 1
        Assert.Equal(1, e.CurrentFloor);
        // Next ticks should be opening/open/closing cycles; ensure not moving
        e.Tick();
        Assert.NotEqual(Direction.Idle, e.Direction); // direction retained or chosen; moving state depends on IsMoving
        Assert.True(e.State == ElevatorState.DoorsOpening || e.State == ElevatorState.DoorsOpen || e.State == ElevatorState.DoorsClosing);
    }
}