namespace ElevatorSim.Domain;

public sealed class Request
{
    public int Floor { get; }
    public Direction Direction { get; }
    public int Count { get; }

    public Request(int floor, Direction direction, int count)
    {
        if (!Enum.IsDefined(typeof(Direction), direction) || direction == Direction.Idle)
            throw new ArgumentException("Direction must be Up or Down.", nameof(direction));
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "People must be > 0.");

        Floor = floor;
        Direction = direction;
        Count = count;
    }

    public override string ToString() => $"Req(F:{Floor}, Dir:{Direction}, P:{Count})";
}