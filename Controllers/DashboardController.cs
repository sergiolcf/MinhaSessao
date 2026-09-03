using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinhaSessao.Data;
using MinhaSessao.Extensions;
using MinhaSessao.Models.ViewModels;
using MinhaSessao.Services;

namespace MinhaSessao.Controllers;

[Authorize(Roles = AutenticacaoService.PapelProfissional)]
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

    [HttpGet]
    public async Task<IActionResult> Configuracoes()
    {
        var profissionalId = User.ObterProfissionalId();
        var profissional = await _context.Profissionais.FirstOrDefaultAsync(p => p.Id == profissionalId);

        if (profissional is null)
        {
            return RedirectToAction("Login", "Account");
        }

        ViewBag.ProfissionalId = profissional.Id;
        ViewBag.ProfissionalNome = profissional.NomeCompleto;
        ViewBag.ProfissionalFotoUrl = profissional.FotoUrl;
        ViewBag.ProfissionalCRP = profissional.RegistroCRP;

        var model = new ConfiguracoesProfissionalViewModel
        {
            NomeCompleto = profissional.NomeCompleto,
            Email = profissional.Email,
            Telefone = profissional.Telefone,
            RegistroCRP = profissional.RegistroCRP,
            AbordagemEspecialidades = profissional.AbordagemEspecialidades,
            Apresentacao = profissional.Apresentacao,
            DuracaoPadraoSessaoMinutos = profissional.DuracaoPadraoSessaoMinutos > 0 ? profissional.DuracaoPadraoSessaoMinutos : 50,
            ValorPadraoConsulta = profissional.ValorPadraoConsulta
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AtualizarPerfil(AtualizarPerfilProfissionalViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = ObterPrimeiroErroModelState() });
        }

        var profissionalId = User.ObterProfissionalId();
        var profissional = await _context.Profissionais.FirstOrDefaultAsync(p => p.Id == profissionalId);

        if (profissional is null)
        {
            return Json(new { success = false, message = "Profissional não encontrado." });
        }

        var emailNormalizado = model.Email.Trim().ToLower();

        var emailEmUso = await _context.Profissionais
            .AnyAsync(p => p.Id != profissionalId && p.Email.ToLower() == emailNormalizado);

        if (emailEmUso)
        {
            return Json(new { success = false, message = "Este e-mail já está em uso por outro cadastro." });
        }

        profissional.NomeCompleto = model.NomeCompleto.Trim();
        profissional.Email = emailNormalizado;
        profissional.Telefone = model.Telefone.Trim();
        profissional.RegistroCRP = model.RegistroCRP.Trim();
        profissional.AbordagemEspecialidades = model.AbordagemEspecialidades?.Trim();
        profissional.Apresentacao = model.Apresentacao?.Trim();

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Perfil atualizado com sucesso!" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AtualizarPreferencias(AtualizarPreferenciasProfissionalViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = ObterPrimeiroErroModelState() });
        }

        var profissionalId = User.ObterProfissionalId();
        var profissional = await _context.Profissionais.FirstOrDefaultAsync(p => p.Id == profissionalId);

        if (profissional is null)
        {
            return Json(new { success = false, message = "Profissional não encontrado." });
        }

        profissional.DuracaoPadraoSessaoMinutos = model.DuracaoPadraoSessaoMinutos;
        profissional.ValorPadraoConsulta = model.ValorPadraoConsulta;

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Preferências atualizadas com sucesso!" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarSenha(AlterarSenhaProfissionalViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = ObterPrimeiroErroModelState() });
        }

        var profissionalId = User.ObterProfissionalId();
        var profissional = await _context.Profissionais.FirstOrDefaultAsync(p => p.Id == profissionalId);

        if (profissional is null)
        {
            return Json(new { success = false, message = "Profissional não encontrado." });
        }

        if (!AutenticacaoService.VerificarSenha(profissional, model.SenhaAtual))
        {
            return Json(new { success = false, message = "A senha atual informada está incorreta." });
        }

        profissional.Senha = AutenticacaoService.HashSenha(profissional, model.NovaSenha);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Senha alterada com sucesso!" });
    }

    private string ObterPrimeiroErroModelState()
    {
        return ModelState
            .Where(par => par.Value?.Errors.Count > 0)
            .SelectMany(par => par.Value!.Errors)
            .Select(erro => erro.ErrorMessage)
            .FirstOrDefault() ?? "Verifique os campos destacados.";
    }
}
