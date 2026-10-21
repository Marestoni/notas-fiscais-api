using NotasFiscais.Api.Dtos;

namespace NotasFiscais.Api.Services;

public interface INotaFiscalService
{
    IReadOnlyList<NotaFiscalResponse> Listar();

    /// <returns>null quando não existe nota com o número informado.</returns>
    NotaFiscalResponse? ObterPorNumero(string numero);

    /// <summary>
    /// Aplica as regras de negócio (data de emissão não futura, número único) e cadastra a nota.
    /// </summary>
    ResultadoCadastro Cadastrar(CriarNotaRequest request);
}
