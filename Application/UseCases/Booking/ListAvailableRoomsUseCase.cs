using CheckInApp.Application.DTOs;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Application.UseCases.Booking;

public class ListAvailableRoomsUseCase : IListAvailableRoomsUseCase
{
    private readonly IRoomCategoryRepository _roomCategoryRepository;
    private readonly IRatePlanRepository _ratePlanRepository;

    public ListAvailableRoomsUseCase(IRoomCategoryRepository roomCategoryRepository, IRatePlanRepository ratePlanRepository)
    {
        _roomCategoryRepository = roomCategoryRepository;
        _ratePlanRepository = ratePlanRepository;
    }

    public AvailableRoomCategoryDto[] Execute(ListAvailableRoomsQuery query)
    {
        var categories = _roomCategoryRepository.ListCategoriesWithAvailability(query.CheckIn, query.CheckOut, query.GuestCount);

        return categories.Select(category =>
        {
            var ratePlans = _ratePlanRepository.ListByCategory(category.Id)
                .Select(p => new RatePlanDto(p.Id, p.Name, p.HoursPackage, p.PricePerDay))
                .ToArray();

            return new AvailableRoomCategoryDto(
                category.Id,
                category.Name,
                category.MaxCapacity,
                category.MinStayDays,
                category.MaxStayDays,
                ratePlans);
        }).ToArray();
    }
}
