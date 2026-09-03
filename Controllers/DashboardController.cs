using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinhaSessao.Data;
using MinhaSessao.Extensions;
using MinhaSessao.Models.ViewModels;

namespace MinhaSessao.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var profissionalId = User.ObterProfissionalId();
        var profissional = await _context.Profissionais.FirstOrDefaultAsync(p => p.Id == profissionalId);

        if (profissional is null)
        {
            return RedirectToAction("Login", "Account");
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
