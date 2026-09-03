namespace MinhaSessao.Models.Entities;

// Vínculo N:N entre Paciente e Profissional — substitui a antiga FK direta
// Paciente.ProfissionalId, permitindo que um paciente tenha vários profissionais ao longo do tempo.
public class VinculoPacienteProfissional
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PacienteId { get; set; }

    public Guid ProfissionalId { get; set; }

    public StatusVinculo Status { get; set; } = StatusVinculo.Ativo;

    public DateTime DataInicio { get; set; } = DateTime.UtcNow;

    public DateTime? DataFim { get; set; }

    // Propriedades de navegação (EF Core)
    public Paciente? Paciente { get; set; }

    public Profissional? Profissional { get; set; }
}
