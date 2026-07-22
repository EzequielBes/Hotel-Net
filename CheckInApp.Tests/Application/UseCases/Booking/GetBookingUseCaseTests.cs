using CheckInApp.Application.UseCases.Booking;
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;
using FluentAssertions;
using Moq;
using Xunit;

namespace CheckInApp.Tests.Application.UseCases.Booking;

public class GetBookingUseCaseTests
{
    private readonly Mock<IBookingOrderRepository> _bookingOrderRepositoryMock;
    private readonly GetBookingUseCase _sut;

    public GetBookingUseCaseTests()
    {
        _bookingOrderRepositoryMock = new Mock<IBookingOrderRepository>();
        _sut = new GetBookingUseCase(_bookingOrderRepositoryMock.Object);
    }

    [Fact]
    public void Execute_ShouldReturnOrder_WhenFound()
    {
        var order = new BookingOrder { Id = 5 };
        _bookingOrderRepositoryMock.Setup(r => r.GetById(5)).Returns(order);

        var result = _sut.Execute(5);

        result.Should().BeSameAs(order);
    }

    [Fact]
    public void Execute_ShouldThrowException_WhenNotFound()
    {
        _bookingOrderRepositoryMock.Setup(r => r.GetById(999)).Returns((BookingOrder?)null);

        Action act = () => _sut.Execute(999);

        act.Should().Throw<Exception>().WithMessage("*not found*");
    }
}
