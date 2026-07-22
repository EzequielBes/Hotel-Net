using CheckInApp.Application.UseCases.Booking;
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Enums;
using CheckInApp.Domain.Ports;
using FluentAssertions;
using Moq;
using Xunit;

namespace CheckInApp.Tests.Application.UseCases.Booking;

public class ProcessBookingUseCaseTests
{
    private readonly Mock<IBookingOrderRepository> _bookingOrderRepositoryMock;
    private readonly Mock<IWebhookSender> _webhookSenderMock;
    private readonly ProcessBookingUseCase _sut;

    public ProcessBookingUseCaseTests()
    {
        _bookingOrderRepositoryMock = new Mock<IBookingOrderRepository>();
        _webhookSenderMock = new Mock<IWebhookSender>();
        _sut = new ProcessBookingUseCase(_bookingOrderRepositoryMock.Object, _webhookSenderMock.Object);
    }

    private static BookingOrder CreatePendingOrder() => new()
    {
        Id = 10,
        RoomCategoryId = 1,
        CheckInDate = new DateTime(2026, 8, 1),
        CheckOutDate = new DateTime(2026, 8, 3),
        Status = BookingStatus.Pending
    };

    [Fact]
    public void Execute_ShouldConfirmOrder_AndAssignRoom_WhenFreeRoomExists()
    {
        var order = CreatePendingOrder();
        var freeRoom = new Room { Id = 3, Number = 103, RoomCategoryId = 1 };
        _bookingOrderRepositoryMock.Setup(r => r.GetById(10)).Returns(order);
        _bookingOrderRepositoryMock
            .Setup(r => r.TryAssignRoom(order))
            .Returns<BookingOrder>(o =>
            {
                o.RoomId = freeRoom.Id;
                o.Status = BookingStatus.Confirmed;
                return freeRoom;
            });

        _sut.Execute(10);

        order.Status.Should().Be(BookingStatus.Confirmed);
        order.RoomId.Should().Be(3);
        _bookingOrderRepositoryMock.Verify(r => r.UpdateBookingOrder(It.IsAny<BookingOrder>()), Times.Never);
        _webhookSenderMock.Verify(w => w.SendBookingResult(order), Times.Once);
    }

    [Fact]
    public void Execute_ShouldRejectOrder_WhenNoFreeRoomExists()
    {
        var order = CreatePendingOrder();
        _bookingOrderRepositoryMock.Setup(r => r.GetById(10)).Returns(order);
        _bookingOrderRepositoryMock
            .Setup(r => r.TryAssignRoom(order))
            .Returns<BookingOrder>(o =>
            {
                o.Status = BookingStatus.Rejected;
                return null;
            });

        _sut.Execute(10);

        order.Status.Should().Be(BookingStatus.Rejected);
        order.RoomId.Should().BeNull();
        _bookingOrderRepositoryMock.Verify(r => r.UpdateBookingOrder(It.IsAny<BookingOrder>()), Times.Never);
        _webhookSenderMock.Verify(w => w.SendBookingResult(order), Times.Once);
    }

    [Fact]
    public void Execute_ShouldDoNothing_WhenOrderAlreadyProcessed()
    {
        var order = CreatePendingOrder();
        order.Status = BookingStatus.Confirmed;
        _bookingOrderRepositoryMock.Setup(r => r.GetById(10)).Returns(order);

        _sut.Execute(10);

        _bookingOrderRepositoryMock.Verify(r => r.TryAssignRoom(It.IsAny<BookingOrder>()), Times.Never);
        _bookingOrderRepositoryMock.Verify(r => r.UpdateBookingOrder(It.IsAny<BookingOrder>()), Times.Never);
        _webhookSenderMock.Verify(w => w.SendBookingResult(It.IsAny<BookingOrder>()), Times.Never);
    }
}
