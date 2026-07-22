using CheckInApp.Domain.Enums;
using CheckInApp.Domain.ValueObjects;

namespace CheckInApp.Domain.Entities;

public class User
{
    protected User() { }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public UserRole Role { get; private set; } = UserRole.Client;

    public Email Email { get; private set; } = null!;
    public Cpf Cpf { get; private set; } = null!;

    public string PasswordHash { get; private set; } = string.Empty;


    public static User Create(
        string name,
        string email,
        string cpf,
        string password,
        string phoneNumber,
        UserRole role = UserRole.Client)
    {
        return new User
        {
            Name = name,
            Email = new Email(email),
            Cpf = new Cpf(cpf),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(new Password(password).Value),
            PhoneNumber = phoneNumber,
            Role = role
        };
    }


    public static User Restore(
        int id,
        string name,
        string email,
        string cpf,
        string passwordHash,
        string phoneNumber,
        UserRole role)
    {
        return new User
        {
            Id = id,
            Name = name,
            Email = new Email(email),
            Cpf = new Cpf(cpf),
            PasswordHash = passwordHash,
            PhoneNumber = phoneNumber,
            Role = role
        };
    }


    public bool IsPasswordCorrect(string plainText) =>
        BCrypt.Net.BCrypt.Verify(plainText, PasswordHash);
}
