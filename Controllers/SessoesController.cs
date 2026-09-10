using System.Globalization;
using System.Text;
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
public class SessoesController : Controller
{
    private const int SessoesPorPagina = 10;

    private readonly ApplicationDbContext _context;
    private readonly ILogger<SessoesController> _logger;
    private readonly VinculoService _vinculoService;

    public SessoesController(ApplicationDbContext context, ILogger<SessoesController> logger, VinculoService vinculoService)
    {
        _context = context;
        _logger = logger;
        _vinculoService = vinculoService;
    }

    private string ObterPrimeiroErroModelState()
    {
        return ModelState
            .Where(par => par.Value?.Errors.Count > 0)
            .SelectMany(par => par.Value!.Errors)
            .Select(erro => erro.ErrorMessage)
            .FirstOrDefault() ?? "Verifique os campos destacados.";
    }

    // Primeira letra do primeiro nome + primeira letra do último nome (ignora nomes do meio),
    // sem acento — usado para montar o Codigo da sessão (ex.: "Sérgio Luís Ferreira" -> "SF")
    private static string GerarIniciaisProfissional(string nomeCompleto)
    {
        var partes = RemoverAcentos(nomeCompleto).Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (partes.Length == 0)
        {
            return string.Empty;
        }

        return $"{partes[0][0]}{partes[^1][0]}".ToUpperInvariant();
    }

    private static string RemoverAcentos(string texto)
    {
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var semAcentos = normalizado.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(semAcentos.ToArray()).Normalize(NormalizationForm.FormC);
    }

    // Próximo número sequencial do Codigo (formato "MS_{numero}_{iniciais}") PARA ESTE profissional:
    // maior número já usado por ele + 1, nunca reaproveitando "buracos" deixados por sessões excluídas
    private async Task<int> ObterProximoNumeroSessaoAsync(Guid profissionalId)
    {
        var codigosExistentes = await _context.Sessoes
            .Where(s => s.ProfissionalId == profissionalId)
            .Select(s => s.Codigo)
            .ToListAsync();

        var maiorNumero = 0;

        foreach (var codigo in codigosExistentes)
        {
            var partes = codigo.Split('_');
            if (partes.Length == 3 && int.TryParse(partes[1], out var numero) && numero > maiorNumero)
            {
                maiorNumero = numero;
            }
        }

        return maiorNumero + 1;
    }

