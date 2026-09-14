namespace MinhaSessao.Models.ViewModels;

public class PainelSessoesViewModel
{
    public SessaoListItemViewModel? ProximaSessao { get; set; }

    public int TotalAgendadas { get; set; }

    public int TotalCanceladas { get; set; }

    public int TotalRealizadas { get; set; }

    public List<SessaoListItemViewModel> Agendadas { get; set; } = new();

    public List<SessaoListItemViewModel> Historico { get; set; } = new();

    public List<AnotacaoClinicaListItemViewModel> AnotacoesClinicas { get; set; } = new();

    public int PaginaAtualAnotacoesClinicas { get; set; } = 1;

    public int TotalPaginasAnotacoesClinicas { get; set; } = 1;
}

public class SessaoListItemViewModel
{
    public Guid Id { get; set; }

    public DateTime DataHora { get; set; }

    public string ProfissionalNome { get; set; } = string.Empty;

    // "Agendada", "Realizada" ou "Cancelada"
    public string Status { get; set; } = string.Empty;
}
