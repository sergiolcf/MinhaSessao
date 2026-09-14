using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinhaSessao.Data;
using MinhaSessao.Extensions;
using MinhaSessao.Models.Entities;
using MinhaSessao.Models.ViewModels;
using MinhaSessao.Services;

namespace MinhaSessao.Controllers;

// Visão consolidada dos Objetivos Terapêuticos de TODOS os pacientes do profissional, numa lista só,
// com filtro por Paciente/Período/Status. O CRUD em si (criar, mudar status, excluir objetivo,
// combinados) continua morando em PacientesController — essas actions já validam a posse do
// profissional via ProfissionalId/VinculoService e são reaproveitadas sem nenhuma mudança; este
// controller só acrescenta a listagem cross-paciente.
[Authorize(Roles = AutenticacaoService.PapelProfissional)]
public class PlanosTratamentoController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly VinculoService _vinculoService;

    public PlanosTratamentoController(ApplicationDbContext context, VinculoService vinculoService)
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

        ViewBag.ProfissionalId = profissional.Id;
        ViewBag.ProfissionalNome = profissional.NomeCompleto;
        ViewBag.ProfissionalFotoUrl = profissional.FotoUrl;
        ViewBag.ProfissionalCRP = profissional.RegistroCRP;

        var pacientesAtivos = await _vinculoService.ObterPacientesAtivosAsync(profissionalId);

        var model = new PlanosTratamentoViewModel
        {
            Pacientes = pacientesAtivos
                .Select(p => new PacienteSelectItemViewModel { Id = p.Id, NomeCompleto = p.NomeCompleto, CpfFormatado = CpfUtil.Formatar(p.Cpf) })
                .ToList()
        };

        // A lista de objetivos em si não vem no SSR (view 100% orientada a AJAX, mesmo espírito da
        // aba "Plano de Tratamento" da Ficha do Paciente) — o JS busca a página inicial (sem filtro)
        // assim que a tela carrega, evitando duplicar a lógica de montagem do card em Razor e em JS.
        return View(model);
    }

    // Objetivos Terapêuticos de TODOS os pacientes do profissional logado, com filtros opcionais de
    // Paciente/Período (data de criação)/Status — mesmo formato de PacientesController.ListarObjetivos,
    // acrescentando pacienteId/pacienteNome/pacienteUrl em cada item, já que aqui a lista é cross-paciente.
    // Sempre filtra por ProfissionalId == User.ObterProfissionalId(), nunca por um Id vindo do cliente.
    [HttpGet]
    public async Task<IActionResult> Buscar(Guid? pacienteId, DateTime? dataInicio, DateTime? dataFim, string status = "todos")
    {
        var profissionalId = User.ObterProfissionalId();

        var consulta = _context.ObjetivosTerapeuticos.Where(o => o.ProfissionalId == profissionalId);

        if (pacienteId.HasValue)
        {
            consulta = consulta.Where(o => o.PacienteId == pacienteId.Value);
        }

        if (dataInicio.HasValue)
        {
            consulta = consulta.Where(o => o.DataCriacao.Date >= dataInicio.Value.Date);
        }

        if (dataFim.HasValue)
        {
            consulta = consulta.Where(o => o.DataCriacao.Date <= dataFim.Value.Date);
        }

        // "todos" (default) não aplica filtro de Status; qualquer outro valor precisa mapear pro enum
        if (!string.Equals(status, "todos", StringComparison.OrdinalIgnoreCase)
            && Enum.TryParse<StatusObjetivo>(status, true, out var statusFiltro))
        {
            consulta = consulta.Where(o => o.Status == statusFiltro);
        }

        var objetivosBrutos = await consulta
            .OrderByDescending(o => o.DataCriacao)
            .Select(o => new
            {
                o.Id,
                o.PacienteId,
                PacienteNome = o.Paciente!.NomeCompleto,
                o.Titulo,
                o.Descricao,
                Status = o.Status.ToString(),
                o.DataCriacao,
                Combinados = o.Combinados.Select(c => new { c.Id, c.Descricao, c.Concluido }).ToList(),
                TotalCombinados = o.Combinados.Count,
                CombinadosConcluidos = o.Combinados.Count(c => c.Concluido),
                TotalSessoesVinculadas = o.SessoesObjetivo.Count
            })
            .ToListAsync();

        // Url.Action não é traduzível pra SQL — só entra depois da consulta já materializada
        var objetivos = objetivosBrutos.Select(o => new
        {
            id = o.Id,
            pacienteId = o.PacienteId,
            pacienteNome = o.PacienteNome,
            pacienteUrl = Url.Action("Detalhes", "Pacientes", new { id = o.PacienteId }),
            titulo = o.Titulo,
            descricao = o.Descricao,
            status = o.Status,
            dataCriacao = o.DataCriacao.ToString("dd/MM/yyyy HH:mm"),
            combinados = o.Combinados.Select(c => new { id = c.Id, descricao = c.Descricao, concluido = c.Concluido }),
            totalCombinados = o.TotalCombinados,
            combinadosConcluidos = o.CombinadosConcluidos,
            totalSessoesVinculadas = o.TotalSessoesVinculadas
        });

        return Json(new { success = true, objetivos });
    }
}
