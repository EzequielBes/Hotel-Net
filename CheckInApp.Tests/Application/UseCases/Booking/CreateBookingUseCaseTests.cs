using CheckInApp.Application.UseCases.Booking;
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Enums;
using CheckInApp.Domain.Ports;
using FluentAssertions;
using Moq;
using Xunit;

namespace CheckInApp.Tests.Application.UseCases.Booking;

public class CreateBookingUseCaseTests
{
    private readonly Mock<IBookingOrderRepository> _bookingOrderRepositoryMock;
    private readonly Mock<IRoomCategoryRepository> _roomCategoryRepositoryMock;
    private readonly Mock<IRatePlanRepository> _ratePlanRepositoryMock;
    private readonly Mock<IBookingMessagePublisher> _publisherMock;
    private readonly CreateBookingUseCase _sut;

    public CreateBookingUseCaseTests()
    {
        _bookingOrderRepositoryMock = new Mock<IBookingOrderRepository>();
        _roomCategoryRepositoryMock = new Mock<IRoomCategoryRepository>();
        _ratePlanRepositoryMock = new Mock<IRatePlanRepository>();
        _publisherMock = new Mock<IBookingMessagePublisher>();
        _sut = new CreateBookingUseCase(
            _bookingOrderRepositoryMock.Object,
            _roomCategoryRepositoryMock.Object,
            _ratePlanRepositoryMock.Object,
            _publisherMock.Object);
    }

    private static RoomCategory CreateCategory(int maxCapacity = 4, int minStayDays = 1, int maxStayDays = 14) =>
        new() { Id = 1, Name = "Standard", MaxCapacity = maxCapacity, MinStayDays = minStayDays, MaxStayDays = maxStayDays };

    private static RatePlan CreateRatePlan(decimal pricePerDay = 150m) =>
        new() { Id = 1, RoomCategoryId = 1, Name = "12h", HoursPackage = 12, PricePerDay = pricePerDay };

    private static CreateBookingCommand CreateValidCommand(string idempotencyKey = "key-1", int guestCount = 2, int stayDays = 3) =>
        new(
            IdempotencyKey: idempotencyKey,
            Cpf: "529.982.247-25",
            GuestName: "João Silva",
            GuestCount: guestCount,
            RoomCategoryId: 1,
            RatePlanId: 1,
            CheckInDate: new DateTime(2026, 8, 1),
            CheckOutDate: new DateTime(2026, 8, 1).AddDays(stayDays)
        );

    [Fact]
    public void Execute_ShouldCreatePendingOrder_AndPublishMessage_WhenNewRequest()
    {
        _roomCategoryRepositoryMock.Setup(r => r.GetById(1)).Returns(CreateCategory());
        _ratePlanRepositoryMock.Setup(r => r.GetById(1)).Returns(CreateRatePlan());
        _bookingOrderRepositoryMock.Setup(r => r.GetByIdempotencyKey("key-1")).Returns((BookingOrder?)null);
        _bookingOrderRepositoryMock
            .Setup(r => r.AddBookingOrder(It.IsAny<BookingOrder>()))
            .Returns<BookingOrder>(o => { o.Id = 42; return o; });

        var result = _sut.Execute(CreateValidCommand());

        result.Status.Should().Be(BookingStatus.Pending);
        result.Id.Should().Be(42);
        _publisherMock.Verify(p => p.PublishProcessBooking(42, 1), Times.Once);
    }

    [Fact]
    public void Execute_ShouldReturnExistingOrder_AndNotPublishAgain_WhenIdempotencyKeyAlreadyExists()
    {
        var existing = new BookingOrder { Id = 7, IdempotencyKey = "key-1", Status = BookingStatus.Confirmed };
        _bookingOrderRepositoryMock.Setup(r => r.GetByIdempotencyKey("key-1")).Returns(existing);

        var result = _sut.Execute(CreateValidCommand());

        result.Should().BeSameAs(existing);
        _bookingOrderRepositoryMock.Verify(r => r.AddBookingOrder(It.IsAny<BookingOrder>()), Times.Never);
        _publisherMock.Verify(p => p.PublishProcessBooking(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void Execute_ShouldThrowException_WhenGuestCountExceedsCategoryCapacity()
    {
        _roomCategoryRepositoryMock.Setup(r => r.GetById(1)).Returns(CreateCategory(maxCapacity: 2));
        _ratePlanRepositoryMock.Setup(r => r.GetById(1)).Returns(CreateRatePlan());
        _bookingOrderRepositoryMock.Setup(r => r.GetByIdempotencyKey(It.IsAny<string>())).Returns((BookingOrder?)null);

        var command = CreateValidCommand(guestCount: 3);

        Action act = () => _sut.Execute(command);

        act.Should().Throw<Exception>().WithMessage("*capacity*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(20)]
    public void Execute_ShouldThrowException_WhenStayDaysOutsideCategoryRange(int stayDays)
    {
        _roomCategoryRepositoryMock.Setup(r => r.GetById(1)).Returns(CreateCategory(minStayDays: 1, maxStayDays: 14));
        _ratePlanRepositoryMock.Setup(r => r.GetById(1)).Returns(CreateRatePlan());
        _bookingOrderRepositoryMock.Setup(r => r.GetByIdempotencyKey(It.IsAny<string>())).Returns((BookingOrder?)null);

        var command = CreateValidCommand(stayDays: stayDays);

        Action act = () => _sut.Execute(command);

        act.Should().Throw<Exception>().WithMessage("*stay*");
    }

    [Fact]
    public void Execute_ShouldCalculateTotalPrice_AsPricePerDayTimesStayDays()
    {
        _roomCategoryRepositoryMock.Setup(r => r.GetById(1)).Returns(CreateCategory());
        _ratePlanRepositoryMock.Setup(r => r.GetById(1)).Returns(CreateRatePlan(pricePerDay: 150m));
        _bookingOrderRepositoryMock.Setup(r => r.GetByIdempotencyKey(It.IsAny<string>())).Returns((BookingOrder?)null);
        _bookingOrderRepositoryMock
            .Setup(r => r.AddBookingOrder(It.IsAny<BookingOrder>()))
            .Returns<BookingOrder>(o => o);

        var result = _sut.Execute(CreateValidCommand(stayDays: 3));

        result.TotalPrice.Should().Be(450m);
    }
}
