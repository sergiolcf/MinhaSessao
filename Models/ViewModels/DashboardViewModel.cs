namespace MinhaSessao.Models.ViewModels;

public class DashboardViewModel
{
    public Guid ProfissionalId { get; set; }

    public string NomeCompleto { get; set; } = string.Empty;

    public string RegistroCRP { get; set; } = string.Empty;

    public string? FotoUrl { get; set; }
}
