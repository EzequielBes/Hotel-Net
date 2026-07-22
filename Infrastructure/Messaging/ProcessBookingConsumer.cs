using CheckInApp.Application.UseCases.Booking;
using MassTransit;

namespace CheckInApp.Infrastructure.Messaging;

public class ProcessBookingConsumer : IConsumer<ProcessBookingMessage>
{
    private readonly IProcessBookingUseCase _processBookingUseCase;

    public ProcessBookingConsumer(IProcessBookingUseCase processBookingUseCase)
    {
        _processBookingUseCase = processBookingUseCase;
    }

    public Task Consume(ConsumeContext<ProcessBookingMessage> context)
    {
        _processBookingUseCase.Execute(context.Message.BookingOrderId);
        return Task.CompletedTask;
    }
}
