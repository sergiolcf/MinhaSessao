using System.ComponentModel.DataAnnotations;

namespace MinhaSessao.Models.Entities;

public class Profissional
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

    // Armazena o hash da senha (gerado via PasswordHasher), nunca o texto puro
    public string Senha { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "A apresentação deve ter no máximo 500 caracteres.")]
    public string? Apresentacao { get; set; }

    [StringLength(200, ErrorMessage = "A abordagem/especialidades deve ter no máximo 200 caracteres.")]
    public string? AbordagemEspecialidades { get; set; }

    public string? FotoUrl { get; set; }

    // Preferências da clínica, usadas como padrão ao agendar novas sessões
    public int DuracaoPadraoSessaoMinutos { get; set; } = 50;

    public decimal ValorPadraoConsulta { get; set; }
}
