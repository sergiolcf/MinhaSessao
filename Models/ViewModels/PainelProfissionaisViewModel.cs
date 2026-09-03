namespace MinhaSessao.Models.ViewModels;

public class PainelProfissionaisViewModel
{
    public List<ProfissionalListaItemViewModel> MeusProfissionais { get; set; } = new();

    public List<ProfissionalListaItemViewModel> TodosProfissionais { get; set; } = new();

    // "meus" ou "todos" — qual aba deve abrir ativa (ex.: chegada via redirect da antiga rota de busca)
    public string AbaInicial { get; set; } = "meus";

    public string? TermoBusca { get; set; }
}

public class ProfissionalListaItemViewModel
{
    public Guid Id { get; set; }

    public string NomeCompleto { get; set; } = string.Empty;

    public string RegistroCRP { get; set; } = string.Empty;

    public string? Apresentacao { get; set; }

    public string? FotoUrl { get; set; }

    public string Iniciais => PacienteIniciais.Calcular(NomeCompleto);

    // null = item da aba "Todos os Profissionais" (sem vínculo); true/false = vínculo Ativo/Encerrado na aba "Meus Profissionais"
    public bool? VinculoAtivo { get; set; }
}
