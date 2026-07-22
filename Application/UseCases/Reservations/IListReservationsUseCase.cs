using CheckInApp.Domain.Entities;

namespace CheckInApp.Application.UseCases.Reservations;

public interface IListReservationsUseCase
{
    Reservation[] Execute();
}
