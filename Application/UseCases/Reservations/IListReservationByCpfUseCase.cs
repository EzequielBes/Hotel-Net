using CheckInApp.Domain.Entities;

namespace CheckInApp.Application.UseCases.Reservations;

public interface IListReservationByCpfUseCase
{
    Reservation Execute(string cpf);
}
