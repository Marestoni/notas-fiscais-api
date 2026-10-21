using NotasFiscais.Api.Extensions;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddNotasFiscais();
builder.Services.AddHealthChecks();

// Hospedagens como o Render informam a porta pela variável PORT. Localmente ela não existe
// e valem as URLs do launchSettings.json.
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } porta)
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{porta}");
}

// Origens permitidas vêm da configuração (appsettings.Development.json em dev; em produção, a
// variável Cors__AllowedOrigins__0). Sem nenhuma origem configurada, o CORS fica desligado.
// A barra final é removida porque o navegador envia a origem sem ela.
var origensPermitidas = (builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .Select(o => o.TrimEnd('/'))
    .ToArray();
if (origensPermitidas.Length > 0)
{
    builder.Services.AddCors(o => o.AddPolicy(FrontendCorsPolicy, p => p
        .WithOrigins(origensPermitidas)
        .AllowAnyHeader()
        .AllowAnyMethod()));
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (origensPermitidas.Length > 0)
{
    app.UseCors(FrontendCorsPolicy);
}
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Necessário para WebApplicationFactory nos testes de integração.
public partial class Program;
