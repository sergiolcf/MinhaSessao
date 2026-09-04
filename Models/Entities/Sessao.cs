namespace MinhaSessao.Models.Entities;

public class Sessao
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PacienteId { get; set; }

    public Guid ProfissionalId { get; set; }

    public DateTime DataHora { get; set; }

    public int DuracaoMinutos { get; set; }

    public StatusSessao Status { get; set; } = StatusSessao.Agendada;

    public string? AnotacoesClinicas { get; set; }

    // Propriedades de navegação (EF Core)
    public Paciente? Paciente { get; set; }

    public Profissional? Profissional { get; set; }
}
