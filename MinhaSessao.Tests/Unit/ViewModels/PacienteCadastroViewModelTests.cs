using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MinhaSessao.Models.ViewModels;
using Xunit;

namespace MinhaSessao.Tests.Unit.ViewModels;

// Testes de validação (DataAnnotations) do PacienteCadastroViewModel, isolados, sem tocar em banco
public class PacienteCadastroViewModelTests
{
    // Objeto "base" totalmente válido; cada teste parte dele e altera só o campo sob teste
    private static PacienteCadastroViewModel CriarModeloValido()
    {
        return new PacienteCadastroViewModel
        {
            NomeCompleto = "Maria Silva",
            Email = "maria.silva@email.com",
            Telefone = "(11) 91234-5678",
            DataNascimento = DateTime.UtcNow.Date.AddYears(-30),
            Cpf = "123.456.789-09",
            Senha = "Senha@123",
            ConfirmarSenha = "Senha@123"
        };
    }

    private static List<ValidationResult> Validar(PacienteCadastroViewModel model)
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

    [Fact]
    public void NomeCompleto_SemSobrenome_DeveFalhar()
    {
        // Arrange
        var model = CriarModeloValido();
        model.NomeCompleto = "Maria";

        // Act
        var resultados = Validar(model);

        // Assert
        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(PacienteCadastroViewModel.NomeCompleto)));
    }

    [Fact]
    public void NomeCompleto_ComNumero_DeveFalhar()
    {
        // Arrange
        var model = CriarModeloValido();
        model.NomeCompleto = "Maria123 Silva";

        // Act
        var resultados = Validar(model);

        // Assert
        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(PacienteCadastroViewModel.NomeCompleto)));
    }

    [Fact]
    public void Telefone_ComMenosDe10Digitos_DeveFalhar()
    {
        // Arrange
        var model = CriarModeloValido();
        model.Telefone = "123456789";

        // Act
        var resultados = Validar(model);

        // Assert
        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(PacienteCadastroViewModel.Telefone)));
    }

    [Theory]
    [InlineData("(11) 91234-5678")] // celular mascarado
    [InlineData("(11) 1234-5678")]  // fixo mascarado
    [InlineData("11912345678")]     // celular só dígitos
    [InlineData("1112345678")]      // fixo só dígitos
    public void Telefone_ValidoMascaradoOuSoDigitos_DevePassar(string telefone)
    {
        // Arrange
        var model = CriarModeloValido();
        model.Telefone = telefone;

        // Act
        var resultados = Validar(model);

        // Assert
        Assert.DoesNotContain(resultados, r => r.MemberNames.Contains(nameof(PacienteCadastroViewModel.Telefone)));
    }

    [Fact]
    public void Cpf_Incompleto_DeveFalhar()
    {
        // Arrange
        var model = CriarModeloValido();
        model.Cpf = "123.456.789";

        // Act
        var resultados = Validar(model);

        // Assert
        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(PacienteCadastroViewModel.Cpf)));
    }

    [Fact]
    public void Cpf_ComDigitoVerificadorInvalido_DeveFalhar()
    {
        // Arrange: formato correto (11 dígitos), mas o dígito verificador não bate com a base
        var model = CriarModeloValido();
        model.Cpf = "147.147.144-78";

        // Act
        var resultados = Validar(model);

        // Assert
        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(PacienteCadastroViewModel.Cpf)));
    }

    [Theory]
    [InlineData("123.456.789-09")] // mascarado
    [InlineData("12345678909")]    // só dígitos
    public void Cpf_ValidoMascaradoOuSoDigitos_DevePassar(string cpf)
    {
        // Arrange
        var model = CriarModeloValido();
        model.Cpf = cpf;

        // Act
        var resultados = Validar(model);

        // Assert
        Assert.DoesNotContain(resultados, r => r.MemberNames.Contains(nameof(PacienteCadastroViewModel.Cpf)));
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
        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(PacienteCadastroViewModel.Senha)));
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
        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(PacienteCadastroViewModel.Senha)));
    }

    [Fact]
    public void Senha_ComMenosDe8Caracteres_DeveFalhar()
    {
        // Arrange
        var model = CriarModeloValido();
        model.Senha = "Sen@12";
        model.ConfirmarSenha = "Sen@12";

        // Act
        var resultados = Validar(model);

        // Assert
        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(PacienteCadastroViewModel.Senha)));
    }
}
