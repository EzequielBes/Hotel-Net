namespace CheckInApp.Application.UseCases.Identidade;


public interface ILoginUseCase
{
    string? Executar(string email, string senha);
}

public interface ITokenService
{
    string GenerateToken(string username, string role);
}
