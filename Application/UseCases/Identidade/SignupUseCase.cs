using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Application.UseCases.Identidade;

public class SignupUseCase : ISignupUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;

    public SignupUseCase(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public void Executar(CadastrarUsuarioCommand command)
    {
        var existente = _usuarioRepository.ObterPorEmail(command.Email);
        if (existente != null)
            throw new InvalidOperationException("Já existe um usuário com este e-mail.");
        var usuario = Usuario.Criar(
            nome: command.Nome,
            email: command.Email,
            cpf: command.Cpf,
            senha: command.Senha,
            numeroCelular: command.NumeroCelular,
            role: command.Role
        );

        _usuarioRepository.Adicionar(usuario);
    }
}