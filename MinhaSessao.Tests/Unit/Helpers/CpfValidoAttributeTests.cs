using MinhaSessao.Helpers;
using Xunit;

namespace MinhaSessao.Tests.Unit.Helpers;

// Testes do CpfValidoAttribute (dígito verificador), isolados, sem tocar em banco
public class CpfValidoAttributeTests
{
    private readonly CpfValidoAttribute _atributo = new();

    [Theory]
    [InlineData("529.982.247-25")] // CPF válido, mascarado
    [InlineData("52998224725")]    // mesmo CPF, só dígitos
    public void CpfComDigitoVerificadorCorreto_DeveSerValido(string cpf)
    {
        // Act
        var valido = _atributo.IsValid(cpf);

        // Assert
        Assert.True(valido);
    }

    [Fact]
    public void CpfComDigitoVerificadorErrado_DeveSerInvalido()
    {
        // Arrange: mesmo formato válido, mas o dígito verificador não bate com a base
        const string cpf = "147.147.144-78";

        // Act
        var valido = _atributo.IsValid(cpf);

        // Assert
        Assert.False(valido);
    }

    [Fact]
    public void CpfComTodosOsDigitosIguais_DeveSerInvalido()
    {
        // Arrange: passa no cálculo do dígito verificador, mas nunca é emitido de verdade
        const string cpf = "111.111.111-11";

        // Act
        var valido = _atributo.IsValid(cpf);

        // Assert
        Assert.False(valido);
    }

    [Fact]
    public void CpfIncompleto_DeveSerInvalido()
    {
        // Arrange
        const string cpf = "123.456.789";

        // Act
        var valido = _atributo.IsValid(cpf);

        // Assert
        Assert.False(valido);
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
