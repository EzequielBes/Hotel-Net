using CheckInApp.Domain.Enums;

namespace CheckInApp.Application.UseCases.Identidade;

public record CadastrarUsuarioCommand(
    string Nome,
    string Email,
    string Cpf,
    string Senha,
    string NumeroCelular,
    TipoUsuario Role = TipoUsuario.Cliente
)
{
    public static CadastrarUsuarioCommand FromDto(CheckInApp.Application.DTOs.SignupDto dto) =>
        new(dto.Nome, dto.Email, dto.Cpf, dto.Senha, dto.NumeroCelular);
};

public interface ISignupUseCase
{
    void Executar(CadastrarUsuarioCommand command);
}
