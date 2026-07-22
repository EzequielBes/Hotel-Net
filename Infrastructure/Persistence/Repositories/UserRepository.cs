using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;
using CheckInApp.Domain.ValueObjects;

namespace CheckInApp.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public User? GetByEmail(string email)
    {
        var emailVo = new Email(email);
        return _context.Users.FirstOrDefault(u => u.Email == emailVo);
    }

    public void Add(User user)
    {
        _context.Users.Add(user);
        _context.SaveChanges();
    }
}
