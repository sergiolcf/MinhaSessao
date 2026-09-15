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

    // Valida o CPF pelo algoritmo oficial de dígito verificador (módulo 11, não só formato/quantidade de dígitos)
    public static bool EhValido(string? cpf)
    {
        var digitos = Normalizar(cpf);

        if (digitos.Length != 11)
        {
            return false;
        }

        // CPFs com todos os dígitos iguais (ex.: 111.111.111-11) passam no cálculo do dígito
        // verificador mas nunca são emitidos de verdade — a Receita Federal os trata como inválidos
        if (digitos.Distinct().Count() == 1)
        {
            return false;
        }

        var numeros = digitos.Select(c => c - '0').ToArray();

        var primeiroDigitoVerificador = CalcularDigitoVerificador(numeros.Take(9).ToArray());
        if (primeiroDigitoVerificador != numeros[9])
        {
            return false;
        }

        var segundoDigitoVerificador = CalcularDigitoVerificador(numeros.Take(10).ToArray());
        return segundoDigitoVerificador == numeros[10];
    }

    // Algoritmo padrão do dígito verificador do CPF: peso decrescente a partir de (quantidade de dígitos base + 1)
    private static int CalcularDigitoVerificador(int[] digitosBase)
    {
        var peso = digitosBase.Length + 1;
        var soma = 0;

        foreach (var digito in digitosBase)
        {
            soma += digito * peso;
            peso--;
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
