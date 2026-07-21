using CheckInApp.Domain.Entities;

namespace CheckInApp.Application.UseCases.Reservas;

public interface IListarReservasUseCase
{
    Reserva[] Executar();
    
}
