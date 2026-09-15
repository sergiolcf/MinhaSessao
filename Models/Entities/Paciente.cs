namespace MinhaSessao.Models.Entities;

public class Paciente
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string NomeCompleto { get; set; } = string.Empty;

    public string? Cpf { get; set; }

    public string Telefone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // Armazena o hash da senha temporária (gerada automaticamente no cadastro ou ao regenerar), nunca o texto puro
    public string Senha { get; set; } = string.Empty;

    public DateTime DataNascimento { get; set; }

    public string? Sexo { get; set; }

    public string? ContatoEmergencia { get; set; }

    public string? Profissao { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

    // Suporte a "Paciente com Responsável" (CARD-35), pra atender menores de idade: quando true, o CPF
    // do paciente vira opcional e os dados abaixo do responsável passam a ser exigidos no cadastro
    public bool NecessitaResponsavel { get; set; }

    public string? NomeResponsavel { get; set; }

    public string? TelefoneResponsavel { get; set; }

    public string? CpfResponsavel { get; set; }

    public string? ProfissaoResponsavel { get; set; }
}
