using CheckInApp.Domain.Entities;

namespace CheckInApp.Application.UseCases.Booking;

public interface ICreateBookingUseCase
{
    BookingOrder Execute(CreateBookingCommand command);
}
