namespace NotasFiscais.Api.Dtos;

public record NotaFiscalResponse(
    string Numero,
    string Cliente,
    decimal Valor,
    string ValorFormatado,
    DateOnly DataEmissao,
    DateTimeOffset DataCadastro);
