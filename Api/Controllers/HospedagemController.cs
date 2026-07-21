using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CheckInApp.Application.UseCases.Hospedagem;
using CheckInApp.Application.UseCases.Reservas;
using CheckInApp.Application.DTOs;

namespace CheckInApp.Api.Controllers;


[Authorize(Roles = "Administrador, Funcionario")]
[ApiController]
[Route("api/[controller]")]
public class HospedagemController : ControllerBase
{
    private readonly ICheckInUseCase _checkInUseCase;
    private readonly ICheckOutUseCase _checkOutUseCase;
    private readonly IListarReservasUseCase _listarReservasUseCase;
    private readonly IListarReservaCpfUseCase _listarReservaCpfUseCase;

    public HospedagemController(
        ICheckInUseCase checkInUseCase,
        ICheckOutUseCase checkOutUseCase,
        IListarReservasUseCase listarReservasUseCase, IListarReservaCpfUseCase listarReservaCpfUseCase)
    {
        _checkInUseCase = checkInUseCase;
        _checkOutUseCase = checkOutUseCase;
        _listarReservasUseCase = listarReservasUseCase;
        _listarReservaCpfUseCase = listarReservaCpfUseCase;
    }

    [HttpPost("checkin")]
    public IActionResult FazerCheckin([FromBody] CheckInRequestDto request)
    {
        var command = new FazerCheckInCommand(
            Cpf: request.Cpf,
            NomeHospede: request.NomeHospede,
            NumeroQuarto: request.NumeroQuarto,
            Checkin: request.Checkin,
            QuantidadeHospede: request.QuantidadeHospede,
            Observacoes: request.Observacoes,
            Acompanhantes: request.Acompanhantes
        );

        var reserva = _checkInUseCase.Executar(command);
        return Ok(reserva);
    }

    [HttpPost("checkout")]
    public IActionResult FazerCheckout([FromBody] CheckOutRequestDto request)
    {
        var command = new FazerCheckOutCommand(
            Cpf: request.Cpf,
            NumeroQuarto: request.NumeroQuarto,
            TaxasAdicionais: request.TaxasAdicionais
        );

        var reserva = _checkOutUseCase.Executar(command);
        return Ok(reserva);
    }

    [HttpGet("reservas")]
    public IActionResult ListarReservas()
    {
        var reservas = _listarReservasUseCase.Executar();
        return Ok(reservas);
    }

    [HttpGet("reservas/{cpf}")]
    public IActionResult ListarReservasPorCpf(string cpf)
    {
        var reservas = _listarReservaCpfUseCase.Executar(cpf);
        return Ok(reservas);
    }
}
