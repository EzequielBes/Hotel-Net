using CheckInApp.Domain.Enums;

namespace CheckInApp.Application.UseCases.Identity;

public record RegisterUserCommand(
    string Name,
    string Email,
    string Cpf,
    string Password,
    string PhoneNumber,
    UserRole Role = UserRole.Client
)
{
    public static RegisterUserCommand FromDto(CheckInApp.Application.DTOs.SignupDto dto) =>
        new(dto.Name, dto.Email, dto.Cpf, dto.Password, dto.PhoneNumber);
};

public interface ISignupUseCase
{
    void Execute(RegisterUserCommand command);
}
