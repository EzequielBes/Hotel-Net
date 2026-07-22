using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Enums;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Application.UseCases.Booking;

public class CreateBookingUseCase : ICreateBookingUseCase
{
    private readonly IBookingOrderRepository _bookingOrderRepository;
    private readonly IRoomCategoryRepository _roomCategoryRepository;
    private readonly IRatePlanRepository _ratePlanRepository;
    private readonly IBookingMessagePublisher _publisher;

    public CreateBookingUseCase(
        IBookingOrderRepository bookingOrderRepository,
        IRoomCategoryRepository roomCategoryRepository,
        IRatePlanRepository ratePlanRepository,
        IBookingMessagePublisher publisher)
    {
        _bookingOrderRepository = bookingOrderRepository;
        _roomCategoryRepository = roomCategoryRepository;
        _ratePlanRepository = ratePlanRepository;
        _publisher = publisher;
    }

    public BookingOrder Execute(CreateBookingCommand command)
    {
        var existing = _bookingOrderRepository.GetByIdempotencyKey(command.IdempotencyKey);
        if (existing != null)
            return existing;

        var category = _roomCategoryRepository.GetById(command.RoomCategoryId);
        if (category == null)
            throw new Exception("Room category not found");

        var ratePlan = _ratePlanRepository.GetById(command.RatePlanId);
        if (ratePlan == null)
            throw new Exception("Rate plan not found");

        if (command.GuestCount > category.MaxCapacity)
            throw new Exception("Guest count exceeds category capacity");

        var stayDays = (command.CheckOutDate.Date - command.CheckInDate.Date).Days;
        if (stayDays < category.MinStayDays || stayDays > category.MaxStayDays)
            throw new Exception("Stay length outside category's allowed range");

        var order = new BookingOrder
        {
            IdempotencyKey = command.IdempotencyKey,
            Cpf = command.Cpf,
            GuestName = command.GuestName,
            GuestCount = command.GuestCount,
            RoomCategoryId = command.RoomCategoryId,
            RatePlanId = command.RatePlanId,
            CheckInDate = command.CheckInDate,
            CheckOutDate = command.CheckOutDate,
            Status = BookingStatus.Pending,
            TotalPrice = ratePlan.PricePerDay * stayDays
        };

        order = _bookingOrderRepository.AddBookingOrder(order);
        _publisher.PublishProcessBooking(order.Id);

        return order;
    }
}
