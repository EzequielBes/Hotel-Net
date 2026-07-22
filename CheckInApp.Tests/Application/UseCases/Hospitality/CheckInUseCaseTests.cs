using CheckInApp.Application.UseCases.Hospitality;
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Enums;
using CheckInApp.Domain.Ports;
using FluentAssertions;
using Moq;

namespace CheckInApp.Tests.Application.UseCases.Hospitality;

public class CheckInUseCaseTests
{
    private readonly Mock<IRoomRepository> _roomRepositoryMock;
    private readonly Mock<IReservationRepository> _reservationRepositoryMock;
    private readonly CheckInUseCase _sut;

    public CheckInUseCaseTests()
    {
        _roomRepositoryMock = new Mock<IRoomRepository>();
        _reservationRepositoryMock = new Mock<IReservationRepository>();
        _sut = new CheckInUseCase(_roomRepositoryMock.Object, _reservationRepositoryMock.Object);
    }

    // Helper: creates a default available room for tests
    private static Room CreateAvailableRoom(int number = 101, int capacity = 2, decimal dailyRate = 150m) =>
        new()
        {
            Id = 1,
            Number = number,
            Status = RoomStatus.Available,
            MaxCapacity = capacity,
            DailyRate = dailyRate
        };

    private static CheckInCommand CreateValidCommand(int roomNumber = 101, int guestCount = 1) =>
        new(
            Cpf: "529.982.247-25",
            GuestName: "João Silva",
            RoomNumber: roomNumber,
            CheckIn: DateTime.Now,
            GuestCount: guestCount,
            Notes: null,
            Companions: null
        );

    // -------------------------------------------------------
    // Scenario: successful check-in
    // -------------------------------------------------------

    [Fact]
    public void Execute_ShouldCreateReservation_WhenRoomIsAvailable()
    {
        // Arrange
        var room = CreateAvailableRoom();

        _roomRepositoryMock
            .Setup(r => r.GetRoomByNumber(101))
            .Returns(room);

        _reservationRepositoryMock
            .Setup(r => r.AddReservation(It.IsAny<Reservation>()))
            .Returns((Reservation r) => r);

        // Act
        var reservation = _sut.Execute(CreateValidCommand());

        // Assert
        reservation.Should().NotBeNull();
        reservation.Cpf.Should().Be("529.982.247-25");
        reservation.RoomNumber.Should().Be(101);
        reservation.CheckOutComplete.Should().BeFalse();
        reservation.BasePrice.Should().Be(150m);
    }

    [Fact]
    public void Execute_ShouldMarkRoomAsOccupied_AfterCheckIn()
    {
        // Arrange
        var room = CreateAvailableRoom();

        _roomRepositoryMock
            .Setup(r => r.GetRoomByNumber(101))
            .Returns(room);

        _reservationRepositoryMock
            .Setup(r => r.AddReservation(It.IsAny<Reservation>()))
            .Returns((Reservation r) => r);

        // Act
        _sut.Execute(CreateValidCommand());

        // Assert
        room.Status.Should().Be(RoomStatus.Occupied);
        _roomRepositoryMock.Verify(r => r.UpdateRoom(room), Times.Once);
    }

    // -------------------------------------------------------
    // Scenario: room not found
    // -------------------------------------------------------

    [Fact]
    public void Execute_ShouldThrowException_WhenRoomNotFound()
    {
        // Arrange
        _roomRepositoryMock
            .Setup(r => r.GetRoomByNumber(It.IsAny<int>()))
            .Returns((Room?)null);

        var action = () => _sut.Execute(CreateValidCommand());

        action.Should().Throw<Exception>()
            .WithMessage("*not found*");
    }

    // -------------------------------------------------------
    // Scenario: room not available
    // -------------------------------------------------------

    [Fact]
    public void Execute_ShouldThrowException_WhenRoomIsOccupied()
    {
        // Arrange — room already occupied
        var room = CreateAvailableRoom();
        room.Status = RoomStatus.Occupied;

        _roomRepositoryMock
            .Setup(r => r.GetRoomByNumber(101))
            .Returns(room);

        var action = () => _sut.Execute(CreateValidCommand());

        action.Should().Throw<Exception>()
            .WithMessage("*available*");
    }

    // -------------------------------------------------------
    // Scenario: capacity exceeded
    // -------------------------------------------------------

    [Fact]
    public void Execute_ShouldThrowException_WhenGuestCountExceedsCapacity()
    {
        // Arrange — room with capacity 2, trying 5 guests
        var room = CreateAvailableRoom(capacity: 2);

        _roomRepositoryMock
            .Setup(r => r.GetRoomByNumber(101))
            .Returns(room);

        var action = () => _sut.Execute(CreateValidCommand(guestCount: 5));

        action.Should().Throw<Exception>()
            .WithMessage("*guests*");
    }

    [Fact]
    public void Execute_ShouldNotThrow_WhenGuestCountEqualsCapacity()
    {
        // Arrange — room with capacity 2, exactly 2 guests (exact limit)
        var room = CreateAvailableRoom(capacity: 2);

        _roomRepositoryMock
            .Setup(r => r.GetRoomByNumber(101))
            .Returns(room);

        _reservationRepositoryMock
            .Setup(r => r.AddReservation(It.IsAny<Reservation>()))
            .Returns((Reservation r) => r);

        var action = () => _sut.Execute(CreateValidCommand(guestCount: 2));

        action.Should().NotThrow();
    }
}
