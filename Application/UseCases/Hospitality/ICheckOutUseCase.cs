using CheckInApp.Domain.Entities;

namespace CheckInApp.Application.UseCases.Hospitality;

public interface ICheckOutUseCase
{
    Reservation Execute(CheckOutCommand command);
}
