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
public class AgendaController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly VinculoService _vinculoService;

    public AgendaController(ApplicationDbContext context, VinculoService vinculoService)
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

        var agora = DateTime.UtcNow;
        var hoje = agora.Date;
        var consultaBase = _context.Sessoes.Where(s => s.ProfissionalId == profissionalId);

        // "Sessões na Semana" é uma janela móvel dos últimos 7 dias (não a mesma semana de calendário
        // exibida na grade, que o profissional pode navegar livremente) — conta só Agendada/Realizada,
        // já que Cancelada não ocupa agenda de fato.
        var seteDiasAtras = hoje.AddDays(-6);
        var sessoesSemana = await consultaBase.CountAsync(s =>
            s.DataHora.Date >= seteDiasAtras && s.DataHora.Date <= hoje
            && (s.Status == StatusSessao.Agendada || s.Status == StatusSessao.Realizada));

        var sessoesMes = await consultaBase.CountAsync(s =>
            s.DataHora.Year == hoje.Year && s.DataHora.Month == hoje.Month);

        var sessoesCanceladasMes = await consultaBase.CountAsync(s =>
            s.Status == StatusSessao.Cancelada && s.DataHora.Year == hoje.Year && s.DataHora.Month == hoje.Month);

        var pacientesAtivos = await _vinculoService.ObterPacientesAtivosAsync(profissionalId);

        // Duração padrão do profissional é 0 apenas para registros criados antes da migration de Configurações — trata como 50
        var duracaoPadrao = profissional.DuracaoPadraoSessaoMinutos == 0 ? 50 : profissional.DuracaoPadraoSessaoMinutos;

        var model = new PainelAgendaProfissionalViewModel
        {
            SessoesSemana = sessoesSemana,
            SessoesMes = sessoesMes,
            SessoesCanceladasMes = sessoesCanceladasMes,
            Pacientes = pacientesAtivos
                .Select(p => new PacienteSelectItemViewModel { Id = p.Id, NomeCompleto = p.NomeCompleto, CpfFormatado = CpfUtil.Formatar(p.Cpf) })
                .ToList(),
            DuracaoPadraoSessaoMinutos = duracaoPadrao
        };

        return View(model);
    }

    // Sessões dentro de um intervalo [inicio, fim) — usado tanto pela grade semanal quanto mensal
    // (o cliente calcula o intervalo certo para cada visão, incluindo os dias de padding do mês).
    // Sempre filtra por ProfissionalId == User.ObterProfissionalId(), nunca por um id vindo do cliente.
    [HttpGet]
    public async Task<IActionResult> BuscarSessoesAgenda(DateTime inicio, DateTime fim)
    {
        var profissionalId = User.ObterProfissionalId();

        var sessoes = await _context.Sessoes
            .Where(s => s.ProfissionalId == profissionalId && s.DataHora >= inicio && s.DataHora < fim)
            .OrderBy(s => s.DataHora)
            .Select(s => new
            {
                id = s.Id,
                pacienteId = s.PacienteId,
                pacienteNome = s.Paciente!.NomeCompleto,
                data = s.DataHora.ToString("dd/MM/yyyy"),
                hora = s.DataHora.ToString("HH:mm"),
                dataHoraIso = s.DataHora.ToString("yyyy-MM-ddTHH:mm"),
                duracaoMinutos = s.DuracaoMinutos,
                status = s.Status.ToString()
            })
            .ToListAsync();

        return Json(new { success = true, sessoes });
    }
}
