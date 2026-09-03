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
}
