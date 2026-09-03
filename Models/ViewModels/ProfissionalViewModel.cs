using System.ComponentModel.DataAnnotations;

namespace MinhaSessao.Models.ViewModels;

public class ProfissionalViewModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "O nome completo é obrigatório.")]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "O CRP é obrigatório.")]
    public string RegistroCRP { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "O telefone é obrigatório.")]
    public string Telefone { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres")]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a senha.")]
    [Compare("Senha", ErrorMessage = "As senhas não coincidem")]
    [DataType(DataType.Password)]
    public string ConfirmarSenha { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "A apresentação deve ter no máximo 500 caracteres.")]
    public string? Apresentacao { get; set; }

    public IFormFile? Foto { get; set; }
}
