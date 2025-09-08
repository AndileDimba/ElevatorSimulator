using System.Collections.Generic;

namespace ElevatorSim.Domain;

public sealed class FloorQueue
{
    public int Floor { get; }
    public Queue<Passenger> Up { get; } = new();
    public Queue<Passenger> Down { get; } = new();

    public FloorQueue(int floor) => Floor = floor;

    public int Count(Direction dir) => dir == Direction.Up ? Up.Count : Down.Count;

    public void Enqueue(Direction dir, Passenger p)
    {
        if (dir == Direction.Up) Up.Enqueue(p);
        else Down.Enqueue(p);
    }

    public Passenger? TryDequeue(Direction dir)
    {
        if (dir == Direction.Up && Up.Count > 0) return Up.Dequeue();
        if (dir == Direction.Down && Down.Count > 0) return Down.Dequeue();
        return null;
    }
}