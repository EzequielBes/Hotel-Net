using CheckInApp.Domain.Entities;

namespace CheckInApp.Domain.Ports;


public interface IQuartoRepository
{
    Quarto? ObterQuartoPorNumero(int numeroQuarto);
    void AtualizarQuarto(Quarto quarto);
}
