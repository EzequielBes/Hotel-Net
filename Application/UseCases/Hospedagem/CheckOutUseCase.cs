using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Enums;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Application.UseCases.Hospedagem;

public class CheckOutUseCase : ICheckOutUseCase
{
    private readonly IReservaRepository _reservaRepository;
    private readonly IQuartoRepository _quartoRepository;

    public CheckOutUseCase(IReservaRepository reservaRepository, IQuartoRepository quartoRepository)
    {
        _reservaRepository = reservaRepository;
        _quartoRepository = quartoRepository;
    }

    public Reserva Executar(FazerCheckOutCommand command)
    {
        var reserva = _reservaRepository.ObterReservaPorCpf(command.Cpf);

        if (reserva == null)
            throw new Exception("Reserva não encontrada");

        if (reserva.CheckoutComplete)
            throw new Exception("Reserva já foi finalizada");

        reserva.TaxasAdicionais = command.TaxasAdicionais;
        reserva.ValorTotal = reserva.ValorMinimo + command.TaxasAdicionais;
        reserva.Checkout = DateTime.Now;
        reserva.CheckoutComplete = true;

        _reservaRepository.AtualizarReserva(reserva);

        var quarto = _quartoRepository.ObterQuartoPorNumero(command.NumeroQuarto);
        if (quarto != null)
        {
            quarto.Status = StatusQuarto.Disponivel;
            quarto.HospedeAtual = null;
            _quartoRepository.AtualizarQuarto(quarto);
        }

        return reserva;
    }
}
