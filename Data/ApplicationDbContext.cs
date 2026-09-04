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

    public DbSet<Sessao> Sessoes => Set<Sessao>();

    public DbSet<ObjetivoTerapeutico> ObjetivosTerapeuticos => Set<ObjetivoTerapeutico>();

    public DbSet<Combinado> Combinados => Set<Combinado>();

    public DbSet<SessaoObjetivo> SessoesObjetivos => Set<SessaoObjetivo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Objetivo Terapêutico pertence a um Paciente e a um Profissional (obrigatórios, sem exclusão em cascata
        // para não apagar objetivos/combinados junto se um Paciente ou Profissional for removido)
        modelBuilder.Entity<ObjetivoTerapeutico>()
            .HasOne(o => o.Paciente)
            .WithMany()
            .HasForeignKey(o => o.PacienteId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ObjetivoTerapeutico>()
            .HasOne(o => o.Profissional)
            .WithMany()
            .HasForeignKey(o => o.ProfissionalId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Combinado pertence a um Objetivo Terapêutico; ao apagar o objetivo, apaga os combinados junto
        modelBuilder.Entity<Combinado>()
            .HasOne(c => c.ObjetivoTerapeutico)
            .WithMany(o => o.Combinados)
            .HasForeignKey(c => c.ObjetivoTerapeuticoId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Vínculo N:N entre Sessão e Objetivo Terapêutico: ao apagar a sessão, apaga os vínculos junto;
        // ao apagar o objetivo, o vínculo é restrito (evita múltiplos caminhos de cascade do EF Core) —
        // exclusão de objetivo com sessões vinculadas precisa ser tratada explicitamente no controller
        modelBuilder.Entity<SessaoObjetivo>()
            .HasOne(so => so.Sessao)
            .WithMany()
            .HasForeignKey(so => so.SessaoId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SessaoObjetivo>()
            .HasOne(so => so.ObjetivoTerapeutico)
            .WithMany(o => o.SessoesObjetivo)
            .HasForeignKey(so => so.ObjetivoTerapeuticoId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
