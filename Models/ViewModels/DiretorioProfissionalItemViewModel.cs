namespace MinhaSessao.Models.ViewModels;

public class DiretorioProfissionalItemViewModel
{
    public string NomeCompleto { get; set; } = string.Empty;

    public string RegistroCRP { get; set; } = string.Empty;

    public string? Apresentacao { get; set; }

    public string Telefone { get; set; } = string.Empty;

    public string? FotoUrl { get; set; }

    // Telefone só com dígitos (com DDI 55), pronto para montar o link https://wa.me/...
    public string TelefoneWhatsApp => "55" + new string(Telefone.Where(char.IsDigit).ToArray());
}
