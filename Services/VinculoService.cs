using Microsoft.EntityFrameworkCore;
using MinhaSessao.Data;
using MinhaSessao.Models.Entities;

namespace MinhaSessao.Services;

// Centraliza a checagem de posse (paciente <-> profissional) e a criação de vínculos,
// para nenhuma controller comparar ProfissionalId direto na entidade Paciente.
public class VinculoService
{
    private readonly ApplicationDbContext _context;

    public VinculoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> PacientePertenceAoProfissionalAsync(Guid pacienteId, Guid profissionalId)
    {
        return await _context.Vinculos.AnyAsync(v =>
            v.PacienteId == pacienteId &&
            v.ProfissionalId == profissionalId &&
            v.Status == StatusVinculo.Ativo);
    }

    public async Task<List<Paciente>> ObterPacientesAtivosAsync(Guid profissionalId)
    {
        return await _context.Vinculos
            .Where(v => v.ProfissionalId == profissionalId && v.Status == StatusVinculo.Ativo)
            .OrderBy(v => v.Paciente!.NomeCompleto)
            .Select(v => v.Paciente!)
            .ToListAsync();
    }

    // Não salva sozinho: quem chamar deve incluir na mesma unidade de trabalho (SaveChangesAsync) que grava o Paciente
    public void CriarVinculo(Guid pacienteId, Guid profissionalId, DateTime? dataInicio = null)
    {
        _context.Vinculos.Add(new VinculoPacienteProfissional
        {
            Id = Guid.NewGuid(),
            PacienteId = pacienteId,
            ProfissionalId = profissionalId,
            Status = StatusVinculo.Ativo,
            DataInicio = dataInicio ?? DateTime.UtcNow
        });
    }
}
