using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Infrastructure.Persistence.Repositories;


public class RoomRepository : IRoomRepository
{
    private readonly AppDbContext _context;

    public RoomRepository(AppDbContext context)
    {
        _context = context;
    }

    public Room? GetRoomByNumber(int roomNumber)
    {
        return _context.Rooms.FirstOrDefault(r => r.Number == roomNumber);
    }

    public void UpdateRoom(Room room)
    {
        _context.Rooms.Update(room);
        _context.SaveChanges();
    }
}
