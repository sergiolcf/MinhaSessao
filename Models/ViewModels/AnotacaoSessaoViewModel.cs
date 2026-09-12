using System.ComponentModel.DataAnnotations;

namespace MinhaSessao.Models.ViewModels;

// Postado pela tela Views/Sessoes/Sessao.cshtml pra criar uma nova anotação clínica da sessão
public class AnotacaoSessaoViewModel
{
    [Required(ErrorMessage = "Sessão inválida.")]
    public Guid SessaoId { get; set; }

    [Required(ErrorMessage = "Informe um título para a anotação.")]
    [StringLength(150, ErrorMessage = "O título deve ter no máximo 150 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escreva o conteúdo da anotação.")]
    public string Conteudo { get; set; } = string.Empty;

    // Objetivos Terapêuticos trabalhados especificamente nesta anotação
    public List<SessaoObjetivoViewModel>? Objetivos { get; set; }
}
