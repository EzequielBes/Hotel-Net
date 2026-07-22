using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Application.UseCases.Reservations;

public class ListReservationsUseCase : IListReservationsUseCase
{
    private readonly IReservationRepository _reservationRepository;

    public ListReservationsUseCase(IReservationRepository reservationRepository)
    {
        _reservationRepository = reservationRepository;
    }

    public Reservation[] Execute()
    {
        return _reservationRepository.ListReservations();
    }
}
