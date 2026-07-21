using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Infrastructure.Persistence.Repositories;

public class ReservaRepository : IReservaRepository
{
    private readonly AppDbContext _context;

    public ReservaRepository(AppDbContext context)
    {
        _context = context;
    }

    public Reserva? CadastrarReserva(Reserva reserva)
    {
        _context.Reservas.Add(reserva);
        _context.SaveChanges();
        return reserva;
    }

    public Reserva? ObterReservaPorCpf(string cpf)
    {
        return _context.Reservas.FirstOrDefault(r => r.Cpf == cpf);
    }

    public Reserva[] ListarReservas()
    {
        return _context.Reservas.ToArray();
    }

    public void AtualizarReserva(Reserva reserva)
    {
        _context.Reservas.Update(reserva);
        _context.SaveChanges();
    }
}
