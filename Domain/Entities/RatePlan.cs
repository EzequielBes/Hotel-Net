namespace CheckInApp.Domain.Entities;

public class RatePlan
{
    public int Id { get; set; }
    public int RoomCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int HoursPackage { get; set; }
    public decimal PricePerDay { get; set; }
}
