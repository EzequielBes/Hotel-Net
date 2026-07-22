using CheckInApp.Domain.Entities;

namespace CheckInApp.Domain.Ports;


public interface IUserRepository
{
    User? GetByEmail(string email);
    void Add(User user);
}
