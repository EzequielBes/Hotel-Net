using CheckInApp.Domain.Entities;

namespace CheckInApp.Application.UseCases.Hospedagem;


public interface ICheckInUseCase
{
    Reserva Executar(FazerCheckInCommand command);
}
