namespace CheckInApp.Domain.Entities;

public class Reservation
{
    public int Id { get; set; }
    public DateTime CheckIn { get; set; }
    public string Cpf { get; set; } = string.Empty;
    public DateTime? CheckOut { get; set; }
    public decimal BasePrice { get; set; }
    public decimal AdditionalFees { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    public string? GuestName { get; set; }
    public string[]? Companions { get; set; }
    public bool CheckOutComplete { get; set; }
    public int GuestCount { get; set; }
    public int RoomNumber { get; set; }
}
