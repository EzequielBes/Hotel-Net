using CheckInApp.Domain.Enums;

namespace CheckInApp.Domain.Entities;

public class Quarto
{
    public int Id { get; set; }
    public int Numero { get; set; }
    public StatusQuarto Status { get; set; }
    public decimal ValorDiaria { get; set; }
    public int CapacidadeMaxima { get; set; }
    public Hospede? HospedeAtual { get; set; }
}
