namespace MinhaSessao.Models.ViewModels;

public class PacienteSelectItemViewModel
{
    public Guid Id { get; set; }

    public string NomeCompleto { get; set; } = string.Empty;

    public string CpfFormatado { get; set; } = string.Empty;
}
