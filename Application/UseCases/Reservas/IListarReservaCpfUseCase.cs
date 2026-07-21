using CheckInApp.Domain.Entities;

namespace CheckInApp.Application.UseCases.Reservas;

public interface IListarReservaCpfUseCase
{
    Reserva Executar(string cpf);
    
}
