namespace CheckInApp.Application.UseCases.Hospitality;


public record CheckInCommand(
    string Cpf,
    string GuestName,
    int RoomNumber,
    DateTime CheckIn,
    int GuestCount,
    string? Notes,
    string[]? Companions
);

public record CheckOutCommand(
    string Cpf,
    int RoomNumber,
    decimal AdditionalFees
);
