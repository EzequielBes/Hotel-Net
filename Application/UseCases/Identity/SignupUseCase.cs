using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Application.UseCases.Identity;

public class SignupUseCase : ISignupUseCase
{
    private readonly IUserRepository _userRepository;

    public SignupUseCase(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public void Execute(RegisterUserCommand command)
    {
        var existing = _userRepository.GetByEmail(command.Email);
        if (existing != null)
            throw new InvalidOperationException("A user with this e-mail already exists.");

        var user = User.Create(
            name: command.Name,
            email: command.Email,
            cpf: command.Cpf,
            password: command.Password,
            phoneNumber: command.PhoneNumber,
            role: command.Role
        );

        _userRepository.Add(user);
    }
}