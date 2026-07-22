namespace CheckInApp.Domain.Ports;

public interface IBookingMessagePublisher
{
    void PublishProcessBooking(int bookingOrderId, int roomCategoryId);
}
