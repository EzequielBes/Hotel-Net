using CheckInApp.Domain.Ports;

namespace CheckInApp.Application.UseCases.Identity;

public class LoginUseCase : ILoginUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;

    public LoginUseCase(IUserRepository userRepository, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    public string? Execute(string email, string password)
    {
        var user = _userRepository.GetByEmail(email);

        if (user == null)
            return null;

        if (!user.IsPasswordCorrect(password))
            return null;

        return _tokenService.GenerateToken(user.Email.Value, user.Role.ToString());
    }
}
