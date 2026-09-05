using System;
using MinhaSessao.Helpers;
using Xunit;

namespace MinhaSessao.Tests.Unit.Helpers;

// Testes do MaiorDeIdadeAttribute (idade mínima 18), isolados, sem tocar em banco
public class MaiorDeIdadeAttributeTests
{
    private readonly MaiorDeIdadeAttribute _atributo = new(18);

    [Fact]
    public void PessoaQueFaz18AnosHoje_DeveSerValido()
    {
        // Arrange
        var dataNascimento = DateTime.UtcNow.Date.AddYears(-18);

        // Act
        var valido = _atributo.IsValid(dataNascimento);

        // Assert
        Assert.True(valido);
    }

    [Fact]
    public void PessoaCom17Anos364Dias_DeveSerInvalido()
    {
        // Arrange: nasceu um dia depois do marco de 18 anos atrás, ou seja, faz 18 anos amanhã
        var dataNascimento = DateTime.UtcNow.Date.AddYears(-18).AddDays(1);

        // Act
        var valido = _atributo.IsValid(dataNascimento);

        // Assert
        Assert.False(valido);
    }

    [Fact]
    public void PessoaClaramenteMaiorDeIdade_DeveSerValido()
    {
        // Arrange
        var dataNascimento = DateTime.UtcNow.Date.AddYears(-30);

        // Act
        var valido = _atributo.IsValid(dataNascimento);

        // Assert
        Assert.True(valido);
    }

    [Fact]
    public void ValorNulo_DeveSerValido_PoisRequiredJaCobreAusenciaDeValor()
    {
        // Act
        var valido = _atributo.IsValid(null);

        // Assert
        Assert.True(valido);
    }
}
