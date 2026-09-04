namespace MinhaSessao.Models.ViewModels;

public class PainelSessoesProfissionalViewModel
{
    public int SessoesHoje { get; set; }

    public SessaoProfissionalListItemViewModel? ProximaSessao { get; set; }

    public int AtendimentosNoMes { get; set; }

    public List<SessaoProfissionalListItemViewModel> Agendadas { get; set; } = new();

    public int PaginaAtualAgendadas { get; set; } = 1;

    public int TotalPaginasAgendadas { get; set; } = 1;

    public List<SessaoProfissionalListItemViewModel> Historico { get; set; } = new();

    public int PaginaAtualHistorico { get; set; } = 1;

    public int TotalPaginasHistorico { get; set; } = 1;

    public List<PacienteSelectItemViewModel> Pacientes { get; set; } = new();

    public int DuracaoPadraoSessaoMinutos { get; set; }
}

public class SessaoProfissionalListItemViewModel
{
    public Guid Id { get; set; }

    public Guid PacienteId { get; set; }

    public DateTime DataHora { get; set; }

    public string PacienteNome { get; set; } = string.Empty;

    public int DuracaoMinutos { get; set; }

    // "Agendada", "Realizada" ou "Cancelada"
    public string Status { get; set; } = string.Empty;

    // Preenchido só onde a evolução da sessão é exibida (ex.: aba "Histórico de Sessões" da Ficha do Paciente)
    public string? AnotacoesClinicas { get; set; }
}
