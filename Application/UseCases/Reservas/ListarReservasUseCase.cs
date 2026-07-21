using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Application.UseCases.Reservas;

public class ListarReservasUseCase : IListarReservasUseCase
{
    private readonly IReservaRepository _reservaRepository;

    public ListarReservasUseCase(IReservaRepository reservaRepository)
    {
        _reservaRepository = reservaRepository;
    }

    public Reserva[] Executar()
    {
        return _reservaRepository.ListarReservas();
    }
}
