using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using NotasFiscais.Api.Dtos;

namespace NotasFiscais.Tests.Controllers;

// Testes de integração do pipeline HTTP completo (binding, DataAnnotations, controller, service, repositório).
// Cada teste cria sua própria factory, então o repositório Singleton em memória começa vazio.
public class NotasControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory = new();
    private readonly HttpClient _client;

    public NotasControllerTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static object NotaValida(string numero = "1001") =>
        new { numero, cliente = "Acme Ltda", valor = 1234.5m, dataEmissao = "2026-10-15" };

    private static async Task<JsonElement> ErrosDe(HttpResponseMessage resposta)
    {
        using var doc = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("errors").Clone();
    }

    [Fact]
    public async Task Post_DeveRetornar201ComLocationQueApontaParaANotaCriada()
    {
        var criada = await _client.PostAsJsonAsync("/api/notas", NotaValida());

        Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
        var location = criada.Headers.Location;
        Assert.NotNull(location);

        var busca = await _client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, busca.StatusCode);
        var nota = await busca.Content.ReadFromJsonAsync<NotaFiscalResponse>();
        Assert.Equal("1001", nota!.Numero);
    }

    [Fact]
    public async Task Post_DeveRetornar409QuandoNumeroDuplicado()
    {
        await _client.PostAsJsonAsync("/api/notas", NotaValida("2002"));

        var duplicada = await _client.PostAsJsonAsync("/api/notas", NotaValida("2002"));

        Assert.Equal(HttpStatusCode.Conflict, duplicada.StatusCode);
    }

    [Fact]
    public async Task Post_DeveRetornar400ComErroPorCampoQuandoCamposObrigatoriosAusentes()
    {
        var resposta = await _client.PostAsJsonAsync("/api/notas", new { });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var erros = await ErrosDe(resposta);
        foreach (var campo in new[] { "Numero", "Cliente", "Valor", "DataEmissao" })
            Assert.True(erros.TryGetProperty(campo, out _), $"Esperava erro para {campo}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1000000000000)] // acima do máximo
    [InlineData(1.005)]          // mais de 2 casas decimais
    public async Task Post_DeveRetornar400QuandoValorInvalido(double valor)
    {
        var nota = new { numero = "3003", cliente = "Acme", valor, dataEmissao = "2026-10-15" };

        var resposta = await _client.PostAsJsonAsync("/api/notas", nota);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.True((await ErrosDe(resposta)).TryGetProperty("Valor", out _));
    }

    [Fact]
    public async Task Post_DeveRetornar400QuandoDataDeEmissaoFutura()
    {
        var nota = new { numero = "4004", cliente = "Acme", valor = 10, dataEmissao = "2999-01-01" };

        var resposta = await _client.PostAsJsonAsync("/api/notas", nota);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.True((await ErrosDe(resposta)).TryGetProperty("DataEmissao", out _));
    }

    [Fact]
    public async Task Post_DeveRetornar400SemQuebrarQuandoValorEstouraDecimal()
    {
        var corpo = new StringContent(
            """{"numero":"5005","cliente":"Acme","valor":1e30,"dataEmissao":"2026-10-15"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var resposta = await _client.PostAsync("/api/notas", corpo);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Get_DeveListarAsNotasComValorFormatado()
    {
        await _client.PostAsJsonAsync("/api/notas", NotaValida());

        var notas = await _client.GetFromJsonAsync<List<NotaFiscalResponse>>("/api/notas");

        var nota = Assert.Single(notas!);
        Assert.Equal(1234.5m, nota.Valor);
        Assert.Equal("R$ 1.234,50", nota.ValorFormatado.Replace(' ', ' '));
    }

    [Theory]
    [InlineData("http://localhost:5173", true)]
    [InlineData("http://origem-nao-configurada.com", false)]
    public async Task Cors_DevePermitirApenasAsOrigensConfiguradas(string origem, bool permitida)
    {
        // Barra final na configuração é tolerada: o navegador envia a origem sem ela.
        using var factory = _factory.WithWebHostBuilder(b => b.UseSetting("Cors:AllowedOrigins:0", "http://localhost:5173/"));
        using var client = factory.CreateClient();
        var requisicao = new HttpRequestMessage(HttpMethod.Get, "/api/notas");
        requisicao.Headers.Add("Origin", origem);

        var resposta = await client.SendAsync(requisicao);

        Assert.Equal(permitida, resposta.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Cors_FicaDesligadoQuandoNenhumaOrigemEhConfigurada()
    {
        using var factory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Production"));
        using var client = factory.CreateClient();
        var requisicao = new HttpRequestMessage(HttpMethod.Get, "/api/notas");
        requisicao.Headers.Add("Origin", "http://localhost:5173");

        var resposta = await client.SendAsync(requisicao);

        Assert.False(resposta.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Health_DeveResponder200()
    {
        var resposta = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task GetPorNumero_DeveRetornar404QuandoNaoExiste()
    {
        var resposta = await _client.GetAsync("/api/notas/inexistente");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
