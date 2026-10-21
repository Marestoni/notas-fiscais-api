using System.ComponentModel.DataAnnotations;
using NotasFiscais.Api.Validation;

namespace NotasFiscais.Api.Dtos;

public record CriarNotaRequest(
    [Required(AllowEmptyStrings = false), StringLength(30)] string? Numero,
    [Required(AllowEmptyStrings = false), StringLength(150)] string? Cliente,
    [Required,
     Range(0.01, 999999999999.99, ErrorMessage = "O valor deve estar entre 0,01 e 999.999.999.999,99."),
     MaxCasasDecimais(2)] decimal? Valor,
    [Required] DateOnly? DataEmissao);
