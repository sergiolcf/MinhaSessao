namespace MinhaSessao.Models.ViewModels;

public class PainelSessoesViewModel
{
    public SessaoListItemViewModel? ProximaSessao { get; set; }

    public int TotalSessoesRealizadas { get; set; }

    public List<SessaoListItemViewModel> Agendadas { get; set; } = new();

    public List<SessaoListItemViewModel> Historico { get; set; } = new();
}

public class SessaoListItemViewModel
{
    public Guid Id { get; set; }

    public DateTime DataHora { get; set; }

    public string ProfissionalNome { get; set; } = string.Empty;

    // "Agendada", "Realizada" ou "Cancelada"
    public string Status { get; set; } = string.Empty;
}
