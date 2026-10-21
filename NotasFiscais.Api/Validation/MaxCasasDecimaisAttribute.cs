using System.ComponentModel.DataAnnotations;

namespace NotasFiscais.Api.Validation;

/// <summary>
/// Rejeita valores monetários com mais casas decimais do que o permitido, para que o valor
/// armazenado seja o mesmo que é exibido (<c>1.005</c> não vira "R$ 1,01" na tela e 1.005 na ordenação).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Field)]
public sealed class MaxCasasDecimaisAttribute(int casas) : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is not decimal valor || decimal.Round(valor, casas) == valor;

    public override string FormatErrorMessage(string name) =>
        $"O valor deve ter no máximo {casas} casas decimais.";
}
