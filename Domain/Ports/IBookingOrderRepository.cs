using CheckInApp.Domain.Entities;

namespace CheckInApp.Domain.Ports;

public interface IBookingOrderRepository
{
    BookingOrder? GetByIdempotencyKey(string idempotencyKey);
    BookingOrder AddBookingOrder(BookingOrder order);
    BookingOrder? GetById(int id);
    void UpdateBookingOrder(BookingOrder order);
    Room? TryAssignRoom(BookingOrder order);
}
