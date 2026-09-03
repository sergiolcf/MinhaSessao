using System.ComponentModel.DataAnnotations;

namespace MinhaSessao.Models.ViewModels;

public class AlterarSenhaPacienteViewModel
{
    [Required(ErrorMessage = "Informe a senha atual.")]
    public string SenhaAtual { get; set; } = string.Empty;

    [Required(ErrorMessage = "A nova senha é obrigatória.")]
    [MinLength(6, ErrorMessage = "A nova senha deve ter no mínimo 6 caracteres")]
    [DataType(DataType.Password)]
    public string NovaSenha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a nova senha.")]
    [Compare("NovaSenha", ErrorMessage = "As senhas não coincidem")]
    [DataType(DataType.Password)]
    public string ConfirmarNovaSenha { get; set; } = string.Empty;
}
