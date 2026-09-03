namespace MinhaSessao.Models.ViewModels;

public class PacienteListItemViewModel
{
    public Guid Id { get; set; }

    public string NomeCompleto { get; set; } = string.Empty;

    public string Telefone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime DataNascimento { get; set; }

    public bool Ativo { get; set; }

    // Calcula a idade a partir da data de nascimento, considerando se o aniversário já ocorreu este ano
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

    // Iniciais para o avatar (primeira letra do primeiro e do último nome)
    public string Iniciais => PacienteIniciais.Calcular(NomeCompleto);
}
