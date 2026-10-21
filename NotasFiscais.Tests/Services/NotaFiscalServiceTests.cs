using Microsoft.Extensions.Time.Testing;
using NotasFiscais.Api.Dtos;
using NotasFiscais.Api.Repositories;
using NotasFiscais.Api.Services;

namespace NotasFiscais.Tests.Services;

public class NotaFiscalServiceTests
{
    // O FakeTimeProvider usa UTC como fuso local, então "hoje" aqui é 2026-10-20.
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero));
    private readonly NotaFiscalService _service;

    public NotaFiscalServiceTests() =>
        _service = new NotaFiscalService(new NotaFiscalMemoryRepository(), _time);

    private static CriarNotaRequest Request(string numero = "1001", decimal valor = 1234.5m, DateOnly? emissao = null) =>
        new(numero, "  Cliente Teste ", valor, emissao ?? new DateOnly(2026, 10, 15));

    [Fact]
    public void Cadastrar_DeveGerarDataCadastroEAplicarTrimNosTextos()
    {
        var resultado = _service.Cadastrar(Request());

        Assert.Equal(StatusCadastro.Criada, resultado.Status);
        Assert.Equal(_time.GetUtcNow(), resultado.Nota!.DataCadastro);
        Assert.Equal("Cliente Teste", resultado.Nota.Cliente);
    }

    [Fact]
    public void Cadastrar_DeveRecusarNumeroDuplicado()
    {
        _service.Cadastrar(Request("2002"));

        var duplicada = _service.Cadastrar(Request("2002"));

        Assert.Equal(StatusCadastro.NumeroDuplicado, duplicada.Status);
        Assert.Null(duplicada.Nota);
        Assert.Single(_service.Listar());
    }

    [Fact]
    public void Cadastrar_DeveRecusarDataDeEmissaoFuturaSemGravar()
    {
        var amanha = new DateOnly(2026, 10, 21);

        var resultado = _service.Cadastrar(Request(emissao: amanha));

        Assert.Equal(StatusCadastro.DataEmissaoFutura, resultado.Status);
        Assert.Null(resultado.Nota);
        Assert.Empty(_service.Listar());
    }

    [Fact]
    public void Cadastrar_DeveAceitarDataDeEmissaoDeHoje()
    {
        var hoje = new DateOnly(2026, 10, 20);

        var resultado = _service.Cadastrar(Request(emissao: hoje));

        Assert.Equal(StatusCadastro.Criada, resultado.Status);
    }

    [Fact]
    public void Listar_DeveFormatarValorComoMoedaPtBrMantendoNumerico()
    {
        _service.Cadastrar(Request(valor: 1234.5m));

        var nota = Assert.Single(_service.Listar());

        Assert.Equal(1234.5m, nota.Valor);
        // O .NET separa o símbolo do valor com espaço não separável (U+00A0).
        Assert.Equal("R$ 1.234,50", nota.ValorFormatado.Replace(' ', ' '));
    }

    [Fact]
    public void Listar_DeveDesempatarPorNumeroQuandoDataDeCadastroForIgual()
    {
        // Mesma data de cadastro (relógio parado): a ordem não pode depender do dicionário.
        foreach (var numero in new[] { "C", "A", "B" })
            _service.Cadastrar(Request(numero));

        Assert.Equal(["A", "B", "C"], _service.Listar().Select(n => n.Numero));
    }

    [Fact]
    public void ObterPorNumero_DeveRetornarANotaOuNull()
    {
        _service.Cadastrar(Request("3003"));

        Assert.Equal("3003", _service.ObterPorNumero("3003")?.Numero);
        Assert.Null(_service.ObterPorNumero("inexistente"));
    }

    [Fact]
    public void Listar_DeveOrdenarPorDataDeCadastro()
    {
        _service.Cadastrar(Request("A"));
        _time.Advance(TimeSpan.FromMinutes(1));
        _service.Cadastrar(Request("B"));

        Assert.Equal(["A", "B"], _service.Listar().Select(n => n.Numero));
    }
}
