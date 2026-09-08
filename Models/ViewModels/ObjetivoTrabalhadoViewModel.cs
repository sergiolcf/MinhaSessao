namespace MinhaSessao.Models.ViewModels;

// Representa um Objetivo Terapêutico trabalhado numa sessão específica (via SessaoObjetivo),
// usado na aba "Histórico de Sessões" da Ficha do Paciente
public class ObjetivoTrabalhadoViewModel
{
    public string Titulo { get; set; } = string.Empty;

    public string? Observacao { get; set; }
}
