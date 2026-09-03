namespace MinhaSessao.Models.ViewModels;

public class DiretorioViewModel
{
    public List<DiretorioProfissionalItemViewModel> Profissionais { get; set; } = new();

    public string? TermoBusca { get; set; }
}
