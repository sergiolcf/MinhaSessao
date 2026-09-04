namespace MinhaSessao.Models.Entities;

public class SessaoObjetivo
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SessaoId { get; set; }

    public Guid ObjetivoTerapeuticoId { get; set; }

    public string? Observacao { get; set; }

    public DateTime DataRegistro { get; set; } = DateTime.UtcNow;

    // Propriedades de navegação (EF Core)
    public Sessao? Sessao { get; set; }

    public ObjetivoTerapeutico? ObjetivoTerapeutico { get; set; }
}
