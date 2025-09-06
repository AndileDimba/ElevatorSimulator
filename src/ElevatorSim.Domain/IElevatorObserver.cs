using ElevatorSim.Domain;

public interface IElevatorObserver
{
    void OnArrivedAtFloor(IElevator elevator, int floor);
    void OnDoorsOpened(IElevator elevator, int floor);
    void OnDoorsClosed(IElevator elevator, int floor);
}