using CheckInApp.Domain.Entities;

namespace CheckInApp.Application.UseCases.Hospitality;


public interface ICheckInUseCase
{
    Reservation Execute(CheckInCommand command);
}
