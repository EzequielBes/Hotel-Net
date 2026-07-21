namespace CheckInApp.Application.UseCases.Hospedagem;


public record FazerCheckInCommand(
    string Cpf,
    string NomeHospede,
    int NumeroQuarto,
    DateTime Checkin,
    int QuantidadeHospede,
    string? Observacoes,
    string[]? Acompanhantes
);

public record FazerCheckOutCommand(
    string Cpf,
    int NumeroQuarto,
    decimal TaxasAdicionais
);
