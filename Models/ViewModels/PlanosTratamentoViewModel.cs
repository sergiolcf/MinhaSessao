namespace MinhaSessao.Models.ViewModels;

// Estrutura da tela "Planos de Tratamento" (Objetivos Terapêuticos de TODOS os pacientes do
// profissional, numa lista só) — a lista de objetivos em si é 100% carregada via AJAX
// (PlanosTratamentoController.Buscar), mesmo padrão já usado na aba "Plano de Tratamento" da Ficha
// do Paciente (lá é só de um paciente por vez). Pacientes aqui alimenta tanto o <select> de filtro
// quanto o <select> de Paciente (obrigatório) do modal "Novo Objetivo".
public class PlanosTratamentoViewModel
{
    public List<PacienteSelectItemViewModel> Pacientes { get; set; } = new();
}
