using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MinhaSessao.Data;
using MinhaSessao.Models.Entities;
using MinhaSessao.Services;
using Xunit;

namespace MinhaSessao.Tests.Integration.Services;

// Testes de integração do VinculoService com EF Core InMemory — banco isolado por teste
// (nome único via Guid.NewGuid()), sem tocar no Postgres real.
public class VinculoServiceTests
{
    private static ApplicationDbContext CriarContexto()
    {
        var opcoes = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(opcoes);
    }

    private static Paciente CriarPaciente()
    {
        return new Paciente
        {
            Id = Guid.NewGuid(),
            NomeCompleto = "Paciente Teste",
            Telefone = "11999999999",
            Email = $"{Guid.NewGuid()}@teste.com",
            Senha = "hash-fake",
            DataNascimento = DateTime.UtcNow.AddYears(-30)
        };
    }

    private static Profissional CriarProfissional()
    {
        return new Profissional
        {
            Id = Guid.NewGuid(),
            NomeCompleto = "Profissional Teste",
            RegistroCRP = "00/00000",
            Email = $"{Guid.NewGuid()}@teste.com",
            Telefone = "11999999999",
            Senha = "hash-fake"
        };
    }

    [Fact]
    public async Task PacientePertenceAoProfissionalAsync_ComVinculoAtivo_DeveRetornarTrue()
    {
        // Arrange
        await using var contexto = CriarContexto();
        var paciente = CriarPaciente();
        var profissional = CriarProfissional();
        contexto.Pacientes.Add(paciente);
        contexto.Profissionais.Add(profissional);
        contexto.Vinculos.Add(new VinculoPacienteProfissional
        {
            Id = Guid.NewGuid(),
            PacienteId = paciente.Id,
            ProfissionalId = profissional.Id,
            Status = StatusVinculo.Ativo,
            DataInicio = DateTime.UtcNow
        });
        await contexto.SaveChangesAsync();
        var servico = new VinculoService(contexto);

        // Act
        var pertence = await servico.PacientePertenceAoProfissionalAsync(paciente.Id, profissional.Id);

        // Assert
        Assert.True(pertence);
    }

    [Fact]
    public async Task PacientePertenceAoProfissionalAsync_ComVinculoEncerrado_DeveRetornarFalse()
    {
        // Arrange
        await using var contexto = CriarContexto();
        var paciente = CriarPaciente();
        var profissional = CriarProfissional();
        contexto.Pacientes.Add(paciente);
        contexto.Profissionais.Add(profissional);
        contexto.Vinculos.Add(new VinculoPacienteProfissional
        {
            Id = Guid.NewGuid(),
            PacienteId = paciente.Id,
            ProfissionalId = profissional.Id,
            Status = StatusVinculo.Encerrado,
            DataInicio = DateTime.UtcNow.AddMonths(-6),
            DataFim = DateTime.UtcNow
        });
        await contexto.SaveChangesAsync();
        var servico = new VinculoService(contexto);

        // Act
        var pertence = await servico.PacientePertenceAoProfissionalAsync(paciente.Id, profissional.Id);

        // Assert
        Assert.False(pertence);
    }

    [Fact]
    public async Task PacientePertenceAoProfissionalAsync_SemVinculo_DeveRetornarFalse()
    {
        // Arrange
        await using var contexto = CriarContexto();
        var paciente = CriarPaciente();
        var profissional = CriarProfissional();
        contexto.Pacientes.Add(paciente);
        contexto.Profissionais.Add(profissional);
        await contexto.SaveChangesAsync();
        var servico = new VinculoService(contexto);

        // Act
        var pertence = await servico.PacientePertenceAoProfissionalAsync(paciente.Id, profissional.Id);

        // Assert
        Assert.False(pertence);
    }

    [Fact]
    public async Task CriarVinculo_DeveGravarComStatusAtivoEDataInicioPreenchida()
    {
        // Arrange
        await using var contexto = CriarContexto();
        var paciente = CriarPaciente();
        var profissional = CriarProfissional();
        contexto.Pacientes.Add(paciente);
        contexto.Profissionais.Add(profissional);
        var antes = DateTime.UtcNow;
        var servico = new VinculoService(contexto);

        // Act
        servico.CriarVinculo(paciente.Id, profissional.Id);
        await contexto.SaveChangesAsync();
        var depois = DateTime.UtcNow;

        // Assert
        var vinculo = await contexto.Vinculos.SingleAsync(v =>
            v.PacienteId == paciente.Id && v.ProfissionalId == profissional.Id);
        Assert.Equal(StatusVinculo.Ativo, vinculo.Status);
        Assert.InRange(vinculo.DataInicio, antes.AddSeconds(-1), depois.AddSeconds(1));
    }
}
