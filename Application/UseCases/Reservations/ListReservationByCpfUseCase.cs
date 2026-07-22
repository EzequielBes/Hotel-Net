using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Application.UseCases.Reservations;

public class ListReservationByCpfUseCase : IListReservationByCpfUseCase
{
    private readonly IReservationRepository _reservationRepository;

    public ListReservationByCpfUseCase(IReservationRepository reservationRepository)
    {
        _reservationRepository = reservationRepository;
    }

    public Reservation Execute(string cpf)
    {
        return _reservationRepository.GetReservationByCpf(cpf);
    }
}
