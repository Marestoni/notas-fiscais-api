namespace NotasFiscais.Api.Models;

public class NotaFiscal
{
    public required string Numero { get; init; }
    public required string Cliente { get; init; }
    public required decimal Valor { get; init; }
    public required DateOnly DataEmissao { get; init; }
    public DateTimeOffset DataCadastro { get; init; }
}
