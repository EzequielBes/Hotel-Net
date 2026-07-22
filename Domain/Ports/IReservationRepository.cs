using CheckInApp.Domain.Entities;

namespace CheckInApp.Domain.Ports;


public interface IReservationRepository
{
    Reservation? AddReservation(Reservation reservation);
    Reservation? GetReservationByCpf(string cpf);
    Reservation[] ListReservations();
    void UpdateReservation(Reservation reservation);
}
