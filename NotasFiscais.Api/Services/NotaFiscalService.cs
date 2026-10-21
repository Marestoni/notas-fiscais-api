using System.Globalization;
using NotasFiscais.Api.Dtos;
using NotasFiscais.Api.Models;
using NotasFiscais.Api.Repositories;

namespace NotasFiscais.Api.Services;

public class NotaFiscalService(INotaFiscalRepository repository, TimeProvider timeProvider) : INotaFiscalService
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public IReadOnlyList<NotaFiscalResponse> Listar() =>
        repository.ObterTodas().Select(ParaResponse).ToList();

    public NotaFiscalResponse? ObterPorNumero(string numero) =>
        repository.ObterPorNumero(numero) is { } nota ? ParaResponse(nota) : null;

    public ResultadoCadastro Cadastrar(CriarNotaRequest request)
    {
        // "Hoje" é a data local do relógio injetado: a regra fica testável e independe do controller.
        var hoje = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        if (request.DataEmissao!.Value > hoje)
            return ResultadoCadastro.DataEmissaoFutura();

        var nota = new NotaFiscal
        {
            Numero = request.Numero!.Trim(),
            Cliente = request.Cliente!.Trim(),
            Valor = request.Valor!.Value,
            DataEmissao = request.DataEmissao!.Value,
            DataCadastro = timeProvider.GetUtcNow()
        };

        return repository.Adicionar(nota)
            ? ResultadoCadastro.Criada(ParaResponse(nota))
            : ResultadoCadastro.NumeroDuplicado();
    }

    private static NotaFiscalResponse ParaResponse(NotaFiscal n) => new(
        n.Numero,
        n.Cliente,
        n.Valor,
        n.Valor.ToString("C", PtBr),
        n.DataEmissao,
        n.DataCadastro);
}
