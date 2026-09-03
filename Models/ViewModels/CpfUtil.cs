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

    // Formata para exibição (000.000.000-00); usado só em telas somente-leitura, o valor salvo continua normalizado
    public static string Formatar(string? cpf)
    {
        var digitos = Normalizar(cpf);

        if (digitos.Length != 11)
        {
            return cpf ?? string.Empty;
        }

        return $"{digitos[..3]}.{digitos[3..6]}.{digitos[6..9]}-{digitos[9..]}";
    }
}
