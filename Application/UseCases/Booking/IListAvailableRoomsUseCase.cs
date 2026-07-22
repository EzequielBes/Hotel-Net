using CheckInApp.Application.DTOs;

namespace CheckInApp.Application.UseCases.Booking;

public record ListAvailableRoomsQuery(DateTime CheckIn, DateTime CheckOut, int GuestCount);

public interface IListAvailableRoomsUseCase
{
    AvailableRoomCategoryDto[] Execute(ListAvailableRoomsQuery query);
}
