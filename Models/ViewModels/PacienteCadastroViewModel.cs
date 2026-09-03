using System.ComponentModel.DataAnnotations;

namespace MinhaSessao.Models.ViewModels;

public class PacienteCadastroViewModel
{
    [Required(ErrorMessage = "Informe o nome completo.")]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o telefone.")]
    public string Telefone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a data de nascimento.")]
    [DataType(DataType.Date)]
    public DateTime DataNascimento { get; set; }

    [Required(ErrorMessage = "Informe o CPF.")]
    public string Cpf { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres")]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a senha.")]
    [Compare("Senha", ErrorMessage = "As senhas não coincidem")]
    [DataType(DataType.Password)]
    public string ConfirmarSenha { get; set; } = string.Empty;
}
