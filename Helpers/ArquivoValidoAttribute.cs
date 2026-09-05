using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MinhaSessao.Helpers;

// Valida tamanho máximo e extensão de um IFormFile opcional (ex.: foto de perfil do Profissional).
// Ausência de arquivo é sempre válida — quem exige presença é [Required], se for o caso.
public class ArquivoValidoAttribute : ValidationAttribute
{
    private readonly long _tamanhoMaximoBytes;
    private readonly string[] _extensoesPermitidas;

    public ArquivoValidoAttribute(long tamanhoMaximoBytes, string[] extensoesPermitidas)
    {
        _tamanhoMaximoBytes = tamanhoMaximoBytes;
        _extensoesPermitidas = extensoesPermitidas;
    }

    // Sobrescreve o overload que recebe ValidationContext (em vez de mutar ErrorMessage em IsValid(object))
    // para não compartilhar estado mutável entre requisições concorrentes — a instância do atributo
    // é reaproveitada pelo cache de metadados do ASP.NET Core.
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IFormFile arquivo || arquivo.Length == 0)
        {
            return ValidationResult.Success;
        }

        var membro = new[] { validationContext.MemberName ?? string.Empty };

        if (arquivo.Length > _tamanhoMaximoBytes)
        {
            var tamanhoMaximoMb = _tamanhoMaximoBytes / (1024 * 1024);
            return new ValidationResult($"O arquivo deve ter no máximo {tamanhoMaximoMb}MB.", membro);
        }

        var extensao = Path.GetExtension(arquivo.FileName);
        if (string.IsNullOrEmpty(extensao) || !_extensoesPermitidas.Contains(extensao, StringComparer.OrdinalIgnoreCase))
        {
            return new ValidationResult($"Extensões permitidas: {string.Join(", ", _extensoesPermitidas)}.", membro);
        }

        return ValidationResult.Success;
    }
}
