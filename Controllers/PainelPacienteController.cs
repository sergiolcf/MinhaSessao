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

        var model = new PainelSessoesViewModel
        {
            ProximaSessao = sessoes
                .Where(s => s.Status == StatusSessao.Agendada.ToString() && s.DataHora >= agora)
                .FirstOrDefault(),
            TotalSessoesRealizadas = sessoes.Count(s => s.Status == StatusSessao.Realizada.ToString()),
            Agendadas = sessoes
                .Where(s => s.Status == StatusSessao.Agendada.ToString())
                .ToList(),
            Historico = sessoes
                .Where(s => s.Status != StatusSessao.Agendada.ToString())
                .OrderByDescending(s => s.DataHora)
                .ToList()
        };

        return View(model);
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

        return Json(new
        {
            success = true,
            data = sessao.DataHora.ToString("dd/MM/yyyy"),
            hora = sessao.DataHora.ToString("HH:mm"),
            profissionalNome = sessao.ProfissionalNome,
            status = sessao.Status
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
