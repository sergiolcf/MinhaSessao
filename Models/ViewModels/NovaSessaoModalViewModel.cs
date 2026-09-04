namespace MinhaSessao.Models.ViewModels;

// Dados que a partial _ModalNovaSessao precisa pra se renderizar — usada tanto em Sessoes/Index
// quanto em Agenda/Index, então fica fora dos ViewModels específicos de cada tela
public class NovaSessaoModalViewModel
{
    public List<PacienteSelectItemViewModel> Pacientes { get; set; } = new();

    public int DuracaoPadraoSessaoMinutos { get; set; }
}
