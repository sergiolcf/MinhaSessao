using System.ComponentModel.DataAnnotations;
using MinhaSessao.Helpers;

namespace MinhaSessao.Models.ViewModels;

public class PacienteCadastroViewModel
{
    [Required(ErrorMessage = "Informe o nome completo.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome completo deve ter entre 3 e 100 caracteres.")]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ]+(\s+[A-Za-zÀ-ÖØ-öø-ÿ]+)+$", ErrorMessage = "Informe nome e sobrenome, usando apenas letras.")]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o telefone.")]
    [RegularExpression(@"^(\(\d{2}\)\s?\d{4,5}-\d{4}|\d{10,11})$", ErrorMessage = "Informe um telefone válido com DDD: fixo (10 dígitos) ou celular (11 dígitos).")]
    public string Telefone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a data de nascimento.")]
    [DataType(DataType.Date)]
    [MaiorDeIdade(18, ErrorMessage = "Para se cadastrar sozinho, você deve ter no mínimo 18 anos.")]
    public DateTime DataNascimento { get; set; }

    [Required(ErrorMessage = "Informe o CPF.")]
    [RegularExpression(@"^(\d{3}\.\d{3}\.\d{3}-\d{2}|\d{11})$", ErrorMessage = "Informe um CPF válido no formato 000.000.000-00.")]
    [CpfValido(ErrorMessage = "Este CPF não é válido.")]
    public string Cpf { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [MinLength(8, ErrorMessage = "A senha deve ter no mínimo 8 caracteres.")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$", ErrorMessage = "A senha deve conter ao menos 1 letra maiúscula, 1 número e 1 caractere especial.")]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a senha.")]
    [Compare("Senha", ErrorMessage = "As senhas não coincidem")]
    [DataType(DataType.Password)]
    public string ConfirmarSenha { get; set; } = string.Empty;
}
