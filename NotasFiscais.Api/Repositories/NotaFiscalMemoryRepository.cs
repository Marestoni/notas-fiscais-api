using System.Collections.Concurrent;
using NotasFiscais.Api.Models;

namespace NotasFiscais.Api.Repositories;

// Registrado como Singleton: o estado vive enquanto o processo da API estiver de pé.
public class NotaFiscalMemoryRepository : INotaFiscalRepository
{
    private readonly ConcurrentDictionary<string, NotaFiscal> _notas = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<NotaFiscal> ObterTodas() =>
        _notas.Values
            .OrderBy(n => n.DataCadastro)
            .ThenBy(n => n.Numero, StringComparer.OrdinalIgnoreCase) // desempate: o dicionário não garante ordem
            .ToList();

    public NotaFiscal? ObterPorNumero(string numero) =>
        _notas.GetValueOrDefault(numero.Trim());

    public bool Adicionar(NotaFiscal nota) => _notas.TryAdd(nota.Numero, nota);
}
