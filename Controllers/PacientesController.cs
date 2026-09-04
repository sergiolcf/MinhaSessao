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
public class PacientesController : Controller
{
    private const int AnotacoesPorPagina = 10;

    private readonly ApplicationDbContext _context;
    private readonly ILogger<PacientesController> _logger;
    private readonly VinculoService _vinculoService;

    public PacientesController(ApplicationDbContext context, ILogger<PacientesController> logger, VinculoService vinculoService)
    {
        _context = context;
        _logger = logger;
        _vinculoService = vinculoService;
    }

    // Busca um Paciente pelo CPF normalizado (só dígitos) — o CPF é o identificador usado para evitar duplicidade
    private async Task<Paciente?> BuscarPacientePorCpfAsync(string cpf)
    {
        var cpfNormalizado = CpfUtil.Normalizar(cpf);

        if (string.IsNullOrEmpty(cpfNormalizado))
        {
            return null;
        }

        return await _context.Pacientes
            .FirstOrDefaultAsync(p => p.Cpf == cpfNormalizado);
    }

    public async Task<IActionResult> Index()
    {
        var profissionalId = User.ObterProfissionalId();
        var profissional = await _context.Profissionais.FirstOrDefaultAsync(p => p.Id == profissionalId);

        if (profissional is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var pacientesAtivos = await _vinculoService.ObterPacientesAtivosAsync(profissional.Id);
        var pacientes = pacientesAtivos
            .Select(p => new PacienteListItemViewModel
            {
                Id = p.Id,
                NomeCompleto = p.NomeCompleto,
                Telefone = p.Telefone,
                Email = p.Email,
                DataNascimento = p.DataNascimento,
                Ativo = p.Ativo
            })
            .ToList();

        ViewBag.ProfissionalId = profissional.Id;
        ViewBag.ProfissionalNome = profissional.NomeCompleto;
        ViewBag.ProfissionalFotoUrl = profissional.FotoUrl;
        ViewBag.ProfissionalCRP = profissional.RegistroCRP;

        var model = new PacientesIndexViewModel
        {
            Pacientes = pacientes
        };

        return View(model);
    }

    public async Task<IActionResult> Detalhes(Guid id)
    {
        var profissionalId = User.ObterProfissionalId();
        var profissional = await _context.Profissionais.FirstOrDefaultAsync(p => p.Id == profissionalId);

        if (profissional is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var pacienteValido = await _vinculoService.PacientePertenceAoProfissionalAsync(id, profissionalId);
        var paciente = pacienteValido ? await _context.Pacientes.FirstOrDefaultAsync(p => p.Id == id) : null;

        if (paciente is null)
        {
            return RedirectToAction("Index");
        }

        ViewBag.ProfissionalId = profissional.Id;
        ViewBag.ProfissionalNome = profissional.NomeCompleto;
        ViewBag.ProfissionalFotoUrl = profissional.FotoUrl;
        ViewBag.ProfissionalCRP = profissional.RegistroCRP;

        var totalAnotacoes = await _context.AnotacoesConfidenciais.CountAsync(a => a.PacienteId == paciente.Id);
        var totalPaginasAnotacoes = totalAnotacoes == 0 ? 1 : (int)Math.Ceiling(totalAnotacoes / (double)AnotacoesPorPagina);

        var anotacoes = await _context.AnotacoesConfidenciais
            .Where(a => a.PacienteId == paciente.Id)
            .OrderByDescending(a => a.DataRegistro)
            .Take(AnotacoesPorPagina)
            .Select(a => new AnotacaoConfidencialItemViewModel
            {
                Id = a.Id,
                Titulo = a.Titulo,
                Conteudo = a.Conteudo,
                DataRegistro = a.DataRegistro
            })
            .ToListAsync();

        var sessoes = await _context.Sessoes
            .Where(s => s.PacienteId == paciente.Id && s.ProfissionalId == profissionalId)
            .OrderByDescending(s => s.DataHora)
            .Select(s => new SessaoProfissionalListItemViewModel
            {
                Id = s.Id,
                PacienteId = s.PacienteId,
                DataHora = s.DataHora,
                PacienteNome = paciente.NomeCompleto,
                DuracaoMinutos = s.DuracaoMinutos,
                Status = s.Status.ToString(),
                AnotacoesClinicas = s.AnotacoesClinicas
            })
            .ToListAsync();

        var model = new PacienteDetalhesViewModel
        {
            Id = paciente.Id,
            NomeCompleto = paciente.NomeCompleto,
            Cpf = paciente.Cpf,
            Telefone = paciente.Telefone,
            Email = paciente.Email,
            DataNascimento = paciente.DataNascimento,
            Sexo = paciente.Sexo,
            ContatoEmergencia = paciente.ContatoEmergencia,
            Profissao = paciente.Profissao,
            Ativo = paciente.Ativo,
            DataCadastro = paciente.DataCadastro,
            Anotacoes = anotacoes,
            PaginaAtualAnotacoes = 1,
            TotalPaginasAnotacoes = totalPaginasAnotacoes,
            Sessoes = sessoes
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> BuscarAnotacoes(Guid pacienteId, int pagina = 1, string? busca = null, string ordem = "recente")
    {
        var profissionalId = User.ObterProfissionalId();

        var pacienteValido = await _vinculoService.PacientePertenceAoProfissionalAsync(pacienteId, profissionalId);

        if (!pacienteValido)
        {
            return Json(new { success = false, message = "Paciente não encontrado." });
        }

        var consulta = _context.AnotacoesConfidenciais.Where(a => a.PacienteId == pacienteId);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termoBusca = busca.Trim().ToLower();
            consulta = consulta.Where(a => a.Titulo != null && a.Titulo.ToLower().Contains(termoBusca));
        }

        var totalAnotacoes = await consulta.CountAsync();
        var totalPaginas = totalAnotacoes == 0 ? 1 : (int)Math.Ceiling(totalAnotacoes / (double)AnotacoesPorPagina);
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        consulta = ordem == "antigo"
            ? consulta.OrderBy(a => a.DataRegistro)
            : consulta.OrderByDescending(a => a.DataRegistro);

        var anotacoes = await consulta
            .Skip((pagina - 1) * AnotacoesPorPagina)
            .Take(AnotacoesPorPagina)
            .Select(a => new
            {
                id = a.Id,
                titulo = a.Titulo,
                conteudo = a.Conteudo,
                dataRegistro = a.DataRegistro.ToString("dd/MM/yyyy HH:mm")
            })
            .ToListAsync();

        return Json(new { success = true, anotacoes, paginaAtual = pagina, totalPaginas });
    }

    [HttpGet]
    public async Task<IActionResult> SugerirTitulosAnotacao(Guid pacienteId, string termo)
    {
        var profissionalId = User.ObterProfissionalId();

        if (string.IsNullOrWhiteSpace(termo))
        {
            return Json(new { success = true, titulos = Array.Empty<string>() });
        }

        var pacienteValido = await _vinculoService.PacientePertenceAoProfissionalAsync(pacienteId, profissionalId);

        if (!pacienteValido)
        {
            return Json(new { success = false, message = "Paciente não encontrado." });
        }

        var termoBusca = termo.Trim().ToLower();

        // Busca mais candidatos do que o necessário para poder remover títulos duplicados e ainda assim sugerir 3
        var candidatos = await _context.AnotacoesConfidenciais
            .Where(a => a.PacienteId == pacienteId && a.Titulo != null && a.Titulo.ToLower().Contains(termoBusca))
            .OrderByDescending(a => a.DataRegistro)
            .Select(a => a.Titulo!)
            .Take(20)
            .ToListAsync();

        var titulos = candidatos.Distinct().Take(3).ToList();

        return Json(new { success = true, titulos });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarAnotacao(AnotacaoConfidencialViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var primeiroErro = ModelState
                .Where(par => par.Value?.Errors.Count > 0)
                .SelectMany(par => par.Value!.Errors)
                .Select(erro => erro.ErrorMessage)
                .FirstOrDefault();

            return Json(new { success = false, message = primeiroErro ?? "Verifique os campos destacados." });
        }

        var profissionalId = User.ObterProfissionalId();

        // Garante que o paciente pertence ao profissional logado antes de gravar a anotação
        var pacienteValido = await _vinculoService.PacientePertenceAoProfissionalAsync(model.PacienteId, profissionalId);

        if (!pacienteValido)
        {
            return Json(new { success = false, message = "Paciente não encontrado." });
        }

        try
        {
            var anotacao = new AnotacaoConfidencial
            {
                Id = Guid.NewGuid(),
                PacienteId = model.PacienteId,
                ProfissionalId = profissionalId,
                Titulo = model.Titulo,
                Conteudo = model.Conteudo,
                DataRegistro = DateTime.UtcNow
            };

            _context.AnotacoesConfidenciais.Add(anotacao);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Anotação registrada com sucesso!",
                anotacao = new
                {
                    id = anotacao.Id,
                    titulo = anotacao.Titulo,
                    conteudo = anotacao.Conteudo,
                    dataRegistro = anotacao.DataRegistro.ToString("dd/MM/yyyy HH:mm")
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar anotação confidencial.");
            return Json(new { success = false, message = "Ocorreu um erro ao salvar a anotação. Tente novamente." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AtualizarAnotacao(AnotacaoConfidencialEditarViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var primeiroErro = ModelState
                .Where(par => par.Value?.Errors.Count > 0)
                .SelectMany(par => par.Value!.Errors)
                .Select(erro => erro.ErrorMessage)
                .FirstOrDefault();

            return Json(new { success = false, message = primeiroErro ?? "Verifique os campos destacados." });
        }

        var profissionalId = User.ObterProfissionalId();

        var anotacao = await _context.AnotacoesConfidenciais
            .FirstOrDefaultAsync(a => a.Id == model.Id && a.ProfissionalId == profissionalId);

        if (anotacao is null)
        {
            return Json(new { success = false, message = "Anotação não encontrada." });
        }

        try
        {
            anotacao.Titulo = model.Titulo;
            anotacao.Conteudo = model.Conteudo;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Anotação atualizada com sucesso!",
                anotacao = new
                {
                    id = anotacao.Id,
                    titulo = anotacao.Titulo,
                    conteudo = anotacao.Conteudo,
                    dataRegistro = anotacao.DataRegistro.ToString("dd/MM/yyyy HH:mm")
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar anotação confidencial.");
            return Json(new { success = false, message = "Ocorreu um erro ao atualizar a anotação. Tente novamente." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirAnotacao(Guid id)
    {
        var profissionalId = User.ObterProfissionalId();

        var anotacao = await _context.AnotacoesConfidenciais
            .FirstOrDefaultAsync(a => a.Id == id && a.ProfissionalId == profissionalId);

        if (anotacao is null)
        {
            return Json(new { success = false, message = "Anotação não encontrada." });
        }

        try
        {
            _context.AnotacoesConfidenciais.Remove(anotacao);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Anotação removida com sucesso!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir anotação confidencial.");
            return Json(new { success = false, message = "Ocorreu um erro ao remover a anotação. Tente novamente." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> VerificarPacienteExistente(string cpf)
    {
        var paciente = await BuscarPacientePorCpfAsync(cpf);

        if (paciente is null)
        {
            return Json(new { existe = false });
        }

        return Json(new
        {
            existe = true,
            pacienteId = paciente.Id,
            nomeCompleto = paciente.NomeCompleto,
            iniciais = PacienteIniciais.Calcular(paciente.NomeCompleto)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Vincular(Guid pacienteId)
    {
        var profissionalId = User.ObterProfissionalId();

        var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.Id == pacienteId);

        if (paciente is null)
        {
            return Json(new { success = false, message = "Paciente não encontrado." });
        }

        var jaVinculado = await _vinculoService.PacientePertenceAoProfissionalAsync(pacienteId, profissionalId);

        if (jaVinculado)
        {
            return Json(new { success = false, message = "Este paciente já está na sua lista." });
        }

        try
        {
            _vinculoService.CriarVinculo(pacienteId, profissionalId);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Paciente vinculado com sucesso!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao vincular paciente existente.");
            return Json(new { success = false, message = "Ocorreu um erro ao vincular o paciente. Tente novamente." });
        }
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
            var profissionalId = User.ObterProfissionalId();

            // A tela já verificou o CPF antes de chegar aqui (VerificarPacienteExistente); esta é só uma
            // rede de segurança contra condição de corrida. Não deveria ocorrer no fluxo normal — se ocorrer,
            // não cadastra e pede pra verificar de novo (o profissional deve reabrir o modal e checar o CPF).
            var pacienteExistente = await BuscarPacientePorCpfAsync(model.Cpf);

            if (pacienteExistente is not null)
            {
                return Json(new { success = false, message = "Já existe um paciente cadastrado com esse CPF. Feche e reabra o cadastro para verificar novamente." });
            }

            var paciente = new Paciente
            {
                Id = Guid.NewGuid(),
                NomeCompleto = model.NomeCompleto,
                Telefone = model.Telefone,
                Email = model.Email,
                DataNascimento = model.DataNascimento,
                Cpf = CpfUtil.Normalizar(model.Cpf),
                Sexo = model.Sexo,
                ContatoEmergencia = model.ContatoEmergencia,
                Profissao = model.Profissao
            };

            // Gera a senha temporária de acesso do paciente; só existe em texto puro nesta resposta
            var senhaTemporaria = AutenticacaoService.GerarSenhaTemporaria();
            paciente.Senha = AutenticacaoService.HashSenhaPaciente(paciente, senhaTemporaria);

            _context.Pacientes.Add(paciente);
            _vinculoService.CriarVinculo(paciente.Id, profissionalId);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Paciente cadastrado com sucesso!", senhaTemporaria });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cadastrar paciente.");
            return Json(new { success = false, message = "Ocorreu um erro ao salvar o cadastro. Tente novamente." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GerarNovaSenha(Guid id)
    {
        var profissionalId = User.ObterProfissionalId();

        var pacienteValido = await _vinculoService.PacientePertenceAoProfissionalAsync(id, profissionalId);
        var paciente = pacienteValido ? await _context.Pacientes.FirstOrDefaultAsync(p => p.Id == id) : null;

        if (paciente is null)
        {
            return Json(new { success = false, message = "Paciente não encontrado." });
        }

        try
        {
            // A senha antiga é sobrescrita e fica definitivamente inutilizável
            var senhaTemporaria = AutenticacaoService.GerarSenhaTemporaria();
            paciente.Senha = AutenticacaoService.HashSenhaPaciente(paciente, senhaTemporaria);

            await _context.SaveChangesAsync();

            return Json(new { success = true, senhaTemporaria });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar nova senha do paciente.");
            return Json(new { success = false, message = "Ocorreu um erro ao gerar a nova senha. Tente novamente." });
        }
    }
}
