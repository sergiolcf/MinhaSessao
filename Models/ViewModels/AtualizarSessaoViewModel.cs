using System.ComponentModel.DataAnnotations;

namespace MinhaSessao.Models.ViewModels;

public class AtualizarSessaoViewModel
{
    [Required(ErrorMessage = "Sessão inválida.")]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Informe a data e hora da sessão.")]
    public DateTime DataHora { get; set; }

    [Range(1, 600, ErrorMessage = "Informe uma duração válida (em minutos).")]
    public int DuracaoMinutos { get; set; }

    [Required(ErrorMessage = "Selecione o status da sessão.")]
    public string Status { get; set; } = string.Empty;

    public string? AnotacoesClinicas { get; set; }
}
