using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using MinhaSessao.Models.ViewModels;

namespace MinhaSessao.Helpers;

// Valida CPF pelo algoritmo oficial de dígito verificador (não só o formato/quantidade de dígitos),
// aceitando o valor mascarado (000.000.000-00) ou só os dígitos — algoritmo centralizado em CpfUtil.EhValido.
// Implementa IClientModelValidator para funcionar como validação ao vivo (data-val-cpfvalido), igual
// MaiorDeIdadeAttribute — o método JS correspondente ("cpfvalido") é registrado em wwwroot/js/cadastro.js.
public class CpfValidoAttribute : ValidationAttribute, IClientModelValidator
{
    public override bool IsValid(object? value)
    {
        // [Required] cuida da ausência de valor
        if (value is not string cpfInformado || string.IsNullOrWhiteSpace(cpfInformado))
        {
            return true;
        }

        return CpfUtil.EhValido(cpfInformado);
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
