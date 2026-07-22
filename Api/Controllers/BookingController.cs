using Microsoft.AspNetCore.Mvc;
using CheckInApp.Application.UseCases.Booking;
using CheckInApp.Application.DTOs;

namespace CheckInApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingController : ControllerBase
{
    private readonly IListAvailableRoomsUseCase _listAvailableRoomsUseCase;
    private readonly ICreateBookingUseCase _createBookingUseCase;
    private readonly IGetBookingUseCase _getBookingUseCase;

    public BookingController(
        IListAvailableRoomsUseCase listAvailableRoomsUseCase,
        ICreateBookingUseCase createBookingUseCase,
        IGetBookingUseCase getBookingUseCase)
    {
        _listAvailableRoomsUseCase = listAvailableRoomsUseCase;
        _createBookingUseCase = createBookingUseCase;
        _getBookingUseCase = getBookingUseCase;
    }

    [HttpGet("rooms/available")]
    public IActionResult ListAvailableRooms([FromQuery] DateTime checkIn, [FromQuery] DateTime checkOut, [FromQuery] int guestCount)
    {
        var query = new ListAvailableRoomsQuery(checkIn, checkOut, guestCount);
        var result = _listAvailableRoomsUseCase.Execute(query);
        return Ok(result);
    }

    [HttpPost("bookings")]
    public IActionResult CreateBooking([FromHeader(Name = "Idempotency-Key")] string idempotencyKey, [FromBody] CreateBookingRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest("Idempotency-Key header is required");

        var command = new CreateBookingCommand(
            IdempotencyKey: idempotencyKey,
            Cpf: request.Cpf,
            GuestName: request.GuestName,
            GuestCount: request.GuestCount,
            RoomCategoryId: request.RoomCategoryId,
            RatePlanId: request.RatePlanId,
            CheckInDate: request.CheckInDate,
            CheckOutDate: request.CheckOutDate
        );

        var order = _createBookingUseCase.Execute(command);
        return AcceptedAtAction(nameof(GetBooking), new { id = order.Id }, order);
    }

    [HttpGet("bookings/{id}")]
    public IActionResult GetBooking(int id)
    {
        var order = _getBookingUseCase.Execute(id);
        return Ok(order);
    }
}
