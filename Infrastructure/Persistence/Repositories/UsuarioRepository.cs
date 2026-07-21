using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;
using CheckInApp.Domain.ValueObjects;

namespace CheckInApp.Infrastructure.Persistence.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _context;

    public UsuarioRepository(AppDbContext context)
    {
        _context = context;
    }

    public Usuario? ObterPorEmail(string email)
    {
        var emailVo = new Email(email);
        return _context.Usuarios.FirstOrDefault(u => u.Email == emailVo);
    }

    public void Adicionar(Usuario usuario)
    {
        _context.Usuarios.Add(usuario);
        _context.SaveChanges();
    }
}
