using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinhaSessao.Data;
using MinhaSessao.Models.Entities;
using MinhaSessao.Models.ViewModels;
using MinhaSessao.Services;

namespace MinhaSessao.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<AccountController> _logger;

    public AccountController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, ILogger<AccountController> logger)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", User.IsInRole(AutenticacaoService.PapelPaciente) ? "PainelPaciente" : "Dashboard");
        }

        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var emailNormalizado = model.Email.Trim().ToLower();
        var senhaInformada = model.Senha.Trim();

        // O e-mail pode pertencer a um Profissional ou a um Paciente; identifica antes de autenticar
        var profissional = await _context.Profissionais
            .FirstOrDefaultAsync(p => p.Email.ToLower() == emailNormalizado);

        if (profissional is not null && AutenticacaoService.VerificarSenha(profissional, senhaInformada))
        {
            await AutenticacaoService.AutenticarProfissionalAsync(HttpContext, profissional, model.LembrarMe);
            return RedirectToAction("Index", "Dashboard");
        }

        var paciente = await _context.Pacientes
            .FirstOrDefaultAsync(p => p.Email.ToLower() == emailNormalizado);

        if (paciente is not null && AutenticacaoService.VerificarSenhaPaciente(paciente, senhaInformada))
        {
            await AutenticacaoService.AutenticarPacienteAsync(HttpContext, paciente, model.LembrarMe);
            return RedirectToAction("Index", "PainelPaciente");
        }

        ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
        return View(model);
    }

    // Tela única de Cadastro com abas Profissional/Paciente; perfil inicial via querystring (?perfil=paciente), default "profissional"
    [HttpGet]
    public IActionResult Cadastro(string? perfil)
    {
        var model = new CadastroViewModel
        {
            PerfilAtivo = perfil == "paciente" ? "paciente" : "profissional",
            Profissional = new ProfissionalViewModel(),
            Paciente = new PacienteCadastroViewModel()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CadastroProfissional(ProfissionalViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return ViewCadastro("profissional", model, new PacienteCadastroViewModel());
        }

        try
        {
            string? fotoUrl = null;

            // Faz o upload da foto de perfil, se enviada, com nome único para evitar colisões
            if (model.Foto is { Length: > 0 })
            {
                var pastaUploads = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profissionais");
                Directory.CreateDirectory(pastaUploads);

                var nomeArquivo = $"{Guid.NewGuid()}{Path.GetExtension(model.Foto.FileName)}";
                var caminhoCompleto = Path.Combine(pastaUploads, nomeArquivo);

                using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
                {
                    await model.Foto.CopyToAsync(stream);
                }

                fotoUrl = $"/uploads/profissionais/{nomeArquivo}";
            }

            var profissional = new Profissional
            {
                Id = Guid.NewGuid(),
                NomeCompleto = model.NomeCompleto,
                RegistroCRP = model.RegistroCRP,
                Email = model.Email.Trim().ToLower(),
                Telefone = model.Telefone,
                Apresentacao = model.Apresentacao,
                FotoUrl = fotoUrl
            };
            profissional.Senha = AutenticacaoService.HashSenha(profissional, model.Senha.Trim());

            _context.Profissionais.Add(profissional);
            await _context.SaveChangesAsync();

            // Autentica automaticamente o profissional recém-cadastrado para acessar o Dashboard
            await AutenticacaoService.AutenticarProfissionalAsync(HttpContext, profissional);

            return RedirectToAction("Index", "Dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cadastrar profissional.");
            ModelState.AddModelError(string.Empty, "Ocorreu um erro ao salvar o cadastro. Tente novamente.");
            return ViewCadastro("profissional", model, new PacienteCadastroViewModel());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CadastroPaciente(PacienteCadastroViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return ViewCadastro("paciente", new ProfissionalViewModel(), model);
        }

        var emailNormalizado = model.Email.Trim().ToLower();

        // Evita duplicar Paciente: o e-mail é o identificador de login
        var pacienteExistente = await _context.Pacientes
            .FirstOrDefaultAsync(p => p.Email.ToLower() == emailNormalizado);

        if (pacienteExistente is not null)
        {
            ModelState.AddModelError(string.Empty, "Este e-mail já está cadastrado. Se você já tem uma senha (temporária ou não), faça login. Caso tenha esquecido, peça ao seu profissional para gerar uma nova.");
            return ViewCadastro("paciente", new ProfissionalViewModel(), model);
        }

        try
        {
            var paciente = new Paciente
            {
                Id = Guid.NewGuid(),
                NomeCompleto = model.NomeCompleto,
                Telefone = new string(model.Telefone.Where(char.IsDigit).ToArray()),
                Email = emailNormalizado,
                DataNascimento = model.DataNascimento,
                Cpf = CpfUtil.Normalizar(model.Cpf)
            };

            paciente.Senha = AutenticacaoService.HashSenhaPaciente(paciente, model.Senha.Trim());

            // Não cria Vínculo aqui: vínculo com um profissional é sempre criado pelo profissional
            _context.Pacientes.Add(paciente);
            await _context.SaveChangesAsync();

            await AutenticacaoService.AutenticarPacienteAsync(HttpContext, paciente);

            return RedirectToAction("Index", "PainelPaciente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao autocadastrar paciente.");
            ModelState.AddModelError(string.Empty, "Ocorreu um erro ao concluir o cadastro. Tente novamente.");
            return ViewCadastro("paciente", new ProfissionalViewModel(), model);
        }
    }

    // Reconstrói a tela de Cadastro (mesma view para as duas abas) preservando os dados já digitados
    private IActionResult ViewCadastro(string perfilAtivo, ProfissionalViewModel profissional, PacienteCadastroViewModel paciente)
    {
        return View("Cadastro", new CadastroViewModel
        {
            PerfilAtivo = perfilAtivo,
            Profissional = profissional,
            Paciente = paciente
        });
    }

    // Ação rápida de desenvolvimento: loga automaticamente com o primeiro profissional cadastrado
    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> LoginSimuladoTeste()
    {
        var profissional = await _context.Profissionais.FirstOrDefaultAsync();

        if (profissional is null)
        {
            ModelState.AddModelError(string.Empty, "Nenhum profissional cadastrado para o login de teste.");
            return View("Login", new LoginViewModel());
        }

        await AutenticacaoService.AutenticarProfissionalAsync(HttpContext, profissional);

        return RedirectToAction("Index", "Dashboard");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
