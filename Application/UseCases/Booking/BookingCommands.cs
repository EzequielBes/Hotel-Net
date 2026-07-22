namespace CheckInApp.Application.UseCases.Booking;

public record CreateBookingCommand(
    string IdempotencyKey,
    string Cpf,
    string GuestName,
    int GuestCount,
    int RoomCategoryId,
    int RatePlanId,
    DateTime CheckInDate,
    DateTime CheckOutDate
);
