namespace MinhaSessao.Models.ViewModels;

public class PainelAgendaProfissionalViewModel
{
    public int SessoesSemana { get; set; }

    public int SessoesMes { get; set; }

    public int SessoesCanceladasMes { get; set; }

    // Usados só pela partial _ModalNovaSessao (criar sessão direto de um dia da grade)
    public List<PacienteSelectItemViewModel> Pacientes { get; set; } = new();

    public int DuracaoPadraoSessaoMinutos { get; set; }
}
