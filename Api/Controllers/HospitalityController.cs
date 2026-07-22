using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CheckInApp.Application.UseCases.Hospitality;
using CheckInApp.Application.UseCases.Reservations;
using CheckInApp.Application.DTOs;

namespace CheckInApp.Api.Controllers;


[Authorize(Roles = "Administrator, Employee")]
[ApiController]
[Route("api/[controller]")]
public class HospitalityController : ControllerBase
{
    private readonly ICheckInUseCase _checkInUseCase;
    private readonly ICheckOutUseCase _checkOutUseCase;
    private readonly IListReservationsUseCase _listReservationsUseCase;
    private readonly IListReservationByCpfUseCase _listReservationByCpfUseCase;

    public HospitalityController(
        ICheckInUseCase checkInUseCase,
        ICheckOutUseCase checkOutUseCase,
        IListReservationsUseCase listReservationsUseCase,
        IListReservationByCpfUseCase listReservationByCpfUseCase)
    {
        _checkInUseCase = checkInUseCase;
        _checkOutUseCase = checkOutUseCase;
        _listReservationsUseCase = listReservationsUseCase;
        _listReservationByCpfUseCase = listReservationByCpfUseCase;
    }

    [HttpPost("checkin")]
    public IActionResult CheckIn([FromBody] CheckInRequestDto request)
    {
        var command = new CheckInCommand(
            Cpf: request.Cpf,
            GuestName: request.GuestName,
            RoomNumber: request.RoomNumber,
            CheckIn: request.CheckIn,
            GuestCount: request.GuestCount,
            Notes: request.Notes,
            Companions: request.Companions
        );

        var reservation = _checkInUseCase.Execute(command);
        return Ok(reservation);
    }

    [HttpPost("checkout")]
    public IActionResult CheckOut([FromBody] CheckOutRequestDto request)
    {
        var command = new CheckOutCommand(
            Cpf: request.Cpf,
            RoomNumber: request.RoomNumber,
            AdditionalFees: request.AdditionalFees
        );

        var reservation = _checkOutUseCase.Execute(command);
        return Ok(reservation);
    }

    [HttpGet("reservations")]
    public IActionResult ListReservations()
    {
        var reservations = _listReservationsUseCase.Execute();
        return Ok(reservations);
    }

    [HttpGet("reservations/{cpf}")]
    public IActionResult ListReservationsByCpf(string cpf)
    {
        var reservation = _listReservationByCpfUseCase.Execute(cpf);
        return Ok(reservation);
    }
}
