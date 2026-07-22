using CheckInApp.Application.UseCases.Booking;
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;
using FluentAssertions;
using Moq;
using Xunit;

namespace CheckInApp.Tests.Application.UseCases.Booking;

public class ListAvailableRoomsUseCaseTests
{
    private readonly Mock<IRoomCategoryRepository> _roomCategoryRepositoryMock;
    private readonly Mock<IRatePlanRepository> _ratePlanRepositoryMock;
    private readonly ListAvailableRoomsUseCase _sut;

    public ListAvailableRoomsUseCaseTests()
    {
        _roomCategoryRepositoryMock = new Mock<IRoomCategoryRepository>();
        _ratePlanRepositoryMock = new Mock<IRatePlanRepository>();
        _sut = new ListAvailableRoomsUseCase(_roomCategoryRepositoryMock.Object, _ratePlanRepositoryMock.Object);
    }

    [Fact]
    public void Execute_ShouldReturnCategoriesWithTheirRatePlans()
    {
        var checkIn = new DateTime(2026, 8, 1);
        var checkOut = new DateTime(2026, 8, 3);
        var category = new RoomCategory { Id = 1, Name = "Standard", MaxCapacity = 4, MinStayDays = 1, MaxStayDays = 14 };
        _roomCategoryRepositoryMock
            .Setup(r => r.ListCategoriesWithAvailability(checkIn, checkOut, 2))
            .Returns(new[] { category });
        _ratePlanRepositoryMock
            .Setup(r => r.ListByCategory(1))
            .Returns(new[] { new RatePlan { Id = 1, RoomCategoryId = 1, Name = "12h", HoursPackage = 12, PricePerDay = 150m } });

        var result = _sut.Execute(new ListAvailableRoomsQuery(checkIn, checkOut, 2));

        result.Should().HaveCount(1);
        result[0].RoomCategoryId.Should().Be(1);
        result[0].Name.Should().Be("Standard");
        result[0].RatePlans.Should().ContainSingle(p => p.Name == "12h" && p.PricePerDay == 150m);
    }
}
