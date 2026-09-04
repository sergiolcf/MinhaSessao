namespace MinhaSessao.Models.ViewModels;

public class PainelPacienteViewModel
{
    public Guid PacienteId { get; set; }

    public string NomeCompleto { get; set; } = string.Empty;

    public DateTime? ProximaSessaoDataHora { get; set; }

    public string? ProximaSessaoProfissionalNome { get; set; }

    public int TotalSessoesRealizadas { get; set; }

    public List<CombinadoAtivoViewModel> CombinadosAtivos { get; set; } = new();
}

public class CombinadoAtivoViewModel
{
    public string Descricao { get; set; } = string.Empty;

    public string ObjetivoTitulo { get; set; } = string.Empty;
}
