using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Application.UseCases.Booking;

public class GetBookingUseCase : IGetBookingUseCase
{
    private readonly IBookingOrderRepository _bookingOrderRepository;

    public GetBookingUseCase(IBookingOrderRepository bookingOrderRepository)
    {
        _bookingOrderRepository = bookingOrderRepository;
    }

    public BookingOrder Execute(int bookingOrderId)
    {
        var order = _bookingOrderRepository.GetById(bookingOrderId);
        if (order == null)
            throw new Exception("Booking order not found");

        return order;
    }
}
