using Microsoft.AspNetCore.Mvc;
using CheckInApp.Application.UseCases.Identity;
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
        _signupUseCase.Execute(RegisterUserCommand.FromDto(signupDto));
        return Ok(new { message = "User registered successfully" });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto loginDto)
    {
        var token = _loginUseCase.Execute(loginDto.Email, loginDto.Password);

        if (token != null)
            return Ok(new { token });

        return Unauthorized(new { message = "Invalid email or password" });
    }
}
