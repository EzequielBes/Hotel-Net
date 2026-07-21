using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Enums;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Application.UseCases.Hospedagem;

public class CheckInUseCase : ICheckInUseCase
{
    private readonly IQuartoRepository _quartoRepository;
    private readonly IReservaRepository _reservaRepository;

    public CheckInUseCase(IQuartoRepository quartoRepository, IReservaRepository reservaRepository)
    {
        _quartoRepository = quartoRepository;
        _reservaRepository = reservaRepository;
    }

    public Reserva Executar(FazerCheckInCommand command)
    {
        var quarto = _quartoRepository.ObterQuartoPorNumero(command.NumeroQuarto);

        if (quarto == null)
            throw new Exception("Quarto não encontrado");

        if (quarto.Status != StatusQuarto.Disponivel)
            throw new Exception("Quarto não está disponível");

        if (quarto.CapacidadeMaxima < command.QuantidadeHospede)
            throw new Exception("Quarto não suporta a quantidade de hóspedes");

        var reserva = new Reserva
        {
            Cpf = command.Cpf,
            NomeHospede = command.NomeHospede,
            NumeroQuarto = command.NumeroQuarto,
            Checkin = command.Checkin,
            QuantidadeHospede = command.QuantidadeHospede,
            Observacoes = command.Observacoes,
            Acompanhantes = command.Acompanhantes,
            ValorMinimo = quarto.ValorDiaria,
            TaxasAdicionais = 0,
            ValorTotal = quarto.ValorDiaria,
            CheckoutComplete = false
        };

        _reservaRepository.CadastrarReserva(reserva);

        quarto.Status = StatusQuarto.Ocupado;
        quarto.HospedeAtual = new Hospede { Nome = reserva.NomeHospede, Cpf = reserva.Cpf };
        _quartoRepository.AtualizarQuarto(quarto);

        return reserva;
    }
}
