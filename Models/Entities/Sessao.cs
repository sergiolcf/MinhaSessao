namespace MinhaSessao.Models.Entities;

public class Sessao
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PacienteId { get; set; }

    public Guid ProfissionalId { get; set; }

    public DateTime DataHora { get; set; }

    public int DuracaoMinutos { get; set; }

    public StatusSessao Status { get; set; } = StatusSessao.Agendada;

    // Código de identificação único da sessão, gerado e gravado na criação (formato MS_{numero}_{iniciais},
    // numero sequencial por Profissional) — nunca recalculado, pra ficar estável mesmo se o profissional
    // mudar de nome ou outras sessões dele forem excluídas
    public string Codigo { get; set; } = string.Empty;

    // Propriedades de navegação (EF Core)
    public Paciente? Paciente { get; set; }

    public Profissional? Profissional { get; set; }
}
