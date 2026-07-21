using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Infrastructure.Persistence.Repositories;


public class QuartoRepository : IQuartoRepository
{
    private readonly AppDbContext _context;

    public QuartoRepository(AppDbContext context)
    {
        _context = context;
    }

    public Quarto? ObterQuartoPorNumero(int numeroQuarto)
    {
        return _context.Quartos.FirstOrDefault(q => q.Numero == numeroQuarto);
    }

    public void AtualizarQuarto(Quarto quarto)
    {
        _context.Quartos.Update(quarto);
        _context.SaveChanges();
    }
}
