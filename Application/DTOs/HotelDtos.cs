namespace CheckInApp.Application.DTOs;

public record CheckInRequestDto(
    string Cpf,
    string NomeHospede,
    int NumeroQuarto,
    DateTime Checkin,
    int QuantidadeHospede,
    string? Observacoes,
    string[]? Acompanhantes
);

public record CheckOutRequestDto(
    string Cpf,
    int NumeroQuarto,
    decimal TaxasAdicionais,
    string? Observacoes
);

public record SignupDto(
    string Nome,
    string Email,
    string Cpf,
    string Senha,
    string NumeroCelular
);

public record LoginDto(
    string Email,
    string Password
);
