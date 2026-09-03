namespace MinhaSessao.Models.ViewModels;

public class PacientesIndexViewModel
{
    public Guid ProfissionalId { get; set; }

    public List<PacienteListItemViewModel> Pacientes { get; set; } = new();
}
