using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using MinhaSessao.Models.Entities;

namespace MinhaSessao.Services;

// Centraliza a criação do Cookie de autenticação a partir de um Profissional/Paciente
// e o hash/verificação de senha, usados tanto no login quanto no cadastro.
public static class AutenticacaoService
{
    // Valores do Claim de papel (ClaimTypes.Role), usados em [Authorize(Roles = "...")]
    public const string PapelProfissional = "Profissional";
    public const string PapelPaciente = "Paciente";

    private static readonly PasswordHasher<Profissional> Hasher = new();
    private static readonly PasswordHasher<Paciente> HasherPaciente = new();

    // Sem caracteres ambíguos (0/O, 1/l/I) para facilitar a digitação manual caso o profissional não copie a senha
    private const string CaracteresSenhaTemporaria = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

    public static string GerarSenhaTemporaria(int tamanho = 10)
    {
        return RandomNumberGenerator.GetString(CaracteresSenhaTemporaria, tamanho);
    }

    public static string HashSenha(Profissional profissional, string senha)
    {
        return Hasher.HashPassword(profissional, senha);
    }

    public static string HashSenhaPaciente(Paciente paciente, string senha)
    {
        return HasherPaciente.HashPassword(paciente, senha);
    }

    public static bool VerificarSenha(Profissional profissional, string senhaInformada)
    {
        var resultado = Hasher.VerifyHashedPassword(profissional, profissional.Senha, senhaInformada);
        return resultado is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }

    public static bool VerificarSenhaPaciente(Paciente paciente, string senhaInformada)
    {
        var resultado = HasherPaciente.VerifyHashedPassword(paciente, paciente.Senha, senhaInformada);
        return resultado is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }

    public static async Task AutenticarProfissionalAsync(HttpContext httpContext, Profissional profissional, bool lembrarMe = false)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, profissional.Id.ToString()),
            new(ClaimTypes.Name, profissional.NomeCompleto),
            new(ClaimTypes.Email, profissional.Email),
            new(ClaimTypes.Role, PapelProfissional)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = lembrarMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        });
    }

    public static async Task AutenticarPacienteAsync(HttpContext httpContext, Paciente paciente, bool lembrarMe = false)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, paciente.Id.ToString()),
            new(ClaimTypes.Name, paciente.NomeCompleto),
            new(ClaimTypes.Email, paciente.Email),
            new(ClaimTypes.Role, PapelPaciente)
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
