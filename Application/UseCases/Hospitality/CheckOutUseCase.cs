using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Enums;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Application.UseCases.Hospitality;

public class CheckOutUseCase : ICheckOutUseCase
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IRoomRepository _roomRepository;

    public CheckOutUseCase(IReservationRepository reservationRepository, IRoomRepository roomRepository)
    {
        _reservationRepository = reservationRepository;
        _roomRepository = roomRepository;
    }

    public Reservation Execute(CheckOutCommand command)
    {
        var reservation = _reservationRepository.GetReservationByCpf(command.Cpf);

        if (reservation == null)
            throw new Exception("Reservation not found");

        if (reservation.CheckOutComplete)
            throw new Exception("Reservation has already been finalized");

        reservation.AdditionalFees = command.AdditionalFees;
        reservation.TotalPrice = reservation.BasePrice + command.AdditionalFees;
        reservation.CheckOut = DateTime.Now;
        reservation.CheckOutComplete = true;

        _reservationRepository.UpdateReservation(reservation);

        var room = _roomRepository.GetRoomByNumber(command.RoomNumber);
        if (room != null)
        {
            room.Status = RoomStatus.Available;
            room.CurrentGuest = null;
            _roomRepository.UpdateRoom(room);
        }

        return reservation;
    }
}
