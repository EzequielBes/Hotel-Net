namespace CheckInApp.Domain.Entities;

public class Reserva
{
    public int Id { get; set; }
    public DateTime Checkin { get; set; }
    public string Cpf { get; set; } = string.Empty;
    public DateTime? Checkout { get; set; }
    public decimal ValorMinimo { get; set; }
    public decimal TaxasAdicionais { get; set; }
    public decimal ValorTotal { get; set; }
    public string? Observacoes { get; set; }
    public string? NomeHospede { get; set; }
    public string[]? Acompanhantes { get; set; }
    public bool CheckoutComplete { get; set; }
    public int QuantidadeHospede { get; set; }
    public int NumeroQuarto { get; set; }
}
