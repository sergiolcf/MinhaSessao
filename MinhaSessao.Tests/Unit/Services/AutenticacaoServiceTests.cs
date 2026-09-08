using MinhaSessao.Models.Entities;
using MinhaSessao.Services;
using Xunit;

namespace MinhaSessao.Tests.Unit.Services;

// Testes de hash/verificação de senha do AutenticacaoService, isolados, sem tocar em banco
public class AutenticacaoServiceTests
{
    private static Profissional CriarProfissional()
    {
        return new Profissional
        {
            NomeCompleto = "Profissional Teste",
            RegistroCRP = "00/00000",
            Email = "profissional@teste.com",
            Telefone = "11999999999"
        };
    }

    [Fact]
    public void SenhaComHashGerado_DeveSerValidaNaVerificacao()
    {
        // Arrange
        var profissional = CriarProfissional();
        const string senha = "Senha@123";

        // Act
        profissional.Senha = AutenticacaoService.HashSenha(profissional, senha);
        var valida = AutenticacaoService.VerificarSenha(profissional, senha);

        // Assert
        Assert.True(valida);
    }

    [Fact]
    public void SenhaErrada_DeveFalharNaVerificacao()
    {
        // Arrange
        var profissional = CriarProfissional();
        profissional.Senha = AutenticacaoService.HashSenha(profissional, "Senha@123");

        // Act
        var valida = AutenticacaoService.VerificarSenha(profissional, "SenhaErrada@123");

        // Assert
        Assert.False(valida);
    }

    [Fact]
    public void MesmaSenha_DeveGerarHashesDiferentes_PorCausaDoSalt()
    {
        // Arrange
        var profissional = CriarProfissional();
        const string senha = "Senha@123";

        // Act
        var hash1 = AutenticacaoService.HashSenha(profissional, senha);
        var hash2 = AutenticacaoService.HashSenha(profissional, senha);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }
}
