# Imagem de produção da API (usada pelo Render). Build em duas etapas: SDK compila, runtime executa.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restaura primeiro só com os .csproj para aproveitar o cache de camadas.
COPY NotasFiscais.Api/NotasFiscais.Api.csproj NotasFiscais.Api/
RUN dotnet restore NotasFiscais.Api/NotasFiscais.Api.csproj

COPY NotasFiscais.Api/ NotasFiscais.Api/
RUN dotnet publish NotasFiscais.Api/NotasFiscais.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Roda sem privilégios de root. A porta vem da variável PORT (definida pelo Render; ver Program.cs).
USER $APP_UID
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "NotasFiscais.Api.dll"]
