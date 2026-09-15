using System.ComponentModel.DataAnnotations;
using MinhaSessao.Helpers;

namespace MinhaSessao.Models.ViewModels;

// Implementa IValidatableObject pra validação condicional entre CPF do Paciente (opcional quando há
// Responsável) e os dados do Responsável (obrigatórios só quando NecessitaResponsavel é true) — ver
// "Pacientes" no CLAUDE.md, suporte a "Paciente com Responsável" pra atender menores de idade (CARD-35).
public class PacienteViewModel : IValidatableObject
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Informe o nome completo do paciente.")]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o telefone de contato.")]
    public string Telefone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail do paciente.")]
    [EmailAddress(ErrorMessage = "Insira um e-mail válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a data de nascimento.")]
    [DataType(DataType.Date)]
    public DateTime DataNascimento { get; set; }

    // Não é mais [Required]: quando há Responsável, o CPF do paciente é opcional — a obrigatoriedade
    // condicional (exigido só quando NecessitaResponsavel é false) é tratada em Validate() abaixo.
    // [CpfValido] já trata string vazia como válida (quem cuida da ausência é a regra condicional).
    [CpfValido(ErrorMessage = "Informe um CPF válido.")]
    public string Cpf { get; set; } = string.Empty;

    public string? Sexo { get; set; }

    public string? ContatoEmergencia { get; set; }

    public string? Profissao { get; set; }

    public bool NecessitaResponsavel { get; set; }

    public string? NomeResponsavel { get; set; }

    public string? TelefoneResponsavel { get; set; }

    [CpfValido(ErrorMessage = "Informe um CPF do responsável válido.")]
    public string? CpfResponsavel { get; set; }

    // Opcional, mesmo padrão da Profissão do próprio paciente (ex.: menor aprendiz)
    public string? ProfissaoResponsavel { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NecessitaResponsavel)
        {
            if (string.IsNullOrWhiteSpace(NomeResponsavel))
            {
                yield return new ValidationResult("Informe o nome do responsável.", new[] { nameof(NomeResponsavel) });
            }

            if (string.IsNullOrWhiteSpace(TelefoneResponsavel))
            {
                yield return new ValidationResult("Informe o telefone do responsável.", new[] { nameof(TelefoneResponsavel) });
            }

            if (string.IsNullOrWhiteSpace(CpfResponsavel))
            {
                yield return new ValidationResult("Informe o CPF do responsável.", new[] { nameof(CpfResponsavel) });
            }
        }
        else if (string.IsNullOrWhiteSpace(Cpf))
        {
            yield return new ValidationResult("Informe o CPF do paciente.", new[] { nameof(Cpf) });
        }
    }
}
