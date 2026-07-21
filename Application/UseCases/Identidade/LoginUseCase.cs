using CheckInApp.Domain.Ports;

namespace CheckInApp.Application.UseCases.Identidade;

public class LoginUseCase : ILoginUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITokenService _tokenService;

    public LoginUseCase(IUsuarioRepository usuarioRepository, ITokenService tokenService)
    {
        _usuarioRepository = usuarioRepository;
        _tokenService = tokenService;
    }

    public string? Executar(string email, string senha)
    {
        var usuario = _usuarioRepository.ObterPorEmail(email);

        if (usuario == null)
            return null;

        if (!usuario.SenhaCorreta(senha))
            return null;

        return _tokenService.GenerateToken(usuario.Email.Value, usuario.Role.ToString());
    }
}
