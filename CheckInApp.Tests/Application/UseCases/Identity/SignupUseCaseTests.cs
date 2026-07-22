using CheckInApp.Application.UseCases.Identity;
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Enums;
using CheckInApp.Domain.Ports;
using CheckInApp.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CheckInApp.Tests.Application.UseCases.Identity;

public class SignupUseCaseTests
{
    // Moq creates a "double" for the repository — no real database
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly SignupUseCase _sut; // sut = System Under Test

    public SignupUseCaseTests()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _sut = new SignupUseCase(_repositoryMock.Object);
    }

    // -------------------------------------------------------
    // Scenario: successful registration
    // -------------------------------------------------------

    [Fact]
    public void Execute_ShouldRegisterUser_WhenDataIsValid()
    {
        // Arrange — email doesn't exist in the repository (returns null)
        _repositoryMock
            .Setup(r => r.GetByEmail(It.IsAny<string>()))
            .Returns((User?)null);

        var command = new RegisterUserCommand(
            Name: "João Silva",
            Email: "joao@teste.com",
            Cpf: "529.982.247-25",
            Password: "Teste@123",
            PhoneNumber: "11999999999"
        );

        // Act
        var action = () => _sut.Execute(command);

        // Assert
        action.Should().NotThrow();

        // Verify the repository was called exactly once with any User
        _repositoryMock.Verify(r => r.Add(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public void Execute_ShouldRegisterWithClientRole_ByDefault()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByEmail(It.IsAny<string>()))
            .Returns((User?)null);

        User? savedUser = null;

        // Capture the user passed to Add
        _repositoryMock
            .Setup(r => r.Add(It.IsAny<User>()))
            .Callback<User>(u => savedUser = u);

        var command = new RegisterUserCommand(
            Name: "Maria",
            Email: "maria@teste.com",
            Cpf: "000.000.000-00",
            Password: "Teste@123",
            PhoneNumber: "11999999999"
        );

        // Act
        _sut.Execute(command);

        // Assert — default role should be Client without specifying it
        savedUser.Should().NotBeNull();
        savedUser!.Role.Should().Be(UserRole.Client);
    }

    // -------------------------------------------------------
    // Scenario: duplicate email
    // -------------------------------------------------------

    [Fact]
    public void Execute_ShouldThrowException_WhenEmailAlreadyRegistered()
    {
        // Arrange — simulates a user with this email already existing
        var existingUser = User.Restore(
            id: 1,
            name: "Existing User",
            email: "joao@teste.com",
            cpf: "529.982.247-25",
            passwordHash: "some_hash",
            phoneNumber: "11999999999",
            role: UserRole.Client
        );

        _repositoryMock
            .Setup(r => r.GetByEmail("joao@teste.com"))
            .Returns(existingUser);

        var command = new RegisterUserCommand(
            Name: "João Duplicado",
            Email: "joao@teste.com",
            Cpf: "000.000.000-00",
            Password: "Teste@123",
            PhoneNumber: "11888888888"
        );

        // Act
        var action = () => _sut.Execute(command);

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*e-mail*");

        // Guarantee that nothing was saved
        _repositoryMock.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
    }

    // -------------------------------------------------------
    // Scenario: invalid data (VO validation)
    // -------------------------------------------------------

    [Fact]
    public void Execute_ShouldThrowException_WhenEmailIsInvalid()
    {
        _repositoryMock
            .Setup(r => r.GetByEmail(It.IsAny<string>()))
            .Returns((User?)null);

        var command = new RegisterUserCommand(
            Name: "João",
            Email: "invalid-email-no-at-sign",
            Cpf: "529.982.247-25",
            Password: "Teste@123",
            PhoneNumber: "11999999999"
        );

        var action = () => _sut.Execute(command);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Execute_ShouldThrowException_WhenPasswordDoesNotMeetPolicy()
    {
        _repositoryMock
            .Setup(r => r.GetByEmail(It.IsAny<string>()))
            .Returns((User?)null);

        var command = new RegisterUserCommand(
            Name: "João",
            Email: "joao@teste.com",
            Cpf: "529.982.247-25",
            Password: "weak",  // short, no uppercase, no special char
            PhoneNumber: "11999999999"
        );

        var action = () => _sut.Execute(command);

        action.Should().Throw<ArgumentException>();
    }
}
