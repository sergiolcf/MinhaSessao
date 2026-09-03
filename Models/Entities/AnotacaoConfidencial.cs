namespace MinhaSessao.Models.Entities;

public class AnotacaoConfidencial
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PacienteId { get; set; }

    public Guid ProfissionalId { get; set; }

    public string? Titulo { get; set; }

    public string Conteudo { get; set; } = string.Empty;

    public DateTime DataRegistro { get; set; } = DateTime.UtcNow;

    // Propriedades de navegação (EF Core)
    public Paciente? Paciente { get; set; }

    public Profissional? Profissional { get; set; }
}
