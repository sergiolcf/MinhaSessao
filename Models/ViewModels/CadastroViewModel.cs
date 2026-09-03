namespace MinhaSessao.Models.ViewModels;

// Só usado para RENDERIZAR a tela única de Cadastro (abas Profissional/Paciente) — cada aba é
// postada separadamente para AccountController.CadastroProfissional/CadastroPaciente.
public class CadastroViewModel
{
    public string PerfilAtivo { get; set; } = "profissional";

    public ProfissionalViewModel Profissional { get; set; } = new();

    public PacienteCadastroViewModel Paciente { get; set; } = new();
}
