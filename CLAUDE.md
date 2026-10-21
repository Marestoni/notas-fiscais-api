# UneCont – Notas Fiscais · Backend

Desafio técnico da UneCont (vaga Dev FullStack Pleno/Sênior). API REST em .NET 8 para cadastrar e listar notas fiscais, armazenando em memória (sem banco). O frontend React fica em outro repositório (`../frontend`). Publicação: **API no Render** (Docker) e **site no Vercel**.

Escopo fechado: `POST /api/notas`, `GET /api/notas`, `GET /api/notas/{numero}` (alvo do `Location` do POST) e `GET /health`. Não ampliar o escopo sem o usuário pedir.

## Comandos
- Rodar: `dotnet run --project NotasFiscais.Api --launch-profile http` (http://localhost:5245, Swagger em `/swagger`)
- Testar: `dotnet test`
- Se uma instância da API estiver rodando, o executável fica travado e o build falha: use `dotnet test --artifacts-path <pasta-temporária>` em vez de encerrar o processo (pode ser a API que o usuário está testando).

## Arquitetura
```
NotasFiscais.Api/
  Controllers/   NotasController: só HTTP; traduz ResultadoCadastro em 201/400/409
  Services/      INotaFiscalService + NotaFiscalService: regras de negócio; ResultadoCadastro (resultado tipado)
  Repositories/  INotaFiscalRepository + NotaFiscalMemoryRepository (Singleton, ConcurrentDictionary)
  Models/        NotaFiscal (entidade)
  Dtos/          CriarNotaRequest, NotaFiscalResponse
  Validation/    MaxCasasDecimaisAttribute
  Extensions/    AddNotasFiscais() registra a DI
NotasFiscais.Tests/
  Services/      testes unitários do Service (FakeTimeProvider)
  Controllers/   testes de integração com WebApplicationFactory<Program>
Dockerfile · .dockerignore · render.yaml   deploy no Render
```

## Regras do projeto
- Camadas: Controller → Service → Repository, sempre por interface + DI. A entidade nunca vai direto na resposta; usar DTOs.
- Regras de negócio (data de emissão não futura, número único) ficam no **Service**, não no Controller.
- Dinheiro é sempre `decimal`. A resposta traz `valor` (número) e `valorFormatado` (pt-BR); o front ordena pelo número.
- O repositório em memória é Singleton e thread-safe; a duplicidade é decidida atomicamente por `TryAdd`.
- Tempo sempre via `TimeProvider` injetado (nunca `DateTime.Now`), para os testes serem determinísticos.
- Não usar `Range(typeof(decimal), "…")` com string: o parse depende da cultura da máquina e quebra em pt-BR (já causou 500). Usar a sobrecarga numérica.
- SDK fixado em .NET 8 (`global.json`).

## Ambiente: local x produção
- **Local:** `ASPNETCORE_ENVIRONMENT=Development` (pelo `launchSettings.json`), porta 5245, CORS para `http://localhost:5173` vindo do `appsettings.Development.json`.
- **Produção (Render):** `Production` (definido no Dockerfile); a porta vem da variável `PORT` (lida em `Program.cs`); o Swagger fica desligado.
- **CORS:** vem só de configuração (`Cors:AllowedOrigins`). O `appsettings.json` base fica **vazio de propósito** (sem origem = CORS desligado); em produção definir `Cors__AllowedOrigins__0` = URL do Vercel, sem barra no final. Nunca fixar origem no código nem colocar `localhost` no `appsettings.json` base.
- Sem segredos neste projeto. Se algum aparecer, vai em variável de ambiente do Render, nunca no git.
- Render free: o serviço dorme e perde os dados em memória ao reiniciar (esperado; está no README).

## Antes de commitar ou publicar (rodar sempre)
1. `dotnet test` — tudo verde.
2. `/code-review` — corrigir o que for real; registrar o que foi deixado de lado.
3. `/security-review` — se a skill falhar por o repositório não ter commit/remote (`origin/HEAD`), fazer a revisão manual: `dotnet list NotasFiscais.Api package --vulnerable --include-transitive`, segredos e `.env` fora do git, mass assignment, Swagger/erros em produção.
4. `/simplify` — aplicar as simplificações seguras.
5. Conferir o ambiente: o `appsettings.json` base sem origens de CORS, `Dockerfile`/`render.yaml` coerentes com `Program.cs` (porta e `/health`).
6. Atualizar a seção **"Qualidade e revisões"** do README com o resultado real (nº de testes, achados, o que não foi executado).

Mudou regra de validação ou de negócio: atualizar o teste e o front (`../frontend/src/utils/validacao.ts` espelha o DTO). Mudou o contrato da API: atualizar DTOs, testes de integração e README.

## Git
- Não commitar nem dar push sem pedido explícito do usuário (ele testa localmente primeiro).
