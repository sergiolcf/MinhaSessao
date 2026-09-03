namespace MinhaSessao.Models.ViewModels;

public class PacienteDetalhesViewModel
{
    public Guid Id { get; set; }

    public string NomeCompleto { get; set; } = string.Empty;

    public string? Cpf { get; set; }

    public string Telefone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime DataNascimento { get; set; }

    public string? Sexo { get; set; }

    public string? ContatoEmergencia { get; set; }

    public string? Profissao { get; set; }

    public bool Ativo { get; set; }

    public DateTime DataCadastro { get; set; }

    public List<AnotacaoConfidencialItemViewModel> Anotacoes { get; set; } = new();

    public int PaginaAtualAnotacoes { get; set; } = 1;

    public int TotalPaginasAnotacoes { get; set; } = 1;

    public string Iniciais => PacienteIniciais.Calcular(NomeCompleto);

    public int Idade
    {
        get
        {
            var hoje = DateTime.Today;
            var idade = hoje.Year - DataNascimento.Year;
            if (DataNascimento.Date > hoje.AddYears(-idade))
            {
                idade--;
            }

            return idade;
        }
    }
}
