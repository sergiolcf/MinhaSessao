using System.ComponentModel.DataAnnotations;

namespace MinhaSessao.Models.ViewModels;

public class AtualizarPerfilProfissionalViewModel
{
    [Required(ErrorMessage = "O nome completo é obrigatório.")]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "O telefone é obrigatório.")]
    public string Telefone { get; set; } = string.Empty;

    [Required(ErrorMessage = "O CRP é obrigatório.")]
    public string RegistroCRP { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "A abordagem/especialidades deve ter no máximo 200 caracteres.")]
    public string? AbordagemEspecialidades { get; set; }

    [StringLength(500, ErrorMessage = "A apresentação deve ter no máximo 500 caracteres.")]
    public string? Apresentacao { get; set; }
}
