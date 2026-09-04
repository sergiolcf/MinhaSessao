using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinhaSessao.Data;
using MinhaSessao.Extensions;
using MinhaSessao.Models.Entities;
using MinhaSessao.Models.ViewModels;
using MinhaSessao.Services;

namespace MinhaSessao.Controllers;

[Authorize(Roles = AutenticacaoService.PapelProfissional)]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly VinculoService _vinculoService;

    public DashboardController(ApplicationDbContext context, VinculoService vinculoService)
    {
        _context = context;
        _vinculoService = vinculoService;
    }

    public async Task<IActionResult> Index()
    {
        var profissionalId = User.ObterProfissionalId();
        var profissional = await _context.Profissionais.FirstOrDefaultAsync(p => p.Id == profissionalId);

        if (profissional is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var agora = DateTime.UtcNow;
        var hoje = agora.Date;
        var consultaBase = _context.Sessoes.Where(s => s.ProfissionalId == profissionalId);

        var sessoesHoje = await consultaBase.CountAsync(s => s.Status == StatusSessao.Agendada && s.DataHora.Date == hoje);

        var atendimentosNoMes = await consultaBase.CountAsync(s => s.Status == StatusSessao.Realizada
            && s.DataHora.Year == hoje.Year && s.DataHora.Month == hoje.Month);

        var sessoesCanceladasMes = await consultaBase.CountAsync(s => s.Status == StatusSessao.Cancelada
            && s.DataHora.Year == hoje.Year && s.DataHora.Month == hoje.Month);

        var atendimentosDeHoje = await consultaBase
            .Where(s => s.DataHora.Date == hoje)
            .OrderBy(s => s.DataHora)
            .Select(s => new SessaoProfissionalListItemViewModel
            {
                Id = s.Id,
                PacienteId = s.PacienteId,
                DataHora = s.DataHora,
                PacienteNome = s.Paciente!.NomeCompleto,
                DuracaoMinutos = s.DuracaoMinutos,
                Status = s.Status.ToString()
            })
            .ToListAsync();

        var pacientesAtivos = await _vinculoService.ObterPacientesAtivosAsync(profissionalId);

        // Duração padrão do profissional é 0 apenas para registros criados antes da migration de Configurações — trata como 50
        var duracaoPadrao = profissional.DuracaoPadraoSessaoMinutos == 0 ? 50 : profissional.DuracaoPadraoSessaoMinutos;

        var model = new DashboardViewModel
        {
            ProfissionalId = profissional.Id,
            NomeCompleto = profissional.NomeCompleto,
            RegistroCRP = profissional.RegistroCRP,
            FotoUrl = profissional.FotoUrl,
            SessoesHoje = sessoesHoje,
            PacientesAtivos = pacientesAtivos.Count,
            AtendimentosNoMes = atendimentosNoMes,
            SessoesCanceladasMes = sessoesCanceladasMes,
            AtendimentosDeHoje = atendimentosDeHoje,
            Pacientes = pacientesAtivos
                .Select(p => new PacienteSelectItemViewModel { Id = p.Id, NomeCompleto = p.NomeCompleto, CpfFormatado = CpfUtil.Formatar(p.Cpf) })
                .ToList(),
            DuracaoPadraoSessaoMinutos = duracaoPadrao
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
