namespace MinhaSessao.Models.ViewModels;

// Anotação clínica de uma sessão, agregada por Paciente (não por Sessão) — usada na aba
// "Anotações Clínicas" da Ficha do Paciente, que reúne num só lugar o que hoje só dá pra ver
// entrando sessão por sessão (Views/Sessoes/Sessao.cshtml)
public class AnotacaoClinicaListItemViewModel
{
    public Guid Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Conteudo { get; set; } = string.Empty;

    public DateTime DataRegistro { get; set; }

    public List<string> Objetivos { get; set; } = new();

    public Guid SessaoId { get; set; }

    public string SessaoCodigo { get; set; } = string.Empty;

    public DateTime SessaoDataHora { get; set; }
}
