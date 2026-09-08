using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MinhaSessao.Helpers;

// Valida que a data de nascimento informada garante idade mínima na data atual.
// Implementa IClientModelValidator para gerar os atributos data-val-* que o jQuery Validate Unobtrusive
// precisa — sem isso, um ValidationAttribute customizado só é checado no POST (servidor), nunca ao digitar,
// diferente de [Required]/[RegularExpression]/etc, que já têm suporte client-side nativo do ASP.NET Core.
// O método JS correspondente ("maiordeidade") é registrado em wwwroot/js/cadastro.js.
public class MaiorDeIdadeAttribute : ValidationAttribute, IClientModelValidator
{
    private readonly int _idadeMinima;

    public MaiorDeIdadeAttribute(int idadeMinima)
    {
        _idadeMinima = idadeMinima;
    }

    public override bool IsValid(object? value)
    {
        // [Required] cuida da ausência de valor; aqui só validamos quando há data informada
        if (value is not DateTime dataNascimento)
        {
            return true;
        }

        var hoje = DateTime.UtcNow.Date;
        var idade = hoje.Year - dataNascimento.Year;

        if (dataNascimento.Date > hoje.AddYears(-idade))
        {
            idade--;
        }

        return idade >= _idadeMinima;
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-maiordeidade", FormatErrorMessage(context.ModelMetadata.GetDisplayName()));
        MergeAttribute(context.Attributes, "data-val-maiordeidade-idademinima", _idadeMinima.ToString(CultureInfo.InvariantCulture));
    }

    private static void MergeAttribute(IDictionary<string, string> attributes, string chave, string valor)
    {
        if (!attributes.ContainsKey(chave))
        {
            attributes.Add(chave, valor);
        }
    }
}
