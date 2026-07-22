using CheckInApp.Domain.Entities;

namespace CheckInApp.Domain.Ports;


public interface IRoomRepository
{
    Room? GetRoomByNumber(int roomNumber);
    void UpdateRoom(Room room);
}
