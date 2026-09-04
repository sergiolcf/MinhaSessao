namespace MinhaSessao.Models.Entities;

public class Combinado
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ObjetivoTerapeuticoId { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public bool Concluido { get; set; } = false;

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    // Propriedade de navegação (EF Core)
    public ObjetivoTerapeutico? ObjetivoTerapeutico { get; set; }
}
