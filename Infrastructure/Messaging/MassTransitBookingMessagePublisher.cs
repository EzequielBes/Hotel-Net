using CheckInApp.Application.UseCases.Booking;
using CheckInApp.Domain.Ports;
using MassTransit;

namespace CheckInApp.Infrastructure.Messaging;

public class MassTransitBookingMessagePublisher : IBookingMessagePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitBookingMessagePublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public void PublishProcessBooking(int bookingOrderId)
    {
        _publishEndpoint.Publish(new ProcessBookingMessage(bookingOrderId)).GetAwaiter().GetResult();
    }
}
