using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Application.UseCases.Reservas;

public class ListarReservaCpfUseCase : IListarReservaCpfUseCase
{
    private readonly IReservaRepository _reservaRepository;

    public ListarReservaCpfUseCase(IReservaRepository reservaRepository)
    {
        _reservaRepository = reservaRepository;
    }

    public Reserva Executar(string cpf)
    {
        return _reservaRepository.ObterReservaPorCpf(cpf);
    }
}
