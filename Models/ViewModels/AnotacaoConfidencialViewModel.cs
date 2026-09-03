using System.ComponentModel.DataAnnotations;

namespace MinhaSessao.Models.ViewModels;

public class AnotacaoConfidencialViewModel
{
    [Required(ErrorMessage = "Paciente inválido.")]
    public Guid PacienteId { get; set; }

    public string? Titulo { get; set; }

    [Required(ErrorMessage = "Escreva o conteúdo da anotação.")]
    public string Conteudo { get; set; } = string.Empty;
}
