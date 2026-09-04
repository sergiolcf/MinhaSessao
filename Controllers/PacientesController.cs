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

    [HttpGet]
    public async Task<IActionResult> ListarObjetivos(Guid pacienteId)
    {
        var profissionalId = User.ObterProfissionalId();

        var pacienteValido = await _vinculoService.PacientePertenceAoProfissionalAsync(pacienteId, profissionalId);

        if (!pacienteValido)
        {
            return Json(new { success = false, message = "Paciente não encontrado." });
        }

        var objetivos = await _context.ObjetivosTerapeuticos
            .Where(o => o.PacienteId == pacienteId)
            .OrderByDescending(o => o.DataCriacao)
            .Select(o => new
            {
                id = o.Id,
                titulo = o.Titulo,
                descricao = o.Descricao,
                status = o.Status.ToString(),
                dataCriacao = o.DataCriacao.ToString("dd/MM/yyyy HH:mm"),
                combinados = o.Combinados.Select(c => new
                {
                    id = c.Id,
                    descricao = c.Descricao,
                    concluido = c.Concluido
                }).ToList(),
                totalCombinados = o.Combinados.Count,
                combinadosConcluidos = o.Combinados.Count(c => c.Concluido),
                totalSessoesVinculadas = o.SessoesObjetivo.Count
            })
            .ToListAsync();

        return Json(new { success = true, objetivos });
    }

    [HttpGet]
    public async Task<IActionResult> ListarSessoesDoObjetivo(Guid objetivoId, int pagina = 1)
    {
        const int tamanhoPagina = 10;

        var profissionalId = User.ObterProfissionalId();

        var objetivoValido = await _context.ObjetivosTerapeuticos
            .AnyAsync(o => o.Id == objetivoId && o.ProfissionalId == profissionalId);

        if (!objetivoValido)
        {
            return Json(new { success = false, message = "Objetivo não encontrado." });
        }

        if (pagina < 1) pagina = 1;

        var query = _context.SessoesObjetivos
            .Where(so => so.ObjetivoTerapeuticoId == objetivoId)
            .OrderByDescending(so => so.Sessao!.DataHora);

        var totalSessoes = await query.CountAsync();
        var totalPaginas = totalSessoes == 0 ? 1 : (int)Math.Ceiling(totalSessoes / (double)tamanhoPagina);

        var sessoes = await query
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Select(so => new
            {
                dataHora = so.Sessao!.DataHora.ToString("dd/MM/yyyy"),
                observacao = so.Observacao
            })
            .ToListAsync();

        return Json(new { success = true, sessoes, paginaAtual = pagina, totalPaginas });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarObjetivo(ObjetivoTerapeuticoViewModel model)
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

        // Garante que o paciente pertence ao profissional logado antes de gravar o objetivo
        var pacienteValido = await _vinculoService.PacientePertenceAoProfissionalAsync(model.PacienteId, profissionalId);

        if (!pacienteValido)
        {
            return Json(new { success = false, message = "Paciente não encontrado." });
        }

        try
        {
            var objetivo = new ObjetivoTerapeutico
            {
                Id = Guid.NewGuid(),
                PacienteId = model.PacienteId,
                ProfissionalId = profissionalId,
                Titulo = model.Titulo,
                Descricao = model.Descricao,
                Status = StatusObjetivo.EmAndamento,
                DataCriacao = DateTime.UtcNow
            };

            // Ignora combinados em branco digitados na mesma tela de criação (Proposta A: tudo em uma tela)
            var combinados = model.Combinados
                .Where(descricao => !string.IsNullOrWhiteSpace(descricao))
                .Select(descricao => new Combinado
                {
                    Id = Guid.NewGuid(),
                    ObjetivoTerapeuticoId = objetivo.Id,
                    Descricao = descricao.Trim(),
                    Concluido = false,
                    DataCriacao = DateTime.UtcNow
                })
                .ToList();

            objetivo.Combinados = combinados;

            _context.ObjetivosTerapeuticos.Add(objetivo);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Objetivo criado com sucesso!",
                objetivo = new
                {
                    id = objetivo.Id,
                    titulo = objetivo.Titulo,
                    descricao = objetivo.Descricao,
                    status = objetivo.Status.ToString(),
                    dataCriacao = objetivo.DataCriacao.ToString("dd/MM/yyyy HH:mm"),
                    combinados = combinados.Select(c => new
                    {
                        id = c.Id,
                        descricao = c.Descricao,
                        concluido = c.Concluido
                    }).ToList(),
                    totalCombinados = combinados.Count,
                    combinadosConcluidos = 0
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar objetivo terapêutico.");
            return Json(new { success = false, message = "Ocorreu um erro ao salvar o objetivo. Tente novamente." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AtualizarStatusObjetivo(Guid id, StatusObjetivo status)
    {
        var profissionalId = User.ObterProfissionalId();

        var objetivo = await _context.ObjetivosTerapeuticos
            .FirstOrDefaultAsync(o => o.Id == id && o.ProfissionalId == profissionalId);

        if (objetivo is null)
        {
            return Json(new { success = false, message = "Objetivo não encontrado." });
        }

        try
        {
            objetivo.Status = status;
            objetivo.DataAtualizacao = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Status atualizado com sucesso!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar status do objetivo terapêutico.");
            return Json(new { success = false, message = "Ocorreu um erro ao atualizar o status. Tente novamente." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirObjetivo(Guid id)
    {
        var profissionalId = User.ObterProfissionalId();

        var objetivo = await _context.ObjetivosTerapeuticos
            .FirstOrDefaultAsync(o => o.Id == id && o.ProfissionalId == profissionalId);

        if (objetivo is null)
        {
            return Json(new { success = false, message = "Objetivo não encontrado." });
        }

        try
        {
            // O delete em cascata configurado no ApplicationDbContext remove os Combinados junto
            _context.ObjetivosTerapeuticos.Remove(objetivo);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Objetivo removido com sucesso!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir objetivo terapêutico.");
            return Json(new { success = false, message = "Ocorreu um erro ao remover o objetivo. Tente novamente." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlternarCombinado(Guid id)
    {
        var profissionalId = User.ObterProfissionalId();

        var combinado = await _context.Combinados
            .Include(c => c.ObjetivoTerapeutico)
            .FirstOrDefaultAsync(c => c.Id == id && c.ObjetivoTerapeutico!.ProfissionalId == profissionalId);

        if (combinado is null)
        {
            return Json(new { success = false, message = "Combinado não encontrado." });
        }

        try
        {
            combinado.Concluido = !combinado.Concluido;

            await _context.SaveChangesAsync();

            return Json(new { success = true, concluido = combinado.Concluido });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao alternar combinado.");
            return Json(new { success = false, message = "Ocorreu um erro ao atualizar o combinado. Tente novamente." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdicionarCombinado(Guid objetivoId, string descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
        {
            return Json(new { success = false, message = "Escreva a descrição do combinado." });
        }

        var profissionalId = User.ObterProfissionalId();

        var objetivo = await _context.ObjetivosTerapeuticos
            .FirstOrDefaultAsync(o => o.Id == objetivoId && o.ProfissionalId == profissionalId);

        if (objetivo is null)
        {
            return Json(new { success = false, message = "Objetivo não encontrado." });
        }

        try
        {
            var combinado = new Combinado
            {
                Id = Guid.NewGuid(),
                ObjetivoTerapeuticoId = objetivo.Id,
                Descricao = descricao.Trim(),
                Concluido = false,
                DataCriacao = DateTime.UtcNow
            };

            _context.Combinados.Add(combinado);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                combinado = new
                {
                    id = combinado.Id,
                    descricao = combinado.Descricao,
                    concluido = combinado.Concluido
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar combinado.");
            return Json(new { success = false, message = "Ocorreu um erro ao adicionar o combinado. Tente novamente." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirCombinado(Guid id)
    {
        var profissionalId = User.ObterProfissionalId();

        var combinado = await _context.Combinados
            .Include(c => c.ObjetivoTerapeutico)
            .FirstOrDefaultAsync(c => c.Id == id && c.ObjetivoTerapeutico!.ProfissionalId == profissionalId);

        if (combinado is null)
        {
            return Json(new { success = false, message = "Combinado não encontrado." });
        }

        try
        {
            _context.Combinados.Remove(combinado);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Combinado removido com sucesso!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir combinado.");
            return Json(new { success = false, message = "Ocorreu um erro ao remover o combinado. Tente novamente." });
        }
    }
}
