namespace CheckInApp.Application.UseCases.Identity;


public interface ILoginUseCase
{
    string? Execute(string email, string password);
}

public interface ITokenService
{
    string GenerateToken(string username, string role);
}
