namespace CheckInApp.Application.DTOs;

public record CheckInRequestDto(
    string Cpf,
    string GuestName,
    int RoomNumber,
    DateTime CheckIn,
    int GuestCount,
    string? Notes,
    string[]? Companions
);

public record CheckOutRequestDto(
    string Cpf,
    int RoomNumber,
    decimal AdditionalFees,
    string? Notes
);

public record SignupDto(
    string Name,
    string Email,
    string Cpf,
    string Password,
    string PhoneNumber
);

public record LoginDto(
    string Email,
    string Password
);
