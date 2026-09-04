using System.ComponentModel.DataAnnotations;

namespace MinhaSessao.Models.ViewModels;

// Criação de um Objetivo Terapêutico já com os Combinados juntos (Proposta A: tudo em uma tela)
public class ObjetivoTerapeuticoViewModel
{
    [Required(ErrorMessage = "Paciente inválido.")]
    public Guid PacienteId { get; set; }

    [Required(ErrorMessage = "Informe o título do objetivo.")]
    public string Titulo { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public List<string> Combinados { get; set; } = new List<string>();
}
