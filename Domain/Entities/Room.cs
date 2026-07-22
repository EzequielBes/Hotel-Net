using CheckInApp.Domain.Enums;

namespace CheckInApp.Domain.Entities;

public class Room
{
    public int Id { get; set; }
    public int Number { get; set; }
    public RoomStatus Status { get; set; }
    public decimal DailyRate { get; set; }
    public int MaxCapacity { get; set; }
    public Guest? CurrentGuest { get; set; }
}
