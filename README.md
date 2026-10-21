# UneCont – Notas Fiscais · Backend

API REST em .NET 8 (Controllers) para cadastro e listagem de notas fiscais, com armazenamento em memória.

> Este repositório é a metade backend do desafio. O frontend (React) está em:
> **[notas-fiscais-web](https://github.com/Marestoni/notas-fiscais-web)** — veja o README dele para rodar os dois juntos.

## Como rodar (local)

```bash
dotnet run --project NotasFiscais.Api --launch-profile http   # http://localhost:5245 (Swagger em /swagger)
dotnet test
```

Requer o SDK do .NET 8 (`global.json` fixa a versão mínima do SDK).

## Estrutura

```
NotasFiscais.Api/    Controllers → Services → Repositories, + Models, Dtos, Validation, Extensions (DI)
NotasFiscais.Tests/  xUnit: testes do Service e testes de integração do controller (WebApplicationFactory)
Dockerfile           imagem de produção (usada pelo Render)
render.yaml          blueprint do Render
```

## API

| Método | Rota | Descrição | Respostas |
|---|---|---|---|
| POST | `/api/notas` | Cadastra nota | 201 (+`Location`), 400, 409 (número duplicado) |
| GET | `/api/notas` | Lista notas | 200 |
| GET | `/api/notas/{numero}` | Busca uma nota (alvo do `Location` do POST) | 200, 404 |
| GET | `/health` | Verificação de saúde (usada pelo Render) | 200 |

Regras do `valor`: entre 0,01 e 999.999.999.999,99 e no máximo 2 casas decimais (o valor armazenado é o mesmo que é exibido e ordenado).
Regras de negócio (no Service): a data de emissão não pode ser futura e o número da nota é único.

Resposta de nota: `numero`, `cliente`, `valor` (numérico), `valorFormatado` (pt-BR), `dataEmissao`, `dataCadastro` (UTC).

## Ambientes e variáveis

| Variável | Local | Produção (Render) |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` (pelo `launchSettings.json`) | `Production` (definida no Dockerfile) |
| `PORT` | não usada (porta 5245) | definida pelo Render; a API escuta nela |
| `Cors__AllowedOrigins__0` | `http://localhost:5173` (vem do `appsettings.Development.json`) | **obrigatória**: URL do site no Vercel, sem barra no final |

- Em produção o CORS começa **desligado**: sem `Cors__AllowedOrigins__0`, o navegador bloqueia o frontend. Várias origens: `Cors__AllowedOrigins__1`, `__2`…
- O Swagger só liga em `Development`.
- Nenhum segredo é necessário: a API não tem banco nem chaves.

## Deploy no Render

1. No Render: **New > Blueprint** apontando para este repositório (usa o `render.yaml`), ou **New > Web Service** com runtime Docker.
2. Defina `Cors__AllowedOrigins__0` com a URL do frontend no Vercel (ex.: `https://notas-fiscais.vercel.app`). Se o frontend ainda não existe, publique-o antes ou volte aqui depois.
3. O health check é `/health`. Anote a URL da API (`https://<nome>.onrender.com`) para configurar `VITE_API_URL` no frontend.

Atenção ao plano gratuito: o serviço **dorme após alguns minutos sem uso** (a primeira chamada leva cerca de um minuto) e, como os dados ficam em memória, **as notas são perdidas** quando ele reinicia ou dorme.

## Decisões

- **Repositório em memória:** `ConcurrentDictionary` + Singleton, atrás de `INotaFiscalRepository`. `TryAdd` garante duplicidade atômica; trocar por EF Core é registrar outra implementação.
- **Dinheiro:** `Valor` é `decimal`. A resposta traz o número (`valor`) e a versão formatada (`valorFormatado`) para o cliente poder ordenar pelo número.
- **Regras de negócio no Service:** o Service devolve um `ResultadoCadastro` tipado (criada, número duplicado, data futura) e o Controller só traduz em 201/409/400. Assim a regra vale para qualquer chamador e é testável sem HTTP.
- **Tempo:** `TimeProvider` injetado para `DataCadastro` (UTC) e para "hoje" (data local do servidor), o que deixa os testes determinísticos.
- **CORS por configuração**, sem origem fixa no código; a barra final é removida porque o navegador envia a origem sem ela.
- **Ordenação da listagem** por `DataCadastro`, com desempate pelo número da nota (o dicionário não garante ordem).
- **Armadilha evitada:** `Range(typeof(decimal), "0.01", ...)` faz o parse com a cultura da máquina e quebra em pt-BR; foi usada a sobrecarga numérica.

## Qualidade e revisões

- **Testes:** 23 (unitários do Service e integração do controller: 201/400/404/409, valores inválidos, data futura, estouro de `decimal`, CORS e `/health`).
- **Code review** (skill `code-review`, nível high): 10 achados; 8 corrigidos (erro 400 silencioso no front, validação do valor, `Location` do 201, teste do controller, CORS por configuração, desempate da ordenação, `index.html` e regra de data movida para o Service). Não aplicados: limites de tamanho do texto sem espelho no front e normalização do filtro a cada tecla.
- **Simplify** (skill `simplify`): aplicado no formulário, na tabela e na configuração de execução.
- **Segurança:** a skill `security-review` falhou na primeira tentativa (repositório sem commit nem remote), e a revisão inicial foi **manual**: `dotnet list package --vulnerable` e `npm audit` sem vulnerabilidades; DTO sem *mass assignment*; `.env` fora do git; Swagger desligado em produção. Depois do push a skill rodou, mas o diff veio vazio (tudo já estava no `origin/main`), então o código completo dos dois projetos foi revisado: nenhuma vulnerabilidade de alta confiança (sem injeção, XSS, segredos ou exposição em produção). Limitações assumidas neste escopo: sem autenticação, sem limite de requisições, lista em memória sem teto.
- **Produção simulada:** `dotnet publish` em `Production` com `PORT` e CORS por variável (health, Swagger desligado, CORS só para a origem configurada). O `Dockerfile` **não foi construído** localmente (Docker indisponível na máquina de desenvolvimento): confira o primeiro build no Render.

## Evolução

- EF Core + banco relacional (e Redis só como cache de leitura, se necessário).
- Paginação e filtro no servidor; ordenação configurável.
- Fuso configurável para a regra de data (hoje é o fuso do servidor).
- Autenticação, limite de requisições e versionamento da API.
- Validação de chave de NF-e e CI.

## Uso de IA

Este projeto foi desenvolvido com apoio de uma ferramenta de IA (Claude Code), que ajudou na estrutura, na implementação, nos testes e nas revisões de código e segurança. O autor é responsável pelas decisões e pelo código entregue.
