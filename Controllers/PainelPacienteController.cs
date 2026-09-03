using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinhaSessao.Data;
using MinhaSessao.Extensions;
using MinhaSessao.Models.Entities;
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

    // Rota descontinuada: "Diretório de Profissionais" virou a aba "Todos os Profissionais" dentro de Profissionais()
    [HttpGet]
    public IActionResult Diretorio(string? busca)
    {
        return RedirectToAction(nameof(Profissionais), new { aba = "todos", busca });
    }

    [HttpGet]
    public async Task<IActionResult> Profissionais(string? aba, string? busca)
    {
        var pacienteId = User.ObterPacienteId();
        var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.Id == pacienteId);

        if (paciente is null)
        {
            return RedirectToAction("Login", "Account");
        }

        ViewBag.PacienteId = paciente.Id;
        ViewBag.PacienteNome = paciente.NomeCompleto;

        var model = new PainelProfissionaisViewModel
        {
            MeusProfissionais = await ObterMeusProfissionaisAsync(pacienteId),
            TodosProfissionais = await ObterTodosProfissionaisAsync(busca),
            AbaInicial = string.Equals(aba, "todos", StringComparison.OrdinalIgnoreCase) ? "todos" : "meus",
            TermoBusca = busca
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> BuscarProfissionais(string? busca)
    {
        var profissionais = await ObterTodosProfissionaisAsync(busca);
        return Json(new { success = true, profissionais });
    }

    [HttpGet]
    public async Task<IActionResult> DetalhesProfissional(Guid id)
    {
        var profissional = await _context.Profissionais.FirstOrDefaultAsync(p => p.Id == id);

        if (profissional is null)
        {
            return Json(new { success = false, message = "Profissional não encontrado." });
        }

        return Json(new
        {
            success = true,
            nomeCompleto = profissional.NomeCompleto,
            registroCRP = profissional.RegistroCRP,
            email = profissional.Email,
            telefone = profissional.Telefone,
            apresentacao = profissional.Apresentacao,
            fotoUrl = profissional.FotoUrl,
            iniciais = PacienteIniciais.Calcular(profissional.NomeCompleto)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Configuracoes()
    {
        var pacienteId = User.ObterPacienteId();
        var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.Id == pacienteId);

        if (paciente is null)
        {
            return RedirectToAction("Login", "Account");
        }

        ViewBag.PacienteId = paciente.Id;
        ViewBag.PacienteNome = paciente.NomeCompleto;

        var model = new ConfiguracoesPacienteViewModel
        {
            NomeCompleto = paciente.NomeCompleto,
            Email = paciente.Email,
            Telefone = paciente.Telefone,
            CpfFormatado = CpfUtil.Formatar(paciente.Cpf)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AtualizarDados(AtualizarDadosPacienteViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = ObterPrimeiroErroModelState() });
        }

        var pacienteId = User.ObterPacienteId();
        var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.Id == pacienteId);

        if (paciente is null)
        {
            return Json(new { success = false, message = "Paciente não encontrado." });
        }

        var emailNormalizado = model.Email.Trim().ToLower();

        var emailEmUso = await _context.Pacientes
            .AnyAsync(p => p.Id != pacienteId && p.Email.ToLower() == emailNormalizado);

        if (emailEmUso)
        {
            return Json(new { success = false, message = "Este e-mail já está em uso por outro cadastro." });
        }

        paciente.NomeCompleto = model.NomeCompleto.Trim();
        paciente.Email = emailNormalizado;
        paciente.Telefone = model.Telefone.Trim();

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Dados atualizados com sucesso!" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarSenha(AlterarSenhaPacienteViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = ObterPrimeiroErroModelState() });
        }

        var pacienteId = User.ObterPacienteId();
        var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.Id == pacienteId);

        if (paciente is null)
        {
            return Json(new { success = false, message = "Paciente não encontrado." });
        }

        if (!AutenticacaoService.VerificarSenhaPaciente(paciente, model.SenhaAtual))
        {
            return Json(new { success = false, message = "A senha atual informada está incorreta." });
        }

        paciente.Senha = AutenticacaoService.HashSenhaPaciente(paciente, model.NovaSenha);
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

    private async Task<List<ProfissionalListaItemViewModel>> ObterMeusProfissionaisAsync(Guid pacienteId)
    {
        return await _context.Vinculos
            .Where(v => v.PacienteId == pacienteId)
            .OrderByDescending(v => v.Status == StatusVinculo.Ativo)
            .ThenBy(v => v.Profissional!.NomeCompleto)
            .Select(v => new ProfissionalListaItemViewModel
            {
                Id = v.ProfissionalId,
                NomeCompleto = v.Profissional!.NomeCompleto,
                RegistroCRP = v.Profissional!.RegistroCRP,
                Apresentacao = v.Profissional!.Apresentacao,
                FotoUrl = v.Profissional!.FotoUrl,
                VinculoAtivo = v.Status == StatusVinculo.Ativo
            })
            .ToListAsync();
    }

    private async Task<List<ProfissionalListaItemViewModel>> ObterTodosProfissionaisAsync(string? busca)
    {
        var consulta = _context.Profissionais.AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termoBusca = busca.Trim().ToLower();
            consulta = consulta.Where(p => p.NomeCompleto.ToLower().Contains(termoBusca) || p.RegistroCRP.ToLower().Contains(termoBusca));
        }

        return await consulta
            .OrderBy(p => p.NomeCompleto)
            .Select(p => new ProfissionalListaItemViewModel
            {
                Id = p.Id,
                NomeCompleto = p.NomeCompleto,
                RegistroCRP = p.RegistroCRP,
                Apresentacao = p.Apresentacao,
                FotoUrl = p.FotoUrl
            })
            .ToListAsync();
    }
}
