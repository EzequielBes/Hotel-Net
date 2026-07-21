using CheckInApp.Domain.ValueObjects;
using FluentAssertions;

namespace CheckInApp.Tests.Domain.ValueObjects;

public class CpfTests
{
    // -------------------------------------------------------
    // Casos válidos
    // -------------------------------------------------------

    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("000.000.000-00")]
    [InlineData("123.456.789-09")]
    public void Cpf_DeveSerCriadoComSucesso_QuandoFormatoValido(string cpfValido)
    {
        // Act
        var cpf = new Cpf(cpfValido);

        // Assert
        cpf.Value.Should().Be(cpfValido);
    }

    [Fact]
    public void Cpf_DeveArmazenarValorOriginal()
    {
        var cpf = new Cpf("529.982.247-25");

        cpf.Value.Should().Be("529.982.247-25");
    }

    // -------------------------------------------------------
    // Casos inválidos — formato
    // -------------------------------------------------------

    [Theory]
    [InlineData("52998224725")]       // sem pontuação
    [InlineData("529.982.247/25")]    // separador errado
    [InlineData("529.982.24-25")]     // dígito faltando
    [InlineData("5299.982.247-25")]   // dígito a mais
    [InlineData("abc.def.ghi-jk")]    // letras
    public void Cpf_DeveLancarExcecao_QuandoFormatoInvalido(string cpfInvalido)
    {
        // Act
        var acao = () => new Cpf(cpfInvalido);

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*format*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Cpf_DeveLancarExcecao_QuandoNuloOuVazio(string? cpfVazio)
    {
        var acao = () => new Cpf(cpfVazio!);

        acao.Should().Throw<ArgumentException>()
            .WithMessage("*empty*");
    }

    // -------------------------------------------------------
    // Igualdade por valor (comportamento de record)
    // -------------------------------------------------------

    [Fact]
    public void DoisCpfs_ComMesmoValor_DevemSerIguais()
    {
        var cpf1 = new Cpf("529.982.247-25");
        var cpf2 = new Cpf("529.982.247-25");

        cpf1.Should().Be(cpf2);
    }

    [Fact]
    public void DoisCpfs_ComValoresDiferentes_NaoDevemSerIguais()
    {
        var cpf1 = new Cpf("529.982.247-25");
        var cpf2 = new Cpf("000.000.000-00");

        cpf1.Should().NotBe(cpf2);
    }
}
