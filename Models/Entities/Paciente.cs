namespace MinhaSessao.Models.Entities;

public class Paciente
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // FK do Profissional responsável pelo paciente
    public Guid ProfissionalId { get; set; }

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

    // Propriedade de navegação (EF Core)
    public Profissional? Profissional { get; set; }
}
