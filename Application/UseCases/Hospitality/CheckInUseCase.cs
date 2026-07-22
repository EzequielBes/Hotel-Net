using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Enums;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Application.UseCases.Hospitality;

public class CheckInUseCase : ICheckInUseCase
{
    private readonly IRoomRepository _roomRepository;
    private readonly IReservationRepository _reservationRepository;

    public CheckInUseCase(IRoomRepository roomRepository, IReservationRepository reservationRepository)
    {
        _roomRepository = roomRepository;
        _reservationRepository = reservationRepository;
    }

    public Reservation Execute(CheckInCommand command)
    {
        var room = _roomRepository.GetRoomByNumber(command.RoomNumber);

        if (room == null)
            throw new Exception("Room not found");

        if (room.Status != RoomStatus.Available)
            throw new Exception("Room is not available");

        if (room.MaxCapacity < command.GuestCount)
            throw new Exception("Room does not support the number of guests");

        var reservation = new Reservation
        {
            Cpf = command.Cpf,
            GuestName = command.GuestName,
            RoomNumber = command.RoomNumber,
            CheckIn = command.CheckIn,
            GuestCount = command.GuestCount,
            Notes = command.Notes,
            Companions = command.Companions,
            BasePrice = room.DailyRate,
            AdditionalFees = 0,
            TotalPrice = room.DailyRate,
            CheckOutComplete = false
        };

        _reservationRepository.AddReservation(reservation);

        room.Status = RoomStatus.Occupied;
        room.CurrentGuest = new Guest { Name = reservation.GuestName, Cpf = reservation.Cpf };
        _roomRepository.UpdateRoom(room);

        return reservation;
    }
}
