using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Infrastructure.Persistence.Repositories;

public class RoomCategoryRepository : IRoomCategoryRepository
{
    private readonly AppDbContext _context;

    public RoomCategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public RoomCategory[] ListCategoriesWithAvailability(DateTime checkIn, DateTime checkOut, int guestCount)
    {
        return _context.RoomCategories
            .Where(c => c.MaxCapacity >= guestCount)
            .Where(c => _context.Rooms.Any(r =>
                r.RoomCategoryId == c.Id &&
                !_context.BookingOrders.Any(b =>
                    b.RoomId == r.Id &&
                    b.Status == Domain.Enums.BookingStatus.Confirmed &&
                    b.CheckInDate < checkOut &&
                    b.CheckOutDate > checkIn)))
            .ToArray();
    }

    public RoomCategory? GetById(int id)
    {
        return _context.RoomCategories.FirstOrDefault(c => c.Id == id);
    }
}
