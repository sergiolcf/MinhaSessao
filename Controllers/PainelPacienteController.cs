using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinhaSessao.Data;
using MinhaSessao.Extensions;
using MinhaSessao.Models.ViewModels;
using MinhaSessao.Services;

namespace MinhaSessao.Controllers;

[Authorize(Roles = AutenticacaoService.PapelPaciente)]
public class PainelPacienteController : Controller
{
    private readonly ApplicationDbContext _context;

    public PainelPacienteController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var pacienteId = User.ObterPacienteId();
        var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.Id == pacienteId);

        if (paciente is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var model = new PainelPacienteViewModel
        {
            PacienteId = paciente.Id,
            NomeCompleto = paciente.NomeCompleto
        };

        ViewBag.PacienteId = paciente.Id;
        ViewBag.PacienteNome = paciente.NomeCompleto;

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Diretorio(string? busca)
    {
        var pacienteId = User.ObterPacienteId();
        var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.Id == pacienteId);

        if (paciente is null)
        {
            return RedirectToAction("Login", "Account");
        }

        ViewBag.PacienteId = paciente.Id;
        ViewBag.PacienteNome = paciente.NomeCompleto;

        var consulta = _context.Profissionais.AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termoBusca = busca.Trim().ToLower();
            consulta = consulta.Where(p => p.NomeCompleto.ToLower().Contains(termoBusca) || p.RegistroCRP.ToLower().Contains(termoBusca));
        }

        var profissionais = await consulta
            .OrderBy(p => p.NomeCompleto)
            .Select(p => new DiretorioProfissionalItemViewModel
            {
                NomeCompleto = p.NomeCompleto,
                RegistroCRP = p.RegistroCRP,
                Apresentacao = p.Apresentacao,
                Telefone = p.Telefone,
                FotoUrl = p.FotoUrl
            })
            .ToListAsync();

        var model = new DiretorioViewModel
        {
            Profissionais = profissionais,
            TermoBusca = busca
        };

        return View(model);
    }
}
