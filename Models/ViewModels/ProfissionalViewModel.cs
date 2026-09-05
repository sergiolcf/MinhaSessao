using System.ComponentModel.DataAnnotations;
using MinhaSessao.Helpers;

namespace MinhaSessao.Models.ViewModels;

public class ProfissionalViewModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "O nome completo é obrigatório.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome completo deve ter entre 3 e 100 caracteres.")]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ]+(\s+[A-Za-zÀ-ÖØ-öø-ÿ]+)+$", ErrorMessage = "Informe nome e sobrenome, usando apenas letras.")]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "O CRP é obrigatório.")]
    [RegularExpression(@"^\d{2}/\d{6}$", ErrorMessage = "Informe o CRP no formato 00/000000.")]
    public string RegistroCRP { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "O telefone é obrigatório.")]
    [RegularExpression(@"^(\(\d{2}\)\s?\d{4,5}-\d{4}|\d{10,11})$", ErrorMessage = "Informe um telefone válido com DDD: fixo (10 dígitos) ou celular (11 dígitos).")]
    public string Telefone { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [MinLength(8, ErrorMessage = "A senha deve ter no mínimo 8 caracteres.")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$", ErrorMessage = "A senha deve conter ao menos 1 letra maiúscula, 1 número e 1 caractere especial.")]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a senha.")]
    [Compare("Senha", ErrorMessage = "As senhas não coincidem")]
    [DataType(DataType.Password)]
    public string ConfirmarSenha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Conte um pouco sobre você.")]
    [StringLength(500, MinimumLength = 30, ErrorMessage = "A apresentação deve ter entre 30 e 500 caracteres.")]
    public string Apresentacao { get; set; } = string.Empty;

    // Mensagem de erro é montada dinamicamente pelo atributo (distingue tamanho de extensão inválida)
    [ArquivoValido(2 * 1024 * 1024, new[] { ".jpg", ".jpeg", ".png" })]
    public IFormFile? Foto { get; set; }
}
