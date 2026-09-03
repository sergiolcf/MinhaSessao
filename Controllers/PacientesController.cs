using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinhaSessao.Data;
using MinhaSessao.Models.Entities;
using MinhaSessao.Models.ViewModels;

namespace MinhaSessao.Controllers;

public class PacientesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PacientesController> _logger;

    public PacientesController(ApplicationDbContext context, ILogger<PacientesController> logger)
    {
        _context = context;
        _logger = logger;
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

        var pacientes = await _context.Pacientes
            .Where(p => p.ProfissionalId == profissional.Id)
            .OrderBy(p => p.NomeCompleto)
            .Select(p => new PacienteListItemViewModel
            {
                Id = p.Id,
                NomeCompleto = p.NomeCompleto,
                Telefone = p.Telefone,
                Email = p.Email,
                DataNascimento = p.DataNascimento,
                Ativo = p.Ativo
            })
            .ToListAsync();

        ViewBag.ProfissionalId = profissional.Id;
        ViewBag.ProfissionalNome = profissional.NomeCompleto;
        ViewBag.ProfissionalFotoUrl = profissional.FotoUrl;
        ViewBag.ProfissionalCRP = profissional.RegistroCRP;

        var model = new PacientesIndexViewModel
        {
            ProfissionalId = profissional.Id,
            Pacientes = pacientes
        };

        return View(model);
    }

    public async Task<IActionResult> Detalhes(Guid id, Guid profissionalId)
    {
        var profissional = await _context.Profissionais.FirstOrDefaultAsync(p => p.Id == profissionalId);

        if (profissional is null)
        {
            return RedirectToAction("Index", "Home");
        }

        var paciente = await _context.Pacientes
            .FirstOrDefaultAsync(p => p.Id == id && p.ProfissionalId == profissionalId);

        if (paciente is null)
        {
            return RedirectToAction("Index", new { profissionalId });
        }

        ViewBag.ProfissionalId = profissional.Id;
        ViewBag.ProfissionalNome = profissional.NomeCompleto;
        ViewBag.ProfissionalFotoUrl = profissional.FotoUrl;
        ViewBag.ProfissionalCRP = profissional.RegistroCRP;

        var model = new PacienteDetalhesViewModel
        {
            Id = paciente.Id,
            ProfissionalId = paciente.ProfissionalId,
            NomeCompleto = paciente.NomeCompleto,
            Cpf = paciente.Cpf,
            Telefone = paciente.Telefone,
            Email = paciente.Email,
            DataNascimento = paciente.DataNascimento,
            Sexo = paciente.Sexo,
            ContatoEmergencia = paciente.ContatoEmergencia,
            Profissao = paciente.Profissao,
            Ativo = paciente.Ativo,
            DataCadastro = paciente.DataCadastro
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(PacienteViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var erros = ModelState
                .Where(par => par.Value?.Errors.Count > 0)
                .ToDictionary(
                    par => par.Key,
                    par => par.Value!.Errors.Select(erro => erro.ErrorMessage).ToArray());

            var primeiroErro = erros.Values.SelectMany(mensagens => mensagens).FirstOrDefault();

            return Json(new { success = false, message = primeiroErro ?? "Verifique os campos destacados.", errors = erros });
        }

        try
        {
            var paciente = new Paciente
            {
                Id = Guid.NewGuid(),
                ProfissionalId = model.ProfissionalId,
                NomeCompleto = model.NomeCompleto,
                Telefone = model.Telefone,
                Email = model.Email,
                DataNascimento = model.DataNascimento,
                Cpf = model.Cpf,
                Sexo = model.Sexo,
                ContatoEmergencia = model.ContatoEmergencia,
                Profissao = model.Profissao
            };

            _context.Pacientes.Add(paciente);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Paciente cadastrado com sucesso!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cadastrar paciente.");
            return Json(new { success = false, message = "Ocorreu um erro ao salvar o cadastro. Tente novamente." });
        }
    }
}
