namespace MinhaSessao.Models.ViewModels;

// Calcula as iniciais exibidas no avatar do paciente (primeira letra do primeiro e do último nome)
public static class PacienteIniciais
{
    public static string Calcular(string nomeCompleto)
    {
        var partes = nomeCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (partes.Length == 0)
        {
            return string.Empty;
        }

        if (partes.Length == 1)
        {
            return partes[0][..Math.Min(2, partes[0].Length)].ToUpperInvariant();
        }

        return $"{partes[0][0]}{partes[^1][0]}".ToUpperInvariant();
    }
}
