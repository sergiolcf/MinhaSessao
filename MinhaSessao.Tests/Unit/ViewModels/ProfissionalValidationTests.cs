using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MinhaSessao.Models.ViewModels;
using Xunit;

namespace MinhaSessao.Tests.Unit.ViewModels;

// Testes de validação (DataAnnotations) do ProfissionalViewModel, isolados, sem tocar em banco
public class ProfissionalValidationTests
{
    // Objeto "base" totalmente válido; cada teste parte dele e altera só o campo sob teste
    private static ProfissionalViewModel CriarModeloValido()
    {
        return new ProfissionalViewModel
        {
            NomeCompleto = "Ana Souza",
            RegistroCRP = "06/123456",
            Email = "ana.souza@email.com",
            Telefone = "(11) 91234-5678",
            Senha = "Senha@123",
            ConfirmarSenha = "Senha@123",
            Apresentacao = "Profissional dedicada ao cuidado da saúde mental dos pacientes.",
            Foto = null
        };
    }

    private static List<ValidationResult> Validar(ProfissionalViewModel model)
    {
        var contexto = new ValidationContext(model);
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(model, contexto, resultados, validateAllProperties: true);
        return resultados;
    }

    [Fact]
    public void ModeloTotalmenteValido_NaoDeveGerarNenhumErro()
    {
        // Arrange
        var model = CriarModeloValido();

        // Act
        var resultados = Validar(model);

        // Assert
        Assert.Empty(resultados);
    }

    [Theory]
    [InlineData("06123456")]  // sem barra
    [InlineData("AB/123456")] // com letras
    public void Crp_ForaDoPadrao_DeveFalhar(string crp)
    {
        // Arrange
        var model = CriarModeloValido();
        model.RegistroCRP = crp;

        // Act
        var resultados = Validar(model);

        // Assert
        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(ProfissionalViewModel.RegistroCRP)));
    }

    [Fact]
    public void Apresentacao_ComMenosDe30Caracteres_DeveFalhar()
    {
        // Arrange
        var model = CriarModeloValido();
        model.Apresentacao = "Muito curta para ser aceita."; // menos de 30 caracteres

        // Act
        var resultados = Validar(model);

        // Assert
        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(ProfissionalViewModel.Apresentacao)));
    }

    [Fact]
    public void Apresentacao_ComMaisDe500Caracteres_DeveFalhar()
    {
        // Arrange
        var model = CriarModeloValido();
        model.Apresentacao = new string('a', 501);

        // Act
        var resultados = Validar(model);

        // Assert
        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(ProfissionalViewModel.Apresentacao)));
    }

    [Fact]
    public void Senha_SemCaractereEspecial_DeveFalhar()
    {
        // Arrange
        var model = CriarModeloValido();
        model.Senha = "Senha1234";
        model.ConfirmarSenha = "Senha1234";

        // Act
        var resultados = Validar(model);

        // Assert
        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(ProfissionalViewModel.Senha)));
    }

    [Fact]
    public void Senha_SemLetraMaiuscula_DeveFalhar()
    {
        // Arrange
        var model = CriarModeloValido();
        model.Senha = "senha@123";
        model.ConfirmarSenha = "senha@123";

        // Act
        var resultados = Validar(model);

        // Assert
        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(ProfissionalViewModel.Senha)));
    }

    [Fact]
    public void Senha_SemNumero_DeveFalhar()
    {
        // Arrange
        var model = CriarModeloValido();
        model.Senha = "Senha@abc";
        model.ConfirmarSenha = "Senha@abc";

        // Act
        var resultados = Validar(model);

        // Assert
        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(ProfissionalViewModel.Senha)));
    }
}
