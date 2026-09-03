using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using MinhaSessao.Models.Entities;

namespace MinhaSessao.Services;

// Centraliza a criação do Cookie de autenticação a partir de um Profissional
// e o hash/verificação de senha, usados tanto no login quanto no cadastro.
public static class AutenticacaoService
{
    private static readonly PasswordHasher<Profissional> Hasher = new();

    public static string HashSenha(Profissional profissional, string senha)
    {
        return Hasher.HashPassword(profissional, senha);
    }

    public static bool VerificarSenha(Profissional profissional, string senhaInformada)
    {
        var resultado = Hasher.VerifyHashedPassword(profissional, profissional.Senha, senhaInformada);
        return resultado is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }

    public static async Task AutenticarProfissionalAsync(HttpContext httpContext, Profissional profissional, bool lembrarMe = false)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, profissional.Id.ToString()),
            new(ClaimTypes.Name, profissional.NomeCompleto),
            new(ClaimTypes.Email, profissional.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = lembrarMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        });
    }
}
