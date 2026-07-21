using CheckInApp.Domain.Entities;

namespace CheckInApp.Domain.Ports;


public interface IReservaRepository
{
    Reserva? CadastrarReserva(Reserva reserva);
    Reserva? ObterReservaPorCpf(string cpf);
    Reserva[] ListarReservas();
    void AtualizarReserva(Reserva reserva);
}
