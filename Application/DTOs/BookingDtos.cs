namespace CheckInApp.Application.DTOs;

public record RatePlanDto(
    int RatePlanId,
    string Name,
    int HoursPackage,
    decimal PricePerDay
);

public record AvailableRoomCategoryDto(
    int RoomCategoryId,
    string Name,
    int MaxCapacity,
    int MinStayDays,
    int MaxStayDays,
    RatePlanDto[] RatePlans
);

public record CreateBookingRequestDto(
    string Cpf,
    string GuestName,
    int GuestCount,
    int RoomCategoryId,
    int RatePlanId,
    DateTime CheckInDate,
    DateTime CheckOutDate
);
