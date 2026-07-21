using CheckInApp.Application.UseCases.Hospedagem;
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Enums;
using CheckInApp.Domain.Ports;
using FluentAssertions;
using Moq;

namespace CheckInApp.Tests.Application.UseCases.Hospedagem;

public class CheckInUseCaseTests
{
    private readonly Mock<IQuartoRepository> _quartoRepositorioMock;
    private readonly Mock<IReservaRepository> _reservaRepositorioMock;
    private readonly CheckInUseCase _sut;

    public CheckInUseCaseTests()
    {
        _quartoRepositorioMock = new Mock<IQuartoRepository>();
        _reservaRepositorioMock = new Mock<IReservaRepository>();
        _sut = new CheckInUseCase(_quartoRepositorioMock.Object, _reservaRepositorioMock.Object);
    }

    // Helper: cria um quarto disponível padrão para os testes
    private static Quarto CriarQuartoDisponivel(int numero = 101, int capacidade = 2, decimal diaria = 150m) =>
        new()
        {
            Id = 1,
            Numero = numero,
            Status = StatusQuarto.Disponivel,
            CapacidadeMaxima = capacidade,
            ValorDiaria = diaria
        };

    private static FazerCheckInCommand CriarCommandValido(int numeroQuarto = 101, int qtdHospedes = 1) =>
        new(
            Cpf: "529.982.247-25",
            NomeHospede: "João Silva",
            NumeroQuarto: numeroQuarto,
            Checkin: DateTime.Now,
            QuantidadeHospede: qtdHospedes,
            Observacoes: null,
            Acompanhantes: null
        );

    // -------------------------------------------------------
    // Cenário: check-in bem-sucedido
    // -------------------------------------------------------

    [Fact]
    public void Executar_DeveCriarReserva_QuandoQuartoDisponivel()
    {
        // Arrange
        var quarto = CriarQuartoDisponivel();

        _quartoRepositorioMock
            .Setup(r => r.ObterQuartoPorNumero(101))
            .Returns(quarto);

        _reservaRepositorioMock
            .Setup(r => r.CadastrarReserva(It.IsAny<Reserva>()))
            .Returns((Reserva r) => r);

        // Act
        var reserva = _sut.Executar(CriarCommandValido());

        // Assert
        reserva.Should().NotBeNull();
        reserva.Cpf.Should().Be("529.982.247-25");
        reserva.NumeroQuarto.Should().Be(101);
        reserva.CheckoutComplete.Should().BeFalse();
        reserva.ValorMinimo.Should().Be(150m);
    }

    [Fact]
    public void Executar_DeveDefinirQuartoComoOcupado_AposCheckIn()
    {
        // Arrange
        var quarto = CriarQuartoDisponivel();

        _quartoRepositorioMock
            .Setup(r => r.ObterQuartoPorNumero(101))
            .Returns(quarto);

        _reservaRepositorioMock
            .Setup(r => r.CadastrarReserva(It.IsAny<Reserva>()))
            .Returns((Reserva r) => r);

        // Act
        _sut.Executar(CriarCommandValido());

        // Assert — verifica que o quarto foi atualizado para Ocupado
        _quartoRepositorioMock.Verify(
            r => r.AtualizarQuarto(It.Is<Quarto>(q => q.Status == StatusQuarto.Ocupado)),
            Times.Once
        );
    }

    [Fact]
    public void Executar_DeveUsarValorDiaria_ComoValorMinimoReserva()
    {
        var quarto = CriarQuartoDisponivel(diaria: 300m);

        _quartoRepositorioMock
            .Setup(r => r.ObterQuartoPorNumero(101))
            .Returns(quarto);

        _reservaRepositorioMock
            .Setup(r => r.CadastrarReserva(It.IsAny<Reserva>()))
            .Returns((Reserva r) => r);

        var reserva = _sut.Executar(CriarCommandValido());

        reserva.ValorMinimo.Should().Be(300m);
        reserva.ValorTotal.Should().Be(300m);
        reserva.TaxasAdicionais.Should().Be(0m);
    }

    // -------------------------------------------------------
    // Cenário: quarto não encontrado
    // -------------------------------------------------------

    [Fact]
    public void Executar_DeveLancarExcecao_QuandoQuartoNaoExiste()
    {
        // Arrange — repositório não acha o quarto
        _quartoRepositorioMock
            .Setup(r => r.ObterQuartoPorNumero(It.IsAny<int>()))
            .Returns((Quarto?)null);

        // Act
        var acao = () => _sut.Executar(CriarCommandValido(numeroQuarto: 999));

        // Assert
        acao.Should().Throw<Exception>()
            .WithMessage("*não encontrado*");
    }

    // -------------------------------------------------------
    // Cenário: quarto ocupado
    // -------------------------------------------------------

    [Fact]
    public void Executar_DeveLancarExcecao_QuandoQuartoOcupado()
    {
        var quartoOcupado = CriarQuartoDisponivel();
        quartoOcupado.Status = StatusQuarto.Ocupado;

        _quartoRepositorioMock
            .Setup(r => r.ObterQuartoPorNumero(101))
            .Returns(quartoOcupado);

        var acao = () => _sut.Executar(CriarCommandValido());

        acao.Should().Throw<Exception>()
            .WithMessage("*disponível*");
    }

    [Fact]
    public void Executar_DeveLancarExcecao_QuandoQuartoIndisponivel()
    {
        var quartoIndisponivel = CriarQuartoDisponivel();
        quartoIndisponivel.Status = StatusQuarto.Indisponivel;

        _quartoRepositorioMock
            .Setup(r => r.ObterQuartoPorNumero(101))
            .Returns(quartoIndisponivel);

        var acao = () => _sut.Executar(CriarCommandValido());

        acao.Should().Throw<Exception>()
            .WithMessage("*disponível*");
    }

    // -------------------------------------------------------
    // Cenário: capacidade excedida
    // -------------------------------------------------------

    [Fact]
    public void Executar_DeveLancarExcecao_QuandoQuantidadeHospedesExcedeCapacidade()
    {
        // Arrange — quarto com capacidade para 2, tentando 5 hóspedes
        var quarto = CriarQuartoDisponivel(capacidade: 2);

        _quartoRepositorioMock
            .Setup(r => r.ObterQuartoPorNumero(101))
            .Returns(quarto);

        var acao = () => _sut.Executar(CriarCommandValido(qtdHospedes: 5));

        acao.Should().Throw<Exception>()
            .WithMessage("*quantidade*");
    }

    [Fact]
    public void Executar_NaoDeveLancarExcecao_QuandoQuantidadeHospedesIgualCapacidade()
    {
        // Arrange — quarto com capacidade 2, exatamente 2 hóspedes (limite exato)
        var quarto = CriarQuartoDisponivel(capacidade: 2);

        _quartoRepositorioMock
            .Setup(r => r.ObterQuartoPorNumero(101))
            .Returns(quarto);

        _reservaRepositorioMock
            .Setup(r => r.CadastrarReserva(It.IsAny<Reserva>()))
            .Returns((Reserva r) => r);

        var acao = () => _sut.Executar(CriarCommandValido(qtdHospedes: 2));

        acao.Should().NotThrow();
    }
}
