using CheckInApp.Domain.Entities;

namespace CheckInApp.Application.UseCases.Hospedagem;

public interface ICheckOutUseCase
{
    Reserva Executar(FazerCheckOutCommand command);
}
