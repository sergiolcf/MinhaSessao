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
    private const int ProfissionaisPorPagina = 10;
    private const int AnotacoesPorPagina = 10;

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

        var agora = DateTime.UtcNow;

        var proximaSessao = await _context.Sessoes
            .Where(s => s.PacienteId == pacienteId && s.Status == StatusSessao.Agendada && s.DataHora >= agora)
            .OrderBy(s => s.DataHora)
            .Select(s => new { s.DataHora, ProfissionalNome = s.Profissional!.NomeCompleto })
            .FirstOrDefaultAsync();

        var totalSessoesRealizadas = await _context.Sessoes
            .CountAsync(s => s.PacienteId == pacienteId && s.Status == StatusSessao.Realizada);

        var combinadosAtivos = await _context.Combinados
            .Where(c => !c.Concluido
                && c.ObjetivoTerapeutico!.PacienteId == pacienteId
                && c.ObjetivoTerapeutico!.Status == StatusObjetivo.EmAndamento)
            .OrderByDescending(c => c.ObjetivoTerapeutico!.DataCriacao)
            .Take(5)
            .Select(c => new CombinadoAtivoViewModel
            {
                Descricao = c.Descricao,
                ObjetivoTitulo = c.ObjetivoTerapeutico!.Titulo
            })
            .ToListAsync();

        var model = new PainelPacienteViewModel
        {
            PacienteId = paciente.Id,
            NomeCompleto = paciente.NomeCompleto,
            ProximaSessaoDataHora = proximaSessao?.DataHora,
            ProximaSessaoProfissionalNome = proximaSessao?.ProfissionalNome,
            TotalSessoesRealizadas = totalSessoesRealizadas,
            CombinadosAtivos = combinadosAtivos
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

        var (todosProfissionais, paginaAtualTodos, totalPaginasTodos) = await ObterPaginaTodosProfissionaisAsync(busca, 1);

        var model = new PainelProfissionaisViewModel
        {
            MeusProfissionais = await ObterMeusProfissionaisAsync(pacienteId),
            TodosProfissionais = todosProfissionais,
            PaginaAtualTodos = paginaAtualTodos,
            TotalPaginasTodos = totalPaginasTodos,
            AbaInicial = string.Equals(aba, "todos", StringComparison.OrdinalIgnoreCase) ? "todos" : "meus",
            TermoBusca = busca
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> BuscarProfissionais(string? busca, int pagina = 1)
    {
        var (profissionais, paginaAtual, totalPaginas) = await ObterPaginaTodosProfissionaisAsync(busca, pagina);
        return Json(new { success = true, profissionais, paginaAtual, totalPaginas });
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
    public async Task<IActionResult> Sessoes()
    {
        var pacienteId = User.ObterPacienteId();
        var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.Id == pacienteId);

        if (paciente is null)
        {
            return RedirectToAction("Login", "Account");
        }

        ViewBag.PacienteId = paciente.Id;
        ViewBag.PacienteNome = paciente.NomeCompleto;

        var agora = DateTime.UtcNow;

        var sessoes = await _context.Sessoes
            .Where(s => s.PacienteId == pacienteId)
            .OrderBy(s => s.DataHora)
            .Select(s => new SessaoListItemViewModel
            {
                Id = s.Id,
                DataHora = s.DataHora,
                ProfissionalNome = s.Profissional!.NomeCompleto,
                Status = s.Status.ToString()
            })
            .ToListAsync();

        var (anotacoesClinicas, totalPaginasAnotacoesClinicas) = await ObterPaginaAnotacoesClinicasAsync(pacienteId, 1, null, null, null);

        var model = new PainelSessoesViewModel
        {
            ProximaSessao = sessoes
                .Where(s => s.Status == StatusSessao.Agendada.ToString() && s.DataHora >= agora)
                .FirstOrDefault(),
            // Contagens dos 4 cards do topo — calculadas em memória a partir da lista "sessoes" já
            // carregada, sem precisar de uma nova consulta ao banco
            TotalAgendadas = sessoes.Count(s => s.Status == StatusSessao.Agendada.ToString()),
            TotalCanceladas = sessoes.Count(s => s.Status == StatusSessao.Cancelada.ToString()),
            TotalRealizadas = sessoes.Count(s => s.Status == StatusSessao.Realizada.ToString()),
            Agendadas = sessoes
                .Where(s => s.Status == StatusSessao.Agendada.ToString())
                .ToList(),
            Historico = sessoes
                .Where(s => s.Status != StatusSessao.Agendada.ToString())
                .OrderByDescending(s => s.DataHora)
                .ToList(),
            AnotacoesClinicas = anotacoesClinicas,
            PaginaAtualAnotacoesClinicas = 1,
            TotalPaginasAnotacoesClinicas = totalPaginasAnotacoesClinicas
        };

        return View(model);
    }

    // Pagina (10 por vez) as Anotações Clínicas (AnotacaoSessao) de TODAS as sessões do paciente
    // logado, reunindo num só lugar o que hoje só dá pra ver entrando sessão por sessão — reaproveitado
    // pelo carregamento inicial de Sessoes() e pelo endpoint AJAX (BuscarAnotacoesClinicas). Filtra só
    // por Sessao.PacienteId (NUNCA por ProfissionalId): o paciente pode ter tido mais de um profissional
    // ao longo do tempo e deve ver as anotações de todos eles — diferente da versão do profissional
    // (PacientesController.ObterPaginaAnotacoesClinicasAsync), que restringe ao profissional logado.
    private async Task<(List<AnotacaoClinicaListItemViewModel> Anotacoes, int TotalPaginas)> ObterPaginaAnotacoesClinicasAsync(
        Guid pacienteId, int pagina, string? busca, DateTime? dataInicio, DateTime? dataFim)
    {
        var consulta = _context.AnotacoesSessao.Where(a => a.Sessao!.PacienteId == pacienteId);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termoBusca = busca.Trim().ToLower();
            consulta = consulta.Where(a => a.Titulo.ToLower().Contains(termoBusca));
        }

        if (dataInicio.HasValue)
        {
            consulta = consulta.Where(a => a.DataRegistro.Date >= dataInicio.Value.Date);
        }

        if (dataFim.HasValue)
        {
            consulta = consulta.Where(a => a.DataRegistro.Date <= dataFim.Value.Date);
        }

        var total = await consulta.CountAsync();
        var totalPaginas = total == 0 ? 1 : (int)Math.Ceiling(total / (double)AnotacoesPorPagina);
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        var anotacoes = await consulta
            .OrderByDescending(a => a.DataRegistro)
            .Skip((pagina - 1) * AnotacoesPorPagina)
            .Take(AnotacoesPorPagina)
            .Select(a => new AnotacaoClinicaListItemViewModel
            {
                Id = a.Id,
                Titulo = a.Titulo,
                Conteudo = a.Conteudo,
                DataRegistro = a.DataRegistro,
                SessaoId = a.SessaoId,
                SessaoCodigo = a.Sessao!.Codigo,
                SessaoDataHora = a.Sessao.DataHora,
                Objetivos = a.SessaoObjetivos.Select(so => so.ObjetivoTerapeutico!.Titulo).ToList()
            })
            .ToListAsync();

        return (anotacoes, totalPaginas);
    }

    // Endpoint AJAX: paginação, busca por título e filtro por período da aba "Anotações Clínicas" —
    // mesmo padrão de PacientesController.BuscarAnotacoesClinicas (lado do profissional), mas sem
    // parâmetro pacienteId na rota (usa sempre o paciente logado, via User.ObterPacienteId()) e sem
    // "sessaoUrl": o paciente não acessa Views/Sessoes/Sessao.cshtml (tela exclusiva do profissional),
    // então cada item só é aberto em modo leitura dentro do próprio Painel do Paciente.
    [HttpGet]
    public async Task<IActionResult> BuscarAnotacoesClinicas(int pagina = 1, string? busca = null, DateTime? dataInicio = null, DateTime? dataFim = null)
    {
        var pacienteId = User.ObterPacienteId();

        var (anotacoes, totalPaginas) = await ObterPaginaAnotacoesClinicasAsync(pacienteId, pagina, busca, dataInicio, dataFim);

        var itens = anotacoes.Select(a => new
        {
            id = a.Id,
            titulo = a.Titulo,
            conteudo = a.Conteudo,
            dataRegistro = a.DataRegistro.ToString("dd/MM/yyyy HH:mm"),
            objetivos = a.Objetivos,
            sessaoCodigo = a.SessaoCodigo,
            sessaoDataHora = a.SessaoDataHora.ToString("dd/MM/yyyy")
        });

        return Json(new { success = true, anotacoes = itens, paginaAtual = Math.Clamp(pagina, 1, totalPaginas), totalPaginas });
    }

    [HttpGet]
    public async Task<IActionResult> DetalhesSessao(Guid id)
    {
        var pacienteId = User.ObterPacienteId();

        var sessao = await _context.Sessoes
            .Where(s => s.Id == id && s.PacienteId == pacienteId)
            .Select(s => new
            {
                s.DataHora,
                ProfissionalNome = s.Profissional!.NomeCompleto,
                Status = s.Status.ToString()
            })
            .FirstOrDefaultAsync();

        if (sessao is null)
        {
            return Json(new { success = false, message = "Sessão não encontrada." });
        }

        // Enum.ToString() não tem espaço ("EmAndamento") — só esse valor precisa de um texto de
        // exibição próprio, mesmo ajuste já feito em Views/PainelPaciente/_LinhaSessao.cshtml
        var statusExibicao = sessao.Status == "EmAndamento" ? "Em Andamento" : sessao.Status;

        return Json(new
        {
            success = true,
            data = sessao.DataHora.ToString("dd/MM/yyyy"),
            hora = sessao.DataHora.ToString("HH:mm"),
            profissionalNome = sessao.ProfissionalNome,
            status = statusExibicao
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

    private async Task<(List<ProfissionalListaItemViewModel> Itens, int PaginaAtual, int TotalPaginas)> ObterPaginaTodosProfissionaisAsync(string? busca, int pagina)
    {
        var consulta = _context.Profissionais.AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termoBusca = busca.Trim().ToLower();
            consulta = consulta.Where(p => p.NomeCompleto.ToLower().Contains(termoBusca) || p.RegistroCRP.ToLower().Contains(termoBusca));
        }

        consulta = consulta.OrderBy(p => p.NomeCompleto);

        var totalProfissionais = await consulta.CountAsync();
        var totalPaginas = totalProfissionais == 0 ? 1 : (int)Math.Ceiling(totalProfissionais / (double)ProfissionaisPorPagina);
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        var itens = await consulta
            .Skip((pagina - 1) * ProfissionaisPorPagina)
            .Take(ProfissionaisPorPagina)
            .Select(p => new ProfissionalListaItemViewModel
            {
                Id = p.Id,
                NomeCompleto = p.NomeCompleto,
                RegistroCRP = p.RegistroCRP,
                Apresentacao = p.Apresentacao,
                FotoUrl = p.FotoUrl
            })
            .ToListAsync();

        return (itens, pagina, totalPaginas);
    }
}
