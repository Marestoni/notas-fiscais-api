using NotasFiscais.Api.Models;

namespace NotasFiscais.Api.Repositories;

public interface INotaFiscalRepository
{
    IReadOnlyList<NotaFiscal> ObterTodas();

    NotaFiscal? ObterPorNumero(string numero);

    /// <returns>false quando já existe nota com o mesmo número.</returns>
    bool Adicionar(NotaFiscal nota);
}
