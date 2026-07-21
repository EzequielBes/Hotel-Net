using CheckInApp.Domain.Enums;
using CheckInApp.Domain.ValueObjects;

namespace CheckInApp.Domain.Entities;

public class Usuario
{
    protected Usuario() { }

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string NumeroCelular { get; private set; } = string.Empty;
    public TipoUsuario Role { get; private set; } = TipoUsuario.Cliente;

    public Email Email { get; private set; } = null!;
    public Cpf Cpf { get; private set; } = null!;

    public string SenhaHash { get; private set; } = string.Empty;

 
    public static Usuario Criar(
        string nome,
        string email,
        string cpf,
        string senha,
        string numeroCelular,
        TipoUsuario role = TipoUsuario.Cliente)
    {
        return new Usuario
        {
            Nome = nome,
            Email = new Email(email),          // valida formato de e-mail
            Cpf = new Cpf(cpf),                // valida formato de CPF
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(new Password(senha).Value), // valida política e hasheia
            NumeroCelular = numeroCelular,
            Role = role
        };
    }


    public static Usuario Restaurar(
        int id,
        string nome,
        string email,
        string cpf,
        string senhaHash,
        string numeroCelular,
        TipoUsuario role)
    {
        return new Usuario
        {
            Id = id,
            Nome = nome,
            Email = new Email(email),
            Cpf = new Cpf(cpf),
            SenhaHash = senhaHash,   // já é hash — não passa pelo Password VO
            NumeroCelular = numeroCelular,
            Role = role
        };
    }

  
    public bool SenhaCorreta(string textoPuro) =>
        BCrypt.Net.BCrypt.Verify(textoPuro, SenhaHash);
}
