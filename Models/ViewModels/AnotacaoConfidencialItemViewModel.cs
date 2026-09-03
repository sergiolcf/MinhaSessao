namespace MinhaSessao.Models.ViewModels;

public class AnotacaoConfidencialItemViewModel
{
    public Guid Id { get; set; }

    public string? Titulo { get; set; }

    public string Conteudo { get; set; } = string.Empty;

    public DateTime DataRegistro { get; set; }
}
