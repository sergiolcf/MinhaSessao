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
    private const int PacientesPorPagina = 10;

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

    // Pagina (10 por vez) a lista de pacientes ativos do profissional, com filtro opcional por nome
    // ou CPF normalizado — reaproveitado pelo Index (página 1, sem filtro) e por BuscarPacientes (AJAX).
    // A base (_vinculoService.ObterPacientesAtivosAsync) já traz tudo pra memória, então filtro/ordenação/
    // paginação aqui são LINQ-to-Objects, não uma nova consulta ao banco.
    private async Task<(List<PacienteListItemViewModel> Pacientes, int TotalPaginas)> ObterPaginaPacientesAsync(
        Guid profissionalId, int pagina, string? termoBusca)
    {
        var pacientesAtivos = await _vinculoService.ObterPacientesAtivosAsync(profissionalId);

        IEnumerable<Paciente> pacientesFiltrados = pacientesAtivos;

        if (!string.IsNullOrWhiteSpace(termoBusca))
        {
            var termoNome = termoBusca.Trim().ToLower();
            var termoCpf = CpfUtil.Normalizar(termoBusca);

            pacientesFiltrados = pacientesAtivos.Where(p =>
                p.NomeCompleto.ToLower().Contains(termoNome)
                || (termoCpf != "" && p.Cpf != null && p.Cpf.Contains(termoCpf)));
        }

        var pacientesOrdenados = pacientesFiltrados.OrderBy(p => p.NomeCompleto).ToList();

        var total = pacientesOrdenados.Count;
        var totalPaginas = total == 0 ? 1 : (int)Math.Ceiling(total / (double)PacientesPorPagina);
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        var pacientes = pacientesOrdenados
            .Skip((pagina - 1) * PacientesPorPagina)
            .Take(PacientesPorPagina)
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

        return (pacientes, totalPaginas);
    }

    public async Task<IActionResult> Index()
    {
        var profissionalId = User.ObterProfissionalId();
        var profissional = await _context.Profissionais.FirstOrDefaultAsync(p => p.Id == profissionalId);

        if (profissional is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var (pacientes, totalPaginas) = await ObterPaginaPacientesAsync(profissional.Id, 1, null);

        ViewBag.ProfissionalId = profissional.Id;
        ViewBag.ProfissionalNome = profissional.NomeCompleto;
        ViewBag.ProfissionalFotoUrl = profissional.FotoUrl;
        ViewBag.ProfissionalCRP = profissional.RegistroCRP;

        var model = new PacientesIndexViewModel
        {
            Pacientes = pacientes,
            PaginaAtual = 1,
            TotalPaginas = totalPaginas
        };

        return View(model);
    }

    // Endpoint AJAX: paginação e busca (por nome ou CPF) da tabela "Meus Pacientes"
    [HttpGet]
    public async Task<IActionResult> BuscarPacientes(int pagina = 1, string? termo = null)
    {
        var profissionalId = User.ObterProfissionalId();

        var (pacientes, totalPaginas) = await ObterPaginaPacientesAsync(profissionalId, pagina, termo);

        var itens = pacientes.Select(p => new
        {
            id = p.Id,
            nomeCompleto = p.NomeCompleto,
            iniciais = p.Iniciais,
            telefone = p.Telefone,
            email = p.Email,
            dataNascimento = p.DataNascimento.ToString("dd/MM/yyyy"),
            idade = p.Idade,
            ativo = p.Ativo,
            url = Url.Action(nameof(Detalhes), new { id = p.Id })
        });

        return Json(new { success = true, pacientes = itens, paginaAtual = Math.Clamp(pagina, 1, totalPaginas), totalPaginas });
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
                Status = s.Status.ToString()
            })
            .ToListAsync();

        // Busca os Objetivos Terapêuticos trabalhados em todas as sessões do paciente numa única
        // consulta (evita N+1 de uma query por sessão) e depois agrupa em memória por SessaoId —
        // cada objetivo pertence a uma Anotação específica, então passa por AnotacaoSessao.SessaoId
        var objetivosTrabalhados = await _context.SessoesObjetivos
            .Where(so => so.AnotacaoSessao!.Sessao!.PacienteId == paciente.Id && so.AnotacaoSessao.Sessao.ProfissionalId == profissionalId)
            .Select(so => new
            {
                SessaoId = so.AnotacaoSessao!.SessaoId,
                Titulo = so.ObjetivoTerapeutico!.Titulo,
                so.Observacao
            })
            .ToListAsync();

        var objetivosPorSessaoId = objetivosTrabalhados
            .GroupBy(o => o.SessaoId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(o => new ObjetivoTrabalhadoViewModel { Titulo = o.Titulo, Observacao = o.Observacao }).ToList());

        // Mesma lógica de evitar N+1: busca as Anotações Clínicas de todas as sessões do paciente
        // numa única consulta e depois agrupa em memória por SessaoId
        var anotacoesDasSessoes = await _context.AnotacoesSessao
            .Where(a => a.Sessao!.PacienteId == paciente.Id && a.Sessao.ProfissionalId == profissionalId)
            .OrderByDescending(a => a.DataRegistro)
            .Select(a => new { a.SessaoId, a.Id, a.Titulo, a.Conteudo, a.DataRegistro })
            .ToListAsync();

        var anotacoesPorSessaoId = anotacoesDasSessoes
            .GroupBy(a => a.SessaoId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(a => new AnotacaoSessaoItemViewModel { Id = a.Id, Titulo = a.Titulo, Conteudo = a.Conteudo, DataRegistro = a.DataRegistro }).ToList());

        foreach (var sessao in sessoes)
        {
            if (objetivosPorSessaoId.TryGetValue(sessao.Id, out var objetivosDaSessao))
            {
                sessao.ObjetivosTrabalhados = objetivosDaSessao;
            }

            if (anotacoesPorSessaoId.TryGetValue(sessao.Id, out var anotacoesDaSessao))
            {
                sessao.Anotacoes = anotacoesDaSessao;
            }
        }

        var (anotacoesClinicas, totalPaginasAnotacoesClinicas) = await ObterPaginaAnotacoesClinicasAsync(
            paciente.Id, profissionalId, 1, null, null, null);

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
            Sessoes = sessoes,
            AnotacoesClinicas = anotacoesClinicas,
            PaginaAtualAnotacoesClinicas = 1,
            TotalPaginasAnotacoesClinicas = totalPaginasAnotacoesClinicas
        };

        return View(model);
    }

    // Pagina (10 por vez) as Anotações Clínicas (AnotacaoSessao) de TODAS as sessões do paciente,
    // reunindo num só lugar o que hoje só dá pra ver entrando sessão por sessão — reaproveitado pelo
    // carregamento inicial da Ficha do Paciente (Detalhes) e pelo endpoint AJAX (BuscarAnotacoesClinicas).
    // Sempre filtra por Sessao.ProfissionalId == profissionalId, nunca só por PacienteId.
    private async Task<(List<AnotacaoClinicaListItemViewModel> Anotacoes, int TotalPaginas)> ObterPaginaAnotacoesClinicasAsync(
        Guid pacienteId, Guid profissionalId, int pagina, string? busca, DateTime? dataInicio, DateTime? dataFim)
    {
        var consulta = _context.AnotacoesSessao
            .Where(a => a.Sessao!.PacienteId == pacienteId && a.Sessao.ProfissionalId == profissionalId);

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

    // Endpoint AJAX: paginação, busca por título e filtro por período da aba "Anotações Clínicas"
    [HttpGet]
    public async Task<IActionResult> BuscarAnotacoesClinicas(Guid pacienteId, int pagina = 1, string? busca = null, DateTime? dataInicio = null, DateTime? dataFim = null)
    {
        var profissionalId = User.ObterProfissionalId();

        var pacienteValido = await _vinculoService.PacientePertenceAoProfissionalAsync(pacienteId, profissionalId);

        if (!pacienteValido)
        {
            return Json(new { success = false, message = "Paciente não encontrado." });
        }

        var (anotacoes, totalPaginas) = await ObterPaginaAnotacoesClinicasAsync(pacienteId, profissionalId, pagina, busca, dataInicio, dataFim);

        var itens = anotacoes.Select(a => new
        {
            id = a.Id,
            titulo = a.Titulo,
            conteudo = a.Conteudo,
            dataRegistro = a.DataRegistro.ToString("dd/MM/yyyy HH:mm"),
            objetivos = a.Objetivos,
            sessaoId = a.SessaoId,
            sessaoCodigo = a.SessaoCodigo,
            sessaoDataHora = a.SessaoDataHora.ToString("dd/MM/yyyy"),
            // anotacaoId na query string permite que a tela da Sessão já abra com esta anotação
            // específica selecionada/destacada na lista "Anotações desta sessão"
            sessaoUrl = Url.Action("Sessao", "Sessoes", new { id = a.SessaoId, anotacaoId = a.Id })
        });

        return Json(new { success = true, anotacoes = itens, paginaAtual = Math.Clamp(pagina, 1, totalPaginas), totalPaginas });
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
    public async Task<IActionResult> VerificarPacienteExistente(string? cpf, string? cpfResponsavel)
    {
        // Paciente com Responsável (CARD-35): quando o CPF do paciente não foi informado (menor sem CPF
        // próprio), a busca é feita pelo CPF do Responsável — um mesmo responsável pode ter mais de um
        // filho cadastrado, então aqui pode haver mais de um resultado (ver bloco abaixo)
        if (string.IsNullOrWhiteSpace(cpf) && !string.IsNullOrWhiteSpace(cpfResponsavel))
        {
            return await VerificarPorCpfResponsavelAsync(cpfResponsavel);
        }

        if (!CpfUtil.EhValido(cpf))
        {
            return Json(new { existe = false, cpfValido = false });
        }

        var paciente = await BuscarPacientePorCpfAsync(cpf!);

        if (paciente is null)
        {
            return Json(new { existe = false, cpfValido = true });
        }

        return Json(new
        {
            existe = true,
            cpfValido = true,
            pacienteId = paciente.Id,
            nomeCompleto = paciente.NomeCompleto,
            iniciais = PacienteIniciais.Calcular(paciente.NomeCompleto)
        });
    }

    // Busca pacientes pelo CPF do Responsável normalizado — usado só quando o paciente (menor) não tem
    // CPF próprio informado na etapa de verificação. Se encontrar mais de um paciente com o mesmo
    // Responsável, não escolhe automaticamente: devolve a lista pro frontend perguntar "é um desses?"
    private async Task<IActionResult> VerificarPorCpfResponsavelAsync(string cpfResponsavel)
    {
        if (!CpfUtil.EhValido(cpfResponsavel))
        {
            return Json(new { existe = false, cpfValido = false });
        }

        var cpfResponsavelNormalizado = CpfUtil.Normalizar(cpfResponsavel);

        var pacientesDoResponsavel = await _context.Pacientes
            .Where(p => p.CpfResponsavel == cpfResponsavelNormalizado)
            .ToListAsync();

        if (pacientesDoResponsavel.Count == 0)
        {
            return Json(new { existe = false, cpfValido = true });
        }

        if (pacientesDoResponsavel.Count > 1)
        {
            return Json(new
            {
                existe = true,
                cpfValido = true,
                multiplos = true,
                pacientes = pacientesDoResponsavel.Select(p => new
                {
                    id = p.Id,
                    nomeCompleto = p.NomeCompleto,
                    iniciais = PacienteIniciais.Calcular(p.NomeCompleto)
                })
            });
        }

        var unicoPaciente = pacientesDoResponsavel[0];

        return Json(new
        {
            existe = true,
            cpfValido = true,
            pacienteId = unicoPaciente.Id,
            nomeCompleto = unicoPaciente.NomeCompleto,
            iniciais = PacienteIniciais.Calcular(unicoPaciente.NomeCompleto)
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
            // Paciente com Responsável (CARD-35): o CPF do paciente é opcional, então essa checagem só
            // faz sentido quando ele foi de fato informado — a etapa de verificação pelo CPF do Responsável
            // já cobre a duplicidade nesse outro caso.
            if (!string.IsNullOrWhiteSpace(model.Cpf))
            {
                var pacienteExistente = await BuscarPacientePorCpfAsync(model.Cpf);

                if (pacienteExistente is not null)
                {
                    return Json(new { success = false, message = "Já existe um paciente cadastrado com esse CPF. Feche e reabra o cadastro para verificar novamente." });
                }
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
                Profissao = model.Profissao,
                NecessitaResponsavel = model.NecessitaResponsavel,
                NomeResponsavel = model.NomeResponsavel,
                TelefoneResponsavel = model.TelefoneResponsavel,
                CpfResponsavel = CpfUtil.Normalizar(model.CpfResponsavel),
                ProfissaoResponsavel = model.ProfissaoResponsavel
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

        // Cada linha aqui é uma anotação que marcou este objetivo (não mais 1 linha por sessão) —
        // agora que Objetivos Trabalhados é por Anotação, a mesma sessão pode aparecer mais de uma
        // vez se objetivo foi marcado em mais de uma anotação dela
        var query = _context.SessoesObjetivos
            .Where(so => so.ObjetivoTerapeuticoId == objetivoId)
            .OrderByDescending(so => so.AnotacaoSessao!.Sessao!.DataHora);

        var totalSessoes = await query.CountAsync();
        var totalPaginas = totalSessoes == 0 ? 1 : (int)Math.Ceiling(totalSessoes / (double)tamanhoPagina);

        var sessoes = await query
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Select(so => new
            {
                sessaoId = so.AnotacaoSessao!.SessaoId,
                codigo = so.AnotacaoSessao.Sessao!.Codigo,
                dataHora = so.AnotacaoSessao.Sessao.DataHora.ToString("dd/MM/yyyy"),
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

        // A relação SessaoObjetivo -> ObjetivoTerapeutico usa DeleteBehavior.Restrict (ver
        // ApplicationDbContext.OnModelCreating) justamente pra não apagar objetivo já trabalhado em
        // sessões registradas — precisa ser checado aqui antes do Remove, senão o banco recusa a
        // exclusão e o profissional só via uma mensagem genérica de erro
        var possuiSessaoVinculada = await _context.SessoesObjetivos
            .AnyAsync(so => so.ObjetivoTerapeuticoId == id);

        if (possuiSessaoVinculada)
        {
            return Json(new { success = false, message = "Não é possível excluir: este objetivo já foi trabalhado em sessões registradas. Altere o status para \"Cancelado\" em vez de excluir, para preservar o histórico clínico." });
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
