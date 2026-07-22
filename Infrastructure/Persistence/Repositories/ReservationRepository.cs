using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Infrastructure.Persistence.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly AppDbContext _context;

    public ReservationRepository(AppDbContext context)
    {
        _context = context;
    }

    public Reservation? AddReservation(Reservation reservation)
    {
        _context.Reservations.Add(reservation);
        _context.SaveChanges();
        return reservation;
    }

    public Reservation? GetReservationByCpf(string cpf)
    {
        return _context.Reservations.FirstOrDefault(r => r.Cpf == cpf);
    }

    public Reservation[] ListReservations()
    {
        return _context.Reservations.ToArray();
    }

    public void UpdateReservation(Reservation reservation)
    {
        _context.Reservations.Update(reservation);
        _context.SaveChanges();
    }
}
