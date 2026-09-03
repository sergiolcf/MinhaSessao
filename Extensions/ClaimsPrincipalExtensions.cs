using System.Security.Claims;

namespace MinhaSessao.Extensions;

public static class ClaimsPrincipalExtensions
{
    // Lê o ProfissionalId do profissional autenticado a partir do Cookie/Claims
    public static Guid ObterProfissionalId(this ClaimsPrincipal usuario)
    {
        return Guid.Parse(usuario.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    // Lê o PacienteId do paciente autenticado a partir do Cookie/Claims
    public static Guid ObterPacienteId(this ClaimsPrincipal usuario)
    {
        return Guid.Parse(usuario.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
