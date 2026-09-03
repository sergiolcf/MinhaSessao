using System.ComponentModel.DataAnnotations;

namespace MinhaSessao.Models.ViewModels;

public class AnotacaoConfidencialEditarViewModel
{
    [Required(ErrorMessage = "Anotação inválida.")]
    public Guid Id { get; set; }

    public string? Titulo { get; set; }

    [Required(ErrorMessage = "Escreva o conteúdo da anotação.")]
    public string Conteudo { get; set; } = string.Empty;
}
