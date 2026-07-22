using CheckInApp.Domain.Entities;

namespace CheckInApp.Domain.Ports;

public interface IRoomCategoryRepository
{
    RoomCategory[] ListCategoriesWithAvailability(DateTime checkIn, DateTime checkOut, int guestCount);
    RoomCategory? GetById(int id);
}
