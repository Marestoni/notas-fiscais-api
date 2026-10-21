using NotasFiscais.Api.Dtos;

namespace NotasFiscais.Api.Services;

public enum StatusCadastro
{
    Criada,
    NumeroDuplicado,
    DataEmissaoFutura
}

/// <summary>Resultado de <see cref="INotaFiscalService.Cadastrar"/>; <see cref="Nota"/> só vem preenchida em caso de sucesso.</summary>
public record ResultadoCadastro(StatusCadastro Status, NotaFiscalResponse? Nota = null)
{
    public static ResultadoCadastro Criada(NotaFiscalResponse nota) => new(StatusCadastro.Criada, nota);
    public static ResultadoCadastro NumeroDuplicado() => new(StatusCadastro.NumeroDuplicado);
    public static ResultadoCadastro DataEmissaoFutura() => new(StatusCadastro.DataEmissaoFutura);
}
