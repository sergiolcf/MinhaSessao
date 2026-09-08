namespace MinhaSessao.Models.ViewModels;

public class PacientesIndexViewModel
{
    public List<PacienteListItemViewModel> Pacientes { get; set; } = new();

    public int PaginaAtual { get; set; } = 1;

    public int TotalPaginas { get; set; } = 1;
}
