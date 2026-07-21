using Microsoft.AspNetCore.Mvc;
using CheckInApp.Application.UseCases.Identidade;
using CheckInApp.Application.DTOs;

namespace CheckInApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILoginUseCase _loginUseCase;
    private readonly ISignupUseCase _signupUseCase;

    public AuthController(ILoginUseCase loginUseCase, ISignupUseCase signupUseCase)
    {
        _loginUseCase = loginUseCase;
        _signupUseCase = signupUseCase;
    }

    [HttpPost("signup")]
    public IActionResult Signup([FromBody] SignupDto signupDto)
    {
        _signupUseCase.Executar(CadastrarUsuarioCommand.FromDto(signupDto));
        return Ok(new { message = "Usuário cadastrado com sucesso" });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto loginDto)
    {
        var token = _loginUseCase.Executar(loginDto.Email, loginDto.Password);

        if (token != null)
            return Ok(new { token });

        return Unauthorized(new { message = "Email ou senha inválidos" });
    }
}
