using CheckInApp.Domain.Entities;

namespace CheckInApp.Application.UseCases.Booking;

public interface IGetBookingUseCase
{
    BookingOrder Execute(int bookingOrderId);
}
