using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinhaSessao.Data;
using MinhaSessao.Models.ViewModels;

namespace MinhaSessao.Controllers;

public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(Guid? profissionalId)
    {
        var profissional = profissionalId.HasValue
            ? await _context.Profissionais.FirstOrDefaultAsync(p => p.Id == profissionalId.Value)
            : await _context.Profissionais.FirstOrDefaultAsync();

        if (profissional is null)
        {
            return RedirectToAction("Index", "Home");
        }

        var model = new DashboardViewModel
        {
            ProfissionalId = profissional.Id,
            NomeCompleto = profissional.NomeCompleto,
            RegistroCRP = profissional.RegistroCRP,
            FotoUrl = profissional.FotoUrl
        };

        ViewBag.ProfissionalId = profissional.Id;
        ViewBag.ProfissionalNome = profissional.NomeCompleto;
        ViewBag.ProfissionalFotoUrl = profissional.FotoUrl;
        ViewBag.ProfissionalCRP = profissional.RegistroCRP;

        return View(model);
    }
}
