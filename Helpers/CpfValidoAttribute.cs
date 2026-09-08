using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MinhaSessao.Helpers;

// Valida CPF pelo algoritmo oficial de dígito verificador (não só o formato/quantidade de dígitos),
// aceitando o valor mascarado (000.000.000-00) ou só os dígitos. Implementa IClientModelValidator
// para funcionar como validação ao vivo (data-val-cpfvalido), igual MaiorDeIdadeAttribute — o método
// JS correspondente ("cpfvalido") é registrado em wwwroot/js/cadastro.js.
public class CpfValidoAttribute : ValidationAttribute, IClientModelValidator
{
    public override bool IsValid(object? value)
    {
        // [Required] cuida da ausência de valor
        if (value is not string cpfInformado || string.IsNullOrWhiteSpace(cpfInformado))
        {
            return true;
        }

        return CpfEhValido(cpfInformado);
    }

    public static bool CpfEhValido(string cpf)
    {
        var digitos = new string(cpf.Where(char.IsDigit).ToArray());

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

    public void AddValidation(ClientModelValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-cpfvalido", FormatErrorMessage(context.ModelMetadata.GetDisplayName()));
    }

    private static void MergeAttribute(IDictionary<string, string> attributes, string chave, string valor)
    {
        if (!attributes.ContainsKey(chave))
        {
            attributes.Add(chave, valor);
        }
    }
}
