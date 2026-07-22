using CheckInApp.Domain.Entities;

namespace CheckInApp.Domain.Ports;

public interface IWebhookSender
{
    void SendBookingResult(BookingOrder order);
}
