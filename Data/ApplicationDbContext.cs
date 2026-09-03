using Microsoft.EntityFrameworkCore;
using MinhaSessao.Models.Entities;

namespace MinhaSessao.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Profissional> Profissionais => Set<Profissional>();

    public DbSet<Paciente> Pacientes => Set<Paciente>();

    public DbSet<AnotacaoConfidencial> AnotacoesConfidenciais => Set<AnotacaoConfidencial>();

    public DbSet<VinculoPacienteProfissional> Vinculos => Set<VinculoPacienteProfissional>();
}
