namespace MinhaSessao.Models.ViewModels;

// Anotação clínica de uma sessão, usada tanto na tela dedicada (Views/Sessoes/Sessao.cshtml)
// quanto nos dois previews somente-leitura que listam anotações por sessão (Histórico de Sessões
// da Ficha do Paciente e o popup "Visualizar Sessão" do Plano de Tratamento)
public class AnotacaoSessaoItemViewModel
{
    public Guid Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Conteudo { get; set; } = string.Empty;

    public DateTime DataRegistro { get; set; }
}
