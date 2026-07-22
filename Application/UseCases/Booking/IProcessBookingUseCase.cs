namespace CheckInApp.Application.UseCases.Booking;

public interface IProcessBookingUseCase
{
    void Execute(int bookingOrderId);
}
