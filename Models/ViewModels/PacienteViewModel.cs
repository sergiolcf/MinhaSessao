using System.ComponentModel.DataAnnotations;

namespace MinhaSessao.Models.ViewModels;

public class PacienteViewModel
{
    public Guid? Id { get; set; }

    public Guid ProfissionalId { get; set; }

    [Required(ErrorMessage = "Informe o nome completo do paciente.")]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o telefone de contato.")]
    public string Telefone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail do paciente.")]
    [EmailAddress(ErrorMessage = "Insira um e-mail válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a data de nascimento.")]
    [DataType(DataType.Date)]
    public DateTime DataNascimento { get; set; }

    public string? Cpf { get; set; }

    public string? Sexo { get; set; }

    public string? ContatoEmergencia { get; set; }

    public string? Profissao { get; set; }
}
