namespace MinhaSessao.Models.ViewModels;

// Normaliza CPF removendo tudo que não for dígito — usado como identificador de busca/duplicidade de Paciente
public static class CpfUtil
{
    public static string Normalizar(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return string.Empty;
        }

        return new string(cpf.Where(char.IsDigit).ToArray());
    }
}