    // Busca uma página (10 por vez) de sessões agendadas (mais próxima primeiro), em andamento ou de
    // histórico (mais recente primeiro), com filtro opcional por paciente — reaproveitado pelo Index (página 1) e por BuscarSessoes (AJAX)
    private async Task<(List<SessaoProfissionalListItemViewModel> Sessoes, int TotalPaginas)> ObterPaginaSessoesAsync(
        Guid profissionalId, string aba, int pagina, Guid? pacienteId)
    {
        var consulta = _context.Sessoes.Where(s => s.ProfissionalId == profissionalId);

        // Histórico é só Realizada/Cancelada (EmAndamento tem aba própria, não deve aparecer aqui)
        consulta = aba switch
        {
            "historico" => consulta.Where(s => s.Status == StatusSessao.Realizada || s.Status == StatusSessao.Cancelada),
            "em_andamento" => consulta.Where(s => s.Status == StatusSessao.EmAndamento),
            _ => consulta.Where(s => s.Status == StatusSessao.Agendada)
        };

        if (pacienteId.HasValue)
        {
            consulta = consulta.Where(s => s.PacienteId == pacienteId.Value);
        }

        var total = await consulta.CountAsync();
        var totalPaginas = total == 0 ? 1 : (int)Math.Ceiling(total / (double)SessoesPorPagina);
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        consulta = aba == "historico"
            ? consulta.OrderByDescending(s => s.DataHora)
            : consulta.OrderBy(s => s.DataHora);

        var sessoes = await consulta
            .Skip((pagina - 1) * SessoesPorPagina)
            .Take(SessoesPorPagina)
            .Select(s => new SessaoProfissionalListItemViewModel
            {
                Id = s.Id,
                PacienteId = s.PacienteId,
                Codigo = s.Codigo,
                DataHora = s.DataHora,
                PacienteNome = s.Paciente!.NomeCompleto,
                DuracaoMinutos = s.DuracaoMinutos,
                Status = s.Status.ToString()
            })
            .ToListAsync();

        return (sessoes, totalPaginas);
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

        var sessoesHoje = await consultaBase.CountAsync(s => s.Status == StatusSessao.Agendada && s.DataHora.Date == hoje);

        var proximaSessao = await consultaBase
            .Where(s => s.Status == StatusSessao.Agendada && s.DataHora >= agora)
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
            .FirstOrDefaultAsync();

        var atendimentosNoMes = await consultaBase.CountAsync(s => s.Status == StatusSessao.Realizada
            && s.DataHora.Year == hoje.Year && s.DataHora.Month == hoje.Month);

        var (agendadas, totalPaginasAgendadas) = await ObterPaginaSessoesAsync(profissionalId, "agendadas", 1, null);
        var (historico, totalPaginasHistorico) = await ObterPaginaSessoesAsync(profissionalId, "historico", 1, null);
        var (emAndamento, totalPaginasEmAndamento) = await ObterPaginaSessoesAsync(profissionalId, "em_andamento", 1, null);

        var pacientesAtivos = await _vinculoService.ObterPacientesAtivosAsync(profissionalId);

        // Duração padrão do profissional é 0 apenas para registros criados antes da migration de Configurações — trata como 50
        var duracaoPadrao = profissional.DuracaoPadraoSessaoMinutos == 0 ? 50 : profissional.DuracaoPadraoSessaoMinutos;

        var model = new PainelSessoesProfissionalViewModel
        {
            SessoesHoje = sessoesHoje,
            ProximaSessao = proximaSessao,
            AtendimentosNoMes = atendimentosNoMes,
            Agendadas = agendadas,
            PaginaAtualAgendadas = 1,
            TotalPaginasAgendadas = totalPaginasAgendadas,
            Historico = historico,
            PaginaAtualHistorico = 1,
            TotalPaginasHistorico = totalPaginasHistorico,
            EmAndamento = emAndamento,
            PaginaAtualEmAndamento = 1,
            TotalPaginasEmAndamento = totalPaginasEmAndamento,
            Pacientes = pacientesAtivos
                .Select(p => new PacienteSelectItemViewModel { Id = p.Id, NomeCompleto = p.NomeCompleto, CpfFormatado = CpfUtil.Formatar(p.Cpf) })
                .ToList(),
            DuracaoPadraoSessaoMinutos = duracaoPadrao
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> BuscarSessoes(string aba, int pagina = 1, Guid? pacienteId = null)
    {
        var profissionalId = User.ObterProfissionalId();
        var abaNormalizada = aba switch
        {
            "historico" => "historico",
            "em_andamento" => "em_andamento",
            _ => "agendadas"
        };

        var (sessoes, totalPaginas) = await ObterPaginaSessoesAsync(profissionalId, abaNormalizada, pagina, pacienteId);

        var itens = sessoes.Select(s => new
        {
            id = s.Id,
            pacienteId = s.PacienteId,
            pacienteNome = s.PacienteNome,
            iniciais = PacienteIniciais.Calcular(s.PacienteNome),
            codigo = s.Codigo,
            data = s.DataHora.ToString("dd/MM/yyyy"),
            hora = s.DataHora.ToString("HH:mm"),
            dataHoraIso = s.DataHora.ToString("yyyy-MM-ddTHH:mm"),
            duracaoMinutos = s.DuracaoMinutos,
            status = s.Status
        });

        return Json(new { success = true, sessoes = itens, paginaAtual = Math.Clamp(pagina, 1, totalPaginas), totalPaginas });
    }

    // Sugere até 3 pacientes vinculados que dão match no nome ou no CPF digitado — alimenta a busca do filtro de paciente
    [HttpGet]
    public async Task<IActionResult> BuscarPacientesFiltro(string termo)
    {
        var profissionalId = User.ObterProfissionalId();

        if (string.IsNullOrWhiteSpace(termo))
        {
            return Json(new { success = true, pacientes = Array.Empty<object>() });
        }

        var termoNome = termo.Trim().ToLower();
        var termoCpf = CpfUtil.Normalizar(termo);

        var pacientes = await _context.Vinculos
            .Where(v => v.ProfissionalId == profissionalId && v.Status == StatusVinculo.Ativo)
            .Select(v => v.Paciente!)
            .Where(p => p.NomeCompleto.ToLower().Contains(termoNome)
                || (termoCpf != "" && p.Cpf != null && p.Cpf.Contains(termoCpf)))
            .OrderBy(p => p.NomeCompleto)
            .Take(3)
            .ToListAsync();

        var itens = pacientes.Select(p => new { id = p.Id, nomeCompleto = p.NomeCompleto, cpfFormatado = CpfUtil.Formatar(p.Cpf) });

        return Json(new { success = true, pacientes = itens });
    }

    [HttpGet]
    public async Task<IActionResult> ObterSessao(Guid id)
    {
        var profissionalId = User.ObterProfissionalId();

        var sessao = await _context.Sessoes
            .Where(s => s.Id == id && s.ProfissionalId == profissionalId)
            .Select(s => new
            {
                s.Id,
                s.PacienteId,
                s.Codigo,
                s.DuracaoMinutos,
                Status = s.Status.ToString(),
                DataHoraIso = s.DataHora.ToString("yyyy-MM-ddTHH:mm"),
                s.AnotacoesClinicas
            })
            .FirstOrDefaultAsync();

        if (sessao is null)
        {
            return Json(new { success = false, message = "Sessão não encontrada." });
        }

        var objetivosAtivos = await _context.ObjetivosTerapeuticos
            .Where(o => o.PacienteId == sessao.PacienteId && o.Status == StatusObjetivo.EmAndamento)
            .OrderBy(o => o.Titulo)
            .Select(o => new { id = o.Id, titulo = o.Titulo })
            .ToListAsync();

        // Título incluído aqui (além do já usado pelo modal de edição) pra alimentar o popup
        // somente leitura do histórico de sessões do Plano de Tratamento, que exibe o objetivo
        // pelo título sem precisar cruzar com a lista de objetivosAtivos
        var objetivosVinculados = await _context.SessoesObjetivos
            .Where(so => so.SessaoId == sessao.Id)
            .Select(so => new
            {
                objetivoTerapeuticoId = so.ObjetivoTerapeuticoId,
                titulo = so.ObjetivoTerapeutico!.Titulo,
                observacao = so.Observacao
            })
            .ToListAsync();

        return Json(new
        {
            success = true,
            id = sessao.Id,
            pacienteId = sessao.PacienteId,
            codigo = sessao.Codigo,
            dataHoraIso = sessao.DataHoraIso,
            duracaoMinutos = sessao.DuracaoMinutos,
            status = sessao.Status,
            anotacoesClinicas = sessao.AnotacoesClinicas,
            objetivosAtivos,
            objetivosVinculados
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(NovaSessaoViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = ObterPrimeiroErroModelState() });
        }

        if (!Enum.TryParse<StatusSessao>(model.Status, out var status))
        {
            return Json(new { success = false, message = "Status inválido." });
        }

        var profissionalId = User.ObterProfissionalId();

        var pacienteValido = await _vinculoService.PacientePertenceAoProfissionalAsync(model.PacienteId, profissionalId);

        if (!pacienteValido)
        {
            return Json(new { success = false, message = "Paciente não encontrado." });
        }

        try
        {
            var profissional = await _context.Profissionais.FirstOrDefaultAsync(p => p.Id == profissionalId);

            if (profissional is null)
            {
                return Json(new { success = false, message = "Profissional não encontrado." });
            }

            // Gerado e gravado aqui, na criação — nunca recalculado depois (ver GerarIniciaisProfissional/ObterProximoNumeroSessaoAsync)
            var proximoNumero = await ObterProximoNumeroSessaoAsync(profissionalId);
            var iniciais = GerarIniciaisProfissional(profissional.NomeCompleto);

            var sessao = new Sessao
            {
                Id = Guid.NewGuid(),
                PacienteId = model.PacienteId,
                ProfissionalId = profissionalId,
                DataHora = model.DataHora,
                DuracaoMinutos = model.DuracaoMinutos,
                Status = status,
                Codigo = $"MS_{proximoNumero}_{iniciais}"
            };

            _context.Sessoes.Add(sessao);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Sessão agendada com sucesso!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar sessão.");
            return Json(new { success = false, message = "Ocorreu um erro ao agendar a sessão. Tente novamente." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Atualizar(AtualizarSessaoViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = ObterPrimeiroErroModelState() });
        }

        if (!Enum.TryParse<StatusSessao>(model.Status, out var status))
        {
            return Json(new { success = false, message = "Status inválido." });
        }

        var profissionalId = User.ObterProfissionalId();

        var sessao = await _context.Sessoes
            .FirstOrDefaultAsync(s => s.Id == model.Id && s.ProfissionalId == profissionalId);

        if (sessao is null)
        {
            return Json(new { success = false, message = "Sessão não encontrada." });
        }

        try
        {
            sessao.DataHora = model.DataHora;
            sessao.DuracaoMinutos = model.DuracaoMinutos;
            sessao.Status = status;
            sessao.AnotacoesClinicas = model.AnotacoesClinicas;

            var objetivosEnviados = model.Objetivos?
                .Where(o => o.ObjetivoTerapeuticoId != Guid.Empty)
                .ToList() ?? new List<SessaoObjetivoViewModel>();

            var vinculosExistentes = await _context.SessoesObjetivos
                .Where(so => so.SessaoId == sessao.Id)
                .ToListAsync();

            var idsEnviados = objetivosEnviados.Select(o => o.ObjetivoTerapeuticoId).ToHashSet();
            var vinculosParaRemover = vinculosExistentes
                .Where(so => !idsEnviados.Contains(so.ObjetivoTerapeuticoId))
                .ToList();

            _context.SessoesObjetivos.RemoveRange(vinculosParaRemover);

            foreach (var objetivo in objetivosEnviados)
            {
                var vinculo = vinculosExistentes.FirstOrDefault(so => so.ObjetivoTerapeuticoId == objetivo.ObjetivoTerapeuticoId);

                if (vinculo is not null)
                {
                    vinculo.Observacao = objetivo.Observacao;
                }
                else
                {
                    _context.SessoesObjetivos.Add(new SessaoObjetivo
                    {
                        SessaoId = sessao.Id,
                        ObjetivoTerapeuticoId = objetivo.ObjetivoTerapeuticoId,
                        Observacao = objetivo.Observacao
                    });
                }
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Sessão atualizada com sucesso!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar sessão.");
            return Json(new { success = false, message = "Ocorreu um erro ao atualizar a sessão. Tente novamente." });
        }
    }

    // Chamado a partir do card "Próxima Sessão" (Minhas Sessões) — por enquanto só troca o status,
    // sem abrir nenhuma tela de atendimento (fica para um card futuro)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IniciarSessao(Guid id)
    {
        var profissionalId = User.ObterProfissionalId();

        var sessao = await _context.Sessoes.FirstOrDefaultAsync(s => s.Id == id && s.ProfissionalId == profissionalId);

        if (sessao is null)
        {
            return Json(new { success = false, message = "Sessão não encontrada." });
        }

        try
        {
            // Só pode haver 1 sessão Em Andamento por profissional — se alguma ficou "presa" nesse
            // status (ex.: tela fechada sem concluir o atendimento anterior), ela volta pra Agendada
            var outraEmAndamento = await _context.Sessoes.FirstOrDefaultAsync(
                s => s.ProfissionalId == profissionalId && s.Status == StatusSessao.EmAndamento && s.Id != id);

            if (outraEmAndamento is not null)
            {
                outraEmAndamento.Status = StatusSessao.Agendada;
            }

            sessao.Status = StatusSessao.EmAndamento;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao iniciar sessão.");
            return Json(new { success = false, message = "Ocorreu um erro ao iniciar a sessão. Tente novamente." });
        }
    }
}
