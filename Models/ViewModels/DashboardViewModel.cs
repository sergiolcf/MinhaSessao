namespace MinhaSessao.Models.ViewModels;

public class DashboardViewModel
{
    public Guid ProfissionalId { get; set; }

    public string NomeCompleto { get; set; } = string.Empty;

    public string RegistroCRP { get; set; } = string.Empty;

    public string? FotoUrl { get; set; }

    public int SessoesHoje { get; set; }

    public int PacientesAtivos { get; set; }

    public int AtendimentosNoMes { get; set; }

    public int SessoesCanceladasMes { get; set; }

    public List<SessaoProfissionalListItemViewModel> AtendimentosDeHoje { get; set; } = new();

    // Usados pela partial _ModalNovaSessao, reaproveitada aqui para o atalho "Agendar Sessão"
    public List<PacienteSelectItemViewModel> Pacientes { get; set; } = new();

    public int DuracaoPadraoSessaoMinutos { get; set; }
}
