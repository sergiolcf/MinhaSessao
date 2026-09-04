namespace MinhaSessao.Models.Entities;

public class ObjetivoTerapeutico
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PacienteId { get; set; }

    // Profissional que criou o objetivo
    public Guid ProfissionalId { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public StatusObjetivo Status { get; set; } = StatusObjetivo.EmAndamento;

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public DateTime? DataAtualizacao { get; set; }

    // Propriedades de navegação (EF Core)
    public Paciente? Paciente { get; set; }

    public Profissional? Profissional { get; set; }

    public ICollection<Combinado> Combinados { get; set; } = new List<Combinado>();

    public ICollection<SessaoObjetivo> SessoesObjetivo { get; set; } = new List<SessaoObjetivo>();
}
