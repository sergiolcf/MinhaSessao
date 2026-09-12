namespace MinhaSessao.Models.ViewModels;

// Alimenta a tela dedicada de conteúdo clínico da sessão (Views/Sessoes/Sessao.cshtml) —
// separada do modal "Editar Sessão", que continua sendo quem edita Data/Duração/Status
public class SessaoDetalheViewModel
{
    public Guid Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public DateTime DataHora { get; set; }

    public int DuracaoMinutos { get; set; }

    public string Status { get; set; } = string.Empty;

    public Guid PacienteId { get; set; }

    public string PacienteNome { get; set; } = string.Empty;

    public string PacienteIniciais { get; set; } = string.Empty;

    // Anotações Clínicas não vêm mais no SSR — a lista é buscada via AJAX (ListarAnotacoesSessao),
    // mesmo espírito da aba "Objetivos Trabalhados" do Plano de Tratamento
    public List<ObjetivoAtivoSessaoViewModel> ObjetivosAtivos { get; set; } = new();
}

// Objetivo em andamento do paciente — universo disponível pra marcar em qualquer anotação desta
// sessão (quais já estão marcados é decidido por anotação, ver AnotacaoSessaoViewModel.Objetivos)
public class ObjetivoAtivoSessaoViewModel
{
    public Guid Id { get; set; }

    public string Titulo { get; set; } = string.Empty;
}
