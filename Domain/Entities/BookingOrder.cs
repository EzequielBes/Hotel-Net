using CheckInApp.Domain.Enums;

namespace CheckInApp.Domain.Entities;

public class BookingOrder
{
    public int Id { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public int GuestCount { get; set; }
    public int RoomCategoryId { get; set; }
    public int RatePlanId { get; set; }
    public int? RoomId { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
