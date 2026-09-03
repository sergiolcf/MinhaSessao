namespace MinhaSessao.Models.ViewModels;

public class ConfiguracoesProfissionalViewModel
{
    public string NomeCompleto { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Telefone { get; set; } = string.Empty;

    public string RegistroCRP { get; set; } = string.Empty;

    public string? AbordagemEspecialidades { get; set; }

    public string? Apresentacao { get; set; }

    public int DuracaoPadraoSessaoMinutos { get; set; }

    public decimal ValorPadraoConsulta { get; set; }
}
