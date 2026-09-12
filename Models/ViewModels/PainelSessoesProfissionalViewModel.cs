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

    public List<SessaoProfissionalListItemViewModel> EmAndamento { get; set; } = new();

    public int PaginaAtualEmAndamento { get; set; } = 1;

    public int TotalPaginasEmAndamento { get; set; } = 1;

    public List<PacienteSelectItemViewModel> Pacientes { get; set; } = new();

    public int DuracaoPadraoSessaoMinutos { get; set; }
}

public class SessaoProfissionalListItemViewModel
{
    public Guid Id { get; set; }

    public Guid PacienteId { get; set; }

    // Código de identificação da sessão (ex.: MS_1_SF), gerado na criação
    public string Codigo { get; set; } = string.Empty;

    public DateTime DataHora { get; set; }

    public string PacienteNome { get; set; } = string.Empty;

    // Iniciais exibidas no avatar da célula Paciente (mesmo padrão de "Meus Pacientes")
    public string Iniciais => PacienteIniciais.Calcular(PacienteNome);

    public int DuracaoMinutos { get; set; }

    // "Agendada", "Realizada" ou "Cancelada"
    public string Status { get; set; } = string.Empty;

    // Anotações Clínicas registradas nesta sessão, preenchido só onde a evolução é exibida
    // (ex.: aba "Histórico de Sessões" da Ficha do Paciente)
    public List<AnotacaoSessaoItemViewModel> Anotacoes { get; set; } = new();

    // Objetivos Terapêuticos trabalhados nesta sessão (via SessaoObjetivo), preenchido só onde exibido
    // (ex.: aba "Histórico de Sessões" da Ficha do Paciente)
    public List<ObjetivoTrabalhadoViewModel> ObjetivosTrabalhados { get; set; } = new();
}
