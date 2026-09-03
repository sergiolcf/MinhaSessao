using Microsoft.AspNetCore.Mvc;
using MinhaSessao.Data;
using MinhaSessao.Models.Entities;
using MinhaSessao.Models.ViewModels;
using MinhaSessao.Services;

namespace MinhaSessao.Controllers;

public class ProfissionalController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<ProfissionalController> _logger;

    public ProfissionalController(
        ApplicationDbContext context,
        IWebHostEnvironment webHostEnvironment,
        ILogger<ProfissionalController> logger)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(ProfissionalViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var erros = ModelState
                .Where(par => par.Value?.Errors.Count > 0)
                .ToDictionary(
                    par => par.Key,
                    par => par.Value!.Errors.Select(erro => erro.ErrorMessage).ToArray());

            return Json(new { success = false, message = "Verifique os campos destacados.", errors = erros });
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

            var redirectUrl = Url.Action("Index", "Dashboard");

            return Json(new { success = true, message = "Profissional cadastrado com sucesso!", redirectUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cadastrar profissional.");
            return Json(new { success = false, message = "Ocorreu um erro ao salvar o cadastro. Tente novamente." });
        }
    }
}
