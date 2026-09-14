namespace MinhaSessao.Models.Entities;

// Anotação clínica pontual de uma Sessão (título + conteúdo + data) — substitui o antigo campo
// único Sessao.AnotacoesClinicas por um histórico de anotações, no mesmo espírito de
// AnotacaoConfidencial (que é por Paciente; esta é por Sessão)
public class AnotacaoSessao
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SessaoId { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Conteudo { get; set; } = string.Empty;

    public DateTime DataRegistro { get; set; } = DateTime.UtcNow;

    // Propriedades de navegação (EF Core)
    public Sessao? Sessao { get; set; }

    public ICollection<SessaoObjetivo> SessaoObjetivos { get; set; } = new List<SessaoObjetivo>();
}
