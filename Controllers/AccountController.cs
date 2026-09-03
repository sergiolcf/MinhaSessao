using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinhaSessao.Data;
using MinhaSessao.Models.ViewModels;
using MinhaSessao.Services;

namespace MinhaSessao.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountController> _logger;

    public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", User.IsInRole(AutenticacaoService.PapelPaciente) ? "PainelPaciente" : "Dashboard");
        }

        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var emailNormalizado = model.Email.Trim().ToLower();
        var senhaInformada = model.Senha.Trim();

        // O e-mail pode pertencer a um Profissional ou a um Paciente; identifica antes de autenticar
        var profissional = await _context.Profissionais
            .FirstOrDefaultAsync(p => p.Email.ToLower() == emailNormalizado);

        if (profissional is not null && AutenticacaoService.VerificarSenha(profissional, senhaInformada))
        {
            await AutenticacaoService.AutenticarProfissionalAsync(HttpContext, profissional, model.LembrarMe);
            return RedirectToAction("Index", "Dashboard");
        }

        var paciente = await _context.Pacientes
            .FirstOrDefaultAsync(p => p.Email.ToLower() == emailNormalizado);

        if (paciente is not null && AutenticacaoService.VerificarSenhaPaciente(paciente, senhaInformada))
        {
            await AutenticacaoService.AutenticarPacienteAsync(HttpContext, paciente, model.LembrarMe);
            return RedirectToAction("Index", "PainelPaciente");
        }

        ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
        return View(model);
    }

    // Ação rápida de desenvolvimento: loga automaticamente com o primeiro profissional cadastrado
    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> LoginSimuladoTeste()
    {
        var profissional = await _context.Profissionais.FirstOrDefaultAsync();

        if (profissional is null)
        {
            ModelState.AddModelError(string.Empty, "Nenhum profissional cadastrado para o login de teste.");
            return View("Login", new LoginViewModel());
        }

        await AutenticacaoService.AutenticarProfissionalAsync(HttpContext, profissional);

        return RedirectToAction("Index", "Dashboard");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
