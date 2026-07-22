using CheckInApp.Domain.Enums;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Application.UseCases.Booking;

public class ProcessBookingUseCase : IProcessBookingUseCase
{
    private readonly IBookingOrderRepository _bookingOrderRepository;
    private readonly IWebhookSender _webhookSender;

    public ProcessBookingUseCase(IBookingOrderRepository bookingOrderRepository, IWebhookSender webhookSender)
    {
        _bookingOrderRepository = bookingOrderRepository;
        _webhookSender = webhookSender;
    }

    public void Execute(int bookingOrderId)
    {
        var order = _bookingOrderRepository.GetById(bookingOrderId);
        if (order == null || order.Status != BookingStatus.Pending)
            return;

        // TryAssignRoom performs the free-room lookup, the RoomId/Status mutation, and the
        // SaveChanges persistence all inside one Serializable transaction, so no separate
        // UpdateBookingOrder call is needed (or safe to make) after this.
        _bookingOrderRepository.TryAssignRoom(order);

        _webhookSender.SendBookingResult(order);
    }
}
