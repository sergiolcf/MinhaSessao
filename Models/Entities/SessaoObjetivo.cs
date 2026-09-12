namespace MinhaSessao.Models.Entities;

// Vínculo N:N entre uma Anotação da Sessão e um Objetivo Terapêutico — cada anotação guarda seus
// próprios objetivos trabalhados (antes era vinculado direto à Sessão inteira; ver AnotacaoSessao)
public class SessaoObjetivo
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AnotacaoSessaoId { get; set; }

    public Guid ObjetivoTerapeuticoId { get; set; }

    public string? Observacao { get; set; }

    public DateTime DataRegistro { get; set; } = DateTime.UtcNow;

    // Propriedades de navegação (EF Core)
    public AnotacaoSessao? AnotacaoSessao { get; set; }

    public ObjetivoTerapeutico? ObjetivoTerapeutico { get; set; }
}
