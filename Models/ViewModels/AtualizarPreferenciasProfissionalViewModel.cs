using System.ComponentModel.DataAnnotations;

namespace MinhaSessao.Models.ViewModels;

public class AtualizarPreferenciasProfissionalViewModel
{
    [Required(ErrorMessage = "A duração padrão da sessão é obrigatória.")]
    [Range(1, 480, ErrorMessage = "Informe uma duração válida (em minutos).")]
    public int DuracaoPadraoSessaoMinutos { get; set; }

    [Required(ErrorMessage = "O valor padrão da consulta é obrigatório.")]
    [Range(0, 999999, ErrorMessage = "Informe um valor válido.")]
    public decimal ValorPadraoConsulta { get; set; }
}
