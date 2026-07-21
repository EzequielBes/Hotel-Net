using CheckInApp.Application.UseCases.Identidade;
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Enums;
using CheckInApp.Domain.Ports;
using CheckInApp.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CheckInApp.Tests.Application.UseCases.Identidade;

public class SignupUseCaseTests
{
    // Moq cria um "dublê" do repositório — sem banco de dados real
    private readonly Mock<IUsuarioRepository> _repositorioMock;
    private readonly SignupUseCase _sut; // sut = System Under Test (padrão de nomenclatura)

    public SignupUseCaseTests()
    {
        _repositorioMock = new Mock<IUsuarioRepository>();
        _sut = new SignupUseCase(_repositorioMock.Object);
    }

    // -------------------------------------------------------
    // Cenário: cadastro bem-sucedido
    // -------------------------------------------------------

    [Fact]
    public void Executar_DeveCadastrarUsuario_QuandoDadosValidos()
    {
        // Arrange — e-mail não existe no banco (repositório retorna null)
        _repositorioMock
            .Setup(r => r.ObterPorEmail(It.IsAny<string>()))
            .Returns((Usuario?)null);

        var command = new CadastrarUsuarioCommand(
            Nome: "João Silva",
            Email: "joao@teste.com",
            Cpf: "529.982.247-25",
            Senha: "Teste@123",
            NumeroCelular: "11999999999"
        );

        // Act
        var acao = () => _sut.Executar(command);

        // Assert
        acao.Should().NotThrow();

        // Verifica que o repositório foi chamado exatamente 1 vez com qualquer Usuario
        _repositorioMock.Verify(r => r.Adicionar(It.IsAny<Usuario>()), Times.Once);
    }

    [Fact]
    public void Executar_DeveCadastrarComRoleCliente_PorPadrao()
    {
        // Arrange
        _repositorioMock
            .Setup(r => r.ObterPorEmail(It.IsAny<string>()))
            .Returns((Usuario?)null);

        Usuario? usuarioSalvo = null;

        // Captura o usuário que foi passado para o Adicionar
        _repositorioMock
            .Setup(r => r.Adicionar(It.IsAny<Usuario>()))
            .Callback<Usuario>(u => usuarioSalvo = u);

        var command = new CadastrarUsuarioCommand(
            Nome: "Maria",
            Email: "maria@teste.com",
            Cpf: "000.000.000-00",
            Senha: "Teste@123",
            NumeroCelular: "11999999999"
        );

        // Act
        _sut.Executar(command);

        // Assert — role padrão deve ser Cliente sem precisar informar
        usuarioSalvo.Should().NotBeNull();
        usuarioSalvo!.Role.Should().Be(TipoUsuario.Cliente);
    }

    // -------------------------------------------------------
    // Cenário: e-mail duplicado
    // -------------------------------------------------------

    [Fact]
    public void Executar_DeveLancarExcecao_QuandoEmailJaCadastrado()
    {
        // Arrange — simula que já existe um usuário com esse e-mail
        var usuarioExistente = Usuario.Restaurar(
            id: 1,
            nome: "Usuário Existente",
            email: "joao@teste.com",
            cpf: "529.982.247-25",
            senhaHash: "hash_qualquer",
            numeroCelular: "11999999999",
            role: TipoUsuario.Cliente
        );

        _repositorioMock
            .Setup(r => r.ObterPorEmail("joao@teste.com"))
            .Returns(usuarioExistente);

        var command = new CadastrarUsuarioCommand(
            Nome: "João Duplicado",
            Email: "joao@teste.com",
            Cpf: "000.000.000-00",
            Senha: "Teste@123",
            NumeroCelular: "11888888888"
        );

        // Act
        var acao = () => _sut.Executar(command);

        // Assert
        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*e-mail*");

        // Garante que nunca chegou a salvar nada
        _repositorioMock.Verify(r => r.Adicionar(It.IsAny<Usuario>()), Times.Never);
    }

    // -------------------------------------------------------
    // Cenário: dados inválidos (validação dos VOs)
    // -------------------------------------------------------

    [Fact]
    public void Executar_DeveLancarExcecao_QuandoEmailInvalido()
    {
        _repositorioMock
            .Setup(r => r.ObterPorEmail(It.IsAny<string>()))
            .Returns((Usuario?)null);

        var command = new CadastrarUsuarioCommand(
            Nome: "João",
            Email: "email-invalido-sem-arroba",
            Cpf: "529.982.247-25",
            Senha: "Teste@123",
            NumeroCelular: "11999999999"
        );

        var acao = () => _sut.Executar(command);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Executar_DeveLancarExcecao_QuandoSenhaNaoCumprePolitica()
    {
        _repositorioMock
            .Setup(r => r.ObterPorEmail(It.IsAny<string>()))
            .Returns((Usuario?)null);

        var command = new CadastrarUsuarioCommand(
            Nome: "João",
            Email: "joao@teste.com",
            Cpf: "529.982.247-25",
            Senha: "fraca",   // curta, sem maiúscula, sem especial
            NumeroCelular: "11999999999"
        );

        var acao = () => _sut.Executar(command);

        acao.Should().Throw<ArgumentException>();
    }
}
