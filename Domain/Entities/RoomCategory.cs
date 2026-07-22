namespace CheckInApp.Domain.Entities;

public class RoomCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MaxCapacity { get; set; }
    public int MinStayDays { get; set; }
    public int MaxStayDays { get; set; }
}
